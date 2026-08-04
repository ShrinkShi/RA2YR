# Source comparison and license boundary

> **Source notice:** ChatGPT Web public-source research. ProjectBaseline was not read. This is not a Codex artifact. No GPL or unclear-license code was copied, translated, mechanically rewritten, or ported. Every source is reference-only. `code_imported: false`.

## Comparison method

A source is recorded by:

- project and role;
- fixed URL or permanent revision;
- commit, revision, or version;
- relevant file path or documentation page;
- license;
- game/version coverage;
- reader, writer, editor, runtime, client, or documentation category;
- whether the implementation is independent;
- known or probable code/knowledge lineage;
- evidence grade supported;
- `reference_only: true`;
- `code_imported: false`.

Agreement between tools that share XCC, OpenRA, CNCMaps, or community lineage is not counted as multiple independent original-runtime confirmations.

## Pinned source matrix

| Source | Revision | Relevant paths / pages | License | Category | Independence and limits |
|---|---|---|---|---|---|
| EA FinalSun / FinalAlert 2 | `6abf0f557469baea73079c6bf6550709e2e3584e` | `MissionEditor/MapD.cpp`, `Houses.cpp`, `SpecialFlags.cpp`, `MapData.cpp`, related UI/resources | GPL-3.0-or-later | Official editor reader/writer/UI | Official editor evidence only; not game runtime source |
| World-Altering Editor | `b4c9481e9b00fb0a38739049a046f528b6054ce2` | `Models/BasicSection.cs`, `House.cs`, `HouseType.cs`, `Initialization/MapLoader.cs`, `MapWriter.cs` | GPL-3.0-or-later | Independent editor/reimplementation | Strong structured reader/writer evidence; has recovery and normalization behavior |
| OpenRA | `a520984d91eda9de48a62b1d15c1e3bad0d4fb1a` | map importers, player definitions, map metadata adapters where applicable | GPL-3.0-or-later | Reimplementation/importer | Architecture differs substantially; not a stock RA2/YR runtime clone |
| CnCNet XNA client | `e6e367bbe04c1a0dc1e34a8fed2856ea3ab7e8c4` | `GameLobbyBase.cs`, map loader/domain classes, mission/client metadata | GPL-3.0 | Launcher/client/session consumer | Strong evidence that lobby player, color, side, start and team are session state; not stock map runtime source |
| MapTool | `f85f2226905496139f1258b5854fad915f9bbac6` | map readers/models where metadata is exposed | GPL-2.0-or-later | Tool/reimplementation | Useful comparison; community knowledge lineage likely |
| CNCMaps | `afb9c1ec118f5128cbc1f3fb5e35c7dfa0e422fb` | map file-format readers and rendering metadata | Repository default plus imported-code exceptions | Renderer/tool | Some code explicitly derives from other GPL/XCC/OpenRA work; not fully independent |
| Chrono Divide public SDK | fixed public repository revision recorded by implementation work | SDK scenario/player/map definitions where publicly available | Repository-specific | Reimplementation/runtime-like web engine | Useful independent architecture reference; compatibility scope differs |
| XCC / OmniBlade lineage | fixed mirror/release references | INI/map tooling and historical conventions | GPL or source-specific | Tool lineage | Historical evidence; derivatives must not be double-counted |
| ModEnc | permanent `oldid` revisions where available | Basic, Map, Houses, Countries, SpecialFlags, MultiplayerDialogSettings, map extensions | Community documentation | Documentation | Broad cross-game coverage; field claims require version filtering |
| Project Perfect Mod | fixed topic URLs | House, alliance, multiplayer and format discussions | Community forum | Documentation/discussion | Useful for conflicts and observations, not runtime proof |
| RA2 DIY | permanently locatable public tutorials/lexicons | RA2/YR mapping and INI field descriptions | Community documentation | Documentation | Chinese community reference; provenance and version must be stated |
| Ares documentation | fixed documentation release/page | House, scenario, multiplayer and extension behavior | Extension documentation | Extension reference | Ares behavior must never be described as vanilla |
| Phobos documentation | versioned build/page | scenario and House/client/runtime extensions | Extension documentation | Extension reference | Explicit profile only; evolving documentation |

All entries use `reference_only: true` and `code_imported: false`.

## Official editor source

### What it proves

The EA source confirms official editor behavior such as:

- the fields exposed in House, map, and SpecialFlags dialogs;
- editor terminology such as visible/usable `LocalSize`;
- `Basic.Player` human-House selection UI;
- House preparation and creation defaults;
- editor map-size constraints and editing workflow;
- direct editing of Theater, Allies, Color, Credits, IQ, TechLevel, PlayerControl, and related fields;
- TS versus RA2-mode UI differences.

Evidence grade: `ConfirmedByOfficialEditorSource`.

### What it does not prove

It does not prove:

- original executable load order;
- exact original runtime defaults;
- acceptance/rejection behavior for malformed metadata;
- alliance symmetry rules;
- multiplayer session precedence;
- player/controller allocation;
- SpecialFlags execution;
- Digest verification;
- savegame behavior.

The editor's generated defaults are not parser defaults.

## WAE source

### Strengths

WAE provides explicit models and current reader/writer behavior for:

- the mixed `[Basic]` field set;
- `Size`, `LocalSize`, and Theater;
- House instances;
- HouseType/Country definitions;
- Allies lists;
- base nodes;
- map-local Countries;
- `[Houses]` and `[Countries]` writer structure;
- `MaxPlayer` writer derivation from Waypoints under one condition.

Evidence grade: `ConfirmedByIndependentImplementation` for WAE behavior.

### Recovery and normalization risks

WAE may:

- default a missing/invalid Country to the first standard Country;
- skip missing allied Houses;
- normalize `Size` to `0,0,w,h`;
- generate or rewrite list ordering;
- derive MaxPlayer from low-numbered Waypoints;
- normalize booleans and numbers;
- write editor defaults.

These actions must be recorded as tool behavior rather than adopted as lossless Core semantics.

## CnCNet client source

### What it proves

The client clearly separates session/lobby data from map metadata:

- human and AI player rows;
- local player identity;
- side/Country selections;
- colors;
- starts;
- lobby team numbers;
- ready/network state;
- checkboxes/dropdowns for mode options;
- random seed and unique game identity;
- client mode/map registration;
- optional removal of authored starting locations before launch.

Evidence grade: `ConfirmedByIndependentImplementation` for client behavior.

### Boundary

Client behavior cannot by itself establish the original RA2/YR map-file runtime contract. Many client values are launch/session overrides.

## Community documentation

### Useful areas

ModEnc and fixed community material provide strong leads for:

- `.MAP`, `.MPR`, `.YRM`, `.MMX`, and `.YRO` roles;
- `[Basic]` field names and version applicability;
- `NewINIFormat` conventions;
- `[SpecialFlags]` fields;
- `[MultiplayerDialogSettings]` defaults;
- `Allies` syntax;
- low-numbered Waypoint start conventions;
- House/Country/Side terminology;
- client and extension behavior.

### Limits

Community pages can combine:

- Tiberian Sun;
- Firestorm;
- Red Alert 2;
- Yuri's Revenge;
- editor conventions;
- Ares/Phobos extensions;
- client behavior;
- observations from sample maps.

Every claim must retain version and evidence metadata. Evidence grade: `CommunityDocumented` unless independently confirmed.

## Major source conflicts

## 1. `Size` field 0 and 1

- WAE reads only fields 2/3 for map dimensions and writes fields 0/1 as zero.
- The raw format is consistently four fields.
- Public evidence is insufficient to prove that nonzero origins are invalid or ignored by original runtime.

Project result: retain all four raw values; interpretation policy explicit; unresolved runtime semantics.

## 2. `LocalSize` meaning

- Official editor labels it visible/usable map size and recalculates its map rectangle after edits.
- WAE models it as `x,y,width,height`.
- Community/client usage often treats it as playable/visible bounds.
- Exact runtime effects—camera, buildability, start validation, AI boundary—remain unresolved.

Project result: rectangle descriptor plus consumer-role candidates, not a hardcoded gameplay area.

## 3. Map dimension limits

- Official editor enforces its own authoring constraints.
- WAE has constants and pre-check limits.
- clients and extensions may support different maxima.

Project result: read limits and format/runtime limits are separate; no editor limit is promoted automatically.

## 4. House versus Country registry

- RA2/YR WAE writer uses `[Countries]` for HouseType/Country definitions and `[Houses]` for instances.
- TS-oriented formats use House/ActsLike/Side differently.
- official editor has separate Rules Houses, map Houses, and RA2 Country creation flows.
- global Rules and map-local sections can overlap.

Project result: explicit identity domains and profile-specific section roles.

## 5. List ordinal significance

- editors often write numeric lists in current memory order.
- gaps and custom IDs exist in INI culture.
- public evidence does not establish that list ordinal is a runtime player slot or stable House identity.

Project result: preserve raw key, numeric candidate, source order, gaps, and duplicates; do not compress or renumber.

## 6. Missing Country recovery

- WAE can default to the first standard Country.
- official editor can synthesize Country/House sections when authoring.
- no official runtime source establishes equivalent behavior.

Project result: dangling reference, no auto-fallback.

## 7. Alliance direction and symmetry

- map/editor formats expose comma-separated House IDs.
- official editor commonly creates self-alliance.
- WAE stores an Allies list and skips missing targets.
- community claims vary on one-way behavior and `FixedAlliance` interaction.

Project result: directed raw graph; symmetry is analysis, not repair.

## 8. Player-control precedence

Potential inputs conflict:

- `[Basic] Player`;
- House `PlayerControl`;
- possible `Human` fields;
- campaign call context;
- client/lobby player rows;
- network peer mapping.

Project result: authored candidate and session assignment remain separate.

## 9. Multiplayer settings provenance

- `[MultiplayerDialogSettings]` is strongly documented as Rules dialog defaults.
- client game options and game-mode files can override or duplicate names.
- maps may carry extensions or copied settings.

Project result: every setting includes source kind and layer; no direct map-to-runtime assumption.

## 10. Start Waypoints

- low Waypoint IDs are a strong multiplayer start convention.
- campaigns use Waypoints for many unrelated purposes.
- client can remove or replace starts.
- player counts and start slots can conflict.

Project result: start-slot interpretation requires an explicit consumer profile.

## 11. Scenario classification

- file extension, Basic fields, campaign registration, launcher database, and invocation context can disagree.
- `.MAP` is not inherently campaign or multiplayer.
- multiplayer/skirmish distinction may be session context.

Project result: evidence collection plus optional policy resolution.

## 12. SpecialFlags applicability

- official editor exposes a broad TS/RA2 shared dialog and hides/renames fields by mode.
- community documentation reports context-specific behavior.
- Rules and lobby settings may overlap.

Project result: raw flags plus profile applicability; no behavior implementation.

## 13. Digest

- public files contain Digest metadata;
- editor/client code references map integrity and checksums in multiple unrelated contexts;
- exact algorithm, canonical byte coverage, and runtime enforcement are not established here.

Project result: opaque metadata, not a trusted signature.

## 14. Lighting

- map lighting fields are distinct from House Color and Theater identity;
- consumers can apply environment-specific behavior;
- no rendering or light-generation conclusion is made in this dossier.

## Evidence-grade application

| Claim | Maximum grade currently supported |
|---|---|
| Official editor exposes LocalSize and Theater UI | `ConfirmedByOfficialEditorSource` |
| Official editor writes/repairs House defaults | `ConfirmedByOfficialEditorSource` |
| WAE reads LocalSize as x,y,w,h | `ConfirmedByIndependentImplementation` |
| WAE writes Size as 0,0,w,h | `ConfirmedByIndependentImplementation` |
| WAE separates RA2 Countries and Houses | `ConfirmedByIndependentImplementation` |
| WAE writes Allies as House-name list | `ConfirmedByIndependentImplementation` |
| CnCNet separates lobby player/side/color/start/team | `ConfirmedByIndependentImplementation` |
| Exact original runtime alliance symmetry | `Unresolved` |
| Exact original runtime Size origin semantics | `Unresolved` |
| Exact Digest algorithm and enforcement | `Unresolved` |
| Future ProjectBaseline aggregate observation | `ObservedByFutureProjectBaselineAudit` |
| Project fail-closed/no-repair defaults | `ConfiguredForProjectPolicy` |

## Code-use boundary

No source control flow, switch table, parser routine, class layout, or algorithm is translated into implementation pseudocode. The documents compare externally observable structures and architectural boundaries only.

No GPL or unclear-license text is imported into project code or formal implementation. `code_imported: false`.
