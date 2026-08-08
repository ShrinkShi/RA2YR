using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using RA2YR.Core.Binary;
using RA2YR.Core.Content;
using RA2YR.Core.Formats.Ini;

namespace RA2YR.Core.Formats.PackedMap
{
    internal enum PackedMapDiagnosticCode
    {
        NonnumericFragmentKey,
        FragmentKeyZero,
        NegativeFragmentKey,
        FragmentKeyOverflow,
        DuplicateNumericFragmentKey,
        FragmentKeyCollision,
        FragmentKeyGap,
        MissingFragmentKeyOne,
        EmptyFragmentValue,
        DuplicateSourceOccurrence,
        FragmentBudgetExceeded,
        AggregateCharacterBudgetExceeded,
        InvalidBase64Character,
        InvalidBase64Padding,
        InvalidBase64Length,
        Base64Whitespace,
        Base64OutputBudgetExceeded,
        Base64DecodeFailure,
        ChunkHeaderTruncated,
        ChunkPayloadTruncated,
        ChunkBudgetExceeded,
        ChunkOutputBudgetExceeded,
        ChunkArithmeticOverflow,
        ChunkSentinelUnresolved,
        ChunkZeroFieldInvalid,
        ChunkTrailingBytes,
        ChunkNoProgress,
        Format80TruncatedCommand,
        Format80TruncatedLiteral,
        Format80InvalidReference,
        Format80ReferenceBeforeOutput,
        Format80OutputOverflow,
        Format80OutputUnderflow,
        Format80MissingTerminator,
        Format80TrailingInput,
        Format80UnknownCommand,
        Format80ArithmeticOverflow,
        Format80BudgetExceeded,
        Format80NoProgress,
        BackendUnavailable,
        BackendFailure,
        BackendLengthMismatch,
        BackendInvalidCodec,
        BackendInputBudgetExceeded,
        BackendConsumedInputMismatch,
        BackendIdentityMissing,
        BackendNullOutput,
        BackendDiagnosticError,
        BackendProvenanceMissing,
        BackendProvenanceMismatch,
        BackendCancelled,
        BackendException,
        InvalidPolicy,
        PipelineStageFailure,
        PipelineBudgetExceeded
    }

    internal sealed class PackedMapDiagnostic
    {
        internal PackedMapDiagnostic(
            PackedMapDiagnosticCode code,
            BinaryDiagnosticSeverity severity,
            string message,
            string sourceId = null,
            long offset = -1)
        {
            Code = code;
            Severity = severity;
            Message = message ?? throw new ArgumentNullException(nameof(message));
            SourceId = sourceId;
            Offset = offset;
        }

        public PackedMapDiagnosticCode Code { get; }
        public BinaryDiagnosticSeverity Severity { get; }
        public string Message { get; }
        public string SourceId { get; }
        public long Offset { get; }
    }

    internal enum PackedIniFragmentOrderingPolicy
    {
        SourceOccurrenceOrder,
        NumericAscendingUnique,
        StrictSequentialFromOne
    }

    internal sealed class PackedIniFragmentOccurrence
    {
        internal PackedIniFragmentOccurrence(
            string sectionName,
            string rawKey,
            string rawValue,
            int sourceOrder,
            string sourceId,
            int physicalLineId,
            IniSourceProvenance provenance)
        {
            SectionName = sectionName ?? throw new ArgumentNullException(nameof(sectionName));
            RawKey = rawKey ?? throw new ArgumentNullException(nameof(rawKey));
            RawValue = rawValue ?? throw new ArgumentNullException(nameof(rawValue));
            if (sourceOrder < 0 || physicalLineId < 0) throw new ArgumentOutOfRangeException();
            SourceOrder = sourceOrder;
            SourceId = BinaryDiagnosticLabel.Validate(sourceId, nameof(sourceId));
            PhysicalLineId = physicalLineId;
            Provenance = provenance ?? throw new ArgumentNullException(nameof(provenance));
        }

        public string SectionName { get; }
        public string RawKey { get; }
        public string RawValue { get; }
        public int SourceOrder { get; }
        public string SourceId { get; }
        public int PhysicalLineId { get; }
        public IniSourceProvenance Provenance { get; }

        internal static IEnumerable<PackedIniFragmentOccurrence> FromDocument(
            IniRawDocument document,
            string sectionName)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var current = string.Empty;
            foreach (IniNode node in document.Nodes)
            {
                var section = node as IniSectionNode;
                if (section != null)
                {
                    current = IniTextEncodingPolicy.StrictAscii.Decode(section.Name);
                    continue;
                }

                var key = node as IniKeyValueNode;
                if (key == null || !string.Equals(current, sectionName, StringComparison.Ordinal)) continue;
                yield return new PackedIniFragmentOccurrence(
                    current,
                    IniTextEncodingPolicy.StrictAscii.Decode(key.Key),
                    IniTextEncodingPolicy.StrictAscii.Decode(key.Value),
                    key.PhysicalLineId,
                    document.Provenance.SourceId,
                    key.PhysicalLineId,
                    document.Provenance);
            }
        }
    }

    internal sealed class PackedIniFragmentCollection
    {
        internal PackedIniFragmentCollection(
            IEnumerable<PackedIniFragmentOccurrence> occurrences,
            IEnumerable<PackedMapDiagnostic> diagnostics)
        {
            Occurrences = Array.AsReadOnly((occurrences ?? throw new ArgumentNullException(nameof(occurrences))).ToArray());
            Diagnostics = Array.AsReadOnly((diagnostics ?? throw new ArgumentNullException(nameof(diagnostics))).ToArray());
        }

        public IReadOnlyList<PackedIniFragmentOccurrence> Occurrences { get; }
        public IReadOnlyList<PackedMapDiagnostic> Diagnostics { get; }
        public bool IsSuccess => Diagnostics.All(d => d.Severity != BinaryDiagnosticSeverity.Error);
    }

    internal sealed class PackedIniFragmentCollectorLimits
    {
        public PackedIniFragmentCollectorLimits(int maxFragments = 4096, long maxCharacters = 4 * 1024 * 1024)
        {
            if (maxFragments < 0 || maxCharacters < 0) throw new ArgumentOutOfRangeException();
            MaxFragments = maxFragments; MaxCharacters = maxCharacters;
        }
        public int MaxFragments { get; }
        public long MaxCharacters { get; }
    }

    internal enum StrictBase64Policy
    {
        StandardAlphabetNoWhitespace
    }

    internal sealed class StrictBase64DecodeResult
    {
        internal StrictBase64DecodeResult(byte[] bytes, IEnumerable<PackedMapDiagnostic> diagnostics)
        {
            Bytes = bytes == null ? null : (byte[])bytes.Clone();
            Diagnostics = Array.AsReadOnly((diagnostics ?? throw new ArgumentNullException(nameof(diagnostics))).ToArray());
        }
        public byte[] Bytes { get; }
        public IReadOnlyList<PackedMapDiagnostic> Diagnostics { get; }
        public bool IsSuccess => Bytes != null && Diagnostics.All(d => d.Severity != BinaryDiagnosticSeverity.Error);
    }

    internal sealed class StrictBase64ReadLimits
    {
        public StrictBase64ReadLimits(long maxDecodedBytes = 64 * 1024 * 1024)
        { if (maxDecodedBytes < 0) throw new ArgumentOutOfRangeException(nameof(maxDecodedBytes)); MaxDecodedBytes = maxDecodedBytes; }
        public long MaxDecodedBytes { get; }
    }

    internal sealed class WestwoodChunkReadLimits
    {
        public WestwoodChunkReadLimits(int maxBlocks = 4096, long maxCompressedBytes = 64 * 1024 * 1024, long maxOutputBytes = 64 * 1024 * 1024, long maxInputBytes = 64 * 1024 * 1024)
        { if (maxBlocks < 0 || maxCompressedBytes < 0 || maxOutputBytes < 0 || maxInputBytes < 0) throw new ArgumentOutOfRangeException(); MaxBlocks = maxBlocks; MaxCompressedBytes = maxCompressedBytes; MaxOutputBytes = maxOutputBytes; MaxInputBytes = maxInputBytes; }
        public int MaxBlocks { get; }
        public long MaxCompressedBytes { get; }
        public long MaxOutputBytes { get; }
        public long MaxInputBytes { get; }
    }

    internal enum ChunkSentinelPolicy { RejectAllZero, AllowZeroZeroAsTerminator }

    internal sealed class WestwoodChunkEnvelope
    {
        internal WestwoodChunkEnvelope(int ordinal, long sourceOffset, ushort compressedSize, ushort uncompressedSize, byte[] payload, IEnumerable<IniSourceProvenance> provenance)
        {
            Ordinal = ordinal;
            SourceOffset = sourceOffset;
            CompressedSize = compressedSize;
            UncompressedSize = uncompressedSize;
            Payload = (byte[])(payload ?? throw new ArgumentNullException(nameof(payload))).Clone();
            IniSourceProvenance[] chain = (provenance ?? throw new ArgumentNullException(nameof(provenance))).ToArray();
            if (chain.Length == 0 || chain.Any(item => item == null)) throw new ArgumentException("Chunk provenance is required.", nameof(provenance));
            Provenance = Array.AsReadOnly(chain);
        }
        public int Ordinal { get; }
        public long SourceOffset { get; }
        public ushort CompressedSize { get; }
        public ushort UncompressedSize { get; }
        public byte[] Payload { get; }
        public IReadOnlyList<IniSourceProvenance> Provenance { get; }
    }

    internal sealed class WestwoodChunkEnvelopeReadResult
    {
        internal WestwoodChunkEnvelopeReadResult(IEnumerable<WestwoodChunkEnvelope> blocks, IEnumerable<PackedMapDiagnostic> diagnostics)
        { Blocks = Array.AsReadOnly((blocks ?? throw new ArgumentNullException(nameof(blocks))).ToArray()); Diagnostics = Array.AsReadOnly((diagnostics ?? throw new ArgumentNullException(nameof(diagnostics))).ToArray()); }
        public IReadOnlyList<WestwoodChunkEnvelope> Blocks { get; }
        public IReadOnlyList<PackedMapDiagnostic> Diagnostics { get; }
        public bool IsSuccess => Diagnostics.All(d => d.Severity != BinaryDiagnosticSeverity.Error);
    }

    internal enum Format80Variant { Absolute, Relative }
    internal enum Format80CommandKind { ShortCopy, Literal, MediumCopy, Fill, LongCopy, Terminator }
    internal sealed class Format80Profile
    {
        public Format80Profile(Format80Variant variant = Format80Variant.Absolute, bool requireTerminator = true, bool allowTrailingAfterTerminator = false, bool rejectZeroFill = true, bool allowInitialMarker = false)
        { Variant = variant; RequireTerminator = requireTerminator; AllowTrailingAfterTerminator = allowTrailingAfterTerminator; RejectZeroFill = rejectZeroFill; AllowInitialMarker = allowInitialMarker; }
        public Format80Variant Variant { get; }
        public bool RequireTerminator { get; }
        public bool AllowTrailingAfterTerminator { get; }
        public bool RejectZeroFill { get; }
        public bool AllowInitialMarker { get; }
    }

    internal sealed class Format80ReadLimits
    {
        public Format80ReadLimits(int maxCommands = 1_000_000, long maxOutputBytes = 64 * 1024 * 1024, long maxInputBytes = 64 * 1024 * 1024)
        { if (maxCommands < 0 || maxOutputBytes < 0 || maxInputBytes < 0) throw new ArgumentOutOfRangeException(); MaxCommands = maxCommands; MaxOutputBytes = maxOutputBytes; MaxInputBytes = maxInputBytes; }
        public int MaxCommands { get; }
        public long MaxOutputBytes { get; }
        public long MaxInputBytes { get; }
    }

    internal sealed class Format80DecodeResult
    {
        internal Format80DecodeResult(byte[] bytes, int consumed, int commands, bool terminatorSeen, IEnumerable<PackedMapDiagnostic> diagnostics)
        { Bytes = bytes == null ? null : (byte[])bytes.Clone(); BytesConsumed = consumed; CommandCount = commands; TerminatorSeen = terminatorSeen; Diagnostics = Array.AsReadOnly((diagnostics ?? throw new ArgumentNullException(nameof(diagnostics))).ToArray()); }
        public byte[] Bytes { get; }
        public int BytesConsumed { get; }
        public int CommandCount { get; }
        public bool TerminatorSeen { get; }
        public IReadOnlyList<PackedMapDiagnostic> Diagnostics { get; }
        public bool IsSuccess => Bytes != null && Diagnostics.All(d => d.Severity != BinaryDiagnosticSeverity.Error);
    }

    internal enum PackedCodecKind { Format80, RawLzo1X }
    internal interface ILzoDecodeBackend
    {
        LzoDecodeResult Decode(LzoDecodeRequest request);
    }
    internal sealed class LzoDecodeRequest
    {
        public LzoDecodeRequest(PackedCodecKind codec, byte[] compressed, int expectedLength, long maxOutputBytes, string provenance)
            : this(codec, compressed, expectedLength, maxOutputBytes, compressed == null ? 0 : compressed.LongLength, provenance, Array.Empty<IniSourceProvenance>(), CancellationToken.None) { }

        public LzoDecodeRequest(PackedCodecKind codec, byte[] compressed, int expectedLength, long maxOutputBytes, string provenance, CancellationToken cancellationToken)
            : this(codec, compressed, expectedLength, maxOutputBytes, compressed == null ? 0 : compressed.LongLength, provenance, Array.Empty<IniSourceProvenance>(), cancellationToken) { }

        public LzoDecodeRequest(PackedCodecKind codec, byte[] compressed, int expectedLength, long maxOutputBytes, string provenance, IEnumerable<IniSourceProvenance> sourceProvenance, CancellationToken cancellationToken = default(CancellationToken))
            : this(codec, compressed, expectedLength, maxOutputBytes, compressed == null ? 0 : compressed.LongLength, provenance, sourceProvenance, cancellationToken) { }

        public LzoDecodeRequest(PackedCodecKind codec, byte[] compressed, int expectedLength, long maxOutputBytes, long maxInputBytes, string provenance, IEnumerable<IniSourceProvenance> sourceProvenance, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (codec != PackedCodecKind.RawLzo1X) throw new ArgumentOutOfRangeException(nameof(codec), "The LZO contract only accepts RawLzo1X.");
            Codec = codec;
            Compressed = (byte[])(compressed ?? throw new ArgumentNullException(nameof(compressed))).Clone();
            if (expectedLength < 0 || maxOutputBytes < 0 || maxInputBytes < 0) throw new ArgumentOutOfRangeException();
            if (Compressed.LongLength > maxInputBytes) throw new ArgumentOutOfRangeException(nameof(compressed), "The compressed input exceeds its bounded input contract.");
            if (expectedLength > maxOutputBytes) throw new ArgumentOutOfRangeException(nameof(expectedLength), "The declared output exceeds its bounded output contract.");
            ExpectedLength = expectedLength;
            MaxOutputBytes = maxOutputBytes;
            MaxInputBytes = maxInputBytes;
            Provenance = BinaryDiagnosticLabel.Validate(provenance, nameof(provenance));
            IniSourceProvenance[] chain = (sourceProvenance ?? throw new ArgumentNullException(nameof(sourceProvenance))).ToArray();
            if (chain.Any(item => item == null)) throw new ArgumentException("LZO source provenance cannot contain null entries.", nameof(sourceProvenance));
            SourceProvenance = Array.AsReadOnly(chain);
            CancellationToken = cancellationToken;
        }
        public PackedCodecKind Codec { get; }
        public byte[] Compressed { get; }
        public int ExpectedLength { get; }
        public long MaxOutputBytes { get; }
        public long MaxInputBytes { get; }
        public string Provenance { get; }
        public IReadOnlyList<IniSourceProvenance> SourceProvenance { get; }
        public CancellationToken CancellationToken { get; }
    }
    internal sealed class LzoDecodeResult
    {
        internal LzoDecodeResult(byte[] bytes, int consumed, string backendIdentity, IEnumerable<PackedMapDiagnostic> diagnostics)
            : this(bytes, consumed, backendIdentity, diagnostics, Array.Empty<IniSourceProvenance>()) { }

        internal LzoDecodeResult(byte[] bytes, int consumed, string backendIdentity, IEnumerable<PackedMapDiagnostic> diagnostics, IEnumerable<IniSourceProvenance> provenance)
        { Bytes = bytes == null ? null : (byte[])bytes.Clone(); ConsumedInput = consumed; BackendIdentity = backendIdentity; Diagnostics = Array.AsReadOnly((diagnostics ?? throw new ArgumentNullException(nameof(diagnostics))).ToArray()); IniSourceProvenance[] chain = (provenance ?? throw new ArgumentNullException(nameof(provenance))).ToArray(); if (chain.Any(item => item == null)) throw new ArgumentException("LZO result provenance cannot contain null entries.", nameof(provenance)); Provenance = Array.AsReadOnly(chain); }
        public byte[] Bytes { get; }
        public int ConsumedInput { get; }
        public string BackendIdentity { get; }
        public IReadOnlyList<PackedMapDiagnostic> Diagnostics { get; }
        public IReadOnlyList<IniSourceProvenance> Provenance { get; }
        public bool IsSuccess => Bytes != null && Diagnostics.All(d => d.Severity != BinaryDiagnosticSeverity.Error);
    }
}
