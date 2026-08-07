> **Source notice:** This research was prepared by **ChatGPT Web** from public sources. It did not read the local ProjectBaseline and is not the product of the local Codex Agent. No GPL or unclear-license code was copied, translated, mechanically rewritten, or imported (`code_imported: false`).

# IsoMapPack5 layer boundaries

## Required pipeline

```text
LosslessMapIniDocument
→ PackedIniFragmentCollection
→ StrictBase64Result
→ WestwoodChunkEnvelope
→ RawLzo1XCompatibleDecodeResult
→ ExactDecodedIsoMapStream
→ IsoMapPack5Document
→ IsoMapRecordRaw[]
→ IsoMapCoordinateAnalysis
→ IsoMapTileBindingResult
→ renderer / simulation / editor adapters
```

Each arrow is an explicit contract. No lower layer may silently perform work owned by a later layer.

## Layer contracts

### 1. Lossless map INI

Owns:

- section and key spelling;
- source order;
- duplicate key occurrences;
- comments and whitespace if the selected parser preserves them;
- provenance of every fragment.

Does not own Base64 order policy or tile semantics.

### 2. Numbered fragment collection

Owns:

- fragment-key parsing;
- normalized numeric ordering policy;
- gap, duplicate, leading-zero, and nonnumeric diagnostics;
- aggregate character budget;
- fragment provenance.

It must not apply ordinary INI key override rules to destroy packed-fragment occurrences.

### 3. Strict Base64

Owns alphabet, padding, whitespace, exact consumption, and output budget. It does not know that the payload is a map.

### 4. Chunk envelope

Owns repeated little-endian compressed/uncompressed sizes, payload windows, block count, and aggregate output declarations. A `0/0` block question belongs here, not in the record reader.

### 5. LZO backend

Owns one bounded raw LZO1X-compatible payload. It receives declared input/output windows and returns exact status and diagnostics. It does not parse coordinates or records.

### 6. Exact decoded stream

A byte sequence plus:

- logical provenance;
- exact length;
- aggregate hash;
- block-to-output span mapping;
- backend diagnostics;
- no invented bytes.

### 7. Record reader

Owns fixed-width 11-byte slicing and raw field extraction. It does not load TMP, choose a tile-field interpretation, validate map-domain membership, or create default cells.

### 8. Coordinate analysis

Owns explicit conversion and classification:

- source raw X/Y;
- candidate signed and unsigned views;
- map-domain result;
- rectangular display-canvas coordinate candidate;
- duplicate grouping;
- dense/sparse analysis.

It must not mutate invalid records.

### 9. Theater tile binding

Owns the evidence-gated transition from raw tile views to a global tile ID and then to a registry range, TileSet, TMP logical candidate, variation, and subtile candidate.

Missing registry entries and missing TMP files are binding diagnostics, not parse errors.

### 10. Adapters

Rendering, movement, slope, cliff, bridge, and Unity coordinate conversion occur after binding. They must consume explicit results and must not reinterpret raw bytes.

## Forbidden couplings

- IsoMap reader parsing Base64.
- LZO backend knowing record width.
- Record reader loading theater INI or TMP.
- MIX reader choosing global tile IDs.
- Coordinate validator replacing out-of-domain coordinates.
- Tile binder clamping SubTile.
- Renderer deciding duplicate winner.
- Unity `Vector2`, `Vector3`, Tilemap, Mesh, Texture, Material, or GameObject in Core.

## Roundtrip boundaries

The following are separate capabilities:

1. parse success;
2. raw preservation;
3. semantic interpretation;
4. effective coordinate index;
5. canonical rewrite;
6. original byte-identical roundtrip;
7. FinalAlert reopen;
8. original runtime acceptance.

Success at one level does not imply success at another.

## Evidence policy

A public tool accepting a stream proves only that tool's behavior. FinalAlert or FinalSun source is `ConfirmedByOfficialToolSource`, not original-runtime source. One named reader or writer is `ImplementationSpecificBehavior` unless a stronger source applies.

Agreement across repositories is `ConfirmedByMultipleIndependentImplementations` only when their implementation lineages are demonstrably independent. XCC-derived code, FinalAlert's bundled XCC lineage, OpenRA/openra2 descendants, and acknowledged shared community ancestry are not counted repeatedly. Uncertain independence is `Underconfirmed`; direct disagreement is `ConflictingSources`.

Raw preservation, explicit profiles, diagnostic-only handling, and refusal to guess or repair are `DefensiveDesign`. Project policy is recorded separately from external evidence. ProjectBaseline remains `AuditStatus: NotRun` and is not an evidence grade.
