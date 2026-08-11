using System;
using System.Collections.Generic;
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
    /// Read-only, aggregate-only Preview/PreviewPack audit for the configured
    /// patched ProjectBaseline source. No file names, paths, payloads, pixels,
    /// or per-entry values are emitted.
    /// </summary>
    public static class PreviewPackProjectBaselineAuditService
    {
        public const string BaselineLogicalName = "YR1001_ProjectBaseline";
        private const string PreviewSectionName = "Preview";
        private const string PreviewPackSectionName = "PreviewPack";

        public static PreviewPackProjectBaselineAuditDelivery Run(
            ExternalContentConfiguration configuration,
            PreviewPackProjectBaselineAuditProfile profile = null)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            profile = profile ?? PreviewPackProjectBaselineAuditProfile.ProjectBaseline;
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
                        aggregate.MountFailures = checked(aggregate.MountFailures + 1);
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
                if (!string.Equals(sourceIndex.Fingerprint, sourceFingerprintAfter, StringComparison.Ordinal))
                    throw new InvalidOperationException("The ProjectBaseline source changed during the PreviewPack audit.");
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
            PreviewPackProjectBaselineAuditStatus status = aggregate.CandidateEntries == 0
                ? PreviewPackProjectBaselineAuditStatus.CompleteWithNoCandidates
                : aggregate.FailedCount == 0 && aggregate.MountFailures == 0
                    ? PreviewPackProjectBaselineAuditStatus.Complete
                    : PreviewPackProjectBaselineAuditStatus.CompleteWithFailures;
            string summary = SerializeSummary(status, source, sourceIndex.Fingerprint, sourceFingerprintAfter, aggregate, aggregateSha256);
            return new PreviewPackProjectBaselineAuditDelivery(
                status,
                sourceIndex.Fingerprint,
                sourceFingerprintAfter,
                aggregate.RootArchiveCount,
                aggregate.MountedEntryCount,
                aggregate.CandidateEntries,
                aggregate.PreviewPresent,
                aggregate.PreviewPackPresent,
                aggregate.BothPresent,
                aggregate.MissingPreview,
                aggregate.MissingPreviewPack,
                aggregate.ValidMetadata,
                aggregate.InvalidMetadata,
                aggregate.Field0Zero,
                aggregate.Field0NonZero,
                aggregate.Field1Zero,
                aggregate.Field1NonZero,
                aggregate.PositiveDimensions,
                aggregate.InvalidDimensions,
                aggregate.MinFragments,
                aggregate.MaxFragments,
                aggregate.MinChunks,
                aggregate.MaxChunks,
                aggregate.ExactDecoded,
                aggregate.Underflow,
                aggregate.Overflow,
                aggregate.FailedCount,
                aggregate.DiagnosticCount,
                aggregateSha256,
                summary);
        }

        private static void InspectEntry(
            MixVirtualEntry entry,
            ExternalContentSourceDescriptor source,
            PreviewPackProjectBaselineAuditProfile profile,
            Aggregate aggregate)
        {
            if (entry.Length <= 0 || entry.Length > profile.MaxIniBytes)
                return;

            IniSourceProvenance provenance = BuildProvenance(entry, source);
            var binarySource = new BinarySourceContext(
                "m3c5-preview-pack-baseline-audit",
                source.Id,
                LogicalContentPath.Parse("preview-pack-audit-entry"));
            IniParseResult parse;
            try
            {
                parse = WestwoodIniReader.Read(entry.PayloadWindow, binarySource, provenance, IniReadLimits.Default);
            }
            catch (Exception)
            {
                return;
            }
            if (!parse.IsSuccess || parse.Document == null)
                return;

            List<SectionGroup> previewSections = ReadSections(parse.Document, PreviewSectionName, profile.MaxPreviewSectionsPerEntry);
            List<SectionGroup> packSections = ReadSections(parse.Document, PreviewPackSectionName, profile.MaxPreviewPackSectionsPerEntry);
            bool hasPreview = previewSections.Count != 0;
            bool hasPack = packSections.Count != 0;
            if (!hasPreview && !hasPack)
                return;

            aggregate.CandidateEntries = checked(aggregate.CandidateEntries + 1);
            if (aggregate.CandidateEntries > profile.MaxCandidateEntries)
                throw new InvalidOperationException("The PreviewPack candidate entry budget was exceeded.");
            if (hasPreview) aggregate.PreviewPresent = checked(aggregate.PreviewPresent + 1); else aggregate.MissingPreview = checked(aggregate.MissingPreview + 1);
            if (hasPack) aggregate.PreviewPackPresent = checked(aggregate.PreviewPackPresent + 1); else aggregate.MissingPreviewPack = checked(aggregate.MissingPreviewPack + 1);
            if (hasPreview && hasPack) aggregate.BothPresent = checked(aggregate.BothPresent + 1);

            PreviewMetadataReadResult metadata = ReadMetadata(parse.Document, previewSections, profile);
            ObserveMetadata(metadata, aggregate);
            if (!hasPreview || !hasPack)
                return;

            SectionGroup selectedPack = packSections.Count == 1 ? packSections[0] : null;
            PreviewSectionSelectionStatus selection = packSections.Count == 0
                ? PreviewSectionSelectionStatus.Missing
                : packSections.Count == 1
                    ? selectedPack.Keys.Count == 0 ? PreviewSectionSelectionStatus.PresentEmpty : PreviewSectionSelectionStatus.SelectedOccurrence
                    : PreviewSectionSelectionStatus.AmbiguousMultipleOccurrences;
            IEnumerable<PackedIniFragmentOccurrence> occurrences = selectedPack == null
                ? Array.Empty<PackedIniFragmentOccurrence>()
                : ToFragmentOccurrences(selectedPack, parse.Document);
            var input = new PreviewPackSectionInput(
                PreviewPackSectionName,
                selection,
                selectedPack == null ? -1 : selectedPack.Ordinal,
                packSections.Count,
                occurrences,
                binarySource,
                new[] { provenance });
            PackedSectionDecodePolicy packedPolicy = new PackedSectionDecodePolicy(
                PackedIniFragmentOrderingPolicy.NumericAscendingUnique,
                StrictBase64Policy.StandardAlphabetNoWhitespace,
                ChunkSentinelPolicy.RejectAllZero,
                PackedCodecKind.RawLzo1X,
                fragmentLimits: new PackedIniFragmentCollectorLimits(profile.MaxFragmentsPerSection, profile.MaxIniBytes),
                chunkLimits: new WestwoodChunkReadLimits(maxCompressedBytes: profile.MaxIniBytes, maxOutputBytes: profile.MaxDecodedBytesPerSection, maxInputBytes: profile.MaxIniBytes));
            var readPolicy = new PreviewPackReadPolicy(
                packedPolicy,
                PreviewMetadataInterpretationProfile.Fields23Dimensions,
                PreviewChannelProfile.RawUnknown,
                PreviewRowOrderProfile.Unknown,
                PreviewLengthPolicy.ExactThreeComponents,
                new PreviewReadLimits(
                    maxMetadataSections: profile.MaxPreviewSectionsPerEntry,
                    maxPreviewPackSections: profile.MaxPreviewPackSectionsPerEntry,
                    maxFragments: profile.MaxFragmentsPerSection,
                    maxFragmentCharacters: profile.MaxIniBytes,
                    maxCompressedBytes: profile.MaxIniBytes,
                    maxDecodedBytes: profile.MaxDecodedBytesPerSection,
                    maxDiagnostics: profile.MaxDiagnostics));
            PreviewPackReadResult result = new PreviewPackSectionReader().Read(
                metadata,
                input,
                readPolicy,
                new ManagedRawLzo1XDecodeBackend());
            ObservePreview(result, aggregate);
            if (result.IsSuccess && result.Decoded != null)
                aggregate.AppendHash(result.Decoded.GetBytesCopy());
        }

        private static PreviewMetadataReadResult ReadMetadata(
            IniRawDocument document,
            List<SectionGroup> sections,
            PreviewPackProjectBaselineAuditProfile profile)
        {
            PreviewSectionSelectionStatus selection = sections.Count == 0
                ? PreviewSectionSelectionStatus.Missing
                : sections.Count == 1
                    ? sections[0].Keys.Count == 0 ? PreviewSectionSelectionStatus.PresentEmpty : PreviewSectionSelectionStatus.SelectedOccurrence
                    : PreviewSectionSelectionStatus.AmbiguousMultipleOccurrences;
            PreviewMetadataSectionOccurrence[] values = sections
                .Select(group => new PreviewMetadataSectionOccurrence(
                    group.Ordinal,
                    group.Keys.Where(key => IsKey(key, "Size"))
                        .Select(key => new PreviewSizeOccurrence(
                            IniTextEncodingPolicy.StrictAscii.Decode(key.Value),
                            key.PhysicalLineId,
                            document.Provenance)),
                    document.Source,
                    document.Provenance))
                .ToArray();
            return new PreviewMetadataReader().Read(
                values,
                new PreviewMetadataReadPolicy(
                    selection,
                    sections.Count == 1 ? sections[0].Ordinal : -1,
                    PreviewMetadataInterpretationProfile.Fields23Dimensions,
                    new PreviewReadLimits(
                        maxMetadataSections: profile.MaxPreviewSectionsPerEntry,
                        maxSizeOccurrences: 64,
                        maxDiagnostics: profile.MaxDiagnostics)));
        }

        private static void ObserveMetadata(PreviewMetadataReadResult result, Aggregate aggregate)
        {
            if (result == null) return;
            ObservePreviewDiagnostics(result.Diagnostics, aggregate);
            if (result.Size != null)
            {
                if (result.Size.Field0Raw == 0) aggregate.Field0Zero = checked(aggregate.Field0Zero + 1);
                else if (result.Size.Field0Raw.HasValue) aggregate.Field0NonZero = checked(aggregate.Field0NonZero + 1);
                if (result.Size.Field1Raw == 0) aggregate.Field1Zero = checked(aggregate.Field1Zero + 1);
                else if (result.Size.Field1Raw.HasValue) aggregate.Field1NonZero = checked(aggregate.Field1NonZero + 1);
                bool dimensionsPositive = result.Size.Field2Raw.HasValue && result.Size.Field3Raw.HasValue &&
                                           result.Size.Field2Raw.Value > 0 && result.Size.Field3Raw.Value > 0;
                if (dimensionsPositive) aggregate.PositiveDimensions = checked(aggregate.PositiveDimensions + 1);
                else aggregate.InvalidDimensions = checked(aggregate.InvalidDimensions + 1);
            }
            if (result.IsSuccess) aggregate.ValidMetadata = checked(aggregate.ValidMetadata + 1);
            else if (result.HasFatalError) aggregate.InvalidMetadata = checked(aggregate.InvalidMetadata + 1);
        }

        private static void ObservePreview(PreviewPackReadResult result, Aggregate aggregate)
        {
            if (result == null)
            {
                aggregate.FailedCount = checked(aggregate.FailedCount + 1);
                return;
            }
            ObservePreviewDiagnostics(result.Diagnostics, aggregate);
            if (result.Packed != null)
            {
                int fragments = result.Packed.Fragments == null ? 0 : result.Packed.Fragments.Occurrences.Count;
                int chunks = result.Packed.Envelope == null ? 0 : result.Packed.Envelope.Blocks.Count;
                aggregate.MinFragments = Math.Min(aggregate.MinFragments, fragments);
                aggregate.MaxFragments = Math.Max(aggregate.MaxFragments, fragments);
                aggregate.MinChunks = Math.Min(aggregate.MinChunks, chunks);
                aggregate.MaxChunks = Math.Max(aggregate.MaxChunks, chunks);
            }
            if (result.Decoded != null)
            {
                switch (result.Decoded.LengthStatus)
                {
                    case PreviewLengthStatus.Exact: aggregate.ExactDecoded = checked(aggregate.ExactDecoded + 1); break;
                    case PreviewLengthStatus.Underflow: aggregate.Underflow = checked(aggregate.Underflow + 1); break;
                    case PreviewLengthStatus.Overflow: aggregate.Overflow = checked(aggregate.Overflow + 1); break;
                }
            }
            if (!result.IsSuccess)
                aggregate.FailedCount = checked(aggregate.FailedCount + 1);
        }

        private static void ObservePreviewDiagnostics(IEnumerable<PreviewDiagnostic> diagnostics, Aggregate aggregate)
        {
            if (diagnostics == null) return;
            foreach (PreviewDiagnostic diagnostic in diagnostics)
            {
                aggregate.DiagnosticCount = checked(aggregate.DiagnosticCount + 1);
                string key = diagnostic.Code.ToString();
                aggregate.DiagnosticCategories[key] = aggregate.DiagnosticCategories.TryGetValue(key, out int count) ? checked(count + 1) : 1;
            }
        }

        private static List<SectionGroup> ReadSections(IniRawDocument document, string name, int maxSections)
        {
            var result = new List<SectionGroup>();
            SectionGroup current = null;
            foreach (IniNode node in document.Nodes)
            {
                IniSectionNode section = node as IniSectionNode;
                if (section != null)
                {
                    string sectionName = IniTextEncodingPolicy.StrictAscii.Decode(section.Name);
                    current = null;
                    if (!string.Equals(sectionName, name, StringComparison.Ordinal))
                        continue;
                    if (result.Count >= maxSections)
                        throw new InvalidOperationException("The Preview section occurrence budget was exceeded.");
                    current = new SectionGroup(result.Count, section);
                    result.Add(current);
                    continue;
                }
                IniKeyValueNode key = node as IniKeyValueNode;
                if (current != null && key != null)
                    current.Keys.Add(key);
            }
            return result;
        }

        private static IEnumerable<PackedIniFragmentOccurrence> ToFragmentOccurrences(SectionGroup group, IniRawDocument document)
        {
            foreach (IniKeyValueNode key in group.Keys)
            {
                yield return new PackedIniFragmentOccurrence(
                    PreviewPackSectionName,
                    IniTextEncodingPolicy.StrictAscii.Decode(key.Key),
                    IniTextEncodingPolicy.StrictAscii.Decode(key.Value),
                    key.PhysicalLineId,
                    document.Provenance.SourceId,
                    key.PhysicalLineId,
                    document.Provenance);
            }
        }

        private static bool IsKey(IniKeyValueNode key, string expected)
        {
            return string.Equals(IniTextEncodingPolicy.StrictAscii.Decode(key.Key), expected, StringComparison.Ordinal);
        }

        private static ExternalContentSourceDescriptor ValidateConfiguration(ExternalContentConfiguration configuration)
        {
            ExternalContentSourceDescriptor[] enabled = configuration.Sources.Where(s => s.Enabled).ToArray();
            if (enabled.Length != 1 || !string.Equals(enabled[0].Id, BaselineLogicalName, StringComparison.Ordinal) || enabled[0].Kind != ContentSourceKind.Patched)
                throw new InvalidOperationException("The PreviewPack audit requires exactly one enabled patched YR1001_ProjectBaseline source.");
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
                if (step.ResolvedName != null) chain.Add(step.ResolvedName);
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
            PreviewPackProjectBaselineAuditStatus status,
            ExternalContentSourceDescriptor source,
            string sourceFingerprint,
            string sourceFingerprintAfter,
            Aggregate aggregate,
            string aggregateSha256)
        {
            var builder = new StringBuilder();
            builder.Append('{');
            builder.Append("\"manifestType\":\"RA2YR.PreviewPackProjectBaselineAuditSanitized\"");
            builder.Append(",\"auditVersion\":\"m3c5-v1\"");
            builder.Append(",\"baselineLogicalName\":\"YR1001_ProjectBaseline\"");
            builder.Append(",\"contentRole\":\"patched-development-content-source\"");
            builder.Append(",\"sourceId\":\"").Append(source.Id).Append('"');
            builder.Append(",\"status\":\"").Append(status).Append('"');
            builder.Append(",\"backendIdentity\":\"").Append(ManagedRawLzo1XDecodeBackend.Identity).Append('"');
            builder.Append(",\"previewPresentCount\":").Append(aggregate.PreviewPresent);
            builder.Append(",\"previewPackPresentCount\":").Append(aggregate.PreviewPackPresent);
            builder.Append(",\"bothPresentCount\":").Append(aggregate.BothPresent);
            builder.Append(",\"missingPreviewCount\":").Append(aggregate.MissingPreview);
            builder.Append(",\"missingPreviewPackCount\":").Append(aggregate.MissingPreviewPack);
            builder.Append(",\"candidateEntryCount\":").Append(aggregate.CandidateEntries);
            builder.Append(",\"validMetadataCount\":").Append(aggregate.ValidMetadata);
            builder.Append(",\"invalidMetadataCount\":").Append(aggregate.InvalidMetadata);
            builder.Append(",\"field0ZeroCount\":").Append(aggregate.Field0Zero);
            builder.Append(",\"field0NonZeroCount\":").Append(aggregate.Field0NonZero);
            builder.Append(",\"field1ZeroCount\":").Append(aggregate.Field1Zero);
            builder.Append(",\"field1NonZeroCount\":").Append(aggregate.Field1NonZero);
            builder.Append(",\"positiveDimensionCount\":").Append(aggregate.PositiveDimensions);
            builder.Append(",\"invalidDimensionCount\":").Append(aggregate.InvalidDimensions);
            builder.Append(",\"fragmentCountRange\":{\"min\":").Append(aggregate.MinFragments == int.MaxValue ? 0 : aggregate.MinFragments).Append(",\"max\":").Append(aggregate.MaxFragments).Append('}');
            builder.Append(",\"chunkCountRange\":{\"min\":").Append(aggregate.MinChunks == int.MaxValue ? 0 : aggregate.MinChunks).Append(",\"max\":").Append(aggregate.MaxChunks).Append('}');
            builder.Append(",\"exactDecodedCount\":").Append(aggregate.ExactDecoded);
            builder.Append(",\"underflowCount\":").Append(aggregate.Underflow);
            builder.Append(",\"overflowCount\":").Append(aggregate.Overflow);
            builder.Append(",\"failedCount\":").Append(aggregate.FailedCount);
            builder.Append(",\"mountFailureCount\":").Append(aggregate.MountFailures);
            builder.Append(",\"diagnosticCount\":").Append(aggregate.DiagnosticCount);
            builder.Append(",\"diagnosticCategories\":{");
            bool first = true;
            foreach (KeyValuePair<string, int> item in aggregate.DiagnosticCategories)
            {
                if (!first) builder.Append(',');
                first = false;
                builder.Append('"').Append(item.Key).Append("\":").Append(item.Value);
            }
            builder.Append('}');
            builder.Append(",\"sourceFingerprintBefore\":\"").Append(sourceFingerprint).Append('"');
            builder.Append(",\"sourceFingerprintAfter\":\"").Append(sourceFingerprintAfter).Append('"');
            builder.Append(",\"aggregateSha256\":\"").Append(aggregateSha256).Append('"');
            builder.Append(",\"originalRuntimeCompatibility\":\"NotConfirmed\"}");
            return builder.ToString();
        }

        private sealed class SectionGroup
        {
            internal SectionGroup(int ordinal, IniSectionNode section)
            {
                Ordinal = ordinal;
                Section = section;
            }
            internal int Ordinal { get; }
            internal IniSectionNode Section { get; }
            internal List<IniKeyValueNode> Keys { get; } = new List<IniKeyValueNode>();
        }

        private sealed class Aggregate
        {
            private readonly SHA256 hash = SHA256.Create();
            internal int RootArchiveCount;
            internal int MountedEntryCount;
            internal int CandidateEntries;
            internal int PreviewPresent;
            internal int PreviewPackPresent;
            internal int BothPresent;
            internal int MissingPreview;
            internal int MissingPreviewPack;
            internal int ValidMetadata;
            internal int InvalidMetadata;
            internal int Field0Zero;
            internal int Field0NonZero;
            internal int Field1Zero;
            internal int Field1NonZero;
            internal int PositiveDimensions;
            internal int InvalidDimensions;
            internal int MinFragments = int.MaxValue;
            internal int MaxFragments;
            internal int MinChunks = int.MaxValue;
            internal int MaxChunks;
            internal int ExactDecoded;
            internal int Underflow;
            internal int Overflow;
            internal int FailedCount;
            internal int MountFailures;
            internal int DiagnosticCount;
            internal SortedDictionary<string, int> DiagnosticCategories { get; } = new SortedDictionary<string, int>(StringComparer.Ordinal);

            internal void AppendHash(byte[] bytes)
            {
                if (bytes == null) return;
                byte[] length = BitConverter.GetBytes(bytes.Length);
                hash.TransformBlock(length, 0, length.Length, length, 0);
                byte[] digest;
                using (SHA256 contentHash = SHA256.Create()) digest = contentHash.ComputeHash(bytes);
                hash.TransformBlock(digest, 0, digest.Length, digest, 0);
            }

            internal string FinalizeHash()
            {
                hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                return BitConverter.ToString(hash.Hash).Replace("-", string.Empty).ToLowerInvariant();
            }
        }
    }
}
