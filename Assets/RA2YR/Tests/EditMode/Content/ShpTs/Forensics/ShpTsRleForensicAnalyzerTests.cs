using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RA2YR.Core.Binary;
using RA2YR.Core.Binary.Seekable;
using RA2YR.Core.Content;
using RA2YR.Core.Content.ShpTs.Forensics;
using RA2YR.Core.Formats.ShpTs;

namespace RA2YR.Tests.EditMode.Content.ShpTs.Forensics
{
    public sealed class ShpTsRleForensicAnalyzerTests
    {
        [Test]
        public void ExactLiteralRowIsClassifiedWithoutExtra()
        {
            Fixture fixture = Build(4, new byte[] { 1, 2, 3, 4 });
            ShpTsRleForensicRowScalar row = Analyze(fixture).Rows.Single();

            Assert.That(row.MechanicalLengthClass, Is.EqualTo(ShpTsRleForensicLengthClass.Width));
            Assert.That(row.ExtraSource, Is.EqualTo(ShpTsRleForensicExtraSource.None));
            Assert.That(row.LiteralCount, Is.EqualTo(4));
            Assert.That(row.ZeroRunCount, Is.Zero);
            Assert.That(row.RemainingClass, Is.EqualTo(ShpTsRleForensicRemainingClass.End));
        }

        [Test]
        public void FinalZeroRunGuardIsClassifiedWithoutProducingPixels()
        {
            Fixture fixture = Build(4, new byte[] { 1, 2, 0, 3 });
            ShpTsRleForensicRowScalar row = Analyze(fixture).Rows.Single();

            Assert.That(row.MechanicalLengthClass, Is.EqualTo(ShpTsRleForensicLengthClass.WidthPlusOne));
            Assert.That(row.ExtraSource, Is.EqualTo(ShpTsRleForensicExtraSource.ZeroRun));
            Assert.That(row.ExtraFromLastCommand, Is.True);
            Assert.That(row.ExtraIsLastOutput, Is.True);
            Assert.That(row.ExtraIsZero, Is.True);
            Assert.That(row.ExtraOvershoot, Is.EqualTo(1));
            Assert.That(row.FinalZeroRunCount, Is.EqualTo(3));
            Assert.That(row.DistanceBeforeFinalZeroRun, Is.EqualTo(2));
            Assert.That(row.IgnoreOneExtraInputExact, Is.True);
            Assert.That(row.GuardPattern, Is.True);
            Assert.That(row.XccVisibleLengthClass, Is.EqualTo(ShpTsRleForensicLengthClass.Width));
        }

        [Test]
        public void LiteralOverflowIsNotAZeroRunGuard()
        {
            Fixture fixture = Build(4, new byte[] { 1, 2, 3, 4, 5 });
            ShpTsRleForensicRowScalar row = Analyze(fixture).Rows.Single();

            Assert.That(row.ExtraSource, Is.EqualTo(ShpTsRleForensicExtraSource.Literal));
            Assert.That(row.LiteralOverflow, Is.True);
            Assert.That(row.ExtraIsZero, Is.False);
            Assert.That(row.GuardPattern, Is.False);
            Assert.That(row.XccVisibleLengthClass, Is.EqualTo(ShpTsRleForensicLengthClass.WidthPlusOne));
        }

        [Test]
        public void NonFinalOverflowingZeroRunDoesNotPassGuardGate()
        {
            Fixture fixture = Build(4, new byte[] { 1, 0, 4, 2 });
            ShpTsRleForensicRowScalar row = Analyze(fixture).Rows.Single();

            Assert.That(row.ExtraSource, Is.EqualTo(ShpTsRleForensicExtraSource.ZeroRun));
            Assert.That(row.ExtraFromLastCommand, Is.False);
            Assert.That(row.ExtraIsLastOutput, Is.False);
            Assert.That(row.GuardPattern, Is.False);
        }

        [Test]
        public void ZeroZeroIsCountedAsUnresolvedZeroOutputCommand()
        {
            Fixture fixture = Build(4, new byte[] { 1, 2, 0, 0, 3, 4 });
            ShpTsRleForensicRowScalar row = Analyze(fixture).Rows.Single();

            Assert.That(row.ZeroZeroCount, Is.EqualTo(1));
            Assert.That(row.MechanicalOutputLength, Is.EqualTo(4));
            Assert.That(row.FinalCommandKind, Is.EqualTo(ShpTsRleForensicCommandKind.Literal));
        }

        [Test]
        public void DanglingZeroFailsWithoutReadingPastTheRow()
        {
            Fixture fixture = Build(4, new byte[] { 1, 2, 0 });
            ShpTsRleForensicFrameAnalysis result = Analyze(fixture);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.FailureCode, Is.EqualTo(ShpTsRleForensicFailureCode.DanglingZero));
            Assert.That(result.FailureRowIndex, Is.Zero);
        }

        [Test]
        public void LineLengthBelowHeaderFailsClosed()
        {
            Fixture fixture = BuildRawRow(4, new byte[] { 1, 0 });
            ShpTsRleForensicFrameAnalysis result = Analyze(fixture);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.FailureCode, Is.EqualTo(ShpTsRleForensicFailureCode.LineLengthTooSmall));
        }

        [Test]
        public void TruncatedPayloadFailsClosed()
        {
            Fixture fixture = BuildRawRow(4, new byte[] { 6, 0, 1, 2 });
            ShpTsRleForensicFrameAnalysis result = Analyze(fixture);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.FailureCode, Is.EqualTo(ShpTsRleForensicFailureCode.RowPayloadTruncated));
        }

        [Test]
        public void CommandBudgetStopsBeforeLaterCommands()
        {
            Fixture fixture = Build(4, new byte[] { 1, 2, 3, 4 });
            var limits = new ShpTsRleForensicLimits(4, 1024, 2, 2);
            ShpTsRleForensicFrameAnalysis result = ShpTsRleForensicAnalyzer.Analyze(
                fixture.Bytes,
                fixture.Document,
                0,
                true,
                limits);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.FailureCode, Is.EqualTo(ShpTsRleForensicFailureCode.CommandBudgetExceeded));
        }

        [Test]
        public void RowBudgetRejectsDeclaredHeightBeforeReadingRows()
        {
            Fixture fixture = Build(4,
                new byte[] { 1, 2, 3, 4 },
                new byte[] { 1, 2, 3, 4 });
            var limits = new ShpTsRleForensicLimits(1, 1024, 32, 64);
            ShpTsRleForensicFrameAnalysis result = ShpTsRleForensicAnalyzer.Analyze(
                fixture.Bytes,
                fixture.Document,
                0,
                true,
                limits);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.FailureCode, Is.EqualTo(ShpTsRleForensicFailureCode.CommandBudgetExceeded));
            Assert.That(result.Rows, Is.Empty);
        }

        [Test]
        public void RowZeroModeDoesNotReadLaterMalformedRow()
        {
            Fixture fixture = BuildRawRows(4,
                new byte[] { 6, 0, 1, 2, 3, 4 },
                new byte[] { 1, 0 });

            ShpTsRleForensicFrameAnalysis first = ShpTsRleForensicAnalyzer.Analyze(
                fixture.Bytes, fixture.Document, 0, false);
            ShpTsRleForensicFrameAnalysis all = ShpTsRleForensicAnalyzer.Analyze(
                fixture.Bytes, fixture.Document, 0, true);

            Assert.That(first.IsSuccess, Is.True);
            Assert.That(first.Rows.Count, Is.EqualTo(1));
            Assert.That(all.IsSuccess, Is.False);
            Assert.That(all.FailureRowIndex, Is.EqualTo(1));
        }

        [Test]
        public void NoHeaderInterpretationIncludesTheNextTwoBytesOnlyForClassification()
        {
            Fixture fixture = Build(4,
                new byte[] { 1, 2, 3, 4 },
                new byte[] { 1, 2, 3, 4 });
            ShpTsRleForensicRowScalar row = Analyze(fixture, false).Rows.Single();

            Assert.That(row.NoHeaderMalformed, Is.True);
            Assert.That(row.NoHeaderLengthClass, Is.EqualTo(ShpTsRleForensicLengthClass.Malformed));
            Assert.That(row.InputExact, Is.True);
        }

        [Test]
        public void MemoryStreamShortReadAndWindowProduceSameScalar()
        {
            Fixture fixture = Build(4,
                new byte[] { 1, 2, 0, 3 },
                new byte[] { 0, 5 });
            ShpTsRleForensicFrameAnalysis memory = Analyze(fixture, true);
            ShpTsRleForensicFrameAnalysis stream;
            using (var shortRead = new ShortReadStream(fixture.Bytes, 1))
            {
                stream = ShpTsRleForensicAnalyzer.Analyze(
                    shortRead,
                    fixture.Bytes.LongLength,
                    fixture.Document,
                    0,
                    true,
                    leaveOpen: true);
            }

            ShpTsRleForensicFrameAnalysis window;
            using (var backing = new MemoryStream(fixture.Bytes, false))
            using (ReadOnlyDataWindowSession session = ReadOnlyDataWindowSession.FromSeekableStream(
                backing,
                Source(),
                0,
                fixture.Bytes.LongLength,
                leaveOpen: true))
            {
                window = ShpTsRleForensicAnalyzer.Analyze(
                    session.Root,
                    fixture.Document,
                    0,
                    true);
            }

            Assert.That(stream.CanonicalScalar(), Is.EqualTo(memory.CanonicalScalar()));
            Assert.That(window.CanonicalScalar(), Is.EqualTo(memory.CanonicalScalar()));
        }

        [Test]
        public void StreamOwnershipIsExplicit()
        {
            Fixture fixture = Build(4, new byte[] { 1, 2, 3, 4 });
            var stream = new MemoryStream(fixture.Bytes, false);
            ShpTsRleForensicAnalyzer.Analyze(
                stream,
                fixture.Bytes.LongLength,
                fixture.Document,
                0,
                true,
                leaveOpen: true);

            Assert.That(stream.CanRead, Is.True);
            stream.Dispose();
        }

        [Test]
        public void InputLengthMismatchIsRejectedBeforeReading()
        {
            Fixture fixture = Build(4, new byte[] { 1, 2, 3, 4 });
            using (var stream = new MemoryStream(fixture.Bytes, false))
            {
                Assert.Throws<ArgumentException>(() => ShpTsRleForensicAnalyzer.Analyze(
                    stream,
                    fixture.Bytes.LongLength - 1,
                    fixture.Document,
                    0,
                    true,
                    leaveOpen: true));
            }
        }

        [Test]
        public void NonFlagsThreeFrameIsRejectedWithoutInterpretation()
        {
            Fixture fixture = Build(4, new byte[] { 1, 2, 3, 4 }, rawFlags: 1);
            ShpTsRleForensicFrameAnalysis result = Analyze(fixture);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.FailureCode,
                Is.EqualTo(ShpTsRleForensicFailureCode.FrameIsNotNonEmptyFlags3));
            Assert.That(result.Rows, Is.Empty);
        }

        [Test]
        public void PublicScalarModelExposesNoByteArrayOrCommandCollection()
        {
            Type type = typeof(ShpTsRleForensicRowScalar);
            Assert.That(type.GetProperties().Any(property =>
                property.PropertyType == typeof(byte[]) ||
                property.Name.IndexOf("CommandArray", StringComparison.Ordinal) >= 0), Is.False);
        }

        private static ShpTsRleForensicFrameAnalysis Analyze(
            Fixture fixture,
            bool allRows = true)
        {
            return ShpTsRleForensicAnalyzer.Analyze(
                fixture.Bytes,
                fixture.Document,
                0,
                allRows);
        }

        private static Fixture Build(
            ushort width,
            byte[] row,
            uint rawFlags = 3)
        {
            return Build(width, new[] { row }, rawFlags);
        }

        private static Fixture Build(
            ushort width,
            byte[] row0,
            byte[] row1)
        {
            return Build(width, new[] { row0, row1 }, 3);
        }

        private static Fixture Build(
            ushort width,
            byte[][] rows,
            uint rawFlags = 3)
        {
            byte[][] physical = rows.Select(payload =>
            {
                var row = new byte[checked(payload.Length + 2)];
                WriteUInt16(row, 0, checked((ushort)row.Length));
                Buffer.BlockCopy(payload, 0, row, 2, payload.Length);
                return row;
            }).ToArray();
            return BuildRawRows(width, physical, rawFlags);
        }

        private static Fixture BuildRawRow(
            ushort width,
            byte[] physical,
            uint rawFlags = 3)
        {
            return BuildRawRows(width, new[] { physical }, rawFlags);
        }

        private static Fixture BuildRawRows(
            ushort width,
            byte[] row0,
            byte[] row1)
        {
            return BuildRawRows(width, new[] { row0, row1 }, 3);
        }

        private static Fixture BuildRawRows(
            ushort width,
            byte[][] rows,
            uint rawFlags = 3)
        {
            const int dataOffset = 32;
            int length = checked(dataOffset + rows.Sum(row => row.Length));
            var bytes = new byte[length];
            int position = dataOffset;
            foreach (byte[] row in rows)
            {
                Buffer.BlockCopy(row, 0, bytes, position, row.Length);
                position = checked(position + row.Length);
            }

            var descriptor = new ShpTsFrameDescriptor(
                0,
                8,
                0,
                0,
                width,
                checked((ushort)rows.Length),
                rawFlags,
                new byte[4],
                0,
                dataOffset,
                dataOffset,
                length,
                rawFlags == 3
                    ? ShpTsCompressionKind.RleZeroTransparent
                    : ShpTsCompressionKind.RawTransparent,
                false);
            var document = new ShpTsDocument(
                Source(),
                Provenance(),
                length,
                0,
                dataOffset,
                new ShpTsHeader(0, width, checked((ushort)rows.Length), 1),
                new[] { descriptor });
            return new Fixture(bytes, document);
        }

        private static BinarySourceContext Source()
        {
            return new BinarySourceContext(
                "format.shp-ts-rle-forensic",
                "synthetic-source",
                LogicalContentPath.Parse("synthetic/forensic.shp"));
        }

        private static ShpTsSourceProvenance Provenance()
        {
            return new ShpTsSourceProvenance(
                "synthetic-source",
                new[]
                {
                    LogicalContentPath.Parse("synthetic.mix"),
                    LogicalContentPath.Parse("forensic.shp")
                });
        }

        private static void WriteUInt16(byte[] bytes, int offset, ushort value)
        {
            bytes[offset] = (byte)value;
            bytes[offset + 1] = (byte)(value >> 8);
        }

        private sealed class Fixture
        {
            public Fixture(byte[] bytes, ShpTsDocument document)
            {
                Bytes = bytes;
                Document = document;
            }

            public byte[] Bytes { get; }
            public ShpTsDocument Document { get; }
        }

        private sealed class ShortReadStream : MemoryStream
        {
            private readonly int maximumChunk;

            public ShortReadStream(byte[] bytes, int maximumChunk)
                : base(bytes, false)
            {
                this.maximumChunk = maximumChunk;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return base.Read(buffer, offset, Math.Min(count, maximumChunk));
            }
        }
    }
}
