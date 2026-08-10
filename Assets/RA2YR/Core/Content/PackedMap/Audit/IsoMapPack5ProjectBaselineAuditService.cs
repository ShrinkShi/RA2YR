using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using RA2YR.Core.Binary;
using RA2YR.Core.Content.Mix;
using RA2YR.Core.Formats.Ini;
using RA2YR.Core.Formats.PackedMap;

namespace RA2YR.Core.Content.PackedMap.Audit
{
    /// <summary>
    /// Read-only aggregate audit for IsoMapPack5 sections in the configured patched source.
    /// No map name, payload, record, coordinate, or fragment value is emitted.
    /// </summary>
    public static class IsoMapPack5ProjectBaselineAuditService
    {
        public const string BaselineLogicalName = "YR1001_ProjectBaseline";
        private const string SectionName = "IsoMapPack5";

        public static IsoMapPack5ProjectBaselineAuditDelivery Run(
            ExternalContentConfiguration configuration,
            IsoMapPack5ProjectBaselineAuditProfile profile = null)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            profile = profile ?? IsoMapPack5ProjectBaselineAuditProfile.ProjectBaseline;
            ExternalContentSourceDescriptor source = ValidateConfiguration(configuration);
            ContentIndexResult index = BuildCompleteIndex(configuration);
            ContentSourceIndex sourceIndex = GetBaselineSource(index);
            LogicalContentPath[] roots = sourceIndex.Files
                .Where(file => file.LogicalPath.Value.EndsWith(".mix", StringComparison.OrdinalIgnoreCase))
                .Select(file => file.LogicalPath)
                .OrderBy(path => path, LogicalContentPathReportComparer.Instance)
                .ToArray();
            if (roots.Length > profile.MaxRootArchives)
                throw new InvalidOperationException("The ProjectBaseline MIX root budget was exceeded.");

            var allCandidates = sourceIndex.Files.Select(file => file.LogicalPath).ToList();
            allCandidates.AddRange(roots);
            var nameCatalog = new MixNameCatalog(allCandidates.Distinct());
            var mounts = new List<MixVirtualContentMountResult>();
            var aggregate = new Aggregate();
            string sourceFingerprintAfter = sourceIndex.Fingerprint;
            Exception operationFailure = null;
            try
            {
                foreach (LogicalContentPath root in roots)
                {
                    MixVirtualContentMountResult mount = MixVirtualContentSource.MountDirectorySource(
                        sourceIndex,
                        new[] { root },
                        nameCatalog,
                        MixArchiveCatalogAdapters.ReadWithCoreReader,
                        MixMountLimits.Default,
                        MixMountIndexMode.ManifestAudit);
                    mounts.Add(mount);
                    if (!mount.IsComplete || mount.Diagnostics.Any(d => d.Severity == MixMountDiagnosticSeverity.Error))
                    {
                        aggregate.Failures++;
                        continue;
                    }
                    aggregate.MountedEntryCount = checked(aggregate.MountedEntryCount + mount.Entries.Count);
                    if (aggregate.MountedEntryCount > profile.MaxMountedEntries)
                        throw new InvalidOperationException("The mounted MIX entry budget was exceeded.");
                    foreach (MixVirtualEntry entry in mount.Entries
                                 .Where(value => !value.IsMountedArchive)
                                 .OrderBy(value => value.Provenance.RootArchivePath, LogicalContentPathReportComparer.Instance)
                                 .ThenBy(value => value.Id))
                    {
                        InspectEntry(entry, source, profile, aggregate);
                    }
                }
                ContentSourceIndex afterSource = GetBaselineSource(BuildCompleteIndex(configuration));
                sourceFingerprintAfter = afterSource.Fingerprint;
                if (!string.Equals(sourceIndex.Fingerprint, afterSource.Fingerprint, StringComparison.Ordinal))
                    throw new InvalidOperationException("The ProjectBaseline source changed during the IsoMapPack5 audit.");
            }
            catch (Exception exception)
            {
                operationFailure = exception;
            }
            finally
            {
                int cleanupFailures = DisposeMounts(mounts);
                if (operationFailure == null && cleanupFailures != 0)
                    operationFailure = new InvalidOperationException("One or more MIX mounts failed to dispose cleanly.");
            }

            if (operationFailure != null)
                throw operationFailure;

            aggregate.RootArchiveCount = roots.Length;
            string aggregateSha256 = aggregate.FinalizeHash();
            IsoMapPack5ProjectBaselineAuditStatus status = aggregate.Candidates == 0
                ? IsoMapPack5ProjectBaselineAuditStatus.NoCandidates
                : aggregate.Failures == 0
                    ? IsoMapPack5ProjectBaselineAuditStatus.Complete
                    : IsoMapPack5ProjectBaselineAuditStatus.CompleteWithFailures;
            string summary = SerializeSummary(status, source, sourceIndex.Fingerprint, sourceFingerprintAfter, aggregate, aggregateSha256);
            return new IsoMapPack5ProjectBaselineAuditDelivery(
                status,
                sourceIndex.Fingerprint,
                sourceFingerprintAfter,
                aggregate.RootArchiveCount,
                aggregate.MountedEntryCount,
                aggregate.Candidates,
                aggregate.Successes,
                aggregate.Failures,
                aggregate.DecodedBytes,
                aggregate.DecodedRecords,
                aggregate.RejectedRemainders,
                aggregate.PreservedRemainders,
                aggregate.ExactFourZeroTrailers,
                aggregate.DuplicateGroups,
                aggregate.Diagnostics,
                aggregateSha256,
                summary);
        }

        private static void InspectEntry(
            MixVirtualEntry entry,
            ExternalContentSourceDescriptor source,
            IsoMapPack5ProjectBaselineAuditProfile profile,
            Aggregate aggregate)
        {
            if (entry.Length <= 0 || entry.Length > profile.MaxIniBytes)
                return;
            IniSourceProvenance provenance = BuildProvenance(entry, source);
            var binarySource = new BinarySourceContext(
                "m3c4-isomap-pack5-baseline-audit",
                source.Id,
                LogicalContentPath.Parse("isomap-pack5-audit-entry"));
            IniParseResult parse;
            try
            {
                parse = WestwoodIniReader.Read(
                    entry.PayloadWindow,
                    binarySource,
                    provenance,
                    IniReadLimits.Default);
            }
            catch (Exception)
            {
                return;
            }
            if (!parse.IsSuccess || parse.Document == null)
                return;

            PackedIniFragmentOccurrence[] occurrences = PackedIniFragmentOccurrence
                .FromDocument(parse.Document, SectionName)
                .Take(profile.MaxFragmentsPerSection + 1)
                .ToArray();
            if (occurrences.Length == 0)
                return;
            if (occurrences.Length > profile.MaxFragmentsPerSection)
            {
                aggregate.Failures++;
                aggregate.Diagnostics++;
                return;
            }

            if (aggregate.Candidates >= profile.MaxCandidateSections)
                throw new InvalidOperationException("The IsoMapPack5 candidate section budget was exceeded.");

            aggregate.Candidates++;
            PackedSectionDecodePolicy packedPolicy = new PackedSectionDecodePolicy(
                PackedIniFragmentOrderingPolicy.NumericAscendingUnique,
                StrictBase64Policy.StandardAlphabetNoWhitespace,
                ChunkSentinelPolicy.RejectAllZero,
                PackedCodecKind.RawLzo1X,
                chunkLimits: new WestwoodChunkReadLimits(
                    maxOutputBytes: profile.MaxDecodedBytesPerSection,
                    maxInputBytes: profile.MaxDecodedBytesPerSection),
                fragmentLimits: new PackedIniFragmentCollectorLimits(
                    profile.MaxFragmentsPerSection,
                    profile.MaxIniBytes),
                cancellationToken: default(System.Threading.CancellationToken));
            var readPolicy = new IsoMapPack5PackedReadPolicy(
                packedPolicy,
                IsoMapPack5TrailingPolicy.AllowExactFourZeroTrailer,
                IsoMapCoordinateDuplicatePolicy.PreserveAllAndDiagnose,
                null,
                new IsoMapPack5ReadLimits(
                    maxInputBytes: profile.MaxDecodedBytesPerSection,
                    maxRecords: profile.MaxRecordsPerSection,
                    maxDiagnostics: profile.MaxDiagnostics));
            IsoMapPack5PackedReadResult result = new IsoMapPack5PackedSectionReader()
                .Read(occurrences, readPolicy, new ManagedRawLzo1XDecodeBackend());
            aggregate.Diagnostics = checked(aggregate.Diagnostics + result.Diagnostics.Count);
            aggregate.ObserveIsoDiagnostics(result.Diagnostics);
            if (result.Packed != null)
                aggregate.ObserveDiagnostics(result.Packed.Diagnostics);
            if (result.Records != null)
                aggregate.ObserveIsoDiagnostics(result.Records.Diagnostics);
            if (result.Coordinates != null)
                aggregate.ObserveIsoDiagnostics(result.Coordinates.Diagnostics);
            ClassifyTrailing(aggregate, result.Records == null ? null : result.Records.Trailing);
            if (!result.IsSuccess)
            {
                if (result.Packed == null)
                    aggregate.PackedNullFailures++;
                else if (result.Packed.Diagnostics.Any(diagnostic => diagnostic.Severity == BinaryDiagnosticSeverity.Error))
                    aggregate.PackedChildFailures++;
                aggregate.Failures++;
                return;
            }

            aggregate.Successes++;
            aggregate.DecodedRecords = checked(aggregate.DecodedRecords + result.Records.Records.Count);
            aggregate.DecodedBytes = checked(aggregate.DecodedBytes + result.Packed.DecodedBytes.LongLength);
            if (result.Coordinates != null)
                aggregate.DuplicateGroups = checked(aggregate.DuplicateGroups + result.Coordinates.Index.DuplicateGroups.Count);
            aggregate.AppendHash(result.Packed.DecodedBytes);
        }

        private static void ClassifyTrailing(Aggregate aggregate, IsoMapPack5TrailingData trailing)
        {
            if (trailing == null)
                return;
            switch (trailing.Classification)
            {
                case IsoMapTrailingClassification.ExactFourZeroTrailer:
                    aggregate.ExactFourZeroTrailers++;
                    break;
                case IsoMapTrailingClassification.PreservedRemainder:
                    aggregate.PreservedRemainders++;
                    break;
                case IsoMapTrailingClassification.RejectedRemainder:
                    aggregate.RejectedRemainders++;
                    break;
            }
        }

        private static ExternalContentSourceDescriptor ValidateConfiguration(ExternalContentConfiguration configuration)
        {
            ExternalContentSourceDescriptor[] enabled = configuration.Sources.Where(s => s.Enabled).ToArray();
            if (enabled.Length != 1 || !string.Equals(enabled[0].Id, BaselineLogicalName, StringComparison.Ordinal) || enabled[0].Kind != ContentSourceKind.Patched)
                throw new InvalidOperationException("The IsoMapPack5 audit requires exactly one enabled patched YR1001_ProjectBaseline source.");
            return enabled[0];
        }

        private static ContentIndexResult BuildCompleteIndex(ExternalContentConfiguration configuration)
        {
            ContentIndexResult result = new ContentIndexer().Build(configuration);
            if (result == null || !result.IsComplete || result.HasErrors)
                throw new InvalidOperationException("The ProjectBaseline directory index is incomplete.");
            return result;
        }

        private static ContentSourceIndex GetBaselineSource(ContentIndexResult index)
        {
            ContentSourceIndex[] matches = index.Sources.Where(s => string.Equals(s.Source.Id, BaselineLogicalName, StringComparison.Ordinal)).ToArray();
            if (matches.Length != 1 || !matches[0].IsComplete)
                throw new InvalidOperationException("The ProjectBaseline source index is incomplete.");
            return matches[0];
        }

        private static IniSourceProvenance BuildProvenance(MixVirtualEntry entry, ExternalContentSourceDescriptor source)
        {
            var chain = new List<LogicalContentPath> { entry.Provenance.RootArchivePath };
            foreach (MixArchiveProvenanceStep step in entry.Provenance.Steps)
            {
                if (step.ResolvedName != null)
                    chain.Add(step.ResolvedName);
            }
            return new IniSourceProvenance(source.Id, chain);
        }

        private static int DisposeMounts(IEnumerable<MixVirtualContentMountResult> mounts)
        {
            int failures = 0;
            foreach (MixVirtualContentMountResult mount in mounts.Reverse())
            {
                try { mount.Dispose(); } catch (Exception) { failures++; }
            }
            return failures;
        }

        private static string SerializeSummary(
            IsoMapPack5ProjectBaselineAuditStatus status,
            ExternalContentSourceDescriptor source,
            string sourceFingerprint,
            string sourceFingerprintAfter,
            Aggregate aggregate,
            string aggregateSha256)
        {
            var builder = new StringBuilder();
            builder.Append('{');
            builder.Append("\"manifestType\":\"RA2YR.IsoMapPack5ProjectBaselineAuditSanitized\"");
            builder.Append(",\"auditVersion\":\"m3c4-v1\"");
            builder.Append(",\"baselineLogicalName\":\"YR1001_ProjectBaseline\"");
            builder.Append(",\"contentRole\":\"patched-development-content-source\"");
            builder.Append(",\"sourceId\":\"").Append(source.Id).Append('"');
            builder.Append(",\"status\":\"").Append(status).Append('\"');
            builder.Append(",\"backendIdentity\":\"").Append(ManagedRawLzo1XDecodeBackend.Identity).Append('\"');
            builder.Append(",\"section\":\"IsoMapPack5\"");
            builder.Append(",\"rootArchiveCount\":").Append(aggregate.RootArchiveCount);
            builder.Append(",\"mountedEntryCount\":").Append(aggregate.MountedEntryCount);
            builder.Append(",\"candidateSectionCount\":").Append(aggregate.Candidates);
            builder.Append(",\"successfulSectionCount\":").Append(aggregate.Successes);
            builder.Append(",\"failedSectionCount\":").Append(aggregate.Failures);
            builder.Append(",\"packedNullFailureCount\":").Append(aggregate.PackedNullFailures);
            builder.Append(",\"packedChildFailureCount\":").Append(aggregate.PackedChildFailures);
            builder.Append(",\"decodedByteCount\":").Append(aggregate.DecodedBytes);
            builder.Append(",\"decodedRecordCount\":").Append(aggregate.DecodedRecords);
            builder.Append(",\"rejectedRemainderCount\":").Append(aggregate.RejectedRemainders);
            builder.Append(",\"preservedRemainderCount\":").Append(aggregate.PreservedRemainders);
            builder.Append(",\"exactFourZeroTrailerCount\":").Append(aggregate.ExactFourZeroTrailers);
            builder.Append(",\"duplicateGroupCount\":").Append(aggregate.DuplicateGroups);
            builder.Append(",\"diagnosticCount\":").Append(aggregate.Diagnostics);
            builder.Append(",\"diagnosticCategories\":{");
            bool firstDiagnostic = true;
            foreach (KeyValuePair<string, int> diagnostic in aggregate.DiagnosticCategories)
            {
                if (!firstDiagnostic) builder.Append(',');
                firstDiagnostic = false;
                builder.Append('\"').Append(diagnostic.Key).Append("\":").Append(diagnostic.Value);
            }
            builder.Append('}');
            builder.Append(",\"sourceFingerprintBefore\":\"").Append(sourceFingerprint).Append('\"');
            builder.Append(",\"sourceFingerprintAfter\":\"").Append(sourceFingerprintAfter).Append('\"');
            builder.Append(",\"aggregateSha256\":\"").Append(aggregateSha256).Append('\"');
            builder.Append(",\"originalRuntimeCompatibility\":\"NotConfirmed\"");
            builder.Append('}');
            return builder.ToString();
        }

        private sealed class Aggregate
        {
            private readonly SHA256 hash = SHA256.Create();
            public int RootArchiveCount;
            public int MountedEntryCount;
            public int Candidates;
            public int Successes;
            public int Failures;
            public int PackedNullFailures;
            public int PackedChildFailures;
            public long DecodedBytes;
            public long DecodedRecords;
            public int RejectedRemainders;
            public int PreservedRemainders;
            public int ExactFourZeroTrailers;
            public int DuplicateGroups;
            public int Diagnostics;
            public SortedDictionary<string, int> DiagnosticCategories { get; } = new SortedDictionary<string, int>(StringComparer.Ordinal);
            public void ObserveDiagnostics(IEnumerable<PackedMapDiagnostic> diagnostics)
            {
                if (diagnostics == null) return;
                foreach (PackedMapDiagnostic diagnostic in diagnostics)
                {
                    string key = diagnostic.Code.ToString();
                    DiagnosticCategories[key] = DiagnosticCategories.TryGetValue(key, out int count)
                        ? checked(count + 1)
                        : 1;
                }
            }
            public void ObserveIsoDiagnostics(IEnumerable<IsoMapDiagnostic> diagnostics)
            {
                if (diagnostics == null) return;
                foreach (IsoMapDiagnostic diagnostic in diagnostics)
                {
                    string key = diagnostic.Code.ToString();
                    DiagnosticCategories[key] = DiagnosticCategories.TryGetValue(key, out int count)
                        ? checked(count + 1)
                        : 1;
                    if (diagnostic.Code == IsoMapDiagnosticCode.PackedStageFailure &&
                        diagnostic.Message != null)
                    {
                        int separator = diagnostic.Message.LastIndexOf(':');
                        if (separator >= 0 && separator + 1 < diagnostic.Message.Length)
                        {
                            string detail = diagnostic.Message.Substring(separator + 1).Trim().TrimEnd('.');
                            string detailKey = "PackedStageFailure." + detail;
                            DiagnosticCategories[detailKey] = DiagnosticCategories.TryGetValue(detailKey, out int detailCount)
                                ? checked(detailCount + 1)
                                : 1;
                        }
                    }
                }
            }
            public void AppendHash(byte[] bytes)
            {
                if (bytes == null) return;
                byte[] length = BitConverter.GetBytes(bytes.Length);
                hash.TransformBlock(length, 0, length.Length, length, 0);
                byte[] digest;
                using (SHA256 contentHash = SHA256.Create())
                {
                    digest = contentHash.ComputeHash(bytes);
                }
                hash.TransformBlock(digest, 0, digest.Length, digest, 0);
            }
            public string FinalizeHash()
            {
                hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                byte[] bytes = hash.Hash;
                return BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();
            }
        }
    }
}
