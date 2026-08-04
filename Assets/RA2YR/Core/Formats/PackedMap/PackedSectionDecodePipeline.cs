using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RA2YR.Core.Binary;
using RA2YR.Core.Formats.Ini;

namespace RA2YR.Core.Formats.PackedMap
{
    internal sealed class PackedSectionDecodePolicy
    {
        public PackedSectionDecodePolicy(
            PackedIniFragmentOrderingPolicy fragmentOrdering,
            StrictBase64Policy base64Policy,
            ChunkSentinelPolicy chunkSentinelPolicy,
            PackedCodecKind codec,
            Format80Profile format80Profile = null,
            PackedIniFragmentCollectorLimits fragmentLimits = null,
            StrictBase64ReadLimits base64Limits = null,
            WestwoodChunkReadLimits chunkLimits = null,
            Format80ReadLimits format80Limits = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            FragmentOrdering = fragmentOrdering; Base64Policy = base64Policy; ChunkSentinelPolicy = chunkSentinelPolicy; Codec = codec;
            Format80Profile = format80Profile ?? new Format80Profile(); FragmentLimits = fragmentLimits ?? new PackedIniFragmentCollectorLimits(); Base64Limits = base64Limits ?? new StrictBase64ReadLimits(); ChunkLimits = chunkLimits ?? new WestwoodChunkReadLimits(); Format80Limits = format80Limits ?? new Format80ReadLimits();
            CancellationToken = cancellationToken;
        }
        public PackedIniFragmentOrderingPolicy FragmentOrdering { get; }
        public StrictBase64Policy Base64Policy { get; }
        public ChunkSentinelPolicy ChunkSentinelPolicy { get; }
        public PackedCodecKind Codec { get; }
        public Format80Profile Format80Profile { get; }
        public PackedIniFragmentCollectorLimits FragmentLimits { get; }
        public StrictBase64ReadLimits Base64Limits { get; }
        public WestwoodChunkReadLimits ChunkLimits { get; }
        public Format80ReadLimits Format80Limits { get; }
        public CancellationToken CancellationToken { get; }
    }

    internal sealed class PackedSectionDecodeResult
    {
        internal PackedSectionDecodeResult(PackedIniFragmentCollection fragments, StrictBase64DecodeResult base64, WestwoodChunkEnvelopeReadResult envelope, IReadOnlyList<byte[]> blockOutputs, IReadOnlyList<PackedMapDiagnostic> diagnostics)
        {
            Fragments = fragments;
            Base64 = base64;
            Envelope = envelope;
            BlockOutputs = Array.AsReadOnly((blockOutputs ?? Array.Empty<byte[]>()).Select(item => item == null ? null : (byte[])item.Clone()).ToArray());
            var diagnosticList = new List<PackedMapDiagnostic>(diagnostics ?? throw new ArgumentNullException(nameof(diagnostics)));
            if (diagnosticList.All(item => item.Severity != BinaryDiagnosticSeverity.Error))
            {
                try
                {
                    DecodedBytes = Concatenate(BlockOutputs);
                    if (DecodedBytes == null && BlockOutputs.Any(item => item == null || item.Length != 0))
                        diagnosticList.Add(new PackedMapDiagnostic(PackedMapDiagnosticCode.PipelineBudgetExceeded, BinaryDiagnosticSeverity.Error, "Aggregate decoded output cannot be materialized within the result budget."));
                }
                catch (OverflowException)
                {
                    diagnosticList.Add(new PackedMapDiagnostic(PackedMapDiagnosticCode.PipelineBudgetExceeded, BinaryDiagnosticSeverity.Error, "Aggregate decoded output length accounting overflowed."));
                }
            }
            Diagnostics = Array.AsReadOnly(diagnosticList.ToArray());
        }
        public PackedIniFragmentCollection Fragments { get; }
        public StrictBase64DecodeResult Base64 { get; }
        public WestwoodChunkEnvelopeReadResult Envelope { get; }
        public IReadOnlyList<byte[]> BlockOutputs { get; }
        public byte[] DecodedBytes { get; }
        public IReadOnlyList<PackedMapDiagnostic> Diagnostics { get; }
        public bool IsSuccess => Diagnostics.All(d => d.Severity != BinaryDiagnosticSeverity.Error);

        private static byte[] Concatenate(IReadOnlyList<byte[]> outputs)
        {
            long total = 0;
            foreach (byte[] output in outputs)
            {
                if (output == null) return null;
                total = checked(total + output.Length);
            }
            if (total > int.MaxValue) return null;
            byte[] result = new byte[(int)total];
            int offset = 0;
            foreach (byte[] output in outputs)
            {
                Buffer.BlockCopy(output, 0, result, offset, output.Length);
                offset += output.Length;
            }
            return result;
        }
    }

    internal sealed class PackedSectionDecodePipeline
    {
        public PackedSectionDecodeResult Decode(
            IEnumerable<PackedIniFragmentOccurrence> occurrences,
            PackedSectionDecodePolicy policy,
            ILzoDecodeBackend lzoBackend = null)
        {
            if (occurrences == null) throw new ArgumentNullException(nameof(occurrences));
            if (policy == null) throw new ArgumentNullException(nameof(policy));
            if (policy.Codec != PackedCodecKind.Format80 && policy.Codec != PackedCodecKind.RawLzo1X)
            {
                return new PackedSectionDecodeResult(
                    null,
                    null,
                    null,
                    Array.Empty<byte[]>(),
                    new[] { new PackedMapDiagnostic(PackedMapDiagnosticCode.BackendInvalidCodec, BinaryDiagnosticSeverity.Error, "The selected packed codec is not supported by this pipeline.") });
            }
            PackedIniFragmentCollection fragments = new PackedIniFragmentCollector().Collect(occurrences, policy.FragmentOrdering, policy.FragmentLimits);
            var diagnostics = new List<PackedMapDiagnostic>(fragments.Diagnostics);
            if (!fragments.IsSuccess) return new PackedSectionDecodeResult(fragments, null, null, Array.Empty<byte[]>(), diagnostics);
            string text = string.Concat(fragments.Occurrences.Select(item => item.RawValue));
            StrictBase64DecodeResult base64 = new StrictBase64Decoder().Decode(text, policy.Base64Limits, policy.Base64Policy);
            diagnostics.AddRange(base64.Diagnostics);
            if (!base64.IsSuccess) return new PackedSectionDecodeResult(fragments, base64, null, Array.Empty<byte[]>(), diagnostics);
            IniSourceProvenance[] provenance = fragments.Occurrences.Select(item => item.Provenance).ToArray();
            WestwoodChunkEnvelopeReadResult envelope = new WestwoodChunkEnvelopeReader().Read(base64.Bytes, policy.ChunkLimits, policy.ChunkSentinelPolicy, 0, provenance);
            diagnostics.AddRange(envelope.Diagnostics);
            if (!envelope.IsSuccess) return new PackedSectionDecodeResult(fragments, base64, envelope, Array.Empty<byte[]>(), diagnostics);
            var outputs = new List<byte[]>();
            foreach (WestwoodChunkEnvelope block in envelope.Blocks)
            {
                if (policy.Codec == PackedCodecKind.Format80)
                {
                    Format80DecodeResult decoded = new Format80Decoder().Decode(block.Payload, block.UncompressedSize, policy.Format80Profile, policy.Format80Limits);
                    diagnostics.AddRange(decoded.Diagnostics);
                    if (!decoded.IsSuccess) return new PackedSectionDecodeResult(fragments, base64, envelope, Array.Empty<byte[]>(), diagnostics);
                    outputs.Add(decoded.Bytes);
                }
                else
                {
                    if (lzoBackend == null)
                    {
                        diagnostics.Add(new PackedMapDiagnostic(PackedMapDiagnosticCode.BackendUnavailable, BinaryDiagnosticSeverity.Error, "No LZO backend was supplied."));
                        return new PackedSectionDecodeResult(fragments, base64, envelope, Array.Empty<byte[]>(), diagnostics);
                    }
                    LzoDecodeRequest request;
                    try
                    {
                        request = new LzoDecodeRequest(
                            PackedCodecKind.RawLzo1X,
                            block.Payload,
                            block.UncompressedSize,
                            policy.ChunkLimits.MaxOutputBytes,
                            block.CompressedSize,
                            "packed-section",
                            block.Provenance,
                            policy.CancellationToken);
                    }
                    catch (ArgumentOutOfRangeException exception)
                    {
                        diagnostics.Add(new PackedMapDiagnostic(PackedMapDiagnosticCode.BackendInputBudgetExceeded, BinaryDiagnosticSeverity.Error, exception.Message));
                        return new PackedSectionDecodeResult(fragments, base64, envelope, Array.Empty<byte[]>(), diagnostics);
                    }

                    if (request.CancellationToken.IsCancellationRequested)
                    {
                        diagnostics.Add(new PackedMapDiagnostic(PackedMapDiagnosticCode.BackendCancelled, BinaryDiagnosticSeverity.Error, "LZO decode was cancelled before backend invocation."));
                        return new PackedSectionDecodeResult(fragments, base64, envelope, Array.Empty<byte[]>(), diagnostics);
                    }

                    LzoDecodeResult decoded;
                    try
                    {
                        decoded = lzoBackend.Decode(request);
                    }
                    catch (OperationCanceledException)
                    {
                        diagnostics.Add(new PackedMapDiagnostic(PackedMapDiagnosticCode.BackendCancelled, BinaryDiagnosticSeverity.Error, "LZO backend cancelled the decode."));
                        return new PackedSectionDecodeResult(fragments, base64, envelope, Array.Empty<byte[]>(), diagnostics);
                    }
                    catch (Exception exception)
                    {
                        diagnostics.Add(new PackedMapDiagnostic(PackedMapDiagnosticCode.BackendException, BinaryDiagnosticSeverity.Error, "LZO backend threw " + exception.GetType().Name + "."));
                        return new PackedSectionDecodeResult(fragments, base64, envelope, Array.Empty<byte[]>(), diagnostics);
                    }

                    if (decoded == null)
                    {
                        diagnostics.Add(new PackedMapDiagnostic(PackedMapDiagnosticCode.BackendFailure, BinaryDiagnosticSeverity.Error, "LZO backend returned no result."));
                        return new PackedSectionDecodeResult(fragments, base64, envelope, Array.Empty<byte[]>(), diagnostics);
                    }

                    diagnostics.AddRange(decoded.Diagnostics);
                    bool hasDiagnosticError = decoded.Diagnostics.Any(item => item.Severity == BinaryDiagnosticSeverity.Error);
                    if (hasDiagnosticError)
                    {
                        diagnostics.Add(new PackedMapDiagnostic(PackedMapDiagnosticCode.BackendDiagnosticError, BinaryDiagnosticSeverity.Error, "LZO backend returned an error diagnostic."));
                        return new PackedSectionDecodeResult(fragments, base64, envelope, Array.Empty<byte[]>(), diagnostics);
                    }
                    if (string.IsNullOrWhiteSpace(decoded.BackendIdentity))
                    {
                        diagnostics.Add(new PackedMapDiagnostic(PackedMapDiagnosticCode.BackendIdentityMissing, BinaryDiagnosticSeverity.Error, "LZO backend identity is required."));
                        return new PackedSectionDecodeResult(fragments, base64, envelope, Array.Empty<byte[]>(), diagnostics);
                    }
                    if (decoded.Bytes == null)
                    {
                        diagnostics.Add(new PackedMapDiagnostic(PackedMapDiagnosticCode.BackendNullOutput, BinaryDiagnosticSeverity.Error, "LZO backend returned null output."));
                        return new PackedSectionDecodeResult(fragments, base64, envelope, Array.Empty<byte[]>(), diagnostics);
                    }
                    if (decoded.ConsumedInput != block.CompressedSize)
                    {
                        diagnostics.Add(new PackedMapDiagnostic(PackedMapDiagnosticCode.BackendConsumedInputMismatch, BinaryDiagnosticSeverity.Error, "LZO backend consumed input different from the bounded compressed payload."));
                        return new PackedSectionDecodeResult(fragments, base64, envelope, Array.Empty<byte[]>(), diagnostics);
                    }
                    if (decoded.Provenance == null || decoded.Provenance.Count == 0)
                    {
                        diagnostics.Add(new PackedMapDiagnostic(PackedMapDiagnosticCode.BackendProvenanceMissing, BinaryDiagnosticSeverity.Error, "LZO backend provenance is required."));
                        return new PackedSectionDecodeResult(fragments, base64, envelope, Array.Empty<byte[]>(), diagnostics);
                    }
                    if (!HasSameProvenance(decoded.Provenance, block.Provenance))
                    {
                        diagnostics.Add(new PackedMapDiagnostic(PackedMapDiagnosticCode.BackendProvenanceMismatch, BinaryDiagnosticSeverity.Error, "LZO backend provenance does not match the chunk provenance."));
                        return new PackedSectionDecodeResult(fragments, base64, envelope, Array.Empty<byte[]>(), diagnostics);
                    }
                    if (decoded.Bytes.LongLength > request.MaxOutputBytes)
                    {
                        diagnostics.Add(new PackedMapDiagnostic(PackedMapDiagnosticCode.PipelineBudgetExceeded, BinaryDiagnosticSeverity.Error, "LZO backend output exceeds the configured output budget."));
                        return new PackedSectionDecodeResult(fragments, base64, envelope, Array.Empty<byte[]>(), diagnostics);
                    }
                    if (decoded.Bytes.Length != block.UncompressedSize)
                    {
                        diagnostics.Add(new PackedMapDiagnostic(PackedMapDiagnosticCode.BackendLengthMismatch, BinaryDiagnosticSeverity.Error, "LZO backend output length differs from the declared block length."));
                        return new PackedSectionDecodeResult(fragments, base64, envelope, Array.Empty<byte[]>(), diagnostics);
                    }
                    outputs.Add(decoded.Bytes);
                }
            }
            return new PackedSectionDecodeResult(fragments, base64, envelope, outputs.AsReadOnly(), diagnostics);
        }

        private static bool HasSameProvenance(IReadOnlyList<IniSourceProvenance> actual, IReadOnlyList<IniSourceProvenance> expected)
        {
            if (actual == null || expected == null || actual.Count != expected.Count) return false;
            for (int index = 0; index < expected.Count; index++)
            {
                if (actual[index] == null || expected[index] == null ||
                    !string.Equals(actual[index].SourceId, expected[index].SourceId, StringComparison.Ordinal) ||
                    actual[index].LogicalChain.Count != expected[index].LogicalChain.Count)
                    return false;
                for (int pathIndex = 0; pathIndex < expected[index].LogicalChain.Count; pathIndex++)
                {
                    if (!string.Equals(actual[index].LogicalChain[pathIndex].Value, expected[index].LogicalChain[pathIndex].Value, StringComparison.Ordinal))
                        return false;
                }
            }
            return true;
        }
    }
}
