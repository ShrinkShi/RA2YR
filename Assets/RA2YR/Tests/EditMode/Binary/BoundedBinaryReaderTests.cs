using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using RA2YR.Core.Binary;
using RA2YR.Core.Content;

namespace RA2YR.Tests.EditMode.Binary
{
    public sealed class BoundedBinaryReaderTests
    {
        [Test]
        public void EmptyInputIsAtEndAndCompletesExactly()
        {
            using (BinaryReadSession session = FromMemory(Array.Empty<byte>()))
            {
                Assert.That(session.Root.Position, Is.Zero);
                Assert.That(session.Root.AbsoluteOffset, Is.Zero);
                Assert.That(session.Root.RemainingLength, Is.Zero);
                Assert.That(session.Root.IsEndOfInput, Is.True);
                Assert.That(
                    session.Root.Complete(TrailingDataPolicy.RequireFullyConsumed).IsComplete,
                    Is.True);
            }
        }

        [Test]
        public void SingleByteBoundaryAndPeekAreExact()
        {
            using (BinaryReadSession session = FromMemory(new byte[] { 0x7f }))
            {
                Assert.That(session.Root.PeekBytes(1, "header")[0], Is.EqualTo(0x7f));
                Assert.That(session.Root.Position, Is.Zero);
                Assert.That(session.Root.ReadUInt8("header"), Is.EqualTo(0x7f));
                Assert.That(session.Root.Position, Is.EqualTo(1));
                Assert.That(session.Root.IsEndOfInput, Is.True);
            }
        }

        [Test]
        public void ZeroLengthReadAndPeekDoNotAdvanceOrConsumeAllocationBudget()
        {
            using (BinaryReadSession session = FromMemory(new byte[] { 0x7f }))
            {
                byte[] read = session.Root.ReadBytes(0, "empty-read");
                byte[] peek = session.Root.PeekBytes(0, "empty-peek");

                Assert.That(read, Is.SameAs(Array.Empty<byte>()));
                Assert.That(peek, Is.SameAs(Array.Empty<byte>()));
                Assert.That(session.Root.Position, Is.Zero);
                Assert.That(session.BudgetUsage.AllocatedBytes, Is.Zero);
            }
        }

        [Test]
        public void ReadOnlyMemorySliceRetainsItsOwnBoundAndLogicalOrigin()
        {
            var memory = new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3, 4 }, 1, 2);
            using (BinaryReadSession session = BinaryReadSession.FromMemory(
                       memory,
                       Source(),
                       Limits(),
                       40))
            {
                Assert.That(session.Root.Length, Is.EqualTo(2));
                Assert.That(session.Root.AbsoluteStartOffset, Is.EqualTo(40));
                Assert.That(session.Root.ReadUInt16("slice"), Is.EqualTo(0x0302));
            }
        }

        [Test]
        public void UInt8ReadsMinimumAndMaximum()
        {
            using (BinaryReadSession session = FromMemory(new byte[] { 0x00, 0xff }))
            {
                Assert.That(session.Root.ReadUInt8("minimum"), Is.EqualTo(byte.MinValue));
                Assert.That(session.Root.ReadUInt8("maximum"), Is.EqualTo(byte.MaxValue));
            }
        }

        [Test]
        public void Int8ReadsMinimumAndMaximum()
        {
            using (BinaryReadSession session = FromMemory(new byte[] { 0x80, 0x7f }))
            {
                Assert.That(session.Root.ReadInt8("minimum"), Is.EqualTo(sbyte.MinValue));
                Assert.That(session.Root.ReadInt8("maximum"), Is.EqualTo(sbyte.MaxValue));
            }
        }

        [Test]
        public void UInt16ReadsMinimumAndMaximum()
        {
            using (BinaryReadSession session = FromMemory(new byte[] { 0, 0, 0xff, 0xff }))
            {
                Assert.That(session.Root.ReadUInt16("minimum"), Is.EqualTo(ushort.MinValue));
                Assert.That(session.Root.ReadUInt16("maximum"), Is.EqualTo(ushort.MaxValue));
            }
        }

        [Test]
        public void Int16ReadsMinimumAndMaximum()
        {
            using (BinaryReadSession session = FromMemory(new byte[] { 0, 0x80, 0xff, 0x7f }))
            {
                Assert.That(session.Root.ReadInt16("minimum"), Is.EqualTo(short.MinValue));
                Assert.That(session.Root.ReadInt16("maximum"), Is.EqualTo(short.MaxValue));
            }
        }

        [Test]
        public void UInt32ReadsMinimumAndMaximum()
        {
            using (BinaryReadSession session = FromMemory(new byte[]
                   {
                       0, 0, 0, 0,
                       0xff, 0xff, 0xff, 0xff
                   }))
            {
                Assert.That(session.Root.ReadUInt32("minimum"), Is.EqualTo(uint.MinValue));
                Assert.That(session.Root.ReadUInt32("maximum"), Is.EqualTo(uint.MaxValue));
            }
        }

        [Test]
        public void Int32ReadsMinimumAndMaximum()
        {
            using (BinaryReadSession session = FromMemory(new byte[]
                   {
                       0, 0, 0, 0x80,
                       0xff, 0xff, 0xff, 0x7f
                   }))
            {
                Assert.That(session.Root.ReadInt32("minimum"), Is.EqualTo(int.MinValue));
                Assert.That(session.Root.ReadInt32("maximum"), Is.EqualTo(int.MaxValue));
            }
        }

        [Test]
        public void UInt64ReadsMinimumAndMaximum()
        {
            using (BinaryReadSession session = FromMemory(new byte[]
                   {
                       0, 0, 0, 0, 0, 0, 0, 0,
                       0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff
                   }))
            {
                Assert.That(session.Root.ReadUInt64("minimum"), Is.EqualTo(ulong.MinValue));
                Assert.That(session.Root.ReadUInt64("maximum"), Is.EqualTo(ulong.MaxValue));
            }
        }

        [Test]
        public void Int64ReadsMinimumAndMaximum()
        {
            using (BinaryReadSession session = FromMemory(new byte[]
                   {
                       0, 0, 0, 0, 0, 0, 0, 0x80,
                       0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x7f
                   }))
            {
                Assert.That(session.Root.ReadInt64("minimum"), Is.EqualTo(long.MinValue));
                Assert.That(session.Root.ReadInt64("maximum"), Is.EqualTo(long.MaxValue));
            }
        }

        [Test]
        public void IntegerReadsAlwaysUseLittleEndianOrder()
        {
            using (BinaryReadSession session = FromMemory(new byte[]
                   {
                       0x78, 0x56, 0x34, 0x12,
                       0xef, 0xcd, 0xab, 0x90, 0x78, 0x56, 0x34, 0x12
                   }))
            {
                Assert.That(session.Root.ReadUInt32("u32"), Is.EqualTo(0x12345678u));
                Assert.That(
                    session.Root.ReadUInt64("u64"),
                    Is.EqualTo(0x1234567890abcdefUL));
            }
        }

        [Test]
        public void ExactReadAndSkipConsumeTheBound()
        {
            using (BinaryReadSession session = FromMemory(new byte[] { 1, 2, 3, 4 }))
            {
                Assert.That(session.Root.ReadBytes(2, "prefix"), Is.EqualTo(new byte[] { 1, 2 }));
                session.Root.Skip(2, "suffix");
                Assert.That(session.Root.Position, Is.EqualTo(4));
                Assert.That(
                    session.Root.Complete(TrailingDataPolicy.RequireFullyConsumed).IsComplete,
                    Is.True);
            }
        }

        [Test]
        public void OneByteShortProducesUnexpectedEndDiagnostic()
        {
            using (BinaryReadSession session = FromMemory(new byte[] { 1, 2, 3 }))
            {
                BinaryReadException exception = Assert.Throws<BinaryReadException>(() =>
                    session.Root.ReadUInt32("header"));

                AssertDiagnostic(
                    exception,
                    BinaryDiagnosticCode.UnexpectedEndOfInput,
                    0,
                    4,
                    3,
                    "header");
            }
        }

        [Test]
        public void StreamSnapshotCompletesAfterMultipleShortReads()
        {
            var stream = new ControlledReadStream(new byte[] { 1, 2, 3, 4 }, 1, false);
            using (BinaryReadSession session = BinaryReadSession.FromStream(
                       stream,
                       4,
                       Source(),
                       Limits(),
                       true))
            {
                Assert.That(session.Root.ReadUInt32("value"), Is.EqualTo(0x04030201u));
                Assert.That(stream.ReadCallCount, Is.EqualTo(4));
            }
        }

        [Test]
        public void StreamSnapshotHonorsMaximumSingleReadBudget()
        {
            var stream = new ControlledReadStream(new byte[] { 1, 2, 3, 4, 5 }, 10, false);
            using (BinaryReadSession session = BinaryReadSession.FromStream(
                       stream,
                       5,
                       Source(),
                       Limits(maxSingleReadBytes: 2),
                       true))
            {
                Assert.That(stream.RequestedCounts, Is.EqualTo(new[] { 2, 2, 1 }));
                Assert.That(
                    Enumerable.Range(0, 5)
                        .Select(item => session.Root.ReadUInt8("byte"))
                        .ToArray(),
                    Is.EqualTo(new byte[] { 1, 2, 3, 4, 5 }));
            }
        }

        [Test]
        public void NonEmptyStreamRejectsZeroByteReadBudgetBeforeAllocation()
        {
            var stream = new ControlledReadStream(new byte[] { 1 }, 1, false);
            BinaryReadException exception = Assert.Throws<BinaryReadException>(() =>
                BinaryReadSession.FromStream(
                    stream,
                    1,
                    Source(),
                    Limits(maxSingleReadBytes: 0),
                    true));

            Assert.That(
                exception.Diagnostic.Code,
                Is.EqualTo(BinaryDiagnosticCode.ReadBudgetExceeded));
            Assert.That(stream.ReadCallCount, Is.Zero);
        }

        [Test]
        public void NonSeekableStreamUsesCallerDeclaredBoundWithoutPositionOrLength()
        {
            var stream = new ControlledReadStream(new byte[] { 9, 8, 7 }, 2, false);
            using (BinaryReadSession session = BinaryReadSession.FromStream(
                       stream,
                       3,
                       Source(),
                       Limits(),
                       true,
                       100))
            {
                Assert.That(session.Root.AbsoluteOffset, Is.EqualTo(100));
                Assert.That(session.Root.RemainingLength, Is.EqualTo(3));
                Assert.That(session.Root.ReadUInt8("value"), Is.EqualTo(9));
                Assert.That(session.Root.AbsoluteOffset, Is.EqualTo(101));
            }
        }

        [Test]
        public void SeekableStreamInfersRemainingBoundAndAbsoluteOffset()
        {
            var stream = new ControlledReadStream(new byte[] { 1, 2, 3, 4 }, 4, true);
            stream.Position = 2;
            using (BinaryReadSession session = BinaryReadSession.FromSeekableStream(
                       stream,
                       Source(),
                       Limits(),
                       true))
            {
                Assert.That(session.Root.AbsoluteStartOffset, Is.EqualTo(2));
                Assert.That(session.Root.Length, Is.EqualTo(2));
                Assert.That(session.Root.ReadUInt16("tail"), Is.EqualTo(0x0403));
            }
        }

        [Test]
        public void NonSeekableInferenceIsExplicitlyUnsupported()
        {
            var stream = new ControlledReadStream(new byte[] { 1 }, 1, false);
            BinaryReadException exception = Assert.Throws<BinaryReadException>(() =>
                BinaryReadSession.FromSeekableStream(stream, Source(), Limits(), true));

            Assert.That(
                exception.Diagnostic.Code,
                Is.EqualTo(BinaryDiagnosticCode.UnsupportedSeekOperation));
            Assert.That(stream.ReadCallCount, Is.Zero);
        }

        [Test]
        public void UnavailablePositionAndLengthAreExplicitlyUnsupported()
        {
            var stream = new ControlledReadStream(new byte[] { 1 }, 1, true)
            {
                ThrowOnPositionOrLength = true
            };
            BinaryReadException exception = Assert.Throws<BinaryReadException>(() =>
                BinaryReadSession.FromSeekableStream(stream, Source(), Limits(), true));

            Assert.That(
                exception.Diagnostic.Code,
                Is.EqualTo(BinaryDiagnosticCode.UnsupportedSeekOperation));
        }

        [Test]
        public void UnavailableSeekCapabilityIsExplicitlyUnsupported()
        {
            var stream = new ControlledReadStream(new byte[] { 1 }, 1, true)
            {
                ThrowOnCanSeek = true
            };
            BinaryReadException exception = Assert.Throws<BinaryReadException>(() =>
                BinaryReadSession.FromSeekableStream(stream, Source(), Limits(), true));

            Assert.That(exception.Diagnostic.Code,
                Is.EqualTo(BinaryDiagnosticCode.UnsupportedSeekOperation));
            Assert.That(stream.ReadCallCount, Is.Zero);
        }

        [Test]
        public void IOExceptionBecomesSanitizedReadFailure()
        {
            var stream = new ControlledReadStream(new byte[] { 1, 2 }, 1, false)
            {
                ThrowOnReadCall = 2
            };
            BinaryReadException exception = Assert.Throws<BinaryReadException>(() =>
                BinaryReadSession.FromStream(stream, 2, Source(), Limits(), true));

            Assert.That(exception.Diagnostic.Code, Is.EqualTo(BinaryDiagnosticCode.ReadFailure));
            Assert.That(exception.Diagnostic.Message, Does.Not.Contain("C:\\private"));
            Assert.That(exception.InnerException, Is.Null);
        }

        [Test]
        public void NegativeLengthIsRejectedBeforeReadOrAllocation()
        {
            var stream = new ControlledReadStream(Array.Empty<byte>(), 1, false);
            BinaryReadException exception = Assert.Throws<BinaryReadException>(() =>
                BinaryReadSession.FromStream(stream, -1, Source(), Limits(), true));

            Assert.That(exception.Diagnostic.Code, Is.EqualTo(BinaryDiagnosticCode.InvalidLength));
            Assert.That(stream.ReadCallCount, Is.Zero);
        }

        [Test]
        public void NegativeReaderLengthDoesNotAdvanceOrAllocate()
        {
            using (BinaryReadSession session = FromMemory(new byte[] { 1 }))
            {
                BinaryReadException exception = Assert.Throws<BinaryReadException>(() =>
                    session.Root.ReadBytes(-1, "payload"));

                Assert.That(exception.Diagnostic.Code,
                    Is.EqualTo(BinaryDiagnosticCode.InvalidLength));
                Assert.That(session.Root.Position, Is.Zero);
                Assert.That(session.BudgetUsage.AllocatedBytes, Is.Zero);
            }
        }

        [Test]
        public void AbsoluteOffsetRangeOverflowIsStructured()
        {
            BinaryReadException exception = Assert.Throws<BinaryReadException>(() =>
                BinaryReadSession.FromMemory(
                    new byte[] { 1 },
                    Source(),
                    Limits(),
                    long.MaxValue));

            Assert.That(
                exception.Diagnostic.Code,
                Is.EqualTo(BinaryDiagnosticCode.ArithmeticOverflow));
        }

        [Test]
        public void SubrangeOffsetPlusLengthOverflowIsStructured()
        {
            using (BinaryReadSession session = FromMemory(new byte[] { 1 }))
            {
                BinaryReadException exception = Assert.Throws<BinaryReadException>(() =>
                    session.Root.CreateSubrangeAt(long.MaxValue, 1, "section"));

                Assert.That(
                    exception.Diagnostic.Code,
                    Is.EqualTo(BinaryDiagnosticCode.ArithmeticOverflow));
            }
        }

        [Test]
        public void OffsetOutsideParentIsInvalidOffset()
        {
            using (BinaryReadSession session = FromMemory(new byte[] { 1 }))
            {
                BinaryReadException exception = Assert.Throws<BinaryReadException>(() =>
                    session.Root.CreateSubrangeAt(2, 0, "section"));

                Assert.That(
                    exception.Diagnostic.Code,
                    Is.EqualTo(BinaryDiagnosticCode.InvalidOffset));
            }
        }

        [Test]
        public void SubrangeBeyondParentIsInvalidSubrange()
        {
            using (BinaryReadSession session = FromMemory(new byte[] { 1, 2, 3 }))
            {
                BinaryReadException exception = Assert.Throws<BinaryReadException>(() =>
                    session.Root.CreateSubrangeAt(1, 3, "section"));

                Assert.That(
                    exception.Diagnostic.Code,
                    Is.EqualTo(BinaryDiagnosticCode.InvalidSubrange));
            }
        }

        [Test]
        public void NestedSubrangesRetainAbsoluteOffsetsAndSharedUsage()
        {
            using (BinaryReadSession session = FromMemory(new byte[] { 0, 1, 2, 3, 4 }))
            {
                BoundedBinaryReader first = session.Root.CreateSubrangeAt(1, 4, "first");
                BoundedBinaryReader second = first.CreateSubrangeAt(1, 2, "second");

                Assert.That(second.AbsoluteStartOffset, Is.EqualTo(2));
                Assert.That(second.ReadUInt16("value"), Is.EqualTo(0x0302));
                Assert.That(session.BudgetUsage.Subranges, Is.EqualTo(2));
                Assert.That(session.BudgetUsage.DeepestNesting, Is.EqualTo(2));
            }
        }

        [Test]
        public void ChildDepthOverflowProducesStructuredArithmeticDiagnostic()
        {
            using (BinaryReadSession session = FromMemory(new byte[] { 1 }))
            {
                var extremeDepthReader = new BoundedBinaryReader(
                    session,
                    0,
                    1,
                    0,
                    int.MaxValue,
                    null);

                BinaryReadException exception = Assert.Throws<BinaryReadException>(() =>
                    extremeDepthReader.CreateSubrangeAt(0, 1, "child"));

                Assert.That(exception.Diagnostic.Code,
                    Is.EqualTo(BinaryDiagnosticCode.ArithmeticOverflow));
            }
        }

        [Test]
        public void ChildReaderCannotReadPastParentSubrange()
        {
            using (BinaryReadSession session = FromMemory(new byte[] { 1, 2, 3, 4 }))
            {
                BoundedBinaryReader child = session.Root.CreateSubrangeAt(1, 2, "child");
                BinaryReadException exception = Assert.Throws<BinaryReadException>(() =>
                    child.ReadBytes(3, "child-data"));

                AssertDiagnostic(
                    exception,
                    BinaryDiagnosticCode.UnexpectedEndOfInput,
                    1,
                    3,
                    2,
                    "child-data");
            }
        }

        [Test]
        public void ParentCannotCompleteUntilReadSubrangeCompletes()
        {
            using (BinaryReadSession session = FromMemory(new byte[] { 1, 2 }))
            {
                BoundedBinaryReader child = session.Root.ReadSubrange(2, "child");
                Assert.That(session.Root.IsEndOfInput, Is.True);

                BinaryReadException exception = Assert.Throws<BinaryReadException>(() =>
                    session.Root.Complete(TrailingDataPolicy.RequireFullyConsumed));

                Assert.That(exception.Diagnostic.Code,
                    Is.EqualTo(BinaryDiagnosticCode.InvalidSubrange));
                Assert.That(session.IsFaulted, Is.True);
                Assert.That(child.Position, Is.Zero);
            }
        }

        [Test]
        public void CompletedReadSubrangeAllowsParentCompletion()
        {
            using (BinaryReadSession session = FromMemory(new byte[] { 1, 2 }))
            {
                BoundedBinaryReader child = session.Root.ReadSubrange(2, "child");
                child.ReadUInt16("value");
                Assert.That(
                    child.Complete(TrailingDataPolicy.RequireFullyConsumed).IsComplete,
                    Is.True);
                Assert.That(
                    session.Root.Complete(TrailingDataPolicy.RequireFullyConsumed).IsComplete,
                    Is.True);
            }
        }

        [Test]
        public void CompletedChildRejectsMutationWhileParentRemainsUsable()
        {
            using (BinaryReadSession session = FromMemory(new byte[] { 1, 2 }))
            {
                BoundedBinaryReader child = session.Root.ReadSubrange(1, "child");
                child.ReadUInt8("child-value");
                child.Complete(TrailingDataPolicy.RequireFullyConsumed);

                Assert.Throws<InvalidOperationException>(() =>
                    child.ReadUInt8("after-complete"));
                Assert.Throws<InvalidOperationException>(() =>
                    child.CreateSubrangeAt(0, 0, "after-complete"));
                Assert.That(session.Root.ReadUInt8("parent-value"), Is.EqualTo(2));
                Assert.That(
                    session.Root.Complete(TrailingDataPolicy.RequireFullyConsumed).IsComplete,
                    Is.True);
            }
        }

        [Test]
        public void PerReadBudgetIsIndependentFromEof()
        {
            using (BinaryReadSession session = FromMemory(
                       new byte[] { 1, 2, 3 },
                       Limits(maxSingleReadBytes: 2)))
            {
                BinaryReadException exception = Assert.Throws<BinaryReadException>(() =>
                    session.Root.ReadBytes(3, "payload"));

                Assert.That(
                    exception.Diagnostic.Code,
                    Is.EqualTo(BinaryDiagnosticCode.ReadBudgetExceeded));
            }
        }

        [Test]
        public void AllocationBudgetIsCumulativeAcrossOperations()
        {
            using (BinaryReadSession session = FromMemory(
                       new byte[] { 1, 2, 3, 4 },
                       Limits(maxAllocatedBytes: 3)))
            {
                session.Root.ReadBytes(2, "first");
                BinaryReadException exception = Assert.Throws<BinaryReadException>(() =>
                    session.Root.PeekBytes(2, "second"));

                Assert.That(
                    exception.Diagnostic.Code,
                    Is.EqualTo(BinaryDiagnosticCode.AllocationBudgetExceeded));
                Assert.That(session.BudgetUsage.AllocatedBytes, Is.EqualTo(2));
            }
        }

        [Test]
        public void AllocationLedgerCannotBeReducedByANegativeReservation()
        {
            using (BinaryReadSession session = FromMemory(new byte[] { 1 }))
            {
                BinaryReadException exception = Assert.Throws<BinaryReadException>(() =>
                    session.ReserveAllocation(-1, 0, 1, "allocation"));

                Assert.That(exception.Diagnostic.Code,
                    Is.EqualTo(BinaryDiagnosticCode.InvalidLength));
                Assert.That(session.BudgetUsage.AllocatedBytes, Is.Zero);
            }
        }

        [Test]
        public void StreamSnapshotAndReadBytesShareCumulativeAllocationBudget()
        {
            var stream = new ControlledReadStream(new byte[] { 1, 2, 3, 4 }, 4, false);
            using (BinaryReadSession session = BinaryReadSession.FromStream(
                       stream,
                       4,
                       Source(),
                       Limits(maxAllocatedBytes: 5),
                       true))
            {
                BinaryReadException exception = Assert.Throws<BinaryReadException>(() =>
                    session.Root.ReadBytes(2, "copy"));

                Assert.That(exception.Diagnostic.Code,
                    Is.EqualTo(BinaryDiagnosticCode.AllocationBudgetExceeded));
                Assert.That(session.BudgetUsage.AllocatedBytes, Is.EqualTo(4));
                Assert.That(session.Root.Position, Is.Zero);
            }
        }

        [Test]
        public void StreamSnapshotAllocationBudgetFailsBeforeReading()
        {
            var stream = new ControlledReadStream(new byte[] { 1, 2, 3, 4 }, 4, false);
            BinaryReadException exception = Assert.Throws<BinaryReadException>(() =>
                BinaryReadSession.FromStream(
                    stream,
                    4,
                    Source(),
                    Limits(maxAllocatedBytes: 3),
                    true));

            Assert.That(exception.Diagnostic.Code,
                Is.EqualTo(BinaryDiagnosticCode.AllocationBudgetExceeded));
            Assert.That(stream.ReadCallCount, Is.Zero);
        }

        [Test]
        public void RecordBudgetIsCumulativeAcrossSubranges()
        {
            using (BinaryReadSession session = FromMemory(
                       new byte[] { 1 },
                       Limits(maxRecords: 2)))
            {
                BoundedBinaryReader child = session.Root.CreateSubrangeAt(0, 1, "child");
                session.Root.ReserveRecords(1, "root-count");
                child.ReserveRecords(1, "child-count");
                BinaryReadException exception = Assert.Throws<BinaryReadException>(() =>
                    child.ReserveRecords(1, "overflow-count"));

                Assert.That(
                    exception.Diagnostic.Code,
                    Is.EqualTo(BinaryDiagnosticCode.RecordBudgetExceeded));
            }
        }

        [Test]
        public void StringLengthBudgetIsValidatedBeforeDecodeOrAllocation()
        {
            using (BinaryReadSession session = FromMemory(
                       new byte[] { 1, 2, 3, 4 },
                       Limits(maxStringLength: 3)))
            {
                BinaryReadException exception = Assert.Throws<BinaryReadException>(() =>
                    session.Root.ValidateStringLength(4, "name"));

                Assert.That(
                    exception.Diagnostic.Code,
                    Is.EqualTo(BinaryDiagnosticCode.StringBudgetExceeded));
                Assert.That(session.BudgetUsage.AllocatedBytes, Is.Zero);
            }
        }

        [Test]
        public void NestingBudgetCannotBeResetByAChildReader()
        {
            using (BinaryReadSession session = FromMemory(
                       new byte[] { 1, 2 },
                       Limits(maxNestingDepth: 1)))
            {
                BoundedBinaryReader child = session.Root.CreateSubrangeAt(0, 2, "child");
                BinaryReadException exception = Assert.Throws<BinaryReadException>(() =>
                    child.CreateSubrangeAt(0, 1, "grandchild"));

                Assert.That(
                    exception.Diagnostic.Code,
                    Is.EqualTo(BinaryDiagnosticCode.NestingBudgetExceeded));
            }
        }

        [Test]
        public void SubrangeCountBudgetCannotBeResetByAChildReader()
        {
            using (BinaryReadSession session = FromMemory(
                       new byte[] { 1, 2 },
                       Limits(maxSubranges: 1)))
            {
                BoundedBinaryReader child = session.Root.CreateSubrangeAt(0, 1, "child");
                BinaryReadException exception = Assert.Throws<BinaryReadException>(() =>
                    child.CreateSubrangeAt(0, 1, "second"));

                Assert.That(
                    exception.Diagnostic.Code,
                    Is.EqualTo(BinaryDiagnosticCode.SubrangeBudgetExceeded));
            }
        }

        [Test]
        public void TrailingDataCanBeRejectedAsError()
        {
            using (BinaryReadSession session = FromMemory(new byte[] { 1, 2 }))
            {
                session.Root.ReadUInt8("value");
                BinaryParseCompletion completion = session.Root.Complete(
                    TrailingDataPolicy.RequireFullyConsumed);

                Assert.That(completion.IsComplete, Is.False);
                Assert.That(completion.HasErrors, Is.True);
                Assert.That(
                    completion.Diagnostics.Single().Code,
                    Is.EqualTo(BinaryDiagnosticCode.TrailingData));
            }
        }

        [Test]
        public void TrailingDataCanBeAllowedWithWarning()
        {
            using (BinaryReadSession session = FromMemory(new byte[] { 1, 2 }))
            {
                session.Root.ReadUInt8("value");
                BinaryParseCompletion completion = session.Root.Complete(
                    TrailingDataPolicy.AllowWithWarning);

                Assert.That(completion.IsComplete, Is.True);
                Assert.That(completion.HasErrors, Is.False);
                Assert.That(completion.Diagnostics.Single().Severity,
                    Is.EqualTo(BinaryDiagnosticSeverity.Warning));
                Assert.That(session.Root.IsEndOfInput, Is.True);
            }
        }

        [Test]
        public void ChildCompletionsDoNotCopyTheGrowingSessionDiagnosticHistory()
        {
            using (BinaryReadSession session = FromMemory(
                       new byte[] { 1 },
                       Limits(maxSubranges: 300)))
            {
                IReadOnlyList<BinaryDiagnostic> sessionView = session.Diagnostics;
                for (int index = 0; index < 256; index++)
                {
                    BoundedBinaryReader child = session.Root.CreateSubrangeAt(
                        0,
                        1,
                        "child");
                    BinaryParseCompletion childCompletion = child.Complete(
                        TrailingDataPolicy.AllowWithWarning);

                    Assert.That(childCompletion.Diagnostics, Has.Count.EqualTo(1));
                    Assert.That(session.Diagnostics, Is.SameAs(sessionView));
                }

                BinaryParseCompletion rootCompletion = session.Root.Complete(
                    TrailingDataPolicy.AllowWithWarning);
                Assert.That(rootCompletion.Diagnostics, Has.Count.EqualTo(257));
                Assert.That(rootCompletion.Diagnostics, Is.SameAs(sessionView));
            }
        }

        [Test]
        public void TrailingDataCanBePreservedAsOpaqueBytes()
        {
            using (BinaryReadSession session = FromMemory(new byte[] { 1, 2, 3 }))
            {
                session.Root.ReadUInt8("value");
                BinaryParseCompletion completion = session.Root.Complete(
                    TrailingDataPolicy.PreserveOpaque);

                Assert.That(completion.IsComplete, Is.True);
                Assert.That(completion.OpaqueTrailingData.ToArray(),
                    Is.EqualTo(new byte[] { 2, 3 }));
                Assert.That(session.BudgetUsage.AllocatedBytes, Is.EqualTo(2));
            }
        }

        [Test]
        public void FormatDefinedTrailingDataRequiresAnExplicitLaterDecision()
        {
            using (BinaryReadSession session = FromMemory(new byte[] { 1, 2 }))
            {
                session.Root.ReadUInt8("value");
                BinaryParseCompletion deferred = session.Root.Complete(
                    TrailingDataPolicy.DeferToFormat);

                Assert.That(deferred.IsComplete, Is.False);
                Assert.That(deferred.RequiresTrailingDataDecision, Is.True);
                Assert.That(session.Root.RemainingLength, Is.EqualTo(1));
                Assert.That(
                    session.Root.Complete(TrailingDataPolicy.AllowWithWarning).IsComplete,
                    Is.True);
            }
        }

        [Test]
        public void MemoryAndStreamInputsProduceTheSameValuesAndOffsets()
        {
            byte[] bytes = { 0x34, 0x12, 0x78, 0x56, 0x34, 0x12 };
            using (BinaryReadSession memory = FromMemory(bytes))
            using (BinaryReadSession stream = BinaryReadSession.FromStream(
                       new ControlledReadStream(bytes, 2, false),
                       bytes.Length,
                       Source(),
                       Limits(),
                       false))
            {
                Assert.That(memory.Root.ReadUInt16("a"), Is.EqualTo(stream.Root.ReadUInt16("a")));
                Assert.That(memory.Root.ReadUInt32("b"), Is.EqualTo(stream.Root.ReadUInt32("b")));
                Assert.That(memory.Root.AbsoluteOffset, Is.EqualTo(stream.Root.AbsoluteOffset));
                Assert.That(memory.Root.RemainingLength, Is.EqualTo(stream.Root.RemainingLength));
            }
        }

        [Test]
        public void DifferentStreamChunkSizesProduceTheSameSnapshot()
        {
            byte[] bytes = Enumerable.Range(0, 31).Select(item => (byte)item).ToArray();
            using (BinaryReadSession one = BinaryReadSession.FromStream(
                       new ControlledReadStream(bytes, 1, false),
                       bytes.Length,
                       Source(),
                       Limits(),
                       false))
            using (BinaryReadSession seven = BinaryReadSession.FromStream(
                       new ControlledReadStream(bytes, 7, false),
                       bytes.Length,
                       Source(),
                       Limits(),
                       false))
            {
                Assert.That(one.Root.ReadBytes(bytes.Length, "all"),
                    Is.EqualTo(seven.Root.ReadBytes(bytes.Length, "all")));
            }
        }

        [Test]
        public void DiagnosticOffsetUsesLogicalAbsoluteOriginAndCurrentPosition()
        {
            using (BinaryReadSession session = BinaryReadSession.FromMemory(
                       new byte[] { 1, 2 },
                       Source(),
                       Limits(),
                       500))
            {
                session.Root.Skip(1, "prefix");
                BinaryReadException exception = Assert.Throws<BinaryReadException>(() =>
                    session.Root.ReadUInt16("field"));

                AssertDiagnostic(
                    exception,
                    BinaryDiagnosticCode.UnexpectedEndOfInput,
                    501,
                    2,
                    1,
                    "field");
                Assert.That(exception.Diagnostic.Source.ParserName,
                    Is.EqualTo("format.bounded-reader"));
                Assert.That(exception.Diagnostic.Source.LogicalSourceId,
                    Is.EqualTo("synthetic-source"));
                Assert.That(exception.Diagnostic.Source.LogicalPath.Value,
                    Is.EqualTo("Synthetic/bounded.bin"));
            }
        }

        [Test]
        public void CompletionAndDiagnosticsCannotBeForgedByPublicConstructors()
        {
            Assert.That(typeof(BinaryReadSession).IsPublic, Is.False);
            Assert.That(typeof(BoundedBinaryReader).IsPublic, Is.False);
            Assert.That(typeof(BinaryParseCompletion).IsPublic, Is.False);
            Assert.That(typeof(TrailingDataPolicy).IsPublic, Is.False);
            Assert.That(typeof(BinarySourceContext).IsPublic, Is.True);
            Assert.That(typeof(BinarySourceContext).GetConstructors(), Is.Empty);
            Assert.That(typeof(BinaryParseCompletion).GetConstructors(), Is.Empty);
            Assert.That(typeof(BinaryDiagnostic).GetConstructors(), Is.Empty);
            Assert.That(typeof(BinaryReadException).GetConstructors(), Is.Empty);
            Assert.That(typeof(BinaryReadSession).GetConstructors(), Is.Empty);
            Assert.That(typeof(BoundedBinaryReader).GetConstructors(), Is.Empty);
            Assert.That(typeof(BinaryParseCompletion).GetProperty("IsComplete").CanWrite,
                Is.False);
        }

        [Test]
        public void FaultedSessionRejectsFurtherReadsWithoutDuplicatingDiagnostics()
        {
            using (BinaryReadSession session = FromMemory(new byte[] { 1 }))
            {
                Assert.Throws<BinaryReadException>(() =>
                    session.Root.ReadUInt16("damaged"));
                Assert.That(session.IsFaulted, Is.True);
                Assert.That(session.Diagnostics, Has.Count.EqualTo(1));

                Assert.Throws<InvalidOperationException>(() =>
                    session.Root.ReadUInt8("retry"));
                Assert.That(session.Diagnostics, Has.Count.EqualTo(1));
                Assert.That(session.Root.Position, Is.Zero);
            }
        }

        [Test]
        public void CompletedRootClosesTheSessionAndReader()
        {
            using (BinaryReadSession session = FromMemory(new byte[] { 1 }))
            {
                session.Root.ReadUInt8("value");
                BinaryParseCompletion completion = session.Root.Complete(
                    TrailingDataPolicy.RequireFullyConsumed);

                Assert.That(completion.IsComplete, Is.True);
                Assert.That(session.IsCompleted, Is.True);
                Assert.Throws<InvalidOperationException>(() =>
                    session.Root.ReadBytes(0, "after-complete"));
                Assert.Throws<InvalidOperationException>(() =>
                    session.Root.CreateSubrangeAt(0, 0, "after-complete"));
                Assert.Throws<InvalidOperationException>(() =>
                    session.Root.ReserveRecords(0, "after-complete"));
            }
        }

        [Test]
        public void SessionRejectsMutationFromForeignThread()
        {
            using (BinaryReadSession session = FromMemory(new byte[] { 1 }))
            {
                Exception observed = null;
                var thread = new Thread(() =>
                {
                    try
                    {
                        session.Root.ReadUInt8("foreign-thread");
                    }
                    catch (Exception exception)
                    {
                        observed = exception;
                    }
                });
                thread.Start();
                Assert.That(thread.Join(5000), Is.True);

                Assert.That(observed, Is.TypeOf<InvalidOperationException>());
                Assert.That(session.Root.Position, Is.Zero);
                Assert.That(session.BudgetUsage.AllocatedBytes, Is.Zero);
            }
        }

        [Test]
        public void DiagnosticLabelsCannotCarryPhysicalPaths()
        {
            Assert.Throws<ArgumentException>(() => new BinarySourceContext(
                "C:\\parser",
                "synthetic-source",
                LogicalContentPath.Parse("Synthetic/bounded.bin")));
            Assert.Throws<ArgumentException>(() => new BinarySourceContext(
                "format.bounded-reader",
                "..\\source",
                LogicalContentPath.Parse("Synthetic/bounded.bin")));

            using (BinaryReadSession session = FromMemory(new byte[] { 1 }))
            {
                Assert.Throws<ArgumentException>(() =>
                    session.Root.ReadUInt8("C:\\field"));
                Assert.That(session.Root.Position, Is.Zero);
                Assert.That(session.Diagnostics, Is.Empty);
            }
        }

        [Test]
        public void ZeroLengthReadResultStopsSnapshotLoopAsEof()
        {
            var stream = new ControlledReadStream(new byte[] { 1 }, 1, false)
            {
                ReturnZeroOnReadCall = 1
            };
            BinaryReadException exception = Assert.Throws<BinaryReadException>(() =>
                BinaryReadSession.FromStream(stream, 1, Source(), Limits(), true));

            Assert.That(
                exception.Diagnostic.Code,
                Is.EqualTo(BinaryDiagnosticCode.UnexpectedEndOfInput));
            Assert.That(stream.ReadCallCount, Is.EqualTo(1));
        }

        [Test]
        public void InvalidStreamReadCountProducesReadFailureWithoutLooping()
        {
            var stream = new ControlledReadStream(new byte[] { 1 }, 1, false)
            {
                ReturnTooManyOnReadCall = 1
            };
            BinaryReadException exception = Assert.Throws<BinaryReadException>(() =>
                BinaryReadSession.FromStream(stream, 1, Source(), Limits(), true));

            Assert.That(exception.Diagnostic.Code,
                Is.EqualTo(BinaryDiagnosticCode.ReadFailure));
            Assert.That(stream.ReadCallCount, Is.EqualTo(1));
        }

        [Test]
        public void OversizedDeclaredInputFailsBeforeReadOrAllocation()
        {
            var stream = new ControlledReadStream(Array.Empty<byte>(), 1, false);
            BinaryReadException exception = Assert.Throws<BinaryReadException>(() =>
                BinaryReadSession.FromStream(
                    stream,
                    int.MaxValue,
                    Source(),
                    Limits(maxInputBytes: 16),
                    true));

            Assert.That(
                exception.Diagnostic.Code,
                Is.EqualTo(BinaryDiagnosticCode.InputBudgetExceeded));
            Assert.That(stream.ReadCallCount, Is.Zero);
        }

        [Test]
        public void SessionHonorsStreamOwnershipOnSuccess()
        {
            var owned = new ControlledReadStream(new byte[] { 1 }, 1, false);
            BinaryReadSession ownedSession = BinaryReadSession.FromStream(
                owned, 1, Source(), Limits(), false);
            ownedSession.Dispose();
            Assert.That(owned.WasDisposed, Is.True);

            var borrowed = new ControlledReadStream(new byte[] { 1 }, 1, false);
            BinaryReadSession borrowedSession = BinaryReadSession.FromStream(
                borrowed, 1, Source(), Limits(), true);
            borrowedSession.Dispose();
            Assert.That(borrowed.WasDisposed, Is.False);
            borrowed.Dispose();
        }

        [Test]
        public void SessionHonorsStreamOwnershipOnFactoryFailure()
        {
            var owned = new ControlledReadStream(new byte[] { 1 }, 1, false);
            Assert.Throws<BinaryReadException>(() => BinaryReadSession.FromStream(
                owned, 2, Source(), Limits(), false));
            Assert.That(owned.WasDisposed, Is.True);

            var borrowed = new ControlledReadStream(new byte[] { 1 }, 1, false);
            Assert.Throws<BinaryReadException>(() => BinaryReadSession.FromStream(
                borrowed, 2, Source(), Limits(), true));
            Assert.That(borrowed.WasDisposed, Is.False);
            borrowed.Dispose();
        }

        [Test]
        public void SeekableFactoryFailureDisposesOwnedStreamExactlyOnce()
        {
            var stream = new ControlledReadStream(new byte[] { 1 }, 1, true)
            {
                ThrowOnReadCall = 1
            };

            Assert.Throws<BinaryReadException>(() =>
                BinaryReadSession.FromSeekableStream(
                    stream,
                    Source(),
                    Limits(),
                    false));

            Assert.That(stream.DisposeCallCount, Is.EqualTo(1));
        }

        private static BinaryReadSession FromMemory(
            byte[] bytes,
            BinaryReadLimits limits = null)
        {
            return BinaryReadSession.FromMemory(bytes, Source(), limits ?? Limits());
        }

        private static BinarySourceContext Source()
        {
            return new BinarySourceContext(
                "format.bounded-reader",
                "synthetic-source",
                LogicalContentPath.Parse("Synthetic/bounded.bin"));
        }

        private static BinaryReadLimits Limits(
            long maxInputBytes = 1024,
            long maxSingleReadBytes = 1024,
            long maxAllocatedBytes = 2048,
            long maxRecords = 1024,
            long maxStringLength = 1024,
            int maxNestingDepth = 16,
            long maxSubranges = 1024)
        {
            return new BinaryReadLimits(
                maxInputBytes,
                maxSingleReadBytes,
                maxAllocatedBytes,
                maxRecords,
                maxStringLength,
                maxNestingDepth,
                maxSubranges);
        }

        private static void AssertDiagnostic(
            BinaryReadException exception,
            BinaryDiagnosticCode code,
            long offset,
            long requested,
            long remaining,
            string field)
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception.Diagnostic.Code, Is.EqualTo(code));
            Assert.That(exception.Diagnostic.AbsoluteOffset, Is.EqualTo(offset));
            Assert.That(exception.Diagnostic.RequestedLength, Is.EqualTo(requested));
            Assert.That(exception.Diagnostic.RemainingLength, Is.EqualTo(remaining));
            Assert.That(exception.Diagnostic.FieldOrSection, Is.EqualTo(field));
            Assert.That(exception.Diagnostic.Message, Does.Not.Contain("Synthetic body"));
        }

        private sealed class ControlledReadStream : Stream
        {
            private readonly byte[] data;
            private readonly int maxChunk;
            private readonly bool canSeek;
            private long position;

            public ControlledReadStream(byte[] data, int maxChunk, bool canSeek)
            {
                this.data = data ?? throw new ArgumentNullException(nameof(data));
                this.maxChunk = maxChunk;
                this.canSeek = canSeek;
            }

            public int ReadCallCount { get; private set; }

            public List<int> RequestedCounts { get; } = new List<int>();

            public int ThrowOnReadCall { get; set; }

            public int ReturnZeroOnReadCall { get; set; }

            public int ReturnTooManyOnReadCall { get; set; }

            public bool ThrowOnPositionOrLength { get; set; }

            public bool ThrowOnCanSeek { get; set; }

            public bool WasDisposed { get; private set; }

            public int DisposeCallCount { get; private set; }

            public override bool CanRead => !WasDisposed;

            public override bool CanSeek
            {
                get
                {
                    if (ThrowOnCanSeek)
                    {
                        throw new NotSupportedException();
                    }

                    return canSeek && !WasDisposed;
                }
            }

            public override bool CanWrite => false;

            public override long Length
            {
                get
                {
                    EnsureSeekMetadata();
                    return data.Length;
                }
            }

            public override long Position
            {
                get
                {
                    EnsureSeekMetadata();
                    return position;
                }
                set
                {
                    EnsureSeekMetadata();
                    if (value < 0 || value > data.Length)
                    {
                        throw new ArgumentOutOfRangeException(nameof(value));
                    }

                    position = value;
                }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (WasDisposed)
                {
                    throw new ObjectDisposedException(nameof(ControlledReadStream));
                }

                ReadCallCount++;
                RequestedCounts.Add(count);
                if (ThrowOnReadCall == ReadCallCount)
                {
                    throw new IOException("Synthetic failure at C:\\private\\source.bin");
                }

                if (ReturnZeroOnReadCall == ReadCallCount)
                {
                    return 0;
                }

                if (ReturnTooManyOnReadCall == ReadCallCount)
                {
                    return count + 1;
                }

                int remaining = data.Length - checked((int)position);
                int read = Math.Min(Math.Min(count, maxChunk), remaining);
                if (read == 0)
                {
                    return 0;
                }

                Array.Copy(data, position, buffer, offset, read);
                position += read;
                return read;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                EnsureSeekMetadata();
                long basis = origin == SeekOrigin.Begin
                    ? 0
                    : origin == SeekOrigin.Current
                        ? position
                        : data.Length;
                Position = checked(basis + offset);
                return position;
            }

            public override void Flush()
            {
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }

            protected override void Dispose(bool disposing)
            {
                DisposeCallCount++;
                WasDisposed = true;
                base.Dispose(disposing);
            }

            private void EnsureSeekMetadata()
            {
                if (!canSeek || ThrowOnPositionOrLength)
                {
                    throw new NotSupportedException();
                }

                if (WasDisposed)
                {
                    throw new ObjectDisposedException(nameof(ControlledReadStream));
                }
            }
        }
    }
}
