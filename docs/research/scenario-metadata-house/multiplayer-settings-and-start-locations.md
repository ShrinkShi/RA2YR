# Multiplayer settings and start locations

> **Source notice:** ChatGPT Web public-source research. ProjectBaseline was not read. This is not a Codex artifact. No GPL or unclear-license code was copied, translated, or mechanically ported. `code_imported: false`.

## Scope

This document separates authored map metadata, Rules multiplayer-dialog defaults, launcher/client settings, lobby/session state, and start-location references.

No network session, lobby, player object, starting unit, or random-start algorithm is implemented.

# `[MultiplayerDialogSettings]`

## Section role conflict

Public documentation primarily describes `[MultiplayerDialogSettings]` as a Rules-level source of defaults for the RA2/YR multiplayer dialog.

Tools, mods, game-mode INIs, and launchers may also expose overlapping settings in:

- a map;
- Rules overrides;
- game-mode files;
- client configuration;
- lobby presets;
- command-line or spawn INI data.

The same key name in these sources does not imply identical provenance or precedence.

## Raw model

```text
ScenarioMultiplayerSettingsRaw
- SectionOccurrence
- PropertyOccurrences[]
- SourceKindCandidate
- SourceLayer
- UnknownFields[]
```

Source kind candidates:

- `ScenarioMapAuthored`;
- `RulesDefault`;
- `GameModeOverride`;
- `ClientDefault`;
- `LobbySessionValue`;
- `ExtensionProfile`;
- `Unknown`.

## Common field candidates

Public/community/client material includes candidates such as:

- `MinPlayers` / `MinPlayer`;
- `MaxPlayers` / `MaxPlayer`;
- `GameMode` / `Mode`;
- `Money` / `Credits`;
- `UnitCount` / starting-unit count;
- `TechLevel`;
- `ShortGame`;
- `SuperWeaponsAllowed`;
- `BuildOffAlly`;
- `MCVRedeploys` or spelling variants;
- `Crates`;
- `Bases`;
- `BridgeDestruction`;
- `MultiEngineer`;
- `HarvesterTruce`;
- `FogOfWar`;
- `Shroud`;
- `ShadowGrow`;
- `TiberiumGrows`;
- `AlliesAllowed`;
- `AllyChangeAllowed`;
- `AIPlayers`;
- `AIDifficulty`;
- obsolete or extension mode flags;
- client filter and UI metadata.

Applicability differs across game version and consumer.

## Client evidence

CnCNet XNA client separates:

- game mode selection;
- map selection;
- human player rows;
- AI player rows;
- side/Country selection;
- color selection;
- starting-position selection;
- lobby team number;
- checkbox/dropdown game options;
- network broadcast state;
- local player lookup;
- random seed and unique game identity.

It can also remove starting locations before launching a map under an explicit client option.

This confirms that many effective multiplayer settings are session/client state, not immutable map metadata.

Evidence grade: `ConfirmedByIndependentImplementation` for CnCNet client behavior.

## Map versus lobby credits

Distinct values:

```text
House.Credits
Basic carry-over candidates
Rules MultiplayerDialogSettings money default
client lobby selected money
spawn/session generated money
runtime current credits
```

A session initializer may choose precedence. Parser Core only records the sources.

## Map versus lobby tech level

Distinct values:

- House `TechLevel`;
- Rules type TechLevel;
- multiplayer dialog TechLevel;
- game-mode override;
- lobby selected TechLevel;
- runtime buildability.

No build rules are calculated.

## Starting-unit settings

Potential inputs:

- lobby unit-count option;
- game-mode default;
- map-authored placements;
- campaign trigger reinforcements;
- base nodes;
- MCV/base option;
- Country/Side starting-unit policy;
- extension settings.

They remain separate.

## Bases and MCV

`Bases`, MCV redeploy/repack candidates, and start-unit settings do not directly identify a start cell.

A future session adapter may use them with:

- start slot;
- selected Country/Side;
- starting-unit policy;
- map geometry;
- Rules.

Parser Core does not create an MCV or construction yard.

# Start location and Waypoint binding

## Identity model

```text
WaypointIdentity
ScenarioCellReference
StartSlotCandidate
ConsumerProfile
EvidenceGrade
```

A Waypoint's numeric identity is not automatically a player slot.

## Low-numbered Waypoint candidate

The strongest community/editor/client convention associates low-numbered Waypoints—commonly 0 through 7 for an eight-player profile—with multiplayer starting positions.

However:

- campaign maps also use Waypoints;
- clients can remove or replace starts;
- profiles can support different maximums;
- extensions can expand Waypoint ranges;
- a low ID may serve camera, reinforcement, script, or mission logic.

Therefore start binding requires an explicit consumer profile.

## Start-location descriptor

```text
ScenarioStartLocationRaw
- WaypointIdRaw
- CellIdRaw
- CoordinateCandidate
- SourceOccurrence

ScenarioStartLocationCandidate
- SlotCandidate
- WaypointIdentity
- CellResolution
- SizeDomainStatus
- LocalSizeDomainStatus
- IsoMapPresenceStatus
- ConsumerProfile
- EvidenceGrade
```

## Domain layers

A start can be:

- syntactically valid ScenarioCellId;
- inside selected Size domain;
- inside selected LocalSize domain;
- outside LocalSize but inside Size;
- inside geometry but missing an IsoMap cell;
- out of geometry;
- duplicate with another start;
- unresolved because geometry is ambiguous.

No start is moved or clamped.

## Missing start

A declared player count may exceed resolved start candidates.

Possible diagnostic:

```text
InsufficientStartLocationsForDeclaredPlayers
```

No synthetic Waypoint or random cell is generated.

## Duplicate start slot

Duplicate can mean:

- duplicate Waypoint ID;
- two Waypoints interpreted as the same slot;
- two slots referencing the same cell;
- duplicate section/key occurrence.

All raw entries remain.

## Fixed versus random starts

Potential sources:

- map/client field indicating fixed starts;
- lobby start dropdown;
- randomized session seed;
- client option to remove starts;
- explicit House start property;
- campaign profile.

The map parser cannot decide random assignment.

## House-authored start candidates

Fields such as `StartLocation`, `StartingWaypoint`, `HomeCell`, or similar names may be profile-specific.

They must not automatically override low-numbered Waypoint candidates.

Recommended consistency analysis compares, without repairing:

- House start reference;
- Basic home/camera candidate;
- multiplayer start Waypoint;
- lobby-selected slot.

## MaxPlayers sources

Potential sources:

- `Basic.MaxPlayer`;
- `Basic.MinPlayer`;
- `Map.MaxPlayers` candidate;
- number of start Waypoints;
- client hard maximum;
- game-mode maximum;
- House count;
- launcher database.

Result:

```text
ScenarioPlayerCountEvidence[]
ScenarioPlayerCountResolution
```

No maximum is inferred from House count alone.

## Player row and House assignment

A client player row contains session identity and selections. It may eventually map to a generated or authored House.

```text
PlayerSlot
→ selected Country/Side
→ selected color
→ selected start
→ team number
→ future House instance
```

This flow is session logic, not map parsing.

## Lobby team versus alliance

Lobby team number is an integer/session option. House `Allies` is an authored directed reference list.

No parser-level conversion occurs.

## Observer

Observer is a session/controller state. It is not inferred from:

- missing House;
- missing start;
- Neutral identity;
- `PlayerControl=no`;
- empty side.

## AI slots

AI player rows and AI difficulty belong to session initialization. They do not correspond directly to map AITrigger definitions or House `IQ`.

## Cooperative mode

Co-op may use:

- multiple human player slots;
- authored campaign Houses;
- shared alliances;
- mode-specific spawn logic;
- client rules.

No single field proves cooperative mode.

## Client-only fields

Fields documented or implemented only in launchers/clients are labeled:

```text
ClientMetadata
```

They are not promoted to stock scenario format.

## Consistency analysis

```text
ScenarioMultiplayerConsistencyAnalysis
- PlayerCountEvidence
- StartSlotCandidates
- DuplicateStarts
- MissingStarts
- DomainInvalidStarts
- HouseAssignmentCandidates
- LobbyTeamCandidates
- AuthoredAllianceComparison
- OptionSourceConflicts
- Diagnostics
```

## Roundtrip

Map roundtrip preserves authored metadata and Waypoints. It does not persist transient lobby selections unless the selected output profile explicitly writes a session/spawn document.

Preserve:

- map fields;
- unknown multiplayer fields;
- Waypoint IDs and raw cell values;
- duplicate starts;
- missing starts;
- out-of-domain references;
- client-private metadata if it is part of the source document.

## Non-goals

No implementation of:

- lobby UI;
- CnCNet protocol;
- LAN/network peer assignment;
- random start selection;
- team assignment;
- AI slot creation;
- credit/tech/unit override application;
- starting MCV or unit creation;
- map mutation for session launch.
