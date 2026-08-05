# Source comparison and license boundary

> **Research provenance:** This document was produced by ChatGPT Web from public sources. It did not read ProjectBaseline, is not a local Codex Agent artifact, and imports no code. GPL or unclear-license sources are reference-only; no source was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

## 1. Source matrix

| Source | Pin / permanent location | Relevant paths | License | Category | Load-bearing observations | Lineage / caveat | Use |
|---|---|---|---|---|---|---|---|
| EA FinalSun / FinalAlert 2 | `6abf0f557469baea73079c6bf6550709e2e3584e` | `MissionEditor/MapData.cpp`, `MapData.h`, `MissionEditorPackLib/*` | GPL-3.0-or-later in published source headers | official editor | separate sections; explicit 262144 outputs; type prefill `0xFF`; data prefill zero; encode 262144; internal transposed storage mapping | editor integrates XCC-derived packing lineage; not game runtime | reference-only |
| OpenRA | `a520984d91eda9de48a62b1d15c1e3bad0d4fb1a` | `OpenRA.Mods.Cnc/UtilityCommands/ImportGen2MapCommand.cs`, common LCW/Format5 helpers | GPL-3.0-or-later | reimplementation/importer | separate section decode; `1 << 18` arrays; `rawX + 512*rawY`; `0xFF` skip; type-specific resource/actor handling | community implementation with XCC knowledge lineage | reference-only |
| World-Altering Editor | `b4c9481e9b00fb0a38739049a046f528b6054ce2` | `MapLoader.cs`, `MapWriter.cs`, `Rules.cs`, `OverlayType.cs`, `Overlay.cs`, connected/bridge mutations | GPL-3.0-or-later | editor/reimplementation | ordinary 512² byte arrays; extended 16-bit type profile for `NewINIFormat >= 5`; `Y*512+X`; list-index registry behavior; FrameIndex model; connected and bridge editor logic | shares CNCMaps/OpenRA ecosystem components and knowledge | reference-only |
| XCC Utilities mirror | `6f91bf8b00d3acabb1be765118a37c0cb74e85ec` | `misc/map_ts_encoder.cpp`, `misc/cc_structures.h`, compression helpers | GPL-3.0 in inspected mirror headers; historical SourceForge lineage may differ by release | tool/reference implementation | fixed map-pack structures; `x + 512*y` family conventions; Format80/Format5 lineage | ancestor for several later tools and EA editor components | reference-only |
| XCC SourceForge | Utilities 1.46 release, 2008-05-02; exact SVN mapping unresolved | release archive / project history | GPL-2.0 lineage reported by project release | historical tool | provenance anchor for XCC behavior | not assumed byte-identical to GitHub mirror | reference-only |
| CNCMaps | `afb9c1ec118f5128cbc1f3fb5e35c7dfa0e422fb` | `CNCMaps.FileFormats/Map/MapFile.cs`, `MapObjects.cs`, Format5 helpers | repository MIT default with explicit OpenRA/XCC GPL exceptions | renderer/reimplementation | separate sections; fixed `1 << 18`; `rawX + 512*rawY`; `0xFF` empty; generic Overlay ID/value object | explicitly acknowledges OpenRA/XCC code use | reference-only |
| MapTool | `f85f2226905496139f1258b5854fad915f9bbac6` | `MapTool.Logic/MapFile.cs`, `MapTile.cs`, pack helpers | GPL-2.0-or-later | map editing tool | fixed `1 << 18`; index-to-coordinate `x=i%512`, `y=i/512`; separate FrameIndex data; write both arrays | uses or acknowledges OpenRA ecosystem pack logic | reference-only |
| Chrono Divide public SDK | `5943c4ae6c19897929d348a417d6d2f1481b75fd` | public SDK tree | public repository license not used as parser evidence | mod SDK | no load-bearing public low-level OverlayPack parser located in the pinned SDK | no vote on storage/codec semantics | reference only for absence statement |
| CnCNet xna client | `e6e367bbe04c1a0dc1e34a8fed2856ea3ab7e8c4` | client map resources and higher-level code | repository-specific; no low-level parser used here | client | no load-bearing storage proof used | ecosystem overlap | not counted |
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

```text
EvidenceGrade: ConfirmedByOfficialToolSource
Source: EA FinalSun / FinalAlert 2
AuditStatus: NotRun
```

This grade confirms official-editor behavior. It does **not** prove:

- original game runtime error handling;
- original runtime decoder exactness;
- registry construction in every game version;
- every hardcoded resource/wall/bridge rule;
- acceptance of short, long, malformed, or extended streams.

No claim in this dossier is graded `ConfirmedByOriginalRuntimeSource`.

## 3. Shared-lineage warning

Source-count voting is unsafe:

- EA's editor integrates XCC-derived pack code;
- OpenRA's map import and later community tools share Format5/LCW knowledge;
- CNCMaps explicitly contains or acknowledges OpenRA/XCC-derived parts;
- MapTool and WAE share ecosystem components and documentation;
- community articles often summarize behavior learned from these tools.

Agreement among five repositories may represent one or two knowledge lineages, not five separate discoveries of original-runtime behavior. This dossier therefore does not use `ConfirmedByMultipleIndependentImplementations` for the Overlay claims reviewed here.

## 4. Notable behavioral differences

### Missing sections

- WAE and CNCMaps return without typed overlays if either section is absent.
- MapTool reports errors for each and materializes only if both succeed.
- EA's editor can initialize buffers even around absent/empty text.

These are `ImplementationSpecificBehavior` for the named tools, not one confirmed runtime rule. Project refusal to synthesize a partner stream is `DefensiveDesign`.

### Short output

EA's editor preinitializes destination arrays, permitting decoder shortfall to leave default bytes. That is `ConfirmedByOfficialToolSource` for editor behavior. Strict Core length rejection is `DefensiveDesign`, not a runtime claim.

### Coordinate naming

Several public implementations expose `X + 512*Y`; EA's editor uses a transposed internal bridge between field data and storage. The external/runtime formula remains `Underconfirmed`, and the claim of one already-settled universal axis contract is `ConflictingSources`.

### Registry construction

WAE's generic type loader assigns sequential list indices by iterated section keys and ignores explicit numeric-key values when assigning `Index`. This is `ImplementationSpecificBehavior` and is not adopted as a project rule.

### Extended type array

WAE supports 16-bit type entries for `NewINIFormat >= 5`. ModEnc states RA2 should normally use NewINIFormat 4. The 524288-byte type array is `ImplementationSpecificBehavior` for an extension/tool profile, not an ordinary vanilla candidate.

## 5. License isolation requirements

For GPL or unclear-license code:

- do not copy code, constants arranged as implementation tables, comments, or tests;
- do not translate line by line;
- do not mechanically rewrite control flow into C#;
- do not imitate class/function layout merely because it is convenient;
- derive clean-room fixtures from format-level statements and synthetic values;
- retain source pins and behavior summaries in research documentation;
- have implementation work use clean-room state-machine requirements, not source-shaped pseudocode.

## 6. Normalized evidence-grade summary

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert/FinalSun reads the two sections separately | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Official-editor behavior only. | Preserve separate section/stream provenance. | `NotRun` |
| Several public tools also use separate type/data streams | `Underconfirmed` | OpenRA, WAE, CNCMaps, MapTool | Cross-tool convergence exists, but lineages are not proven independent. | Do not synthesize one from the other. | `NotRun` |
| Ordinary storage uses two 262144-byte arrays in FinalAlert/FinalSun | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Does not prove runtime strictness. | Ordinary profile expects exact length. | `NotRun` |
| Several public tools use the same 262144-byte ordinary length | `Underconfirmed` | OpenRA, WAE, CNCMaps, MapTool, ModEnc | Shared ancestry and runtime applicability remain open. | Keep as explicit ordinary-profile candidate. | `NotRun` |
| WAE supports a 524288-byte extended OverlayPack type stream | `ImplementationSpecificBehavior` | World-Altering Editor | The behavior is tied to `NewINIFormat >= 5`; it is an extension profile and not an ordinary vanilla candidate. | Require explicit extension profile. | `NotRun` |
| Dominant external index candidate is `X + 512Y` | `Underconfirmed` | OpenRA, WAE, CNCMaps, MapTool, ModEnc | Formula convergence does not prove independent lineage or original-runtime authority. | Explicit coordinate profile only. | `NotRun` |
| One unique runtime index/axis contract is already established | `ConflictingSources` | Row-major public candidate versus EA official-editor transposed internal mapping | No original-runtime source selects a unique interpretation. | Preserve both named views and never trial-select. | `NotRun` |
| FinalAlert/FinalSun uses `0xFF` for no Overlay | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Official-editor behavior only. | Preserve raw and classify through the ordinary profile. | `NotRun` |
| `0xFF` is a stable ordinary-profile tool/community sentinel | `ConfirmedCommunityConvention` | ModEnc and public tools | Does not establish universal runtime exclusivity. | Do not treat unknown values as empty. | `NotRun` |
| `OverlayDataRaw` is universally only a frame | `ConflictingSources` | Generic frame tools versus resource/wall/bridge behavior | Type-specific semantics prevent a universal frame-only claim. | Require semantic profiles and preserve raw. | `NotRun` |
| FrameIndex is a stable community/tool candidate | `ConfirmedCommunityConvention` | ModEnc, WAE, CNCMaps, MapTool | Convention only; runtime universality remains open. | Named opt-in profile. | `NotRun` |
| Raw type commonly represents a zero-based registry ordinal | `ConfirmedCommunityConvention` | ModEnc and public tools | Exact runtime gap/duplicate/case behavior remains unresolved. | Build a provenance-preserving ordinal registry. | `NotRun` |
| Exact original-runtime registry gap/duplicate behavior | `Unresolved` | No original-runtime source located | Tool policies differ. | Fail closed on ambiguity. | `NotRun` |
| Absolute-position Overlay Format80 profile is the current project contract | `DefensiveDesign` | M3-R2/M3-C1 project contract | Explicit profile does not promote Overlay compatibility. | No trial decode or plausibility selection. | `NotRun` |
| WAE connected-wall and bridge mutations | `ImplementationSpecificBehavior` | World-Altering Editor | Editor behavior only. | Keep in named semantic adapters. | `NotRun` |
| Community high/low bridge model | `ConfirmedCommunityConvention` | Fixed PPM discussion | Convention/candidate only; stock runtime applicability remains underconfirmed. | Preserve bridge layers separately. | `NotRun` |
| Unknown values, exact lengths, stable ordinals, and no repair | `DefensiveDesign` | Project policy | Preservation and fail-closed requirements, not external evidence. | Apply through explicit policies. | `NotRun` |
| Availability of ProjectBaseline aggregate findings | `Unresolved` | No audit executed | ProjectBaseline was not read. | `FutureEvidenceSource: ProjectBaselineAggregateAudit`. | `NotRun` |

## 7. Sources not found or not used as proof

- No official RA2/YR runtime source was located.
- No load-bearing Chrono Divide public Overlay parser was located in the pinned SDK.
- No load-bearing low-level CnCNet client parser was used.
- No ModdingWiki page specific enough to resolve Overlay storage conflicts was located.
- No ProjectBaseline data was accessed.

```text
AuditStatus: NotRun
FutureEvidenceSource: ProjectBaselineAggregateAudit
```
