using System;
using System.Collections.Generic;
using System.Linq;
using RA2YR.Core.Binary;
using RA2YR.Core.Content;
using RA2YR.Core.Formats.Ini;

namespace RA2YR.Core.Formats.PackedMap
{
    /// <summary>
    /// Independent managed decoder for bounded raw LZO1X-compatible streams.
    /// The decoder consumes one chunk payload and never owns map-specific semantics.
    /// </summary>
    internal sealed class ManagedRawLzo1XDecodeBackend : ILzoDecodeBackend
    {
        internal const string Identity = "ra2yr-managed-raw-lzo1x-v1";

        public LzoDecodeResult Decode(LzoDecodeRequest request)
        {
            if (request == null)
                return Result(null, 0, 0, false, EmptyProvenance(), PackedMapDiagnosticCode.BackendFailure, "LZO decode request is null.");

            byte[] input = request.Compressed;
            int inputLength = input == null ? 0 : input.Length;
            int inputOffset = 0;
            int outputOffset = 0;
            bool terminatorSeen = false;
            byte[] output = null;
            var diagnostics = new List<PackedMapDiagnostic>();

            try
            {
                if (request.Codec != PackedCodecKind.RawLzo1X)
                    return Result(null, 0, 0, false, request.SourceProvenance, PackedMapDiagnosticCode.BackendInvalidCodec, "Managed backend accepts RawLzo1X only.");
                if (input == null)
                    return Result(null, 0, 0, false, request.SourceProvenance, PackedMapDiagnosticCode.BackendFailure, "Compressed input is null.");
                if (input.LongLength > request.MaxInputBytes)
                    return Result(null, 0, 0, false, request.SourceProvenance, PackedMapDiagnosticCode.BackendInputBudgetExceeded, "Compressed input exceeds the bounded input budget.");
                if (request.ExpectedLength < 0 || request.ExpectedLength > request.MaxOutputBytes)
                    return Result(null, 0, 0, false, request.SourceProvenance, PackedMapDiagnosticCode.BackendBudgetExceeded, "Declared output exceeds the bounded output budget.");

                request.CancellationToken.ThrowIfCancellationRequested();
                output = new byte[request.ExpectedLength];
                if (inputLength < 3)
                    return Result(null, inputOffset, outputOffset, false, request.SourceProvenance, PackedMapDiagnosticCode.BackendInputTruncated, "Raw LZO1X stream is shorter than the minimum terminal sequence.");

                int state = 0;
                byte first = input[0];
                if (first >= 22)
                {
                    inputOffset = 1;
                    int length = first - 17;
                    if (!CopyLiteral(input, output, ref inputOffset, ref outputOffset, length, request, diagnostics))
                        return Failure(request, inputOffset, outputOffset, terminatorSeen, diagnostics);
                    state = 4;
                }
                else if (first >= 18)
                {
                    inputOffset = 1;
                    state = first - 17;
                    if (!CopyLiteral(input, output, ref inputOffset, ref outputOffset, state, request, diagnostics))
                        return Failure(request, inputOffset, outputOffset, terminatorSeen, diagnostics);
                }

                while (!terminatorSeen)
                {
                    request.CancellationToken.ThrowIfCancellationRequested();
                    if (inputOffset >= inputLength && outputOffset == request.ExpectedLength)
                    {
                        Add(diagnostics, request, PackedMapDiagnosticCode.BackendMissingTerminator, "LZO stream ended after output completion without a terminal marker.", inputOffset);
                        return Failure(request, inputOffset, outputOffset, terminatorSeen, diagnostics);
                    }
                    int beforeInput = inputOffset;
                    int beforeOutput = outputOffset;
                    if (!TryRead(input, ref inputOffset, out byte instruction))
                    {
                        Add(diagnostics, request, PackedMapDiagnosticCode.BackendInputTruncated, "LZO command is truncated.", inputOffset);
                        return Failure(request, inputOffset, outputOffset, terminatorSeen, diagnostics);
                    }

                    int length;
                    int distance;
                    int nextState;
                    if ((instruction & 0xC0) != 0)
                    {
                        if (!TryRead(input, ref inputOffset, out byte highDistance))
                        {
                            Add(diagnostics, request, PackedMapDiagnosticCode.BackendInputTruncated, "LZO short-match distance is truncated.", inputOffset);
                            return Failure(request, inputOffset, outputOffset, terminatorSeen, diagnostics);
                        }
                        length = (instruction >> 5) + 1;
                        distance = checked((highDistance << 3) + ((instruction >> 2) & 7) + 1);
                        nextState = instruction & 3;
                    }
                    else if ((instruction & 0x20) != 0)
                    {
                        int lowLength = instruction & 0x1F;
                        if (lowLength == 0)
                        {
                            if (!TryReadExtendedLength(input, ref inputOffset, 31, request, diagnostics, out int extension))
                                return Failure(request, inputOffset, outputOffset, terminatorSeen, diagnostics);
                            length = checked(extension + 2);
                        }
                        else
                        {
                            length = checked(lowLength + 2);
                        }
                        if (!TryReadUInt16(input, ref inputOffset, out ushort packedDistance))
                        {
                            Add(diagnostics, request, PackedMapDiagnosticCode.BackendInputTruncated, "LZO medium-match distance is truncated.", inputOffset);
                            return Failure(request, inputOffset, outputOffset, terminatorSeen, diagnostics);
                        }
                        distance = checked((packedDistance >> 2) + 1);
                        nextState = packedDistance & 3;
                    }
                    else if ((instruction & 0x10) != 0)
                    {
                        int lowLength = instruction & 7;
                        if (lowLength == 0)
                        {
                            if (!TryReadExtendedLength(input, ref inputOffset, 7, request, diagnostics, out int extension))
                                return Failure(request, inputOffset, outputOffset, terminatorSeen, diagnostics);
                            length = checked(extension + 2);
                        }
                        else
                        {
                            length = checked(lowLength + 2);
                        }
                        if (!TryReadUInt16(input, ref inputOffset, out ushort packedDistance))
                        {
                            Add(diagnostics, request, PackedMapDiagnosticCode.BackendInputTruncated, "LZO long-match distance is truncated.", inputOffset);
                            return Failure(request, inputOffset, outputOffset, terminatorSeen, diagnostics);
                        }
                        int encodedDistance = checked(((instruction & 8) << 11) + (packedDistance >> 2));
                        nextState = packedDistance & 3;
                        if (encodedDistance == 0)
                        {
                            if (length != 3)
                            {
                                Add(diagnostics, request, PackedMapDiagnosticCode.BackendMalformedStream, "LZO terminal marker has an invalid length.", inputOffset - 2);
                                return Failure(request, inputOffset, outputOffset, terminatorSeen, diagnostics);
                            }
                            terminatorSeen = true;
                            continue;
                        }
                        distance = checked(encodedDistance + 16384);
                    }
                    else if (state == 0)
                    {
                        int lowLength = instruction;
                        if (lowLength == 0)
                        {
                            if (!TryReadExtendedLength(input, ref inputOffset, 15, request, diagnostics, out int extension))
                                return Failure(request, inputOffset, outputOffset, terminatorSeen, diagnostics);
                            length = checked(extension + 3);
                        }
                        else
                        {
                            length = checked(lowLength + 3);
                        }
                        if (!CopyLiteral(input, output, ref inputOffset, ref outputOffset, length, request, diagnostics))
                            return Failure(request, inputOffset, outputOffset, terminatorSeen, diagnostics);
                        state = 4;
                        continue;
                    }
                    else
                    {
                        if (!TryRead(input, ref inputOffset, out byte highDistance))
                        {
                            Add(diagnostics, request, PackedMapDiagnosticCode.BackendInputTruncated, "LZO short-distance match is truncated.", inputOffset);
                            return Failure(request, inputOffset, outputOffset, terminatorSeen, diagnostics);
                        }
                        nextState = instruction & 3;
                        distance = state == 4
                            ? checked((instruction >> 2) + (highDistance << 2) + 2049)
                            : checked((instruction >> 2) + (highDistance << 2) + 1);
                        length = state == 4 ? 3 : 2;
                    }

                    if (!CopyMatch(output, ref outputOffset, distance, length, request, diagnostics))
                        return Failure(request, inputOffset, outputOffset, terminatorSeen, diagnostics);
                    state = nextState;
                    if (!CopyLiteral(input, output, ref inputOffset, ref outputOffset, state, request, diagnostics))
                        return Failure(request, inputOffset, outputOffset, terminatorSeen, diagnostics);
                    if (inputOffset == beforeInput && outputOffset == beforeOutput)
                    {
                        Add(diagnostics, request, PackedMapDiagnosticCode.BackendNoProgress, "LZO decoder made no progress.", inputOffset);
                        return Failure(request, inputOffset, outputOffset, terminatorSeen, diagnostics);
                    }
                }

                if (!terminatorSeen)
                {
                    Add(diagnostics, request, PackedMapDiagnosticCode.BackendMissingTerminator, "LZO stream ended without a terminal marker.", inputOffset);
                    return Failure(request, inputOffset, outputOffset, false, diagnostics);
                }
                if (outputOffset != request.ExpectedLength)
                {
                    Add(diagnostics, request, outputOffset < request.ExpectedLength ? PackedMapDiagnosticCode.BackendOutputUnderflow : PackedMapDiagnosticCode.BackendOutputOverflow, "LZO output length differs from the declared output length.", inputOffset);
                    return Failure(request, inputOffset, outputOffset, terminatorSeen, diagnostics);
                }
                if (inputOffset != inputLength)
                {
                    Add(diagnostics, request, PackedMapDiagnosticCode.BackendTrailingInput, "Compressed bytes remain after the terminal marker.", inputOffset);
                    return Failure(request, inputOffset, outputOffset, terminatorSeen, diagnostics);
                }
                return new LzoDecodeResult(output, inputOffset, outputOffset, true, Identity, diagnostics, request.SourceProvenance);
            }
            catch (OperationCanceledException)
            {
                Add(diagnostics, request, PackedMapDiagnosticCode.BackendCancelled, "LZO decode was cancelled.", inputOffset);
                return Failure(request, inputOffset, outputOffset, terminatorSeen, diagnostics);
            }
            catch (OverflowException)
            {
                Add(diagnostics, request, PackedMapDiagnosticCode.BackendArithmeticOverflow, "LZO arithmetic overflowed while decoding.", inputOffset);
                return Failure(request, inputOffset, outputOffset, terminatorSeen, diagnostics);
            }
            catch (OutOfMemoryException)
            {
                Add(diagnostics, request, PackedMapDiagnosticCode.BackendBudgetExceeded, "LZO output allocation exceeded the bounded budget.", inputOffset);
                return Failure(request, inputOffset, outputOffset, terminatorSeen, diagnostics);
            }
            catch (Exception exception)
            {
                Add(diagnostics, request, PackedMapDiagnosticCode.BackendFailure, "Managed LZO decoder failed with " + exception.GetType().Name + ".", inputOffset);
                return Failure(request, inputOffset, outputOffset, terminatorSeen, diagnostics);
            }
        }

        private static bool CopyLiteral(
            byte[] input,
            byte[] output,
            ref int inputOffset,
            ref int outputOffset,
            int length,
            LzoDecodeRequest request,
            IList<PackedMapDiagnostic> diagnostics)
        {
            if (length < 0)
            {
                Add(diagnostics, request, PackedMapDiagnosticCode.BackendArithmeticOverflow, "Literal length is negative.", inputOffset);
                return false;
            }
            if (length > input.Length - inputOffset)
            {
                Add(diagnostics, request, PackedMapDiagnosticCode.BackendInputTruncated, "LZO literal body is truncated.", inputOffset);
                return false;
            }
            if (length > output.Length - outputOffset)
            {
                Add(diagnostics, request, PackedMapDiagnosticCode.BackendOutputOverflow, "LZO literal body exceeds the declared output length.", inputOffset);
                return false;
            }
            for (int index = 0; index < length; index++)
            {
                if ((index & 0x3FF) == 0)
                    request.CancellationToken.ThrowIfCancellationRequested();
                output[outputOffset + index] = input[inputOffset + index];
            }
            inputOffset += length;
            outputOffset += length;
            return true;
        }

        private static bool CopyMatch(
            byte[] output,
            ref int outputOffset,
            int distance,
            int length,
            LzoDecodeRequest request,
            IList<PackedMapDiagnostic> diagnostics)
        {
            if (distance <= 0 || distance > outputOffset)
            {
                Add(diagnostics, request, PackedMapDiagnosticCode.BackendLookbehindOverrun, "LZO match references bytes before the output start.", outputOffset);
                return false;
            }
            if (length < 0 || length > output.Length - outputOffset)
            {
                Add(diagnostics, request, PackedMapDiagnosticCode.BackendOutputOverflow, "LZO match exceeds the declared output length.", outputOffset);
                return false;
            }
            for (int index = 0; index < length; index++)
            {
                if ((index & 0x3FF) == 0)
                    request.CancellationToken.ThrowIfCancellationRequested();
                output[outputOffset + index] = output[outputOffset + index - distance];
            }
            outputOffset += length;
            return true;
        }

        private static bool TryRead(byte[] input, ref int inputOffset, out byte value)
        {
            if (inputOffset >= input.Length)
            {
                value = 0;
                return false;
            }
            value = input[inputOffset++];
            return true;
        }

        private static bool TryReadUInt16(byte[] input, ref int inputOffset, out ushort value)
        {
            if (input.Length - inputOffset < 2)
            {
                value = 0;
                return false;
            }
            value = (ushort)(input[inputOffset] | (input[inputOffset + 1] << 8));
            inputOffset += 2;
            return true;
        }

        private static bool TryReadExtendedLength(
            byte[] input,
            ref int inputOffset,
            int baseLength,
            LzoDecodeRequest request,
            IList<PackedMapDiagnostic> diagnostics,
            out int value)
        {
            long zeroCount = 0;
            while (true)
            {
                request.CancellationToken.ThrowIfCancellationRequested();
                if (!TryRead(input, ref inputOffset, out byte part))
                {
                    Add(diagnostics, request, PackedMapDiagnosticCode.BackendInputTruncated, "LZO extended length is truncated.", inputOffset);
                    value = 0;
                    return false;
                }
                if (part != 0)
                {
                    long expanded = checked(baseLength + zeroCount * 255L + part);
                    if (expanded > int.MaxValue)
                    {
                        Add(diagnostics, request, PackedMapDiagnosticCode.BackendArithmeticOverflow, "LZO extended length exceeds Int32.", inputOffset - 1);
                        value = 0;
                        return false;
                    }
                    value = (int)expanded;
                    return true;
                }
                zeroCount = checked(zeroCount + 1);
                if (zeroCount > input.Length)
                {
                    Add(diagnostics, request, PackedMapDiagnosticCode.BackendArithmeticOverflow, "LZO extended length exceeds the input budget.", inputOffset);
                    value = 0;
                    return false;
                }
            }
        }

        private static LzoDecodeResult Failure(LzoDecodeRequest request, int consumed, int produced, bool terminalSeen, IEnumerable<PackedMapDiagnostic> diagnostics)
        {
            return new LzoDecodeResult(null, consumed, produced, terminalSeen, Identity, diagnostics, request.SourceProvenance);
        }

        private static LzoDecodeResult Result(byte[] bytes, int consumed, int produced, bool terminalSeen, IEnumerable<IniSourceProvenance> provenance, PackedMapDiagnosticCode code, string message)
        {
            IniSourceProvenance[] chain = (provenance ?? Array.Empty<IniSourceProvenance>()).ToArray();
            var diagnostics = new[] { new PackedMapDiagnostic(code, BinaryDiagnosticSeverity.Error, message, chain.Length == 0 ? "packed-section" : chain[0].SourceId, consumed) };
            return new LzoDecodeResult(bytes, consumed, produced, terminalSeen, Identity, diagnostics, chain);
        }

        private static void Add(IList<PackedMapDiagnostic> diagnostics, LzoDecodeRequest request, PackedMapDiagnosticCode code, string message, long offset)
        {
            string sourceId = request.SourceProvenance != null && request.SourceProvenance.Count != 0 ? request.SourceProvenance[0].SourceId : request.Provenance;
            diagnostics.Add(new PackedMapDiagnostic(code, BinaryDiagnosticSeverity.Error, message, sourceId, offset));
        }

        private static IReadOnlyList<IniSourceProvenance> EmptyProvenance()
        {
            return Array.Empty<IniSourceProvenance>();
        }
    }
}
