using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using RA2YR.Core.Binary.Seekable;
using RA2YR.Core.Formats.Mix;

namespace RA2YR.Core.Content.Mix
{
    internal enum MixMountDiagnosticSeverity
    {
        Warning,
        Error
    }

    internal enum MixMountIndexMode
    {
        StructureOnly,
        ManifestAudit
    }

    internal enum MixMountDiagnosticCode
    {
        IncompleteDirectoryIndex,
        RootArchiveMissing,
        RootArchiveDuplicate,
        RootArchiveNotMix,
        RootArchivePathRejected,
        RootArchiveReparsePoint,
        RootArchiveChanged,
        StructureOnlyPlatformUnsupported,
        ArchiveReadFailed,
        ArchiveIncomplete,
        InvalidEntryRange,
        ArchiveLimitExceeded,
        EntryLimitExceeded,
        NestingDepthExceeded,
        RepeatedArchiveRange,
        AmbiguousCandidateName,
        DuplicateLogicalName,
        PayloadHashFailed
    }

    internal sealed class MixMountDiagnostic
    {
        public MixMountDiagnostic(
            MixMountDiagnosticSeverity severity,
            MixMountDiagnosticCode code,
            string message,
            string sourceId,
            LogicalContentPath archivePath = null,
            MixFileId? entryId = null,
            MixDiagnostic formatDiagnostic = null)
        {
            Severity = severity;
            Code = code;
            Message = message ?? throw new ArgumentNullException(nameof(message));
            if (!ContentConfigurationValueRules.IsValidSourceId(sourceId))
            {
                throw new ArgumentException("A valid source id is required.", nameof(sourceId));
            }

            SourceId = sourceId;
            ArchivePath = archivePath;
            EntryId = entryId;
            FormatDiagnostic = formatDiagnostic;
        }

        public MixMountDiagnosticSeverity Severity { get; }

        public MixMountDiagnosticCode Code { get; }

        public string Message { get; }

        public string SourceId { get; }

        public LogicalContentPath ArchivePath { get; }

        public MixFileId? EntryId { get; }

        public MixDiagnostic FormatDiagnostic { get; }
    }

    internal sealed class MixMountLimits
    {
        public MixMountLimits(
            int maxNestedArchiveDepth,
            long maxMountedArchives,
            long maxTotalEntries,
            ReadOnlyDataWindowLimits windowLimits)
        {
            if (maxNestedArchiveDepth < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxNestedArchiveDepth));
            }

            if (maxMountedArchives < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxMountedArchives));
            }

            if (maxTotalEntries < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxTotalEntries));
            }

            MaxNestedArchiveDepth = maxNestedArchiveDepth;
            MaxMountedArchives = maxMountedArchives;
            MaxTotalEntries = maxTotalEntries;
            WindowLimits = windowLimits ?? throw new ArgumentNullException(nameof(windowLimits));
        }

        public static MixMountLimits Default { get; } = new MixMountLimits(
            16,
            1024,
            2_000_000,
            ReadOnlyDataWindowLimits.Default);

        public int MaxNestedArchiveDepth { get; }

        public long MaxMountedArchives { get; }

        public long MaxTotalEntries { get; }

        public ReadOnlyDataWindowLimits WindowLimits { get; }
    }

    internal sealed class MixArchiveProvenanceStep
    {
        public MixArchiveProvenanceStep(
            LogicalContentPath archivePath,
            MixFileId entryId,
            LogicalContentPath resolvedName)
        {
            ArchivePath = archivePath ?? throw new ArgumentNullException(nameof(archivePath));
            EntryId = entryId;
            ResolvedName = resolvedName;
        }

        public LogicalContentPath ArchivePath { get; }

        public MixFileId EntryId { get; }

        public LogicalContentPath ResolvedName { get; }
    }

    internal sealed class MixEntryProvenance
    {
        public MixEntryProvenance(
            ExternalContentSourceDescriptor source,
            LogicalContentPath rootArchivePath,
            IEnumerable<MixArchiveProvenanceStep> steps)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            RootArchivePath = rootArchivePath ??
                throw new ArgumentNullException(nameof(rootArchivePath));
            MixArchiveProvenanceStep[] stepArray =
                (steps ?? throw new ArgumentNullException(nameof(steps))).ToArray();
            if (stepArray.Length == 0 || stepArray.Any(step => step == null))
            {
                throw new ArgumentException(
                    "Entry provenance must contain at least one archive step.",
                    nameof(steps));
            }

            Steps = Array.AsReadOnly(stepArray);
        }

        public ExternalContentSourceDescriptor Source { get; }

        public LogicalContentPath RootArchivePath { get; }

        public IReadOnlyList<MixArchiveProvenanceStep> Steps { get; }
    }

    internal sealed class MixVirtualEntry
    {
        private readonly ReadOnlyDataWindow payloadWindow;

        internal MixVirtualEntry(
            MixFileId id,
            LogicalContentPath logicalName,
            long length,
            string sha256,
            MixEntryProvenance provenance,
            ReadOnlyDataWindow payloadWindow,
            bool isMountedArchive)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            if (sha256 != null && !Sha256Utilities.IsLowerSha256(sha256))
            {
                throw new ArgumentException("A lowercase SHA-256 value is required.", nameof(sha256));
            }

            Id = id;
            LogicalName = logicalName;
            Length = length;
            Sha256 = sha256;
            Provenance = provenance ?? throw new ArgumentNullException(nameof(provenance));
            this.payloadWindow = payloadWindow ?? throw new ArgumentNullException(nameof(payloadWindow));
            IsMountedArchive = isMountedArchive;
        }

        public MixFileId Id { get; }

        public LogicalContentPath LogicalName { get; }

        public bool HasResolvedName => LogicalName != null;

        public long Length { get; }

        public string Sha256 { get; }

        public bool HasSha256 => Sha256 != null;

        public MixEntryProvenance Provenance { get; }

        public bool IsMountedArchive { get; }

        internal ReadOnlyDataWindow PayloadWindow => payloadWindow;
    }

    internal sealed class MixMountedArchive
    {
        public MixMountedArchive(
            LogicalContentPath logicalPath,
            int nestedDepth,
            int entryCount,
            MixArchiveHeaderKind headerKind,
            MixArchiveFlags flags,
            bool checksumVerified)
        {
            LogicalPath = logicalPath ?? throw new ArgumentNullException(nameof(logicalPath));
            NestedDepth = nestedDepth;
            EntryCount = entryCount;
            HeaderKind = headerKind;
            Flags = flags;
            ChecksumVerified = checksumVerified;
        }

        public LogicalContentPath LogicalPath { get; }

        public int NestedDepth { get; }

        public int EntryCount { get; }

        public MixArchiveHeaderKind HeaderKind { get; }

        public MixArchiveFlags Flags { get; }

        public bool ChecksumVerified { get; }
    }

    internal sealed class MixVirtualContentMountResult : IDisposable
    {
        private readonly List<ReadOnlyDataWindowSession> pendingSessions;
        private readonly int ownerThreadId;
        private bool disposeStarted;
        private bool disposed;

        internal MixVirtualContentMountResult(
            ExternalContentSourceDescriptor source,
            IEnumerable<MixMountedArchive> archives,
            IEnumerable<MixVirtualEntry> entries,
            IEnumerable<MixMountDiagnostic> diagnostics,
            IEnumerable<ReadOnlyDataWindowSession> sessions,
            MixMountIndexMode indexMode,
            bool isComplete)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Archives = Array.AsReadOnly((archives ?? Enumerable.Empty<MixMountedArchive>()).ToArray());
            Entries = Array.AsReadOnly((entries ?? Enumerable.Empty<MixVirtualEntry>()).ToArray());
            Diagnostics = Array.AsReadOnly(
                (diagnostics ?? Enumerable.Empty<MixMountDiagnostic>()).ToArray());
            pendingSessions = (sessions ?? Enumerable.Empty<ReadOnlyDataWindowSession>()).ToList();
            if (pendingSessions.Any(session => session == null))
            {
                throw new ArgumentException("Window sessions may not contain null.", nameof(sessions));
            }

            ownerThreadId = Thread.CurrentThread.ManagedThreadId;
            IndexMode = indexMode;
            IsComplete = isComplete && Diagnostics.All(
                diagnostic => diagnostic.Severity != MixMountDiagnosticSeverity.Error);
        }

        public ExternalContentSourceDescriptor Source { get; }

        public IReadOnlyList<MixMountedArchive> Archives { get; }

        public IReadOnlyList<MixVirtualEntry> Entries { get; }

        public IReadOnlyList<MixMountDiagnostic> Diagnostics { get; }

        public bool IsComplete { get; }

        public MixMountIndexMode IndexMode { get; }

        public IReadOnlyList<MixVirtualEntry> FindById(MixFileId id)
        {
            EnsureUsable();
            return Array.AsReadOnly(Entries
                .Where(entry => entry.Id == id)
                .ToArray());
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            if (Thread.CurrentThread.ManagedThreadId != ownerThreadId)
            {
                throw new InvalidOperationException(
                    "A MIX mount must be disposed on the thread that created it.");
            }

            disposeStarted = true;
            var failures = new List<Exception>();
            for (int index = pendingSessions.Count - 1; index >= 0; index--)
            {
                try
                {
                    pendingSessions[index].Dispose();
                    pendingSessions.RemoveAt(index);
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                }
            }

            if (pendingSessions.Count == 0)
            {
                disposed = true;
            }

            if (failures.Count == 1)
            {
                ExceptionDispatchInfo.Capture(failures[0]).Throw();
            }

            if (failures.Count > 1)
            {
                throw new AggregateException(
                    "Multiple MIX mount sessions failed to dispose.",
                    failures);
            }
        }

        private void EnsureUsable()
        {
            if (disposed || disposeStarted)
            {
                throw new ObjectDisposedException(nameof(MixVirtualContentMountResult));
            }
        }
    }
}
