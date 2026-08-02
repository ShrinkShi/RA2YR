using System;
using System.Collections.Generic;
using System.Linq;
using RA2YR.Core.Content.Mix;
using RA2YR.Core.Formats.Csf;
using RA2YR.Core.Formats.Mix;

namespace RA2YR.Core.Content.Csf.Audit
{
    public enum CsfProjectBaselineAuditStatus
    {
        Complete
    }

    public enum CsfProjectBaselineAuditFailureCode
    {
        InvalidBaselineConfiguration,
        DirectoryIndexIncomplete,
        RootArchiveMissing,
        LooseCsfCandidateFound,
        MixMountFailed,
        TargetMissing,
        TargetAmbiguous,
        TargetIdentityMismatch,
        TargetProvenanceMismatch,
        TargetLengthMismatch,
        TargetHashMismatch,
        CsfParseFailed,
        NormalizedModelHashMismatch,
        BaselineChangedDuringAudit,
        ManifestBudgetExceeded,
        ExternalManifestWriteFailed,
        MountCleanupFailed
    }

    public sealed class CsfProjectBaselineAuditException : InvalidOperationException
    {
        internal CsfProjectBaselineAuditException(
            CsfProjectBaselineAuditFailureCode code,
            string message,
            int cleanupFailureCount = 0)
            : base(message)
        {
            if (cleanupFailureCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cleanupFailureCount));
            }

            Code = code;
            CleanupFailureCount = cleanupFailureCount;
        }

        public CsfProjectBaselineAuditFailureCode Code { get; }

        public int CleanupFailureCount { get; private set; }

        internal void RecordCleanupFailures(int count)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            CleanupFailureCount = checked(CleanupFailureCount + count);
        }
    }

    public sealed class CsfProjectBaselineAuditDelivery
    {
        internal CsfProjectBaselineAuditDelivery(
            string sanitizedSummaryJson,
            string externalManifestCacheRelativePath,
            long externalManifestLength,
            string externalManifestSha256)
        {
            if (string.IsNullOrEmpty(sanitizedSummaryJson))
            {
                throw new ArgumentException(
                    "A sanitized summary is required.",
                    nameof(sanitizedSummaryJson));
            }

            Status = CsfProjectBaselineAuditStatus.Complete;
            DocumentCount = 1;
            SanitizedSummaryJson = sanitizedSummaryJson;
            ExternalManifestCacheRelativePath = LogicalContentPath.Parse(
                externalManifestCacheRelativePath ??
                throw new ArgumentNullException(nameof(externalManifestCacheRelativePath))).Value;
            if (externalManifestLength <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(externalManifestLength));
            }

            if (!Sha256Utilities.IsLowerSha256(externalManifestSha256))
            {
                throw new ArgumentException(
                    "A lowercase SHA-256 value is required.",
                    nameof(externalManifestSha256));
            }

            ExternalManifestLength = externalManifestLength;
            ExternalManifestSha256 = externalManifestSha256;
        }

        public CsfProjectBaselineAuditStatus Status { get; }

        public int DocumentCount { get; }

        public string SanitizedSummaryJson { get; }

        public string ExternalManifestCacheRelativePath { get; }

        public long ExternalManifestLength { get; }

        public string ExternalManifestSha256 { get; }
    }

    internal sealed class CsfGoldenSampleSpecification
    {
        public CsfGoldenSampleSpecification(
            string logicalName,
            uint expectedMixId,
            long expectedLength,
            string expectedSha256,
            string expectedNormalizedModelSha256)
        {
            LogicalName = LogicalContentPath.Parse(logicalName);
            ExpectedMixId = MixFileId.FromRaw(expectedMixId);
            if (MixFileId.ComputeCandidateId(LogicalName.Value) != ExpectedMixId)
            {
                throw new ArgumentException(
                    "The expected MIX id does not match the logical name.",
                    nameof(expectedMixId));
            }

            if (expectedLength <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(expectedLength));
            }

            if (!Sha256Utilities.IsLowerSha256(expectedSha256) ||
                !Sha256Utilities.IsLowerSha256(expectedNormalizedModelSha256))
            {
                throw new ArgumentException("Lowercase SHA-256 values are required.");
            }

            ExpectedLength = expectedLength;
            ExpectedSha256 = expectedSha256;
            ExpectedNormalizedModelSha256 = expectedNormalizedModelSha256;
        }

        public LogicalContentPath LogicalName { get; }

        public MixFileId ExpectedMixId { get; }

        public long ExpectedLength { get; }

        public string ExpectedSha256 { get; }

        public string ExpectedNormalizedModelSha256 { get; }
    }

    internal sealed class CsfGoldenProvenanceLayer
    {
        public CsfGoldenProvenanceLayer(
            LogicalContentPath archive,
            MixFileId entryId,
            LogicalContentPath resolvedName)
        {
            Archive = archive ?? throw new ArgumentNullException(nameof(archive));
            EntryId = entryId;
            ResolvedName = resolvedName ??
                throw new ArgumentNullException(nameof(resolvedName));
        }

        public LogicalContentPath Archive { get; }

        public MixFileId EntryId { get; }

        public LogicalContentPath ResolvedName { get; }
    }

    internal sealed class CsfGoldenProvenance
    {
        public CsfGoldenProvenance(
            string sourceId,
            LogicalContentPath rootArchive,
            IEnumerable<CsfGoldenProvenanceLayer> layers)
        {
            if (!ContentConfigurationValueRules.IsValidSourceId(sourceId))
            {
                throw new ArgumentException("A valid source id is required.", nameof(sourceId));
            }

            RootArchive = rootArchive ?? throw new ArgumentNullException(nameof(rootArchive));
            CsfGoldenProvenanceLayer[] layerArray =
                (layers ?? throw new ArgumentNullException(nameof(layers))).ToArray();
            if (layerArray.Length == 0 || layerArray.Any(layer => layer == null))
            {
                throw new ArgumentException(
                    "A complete MIX provenance chain is required.",
                    nameof(layers));
            }

            SourceId = sourceId;
            Layers = Array.AsReadOnly(layerArray);
        }

        public string SourceId { get; }

        public LogicalContentPath RootArchive { get; }

        public IReadOnlyList<CsfGoldenProvenanceLayer> Layers { get; }
    }

    internal sealed class CsfGoldenSampleRecord
    {
        public CsfGoldenSampleRecord(
            CsfGoldenSampleSpecification specification,
            CsfGoldenProvenance provenance,
            long length,
            string sha256,
            CsfDocument document,
            int totalValueCount,
            int normalValueCount,
            int extendedValueCount,
            int emptyValueCount,
            int duplicateLabelCount,
            int maximumValuesPerLabel,
            int minimumLabelNameLength,
            int maximumLabelNameLength,
            int minimumMainTextLength,
            int maximumMainTextLength,
            int minimumExtendedTextLength,
            int maximumExtendedTextLength,
            int diagnosticCount)
        {
            Specification = specification ??
                throw new ArgumentNullException(nameof(specification));
            Provenance = provenance ?? throw new ArgumentNullException(nameof(provenance));
            Document = document ?? throw new ArgumentNullException(nameof(document));
            if (length <= 0 || totalValueCount < 0 || normalValueCount < 0 ||
                extendedValueCount < 0 || emptyValueCount < 0 ||
                duplicateLabelCount < 0 || maximumValuesPerLabel < 0 ||
                minimumLabelNameLength < 0 || maximumLabelNameLength < minimumLabelNameLength ||
                minimumMainTextLength < 0 || maximumMainTextLength < minimumMainTextLength ||
                minimumExtendedTextLength < 0 ||
                maximumExtendedTextLength < minimumExtendedTextLength ||
                diagnosticCount < 0 || normalValueCount + extendedValueCount != totalValueCount)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            if (!Sha256Utilities.IsLowerSha256(sha256))
            {
                throw new ArgumentException("A lowercase SHA-256 value is required.", nameof(sha256));
            }

            Length = length;
            Sha256 = sha256;
            TotalValueCount = totalValueCount;
            NormalValueCount = normalValueCount;
            ExtendedValueCount = extendedValueCount;
            EmptyValueCount = emptyValueCount;
            DuplicateLabelCount = duplicateLabelCount;
            MaximumValuesPerLabel = maximumValuesPerLabel;
            MinimumLabelNameLength = minimumLabelNameLength;
            MaximumLabelNameLength = maximumLabelNameLength;
            MinimumMainTextLength = minimumMainTextLength;
            MaximumMainTextLength = maximumMainTextLength;
            MinimumExtendedTextLength = minimumExtendedTextLength;
            MaximumExtendedTextLength = maximumExtendedTextLength;
            DiagnosticCount = diagnosticCount;
        }

        public CsfGoldenSampleSpecification Specification { get; }
        public CsfGoldenProvenance Provenance { get; }
        public long Length { get; }
        public string Sha256 { get; }
        public CsfDocument Document { get; }
        public int TotalValueCount { get; }
        public int NormalValueCount { get; }
        public int ExtendedValueCount { get; }
        public int EmptyValueCount { get; }
        public int DuplicateLabelCount { get; }
        public int MaximumValuesPerLabel { get; }
        public int MinimumLabelNameLength { get; }
        public int MaximumLabelNameLength { get; }
        public int MinimumMainTextLength { get; }
        public int MaximumMainTextLength { get; }
        public int MinimumExtendedTextLength { get; }
        public int MaximumExtendedTextLength { get; }
        public int DiagnosticCount { get; }
    }

    internal sealed class CsfProjectBaselineAuditModel
    {
        public CsfProjectBaselineAuditModel(
            ExternalContentSourceDescriptor source,
            string directoryFingerprint,
            CsfGoldenSampleRecord sample,
            DateTime startedUtc,
            DateTime completedUtc)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            if (!Sha256Utilities.IsLowerSha256(directoryFingerprint))
            {
                throw new ArgumentException(
                    "A lowercase directory fingerprint is required.",
                    nameof(directoryFingerprint));
            }

            if (startedUtc.Kind != DateTimeKind.Utc || completedUtc.Kind != DateTimeKind.Utc ||
                completedUtc < startedUtc)
            {
                throw new ArgumentException("Audit timestamps must be ordered UTC values.");
            }

            DirectoryFingerprint = directoryFingerprint;
            Sample = sample ?? throw new ArgumentNullException(nameof(sample));
            StartedUtc = startedUtc;
            CompletedUtc = completedUtc;
        }

        public ExternalContentSourceDescriptor Source { get; }
        public string DirectoryFingerprint { get; }
        public CsfGoldenSampleRecord Sample { get; }
        public DateTime StartedUtc { get; }
        public DateTime CompletedUtc { get; }
    }

    internal sealed class CsfAuditExternalManifestReference
    {
        public CsfAuditExternalManifestReference(
            string cacheRelativePath,
            long length,
            string sha256)
        {
            CacheRelativePath = LogicalContentPath.Parse(cacheRelativePath).Value;
            if (length <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            if (!Sha256Utilities.IsLowerSha256(sha256))
            {
                throw new ArgumentException("A lowercase SHA-256 is required.", nameof(sha256));
            }

            Length = length;
            Sha256 = sha256;
        }

        public string CacheRelativePath { get; }
        public long Length { get; }
        public string Sha256 { get; }
    }
}
