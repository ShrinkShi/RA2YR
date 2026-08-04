# Recommended implementation boundaries

## 1. Pipeline

```text
LosslessIniDocument
→ PackedIniFragmentCollector
→ StrictBase64Decoder
→ WestwoodChunkEnvelopeReader
→ Format80Decoder or LzoDecodeBackend
→ ExactOutputValidator
→ map-specific record reader
```

Every arrow crosses a typed result with diagnostics and provenance.

## 2. Format80 API

Suggested types:

- `Format80Decoder`
- `Format80Variant`
- `Format80CommandKind`
- `Format80DecodeRequest`
- `Format80DecodeResult`
- `Format80Diagnostic`
- `Format80ReadLimits`

`Format80DecodeRequest` contains:

- bounded input window;
- bounded output window or explicit expected output;
- selected position variant;
- terminator policy;
- exact-input policy;
- command/output limits.

The decoder does not allocate from stream fields and does not know about Overlay objects.

## 3. Chunk envelope API

- `WestwoodChunkEnvelopeReader`
- `ChunkedLzoEnvelopeReader` as a configured facade if useful
- `ChunkedFormat80EnvelopeReader` as a configured facade
- `WestwoodChunkDescriptor`
- `ChunkEnvelopeReadResult`
- `ChunkEnvelopeDiagnostic`
- `ChunkEnvelopeReadLimits`

The common reader parses headers and windows only. It delegates payload decoding through:

```text
IPackedBlockCodecBackend
```

## 4. LZO API

- `LzoCodecKind`
- `LzoDecodeBackend`
- `LzoDecodeRequest`
- `LzoDecodeResult`
- `LzoDiagnostic`

Initial codec kind:

```text
RawLzo1X
```

The backend does not parse the Westwood header and cannot decide record count, preview dimensions, or IsoMap padding.

## 5. Fragment API

- `PackedIniFragmentCollector`
- `PackedIniFragmentPolicy`
- `PackedIniFragmentOccurrence`
- `PackedIniFragmentResult`
- `PackedIniFragmentDiagnostic`
- `PackedIniFragmentLimits`

The collector reads a lossless section view. It does not use the ordinary INI effective-key map.

## 6. Complete pipeline

`PackedSectionDecodePipeline` composes components but contains no codec logic.

Request fields include:

- section role;
- fragment policy;
- Base64 policy;
- envelope profile;
- codec kind/variant;
- expected aggregate output;
- all read limits;
- provenance root.

Result fields include stage results even when a later stage fails.

## 7. Diagnostics

Diagnostics are structured and stable:

```text
Stage
Code
Severity
InputOffset?
OutputOffset?
BlockOrdinal?
CommandOrdinal?
FragmentOccurrence?
SourceProvenance
```

Messages are derived presentation, not the stable contract.

## 8. Strict invariants

All codec paths:

- operate on bounded windows;
- preflight output budget;
- use checked arithmetic;
- guarantee command/block progress;
- validate back-references;
- allow only format-defined overlap;
- never clamp;
- never pad;
- never hide trailing input;
- never return partial success;
- cap diagnostics and iterations;
- avoid file-driven unbounded allocation.

## 9. Input modes

Memory, seekable stream, short-read stream, and MIX virtual-entry windows must call one parser state machine. Adapters may stage bounded chunks, but cannot have different tolerance.

Required equality:

- status;
- bytes consumed/produced;
- command/block counts;
- diagnostics and order;
- canonical model hash.

## 10. Forbidden dependencies

- codec layer reading INI;
- MAP reader decoding Base64;
- LZO backend knowing IsoMap record size;
- Format80 decoder constructing overlays;
- MIX reader selecting codec semantics;
- Unity types in Core;
- synthetic fixture writer reusing production decoder formulas;
- fallback from one Format80 variant to another after failure.

## 11. Encoder boundary

Encoding is a later, separately reviewed feature:

- decoder support does not imply encoder support;
- canonical writer policy is independent from reader acceptance;
- no GPL-derived encoder may be ported;
- roundtrip tests distinguish decoded semantic equality from byte equality.
