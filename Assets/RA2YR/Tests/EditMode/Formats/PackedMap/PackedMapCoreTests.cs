using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using RA2YR.Core.Binary;
using RA2YR.Core.Content;
using RA2YR.Core.Binary.Seekable;
using RA2YR.Core.Formats.Ini;
using RA2YR.Core.Formats.PackedMap;

namespace RA2YR.Tests.EditMode.Formats.PackedMap
{
    public sealed class PackedMapCoreTests
    {
        [TestCase("AQ==", "01")]
        [TestCase("AQI=", "0102")]
        [TestCase("AQID", "010203")]
        [TestCase("", "")]
        public void StrictBase64AcceptsCanonicalValues(string text, string expectedHex)
        {
            StrictBase64DecodeResult result = new StrictBase64Decoder().Decode(text);
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Bytes.Select(item => item.ToString("X2")).ToArray(), Is.EqualTo(expectedHex.Length == 0 ? new string[0] : Enumerable.Range(0, expectedHex.Length / 2).Select(index => expectedHex.Substring(index * 2, 2)).ToArray()));
        }

        [TestCase("A===")][TestCase("====")][TestCase("A A=")][TestCase("A-A=")][TestCase("A_A=")]
        [TestCase("A")][TestCase("AA")][TestCase("AAA")][TestCase("AA=A")]
        public void StrictBase64RejectsMalformedValues(string text)
        { Assert.That(new StrictBase64Decoder().Decode(text).IsSuccess, Is.False); }

        [TestCase("AB==")]
        [TestCase("AAB=")]
        public void StrictBase64RejectsNonCanonicalPadBits(string text)
        { Assert.That(new StrictBase64Decoder().Decode(text).Diagnostics.Any(item => item.Code == PackedMapDiagnosticCode.InvalidBase64Padding), Is.True); }

        [Test]
        public void StrictBase64AcceptsNoPaddingQuantum()
        {
            StrictBase64DecodeResult result = new StrictBase64Decoder().Decode("AQID");
            Assert.That(result.Bytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
        }

        [Test]
        public void StrictBase64AcceptsOnePaddingQuantum()
        {
            StrictBase64DecodeResult result = new StrictBase64Decoder().Decode("AQI=");
            Assert.That(result.Bytes, Is.EqualTo(new byte[] { 1, 2 }));
        }

        [Test]
        public void StrictBase64AcceptsTwoPaddingQuantum()
        {
            StrictBase64DecodeResult result = new StrictBase64Decoder().Decode("AQ==");
            Assert.That(result.Bytes, Is.EqualTo(new byte[] { 1 }));
        }

        [Test]
        public void StrictBase64RejectsEmbeddedPadding()
        {
            StrictBase64DecodeResult result = new StrictBase64Decoder().Decode("AQ=I");
            Assert.That(HasCode(result, PackedMapDiagnosticCode.InvalidBase64Padding), Is.True);
            Assert.That(result.Bytes, Is.Null);
        }

        [Test]
        public void StrictBase64RejectsPaddingAfterData()
        {
            StrictBase64DecodeResult result = new StrictBase64Decoder().Decode("AQ==AAAA");
            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public void StrictBase64RejectsCarriageReturn()
        {
            StrictBase64DecodeResult result = new StrictBase64Decoder().Decode("AQ==\r");
            Assert.That(HasCode(result, PackedMapDiagnosticCode.InvalidBase64Length), Is.True);
        }

        [Test]
        public void StrictBase64RejectsOutputBudgetBeforeDecode()
        {
            StrictBase64DecodeResult result = new StrictBase64Decoder().Decode("AQID", new StrictBase64ReadLimits(2));
            Assert.That(HasCode(result, PackedMapDiagnosticCode.Base64OutputBudgetExceeded), Is.True);
            Assert.That(result.Bytes, Is.Null);
        }

        [Test]
        public void StrictBase64FailureNeverReturnsPartialBytes()
        {
            StrictBase64DecodeResult result = new StrictBase64Decoder().Decode("AQID$");
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Bytes, Is.Null);
        }

        [Test]
        public void SourceOccurrenceOrderPreservesRawKeysWithoutNumericDiagnostics()
        {
            PackedIniFragmentCollection result = new PackedIniFragmentCollector().Collect(
                new[] { Occurrence("2", "A", 0), Occurrence("x", "B", 1) },
                PackedIniFragmentOrderingPolicy.SourceOccurrenceOrder);
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Diagnostics.Any(item => item.Code == PackedMapDiagnosticCode.NonnumericFragmentKey), Is.False);
        }

        [Test]
        public void StrictSequentialPolicyRejectsGapsAndMissingOne()
        {
            PackedIniFragmentCollection result = new PackedIniFragmentCollector().Collect(
                new[] { Occurrence("2", "A", 0), Occurrence("4", "B", 1) },
                PackedIniFragmentOrderingPolicy.StrictSequentialFromOne);
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Any(item => item.Code == PackedMapDiagnosticCode.FragmentKeyGap), Is.True);
            Assert.That(result.Diagnostics.Any(item => item.Code == PackedMapDiagnosticCode.MissingFragmentKeyOne), Is.True);
        }

        [Test]
        public void CollectorPreservesRawValueWhitespaceAndDuplicateSectionOccurrences()
        {
            PackedIniFragmentOccurrence first = Occurrence("1", " A ", 4);
            PackedIniFragmentOccurrence duplicate = Occurrence("1", " B ", 4);
            PackedIniFragmentCollection result = new PackedIniFragmentCollector().Collect(
                new[] { first, duplicate }, PackedIniFragmentOrderingPolicy.SourceOccurrenceOrder);
            Assert.That(result.Occurrences.Select(item => item.RawValue).ToArray(), Is.EqualTo(new[] { " A ", " B " }));
            Assert.That(result.Diagnostics.Any(item => item.Code == PackedMapDiagnosticCode.DuplicateSourceOccurrence), Is.True);
            Assert.That(result.Occurrences[0].Provenance.SourceId, Is.EqualTo("synthetic"));
            Assert.That(result.Occurrences[0].PhysicalLineId, Is.EqualTo(4));
        }

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
        public void CollectorNumericOrderReportsNonnumericKey()
        {
            PackedIniFragmentCollection result = new PackedIniFragmentCollector().Collect(
                new[] { Occurrence("1", "A", 0), Occurrence("x", "B", 1) },
                PackedIniFragmentOrderingPolicy.NumericAscendingUnique);
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(HasCode(result, PackedMapDiagnosticCode.NonnumericFragmentKey), Is.True);
        }

        [Test]
        public void CollectorReportsZeroKey()
        {
            PackedIniFragmentCollection result = new PackedIniFragmentCollector().Collect(
                new[] { Occurrence("0", "A", 0) }, PackedIniFragmentOrderingPolicy.NumericAscendingUnique);
            Assert.That(HasCode(result, PackedMapDiagnosticCode.FragmentKeyZero), Is.True);
        }

        [Test]
        public void CollectorReportsNegativeKey()
        {
            PackedIniFragmentCollection result = new PackedIniFragmentCollector().Collect(
                new[] { Occurrence("-1", "A", 0) }, PackedIniFragmentOrderingPolicy.NumericAscendingUnique);
            Assert.That(HasCode(result, PackedMapDiagnosticCode.NegativeFragmentKey), Is.True);
        }

        [Test]
        public void CollectorReportsOverflowKey()
        {
            PackedIniFragmentCollection result = new PackedIniFragmentCollector().Collect(
                new[] { Occurrence("999999999999999999999", "A", 0) }, PackedIniFragmentOrderingPolicy.NumericAscendingUnique);
            Assert.That(HasCode(result, PackedMapDiagnosticCode.FragmentKeyOverflow), Is.True);
        }

        [Test]
        public void CollectorReportsDuplicateNumericKey()
        {
            PackedIniFragmentCollection result = new PackedIniFragmentCollector().Collect(
                new[] { Occurrence("1", "A", 0), Occurrence("1", "B", 1) }, PackedIniFragmentOrderingPolicy.NumericAscendingUnique);
            Assert.That(HasCode(result, PackedMapDiagnosticCode.DuplicateNumericFragmentKey), Is.True);
        }

        [Test]
        public void CollectorReportsNumericGapWithoutChangingOrder()
        {
            PackedIniFragmentCollection result = new PackedIniFragmentCollector().Collect(
                new[] { Occurrence("1", "A", 0), Occurrence("3", "B", 1) }, PackedIniFragmentOrderingPolicy.NumericAscendingUnique);
            Assert.That(HasCode(result, PackedMapDiagnosticCode.FragmentKeyGap), Is.True);
            Assert.That(result.Occurrences.Select(item => item.RawKey).ToArray(), Is.EqualTo(new[] { "1", "3" }));
        }

        [Test]
        public void CollectorReportsMissingKeyOneAsWarning()
        {
            PackedIniFragmentCollection result = new PackedIniFragmentCollector().Collect(
                new[] { Occurrence("2", "A", 0) }, PackedIniFragmentOrderingPolicy.NumericAscendingUnique);
            Assert.That(HasCode(result, PackedMapDiagnosticCode.MissingFragmentKeyOne), Is.True);
            Assert.That(result.IsSuccess, Is.True);
        }

        [Test]
        public void CollectorPreservesEmptyFragmentWithWarning()
        {
            PackedIniFragmentCollection result = new PackedIniFragmentCollector().Collect(
                new[] { Occurrence("1", string.Empty, 0) }, PackedIniFragmentOrderingPolicy.SourceOccurrenceOrder);
            Assert.That(result.Occurrences[0].RawValue, Is.EqualTo(string.Empty));
            Assert.That(HasCode(result, PackedMapDiagnosticCode.EmptyFragmentValue), Is.True);
            Assert.That(result.IsSuccess, Is.True);
        }

        [Test]
        public void CollectorStopsAtCharacterBudgetWithoutDroppingPriorOccurrence()
        {
            PackedIniFragmentCollection result = new PackedIniFragmentCollector().Collect(
                new[] { Occurrence("1", "AB", 0), Occurrence("2", "CD", 1) },
                PackedIniFragmentOrderingPolicy.SourceOccurrenceOrder,
                new PackedIniFragmentCollectorLimits(8, 3));
            Assert.That(HasCode(result, PackedMapDiagnosticCode.AggregateCharacterBudgetExceeded), Is.True);
            Assert.That(result.Occurrences, Has.Count.EqualTo(1));
            Assert.That(result.Occurrences[0].RawValue, Is.EqualTo("AB"));
        }

        [Test]
        public void CollectorDuplicateSourceOccurrenceUsesSourceAndPhysicalLineIdentity()
        {
            PackedIniFragmentOccurrence first = Occurrence("1", "A", 7);
            PackedIniFragmentOccurrence same = Occurrence("2", "B", 7);
            PackedIniFragmentCollection result = new PackedIniFragmentCollector().Collect(
                new[] { first, same }, PackedIniFragmentOrderingPolicy.SourceOccurrenceOrder);
            Assert.That(HasCode(result, PackedMapDiagnosticCode.DuplicateSourceOccurrence), Is.True);
            Assert.That(result.Diagnostics[0].SourceId, Is.EqualTo("synthetic"));
            Assert.That(result.Diagnostics[0].Offset, Is.EqualTo(7));
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

        [TestCase(new byte[] { 0, 0, 1, 0 })]
        [TestCase(new byte[] { 1, 0, 0, 0 })]
        public void ChunkRejectsSingleZeroFields(byte[] input)
        {
            WestwoodChunkEnvelopeReadResult result = new WestwoodChunkEnvelopeReader().Read(input);
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Any(item => item.Code == PackedMapDiagnosticCode.ChunkZeroFieldInvalid), Is.True);
            Assert.That(result.Blocks, Is.Empty);
        }

        [Test]
        public void ChunkSentinelRejectsTrailingBytes()
        {
            WestwoodChunkEnvelopeReadResult result = new WestwoodChunkEnvelopeReader().Read(
                new byte[] { 0, 0, 0, 0, 1 },
                sentinelPolicy: ChunkSentinelPolicy.AllowZeroZeroAsTerminator);
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Any(item => item.Code == PackedMapDiagnosticCode.ChunkTrailingBytes), Is.True);
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
        public void ChunkReaderAcceptsOneBlockAndPreservesPayload()
        {
            WestwoodChunkEnvelopeReadResult result = new WestwoodChunkEnvelopeReader().Read(new byte[] { 1, 0, 1, 0, 0x7f });
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Blocks, Has.Count.EqualTo(1));
            Assert.That(result.Blocks[0].SourceOffset, Is.EqualTo(0));
            Assert.That(result.Blocks[0].Payload, Is.EqualTo(new byte[] { 0x7f }));
        }

        [Test]
        public void ChunkReaderAcceptsMultipleBlocksInSourceOrder()
        {
            WestwoodChunkEnvelopeReadResult result = new WestwoodChunkEnvelopeReader().Read(
                new byte[] { 1, 0, 1, 0, 0x01, 2, 0, 2, 0, 0x02, 0x03 });
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Blocks.Select(item => item.Ordinal).ToArray(), Is.EqualTo(new[] { 0, 1 }));
            Assert.That(result.Blocks.Select(item => item.SourceOffset).ToArray(), Is.EqualTo(new long[] { 0, 5 }));
        }

        [Test]
        public void ChunkReaderEnforcesBlockBudget()
        {
            WestwoodChunkEnvelopeReadResult result = new WestwoodChunkEnvelopeReader().Read(
                new byte[] { 1, 0, 1, 0, 0x01, 1, 0, 1, 0, 0x02 },
                new WestwoodChunkReadLimits(maxBlocks: 1));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(HasCode(result, PackedMapDiagnosticCode.ChunkBudgetExceeded), Is.True);
            Assert.That(result.Blocks, Has.Count.EqualTo(1));
        }

        [Test]
        public void ChunkReaderEnforcesCompressedBudget()
        {
            WestwoodChunkEnvelopeReadResult result = new WestwoodChunkEnvelopeReader().Read(
                new byte[] { 2, 0, 1, 0, 0x01, 0x02 },
                new WestwoodChunkReadLimits(maxCompressedBytes: 1));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(HasCode(result, PackedMapDiagnosticCode.ChunkBudgetExceeded), Is.True);
        }

        [Test]
        public void ChunkReaderEnforcesDeclaredOutputBudget()
        {
            WestwoodChunkEnvelopeReadResult result = new WestwoodChunkEnvelopeReader().Read(
                new byte[] { 1, 0, 2, 0, 0x01 },
                new WestwoodChunkReadLimits(maxOutputBytes: 1));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(HasCode(result, PackedMapDiagnosticCode.ChunkOutputBudgetExceeded), Is.True);
        }

        [Test]
        public void ChunkReaderReportsAbsoluteOffset()
        {
            WestwoodChunkEnvelopeReadResult result = new WestwoodChunkEnvelopeReader().Read(
                new byte[] { 1, 0, 1, 0, 0x01 }, new WestwoodChunkReadLimits(), ChunkSentinelPolicy.RejectAllZero, 42);
            Assert.That(result.Blocks[0].SourceOffset, Is.EqualTo(42));
        }

        [Test]
        public void ChunkMemoryAndStreamPathsHaveEquivalentBlocks()
        {
            byte[] input = { 1, 0, 1, 0, 0x01, 1, 0, 1, 0, 0x02 };
            WestwoodChunkEnvelopeReadResult memory = new WestwoodChunkEnvelopeReader().Read(input);
            using (var stream = new MemoryStream(input))
            {
                WestwoodChunkEnvelopeReadResult streamed = new WestwoodChunkEnvelopeReader().Read(
                    stream, input.Length, SourceContext());
                Assert.That(streamed.IsSuccess, Is.EqualTo(memory.IsSuccess));
                Assert.That(streamed.Blocks.Select(item => item.Payload).ToArray(), Is.EqualTo(memory.Blocks.Select(item => item.Payload).ToArray()));
            }
        }

        [Test]
        public void ChunkWindowPathUsesTheSameBoundedStateMachine()
        {
            byte[] input = { 1, 0, 1, 0, 0x01 };
            using (var stream = new MemoryStream(input))
            using (ReadOnlyDataWindowSession session = ReadOnlyDataWindowSession.FromSeekableStream(stream, SourceContext(), 0, input.Length, leaveOpen: true))
            {
                WestwoodChunkEnvelopeReadResult result = new WestwoodChunkEnvelopeReader().Read(session.Root);
                Assert.That(result.IsSuccess, Is.True);
                Assert.That(result.Blocks[0].Payload, Is.EqualTo(new byte[] { 0x01 }));
            }
        }

        [Test]
        public void ChunkReaderRejectsTrailingHeaderFragment()
        {
            WestwoodChunkEnvelopeReadResult result = new WestwoodChunkEnvelopeReader().Read(new byte[] { 1, 0, 1, 0, 0x01, 0x7f });
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(HasCode(result, PackedMapDiagnosticCode.ChunkHeaderTruncated), Is.True);
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
        public void Format80DecodesMediumAbsoluteCopy()
        {
            Format80DecodeResult result = new Format80Decoder().Decode(new byte[] { 0x83, 1, 2, 3, 0xc0, 0, 0, 0x80 }, 6);
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Bytes, Is.EqualTo(new byte[] { 1, 2, 3, 1, 2, 3 }));
        }

        [Test]
        public void Format80DecodesMediumRelativeCopy()
        {
            Format80DecodeResult result = new Format80Decoder().Decode(new byte[] { 0x83, 1, 2, 3, 0xc0, 3, 0, 0x80 }, 6, new Format80Profile(Format80Variant.Relative));
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Bytes, Is.EqualTo(new byte[] { 1, 2, 3, 1, 2, 3 }));
        }

        [Test]
        public void Format80DecodesFill()
        {
            Format80DecodeResult result = new Format80Decoder().Decode(new byte[] { 0xfe, 3, 0, 0x7a, 0x80 }, 3);
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Bytes, Is.EqualTo(new byte[] { 0x7a, 0x7a, 0x7a }));
        }

        [Test]
        public void Format80DecodesLongAbsoluteCopy()
        {
            Format80DecodeResult result = new Format80Decoder().Decode(new byte[] { 0x83, 1, 2, 3, 0xff, 3, 0, 0, 0, 0x80 }, 6);
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Bytes, Is.EqualTo(new byte[] { 1, 2, 3, 1, 2, 3 }));
        }

        [Test]
        public void Format80DecodesLongRelativeCopy()
        {
            Format80DecodeResult result = new Format80Decoder().Decode(new byte[] { 0x83, 1, 2, 3, 0xff, 3, 0, 3, 0, 0x80 }, 6, new Format80Profile(Format80Variant.Relative));
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Bytes, Is.EqualTo(new byte[] { 1, 2, 3, 1, 2, 3 }));
        }

        [Test]
        public void Format80DecodesMediumOverlap()
        {
            Format80DecodeResult result = new Format80Decoder().Decode(new byte[] { 0x81, 7, 0xc0, 1, 0, 0x80 }, 4, new Format80Profile(Format80Variant.Relative));
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Bytes, Is.EqualTo(new byte[] { 7, 7, 7, 7 }));
        }

        [Test]
        public void Format80DecodesLongOverlap()
        {
            Format80DecodeResult result = new Format80Decoder().Decode(new byte[] { 0x81, 7, 0xff, 3, 0, 1, 0, 0x80 }, 4, new Format80Profile(Format80Variant.Relative));
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Bytes, Is.EqualTo(new byte[] { 7, 7, 7, 7 }));
        }

        [Test]
        public void Format80HonorsInitialMarkerProfile()
        {
            Format80DecodeResult result = new Format80Decoder().Decode(new byte[] { 0, 0x81, 1, 0x80 }, 1, new Format80Profile(allowInitialMarker: true));
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Bytes, Is.EqualTo(new byte[] { 1 }));
        }

        [Test]
        public void Format80RejectsInitialMarkerWhenDisabled()
        {
            Format80DecodeResult result = new Format80Decoder().Decode(new byte[] { 0, 0x81, 1, 0x80 }, 1);
            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public void Format80OptionalTerminatorAllowsExactInputWithoutMarker()
        {
            Format80DecodeResult result = new Format80Decoder().Decode(new byte[] { 0x81, 1 }, 1, new Format80Profile(requireTerminator: false));
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.BytesConsumed, Is.EqualTo(2));
        }

        [Test]
        public void Format80AllowsTrailingBytesOnlyWhenProfilePermits()
        {
            Format80DecodeResult result = new Format80Decoder().Decode(new byte[] { 0x81, 1, 0x80, 0x55 }, 1, new Format80Profile(allowTrailingAfterTerminator: true));
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.BytesConsumed, Is.EqualTo(3));
        }

        [Test]
        public void Format80CanExplicitlyAcceptZeroFill()
        {
            Format80DecodeResult result = new Format80Decoder().Decode(new byte[] { 0xfe, 0, 0, 7, 0x80 }, 0, new Format80Profile(rejectZeroFill: false));
            Assert.That(result.IsSuccess, Is.True);
        }

        [Test]
        public void Format80RejectsZeroLongCopyAsNoProgress()
        {
            Format80DecodeResult result = new Format80Decoder().Decode(new byte[] { 0xff, 0, 0, 0, 0, 0x80 }, 0);
            Assert.That(HasCode(result, PackedMapDiagnosticCode.Format80NoProgress), Is.True);
        }

        [Test]
        public void Format80RejectsTruncatedLiteral()
        {
            Format80DecodeResult result = new Format80Decoder().Decode(new byte[] { 0x83, 1 }, 1);
            Assert.That(HasCode(result, PackedMapDiagnosticCode.Format80TruncatedLiteral), Is.True);
        }

        [Test]
        public void Format80DistinguishesOutputUnderflow()
        {
            Format80DecodeResult result = new Format80Decoder().Decode(new byte[] { 0x81, 1, 0x80 }, 2);
            Assert.That(HasCode(result, PackedMapDiagnosticCode.Format80OutputUnderflow), Is.True);
        }

        [Test]
        public void Format80DistinguishesOutputOverflow()
        {
            Format80DecodeResult result = new Format80Decoder().Decode(new byte[] { 0x81, 1, 0x80 }, 0);
            Assert.That(HasCode(result, PackedMapDiagnosticCode.Format80OutputOverflow), Is.True);
        }

        [Test]
        public void Format80EnforcesCommandBudget()
        {
            Format80DecodeResult result = new Format80Decoder().Decode(new byte[] { 0x81, 1, 0x80 }, 1, limits: new Format80ReadLimits(maxCommands: 1));
            Assert.That(HasCode(result, PackedMapDiagnosticCode.Format80BudgetExceeded), Is.True);
        }

        [Test]
        public void Format80RejectsReferenceBeforeOutput()
        {
            Format80DecodeResult result = new Format80Decoder().Decode(new byte[] { 0xff, 1, 0, 0, 0, 0x80 }, 1);
            Assert.That(HasCode(result, PackedMapDiagnosticCode.Format80ReferenceBeforeOutput), Is.True);
        }

        [Test]
        public void Format80RejectsReservedCommand()
        {
            Format80DecodeResult result = new Format80Decoder().Decode(new byte[] { 0xc0, 0, 0, 0x80 }, 0);
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(HasCode(result, PackedMapDiagnosticCode.Format80ReferenceBeforeOutput), Is.True);
        }

        [Test]
        public void Format80MemoryAndWindowPathsAreEquivalent()
        {
            byte[] input = { 0x83, 1, 2, 3, 0x80 };
            Format80DecodeResult memory = new Format80Decoder().Decode(input, 3);
            using (var stream = new MemoryStream(input))
            using (ReadOnlyDataWindowSession session = ReadOnlyDataWindowSession.FromSeekableStream(stream, SourceContext(), 0, input.Length, leaveOpen: true))
            {
                Format80DecodeResult window = new Format80Decoder().Decode(session.Root, 3);
                Assert.That(window.IsSuccess, Is.EqualTo(memory.IsSuccess));
                Assert.That(window.Bytes, Is.EqualTo(memory.Bytes));
                Assert.That(window.BytesConsumed, Is.EqualTo(memory.BytesConsumed));
            }
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
        public void PipelineRejectsUnknownCodecBeforeCollectingInput()
        {
            PackedSectionDecodeResult result = new PackedSectionDecodePipeline().Decode(
                new[] { Occurrence("1", "not-read", 0) },
                new PackedSectionDecodePolicy(
                    PackedIniFragmentOrderingPolicy.SourceOccurrenceOrder,
                    StrictBase64Policy.StandardAlphabetNoWhitespace,
                    ChunkSentinelPolicy.RejectAllZero,
                    (PackedCodecKind)99));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(HasCode(result, PackedMapDiagnosticCode.BackendInvalidCodec), Is.True);
            Assert.That(result.Fragments, Is.Null);
        }

        [Test]
        public void PipelineRejectsLzoConsumedInputMismatch()
        {
            PackedSectionDecodeResult result = DecodeWithBackend(new FakeBackend(new byte[] { 1 }, consumed: 0, identity: "fake"));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Any(item => item.Code == PackedMapDiagnosticCode.BackendConsumedInputMismatch), Is.True);
        }

        [Test]
        public void PipelineRejectsLzoMissingIdentity()
        {
            PackedSectionDecodeResult result = DecodeWithBackend(new FakeBackend(new byte[] { 1 }, consumed: 1, identity: ""));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Any(item => item.Code == PackedMapDiagnosticCode.BackendIdentityMissing), Is.True);
        }

        [Test]
        public void PipelineRejectsLzoProvenanceMismatch()
        {
            PackedSectionDecodeResult result = DecodeWithBackend(new FakeBackend(new byte[] { 1 }, consumed: 1, identity: "fake", provenance: new[] { new IniSourceProvenance("other", new[] { LogicalContentPath.Parse("other.ini") }) }));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Any(item => item.Code == PackedMapDiagnosticCode.BackendProvenanceMismatch), Is.True);
        }

        [Test]
        public void PipelineConvertsBackendExceptionToDiagnostic()
        {
            PackedSectionDecodeResult result = DecodeWithBackend(new ThrowingBackend());
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Any(item => item.Code == PackedMapDiagnosticCode.BackendException), Is.True);
        }

        [Test]
        public void PipelineConvertsBackendCancellationToDiagnostic()
        {
            PackedSectionDecodeResult result = DecodeWithBackend(new CancellingBackend());
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Any(item => item.Code == PackedMapDiagnosticCode.BackendCancelled), Is.True);
        }

        [Test]
        public void PipelineAcceptsContractValidFakeLzoSuccess()
        {
            PackedSectionDecodeResult result = DecodeWithBackend(new FakeBackend(new byte[] { 1 }, consumed: 1, identity: "fake"));
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.DecodedBytes, Is.EqualTo(new byte[] { 1 }));
        }

        [Test]
        public void PipelineRejectsBackendErrorDiagnosticEvenWhenOutputLooksValid()
        {
            PackedSectionDecodeResult result = DecodeWithBackend(new FakeBackend(new byte[] { 1 }, consumed: 1, identity: "fake", diagnostics: new[] { new PackedMapDiagnostic(PackedMapDiagnosticCode.BackendFailure, BinaryDiagnosticSeverity.Error, "synthetic") }));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Any(item => item.Code == PackedMapDiagnosticCode.BackendDiagnosticError), Is.True);
        }

        [Test]
        public void PipelineRejectsNullBackendResult()
        {
            PackedSectionDecodeResult result = DecodeWithBackend(new NullResultBackend());
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Any(item => item.Code == PackedMapDiagnosticCode.BackendFailure), Is.True);
        }

        [Test]
        public void PipelineRejectsNullBackendOutput()
        {
            PackedSectionDecodeResult result = DecodeWithBackend(new FakeBackend(null, consumed: 1, identity: "fake"));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Any(item => item.Code == PackedMapDiagnosticCode.BackendNullOutput), Is.True);
        }

        [Test]
        public void PipelineRejectsLongConsumedInput()
        {
            PackedSectionDecodeResult result = DecodeWithBackend(new FakeBackend(new byte[] { 1 }, consumed: 2, identity: "fake"));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Any(item => item.Code == PackedMapDiagnosticCode.BackendConsumedInputMismatch), Is.True);
        }

        [Test]
        public void PipelineRejectsWhitespaceOnlyBackendIdentity()
        {
            PackedSectionDecodeResult result = DecodeWithBackend(new FakeBackend(new byte[] { 1 }, consumed: 1, identity: "   "));
            Assert.That(HasCode(result, PackedMapDiagnosticCode.BackendIdentityMissing), Is.True);
        }

        [Test]
        public void PipelineRejectsBackendOutputLengthMismatch()
        {
            PackedSectionDecodeResult result = DecodeWithBackend(new FakeBackend(new byte[] { 1, 2 }, consumed: 1, identity: "fake"));
            Assert.That(HasCode(result, PackedMapDiagnosticCode.BackendLengthMismatch), Is.True);
        }

        [Test]
        public void PipelineRejectsMissingBackendProvenance()
        {
            PackedSectionDecodeResult result = DecodeWithBackend(new FakeBackend(new byte[] { 1 }, consumed: 1, identity: "fake", provenance: Array.Empty<IniSourceProvenance>()));
            Assert.That(HasCode(result, PackedMapDiagnosticCode.BackendProvenanceMissing), Is.True);
        }

        [Test]
        public void PipelinePreservesBackendWarningDiagnostics()
        {
            PackedSectionDecodeResult result = DecodeWithBackend(new FakeBackend(
                new byte[] { 1 }, consumed: 1, identity: "fake",
                diagnostics: new[] { new PackedMapDiagnostic(PackedMapDiagnosticCode.BackendFailure, BinaryDiagnosticSeverity.Warning, "synthetic warning") }));
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(HasCode(result, PackedMapDiagnosticCode.BackendFailure), Is.True);
        }

        [Test]
        public void LzoRequestRejectsExpectedOutputBeyondBudget()
        {
            Assert.That(() => new LzoDecodeRequest(PackedCodecKind.RawLzo1X, new byte[] { 1 }, 2, 1, "synthetic"), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void PipelinePassesCancellationTokenToBackend()
        {
            using (var cancellation = new CancellationTokenSource())
            {
                var backend = new CapturingBackend();
                PackedSectionDecodeResult result = DecodeWithBackend(backend, cancellation.Token);
                Assert.That(result.IsSuccess, Is.True);
                Assert.That(backend.Token, Is.EqualTo(cancellation.Token));
            }
        }

        [Test]
        public void PipelineAggregatesMultipleLzoBlocksInOrder()
        {
            byte[] packed = { 1, 0, 1, 0, 0x01, 1, 0, 1, 0, 0x02 };
            PackedSectionDecodeResult result = DecodePackedWithBackend(Convert.ToBase64String(packed), new SequenceBackend(new[] { new byte[] { 0x11 }, new byte[] { 0x22 } }));
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.DecodedBytes, Is.EqualTo(new byte[] { 0x11, 0x22 }));
        }

        [Test]
        public void PipelineStopsOnSecondBlockFailureWithoutPartialSuccess()
        {
            byte[] packed = { 1, 0, 1, 0, 0x01, 1, 0, 1, 0, 0x02 };
            PackedSectionDecodeResult result = DecodePackedWithBackend(Convert.ToBase64String(packed), new SequenceBackend(new[] { new byte[] { 0x11 }, null }));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.DecodedBytes, Is.Null);
            Assert.That(HasCode(result, PackedMapDiagnosticCode.BackendNullOutput), Is.True);
        }

        [Test]
        public void PipelineRejectsBackendInputBudgetBeforeInvocation()
        {
            PackedSectionDecodeResult result = new PackedSectionDecodePipeline().Decode(
                new[] { Occurrence("1", "AQABACo=", 0) },
                new PackedSectionDecodePolicy(
                    PackedIniFragmentOrderingPolicy.SourceOccurrenceOrder,
                    StrictBase64Policy.StandardAlphabetNoWhitespace,
                    ChunkSentinelPolicy.RejectAllZero,
                    PackedCodecKind.RawLzo1X,
                    chunkLimits: new WestwoodChunkReadLimits(maxInputBytes: 4)),
                new FakeBackend(new byte[] { 1 }));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(HasCode(result, PackedMapDiagnosticCode.ChunkBudgetExceeded), Is.True);
        }

        [Test]
        public void PipelineRejectsBackendOutputBudgetMismatch()
        {
            PackedSectionDecodeResult result = DecodeWithBackend(
                new FakeBackend(new byte[] { 1 }, consumed: 1, identity: "fake"),
                CancellationToken.None,
                new WestwoodChunkReadLimits(maxOutputBytes: 0));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(HasCode(result, PackedMapDiagnosticCode.ChunkOutputBudgetExceeded), Is.True);
        }

        [Test]
        public void PipelineRejectsPreCancelledPolicy()
        {
            using (var cancellation = new CancellationTokenSource())
            {
                cancellation.Cancel();
                PackedSectionDecodeResult result = DecodeWithBackend(new FakeBackend(new byte[] { 1 }, consumed: 1, identity: "fake"), cancellation.Token);
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Diagnostics.Any(item => item.Code == PackedMapDiagnosticCode.BackendCancelled), Is.True);
            }
        }

        [Test]
        public void LzoRequestRejectsNonLzoCodec()
        {
            Assert.That(() => new LzoDecodeRequest(PackedCodecKind.Format80, new byte[] { 1 }, 1, 1, "synthetic"), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void LzoRequestRejectsInputBeyondBudget()
        {
            Assert.That(() => new LzoDecodeRequest(PackedCodecKind.RawLzo1X, new byte[] { 1, 2 }, 1, 1, 1, "synthetic", Array.Empty<IniSourceProvenance>()), Throws.TypeOf<ArgumentOutOfRangeException>());
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

        private static BinarySourceContext SourceContext()
        {
            return new BinarySourceContext("packed-map-tests", "synthetic", LogicalContentPath.Parse("synthetic-packed.bin"));
        }

        private static PackedSectionDecodeResult DecodePackedWithBackend(string base64, ILzoDecodeBackend backend)
        {
            return new PackedSectionDecodePipeline().Decode(
                new[] { Occurrence("1", base64, 0) },
                new PackedSectionDecodePolicy(PackedIniFragmentOrderingPolicy.SourceOccurrenceOrder, StrictBase64Policy.StandardAlphabetNoWhitespace, ChunkSentinelPolicy.RejectAllZero, PackedCodecKind.RawLzo1X),
                backend);
        }

        private static bool HasCode(PackedIniFragmentCollection result, PackedMapDiagnosticCode code)
        {
            return result.Diagnostics.Any(item => item.Code == code);
        }

        private static bool HasCode(WestwoodChunkEnvelopeReadResult result, PackedMapDiagnosticCode code)
        {
            return result.Diagnostics.Any(item => item.Code == code);
        }

        private static bool HasCode(Format80DecodeResult result, PackedMapDiagnosticCode code)
        {
            return result.Diagnostics.Any(item => item.Code == code);
        }

        private static bool HasCode(PackedSectionDecodeResult result, PackedMapDiagnosticCode code)
        {
            return result.Diagnostics.Any(item => item.Code == code);
        }

        private static bool HasCode(StrictBase64DecodeResult result, PackedMapDiagnosticCode code)
        {
            return result.Diagnostics.Any(item => item.Code == code);
        }

        private static PackedSectionDecodeResult DecodeWithBackend(ILzoDecodeBackend backend, CancellationToken cancellationToken = default(CancellationToken), WestwoodChunkReadLimits chunkLimits = null)
        {
            return new PackedSectionDecodePipeline().Decode(
                new[] { Occurrence("1", "AQABACo=", 0) },
                new PackedSectionDecodePolicy(PackedIniFragmentOrderingPolicy.SourceOccurrenceOrder, StrictBase64Policy.StandardAlphabetNoWhitespace, ChunkSentinelPolicy.RejectAllZero, PackedCodecKind.RawLzo1X, chunkLimits: chunkLimits, cancellationToken: cancellationToken),
                backend);
        }

        private sealed class FakeBackend : ILzoDecodeBackend
        {
            private readonly byte[] bytes;
            private readonly int consumed;
            private readonly string identity;
            private readonly IReadOnlyList<IniSourceProvenance> provenance;
            private readonly IReadOnlyList<PackedMapDiagnostic> diagnostics;
            public FakeBackend(byte[] bytes, int? consumed = null, string identity = "fake", IEnumerable<IniSourceProvenance> provenance = null, IEnumerable<PackedMapDiagnostic> diagnostics = null) { this.bytes = bytes; this.consumed = consumed ?? 1; this.identity = identity; this.provenance = provenance == null ? new[] { new IniSourceProvenance("synthetic", new[] { LogicalContentPath.Parse("synthetic.ini") }) } : provenance.ToArray(); this.diagnostics = diagnostics == null ? Array.Empty<PackedMapDiagnostic>() : diagnostics.ToArray(); }
            public LzoDecodeResult Decode(LzoDecodeRequest request) { return new LzoDecodeResult(bytes, consumed, identity, diagnostics, provenance); }
        }

        private sealed class NullResultBackend : ILzoDecodeBackend
        { public LzoDecodeResult Decode(LzoDecodeRequest request) { return null; } }

        private sealed class ThrowingBackend : ILzoDecodeBackend
        { public LzoDecodeResult Decode(LzoDecodeRequest request) { throw new InvalidOperationException("synthetic backend failure"); } }

        private sealed class CancellingBackend : ILzoDecodeBackend
        { public LzoDecodeResult Decode(LzoDecodeRequest request) { throw new OperationCanceledException(); } }

        private sealed class CapturingBackend : ILzoDecodeBackend
        {
            public CancellationToken Token { get; private set; }
            public LzoDecodeResult Decode(LzoDecodeRequest request)
            {
                Token = request.CancellationToken;
                return new LzoDecodeResult(new byte[] { 1 }, request.Compressed.LongLength > int.MaxValue ? 0 : (int)request.Compressed.LongLength, "capturing", Array.Empty<PackedMapDiagnostic>(), request.SourceProvenance);
            }
        }

        private sealed class SequenceBackend : ILzoDecodeBackend
        {
            private readonly IReadOnlyList<byte[]> outputs;
            private int index;
            public SequenceBackend(IEnumerable<byte[]> outputs) { this.outputs = outputs.ToArray(); }
            public LzoDecodeResult Decode(LzoDecodeRequest request)
            {
                byte[] output = outputs[Math.Min(index++, outputs.Count - 1)];
                return new LzoDecodeResult(output, (int)request.Compressed.LongLength, "sequence", Array.Empty<PackedMapDiagnostic>(), request.SourceProvenance);
            }
        }
    }
}
