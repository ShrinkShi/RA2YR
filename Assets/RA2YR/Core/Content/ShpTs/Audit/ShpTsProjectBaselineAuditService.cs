using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using RA2YR.Core.Binary;
using RA2YR.Core.Content.Mix;
using RA2YR.Core.Formats.Mix;
using RA2YR.Core.Formats.ShpTs;

namespace RA2YR.Core.Content.ShpTs.Audit
{
    public static class ShpTsProjectBaselineAuditService
    {
        public const string BaselineLogicalName = "YR1001_ProjectBaseline";

        public static ShpTsProjectBaselineAuditDelivery Run(
            ExternalContentConfiguration configuration)
        {
            return RunCore(
                configuration,
                ShpTsProjectBaselineAuditProfile.ProjectBaseline,
                value => new ContentIndexer().Build(value),
                () => DateTime.UtcNow);
        }

        internal static ShpTsProjectBaselineAuditDelivery RunForTesting(
            ExternalContentConfiguration configuration,
            ShpTsProjectBaselineAuditProfile profile,
            Func<ExternalContentConfiguration, ContentIndexResult> buildIndex,
            Func<DateTime> utcNow)
        {
            return RunCore(configuration, profile, buildIndex, utcNow);
        }

        internal static TResult UseFixedEntries<TResult>(
            ExternalContentConfiguration configuration,
            Func<
                ExternalContentSourceDescriptor,
                string,
                IReadOnlyList<ShpTsGoldenSampleEntryContext>,
                TResult> inspect)
        {
            if (configuration == null || inspect == null)
            {
                throw new ArgumentNullException(
                    configuration == null ? nameof(configuration) : nameof(inspect));
            }

            ShpTsProjectBaselineAuditProfile profile =
                ShpTsProjectBaselineAuditProfile.ProjectBaseline;
            ExternalContentSourceDescriptor source = ValidateConfiguration(configuration);
            ContentSourceIndex beforeSource = GetBaselineSource(
                BuildCompleteIndex(configuration, value => new ContentIndexer().Build(value)));
            RejectLooseCandidates(beforeSource, profile);

            MixNameCatalog nameCatalog = BuildNameCatalog(profile);
            var mounts = new List<MixVirtualContentMountResult>();
            Exception operationFailure = null;
            TResult result = default(TResult);
            bool hasResult = false;
            try
            {
                foreach (LogicalContentPath root in profile.Samples
                             .Select(sample => sample.RootArchive)
                             .Distinct()
                             .OrderBy(value => value, LogicalContentPathReportComparer.Instance))
                {
                    MixVirtualContentMountResult mount = MixVirtualContentSource.MountDirectorySource(
                        beforeSource,
                        new[] { root },
                        nameCatalog,
                        MixArchiveCatalogAdapters.ReadWithCoreReader,
                        profile.MountLimits,
                        MixMountIndexMode.ManifestAudit);
                    mounts.Add(mount);
                    if (!mount.IsComplete || mount.Diagnostics.Any(value =>
                            value.Severity == MixMountDiagnosticSeverity.Error))
                    {
                        throw Failure(
                            ShpTsProjectBaselineAuditFailureCode.MixMountFailed,
                            "A controlled SHP root MIX mount failed closed.");
                    }
                }

                IReadOnlyList<ShpTsGoldenSampleEntryContext> entries = Array.AsReadOnly(
                    profile.Samples.Select(specification =>
                        new ShpTsGoldenSampleEntryContext(
                            specification,
                            FindExactEntry(mounts, specification)))
                    .ToArray());
                result = inspect(source, beforeSource.Fingerprint, entries);
                hasResult = true;

                ContentSourceIndex afterSource = GetBaselineSource(
                    BuildCompleteIndex(configuration, value => new ContentIndexer().Build(value)));
                if (!string.Equals(
                        beforeSource.Fingerprint,
                        afterSource.Fingerprint,
                        StringComparison.Ordinal))
                {
                    throw Failure(
                        ShpTsProjectBaselineAuditFailureCode.BaselineChangedDuringAudit,
                        "The ProjectBaseline fingerprint changed during the SHP forensic read.");
                }
            }
            catch (Exception exception)
            {
                operationFailure = exception;
            }

            int cleanupFailures = DisposeAll(mounts);
            ThrowAfterCleanup(operationFailure, cleanupFailures);
            if (!hasResult)
            {
                throw new InvalidOperationException(
                    "The fixed SHP entry callback ended without a result or structured failure.");
            }

            return result;
        }

        private static ShpTsProjectBaselineAuditDelivery RunCore(
            ExternalContentConfiguration configuration,
            ShpTsProjectBaselineAuditProfile profile,
            Func<ExternalContentConfiguration, ContentIndexResult> buildIndex,
            Func<DateTime> utcNow)
        {
            if (configuration == null || profile == null ||
                buildIndex == null || utcNow == null)
            {
                throw new ArgumentNullException(configuration == null
                    ? nameof(configuration)
                    : profile == null
                        ? nameof(profile)
                        : buildIndex == null ? nameof(buildIndex) : nameof(utcNow));
            }

            ExternalContentSourceDescriptor source = ValidateConfiguration(configuration);
            DateTime startedUtc = utcNow().ToUniversalTime();
            ContentSourceIndex beforeSource = GetBaselineSource(
                BuildCompleteIndex(configuration, buildIndex));
            RejectLooseCandidates(beforeSource, profile);

            MixNameCatalog nameCatalog = BuildNameCatalog(profile);
            var mounts = new List<MixVirtualContentMountResult>();
            Exception operationFailure = null;
            ShpTsProjectBaselineAuditDelivery delivery = null;
            try
            {
                foreach (LogicalContentPath root in profile.Samples
                             .Select(sample => sample.RootArchive)
                             .Distinct()
                             .OrderBy(value => value, LogicalContentPathReportComparer.Instance))
                {
                    MixVirtualContentMountResult mount = MixVirtualContentSource.MountDirectorySource(
                        beforeSource,
                        new[] { root },
                        nameCatalog,
                        MixArchiveCatalogAdapters.ReadWithCoreReader,
                        profile.MountLimits,
                        MixMountIndexMode.ManifestAudit);
                    mounts.Add(mount);
                    if (!mount.IsComplete || mount.Diagnostics.Any(value =>
                            value.Severity == MixMountDiagnosticSeverity.Error))
                    {
                        throw Failure(
                            ShpTsProjectBaselineAuditFailureCode.MixMountFailed,
                            "A controlled SHP root MIX mount failed closed.");
                    }
                }

                var records = new List<ShpTsGoldenSampleRecord>(profile.Samples.Count);
                foreach (ShpTsGoldenSampleSpecification specification in profile.Samples)
                {
                    MixVirtualEntry entry = FindExactEntry(mounts, specification);
                    records.Add(AuditEntry(entry, specification, profile.ReadLimits));
                }

                ContentSourceIndex afterSource = GetBaselineSource(
                    BuildCompleteIndex(configuration, buildIndex));
                if (!string.Equals(
                    beforeSource.Fingerprint,
                    afterSource.Fingerprint,
                    StringComparison.Ordinal))
                {
                    throw Failure(
                        ShpTsProjectBaselineAuditFailureCode.BaselineChangedDuringAudit,
                        "The ProjectBaseline fingerprint changed during the SHP audit.");
                }

                DateTime completedUtc = utcNow().ToUniversalTime();
                var model = new ShpTsProjectBaselineAuditModel(
                    source,
                    beforeSource.Fingerprint,
                    records,
                    startedUtc,
                    completedUtc);
                byte[] externalBytes = ShpTsProjectBaselineAuditSerializer
                    .SerializeExternalManifestUtf8(
                        model,
                        profile.MaxExternalManifestUtf8Bytes);
                ShpTsAuditExternalManifestReference external =
                    ShpTsAuditExternalManifestWriter.Write(
                        configuration,
                        source.Id,
                        beforeSource.Fingerprint,
                        externalBytes);
                string summary = ShpTsProjectBaselineAuditSerializer
                    .SerializeSanitizedSummary(model, external);
                int unresolved = records.Sum(record => record.UnresolvedFrameCount);
                int failed = records.Sum(record => record.FailedFrameCount);
                delivery = new ShpTsProjectBaselineAuditDelivery(
                    failed != 0
                        ? ShpTsProjectBaselineAuditStatus.CompleteWithDecodeFailures
                        : unresolved == 0
                            ? ShpTsProjectBaselineAuditStatus.Complete
                            : ShpTsProjectBaselineAuditStatus.CompleteWithUnresolvedFrames,
                    records.Count,
                    unresolved,
                    failed,
                    summary,
                    external.CacheRelativePath,
                    external.Length,
                    external.Sha256);
            }
            catch (Exception exception)
            {
                operationFailure = exception;
            }

            int cleanupFailures = DisposeAll(mounts);
            ThrowAfterCleanup(operationFailure, cleanupFailures);
            return delivery ?? throw new InvalidOperationException(
                "The SHP audit ended without a delivery or structured failure.");
        }

        private static ExternalContentSourceDescriptor ValidateConfiguration(
            ExternalContentConfiguration configuration)
        {
            ExternalContentSourceDescriptor[] enabled = configuration.Sources
                .Where(value => value.Enabled)
                .ToArray();
            if (enabled.Length != 1 ||
                !string.Equals(enabled[0].Id, BaselineLogicalName, StringComparison.Ordinal) ||
                enabled[0].Kind != ContentSourceKind.Patched)
            {
                throw Failure(
                    ShpTsProjectBaselineAuditFailureCode.InvalidBaselineConfiguration,
                    "The SHP audit requires exactly one enabled patched YR1001_ProjectBaseline source.");
            }

            return enabled[0];
        }

        private static ContentIndexResult BuildCompleteIndex(
            ExternalContentConfiguration configuration,
            Func<ExternalContentConfiguration, ContentIndexResult> buildIndex)
        {
            ContentIndexResult result;
            try
            {
                result = buildIndex(configuration);
            }
            catch (Exception exception) when (IsExpectedReadFailure(exception))
            {
                throw Failure(
                    ShpTsProjectBaselineAuditFailureCode.DirectoryIndexIncomplete,
                    "The controlled ProjectBaseline directory index failed closed.");
            }

            if (result == null || !result.IsComplete || result.HasErrors)
            {
                throw Failure(
                    ShpTsProjectBaselineAuditFailureCode.DirectoryIndexIncomplete,
                    "The controlled ProjectBaseline directory index is incomplete.");
            }

            return result;
        }

        private static ContentSourceIndex GetBaselineSource(ContentIndexResult index)
        {
            ContentSourceIndex[] matches = index.Sources.Where(value =>
                    string.Equals(
                        value.Source.Id,
                        BaselineLogicalName,
                        StringComparison.Ordinal))
                .ToArray();
            if (matches.Length != 1 || !matches[0].IsComplete)
            {
                throw Failure(
                    ShpTsProjectBaselineAuditFailureCode.DirectoryIndexIncomplete,
                    "The controlled ProjectBaseline source index is incomplete.");
            }

            return matches[0];
        }

        private static void RejectLooseCandidates(
            ContentSourceIndex source,
            ShpTsProjectBaselineAuditProfile profile)
        {
            HashSet<LogicalContentPath> names = new HashSet<LogicalContentPath>(
                profile.Samples.Select(value => value.LogicalName));
            if (source.Files.Any(file => names.Contains(file.LogicalPath)))
            {
                throw Failure(
                    ShpTsProjectBaselineAuditFailureCode.LooseCandidateFound,
                    "A loose SHP candidate cannot replace a controlled MIX entry.");
            }
        }

        private static MixNameCatalog BuildNameCatalog(
            ShpTsProjectBaselineAuditProfile profile)
        {
            var names = new HashSet<LogicalContentPath>();
            foreach (ShpTsGoldenSampleSpecification sample in profile.Samples)
            {
                names.Add(sample.LogicalName);
                foreach (LogicalContentPath archive in sample.ExpectedArchiveChain.Skip(1))
                {
                    int slash = archive.Value.LastIndexOf('/');
                    names.Add(LogicalContentPath.Parse(slash < 0
                        ? archive.Value
                        : archive.Value.Substring(slash + 1)));
                }
            }

            return new MixNameCatalog(names);
        }

        private static MixVirtualEntry FindExactEntry(
            IEnumerable<MixVirtualContentMountResult> mounts,
            ShpTsGoldenSampleSpecification specification)
        {
            MixVirtualContentMountResult[] rootMatches = mounts.Where(mount =>
                    mount.Archives.Any(archive =>
                        archive.NestedDepth == 0 &&
                        archive.LogicalPath.Equals(specification.RootArchive)))
                .ToArray();
            if (rootMatches.Length == 0)
            {
                throw Failure(
                    ShpTsProjectBaselineAuditFailureCode.RootArchiveMissing,
                    "A fixed SHP root archive is missing.");
            }

            MixVirtualEntry[] idMatches = rootMatches
                .SelectMany(mount => mount.FindById(specification.ExpectedMixId))
                .Where(entry => entry.HasResolvedName &&
                    entry.LogicalName.Equals(specification.LogicalName))
                .ToArray();
            MixVirtualEntry[] exact = idMatches.Where(entry =>
                    ProvenanceMatches(entry.Provenance, specification))
                .ToArray();
            if (exact.Length == 0)
            {
                throw Failure(
                    idMatches.Length == 0
                        ? ShpTsProjectBaselineAuditFailureCode.TargetMissing
                        : ShpTsProjectBaselineAuditFailureCode.TargetProvenanceMismatch,
                    "A fixed SHP entry is missing or has changed provenance.");
            }

            if (exact.Length != 1)
            {
                throw Failure(
                    ShpTsProjectBaselineAuditFailureCode.TargetAmbiguous,
                    "A fixed SHP entry is ambiguous within its expected provenance.");
            }

            MixVirtualEntry selected = exact[0];
            if (selected.Id != specification.ExpectedMixId)
            {
                throw Failure(
                    ShpTsProjectBaselineAuditFailureCode.TargetIdentityMismatch,
                    "A fixed SHP MIX id changed.");
            }

            if (selected.Length != specification.ExpectedLength)
            {
                throw Failure(
                    ShpTsProjectBaselineAuditFailureCode.TargetLengthMismatch,
                    "A fixed SHP entry length changed.");
            }

            if (!selected.HasSha256 || !string.Equals(
                    selected.Sha256,
                    specification.ExpectedSha256,
                    StringComparison.Ordinal))
            {
                throw Failure(
                    ShpTsProjectBaselineAuditFailureCode.TargetHashMismatch,
                    "A fixed SHP entry digest changed.");
            }

            return selected;
        }

        private static bool ProvenanceMatches(
            MixEntryProvenance provenance,
            ShpTsGoldenSampleSpecification specification)
        {
            if (provenance == null ||
                !provenance.RootArchivePath.Equals(specification.RootArchive) ||
                provenance.Steps.Count != specification.ExpectedArchiveChain.Count)
            {
                return false;
            }

            for (int index = 0; index < provenance.Steps.Count; index++)
            {
                MixArchiveProvenanceStep step = provenance.Steps[index];
                if (!step.ArchivePath.Equals(specification.ExpectedArchiveChain[index]) ||
                    step.ResolvedName == null)
                {
                    return false;
                }
            }

            return provenance.Steps[provenance.Steps.Count - 1]
                .ResolvedName.Equals(specification.LogicalName);
        }

        private static ShpTsGoldenSampleRecord AuditEntry(
            MixVirtualEntry entry,
            ShpTsGoldenSampleSpecification specification,
            ShpTsReadLimits limits)
        {
            byte[] bytes = ReadWindowBytes(entry, limits);
            ShpTsSourceProvenance provenance = BuildFormatProvenance(entry);
            var source = new BinarySourceContext(
                "format.shp-ts-project-baseline",
                entry.Provenance.Source.Id,
                specification.LogicalName);
            long absoluteStart = entry.PayloadWindow.AbsoluteStartOffset;

            ShpTsParseResult memoryParse = WestwoodShpTsReader.Read(
                bytes,
                source,
                provenance,
                limits,
                absoluteStart);
            ShpTsParseResult streamParse;
            using (var stream = new MemoryStream(bytes, false))
            {
                streamParse = WestwoodShpTsReader.Read(
                    stream,
                    bytes.LongLength,
                    source,
                    provenance,
                    limits,
                    false,
                    absoluteStart);
            }

            ShpTsParseResult windowParse = WestwoodShpTsReader.Read(
                entry.PayloadWindow,
                source,
                provenance,
                limits);
            if (!memoryParse.IsSuccess || !streamParse.IsSuccess || !windowParse.IsSuccess)
            {
                throw Failure(
                    ShpTsProjectBaselineAuditFailureCode.ShpParseFailed,
                    "A fixed SHP entry failed strict directory parsing.");
            }

            if (!DirectoryEquivalent(memoryParse.Document, streamParse.Document) ||
                !DirectoryEquivalent(memoryParse.Document, windowParse.Document) ||
                !DiagnosticsEquivalent(memoryParse.Diagnostics, streamParse.Diagnostics) ||
                !DiagnosticsEquivalent(memoryParse.Diagnostics, windowParse.Diagnostics))
            {
                throw Failure(
                    ShpTsProjectBaselineAuditFailureCode.InputModeMismatch,
                    "Memory, Stream, and MIX-window SHP directory results differ.");
            }

            if (specification.ExpectedDirectoryModelSha256 != null &&
                !string.Equals(
                    memoryParse.Document.CanonicalDirectoryModelSha256,
                    specification.ExpectedDirectoryModelSha256,
                    StringComparison.Ordinal))
            {
                throw Failure(
                    ShpTsProjectBaselineAuditFailureCode.DirectoryModelHashMismatch,
                    "A fixed SHP directory model digest changed.");
            }

            var decodedFrames = new List<ShpTsIndexedLocalFrame>();
            var frameRecords = new List<ShpTsAuditFrameRecord>();
            var diagnostics = new List<ShpTsDiagnostic>(memoryParse.Diagnostics);
            foreach (ShpTsFrameDescriptor descriptor in memoryParse.Document.Frames)
            {
                ShpTsDecodeResult memory = WestwoodShpTsDecoder.DecodeFrame(
                    bytes,
                    memoryParse.Document,
                    descriptor.Index,
                    limits);
                ShpTsDecodeResult streamed;
                using (var stream = new MemoryStream(bytes, false))
                {
                    streamed = WestwoodShpTsDecoder.DecodeFrame(
                        stream,
                        bytes.LongLength,
                        streamParse.Document,
                        descriptor.Index,
                        limits,
                        false);
                }

                ShpTsDecodeResult windowed = WestwoodShpTsDecoder.DecodeFrame(
                    entry.PayloadWindow,
                    windowParse.Document,
                    descriptor.Index,
                    limits);
                if (!DecodeEquivalent(memory, streamed) ||
                    !DecodeEquivalent(memory, windowed))
                {
                    throw Failure(
                        ShpTsProjectBaselineAuditFailureCode.InputModeMismatch,
                        "Memory, Stream, and MIX-window SHP frame results differ.");
                }

                diagnostics.AddRange(memory.Diagnostics);
                if (memory.IsSuccess)
                {
                    decodedFrames.Add(memory.Frame);
                    frameRecords.Add(new ShpTsAuditFrameRecord(
                        descriptor,
                        "decoded",
                        memory.Frame.BytesConsumed,
                        memory.Frame.PaddingBytes,
                        memory.Frame.PixelCount,
                        memory.Frame.MinimumIndex,
                        memory.Frame.MaximumIndex,
                        memory.Diagnostics));
                    continue;
                }

                if (!IsControlledUnresolved(memory))
                {
                    frameRecords.Add(new ShpTsAuditFrameRecord(
                        descriptor,
                        "failed",
                        0,
                        0,
                        0,
                        0,
                        0,
                        memory.Diagnostics));
                    continue;
                }

                frameRecords.Add(new ShpTsAuditFrameRecord(
                    descriptor,
                    "unresolved",
                    0,
                    0,
                    0,
                    0,
                    0,
                    memory.Diagnostics));
            }

            var decoded = new ShpTsDecodedDocument(decodedFrames);
            if (specification.ExpectedDecodedModelSha256 != null &&
                !string.Equals(
                    decoded.CanonicalDecodedModelSha256,
                    specification.ExpectedDecodedModelSha256,
                    StringComparison.Ordinal))
            {
                throw Failure(
                    ShpTsProjectBaselineAuditFailureCode.DecodedModelHashMismatch,
                    "A fixed SHP decoded model digest changed.");
            }

            return new ShpTsGoldenSampleRecord(
                specification,
                entry.Provenance.Steps.Select(step => new ShpTsAuditProvenanceLayer(
                    step.ArchivePath,
                    step.EntryId,
                    step.ResolvedName)),
                entry.Length,
                entry.Sha256,
                memoryParse.Document,
                decoded,
                frameRecords,
                diagnostics,
                true);
        }

        private static byte[] ReadWindowBytes(
            MixVirtualEntry entry,
            ShpTsReadLimits limits)
        {
            if (entry.Length > limits.MaxInputBytes ||
                entry.Length > limits.MaxAllocatedBytes ||
                entry.Length > int.MaxValue)
            {
                throw Failure(
                    ShpTsProjectBaselineAuditFailureCode.ShpParseFailed,
                    "A fixed SHP entry exceeds its explicit audit input budget.");
            }

            var bytes = new byte[checked((int)entry.Length)];
            int offset = 0;
            int chunkLimit = checked((int)Math.Max(1,
                Math.Min(limits.MaxSingleReadBytes, int.MaxValue)));
            while (offset < bytes.Length)
            {
                int count = Math.Min(chunkLimit, bytes.Length - offset);
                entry.PayloadWindow.ReadExactly(
                    offset,
                    bytes,
                    offset,
                    count,
                    "shp-project-baseline-snapshot");
                offset = checked(offset + count);
            }

            return bytes;
        }

        private static ShpTsSourceProvenance BuildFormatProvenance(MixVirtualEntry entry)
        {
            var chain = new List<LogicalContentPath>();
            chain.AddRange(entry.Provenance.Steps.Select(step => step.ArchivePath));
            chain.Add(entry.LogicalName);
            return new ShpTsSourceProvenance(entry.Provenance.Source.Id, chain);
        }

        private static bool DirectoryEquivalent(ShpTsDocument left, ShpTsDocument right)
        {
            return left != null && right != null &&
                left.InputLength == right.InputLength &&
                left.AbsoluteStartOffset == right.AbsoluteStartOffset &&
                string.Equals(
                    left.CanonicalDirectoryModelSha256,
                    right.CanonicalDirectoryModelSha256,
                    StringComparison.Ordinal);
        }

        private static bool DecodeEquivalent(
            ShpTsDecodeResult left,
            ShpTsDecodeResult right)
        {
            if (left == null || right == null || left.IsSuccess != right.IsSuccess ||
                !DiagnosticsEquivalent(left.Diagnostics, right.Diagnostics))
            {
                return false;
            }

            if (!left.IsSuccess)
            {
                return true;
            }

            return left.Frame.FrameIndex == right.Frame.FrameIndex &&
                left.Frame.Width == right.Frame.Width &&
                left.Frame.Height == right.Frame.Height &&
                left.Frame.CompressionKind == right.Frame.CompressionKind &&
                left.Frame.BytesConsumed == right.Frame.BytesConsumed &&
                left.Frame.PaddingBytes == right.Frame.PaddingBytes &&
                left.Frame.GetIndicesCopy().SequenceEqual(right.Frame.GetIndicesCopy());
        }

        private static bool DiagnosticsEquivalent(
            IReadOnlyList<ShpTsDiagnostic> left,
            IReadOnlyList<ShpTsDiagnostic> right)
        {
            if (left.Count != right.Count)
            {
                return false;
            }

            for (int index = 0; index < left.Count; index++)
            {
                if (left[index].Severity != right[index].Severity ||
                    left[index].Code != right[index].Code ||
                    left[index].AbsoluteOffset != right[index].AbsoluteOffset ||
                    left[index].FrameIndex != right[index].FrameIndex ||
                    left[index].RowIndex != right[index].RowIndex)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsControlledUnresolved(ShpTsDecodeResult result)
        {
            return result != null && !result.IsSuccess && result.Diagnostics.Count != 0 &&
                result.Diagnostics.All(value =>
                    value.Code == ShpTsDiagnosticCode.SourceConflictingFlags2 ||
                    value.Code == ShpTsDiagnosticCode.UnknownFlags ||
                    value.Code == ShpTsDiagnosticCode.ZeroOutputCommandSemanticsUnresolved);
        }

        private static int DisposeAll(IEnumerable<IDisposable> values)
        {
            int failures = 0;
            foreach (IDisposable value in values.Reverse())
            {
                try
                {
                    value.Dispose();
                }
                catch (Exception)
                {
                    failures = checked(failures + 1);
                }
            }

            return failures;
        }

        private static void ThrowAfterCleanup(Exception failure, int cleanupFailures)
        {
            if (failure != null)
            {
                var structured = failure as ShpTsProjectBaselineAuditException;
                if (structured != null && cleanupFailures != 0)
                {
                    structured.RecordCleanupFailures(cleanupFailures);
                }

                ExceptionDispatchInfo.Capture(failure).Throw();
            }

            if (cleanupFailures != 0)
            {
                throw new ShpTsProjectBaselineAuditException(
                    ShpTsProjectBaselineAuditFailureCode.MountCleanupFailed,
                    "One or more controlled SHP MIX mounts failed cleanup.",
                    cleanupFailures);
            }
        }

        private static bool IsExpectedReadFailure(Exception exception)
        {
            return exception is IOException ||
                exception is UnauthorizedAccessException ||
                exception is NotSupportedException ||
                exception is OverflowException ||
                exception is System.Security.SecurityException ||
                exception is BinaryReadException;
        }

        private static ShpTsProjectBaselineAuditException Failure(
            ShpTsProjectBaselineAuditFailureCode code,
            string message)
        {
            return new ShpTsProjectBaselineAuditException(code, message);
        }
    }
}
