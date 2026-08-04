# Source comparison and conflict register

> GPL and unclear-license material is reference-only. No code was copied, line-translated, mechanically rewritten or converted into a near-structural C# implementation.

## 1. Pinned source table

| Source | Pin / path | License | Scope | Important limits |
|---|---|---|---|---|
| Electronic Arts FinalSun/FinalAlert 2 | commit `6abf0f557469baea73079c6bf6550709e2e3584e`; `MissionEditor/MapData.cpp`, `FinalSunDlg.cpp`, `MissionEditorPackLib/MissionEditorPackLib.cpp/.h`, bundled XCC TMP sources | GPL-3.0 for EA editor; bundled third-party files retain their licenses | Official editor/tool writer, Base64, Format80, chunked LZO, Preview, IsoMap and TMP integration | Not original game runtime; editor may repair, normalize and discard unsupported data |
| OpenRA engine | commit `a520984d91eda9de48a62b1d15c1e3bad0d4fb1a`; `OpenRA.Mods.Cnc/UtilityCommands/ImportGen2MapCommand.cs`, `SpriteLoaders/TmpTSLoader.cs` | GPL-3.0-or-later | Independent map importer; IsoMap/overlay coordinate conversion; TMP image/depth/extra reader | Importer converts to OpenRA model, repairs unknown tiles and does not preserve all map sections |
| World-Altering Editor | commit `b4c9481e9b00fb0a38739049a046f528b6054ce2`; `MapLoader.cs`, `MapWriter.cs`, `IsoMapPack5Tile.cs` | GPL-3.0-or-later | Modern RA2/YR/TS editor, read/write packs, mission graph and previews | Editor-specific normalization/repair; supports extensions such as wider overlays |
| CnCNet XNA client | commit `e6e367bbe04c1a0dc1e34a8fed2856ea3ab7e8c4`; `MapPreviewExtractor.cs`, `FastMapPreviewExtractor.cs` | repository license/profile; reference-only for this dossier | PreviewPack dimensions, chunked LZO and 3-byte pixel extraction | Launcher/preview consumer, not full map parser or original runtime |
| OmniBlade/XCC mirror | commit `62bb77080f13bdf65c79c84837b7cc264bdd432d`; TMP structures, compression and map utilities | SourceForge GPL-2.0 lineage; reference-only | Historical packed structures, TMP reader/writer and compression tools | Mirror-to-SourceForge revision equivalence not proven; tool behavior is not runtime behavior |
| XCC SourceForge | XCC Utilities 1.46 source release | GPL-2.0 | Historical release/license origin | No unproven file-level revision mapping to Git mirrors |
| Chrono Divide mod SDK | commit `5943c4ae6c19897929d348a417d6d2f1481b75fd`; `MAPS.md` | no clear repository license file located; reference-only | Public RA2 reimplementation support matrix and `ART.<section>` map extension | Explicitly not YR-compatible; support table documents Chrono Divide, not vanilla runtime |
| ModEnc | `Maps`, `IsoMapPack5`, `OverlayPack`, `PreviewPack`, `TMP`, `NewINIFormat`, map-section pages | community factual reference | Extensions/roles, pack descriptions, theater and map-section conventions | Community-maintained; not source code or original-runtime proof |
| Project Perfect Mod | fixed TMP/ramp/bridge/file-format discussions | discussion; code snippets unclear-license | Historical conflicts, damaged-input and tool observations | Claims are leads/uncertainty evidence, not normative contracts |
| RA2 mapping tutorials | RA2DIY/RA2 map community material where accessible | community/tutorial terms | Practical map/editor workflows | Not binary-format authority |

## 2. Independence warning

- EA's editor bundles/uses XCC code; it is not independent of XCC for those components.
- WAE reuses public compression ports and community knowledge.
- CnCNet client and WAE share ecosystem/code ancestry in places.
- OpenRA converts data directly into its own engine model.

Multiple repositories with shared ancestry do not count as multiple independent confirmations.

## 3. Conflict table

| Topic | Competing evidence | Current decision |
|---|---|---|
| `.map/.mpr/.yrm` | community sources describe discovery-role differences, same INI family | one document family with separate extension/discovery metadata |
| packed-fragment order | writers use numeric keys starting at 1; generic INI maps may not preserve order | numeric-order candidate with duplicate ambiguity; raw occurrences retained |
| IsoMap record bytes 4..7 | WAE: signed 32-bit tile index; OpenRA: u16 tile + ignored i16 | preserve raw 32-bit and both word views; unresolved semantic split |
| IsoMap density | OpenRA expects full-size buffer; WAE writer omits clear level-zero cells | sparse/dense acceptance remains profile/golden gated |
| IsoMap final four bytes | padding versus `(0,0)` terminator | presence strongly supported; exact runtime semantics underconfirmed |
| overlay output width | vanilla candidate 1 byte; WAE extension can use 2 bytes | vanilla and extension profiles explicitly separated |
| OverlayData 0xFF | full frame byte versus potential sentinel assumptions | no universal sentinel; overlay-specific semantics later |
| Preview pixel order | RGB/BGR terminology differs across writer and graphics consumer | retain component0/1/2; adapter selection golden-gated |
| Preview section position | WAE says original executables expect Preview sections first | explicit compatibility writer profile; reader preserves order |
| absent preview | WAE writes dummy to avoid reported crashes | reader never fabricates; runtime legality remains profile evidence |
| TMP file header | broad dimensions + offset table agreed | confirmed candidate with checked bounds |
| TMP cell metadata names | height/terrain/ramp/flags/color labels and offsets differ | raw prefix/suffix preserved; named views underconfirmed |
| TMP extra data | bit-0 candidate and extra color/depth planes broadly agreed | strong candidate; exact stock flags beyond bit 0 unresolved |
| depth values | renderer depth versus height/alpha assumptions | retain raw depth plane; interpretation outside parser |
| editor repairs | invalid tile/subtile may be replaced or skipped | implementation-specific; strict Core does not repair silently |
| map-local Art | Chrono Divide documents `ART.` prefix; vanilla evidence incomplete | profile-specific extension, disabled by default for vanilla claims |
| mission opcodes | editors/reimplementations support different subsets | raw opcode/parameters preserved; runtime registry separate |
| roundtrip | editor reopen versus byte/semantic preservation | four distinct roundtrip levels; no implicit equivalence |

## 4. High-confidence structure

Strongly supported:

- INI-shell map family;
- numbered Base64 packed sections;
- chunked LZO envelope for IsoMap/Preview;
- Format80/LCW overlays;
- 11-byte IsoMap records;
- fixed 512×512 overlay domain for vanilla candidate;
- three bytes per preview pixel;
- TMP file dimensions plus cell-offset table;
- separate TMP color/depth planes and optional extra color/depth rectangle;
- map mission data expressed as inter-referencing INI registries/records.

## 5. Evidence still needed

- exact stock meaning/signedness of all 11 IsoMap bytes;
- sparse versus dense legal IsoMap streams;
- Base64 duplicate/gap behavior in original loaders;
- exact Preview channel order and zero-size block behavior;
- exact TMP metadata offsets/names and legal flag bits;
- TMP cell overlap/alias legality;
- original runtime handling of missing/misordered Preview sections;
- vanilla map-local Rules/Art scope and composition order;
- exact RA2 versus YR mission-record schema differences;
- safe semantic roundtrip criteria.

## 6. License handling

- GPL sources: behavior/facts only; no implementation copying.
- unclear-license community code: reference-only.
- wiki/forum prose: paraphrased leads and convention evidence only.
- no original map text, packed body, preview pixels, TMP image/depth data or reconstructable terrain is included.
