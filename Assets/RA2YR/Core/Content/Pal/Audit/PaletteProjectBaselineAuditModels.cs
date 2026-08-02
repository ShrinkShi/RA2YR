using System;
using System.Collections.Generic;
using System.Linq;
using RA2YR.Core.Content.Mix;
using RA2YR.Core.Formats.Mix;
using RA2YR.Core.Formats.Pal;

namespace RA2YR.Core.Content.Pal.Audit
{
    public enum PaletteProjectBaselineAuditStatus
    {
        Complete
    }

    public enum PaletteProjectBaselineAuditFailureCode
    {
        InvalidBaselineConfiguration,
        DirectoryIndexIncomplete,
        RootArchiveMissing,
        LoosePaletteCandidateFound,
        MixMountFailed,
        TargetMissing,
        TargetAmbiguous,
        TargetIdentityMismatch,
        TargetProvenanceMismatch,
        TargetLengthMismatch,
        TargetHashMismatch,
        PaletteParseFailed,
        NormalizedModelHashMismatch,
        BaselineChangedDuringAudit,
        ManifestBudgetExceeded,
        ExternalManifestWriteFailed,
        MountCleanupFailed
    }

    public sealed class PaletteProjectBaselineAuditException : InvalidOperationException
    {
        internal PaletteProjectBaselineAuditException(
            PaletteProjectBaselineAuditFailureCode code,
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

        public PaletteProjectBaselineAuditFailureCode Code { get; }

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

    public sealed class PaletteProjectBaselineAuditDelivery
    {
        internal PaletteProjectBaselineAuditDelivery(
            int paletteCount,
            string sanitizedSummaryJson,
            string externalManifestCacheRelativePath,
            long externalManifestLength,
            string externalManifestSha256)
        {
            if (paletteCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(paletteCount));
            }

            if (string.IsNullOrEmpty(sanitizedSummaryJson))
            {
                throw new ArgumentException(
                    "A sanitized summary is required.",
                    nameof(sanitizedSummaryJson));
            }

            Status = PaletteProjectBaselineAuditStatus.Complete;
            PaletteCount = paletteCount;
            SanitizedSummaryJson = sanitizedSummaryJson;
            ExternalManifestCacheRelativePath = LogicalContentPath.Parse(
                externalManifestCacheRelativePath ??
                throw new ArgumentNullException(nameof(externalManifestCacheRelativePath))).Value;
            if (externalManifestLength < 0)
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

        public PaletteProjectBaselineAuditStatus Status { get; }

        public int PaletteCount { get; }

        public string SanitizedSummaryJson { get; }

        public string ExternalManifestCacheRelativePath { get; }

        public long ExternalManifestLength { get; }

        public string ExternalManifestSha256 { get; }
    }

    internal sealed class PaletteGoldenSampleSpecification
    {
        public PaletteGoldenSampleSpecification(
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
                throw new ArgumentException(
                    "Lowercase SHA-256 values are required.",
                    nameof(expectedSha256));
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

    internal sealed class PaletteProjectBaselineAuditProfile
    {
        public PaletteProjectBaselineAuditProfile(
            IEnumerable<PaletteGoldenSampleSpecification> samples,
            long maxExternalManifestUtf8Bytes,
            MixMountLimits mountLimits,
            PaletteReadLimits readLimits)
        {
            PaletteGoldenSampleSpecification[] sampleArray =
                (samples ?? throw new ArgumentNullException(nameof(samples))).ToArray();
            if (sampleArray.Length == 0 || sampleArray.Any(sample => sample == null) ||
                sampleArray.GroupBy(sample => sample.LogicalName).Any(group => group.Count() != 1) ||
                sampleArray.GroupBy(sample => sample.ExpectedMixId).Any(group => group.Count() != 1))
            {
                throw new ArgumentException(
                    "Golden palette specifications must be non-empty and unique.",
                    nameof(samples));
            }

            if (maxExternalManifestUtf8Bytes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExternalManifestUtf8Bytes));
            }

            Samples = Array.AsReadOnly(sampleArray
                .OrderBy(sample => sample.LogicalName, LogicalContentPathReportComparer.Instance)
                .ToArray());
            MaxExternalManifestUtf8Bytes = maxExternalManifestUtf8Bytes;
            MountLimits = mountLimits ?? throw new ArgumentNullException(nameof(mountLimits));
            ReadLimits = readLimits ?? throw new ArgumentNullException(nameof(readLimits));
        }

        public static PaletteProjectBaselineAuditProfile ProjectBaseline { get; } =
            new PaletteProjectBaselineAuditProfile(
                new[]
                {
                    new PaletteGoldenSampleSpecification(
                        "isotem.pal",
                        0x5f9d97b9u,
                        768,
                        "5d6e40fcd11a592a31494c635d93c21796cfe86a2743f0258b1f7d0aff850795",
                        "f8650500f5d49f5fe8dd050eda345e1eb9eec82b42a8064770ed58c9c31c6524"),
                    new PaletteGoldenSampleSpecification(
                        "temperat.pal",
                        0x9c58de40u,
                        768,
                        "5903b69868b84f494cfbb4e7100398015ef9775b37726019a0d7b5fb6cb33b55",
                        "8932af31cfa5a30098429efdc5ab61445af555b95cc827721426bf066ef1fc42"),
                    new PaletteGoldenSampleSpecification(
                        "unittem.pal",
                        0x63da7359u,
                        768,
                        "ed785e62eed291480f3198dd44f6b656ebe3a9b75e9f641944d710abc6bde3e3",
                        "36d158b0a336d5f0ebb3749e66c79089191f4336dd970f02f7c5c24d35207717")
                },
                4L * 1024 * 1024,
                MixMountLimits.Default,
                PaletteReadLimits.Default);

        public IReadOnlyList<PaletteGoldenSampleSpecification> Samples { get; }

        public long MaxExternalManifestUtf8Bytes { get; }

        public MixMountLimits MountLimits { get; }

        public PaletteReadLimits ReadLimits { get; }
    }

    internal sealed class PaletteGoldenProvenanceLayer
    {
        public PaletteGoldenProvenanceLayer(
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

    internal sealed class PaletteGoldenProvenance
    {
        public PaletteGoldenProvenance(
            string sourceId,
            LogicalContentPath rootArchive,
            IEnumerable<PaletteGoldenProvenanceLayer> layers)
        {
            if (!ContentConfigurationValueRules.IsValidSourceId(sourceId))
            {
                throw new ArgumentException("A valid source id is required.", nameof(sourceId));
            }

            RootArchive = rootArchive ?? throw new ArgumentNullException(nameof(rootArchive));
            PaletteGoldenProvenanceLayer[] layerArray =
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

        public IReadOnlyList<PaletteGoldenProvenanceLayer> Layers { get; }
    }

    internal sealed class PaletteGoldenSampleRecord
    {
        private readonly byte[] rawRgbTriplets;

        public PaletteGoldenSampleRecord(
            PaletteGoldenSampleSpecification specification,
            PaletteGoldenProvenance provenance,
            long length,
            string sha256,
            int colorCount,
            int rawChannelMin,
            int rawChannelMax,
            int invalidChannelCount,
            int distinctColorCount,
            string normalizedModelSha256,
            string displayConversionStrategy,
            int diagnosticCount,
            byte[] rawRgbTriplets)
        {
            Specification = specification ??
                throw new ArgumentNullException(nameof(specification));
            Provenance = provenance ?? throw new ArgumentNullException(nameof(provenance));

            if (length < 0 || colorCount < 0 || rawChannelMin < 0 ||
                rawChannelMax < rawChannelMin || invalidChannelCount < 0 ||
                distinctColorCount < 0 || distinctColorCount > colorCount ||
                diagnosticCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            if (!Sha256Utilities.IsLowerSha256(sha256) ||
                !Sha256Utilities.IsLowerSha256(normalizedModelSha256))
            {
                throw new ArgumentException("Lowercase SHA-256 values are required.");
            }

            if (string.IsNullOrWhiteSpace(displayConversionStrategy))
            {
                throw new ArgumentException(
                    "A display conversion strategy identifier is required.",
                    nameof(displayConversionStrategy));
            }

            byte[] rawCopy = (byte[])(rawRgbTriplets ??
                throw new ArgumentNullException(nameof(rawRgbTriplets))).Clone();
            if (rawCopy.LongLength != length || rawCopy.Length != checked(colorCount * 3))
            {
                throw new ArgumentException(
                    "Raw RGB triplets must match the audited model dimensions.",
                    nameof(rawRgbTriplets));
            }

            Specification = specification;
            Length = length;
            Sha256 = sha256;
            ColorCount = colorCount;
            RawChannelMin = rawChannelMin;
            RawChannelMax = rawChannelMax;
            InvalidChannelCount = invalidChannelCount;
            DistinctColorCount = distinctColorCount;
            NormalizedModelSha256 = normalizedModelSha256;
            DisplayConversionStrategy = displayConversionStrategy;
            DiagnosticCount = diagnosticCount;
            this.rawRgbTriplets = rawCopy;
        }

        public PaletteGoldenSampleSpecification Specification { get; }

        public PaletteGoldenProvenance Provenance { get; }

        public long Length { get; }

        public string Sha256 { get; }

        public int ColorCount { get; }

        public int RawChannelMin { get; }

        public int RawChannelMax { get; }

        public int InvalidChannelCount { get; }

        public int DistinctColorCount { get; }

        public string NormalizedModelSha256 { get; }

        public string DisplayConversionStrategy { get; }

        public int DiagnosticCount { get; }

        public byte[] GetRawRgbTripletsCopy()
        {
            return (byte[])rawRgbTriplets.Clone();
        }
    }

    internal sealed class PaletteProjectBaselineAuditModel
    {
        public PaletteProjectBaselineAuditModel(
            ExternalContentSourceDescriptor source,
            string directoryFingerprint,
            IEnumerable<PaletteGoldenSampleRecord> palettes,
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

            PaletteGoldenSampleRecord[] paletteArray =
                (palettes ?? throw new ArgumentNullException(nameof(palettes))).ToArray();
            if (paletteArray.Length == 0 || paletteArray.Any(palette => palette == null))
            {
                throw new ArgumentException(
                    "At least one completed palette record is required.",
                    nameof(palettes));
            }

            if (startedUtc.Kind != DateTimeKind.Utc || completedUtc.Kind != DateTimeKind.Utc ||
                completedUtc < startedUtc)
            {
                throw new ArgumentException("Audit timestamps must be ordered UTC values.");
            }

            DirectoryFingerprint = directoryFingerprint;
            Palettes = Array.AsReadOnly(paletteArray
                .OrderBy(palette => palette.Specification.LogicalName,
                    LogicalContentPathReportComparer.Instance)
                .ToArray());
            StartedUtc = startedUtc;
            CompletedUtc = completedUtc;
        }

        public ExternalContentSourceDescriptor Source { get; }

        public string DirectoryFingerprint { get; }

        public IReadOnlyList<PaletteGoldenSampleRecord> Palettes { get; }

        public DateTime StartedUtc { get; }

        public DateTime CompletedUtc { get; }
    }

    internal sealed class PaletteAuditExternalManifestReference
    {
        public PaletteAuditExternalManifestReference(
            string cacheRelativePath,
            long length,
            string sha256)
        {
            CacheRelativePath = LogicalContentPath.Parse(cacheRelativePath).Value;
            if (length < 0)
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
