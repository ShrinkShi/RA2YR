> **Source notice:** This research was prepared by **ChatGPT Web** from public sources. It did not read the local ProjectBaseline and is not the product of the local Codex Agent. No GPL or unclear-license code was copied, translated, mechanically rewritten, or imported (`code_imported: false`).

# 11-byte record layout and field map

## Width

The public implementations reviewed converge on an 11-byte record. Ten- and twelve-byte models have no supporting source in this study.

## Normalized width evidence

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| IsoMapPack5 records are 11 bytes wide in the official editor structure | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 `MAPFIELDDATA` | This establishes official editor behavior, not original-runtime consumption. XCC, OpenRA, WAE, CNCMaps, and MapTool corroborate the width but shared lineage is not counted repeatedly. | Parse only exact 11-byte records; retain incomplete bytes as a diagnosed trailer/partial result. | `NotRun` |

## Raw byte map

| Offset | Size | Required raw model | Common names | Grade | Source | Notes | Policy | AuditStatus |
|---:|---:|---|---|---|---|---|---|---|
| `0` | 2 | `XRaw16` | X, RX, wX | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Official editor places the first coordinate at bytes 0..1. Signedness and stock-runtime constraints remain unconfirmed. | Preserve raw16 and expose signed/unsigned candidate views. | `NotRun` |
| `2` | 2 | `YRaw16` | Y, RY, wY | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Official editor places the second coordinate at bytes 2..3. Internal axis naming does not prove runtime coordinate semantics. | Preserve raw16 and expose signed/unsigned candidate views. | `NotRun` |
| `4` | 4 | `TileFieldRaw32` plus low/high views | TileIndex, TileNum, wGround + data | `ConflictingSources` | EA FinalSun / FinalAlert 2 and XCC versus WAE, CNCMaps, and MapTool | Official editor/XCC lineage exposes a 16-bit tile plus adjacent bytes, while modern tools expose one 32-bit tile. No unique runtime interpretation is selected. | Preserve raw32, low/high 16-bit, and byte 6/7 views under explicit profiles. | `NotRun` |
| `8` | 1 | `SubTileRaw` | SubTile, TileSubIndex | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Official editor stores SubTile at byte 8; other tools corroborate. Exact runtime validation belongs to TMP binding. | Preserve unsigned byte; do not clamp or validate in the record reader. | `NotRun` |
| `9` | 1 | `LevelRaw` | Z, Level, Height | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Official editor stores its height/level field at byte 9; exact runtime range and semantics remain underconfirmed. | Preserve unsigned byte and defer semantic limits. | `NotRun` |
| `10` | 1 | `FinalByteRaw` | zero3, bMapData2, IceGrowth | `Underconfirmed` | EA FinalSun / FinalAlert 2, XCC, OpenRA, WAE, CNCMaps, MapTool, ModEnc | The official editor preserves a separate byte, but tools disagree between zero/ignored and IceGrowth naming. Stock RA2/YR meaning is unresolved. | Preserve the byte raw; semantic names require an explicit profile. | `NotRun` |

## Source-specific layouts

### EA FinalSun / FinalAlert 2 official editor

Pinned source: commit `6abf0f557469baea73079c6bf6550709e2e3584e`, `MissionEditor/MapData.h` and `MapData.cpp`, GPL-3.0.

Packed structure:

```text
u16 wX
u16 wY
u16 wGround
u8  bData[3]
u8  bHeight
u8  bData2[1]
```

`FIELDDATA` stores `wGround`, a 16-bit `bMapData`, a byte `bSubTile`, a byte `bHeight`, and a byte `bMapData2`. The editor copies three bytes starting at `bMapData`, so bytes 6..7 preserve `bMapData` and byte 8 preserves `bSubTile`. The final byte is preserved through `bMapData2`. This proves official editor preservation, not original runtime semantics.

### XCC

Pinned source: commit `6f91bf8b00d3acabb1be765118a37c0cb74e85ec`, `misc/cc_structures.h`, GPL file header.

```text
u16 x
u16 y
i16 tile
u8  zero1
u8  zero2
u8  sub_tile
u8  z
u8  zero3
```

XCC names bytes 6, 7, and 10 as zero fields and writes zero in conversion paths. This is a tool canonicalization choice and cannot erase the official editor's raw preservation evidence.

### OpenRA importer

Pinned source: commit `a520984d91eda9de48a62b1d15c1e3bad0d4fb1a`, `OpenRA.Mods.Cnc/UtilityCommands/ImportGen2MapCommand.cs`, GPL-3.0-or-later.

```text
u16 rx
u16 ry
u16 tilenum
i16 ignoredZero1
u8  subtile
u8  z
u8  ignoredZero2
```

OpenRA discards bytes 6..7 and 10 during import. It is therefore not a lossless source for those fields.

### World-Altering Editor

Pinned source: commit `b4c9481e9b00fb0a38739049a046f528b6054ce2`, `IsoMapPack5Tile.cs`, `MapLoader.cs`, and `MapWriter.cs`, GPL-3.0-or-later.

```text
i16 X
i16 Y
i32 TileIndex
u8  SubTileIndex
u8  Level
u8  IceGrowth
```

Its writer serializes the same model and adds four decoded zero bytes after all records.

### CNCMaps renderer

Pinned source: commit `afb9c1ec118f5128cbc1f3fb5e35c7dfa0e422fb`, `MapFile.cs`, `MapObjects.cs`, and `TileLayer.cs`. Repository default code is MIT, with explicit exceptions for imported GPL files; the reviewed map model files carry no conflicting header but remain reference-only.

It reads/writes unsigned 16-bit coordinates, a signed 32-bit tile integer, and three final bytes named SubTile, Z, and IceGrowth.

### MapTool

Pinned source: commit `f85f2226905496139f1258b5854fad915f9bbac6`, `MapTool.Logic/MapFile.cs`, GPL-2.0-or-later.

It reads unsigned 16-bit coordinates, signed 32-bit tile index, SubTile, Level, and IceGrowth, then stores by coordinate.

## Required raw data model

```text
IsoMapRecordRaw
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
  RawBytes[11] or equivalent lossless window
```

Derived views must not overwrite raw fields:

- `XUnsigned`, `XSigned`
- `YUnsigned`, `YSigned`
- `TileUnsigned32`, `TileSigned32`
- `TileLowUnsigned16`, `TileLowSigned16`
- `TileHighUnsigned16`, `TileHighSigned16`

## Signedness

- EA and XCC declare X/Y as unsigned 16-bit.
- WAE stores X/Y as signed 16-bit.
- Public maps and tools generally operate in positive ranges, but that observation does not settle format signedness.
- The project should reject no raw value at the byte-reader layer solely because one interpretation is negative.

## Writer observations

- Official editor writes its 16-bit ground field and preserves its adjacent metadata bytes.
- XCC conversion writes the three zero-named bytes as zero.
- WAE, CNCMaps, and MapTool write a full 32-bit tile integer.
- No public official runtime writer was found.

## Non-lossless behavior to avoid

- reading only `u16 tile` and dropping bytes 6..7;
- reading `i32 tile` and discarding low/high views;
- converting `0xFFFF` to tile 0 inside the raw reader;
- treating byte 10 as padding and deleting it;
- using host endianness without explicit little-endian reads;
- accepting partial 11-byte records.
