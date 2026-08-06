# Game-mode, campaign, skirmish, SpecialFlags, and integrity boundaries

> **Source notice:** ChatGPT Web public-source research. ProjectBaseline was not read. This is not a Codex artifact. No GPL or unclear-license code was copied, translated, or mechanically ported. `code_imported: false`.

## Scenario classification is evidence-based

Candidate categories:

- campaign;
- skirmish;
- multiplayer;
- cooperative;
- tutorial;
- challenge;
- mission-disk/add-on;
- client custom mode;
- unknown or mixed.

No single field is universally authoritative.

## Candidate evidence sources

```text
file extension
file/container registration
[Basic] fields
[MultiplayerDialogSettings]
campaign control INI
.PKT / launcher database
client game-mode configuration
directory context
executable call path
explicit caller intent
```

Each source creates one or more `ScenarioModeEvidence` entries.

## Mode evidence model

```text
ScenarioGameModeCandidate
- ModeKind
- EvidenceItems[]
- ConfidenceCandidate

ScenarioModeEvidence
- SourceKind
- SourceLocation
- RawValue
- CandidateMeaning
- EvidenceGrade
- ConflictsWith[]

ScenarioGameModeResolution
- Candidates[]
- SelectedCandidate?
- SelectionPolicy
- Diagnostics[]
```

A selected candidate is only produced under an explicit policy.

## File extensions

Public conventions include:

- `.MAP` for registered official/campaign/multiplayer content depending on external control files;
- `.MPR` for custom RA2 multiplayer content;
- `.YRM` for custom YR multiplayer content;
- `.MMX` / `.YRO` containers for map-pack distribution.

Extension is useful evidence, not complete classification. Renaming a file cannot safely transform campaign logic into multiplayer logic.

## `[Basic] MultiplayerOnly`

`MultiplayerOnly` is a strong mode hint in editor/community sources.

It can contribute:

```text
ModeCandidate = Multiplayer
```

but conflicts may exist with:

- campaign control registration;
- `[Basic] Player`;
- campaign media fields;
- Trigger-heavy mission structure;
- client mode database;
- file extension.

No automatic repair occurs.

## `[Basic] Player`

A resolved authored player House is campaign/singleplayer evidence, but not conclusive:

- multiplayer maps may retain stale Player metadata;
- co-op missions may define authored Houses;
- editor templates may include defaults;
- client workflows can ignore it.

## Min/Max player evidence

`MinPlayer`, `MaxPlayer`, start Waypoints, and client player limits provide player-count evidence. They do not by themselves distinguish skirmish, multiplayer, or co-op.

## `GameMode` and `GameModes`

These are raw strings whose meanings can be:

- stock game mode identifier;
- client map filter;
- editor metadata;
- extension-defined mode;
- comma-separated mode list;
- legacy field.

No normalization by display text is performed.

## Official and custom distribution

`Official` can indicate distribution/transfer behavior in community documentation. It is not a security or authorship guarantee.

`Official=yes` does not prove the map shipped with the user's installed game, and `Official=no` does not prove multiplayer gameplay semantics.

## Campaign control

Campaign scenarios can be selected through external campaign control INIs, mission lists, executable flow, or client mission databases.

The map alone may not contain enough evidence to reconstruct:

- previous/next mission;
- difficulty path;
- side/country campaign;
- movie/briefing sequence;
- carry-over state;
- mission completion routing.

A campaign adapter must accept external control data.

## Skirmish versus multiplayer

The same map geometry and starts can be used for:

- offline skirmish;
- LAN;
- online multiplayer;
- client-defined challenge;
- co-op.

The distinction may be session launch context rather than a map-internal format property.

## Cooperative mode

Co-op classification may require combined evidence:

- client game-mode category;
- multiple human slots;
- authored Houses;
- fixed alliances;
- campaign-style triggers;
- explicit mode configuration.

No generic “two allied players” rule is sufficient.

## Tutorial and challenge

Tutorial/challenge may be identified through:

- campaign/client catalog;
- Basic fields;
- file/directory convention;
- explicit launcher metadata;
- content conventions.

Content inspection is not used to guess mode in Core.

# `[SpecialFlags]`

## Official editor evidence

The official editor exposes raw values for:

- `TiberiumGrows`;
- `TiberiumSpreads`;
- `TiberiumExplosive`;
- `DestroyableBridges`;
- `MCVDeploy`;
- `InitialVeteran`;
- `FixedAlliance`;
- `HarvesterImmune`;
- `FogOfWar`;
- `Inert`;
- `IonStorms`;
- `Meteorites`;
- `Visceroids`.

In RA2 mode it changes UI labels and hides some TS-specific controls. This proves editor profile behavior, not full runtime applicability.

## Raw SpecialFlags model

```text
ScenarioSpecialFlagsRaw
- PropertyOccurrences[]
- UnknownFields[]
- DuplicateGroups[]
```

Each property retains original boolean/string spelling.

## Behavior boundaries

This dossier does not implement:

- ore/Tiberium growth or spread;
- resource explosiveness;
- bridge destruction;
- MCV deployment;
- initial veterancy;
- locked alliances;
- harvester immunity;
- shroud/fog simulation;
- weather storms;
- meteorites;
- visceroids.

## Field applicability conflicts

Community documentation distinguishes campaign, skirmish, and multiplayer behavior for some flags. For example, a field may be read from SpecialFlags only in one context or ignored/overridden in another.

Therefore each descriptor records:

- game/version profile;
- scenario-mode candidates;
- source section;
- editor visibility;
- community runtime claim;
- unresolved conflicts.

## SpecialFlags versus multiplayer options

Overlapping names can exist in:

- `[SpecialFlags]` map metadata;
- Rules `[MultiplayerDialogSettings]`;
- lobby session settings;
- client game-mode overrides.

They are separate source layers. Precedence belongs to future initialization policy.

# `[Digest]`

## Opaque integrity metadata

```text
ScenarioDigestRaw
- SectionOccurrences[]
- KeyOccurrences[]
- RawText
- ShapeCandidates[]
- EvidenceGrade
```

No algorithm is assumed.

## Distinctions

```text
Digest metadata
≠ repository commit SHA
≠ file SHA published by audit
≠ MIX checksum
≠ map filename identity
≠ trusted digital signature
≠ anti-cheat proof
```

## Possible roles

Public sources suggest candidates such as:

- editor-generated integrity field;
- runtime or client validation input;
- obsolete compatibility data;
- Base64-encoded digest;
- map-change detector.

Exact algorithm, covered byte range, canonicalization, and consumer remain unresolved in this dossier.

## Security boundary

Digest is untrusted map input. It must never be used as a cryptographic authenticity guarantee without a separately verified scheme and trusted key infrastructure.

## Roundtrip

A lossless writer preserves Digest exactly. A canonical editor rewrite may recalculate or remove it only under an explicit profile.

# `[Lighting]`

## Reference-only scope

Potential raw fields include:

- ambient;
- red;
- green;
- blue;
- level;
- ground;
- aircraft;
- ion-storm or alternate-lighting candidates;
- extension values.

This dossier only records that Lighting is a separate environment input.

It is not:

- House color;
- theater identity;
- minimap remap;
- player color;
- campaign mode;
- runtime Unity light.

No lighting color or render state is generated.

# Map-local Rules composition

## Ordered global layers

Existing project composition provides an ordered Rules/Art pipeline such as:

```text
ra2
→ ra2md
→ expandmd01..99
→ loose layers
→ explicit map-local layer
```

## Scenario-local section classification

Not every map section is a Rules override.

Candidate classes:

- scenario metadata section;
- House instance section;
- map-local Country/HouseType definition;
- map-local Rules type override;
- placement/trigger data;
- editor recovery/private data;
- unknown.

## Explicit policy

```text
ScenarioLocalCompositionPolicy
- SectionClassifier
- AllowedRulesTypeSections
- HouseInstanceSections
- CountryDefinitionSections
- MetadataSections
- UnknownSectionHandling
```

A section named after a Rules type may collide with a House identity. Per-key provenance and expected-key profiles are needed; the whole section cannot be blindly assigned to one domain.

## Winner and suppressed provenance

For actual Rules-composed properties, retain:

- global candidate;
- map-local candidate;
- selected winner;
- suppressed candidate;
- source layer;
- exact key provenance.

House instance properties are not merged into the Country/Rules type by default.

# Initialization descriptor

Mode resolution and metadata produce:

```text
ScenarioInitializationDescriptor
- GeometryDescriptor
- TheaterBinding
- HouseIdentityGraph
- HouseStartingStates
- AllianceGraph
- PlayerCountEvidence
- StartLocationCandidates
- MultiplayerSettingsSources
- ModeResolution
- SpecialFlagsRaw
- DigestRaw
- EnvironmentReferences
- Diagnostics
```

It remains non-executable.

# Conflict examples

## Campaign versus multiplayer

```text
Basic.MultiplayerOnly=yes
Basic.Player=Alpha
client catalog=Campaign
```

Result: conflicting evidence; no automatic classification.

## Fixed alliance versus lobby

```text
SpecialFlags.FixedAlliance=yes
lobby permits team changes
```

Result: source conflict for future session policy; no mutation during parsing.

## MCV redeploy conflict

```text
Rules default=yes
map extension override=no
lobby selection=yes
```

All three sources remain with provenance.

## Digest mismatch

A future verifier may report mismatch without changing metadata or refusing raw parse. Parse success and integrity validation are distinct.

# Non-goals

No implementation of:

- campaign progression;
- session classification by content heuristics;
- SpecialFlags gameplay;
- Digest algorithm;
- Lighting rendering;
- Rules override code;
- Unity scene/environment setup.
