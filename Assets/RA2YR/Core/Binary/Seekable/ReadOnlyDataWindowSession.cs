using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

namespace RA2YR.Core.Binary.Seekable
{
    internal sealed class ReadOnlyDataWindowSession : IDisposable
    {
        internal const int DefaultTransferBufferSize = 81920;

        private const string LowerHexCharacters = "0123456789abcdef";

        private readonly List<BinaryDiagnostic> diagnostics =
            new List<BinaryDiagnostic>();
        private readonly IReadOnlyList<BinaryDiagnostic> diagnosticView;
        private readonly Stream backingStream;
        private readonly bool leaveOpen;
        private readonly int ownerThreadId;
        private long bytesRead;
        private long windowsCreated;
        private int deepestWindow;
        private bool disposed;
        private bool disposeFailed;
        private bool faulted;

        private ReadOnlyDataWindowSession(
            Stream stream,
            BinarySourceContext source,
            long startOffset,
            long length,
            ReadOnlyDataWindowLimits limits,
            bool leaveOpen)
        {
            backingStream = stream;
            Source = source;
            Limits = limits;
            this.leaveOpen = leaveOpen;
            ownerThreadId = Thread.CurrentThread.ManagedThreadId;
            diagnosticView = diagnostics.AsReadOnly();
            windowsCreated = 1;
            Root = new ReadOnlyDataWindow(this, startOffset, length, 0);
        }

        public BinarySourceContext Source { get; }

        public ReadOnlyDataWindowLimits Limits { get; }

        public ReadOnlyDataWindow Root { get; }

        public IReadOnlyList<BinaryDiagnostic> Diagnostics
        {
            get
            {
                EnsureOwnerThread();
                return diagnosticView;
            }
        }

        public bool IsFaulted
        {
            get
            {
                EnsureOwnerThread();
                return faulted;
            }
        }

        public ReadOnlyDataWindowBudgetUsage BudgetUsage
        {
            get
            {
                EnsureOwnerThread();
                return new ReadOnlyDataWindowBudgetUsage(
                    bytesRead,
                    windowsCreated,
                    deepestWindow);
            }
        }

        public static ReadOnlyDataWindowSession FromSeekableStream(
            Stream stream,
            BinarySourceContext source,
            long startOffset,
            long length,
            ReadOnlyDataWindowLimits limits = null,
            bool leaveOpen = false)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            ReadOnlyDataWindowLimits effectiveLimits =
                limits ?? ReadOnlyDataWindowLimits.Default;
            bool ownershipDelegated = false;
            try
            {
                ValidateStreamCapabilities(stream, source);
                ValidateRootRange(
                    stream,
                    source,
                    startOffset,
                    length,
                    effectiveLimits);

                ownershipDelegated = true;
                return new ReadOnlyDataWindowSession(
                    stream,
                    source,
                    startOffset,
                    length,
                    effectiveLimits,
                    leaveOpen);
            }
            catch
            {
                if (!leaveOpen && !ownershipDelegated)
                {
                    TryDispose(stream);
                }

                throw;
            }
        }

        public static ReadOnlyDataWindowSession FromFile(
            string physicalPath,
            BinarySourceContext source,
            long startOffset,
            long length,
            ReadOnlyDataWindowLimits limits = null)
        {
            if (physicalPath == null)
            {
                throw new ArgumentNullException(nameof(physicalPath));
            }

            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            FileStream stream = null;
            bool delegated = false;
            try
            {
                stream = new FileStream(
                    physicalPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    DefaultTransferBufferSize,
                    FileOptions.RandomAccess);
                delegated = true;
                return FromSeekableStream(
                    stream,
                    source,
                    startOffset,
                    length,
                    limits,
                    false);
            }
            catch (BinaryReadException)
            {
                throw;
            }
            catch (Exception exception) when (
                exception is IOException ||
                exception is UnauthorizedAccessException ||
                exception is NotSupportedException ||
                exception is ArgumentException)
            {
                throw FactoryFailure(
                    source,
                    BinaryDiagnosticCode.ReadFailure,
                    startOffset,
                    length,
                    0,
                    "input",
                    "The read-only file backing could not be opened.");
            }
            finally
            {
                if (!delegated && stream != null)
                {
                    TryDispose(stream);
                }
            }
        }

        public static ReadOnlyDataWindowSession FromFile(
            string physicalPath,
            BinarySourceContext source,
            ReadOnlyDataWindowLimits limits = null)
        {
            if (physicalPath == null)
            {
                throw new ArgumentNullException(nameof(physicalPath));
            }

            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            FileStream stream = null;
            bool delegated = false;
            try
            {
                stream = new FileStream(
                    physicalPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    DefaultTransferBufferSize,
                    FileOptions.RandomAccess);
                long length = stream.Length;
                delegated = true;
                return FromSeekableStream(
                    stream,
                    source,
                    0,
                    length,
                    limits,
                    false);
            }
            catch (BinaryReadException)
            {
                throw;
            }
            catch (Exception exception) when (
                exception is IOException ||
                exception is UnauthorizedAccessException ||
                exception is NotSupportedException ||
                exception is ArgumentException)
            {
                throw FactoryFailure(
                    source,
                    BinaryDiagnosticCode.ReadFailure,
                    0,
                    0,
                    0,
                    "input",
                    "The read-only file backing could not be opened.");
            }
            finally
            {
                if (!delegated && stream != null)
                {
                    TryDispose(stream);
                }
            }
        }

        public void Dispose()
        {
            EnsureOwnerThread();
            if (disposed)
            {
                return;
            }

            if (leaveOpen)
            {
                disposed = true;
                return;
            }

            disposeFailed = true;
            backingStream.Dispose();
            disposeFailed = false;
            disposed = true;
        }

        internal ReadOnlyDataWindow CreateChild(
            ReadOnlyDataWindow parent,
            long relativeStartOffset,
            long length,
            string fieldOrSection)
        {
            EnsureUsable();
            string field = BinaryDiagnosticLabel.Validate(
                fieldOrSection,
                nameof(fieldOrSection));
            if (relativeStartOffset < 0)
            {
                Throw(
                    BinaryDiagnosticCode.InvalidOffset,
                    parent.AbsoluteStartOffset,
                    relativeStartOffset,
                    parent.Length,
                    field,
                    "A child window offset cannot be negative.");
            }

            if (length < 0)
            {
                Throw(
                    BinaryDiagnosticCode.InvalidLength,
                    parent.AbsoluteStartOffset,
                    length,
                    parent.Length,
                    field,
                    "A child window length cannot be negative.");
            }

            long relativeEnd;
            long absoluteStart;
            try
            {
                relativeEnd = checked(relativeStartOffset + length);
                absoluteStart = checked(parent.AbsoluteStartOffset + relativeStartOffset);
            }
            catch (OverflowException)
            {
                Throw(
                    BinaryDiagnosticCode.ArithmeticOverflow,
                    parent.AbsoluteStartOffset,
                    length,
                    parent.Length,
                    field,
                    "The child window range overflows Int64.");
                return null;
            }

            if (relativeEnd > parent.Length)
            {
                Throw(
                    BinaryDiagnosticCode.InvalidSubrange,
                    absoluteStart,
                    length,
                    Math.Max(0, parent.Length - relativeStartOffset),
                    field,
                    "The child window crosses its parent boundary.");
            }

            int depth;
            try
            {
                depth = checked(parent.Depth + 1);
            }
            catch (OverflowException)
            {
                Throw(
                    BinaryDiagnosticCode.ArithmeticOverflow,
                    absoluteStart,
                    length,
                    parent.Length - relativeEnd,
                    field,
                    "The child window depth overflows Int32.");
                return null;
            }

            if (depth > Limits.MaxWindowDepth)
            {
                Throw(
                    BinaryDiagnosticCode.NestingBudgetExceeded,
                    absoluteStart,
                    length,
                    parent.Length - relativeEnd,
                    field,
                    "The child window nesting budget would be exceeded.");
            }

            long updatedWindows;
            try
            {
                updatedWindows = checked(windowsCreated + 1);
            }
            catch (OverflowException)
            {
                Throw(
                    BinaryDiagnosticCode.ArithmeticOverflow,
                    absoluteStart,
                    length,
                    parent.Length - relativeEnd,
                    field,
                    "Cumulative window accounting overflowed.");
                return null;
            }

            if (updatedWindows > Limits.MaxWindows)
            {
                Throw(
                    BinaryDiagnosticCode.SubrangeBudgetExceeded,
                    absoluteStart,
                    length,
                    parent.Length - relativeEnd,
                    field,
                    "The child window count budget would be exceeded.");
            }

            windowsCreated = updatedWindows;
            if (depth > deepestWindow)
            {
                deepestWindow = depth;
            }

            return new ReadOnlyDataWindow(this, absoluteStart, length, depth);
        }

        internal void ReadExactly(
            ReadOnlyDataWindow window,
            long relativeOffset,
            byte[] destination,
            int destinationOffset,
            int count,
            string fieldOrSection)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (destinationOffset < 0 || count < 0 ||
                destinationOffset > destination.Length - count)
            {
                throw new ArgumentOutOfRangeException(nameof(destinationOffset));
            }

            EnsureUsable();
            string field = BinaryDiagnosticLabel.Validate(
                fieldOrSection,
                nameof(fieldOrSection));
            ValidateReadRange(window, relativeOffset, count, field);
            ValidateSingleRead(count, window, relativeOffset, field);
            ReserveReadBudget(count, window, relativeOffset, field);
            ReadCore(window, relativeOffset, destination, destinationOffset, count, field);
        }

        internal void CopyTo(
            ReadOnlyDataWindow window,
            Stream destination,
            string fieldOrSection,
            int bufferSize)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            EnsureUsable();
            string field = BinaryDiagnosticLabel.Validate(
                fieldOrSection,
                nameof(fieldOrSection));
            ValidateTransferBuffer(bufferSize, window, field);
            ValidateDestination(destination, window, field);
            ReserveReadBudget(window.Length, window, 0, field);

            ProcessChunks(
                window,
                bufferSize,
                field,
                (buffer, count, absoluteOffset, remaining) =>
                {
                    try
                    {
                        destination.Write(buffer, 0, count);
                    }
                    catch (Exception exception) when (
                        exception is IOException ||
                        exception is NotSupportedException ||
                        exception is ObjectDisposedException)
                    {
                        Throw(
                            BinaryDiagnosticCode.ReadFailure,
                            absoluteOffset,
                            count,
                            remaining,
                            field,
                            "The destination failed during a bounded streaming copy.");
                    }
                });
        }

        internal string ComputeSha256(
            ReadOnlyDataWindow window,
            string fieldOrSection,
            int bufferSize)
        {
            EnsureUsable();
            string field = BinaryDiagnosticLabel.Validate(
                fieldOrSection,
                nameof(fieldOrSection));
            ValidateTransferBuffer(bufferSize, window, field);
            ReserveReadBudget(window.Length, window, 0, field);

            using (SHA256 sha256 = SHA256.Create())
            {
                ProcessChunks(
                    window,
                    bufferSize,
                    field,
                    (buffer, count, absoluteOffset, remaining) =>
                    {
                        sha256.TransformBlock(buffer, 0, count, buffer, 0);
                    });
                sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                return ToLowerHex(sha256.Hash);
            }
        }

        private void ProcessChunks(
            ReadOnlyDataWindow window,
            int bufferSize,
            string field,
            Action<byte[], int, long, long> consume)
        {
            int effectiveBufferSize = (int)Math.Min(bufferSize, Math.Max(1, window.Length));
            byte[] buffer;
            try
            {
                buffer = new byte[effectiveBufferSize];
            }
            catch (OutOfMemoryException)
            {
                Throw(
                    BinaryDiagnosticCode.ReadFailure,
                    window.AbsoluteStartOffset,
                    effectiveBufferSize,
                    window.Length,
                    field,
                    "The bounded transfer buffer could not be allocated.");
                return;
            }

            long position = 0;
            while (position < window.Length)
            {
                int count = (int)Math.Min(buffer.Length, window.Length - position);
                ReadCore(window, position, buffer, 0, count, field);
                position = checked(position + count);
                consume(
                    buffer,
                    count,
                    window.AbsoluteStartOffset + position - count,
                    window.Length - position);
            }
        }

        private void ReadCore(
            ReadOnlyDataWindow window,
            long relativeOffset,
            byte[] destination,
            int destinationOffset,
            int count,
            string field)
        {
            long absoluteOffset = checked(window.AbsoluteStartOffset + relativeOffset);
            try
            {
                long actualPosition = backingStream.Seek(absoluteOffset, SeekOrigin.Begin);
                if (actualPosition != absoluteOffset)
                {
                    Throw(
                        BinaryDiagnosticCode.ReadFailure,
                        absoluteOffset,
                        count,
                        window.Length - relativeOffset,
                        field,
                        "The backing stream did not seek to the requested bounded offset.");
                }
            }
            catch (BinaryReadException)
            {
                throw;
            }
            catch (Exception exception) when (
                exception is IOException ||
                exception is NotSupportedException ||
                exception is ObjectDisposedException)
            {
                Throw(
                    BinaryDiagnosticCode.ReadFailure,
                    absoluteOffset,
                    count,
                    window.Length - relativeOffset,
                    field,
                    "The backing stream failed while seeking within a bounded window.");
            }

            int totalRead = 0;
            while (totalRead < count)
            {
                int requested = count - totalRead;
                int read;
                try
                {
                    read = backingStream.Read(
                        destination,
                        destinationOffset + totalRead,
                        requested);
                }
                catch (Exception exception) when (
                    exception is IOException ||
                    exception is NotSupportedException ||
                    exception is ObjectDisposedException)
                {
                    Throw(
                        BinaryDiagnosticCode.ReadFailure,
                        absoluteOffset + totalRead,
                        requested,
                        window.Length - relativeOffset - totalRead,
                        field,
                        "The backing stream failed during an exact bounded read.");
                    return;
                }

                if (read < 0 || read > requested)
                {
                    Throw(
                        BinaryDiagnosticCode.ReadFailure,
                        absoluteOffset + totalRead,
                        requested,
                        window.Length - relativeOffset - totalRead,
                        field,
                        "The backing stream returned an invalid read count.");
                }

                if (read == 0)
                {
                    Throw(
                        BinaryDiagnosticCode.UnexpectedEndOfInput,
                        absoluteOffset + totalRead,
                        requested,
                        window.Length - relativeOffset - totalRead,
                        field,
                        "The backing stream ended before the bounded read completed.");
                }

                totalRead = checked(totalRead + read);
            }
        }

        private void ValidateReadRange(
            ReadOnlyDataWindow window,
            long relativeOffset,
            long count,
            string field)
        {
            if (relativeOffset < 0)
            {
                Throw(
                    BinaryDiagnosticCode.InvalidOffset,
                    window.AbsoluteStartOffset,
                    relativeOffset,
                    window.Length,
                    field,
                    "A window read offset cannot be negative.");
            }

            long end;
            try
            {
                end = checked(relativeOffset + count);
            }
            catch (OverflowException)
            {
                Throw(
                    BinaryDiagnosticCode.ArithmeticOverflow,
                    window.AbsoluteStartOffset,
                    count,
                    window.Length,
                    field,
                    "The window read range overflows Int64.");
                return;
            }

            if (end > window.Length)
            {
                long absoluteOffset;
                try
                {
                    absoluteOffset = checked(window.AbsoluteStartOffset + relativeOffset);
                }
                catch (OverflowException)
                {
                    absoluteOffset = window.AbsoluteStartOffset;
                }

                Throw(
                    BinaryDiagnosticCode.UnexpectedEndOfInput,
                    absoluteOffset,
                    count,
                    Math.Max(0, window.Length - relativeOffset),
                    field,
                    "The exact read crosses the bounded window end.");
            }
        }

        private void ValidateSingleRead(
            long count,
            ReadOnlyDataWindow window,
            long relativeOffset,
            string field)
        {
            if (count > Limits.MaxSingleReadBytes)
            {
                Throw(
                    BinaryDiagnosticCode.ReadBudgetExceeded,
                    window.AbsoluteStartOffset + relativeOffset,
                    count,
                    window.Length - relativeOffset,
                    field,
                    "The exact read exceeds the per-operation window read budget.");
            }
        }

        private void ReserveReadBudget(
            long count,
            ReadOnlyDataWindow window,
            long relativeOffset,
            string field)
        {
            long updated;
            try
            {
                updated = checked(bytesRead + count);
            }
            catch (OverflowException)
            {
                Throw(
                    BinaryDiagnosticCode.ArithmeticOverflow,
                    window.AbsoluteStartOffset + relativeOffset,
                    count,
                    window.Length - relativeOffset,
                    field,
                    "Cumulative window read accounting overflowed.");
                return;
            }

            if (updated > Limits.MaxTotalReadBytes)
            {
                Throw(
                    BinaryDiagnosticCode.ReadBudgetExceeded,
                    window.AbsoluteStartOffset + relativeOffset,
                    count,
                    window.Length - relativeOffset,
                    field,
                    "The cumulative window read budget would be exceeded.");
            }

            bytesRead = updated;
        }

        private void ValidateTransferBuffer(
            int bufferSize,
            ReadOnlyDataWindow window,
            string field)
        {
            if (bufferSize <= 0)
            {
                Throw(
                    BinaryDiagnosticCode.InvalidLength,
                    window.AbsoluteStartOffset,
                    bufferSize,
                    window.Length,
                    field,
                    "A transfer buffer size must be positive.");
            }

            if (bufferSize > Limits.MaxSingleReadBytes)
            {
                Throw(
                    BinaryDiagnosticCode.ReadBudgetExceeded,
                    window.AbsoluteStartOffset,
                    bufferSize,
                    window.Length,
                    field,
                    "The transfer buffer exceeds the per-operation window read budget.");
            }
        }

        private void ValidateDestination(
            Stream destination,
            ReadOnlyDataWindow window,
            string field)
        {
            bool canWrite;
            try
            {
                canWrite = destination.CanWrite;
            }
            catch (Exception exception) when (
                exception is IOException ||
                exception is NotSupportedException ||
                exception is ObjectDisposedException)
            {
                Throw(
                    BinaryDiagnosticCode.ReadFailure,
                    window.AbsoluteStartOffset,
                    window.Length,
                    window.Length,
                    field,
                    "The copy destination capability could not be inspected.");
                return;
            }

            if (!canWrite)
            {
                Throw(
                    BinaryDiagnosticCode.ReadFailure,
                    window.AbsoluteStartOffset,
                    window.Length,
                    window.Length,
                    field,
                    "A writable destination is required for a bounded streaming copy.");
            }
        }

        private void EnsureUsable()
        {
            EnsureOwnerThread();
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(ReadOnlyDataWindowSession));
            }

            if (disposeFailed)
            {
                throw new InvalidOperationException(
                    "The read-only window session is awaiting disposal retry.");
            }

            if (faulted)
            {
                throw new InvalidOperationException(
                    "The read-only data window session is faulted and cannot continue.");
            }
        }

        private void Throw(
            BinaryDiagnosticCode code,
            long absoluteOffset,
            long requestedLength,
            long remainingLength,
            string fieldOrSection,
            string message)
        {
            var diagnostic = new BinaryDiagnostic(
                BinaryDiagnosticSeverity.Error,
                code,
                Source,
                absoluteOffset,
                requestedLength,
                remainingLength,
                fieldOrSection,
                message);
            diagnostics.Add(diagnostic);
            faulted = true;
            throw new BinaryReadException(diagnostic);
        }

        private static void ValidateStreamCapabilities(
            Stream stream,
            BinarySourceContext source)
        {
            bool canRead;
            bool canSeek;
            try
            {
                canRead = stream.CanRead;
                canSeek = stream.CanSeek;
            }
            catch (Exception exception) when (
                exception is IOException ||
                exception is NotSupportedException ||
                exception is ObjectDisposedException)
            {
                throw FactoryFailure(
                    source,
                    BinaryDiagnosticCode.ReadFailure,
                    0,
                    0,
                    0,
                    "input",
                    "The backing stream capabilities could not be inspected.");
            }

            if (!canRead)
            {
                throw FactoryFailure(
                    source,
                    BinaryDiagnosticCode.ReadFailure,
                    0,
                    0,
                    0,
                    "input",
                    "A readable stream is required for a read-only data window.");
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
                    "A seekable stream is required for a read-only data window.");
            }
        }

        private static void ValidateRootRange(
            Stream stream,
            BinarySourceContext source,
            long startOffset,
            long length,
            ReadOnlyDataWindowLimits limits)
        {
            if (limits == null)
            {
                throw new ArgumentNullException(nameof(limits));
            }

            if (startOffset < 0)
            {
                throw FactoryFailure(
                    source,
                    BinaryDiagnosticCode.InvalidOffset,
                    startOffset,
                    length,
                    0,
                    "input",
                    "A root window offset cannot be negative.");
            }

            if (length < 0)
            {
                throw FactoryFailure(
                    source,
                    BinaryDiagnosticCode.InvalidLength,
                    startOffset,
                    length,
                    0,
                    "input",
                    "A root window length cannot be negative.");
            }

            long end;
            try
            {
                end = checked(startOffset + length);
            }
            catch (OverflowException)
            {
                throw FactoryFailure(
                    source,
                    BinaryDiagnosticCode.ArithmeticOverflow,
                    startOffset,
                    length,
                    0,
                    "input",
                    "The root window range overflows Int64.");
            }

            if (length > limits.MaxRootLength)
            {
                throw FactoryFailure(
                    source,
                    BinaryDiagnosticCode.InputBudgetExceeded,
                    startOffset,
                    length,
                    0,
                    "input",
                    "The root window exceeds its length budget.");
            }

            if (limits.MaxWindows < 1)
            {
                throw FactoryFailure(
                    source,
                    BinaryDiagnosticCode.SubrangeBudgetExceeded,
                    startOffset,
                    length,
                    0,
                    "input",
                    "The window count budget cannot admit the root window.");
            }

            long streamLength;
            try
            {
                streamLength = stream.Length;
            }
            catch (NotSupportedException)
            {
                throw FactoryFailure(
                    source,
                    BinaryDiagnosticCode.UnsupportedSeekOperation,
                    startOffset,
                    length,
                    0,
                    "input",
                    "The backing stream does not expose its length.");
            }
            catch (Exception exception) when (
                exception is IOException || exception is ObjectDisposedException)
            {
                throw FactoryFailure(
                    source,
                    BinaryDiagnosticCode.ReadFailure,
                    startOffset,
                    length,
                    0,
                    "input",
                    "The backing stream length could not be read.");
            }

            if (streamLength < 0 || end > streamLength)
            {
                throw FactoryFailure(
                    source,
                    BinaryDiagnosticCode.InvalidSubrange,
                    startOffset,
                    length,
                    Math.Max(0, streamLength - startOffset),
                    "input",
                    "The root window crosses the backing stream boundary.");
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

        private static string ToLowerHex(byte[] bytes)
        {
            var characters = new char[bytes.Length * 2];
            for (int index = 0; index < bytes.Length; index++)
            {
                byte value = bytes[index];
                characters[index * 2] = LowerHexCharacters[value >> 4];
                characters[index * 2 + 1] = LowerHexCharacters[value & 0x0f];
            }

            return new string(characters);
        }

        private static void TryDispose(Stream stream)
        {
            try
            {
                stream.Dispose();
            }
            catch
            {
                // Cleanup cannot replace the structured factory failure.
            }
        }

        private void EnsureOwnerThread()
        {
            if (Thread.CurrentThread.ManagedThreadId != ownerThreadId)
            {
                throw new InvalidOperationException(
                    "A read-only data window session is confined to its creating thread.");
            }
        }
    }
}
