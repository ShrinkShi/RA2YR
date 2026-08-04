# Ramp, height, and terrain semantics

## 1. Four different height domains

Do not merge these fields:

1. **Map Level** — elevation attached to one IsoMap placement record.
2. **TMP HeightRaw** — local vertical placement candidate for one subtile inside a multi-cell TMP.
3. **TMP RampTypeRaw** — slope/corner topology candidate.
4. **TMP depth plane** — per-pixel render-order/Z information.

Final movement and render geometry can use several of them, but the raw readers expose them independently.

## 2. TMP HeightRaw

Public tools use TMP `height` to shift a cell image vertically by a multiple of half the tile pixel height or an engine cell-height constant.

Strong interpretation:

- local to the TMP cell;
- useful for multi-cell templates whose component cells occupy different local levels;
- not the absolute map height;
- combined later with map placement Level.

Signedness remains unresolved. Core stores the raw byte and exposes signed and unsigned candidate views.

## 3. RampTypeRaw

The common enum lineage describes values `0..20`:

- `0`: flat/none;
- `1..4`: four basic slopes;
- `5..8`: outside-corner slopes;
- `9..12`: inside-corner slopes;
- `13..16`: steep/full slopes;
- `17..20`: double/alternating ramps.

The reviewed WAE enum is attributed to TS++/`TIBSUN_DEFINES.H`, not original RA2/YR source. Therefore:

- retain raw byte;
- allow a named `TsppRamp20` candidate table;
- do not reject values above 20 at binary parse time;
- do not construct corner heights inside the TMP reader;
- report whether a sample is inside the candidate enum.

## 4. TerrainTypeRaw

The byte at `0x29` is conventionally named terrain or land type. A modern editor model includes categories such as:

- Clear
- Ice
- Beach
- Rough
- Road
- Railroad
- Tiberium
- Rock
- Weeds
- Tunnel
- Water

Direct one-to-one binding between the raw TMP byte and that exact enum order remains underconfirmed in the reviewed sources.

The project should represent:

```text
TerrainTypeRaw
TerrainTypeCandidateKind
TerrainTypeCandidateValue
TerrainTypeEvidence
```

Movement-cost and buildability decisions belong to a later terrain semantic registry.

## 5. Final corner-height candidate

A later adapter may compute corner elevations from:

```text
map placement Level
+ local TMP HeightRaw
+ RampType candidate corner deltas
```

This operation requires an explicit convention for:

- half-level versus full-level units;
- orientation of map north/east/south/west;
- cell corner ordering;
- steep and double-ramp deltas;
- multi-cell template offsets;
- theater or game-specific differences.

No such convention is frozen in the binary reader.

## 6. Terrain behavior boundary

Final terrain behavior may depend on:

- TMP TerrainTypeRaw;
- TileSet semantic role from theater INI;
- map overlay occupying the cell;
- bridge state;
- Rules locomotor/speed settings;
- extension-engine overrides;
- dynamic game state.

A TMP byte alone cannot answer whether a unit can move, build, swim, or deploy.

## 7. Ramp registry binding

The theater `[General]` keys such as `RampBase`, `RampSmooth`, and `SlopeSetPieces` identify TileSets used for slope workflows. They do not replace per-cell `RampTypeRaw`.

Binding result should include:

```text
RampSemanticBinding
- TileSetRole
- TmpCellRampRaw
- CandidateRampKind
- LocalHeightRaw
- EvidenceLevel
- Status
- Diagnostics
```

A TileSet can be a ramp set while containing cells with different ramp values.

## 8. Invalid and unknown combinations

Diagnostics should cover:

- flat ramp with nonzero local height;
- nonflat ramp outside known enum;
- terrain value outside configured table;
- special ramp TileSet with all cells unclassified;
- multi-cell TMP with inconsistent local heights;
- map SubTile selecting an empty cell;
- final corner heights outside configured world-height budget.

These are semantic diagnostics. Safe raw parsing may still succeed.

## 9. Editor versus runtime

Editors may:

- flatten adjacent cells;
- choose replacement ramp tiles;
- repair mismatched slopes;
- substitute Marble Madness sets;
- infer terrain by TileSet name.

Such behavior is not binary parsing and is not evidence that the original runtime performs the same repair.

## 10. Evidence status

| Claim | Status |
|---|---|
| TMP height is local cell elevation/visual offset | multiple tool implementations |
| RampType 0..20 table | TS++/community-derived candidate |
| TerrainType exact enum order | `Underconfirmed` |
| depth plane controls movement height | rejected boundary |
| map Level and TMP height are identical | rejected boundary |
| final corner algorithm | `Unresolved` pending source and local audit |
