> **Source notice:** This research was prepared by **ChatGPT Web** from public sources. It did not read the local ProjectBaseline and is not the product of the local Codex Agent. No GPL or unclear-license code was copied, translated, mechanically rewritten, or imported (`code_imported: false`).

# SubTile, Level, and the final byte

## Byte 8 — `SubTileRaw`

Public implementations broadly call byte 8 `SubTile`, `sub_tile`, or `SubTileIndex`. The strongest candidate is an index into the cell slots of the selected TMP tile.

This is not proven solely by the field name. Binding evidence is stronger because modern editors validate it against the loaded tile's `SubTileCount`, and multi-cell TMP files expose indexed cell slots.

### Required boundary

```text
record.SubTileRaw
→ resolved GlobalTileId
→ TheaterTileRegistry entry
→ resolved TMP candidate
→ TMP offset-table slot candidate
→ SubTile validation result
```

The record reader does not know TMP dimensions and therefore cannot validate byte 8.

### Candidate validity

For a resolved TMP with `CellsX × CellsY` slots:

```text
0 <= SubTileRaw < CellsX × CellsY
```

This range check is necessary but not sufficient. The selected slot may be empty because its TMP offset-table entry is zero.

Possible results:

- `ValidPopulatedSubTile`
- `ValidRangeButEmptySlot`
- `OutOfRangeSubTile`
- `TmpUnavailable`
- `TileInterpretationAmbiguous`

### Tool repair behavior

WAE post-load validation sets an out-of-range SubTile to zero and may set the tile to zero if the TMP has no slots. OpenRA also clears tile/subtile when the imported template is unknown. These are editor/importer repairs, not raw-format semantics.

Project Core policy is diagnostic-only. It does not clamp, wrap, skip, or replace SubTile.

### Signed byte errors

The field is read as an unsigned byte by all reviewed implementations. A signed-byte consumer could reinterpret values `128..255` as negative. Tests must include this failure mode even though practical valid TMP slot counts are normally much smaller. Practical size does not change the stored byte width.

## Byte 9 — `LevelRaw`

Public names include `z`, `Level`, `Height`, and `bHeight`. The strongest candidate is the map cell's absolute elevation level.

### Raw type

The reviewed structures use one unsigned byte. The Core retains `LevelRaw` as `u8` and does not apply a gameplay maximum in the record reader.

### Required semantic separation

```text
Map record LevelRaw
≠ TMP cell HeightRaw
≠ TMP RampTypeRaw
≠ TMP color/depth plane
≠ derived corner-height field
≠ movement height
≠ projected/rendered pixel Y offset
```

- `LevelRaw` places the map cell at a map elevation candidate.
- TMP `HeightRaw` is local metadata inside the selected TMP cell.
- TMP `RampTypeRaw` describes a slope-shape candidate.
- depth bytes support visual ordering.
- final corner heights and movement surfaces require later terrain semantics.

### Range

No official runtime maximum was located. Limits found in editors or mods can be:

- UI restrictions;
- renderer assumptions;
- gameplay constraints;
- engine-specific limits;
- safety budgets.

The project should use a configurable semantic limit and report values outside it without changing `LevelRaw`.

## Byte 10 — `FinalByteRaw`

This byte has the weakest settled meaning.

| Source | Name/behavior | Evidence |
|---|---|---|
| EA FinalSun/FinalAlert 2 | `bData2`, copied into and out of editor field data | official editor preservation |
| XCC | `zero3`, generated as zero in conversion path | tool convention |
| OpenRA importer | ignored `zero2` | importer behavior |
| WAE | `IceGrowth`, read and written | modern editor behavior |
| CNCMaps | `IceGrowth`, read and written | renderer/tool behavior |
| MapTool | `IceGrowth`, read and written | map-tool behavior |
| ModEnc | documents ice growth, especially TS snow behavior | community documentation |

### What is confirmed

- byte 10 exists in every 11-byte model reviewed;
- the official editor preserves it as a separate byte;
- several community tools expose it as `IceGrowth`;
- other tools expect or write zero.

### What is not confirmed

- stock RA2/YR runtime meaning;
- whether the semantic is only active in TS/Firestorm;
- whether nonzero values are legal in all theaters;
- whether individual bits have meanings;
- whether it is a scalar, flags field, timer, stage, or reserved byte;
- whether FinalAlert UI edits it directly.

### Required model

```text
FinalByteRaw
FinalByteViews
  UnsignedValue
  IceGrowthCandidate
  BitFlagsCandidate
FinalByteInterpretation
  ProfileId
  EvidenceGrade
  SemanticValue?
  Diagnostics
```

The default raw parser exposes no final semantic name beyond `FinalByteRaw`.

An opt-in TS snow profile may expose an `IceGrowthCandidate`, with `CommunityDocumented` or implementation-specific evidence. That profile must not relabel RA2/YR data globally.

## Writer and roundtrip policy

A lossless writer must preserve all three bytes exactly unless a caller explicitly edits them:

- SubTileRaw;
- LevelRaw;
- FinalByteRaw.

A canonical writer that always writes final byte zero would destroy unknown or ice-growth data. A writer that always calls it IceGrowth may incorrectly impose TS semantics on RA2/YR.

## Diagnostics

Recommended diagnostic identifiers:

- `SubTileOutOfRange`
- `SubTileReferencesEmptyTmpSlot`
- `SubTileCannotValidateWithoutTmp`
- `LevelOutsideConfiguredSemanticRange`
- `FinalByteNonZeroUninterpreted`
- `FinalByteProfileUnavailable`
- `FinalByteInterpretationConflict`

None modifies the source record.
