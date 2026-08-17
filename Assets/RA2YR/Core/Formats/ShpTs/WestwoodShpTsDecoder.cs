using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RA2YR.Core.Binary;
using RA2YR.Core.Binary.Seekable;

namespace RA2YR.Core.Formats.ShpTs
{
    internal static class WestwoodShpTsDecoder
    {
        public static ShpTsDecodeResult DecodeFrame(
            ReadOnlyMemory<byte> input,
            ShpTsDocument document,
            int frameIndex,
            ShpTsReadLimits limits = null)
        {
            return DecodeFrame(input, document, frameIndex, limits, ShpTsRleRowPolicy.StrictDeclaredWidth);
        }

        internal static ShpTsDecodeResult DecodeFrame(
            ReadOnlyMemory<byte> input,
            ShpTsDocument document,
            int frameIndex,
            ShpTsReadLimits limits,
            ShpTsRleRowPolicy rowPolicy)
        {
            ValidateDocument(document, input.Length, frameIndex);
            return DecodeMemoryCore(
                input,
                document,
                frameIndex,
                limits ?? ShpTsReadLimits.Default,
                0,
                rowPolicy);
        }

        public static ShpTsDecodeResult DecodeFrame(
            Stream stream,
            long inputLength,
            ShpTsDocument document,
            int frameIndex,
            ShpTsReadLimits limits = null,
            bool leaveOpen = false)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            ValidateDocument(document, inputLength, frameIndex);
            BinaryReadSession session = null;
            try
            {
                ShpTsReadLimits effective = limits ?? ShpTsReadLimits.Default;
                session = BinaryReadSession.FromStream(
                    stream,
                    inputLength,
                    document.Source,
                    effective.ToBinaryLimits(),
                    leaveOpen,
                    document.AbsoluteStartOffset);
                return DecodeSession(session, document, frameIndex, effective, ShpTsRleRowPolicy.StrictDeclaredWidth);
            }
            catch (ShpTsReadException exception)
            {
                return ShpTsDecodeResult.Failure(exception.Diagnostic);
            }
            catch (BinaryReadException exception)
            {
                return ShpTsDecodeResult.Failure(WestwoodShpTsReader.MapBinaryFailure(
                    exception.Diagnostic,
                    document.Source,
                    document.Provenance,
                    frameIndex,
                    -1));
            }
            finally
            {
                session?.Dispose();
            }
        }

        public static ShpTsDecodeResult DecodeFrame(
            ReadOnlyDataWindow window,
            ShpTsDocument document,
            int frameIndex,
            ShpTsReadLimits limits = null)
        {
            if (window == null)
            {
                throw new ArgumentNullException(nameof(window));
            }

            ValidateDocument(document, window.Length, frameIndex);
            ShpTsReadLimits effective = limits ?? ShpTsReadLimits.Default;
            if (window.AbsoluteStartOffset != document.AbsoluteStartOffset)
            {
                throw new ArgumentException("The SHP window start does not match the parsed document.", nameof(window));
            }

            if (window.Length > effective.MaxInputBytes ||
                window.Length > effective.MaxAllocatedBytes ||
                window.Length > int.MaxValue)
            {
                return ShpTsDecodeResult.Failure(new ShpTsDiagnostic(
                    BinaryDiagnosticSeverity.Error,
                    window.Length > effective.MaxInputBytes
                        ? ShpTsDiagnosticCode.InputBudgetExceeded
                        : ShpTsDiagnosticCode.AllocationBudgetExceeded,
                    document.Source,
                    document.Provenance,
                    window.AbsoluteStartOffset,
                    window.Length,
                    Math.Min(effective.MaxInputBytes, effective.MaxAllocatedBytes),
                    "shp-window-snapshot",
                    frameIndex,
                    -1,
                    "The SHP window cannot be snapshotted within the decode budget."));
            }

            byte[] snapshot;
            try
            {
                snapshot = new byte[checked((int)window.Length)];
                window.ReadExactly(0, snapshot, 0, snapshot.Length, "shp-window-input");
            }
            catch (BinaryReadException exception)
            {
                return ShpTsDecodeResult.Failure(WestwoodShpTsReader.MapBinaryFailure(
                    exception.Diagnostic,
                    document.Source,
                    document.Provenance,
                    frameIndex,
                    -1));
            }

            return DecodeMemoryCore(snapshot, document, frameIndex, effective, snapshot.LongLength, ShpTsRleRowPolicy.StrictDeclaredWidth);
        }

        public static ShpTsDecodeDocumentResult DecodeAll(
            ReadOnlyMemory<byte> input,
            ShpTsDocument document,
            ShpTsReadLimits limits = null)
        {
            ValidateDocument(document, input.Length, 0);
            ShpTsReadLimits effective = limits ?? ShpTsReadLimits.Default;
            var frames = new List<ShpTsIndexedLocalFrame>(document.Frames.Count);
            var diagnostics = new List<ShpTsDiagnostic>();
            long totalPixels = 0;
            foreach (ShpTsFrameDescriptor descriptor in document.Frames)
            {
                long updated;
                try
                {
                    updated = checked(totalPixels + checked((long)descriptor.WidthRaw * descriptor.HeightRaw));
                }
                catch (OverflowException)
                {
                    diagnostics.Add(DirectError(document, descriptor.Index, -1,
                        ShpTsDiagnosticCode.ArithmeticOverflow,
                        descriptor.DescriptorAbsoluteOffset + 4, 4, 0,
                        "shp-total-decoded-pixels",
                        "The cumulative decoded pixel count overflowed."));
                    return ShpTsDecodeDocumentResult.Failure(diagnostics);
                }

                if (updated > effective.MaxTotalDecodedPixels)
                {
                    diagnostics.Add(DirectError(document, descriptor.Index, -1,
                        ShpTsDiagnosticCode.TotalDecodedPixelBudgetExceeded,
                        descriptor.DescriptorAbsoluteOffset + 4, updated,
                        effective.MaxTotalDecodedPixels,
                        "shp-total-decoded-pixels",
                        "The cumulative decoded pixel budget would be exceeded."));
                    return ShpTsDecodeDocumentResult.Failure(diagnostics);
                }

                totalPixels = updated;
                ShpTsDecodeResult result = DecodeFrame(input, document, descriptor.Index, effective);
                diagnostics.AddRange(result.Diagnostics);
                if (!result.IsSuccess)
                {
                    return ShpTsDecodeDocumentResult.Failure(diagnostics);
                }

                frames.Add(result.Frame);
            }

            AddOverlapDiagnostics(document, frames, diagnostics, effective);
            if (diagnostics.Any(item => item.Severity == BinaryDiagnosticSeverity.Error))
            {
                return ShpTsDecodeDocumentResult.Failure(diagnostics);
            }

            return ShpTsDecodeDocumentResult.Success(
                new ShpTsDecodedDocument(frames),
                diagnostics);
        }

        private static ShpTsDecodeResult DecodeMemoryCore(
            ReadOnlyMemory<byte> input,
            ShpTsDocument document,
            int frameIndex,
            ShpTsReadLimits limits,
            long initialAllocation,
            ShpTsRleRowPolicy rowPolicy)
        {
            BinaryReadSession session = null;
            try
            {
                session = BinaryReadSession.FromMemory(
                    input,
                    document.Source,
                    limits.ToBinaryLimits(),
                    document.AbsoluteStartOffset);
                if (initialAllocation != 0)
                {
                    session.ReserveAllocation(
                        initialAllocation,
                        document.AbsoluteStartOffset,
                        input.Length,
                        "shp-window-snapshot");
                }

                return DecodeSession(session, document, frameIndex, limits, rowPolicy);
            }
            catch (ShpTsReadException exception)
            {
                return ShpTsDecodeResult.Failure(exception.Diagnostic);
            }
            catch (BinaryReadException exception)
            {
                return ShpTsDecodeResult.Failure(WestwoodShpTsReader.MapBinaryFailure(
                    exception.Diagnostic,
                    document.Source,
                    document.Provenance,
                    frameIndex,
                    -1));
            }
            finally
            {
                session?.Dispose();
            }
        }

        private static ShpTsDecodeResult DecodeSession(
            BinaryReadSession session,
            ShpTsDocument document,
            int frameIndex,
            ShpTsReadLimits limits,
            ShpTsRleRowPolicy rowPolicy)
        {
            ShpTsFrameDescriptor descriptor = document.Frames[frameIndex];
            if (descriptor.IsCanonicalEmpty)
            {
                return ShpTsDecodeResult.Success(new ShpTsIndexedLocalFrame(
                    frameIndex,
                    0,
                    0,
                    descriptor.CompressionKind,
                    0,
                    0,
                    Array.Empty<byte>()));
            }

            if (descriptor.CompressionKind == ShpTsCompressionKind.SourceConflictingFlags2)
            {
                return ShpTsDecodeResult.Failure(DirectError(
                    document, frameIndex, -1, ShpTsDiagnosticCode.SourceConflictingFlags2,
                    descriptor.DescriptorAbsoluteOffset + 8, 4, 0, "shp-frame-flags",
                    "Raw flags value 2 has conflicting source semantics and is not decoded."));
            }

            if (descriptor.CompressionKind == ShpTsCompressionKind.UnknownFlags)
            {
                return ShpTsDecodeResult.Failure(DirectError(
                    document, frameIndex, -1, ShpTsDiagnosticCode.UnknownFlags,
                    descriptor.DescriptorAbsoluteOffset + 8, 4, 0, "shp-frame-flags",
                    "Unknown raw flags are preserved but not decoded."));
            }

            long rangeLength = checked(descriptor.DataUpperBoundRelative - descriptor.DataOffsetRaw);
            BoundedBinaryReader frameReader = session.Root.CreateSubrangeAt(
                descriptor.DataOffsetRaw,
                rangeLength,
                "shp-frame-data-window");
            if (descriptor.CompressionKind == ShpTsCompressionKind.RawOpaque ||
                descriptor.CompressionKind == ShpTsCompressionKind.RawTransparent)
            {
                return DecodeRaw(frameReader, document, descriptor);
            }

            return DecodeRle(frameReader, session, document, descriptor, limits, rowPolicy);
        }

        private static ShpTsDecodeResult DecodeRaw(
            BoundedBinaryReader reader,
            ShpTsDocument document,
            ShpTsFrameDescriptor descriptor)
        {
            long area = checked((long)descriptor.WidthRaw * descriptor.HeightRaw);
            if (area > reader.RemainingLength)
            {
                return ShpTsDecodeResult.Failure(DirectError(
                    document, descriptor.Index, -1, ShpTsDiagnosticCode.RawPayloadTruncated,
                    reader.AbsoluteOffset, area, reader.RemainingLength,
                    "shp-raw-payload", "The raw frame payload is truncated."));
            }

            byte[] indices = reader.ReadBytes(area, "shp-raw-payload");
            long padding = reader.RemainingLength;
            if (padding != 0)
            {
                reader.Skip(padding, "shp-frame-padding");
            }

            reader.Complete(TrailingDataPolicy.RequireFullyConsumed, "shp-frame-data-window");
            return ShpTsDecodeResult.Success(new ShpTsIndexedLocalFrame(
                descriptor.Index,
                descriptor.WidthRaw,
                descriptor.HeightRaw,
                descriptor.CompressionKind,
                area,
                padding,
                indices));
        }

        private static ShpTsDecodeResult DecodeRle(
            BoundedBinaryReader reader,
            BinaryReadSession session,
            ShpTsDocument document,
            ShpTsFrameDescriptor descriptor,
            ShpTsReadLimits limits,
            ShpTsRleRowPolicy rowPolicy)
        {
            long area = checked((long)descriptor.WidthRaw * descriptor.HeightRaw);
            session.ReserveAllocation(
                area,
                reader.AbsoluteOffset,
                reader.RemainingLength,
                "shp-decoded-indices");
            byte[] output;
            try
            {
                output = new byte[checked((int)area)];
            }
            catch (OutOfMemoryException)
            {
                return ShpTsDecodeResult.Failure(DirectError(
                    document, descriptor.Index, -1,
                    ShpTsDiagnosticCode.AllocationBudgetExceeded,
                    reader.AbsoluteOffset, area, reader.RemainingLength,
                    "shp-decoded-indices",
                    "The validated decoded-index allocation failed."));
            }

            int outputOffset = 0;
            long frameCommandCount = 0;
            for (int row = 0; row < descriptor.HeightRaw; row++)
            {
                long rowHeaderOffset = reader.AbsoluteOffset;
                ushort lineLength;
                try
                {
                    lineLength = reader.ReadUInt16("shp-rle-line-length");
                }
                catch (BinaryReadException exception)
                {
                    return ShpTsDecodeResult.Failure(WestwoodShpTsReader.MapBinaryFailure(
                        exception.Diagnostic, document.Source, document.Provenance,
                        descriptor.Index, row));
                }

                if (lineLength < 2)
                {
                    return ShpTsDecodeResult.Failure(DirectError(
                        document, descriptor.Index, row,
                        ShpTsDiagnosticCode.RleLineLengthTooSmall,
                        rowHeaderOffset, lineLength, reader.RemainingLength,
                        "shp-rle-line-length",
                        "An RLE line length must include its two-byte header."));
                }

                if (lineLength > limits.MaxSingleRowBytes)
                {
                    return ShpTsDecodeResult.Failure(DirectError(
                        document, descriptor.Index, row,
                        ShpTsDiagnosticCode.RleLineLengthBudgetExceeded,
                        rowHeaderOffset, lineLength, limits.MaxSingleRowBytes,
                        "shp-rle-line-length",
                        "The RLE line length exceeds its explicit budget."));
                }

                int payloadLength = lineLength - 2;
                if (payloadLength > reader.RemainingLength)
                {
                    return ShpTsDecodeResult.Failure(DirectError(
                        document, descriptor.Index, row,
                        ShpTsDiagnosticCode.RleLineTruncated,
                        reader.AbsoluteOffset, payloadLength, reader.RemainingLength,
                        "shp-rle-line-payload", "The RLE line payload is truncated."));
                }

                BoundedBinaryReader rowReader = reader.ReadSubrange(
                    payloadLength,
                    "shp-rle-line-payload");
                int rowOutput = 0;
                bool validatedTrailingGuard = false;
                int rowCommands = 0;
                while (!rowReader.IsEndOfInput)
                {
                    rowCommands = checked(rowCommands + 1);
                    frameCommandCount = checked(frameCommandCount + 1);
                    if (rowCommands > limits.MaxCommandsPerRow ||
                        frameCommandCount > limits.MaxCommandsPerFrame)
                    {
                        return ShpTsDecodeResult.Failure(DirectError(
                            document, descriptor.Index, row,
                            ShpTsDiagnosticCode.CommandBudgetExceeded,
                            rowReader.AbsoluteOffset, rowCommands,
                            rowReader.RemainingLength, "shp-rle-commands",
                            "The RLE command budget was exceeded."));
                    }

                    byte command = rowReader.ReadUInt8("shp-rle-command");
                    if (command != 0)
                    {
                        if (rowOutput >= descriptor.WidthRaw)
                        {
                            return ShpTsDecodeResult.Failure(DirectError(
                                document, descriptor.Index, row,
                                ShpTsDiagnosticCode.RleOutputOverflow,
                                rowReader.AbsoluteOffset - 1, 1,
                                rowReader.RemainingLength, "shp-rle-literal",
                                "An RLE literal exceeds the declared row width: width=" +
                                descriptor.WidthRaw + ", row=" + row +
                                ", lineLength=" + lineLength +
                                ", produced=" + rowOutput + "."));
                        }

                        output[outputOffset + rowOutput++] = command;
                        continue;
                    }

                    if (rowReader.IsEndOfInput)
                    {
                        return ShpTsDecodeResult.Failure(DirectError(
                            document, descriptor.Index, row,
                            ShpTsDiagnosticCode.RleDanglingZeroCommand,
                            rowReader.AbsoluteOffset - 1, 1, 0,
                            "shp-rle-zero-run",
                            "An RLE zero command is missing its count byte."));
                    }

                    byte count = rowReader.ReadUInt8("shp-rle-zero-count");
                    if (count == 0)
                    {
                        return ShpTsDecodeResult.Failure(DirectError(
                            document, descriptor.Index, row,
                            ShpTsDiagnosticCode.ZeroOutputCommandSemanticsUnresolved,
                            rowReader.AbsoluteOffset - 2, 2,
                            rowReader.RemainingLength, "shp-rle-zero-run",
                            "The 00 00 command was consumed but its acceptance semantics remain unresolved."));
                    }

                    int nextOutput = checked(rowOutput + count);
                    if (nextOutput > descriptor.WidthRaw)
                    {
                        if (rowPolicy == ShpTsRleRowPolicy.ValidatedTrailingTransparentGuard &&
                            !validatedTrailingGuard && nextOutput == descriptor.WidthRaw + 1 &&
                            rowReader.IsEndOfInput)
                        {
                            rowOutput = nextOutput;
                            validatedTrailingGuard = true;
                            continue;
                        }
                        return ShpTsDecodeResult.Failure(DirectError(
                            document, descriptor.Index, row,
                            ShpTsDiagnosticCode.RleOutputOverflow,
                            rowReader.AbsoluteOffset - 2, 2,
                            rowReader.RemainingLength, "shp-rle-zero-run",
                            "An RLE zero run exceeds the declared row width: width=" +
                            descriptor.WidthRaw + ", row=" + row +
                            ", lineLength=" + lineLength +
                            ", produced=" + rowOutput +
                            ", count=" + count + "."));
                    }

                    rowOutput += count;
                }

                rowReader.Complete(TrailingDataPolicy.RequireFullyConsumed, "shp-rle-line-payload");
                if (validatedTrailingGuard) rowOutput = descriptor.WidthRaw;
                if (rowOutput != descriptor.WidthRaw)
                {
                    return ShpTsDecodeResult.Failure(DirectError(
                        document, descriptor.Index, row,
                        ShpTsDiagnosticCode.RleOutputUnderflow,
                        rowHeaderOffset, lineLength, 0,
                        "shp-rle-row-output",
                        "The RLE row output is shorter than the declared width."));
                }

                outputOffset = checked(outputOffset + rowOutput);
                if (reader.Position > limits.MaxSingleFrameCompressedBytes)
                {
                    return ShpTsDecodeResult.Failure(DirectError(
                        document, descriptor.Index, row,
                        ShpTsDiagnosticCode.CompressedFrameBudgetExceeded,
                        reader.AbsoluteOffset, reader.Position,
                        limits.MaxSingleFrameCompressedBytes,
                        "shp-rle-frame-bytes",
                        "The compressed frame byte budget was exceeded."));
                }
            }

            if (outputOffset != output.Length)
            {
                return ShpTsDecodeResult.Failure(DirectError(
                    document, descriptor.Index, descriptor.HeightRaw,
                    ShpTsDiagnosticCode.RleRowCountIncomplete,
                    reader.AbsoluteOffset, output.Length - outputOffset,
                    reader.RemainingLength, "shp-rle-frame-output",
                    "The decoded frame did not complete its declared row count."));
            }

            long consumed = reader.Position;
            long padding = reader.RemainingLength;
            if (padding != 0)
            {
                reader.Skip(padding, "shp-frame-padding");
            }

            reader.Complete(TrailingDataPolicy.RequireFullyConsumed, "shp-frame-data-window");
            return ShpTsDecodeResult.Success(new ShpTsIndexedLocalFrame(
                descriptor.Index,
                descriptor.WidthRaw,
                descriptor.HeightRaw,
                descriptor.CompressionKind,
                consumed,
                padding,
                output));
        }

        private static void AddOverlapDiagnostics(
            ShpTsDocument document,
            IReadOnlyList<ShpTsIndexedLocalFrame> frames,
            IList<ShpTsDiagnostic> diagnostics,
            ShpTsReadLimits limits)
        {
            var ranges = frames.Where(frame => frame.BytesConsumed != 0)
                .Select(frame => new
                {
                    Frame = frame,
                    Start = (long)document.Frames[frame.FrameIndex].DataOffsetRaw,
                    End = checked((long)document.Frames[frame.FrameIndex].DataOffsetRaw + frame.BytesConsumed)
                })
                .OrderBy(item => item.Start)
                .ThenBy(item => item.Frame.FrameIndex)
                .ToArray();
            for (int index = 1; index < ranges.Length; index++)
            {
                if (ranges[index].Start < ranges[index - 1].End)
                {
                    if (diagnostics.Count >= limits.MaxDiagnostics)
                    {
                        diagnostics.Add(DirectError(
                            document, ranges[index].Frame.FrameIndex, -1,
                            ShpTsDiagnosticCode.DiagnosticBudgetExceeded,
                            document.AbsoluteStartOffset + ranges[index].Start,
                            ranges[index].Frame.BytesConsumed, 0,
                            "shp-diagnostics",
                            "The SHP diagnostic budget was exceeded."));
                        return;
                    }

                    diagnostics.Add(new ShpTsDiagnostic(
                        BinaryDiagnosticSeverity.Warning,
                        ShpTsDiagnosticCode.FrameDataOverlap,
                        document.Source,
                        document.Provenance,
                        document.AbsoluteStartOffset + ranges[index].Start,
                        ranges[index].Frame.BytesConsumed,
                        Math.Max(0, ranges[index - 1].End - ranges[index].Start),
                        "shp-frame-data-overlap",
                        ranges[index].Frame.FrameIndex,
                        -1,
                        "Decoded frame payload ranges overlap; no dependency was inferred."));
                }
            }
        }

        private static ShpTsDiagnostic DirectError(
            ShpTsDocument document,
            int frameIndex,
            int rowIndex,
            ShpTsDiagnosticCode code,
            long absoluteOffset,
            long requestedLength,
            long remainingLength,
            string field,
            string message)
        {
            return new ShpTsDiagnostic(
                BinaryDiagnosticSeverity.Error,
                code,
                document.Source,
                document.Provenance,
                absoluteOffset,
                requestedLength,
                remainingLength,
                field,
                frameIndex,
                rowIndex,
                message);
        }

        private static void ValidateDocument(
            ShpTsDocument document,
            long inputLength,
            int frameIndex)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (inputLength != document.InputLength)
            {
                throw new ArgumentException("The decode input length does not match the parsed document.", nameof(inputLength));
            }

            if (frameIndex < 0 || frameIndex >= document.Frames.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(frameIndex));
            }
        }
    }
}
