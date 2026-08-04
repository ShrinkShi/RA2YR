> **Source notice:** This research was prepared by **ChatGPT Web** from public sources. It did not read the local ProjectBaseline and is not the product of the local Codex Agent. No GPL or unclear-license code was copied, translated, mechanically rewritten, or imported (`code_imported: false`).

# Source comparison, provenance, and licensing

All sources are behavior-level references. `code_imported: false` for every row.

## Comparison table

| Source | Pin and path | License | Role | Record model | Density/order | Trailer | Independence / caveat |
|---|---|---|---|---|---|---|---|
| EA FinalSun / FinalAlert 2 | `6abf0f557469baea73079c6bf6550709e2e3584e`; `MissionEditor/MapData.h`, `MapData.cpp`, `MissionEditorPackLib/MissionEditorPackLib.cpp` | GPL-3.0 | official editor reader/writer | `u16 X`, `u16 Y`, `u16 ground`, 2 raw metadata bytes, SubTile, Height, final raw byte | dense diamond traversal candidate | reader integer-divides total output by 11; cited writer passes `N×11` | strongest editor provenance; not game runtime; integrates XCC-derived pack library |
| XCC public repository | `6f91bf8b00d3acabb1be765118a37c0cb74e85ec`; `misc/cc_structures.h`, `map_ts_encoder.cpp` | GPL file headers; historical SourceForge lineage | tool/parser/converter | `u16 X`, `u16 Y`, `i16 tile`, zero1, zero2, SubTile, Z, zero3 | canonical dense diamond traversal; format4 conversion also present | no universal decoded +4 conclusion established here | many descendants share XCC knowledge; not independent runtime evidence |
| OpenRA | `a520984d91eda9de48a62b1d15c1e3bad0d4fb1a`; `OpenRA.Mods.Cnc/UtilityCommands/ImportGen2MapCommand.cs` | GPL-3.0-or-later | reimplementation importer | low `u16 tile`, ignores high 16 and final byte | assumes `(2W-1)×H` records | allocates `N×11+4`, calls final 4 no-more-data header | importer repairs invalid templates; not lossless and not stock runtime |
| World-Altering Editor | `b4c9481e9b00fb0a38739049a046f528b6054ce2`; `IsoMapPack5Tile.cs`, `MapLoader.cs`, `MapWriter.cs` | GPL-3.0-or-later | modern editor reader/writer | `i16 X`, `i16 Y`, `i32 TileIndex`, SubTile, Level, IceGrowth | sparse writer; sorts X/Level/TileIndex | explicitly appends four decoded zero bytes | modern behavior; repairs invalid tile/subtile; not vanilla proof |
| CNCMaps renderer | `afb9c1ec118f5128cbc1f3fb5e35c7dfa0e422fb`; `MapFile.cs`, `TileLayer.cs`, `MapObjects.cs` | repository default MIT with file-level GPL exceptions noted in README; reviewed files remain reference-only | renderer and writer | `u16 X/Y`, `i32 TileNum`, SubTile, Z, IceGrowth | supports dense and sparse; tries multiple compression orders | allocates/writes `N×11+4` | acknowledges OpenRA/XCC influence; not counted as fully independent format discovery |
| MapTool | `f85f2226905496139f1258b5854fad915f9bbac6`; `MapTool.Logic/MapFile.cs`, `MapTile.cs` | GPL-2.0-or-later | map transformation tool | `u16 X/Y`, `i32 TileIndex`, SubTile, Level, IceGrowth | dictionary coordinate model; dense defaults plus parsed records | `N×11+4` convention | last-assignment effects are tool behavior, not format policy |
| Chrono Divide public SDK | `5943c4ae6c19897929d348a417d6d2f1481b75fd` | repository-specific public SDK terms; inspect before reuse | public mod SDK | no public IsoMapPack5 parser found in targeted search | no evidence | no evidence | absence is recorded; no behavior inferred |
| CnCNet client | `e6e367bbe04c1a0dc1e34a8fed2856ea3ab7e8c4` | repository license applies | launcher/client and bundled map content | no load-bearing low-level reader located in targeted search | no evidence | no evidence | map bodies were not used as evidence or decoded in this research |
| ModEnc IsoMapPack5 | permanent page candidate `oldid=22328` | community wiki terms; documentation only | community documentation | documents 11-byte fields and IceGrowth | documents sparse omission and compression-oriented sorting | documents four-byte termination convention in community lineage | not source code and not official runtime proof |
| ModEnc LastTilesInSet | fixed page/revision should be retained by future citation audit | community wiki terms | community registry documentation | explains cumulative global tile numbering | n/a | n/a | supports registry reasoning at community grade |
| Project Perfect Mod | no single stable load-bearing post pinned in this pass | varies by post | community discussion | no conclusion derived | no conclusion | no conclusion | withheld rather than cite an unstable or ambiguous thread |
| ModdingWiki | no dedicated stable RA2/YR IsoMapPack5 page located | wiki terms | community documentation | no conclusion derived | no conclusion | no conclusion | do not invent a source merely to fill the requested list |

## Detailed evidence classification

### ConfirmedByOfficialEditorSource

- record width 11 in FinalAlert's `MAPFIELDDATA`;
- official editor's low-16 ground/tile representation;
- preservation of bytes 6..8 through adjacent map metadata and SubTile storage;
- preservation of byte 10 as a separate editor field;
- dense valid-domain writer traversal candidate;
- reader's permissive decoded-length integer division;
- writer path using `recordCount × 11` source bytes before its pack encoder.

These findings do not prove the stock game runtime.

### ConfirmedByIndependentImplementation

Using independence conservatively:

- 11-byte records are implemented across official editor, OpenRA, WAE, CNCMaps, MapTool, and XCC lineages;
- `(2W-1) × H` is a common normalized dense canvas count;
- coordinate transform formulas converge across OpenRA and CNCMaps;
- SubTile and Level positions converge;
- sparse records are intentionally produced by more than one modern tool.

Shared knowledge and code ancestry prevent treating every repository as a separate vote.

### CommunityDocumented

- byte 10 as IceGrowth, particularly in TS snow behavior;
- omission of default clear level-0 records;
- sort-order choices for compression;
- cumulative TileSet global IDs and the danger of shifting later ranges.

### Unresolved

- original runtime's complete 32-bit versus split-16 tile interpretation;
- original runtime duplicate-coordinate behavior;
- exact sparse acceptance rules in stock RA2/YR;
- universal meaning or presence of four decoded trailing bytes;
- signedness and legal range of coordinates and Level;
- RA2/YR semantics of byte 10;
- original writer's canonical order;
- exact empty/sentinel tile rules.

## License and clean-room boundary

GPL and unclear-license code is reference-only:

- no copied functions;
- no line-by-line translation;
- no mechanical control-flow rewrite;
- no near-structural C# pseudocode;
- no constants table copied when behavior can be specified independently;
- no test fixture built from implementation-private output bytes.

Allowed research output:

- byte offsets and sizes;
- independently stated field candidates;
- observable input/output contracts;
- source conflicts;
- safety limits;
- data-model boundaries;
- synthetic fixture specifications;
- aggregate audit requests.

A future implementation should derive from this format dossier and independently authored fixtures. Permissive dependencies require separate legal, security, maintenance, and platform review.

## Source-strength cautions

- Official editor source is not official runtime source.
- An importer that opens common maps may still ignore fields or repair errors.
- A writer producing accepted maps does not prove every value it can serialize is accepted by stock runtime.
- Multiple tools sharing XCC/OpenRA lineage are not independent confirmation.
- Community documentation can describe real runtime behavior without supplying source proof; its grade remains `CommunityDocumented` until stronger evidence is obtained.
- Future ProjectBaseline observations are `ObservedByFutureProjectBaselineAudit`, not runtime proof by themselves.
