using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using RA2YR.Core.Binary;
using RA2YR.Core.Content.Mix;
using RA2YR.Core.Content.ShpTs.Audit;
using RA2YR.Core.Formats.ShpTs;

namespace RA2YR.Core.Content.ShpTs.Forensics
{
    public static class ShpTsRleForensicAuditService
    {
        private const int ExpectedCandidateFrames = 257;
        private const int ExpectedOddWidths = 137;
        private const int ExpectedEvenWidths = 120;
        private const int ExpectedMinimumWidth = 14;
        private const int ExpectedMaximumWidth = 202;
        private const long MaximumExternalManifestBytes = 32L * 1024 * 1024;

        public static ShpTsRleForensicAuditDelivery Run(
            ExternalContentConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            DateTime startedUtc = DateTime.UtcNow;
            return ShpTsProjectBaselineAuditService.UseFixedEntries(
                configuration,
                (source, fingerprint, entries) => RunWithEntries(
                    configuration,
                    source,
                    fingerprint,
                    entries,
                    startedUtc,
                    DateTime.UtcNow));
        }

        private static ShpTsRleForensicAuditDelivery RunWithEntries(
            ExternalContentConfiguration configuration,
            ExternalContentSourceDescriptor source,
            string fingerprint,
            IReadOnlyList<ShpTsGoldenSampleEntryContext> entries,
            DateTime startedUtc,
            DateTime completedUtc)
        {
            var bundles = new List<SampleBundle>(entries.Count);
            var candidates = new List<Candidate>();
            int productionFailures = 0;
            int productionRowZeroFailures = 0;
            int productionOverflowFailures = 0;
            bool inputModesEquivalent = true;
            foreach (ShpTsGoldenSampleEntryContext context in entries)
            {
                SampleBundle bundle = LoadBundle(context);
                bundles.Add(bundle);
                foreach (ShpTsFrameDescriptor descriptor in bundle.MemoryDocument.Frames.Where(
                    value => value.RawFlags == 3 && !value.IsCanonicalEmpty))
                {
                    ShpTsDecodeResult production = WestwoodShpTsDecoder.DecodeFrame(
                        bundle.Bytes,
                        bundle.MemoryDocument,
                        descriptor.Index,
                        ShpTsProjectBaselineAuditProfile.ProjectBaseline.ReadLimits);
                    if (!production.IsSuccess)
                    {
                        productionFailures++;
                        if (production.Diagnostics.Any(value => value.RowIndex == 0))
                        {
                            productionRowZeroFailures++;
                        }
                        if (production.Diagnostics.Any(value =>
                                value.Code == ShpTsDiagnosticCode.RleOutputOverflow))
                        {
                            productionOverflowFailures++;
                        }
                    }

                    ShpTsRleForensicFrameAnalysis stageA = AnalyzeEquivalent(
                        bundle,
                        descriptor.Index,
                        false,
                        ref inputModesEquivalent);
                    if (!stageA.IsSuccess || stageA.Rows.Count != 1)
                    {
                        throw Failure(
                            ShpTsRleForensicAuditFailureCode.AnalyzerFailed,
                            "The independent row-zero forensic analyzer failed closed.");
                    }

                    var record = new ShpTsRleForensicFrameRecord(
                        context.Specification.SampleId,
                        bundle.Category,
                        descriptor.Index,
                        descriptor.WidthRaw,
                        descriptor.HeightRaw,
                        stageA.Rows[0],
                        null);
                    candidates.Add(new Candidate(bundle, descriptor.Index, record));
                }
            }

            ValidateBaselineLock(
                candidates,
                productionFailures,
                productionRowZeroFailures,
                productionOverflowFailures);

            bool stageBExecuted = candidates.All(candidate =>
                candidate.Record.StageARow.GuardPattern &&
                candidate.Record.StageARow.ExtraSource ==
                    ShpTsRleForensicExtraSource.ZeroRun &&
                candidate.Record.StageARow.ExtraIsZero &&
                candidate.Record.StageARow.ExtraFromLastCommand &&
                candidate.Record.StageARow.ExtraOvershoot == 1 &&
                candidate.Record.StageARow.IgnoreOneExtraInputExact);

            var records = new List<ShpTsRleForensicFrameRecord>(candidates.Count);
            if (stageBExecuted)
            {
                foreach (Candidate candidate in candidates)
                {
                    ShpTsRleForensicFrameAnalysis stageB = AnalyzeEquivalent(
                        candidate.Bundle,
                        candidate.FrameIndex,
                        true,
                        ref inputModesEquivalent);
                    records.Add(candidate.Record.WithStageB(stageB));
                }
            }
            else
            {
                records.AddRange(candidates.Select(candidate => candidate.Record));
            }

            ShpTsRleForensicDecision decision = Decide(records, stageBExecuted);
            string catalogHash = ComputeInputCatalogHash(fingerprint, entries);
            var model = new ShpTsRleForensicAuditModel(
                source,
                fingerprint,
                catalogHash,
                records,
                stageBExecuted,
                decision,
                inputModesEquivalent,
                startedUtc,
                completedUtc);
            byte[] externalBytes = ShpTsRleForensicSerializer.SerializeExternalManifestUtf8(
                model,
                MaximumExternalManifestBytes);
            ShpTsAuditExternalManifestReference external;
            try
            {
                external = ShpTsAuditExternalManifestWriter.Write(
                    configuration,
                    source.Id,
                    fingerprint,
                    externalBytes);
            }
            catch (ShpTsProjectBaselineAuditException exception)
            {
                throw new ShpTsRleForensicAuditException(
                    ShpTsRleForensicAuditFailureCode.ExternalManifestWriteFailed,
                    exception.Message);
            }

            string summary = ShpTsRleForensicSerializer.SerializeSanitizedSummary(
                model,
                external);
            return new ShpTsRleForensicAuditDelivery(
                records.Count,
                stageBExecuted,
                records.Where(value => value.StageB != null)
                    .Sum(value => (long)value.StageB.Rows.Count),
                decision,
                model.ProductionRepairRecommended,
                catalogHash,
                model.CanonicalModelSha256,
                summary,
                external.CacheRelativePath,
                external.Length,
                external.Sha256);
        }

        private static SampleBundle LoadBundle(ShpTsGoldenSampleEntryContext context)
        {
            ShpTsReadLimits limits = ShpTsProjectBaselineAuditProfile.ProjectBaseline.ReadLimits;
            byte[] bytes = Snapshot(context.Entry, limits);
            ShpTsSourceProvenance provenance = BuildProvenance(context.Entry);
            var source = new BinarySourceContext(
                "format.shp-ts-rle-forensic",
                context.Entry.Provenance.Source.Id,
                context.Specification.LogicalName);
            long absoluteStart = context.Entry.PayloadWindow.AbsoluteStartOffset;
            ShpTsParseResult memory = WestwoodShpTsReader.Read(
                bytes,
                source,
                provenance,
                limits,
                absoluteStart);
            ShpTsParseResult stream;
            using (var input = new MemoryStream(bytes, false))
            {
                stream = WestwoodShpTsReader.Read(
                    input,
                    bytes.LongLength,
                    source,
                    provenance,
                    limits,
                    false,
                    absoluteStart);
            }
            ShpTsParseResult window = WestwoodShpTsReader.Read(
                context.Entry.PayloadWindow,
                source,
                provenance,
                limits);
            if (!memory.IsSuccess || !stream.IsSuccess || !window.IsSuccess ||
                !DirectoryEquivalent(memory.Document, stream.Document) ||
                !DirectoryEquivalent(memory.Document, window.Document))
            {
                throw Failure(
                    ShpTsRleForensicAuditFailureCode.DirectoryParseFailed,
                    "A fixed SHP directory failed forensic input equivalence.");
            }

            return new SampleBundle(
                context,
                GetCategory(context.Specification.SampleId),
                bytes,
                memory.Document,
                stream.Document,
                window.Document);
        }

        private static ShpTsRleForensicFrameAnalysis AnalyzeEquivalent(
            SampleBundle bundle,
            int frameIndex,
            bool allRows,
            ref bool aggregateEquivalent)
        {
            ShpTsRleForensicFrameAnalysis memory = ShpTsRleForensicAnalyzer.Analyze(
                bundle.Bytes,
                bundle.MemoryDocument,
                frameIndex,
                allRows);
            ShpTsRleForensicFrameAnalysis stream;
            using (var input = new MemoryStream(bundle.Bytes, false))
            {
                stream = ShpTsRleForensicAnalyzer.Analyze(
                    input,
                    bundle.Bytes.LongLength,
                    bundle.StreamDocument,
                    frameIndex,
                    allRows,
                    leaveOpen: true);
            }
            ShpTsRleForensicFrameAnalysis window = ShpTsRleForensicAnalyzer.Analyze(
                bundle.Context.Entry.PayloadWindow,
                bundle.WindowDocument,
                frameIndex,
                allRows);
            bool equivalent = string.Equals(
                    memory.CanonicalScalar(),
                    stream.CanonicalScalar(),
                    StringComparison.Ordinal) &&
                string.Equals(
                    memory.CanonicalScalar(),
                    window.CanonicalScalar(),
                    StringComparison.Ordinal);
            aggregateEquivalent &= equivalent;
            if (!equivalent)
            {
                throw Failure(
                    ShpTsRleForensicAuditFailureCode.InputModeMismatch,
                    "Memory, Stream, and MIX-window forensic scalars differ.");
            }

            return memory;
        }

        private static void ValidateBaselineLock(
            IReadOnlyList<Candidate> candidates,
            int productionFailures,
            int productionRowZeroFailures,
            int productionOverflowFailures)
        {
            int odd = candidates.Count(value => (value.Record.Width & 1) != 0);
            int even = candidates.Count - odd;
            int minimum = candidates.Count == 0 ? -1 : candidates.Min(value => value.Record.Width);
            int maximum = candidates.Count == 0 ? -1 : candidates.Max(value => value.Record.Width);
            bool allWidthPlusOne = candidates.All(value =>
                value.Record.StageARow.MechanicalOutputLength ==
                    checked((long)value.Record.Width + 1));
            ValidateBaselineLockSnapshot(
                candidates.Count,
                productionFailures,
                productionRowZeroFailures,
                productionOverflowFailures,
                minimum,
                maximum,
                odd,
                even,
                allWidthPlusOne);
        }

        internal static void ValidateBaselineLockSnapshot(
            int candidateFrames,
            int productionFailures,
            int productionRowZeroFailures,
            int productionOverflowFailures,
            int minimumWidth,
            int maximumWidth,
            int oddWidths,
            int evenWidths,
            bool allWidthPlusOne)
        {
            if (candidateFrames != ExpectedCandidateFrames ||
                productionFailures != ExpectedCandidateFrames ||
                productionRowZeroFailures != ExpectedCandidateFrames ||
                productionOverflowFailures != ExpectedCandidateFrames ||
                minimumWidth != ExpectedMinimumWidth ||
                maximumWidth != ExpectedMaximumWidth ||
                oddWidths != ExpectedOddWidths || evenWidths != ExpectedEvenWidths ||
                !allWidthPlusOne)
            {
                throw Failure(
                    ShpTsRleForensicAuditFailureCode.BaselineProbeInputDrift,
                    "The locked 257-frame flags-3 forensic baseline changed; inference stopped.");
            }
        }

        internal static ShpTsRleForensicDecision Decide(
            IReadOnlyList<ShpTsRleForensicFrameRecord> records,
            bool stageBExecuted)
        {
            if (!stageBExecuted)
            {
                return records.Any(value =>
                    value.StageARow.ExtraSource == ShpTsRleForensicExtraSource.Literal)
                    ? ShpTsRleForensicDecision.C
                    : ShpTsRleForensicDecision.E;
            }

            ShpTsRleForensicRowScalar[] rows = records
                .Where(value => value.StageB != null)
                .SelectMany(value => value.StageB.Rows)
                .ToArray();
            if (records.Any(value => value.StageB == null || !value.StageB.IsSuccess) ||
                rows.Length == 0)
            {
                return ShpTsRleForensicDecision.B;
            }
            if (rows.Any(value => value.LiteralOverflow))
            {
                return ShpTsRleForensicDecision.C;
            }
            if (rows.All(value => value.GuardPattern))
            {
                return ShpTsRleForensicDecision.A1;
            }

            string[] categoryPatterns = records.GroupBy(value => value.Category)
                .Select(group => string.Join(",", group
                    .SelectMany(value => value.StageB.Rows)
                    .Select(value => value.MechanicalLengthClass)
                    .Distinct()
                    .OrderBy(value => value)))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            return categoryPatterns.Length > 1
                ? ShpTsRleForensicDecision.D
                : ShpTsRleForensicDecision.B;
        }

        private static byte[] Snapshot(MixVirtualEntry entry, ShpTsReadLimits limits)
        {
            if (entry.Length > limits.MaxInputBytes ||
                entry.Length > limits.MaxAllocatedBytes ||
                entry.Length > int.MaxValue)
            {
                throw Failure(
                    ShpTsRleForensicAuditFailureCode.DirectoryParseFailed,
                    "A forensic input exceeds its fixed snapshot budget.");
            }

            var bytes = new byte[checked((int)entry.Length)];
            int position = 0;
            int chunk = checked((int)Math.Max(1,
                Math.Min(limits.MaxSingleReadBytes, int.MaxValue)));
            while (position < bytes.Length)
            {
                int count = Math.Min(chunk, bytes.Length - position);
                entry.PayloadWindow.ReadExactly(
                    position,
                    bytes,
                    position,
                    count,
                    "shp-rle-forensic-input");
                position = checked(position + count);
            }
            return bytes;
        }

        private static ShpTsSourceProvenance BuildProvenance(MixVirtualEntry entry)
        {
            var chain = new List<LogicalContentPath>();
            chain.AddRange(entry.Provenance.Steps.Select(value => value.ArchivePath));
            chain.Add(entry.LogicalName);
            return new ShpTsSourceProvenance(entry.Provenance.Source.Id, chain);
        }

        private static bool DirectoryEquivalent(ShpTsDocument left, ShpTsDocument right)
        {
            return left != null && right != null &&
                left.InputLength == right.InputLength &&
                left.AbsoluteStartOffset == right.AbsoluteStartOffset &&
                string.Equals(
                    left.CanonicalDirectoryModelSha256,
                    right.CanonicalDirectoryModelSha256,
                    StringComparison.Ordinal);
        }

        private static ShpTsRleForensicCategory GetCategory(string sampleId)
        {
            switch (sampleId)
            {
                case "building-explicit-image":
                    return ShpTsRleForensicCategory.Building;
                case "infantry-explicit-image":
                    return ShpTsRleForensicCategory.Infantry;
                case "techno-animation-catalog-survey":
                    return ShpTsRleForensicCategory.Animation;
                case "map-addon-catalog-survey":
                    return ShpTsRleForensicCategory.MapAddon;
                default:
                    return ShpTsRleForensicCategory.MapAddon;
            }
        }

        private static string ComputeInputCatalogHash(
            string fingerprint,
            IEnumerable<ShpTsGoldenSampleEntryContext> entries)
        {
            var builder = new StringBuilder("RA2YR.SHP.TS.RLE.FORENSIC.CATALOG.V1\0");
            builder.Append(fingerprint).Append('\n');
            foreach (ShpTsGoldenSampleEntryContext entry in entries
                         .OrderBy(value => value.Specification.SampleId, StringComparer.Ordinal))
            {
                builder.Append(entry.Specification.SampleId).Append('|')
                    .Append(entry.Specification.ExpectedMixId).Append('|')
                    .Append(entry.Specification.ExpectedLength).Append('|')
                    .Append(entry.Specification.ExpectedSha256).Append('|')
                    .Append(entry.Specification.ExpectedDirectoryModelSha256)
                    .Append('\n');
            }
            using (SHA256 algorithm = SHA256.Create())
            {
                return BitConverter.ToString(algorithm.ComputeHash(
                        new UTF8Encoding(false, true).GetBytes(builder.ToString())))
                    .Replace("-", string.Empty)
                    .ToLowerInvariant();
            }
        }

        private static ShpTsRleForensicAuditException Failure(
            ShpTsRleForensicAuditFailureCode code,
            string message)
        {
            return new ShpTsRleForensicAuditException(code, message);
        }

        private sealed class SampleBundle
        {
            public SampleBundle(
                ShpTsGoldenSampleEntryContext context,
                ShpTsRleForensicCategory category,
                byte[] bytes,
                ShpTsDocument memoryDocument,
                ShpTsDocument streamDocument,
                ShpTsDocument windowDocument)
            {
                Context = context;
                Category = category;
                Bytes = bytes;
                MemoryDocument = memoryDocument;
                StreamDocument = streamDocument;
                WindowDocument = windowDocument;
            }

            public ShpTsGoldenSampleEntryContext Context { get; }
            public ShpTsRleForensicCategory Category { get; }
            public byte[] Bytes { get; }
            public ShpTsDocument MemoryDocument { get; }
            public ShpTsDocument StreamDocument { get; }
            public ShpTsDocument WindowDocument { get; }
        }

        private sealed class Candidate
        {
            public Candidate(
                SampleBundle bundle,
                int frameIndex,
                ShpTsRleForensicFrameRecord record)
            {
                Bundle = bundle;
                FrameIndex = frameIndex;
                Record = record;
            }

            public SampleBundle Bundle { get; }
            public int FrameIndex { get; }
            public ShpTsRleForensicFrameRecord Record { get; }
        }
    }
}
