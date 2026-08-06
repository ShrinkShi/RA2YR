using System;
using System.Collections.Generic;
using RA2YR.Core.Binary;
using RA2YR.Core.Binary.Seekable;

namespace RA2YR.Core.Formats.PackedMap
{
    internal sealed class Format80Decoder
    {
        public Format80DecodeResult Decode(ReadOnlyDataWindow window, int expectedOutputLength, Format80Profile profile = null, Format80ReadLimits limits = null)
        {
            limits = limits ?? new Format80ReadLimits();
            try { return Decode(PackedMapBoundedInput.ReadWindow(window, "format80", limits.MaxInputBytes), expectedOutputLength, profile, limits); }
            catch (ArgumentOutOfRangeException exception) { return new Format80DecodeResult(null, 0, 0, false, new[] { Error(PackedMapDiagnosticCode.Format80BudgetExceeded, exception.Message, window.AbsoluteStartOffset) }); }
            catch (BinaryReadException exception) { return new Format80DecodeResult(null, 0, 0, false, new[] { new PackedMapDiagnostic(PackedMapDiagnosticCode.Format80TruncatedCommand, BinaryDiagnosticSeverity.Error, exception.Message, null, exception.Diagnostic.AbsoluteOffset) }); }
        }

        public Format80DecodeResult Decode(System.IO.Stream stream, long length, BinarySourceContext source, int expectedOutputLength, Format80Profile profile = null, Format80ReadLimits limits = null)
        {
            limits = limits ?? new Format80ReadLimits();
            try { return Decode(PackedMapBoundedInput.ReadStream(stream, length, source, limits.MaxInputBytes), expectedOutputLength, profile, limits); }
            catch (ArgumentOutOfRangeException exception) { return new Format80DecodeResult(null, 0, 0, false, new[] { Error(PackedMapDiagnosticCode.Format80BudgetExceeded, exception.Message, 0) }); }
            catch (BinaryReadException exception) { return new Format80DecodeResult(null, 0, 0, false, new[] { new PackedMapDiagnostic(PackedMapDiagnosticCode.Format80TruncatedCommand, BinaryDiagnosticSeverity.Error, exception.Message, source.LogicalSourceId, exception.Diagnostic.AbsoluteOffset) }); }
        }

        public Format80DecodeResult Decode(
            byte[] input,
            int expectedOutputLength,
            Format80Profile profile = null,
            Format80ReadLimits limits = null)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (expectedOutputLength < 0) throw new ArgumentOutOfRangeException(nameof(expectedOutputLength));
            profile = profile ?? new Format80Profile();
            limits = limits ?? new Format80ReadLimits();
            if (input.LongLength > limits.MaxInputBytes)
                return new Format80DecodeResult(null, 0, 0, false, new[] { Error(PackedMapDiagnosticCode.Format80BudgetExceeded, "Format80 input exceeds the configured byte budget.", 0) });
            var diagnostics = new List<PackedMapDiagnostic>();
            var output = new List<byte>(Math.Min(expectedOutputLength, 4096));
            int position = 0;
            int commands = 0;
            bool terminator = false;

            if (profile.AllowInitialMarker && input.Length > 0 && input[0] == 0)
                position++;

            while (position < input.Length)
            {
                if (commands >= limits.MaxCommands)
                { diagnostics.Add(Error(PackedMapDiagnosticCode.Format80BudgetExceeded, "Format80 command budget exceeded.", position)); break; }
                int commandOffset = position;
                byte command = input[position++];
                commands++;
                if (command == 0x80)
                {
                    terminator = true;
                    break;
                }

                if ((command & 0x80) == 0)
                {
                    if (position >= input.Length) { diagnostics.Add(Error(PackedMapDiagnosticCode.Format80TruncatedCommand, "Short copy distance is truncated.", commandOffset)); break; }
                    int length = ((command >> 4) & 0x07) + 3;
                    int field = ((command & 0x0f) << 8) | input[position++];
                    if (!Copy(output, field, length, Format80Variant.Relative, expectedOutputLength, limits, diagnostics, commandOffset)) break;
                    continue;
                }

                if ((command & 0xc0) == 0x80)
                {
                    int length = command & 0x3f;
                    if (length <= 0) { diagnostics.Add(Error(PackedMapDiagnosticCode.Format80UnknownCommand, "A zero-length literal is reserved for the terminator.", commandOffset)); break; }
                    if (length > input.Length - position) { diagnostics.Add(Error(PackedMapDiagnosticCode.Format80TruncatedLiteral, "Literal payload is truncated.", commandOffset)); break; }
                    if (!EnsureOutputRoom(output.Count, length, expectedOutputLength, limits, diagnostics, commandOffset)) break;
                    for (int index = 0; index < length; index++) output.Add(input[position++]);
                    continue;
                }

                if (command == 0xfe)
                {
                    if (input.Length - position < 3) { diagnostics.Add(Error(PackedMapDiagnosticCode.Format80TruncatedCommand, "Fill command is truncated.", commandOffset)); break; }
                    int length = input[position] | (input[position + 1] << 8);
                    byte value = input[position + 2];
                    position += 3;
                    if (length == 0 && profile.RejectZeroFill) { diagnostics.Add(Error(PackedMapDiagnosticCode.Format80NoProgress, "Zero-length fill is not accepted by the selected profile.", commandOffset)); break; }
                    if (!EnsureOutputRoom(output.Count, length, expectedOutputLength, limits, diagnostics, commandOffset)) break;
                    for (int index = 0; index < length; index++) output.Add(value);
                    continue;
                }

                if (command == 0xff)
                {
                    if (input.Length - position < 4) { diagnostics.Add(Error(PackedMapDiagnosticCode.Format80TruncatedCommand, "Long copy command is truncated.", commandOffset)); break; }
                    int length = input[position] | (input[position + 1] << 8);
                    int field = input[position + 2] | (input[position + 3] << 8);
                    position += 4;
                    if (length == 0) { diagnostics.Add(Error(PackedMapDiagnosticCode.Format80NoProgress, "Zero-length long copy is not accepted by the selected profile.", commandOffset)); break; }
                    if (!Copy(output, field, length, profile.Variant, expectedOutputLength, limits, diagnostics, commandOffset)) break;
                    continue;
                }

                int mediumLength = command & 0x3f;
                if (mediumLength >= 0x3e) { diagnostics.Add(Error(PackedMapDiagnosticCode.Format80UnknownCommand, "Reserved Format80 command is not accepted.", commandOffset)); break; }
                if (input.Length - position < 2) { diagnostics.Add(Error(PackedMapDiagnosticCode.Format80TruncatedCommand, "Medium copy position is truncated.", commandOffset)); break; }
                int mediumField = input[position] | (input[position + 1] << 8);
                position += 2;
                if (!Copy(output, mediumField, mediumLength + 3, profile.Variant, expectedOutputLength, limits, diagnostics, commandOffset)) break;
            }

            if (diagnostics.Count == 0 && profile.RequireTerminator && !terminator)
                diagnostics.Add(Error(PackedMapDiagnosticCode.Format80MissingTerminator, "The selected profile requires a terminator.", position));
            if (terminator && position < input.Length && !profile.AllowTrailingAfterTerminator)
                diagnostics.Add(Error(PackedMapDiagnosticCode.Format80TrailingInput, "Compressed bytes remain after the terminator.", position));
            if (diagnostics.Count == 0 && output.Count != expectedOutputLength)
                diagnostics.Add(Error(output.Count < expectedOutputLength ? PackedMapDiagnosticCode.Format80OutputUnderflow : PackedMapDiagnosticCode.Format80OutputOverflow, "Decoded output does not equal the declared output length.", position));

            return new Format80DecodeResult(
                diagnostics.Count == 0 ? output.ToArray() : null,
                position,
                commands,
                terminator,
                diagnostics);
        }

        private static bool Copy(List<byte> output, int field, int length, Format80Variant variant, int expectedLength, Format80ReadLimits limits, List<PackedMapDiagnostic> diagnostics, int offset)
        {
            if (length <= 0) { diagnostics.Add(Error(PackedMapDiagnosticCode.Format80NoProgress, "A copy command must produce output.", offset)); return false; }
            int source;
            try { source = variant == Format80Variant.Absolute ? field : checked(output.Count - field); }
            catch (OverflowException) { diagnostics.Add(Error(PackedMapDiagnosticCode.Format80ArithmeticOverflow, "Reference arithmetic overflowed.", offset)); return false; }
            if (field == 0 && variant == Format80Variant.Relative || source < 0 || source >= output.Count)
            { diagnostics.Add(Error(PackedMapDiagnosticCode.Format80ReferenceBeforeOutput, "Reference does not point to already produced output.", offset)); return false; }
            if (!EnsureOutputRoom(output.Count, length, expectedLength, limits, diagnostics, offset)) return false;
            for (int index = 0; index < length; index++)
            {
                int sourceIndex = source + index;
                if (sourceIndex < 0 || sourceIndex >= output.Count)
                { diagnostics.Add(Error(PackedMapDiagnosticCode.Format80InvalidReference, "Reference expansion crossed the produced output boundary.", offset)); return false; }
                output.Add(output[sourceIndex]);
            }
            return true;
        }

        private static bool EnsureOutputRoom(int current, int additional, int expected, Format80ReadLimits limits, List<PackedMapDiagnostic> diagnostics, int offset)
        {
            long end;
            try { end = checked((long)current + additional); }
            catch (OverflowException) { diagnostics.Add(Error(PackedMapDiagnosticCode.Format80ArithmeticOverflow, "Output length arithmetic overflowed.", offset)); return false; }
            if (end > expected || end > limits.MaxOutputBytes)
            { diagnostics.Add(Error(end > expected ? PackedMapDiagnosticCode.Format80OutputOverflow : PackedMapDiagnosticCode.Format80BudgetExceeded, "Decoded output exceeds its bounded destination.", offset)); return false; }
            return true;
        }

        private static PackedMapDiagnostic Error(PackedMapDiagnosticCode code, string message, long offset) => new PackedMapDiagnostic(code, BinaryDiagnosticSeverity.Error, message, null, offset);
    }
}
