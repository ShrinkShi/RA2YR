> **Source notice:** This research was prepared by **ChatGPT Web** from public sources. It did not read the local ProjectBaseline and is not the product of the local Codex Agent. No GPL or unclear-license code was copied, translated, mechanically rewritten, or imported (`code_imported: false`).

# Recommended Core implementation boundaries

This is an implementation design, not code.

## Component graph

```text
ExactDecodedIsoMapStream
  ↓
IsoMapPack5Reader
  ↓
IsoMapPack5Document + IsoMapParseResult
  ↓
IsoMapCoordinateAnalyzer
  ↓
IsoMapRecordIndex + IsoMapDensityAnalysis
  ↓
IsoMapTileInterpreter
  ↓
IsoMapTileBinder
  ↓
IsoMapTileBindingResult
```

The existing upstream map-compression pipeline remains separate. The theater registry and TMP metadata services are injected read-only dependencies.

## Candidate data models

### `IsoMapPack5Document`

Contains:

- source provenance;
- exact decoded stream length and hash;
- immutable `IsoMapRecordRaw` sequence in source order;
- `IsoMapDecodedStreamTrailer`;
- read limits used;
- parse diagnostics;
- whether complete semantic success was achieved.

It does not contain Unity objects, TMP pixels, or repaired effective cells.

### `IsoMapRecordRaw`

Suggested fields:

```text
SourceOrdinal
RecordByteOffset
XRaw16
YRaw16
TileFieldRaw32
TileFieldLowRaw16
TileFieldHighRaw16
SubTileRaw
LevelRaw
FinalByteRaw
RawRecordBytes or lossless bounded slice
```

Field names ending in `Raw` do not assert semantic meaning.

### `IsoMapTileFieldViews`

Derived without loss:

```text
Unsigned32
Signed32
LowUnsigned16
LowSigned16
HighUnsigned16
HighSigned16
Byte6Raw
Byte7Raw
```

Views are value projections, not winners.

### `IsoMapDecodedStreamTrailer`

Contains offset, length, raw bytes or bounded source slice, zero classification, candidate kinds, evidence grade, and diagnostics.

### `IsoMapCoordinateDomain`

Serializable description of:

- dimension source;
- width and height;
- raw-coordinate signedness profile;
- raw-to-canvas transform ID;
- valid-domain predicate ID;
- expected dense count;
- arithmetic validation result;
- evidence grade.

### `IsoMapCoordinateResult`

Per record:

- raw coordinate views;
- selected profile;
- display column/expanded Y/row candidates;
- parity status;
- in-domain status;
- diagnostics.

### `IsoMapRecordIndex`

Contains:

- source-order records;
- coordinate-key to record-ordinal groups;
- duplicate groups;
- out-of-domain records;
- missing-coordinate summary or bounded set;
- deterministic aggregate hashes.

It never drops duplicates.

### `IsoMapDuplicateCoordinateGroup`

Contains coordinate key, ordered record ordinals, byte-identical classification, semantic-conflict fields, evidence profile, and ambiguity status.

### `IsoMapDensityAnalysis`

Contains expected domain count, source record count, distinct in-domain count, missing count, duplicate count, out-of-domain count, classification, and diagnostics.

### `IsoMapTileBindingResult`

Contains one binding trace per eligible record or coordinate group plus document aggregates. It distinguishes parse success, coordinate validity, tile interpretation, registry lookup, TMP resolution, and SubTile validation.

### `IsoMapTileResolutionTrace`

Suggested fields:

```text
RecordOrdinal
RawTileViews
InterpretationCandidates
SelectedInterpretation?
EvidenceGrade
GlobalTileIdCandidates
RegistryRangeCandidates
SelectedTileSet?
TileIndexInSet?
TmpLogicalCandidates
TmpWinner?
SubTileValidation
SuppressedCandidates
Diagnostics
```

## Policies are explicit inputs

Candidate policy interfaces/descriptors:

- `IsoMapRecordLayoutProfile`
- `IsoMapTileFieldInterpretationPolicy`
- `IsoMapCoordinatePolicy`
- `IsoMapTrailerPolicy`
- `IsoMapDuplicatePolicy`
- `IsoMapMissingCellPolicy`
- `IsoMapBindingPolicy`
- `IsoMapReadLimits`

No policy is inferred from whichever interpretation happens to resolve.

## Reader contract

`IsoMapPack5Reader` receives a bounded exact decoded window and:

1. validates the window length against limits;
2. computes full 11-byte record count with checked arithmetic;
3. reads only complete records;
4. retains any remainder as trailer;
5. returns structured status based on the selected trailer policy;
6. advances on every successful record or terminates with a diagnostic;
7. performs no coordinate, tile, TMP, or Unity work.

## Input modes

Memory, seekable Stream, non-seekable/short-read Stream, and MIX entry window must share one state machine and one diagnostic contract.

Required behavior:

- exact bounded reads;
- short-read loops with progress checks;
- no assumption that one `Read` fills a request;
- no seeking outside an entry window;
- same record/trailer hashes across modes;
- cancellation and limits applied consistently.

## Coordinate analyzer contract

- consumes raw records and dimensions;
- does checked transformations;
- never swaps or repairs coordinates automatically;
- emits domain and parity classifications;
- builds duplicate groups deterministically;
- remains independent of tile semantics.

## Tile interpreter contract

- consumes `IsoMapTileFieldViews` and an explicit profile;
- can return multiple candidates;
- retains high metadata;
- assigns evidence grades;
- never queries TMP files to decide which binary interpretation is correct.

## Binder contract

- consumes interpreted GlobalTileId candidates and a frozen theater registry;
- resolves cumulative ranges without compacting missing assets;
- delegates logical content lookup to the content provider layer;
- validates SubTile only from parsed TMP metadata;
- returns traces and diagnostics;
- performs no repair.

## Diagnostics

Suggested stable identifiers:

### Parse

- `IsoMapDecodedLengthBudgetExceeded`
- `IsoMapRecordTruncated`
- `IsoMapUnexpectedTrailer`
- `IsoMapNonZeroTrailer`
- `IsoMapDiagnosticBudgetExceeded`

### Coordinates

- `IsoMapCoordinateArithmeticOverflow`
- `IsoMapCoordinateParityInvalid`
- `IsoMapCoordinateOutOfDomain`
- `IsoMapCoordinateProfileAmbiguous`
- `IsoMapDuplicateCoordinate`
- `IsoMapConflictingDuplicateCoordinate`

### Tile interpretation and binding

- `IsoMapTileFieldInterpretationAmbiguous`
- `IsoMapTileHighBitsNonZero`
- `IsoMapTileIdOutOfRegistryRange`
- `IsoMapTileSetRangeAmbiguous`
- `IsoMapTmpMissing`
- `IsoMapTmpCandidateAmbiguous`
- `IsoMapSubTileOutOfRange`
- `IsoMapSubTileEmptySlot`
- `IsoMapFinalByteUninterpreted`

Every diagnostic includes source provenance, record ordinal or stream offset where safe, severity, policy ID, evidence grade, and bounded context. Public audit output excludes per-record coordinates and values.

## Limits

`IsoMapReadLimits` should include:

- maximum decoded bytes;
- maximum records;
- maximum trailer bytes retained;
- maximum coordinate-index entries;
- maximum duplicate groups;
- maximum records per duplicate group;
- maximum missing-coordinate materialization count;
- maximum tile interpretations per record;
- maximum registry candidates per tile;
- maximum TMP candidates;
- maximum diagnostics;
- maximum aggregate trace entries.

Counts are checked before allocation. When detailed collection exceeds a limit, return aggregate counts plus `BudgetExceeded`; do not continue unbounded.

## Determinism

All derived ordering keys must be centralized and serializable. Hashable result inputs include:

- parser version/profile;
- coordinate profile;
- tile interpretation profile;
- trailer policy;
- duplicate policy;
- theater registry hash;
- content-resolution policy hash;
- limits.

Filesystem enumeration and dictionary iteration cannot affect canonical results.

Synthetic fixture builders must not call production conversion, sorting, or interpretation functions. Expected bytes and coordinates are authored independently.

## Layer prohibitions

- INI resolver does not parse records.
- record reader does not call LZO.
- LZO backend does not know 11-byte width.
- record reader does not instantiate default cells.
- coordinate analyzer does not resolve TileSets.
- binder does not read theater INI directly.
- TMP reader does not decide global precedence.
- rendering does not select duplicate winners.
- no magic priority numbers scattered across components.
- no UnityEngine dependency in Core.

## Preservation and writer boundary

A future writer must be a separate component. Parsing does not imply permission to canonicalize.

For byte-identical or lossless rewrite, retain:

- raw record bytes;
- source record order;
- duplicate and out-of-domain records;
- trailer bytes;
- tile high metadata;
- final byte;
- upstream chunk boundaries and fragment grouping where needed.

A canonical writer requires explicit choices for tile width, signedness, density, order, duplicate handling, default cells, final byte, and trailer. M3-R4 selects none of them.
