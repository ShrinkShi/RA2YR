using System;
using System.Collections.Generic;
using System.Linq;

namespace RA2YR.Core.Content.TmpTheater.Audit
{
    public enum TmpTheaterAuditStatus { Complete, CompleteWithFailures, CompleteWithNoCandidates, Failed }

    public sealed class TmpTheaterProfileAuditAggregate
    {
        internal TmpTheaterProfileAuditAggregate(string profile) { Profile = profile; }
        public string Profile { get; }
        public int ControlDocumentCount { get; internal set; }
        public int TmpCandidateCount { get; internal set; }
        public int ValidTmpCount { get; internal set; }
        public int InvalidTmpCount { get; internal set; }
        public int CellCount { get; internal set; }
        public int EmptySlotCount { get; internal set; }
        public int Header48CandidateCount { get; internal set; }
        public int Header52ProductionCount { get; internal set; }
        public int DeclaredSuccessCount { get; internal set; }
        public int SequentialWithZSuccessCount { get; internal set; }
        public int SequentialWithoutZSuccessCount { get; internal set; }
        public int MissingAssetCount { get; internal set; }
        public int UnknownFlagCellCount { get; internal set; }
        public int TrailingCellCount { get; internal set; }
    }

    public sealed class TmpTheaterProjectBaselineAuditDelivery
    {
        internal TmpTheaterProjectBaselineAuditDelivery(TmpTheaterAuditStatus status, string before, string after,
            IEnumerable<TmpTheaterProfileAuditAggregate> profiles, int roots, int mountedEntries, int candidates,
            int successes, int failures, int header48, int header52, int declared, int withZ, int withoutZ,
            int memoryStreamEquivalent, string hash, string summary)
        {
            Status = status; SourceFingerprintBefore = before; SourceFingerprintAfter = after;
            Profiles = Array.AsReadOnly((profiles ?? Enumerable.Empty<TmpTheaterProfileAuditAggregate>()).ToArray());
            RootArchiveCount = roots; MountedEntryCount = mountedEntries; CandidateCount = candidates; SuccessCount = successes; FailureCount = failures;
            Header48CandidateCount = header48; Header52ProductionCount = header52; DeclaredOffsetSuccessCount = declared;
            SequentialWithZSuccessCount = withZ; SequentialWithoutZSuccessCount = withoutZ; MemoryStreamEquivalentCount = memoryStreamEquivalent;
            AggregateSha256 = hash; SanitizedSummaryJson = summary;
        }
        public TmpTheaterAuditStatus Status { get; }
        public string SourceFingerprintBefore { get; }
        public string SourceFingerprintAfter { get; }
        public IReadOnlyList<TmpTheaterProfileAuditAggregate> Profiles { get; }
        public int RootArchiveCount { get; }
        public int MountedEntryCount { get; }
        public int CandidateCount { get; }
        public int SuccessCount { get; }
        public int FailureCount { get; }
        public int Header48CandidateCount { get; }
        public int Header52ProductionCount { get; }
        public int DeclaredOffsetSuccessCount { get; }
        public int SequentialWithZSuccessCount { get; }
        public int SequentialWithoutZSuccessCount { get; }
        public int MemoryStreamEquivalentCount { get; }
        public string AggregateSha256 { get; }
        public string SanitizedSummaryJson { get; }
    }
}
