using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using RA2YR.Core.Content;

namespace RA2YR.Core.Content.PackedMap.Audit
{
    public enum IsoMapPack5ProjectBaselineAuditStatus
    {
        Complete,
        CompleteWithFailures,
        NoCandidates
    }

    public sealed class IsoMapPack5ProjectBaselineAuditProfile
    {
        public IsoMapPack5ProjectBaselineAuditProfile(
            int maxRootArchives = 256,
            int maxMountedEntries = 250000,
            int maxCandidateSections = 10000,
            int maxFragmentsPerSection = 1000000,
            long maxIniBytes = 16 * 1024 * 1024,
            long maxDecodedBytesPerSection = 64 * 1024 * 1024,
            int maxRecordsPerSection = 1000000,
            int maxDiagnostics = 4096)
        {
            if (maxRootArchives < 0 || maxMountedEntries < 0 || maxCandidateSections < 0 || maxFragmentsPerSection < 0 ||
                maxIniBytes < 0 || maxDecodedBytesPerSection < 0 || maxRecordsPerSection < 0 ||
                maxDiagnostics < 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            MaxRootArchives = maxRootArchives;
            MaxMountedEntries = maxMountedEntries;
            MaxCandidateSections = maxCandidateSections;
            MaxFragmentsPerSection = maxFragmentsPerSection;
            MaxIniBytes = maxIniBytes;
            MaxDecodedBytesPerSection = maxDecodedBytesPerSection;
            MaxRecordsPerSection = maxRecordsPerSection;
            MaxDiagnostics = maxDiagnostics;
        }

        public static IsoMapPack5ProjectBaselineAuditProfile ProjectBaseline { get; } =
            new IsoMapPack5ProjectBaselineAuditProfile();

        public int MaxRootArchives { get; }
        public int MaxMountedEntries { get; }
        public int MaxCandidateSections { get; }
        public int MaxFragmentsPerSection { get; }
        public long MaxIniBytes { get; }
        public long MaxDecodedBytesPerSection { get; }
        public int MaxRecordsPerSection { get; }
        public int MaxDiagnostics { get; }
    }

    public sealed class IsoMapPack5ProjectBaselineAuditDelivery
    {
        internal IsoMapPack5ProjectBaselineAuditDelivery(
            IsoMapPack5ProjectBaselineAuditStatus status,
            string sourceFingerprint,
            string sourceFingerprintAfter,
            int rootArchiveCount,
            int mountedEntryCount,
            int candidateSectionCount,
            int successfulSectionCount,
            int failedSectionCount,
            long decodedByteCount,
            long decodedRecordCount,
            int rejectedRemainderCount,
            int preservedRemainderCount,
            int exactFourZeroTrailerCount,
            int duplicateGroupCount,
            int diagnosticCount,
            string aggregateSha256,
            string sanitizedSummaryJson)
        {
            if (string.IsNullOrWhiteSpace(sourceFingerprint) ||
                string.IsNullOrWhiteSpace(sourceFingerprintAfter) ||
                string.IsNullOrWhiteSpace(aggregateSha256))
                throw new ArgumentException("Audit fingerprints are required.");
            if (!Sha256Utilities.IsLowerSha256(sourceFingerprint) ||
                !Sha256Utilities.IsLowerSha256(sourceFingerprintAfter) ||
                !Sha256Utilities.IsLowerSha256(aggregateSha256))
                throw new ArgumentException("Audit fingerprints must be SHA-256 values.");
            Status = status;
            SourceFingerprint = sourceFingerprint;
            SourceFingerprintAfter = sourceFingerprintAfter;
            RootArchiveCount = rootArchiveCount;
            MountedEntryCount = mountedEntryCount;
            CandidateSectionCount = candidateSectionCount;
            SuccessfulSectionCount = successfulSectionCount;
            FailedSectionCount = failedSectionCount;
            DecodedByteCount = decodedByteCount;
            DecodedRecordCount = decodedRecordCount;
            RejectedRemainderCount = rejectedRemainderCount;
            PreservedRemainderCount = preservedRemainderCount;
            ExactFourZeroTrailerCount = exactFourZeroTrailerCount;
            DuplicateGroupCount = duplicateGroupCount;
            DiagnosticCount = diagnosticCount;
            AggregateSha256 = aggregateSha256;
            SanitizedSummaryJson = sanitizedSummaryJson ?? throw new ArgumentNullException(nameof(sanitizedSummaryJson));
        }

        public IsoMapPack5ProjectBaselineAuditStatus Status { get; }
        public string SourceFingerprint { get; }
        public string SourceFingerprintAfter { get; }
        public int RootArchiveCount { get; }
        public int MountedEntryCount { get; }
        public int CandidateSectionCount { get; }
        public int SuccessfulSectionCount { get; }
        public int FailedSectionCount { get; }
        public long DecodedByteCount { get; }
        public long DecodedRecordCount { get; }
        public int RejectedRemainderCount { get; }
        public int PreservedRemainderCount { get; }
        public int ExactFourZeroTrailerCount { get; }
        public int DuplicateGroupCount { get; }
        public int DiagnosticCount { get; }
        public string AggregateSha256 { get; }
        public string SanitizedSummaryJson { get; }
    }
}
