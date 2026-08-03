using System;
using System.Collections.Generic;
using System.Linq;
using RA2YR.Core.Content.Mix;
using RA2YR.Core.Formats.Ini;
using RA2YR.Core.Formats.Mix;

namespace RA2YR.Core.Content.Ini.Audit
{
    public enum IniProjectBaselineAuditStatus
    {
        Complete
    }

    public enum IniProjectBaselineAuditFailureCode
    {
        InvalidBaselineConfiguration,
        DirectoryIndexIncomplete,
        RootArchiveMissing,
        LooseIniCandidateFound,
        MixMountFailed,
        GoldenTargetMissing,
        GoldenTargetAmbiguous,
        GoldenTargetIdentityMismatch,
        GoldenTargetProvenanceMismatch,
        GoldenTargetLengthMismatch,
        GoldenTargetHashMismatch,
        IniParseFailed,
        IdentityWriteFailed,
        IdentityVerificationFailed,
        BaselineChangedDuringAudit,
        ManifestBudgetExceeded,
        ExternalArtifactWriteFailed,
        MountCleanupFailed
    }

    public sealed class IniProjectBaselineAuditException : InvalidOperationException
    {
        internal IniProjectBaselineAuditException(
            IniProjectBaselineAuditFailureCode code,
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

        public IniProjectBaselineAuditFailureCode Code { get; }

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

    public sealed class IniProjectBaselineAuditDelivery
    {
        internal IniProjectBaselineAuditDelivery(
            int documentCount,
            int locatedSurveyCandidateCount,
            string sanitizedSummaryJson,
            string externalManifestCacheRelativePath,
            long externalManifestLength,
            string externalManifestSha256)
        {
            if (documentCount <= 0 || locatedSurveyCandidateCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(documentCount));
            }

            if (string.IsNullOrEmpty(sanitizedSummaryJson))
            {
                throw new ArgumentException(
                    "A sanitized summary is required.",
                    nameof(sanitizedSummaryJson));
            }

            if (externalManifestLength <= 0 ||
                !Sha256Utilities.IsLowerSha256(externalManifestSha256))
            {
                throw new ArgumentException("A valid external manifest reference is required.");
            }

            Status = IniProjectBaselineAuditStatus.Complete;
            DocumentCount = documentCount;
            LocatedSurveyCandidateCount = locatedSurveyCandidateCount;
            SanitizedSummaryJson = sanitizedSummaryJson;
            ExternalManifestCacheRelativePath = LogicalContentPath.Parse(
                externalManifestCacheRelativePath ??
                throw new ArgumentNullException(nameof(externalManifestCacheRelativePath))).Value;
            ExternalManifestLength = externalManifestLength;
            ExternalManifestSha256 = externalManifestSha256;
        }

        public IniProjectBaselineAuditStatus Status { get; }
        public int DocumentCount { get; }
        public int LocatedSurveyCandidateCount { get; }
        public string SanitizedSummaryJson { get; }
        public string ExternalManifestCacheRelativePath { get; }
        public long ExternalManifestLength { get; }
        public string ExternalManifestSha256 { get; }
    }

    internal sealed class IniGoldenSampleSpecification
    {
        public IniGoldenSampleSpecification(
            string sampleId,
            string rootArchive,
            string nestedArchive,
            string logicalName,
            long expectedLength,
            string expectedSha256,
            string expectedCanonicalModelSha256 = null)
        {
            if (!ContentConfigurationValueRules.IsValidSourceId(sampleId))
            {
                throw new ArgumentException("A stable sample id is required.", nameof(sampleId));
            }

            RootArchive = LogicalContentPath.Parse(rootArchive);
            NestedArchive = nestedArchive == null
                ? null
                : LogicalContentPath.Parse(nestedArchive);
            LogicalName = LogicalContentPath.Parse(logicalName);
            ExpectedMixId = MixFileId.ComputeCandidateId(LogicalName.Value);
            if (expectedLength <= 0 || !Sha256Utilities.IsLowerSha256(expectedSha256))
            {
                throw new ArgumentException("A fixed length and lowercase SHA-256 are required.");
            }

            if (expectedCanonicalModelSha256 != null &&
                !Sha256Utilities.IsLowerSha256(expectedCanonicalModelSha256))
            {
                throw new ArgumentException(
                    "The optional canonical model SHA-256 must be lowercase.",
                    nameof(expectedCanonicalModelSha256));
            }

            SampleId = sampleId;
            ExpectedLength = expectedLength;
            ExpectedSha256 = expectedSha256;
            ExpectedCanonicalModelSha256 = expectedCanonicalModelSha256;
        }

        public string SampleId { get; }
        public LogicalContentPath RootArchive { get; }
        public LogicalContentPath NestedArchive { get; }
        public LogicalContentPath LogicalName { get; }
        public MixFileId ExpectedMixId { get; }
        public long ExpectedLength { get; }
        public string ExpectedSha256 { get; }
        public string ExpectedCanonicalModelSha256 { get; }
    }

    internal sealed class IniAuditProvenanceLayer
    {
        public IniAuditProvenanceLayer(
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

    internal sealed class IniAuditProvenance
    {
        public IniAuditProvenance(
            string sourceId,
            LogicalContentPath rootArchive,
            IEnumerable<IniAuditProvenanceLayer> layers)
        {
            if (!ContentConfigurationValueRules.IsValidSourceId(sourceId))
            {
                throw new ArgumentException("A valid source id is required.", nameof(sourceId));
            }

            RootArchive = rootArchive ?? throw new ArgumentNullException(nameof(rootArchive));
            IniAuditProvenanceLayer[] values =
                (layers ?? throw new ArgumentNullException(nameof(layers))).ToArray();
            if (values.Length == 0 || values.Any(value => value == null))
            {
                throw new ArgumentException("A complete MIX provenance chain is required.");
            }

            SourceId = sourceId;
            Layers = Array.AsReadOnly(values);
        }

        public string SourceId { get; }
        public LogicalContentPath RootArchive { get; }
        public IReadOnlyList<IniAuditProvenanceLayer> Layers { get; }
    }

    internal sealed class IniLineAuditRecord
    {
        public IniLineAuditRecord(
            int lineId,
            long absoluteOffset,
            int contentLength,
            int endingLength,
            IniLineEnding ending,
            IniNodeKind nodeKind,
            IniOpaqueReason? opaqueReason,
            string rawLineSha256)
        {
            if (lineId < 0 || absoluteOffset < 0 || contentLength < 0 || endingLength < 0 ||
                !Sha256Utilities.IsLowerSha256(rawLineSha256))
            {
                throw new ArgumentOutOfRangeException(nameof(lineId));
            }

            LineId = lineId;
            AbsoluteOffset = absoluteOffset;
            ContentLength = contentLength;
            EndingLength = endingLength;
            Ending = ending;
            NodeKind = nodeKind;
            OpaqueReason = opaqueReason;
            RawLineSha256 = rawLineSha256;
        }

        public int LineId { get; }
        public long AbsoluteOffset { get; }
        public int ContentLength { get; }
        public int EndingLength { get; }
        public IniLineEnding Ending { get; }
        public IniNodeKind NodeKind { get; }
        public IniOpaqueReason? OpaqueReason { get; }
        public string RawLineSha256 { get; }
    }

    internal sealed class IniGoldenSampleRecord
    {
        public IniGoldenSampleRecord(
            IniGoldenSampleSpecification specification,
            IniAuditProvenance provenance,
            IniRawDocument document,
            long length,
            string sha256,
            string identitySha256,
            string identityCacheRelativePath,
            bool byteIdentical,
            int crlfCount,
            int lfCount,
            int crCount,
            int noEndingCount,
            int sectionCount,
            int keyValueCount,
            int commentCount,
            int blankCount,
            int opaqueCount,
            int duplicateSectionCount,
            int duplicateKeyCount,
            int maximumLineLength,
            IReadOnlyDictionary<string, int> diagnosticCounts,
            IEnumerable<IniLineAuditRecord> lineRecords)
        {
            Specification = specification ?? throw new ArgumentNullException(nameof(specification));
            Provenance = provenance ?? throw new ArgumentNullException(nameof(provenance));
            Document = document ?? throw new ArgumentNullException(nameof(document));
            if (length <= 0 || !Sha256Utilities.IsLowerSha256(sha256) ||
                !Sha256Utilities.IsLowerSha256(identitySha256) ||
                !byteIdentical || crlfCount < 0 || lfCount < 0 || crCount < 0 ||
                noEndingCount < 0 || sectionCount < 0 || keyValueCount < 0 ||
                commentCount < 0 || blankCount < 0 || opaqueCount < 0 ||
                duplicateSectionCount < 0 || duplicateKeyCount < 0 ||
                maximumLineLength < 0)
            {
                throw new ArgumentException("The INI sample statistics are inconsistent.");
            }

            IniLineAuditRecord[] records =
                (lineRecords ?? throw new ArgumentNullException(nameof(lineRecords))).ToArray();
            if (records.Length != document.Lines.Count || records.Any(record => record == null))
            {
                throw new ArgumentException("One line audit record is required per physical line.");
            }

            Specification = specification;
            Length = length;
            Sha256 = sha256;
            IdentitySha256 = identitySha256;
            IdentityCacheRelativePath = LogicalContentPath.Parse(
                identityCacheRelativePath ??
                throw new ArgumentNullException(nameof(identityCacheRelativePath))).Value;
            ByteIdentical = byteIdentical;
            CrlfCount = crlfCount;
            LfCount = lfCount;
            CrCount = crCount;
            NoEndingCount = noEndingCount;
            SectionCount = sectionCount;
            KeyValueCount = keyValueCount;
            CommentCount = commentCount;
            BlankCount = blankCount;
            OpaqueCount = opaqueCount;
            DuplicateSectionCount = duplicateSectionCount;
            DuplicateKeyCount = duplicateKeyCount;
            MaximumLineLength = maximumLineLength;
            DiagnosticCounts = (diagnosticCounts ??
                    throw new ArgumentNullException(nameof(diagnosticCounts)))
                .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
            LineRecords = Array.AsReadOnly(records);
        }

        public IniGoldenSampleSpecification Specification { get; }
        public IniAuditProvenance Provenance { get; }
        public IniRawDocument Document { get; }
        public long Length { get; }
        public string Sha256 { get; }
        public string IdentitySha256 { get; }
        public string IdentityCacheRelativePath { get; }
        public bool ByteIdentical { get; }
        public int CrlfCount { get; }
        public int LfCount { get; }
        public int CrCount { get; }
        public int NoEndingCount { get; }
        public int SectionCount { get; }
        public int KeyValueCount { get; }
        public int CommentCount { get; }
        public int BlankCount { get; }
        public int OpaqueCount { get; }
        public int DuplicateSectionCount { get; }
        public int DuplicateKeyCount { get; }
        public int MaximumLineLength { get; }
        public IReadOnlyDictionary<string, int> DiagnosticCounts { get; }
        public IReadOnlyList<IniLineAuditRecord> LineRecords { get; }
    }

    internal sealed class IniSurveyCandidate
    {
        public IniSurveyCandidate(
            LogicalContentPath logicalName,
            MixFileId mixId,
            IniAuditProvenance provenance,
            long length,
            string sha256)
        {
            LogicalName = logicalName ?? throw new ArgumentNullException(nameof(logicalName));
            MixId = mixId;
            Provenance = provenance ?? throw new ArgumentNullException(nameof(provenance));
            if (length < 0 || !Sha256Utilities.IsLowerSha256(sha256))
            {
                throw new ArgumentException("A surveyed MIX candidate requires bounded metadata.");
            }

            Length = length;
            Sha256 = sha256;
        }

        public LogicalContentPath LogicalName { get; }
        public MixFileId MixId { get; }
        public IniAuditProvenance Provenance { get; }
        public long Length { get; }
        public string Sha256 { get; }
    }

    internal sealed class IniProjectBaselineAuditModel
    {
        public IniProjectBaselineAuditModel(
            ExternalContentSourceDescriptor source,
            string directoryFingerprint,
            IEnumerable<IniGoldenSampleRecord> samples,
            IEnumerable<IniSurveyCandidate> surveyCandidates,
            IEnumerable<LogicalContentPath> unresolvedSurveyNames,
            DateTime startedUtc,
            DateTime completedUtc)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            if (!Sha256Utilities.IsLowerSha256(directoryFingerprint))
            {
                throw new ArgumentException("A lowercase directory fingerprint is required.");
            }

            IniGoldenSampleRecord[] sampleArray =
                (samples ?? throw new ArgumentNullException(nameof(samples))).ToArray();
            IniSurveyCandidate[] surveyArray =
                (surveyCandidates ?? throw new ArgumentNullException(nameof(surveyCandidates))).ToArray();
            LogicalContentPath[] unresolvedArray =
                (unresolvedSurveyNames ??
                    throw new ArgumentNullException(nameof(unresolvedSurveyNames))).ToArray();
            if (sampleArray.Length == 0 || sampleArray.Any(value => value == null) ||
                surveyArray.Any(value => value == null) || unresolvedArray.Any(value => value == null) ||
                startedUtc.Kind != DateTimeKind.Utc || completedUtc.Kind != DateTimeKind.Utc ||
                completedUtc < startedUtc)
            {
                throw new ArgumentException("The INI audit model is incomplete.");
            }

            DirectoryFingerprint = directoryFingerprint;
            Samples = Array.AsReadOnly(sampleArray);
            SurveyCandidates = Array.AsReadOnly(surveyArray);
            UnresolvedSurveyNames = Array.AsReadOnly(unresolvedArray);
            StartedUtc = startedUtc;
            CompletedUtc = completedUtc;
        }

        public ExternalContentSourceDescriptor Source { get; }
        public string DirectoryFingerprint { get; }
        public IReadOnlyList<IniGoldenSampleRecord> Samples { get; }
        public IReadOnlyList<IniSurveyCandidate> SurveyCandidates { get; }
        public IReadOnlyList<LogicalContentPath> UnresolvedSurveyNames { get; }
        public DateTime StartedUtc { get; }
        public DateTime CompletedUtc { get; }
    }

    internal sealed class IniAuditExternalManifestReference
    {
        public IniAuditExternalManifestReference(string cacheRelativePath, long length, string sha256)
        {
            CacheRelativePath = LogicalContentPath.Parse(cacheRelativePath).Value;
            if (length <= 0 || !Sha256Utilities.IsLowerSha256(sha256))
            {
                throw new ArgumentException("A valid external manifest reference is required.");
            }

            Length = length;
            Sha256 = sha256;
        }

        public string CacheRelativePath { get; }
        public long Length { get; }
        public string Sha256 { get; }
    }
}
