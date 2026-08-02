using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using NUnit.Framework;
using RA2YR.Core.Binary;
using RA2YR.Core.Binary.Seekable;
using RA2YR.Core.Content;

namespace RA2YR.Tests.EditMode.Binary
{
    public sealed class ReadOnlyDataWindowTests
    {
        [Test]
        public void OpeningWindowDoesNotSnapshotBackingStream()
        {
            var stream = new ControlledSeekableStream(Bytes(16), 2);
            using (ReadOnlyDataWindowSession session = Session(stream, 2, 10))
            {
                Assert.That(stream.ReadCallCount, Is.Zero);
                Assert.That(session.Root.AbsoluteStartOffset, Is.EqualTo(2));
                Assert.That(session.Root.Length, Is.EqualTo(10));
            }
        }

        [Test]
        public void ExactReadUsesExplicitRootOffsetAndLength()
        {
            using (ReadOnlyDataWindowSession session = Session(
                       new ControlledSeekableStream(Bytes(16), 16),
                       4,
                       5))
            {
                var result = new byte[3];
                session.Root.ReadExactly(1, result, 0, result.Length, "payload");

                Assert.That(result, Is.EqualTo(new byte[] { 5, 6, 7 }));
                Assert.That(session.BudgetUsage.BytesRead, Is.EqualTo(3));
            }
        }

        [Test]
        public void ExactReadLoopsAcrossShortReads()
        {
            var stream = new ControlledSeekableStream(Bytes(10), 2);
            using (ReadOnlyDataWindowSession session = Session(stream, 0, 10))
            {
                var result = new byte[7];
                session.Root.ReadExactly(1, result, 0, result.Length, "payload");

                Assert.That(result, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6, 7 }));
                Assert.That(stream.ReadCallCount, Is.EqualTo(4));
            }
        }

        [Test]
        public void NestedChildIsRestrictedToEveryParentBoundary()
        {
            using (ReadOnlyDataWindowSession session = Session(
                       new ControlledSeekableStream(Bytes(20), 20),
                       2,
                       16))
            {
                ReadOnlyDataWindow child = session.Root.CreateChild(3, 8, "child");
                ReadOnlyDataWindow nested = child.CreateChild(2, 3, "nested");
                var result = new byte[3];
                nested.ReadExactly(0, result, 0, result.Length, "payload");

                Assert.That(nested.AbsoluteStartOffset, Is.EqualTo(7));
                Assert.That(result, Is.EqualTo(new byte[] { 7, 8, 9 }));
                Assert.That(session.BudgetUsage.WindowsCreated, Is.EqualTo(3));
                Assert.That(session.BudgetUsage.DeepestWindow, Is.EqualTo(2));
            }
        }

        [Test]
        public void ChildCrossingParentFailsWithStructuredDiagnostic()
        {
            using (ReadOnlyDataWindowSession session = Session(
                       new ControlledSeekableStream(Bytes(10), 10),
                       0,
                       10))
            {
                ReadOnlyDataWindow child = session.Root.CreateChild(2, 4, "child");
                BinaryReadException exception = Assert.Throws<BinaryReadException>(() =>
                    child.CreateChild(3, 2, "nested"));

                AssertDiagnostic(
                    exception,
                    BinaryDiagnosticCode.InvalidSubrange,
                    5,
                    2,
                    1,
                    "nested");
            }
        }

        [Test]
        public void ChildOffsetOverflowFailsBeforeBackingAccess()
        {
            var stream = new ControlledSeekableStream(Bytes(10), 10);
            using (ReadOnlyDataWindowSession session = Session(stream, 0, 10))
            {
                BinaryReadException exception = Assert.Throws<BinaryReadException>(() =>
                    session.Root.CreateChild(long.MaxValue, 1, "child"));

                Assert.That(
                    exception.Diagnostic.Code,
                    Is.EqualTo(BinaryDiagnosticCode.ArithmeticOverflow));
                Assert.That(stream.ReadCallCount, Is.Zero);
            }
        }

        [Test]
        public void ReadCannotCrossWindowEnd()
        {
            var stream = new ControlledSeekableStream(Bytes(10), 10);
            using (ReadOnlyDataWindowSession session = Session(stream, 2, 4))
            {
                BinaryReadException exception = Assert.Throws<BinaryReadException>(() =>
                    session.Root.ReadExactly(3, new byte[2], 0, 2, "payload"));

                AssertDiagnostic(
                    exception,
                    BinaryDiagnosticCode.UnexpectedEndOfInput,
                    5,
                    2,
                    1,
                    "payload");
                Assert.That(stream.ReadCallCount, Is.Zero);
            }
        }

        [Test]
        public void RootRangeCannotCrossBackingStream()
        {
            var stream = new ControlledSeekableStream(Bytes(4), 4);
            BinaryReadException exception = Assert.Throws<BinaryReadException>(() =>
                ReadOnlyDataWindowSession.FromSeekableStream(
                    stream,
                    Source(),
                    3,
                    2,
                    Limits(),
                    true));

            Assert.That(
                exception.Diagnostic.Code,
                Is.EqualTo(BinaryDiagnosticCode.InvalidSubrange));
            Assert.That(stream.ReadCallCount, Is.Zero);
        }

        [Test]
        public void WindowCountAndDepthBudgetsAreEnforced()
        {
            var countLimits = Limits(maxWindows: 1);
            using (ReadOnlyDataWindowSession session = Session(
                       new ControlledSeekableStream(Bytes(4), 4),
                       0,
                       4,
                       countLimits))
            {
                BinaryReadException exception = Assert.Throws<BinaryReadException>(() =>
                    session.Root.CreateChild(0, 1, "child"));
                Assert.That(
                    exception.Diagnostic.Code,
                    Is.EqualTo(BinaryDiagnosticCode.SubrangeBudgetExceeded));
            }

            var depthLimits = Limits(maxWindowDepth: 0);
            using (ReadOnlyDataWindowSession session = Session(
                       new ControlledSeekableStream(Bytes(4), 4),
                       0,
                       4,
                       depthLimits))
            {
                BinaryReadException exception = Assert.Throws<BinaryReadException>(() =>
                    session.Root.CreateChild(0, 1, "child"));
                Assert.That(
                    exception.Diagnostic.Code,
                    Is.EqualTo(BinaryDiagnosticCode.NestingBudgetExceeded));
            }
        }

        [Test]
        public void SingleAndCumulativeReadBudgetsAreEnforcedBeforeRead()
        {
            var stream = new ControlledSeekableStream(Bytes(10), 10);
            using (ReadOnlyDataWindowSession session = Session(
                       stream,
                       0,
                       10,
                       Limits(maxSingleReadBytes: 2)))
            {
                BinaryReadException exception = Assert.Throws<BinaryReadException>(() =>
                    session.Root.ReadExactly(0, new byte[3], 0, 3, "payload"));
                Assert.That(
                    exception.Diagnostic.Code,
                    Is.EqualTo(BinaryDiagnosticCode.ReadBudgetExceeded));
                Assert.That(stream.ReadCallCount, Is.Zero);
            }

            stream = new ControlledSeekableStream(Bytes(10), 10);
            using (ReadOnlyDataWindowSession session = Session(
                       stream,
                       0,
                       10,
                       Limits(maxTotalReadBytes: 3)))
            {
                session.Root.ReadExactly(0, new byte[2], 0, 2, "first");
                BinaryReadException exception = Assert.Throws<BinaryReadException>(() =>
                    session.Root.ReadExactly(2, new byte[2], 0, 2, "second"));
                Assert.That(
                    exception.Diagnostic.Code,
                    Is.EqualTo(BinaryDiagnosticCode.ReadBudgetExceeded));
                Assert.That(stream.ReadCallCount, Is.EqualTo(1));
            }
        }

        [Test]
        public void StreamOwnershipHonorsLeaveOpen()
        {
            var owned = new ControlledSeekableStream(Bytes(2), 2);
            ReadOnlyDataWindowSession ownedSession = Session(owned, 0, 2, leaveOpen: false);
            ownedSession.Dispose();
            Assert.That(owned.WasDisposed, Is.True);

            var borrowed = new ControlledSeekableStream(Bytes(2), 2);
            ReadOnlyDataWindowSession borrowedSession = Session(
                borrowed,
                0,
                2,
                leaveOpen: true);
            borrowedSession.Dispose();
            Assert.That(borrowed.WasDisposed, Is.False);
            borrowed.Dispose();
        }

        [Test]
        public void OwnedStreamIsDisposedWhenFactoryValidationFails()
        {
            var stream = new ControlledSeekableStream(Bytes(2), 2);
            Assert.Throws<BinaryReadException>(() =>
                ReadOnlyDataWindowSession.FromSeekableStream(
                    stream,
                    Source(),
                    0,
                    3,
                    Limits(),
                    false));

            Assert.That(stream.DisposeCallCount, Is.EqualTo(1));
        }

        [Test]
        public void DisposingRootSessionInvalidatesExistingChildren()
        {
            var stream = new ControlledSeekableStream(Bytes(4), 4);
            ReadOnlyDataWindowSession session = Session(stream, 0, 4);
            ReadOnlyDataWindow child = session.Root.CreateChild(1, 2, "child");
            session.Dispose();

            Assert.Throws<ObjectDisposedException>(() =>
                child.ReadExactly(0, new byte[1], 0, 1, "payload"));
            Assert.That(stream.WasDisposed, Is.True);
        }

        [Test]
        public void WindowOperationsAreConfinedToCreatingThread()
        {
            var stream = new ControlledSeekableStream(Bytes(4), 4);
            using (ReadOnlyDataWindowSession session = Session(stream, 0, 4))
            {
                Exception observed = null;
                var thread = new Thread(() =>
                {
                    try
                    {
                        session.Root.ReadExactly(0, new byte[1], 0, 1, "payload");
                    }
                    catch (Exception exception)
                    {
                        observed = exception;
                    }
                });
                thread.Start();
                Assert.That(thread.Join(5000), Is.True);

                Assert.That(observed, Is.TypeOf<InvalidOperationException>());
                Assert.That(stream.ReadCallCount, Is.Zero);
                Assert.That(session.BudgetUsage.BytesRead, Is.Zero);
            }
        }

        [Test]
        public void ReadFailureIsStructuredAndDoesNotExposePhysicalPath()
        {
            var stream = new ControlledSeekableStream(Bytes(4), 4)
            {
                ThrowOnReadCall = 1
            };
            using (ReadOnlyDataWindowSession session = Session(stream, 0, 4))
            {
                BinaryReadException exception = Assert.Throws<BinaryReadException>(() =>
                    session.Root.ReadExactly(0, new byte[2], 0, 2, "payload"));

                Assert.That(
                    exception.Diagnostic.Code,
                    Is.EqualTo(BinaryDiagnosticCode.ReadFailure));
                Assert.That(exception.Message, Does.Not.Contain("private"));
                Assert.That(exception.Message, Does.Not.Contain(":"));
                Assert.That(exception.Diagnostic.Source.LogicalPath.Value,
                    Is.EqualTo("Synthetic/window.bin"));
            }
        }

        [Test]
        public void PrematureBackingEofIsStructuredAndCannotLoop()
        {
            var stream = new ControlledSeekableStream(Bytes(4), 4)
            {
                ReportedLength = 8
            };
            using (ReadOnlyDataWindowSession session = Session(stream, 0, 8))
            {
                BinaryReadException exception = Assert.Throws<BinaryReadException>(() =>
                    session.Root.ReadExactly(0, new byte[8], 0, 8, "payload"));

                Assert.That(
                    exception.Diagnostic.Code,
                    Is.EqualTo(BinaryDiagnosticCode.UnexpectedEndOfInput));
                Assert.That(stream.ReadCallCount, Is.EqualTo(2));
            }
        }

        [Test]
        public void StreamingCopyPreservesBytesAcrossShortReads()
        {
            var stream = new ControlledSeekableStream(Bytes(25), 3);
            using (ReadOnlyDataWindowSession session = Session(stream, 4, 15))
            using (var destination = new MemoryStream())
            {
                session.Root.CopyTo(destination, "payload", 4);

                Assert.That(
                    destination.ToArray(),
                    Is.EqualTo(new byte[]
                    {
                        4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18
                    }));
                Assert.That(session.BudgetUsage.BytesRead, Is.EqualTo(15));
            }
        }

        [Test]
        public void StreamingSha256HashesOnlyTheWindow()
        {
            byte[] bytes = new byte[] { 0, (byte)'a', (byte)'b', (byte)'c', 0 };
            using (ReadOnlyDataWindowSession session = Session(
                       new ControlledSeekableStream(bytes, 1),
                       1,
                       3))
            {
                string digest = session.Root.ComputeSha256("payload", 2);

                Assert.That(
                    digest,
                    Is.EqualTo("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad"));
                Assert.That(session.BudgetUsage.BytesRead, Is.EqualTo(3));
            }
        }

        [Test]
        public void EmptyWindowCopiesAndHashesWithoutBackingRead()
        {
            var stream = new ControlledSeekableStream(Bytes(2), 1);
            using (ReadOnlyDataWindowSession session = Session(stream, 1, 0))
            using (var destination = new MemoryStream())
            {
                session.Root.CopyTo(destination, "payload", 1);
                string digest = session.Root.ComputeSha256("payload", 1);

                Assert.That(destination.Length, Is.Zero);
                Assert.That(
                    digest,
                    Is.EqualTo("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"));
                Assert.That(stream.ReadCallCount, Is.Zero);
            }
        }

        [Test]
        public void FileBackingReadsOnlyRequestedWindow()
        {
            string directory = Path.Combine(
                Path.GetTempPath(),
                "RA2YR-ReadOnlyDataWindowTests");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".bin");
            File.WriteAllBytes(path, Bytes(12));
            try
            {
                using (ReadOnlyDataWindowSession session =
                       ReadOnlyDataWindowSession.FromFile(
                           path,
                           Source(),
                           3,
                           4,
                           Limits()))
                {
                    var result = new byte[4];
                    session.Root.ReadExactly(0, result, 0, result.Length, "payload");
                    Assert.That(result, Is.EqualTo(new byte[] { 3, 4, 5, 6 }));
                }
            }
            finally
            {
                File.Delete(path);
            }
        }

        private static ReadOnlyDataWindowSession Session(
            ControlledSeekableStream stream,
            long startOffset,
            long length,
            ReadOnlyDataWindowLimits limits = null,
            bool leaveOpen = false)
        {
            return ReadOnlyDataWindowSession.FromSeekableStream(
                stream,
                Source(),
                startOffset,
                length,
                limits ?? Limits(),
                leaveOpen);
        }

        private static BinarySourceContext Source()
        {
            return new BinarySourceContext(
                "format.seekable-window",
                "synthetic-source",
                LogicalContentPath.Parse("Synthetic/window.bin"));
        }

        private static ReadOnlyDataWindowLimits Limits(
            long maxRootLength = 1024,
            long maxSingleReadBytes = 1024,
            long maxTotalReadBytes = 2048,
            long maxWindows = 128,
            int maxWindowDepth = 16)
        {
            return new ReadOnlyDataWindowLimits(
                maxRootLength,
                maxSingleReadBytes,
                maxTotalReadBytes,
                maxWindows,
                maxWindowDepth);
        }

        private static byte[] Bytes(int count)
        {
            var bytes = new byte[count];
            for (int index = 0; index < count; index++)
            {
                bytes[index] = checked((byte)index);
            }

            return bytes;
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
        }

        private sealed class ControlledSeekableStream : Stream
        {
            private readonly byte[] data;
            private readonly int maxChunk;
            private long position;

            public ControlledSeekableStream(byte[] data, int maxChunk)
            {
                this.data = data ?? throw new ArgumentNullException(nameof(data));
                this.maxChunk = maxChunk;
                ReportedLength = data.Length;
            }

            public int ReadCallCount { get; private set; }

            public int DisposeCallCount { get; private set; }

            public bool WasDisposed { get; private set; }

            public int ThrowOnReadCall { get; set; }

            public bool ThrowOnSeek { get; set; }

            public long ReportedLength { get; set; }

            public override bool CanRead => !WasDisposed;

            public override bool CanSeek => !WasDisposed;

            public override bool CanWrite => false;

            public override long Length
            {
                get
                {
                    EnsureOpen();
                    return ReportedLength;
                }
            }

            public override long Position
            {
                get
                {
                    EnsureOpen();
                    return position;
                }
                set
                {
                    EnsureOpen();
                    position = value;
                }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                EnsureOpen();
                ReadCallCount++;
                if (ThrowOnReadCall == ReadCallCount)
                {
                    throw new IOException("Synthetic C:\\private\\backing.mix failure.");
                }

                int remaining = position >= data.Length
                    ? 0
                    : data.Length - checked((int)position);
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
                EnsureOpen();
                if (ThrowOnSeek)
                {
                    throw new IOException("Synthetic C:\\private\\backing.mix seek failure.");
                }

                long basis = origin == SeekOrigin.Begin
                    ? 0
                    : origin == SeekOrigin.Current
                        ? position
                        : ReportedLength;
                position = checked(basis + offset);
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

            private void EnsureOpen()
            {
                if (WasDisposed)
                {
                    throw new ObjectDisposedException(nameof(ControlledSeekableStream));
                }
            }
        }
    }
}
