using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using RA2YR.Core.Content.ShpTs.Audit;
using RA2YR.Core.Formats.ShpTs;

namespace RA2YR.Core.Content.ShpTs.Forensics
{
    public enum ShpTsRleForensicDecision
    {
        A1,
        B,
        C,
        D,
        E
    }

    public enum ShpTsRleForensicAuditFailureCode
    {
        InvalidConfiguration,
        BaselineProbeInputDrift,
        DirectoryParseFailed,
        ProductionBaselineMismatch,
        AnalyzerFailed,
        InputModeMismatch,
        ManifestBudgetExceeded,
        ExternalManifestWriteFailed
    }

    public sealed class ShpTsRleForensicAuditException : InvalidOperationException
    {
        internal ShpTsRleForensicAuditException(
            ShpTsRleForensicAuditFailureCode code,
            string message)
            : base(message)
        {
            Code = code;
        }

        public ShpTsRleForensicAuditFailureCode Code { get; }
    }

    public sealed class ShpTsRleForensicAuditDelivery
    {
        internal ShpTsRleForensicAuditDelivery(
            int stageAFrameCount,
            bool stageBExecuted,
            long stageBRowCount,
            ShpTsRleForensicDecision decision,
            bool productionRepairRecommended,
            string inputCatalogSha256,
            string canonicalModelSha256,
            string sanitizedSummaryJson,
            string externalManifestCacheRelativePath,
            long externalManifestLength,
            string externalManifestSha256)
        {
            if (stageAFrameCount <= 0 || stageBRowCount < 0 ||
                !Enum.IsDefined(typeof(ShpTsRleForensicDecision), decision) ||
                !Sha256Utilities.IsLowerSha256(inputCatalogSha256) ||
                !Sha256Utilities.IsLowerSha256(canonicalModelSha256) ||
                string.IsNullOrWhiteSpace(sanitizedSummaryJson) ||
                externalManifestLength <= 0 ||
                !Sha256Utilities.IsLowerSha256(externalManifestSha256))
            {
                throw new ArgumentException("The forensic delivery is inconsistent.");
            }

            StageAFrameCount = stageAFrameCount;
            StageBExecuted = stageBExecuted;
            StageBRowCount = stageBRowCount;
            Decision = decision;
            ProductionRepairRecommended = productionRepairRecommended;
            InputCatalogSha256 = inputCatalogSha256;
            CanonicalModelSha256 = canonicalModelSha256;
            SanitizedSummaryJson = sanitizedSummaryJson;
            ExternalManifestCacheRelativePath = LogicalContentPath.Parse(
                externalManifestCacheRelativePath).Value;
            ExternalManifestLength = externalManifestLength;
            ExternalManifestSha256 = externalManifestSha256;
        }

        public int StageAFrameCount { get; }
        public bool StageBExecuted { get; }
        public long StageBRowCount { get; }
        public ShpTsRleForensicDecision Decision { get; }
        public bool ProductionRepairRecommended { get; }
        public string InputCatalogSha256 { get; }
        public string CanonicalModelSha256 { get; }
        public string SanitizedSummaryJson { get; }
        public string ExternalManifestCacheRelativePath { get; }
        public long ExternalManifestLength { get; }
        public string ExternalManifestSha256 { get; }
    }

    internal enum ShpTsRleForensicCategory
    {
        Building,
        Infantry,
        Animation,
        MapAddon
    }

    internal sealed class ShpTsRleForensicFrameRecord
    {
        public ShpTsRleForensicFrameRecord(
            string sampleId,
            ShpTsRleForensicCategory category,
            int frameIndex,
            ushort width,
            ushort height,
            ShpTsRleForensicRowScalar stageARow,
            ShpTsRleForensicFrameAnalysis stageB)
        {
            SampleId = Binary.BinaryDiagnosticLabel.Validate(sampleId, nameof(sampleId));
            if (frameIndex < 0 || stageARow == null)
            {
                throw new ArgumentException("The forensic frame record is invalid.");
            }

            Category = category;
            FrameIndex = frameIndex;
            Width = width;
            Height = height;
            StageARow = stageARow;
            StageB = stageB;
        }

        public string SampleId { get; }
        public ShpTsRleForensicCategory Category { get; }
        public int FrameIndex { get; }
        public ushort Width { get; }
        public ushort Height { get; }
        public ShpTsRleForensicRowScalar StageARow { get; }
        public ShpTsRleForensicFrameAnalysis StageB { get; }

        public ShpTsRleForensicFrameRecord WithStageB(
            ShpTsRleForensicFrameAnalysis stageB)
        {
            return new ShpTsRleForensicFrameRecord(
                SampleId,
                Category,
                FrameIndex,
                Width,
                Height,
                StageARow,
                stageB ?? throw new ArgumentNullException(nameof(stageB)));
        }
    }

    internal sealed class ShpTsRleForensicAuditModel
    {
        private const string HashDomain = "RA2YR.SHP.TS.RLE.FORENSIC.V1\0";
        private readonly IReadOnlyList<ShpTsRleForensicFrameRecord> records;

        public ShpTsRleForensicAuditModel(
            ExternalContentSourceDescriptor source,
            string directoryFingerprint,
            string inputCatalogSha256,
            IEnumerable<ShpTsRleForensicFrameRecord> records,
            bool stageBExecuted,
            ShpTsRleForensicDecision decision,
            bool inputModesEquivalent,
            DateTime startedUtc,
            DateTime completedUtc)
        {
            ShpTsRleForensicFrameRecord[] values = (records ??
                throw new ArgumentNullException(nameof(records))).ToArray();
            if (source == null || values.Length == 0 || values.Any(value => value == null) ||
                !Sha256Utilities.IsLowerSha256(directoryFingerprint) ||
                !Sha256Utilities.IsLowerSha256(inputCatalogSha256) ||
                completedUtc < startedUtc)
            {
                throw new ArgumentException("The forensic audit model is inconsistent.");
            }

            Source = source;
            DirectoryFingerprint = directoryFingerprint;
            InputCatalogSha256 = inputCatalogSha256;
            this.records = Array.AsReadOnly(values
                .OrderBy(value => value.SampleId, StringComparer.Ordinal)
                .ThenBy(value => value.FrameIndex)
                .ToArray());
            StageBExecuted = stageBExecuted;
            Decision = decision;
            InputModesEquivalent = inputModesEquivalent;
            StartedUtc = startedUtc.ToUniversalTime();
            CompletedUtc = completedUtc.ToUniversalTime();
            CanonicalModelSha256 = ComputeHash();
        }

        public ExternalContentSourceDescriptor Source { get; }
        public string DirectoryFingerprint { get; }
        public string InputCatalogSha256 { get; }
        public IReadOnlyList<ShpTsRleForensicFrameRecord> Records => records;
        public bool StageBExecuted { get; }
        public ShpTsRleForensicDecision Decision { get; }
        public bool InputModesEquivalent { get; }
        public DateTime StartedUtc { get; }
        public DateTime CompletedUtc { get; }
        public string CanonicalModelSha256 { get; }
        public bool ProductionRepairRecommended => Decision == ShpTsRleForensicDecision.A1;

        private string ComputeHash()
        {
            var builder = new StringBuilder(HashDomain);
            builder.Append(DirectoryFingerprint).Append('|')
                .Append(InputCatalogSha256).Append('|')
                .Append(StageBExecuted ? '1' : '0').Append('|')
                .Append(Decision).Append('|')
                .Append(InputModesEquivalent ? '1' : '0').Append('\n');
            foreach (ShpTsRleForensicFrameRecord record in records)
            {
                builder.Append(record.SampleId).Append('|')
                    .Append(record.Category).Append('|')
                    .Append(record.FrameIndex).Append('|')
                    .Append(record.Width).Append('|')
                    .Append(record.Height).Append('\n')
                    .Append(record.StageARow.CanonicalScalar()).Append('\n');
                if (record.StageB != null)
                {
                    builder.Append(record.StageB.CanonicalScalar()).Append('\n');
                }
            }

            using (SHA256 algorithm = SHA256.Create())
            {
                return BitConverter.ToString(algorithm.ComputeHash(
                        new UTF8Encoding(false, true).GetBytes(builder.ToString())))
                    .Replace("-", string.Empty)
                    .ToLowerInvariant();
            }
        }
    }
}
