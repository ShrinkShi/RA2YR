# House, Country, Side, and player identities

> **Source notice:** ChatGPT Web public-source research. ProjectBaseline was not read. This is not a Codex artifact. No GPL or unclear-license code was copied, translated, or mechanically ported. `code_imported: false`.

## Identity domains

The following are not interchangeable:

```text
House instance
Country / HouseType definition
Side
player slot
controller kind
local human assignment
network peer
campaign-authored player House
AI participant
Neutral
Special
civilian House
multiplayer generated House
```

A correct Core model keeps each domain explicit.

## `[Houses]` ambiguity across games and layers

`[Houses]` can serve different roles depending on game/profile:

- a registry of House instances in a map;
- a Rules registry of House/Country definitions in older profiles;
- a map-local identity list;
- an editor recovery list;
- a source of numeric list keys whose semantic importance is uncertain.

Therefore the section name alone does not define the identity type.

Recommended raw model:

```text
ScenarioHouseRegistryRaw
- SectionOccurrence
- Entries[]

ScenarioHouseRegistryEntryRaw
- ListKeyRaw
- ListOrdinalCandidate
- LogicalNameRaw
- SourceOrder
```

## `[Countries]`

In RA2/YR-oriented WAE behavior:

- `[Countries]` lists map-local HouseType/Country definitions;
- each listed logical ID can have its own section;
- a standard Rules HouseType can also be modified in the map without necessarily being a newly listed local Country;
- House instances refer to Country definitions through `Country=`.

This is a strong implementation model, not complete original runtime source.

## Official editor behavior

The official editor separates:

- map Houses;
- RA2 Countries;
- Rules Houses/Countries;
- House sections;
- `[Basic] Player` selection.

Its “prepare houses” and “add house” operations can create registry entries and default sections. These are editor-authoring and repair behaviors.

They must not be converted into parser behavior such as:

- auto-creating a missing House;
- auto-creating a Country section;
- generating self alliances;
- filling default colors or credits;
- selecting a player.

## WAE HouseType evidence

WAE's HouseType model includes candidates such as:

- `ParentCountry`;
- `Suffix`;
- `Prefix`;
- `Color`;
- `Side`;
- `SmartAI`;
- `Multiplay`;
- `MultiplayPassive`;
- `WallOwner`;
- country multipliers;
- internal index;
- map-modified marker.

WAE explicitly notes that multiple House instances can use one HouseType in YR.

This supports:

```text
House instance N → Country/HouseType X
House instance M → Country/HouseType X
```

without merging N and M.

## Registry identity model

```text
ScenarioIdentity
- Domain
- RawId
- SourceOccurrence
- NormalizedCandidates[]
- DuplicateIdentityGroup
- CaseCollisionGroup
- Provenance
```

Suggested domains:

- `HouseInstance`;
- `CountryDefinition`;
- `SideDefinition`;
- `PlayerSlot`;
- `Controller`;
- `NetworkPeer`;
- `SpecialHouseSelector`.

## List key versus logical identity

Example:

```ini
[Houses]
0=Alpha
2=Bravo
```

The list key and logical name are distinct.

Preserve:

- raw key `0`;
- raw key `2`;
- gap at candidate ordinal 1;
- logical names `Alpha` and `Bravo`;
- source order.

Do not:

- compress `2` to `1`;
- use section enumeration order as an ordinal;
- assume list ordinal is runtime player slot;
- assume House name is derived from ordinal.

## Duplicate ordinal

```ini
[Houses]
1=Alpha
01=Bravo
```

Raw keys differ, while normalized numeric candidates collide.

Recommended output:

```text
Raw entries preserved
NormalizedOrdinalCollision(1)
No unique ordinal binding
```

## Duplicate logical name

```ini
[Houses]
0=Alpha
1=Alpha
```

Both entries remain. A semantic House identity group is ambiguous until a profile defines duplicate handling.

## Case collision

```ini
[Houses]
0=Alpha
1=ALPHA
```

Possible states:

- exact-string distinct;
- case-insensitive collision;
- profile-specific alias;
- unresolved.

No case normalization destroys raw identity.

## Listed but section missing

```ini
[Houses]
0=Alpha
```

with no `[Alpha]` section.

Result:

```text
House identity candidate exists
House property section unresolved
No auto-created properties
```

## Unlisted section

A section may look like a House because it contains `Country`, `Allies`, or `Credits`, but is not listed.

It becomes:

```text
UnlistedHouseSectionCandidate
```

not an automatically registered House.

An explicit editor-recovery or game-specific profile may bind it later.

## House-to-Country binding

```text
ScenarioHouseRaw.CountryRaw
→ CountryDefinition candidate group
```

Possible resolutions:

- unique map-local Country;
- unique global Rules Country;
- map-local override of global definition;
- duplicate map-local definition;
- global/map-local ambiguity;
- missing Country;
- special selector;
- invalid syntax.

WAE's loader may fall back to a first standard Country when the reference is missing. This is a recovery behavior and is not adopted by Core.

## Country-to-Side binding

```text
CountryRaw.SideRaw
→ Side definition candidate
```

A Side may influence future technology, AI, UI, or start-unit policy, but this dossier only creates a reference result.

House does not become Side, and Country does not become player slot.

## ParentCountry

`ParentCountry` is a definition-level reference candidate, potentially supporting inheritance or editor copying.

Core records:

```text
CountryDefinition → ParentCountry candidate
```

It does not merge property dictionaries or execute inheritance without an explicit composition policy.

## Neutral, Special, and civilian identities

These may be represented as:

- standard Rules House/Country definitions;
- map-local House instances;
- hardcoded/special selectors in triggers;
- client/editor aliases;
- runtime-generated Houses.

The parser must not treat any unknown name as Neutral.

Recommended states:

- `KnownLogicalHouse`;
- `KnownSpecialSelector`;
- `KnownNeutralCandidate`;
- `KnownCivilianCandidate`;
- `UnknownIdentity`;
- `ProfileRequired`.

## Player slot

A player slot is a session/lobby identity and may carry:

- slot number;
- human/AI/observer state;
- side/Country selection;
- color selection;
- start-position selection;
- team number;
- network peer;
- ready state.

It is not a House-list ordinal.

## Campaign-authored player

`[Basic] Player` can identify an authored campaign House candidate.

Recommended object:

```text
AuthoredPlayerHouseCandidate
- HouseIdRaw
- HouseResolution
- Profile
- EvidenceGrade
```

A campaign session adapter may choose it as local controller, but the parser does not.

## House `PlayerControl`

House `PlayerControl` is a raw House property candidate. It may describe author intent or engine control behavior in certain profiles.

It must not automatically override:

- lobby player assignment;
- network peer mapping;
- observer state;
- `[Basic] Player`;
- AI slot generation.

Conflicts become diagnostics.

## Controller model

```text
ScenarioPlayerControlRaw
- BasicPlayerRaw
- HousePlayerControlRaw
- HouseHumanRaw
- ClientSlotInput
- ResolutionCandidates[]
```

Future session assignment:

```text
ScenarioPlayerSlotDescriptor
- SlotIdentity
- HouseCandidate
- CountryCandidate
- ControllerKindCandidate
- NetworkPeerCandidate
- StartLocationCandidate
- TeamNumberCandidate
```

## Map-local and global composition

Suggested identity composition inputs:

```text
base Rules Countries / HouseTypes
→ YR Rules layer
→ ordered expansion layers
→ loose layers
→ explicit map-local Country modifications/additions
```

House instances are not part of that Rules type composition. They remain scenario instances.

## Provenance

Every identity binding records:

- source document;
- source section occurrence;
- source key occurrence;
- global/map-local classification;
- winner and suppressed candidates where composition applies;
- evidence grade;
- selected policy.

## Prohibited inference

Do not infer House identity from:

- color;
- Country display name;
- Side;
- object ownership distribution;
- starting position;
- list order;
- section order;
- Rules asset availability;
- UI translation;
- filename.

## Roundtrip

Preserve:

- `[Houses]` and `[Countries]` occurrence order;
- list-key spelling and gaps;
- logical-name casing;
- listed missing sections;
- unlisted candidate sections;
- duplicate definitions;
- unknown properties;
- global/map-local provenance.

No default writer reindexes or creates identities.
