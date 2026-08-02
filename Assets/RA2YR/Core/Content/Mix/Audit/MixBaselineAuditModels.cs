using System;
using System.Collections.Generic;
using System.Linq;
using RA2YR.Core.Formats.Mix;

namespace RA2YR.Core.Content.Mix.Audit
{
    public enum MixBaselineAuditStatus
    {
        Complete,
        CompleteWithArchiveFailures
    }

    public enum MixBaselineAuditFailureCode
    {
        InvalidBaselineConfiguration,
        DirectoryIndexIncomplete,
        NoRootMixArchives,
        RootArchiveBudgetExceeded,
        XccNameDatabasePathRejected,
        XccNameDatabaseBudgetExceeded,
        XccNameDatabaseChanged,
        XccNameDatabaseHashMismatch,
        XccNameDatabaseInvalid,
        BaselineChangedDuringAudit,
        ManifestBudgetExceeded,
        ExternalManifestWriteFailed,
        RootMountCleanupFailed
    }

    public sealed class MixBaselineAuditException : InvalidOperationException
    {
        internal MixBaselineAuditException(
            MixBaselineAuditFailureCode code,
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

        public MixBaselineAuditFailureCode Code { get; }

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

    public sealed class MixBaselineAuditDelivery
    {
        internal MixBaselineAuditDelivery(
            MixBaselineAuditStatus status,
            int rootArchiveCount,
            int parsedRootArchiveCount,
            string sanitizedSummaryJson,
            string externalManifestCacheRelativePath,
            long externalManifestLength,
            string externalManifestSha256)
        {
            if (rootArchiveCount < 0 || parsedRootArchiveCount < 0 ||
                parsedRootArchiveCount > rootArchiveCount)
            {
                throw new ArgumentOutOfRangeException(nameof(rootArchiveCount));
            }

            Status = status;
            RootArchiveCount = rootArchiveCount;
            ParsedRootArchiveCount = parsedRootArchiveCount;
            SanitizedSummaryJson = sanitizedSummaryJson ??
                throw new ArgumentNullException(nameof(sanitizedSummaryJson));
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

        public MixBaselineAuditStatus Status { get; }

        public int RootArchiveCount { get; }

        public int ParsedRootArchiveCount { get; }

        public int FailedRootArchiveCount =>
            RootArchiveCount - ParsedRootArchiveCount;

        public string SanitizedSummaryJson { get; }

        public string ExternalManifestCacheRelativePath { get; }

        public long ExternalManifestLength { get; }

        public string ExternalManifestSha256 { get; }
    }

    internal sealed class MixBaselineAuditProfile
    {
        public MixBaselineAuditProfile(
            string expectedXccDatabaseSha256,
            long maxXccDatabaseBytes,
            int maxRootArchives,
            long maxManifestUtf8Bytes,
            MixMountLimits mountLimits)
        {
            if (!Sha256Utilities.IsLowerSha256(expectedXccDatabaseSha256))
            {
                throw new ArgumentException(
                    "A lowercase SHA-256 value is required.",
                    nameof(expectedXccDatabaseSha256));
            }

            if (maxXccDatabaseBytes < 0 || maxRootArchives < 0 ||
                maxManifestUtf8Bytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxXccDatabaseBytes));
            }

            ExpectedXccDatabaseSha256 = expectedXccDatabaseSha256;
            MaxXccDatabaseBytes = maxXccDatabaseBytes;
            MaxRootArchives = maxRootArchives;
            MaxManifestUtf8Bytes = maxManifestUtf8Bytes;
            MountLimits = mountLimits ?? throw new ArgumentNullException(nameof(mountLimits));
        }

        public static MixBaselineAuditProfile ProjectBaseline { get; } =
            new MixBaselineAuditProfile(
                "c76f529af17cbe516e85aa4dddce614cf0ad98a8590208c71fbe3a047fb77ab8",
                16L * 1024 * 1024,
                4096,
                256L * 1024 * 1024,
                MixMountLimits.Default);

        public string ExpectedXccDatabaseSha256 { get; }

        public long MaxXccDatabaseBytes { get; }

        public int MaxRootArchives { get; }

        public long MaxManifestUtf8Bytes { get; }

        public MixMountLimits MountLimits { get; }
    }

    internal sealed class MixBaselineRootAudit : IDisposable
    {
        public MixBaselineRootAudit(
            ContentFileRecord rootFile,
            MixVirtualContentMountResult mount)
        {
            RootFile = rootFile ?? throw new ArgumentNullException(nameof(rootFile));
            Mount = mount ?? throw new ArgumentNullException(nameof(mount));
        }

        public ContentFileRecord RootFile { get; }

        public MixVirtualContentMountResult Mount { get; }

        public bool IsParsed => Mount.IsComplete;

        public void Dispose()
        {
            Mount.Dispose();
        }
    }

    internal sealed class MixBaselineTargetMatch
    {
        public MixBaselineTargetMatch(
            string storageKind,
            long length,
            string sha256,
            bool encryptedChain,
            MixEntryProvenance provenance)
        {
            if (storageKind != "Directory" && storageKind != "MixArchive")
            {
                throw new ArgumentException("Unknown target storage kind.", nameof(storageKind));
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            if (!Sha256Utilities.IsLowerSha256(sha256))
            {
                throw new ArgumentException("A lowercase SHA-256 is required.", nameof(sha256));
            }

            if ((storageKind == "Directory" && provenance != null) ||
                (storageKind == "MixArchive" && provenance == null))
            {
                throw new ArgumentException(
                    "Only MIX archive target matches carry MIX provenance.",
                    nameof(provenance));
            }

            StorageKind = storageKind;
            Length = length;
            Sha256 = sha256;
            EncryptedChain = encryptedChain;
            Provenance = provenance;
        }

        public string StorageKind { get; }

        public long Length { get; }

        public string Sha256 { get; }

        public bool EncryptedChain { get; }

        public MixEntryProvenance Provenance { get; }
    }

    internal sealed class MixBaselineTargetAudit
    {
        public MixBaselineTargetAudit(
            LogicalContentPath logicalPath,
            MixFileId id,
            IEnumerable<MixBaselineTargetMatch> matches,
            bool nameIdAmbiguous)
        {
            LogicalPath = logicalPath ?? throw new ArgumentNullException(nameof(logicalPath));
            Id = id;
            Matches = Array.AsReadOnly(
                (matches ?? throw new ArgumentNullException(nameof(matches))).ToArray());
            NameIdAmbiguous = nameIdAmbiguous;
        }

        public LogicalContentPath LogicalPath { get; }

        public MixFileId Id { get; }

        public IReadOnlyList<MixBaselineTargetMatch> Matches { get; }

        public bool NameIdAmbiguous { get; }

        public int DiagnosticCount =>
            (Matches.Count == 0 ? 1 : 0) +
            (Matches.Count > 1 ? 1 : 0) +
            (NameIdAmbiguous ? 1 : 0);

        public string Status => NameIdAmbiguous || Matches.Count > 1
            ? "ambiguous"
            : Matches.Count == 1
                ? "found"
                : "not-found";
    }

    internal sealed class MixBaselineAuditModel
    {
        public MixBaselineAuditModel(
            ExternalContentSourceDescriptor source,
            string directoryFingerprint,
            string xccDatabaseSha256,
            long xccDatabaseLength,
            IEnumerable<MixBaselineRootAudit> roots,
            IEnumerable<MixBaselineTargetAudit> targets,
            DateTime startedUtc,
            DateTime completedUtc)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            if (!Sha256Utilities.IsLowerSha256(directoryFingerprint))
            {
                throw new ArgumentException(
                    "A lowercase SHA-256 directory fingerprint is required.",
                    nameof(directoryFingerprint));
            }

            if (!Sha256Utilities.IsLowerSha256(xccDatabaseSha256))
            {
                throw new ArgumentException(
                    "A lowercase SHA-256 XCC database digest is required.",
                    nameof(xccDatabaseSha256));
            }

            if (xccDatabaseLength < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(xccDatabaseLength));
            }

            if (startedUtc.Kind != DateTimeKind.Utc || completedUtc.Kind != DateTimeKind.Utc ||
                completedUtc < startedUtc)
            {
                throw new ArgumentException(
                    "Audit timestamps must be ordered UTC values.",
                    nameof(completedUtc));
            }

            DirectoryFingerprint = directoryFingerprint;
            XccDatabaseSha256 = xccDatabaseSha256;
            XccDatabaseLength = xccDatabaseLength;
            MixBaselineRootAudit[] rootArray =
                (roots ?? throw new ArgumentNullException(nameof(roots))).ToArray();
            MixBaselineTargetAudit[] targetArray =
                (targets ?? throw new ArgumentNullException(nameof(targets))).ToArray();
            if (rootArray.Any(root => root == null) ||
                targetArray.Any(target => target == null))
            {
                throw new ArgumentException("Audit collections cannot contain null values.");
            }

            Roots = Array.AsReadOnly(rootArray);
            Targets = Array.AsReadOnly(targetArray);
            StartedUtc = startedUtc;
            CompletedUtc = completedUtc;
        }

        public ExternalContentSourceDescriptor Source { get; }

        public string DirectoryFingerprint { get; }

        public string XccDatabaseSha256 { get; }

        public long XccDatabaseLength { get; }

        public IReadOnlyList<MixBaselineRootAudit> Roots { get; }

        public IReadOnlyList<MixBaselineTargetAudit> Targets { get; }

        public DateTime StartedUtc { get; }

        public DateTime CompletedUtc { get; }
    }
}
