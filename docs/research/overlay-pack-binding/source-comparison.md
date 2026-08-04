# Source comparison and license boundary

> **Research provenance:** This document was produced by ChatGPT Web from public sources. It did not read ProjectBaseline, is not a local Codex Agent artifact, and imports no code. GPL or unclear-license sources are reference-only; no source was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

## 1. Source matrix

| Source | Pin / permanent location | Relevant paths | License | Category | Load-bearing observations | Independence / lineage | Use |
|---|---|---|---|---|---|---|---|
| EA FinalSun / FinalAlert 2 | `6abf0f557469baea73079c6bf6550709e2e3584e` | `MissionEditor/MapData.cpp`, `MapData.h`, `MissionEditorPackLib/*` | GPL-3.0-or-later in published source headers | official editor | separate sections; explicit 262144 outputs; type prefill `0xFF`; data prefill zero; encode 262144; internal transposed storage mapping | editor integrates XCC-derived packing lineage; not game runtime | reference-only |
| OpenRA | `a520984d91eda9de48a62b1d15c1e3bad0d4fb1a` | `OpenRA.Mods.Cnc/UtilityCommands/ImportGen2MapCommand.cs`, common LCW/Format5 helpers | GPL-3.0-or-later | reimplementation/importer | independent section decode; `1 << 18` arrays; `rawX + 512*rawY`; `0xFF` skip; type-specific resource/actor handling | community implementation; XCC knowledge lineage | reference-only |
| World-Altering Editor | `b4c9481e9b00fb0a38739049a046f528b6054ce2` | `MapLoader.cs`, `MapWriter.cs`, `Rules.cs`, `OverlayType.cs`, `Overlay.cs`, connected/bridge mutations | GPL-3.0-or-later | editor/reimplementation | ordinary 512² byte arrays; extended 16-bit type profile for `NewINIFormat >= 5`; `Y*512+X`; list-index registry behavior; FrameIndex model; connected and bridge editor logic | shares CNCMaps/OpenRA ecosystem components | reference-only |
| XCC Utilities mirror | `6f91bf8b00d3acabb1be765118a37c0cb74e85ec` | `misc/map_ts_encoder.cpp`, `misc/cc_structures.h`, compression helpers | GPL-3.0 in inspected mirror headers; historical SourceForge lineage may differ by release | tool/reference implementation | fixed map-pack structures; `x + 512*y` family conventions; Format80/Format5 lineage | ancestor for several later tools and EA editor components | reference-only |
| XCC SourceForge | Utilities 1.46 release, 2008-05-02; exact SVN mapping unresolved | release archive / project history | GPL-2.0 lineage reported by project release | historical tool | provenance anchor for XCC behavior | not assumed byte-identical to GitHub mirror | reference-only |
| CNCMaps | `afb9c1ec118f5128cbc1f3fb5e35c7dfa0e422fb` | `CNCMaps.FileFormats/Map/MapFile.cs`, `MapObjects.cs`, Format5 helpers | repository MIT default with explicit OpenRA/XCC GPL exceptions | renderer/reimplementation | independent sections; fixed `1 << 18`; `rawX + 512*rawY`; `0xFF` empty; generic Overlay ID/value object | explicitly acknowledges OpenRA/XCC code use | reference-only |
| MapTool | `f85f2226905496139f1258b5854fad915f9bbac6` | `MapTool.Logic/MapFile.cs`, `MapTile.cs`, pack helpers | GPL-2.0-or-later | map editing tool | fixed `1 << 18`; index-to-coordinate `x=i%512`, `y=i/512`; separate FrameIndex data; write both arrays | uses/acknowledges OpenRA ecosystem pack logic | reference-only |
| Chrono Divide public SDK | `5943c4ae6c19897929d348a417d6d2f1481b75fd` | public SDK tree | public repository license not used as parser evidence | mod SDK | no load-bearing public low-level OverlayPack parser located in the pinned SDK | no vote on storage/codec semantics | reference only for absence statement |
| CnCNet xna client | `e6e367bbe04c1a0dc1e34a8fed2856ea3ab7e8c4` | client map resources and higher-level code | repository-specific; no low-level parser used here | client | no independent load-bearing storage proof used | ecosystem overlap | not counted |
| ModEnc OverlayPack | permanent revision `https://modenc.renegadeprojects.com/index.php?title=OverlayPack&oldid=21267` | community article | community documentation; not source code | documentation | two sections; zero-based OverlayTypes index; data described as frame; 262144 bytes; `X+512Y`; `0xFF` empty | may synthesize community/tool knowledge | documentation evidence |
| Project Perfect Mod bridge thread | `https://ppmforums.com/topic-45898/info-on-bridge-overlay-types/` | fixed forum topic, 2018 | forum content | community reverse engineering | high/low bridge storage observations and hardcoded ordinal discussion | not official source | documentation evidence |
| PPM DTA Map Assistant thread | `https://ppmforums.com/topic-46069/dta-map-assistant/` | fixed forum topic, 2018 | forum content | community/tool discussion | zero-based registry and `512*Y+X` guidance; explicitly points to MapTool/OpenRA lineage | derivative community evidence | documentation evidence |

Every row has `code_imported: false`.

## 2. What the official editor proves

EA's published source is the strongest evidence in this dossier for editor behavior:

- the two packed sections are distinct;
- fixed 262144-byte ordinary arrays are used;
- blank type storage is `0xFF` and blank data storage is zero;
- saving passes exactly 262144 bytes from each ordinary array to the Format80 packer;
- the editor's internal field-array mapping is transposed relative to the dominant externally named formula.

It does **not** prove:

- original game runtime error handling;
- original runtime decoder exactness;
- registry construction in every game version;
- every hardcoded resource/wall/bridge rule;
- acceptance of short, long, malformed, or extended streams.

Evidence grade remains `ConfirmedByOfficialEditorSource`.

## 3. Shared-lineage warning

Source-count voting is unsafe:

- EA's editor integrates XCC-derived pack code;
- OpenRA's map import and later community tools share Format5/LCW knowledge;
- CNCMaps explicitly contains or acknowledges OpenRA/XCC-derived parts;
- MapTool and WAE share ecosystem components and documentation;
- community articles often summarize behavior learned from these tools.

Agreement among five repositories may represent one or two knowledge lineages, not five independent observations of the original runtime.

## 4. Notable behavioral differences

### Missing sections

- WAE and CNCMaps return without typed overlays if either section is absent.
- MapTool reports errors for each and materializes only if both succeed.
- EA's editor can initialize buffers even around absent/empty text.

These are tool policies, not one confirmed runtime rule.

### Short output

EA's editor preinitializes destination arrays, permitting decoder shortfall to leave default bytes. Strict Core policy rejects length mismatch rather than inheriting this tolerance.

### Coordinate naming

Most implementations expose `X + 512*Y`; EA's editor uses a transposed internal bridge between fielddata and storage. The external runtime formula remains evidence-gated.

### Registry construction

WAE's generic type loader assigns sequential list indices by iterated section keys and ignores explicit numeric-key values when assigning `Index`. That is not adopted as a project rule.

### Extended type array

WAE supports 16-bit type entries for `NewINIFormat >= 5`. ModEnc states RA2 should normally use NewINIFormat 4. The extended profile is isolated as extension/tool behavior until stronger game-specific evidence is supplied.

## 5. License isolation requirements

For GPL or unclear-license code:

- do not copy code, constants arranged as implementation tables, comments, or tests;
- do not translate line by line;
- do not mechanically rewrite control flow into C#;
- do not imitate class/function layout merely because it is convenient;
- derive independent fixtures from format-level statements and synthetic values;
- retain source pins and behavior summaries in research documentation;
- have implementation work use clean-room state-machine requirements, not source-shaped pseudocode.

## 6. Evidence-grade summary

| Conclusion | Grade |
|---|---|
| sections are independent | `ConfirmedByOfficialEditorSource` |
| ordinary storage is two 262144-byte arrays | `ConfirmedByOfficialEditorSource` |
| dominant external index is `X + 512Y` | `ConfirmedByIndependentImplementation` + `CommunityDocumented` |
| `0xFF` means no Overlay in ordinary profile | `ConfirmedByOfficialEditorSource` + implementations |
| data byte is universally a frame | `Unresolved`; community/tool candidate only |
| raw type is zero-based registry ordinal | `CommunityDocumented` + implementation evidence |
| exact original runtime registry gap/duplicate behavior | `Unresolved` |
| absolute-position Format80 Overlay profile | `ConfiguredForProjectPolicy` |
| WAE 16-bit type stream is vanilla YR behavior | not established; extension/tool profile |
| bridge hardcoded ordinal behavior | `CommunityDocumented`, partly editor-observed |

## 7. Sources not found or not used as proof

- No official RA2/YR runtime source was located.
- No load-bearing Chrono Divide public Overlay parser was located in the pinned SDK.
- No independent low-level CnCNet client parser was used.
- No ModdingWiki page specific enough to resolve Overlay storage conflicts was located.
- No ProjectBaseline data was accessed.