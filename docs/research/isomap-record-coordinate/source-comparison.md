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

## Normalized evidence classification

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| Official editor record structure is 11 bytes | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Establishes official editor behavior only. Other tools corroborate, but shared XCC/community lineage is not counted as independent runtime proof. | Parse exact 11-byte records and preserve incomplete remainder separately. | `NotRun` |
| Bytes 4..7 have one settled tile interpretation | `ConflictingSources` | EA FinalSun / FinalAlert 2 and XCC versus WAE, CNCMaps, and MapTool | Split-16 and full-32 models directly disagree. No stock-runtime source resolves the conflict. | Preserve raw32, low/high16, and byte 6/7 views; require an explicit interpretation profile. | `NotRun` |
| Byte 10 exists as a separately preserved field in the official editor | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | This confirms preservation by the editor, not its runtime semantic meaning. | Preserve as `FinalByteRaw`. | `NotRun` |
| Byte 10 universally means IceGrowth in RA2/YR | `Underconfirmed` | WAE, CNCMaps, MapTool, ModEnc; XCC/OpenRA use zero/ignored views | Community/tool naming is substantial but applicability to stock RA2/YR is not established. | Expose an opt-in semantic candidate only; default remains raw. | `NotRun` |
| `(2W-1) × H` is a useful dense canvas convention | `Underconfirmed` | FinalAlert, XCC/OpenRA lineage, CNCMaps, MapTool | Several tools agree, but implementation independence and universal runtime applicability are not proven. | Treat as an explicit coordinate/density profile, not an automatic format fact. | `NotRun` |
| Modern tools intentionally emit sparse streams | `Underconfirmed` | WAE and CNCMaps; ModEnc documentation | Named writers demonstrate tool behavior, but independence and stock-runtime acceptance are not established. | Preserve missing versus explicit-default distinctions; never synthesize during parsing. | `NotRun` |
| Stock runtime duplicate-coordinate winner behavior | `Unresolved` | No original-runtime source located | Array/dictionary last-assignment effects in tools are implementation behavior, not runtime proof. | Preserve every occurrence and fail closed on conflicting duplicates. | `NotRun` |
| Public writer orders are not a reliable identity contract | `Underconfirmed` | FinalAlert, XCC, WAE, CNCMaps, MapTool | Writers use multiple traversals and compression-oriented orders; exact stock-runtime order requirements remain unknown. | Preserve source order and build coordinate indexes separately. | `NotRun` |
| A four-zero decoded trailer is a stable tool/community convention | `ConfirmedCommunityConvention` | WAE, CNCMaps, MapTool, OpenRA, ModEnc | This grade confirms the convention, not universal format or runtime necessity. | Support only through an explicit trailer profile and retain the bytes. | `NotRun` |
| Every valid stream universally requires a four-zero decoded trailer | `ConflictingSources` | Four-zero tool lineage versus FinalAlert reader/writer behavior | Official editor writer passes `N×11`; reader integer-divides decoded length. | Default fail-closed policy distinguishes exact records, explicit four-zero profile, and arbitrary remainder. | `NotRun` |
| Raw/canvas coordinate transforms and domain formulas are universally authoritative | `Underconfirmed` | OpenRA, CNCMaps, FinalAlert/XCC traversal evidence | Formula convergence exists, but shared lineage and runtime applicability are not proven. Signedness also conflicts. | Keep transforms, axis order, signedness, and domain predicate in explicit profiles. | `NotRun` |
| Cumulative TileSet global numbering is a stable community/toolchain convention | `ConfirmedCommunityConvention` | ModEnc LastTilesInSet and editor/tool registry behavior | No original-runtime source was reviewed. | Construct deterministic cumulative ranges with checked arithmetic. | `NotRun` |
| Missing TMP assets must not shift later global tile ranges | `DefensiveDesign` | Project preservation policy informed by registry conventions | This is a project integrity rule, not external runtime evidence. | Reserve ranges from registry metadata and diagnose missing assets separately. | `NotRun` |
| `.ubn → .urb` fallback is a universal vanilla behavior | `ImplementationSpecificBehavior` | WAE/editor-compatibility behavior | The observed fallback belongs to a named editor compatibility profile and is not stock-runtime proof. | Keep fallback opt-in and provenance-labeled. | `NotRun` |
| ProjectBaseline aggregate findings are already available | `Unresolved` | No audit executed | ProjectBaseline was not read during this research. Planned audit is not evidence. | `FutureEvidenceSource: ProjectBaselineAggregateAudit`. | `NotRun` |

## Official tool evidence

The following are `ConfirmedByOfficialToolSource` for FinalAlert/FinalSun behavior:

- record width 11 in FinalAlert's `MAPFIELDDATA`;
- official editor's low-16 ground/tile representation;
- preservation of bytes 6..8 through adjacent map metadata and SubTile storage;
- preservation of byte 10 as a separate editor field;
- dense valid-domain writer traversal candidate;
- reader's permissive decoded-length integer division;
- writer path using `recordCount × 11` source bytes before its pack encoder.

These findings do not prove the stock game runtime.

## Cross-implementation evidence with unresolved independence

The following are `Underconfirmed` rather than multiple-independent confirmation:

- 11-byte records are implemented across official editor, OpenRA, WAE, CNCMaps, MapTool, and XCC lineages;
- `(2W-1) × H` is a common normalized dense canvas count;
- coordinate transform formulas converge across OpenRA and CNCMaps;
- SubTile and Level positions converge;
- sparse records are intentionally produced by more than one modern tool.

Shared knowledge and code ancestry prevent treating every repository as a separate vote.

## Community conventions

The following are `ConfirmedCommunityConvention` only for the convention itself:

- byte 10 is widely called IceGrowth, particularly in TS snow discussions;
- omission of default clear level-0 records is documented in the community;
- compression-oriented sort-order choices are documented;
- cumulative TileSet global IDs and the danger of shifting later ranges are established toolchain conventions.

Runtime applicability may remain `Underconfirmed` or `Unresolved` as stated in the normalized table.

## Unresolved

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

- Official editor source is not original-runtime source.
- An importer that opens common maps may still ignore fields or repair errors.
- A writer producing accepted maps does not prove every value it can serialize is accepted by stock runtime.
- Multiple tools sharing XCC/OpenRA lineage are not independent confirmation.
- Community documentation can describe plausible runtime behavior without supplying source proof; the convention may be `ConfirmedCommunityConvention` while runtime applicability remains `Underconfirmed`.
- Future ProjectBaseline work remains `AuditStatus: NotRun` until executed and cannot by itself become `ConfirmedByOriginalRuntimeSource`.
