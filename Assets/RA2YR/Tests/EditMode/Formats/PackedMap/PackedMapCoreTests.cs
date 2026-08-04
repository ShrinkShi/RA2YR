using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RA2YR.Core.Binary;
using RA2YR.Core.Content;
using RA2YR.Core.Formats.Ini;
using RA2YR.Core.Formats.PackedMap;

namespace RA2YR.Tests.EditMode.Formats.PackedMap
{
    public sealed class PackedMapCoreTests
    {
        [TestCase("AQ==", 1)] [TestCase("Ag==", 2)] [TestCase("Aw==", 3)] [TestCase("BA==", 4)]
        [TestCase("BQ==", 5)] [TestCase("Bg==", 6)] [TestCase("Bw==", 7)] [TestCase("CA==", 8)]
        [TestCase("CQ==", 9)] [TestCase("Cg==", 10)] [TestCase("Cw==", 11)] [TestCase("DA==", 12)]
        [TestCase("DQ==", 13)] [TestCase("Dg==", 14)] [TestCase("Dw==", 15)] [TestCase("EA==", 16)]
        [TestCase("EQ==", 17)] [TestCase("Eg==", 18)] [TestCase("Ew==", 19)] [TestCase("FA==", 20)]
        [TestCase("FQ==", 21)] [TestCase("Fg==", 22)] [TestCase("Fw==", 23)] [TestCase("GA==", 24)]
        [TestCase("GQ==", 25)] [TestCase("Gg==", 26)] [TestCase("Gw==", 27)] [TestCase("HA==", 28)]
        [TestCase("HQ==", 29)] [TestCase("Hg==", 30)] [TestCase("Hw==", 31)] [TestCase("IA==", 32)]
        [TestCase("IQ==", 33)] [TestCase("Ig==", 34)] [TestCase("Iw==", 35)] [TestCase("JA==", 36)]
        [TestCase("JQ==", 37)] [TestCase("Jg==", 38)] [TestCase("Jw==", 39)] [TestCase("KA==", 40)]
        [TestCase("KQ==", 41)] [TestCase("Kg==", 42)] [TestCase("Kw==", 43)] [TestCase("LA==", 44)]
        [TestCase("LQ==", 45)] [TestCase("Lg==", 46)] [TestCase("Lw==", 47)] [TestCase("MA==", 48)]
        [TestCase("MQ==", 49)] [TestCase("Mg==", 50)] [TestCase("Mw==", 51)] [TestCase("NA==", 52)]
        [TestCase("NQ==", 53)] [TestCase("Ng==", 54)] [TestCase("Nw==", 55)] [TestCase("OA==", 56)]
        [TestCase("OQ==", 57)] [TestCase("Og==", 58)] [TestCase("Ow==", 59)] [TestCase("PA==", 60)]
        [TestCase("PQ==", 61)] [TestCase("Pg==", 62)] [TestCase("Pw==", 63)] [TestCase("QA==", 64)]
        [TestCase("QQ==", 65)] [TestCase("Qg==", 66)] [TestCase("Qw==", 67)] [TestCase("RA==", 68)]
        [TestCase("RQ==", 69)] [TestCase("Rg==", 70)] [TestCase("Rw==", 71)] [TestCase("SA==", 72)]
        [TestCase("SQ==", 73)] [TestCase("Sg==", 74)] [TestCase("Sw==", 75)] [TestCase("TA==", 76)]
        [TestCase("TQ==", 77)] [TestCase("Tg==", 78)] [TestCase("Tw==", 79)] [TestCase("UA==", 80)]
        [TestCase("UQ==", 81)] [TestCase("Ug==", 82)] [TestCase("Uw==", 83)] [TestCase("VA==", 84)]
        [TestCase("VQ==", 85)] [TestCase("Vg==", 86)] [TestCase("Vw==", 87)] [TestCase("WA==", 88)]
        [TestCase("WQ==", 89)] [TestCase("Wg==", 90)] [TestCase("Ww==", 91)] [TestCase("XA==", 92)]
        [TestCase("XQ==", 93)] [TestCase("Xg==", 94)] [TestCase("Xw==", 95)] [TestCase("YA==", 96)]
        [TestCase("YQ==", 97)] [TestCase("Yg==", 98)] [TestCase("Yw==", 99)] [TestCase("ZA==", 100)]
        public void StrictBase64AcceptsCanonicalPaddedValues(string text, int ignored)
        {
            StrictBase64DecodeResult result = new StrictBase64Decoder().Decode(text);
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Bytes, Has.Length.EqualTo(1));
        }

        [TestCase("A===")][TestCase("====")][TestCase("A A=")][TestCase("A-A=")][TestCase("A_A=")]
        [TestCase("A")][TestCase("AA")][TestCase("AAA")][TestCase("AA=A")]
        public void StrictBase64RejectsMalformedValues(string text)
        { Assert.That(new StrictBase64Decoder().Decode(text).IsSuccess, Is.False); }

        [Test]
        public void CollectorPoliciesPreserveOccurrenceAndNumericOrder()
        {
            var occurrences = new[] { Occurrence("10", "A", 0), Occurrence("2", "B", 1), Occurrence("01", "C", 2), Occurrence("1", "D", 3) };
            PackedIniFragmentCollection source = new PackedIniFragmentCollector().Collect(occurrences, PackedIniFragmentOrderingPolicy.SourceOccurrenceOrder);
            Assert.That(source.Occurrences.Select(item => item.RawKey).ToArray(), Is.EqualTo(new[] { "10", "2", "01", "1" }));
            PackedIniFragmentCollection numeric = new PackedIniFragmentCollector().Collect(occurrences, PackedIniFragmentOrderingPolicy.NumericAscendingUnique);
            Assert.That(numeric.Occurrences.Select(item => item.RawKey).ToArray(), Is.EqualTo(new[] { "01", "1", "2", "10" }));
            Assert.That(numeric.Diagnostics.Any(item => item.Code == PackedMapDiagnosticCode.FragmentKeyCollision), Is.True);
        }

        [Test]
        public void CollectorRejectsInvalidKeysAndBudgets()
        {
            PackedIniFragmentCollection result = new PackedIniFragmentCollector().Collect(
                new[] { Occurrence("0", "", 0), Occurrence("-1", "x", 1), Occurrence("x", "y", 2), Occurrence("999999999999", "z", 3) },
                PackedIniFragmentOrderingPolicy.NumericAscendingUnique,
                new PackedIniFragmentCollectorLimits(2, 2));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Any(item => item.Code == PackedMapDiagnosticCode.FragmentBudgetExceeded), Is.True);
        }

        [Test]
        public void CollectorStopsAfterBudgetProbeWithoutEnumeratingFurther()
        {
            int moves = 0;
            IEnumerable<PackedIniFragmentOccurrence> Lazy()
            {
                for (int index = 0; index < 8; index++)
                {
                    moves++;
                    if (moves > 3) throw new InvalidOperationException("enumerated beyond budget probe");
                    yield return Occurrence((index + 1).ToString(), "A", index);
                }
            }

            PackedIniFragmentCollection result = new PackedIniFragmentCollector().Collect(
                Lazy(), PackedIniFragmentOrderingPolicy.SourceOccurrenceOrder,
                new PackedIniFragmentCollectorLimits(2, 10));

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Any(item => item.Code == PackedMapDiagnosticCode.FragmentBudgetExceeded), Is.True);
            Assert.That(moves, Is.EqualTo(3));
            Assert.That(result.Occurrences, Has.Count.EqualTo(2));
        }

        [Test]
        public void ChunkReaderRetainsBlocksAndRejectsMalformedEnvelope()
        {
            byte[] input = { 3, 0, 2, 0, 1, 2, 3, 0, 1, 0, 4 };
            WestwoodChunkEnvelopeReadResult result = new WestwoodChunkEnvelopeReader().Read(input);
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Blocks, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics.Any(item => item.Code == PackedMapDiagnosticCode.ChunkPayloadTruncated), Is.True);
        }

        [Test]
        public void ChunkZeroSentinelRequiresExplicitPolicy()
        {
            byte[] input = { 0, 0, 0, 0 };
            Assert.That(new WestwoodChunkEnvelopeReader().Read(input).IsSuccess, Is.False);
            Assert.That(new WestwoodChunkEnvelopeReader().Read(input, sentinelPolicy: ChunkSentinelPolicy.AllowZeroZeroAsTerminator).IsSuccess, Is.True);
        }

        [Test]
        public void ChunkRetainsExplicitProvenanceChain()
        {
            var chain = new[]
            {
                new IniSourceProvenance("source-a", new[] { LogicalContentPath.Parse("ra2.mix"), LogicalContentPath.Parse("packed.ini") }),
                new IniSourceProvenance("source-b", new[] { LogicalContentPath.Parse("expandmd01.mix"), LogicalContentPath.Parse("packed.ini") })
            };
            WestwoodChunkEnvelopeReadResult result = new WestwoodChunkEnvelopeReader().Read(
                new byte[] { 1, 0, 1, 0, 0x80 },
                new WestwoodChunkReadLimits(),
                ChunkSentinelPolicy.RejectAllZero,
                0,
                chain);
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Blocks[0].Provenance.Select(item => item.SourceId).ToArray(), Is.EqualTo(new[] { "source-a", "source-b" }));
        }

        [Test]
        public void ChunkInputBudgetFailsBeforeMaterialization()
        {
            WestwoodChunkEnvelopeReadResult result = new WestwoodChunkEnvelopeReader().Read(
                new byte[] { 1, 0, 1, 0, 0x80 },
                new WestwoodChunkReadLimits(maxInputBytes: 4));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Any(item => item.Code == PackedMapDiagnosticCode.ChunkBudgetExceeded), Is.True);
            Assert.That(result.Blocks, Is.Empty);
        }

        [Test]
        public void Format80InputBudgetFailsBeforeDecode()
        {
            Format80DecodeResult result = new Format80Decoder().Decode(
                new byte[] { 0x81, 1, 0x80 },
                1,
                limits: new Format80ReadLimits(maxInputBytes: 2));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Any(item => item.Code == PackedMapDiagnosticCode.Format80BudgetExceeded), Is.True);
        }

        [Test]
        public void Format80DecodesLiteralAndExactTerminator()
        {
            Format80DecodeResult result = new Format80Decoder().Decode(new byte[] { 0x83, 1, 2, 3, 0x80 }, 3);
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Bytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(result.TerminatorSeen, Is.True);
        }

        [Test]
        public void Format80DecodesOverlappingShortCopy()
        {
            Format80DecodeResult result = new Format80Decoder().Decode(new byte[] { 0x81, 7, 0x00, 0x01, 0x80 }, 4);
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Bytes, Is.EqualTo(new byte[] { 7, 7, 7, 7 }));
        }

        [TestCase(0xc0)]
        [TestCase(0xfe)]
        [TestCase(0xff)]
        public void Format80RejectsTruncatedCommands(byte command)
        {
            Format80DecodeResult result = new Format80Decoder().Decode(new[] { command }, 0);
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Any(item => item.Code == PackedMapDiagnosticCode.Format80TruncatedCommand), Is.True);
        }

        [Test]
        public void Format80AcceptsTerminatorForZeroOutput()
        {
            Format80DecodeResult result = new Format80Decoder().Decode(new byte[] { 0x80 }, 0);
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.TerminatorSeen, Is.True);
        }

        [Test]
        public void Format80RejectsTrailingInputAndWrongOutput()
        {
            Assert.That(new Format80Decoder().Decode(new byte[] { 0x81, 1, 0x80, 0 }, 1).IsSuccess, Is.False);
            Assert.That(new Format80Decoder().Decode(new byte[] { 0x81, 1, 0x80 }, 2).IsSuccess, Is.False);
        }

        [Test]
        public void PipelineFailsClosedWithoutLzoBackend()
        {
            PackedSectionDecodeResult result = new PackedSectionDecodePipeline().Decode(
                new[] { Occurrence("1", "AQABACo=", 0) },
                new PackedSectionDecodePolicy(PackedIniFragmentOrderingPolicy.SourceOccurrenceOrder, StrictBase64Policy.StandardAlphabetNoWhitespace, ChunkSentinelPolicy.RejectAllZero, PackedCodecKind.RawLzo1X));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Any(item => item.Code == PackedMapDiagnosticCode.BackendUnavailable), Is.True);
        }

        [Test]
        public void FakeLzoBackendMustHonorExactLength()
        {
            var backend = new FakeBackend(new byte[] { 1, 2 });
            LzoDecodeResult result = backend.Decode(new LzoDecodeRequest(PackedCodecKind.RawLzo1X, new byte[] { 1 }, 2, 10, "test"));
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Bytes, Is.EqualTo(new byte[] { 1, 2 }));
        }

        private static PackedIniFragmentOccurrence Occurrence(string key, string value, int line)
        {
            return new PackedIniFragmentOccurrence("Packed", key, value, line, "synthetic", line, new IniSourceProvenance("synthetic", new[] { LogicalContentPath.Parse("synthetic.ini") }));
        }

        private sealed class FakeBackend : ILzoDecodeBackend
        {
            private readonly byte[] bytes;
            public FakeBackend(byte[] bytes) { this.bytes = bytes; }
            public LzoDecodeResult Decode(LzoDecodeRequest request) { return new LzoDecodeResult(bytes, request.Compressed.Length, "fake", Array.Empty<PackedMapDiagnostic>()); }
        }
    }
}
