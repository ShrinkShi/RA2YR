using System;
using System.Collections.Generic;
using System.Linq;
using RA2YR.Core.Content.Mix;
using RA2YR.Core.Formats.Mix;
using RA2YR.Core.Formats.ShpTs;

namespace RA2YR.Core.Content.ShpTs.Audit
{
    public enum ShpTsProjectBaselineAuditStatus
    {
        Complete,
        CompleteWithUnresolvedFrames,
        CompleteWithDecodeFailures
    }

    public enum ShpTsProjectBaselineAuditFailureCode
    {
        InvalidBaselineConfiguration,
        DirectoryIndexIncomplete,
        RootArchiveMissing,
        LooseCandidateFound,
        MixMountFailed,
        TargetMissing,
        TargetAmbiguous,
        TargetIdentityMismatch,
        TargetProvenanceMismatch,
        TargetLengthMismatch,
        TargetHashMismatch,
        ShpParseFailed,
        ShpDecodeFailed,
        InputModeMismatch,
        DirectoryModelHashMismatch,
        DecodedModelHashMismatch,
        BaselineChangedDuringAudit,
        ManifestBudgetExceeded,
        ExternalManifestWriteFailed,
        MountCleanupFailed
    }

    public enum ShpTsSelectionBasis
    {
        ArtExplicitShpRoute,
        ArtExplicitImageCatalogResolved,
        UiResourceConfiguration,
        MouseOrCursorConfiguration,
        VerifiedCatalogSurvey
    }

    public sealed class ShpTsProjectBaselineAuditException : InvalidOperationException
    {
        internal ShpTsProjectBaselineAuditException(
            ShpTsProjectBaselineAuditFailureCode code,
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

        public ShpTsProjectBaselineAuditFailureCode Code { get; }
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

    public sealed class ShpTsProjectBaselineAuditDelivery
    {
        internal ShpTsProjectBaselineAuditDelivery(
            ShpTsProjectBaselineAuditStatus status,
            int sampleCount,
            int unresolvedFrameCount,
            int failedFrameCount,
            string sanitizedSummaryJson,
            string externalManifestCacheRelativePath,
            long externalManifestLength,
            string externalManifestSha256)
        {
            if (!Enum.IsDefined(typeof(ShpTsProjectBaselineAuditStatus), status) ||
                sampleCount <= 0 || unresolvedFrameCount < 0 || failedFrameCount < 0 ||
                string.IsNullOrWhiteSpace(sanitizedSummaryJson) ||
                externalManifestLength <= 0 ||
                !Sha256Utilities.IsLowerSha256(externalManifestSha256))
            {
                throw new ArgumentException("The SHP audit delivery is inconsistent.");
            }

            Status = status;
            SampleCount = sampleCount;
            UnresolvedFrameCount = unresolvedFrameCount;
            FailedFrameCount = failedFrameCount;
            SanitizedSummaryJson = sanitizedSummaryJson;
            ExternalManifestCacheRelativePath = LogicalContentPath.Parse(
                externalManifestCacheRelativePath).Value;
            ExternalManifestLength = externalManifestLength;
            ExternalManifestSha256 = externalManifestSha256;
        }

        public ShpTsProjectBaselineAuditStatus Status { get; }
        public int SampleCount { get; }
        public int UnresolvedFrameCount { get; }
        public int FailedFrameCount { get; }
        public string SanitizedSummaryJson { get; }
        public string ExternalManifestCacheRelativePath { get; }
        public long ExternalManifestLength { get; }
        public string ExternalManifestSha256 { get; }
    }

    internal sealed class ShpTsGoldenSampleSpecification
    {
        public ShpTsGoldenSampleSpecification(
            string sampleId,
            string logicalRole,
            ShpTsSelectionBasis selectionBasis,
            string logicalName,
            string rootArchive,
            IEnumerable<string> expectedArchiveChain,
            long expectedLength,
            string expectedSha256,
            string expectedDirectoryModelSha256 = null,
            string expectedDecodedModelSha256 = null)
        {
            if (string.IsNullOrWhiteSpace(sampleId) ||
                string.IsNullOrWhiteSpace(logicalRole) ||
                !Enum.IsDefined(typeof(ShpTsSelectionBasis), selectionBasis))
            {
                throw new ArgumentException("A controlled SHP sample identity is required.");
            }

            LogicalName = LogicalContentPath.Parse(logicalName);
            RootArchive = LogicalContentPath.Parse(rootArchive);
            LogicalContentPath[] chain = (expectedArchiveChain ??
                throw new ArgumentNullException(nameof(expectedArchiveChain)))
                .Select(LogicalContentPath.Parse)
                .ToArray();
            if (chain.Length == 0 || !chain[0].Equals(RootArchive) ||
                expectedLength <= 0 || !Sha256Utilities.IsLowerSha256(expectedSha256) ||
                (expectedDirectoryModelSha256 != null &&
                    !Sha256Utilities.IsLowerSha256(expectedDirectoryModelSha256)) ||
                (expectedDecodedModelSha256 != null &&
                    !Sha256Utilities.IsLowerSha256(expectedDecodedModelSha256)))
            {
                throw new ArgumentException("The controlled SHP sample specification is invalid.");
            }

            SampleId = Binary.BinaryDiagnosticLabel.Validate(sampleId, nameof(sampleId));
            LogicalRole = Binary.BinaryDiagnosticLabel.Validate(logicalRole, nameof(logicalRole));
            SelectionBasis = selectionBasis;
            ExpectedMixId = MixFileId.ComputeCandidateId(LogicalName.Value);
            ExpectedArchiveChain = Array.AsReadOnly(chain);
            ExpectedLength = expectedLength;
            ExpectedSha256 = expectedSha256;
            ExpectedDirectoryModelSha256 = expectedDirectoryModelSha256;
            ExpectedDecodedModelSha256 = expectedDecodedModelSha256;
        }

        public string SampleId { get; }
        public string LogicalRole { get; }
        public ShpTsSelectionBasis SelectionBasis { get; }
        public LogicalContentPath LogicalName { get; }
        public MixFileId ExpectedMixId { get; }
        public LogicalContentPath RootArchive { get; }
        public IReadOnlyList<LogicalContentPath> ExpectedArchiveChain { get; }
        public long ExpectedLength { get; }
        public string ExpectedSha256 { get; }
        public string ExpectedDirectoryModelSha256 { get; }
        public string ExpectedDecodedModelSha256 { get; }
    }

    internal sealed class ShpTsProjectBaselineAuditProfile
    {
        public ShpTsProjectBaselineAuditProfile(
            IEnumerable<ShpTsGoldenSampleSpecification> samples,
            long maxExternalManifestUtf8Bytes,
            MixMountLimits mountLimits,
            ShpTsReadLimits readLimits)
        {
            ShpTsGoldenSampleSpecification[] values = (samples ??
                throw new ArgumentNullException(nameof(samples))).ToArray();
            if (values.Length == 0 || values.Any(value => value == null) ||
                values.GroupBy(value => value.SampleId, StringComparer.Ordinal)
                    .Any(group => group.Count() != 1) ||
                maxExternalManifestUtf8Bytes <= 0)
            {
                throw new ArgumentException("The SHP audit profile is invalid.");
            }

            Samples = Array.AsReadOnly(values
                .OrderBy(value => value.SampleId, StringComparer.Ordinal)
                .ToArray());
            MaxExternalManifestUtf8Bytes = maxExternalManifestUtf8Bytes;
            MountLimits = mountLimits ?? throw new ArgumentNullException(nameof(mountLimits));
            ReadLimits = readLimits ?? throw new ArgumentNullException(nameof(readLimits));
        }

        public static ShpTsProjectBaselineAuditProfile ProjectBaseline { get; } =
            new ShpTsProjectBaselineAuditProfile(
                new[]
                {
                    new ShpTsGoldenSampleSpecification(
                        "building-explicit-image",
                        "building-explicit-image",
                        ShpTsSelectionBasis.ArtExplicitImageCatalogResolved,
                        "yatech.shp",
                        "ra2md.mix",
                        new[] { "ra2md.mix", "ra2md.mix/snowmd.mix" },
                        50184,
                        "1addf99f3958875c4561915acb1865f91a311afc526015569d95058c0b2a4460",
                        "7e7886f36057505d5274e0b44bef3c2dab7f80a76a4d25089dbc2a2facd0e4a9",
                        "8313e86a462c0ad94fec669c9002f508831d5bb77297c34f3c25b63d0ade2893"),
                    new ShpTsGoldenSampleSpecification(
                        "infantry-explicit-image",
                        "infantry-explicit-image",
                        ShpTsSelectionBasis.ArtExplicitImageCatalogResolved,
                        "engineer.shp",
                        "ra2.mix",
                        new[] { "ra2.mix", "ra2.mix/conquer.mix" },
                        114032,
                        "f8eb0dc0156a877028ac2ed57bf2e71a9445ffcb6ef6a4caf3b0dbb7b111834c",
                        "9f69eab472f99f5cb6b624760a148de1edf5f97eea24129dc950a640277472cc",
                        "4a858e914051130cd51e1e0c1d6e646b7779045499a40fb1c62fa080d45099ac"),
                    new ShpTsGoldenSampleSpecification(
                        "map-addon-catalog-survey",
                        "map-addon-catalog-survey",
                        ShpTsSelectionBasis.VerifiedCatalogSurvey,
                        "cnoild.shp",
                        "expandmd01.mix",
                        new[] { "expandmd01.mix" },
                        16016,
                        "d7e92839fef021b832d96b4571f870a939f124efe77b33b503711188dea93077",
                        "762caa222a8033fcc56cb01b3cbdd3956b707fca10a65d79aabe3c304b6a5fd7",
                        "8313e86a462c0ad94fec669c9002f508831d5bb77297c34f3c25b63d0ade2893"),
                    new ShpTsGoldenSampleSpecification(
                        "mouse-cursor-catalog-survey",
                        "mouse-cursor-catalog-survey",
                        ShpTsSelectionBasis.VerifiedCatalogSurvey,
                        "mouse.shp",
                        "ra2.mix",
                        new[] { "ra2.mix", "ra2.mix/conquer.mix" },
                        359800,
                        "e5a356737787d681dd1a2b1255c7c7d1e9bd8334e5674705f2ccc39cd12634df",
                        "76ab99c686c8745ae6faee099eddb03c76f72a2fbc7ef449da02bb32abb87e1c",
                        "e38e2630b44da98d444dc833751f522af4c88d310ebb99216e4169340e1d0595"),
                    new ShpTsGoldenSampleSpecification(
                        "techno-animation-catalog-survey",
                        "techno-animation-catalog-survey",
                        ShpTsSelectionBasis.VerifiedCatalogSurvey,
                        "chronofd.shp",
                        "ra2.mix",
                        new[] { "ra2.mix", "ra2.mix/conquer.mix" },
                        298016,
                        "e4ce5033b296035ed5ad3f66bc8d5cba2c29b80c7061970fcacb6e5d749a6cff",
                        "8e626052744fcce362aab01a6fb5d10442304662a7d71b4f64663ae7e9357f65",
                        "8313e86a462c0ad94fec669c9002f508831d5bb77297c34f3c25b63d0ade2893"),
                    new ShpTsGoldenSampleSpecification(
                        "ui-cameo-configuration",
                        "ui-cameo-configuration",
                        ShpTsSelectionBasis.UiResourceConfiguration,
                        "e1icon.shp",
                        "language.mix",
                        new[] { "language.mix", "language.mix/cameo.mix" },
                        2912,
                        "438d514ffbd5e0bf925d16b71dd6bb0e03c1c259b95e47c6879d94e12fe93768",
                        "9e7342edbebc2173b6d2fc934e10e2c02f315716cbc86e74529792eb8d31a781",
                        "23ef215926a6ab7c52204bed389355613fe4465822af770984fd555294b2befc")
                },
                16L * 1024 * 1024,
                MixMountLimits.Default,
                ShpTsReadLimits.Default);

        public IReadOnlyList<ShpTsGoldenSampleSpecification> Samples { get; }
        public long MaxExternalManifestUtf8Bytes { get; }
        public MixMountLimits MountLimits { get; }
        public ShpTsReadLimits ReadLimits { get; }
    }

    internal sealed class ShpTsGoldenSampleEntryContext
    {
        public ShpTsGoldenSampleEntryContext(
            ShpTsGoldenSampleSpecification specification,
            MixVirtualEntry entry)
        {
            Specification = specification ??
                throw new ArgumentNullException(nameof(specification));
            Entry = entry ?? throw new ArgumentNullException(nameof(entry));
        }

        public ShpTsGoldenSampleSpecification Specification { get; }
        public MixVirtualEntry Entry { get; }
    }

    internal sealed class ShpTsAuditProvenanceLayer
    {
        public ShpTsAuditProvenanceLayer(
            LogicalContentPath archive,
            MixFileId entryId,
            LogicalContentPath resolvedName)
        {
            Archive = archive ?? throw new ArgumentNullException(nameof(archive));
            EntryId = entryId;
            ResolvedName = resolvedName ?? throw new ArgumentNullException(nameof(resolvedName));
        }

        public LogicalContentPath Archive { get; }
        public MixFileId EntryId { get; }
        public LogicalContentPath ResolvedName { get; }
    }

    internal sealed class ShpTsAuditFrameRecord
    {
        public ShpTsAuditFrameRecord(
            ShpTsFrameDescriptor descriptor,
            string decodeStatus,
            long bytesConsumed,
            long paddingBytes,
            int pixelCount,
            int minimumIndex,
            int maximumIndex,
            IEnumerable<ShpTsDiagnostic> diagnostics)
        {
            Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            DecodeStatus = Binary.BinaryDiagnosticLabel.Validate(
                decodeStatus, nameof(decodeStatus));
            if (bytesConsumed < 0 || paddingBytes < 0 || pixelCount < 0 ||
                minimumIndex < 0 || minimumIndex > 255 ||
                maximumIndex < minimumIndex || maximumIndex > 255)
            {
                throw new ArgumentOutOfRangeException(nameof(bytesConsumed));
            }

            ShpTsDiagnostic[] diagnosticArray = (diagnostics ??
                throw new ArgumentNullException(nameof(diagnostics))).ToArray();
            if (diagnosticArray.Any(value => value == null))
            {
                throw new ArgumentException("Frame diagnostics cannot contain null.");
            }

            BytesConsumed = bytesConsumed;
            PaddingBytes = paddingBytes;
            PixelCount = pixelCount;
            MinimumIndex = minimumIndex;
            MaximumIndex = maximumIndex;
            Diagnostics = Array.AsReadOnly(diagnosticArray);
        }

        public ShpTsFrameDescriptor Descriptor { get; }
        public string DecodeStatus { get; }
        public long BytesConsumed { get; }
        public long PaddingBytes { get; }
        public int PixelCount { get; }
        public int MinimumIndex { get; }
        public int MaximumIndex { get; }
        public IReadOnlyList<ShpTsDiagnostic> Diagnostics { get; }
    }

    internal sealed class ShpTsGoldenSampleRecord
    {
        public ShpTsGoldenSampleRecord(
            ShpTsGoldenSampleSpecification specification,
            IEnumerable<ShpTsAuditProvenanceLayer> provenanceLayers,
            long length,
            string sha256,
            ShpTsDocument directory,
            ShpTsDecodedDocument decoded,
            IEnumerable<ShpTsAuditFrameRecord> frames,
            IEnumerable<ShpTsDiagnostic> diagnostics,
            bool memoryStreamWindowEquivalent)
        {
            Specification = specification ?? throw new ArgumentNullException(nameof(specification));
            Directory = directory ?? throw new ArgumentNullException(nameof(directory));
            Decoded = decoded ?? throw new ArgumentNullException(nameof(decoded));
            ProvenanceLayers = Array.AsReadOnly((provenanceLayers ??
                throw new ArgumentNullException(nameof(provenanceLayers))).ToArray());
            Frames = Array.AsReadOnly((frames ??
                throw new ArgumentNullException(nameof(frames))).ToArray());
            Diagnostics = Array.AsReadOnly((diagnostics ??
                throw new ArgumentNullException(nameof(diagnostics))).ToArray());
            if (ProvenanceLayers.Count == 0 || Frames.Count != Directory.Frames.Count ||
                length <= 0 || !Sha256Utilities.IsLowerSha256(sha256) ||
                !memoryStreamWindowEquivalent)
            {
                throw new ArgumentException("The completed SHP sample record is inconsistent.");
            }

            Length = length;
            Sha256 = sha256;
            MemoryStreamWindowEquivalent = memoryStreamWindowEquivalent;
        }

        public ShpTsGoldenSampleSpecification Specification { get; }
        public IReadOnlyList<ShpTsAuditProvenanceLayer> ProvenanceLayers { get; }
        public long Length { get; }
        public string Sha256 { get; }
        public ShpTsDocument Directory { get; }
        public ShpTsDecodedDocument Decoded { get; }
        public IReadOnlyList<ShpTsAuditFrameRecord> Frames { get; }
        public IReadOnlyList<ShpTsDiagnostic> Diagnostics { get; }
        public bool MemoryStreamWindowEquivalent { get; }

        public int UnresolvedFrameCount => Frames.Count(frame =>
            string.Equals(frame.DecodeStatus, "unresolved", StringComparison.Ordinal));

        public int FailedFrameCount => Frames.Count(frame =>
            string.Equals(frame.DecodeStatus, "failed", StringComparison.Ordinal));
    }

    internal sealed class ShpTsProjectBaselineAuditModel
    {
        public ShpTsProjectBaselineAuditModel(
            ExternalContentSourceDescriptor source,
            string directoryFingerprint,
            IEnumerable<ShpTsGoldenSampleRecord> samples,
            DateTime startedUtc,
            DateTime completedUtc)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            if (!Sha256Utilities.IsLowerSha256(directoryFingerprint) ||
                startedUtc.Kind != DateTimeKind.Utc ||
                completedUtc.Kind != DateTimeKind.Utc || completedUtc < startedUtc)
            {
                throw new ArgumentException("The SHP audit model identity is invalid.");
            }

            ShpTsGoldenSampleRecord[] values = (samples ??
                throw new ArgumentNullException(nameof(samples))).ToArray();
            if (values.Length == 0 || values.Any(value => value == null))
            {
                throw new ArgumentException("At least one SHP sample is required.");
            }

            DirectoryFingerprint = directoryFingerprint;
            Samples = Array.AsReadOnly(values
                .OrderBy(value => value.Specification.SampleId, StringComparer.Ordinal)
                .ToArray());
            StartedUtc = startedUtc;
            CompletedUtc = completedUtc;
        }

        public ExternalContentSourceDescriptor Source { get; }
        public string DirectoryFingerprint { get; }
        public IReadOnlyList<ShpTsGoldenSampleRecord> Samples { get; }
        public DateTime StartedUtc { get; }
        public DateTime CompletedUtc { get; }
    }

    internal sealed class ShpTsAuditExternalManifestReference
    {
        public ShpTsAuditExternalManifestReference(
            string cacheRelativePath,
            long length,
            string sha256)
        {
            CacheRelativePath = LogicalContentPath.Parse(cacheRelativePath).Value;
            if (length <= 0 || !Sha256Utilities.IsLowerSha256(sha256))
            {
                throw new ArgumentException("The SHP manifest reference is invalid.");
            }

            Length = length;
            Sha256 = sha256;
        }

        public string CacheRelativePath { get; }
        public long Length { get; }
        public string Sha256 { get; }
    }
}
