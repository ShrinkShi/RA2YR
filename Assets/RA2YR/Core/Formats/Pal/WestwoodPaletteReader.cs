using System;
using System.IO;
using System.Linq;
using RA2YR.Core.Binary;
using RA2YR.Core.Binary.Seekable;

namespace RA2YR.Core.Formats.Pal
{
    internal static class WestwoodPaletteReader
    {
        private const long PaletteModelAllocationEstimate = 4096;

        public static PaletteParseResult Read(
            ReadOnlyMemory<byte> input,
            BinarySourceContext source,
            PaletteSourceProvenance provenance,
            PaletteReadLimits limits = null,
            long absoluteStartOffset = 0)
        {
            ValidateContext(source, provenance);
            return ReadMemoryCore(
                input,
                source,
                provenance,
                limits ?? PaletteReadLimits.Default,
                absoluteStartOffset,
                0);
        }

        public static PaletteParseResult Read(
            Stream stream,
            long inputLength,
            BinarySourceContext source,
            PaletteSourceProvenance provenance,
            PaletteReadLimits limits = null,
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
                session = BinaryReadSession.FromStream(
                    stream,
                    inputLength,
                    source,
                    (limits ?? PaletteReadLimits.Default).ToBinaryLimits(),
                    leaveOpen,
                    absoluteStartOffset);
                return ParseSession(session, source, provenance);
            }
            catch (PaletteReadException exception)
            {
                return PaletteParseResult.Failure(exception.Diagnostic);
            }
            catch (BinaryReadException exception)
            {
                return PaletteParseResult.Failure(MapBinaryFailure(
                    exception.Diagnostic,
                    source,
                    provenance,
                    -1,
                    null));
            }
            finally
            {
                session?.Dispose();
            }
        }

        public static PaletteParseResult ReadSeekable(
            Stream stream,
            BinarySourceContext source,
            PaletteSourceProvenance provenance,
            PaletteReadLimits limits = null,
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
                session = BinaryReadSession.FromSeekableStream(
                    stream,
                    source,
                    (limits ?? PaletteReadLimits.Default).ToBinaryLimits(),
                    leaveOpen);
                return ParseSession(session, source, provenance);
            }
            catch (PaletteReadException exception)
            {
                return PaletteParseResult.Failure(exception.Diagnostic);
            }
            catch (BinaryReadException exception)
            {
                return PaletteParseResult.Failure(MapBinaryFailure(
                    exception.Diagnostic,
                    source,
                    provenance,
                    -1,
                    null));
            }
            finally
            {
                session?.Dispose();
            }
        }

        public static PaletteParseResult Read(
            ReadOnlyDataWindow window,
            BinarySourceContext source,
            PaletteSourceProvenance provenance,
            PaletteReadLimits limits = null)
        {
            if (window == null)
            {
                throw new ArgumentNullException(nameof(window));
            }

            ValidateContext(source, provenance);
            PaletteReadLimits effectiveLimits = limits ?? PaletteReadLimits.Default;
            if (window.Length > effectiveLimits.MaxInputBytes)
            {
                return PaletteParseResult.Failure(CreateDirectFailure(
                    PaletteDiagnosticCode.InputBudgetExceeded,
                    source,
                    provenance,
                    window.AbsoluteStartOffset,
                    window.Length,
                    window.Length,
                    "palette-input",
                    "The bounded palette window exceeds its explicit input budget.",
                    BinaryDiagnosticCode.InputBudgetExceeded));
            }

            if (window.Length > int.MaxValue)
            {
                return PaletteParseResult.Failure(CreateDirectFailure(
                    PaletteDiagnosticCode.InvalidLength,
                    source,
                    provenance,
                    window.AbsoluteStartOffset,
                    window.Length,
                    window.Length,
                    "palette-input",
                    "The bounded palette window length cannot be represented safely.",
                    BinaryDiagnosticCode.InvalidLength));
            }

            if (window.Length > effectiveLimits.MaxAllocatedBytes)
            {
                return PaletteParseResult.Failure(CreateDirectFailure(
                    PaletteDiagnosticCode.AllocationBudgetExceeded,
                    source,
                    provenance,
                    window.AbsoluteStartOffset,
                    window.Length,
                    effectiveLimits.MaxAllocatedBytes,
                    "palette-window-snapshot",
                    "The bounded palette window exceeds its snapshot allocation budget.",
                    BinaryDiagnosticCode.AllocationBudgetExceeded));
            }

            byte[] snapshot;
            try
            {
                snapshot = new byte[checked((int)window.Length)];
                window.ReadExactly(
                    0,
                    snapshot,
                    0,
                    snapshot.Length,
                    "palette-window-input");
            }
            catch (BinaryReadException exception)
            {
                return PaletteParseResult.Failure(MapBinaryFailure(
                    exception.Diagnostic,
                    source,
                    provenance,
                    -1,
                    null));
            }
            catch (OutOfMemoryException)
            {
                return PaletteParseResult.Failure(CreateDirectFailure(
                    PaletteDiagnosticCode.AllocationBudgetExceeded,
                    source,
                    provenance,
                    window.AbsoluteStartOffset,
                    window.Length,
                    effectiveLimits.MaxAllocatedBytes,
                    "palette-window-snapshot",
                    "The validated palette window snapshot could not be allocated.",
                    BinaryDiagnosticCode.AllocationBudgetExceeded));
            }

            return ReadMemoryCore(
                snapshot,
                source,
                provenance,
                effectiveLimits,
                window.AbsoluteStartOffset,
                snapshot.LongLength);
        }

        private static PaletteParseResult ReadMemoryCore(
            ReadOnlyMemory<byte> input,
            BinarySourceContext source,
            PaletteSourceProvenance provenance,
            PaletteReadLimits limits,
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
                        "palette-window-snapshot");
                }

                return ParseSession(session, source, provenance);
            }
            catch (PaletteReadException exception)
            {
                return PaletteParseResult.Failure(exception.Diagnostic);
            }
            catch (BinaryReadException exception)
            {
                return PaletteParseResult.Failure(MapBinaryFailure(
                    exception.Diagnostic,
                    source,
                    provenance,
                    -1,
                    null));
            }
            finally
            {
                session?.Dispose();
            }
        }

        private static PaletteParseResult ParseSession(
            BinaryReadSession session,
            BinarySourceContext source,
            PaletteSourceProvenance provenance)
        {
            BoundedBinaryReader reader = session.Root;
            reader.ReserveRecords(WestwoodPalette.ColorCount, "palette-color-records");
            session.ReserveAllocation(
                PaletteModelAllocationEstimate,
                reader.AbsoluteOffset,
                reader.RemainingLength,
                "palette-color-model");

            var colors = new PaletteColorRaw[WestwoodPalette.ColorCount];
            for (int index = 0; index < colors.Length; index++)
            {
                byte red = ReadChannel(
                    reader,
                    source,
                    provenance,
                    index,
                    PaletteChannel.Red,
                    "palette-red-channel");
                byte green = ReadChannel(
                    reader,
                    source,
                    provenance,
                    index,
                    PaletteChannel.Green,
                    "palette-green-channel");
                byte blue = ReadChannel(
                    reader,
                    source,
                    provenance,
                    index,
                    PaletteChannel.Blue,
                    "palette-blue-channel");
                colors[index] = new PaletteColorRaw(red, green, blue);
            }

            BinaryParseCompletion completion = reader.Complete(
                TrailingDataPolicy.RequireFullyConsumed,
                "palette-trailing-data");
            if (!completion.IsComplete)
            {
                BinaryDiagnostic diagnostic = completion.Diagnostics.First(
                    item => item.Severity == BinaryDiagnosticSeverity.Error);
                return PaletteParseResult.Failure(MapBinaryFailure(
                    diagnostic,
                    source,
                    provenance,
                    -1,
                    null));
            }

            return PaletteParseResult.Success(new WestwoodPalette(
                source,
                provenance,
                colors));
        }

        private static byte ReadChannel(
            BoundedBinaryReader reader,
            BinarySourceContext source,
            PaletteSourceProvenance provenance,
            int colorIndex,
            PaletteChannel channel,
            string field)
        {
            byte value;
            try
            {
                value = reader.ReadUInt8(field);
            }
            catch (BinaryReadException exception)
            {
                throw new PaletteReadException(MapBinaryFailure(
                    exception.Diagnostic,
                    source,
                    provenance,
                    colorIndex,
                    channel));
            }

            if (value > PaletteColorRaw.MaximumChannelValue)
            {
                throw new PaletteReadException(new PaletteDiagnostic(
                    PaletteDiagnosticCode.InvalidChannelValue,
                    source,
                    provenance,
                    checked(reader.AbsoluteOffset - 1),
                    1,
                    reader.RemainingLength,
                    field,
                    colorIndex,
                    channel,
                    "A raw palette channel lies outside the confirmed six-bit range."));
            }

            return value;
        }

        private static void ValidateContext(
            BinarySourceContext source,
            PaletteSourceProvenance provenance)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (provenance == null)
            {
                throw new ArgumentNullException(nameof(provenance));
            }

            if (!string.Equals(
                    source.LogicalSourceId,
                    provenance.SourceId,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Palette provenance must identify the binary source.",
                    nameof(provenance));
            }
        }

        private static PaletteDiagnostic MapBinaryFailure(
            BinaryDiagnostic diagnostic,
            BinarySourceContext source,
            PaletteSourceProvenance provenance,
            int colorIndex,
            PaletteChannel? channel)
        {
            return new PaletteDiagnostic(
                MapCode(diagnostic.Code),
                source,
                provenance,
                diagnostic.AbsoluteOffset,
                diagnostic.RequestedLength,
                diagnostic.RemainingLength,
                diagnostic.FieldOrSection,
                colorIndex,
                channel,
                diagnostic.Message,
                diagnostic.Code);
        }

        private static PaletteDiagnostic CreateDirectFailure(
            PaletteDiagnosticCode code,
            BinarySourceContext source,
            PaletteSourceProvenance provenance,
            long absoluteOffset,
            long requestedLength,
            long remainingLength,
            string field,
            string message,
            BinaryDiagnosticCode? binaryCode)
        {
            return new PaletteDiagnostic(
                code,
                source,
                provenance,
                absoluteOffset,
                requestedLength,
                remainingLength,
                field,
                -1,
                null,
                message,
                binaryCode);
        }

        private static PaletteDiagnosticCode MapCode(BinaryDiagnosticCode code)
        {
            switch (code)
            {
                case BinaryDiagnosticCode.UnexpectedEndOfInput:
                    return PaletteDiagnosticCode.UnexpectedEndOfInput;
                case BinaryDiagnosticCode.InvalidLength:
                    return PaletteDiagnosticCode.InvalidLength;
                case BinaryDiagnosticCode.TrailingData:
                    return PaletteDiagnosticCode.UnexpectedTrailingData;
                case BinaryDiagnosticCode.InputBudgetExceeded:
                    return PaletteDiagnosticCode.InputBudgetExceeded;
                case BinaryDiagnosticCode.ReadBudgetExceeded:
                    return PaletteDiagnosticCode.ReadBudgetExceeded;
                case BinaryDiagnosticCode.AllocationBudgetExceeded:
                    return PaletteDiagnosticCode.AllocationBudgetExceeded;
                case BinaryDiagnosticCode.RecordBudgetExceeded:
                    return PaletteDiagnosticCode.RecordBudgetExceeded;
                case BinaryDiagnosticCode.ArithmeticOverflow:
                    return PaletteDiagnosticCode.ArithmeticOverflow;
                case BinaryDiagnosticCode.UnsupportedSeekOperation:
                    return PaletteDiagnosticCode.UnsupportedSeekOperation;
                case BinaryDiagnosticCode.ReadFailure:
                    return PaletteDiagnosticCode.ReadFailure;
                default:
                    return PaletteDiagnosticCode.BinaryReadFailure;
            }
        }
    }
}
