using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using RA2YR.Core.Binary;
using RA2YR.Core.Binary.Seekable;
using RA2YR.Core.Formats.Mix;

namespace RA2YR.Core.Content.Mix
{
    internal static class MixVirtualContentSource
    {
        public static MixVirtualContentMountResult MountDirectorySource(
            ContentSourceIndex directoryIndex,
            IEnumerable<LogicalContentPath> rootArchivePaths,
            MixNameCatalog nameCatalog,
            MixArchiveCatalogReader archiveReader,
            MixMountLimits limits = null,
            MixMountIndexMode indexMode = MixMountIndexMode.StructureOnly,
            Action postOpenValidationHook = null)
        {
            if (directoryIndex == null)
            {
                throw new ArgumentNullException(nameof(directoryIndex));
            }

            if (rootArchivePaths == null)
            {
                throw new ArgumentNullException(nameof(rootArchivePaths));
            }

            if (nameCatalog == null)
            {
                throw new ArgumentNullException(nameof(nameCatalog));
            }

            if (archiveReader == null)
            {
                throw new ArgumentNullException(nameof(archiveReader));
            }

            MixMountLimits effectiveLimits = limits ?? MixMountLimits.Default;
            if (!Enum.IsDefined(typeof(MixMountIndexMode), indexMode))
            {
                throw new ArgumentOutOfRangeException(nameof(indexMode));
            }

            LogicalContentPath[] requestedRoots = rootArchivePaths.ToArray();
            if (requestedRoots.Any(path => path == null))
            {
                throw new ArgumentException("Root archive paths may not contain null.", nameof(rootArchivePaths));
            }

            var state = new MountState(
                directoryIndex.Source,
                nameCatalog,
                archiveReader,
                effectiveLimits,
                indexMode);
            if (!directoryIndex.IsComplete)
            {
                state.Error(
                    MixMountDiagnosticCode.IncompleteDirectoryIndex,
                    "MIX mounting requires a complete directory-source index.");
                return state.Fail();
            }

            IGrouping<LogicalContentPath, LogicalContentPath> duplicateRoot = requestedRoots
                .GroupBy(path => path)
                .FirstOrDefault(group => group.Count() != 1);
            if (duplicateRoot != null)
            {
                state.Error(
                    MixMountDiagnosticCode.RootArchiveDuplicate,
                    "The same root MIX was requested more than once.",
                    duplicateRoot.Key);
                return state.Fail();
            }

            Dictionary<LogicalContentPath, ContentFileRecord> indexedFiles = directoryIndex.Files
                .ToDictionary(file => file.LogicalPath);
            foreach (LogicalContentPath requestedPath in requestedRoots
                         .OrderBy(path => path, LogicalContentPathReportComparer.Instance))
            {
                ContentFileRecord record;
                if (!indexedFiles.TryGetValue(requestedPath, out record))
                {
                    state.Error(
                        MixMountDiagnosticCode.RootArchiveMissing,
                        "The requested root MIX is not present in the trusted directory index.",
                        requestedPath);
                    return state.Fail();
                }

                if (!record.RelativePath.EndsWith(".mix", StringComparison.OrdinalIgnoreCase))
                {
                    state.Error(
                        MixMountDiagnosticCode.RootArchiveNotMix,
                        "Only files with a .mix logical name may be mounted as root archives.",
                        record.LogicalPath);
                    return state.Fail();
                }

                ReadOnlyDataWindowSession session;
                if (!TryOpenIndexedRoot(
                        directoryIndex,
                        record,
                        effectiveLimits,
                        indexMode,
                        postOpenValidationHook,
                        state,
                        out session))
                {
                    return state.Fail();
                }

                state.Sessions.Add(session);
                if (indexMode == MixMountIndexMode.ManifestAudit)
                {
                    string actualDigest;
                    try
                    {
                        actualDigest = session.Root.ComputeSha256("root-archive-identity");
                    }
                    catch (BinaryReadException)
                    {
                        state.Error(
                            MixMountDiagnosticCode.RootArchiveChanged,
                            "The root MIX could not be verified against its indexed digest.",
                            record.LogicalPath);
                        return state.Fail();
                    }

                    if (!string.Equals(actualDigest, record.Sha256, StringComparison.Ordinal))
                    {
                        state.Error(
                            MixMountDiagnosticCode.RootArchiveChanged,
                            "The root MIX changed after directory indexing; mounting was rejected.",
                            record.LogicalPath);
                        return state.Fail();
                    }
                }

                var rootRange = new PhysicalRangeIdentity(
                    record.LogicalPath,
                    session.Root.AbsoluteStartOffset,
                    session.Root.Length);
                if (!MountArchive(
                        state,
                        session.Root,
                        record.LogicalPath,
                        record.LogicalPath,
                        rootRange,
                        0,
                        Array.Empty<MixArchiveProvenanceStep>()))
                {
                    return state.Fail();
                }
            }

            return state.Complete();
        }

        private static bool TryOpenIndexedRoot(
            ContentSourceIndex directoryIndex,
            ContentFileRecord record,
            MixMountLimits limits,
            MixMountIndexMode indexMode,
            Action postOpenValidationHook,
            MountState state,
            out ReadOnlyDataWindowSession session)
        {
            session = null;
            ReadOnlyDataWindowSession localSession = null;
            string physicalPath;
            try
            {
                string nativeRelative = record.RelativePath.Replace(
                    '/',
                    Path.DirectorySeparatorChar);
                physicalPath = RepositoryPathPolicy.NormalizeAbsolutePath(
                    Path.Combine(directoryIndex.Source.RootPath, nativeRelative));
                if (!RepositoryPathPolicy.IsInsideOrEqual(
                        physicalPath,
                        directoryIndex.Source.RootPath))
                {
                    state.Error(
                        MixMountDiagnosticCode.RootArchivePathRejected,
                        "The indexed root MIX escaped its directory-source boundary.",
                        record.LogicalPath);
                    return false;
                }

                string reparsePath;
                if (RepositoryPathPolicy.ContainsExistingReparsePoint(
                        physicalPath,
                        out reparsePath))
                {
                    state.Error(
                        MixMountDiagnosticCode.RootArchiveReparsePoint,
                        "The indexed root MIX now traverses a reparse point.",
                        record.LogicalPath);
                    return false;
                }

                var info = new FileInfo(physicalPath);
                info.Refresh();
                if (!info.Exists || info.Length != record.Length)
                {
                    state.Error(
                        MixMountDiagnosticCode.RootArchiveChanged,
                        "The indexed root MIX length changed before mounting.",
                        record.LogicalPath);
                    return false;
                }

                long lengthBefore = info.Length;
                DateTime lastWriteBefore = info.LastWriteTimeUtc;
                FileAttributes attributesBefore = info.Attributes;

                if (indexMode == MixMountIndexMode.StructureOnly &&
                    Path.DirectorySeparatorChar != '\\')
                {
                    state.Error(
                        MixMountDiagnosticCode.StructureOnlyPlatformUnsupported,
                        "Structure-only mounting requires verified Windows file-sharing identity semantics.",
                        record.LogicalPath);
                    return false;
                }

                var context = new BinarySourceContext(
                    "format.mix-container-read",
                    directoryIndex.Source.Id,
                    record.LogicalPath);
                localSession = ReadOnlyDataWindowSession.FromFile(
                    physicalPath,
                    context,
                    limits.WindowLimits);

                postOpenValidationHook?.Invoke();

                info.Refresh();
                if (!info.Exists ||
                    info.Length != lengthBefore ||
                    info.LastWriteTimeUtc != lastWriteBefore ||
                    info.Attributes != attributesBefore)
                {
                    state.Error(
                        MixMountDiagnosticCode.RootArchiveChanged,
                        "The root MIX metadata changed while its read-only handle was acquired.",
                        record.LogicalPath);
                    return false;
                }

                session = localSession;
                localSession = null;
                return true;
            }
            catch (Exception exception) when (
                exception is IOException ||
                exception is UnauthorizedAccessException ||
                exception is SecurityException ||
                exception is NotSupportedException ||
                exception is ArgumentException ||
                exception is BinaryReadException)
            {
                state.Error(
                    MixMountDiagnosticCode.RootArchivePathRejected,
                    "The indexed root MIX could not be opened through the safe path boundary.",
                    record.LogicalPath);
                return false;
            }
            finally
            {
                if (localSession != null)
                {
                    try
                    {
                        localSession.Dispose();
                    }
                    catch (Exception exception) when (
                        exception is IOException ||
                        exception is ObjectDisposedException ||
                        exception is InvalidOperationException)
                    {
                        state.Sessions.Add(localSession);
                        state.Error(
                            MixMountDiagnosticCode.RootArchivePathRejected,
                            "A failed root MIX open retained a cleanup-only handle for disposal retry.",
                            record.LogicalPath);
                    }

                    localSession = null;
                }
            }
        }

        private static bool MountArchive(
            MountState state,
            ReadOnlyDataWindow archiveWindow,
            LogicalContentPath archivePath,
            LogicalContentPath rootArchivePath,
            PhysicalRangeIdentity range,
            int nestedDepth,
            IReadOnlyList<MixArchiveProvenanceStep> inheritedSteps)
        {
            if (nestedDepth > state.Limits.MaxNestedArchiveDepth)
            {
                state.Error(
                    MixMountDiagnosticCode.NestingDepthExceeded,
                    "The nested MIX depth budget was exceeded.",
                    archivePath);
                return false;
            }

            long updatedArchiveCount;
            try
            {
                updatedArchiveCount = checked(state.ArchiveCount + 1);
            }
            catch (OverflowException)
            {
                state.Error(
                    MixMountDiagnosticCode.ArchiveLimitExceeded,
                    "Mounted archive accounting overflowed.",
                    archivePath);
                return false;
            }

            if (updatedArchiveCount > state.Limits.MaxMountedArchives)
            {
                state.Error(
                    MixMountDiagnosticCode.ArchiveLimitExceeded,
                    "The mounted archive count budget was exceeded.",
                    archivePath);
                return false;
            }

            if (!state.MountedRanges.Add(range))
            {
                state.Error(
                    MixMountDiagnosticCode.RepeatedArchiveRange,
                    "A physical archive range was encountered more than once; recursive mounting was stopped.",
                    archivePath);
                return false;
            }

            state.ArchiveCount = updatedArchiveCount;
            MixArchiveCatalog catalog;
            try
            {
                var source = new BinarySourceContext(
                    "format.mix-container-read",
                    state.Source.Id,
                    archivePath);
                catalog = state.ArchiveReader(archiveWindow, source);
            }
            catch (Exception exception) when (
                exception is BinaryReadException ||
                exception is IOException ||
                exception is InvalidDataException ||
                exception is NotSupportedException ||
                exception is ArgumentException ||
                exception is OverflowException)
            {
                state.Error(
                    MixMountDiagnosticCode.ArchiveReadFailed,
                    "The MIX reader rejected the bounded archive window.",
                    archivePath);
                return false;
            }

            if (catalog == null)
            {
                state.Error(
                    MixMountDiagnosticCode.ArchiveIncomplete,
                    "An incomplete MIX parse cannot be mounted as content.",
                    archivePath);
                return false;
            }

            if (!catalog.IsComplete)
            {
                if (catalog.Diagnostics.Count == 0)
                {
                    state.Error(
                        MixMountDiagnosticCode.ArchiveIncomplete,
                        "An incomplete MIX parse cannot be mounted as content.",
                        archivePath);
                }
                else
                {
                    foreach (MixDiagnostic diagnostic in catalog.Diagnostics)
                    {
                        state.FormatError(diagnostic, archivePath);
                    }
                }

                return false;
            }

            long updatedEntryCount;
            try
            {
                updatedEntryCount = checked(state.EntryCount + catalog.Entries.Count);
            }
            catch (OverflowException)
            {
                state.Error(
                    MixMountDiagnosticCode.EntryLimitExceeded,
                    "Mounted entry accounting overflowed.",
                    archivePath);
                return false;
            }

            if (updatedEntryCount > state.Limits.MaxTotalEntries)
            {
                state.Error(
                    MixMountDiagnosticCode.EntryLimitExceeded,
                    "The total mounted entry budget was exceeded.",
                    archivePath);
                return false;
            }

            state.EntryCount = updatedEntryCount;
            state.Archives.Add(new MixMountedArchive(
                archivePath,
                nestedDepth,
                catalog.Entries.Count,
                catalog.HeaderKind.Value,
                catalog.Flags,
                catalog.ChecksumVerified));

            foreach (MixArchiveCatalogEntry catalogEntry in catalog.Entries)
            {
                ReadOnlyDataWindow payload;
                try
                {
                    payload = archiveWindow.CreateChild(
                        catalogEntry.PayloadOffsetFromArchiveStart,
                        catalogEntry.Length,
                        "mix-entry-payload");
                }
                catch (BinaryReadException)
                {
                    state.Error(
                        MixMountDiagnosticCode.InvalidEntryRange,
                        "A MIX entry payload crosses its parent archive window.",
                        archivePath,
                        catalogEntry.Id);
                    return false;
                }

                LogicalContentPath resolvedName;
                bool resolved = state.NameCatalog.TryResolve(
                    catalogEntry.Id,
                    out resolvedName);
                if (!resolved && state.NameCatalog.IsAmbiguous(catalogEntry.Id))
                {
                    state.Warning(
                        MixMountDiagnosticCode.AmbiguousCandidateName,
                        "Multiple controlled candidate names share this ID; the entry remains unnamed.",
                        archivePath,
                        catalogEntry.Id);
                }

                var steps = new List<MixArchiveProvenanceStep>(inheritedSteps)
                {
                    new MixArchiveProvenanceStep(
                        archivePath,
                        catalogEntry.Id,
                        resolvedName)
                };
                string sha256 = null;
                if (state.IndexMode == MixMountIndexMode.ManifestAudit)
                {
                    try
                    {
                        sha256 = payload.ComputeSha256("mix-entry-payload-hash");
                    }
                    catch (BinaryReadException)
                    {
                        state.Error(
                            MixMountDiagnosticCode.PayloadHashFailed,
                            "A MIX entry payload could not be hashed within its bounded window.",
                            archivePath,
                            catalogEntry.Id);
                        return false;
                    }
                }

                bool isNestedArchive = resolvedName != null &&
                    resolvedName.Value.EndsWith(".mix", StringComparison.OrdinalIgnoreCase);

                var entry = new MixVirtualEntry(
                    catalogEntry.Id,
                    resolvedName,
                    catalogEntry.Length,
                    sha256,
                    new MixEntryProvenance(state.Source, rootArchivePath, steps),
                    payload,
                    isNestedArchive);
                state.Entries.Add(entry);

                if (!isNestedArchive)
                {
                    continue;
                }

                LogicalContentPath nestedArchivePath;
                try
                {
                    nestedArchivePath = LogicalContentPath.Parse(
                        archivePath.Value + "/" + resolvedName.Value);
                }
                catch (ArgumentException)
                {
                    state.Error(
                        MixMountDiagnosticCode.ArchiveReadFailed,
                        "The nested archive address cannot be represented safely.",
                        archivePath,
                        catalogEntry.Id);
                    return false;
                }

                var nestedRange = new PhysicalRangeIdentity(
                    rootArchivePath,
                    payload.AbsoluteStartOffset,
                    payload.Length);
                if (nestedDepth == int.MaxValue)
                {
                    state.Error(
                        MixMountDiagnosticCode.NestingDepthExceeded,
                        "The nested MIX depth cannot be represented safely.",
                        archivePath,
                        catalogEntry.Id);
                    return false;
                }

                if (!MountArchive(
                        state,
                        payload,
                        nestedArchivePath,
                        rootArchivePath,
                        nestedRange,
                        nestedDepth + 1,
                        steps))
                {
                    return false;
                }
            }

            return true;
        }

        private sealed class MountState
        {
            public MountState(
                ExternalContentSourceDescriptor source,
                MixNameCatalog nameCatalog,
                MixArchiveCatalogReader archiveReader,
                MixMountLimits limits,
                MixMountIndexMode indexMode)
            {
                Source = source;
                NameCatalog = nameCatalog;
                ArchiveReader = archiveReader;
                Limits = limits;
                IndexMode = indexMode;
            }

            public ExternalContentSourceDescriptor Source { get; }

            public MixNameCatalog NameCatalog { get; }

            public MixArchiveCatalogReader ArchiveReader { get; }

            public MixMountLimits Limits { get; }

            public MixMountIndexMode IndexMode { get; }

            public List<MixMountedArchive> Archives { get; } =
                new List<MixMountedArchive>();

            public List<MixVirtualEntry> Entries { get; } =
                new List<MixVirtualEntry>();

            public List<MixMountDiagnostic> Diagnostics { get; } =
                new List<MixMountDiagnostic>();

            public List<ReadOnlyDataWindowSession> Sessions { get; } =
                new List<ReadOnlyDataWindowSession>();

            public HashSet<PhysicalRangeIdentity> MountedRanges { get; } =
                new HashSet<PhysicalRangeIdentity>();

            public long ArchiveCount { get; set; }

            public long EntryCount { get; set; }

            public void Error(
                MixMountDiagnosticCode code,
                string message,
                LogicalContentPath archivePath = null,
                MixFileId? entryId = null)
            {
                Diagnostics.Add(new MixMountDiagnostic(
                    MixMountDiagnosticSeverity.Error,
                    code,
                    message,
                    Source.Id,
                    archivePath,
                    entryId));
            }

            public void Warning(
                MixMountDiagnosticCode code,
                string message,
                LogicalContentPath archivePath,
                MixFileId entryId)
            {
                Diagnostics.Add(new MixMountDiagnostic(
                    MixMountDiagnosticSeverity.Warning,
                    code,
                    message,
                    Source.Id,
                    archivePath,
                    entryId));
            }

            public void FormatError(
                MixDiagnostic diagnostic,
                LogicalContentPath archivePath)
            {
                Diagnostics.Add(new MixMountDiagnostic(
                    MixMountDiagnosticSeverity.Error,
                    MixMountDiagnosticCode.ArchiveIncomplete,
                    "The MIX format reader returned a structured failure.",
                    Source.Id,
                    archivePath,
                    diagnostic.EntryId,
                    diagnostic));
            }

            public MixVirtualContentMountResult Complete()
            {
                return new MixVirtualContentMountResult(
                    Source,
                    Archives,
                    Entries,
                    Diagnostics,
                    Sessions,
                    IndexMode,
                    true);
            }

            public MixVirtualContentMountResult Fail()
            {
                var cleanupFailures = new List<ReadOnlyDataWindowSession>();
                for (int index = Sessions.Count - 1; index >= 0; index--)
                {
                    try
                    {
                        Sessions[index].Dispose();
                    }
                    catch (Exception)
                    {
                        cleanupFailures.Add(Sessions[index]);
                    }
                }

                Sessions.Clear();
                cleanupFailures.Reverse();
                if (cleanupFailures.Count != 0)
                {
                    Error(
                        MixMountDiagnosticCode.RootArchivePathRejected,
                        "One or more cleanup-only MIX handles require a disposal retry.");
                }

                return new MixVirtualContentMountResult(
                    Source,
                    Array.Empty<MixMountedArchive>(),
                    Array.Empty<MixVirtualEntry>(),
                    Diagnostics,
                    cleanupFailures,
                    IndexMode,
                    false);
            }
        }

        private readonly struct PhysicalRangeIdentity : IEquatable<PhysicalRangeIdentity>
        {
            public PhysicalRangeIdentity(
                LogicalContentPath rootArchivePath,
                long absoluteStart,
                long length)
            {
                RootArchivePath = rootArchivePath ??
                    throw new ArgumentNullException(nameof(rootArchivePath));
                AbsoluteStart = absoluteStart;
                Length = length;
            }

            private LogicalContentPath RootArchivePath { get; }

            private long AbsoluteStart { get; }

            private long Length { get; }

            public bool Equals(PhysicalRangeIdentity other)
            {
                return RootArchivePath.Equals(other.RootArchivePath) &&
                       AbsoluteStart == other.AbsoluteStart &&
                       Length == other.Length;
            }

            public override bool Equals(object obj)
            {
                return obj is PhysicalRangeIdentity other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = RootArchivePath.GetHashCode();
                    hash = (hash * 397) ^ AbsoluteStart.GetHashCode();
                    hash = (hash * 397) ^ Length.GetHashCode();
                    return hash;
                }
            }
        }
    }
}
