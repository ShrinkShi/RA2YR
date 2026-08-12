using System;

namespace RA2YR.Core.Content.MapTerrain.Audit
{
    public enum MapTerrainProjectBaselineAuditStatus { Complete, CompleteWithFailures, CompleteWithNoCandidates }

    public sealed class MapTerrainProjectBaselineAuditDelivery
    {
        public MapTerrainProjectBaselineAuditDelivery(MapTerrainProjectBaselineAuditStatus status, int roots, int mounted, int mapCandidates, int parsedCandidates, int incompleteBindings, int failures, string before, string after, string hash, string summary)
        { Status = status; RootArchiveCount = roots; MountedEntryCount = mounted; MapCandidateCount = mapCandidates; ParsedCandidateCount = parsedCandidates; IncompleteBindingCount = incompleteBindings; FailureCount = failures; SourceFingerprintBefore = before; SourceFingerprintAfter = after; AggregateSha256 = hash; SanitizedSummaryJson = summary; }
        public MapTerrainProjectBaselineAuditStatus Status { get; }
        public int RootArchiveCount { get; }
        public int MountedEntryCount { get; }
        public int MapCandidateCount { get; }
        public int ParsedCandidateCount { get; }
        public int IncompleteBindingCount { get; }
        public int FailureCount { get; }
        public string SourceFingerprintBefore { get; }
        public string SourceFingerprintAfter { get; }
        public string AggregateSha256 { get; }
        public string SanitizedSummaryJson { get; }
    }
}
