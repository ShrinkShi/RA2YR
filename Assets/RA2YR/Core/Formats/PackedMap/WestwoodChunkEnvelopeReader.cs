using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RA2YR.Core.Binary;
using RA2YR.Core.Binary.Seekable;
using RA2YR.Core.Content;
using RA2YR.Core.Formats.Ini;

namespace RA2YR.Core.Formats.PackedMap
{
    internal sealed class WestwoodChunkEnvelopeReader
    {
        public WestwoodChunkEnvelopeReadResult Read(
            ReadOnlyDataWindow window,
            WestwoodChunkReadLimits limits = null,
            ChunkSentinelPolicy sentinelPolicy = ChunkSentinelPolicy.RejectAllZero)
        {
            limits = limits ?? new WestwoodChunkReadLimits();
            try { return Read(PackedMapBoundedInput.ReadWindow(window, "chunk-envelope", limits.MaxInputBytes), limits, sentinelPolicy, window.AbsoluteStartOffset, null, SyntheticProvenance()); }
            catch (ArgumentOutOfRangeException exception) { return new WestwoodChunkEnvelopeReadResult(Array.Empty<WestwoodChunkEnvelope>(), new[] { Error(PackedMapDiagnosticCode.ChunkBudgetExceeded, exception.Message, window.AbsoluteStartOffset) }); }
            catch (BinaryReadException exception) { return new WestwoodChunkEnvelopeReadResult(Array.Empty<WestwoodChunkEnvelope>(), new[] { new PackedMapDiagnostic(PackedMapDiagnosticCode.ChunkPayloadTruncated, BinaryDiagnosticSeverity.Error, exception.Message, null, exception.Diagnostic.AbsoluteOffset) }); }
        }

        public WestwoodChunkEnvelopeReadResult Read(
            Stream stream,
            long length,
            BinarySourceContext source,
            WestwoodChunkReadLimits limits = null,
            ChunkSentinelPolicy sentinelPolicy = ChunkSentinelPolicy.RejectAllZero)
        {
            limits = limits ?? new WestwoodChunkReadLimits();
            try { return Read(PackedMapBoundedInput.ReadStream(stream, length, source, limits.MaxInputBytes), limits, sentinelPolicy, 0, source.LogicalSourceId, new[] { new IniSourceProvenance(source.LogicalSourceId, new[] { source.LogicalPath }) }); }
            catch (ArgumentOutOfRangeException exception) { return new WestwoodChunkEnvelopeReadResult(Array.Empty<WestwoodChunkEnvelope>(), new[] { Error(PackedMapDiagnosticCode.ChunkBudgetExceeded, exception.Message, 0, source.LogicalSourceId) }); }
            catch (BinaryReadException exception) { return new WestwoodChunkEnvelopeReadResult(Array.Empty<WestwoodChunkEnvelope>(), new[] { new PackedMapDiagnostic(PackedMapDiagnosticCode.ChunkPayloadTruncated, BinaryDiagnosticSeverity.Error, exception.Message, source.LogicalSourceId, exception.Diagnostic.AbsoluteOffset) }); }
        }

        public WestwoodChunkEnvelopeReadResult Read(byte[] input, WestwoodChunkReadLimits limits = null, ChunkSentinelPolicy sentinelPolicy = ChunkSentinelPolicy.RejectAllZero, long absoluteOffset = 0)
        {
            return Read(input, limits, sentinelPolicy, absoluteOffset, null, SyntheticProvenance());
        }

        internal WestwoodChunkEnvelopeReadResult Read(byte[] input, WestwoodChunkReadLimits limits, ChunkSentinelPolicy sentinelPolicy, long absoluteOffset, IEnumerable<IniSourceProvenance> provenance)
        {
            IniSourceProvenance[] chain = (provenance ?? throw new ArgumentNullException(nameof(provenance))).ToArray();
            if (chain.Length == 0 || chain.Any(item => item == null)) throw new ArgumentException("Chunk provenance is required.", nameof(provenance));
            return Read(input, limits, sentinelPolicy, absoluteOffset, chain[0].SourceId, chain);
        }

        private WestwoodChunkEnvelopeReadResult Read(byte[] input, WestwoodChunkReadLimits limits, ChunkSentinelPolicy sentinelPolicy, long absoluteOffset, string sourceId, IEnumerable<IniSourceProvenance> provenance)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            limits = limits ?? new WestwoodChunkReadLimits();
            if (input.LongLength > limits.MaxInputBytes)
                return new WestwoodChunkEnvelopeReadResult(Array.Empty<WestwoodChunkEnvelope>(), new[] { Error(PackedMapDiagnosticCode.ChunkBudgetExceeded, "Chunk input exceeds the configured byte budget.", absoluteOffset, sourceId) });
            var blocks = new List<WestwoodChunkEnvelope>();
            var diagnostics = new List<PackedMapDiagnostic>();
            int position = 0;
            long compressedTotal = 0;
            long outputTotal = 0;
            while (position < input.Length)
            {
                if (blocks.Count >= limits.MaxBlocks) { diagnostics.Add(Error(PackedMapDiagnosticCode.ChunkBudgetExceeded, "Chunk count exceeds the configured budget.", absoluteOffset + position)); break; }
                if (input.Length - position < 4) { diagnostics.Add(Error(PackedMapDiagnosticCode.ChunkHeaderTruncated, "Chunk header is truncated.", absoluteOffset + position)); break; }
                ushort compressed = (ushort)(input[position] | input[position + 1] << 8);
                ushort output = (ushort)(input[position + 2] | input[position + 3] << 8);
                position += 4;
                if (compressed == 0 || output == 0)
                {
                    bool allowed = compressed == 0 && output == 0 && sentinelPolicy == ChunkSentinelPolicy.AllowZeroZeroAsTerminator;
                    if (!allowed)
                    {
                        diagnostics.Add(Error(
                            compressed == 0 && output == 0
                                ? PackedMapDiagnosticCode.ChunkSentinelUnresolved
                                : PackedMapDiagnosticCode.ChunkZeroFieldInvalid,
                            "A one-zero chunk field is not a valid block or terminator; only explicit 0/0 sentinel policy is accepted.",
                            absoluteOffset + position - 4));
                        break;
                    }
                    if (position != input.Length) diagnostics.Add(Error(PackedMapDiagnosticCode.ChunkTrailingBytes, "A sentinel chunk was followed by bytes.", absoluteOffset + position));
                    break;
                }
                if (compressed > input.Length - position) { diagnostics.Add(Error(PackedMapDiagnosticCode.ChunkPayloadTruncated, "Chunk payload is truncated.", absoluteOffset + position)); break; }
                try
                {
                    compressedTotal = checked(compressedTotal + compressed);
                    outputTotal = checked(outputTotal + output);
                }
                catch (OverflowException)
                {
                    diagnostics.Add(Error(PackedMapDiagnosticCode.ChunkArithmeticOverflow, "Aggregate chunk size accounting overflowed.", absoluteOffset + position - 4));
                    break;
                }
                if (compressedTotal > limits.MaxCompressedBytes) { diagnostics.Add(Error(PackedMapDiagnosticCode.ChunkBudgetExceeded, "Aggregate compressed bytes exceed the configured budget.", absoluteOffset + position)); break; }
                if (outputTotal > limits.MaxOutputBytes) { diagnostics.Add(Error(PackedMapDiagnosticCode.ChunkOutputBudgetExceeded, "Aggregate declared output exceeds the configured budget.", absoluteOffset + position)); break; }
                byte[] payload = new byte[compressed]; Buffer.BlockCopy(input, position, payload, 0, compressed);
                blocks.Add(new WestwoodChunkEnvelope(blocks.Count, absoluteOffset + position - 4, compressed, output, payload, provenance));
                position += compressed;
            }
            return new WestwoodChunkEnvelopeReadResult(blocks, diagnostics);
        }
        private static IReadOnlyList<IniSourceProvenance> SyntheticProvenance()
        { return new[] { new IniSourceProvenance("packed-map-input", new[] { LogicalContentPath.Parse("packed-map-input") }) }; }
        private static PackedMapDiagnostic Error(PackedMapDiagnosticCode code, string message, long offset, string sourceId = null) => new PackedMapDiagnostic(code, BinaryDiagnosticSeverity.Error, message, sourceId, offset);
    }
}
