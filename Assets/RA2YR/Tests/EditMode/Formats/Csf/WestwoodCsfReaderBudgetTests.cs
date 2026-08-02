using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RA2YR.Core.Binary;
using RA2YR.Core.Content;
using RA2YR.Core.Formats.Csf;

namespace RA2YR.Tests.EditMode.Formats.Csf
{
    public sealed class WestwoodCsfReaderBudgetTests
    {
        [Test]
        public void InputBudgetRejectsMemoryBeforeParsing()
        {
            byte[] input = Empty();
            CsfDiagnostic diagnostic = AssertFailure(
                Read(input, Limits(maxInputBytes: input.Length - 1)),
                CsfDiagnosticCode.InputBudgetExceeded);

            Assert.That(diagnostic.RequestedLength, Is.EqualTo(input.Length));
        }

        [Test]
        public void InputBudgetRejectsStreamBeforeFirstRead()
        {
            byte[] input = Empty();
            using (var stream = new CountingStream(input))
            {
                AssertFailure(
                    WestwoodCsfReader.Read(
                        stream,
                        input.Length,
                        Source(),
                        Provenance(),
                        Limits(maxInputBytes: input.Length - 1),
                        leaveOpen: true),
                    CsfDiagnosticCode.InputBudgetExceeded);
                Assert.That(stream.ReadCalls, Is.Zero);
            }
        }

        [Test]
        public void ZeroPerReadBudgetFailsIndependentlyFromEof()
        {
            AssertFailure(
                Read(Empty(), Limits(maxSingleReadBytes: 0)),
                CsfDiagnosticCode.ReadBudgetExceeded);
        }

        [Test]
        public void LabelCountBudgetFailsBeforeLabelAllocation()
        {
            byte[] input = CsfSyntheticFixtureFactory.Build(new[]
            {
                CsfSyntheticFixtureFactory.Label("A")
            });

            AssertFailure(
                Read(input, Limits(maxLabels: 0)),
                CsfDiagnosticCode.LabelBudgetExceeded);
        }

        [Test]
        public void TotalValueBudgetFailsBeforeValueAllocation()
        {
            byte[] input = OneValue();

            AssertFailure(
                Read(input, Limits(maxTotalValues: 0)),
                CsfDiagnosticCode.TotalValueBudgetExceeded);
        }

        [Test]
        public void PerLabelValueBudgetFailsBeforeValueAllocation()
        {
            byte[] input = OneValue();

            AssertFailure(
                Read(input, Limits(maxValuesPerLabel: 0)),
                CsfDiagnosticCode.ValuesPerLabelBudgetExceeded);
        }

        [Test]
        public void LabelNameBudgetIsIndependent()
        {
            byte[] input = CsfSyntheticFixtureFactory.Build(new[]
            {
                CsfSyntheticFixtureFactory.Label("AB")
            });

            AssertFailure(
                Read(input, Limits(maxLabelNameBytes: 1)),
                CsfDiagnosticCode.LabelNameBudgetExceeded);
        }

        [Test]
        public void TruncatedHugeLabelDeclarationReportsEofBeforeAllocationBudget()
        {
            byte[] input = CsfSyntheticFixtureFactory.Build(new[]
            {
                CsfSyntheticFixtureFactory.Label("A")
            });
            Array.Resize(ref input, WestwoodCsfReader.HeaderLength + 12);
            CsfSyntheticFixtureFactory.WriteUInt32(
                input,
                WestwoodCsfReader.HeaderLength + 8,
                500_000);

            CsfDiagnostic diagnostic = AssertFailure(
                Read(input, Limits(
                    maxAllocatedBytes: 16 * 1024,
                    maxLabelNameBytes: 500_000)),
                CsfDiagnosticCode.UnexpectedEndOfInput);

            Assert.That(diagnostic.AbsoluteOffset, Is.EqualTo(input.Length));
            Assert.That(diagnostic.RequestedLength, Is.EqualTo(500_000));
            Assert.That(diagnostic.RemainingLength, Is.Zero);
            Assert.That(diagnostic.LabelIndex, Is.Zero);
            Assert.That(diagnostic.RawRecordMarker,
                Is.EqualTo(WestwoodCsfReader.LabelMarker));
        }

        [Test]
        public void MainTextBudgetIsMeasuredInUtf16CodeUnits()
        {
            byte[] input = CsfSyntheticFixtureFactory.Build(new[]
            {
                CsfSyntheticFixtureFactory.Label(
                    "A",
                    CsfSyntheticFixtureFactory.Normal("BC"))
            });

            AssertFailure(
                Read(input, Limits(maxMainTextCodeUnits: 1)),
                CsfDiagnosticCode.MainTextBudgetExceeded);
        }

        [Test]
        public void TruncatedHugeMainTextReportsEofBeforeAllocationBudget()
        {
            byte[] input = OneValue();
            int marker = CsfSyntheticFixtureFactory.FindUInt32(
                input,
                WestwoodCsfReader.NormalValueMarker,
                WestwoodCsfReader.HeaderLength);
            Array.Resize(ref input, marker + 8);
            CsfSyntheticFixtureFactory.WriteUInt32(input, marker + 4, 500_000);

            CsfDiagnostic diagnostic = AssertFailure(
                Read(input, Limits(
                    maxAllocatedBytes: 16 * 1024,
                    maxMainTextCodeUnits: 500_000,
                    maxCumulativeUtf16CodeUnits: 500_000)),
                CsfDiagnosticCode.UnexpectedEndOfInput);

            Assert.That(diagnostic.AbsoluteOffset, Is.EqualTo(input.Length));
            Assert.That(diagnostic.RequestedLength, Is.EqualTo(1_000_000));
            Assert.That(diagnostic.RemainingLength, Is.Zero);
            Assert.That(diagnostic.LabelIndex, Is.Zero);
            Assert.That(diagnostic.ValueIndex, Is.Zero);
            Assert.That(diagnostic.RawRecordMarker,
                Is.EqualTo(WestwoodCsfReader.NormalValueMarker));
        }

        [Test]
        public void ExtraTextBudgetIsMeasuredInAsciiBytes()
        {
            byte[] input = CsfSyntheticFixtureFactory.Build(new[]
            {
                CsfSyntheticFixtureFactory.Label(
                    "A",
                    CsfSyntheticFixtureFactory.Extended("B", "CD"))
            });

            AssertFailure(
                Read(input, Limits(maxExtraTextBytes: 1)),
                CsfDiagnosticCode.ExtraTextBudgetExceeded);
        }

        [Test]
        public void CumulativeCodeUnitBudgetSpansAllValues()
        {
            byte[] input = CsfSyntheticFixtureFactory.Build(new[]
            {
                CsfSyntheticFixtureFactory.Label(
                    "A",
                    CsfSyntheticFixtureFactory.Normal("12"),
                    CsfSyntheticFixtureFactory.Normal("34"))
            });

            CsfDiagnostic diagnostic = AssertFailure(
                Read(input, Limits(maxCumulativeUtf16CodeUnits: 3)),
                CsfDiagnosticCode.CumulativeCodeUnitBudgetExceeded);
            Assert.That(diagnostic.LabelIndex, Is.Zero);
            Assert.That(diagnostic.ValueIndex, Is.EqualTo(1));
        }

        [Test]
        public void AllocationBudgetFailsBeforeFixedModelAllocation()
        {
            AssertFailure(
                Read(Empty(), Limits(maxAllocatedBytes: 8191)),
                CsfDiagnosticCode.AllocationBudgetExceeded);
        }

        [Test]
        public void StreamSnapshotAndModelUseOneAllocationBudget()
        {
            byte[] input = Empty();
            using (var stream = new MemoryStream(input, false))
            {
                AssertFailure(
                    WestwoodCsfReader.ReadSeekable(
                        stream,
                        Source(),
                        Provenance(),
                        Limits(maxAllocatedBytes: 8192),
                        leaveOpen: true),
                    CsfDiagnosticCode.AllocationBudgetExceeded);
            }
        }

        [Test]
        public void AbsoluteOffsetOverflowProducesControlledDiagnostic()
        {
            CsfDiagnostic diagnostic = AssertFailure(
                WestwoodCsfReader.Read(
                    Empty(),
                    Source(),
                    Provenance(),
                    absoluteStartOffset: long.MaxValue - 1),
                CsfDiagnosticCode.ArithmeticOverflow);

            Assert.That(diagnostic.BinaryCode,
                Is.EqualTo(BinaryDiagnosticCode.ArithmeticOverflow));
        }

        [Test]
        public void StreamIoExceptionProducesSanitizedReadFailure()
        {
            using (var stream = new ThrowingStream())
            {
                CsfDiagnostic diagnostic = AssertFailure(
                    WestwoodCsfReader.Read(
                        stream,
                        Empty().Length,
                        Source(),
                        Provenance(),
                        leaveOpen: true),
                    CsfDiagnosticCode.ReadFailure);

                Assert.That(diagnostic.Message, Does.Not.Contain("private"));
                Assert.That(diagnostic.Message, Does.Not.Contain(":\\"));
            }
        }

        [Test]
        public void NonSeekableStreamParsesWhenLengthIsExplicit()
        {
            byte[] input = OneValue();
            using (var stream = new CountingStream(input, canSeek: false))
            {
                CsfParseResult result = WestwoodCsfReader.Read(
                    stream,
                    input.Length,
                    Source(),
                    Provenance(),
                    leaveOpen: true);

                Assert.That(result.IsSuccess, Is.True);
            }
        }

        [Test]
        public void SeekableInferenceRejectsNonSeekableStream()
        {
            using (var stream = new CountingStream(Empty(), canSeek: false))
            {
                AssertFailure(
                    WestwoodCsfReader.ReadSeekable(
                        stream,
                        Source(),
                        Provenance(),
                        leaveOpen: true),
                    CsfDiagnosticCode.UnsupportedSeekOperation);
            }
        }

        [Test]
        public void StreamOwnershipFollowsLeaveOpenPolicy()
        {
            byte[] input = Empty();
            var owned = new TrackingStream(input);
            Assert.That(WestwoodCsfReader.ReadSeekable(
                owned,
                Source(),
                Provenance(),
                leaveOpen: false).IsSuccess, Is.True);
            Assert.That(owned.WasDisposed, Is.True);

            var borrowed = new TrackingStream(input);
            Assert.That(WestwoodCsfReader.ReadSeekable(
                borrowed,
                Source(),
                Provenance(),
                leaveOpen: true).IsSuccess, Is.True);
            Assert.That(borrowed.WasDisposed, Is.False);
            borrowed.Dispose();
        }

        [Test]
        public void EveryTruncationOfSmallDocumentFailsWithoutPartialModel()
        {
            byte[] input = OneValue();
            for (int length = 0; length < input.Length; length++)
            {
                CsfParseResult result = Read(input.Take(length).ToArray());
                Assert.That(result.IsSuccess, Is.False, "length=" + length);
                Assert.That(result.Document, Is.Null, "length=" + length);
                Assert.That(result.Diagnostics, Has.Count.EqualTo(1), "length=" + length);
            }
        }

        [Test]
        public void DamagedHugeDeclarationsDoNotCauseUnboundedAllocation()
        {
            byte[] input = Empty();
            CsfSyntheticFixtureFactory.WriteUInt32(input, 8, uint.MaxValue);
            CsfSyntheticFixtureFactory.WriteUInt32(input, 12, uint.MaxValue);

            CsfParseResult result = Read(input);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Document, Is.Null);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
        }

        [Test]
        public void ProvenanceCannotClaimAnotherLogicalSource()
        {
            var other = new CsfSourceProvenance(
                "other-source",
                new[] { LogicalContentPath.Parse("strings.csf") });

            Assert.Throws<ArgumentException>(() => WestwoodCsfReader.Read(
                Empty(),
                Source(),
                other));
        }

        private static byte[] Empty()
        {
            return CsfSyntheticFixtureFactory.Build(Array.Empty<SyntheticCsfLabel>());
        }

        private static byte[] OneValue()
        {
            return CsfSyntheticFixtureFactory.Build(new[]
            {
                CsfSyntheticFixtureFactory.Label(
                    "A",
                    CsfSyntheticFixtureFactory.Normal("B"))
            });
        }

        private static CsfParseResult Read(byte[] input, CsfReadLimits limits = null)
        {
            return WestwoodCsfReader.Read(input, Source(), Provenance(), limits);
        }

        private static CsfDiagnostic AssertFailure(
            CsfParseResult result,
            CsfDiagnosticCode code)
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Document, Is.Null);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(code));
            return result.Diagnostics[0];
        }

        private static CsfReadLimits Limits(
            long maxInputBytes = 1024 * 1024,
            long maxSingleReadBytes = 1024 * 1024,
            long maxAllocatedBytes = 1024 * 1024,
            long maxLabels = 1024,
            long maxTotalValues = 1024,
            long maxValuesPerLabel = 128,
            long maxLabelNameBytes = 1024,
            long maxMainTextCodeUnits = 1024,
            long maxExtraTextBytes = 1024,
            long maxCumulativeUtf16CodeUnits = 4096)
        {
            return new CsfReadLimits(
                maxInputBytes,
                maxSingleReadBytes,
                maxAllocatedBytes,
                maxLabels,
                maxTotalValues,
                maxValuesPerLabel,
                maxLabelNameBytes,
                maxMainTextCodeUnits,
                maxExtraTextBytes,
                maxCumulativeUtf16CodeUnits);
        }

        private static BinarySourceContext Source()
        {
            return new BinarySourceContext(
                "format.csf",
                "synthetic-source",
                LogicalContentPath.Parse("synthetic/strings.csf"));
        }

        private static CsfSourceProvenance Provenance()
        {
            return new CsfSourceProvenance(
                "synthetic-source",
                new[] { LogicalContentPath.Parse("strings.csf") });
        }

        private class CountingStream : MemoryStream
        {
            private readonly bool canSeek;

            public CountingStream(byte[] bytes, bool canSeek = true)
                : base(bytes, false)
            {
                this.canSeek = canSeek;
            }

            public int ReadCalls { get; private set; }

            public override bool CanSeek => canSeek;

            public override int Read(byte[] buffer, int offset, int count)
            {
                ReadCalls++;
                return base.Read(buffer, offset, count);
            }
        }

        private sealed class TrackingStream : MemoryStream
        {
            public TrackingStream(byte[] bytes)
                : base(bytes, false)
            {
            }

            public bool WasDisposed { get; private set; }

            protected override void Dispose(bool disposing)
            {
                WasDisposed = true;
                base.Dispose(disposing);
            }
        }

        private sealed class ThrowingStream : Stream
        {
            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new IOException("private C:\\source must not escape");
            }

            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) =>
                throw new NotSupportedException();
            public override void SetLength(long value) =>
                throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) =>
                throw new NotSupportedException();
        }
    }
}
