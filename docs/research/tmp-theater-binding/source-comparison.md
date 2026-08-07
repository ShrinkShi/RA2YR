# Source comparison and conflict register

> GPL and unclear-license material is reference-only. No source code was copied, translated, mechanically rewritten, or converted into a near-structural implementation design.

## 1. Pinned source table

| Source | Pin and paths | License | Relevant scope | Important limits |
|---|---|---|---|---|
| Electronic Arts FinalSun/FinalAlert 2 | `6abf0f557469baea73079c6bf6550709e2e3584e`; `MissionEditorPackLib/MissionEditorPackLib.cpp`, `MissionEditor/MapData.cpp/.h` | GPL-3.0 | official editor integration, theater General keys, map/tile workflows | reuses XCC TMP classes; not independent TMP evidence and not original game runtime |
| XCC public repository | `6f91bf8b00d3acabb1be765118a37c0cb74e85ec`; `misc/cc_structures.h`, `tmp_ts_file.h/.cpp` | file headers GPL-3-or-later; historical SourceForge release lineage differs | packed 52-byte struct, offsets, flags, diamond size and drawing | tool/library behavior; mirror-to-historical-release identity not asserted |
| XCC Utilities SourceForge | release 1.46 source | GPL-2.0 historical project metadata | historical lineage and tool behavior | no unverified revision mapping to the public Git commit |
| OpenRA | `a520984d91eda9de48a62b1d15c1e3bad0d4fb1a`; `OpenRA.Mods.Cnc/SpriteLoaders/TmpTSLoader.cs` | GPL-3.0-or-later | independent importer/render-frame reader; 52-byte detector; diamond/depth/extra planes | skips most header semantics and consumes planes sequentially; not original runtime |
| World-Altering Editor | `b4c9481e9b00fb0a38739049a046f528b6054ce2`; `TmpFile.cs`, `Theater.cs`, `TileSet.cs`, `TheaterTileData.cs`, `TileImage.cs`, `RampType.cs` | GPL-3.0-or-later | modern editor TMP parser, theater registry, cumulative IDs, variations, palettes/LAT roles | 48-byte constant bug; sequential plane reads; NEWURBAN fallback and extensions are editor behavior |
| openra2 / Vanguard | `8ba59f0bcd48ba0c89892c0455eeca7da4408f4c`; `packages/formats/src/tmp/tmpFile.ts` | GPL-3.0 | defensive 52-byte parser and offset validation | explicitly states byte-for-byte XCC layout port; not independent evidence |
| TS++ lineage as cited by WAE | WAE `RampType.cs` attribution to `Vinifera-Developers/TSpp`, `TIBSUN_DEFINES.H` | separate project/license review required | candidate ramp enum values and names | indirect attribution in this dossier; not original RA2/YR source proof |
| ModEnc | `Terrain Control INI Files`, `TMPTiles`, `Theaters`, `IsoMapPack5`, `LAT System`, related pages | community documentation | theater names, extensions, palettes, cumulative tile IDs, LAT terminology | community-maintained, not source-code or runtime proof |
| Project Perfect Mod | fixed TMP/ramp/damaged-data/terrain-expansion discussions | forum/community | historical tool observations, conflict discovery | claims are leads and uncertainty evidence |

## 2. Independence warning

- EA's editor calls bundled XCC TMP code.
- openra2 explicitly ports XCC's layout.
- WAE reuses community/XCC knowledge and TS++ ramp definitions.
- OpenRA is the strongest structurally independent implementation among the reviewed parsers, but intentionally discards much metadata.

Agreement among shared descendants does not count as multiple independent confirmations.

## 3. Field-map comparison

| Field | XCC | OpenRA | WAE | Current decision |
|---|---|---|---|---|
| cell header length | packed struct = 52 | detector and skip pattern = 52 | constant says 48, actual read = 52 | 52 confirmed; 48 is a bug |
| X/Y | signed 32-bit | skipped | signed 32-bit | raw signed candidate |
| plane offsets | signed 32-bit, exposed | mostly ignored | read as unsigned, mostly ignored | preserve raw bits; validate declared-offset view |
| extra rectangle | signed dimensions in struct | signed reads | unsigned dimensions | retain bits plus signed/unsigned candidate views |
| flags | bitfield | only bit 0 used | 32-bit enum bits 0..2 | preserve word and known mask |
| height/terrain/ramp | signed bytes in XCC struct | skipped | unsigned-byte properties | preserve raw bytes; signedness unresolved |
| radar colors | two three-byte groups | skipped | two RGB values | preserve raw component triples |
| trailing 3 bytes | padding | skipped | called uninitialized/trash | preserve and diagnose |

## 4. Plane-layout comparison

| Topic | XCC | OpenRA | WAE | Decision |
|---|---|---|---|---|
| diamond size | `W×H/2` | widening/narrowing rows | fixed tile buffer size | confirmed formula |
| depth presence | offset accessor/assertion | always reads depth | gated by bit 1 | explicit conflict; retain candidate strategies |
| extra color | offset accessor | sequential | sequential | declared offset primary + sequential comparison |
| extra depth | offset accessor | sequential when extra | gated by flags and offset | preserve candidate; validate exact window |
| extra color zero | renderer skips zero | skips zero on compose | renderer-specific | raw parser preserves zero |
| extra depth ≥32 | no parser rule shown | ignored in composed depth | no universal raw rule | permissive adapter behavior only |

## 5. Theater-registry comparison

| Topic | EA editor | WAE | Community | Decision |
|---|---|---|---|---|
| General role keys | Ramp/Cliff/Water/Shore and more | Ramp/Bridge/Ice/LAT keys | broadly documented | typed schema with provenance |
| TileSet section grammar | tool reads registry | scans `TileSet0000...` | four-digit convention | collect all valid candidates; no silent stop |
| gap behavior | not established | stops at first gap | underdocumented | diagnose gap; preserve later sections |
| global tile IDs | editor utilities use registry indices | cumulative `TilesInSet` | documented cumulative numbering | deterministic cumulative ranges |
| filename numbering | community/tool conventions | `FileName + D2(i+1)` | documented | strong candidate, profile-controlled |
| variation suffixes | underconfirmed | base plus `a..f` | modding convention | implementation-specific range |
| NEWURBAN fallback | editor-specific support | `.ubn` fallback to `.urb` | terrain-expansion discussions | explicit editor compatibility profile only |

## 6. Semantic conflicts

| Topic | Competing evidence | Current decision |
|---|---|---|
| `HeightRaw` signedness | XCC signed byte; WAE byte | preserve raw byte and both views |
| terrain enum | field name plus modern LandType enum | no direct original binding; profile-gated |
| ramp enum | TS++-derived 0..20 names | candidate table, not binary validity limit |
| radar ordering/use | RGB naming, graphics-memory ambiguity | retain components 0/1/2 and raw values |
| unknown flag bits | typed models hide them; WAE reports trash | preserve and aggregate; do not require zero |
| damaged-data bit | name agreed, body absent | no damaged-plane decoder yet |
| plane offsets vs sequence | XCC exposes offsets; readers often ignore them | validate both, no silent fallback |
| palette selection | theater profiles/community names | typed theater palette binding, not TMP reader |
| LAT | INI registry relationship | separate semantic binder, not palette or TMP format |
| bridge behavior | TMP sets plus overlays/runtime | cross-format binding result |

## 7. Official versus runtime evidence

The EA source establishes what FinalSun/FinalAlert 2 does and which theater concepts its editor knows. It does not establish:

- exact RA2/YR executable read validation;
- whether unknown flag bits are ignored;
- whether plane offsets or sequential order win;
- exact TileSet gap behavior;
- NEWURBAN fallback in the original runtime;
- pathfinding semantics for terrain/ramp bytes.

These remain separately labelled.

## 8. Known loose or defective behavior

- WAE minimum-header constant is 48 while actual read is 52.
- WAE and OpenRA can ignore stored plane offsets.
- OpenRA always reads a diamond depth plane and filters extra depth values.
- WAE can create empty placeholders for missing TMP files while retaining ID allocation.
- WAE uses file-name substring/editor classification for some display workflows.
- openra2 derives from XCC and cannot be counted as independent confirmation.

## 9. License handling

- GPL implementations are reference-only.
- Community wiki/forum prose is paraphrased and used as convention/conflict evidence.
- No complete palette, TMP byte sequence, theater INI body, or reconstructable terrain content is included.
- Any future implementation must be written independently from the documented field facts and independently authored synthetic fixtures.
