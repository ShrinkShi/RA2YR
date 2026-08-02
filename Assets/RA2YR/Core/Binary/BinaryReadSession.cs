using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace RA2YR.Core.Binary
{
    internal sealed class BinaryReadSession : IDisposable
    {
        private const int StreamReadChunkSize = 81920;

        private readonly List<BinaryDiagnostic> diagnostics =
            new List<BinaryDiagnostic>();
        private readonly IReadOnlyList<BinaryDiagnostic> diagnosticView;
        private readonly Stream ownedStream;
        private readonly bool leaveOpen;
        private readonly int ownerThreadId;
        private long allocatedBytes;
        private long records;
        private long subranges;
        private long longestStringLength;
        private int deepestNesting;
        private bool disposed;
        private bool faulted;
        private bool completed;

        private BinaryReadSession(
            ReadOnlyMemory<byte> memory,
            BinarySourceContext source,
            BinaryReadLimits limits,
            long absoluteStartOffset,
            long initialAllocation,
            Stream ownedStream,
            bool leaveOpen)
        {
            Memory = memory;
            Source = source;
            Limits = limits;
            AbsoluteStartOffset = absoluteStartOffset;
            allocatedBytes = initialAllocation;
            this.ownedStream = ownedStream;
            this.leaveOpen = leaveOpen;
            diagnosticView = diagnostics.AsReadOnly();
            ownerThreadId = Thread.CurrentThread.ManagedThreadId;
            Root = new BoundedBinaryReader(
                this,
                0,
                memory.Length,
                absoluteStartOffset,
                0,
                null);
        }

        public BinarySourceContext Source { get; }

        public BinaryReadLimits Limits { get; }

        public long AbsoluteStartOffset { get; }

        public BoundedBinaryReader Root { get; }

        public bool IsFaulted
        {
            get
            {
                EnsureOwnerThread();
                return faulted;
            }
        }

        public bool IsCompleted
        {
            get
            {
                EnsureOwnerThread();
                return completed;
            }
        }

        public IReadOnlyList<BinaryDiagnostic> Diagnostics
        {
            get
            {
                EnsureOwnerThread();
                return diagnosticView;
            }
        }

        public BinaryBudgetUsage BudgetUsage
        {
            get
            {
                EnsureOwnerThread();
                return new BinaryBudgetUsage(
                    Memory.Length,
                    allocatedBytes,
                    records,
                    subranges,
                    longestStringLength,
                    deepestNesting);
            }
        }

        internal ReadOnlyMemory<byte> Memory { get; }

        public static BinaryReadSession FromMemory(
            ReadOnlyMemory<byte> input,
            BinarySourceContext source,
            BinaryReadLimits limits = null,
            long absoluteStartOffset = 0)
        {
            BinaryReadLimits effectiveLimits = limits ?? BinaryReadLimits.Default;
            ValidateInputBounds(
                source,
                effectiveLimits,
                input.Length,
                absoluteStartOffset,
                false);
            return new BinaryReadSession(
                input,
                source,
                effectiveLimits,
                absoluteStartOffset,
                0,
                null,
                true);
        }

        public static BinaryReadSession FromStream(
            Stream stream,
            long inputLength,
            BinarySourceContext source,
            BinaryReadLimits limits = null,
            bool leaveOpen = false,
            long absoluteStartOffset = 0)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            BinaryReadLimits effectiveLimits = limits ?? BinaryReadLimits.Default;
            try
            {
                ValidateInputBounds(
                    source,
                    effectiveLimits,
                    inputLength,
                    absoluteStartOffset,
                    true);
                if (inputLength > 0 && effectiveLimits.MaxSingleReadBytes == 0)
                {
                    throw FactoryFailure(
                        source,
                        BinaryDiagnosticCode.ReadBudgetExceeded,
                        absoluteStartOffset,
                        inputLength,
                        inputLength,
                        "input",
                        "The Stream snapshot cannot read within a zero-byte operation budget.");
                }

                int snapshotReadLimit = (int)Math.Min(
                    StreamReadChunkSize,
                    effectiveLimits.MaxSingleReadBytes);
                int bufferLength = ConvertLengthToInt(
                    source,
                    inputLength,
                    absoluteStartOffset);
                byte[] buffer;
                try
                {
                    buffer = new byte[bufferLength];
                }
                catch (OutOfMemoryException)
                {
                    throw FactoryFailure(
                        source,
                        BinaryDiagnosticCode.ReadFailure,
                        absoluteStartOffset,
                        inputLength,
                        inputLength,
                        "input",
                        "The validated bounded input snapshot could not be allocated.");
                }

                int totalRead = 0;
                while (totalRead < bufferLength)
                {
                    int requested = Math.Min(snapshotReadLimit, bufferLength - totalRead);
                    int read;
                    try
                    {
                        read = stream.Read(buffer, totalRead, requested);
                    }
                    catch (Exception exception) when (
                        exception is IOException ||
                        exception is NotSupportedException ||
                        exception is ObjectDisposedException)
                    {
                        throw FactoryFailure(
                            source,
                            BinaryDiagnosticCode.ReadFailure,
                            absoluteStartOffset + totalRead,
                            requested,
                            bufferLength - totalRead,
                            "input",
                            "The input stream failed while creating a bounded snapshot.");
                    }

                    if (read < 0 || read > requested)
                    {
                        throw FactoryFailure(
                            source,
                            BinaryDiagnosticCode.ReadFailure,
                            absoluteStartOffset + totalRead,
                            requested,
                            bufferLength - totalRead,
                            "input",
                            "The input stream returned an invalid read count.");
                    }

                    if (read == 0)
                    {
                        throw FactoryFailure(
                            source,
                            BinaryDiagnosticCode.UnexpectedEndOfInput,
                            absoluteStartOffset + totalRead,
                            requested,
                            bufferLength - totalRead,
                            "input",
                            "The input stream ended before its declared bounded length.");
                    }

                    totalRead = checked(totalRead + read);
                }

                return new BinaryReadSession(
                    buffer,
                    source,
                    effectiveLimits,
                    absoluteStartOffset,
                    inputLength,
                    stream,
                    leaveOpen);
            }
            catch
            {
                if (!leaveOpen)
                {
                    TryDispose(stream);
                }

                throw;
            }
        }

        public static BinaryReadSession FromSeekableStream(
            Stream stream,
            BinarySourceContext source,
            BinaryReadLimits limits = null,
            bool leaveOpen = false)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            bool delegatedToBoundedSnapshot = false;
            try
            {
                return FromSeekableStreamCore(
                    stream,
                    source,
                    limits,
                    leaveOpen,
                    ref delegatedToBoundedSnapshot);
            }
            catch
            {
                if (!leaveOpen && !delegatedToBoundedSnapshot)
                {
                    TryDispose(stream);
                }

                throw;
            }
        }

        private static BinaryReadSession FromSeekableStreamCore(
            Stream stream,
            BinarySourceContext source,
            BinaryReadLimits limits,
            bool leaveOpen,
            ref bool delegatedToBoundedSnapshot)
        {

            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            bool canSeek;
            try
            {
                canSeek = stream.CanSeek;
            }
            catch (NotSupportedException)
            {
                throw FactoryFailure(
                    source,
                    BinaryDiagnosticCode.UnsupportedSeekOperation,
                    0,
                    0,
                    0,
                    "input",
                    "The stream does not expose seek capability metadata.");
            }
            catch (Exception exception) when (
                exception is IOException || exception is ObjectDisposedException)
            {
                throw FactoryFailure(
                    source,
                    BinaryDiagnosticCode.ReadFailure,
                    0,
                    0,
                    0,
                    "input",
                    "The stream capability could not be inspected.");
            }

            if (!canSeek)
            {
                throw FactoryFailure(
                    source,
                    BinaryDiagnosticCode.UnsupportedSeekOperation,
                    0,
                    0,
                    0,
                    "input",
                    "A seekable stream is required when the input length is inferred.");
            }

            long position;
            long length;
            try
            {
                position = stream.Position;
                length = stream.Length;
            }
            catch (NotSupportedException)
            {
                throw FactoryFailure(
                    source,
                    BinaryDiagnosticCode.UnsupportedSeekOperation,
                    0,
                    0,
                    0,
                    "input",
                    "The stream does not expose Position and Length.");
            }
            catch (Exception exception) when (
                exception is IOException || exception is ObjectDisposedException)
            {
                throw FactoryFailure(
                    source,
                    BinaryDiagnosticCode.ReadFailure,
                    0,
                    0,
                    0,
                    "input",
                    "The stream Position or Length could not be read.");
            }

            if (position < 0 || length < position)
            {
                throw FactoryFailure(
                    source,
                    BinaryDiagnosticCode.InvalidLength,
                    position,
                    length,
                    0,
                    "input",
                    "The seekable stream reported an invalid Position or Length.");
            }

            delegatedToBoundedSnapshot = true;
            return FromStream(
                stream,
                length - position,
                source,
                limits,
                leaveOpen,
                position);
        }

        public void Dispose()
        {
            EnsureOwnerThread();
            if (disposed)
            {
                return;
            }

            disposed = true;
            if (!leaveOpen && ownedStream != null)
            {
                ownedStream.Dispose();
            }
        }

        internal void EnsureUsable()
        {
            EnsureOwnerThread();
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(BinaryReadSession));
            }

            if (faulted)
            {
                throw new InvalidOperationException(
                    "The binary read session is faulted and cannot continue.");
            }

            if (completed)
            {
                throw new InvalidOperationException(
                    "The binary read session is complete and cannot continue.");
            }
        }

        internal void ValidateReadLength(
            long requestedLength,
            long absoluteOffset,
            long remainingLength,
            string fieldOrSection)
        {
            EnsureUsable();
            string field = BinaryDiagnosticLabel.Validate(fieldOrSection, nameof(fieldOrSection));
            if (requestedLength < 0)
            {
                Throw(
                    BinaryDiagnosticCode.InvalidLength,
                    absoluteOffset,
                    requestedLength,
                    remainingLength,
                    field,
                    "A binary read length cannot be negative.");
            }

            if (requestedLength > Limits.MaxSingleReadBytes)
            {
                Throw(
                    BinaryDiagnosticCode.ReadBudgetExceeded,
                    absoluteOffset,
                    requestedLength,
                    remainingLength,
                    field,
                    "The requested read exceeds the per-operation read budget.");
            }
        }

        internal void ReserveAllocation(
            long byteCount,
            long absoluteOffset,
            long remainingLength,
            string fieldOrSection)
        {
            EnsureUsable();
            string field = BinaryDiagnosticLabel.Validate(fieldOrSection, nameof(fieldOrSection));
            if (byteCount < 0)
            {
                Throw(
                    BinaryDiagnosticCode.InvalidLength,
                    absoluteOffset,
                    byteCount,
                    remainingLength,
                    field,
                    "An allocation length cannot be negative.");
            }

            long updated;
            try
            {
                updated = checked(allocatedBytes + byteCount);
            }
            catch (OverflowException)
            {
                Throw(
                    BinaryDiagnosticCode.ArithmeticOverflow,
                    absoluteOffset,
                    byteCount,
                    remainingLength,
                    field,
                    "Cumulative allocation accounting overflowed.");
                return;
            }

            if (updated > Limits.MaxAllocatedBytes)
            {
                Throw(
                    BinaryDiagnosticCode.AllocationBudgetExceeded,
                    absoluteOffset,
                    byteCount,
                    remainingLength,
                    field,
                    "The parse session allocation budget would be exceeded.");
            }

            allocatedBytes = updated;
        }

        internal void ReserveRecords(
            long count,
            long absoluteOffset,
            long remainingLength,
            string fieldOrSection)
        {
            EnsureUsable();
            if (count < 0)
            {
                Throw(
                    BinaryDiagnosticCode.InvalidLength,
                    absoluteOffset,
                    count,
                    remainingLength,
                    fieldOrSection,
                    "A record count cannot be negative.");
            }

            long updated;
            try
            {
                updated = checked(records + count);
            }
            catch (OverflowException)
            {
                Throw(
                    BinaryDiagnosticCode.ArithmeticOverflow,
                    absoluteOffset,
                    count,
                    remainingLength,
                    fieldOrSection,
                    "Cumulative record accounting overflowed.");
                return;
            }

            if (updated > Limits.MaxRecords)
            {
                Throw(
                    BinaryDiagnosticCode.RecordBudgetExceeded,
                    absoluteOffset,
                    count,
                    remainingLength,
                    fieldOrSection,
                    "The parse session record budget would be exceeded.");
            }

            records = updated;
        }

        internal void ValidateStringLength(
            long length,
            long absoluteOffset,
            long remainingLength,
            string fieldOrSection)
        {
            EnsureUsable();
            if (length < 0)
            {
                Throw(
                    BinaryDiagnosticCode.InvalidLength,
                    absoluteOffset,
                    length,
                    remainingLength,
                    fieldOrSection,
                    "A string length cannot be negative.");
            }

            if (length > Limits.MaxStringLength)
            {
                Throw(
                    BinaryDiagnosticCode.StringBudgetExceeded,
                    absoluteOffset,
                    length,
                    remainingLength,
                    fieldOrSection,
                    "The declared string length exceeds the string budget.");
            }

            if (length > longestStringLength)
            {
                longestStringLength = length;
            }
        }

        internal void ReserveSubrange(
            int depth,
            long length,
            long absoluteOffset,
            long remainingLength,
            string fieldOrSection)
        {
            EnsureUsable();
            if (depth > Limits.MaxNestingDepth)
            {
                Throw(
                    BinaryDiagnosticCode.NestingBudgetExceeded,
                    absoluteOffset,
                    length,
                    remainingLength,
                    fieldOrSection,
                    "The bounded subrange nesting budget would be exceeded.");
            }

            long updated;
            try
            {
                updated = checked(subranges + 1);
            }
            catch (OverflowException)
            {
                Throw(
                    BinaryDiagnosticCode.ArithmeticOverflow,
                    absoluteOffset,
                    length,
                    remainingLength,
                    fieldOrSection,
                    "Cumulative subrange accounting overflowed.");
                return;
            }

            if (updated > Limits.MaxSubranges)
            {
                Throw(
                    BinaryDiagnosticCode.SubrangeBudgetExceeded,
                    absoluteOffset,
                    length,
                    remainingLength,
                    fieldOrSection,
                    "The parse session subrange budget would be exceeded.");
            }

            subranges = updated;
            if (depth > deepestNesting)
            {
                deepestNesting = depth;
            }
        }

        internal BinaryDiagnostic Record(
            BinaryDiagnosticSeverity severity,
            BinaryDiagnosticCode code,
            long absoluteOffset,
            long requestedLength,
            long remainingLength,
            string fieldOrSection,
            string message)
        {
            EnsureUsable();
            var diagnostic = new BinaryDiagnostic(
                severity,
                code,
                Source,
                absoluteOffset,
                requestedLength,
                remainingLength,
                fieldOrSection,
                message);
            diagnostics.Add(diagnostic);
            return diagnostic;
        }

        internal void MarkFaulted()
        {
            EnsureOwnerThread();
            faulted = true;
        }

        internal void MarkCompleted()
        {
            EnsureUsable();
            completed = true;
        }

        internal void Throw(
            BinaryDiagnosticCode code,
            long absoluteOffset,
            long requestedLength,
            long remainingLength,
            string fieldOrSection,
            string message)
        {
            BinaryDiagnostic diagnostic = Record(
                BinaryDiagnosticSeverity.Error,
                code,
                absoluteOffset,
                requestedLength,
                remainingLength,
                fieldOrSection,
                message);
            faulted = true;
            throw new BinaryReadException(diagnostic);
        }

        private static void ValidateInputBounds(
            BinarySourceContext source,
            BinaryReadLimits limits,
            long inputLength,
            long absoluteStartOffset,
            bool allocationRequired)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (limits == null)
            {
                throw new ArgumentNullException(nameof(limits));
            }

            if (inputLength < 0)
            {
                throw FactoryFailure(
                    source,
                    BinaryDiagnosticCode.InvalidLength,
                    absoluteStartOffset,
                    inputLength,
                    0,
                    "input",
                    "A bounded input length cannot be negative.");
            }

            if (absoluteStartOffset < 0)
            {
                throw FactoryFailure(
                    source,
                    BinaryDiagnosticCode.InvalidOffset,
                    absoluteStartOffset,
                    inputLength,
                    0,
                    "input",
                    "An absolute input offset cannot be negative.");
            }

            try
            {
                long rangeEnd = checked(absoluteStartOffset + inputLength);
                if (rangeEnd < absoluteStartOffset)
                {
                    throw new OverflowException();
                }
            }
            catch (OverflowException)
            {
                throw FactoryFailure(
                    source,
                    BinaryDiagnosticCode.ArithmeticOverflow,
                    absoluteStartOffset,
                    inputLength,
                    0,
                    "input",
                    "The absolute input range overflows Int64.");
            }

            if (inputLength > limits.MaxInputBytes)
            {
                throw FactoryFailure(
                    source,
                    BinaryDiagnosticCode.InputBudgetExceeded,
                    absoluteStartOffset,
                    inputLength,
                    0,
                    "input",
                    "The bounded input exceeds the input byte budget.");
            }

            if (allocationRequired && inputLength > limits.MaxAllocatedBytes)
            {
                throw FactoryFailure(
                    source,
                    BinaryDiagnosticCode.AllocationBudgetExceeded,
                    absoluteStartOffset,
                    inputLength,
                    inputLength,
                    "input",
                    "The stream snapshot exceeds the allocation budget.");
            }
        }

        private static int ConvertLengthToInt(
            BinarySourceContext source,
            long inputLength,
            long absoluteStartOffset)
        {
            try
            {
                return checked((int)inputLength);
            }
            catch (OverflowException)
            {
                throw FactoryFailure(
                    source,
                    BinaryDiagnosticCode.ArithmeticOverflow,
                    absoluteStartOffset,
                    inputLength,
                    inputLength,
                    "input",
                    "The bounded stream length cannot be represented by the snapshot buffer.");
            }
        }

        private static BinaryReadException FactoryFailure(
            BinarySourceContext source,
            BinaryDiagnosticCode code,
            long absoluteOffset,
            long requestedLength,
            long remainingLength,
            string fieldOrSection,
            string message)
        {
            return new BinaryReadException(new BinaryDiagnostic(
                BinaryDiagnosticSeverity.Error,
                code,
                source,
                absoluteOffset,
                requestedLength,
                remainingLength,
                fieldOrSection,
                message));
        }

        private static void TryDispose(Stream stream)
        {
            try
            {
                stream.Dispose();
            }
            catch
            {
                // Cleanup must not replace the structured parse failure.
            }
        }

        private void EnsureOwnerThread()
        {
            if (Thread.CurrentThread.ManagedThreadId != ownerThreadId)
            {
                throw new InvalidOperationException(
                    "A binary read session is confined to its creating thread.");
            }
        }
    }
}
