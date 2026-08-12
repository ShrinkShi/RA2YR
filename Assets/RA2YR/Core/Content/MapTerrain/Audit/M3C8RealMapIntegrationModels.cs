using System;

namespace RA2YR.Core.Content.MapTerrain.Audit
{
    public enum M3C8AuditStatus
    {
        Complete,
        CompleteWithFailures,
        CompleteWithNoCandidates
    }

    public sealed class M3C8RealMapIntegrationDelivery
    {
        internal M3C8RealMapIntegrationDelivery(
            M3C8AuditStatus status, string before, string after, int roots, int mounts,
            int mapCandidates, int isoCandidates, int isoSuccesses, int isoFailures,
            int previewCandidates, int previewExact, int previewFailures,
            int theaterCandidates, int theaterSuccesses, int theaterFailures,
            int terrainFullyBound, int terrainPartiallyBound, int terrainUnresolved,
            int diagnostics, string hash, string summary)
        {
            Status = status; SourceFingerprintBefore = before; SourceFingerprintAfter = after;
            RootArchiveCount = roots; MountedEntryCount = mounts; MapCandidateCount = mapCandidates;
            IsoMapCandidateCount = isoCandidates; IsoMapSuccessCount = isoSuccesses; IsoMapFailureCount = isoFailures;
            PreviewCandidateCount = previewCandidates; PreviewExactCount = previewExact; PreviewFailureCount = previewFailures;
            TheaterCandidateCount = theaterCandidates; TheaterSuccessCount = theaterSuccesses; TheaterFailureCount = theaterFailures;
            TerrainFullyBoundCount = terrainFullyBound; TerrainPartiallyBoundCount = terrainPartiallyBound; TerrainUnresolvedCount = terrainUnresolved;
            DiagnosticCount = diagnostics; AggregateSha256 = hash; SanitizedSummaryJson = summary;
        }

        public M3C8AuditStatus Status { get; }
        public string SourceFingerprintBefore { get; }
        public string SourceFingerprintAfter { get; }
        public int RootArchiveCount { get; }
        public int MountedEntryCount { get; }
        public int MapCandidateCount { get; }
        public int IsoMapCandidateCount { get; }
        public int IsoMapSuccessCount { get; }
        public int IsoMapFailureCount { get; }
        public int PreviewCandidateCount { get; }
        public int PreviewExactCount { get; }
        public int PreviewFailureCount { get; }
        public int TheaterCandidateCount { get; }
        public int TheaterSuccessCount { get; }
        public int TheaterFailureCount { get; }
        public int TerrainFullyBoundCount { get; }
        public int TerrainPartiallyBoundCount { get; }
        public int TerrainUnresolvedCount { get; }
        public int DiagnosticCount { get; }
        public string AggregateSha256 { get; }
        public string SanitizedSummaryJson { get; }
    }
}
