# Section and initialization boundaries

> **Source notice:** ChatGPT Web public-source research. ProjectBaseline was not read. This is not a Codex artifact. No GPL or unclear-license code was copied, translated, or mechanically ported. `code_imported: false`.

## Purpose

This document freezes the architectural boundary between lossless scenario metadata parsing and later game/session initialization.

The research target is declarative data. It is not a player manager, campaign loader, multiplayer lobby, House runtime, diplomacy system, or Unity scene bootstrap.

## Frozen pipeline

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

Each arrow is a boundary with its own input, output, policy, diagnostics, and evidence grade.

## Section families

### Core scenario metadata

- `[Basic]`
- `[Map]`
- `[SpecialFlags]`
- `[MultiplayerDialogSettings]`

### Identity registries

- `[Houses]`
- `[Countries]`
- map-local House sections
- map-local Country / HouseType sections

### Reference-only sections

- `[Waypoints]`
- `[Triggers]`
- `[Tags]`
- `[VariableNames]` and profile-specific alternatives
- `[Lighting]`
- `[Digest]`
- `[Preview]`
- campaign control INIs
- `.PKT` or launcher metadata
- editor/client-private sections

### External registries

- Rules Countries / HouseTypes candidates
- Rules Sides
- Rules `[Colors]`
- starting-unit or multiplayer-default settings
- game-mode INIs
- client map databases

These families may contribute references, but they do not become one merged dictionary.

## Lossless INI layer

The input layer must preserve:

- section name raw spelling;
- section occurrence index;
- physical section order;
- key raw spelling;
- key occurrence index;
- physical key order;
- duplicate sections;
- duplicate keys;
- empty values;
- whitespace and comment provenance where supported;
- exact raw value text;
- line/source provenance;
- source layer provenance.

A semantic view may normalize a section or key candidate, but it cannot replace the lossless document.

## Duplicate sections

Examples:

```ini
[Basic]
Name=A

[Basic]
Name=B
```

The parser must not silently flatten this into one map.

Recommended result:

```text
SectionOccurrence[0] → Name=A
SectionOccurrence[1] → Name=B
SemanticBasicCandidateGroup → ambiguous duplicate section
```

A policy may later define a compatibility view, but raw identity remains available.

## Duplicate keys

Examples:

```ini
[Map]
Theater=TEMPERATE
Theater=SNOW
```

or:

```ini
[SomeHouse]
Allies=A,B
Allies=C
```

Default parsing records all occurrences and reports a duplicate semantic candidate. It does not choose first-wins or last-wins without an explicit source/profile policy.

## Raw metadata sections

The raw metadata view should provide:

```text
ScenarioMetadataSectionRaw
- SectionNameRaw
- Occurrence
- KeyOccurrences[]
- SourceProvenance
```

No semantic default is applied at this layer.

## Explicit metadata layout profiles

A layout profile may state:

- which section names are recognized;
- which keys are recognized;
- raw type candidates;
- profile-specific default candidates;
- version applicability;
- source and evidence grade;
- whether unknown fields are allowed;
- whether duplicate keys make a semantic view ambiguous;
- whether a field is editor-only, client-only, extension-only, or unresolved.

A profile is not selected by trying interpretations until one “looks correct.”

## Geometry dependency

Map geometry must be resolved before:

- validating LocalSize containment candidates;
- checking start Waypoint domains;
- checking House base-node coordinates;
- classifying scenario-cell references;
- evaluating whether an authored start lies outside the playable area.

Geometry resolution does not require loading IsoMap, TMP, Overlay, Preview, or Unity coordinates.

## Theater dependency

Theater identity depends on:

- `[Map] TheaterRaw`;
- selected theater profile registry;
- optional explicit extension profile.

It may produce logical resource roles, but no resource is opened.

Theater resolution is independent of:

- House identity;
- player slot;
- alliances;
- game mode;
- lighting behavior;
- weather behavior.

## House and Country identity dependency

Identity collection precedes House property binding.

Recommended order:

```text
collect [Houses] entries
collect [Countries] entries
collect candidate House sections
collect candidate Country sections
compose explicit map-local/global type views
create identity candidate groups
bind House → Country candidates
```

A House property must not create a House identity implicitly unless an explicit editor-recovery policy is selected.

## Starting-state dependency

Starting-state descriptors depend on logical House identity and may contain:

- credits candidate;
- authored player-control candidate;
- Country reference;
- color reference;
- base-node templates;
- start Waypoint or cell candidates;
- authored unit/building count statistics;
- unknown fields.

They do not create player controllers or spawned entities.

## Alliance dependency

Alliance parsing occurs after House identities are collected, because an ally token is first a raw string and only then a House reference candidate.

Result:

```text
ScenarioAllianceEdgeRaw
- SourceHouseRaw
- TargetHouseRaw
- SourcePropertyOccurrence
- ResolutionState
```

No reverse edge is generated.

## Player-slot dependency

Player slots depend on:

- mode candidate;
- player-count metadata;
- start Waypoint candidates;
- lobby/session input;
- optional authored House assignments.

Player slots do not depend solely on `[Houses]` list order.

## Game-mode descriptor dependency

The mode descriptor may aggregate evidence from:

- `[Basic]`;
- `[MultiplayerDialogSettings]`;
- file extension;
- launcher/client database;
- campaign control INI;
- `.PKT` registration;
- directory or executable invocation context;
- explicit caller profile.

The result must keep evidence items rather than storing only one enum.

## Future simulation/session boundary

A future adapter may consume:

```text
ScenarioInitializationDescriptor
SessionConfiguration
RuntimeRulesDescriptor
WorldGeometry
ObjectPlacementGraph
TriggerGraph
```

It may then create players, assign controllers, apply lobby overrides, or spawn units. None of those operations belong to this dossier's Core boundary.

## Explicit initialization dependency graph

```text
RawScenarioMetadata
    │
    ├──> BasicRaw
    ├──> MapRaw
    ├──> SpecialFlagsRaw
    ├──> MultiplayerSettingsRaw
    ├──> HouseRegistryRaw
    └──> CountryRegistryRaw

MapRaw
    ├──> MapGeometryDescriptor
    └──> TheaterBindingResult

HouseRegistryRaw + CountryRegistryRaw + explicit Rules composition
    └──> HouseCountryIdentityGraph

HouseCountryIdentityGraph + House sections
    └──> HouseStartingStateDescriptors

HouseCountryIdentityGraph + Allies fields
    └──> DirectedAllianceGraph

MapGeometryDescriptor + Waypoints + multiplayer metadata
    └──> StartLocationCandidates

BasicRaw + client/control metadata + multiplayer metadata
    └──> ScenarioModeResolution

all descriptors
    └──> ScenarioInitializationDescriptor
```

## Prohibited parser side effects

The parser must not:

- create a House instance in the runtime heap;
- select a local human player;
- create an AI player;
- assign a network peer;
- add reciprocal alliances;
- resolve hostility dynamically;
- select random starts;
- spawn MCVs or starting units;
- calculate credits or carry-over money;
- update score, winner, loser, or defeated state;
- run SpecialFlags logic;
- load media;
- verify Digest as a security signature;
- create Unity scenes or objects.

## Error handling

All failures are structured diagnostics.

Suggested categories:

- `MissingRequiredSectionCandidate`;
- `DuplicateSectionAmbiguity`;
- `DuplicateKeyAmbiguity`;
- `MalformedRectangle`;
- `ArithmeticOverflow`;
- `UnknownTheater`;
- `DuplicateIdentity`;
- `MissingIdentitySection`;
- `UnlistedIdentitySection`;
- `DanglingCountryReference`;
- `DanglingAllianceReference`;
- `PlayerControlConflict`;
- `StartLocationConflict`;
- `ModeEvidenceConflict`;
- `UnknownSpecialFlag`;
- `DigestShapeUnknown`;
- `ExtensionProfileRequired`.

## Fail-closed versus preservation

Fail-closed means semantic initialization cannot proceed automatically. It does not mean the raw data is discarded.

Examples:

- malformed `Size` → raw map metadata retained; geometry descriptor unavailable;
- duplicate House ID → all candidates retained; unique House binding unavailable;
- missing ally target → directed raw edge retained; target unresolved;
- unknown theater → raw token retained; resource profile unavailable;
- conflicting mode evidence → all evidence retained; no automatic mode selected.

## Read limits

Suggested limits include:

- maximum section occurrences;
- maximum keys per section;
- maximum raw value length;
- maximum House entries;
- maximum Country entries;
- maximum alliance tokens per House;
- maximum base nodes per House;
- maximum Waypoints considered for start binding;
- maximum diagnostics;
- maximum total reference edges.

All counters use checked arithmetic.

## Input-mode equivalence

The same state machine and semantic pipeline must serve:

- `Memory<byte>` or equivalent in-memory text;
- seekable Stream;
- non-seekable Stream where supported;
- short-read Stream;
- bounded MIX entry window.

A MIX window provides bytes and provenance only. It cannot select a metadata, theater, House, or mode profile.

## Roundtrip boundary

The following identities are distinct:

- lossless INI identity;
- raw metadata identity;
- semantic descriptor equality;
- canonical editor rewrite;
- session/lobby override equality;
- FinalAlert reopen behavior;
- original runtime acceptance;
- gameplay equivalence.

A default writer must not repair duplicates, reindex Houses, symmetrize allies, normalize IDs, or replace unknown fields.

## Evidence policy

Editor UI labels and repair defaults are `ConfirmedByOfficialEditorSource`, not runtime proof.

Independent implementations can confirm shared conventions, but shared code lineage must be recorded. Client behavior is client evidence. Community documentation remains `CommunityDocumented` unless independently elevated.
