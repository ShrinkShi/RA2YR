# House properties and starting-state boundaries

> **Source notice:** ChatGPT Web public-source research. ProjectBaseline was not read. This is not a Codex artifact. No GPL or unclear-license code was copied, translated, or mechanically ported. `code_imported: false`.

## Purpose

This document separates raw House properties from identity binding, authored starting state, editor statistics, and future runtime state.

A House section is not a serialized runtime player object.

## Raw property model

```text
ScenarioHousePropertyRaw
- HouseIdentityRaw
- KeyRaw
- ValueRaw
- Occurrence
- SourceOrder
- TypeCandidates[]
- EvidenceGrade
```

Unknown fields are retained.

## Common public candidates

WAE and the official editor expose fields including:

- `IQ`;
- `Edge`;
- `Color`;
- `Allies`;
- `Credits`;
- `Country` in RA2/YR-oriented profiles;
- `ActsLike` and `Side` in TS-oriented profiles;
- `TechLevel`;
- `PercentBuilt`;
- `PlayerControl`;
- `NodeCount`;
- indexed base-node entries;
- extension/editor-specific fields.

Other public/community candidates include:

- `Name`;
- `ParentCountry` on Country definitions;
- `Human`;
- `MultiplayerPassive`;
- `StartLocation`;
- `StartingWaypoint`;
- `StartingUnits`;
- `StartingBuildings`;
- `Base`;
- `Production`;
- `Power`;
- unit/building/infantry/aircraft/ship counts;
- defeated/winner/loser state candidates;
- map-local extension fields.

No single profile is assumed to contain all of them.

## Property families

### Identity references

- `Country`;
- TS `ActsLike`;
- `Side` candidates;
- `ParentCountry` on a Country definition.

These create reference candidates only.

### Presentation references

- `Color`;
- display `Name`;
- UI/editor color fields.

These do not define House identity.

### Starting economy candidates

- `Credits`;
- campaign carry-over inputs from `[Basic]`;
- multiplayer money/lobby settings;
- extension economy fields.

### AI and difficulty candidates

- `IQ`;
- `TechLevel`;
- `PercentBuilt`;
- `Edge`;
- passive/AI flags.

These may influence future runtime behavior, but parser output is declarative.

### Player-control candidates

- `PlayerControl`;
- `Human`;
- `MultiplayerPassive`;
- `[Basic] Player` reference;
- session/lobby controller assignment.

These must remain separate.

### Base-template candidates

- `NodeCount`;
- `000` through `999`-style base-node records;
- building type and cell coordinates.

A base node is a template/reference, not a spawned Structure.

### Statistics or runtime-state candidates

- unit counts;
- building counts;
- aircraft counts;
- infantry counts;
- ship counts;
- power/production fields;
- defeated/winner/loser candidates.

These may be editor-maintained statistics, save-state-like metadata, or runtime inputs depending on profile. They are not automatically authoritative starting state.

## WAE House model

WAE commit `b4c9481e9b00fb0a38739049a046f528b6054ce2` models:

```text
House.ININame
House.HouseType
IQ
Edge
Color
Allies
Credits
Country
ActsLike
TechLevel
PercentBuilt
PlayerControl
DefaultRepairableStructures
ID
BaseNodes
```

This is strong evidence for WAE's reader/writer contract. It is not full official runtime evidence.

## Official editor House UI

The official editor exposes controls for:

- human player selection through `[Basic] Player`;
- House list;
- Allies;
- Color;
- Credits;
- Edge;
- IQ;
- NodeCount;
- PercentBuilt;
- PlayerControl;
- TechLevel;
- Country or ActsLike/Side depending on mode.

Its “prepare houses” and “add house” actions write defaults such as self-alliance, credits, Country, TechLevel, PercentBuilt, and PlayerControl.

These are canonical authoring defaults. Parser Core must not apply them to missing data.

## Credits

### Raw model

```text
ScenarioCreditsRaw
- RawText
- SignedIntegerCandidate
- UnsignedIntegerCandidate
- ParseStatus
- SourceKind
```

### Distinct money sources

```text
House.Credits
Basic.CarryOverMoney
Basic.CarryOverCap
MultiplayerDialogSettings.Money/Credits candidate
lobby selected credits
runtime current credits
resource Overlay value
score
AI economy state
```

They are not merged by the parser.

### Negative and overflow values

Negative, oversized, malformed, and empty credits are retained with diagnostics. No clamp to zero occurs.

## Carry-over

Campaign carry-over may contribute to starting economy after a prior mission or savegame.

A future campaign adapter may compute:

```text
authored House Credits
+ carry-over policy result
+ campaign/session override
```

This dossier does not define that formula.

## Color

### Raw binding

```text
ScenarioColorReferenceRaw
- ColorIdRaw
- SourceHouse
```

Potential targets:

- Rules `[Colors]` registry;
- map-local `[Colors]` contribution;
- client multiplayer color list;
- extension color registry.

### Prohibited behavior

Core does not:

- load a palette;
- build a remap ramp;
- calculate RGB;
- choose nearest color;
- fall back to red or white;
- infer House identity from color;
- assign a multiplayer slot color.

### Result

```text
ScenarioColorBindingResult
- RawReference
- LogicalColorCandidate
- ResolutionState
- Provenance
- EvidenceGrade
```

## IQ

`IQ` is a numeric candidate associated with AI behavior in editor models.

Core stores:

- raw text;
- signed/unsigned candidates;
- profile applicability;
- invalid/overflow status.

It does not instantiate AI or select difficulty.

## TechLevel

House `TechLevel` is separate from:

- multiplayer lobby tech-level option;
- Country technology tree;
- Rules prerequisite system;
- unit/building TechLevel fields;
- AITrigger TechLevel.

No build permission is computed.

## PercentBuilt

Public editor defaults sometimes write `PercentBuilt` values when creating Houses. Its exact meaning and runtime range remain profile-specific.

Do not clamp to 0–100 without an explicit profile.

## Edge

`Edge` is a raw enum/string candidate, often associated with reinforcement arrival edge or AI behavior. It is not a geometric map edge object.

Unknown values remain raw.

## PlayerControl and Human

Possible conflicts:

```text
Basic.Player=Alpha
[Alpha] PlayerControl=no
[Bravo] PlayerControl=yes
session local player=Charlie
```

The parser reports all candidates. It does not resolve current-machine control.

Suggested diagnostic:

- `BasicPlayerVsHouseControlConflict`;
- `MultiplePlayerControlHouses`;
- `NoAuthoredPlayerCandidate`;
- `SessionAssignmentRequired`.

## Base nodes

WAE models base nodes as:

```text
index=BuildingType,X,Y
```

and writes `NodeCount`.

Core must preserve:

- raw index spelling;
- source order;
- count/type/coordinate tokens;
- gaps;
- duplicate node keys;
- nodes after gaps;
- unknown BuildingType;
- out-of-domain coordinates;
- `NodeCount` mismatch.

It does not create Structures or validate art.

## Starting units and buildings

Potential authored sources include:

- placement sections;
- House base nodes;
- multiplayer starting-unit settings;
- game-mode overrides;
- starting MCV policy;
- triggers/reinforcements;
- campaign control or savegame state.

These remain independent inputs.

A field named `StartingUnits` is not automatically equivalent to placement units or a lobby unit-count slider.

## Editor statistics

Fields such as `UnitCount`, `BuildingCount`, or `AircraftCount` may be:

- editor-maintained summaries;
- runtime caches;
- ignored metadata;
- version-specific fields;
- malformed/stale values.

Core records them as raw statistics candidates and does not compare them to placements by default.

An explicit consistency analysis may later report mismatches without rewriting either source.

## Starting-state descriptor

```text
ScenarioHouseStartingStateDescriptor
- HouseIdentity
- CountryBinding
- ColorBinding
- CreditsCandidate
- IQCandidate
- TechLevelCandidate
- PercentBuiltCandidate
- PlayerControlCandidates[]
- BaseNodeTemplates[]
- StatisticsCandidates[]
- UnknownProperties[]
- Diagnostics[]
```

This descriptor is immutable and non-executable.

## House-section collisions

A House logical name may collide with another section type or Rules type. WAE explicitly guards against erasing unrelated keys when deleting House data from a shared-name section.

Core therefore preserves property provenance per key and must not assume the entire section belongs exclusively to the House semantic view.

## Map-local extensions

Ares/Phobos and editor/client tooling may add House properties. They require an explicit extension profile.

Unknown fields are not rejected solely because the stock profile does not recognize them.

## Roundtrip

Preserve:

- property key casing;
- raw boolean and numeric spelling;
- unknown fields;
- duplicate keys;
- base-node index spelling and gaps;
- invalid references;
- map-local extension fields;
- stale statistics;
- asymmetric or self allies, handled in the alliance dossier.

No default canonical writer recalculates statistics, base nodes, credits, or controller fields.
