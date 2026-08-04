# M3-R9 — Scenario metadata, House initialization, alliances, and game-mode research

> **Source and authorship notice**
>
> This document was produced by **ChatGPT Web** from public sources. It did **not** read the local `ProjectBaseline`, is **not** a local Codex Agent artifact, and does not copy, translate, mechanically rewrite, or port GPL or unclear-license source code. All source code was reference-only. `code_imported: false`.

## Status

Research only. No parser, runtime, Unity adapter, player system, alliance system, network session, campaign progression, economy, rendering, or compatibility promotion is implemented here.

## Scope

This dossier studies the declarative input boundaries around:

- `[Basic]`;
- `[Map]`;
- `[SpecialFlags]`;
- `[MultiplayerDialogSettings]`;
- `[Houses]`;
- `[Countries]`;
- per-House and per-Country sections;
- House/Country/Side/player-slot identity;
- authored starting state;
- directed alliance records;
- multiplayer start-slot and Waypoint relationships;
- campaign, skirmish, multiplayer, cooperative, tutorial, and client-defined mode evidence;
- Digest and Lighting reference boundaries;
- map-local Rules composition boundaries.

Related sections such as `[Waypoints]`, `[Triggers]`, `[Tags]`, `[VariableNames]`, `[Lighting]`, `[Digest]`, `[Preview]`, and client/editor-private metadata are considered only where they provide references or classification evidence.

## Frozen layer boundary

```text
lossless map INI
→ raw scenario metadata sections
→ explicit metadata layout profiles
→ map geometry and theater descriptor
→ logical House/Country identity graph
→ starting-state descriptors
→ alliance and player-slot graph
→ game-mode initialization descriptor
→ future simulation/session adapters
```

The Core boundary must not:

- load TMP, SHP, VXL, HVA, palettes, or theater assets;
- create a runtime player or House object;
- assign a local network peer;
- mutate diplomacy;
- select a random start;
- generate starting units or buildings;
- apply lobby overrides;
- start a campaign mission or multiplayer session;
- create `UnityEngine` objects.

## Leading findings

### `[Basic]`

`[Basic]` is not one coherent runtime object. Public implementations expose a mixture of:

- display metadata such as `Name` and author/client fields;
- campaign media and progression references;
- `Player` House selection candidates;
- multiplayer player-count and mode hints;
- map-format markers such as `NewINIFormat`;
- carry-over and mission-state candidates;
- editor/client extensions;
- fields inherited from earlier games that may be obsolete in RA2/YR.

WAE's model includes `Name`, `Author`, `Player`, `Percent`, `GameMode`, `GameModes`, `HomeCell`, `AltHomeCell`, `Theme`, `InitTime`, `Official`, `EndOfGame`, `FreeRadar`, `MaxPlayer`, `MinPlayer`, `SkipScore`, crate flags, carry-over fields, `NewINIFormat`, next-scenario fields, `RequiredAddOn`, `MultiplayerOnly`, growth flags, and `IgnoreGlobalAITriggers`. This is implementation coverage, not proof that all properties share one stock RA2/YR runtime profile.

### `[Map]`

The strongest common candidate is:

```text
Size      = originX, originY, width, height
LocalSize = originX, originY, width, height
```

Evidence is uneven:

- WAE reads `Size` width/height from fields 2 and 3 and ignores its first two fields in its main model;
- WAE writes `Size=0,0,width,height`;
- WAE preserves all four `LocalSize` fields;
- the official editor labels `LocalSize` as the visible/usable map area and permits explicit edits;
- the official editor's map-resize UI has its own width/height constraints, which are editor limits rather than automatically runtime format limits.

Core therefore retains all four raw tokens for both rectangles and performs interpretation only under an explicit profile.

### Theater

`[Map] Theater` is a logical identity token. The leading stock profiles are:

- Temperate;
- Snow;
- Urban;
- NewUrban;
- Desert;
- Lunar.

A theater binding result may identify control INI, TMP extension, ISO palette role, unit palette role, and other resource candidates, but this research does not load them. Unknown theater tokens remain unresolved and do not fall back to Temperate.

### House, Country, Side, and player slot

The following identities are separate:

```text
House instance
Country / HouseType definition
Side
player slot
local controller assignment
network peer
campaign-authored player House
Neutral / Special / civilian identity
```

WAE's RA2/YR writer uses `[Countries]` for map-local HouseType/Country definitions and `[Houses]` for House instances. WAE also documents that multiple House instances can use one HouseType in YR. Neither list enumeration nor section order is sufficient to collapse these identity domains.

### House properties

Public editor models expose candidates including:

- `IQ`;
- `Edge`;
- `Color`;
- `Allies`;
- `Credits`;
- `Country` or TS `ActsLike`;
- `TechLevel`;
- `PercentBuilt`;
- `PlayerControl`;
- base-node records;
- map/editor statistics and extension fields.

They must be separated into raw authored properties, identity references, economy candidates, placement templates, editor statistics, and future runtime state.

### Alliances

`Allies=` is best represented first as a raw ordered list of logical House references. The default graph is directed:

```text
HouseA → HouseB
```

Self-reference, duplicates, missing targets, case collisions, and asymmetric pairs are preserved. No reverse edge is synthesized. `FixedAlliance` is a separate SpecialFlags policy candidate and does not turn the raw authored graph into a symmetric graph during parsing.

### Player assignment

`[Basic] Player`, House `PlayerControl`, client lobby player rows, AI rows, observer state, and network peer ownership are different layers. A map may provide an authored campaign-player candidate, but a multiplayer session must assign the current machine and network peers outside the map parser.

### Multiplayer settings

`[MultiplayerDialogSettings]` has strong documentation as a Rules/client dialog-default section for RA2/YR. Candidate flags include money, bases, crates, shroud, short game, superweapons, allied building, MCV redeploy, bridge destruction, alliances, AI defaults, and related options.

A map, launcher, game-mode INI, lobby preset, or command line may also carry overlapping names. Core must record provenance and may not treat CnCNet client settings as stock map-format facts.

### Start locations

Low-numbered Waypoints are strong multiplayer-start candidates, but identity alone is insufficient. A result records:

- Waypoint identity;
- ScenarioCell reference;
- domain validation;
- possible start-slot identity;
- selected consumer profile;
- evidence grade.

No starting MCV, camera, player, or slot is created by the parser.

### Scenario mode

No single field is authoritative across all contexts. Evidence may include:

- file extension;
- `[Basic] MultiplayerOnly`;
- `MinPlayer` / `MaxPlayer`;
- `GameMode` / `GameModes`;
- `Official`;
- campaign control INI;
- `.PKT` or launcher database registration;
- client map category;
- executable call path;
- mission-directory context.

The recommended result contains candidates and evidence rather than a single guessed boolean.

### SpecialFlags

The official editor exposes raw fields such as:

- `TiberiumGrows` / ore growth label in RA2 mode;
- `TiberiumSpreads`;
- `TiberiumExplosive`;
- `DestroyableBridges`;
- `MCVDeploy`;
- `InitialVeteran`;
- `FixedAlliance`;
- `HarvesterImmune`;
- `FogOfWar` / shroud label;
- `Inert`;
- `IonStorms` / weather storms label;
- `Meteorites`;
- `Visceroids`.

Editor visibility changes between TS and RA2 modes are not runtime proof. This dossier records raw metadata and profile candidates only.

### Digest

`[Digest]` is treated as opaque integrity metadata. It is not:

- a repository SHA;
- a MIX checksum;
- a trusted digital signature;
- a stable scenario identity;
- proof that a map is unmodified.

No digest algorithm or verification path is implemented.

## Evidence grades

Every semantic claim is assigned one of:

- `ConfirmedByOfficialRuntimeSource`;
- `ConfirmedByOfficialEditorSource`;
- `ConfirmedByIndependentImplementation`;
- `CommunityDocumented`;
- `ObservedByFutureProjectBaselineAudit`;
- `ConfiguredForProjectPolicy`;
- `Unresolved`.

This research expects little or no complete official runtime-source coverage and does not promote editor behavior to runtime fact.

## Key project policies

1. Preserve raw section occurrences, keys, values, casing, numeric spelling, empty values, duplicates, and source order.
2. Keep raw and derived interpretations separate.
3. Require explicit layout and composition policies.
4. Keep House instance, Country, Side, player slot, controller, and network peer separate.
5. Treat alliances as directed raw edges before any gameplay policy.
6. Preserve map-domain-invalid references for diagnostics and lossless roundtrip.
7. Do not infer a scenario mode from one field alone.
8. Do not use color to infer House identity.
9. Do not use Waypoint identity alone to assign a multiplayer start.
10. Do not treat lobby settings as authored scenario state.
11. Do not auto-create Neutral, Special, civilian, or missing Houses.
12. Do not auto-repair malformed rectangles, player counts, alliances, or starts.

## Source and license boundary

Pinned sources include:

- EA FinalSun / FinalAlert 2 mission editor, commit `6abf0f557469baea73079c6bf6550709e2e3584e`, GPL-3.0-or-later, official editor evidence only;
- World-Altering Editor, commit `b4c9481e9b00fb0a38739049a046f528b6054ce2`, GPL-3.0-or-later;
- OpenRA, commit `a520984d91eda9de48a62b1d15c1e3bad0d4fb1a`, GPL-3.0-or-later;
- CnCNet XNA client, commit `e6e367bbe04c1a0dc1e34a8fed2856ea3ab7e8c4`, GPL-3.0;
- MapTool, commit `f85f2226905496139f1258b5854fad915f9bbac6`, GPL-2.0-or-later;
- CNCMaps, commit `afb9c1ec118f5128cbc1f3fb5e35c7dfa0e422fb`, mixed/default license with imported-code exceptions;
- Chrono Divide public SDK, XCC/OmniBlade lineage, ModEnc permanent revisions, fixed PPM posts, RA2 DIY material where permanently locatable, Ares documentation, and versioned Phobos documentation.

All are reference-only. Shared implementation lineages are not counted as independent runtime proof. `code_imported: false`.

## Files in this dossier

- `section-and-initialization-boundaries.md`
- `basic-and-map-metadata.md`
- `map-size-local-size-and-theater.md`
- `house-country-and-player-identities.md`
- `house-properties-and-starting-state.md`
- `alliances-and-diplomacy.md`
- `multiplayer-settings-and-start-locations.md`
- `game-mode-campaign-and-skirmish-boundaries.md`
- `source-comparison.md`
- `implementation-boundaries.md`
- `test-matrix.md`
- `baseline-audit-request.md`
- `unresolved-questions.md`

## Explicit non-goals

This dossier does not implement or execute:

- metadata parsing code;
- House/Country registries;
- player/session creation;
- networking or lobby logic;
- alliances or diplomacy mutation;
- starting-unit generation;
- campaign progression;
- economy;
- SpecialFlags behavior;
- Digest verification;
- Lighting;
- Unity objects;
- compatibility-matrix or ADR changes.
