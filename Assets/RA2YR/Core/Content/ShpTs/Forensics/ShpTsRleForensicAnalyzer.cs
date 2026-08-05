using System;
using System.Collections.Generic;
using System.IO;
using RA2YR.Core.Binary;
using RA2YR.Core.Binary.Seekable;
using RA2YR.Core.Formats.ShpTs;

namespace RA2YR.Core.Content.ShpTs.Forensics
{
    internal static class ShpTsRleForensicAnalyzer
    {
        public static ShpTsRleForensicFrameAnalysis Analyze(
            ReadOnlyMemory<byte> input,
            ShpTsDocument document,
            int frameIndex,
            bool allRows,
            ShpTsRleForensicLimits limits = null)
        {
            using (var source = new MemoryInput(input, document?.AbsoluteStartOffset ?? 0))
            {
                return AnalyzeCore(source, document, frameIndex, allRows, limits);
            }
        }

        public static ShpTsRleForensicFrameAnalysis Analyze(
            Stream stream,
            long inputLength,
            ShpTsDocument document,
            int frameIndex,
            bool allRows,
            ShpTsRleForensicLimits limits = null,
            bool leaveOpen = false)
        {
            using (var source = new StreamInput(
                stream,
                inputLength,
                document?.AbsoluteStartOffset ?? 0,
                leaveOpen))
            {
                return AnalyzeCore(source, document, frameIndex, allRows, limits);
            }
        }

        public static ShpTsRleForensicFrameAnalysis Analyze(
            ReadOnlyDataWindow window,
            ShpTsDocument document,
            int frameIndex,
            bool allRows,
            ShpTsRleForensicLimits limits = null)
        {
            using (var source = new WindowInput(window))
            {
                return AnalyzeCore(source, document, frameIndex, allRows, limits);
            }
        }

        private static ShpTsRleForensicFrameAnalysis AnalyzeCore(
            IInput source,
            ShpTsDocument document,
            int frameIndex,
            bool allRows,
            ShpTsRleForensicLimits limits)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }
            if (frameIndex < 0 || frameIndex >= document.Frames.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(frameIndex));
            }
            if (source.Length != document.InputLength)
            {
                throw new ArgumentException("The forensic input length does not match the parsed document.");
            }
            if (source.AbsoluteStartOffset != document.AbsoluteStartOffset)
            {
                throw new ArgumentException("The forensic input start does not match the parsed document.");
            }

            ShpTsFrameDescriptor descriptor = document.Frames[frameIndex];
            if (descriptor.IsCanonicalEmpty ||
                descriptor.CompressionKind != ShpTsCompressionKind.RleZeroTransparent)
            {
                return ShpTsRleForensicFrameAnalysis.Failure(
                    frameIndex,
                    descriptor.WidthRaw,
                    descriptor.HeightRaw,
                    Array.Empty<ShpTsRleForensicRowScalar>(),
                    ShpTsRleForensicFailureCode.FrameIsNotNonEmptyFlags3,
                    0,
                    descriptor.DescriptorAbsoluteOffset);
            }

            ShpTsRleForensicLimits effective = limits ?? ShpTsRleForensicLimits.Default;
            int requestedRows = allRows ? descriptor.HeightRaw : 1;
            if (requestedRows > effective.MaxRowsPerFrame)
            {
                return Failure(
                    descriptor,
                    Array.Empty<ShpTsRleForensicRowScalar>(),
                    ShpTsRleForensicFailureCode.CommandBudgetExceeded,
                    0,
                    descriptor.DataAbsoluteOffset);
            }

            var rows = new List<ShpTsRleForensicRowScalar>(requestedRows);
            long cursor = descriptor.DataOffsetRaw;
            long frameEnd = descriptor.DataUpperBoundRelative;
            long frameCommands = 0;
            var header = new byte[2];
            for (int rowIndex = 0; rowIndex < requestedRows; rowIndex++)
            {
                try
                {
                    if (checked(cursor + 2) > frameEnd)
                    {
                        return Failure(
                            descriptor,
                            rows,
                            ShpTsRleForensicFailureCode.RowHeaderTruncated,
                            rowIndex,
                            checked(source.AbsoluteStartOffset + cursor));
                    }

                    source.ReadExactly(cursor, header, 0, 2);
                    ushort lineLength = (ushort)(header[0] | (header[1] << 8));
                    if (lineLength < 2)
                    {
                        return Failure(
                            descriptor,
                            rows,
                            ShpTsRleForensicFailureCode.LineLengthTooSmall,
                            rowIndex,
                            checked(source.AbsoluteStartOffset + cursor));
                    }
                    if (lineLength > effective.MaxLineBytes)
                    {
                        return Failure(
                            descriptor,
                            rows,
                            ShpTsRleForensicFailureCode.LineLengthBudgetExceeded,
                            rowIndex,
                            checked(source.AbsoluteStartOffset + cursor));
                    }

                    long rowEnd = checked(cursor + lineLength);
                    if (rowEnd > frameEnd)
                    {
                        return Failure(
                            descriptor,
                            rows,
                            ShpTsRleForensicFailureCode.RowPayloadTruncated,
                            rowIndex,
                            checked(source.AbsoluteStartOffset + cursor + 2));
                    }

                    int payloadLength = lineLength - 2;
                    bool noHeaderAvailable = checked(rowEnd + 2) <= frameEnd;
                    int readLength = checked(payloadLength + (noHeaderAvailable ? 2 : 0));
                    var payload = new byte[readLength];
                    source.ReadExactly(checked(cursor + 2), payload, 0, readLength);

                    PayloadAnalysis standard = AnalyzePayload(
                        payload,
                        payloadLength,
                        descriptor.WidthRaw,
                        effective,
                        frameCommands);
                    frameCommands = checked(frameCommands + standard.CommandCount);
                    if (standard.FailureCode != ShpTsRleForensicFailureCode.None)
                    {
                        return Failure(
                            descriptor,
                            rows,
                            standard.FailureCode,
                            rowIndex,
                            checked(source.AbsoluteStartOffset + cursor + 2 + standard.FailureOffset));
                    }

                    PayloadAnalysis noHeader = noHeaderAvailable
                        ? AnalyzePayload(
                            payload,
                            readLength,
                            descriptor.WidthRaw,
                            effective,
                            frameCommands)
                        : PayloadAnalysis.Malformed;
                    rows.Add(CreateRowScalar(
                        rowIndex,
                        descriptor.WidthRaw,
                        lineLength,
                        standard,
                        noHeader));
                    cursor = rowEnd;
                }
                catch (OverflowException)
                {
                    return Failure(
                        descriptor,
                        rows,
                        ShpTsRleForensicFailureCode.ArithmeticOverflow,
                        rowIndex,
                        checked(source.AbsoluteStartOffset + Math.Min(cursor, source.Length)));
                }
                catch (ForensicReadException exception)
                {
                    return Failure(
                        descriptor,
                        rows,
                        ShpTsRleForensicFailureCode.ReadFailure,
                        rowIndex,
                        exception.AbsoluteOffset);
                }
            }

            return ShpTsRleForensicFrameAnalysis.Success(
                descriptor.Index,
                descriptor.WidthRaw,
                descriptor.HeightRaw,
                rows);
        }

        private static PayloadAnalysis AnalyzePayload(
            byte[] payload,
            int length,
            ushort width,
            ShpTsRleForensicLimits limits,
            long commandsBeforeFrame)
        {
            long commandCount = 0;
            long literalCount = 0;
            long zeroRunCount = 0;
            long zeroZeroCount = 0;
            long output = 0;
            long xccVisible = 0;
            long remainingAtWidth = -1;
            ShpTsRleForensicRemainingClass remainingClass =
                ShpTsRleForensicRemainingClass.NotReached;
            ShpTsRleForensicExtraSource extraSource = ShpTsRleForensicExtraSource.None;
            long extraCommandOrdinal = -1;
            long extraOvershoot = 0;
            ShpTsRleForensicCommandKind finalKind = ShpTsRleForensicCommandKind.None;
            int finalZeroRunCount = 0;
            long finalZeroRunDistance = -1;
            bool literalOverflow = false;
            int position = 0;
            while (position < length)
            {
                commandCount = checked(commandCount + 1);
                if (commandCount > limits.MaxCommandsPerRow ||
                    checked(commandsBeforeFrame + commandCount) > limits.MaxCommandsPerFrame)
                {
                    return PayloadAnalysis.Failure(
                        ShpTsRleForensicFailureCode.CommandBudgetExceeded,
                        position);
                }

                long before = output;
                byte command = payload[position++];
                if (command != 0)
                {
                    literalCount = checked(literalCount + 1);
                    output = checked(output + 1);
                    xccVisible = checked(xccVisible + 1);
                    finalKind = ShpTsRleForensicCommandKind.Literal;
                    finalZeroRunCount = 0;
                    finalZeroRunDistance = -1;
                    if (output > width)
                    {
                        literalOverflow = true;
                    }
                }
                else
                {
                    if (position >= length)
                    {
                        return PayloadAnalysis.Failure(
                            ShpTsRleForensicFailureCode.DanglingZero,
                            position - 1);
                    }

                    byte count = payload[position++];
                    if (count == 0)
                    {
                        zeroZeroCount = checked(zeroZeroCount + 1);
                        finalKind = ShpTsRleForensicCommandKind.ZeroZero;
                        finalZeroRunCount = 0;
                        finalZeroRunDistance = -1;
                    }
                    else
                    {
                        zeroRunCount = checked(zeroRunCount + 1);
                        output = checked(output + count);
                        long visibleRoom = Math.Max(0, checked((long)width - xccVisible));
                        xccVisible = checked(xccVisible + Math.Min(visibleRoom, count));
                        finalKind = ShpTsRleForensicCommandKind.ZeroRun;
                        finalZeroRunCount = count;
                        finalZeroRunDistance = checked((long)width - before);
                    }
                }

                if (remainingAtWidth < 0 && before < width && output >= width)
                {
                    remainingAtWidth = length - position;
                    remainingClass = ClassifyRemaining(payload, position, length);
                }

                if (extraSource == ShpTsRleForensicExtraSource.None &&
                    before <= width && output > width)
                {
                    extraSource = finalKind == ShpTsRleForensicCommandKind.Literal
                        ? ShpTsRleForensicExtraSource.Literal
                        : finalKind == ShpTsRleForensicCommandKind.ZeroRun
                            ? ShpTsRleForensicExtraSource.ZeroRun
                            : ShpTsRleForensicExtraSource.Malformed;
                    extraCommandOrdinal = commandCount;
                    extraOvershoot = checked(output - width);
                }
            }

            return new PayloadAnalysis(
                commandCount,
                literalCount,
                zeroRunCount,
                zeroZeroCount,
                output,
                xccVisible,
                remainingAtWidth,
                remainingClass,
                extraSource,
                extraCommandOrdinal,
                extraOvershoot,
                finalKind,
                finalZeroRunCount,
                finalZeroRunDistance,
                literalOverflow,
                ShpTsRleForensicFailureCode.None,
                -1);
        }

        private static ShpTsRleForensicRowScalar CreateRowScalar(
            int rowIndex,
            ushort width,
            ushort lineLength,
            PayloadAnalysis standard,
            PayloadAnalysis noHeader)
        {
            bool extraFromLast = standard.ExtraCommandOrdinal > 0 &&
                standard.ExtraCommandOrdinal == standard.CommandCount;
            bool extraIsLastOutput = standard.MechanicalOutput == checked((long)width + 1);
            bool extraIsZero = standard.ExtraSource == ShpTsRleForensicExtraSource.ZeroRun;
            bool inputExact = standard.FailureCode == ShpTsRleForensicFailureCode.None;
            bool ignoreOneExtraInputExact = extraIsLastOutput && inputExact;
            bool guardPattern =
                standard.ExtraSource == ShpTsRleForensicExtraSource.ZeroRun &&
                extraFromLast && extraIsLastOutput && extraIsZero &&
                standard.ExtraOvershoot == 1 && inputExact &&
                standard.FinalKind == ShpTsRleForensicCommandKind.ZeroRun;
            return new ShpTsRleForensicRowScalar(
                rowIndex,
                width,
                lineLength,
                standard.CommandCount,
                standard.LiteralCount,
                standard.ZeroRunCount,
                standard.ZeroZeroCount,
                standard.MechanicalOutput,
                standard.XccVisibleOutput,
                noHeader.MechanicalOutput,
                noHeader.FailureCode != ShpTsRleForensicFailureCode.None,
                standard.ExtraSource,
                extraFromLast,
                extraIsLastOutput,
                extraIsZero,
                ignoreOneExtraInputExact,
                standard.FinalKind,
                standard.FinalZeroRunCount,
                standard.FinalZeroRunDistance,
                standard.ExtraOvershoot,
                standard.RemainingAtWidth,
                standard.RemainingClass,
                inputExact,
                standard.LiteralOverflow,
                guardPattern);
        }

        private static ShpTsRleForensicRemainingClass ClassifyRemaining(
            byte[] payload,
            int position,
            int length)
        {
            int remaining = length - position;
            if (remaining == 0)
            {
                return ShpTsRleForensicRemainingClass.End;
            }
            if (remaining == 1)
            {
                return payload[position] == 0
                    ? ShpTsRleForensicRemainingClass.IncompleteCommand
                    : ShpTsRleForensicRemainingClass.OneByte;
            }
            return remaining == 2
                ? ShpTsRleForensicRemainingClass.TwoBytes
                : ShpTsRleForensicRemainingClass.ThreeOrMore;
        }

        private static ShpTsRleForensicFrameAnalysis Failure(
            ShpTsFrameDescriptor descriptor,
            IEnumerable<ShpTsRleForensicRowScalar> rows,
            ShpTsRleForensicFailureCode code,
            int rowIndex,
            long absoluteOffset)
        {
            return ShpTsRleForensicFrameAnalysis.Failure(
                descriptor.Index,
                descriptor.WidthRaw,
                descriptor.HeightRaw,
                rows,
                code,
                rowIndex,
                absoluteOffset);
        }

        private interface IInput : IDisposable
        {
            long Length { get; }
            long AbsoluteStartOffset { get; }
            void ReadExactly(long relativeOffset, byte[] destination, int offset, int count);
        }

        private sealed class MemoryInput : IInput
        {
            private readonly ReadOnlyMemory<byte> memory;

            public MemoryInput(ReadOnlyMemory<byte> memory, long absoluteStartOffset)
            {
                this.memory = memory;
                AbsoluteStartOffset = absoluteStartOffset;
            }

            public long Length => memory.Length;
            public long AbsoluteStartOffset { get; }

            public void ReadExactly(long relativeOffset, byte[] destination, int offset, int count)
            {
                ValidateRange(Length, relativeOffset, destination, offset, count);
                memory.Slice(checked((int)relativeOffset), count)
                    .CopyTo(destination.AsMemory(offset, count));
            }

            public void Dispose()
            {
            }
        }

        private sealed class StreamInput : IInput
        {
            private readonly Stream stream;
            private readonly bool leaveOpen;

            public StreamInput(
                Stream stream,
                long length,
                long absoluteStartOffset,
                bool leaveOpen)
            {
                this.stream = stream ?? throw new ArgumentNullException(nameof(stream));
                if (length < 0 || !stream.CanRead || !stream.CanSeek)
                {
                    throw new ArgumentException("The forensic stream must be bounded, readable, and seekable.");
                }

                Length = length;
                AbsoluteStartOffset = absoluteStartOffset;
                this.leaveOpen = leaveOpen;
            }

            public long Length { get; }
            public long AbsoluteStartOffset { get; }

            public void ReadExactly(long relativeOffset, byte[] destination, int offset, int count)
            {
                ValidateRange(Length, relativeOffset, destination, offset, count);
                try
                {
                    if (stream.Seek(relativeOffset, SeekOrigin.Begin) != relativeOffset)
                    {
                        throw new IOException("The forensic stream did not seek exactly.");
                    }
                    int total = 0;
                    while (total < count)
                    {
                        int read = stream.Read(destination, offset + total, count - total);
                        if (read <= 0)
                        {
                            throw new EndOfStreamException();
                        }
                        total = checked(total + read);
                    }
                }
                catch (Exception exception) when (
                    exception is IOException ||
                    exception is NotSupportedException ||
                    exception is ObjectDisposedException)
                {
                    throw new ForensicReadException(
                        checked(AbsoluteStartOffset + relativeOffset),
                        exception);
                }
            }

            public void Dispose()
            {
                if (!leaveOpen)
                {
                    stream.Dispose();
                }
            }
        }

        private sealed class WindowInput : IInput
        {
            private readonly ReadOnlyDataWindow window;

            public WindowInput(ReadOnlyDataWindow window)
            {
                this.window = window ?? throw new ArgumentNullException(nameof(window));
            }

            public long Length => window.Length;
            public long AbsoluteStartOffset => window.AbsoluteStartOffset;

            public void ReadExactly(long relativeOffset, byte[] destination, int offset, int count)
            {
                ValidateRange(Length, relativeOffset, destination, offset, count);
                try
                {
                    window.ReadExactly(
                        relativeOffset,
                        destination,
                        offset,
                        count,
                        "shp-rle-forensic-row-payload");
                }
                catch (BinaryReadException exception)
                {
                    throw new ForensicReadException(exception.Diagnostic.AbsoluteOffset, exception);
                }
            }

            public void Dispose()
            {
            }
        }

        private static void ValidateRange(
            long length,
            long relativeOffset,
            byte[] destination,
            int offset,
            int count)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }
            if (relativeOffset < 0 || count < 0 || offset < 0 ||
                offset > destination.Length - count ||
                relativeOffset > length - count)
            {
                throw new ArgumentOutOfRangeException(nameof(relativeOffset));
            }
        }

        private sealed class ForensicReadException : IOException
        {
            public ForensicReadException(long absoluteOffset, Exception inner)
                : base("The bounded forensic read failed.", inner)
            {
                AbsoluteOffset = absoluteOffset;
            }

            public long AbsoluteOffset { get; }
        }

        private sealed class PayloadAnalysis
        {
            public static PayloadAnalysis Malformed { get; } = Failure(
                ShpTsRleForensicFailureCode.RowPayloadTruncated,
                0);

            public PayloadAnalysis(
                long commandCount,
                long literalCount,
                long zeroRunCount,
                long zeroZeroCount,
                long mechanicalOutput,
                long xccVisibleOutput,
                long remainingAtWidth,
                ShpTsRleForensicRemainingClass remainingClass,
                ShpTsRleForensicExtraSource extraSource,
                long extraCommandOrdinal,
                long extraOvershoot,
                ShpTsRleForensicCommandKind finalKind,
                int finalZeroRunCount,
                long finalZeroRunDistance,
                bool literalOverflow,
                ShpTsRleForensicFailureCode failureCode,
                int failureOffset)
            {
                CommandCount = commandCount;
                LiteralCount = literalCount;
                ZeroRunCount = zeroRunCount;
                ZeroZeroCount = zeroZeroCount;
                MechanicalOutput = mechanicalOutput;
                XccVisibleOutput = xccVisibleOutput;
                RemainingAtWidth = remainingAtWidth;
                RemainingClass = remainingClass;
                ExtraSource = extraSource;
                ExtraCommandOrdinal = extraCommandOrdinal;
                ExtraOvershoot = extraOvershoot;
                FinalKind = finalKind;
                FinalZeroRunCount = finalZeroRunCount;
                FinalZeroRunDistance = finalZeroRunDistance;
                LiteralOverflow = literalOverflow;
                FailureCode = failureCode;
                FailureOffset = failureOffset;
            }

            public long CommandCount { get; }
            public long LiteralCount { get; }
            public long ZeroRunCount { get; }
            public long ZeroZeroCount { get; }
            public long MechanicalOutput { get; }
            public long XccVisibleOutput { get; }
            public long RemainingAtWidth { get; }
            public ShpTsRleForensicRemainingClass RemainingClass { get; }
            public ShpTsRleForensicExtraSource ExtraSource { get; }
            public long ExtraCommandOrdinal { get; }
            public long ExtraOvershoot { get; }
            public ShpTsRleForensicCommandKind FinalKind { get; }
            public int FinalZeroRunCount { get; }
            public long FinalZeroRunDistance { get; }
            public bool LiteralOverflow { get; }
            public ShpTsRleForensicFailureCode FailureCode { get; }
            public int FailureOffset { get; }

            public static PayloadAnalysis Failure(
                ShpTsRleForensicFailureCode code,
                int offset)
            {
                return new PayloadAnalysis(
                    0, 0, 0, 0, 0, 0, -1,
                    ShpTsRleForensicRemainingClass.NotReached,
                    ShpTsRleForensicExtraSource.Malformed,
                    -1, 0, ShpTsRleForensicCommandKind.DanglingZero,
                    0, -1, false, code, offset);
            }
        }
    }
}
