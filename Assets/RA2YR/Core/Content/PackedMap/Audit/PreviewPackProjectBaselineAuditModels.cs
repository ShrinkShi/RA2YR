using System;

namespace RA2YR.Core.Content.PackedMap.Audit
{
    public enum PreviewPackProjectBaselineAuditStatus
    {
        Complete,
        CompleteWithFailures,
        CompleteWithNoCandidates
    }

    public sealed class PreviewPackProjectBaselineAuditProfile
    {
        public PreviewPackProjectBaselineAuditProfile(
            int maxRootArchives = 256,
            int maxMountedEntries = 250000,
            int maxCandidateEntries = 10000,
            int maxPreviewSectionsPerEntry = 64,
            int maxPreviewPackSectionsPerEntry = 64,
            int maxFragmentsPerSection = 1000000,
            long maxIniBytes = 16 * 1024 * 1024,
            long maxDecodedBytesPerSection = 64 * 1024 * 1024,
            int maxDiagnostics = 4096)
        {
            if (maxRootArchives < 0 || maxMountedEntries < 0 || maxCandidateEntries < 0 ||
                maxPreviewSectionsPerEntry < 0 || maxPreviewPackSectionsPerEntry < 0 ||
                maxFragmentsPerSection < 0 || maxIniBytes < 0 || maxDecodedBytesPerSection < 0 ||
                maxDiagnostics < 0)
                throw new ArgumentOutOfRangeException();

            MaxRootArchives = maxRootArchives;
            MaxMountedEntries = maxMountedEntries;
            MaxCandidateEntries = maxCandidateEntries;
            MaxPreviewSectionsPerEntry = maxPreviewSectionsPerEntry;
            MaxPreviewPackSectionsPerEntry = maxPreviewPackSectionsPerEntry;
            MaxFragmentsPerSection = maxFragmentsPerSection;
            MaxIniBytes = maxIniBytes;
            MaxDecodedBytesPerSection = maxDecodedBytesPerSection;
            MaxDiagnostics = maxDiagnostics;
        }

        public static PreviewPackProjectBaselineAuditProfile ProjectBaseline { get; } =
            new PreviewPackProjectBaselineAuditProfile();

        public int MaxRootArchives { get; }
        public int MaxMountedEntries { get; }
        public int MaxCandidateEntries { get; }
        public int MaxPreviewSectionsPerEntry { get; }
        public int MaxPreviewPackSectionsPerEntry { get; }
        public int MaxFragmentsPerSection { get; }
        public long MaxIniBytes { get; }
        public long MaxDecodedBytesPerSection { get; }
        public int MaxDiagnostics { get; }
    }

    public sealed class PreviewPackProjectBaselineAuditDelivery
    {
        internal PreviewPackProjectBaselineAuditDelivery(
            PreviewPackProjectBaselineAuditStatus status,
            string sourceFingerprint,
            string sourceFingerprintAfter,
            int rootArchiveCount,
            int mountedEntryCount,
            int candidateEntryCount,
            int previewPresentCount,
            int previewPackPresentCount,
            int bothPresentCount,
            int missingPreviewCount,
            int missingPreviewPackCount,
            int validMetadataCount,
            int invalidMetadataCount,
            int exactDecodedCount,
            int underflowCount,
            int overflowCount,
            int failedCount,
            int diagnosticCount,
            string aggregateSha256,
            string sanitizedSummaryJson)
        {
            SourceFingerprint = sourceFingerprint ?? throw new ArgumentNullException(nameof(sourceFingerprint));
            SourceFingerprintAfter = sourceFingerprintAfter ?? throw new ArgumentNullException(nameof(sourceFingerprintAfter));
            AggregateSha256 = aggregateSha256 ?? throw new ArgumentNullException(nameof(aggregateSha256));
            Status = status;
            RootArchiveCount = rootArchiveCount;
            MountedEntryCount = mountedEntryCount;
            CandidateEntryCount = candidateEntryCount;
            PreviewPresentCount = previewPresentCount;
            PreviewPackPresentCount = previewPackPresentCount;
            BothPresentCount = bothPresentCount;
            MissingPreviewCount = missingPreviewCount;
            MissingPreviewPackCount = missingPreviewPackCount;
            ValidMetadataCount = validMetadataCount;
            InvalidMetadataCount = invalidMetadataCount;
            ExactDecodedCount = exactDecodedCount;
            UnderflowCount = underflowCount;
            OverflowCount = overflowCount;
            FailedCount = failedCount;
            DiagnosticCount = diagnosticCount;
            SanitizedSummaryJson = sanitizedSummaryJson ?? throw new ArgumentNullException(nameof(sanitizedSummaryJson));
        }

        public PreviewPackProjectBaselineAuditStatus Status { get; }
        public string SourceFingerprint { get; }
        public string SourceFingerprintAfter { get; }
        public int RootArchiveCount { get; }
        public int MountedEntryCount { get; }
        public int CandidateEntryCount { get; }
        public int PreviewPresentCount { get; }
        public int PreviewPackPresentCount { get; }
        public int BothPresentCount { get; }
        public int MissingPreviewCount { get; }
        public int MissingPreviewPackCount { get; }
        public int ValidMetadataCount { get; }
        public int InvalidMetadataCount { get; }
        public int ExactDecodedCount { get; }
        public int UnderflowCount { get; }
        public int OverflowCount { get; }
        public int FailedCount { get; }
        public int DiagnosticCount { get; }
        public string AggregateSha256 { get; }
        public string SanitizedSummaryJson { get; }
    }
}
