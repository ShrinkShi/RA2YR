using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RA2YR.Core.Formats.Mix;
using RA2YR.Core.Formats.ShpTs;

namespace RA2YR.Tests.EditMode.Formats.ShpTs
{
    [TestFixture]
    public sealed class WestwoodShpTsDecoderTests
    {
        [TestCase(0u, 0)]
        [TestCase(1u, 1)]
        public void RawFlagsDecodeRowMajorWithoutChangingZeroIndices(
            uint flags,
            int expectedKind)
        {
            byte[] bytes = ShpTsSyntheticFixtureFactory.Build(
                2, 2, ShpTsSyntheticFixtureFactory.Raw(2, 2, flags, 0, 2, 3, 4));
            ShpTsDocument document = ShpTsTestSupport.AssertParseSuccess(
                ShpTsTestSupport.Read(bytes));
            ShpTsIndexedLocalFrame frame = ShpTsTestSupport.AssertDecodeSuccess(
                WestwoodShpTsDecoder.DecodeFrame(bytes, document, 0));

            Assert.That(frame.CompressionKind,
                Is.EqualTo((ShpTsCompressionKind)expectedKind));
            Assert.That(frame.GetIndicesCopy(), Is.EqualTo(new byte[] { 0, 2, 3, 4 }));
            Assert.That(frame.BytesConsumed, Is.EqualTo(4));
            Assert.That(frame.MinimumIndex, Is.Zero);
            Assert.That(frame.MaximumIndex, Is.EqualTo(4));
        }

        [Test]
        public void RawPayloadTruncationFailsWithoutPaddingZeros()
        {
            byte[] bytes = ShpTsSyntheticFixtureFactory.Build(
                2, 2, ShpTsSyntheticFixtureFactory.Raw(2, 2, 0, 1, 2, 3));
            ShpTsDocument document = ShpTsTestSupport.AssertParseSuccess(
                ShpTsTestSupport.Read(bytes));
            ShpTsTestSupport.AssertDecodeFailure(
                WestwoodShpTsDecoder.DecodeFrame(bytes, document, 0),
                ShpTsDiagnosticCode.RawPayloadTruncated);
        }

        [Test]
        public void RawFrameReportsAlignmentPaddingWithoutDecodingIt()
        {
            byte[] baseBytes = ShpTsSyntheticFixtureFactory.Build(
                1, 1, ShpTsSyntheticFixtureFactory.Raw(1, 1, 0, 9));
            byte[] bytes = ShpTsTestSupport.Extend(baseBytes, 7);
            ShpTsDocument document = ShpTsTestSupport.AssertParseSuccess(
                ShpTsTestSupport.Read(bytes));
            ShpTsIndexedLocalFrame frame = ShpTsTestSupport.AssertDecodeSuccess(
                WestwoodShpTsDecoder.DecodeFrame(bytes, document, 0));
            Assert.That(frame.GetIndicesCopy(), Is.EqualTo(new byte[] { 9 }));
            Assert.That(frame.PaddingBytes, Is.EqualTo(7));
        }

        [Test]
        public void RleConsecutiveLiteralsDecodeAsIndividualIndices()
        {
            byte[] bytes = ShpTsSyntheticFixtureFactory.Build(
                4, 1, ShpTsSyntheticFixtureFactory.Rle(
                    4, 1, new byte[] { 1, 2, 3, 4 }));
            ShpTsIndexedLocalFrame frame = Decode(bytes);
            Assert.That(frame.GetIndicesCopy(), Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(frame.CompressionKind,
                Is.EqualTo(ShpTsCompressionKind.RleZeroTransparent));
        }

        [TestCase(1)]
        [TestCase(4)]
        [TestCase(255)]
        public void RleZeroRunOutputsExactCount(int width)
        {
            byte[] bytes = ShpTsSyntheticFixtureFactory.Build(
                checked((ushort)width),
                1,
                ShpTsSyntheticFixtureFactory.Rle(
                    checked((ushort)width),
                    1,
                    new byte[] { 0, checked((byte)width) }));
            Assert.That(Decode(bytes).GetIndicesCopy(), Is.EqualTo(new byte[width]));
        }

        [Test]
        public void WideTransparentRowCanUseMultipleRuns()
        {
            byte[] bytes = ShpTsSyntheticFixtureFactory.Build(
                300, 1, ShpTsSyntheticFixtureFactory.Rle(
                    300, 1, new byte[] { 0, 255, 0, 45 }));
            Assert.That(Decode(bytes).PixelCount, Is.EqualTo(300));
        }

        [Test]
        public void MixedLiteralAndZeroRunPreservesRowOrder()
        {
            byte[] bytes = ShpTsSyntheticFixtureFactory.Build(
                6, 1, ShpTsSyntheticFixtureFactory.Rle(
                    6, 1, new byte[] { 8, 0, 3, 9, 10 }));
            Assert.That(Decode(bytes).GetIndicesCopy(),
                Is.EqualTo(new byte[] { 8, 0, 0, 0, 9, 10 }));
        }

        [Test]
        public void MultipleRleRowsCompleteExactly()
        {
            byte[] bytes = ShpTsSyntheticFixtureFactory.Build(
                3, 2, ShpTsSyntheticFixtureFactory.Rle(
                    3,
                    2,
                    new byte[] { 1, 2, 3 },
                    new byte[] { 0, 2, 4 }));
            Assert.That(Decode(bytes).GetIndicesCopy(),
                Is.EqualTo(new byte[] { 1, 2, 3, 0, 0, 4 }));
        }

        [TestCase((ushort)0)]
        [TestCase((ushort)1)]
        public void RleLineLengthBelowHeaderFails(ushort declaredLength)
        {
            var frame = new ShpTsSyntheticFixtureFactory.FrameSpec
            {
                Width = 1,
                Height = 1,
                Flags = 3,
                Payload = ShpTsSyntheticFixtureFactory.RlePayloadWithDeclaredLine(
                    declaredLength)
            };
            AssertDecodeFailure(bytes: ShpTsSyntheticFixtureFactory.Build(1, 1, frame),
                ShpTsDiagnosticCode.RleLineLengthTooSmall);
        }

        [Test]
        public void RleDeclaredLineCannotCrossFrameWindow()
        {
            var frame = new ShpTsSyntheticFixtureFactory.FrameSpec
            {
                Width = 1,
                Height = 1,
                Flags = 3,
                Payload = ShpTsSyntheticFixtureFactory.RlePayloadWithDeclaredLine(5, 1)
            };
            ShpTsDiagnostic diagnostic = AssertDecodeFailure(
                ShpTsSyntheticFixtureFactory.Build(1, 1, frame),
                ShpTsDiagnosticCode.RleLineTruncated);
            Assert.That(diagnostic.RowIndex, Is.Zero);
        }

        [Test]
        public void DanglingZeroCommandFailsAtExactRow()
        {
            byte[] bytes = ShpTsSyntheticFixtureFactory.Build(
                1, 1, ShpTsSyntheticFixtureFactory.Rle(1, 1, new byte[] { 0 }));
            ShpTsDiagnostic diagnostic = AssertDecodeFailure(
                bytes,
                ShpTsDiagnosticCode.RleDanglingZeroCommand);
            Assert.That(diagnostic.FrameIndex, Is.Zero);
            Assert.That(diagnostic.RowIndex, Is.Zero);
        }

        [Test]
        public void ZeroZeroConsumesTwoBytesButReturnsUnresolved()
        {
            byte[] bytes = ShpTsSyntheticFixtureFactory.Build(
                1, 1, ShpTsSyntheticFixtureFactory.Rle(1, 1, new byte[] { 0, 0, 1 }));
            ShpTsDiagnostic diagnostic = AssertDecodeFailure(
                bytes,
                ShpTsDiagnosticCode.ZeroOutputCommandSemanticsUnresolved);
            Assert.That(diagnostic.RequestedLength, Is.EqualTo(2));
            Assert.That(diagnostic.AbsoluteOffset,
                Is.EqualTo(ShpTsTestSupport.AssertParseSuccess(
                    ShpTsTestSupport.Read(bytes)).Frames[0].DataAbsoluteOffset + 2));
        }

        [Test]
        public void RleOutputUnderflowFailsWithoutTransparentPadding()
        {
            AssertDecodeFailure(
                ShpTsSyntheticFixtureFactory.Build(
                    2, 1, ShpTsSyntheticFixtureFactory.Rle(2, 1, new byte[] { 7 })),
                ShpTsDiagnosticCode.RleOutputUnderflow);
        }

        [Test]
        public void RleLiteralOutputOverflowFailsWithoutClamp()
        {
            AssertDecodeFailure(
                ShpTsSyntheticFixtureFactory.Build(
                    1, 1, ShpTsSyntheticFixtureFactory.Rle(1, 1, new byte[] { 7, 8 })),
                ShpTsDiagnosticCode.RleOutputOverflow);
        }

        [Test]
        public void RleRunOutputOverflowFailsWithoutClamp()
        {
            AssertDecodeFailure(
                ShpTsSyntheticFixtureFactory.Build(
                    2, 1, ShpTsSyntheticFixtureFactory.Rle(2, 1, new byte[] { 0, 3 })),
                ShpTsDiagnosticCode.RleOutputOverflow);
        }

        [Test]
        public void RleBytesAfterCompletedWidthAreNotIgnored()
        {
            AssertDecodeFailure(
                ShpTsSyntheticFixtureFactory.Build(
                    1, 1, ShpTsSyntheticFixtureFactory.Rle(1, 1, new byte[] { 7, 8 })),
                ShpTsDiagnosticCode.RleOutputOverflow);
        }

        [Test]
        public void RleLineLengthBudgetIsIndependent()
        {
            byte[] bytes = ShpTsSyntheticFixtureFactory.Build(
                2, 1, ShpTsSyntheticFixtureFactory.Rle(2, 1, new byte[] { 1, 2 }));
            ShpTsDocument document = ShpTsTestSupport.AssertParseSuccess(
                ShpTsTestSupport.Read(bytes));
            ShpTsTestSupport.AssertDecodeFailure(
                WestwoodShpTsDecoder.DecodeFrame(
                    bytes,
                    document,
                    0,
                    ShpTsTestSupport.Limits(maxSingleRowBytes: 3)),
                ShpTsDiagnosticCode.RleLineLengthBudgetExceeded);
        }

        [Test]
        public void CommandsPerRowBudgetStopsBeforeUnboundedLoop()
        {
            byte[] bytes = ShpTsSyntheticFixtureFactory.Build(
                3, 1, ShpTsSyntheticFixtureFactory.Rle(3, 1, new byte[] { 1, 2, 3 }));
            ShpTsDocument document = ShpTsTestSupport.AssertParseSuccess(
                ShpTsTestSupport.Read(bytes));
            ShpTsTestSupport.AssertDecodeFailure(
                WestwoodShpTsDecoder.DecodeFrame(bytes, document, 0,
                    ShpTsTestSupport.Limits(maxCommandsPerRow: 2)),
                ShpTsDiagnosticCode.CommandBudgetExceeded);
        }

        [Test]
        public void CommandsPerFrameBudgetSpansRows()
        {
            byte[] bytes = ShpTsSyntheticFixtureFactory.Build(
                1, 2, ShpTsSyntheticFixtureFactory.Rle(
                    1, 2, new byte[] { 1 }, new byte[] { 2 }));
            ShpTsDocument document = ShpTsTestSupport.AssertParseSuccess(
                ShpTsTestSupport.Read(bytes));
            ShpTsTestSupport.AssertDecodeFailure(
                WestwoodShpTsDecoder.DecodeFrame(bytes, document, 0,
                    ShpTsTestSupport.Limits(maxCommandsPerFrame: 1)),
                ShpTsDiagnosticCode.CommandBudgetExceeded);
        }

        [Test]
        public void CompressedFrameByteBudgetIsExplicit()
        {
            byte[] bytes = ShpTsSyntheticFixtureFactory.Build(
                2, 1, ShpTsSyntheticFixtureFactory.Rle(2, 1, new byte[] { 1, 2 }));
            ShpTsDocument document = ShpTsTestSupport.AssertParseSuccess(
                ShpTsTestSupport.Read(bytes));
            ShpTsTestSupport.AssertDecodeFailure(
                WestwoodShpTsDecoder.DecodeFrame(bytes, document, 0,
                    ShpTsTestSupport.Limits(maxSingleFrameCompressedBytes: 3)),
                ShpTsDiagnosticCode.CompressedFrameBudgetExceeded);
        }

        [Test]
        public void Flags2DecodeFailsAsUnresolvedNotCorrupt()
        {
            byte[] bytes = ShpTsSyntheticFixtureFactory.Build(
                1, 1, ShpTsSyntheticFixtureFactory.Raw(1, 1, 2, 7));
            ShpTsDocument document = ShpTsTestSupport.AssertParseSuccess(
                ShpTsTestSupport.Read(bytes));
            ShpTsTestSupport.AssertDecodeFailure(
                WestwoodShpTsDecoder.DecodeFrame(bytes, document, 0),
                ShpTsDiagnosticCode.SourceConflictingFlags2);
        }

        [Test]
        public void UnknownFlagsDecodeFailsWithoutMasking()
        {
            byte[] bytes = ShpTsSyntheticFixtureFactory.Build(
                1, 1, ShpTsSyntheticFixtureFactory.Raw(1, 1, 9, 7));
            ShpTsDocument document = ShpTsTestSupport.AssertParseSuccess(
                ShpTsTestSupport.Read(bytes));
            ShpTsTestSupport.AssertDecodeFailure(
                WestwoodShpTsDecoder.DecodeFrame(bytes, document, 0),
                ShpTsDiagnosticCode.UnknownFlags);
        }

        [Test]
        public void CanonicalEmptyFrameDecodesToEmptyLocalFrame()
        {
            byte[] bytes = ShpTsSyntheticFixtureFactory.Build(
                1, 1, ShpTsSyntheticFixtureFactory.Empty());
            ShpTsIndexedLocalFrame frame = Decode(bytes);
            Assert.That(frame.Width, Is.Zero);
            Assert.That(frame.Height, Is.Zero);
            Assert.That(frame.PixelCount, Is.Zero);
            Assert.That(frame.BytesConsumed, Is.Zero);
        }

        [Test]
        public void DecodeAllSupportsMixedRawAndRleFrames()
        {
            byte[] bytes = ShpTsSyntheticFixtureFactory.Build(
                2,
                1,
                ShpTsSyntheticFixtureFactory.Raw(2, 1, 0, 1, 2),
                ShpTsSyntheticFixtureFactory.Rle(2, 1, new byte[] { 0, 1, 3 }));
            ShpTsDocument document = ShpTsTestSupport.AssertParseSuccess(
                ShpTsTestSupport.Read(bytes));
            ShpTsDecodeDocumentResult result = WestwoodShpTsDecoder.DecodeAll(
                bytes, document);
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Document.Frames, Has.Count.EqualTo(2));
            Assert.That(result.Document.Frames[1].GetIndicesCopy(),
                Is.EqualTo(new byte[] { 0, 3 }));
        }

        [Test]
        public void TotalDecodedPixelBudgetFailsBeforeSecondFrame()
        {
            byte[] bytes = ShpTsSyntheticFixtureFactory.Build(
                2,
                1,
                ShpTsSyntheticFixtureFactory.Raw(2, 1, 0, 1, 2),
                ShpTsSyntheticFixtureFactory.Raw(2, 1, 0, 3, 4));
            ShpTsDocument document = ShpTsTestSupport.AssertParseSuccess(
                ShpTsTestSupport.Read(bytes));
            ShpTsDecodeDocumentResult result = WestwoodShpTsDecoder.DecodeAll(
                bytes,
                document,
                ShpTsTestSupport.Limits(maxTotalDecodedPixels: 3));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Document, Is.Null);
            Assert.That(result.Diagnostics.Any(item =>
                item.Code == ShpTsDiagnosticCode.TotalDecodedPixelBudgetExceeded), Is.True);
        }

        [Test]
        public void RepeatedOffsetFramesDecodeSeparatelyAndReportOverlap()
        {
            var first = ShpTsSyntheticFixtureFactory.Raw(1, 1, 0, 7);
            var second = ShpTsSyntheticFixtureFactory.Raw(1, 1, 0, 7);
            first.DataOffset = 64;
            second.DataOffset = 64;
            byte[] bytes = ShpTsSyntheticFixtureFactory.Build(1, 1, first, second);
            ShpTsDocument document = ShpTsTestSupport.AssertParseSuccess(
                ShpTsTestSupport.Read(bytes));
            ShpTsDecodeDocumentResult result = WestwoodShpTsDecoder.DecodeAll(bytes, document);
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Document.Frames, Has.Count.EqualTo(2));
            Assert.That(result.Diagnostics.Any(item =>
                item.Code == ShpTsDiagnosticCode.FrameDataOverlap), Is.True);
        }

        [Test]
        public void DescendingOffsetsDecodeByAbsoluteLocation()
        {
            var first = ShpTsSyntheticFixtureFactory.Raw(1, 1, 0, 1);
            var second = ShpTsSyntheticFixtureFactory.Raw(1, 1, 0, 2);
            first.DataOffset = 80;
            second.DataOffset = 64;
            byte[] bytes = ShpTsSyntheticFixtureFactory.Build(1, 1, first, second);
            ShpTsDocument document = ShpTsTestSupport.AssertParseSuccess(
                ShpTsTestSupport.Read(bytes));
            Assert.That(ShpTsTestSupport.AssertDecodeSuccess(
                WestwoodShpTsDecoder.DecodeFrame(bytes, document, 0)).GetIndicesCopy(),
                Is.EqualTo(new byte[] { 1 }));
            Assert.That(ShpTsTestSupport.AssertDecodeSuccess(
                WestwoodShpTsDecoder.DecodeFrame(bytes, document, 1)).GetIndicesCopy(),
                Is.EqualTo(new byte[] { 2 }));
        }

        [Test]
        public void RlePaddingIsOutsideActualConsumption()
        {
            byte[] baseBytes = ShpTsSyntheticFixtureFactory.Build(
                1, 1, ShpTsSyntheticFixtureFactory.Rle(1, 1, new byte[] { 7 }));
            byte[] bytes = ShpTsTestSupport.Extend(baseBytes, 5);
            ShpTsIndexedLocalFrame frame = Decode(bytes);
            Assert.That(frame.BytesConsumed, Is.EqualTo(3));
            Assert.That(frame.PaddingBytes, Is.EqualTo(5));
        }

        [Test]
        public void SeekableShortReadDecodeMatchesMemory()
        {
            byte[] bytes = ShpTsSyntheticFixtureFactory.Build(
                3, 1, ShpTsSyntheticFixtureFactory.Rle(3, 1, new byte[] { 1, 0, 1, 2 }));
            ShpTsDocument document = ShpTsTestSupport.AssertParseSuccess(
                ShpTsTestSupport.Read(bytes));
            byte[] memory = ShpTsTestSupport.AssertDecodeSuccess(
                WestwoodShpTsDecoder.DecodeFrame(bytes, document, 0)).GetIndicesCopy();
            using (var stream = new ShpTsShortReadMemoryStream(bytes, 1))
            {
                byte[] streamed = ShpTsTestSupport.AssertDecodeSuccess(
                    WestwoodShpTsDecoder.DecodeFrame(
                        stream, bytes.Length, document, 0, leaveOpen: true)).GetIndicesCopy();
                Assert.That(streamed, Is.EqualTo(memory));
                Assert.That(stream.ReadCalls, Is.GreaterThan(1));
            }
        }

        [Test]
        public void MixWindowDecodeMatchesMemory()
        {
            byte[] bytes = ShpTsSyntheticFixtureFactory.Build(
                3, 1, ShpTsSyntheticFixtureFactory.Raw(3, 1, 0, 1, 2, 3));
            ShpTsDocument memoryDocument = ShpTsTestSupport.AssertParseSuccess(
                ShpTsTestSupport.Read(bytes));
            byte[] memory = ShpTsTestSupport.AssertDecodeSuccess(
                WestwoodShpTsDecoder.DecodeFrame(bytes, memoryDocument, 0)).GetIndicesCopy();
            MixArchiveReadResult mix = ShpTsTestSupport.ReadSyntheticMix(bytes);
            Assert.That(mix.IsSuccess, Is.True);
            using (mix.Archive)
            {
                var window = mix.Archive.Entries.Single().OpenPayloadWindow();
                ShpTsDocument windowDocument = ShpTsTestSupport.AssertParseSuccess(
                    WestwoodShpTsReader.Read(
                        window,
                        ShpTsTestSupport.Source(),
                        ShpTsTestSupport.Provenance()));
                byte[] decoded = ShpTsTestSupport.AssertDecodeSuccess(
                    WestwoodShpTsDecoder.DecodeFrame(window, windowDocument, 0)).GetIndicesCopy();
                Assert.That(decoded, Is.EqualTo(memory));
            }
        }

        [Test]
        public void SubwindowBudgetFailsBeforeRleRowRead()
        {
            byte[] bytes = ShpTsSyntheticFixtureFactory.Build(
                1, 1, ShpTsSyntheticFixtureFactory.Rle(1, 1, new byte[] { 7 }));
            ShpTsDocument document = ShpTsTestSupport.AssertParseSuccess(
                ShpTsTestSupport.Read(bytes));
            ShpTsTestSupport.AssertDecodeFailure(
                WestwoodShpTsDecoder.DecodeFrame(bytes, document, 0,
                    ShpTsTestSupport.Limits(maxSubwindows: 1)),
                ShpTsDiagnosticCode.SubwindowBudgetExceeded);
        }

        [Test]
        public void DecodedFrameDoesNotAliasInputOrReturnedCopies()
        {
            byte[] bytes = ShpTsSyntheticFixtureFactory.Build(
                2, 1, ShpTsSyntheticFixtureFactory.Raw(2, 1, 0, 1, 2));
            ShpTsIndexedLocalFrame frame = Decode(bytes);
            byte[] copy = frame.GetIndicesCopy();
            copy[0] = 99;
            bytes[bytes.Length - 2] = 88;
            Assert.That(frame.GetIndicesCopy(), Is.EqualTo(new byte[] { 1, 2 }));
        }

        [Test]
        public void SameInputProducesStableDecodedModelHash()
        {
            byte[] bytes = ShpTsSyntheticFixtureFactory.Build(
                2, 1, ShpTsSyntheticFixtureFactory.Raw(2, 1, 0, 1, 2));
            ShpTsDocument firstDocument = ShpTsTestSupport.AssertParseSuccess(
                ShpTsTestSupport.Read(bytes));
            ShpTsDocument secondDocument = ShpTsTestSupport.AssertParseSuccess(
                ShpTsTestSupport.Read((byte[])bytes.Clone()));
            string first = WestwoodShpTsDecoder.DecodeAll(bytes, firstDocument)
                .Document.CanonicalDecodedModelSha256;
            string second = WestwoodShpTsDecoder.DecodeAll(bytes, secondDocument)
                .Document.CanonicalDecodedModelSha256;
            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void CorruptRleTerminatesWithoutUnboundedAllocation()
        {
            byte[] commands = Enumerable.Repeat((byte)1, 200).ToArray();
            byte[] bytes = ShpTsSyntheticFixtureFactory.Build(
                1, 1, ShpTsSyntheticFixtureFactory.Rle(1, 1, commands));
            ShpTsDiagnostic diagnostic = AssertDecodeFailure(
                bytes,
                ShpTsDiagnosticCode.RleOutputOverflow);
            Assert.That(diagnostic.AbsoluteOffset, Is.GreaterThanOrEqualTo(0));
        }

        private static ShpTsIndexedLocalFrame Decode(byte[] bytes)
        {
            ShpTsDocument document = ShpTsTestSupport.AssertParseSuccess(
                ShpTsTestSupport.Read(bytes));
            return ShpTsTestSupport.AssertDecodeSuccess(
                WestwoodShpTsDecoder.DecodeFrame(bytes, document, 0));
        }

        private static ShpTsDiagnostic AssertDecodeFailure(
            byte[] bytes,
            ShpTsDiagnosticCode code)
        {
            ShpTsDocument document = ShpTsTestSupport.AssertParseSuccess(
                ShpTsTestSupport.Read(bytes));
            return ShpTsTestSupport.AssertDecodeFailure(
                WestwoodShpTsDecoder.DecodeFrame(bytes, document, 0),
                code);
        }
    }
}
