using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RA2YR.Core.Binary;
using RA2YR.Core.Binary.Seekable;

namespace RA2YR.Core.Formats.ShpTs
{
    internal static class WestwoodShpTsReader
    {
        private const int HeaderLength = 8;
        private const int DescriptorLength = 24;
        private const long DescriptorModelAllocationEstimate = 128;

        public static ShpTsParseResult Read(
            ReadOnlyMemory<byte> input,
            BinarySourceContext source,
            ShpTsSourceProvenance provenance,
            ShpTsReadLimits limits = null,
            long absoluteStartOffset = 0)
        {
            ValidateContext(source, provenance);
            return ReadMemoryCore(
                input,
                source,
                provenance,
                limits ?? ShpTsReadLimits.Default,
                absoluteStartOffset,
                0);
        }

        public static ShpTsParseResult Read(
            Stream stream,
            long inputLength,
            BinarySourceContext source,
            ShpTsSourceProvenance provenance,
            ShpTsReadLimits limits = null,
            bool leaveOpen = false,
            long absoluteStartOffset = 0)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            ValidateContext(source, provenance);
            BinaryReadSession session = null;
            try
            {
                ShpTsReadLimits effective = limits ?? ShpTsReadLimits.Default;
                session = BinaryReadSession.FromStream(
                    stream,
                    inputLength,
                    source,
                    effective.ToBinaryLimits(),
                    leaveOpen,
                    absoluteStartOffset);
                return ParseSession(session, source, provenance, effective);
            }
            catch (ShpTsReadException exception)
            {
                return ShpTsParseResult.Failure(exception.Diagnostic);
            }
            catch (BinaryReadException exception)
            {
                return ShpTsParseResult.Failure(MapBinaryFailure(
                    exception.Diagnostic,
                    source,
                    provenance,
                    -1,
                    -1));
            }
            finally
            {
                session?.Dispose();
            }
        }

        public static ShpTsParseResult ReadSeekable(
            Stream stream,
            BinarySourceContext source,
            ShpTsSourceProvenance provenance,
            ShpTsReadLimits limits = null,
            bool leaveOpen = false)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            ValidateContext(source, provenance);
            BinaryReadSession session = null;
            try
            {
                ShpTsReadLimits effective = limits ?? ShpTsReadLimits.Default;
                session = BinaryReadSession.FromSeekableStream(
                    stream,
                    source,
                    effective.ToBinaryLimits(),
                    leaveOpen);
                return ParseSession(session, source, provenance, effective);
            }
            catch (ShpTsReadException exception)
            {
                return ShpTsParseResult.Failure(exception.Diagnostic);
            }
            catch (BinaryReadException exception)
            {
                return ShpTsParseResult.Failure(MapBinaryFailure(
                    exception.Diagnostic,
                    source,
                    provenance,
                    -1,
                    -1));
            }
            finally
            {
                session?.Dispose();
            }
        }

        public static ShpTsParseResult Read(
            ReadOnlyDataWindow window,
            BinarySourceContext source,
            ShpTsSourceProvenance provenance,
            ShpTsReadLimits limits = null)
        {
            if (window == null)
            {
                throw new ArgumentNullException(nameof(window));
            }

            ValidateContext(source, provenance);
            ShpTsReadLimits effective = limits ?? ShpTsReadLimits.Default;
            if (window.Length > effective.MaxInputBytes || window.Length > int.MaxValue)
            {
                return ShpTsParseResult.Failure(DirectFailure(
                    window.Length > effective.MaxInputBytes
                        ? ShpTsDiagnosticCode.InputBudgetExceeded
                        : ShpTsDiagnosticCode.InvalidDataOffset,
                    source,
                    provenance,
                    window.AbsoluteStartOffset,
                    window.Length,
                    window.Length,
                    "shp-input",
                    "The bounded SHP window cannot be represented within its input budget.",
                    window.Length > effective.MaxInputBytes
                        ? BinaryDiagnosticCode.InputBudgetExceeded
                        : BinaryDiagnosticCode.InvalidLength));
            }

            if (window.Length > effective.MaxAllocatedBytes)
            {
                return ShpTsParseResult.Failure(DirectFailure(
                    ShpTsDiagnosticCode.AllocationBudgetExceeded,
                    source,
                    provenance,
                    window.AbsoluteStartOffset,
                    window.Length,
                    effective.MaxAllocatedBytes,
                    "shp-window-snapshot",
                    "The bounded SHP window exceeds its snapshot allocation budget.",
                    BinaryDiagnosticCode.AllocationBudgetExceeded));
            }

            byte[] snapshot;
            try
            {
                snapshot = new byte[checked((int)window.Length)];
                window.ReadExactly(0, snapshot, 0, snapshot.Length, "shp-window-input");
            }
            catch (BinaryReadException exception)
            {
                return ShpTsParseResult.Failure(MapBinaryFailure(
                    exception.Diagnostic,
                    source,
                    provenance,
                    -1,
                    -1));
            }
            catch (OutOfMemoryException)
            {
                return ShpTsParseResult.Failure(DirectFailure(
                    ShpTsDiagnosticCode.AllocationBudgetExceeded,
                    source,
                    provenance,
                    window.AbsoluteStartOffset,
                    window.Length,
                    effective.MaxAllocatedBytes,
                    "shp-window-snapshot",
                    "The validated SHP window snapshot could not be allocated.",
                    BinaryDiagnosticCode.AllocationBudgetExceeded));
            }

            return ReadMemoryCore(
                snapshot,
                source,
                provenance,
                effective,
                window.AbsoluteStartOffset,
                snapshot.LongLength);
        }

        private static ShpTsParseResult ReadMemoryCore(
            ReadOnlyMemory<byte> input,
            BinarySourceContext source,
            ShpTsSourceProvenance provenance,
            ShpTsReadLimits limits,
            long absoluteStartOffset,
            long initialAllocation)
        {
            BinaryReadSession session = null;
            try
            {
                session = BinaryReadSession.FromMemory(
                    input,
                    source,
                    limits.ToBinaryLimits(),
                    absoluteStartOffset);
                if (initialAllocation != 0)
                {
                    session.ReserveAllocation(
                        initialAllocation,
                        absoluteStartOffset,
                        input.Length,
                        "shp-window-snapshot");
                }

                return ParseSession(session, source, provenance, limits);
            }
            catch (ShpTsReadException exception)
            {
                return ShpTsParseResult.Failure(exception.Diagnostic);
            }
            catch (BinaryReadException exception)
            {
                return ShpTsParseResult.Failure(MapBinaryFailure(
                    exception.Diagnostic,
                    source,
                    provenance,
                    -1,
                    -1));
            }
            finally
            {
                session?.Dispose();
            }
        }

        private static ShpTsParseResult ParseSession(
            BinaryReadSession session,
            BinarySourceContext source,
            ShpTsSourceProvenance provenance,
            ShpTsReadLimits limits)
        {
            BoundedBinaryReader reader = session.Root;
            var diagnostics = new List<ShpTsDiagnostic>();
            ushort marker = reader.ReadUInt16("shp-family-marker");
            ushort canvasWidth = reader.ReadUInt16("shp-canvas-width");
            ushort canvasHeight = reader.ReadUInt16("shp-canvas-height");
            ushort frameCount = reader.ReadUInt16("shp-frame-count");
            if (marker != 0)
            {
                ThrowDirect(ShpTsDiagnosticCode.InvalidFamilyMarker, source, provenance,
                    reader.AbsoluteStartOffset, 2, reader.Length, "shp-family-marker", -1,
                    "The SHP family marker is not the confirmed SHP(TS) value.");
            }

            if (frameCount == 0)
            {
                ThrowDirect(ShpTsDiagnosticCode.ZeroFrameCount, source, provenance,
                    reader.AbsoluteStartOffset + 6, 2, reader.Length - 6, "shp-frame-count", -1,
                    "A zero-frame SHP(TS) is not accepted without golden evidence.");
            }

            if (frameCount > limits.MaxFrameCount)
            {
                ThrowDirect(ShpTsDiagnosticCode.FrameCountBudgetExceeded, source, provenance,
                    reader.AbsoluteStartOffset + 6, frameCount, limits.MaxFrameCount,
                    "shp-frame-count", -1, "The SHP frame count exceeds its explicit budget.");
            }

            if (frameCount > limits.MaxDescriptors)
            {
                ThrowDirect(ShpTsDiagnosticCode.DescriptorBudgetExceeded, source, provenance,
                    reader.AbsoluteStartOffset + 6, frameCount, limits.MaxDescriptors,
                    "shp-descriptor-count", -1,
                    "The SHP descriptor count exceeds its explicit budget.");
            }

            ValidateCanvas(canvasWidth, canvasHeight, reader, source, provenance, limits);
            long directoryLength;
            try
            {
                directoryLength = checked(HeaderLength + checked((long)frameCount * DescriptorLength));
            }
            catch (OverflowException)
            {
                ThrowDirect(ShpTsDiagnosticCode.DirectorySizeOverflow, source, provenance,
                    reader.AbsoluteStartOffset + 6, frameCount, reader.RemainingLength,
                    "shp-directory-size", -1, "The SHP directory size overflowed.");
                return null;
            }

            if (directoryLength > reader.Length)
            {
                ThrowDirect(ShpTsDiagnosticCode.UnexpectedEndOfInput, source, provenance,
                    reader.AbsoluteStartOffset + HeaderLength,
                    directoryLength - HeaderLength,
                    Math.Max(0, reader.Length - HeaderLength),
                    "shp-frame-directory", -1, "The SHP frame directory is truncated.",
                    BinaryDiagnosticCode.UnexpectedEndOfInput);
            }

            reader.ReserveRecords(frameCount, "shp-frame-descriptors");
            session.ReserveAllocation(
                checked(frameCount * DescriptorModelAllocationEstimate),
                reader.AbsoluteOffset,
                reader.RemainingLength,
                "shp-descriptor-model");

            var raw = new RawDescriptor[frameCount];
            for (int index = 0; index < frameCount; index++)
            {
                long descriptorOffset = reader.AbsoluteOffset;
                ushort x;
                ushort y;
                ushort width;
                ushort height;
                uint flags;
                byte[] color;
                uint reserved;
                uint dataOffset;
                try
                {
                    x = reader.ReadUInt16("shp-frame-x");
                    y = reader.ReadUInt16("shp-frame-y");
                    width = reader.ReadUInt16("shp-frame-width");
                    height = reader.ReadUInt16("shp-frame-height");
                    flags = reader.ReadUInt32("shp-frame-flags");
                    color = reader.ReadBytes(4, "shp-frame-color");
                    reserved = reader.ReadUInt32("shp-frame-reserved");
                    dataOffset = reader.ReadUInt32("shp-frame-data-offset");
                }
                catch (BinaryReadException exception)
                {
                    throw new ShpTsReadException(MapBinaryFailure(
                        exception.Diagnostic, source, provenance, index, -1));
                }

                bool empty = width == 0 && height == 0 && dataOffset == 0;
                bool anyEmptyPart = width == 0 || height == 0 || dataOffset == 0;
                if (anyEmptyPart && !empty)
                {
                    ThrowDirect(ShpTsDiagnosticCode.PartialEmptyFrame, source, provenance,
                        descriptorOffset, DescriptorLength, reader.RemainingLength,
                        "shp-empty-frame", index,
                        "A partial empty-frame descriptor is not accepted.");
                }

                if (empty)
                {
                    if (x != 0 || y != 0)
                    {
                        AddWarning(diagnostics, limits,
                            ShpTsDiagnosticCode.EmptyFrameCoordinatesNonZero,
                            source, provenance, descriptorOffset, DescriptorLength,
                            reader.RemainingLength, "shp-empty-frame", index,
                            "A canonical empty frame retains nonzero raw coordinates.");
                    }
                }
                else
                {
                    ValidateFrameGeometry(x, y, width, height, canvasWidth, canvasHeight,
                        descriptorOffset, reader, source, provenance, limits, index, diagnostics);
                    if (dataOffset < directoryLength)
                    {
                        ThrowDirect(ShpTsDiagnosticCode.DataOffsetInsideDirectory,
                            source, provenance, descriptorOffset + 20, 4,
                            reader.RemainingLength, "shp-frame-data-offset", index,
                            "A nonempty frame points inside the SHP directory.");
                    }

                    if (dataOffset >= reader.Length)
                    {
                        ThrowDirect(ShpTsDiagnosticCode.DataOffsetOutsideInput,
                            source, provenance, descriptorOffset + 20, 4,
                            Math.Max(0, reader.Length - dataOffset),
                            "shp-frame-data-offset", index,
                            "A nonempty frame points outside the SHP input window.");
                    }
                }

                if (reserved != 0)
                {
                    AddWarning(diagnostics, limits, ShpTsDiagnosticCode.ReservedFieldNonZero,
                        source, provenance, descriptorOffset + 16, 4, reader.RemainingLength,
                        "shp-frame-reserved", index,
                        "A nonzero reserved field was preserved without interpretation.");
                }

                if (!empty && (dataOffset & 7u) != 0)
                {
                    AddWarning(diagnostics, limits,
                        ShpTsDiagnosticCode.DataOffsetNotEightByteAligned,
                        source, provenance, descriptorOffset + 20, 4, reader.RemainingLength,
                        "shp-frame-data-offset", index,
                        "A frame data offset is not eight-byte aligned.");
                }

                ShpTsCompressionKind kind = DeriveCompression(flags);
                if (kind == ShpTsCompressionKind.SourceConflictingFlags2)
                {
                    AddWarning(diagnostics, limits, ShpTsDiagnosticCode.SourceConflictingFlags2,
                        source, provenance, descriptorOffset + 8, 4, reader.RemainingLength,
                        "shp-frame-flags", index,
                        "Raw flags value 2 is preserved but its decode semantics remain unresolved.");
                }
                else if (kind == ShpTsCompressionKind.UnknownFlags)
                {
                    AddWarning(diagnostics, limits, ShpTsDiagnosticCode.UnknownFlags,
                        source, provenance, descriptorOffset + 8, 4, reader.RemainingLength,
                        "shp-frame-flags", index,
                        "Unknown raw flags were preserved and are not decoded.");
                }

                raw[index] = new RawDescriptor(
                    index, descriptorOffset, x, y, width, height, flags, color,
                    reserved, dataOffset, kind, empty);
            }

            uint previous = 0;
            bool havePrevious = false;
            foreach (RawDescriptor item in raw.Where(item => !item.IsEmpty))
            {
                if (havePrevious && item.DataOffset < previous)
                {
                    AddWarning(diagnostics, limits, ShpTsDiagnosticCode.DescendingDataOffset,
                        source, provenance, item.DescriptorAbsoluteOffset + 20, 4,
                        reader.Length - item.DataOffset, "shp-frame-data-offset", item.Index,
                        "Frame data offsets are not in descriptor order.");
                }

                previous = item.DataOffset;
                havePrevious = true;
            }

            foreach (IGrouping<uint, RawDescriptor> group in raw
                         .Where(item => !item.IsEmpty)
                         .GroupBy(item => item.DataOffset)
                         .Where(group => group.Count() > 1))
            {
                foreach (RawDescriptor item in group)
                {
                    AddWarning(diagnostics, limits, ShpTsDiagnosticCode.DuplicateDataOffset,
                        source, provenance, item.DescriptorAbsoluteOffset + 20, 4,
                        reader.Length - item.DataOffset, "shp-frame-data-offset", item.Index,
                        "Multiple descriptors retain the same data offset; no dependency was inferred.");
                }
            }

            uint[] distinctOffsets = raw.Where(item => !item.IsEmpty)
                .Select(item => item.DataOffset)
                .Distinct()
                .OrderBy(value => value)
                .ToArray();
            var frames = new ShpTsFrameDescriptor[raw.Length];
            for (int index = 0; index < raw.Length; index++)
            {
                RawDescriptor item = raw[index];
                long upper = 0;
                long absolute = reader.AbsoluteStartOffset;
                if (!item.IsEmpty)
                {
                    upper = distinctOffsets.FirstOrDefault(value => value > item.DataOffset);
                    if (upper == 0)
                    {
                        upper = reader.Length;
                    }

                    absolute = checked(reader.AbsoluteStartOffset + item.DataOffset);
                }

                frames[index] = new ShpTsFrameDescriptor(
                    item.Index,
                    item.DescriptorAbsoluteOffset,
                    item.X,
                    item.Y,
                    item.Width,
                    item.Height,
                    item.Flags,
                    item.Color,
                    item.Reserved,
                    item.DataOffset,
                    absolute,
                    upper,
                    item.Kind,
                    item.IsEmpty);
            }

            return ShpTsParseResult.Success(new ShpTsDocument(
                source,
                provenance,
                reader.Length,
                reader.AbsoluteStartOffset,
                directoryLength,
                new ShpTsHeader(marker, canvasWidth, canvasHeight, frameCount),
                frames), diagnostics);
        }

        private static void ValidateCanvas(
            ushort width,
            ushort height,
            BoundedBinaryReader reader,
            BinarySourceContext source,
            ShpTsSourceProvenance provenance,
            ShpTsReadLimits limits)
        {
            if (width == 0 || height == 0 ||
                width > limits.MaxCanvasDimension || height > limits.MaxCanvasDimension)
            {
                ThrowDirect(ShpTsDiagnosticCode.CanvasDimensionBudgetExceeded,
                    source, provenance, reader.AbsoluteStartOffset + 2, 4,
                    reader.Length - 2, "shp-canvas", -1,
                    "The SHP canvas dimensions are zero or exceed their explicit budget.");
            }

            long area = checked((long)width * height);
            if (area > limits.MaxCanvasArea)
            {
                ThrowDirect(ShpTsDiagnosticCode.CanvasAreaBudgetExceeded,
                    source, provenance, reader.AbsoluteStartOffset + 2, area,
                    limits.MaxCanvasArea, "shp-canvas-area", -1,
                    "The SHP canvas area exceeds its explicit budget.");
            }
        }

        private static void ValidateFrameGeometry(
            ushort x,
            ushort y,
            ushort width,
            ushort height,
            ushort canvasWidth,
            ushort canvasHeight,
            long descriptorOffset,
            BoundedBinaryReader reader,
            BinarySourceContext source,
            ShpTsSourceProvenance provenance,
            ShpTsReadLimits limits,
            int frameIndex,
            IList<ShpTsDiagnostic> diagnostics)
        {
            long area = checked((long)width * height);
            if (area > limits.MaxLocalFrameArea)
            {
                ThrowDirect(ShpTsDiagnosticCode.LocalFrameAreaBudgetExceeded,
                    source, provenance, descriptorOffset + 4, area,
                    limits.MaxLocalFrameArea, "shp-local-frame-area", frameIndex,
                    "The local frame area exceeds its explicit budget.");
            }

            if ((x & 0x8000) != 0 || (y & 0x8000) != 0)
            {
                AddWarning(diagnostics, limits,
                    ShpTsDiagnosticCode.CoordinateSignednessUnresolved,
                    source, provenance, descriptorOffset, 4, reader.RemainingLength,
                    "shp-frame-coordinates", frameIndex,
                    "A coordinate high bit is set; raw values were preserved without signed reinterpretation.");
                return;
            }

            long right = checked((long)x + width);
            long bottom = checked((long)y + height);
            if (right > canvasWidth || bottom > canvasHeight)
            {
                ThrowDirect(ShpTsDiagnosticCode.FrameRectangleOutsideCanvas,
                    source, provenance, descriptorOffset, 8, reader.RemainingLength,
                    "shp-frame-rectangle", frameIndex,
                    "The unsigned local frame rectangle lies outside the canvas.");
            }
        }

        private static ShpTsCompressionKind DeriveCompression(uint flags)
        {
            switch (flags)
            {
                case 0: return ShpTsCompressionKind.RawOpaque;
                case 1: return ShpTsCompressionKind.RawTransparent;
                case 2: return ShpTsCompressionKind.SourceConflictingFlags2;
                case 3: return ShpTsCompressionKind.RleZeroTransparent;
                default: return ShpTsCompressionKind.UnknownFlags;
            }
        }

        private static void AddWarning(
            IList<ShpTsDiagnostic> diagnostics,
            ShpTsReadLimits limits,
            ShpTsDiagnosticCode code,
            BinarySourceContext source,
            ShpTsSourceProvenance provenance,
            long absoluteOffset,
            long requestedLength,
            long remainingLength,
            string field,
            int frameIndex,
            string message)
        {
            if (diagnostics.Count >= limits.MaxDiagnostics)
            {
                ThrowDirect(ShpTsDiagnosticCode.DiagnosticBudgetExceeded,
                    source, provenance, absoluteOffset, requestedLength, remainingLength,
                    "shp-diagnostics", frameIndex,
                    "The SHP diagnostic budget was exceeded.");
            }

            diagnostics.Add(new ShpTsDiagnostic(
                BinaryDiagnosticSeverity.Warning, code, source, provenance,
                absoluteOffset, requestedLength, remainingLength, field,
                frameIndex, -1, message));
        }

        private static void ThrowDirect(
            ShpTsDiagnosticCode code,
            BinarySourceContext source,
            ShpTsSourceProvenance provenance,
            long absoluteOffset,
            long requestedLength,
            long remainingLength,
            string field,
            int frameIndex,
            string message,
            BinaryDiagnosticCode? binaryCode = null)
        {
            throw new ShpTsReadException(new ShpTsDiagnostic(
                BinaryDiagnosticSeverity.Error, code, source, provenance,
                absoluteOffset, requestedLength, remainingLength, field,
                frameIndex, -1, message, binaryCode));
        }

        private static ShpTsDiagnostic DirectFailure(
            ShpTsDiagnosticCode code,
            BinarySourceContext source,
            ShpTsSourceProvenance provenance,
            long absoluteOffset,
            long requestedLength,
            long remainingLength,
            string field,
            string message,
            BinaryDiagnosticCode? binaryCode)
        {
            return new ShpTsDiagnostic(
                BinaryDiagnosticSeverity.Error, code, source, provenance,
                absoluteOffset, requestedLength, remainingLength, field,
                -1, -1, message, binaryCode);
        }

        internal static ShpTsDiagnostic MapBinaryFailure(
            BinaryDiagnostic diagnostic,
            BinarySourceContext source,
            ShpTsSourceProvenance provenance,
            int frameIndex,
            int rowIndex)
        {
            return new ShpTsDiagnostic(
                BinaryDiagnosticSeverity.Error,
                MapCode(diagnostic.Code),
                source,
                provenance,
                diagnostic.AbsoluteOffset,
                diagnostic.RequestedLength,
                diagnostic.RemainingLength,
                diagnostic.FieldOrSection,
                frameIndex,
                rowIndex,
                diagnostic.Message,
                diagnostic.Code);
        }

        private static ShpTsDiagnosticCode MapCode(BinaryDiagnosticCode code)
        {
            switch (code)
            {
                case BinaryDiagnosticCode.UnexpectedEndOfInput:
                    return ShpTsDiagnosticCode.UnexpectedEndOfInput;
                case BinaryDiagnosticCode.InputBudgetExceeded:
                    return ShpTsDiagnosticCode.InputBudgetExceeded;
                case BinaryDiagnosticCode.ReadBudgetExceeded:
                    return ShpTsDiagnosticCode.ReadBudgetExceeded;
                case BinaryDiagnosticCode.AllocationBudgetExceeded:
                    return ShpTsDiagnosticCode.AllocationBudgetExceeded;
                case BinaryDiagnosticCode.RecordBudgetExceeded:
                    return ShpTsDiagnosticCode.RecordBudgetExceeded;
                case BinaryDiagnosticCode.SubrangeBudgetExceeded:
                case BinaryDiagnosticCode.NestingBudgetExceeded:
                    return ShpTsDiagnosticCode.SubwindowBudgetExceeded;
                case BinaryDiagnosticCode.ArithmeticOverflow:
                    return ShpTsDiagnosticCode.ArithmeticOverflow;
                case BinaryDiagnosticCode.UnsupportedSeekOperation:
                    return ShpTsDiagnosticCode.UnsupportedSeekOperation;
                case BinaryDiagnosticCode.ReadFailure:
                    return ShpTsDiagnosticCode.ReadFailure;
                default:
                    return ShpTsDiagnosticCode.BinaryReadFailure;
            }
        }

        internal static void ValidateContext(
            BinarySourceContext source,
            ShpTsSourceProvenance provenance)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (provenance == null)
            {
                throw new ArgumentNullException(nameof(provenance));
            }

            if (!string.Equals(source.LogicalSourceId, provenance.SourceId, StringComparison.Ordinal))
            {
                throw new ArgumentException("SHP provenance must identify the binary source.", nameof(provenance));
            }
        }

        private sealed class RawDescriptor
        {
            public RawDescriptor(int index, long descriptorAbsoluteOffset, ushort x, ushort y,
                ushort width, ushort height, uint flags, byte[] color, uint reserved,
                uint dataOffset, ShpTsCompressionKind kind, bool isEmpty)
            {
                Index = index;
                DescriptorAbsoluteOffset = descriptorAbsoluteOffset;
                X = x;
                Y = y;
                Width = width;
                Height = height;
                Flags = flags;
                Color = color;
                Reserved = reserved;
                DataOffset = dataOffset;
                Kind = kind;
                IsEmpty = isEmpty;
            }

            public int Index { get; }
            public long DescriptorAbsoluteOffset { get; }
            public ushort X { get; }
            public ushort Y { get; }
            public ushort Width { get; }
            public ushort Height { get; }
            public uint Flags { get; }
            public byte[] Color { get; }
            public uint Reserved { get; }
            public uint DataOffset { get; }
            public ShpTsCompressionKind Kind { get; }
            public bool IsEmpty { get; }
        }
    }

}
