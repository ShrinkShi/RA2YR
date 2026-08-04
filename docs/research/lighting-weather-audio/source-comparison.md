# Source Comparison and License Boundary

> **Source notice:** ChatGPT Web public-source research. ProjectBaseline was not read. Not a Codex artifact. No GPL or unclear-license code was copied, translated, mechanically rewritten, or ported. `code_imported: false`.

## Source-use policy

All external implementations are reference-only. This dossier records observations and derives interface boundaries. It does not reproduce source control flow, formulas as implementation pseudocode, switch tables, or code structure.

Every source record includes:

- project;
- permanent URL or pinned commit/revision;
- file/path;
- license;
- category;
- version scope;
- independence and lineage notes;
- evidence role;
- `code_imported: false`.

## Evidence grades

```text
ConfirmedByOfficialRuntimeSource
ConfirmedByOfficialEditorSource
ConfirmedByIndependentImplementation
CommunityDocumented
ObservedByFutureProjectBaselineAudit
ConfiguredForProjectPolicy
Unresolved
```

No full original RA2/YR runtime source was identified. The highest source grade used for field names and editor authoring behavior is `ConfirmedByOfficialEditorSource`.

## Load-bearing source records

### Electronic Arts FinalSun / FinalAlert 2 Mission Editor

- Project: `electronicarts/CNC_TS_and_RA2_Mission_Editor`
- Commit: `6abf0f557469baea73079c6bf6550709e2e3584e`
- Repository: `https://github.com/electronicarts/CNC_TS_and_RA2_Mission_Editor`
- License: GPL-3.0-or-later, as stated by repository license/header text.
- Category: official editor / writer / authoring tools.
- Runtime category: **not** original game runtime source.
- Independence: official editor implementation; can share assumptions and historical code/data with Westwood tooling, but its UI behavior is not runtime behavior.
- `reference-only: true`
- `code_imported: false`

Key files:

| Path | Evidence role |
|---|---|
| `MissionEditor/Lighting.cpp` | normal and Ion field names; raw editor read/write; RA2 Weather Storm label |
| `MissionEditor/SpecialFlags.cpp` | IonStorms, FogOfWar, Meteorites and other flags; RA2 label/hide behavior |
| `MissionEditor/data/FinalAlert2/Scripts/Create ... Lighting.fscript` | static authoring presets and numeric spelling examples |
| `MissionEditor/data/FinalSun/Scripts/Day-Night Loop.fscript` | editor-generated Trigger chain plus initial Lighting values |
| `MissionEditor/data/FinalAlert2/FAData.ini` and `FinalSun/FSData.ini` | editor action/event metadata candidates |

Evidence grades:

- exact editor field names and raw UI behavior: `ConfirmedByOfficialEditorSource`;
- editor presets and generated Trigger records: `ConfirmedByOfficialEditorSource`;
- stock runtime formula or automatic day/night system: `Unresolved`.

### World-Altering Editor

- Project: `CnCNet/WorldAlteringEditor`
- Commit: `b4c9481e9b00fb0a38739049a046f528b6054ce2`
- Repository: `https://github.com/CnCNet/WorldAlteringEditor`
- License: GPL-3.0-or-later (`COPYING`).
- Category: independent editor / reader / writer / preview renderer.
- Version scope: TS and RA2/YR editor profiles, plus extension-aware fields.
- Independence: independent implementation, but influenced by public community knowledge and other tools; not runtime source.
- `reference-only: true`
- `code_imported: false`

Key files:

| Path | Evidence role |
|---|---|
| `src/TSMapEditor/Models/Lighting.cs` | normal/Ion/Dominator fields; WAE preview composition and cap |
| `src/TSMapEditor/UI/Windows/LightingSettingsWindow.cs` | editor UI formatting and preview profile selection |
| `src/TSMapEditor/Models/BuildingType.cs` | LightVisibility, LightIntensity, tint fields, HasSpotlight |
| `src/TSMapEditor/Config/Default/Actions.ini` | declarative Trigger action names and parameter domains |
| `src/TSMapEditor/Initialization/MapLoader.cs` | section-reading behavior and tool recovery candidates |
| `src/TSMapEditor/Initialization/MapWriter.cs` | canonical writer behavior |

Evidence grade: `ConfirmedByIndependentImplementation`.

Important limitation: WAE's formulas, fallbacks, three-decimal UI formatting, and preview modes describe WAE. They are not promoted to stock runtime.

### OpenRA

- Project: `OpenRA/OpenRA`
- Commit: `a520984d91eda9de48a62b1d15c1e3bad0d4fb1a`
- Repository: `https://github.com/OpenRA/OpenRA`
- License: GPL-3.0-or-later.
- Category: independent engine / importer / renderer.
- Version scope: OpenRA's import of Westwood second-generation maps into OpenRA data.
- Independence: independent engine; importer adapts source fields into a different runtime model.
- `reference-only: true`
- `code_imported: false`

Key file:

`OpenRA.Mods.Cnc/UtilityCommands/ImportGen2MapCommand.cs`

Observed importer behavior:

- maps Red/Green/Blue/Ambient/Level to OpenRA tint/intensity/height-step fields;
- combines Ground into Ambient for the target representation;
- imports configured lamp actor type fields into OpenRA TerrainLightSource data;
- repairs comma decimal notation for local light fields;
- ignores or reports fields not represented by its target model.

Evidence grade: `ConfirmedByIndependentImplementation`.

Important limitation: conversion into OpenRA traits is not a stock RA2/YR formula.

### CnCNet XNA client

- Project: `CnCNet/xna-cncnet-client`
- Commit: `e6e367bbe04c1a0dc1e34a8fed2856ea3ab7e8c4`
- Repository: `https://github.com/CnCNet/xna-cncnet-client`
- License: GPL-3.0.
- Category: launcher / client / lobby / map catalog / UI.
- Version scope: CnCNet client ecosystem and configured games.
- Independence: client behavior; not stock map runtime source.
- `reference-only: true`
- `code_imported: false`

Evidence role:

- separates client/lobby/game-mode settings from authored map data;
- provides context for playlist, preview, Shroud/Fog options, player/session settings, and client-only metadata;
- demonstrates that UI behavior can override or supplement map inputs.

Client playlist, map filters, and lobby options are never called stock scenario format facts.

### Ares documentation

- Project: Ares documentation.
- Versioned site: `https://ares-developers.github.io/Ares-docs/`
- Referenced pages include:
  - `/new/superweapons/lighting.html`;
  - `/new/superweapons/types/lightningstorm.html`;
  - `/bugfixes/type2/movingalphalights.html`;
  - audio-related extension pages.
- License: documentation/project license as published by Ares; reference-only. Exact page-source commit should be pinned before implementation adoption.
- Category: Yuri's Revenge extension documentation.
- Version scope: Ares, not vanilla.
- Independence: extension behavior, often documenting changes/restorations relative to YR.
- `reference-only: true`
- `code_imported: false`

Evidence role:

- proves that extension weather/superweapon systems explicitly separate lighting, sound, animations, damage, radar outage, and timing;
- documents extension lighting and moving alpha-light capabilities;
- helps prevent collapsed Core models.

Evidence grade: `CommunityDocumented` or extension-documentation grade. Never `ConfirmedByOfficialRuntimeSource`.

### Phobos documentation

- Project: Phobos.
- Documentation: public versioned Phobos documentation and repository pages.
- Exact revision: must be pinned for every field used by a future implementation.
- License: project/documentation license to be recorded with the pinned source.
- Category: Yuri's Revenge extension documentation/implementation.
- Version scope: Phobos only.
- Independence: extension lineage can overlap Ares/Community knowledge.
- `reference-only: true`
- `code_imported: false`

This dossier does not rely on an unpinned Phobos field for a vanilla conclusion. Unknown fields remain extension candidates.

### ModEnc

- Project: ModEnc, community encyclopedia.
- Site: `https://modenc.renegadeprojects.com/`
- Example pinned revision: `IonAmbient`, oldid `30250`.
- License: site/content terms must be observed; reference-only.
- Category: community documentation.
- Version scope: TS/RA2/YR and extensions depending on page.
- Independence: community synthesis, potentially derived from tool/runtime experimentation and shared knowledge.
- `reference-only: true`
- `code_imported: false`

Evidence grade: `CommunityDocumented`.

ModEnc descriptions are useful for field applicability and expected qualitative effects. They are not original runtime source and do not by themselves establish exact math.

## Secondary candidate sources

### CNCMaps

- Pinned candidate commit used by adjacent research: `afb9c1ec118f5128cbc1f3fb5e35c7dfa0e422fb`.
- Category: renderer/tool.
- License: repository is mixed and contains imported-code considerations; each path requires individual license review.
- Use in this dossier: comparison candidate only, not load-bearing.
- `reference-only: true`
- `code_imported: false`.

### MapTool

- Pinned candidate commit used by adjacent research: `f85f2226905496139f1258b5854fad915f9bbac6`.
- Category: reader/writer/tool.
- License: GPL-2.0-or-later as recorded by adjacent research; verify path-level headers before use.
- Use: possible reader/writer comparison; not load-bearing here.
- `reference-only: true`
- `code_imported: false`.

### Chrono Divide SDK

- Category: independent web runtime/SDK.
- Exact revision and license: must be pinned before a conclusion relies on it.
- Use: future comparison candidate for lighting, audio, visibility, and Trigger behavior.
- `reference-only: true`
- `code_imported: false`.

### XCC / OmniBlade lineage

- Category: legacy tools and reverse-engineered implementations.
- Use: source-comparison candidate.
- Independence warning: descendants or ports must not be counted as separate confirmation.
- `reference-only: true`
- `code_imported: false`.

### Project Perfect Mod and RA2 DIY

- Category: fixed community posts/tutorials.
- Requirement: use permanent thread/post links where possible and preserve post date/version context.
- Use: community behavior reports, not runtime source.
- `reference-only: true`
- `code_imported: false`.

## Source conflict summary

### Global Lighting composition

- Official editor: exposes raw values, no runtime formula.
- WAE: Ambient-multiplied RGB preview with cap.
- OpenRA: converts to target tint/intensity/height step and merges Ground.
- Community docs: qualitative descriptions.

Result: explicit composition profiles; stock formula `Unresolved`.

### Numeric parsing

- Official editor dialog: raw text pass-through.
- WAE: `double` model and formatted editor UI.
- OpenRA: target importer `float`; comma repair for local lamps.

Result: raw text first, explicit numeric policy, no locale repair by default.

### Day/night

- Official editor: static presets and trigger-generating scripts.
- No full original runtime source proving an autonomous map clock.

Result: static profile plus declarative dynamic candidates.

### Weather

- Official editor: IonStorms/Weather Storm label and alternate fields.
- Ares: extension superweapon splits lighting/audio/visual/simulation settings.

Result: capability/state/effect separation.

### Local lights

- WAE: type fields.
- OpenRA: converts configured lamp types to target engine light sources.
- Ares: extension alpha-light/moving behavior.

Result: type/placement/state/rendering layers remain distinct.

### Fog/Shroud

- Official editor: raw FogOfWar key, RA2 Shroud label.
- Editors/clients expose reveal and session options.

Result: authored metadata is not current visibility state or visual fog.

## Shared-lineage controls

Do not count the following as independent confirmations without provenance review:

- a tool porting another tool's parser;
- an importer copied from XCC or CNCMaps;
- wiki text derived from one implementation;
- editor action definitions copied from FinalAlert data;
- extension docs describing restored behavior based on shared community research.

Every conclusion records both source count and independence assessment.

## Code-use prohibition

Forbidden:

- copying GPL source;
- translating source line by line;
- mechanically rewriting source;
- reproducing switch/control flow as C# pseudocode;
- importing formulas without an independently specified interface and license review;
- using screenshots or generated outputs as an algorithm oracle;
- calling source agreement official runtime proof.

Allowed:

- field inventories;
- behavioral observations;
- conflict tables;
- evidence-graded semantic candidates;
- independently designed Core models, policies, diagnostics, and tests.

## Future source-pinning checklist

Before any implementation PR:

1. pin exact commit/revision and path;
2. record license and path-level header;
3. classify editor/reader/writer/renderer/client/runtime/extension;
4. document lineage and independence;
5. extract only behavioral requirements;
6. design independently;
7. record `code_imported: false` or perform formal license process if policy changes;
8. keep compatibility status unchanged until implementation plus audit evidence exists.
