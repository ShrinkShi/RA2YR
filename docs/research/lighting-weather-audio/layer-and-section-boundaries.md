# Layer and Section Boundaries

> **Source notice:** ChatGPT Web public-source research. ProjectBaseline was not read. Not a Codex artifact. No GPL or unclear-license code was copied, translated, mechanically rewritten, or ported. `code_imported: false`.

## Purpose

This file classifies environment-related inputs before any numeric, resource, presentation, audio, visibility, or gameplay interpretation occurs.

## Lossless first layer

The scenario INI layer retains:

- every physical section occurrence;
- physical section order;
- exact section spelling;
- every key occurrence;
- exact key spelling;
- raw value text;
- empty values;
- leading and trailing whitespace;
- comments when supported by the existing lossless document;
- duplicate sections and duplicate keys;
- source-layer and source-document provenance.

It does not:

- merge duplicate `[Lighting]` sections;
- choose first-wins or last-wins;
- parse a decimal;
- normalize a Theme or Sound identifier;
- bind a Rules object type;
- execute a Trigger;
- apply a palette;
- construct presentation state.

## Section classification

### `[Lighting]`

Primary raw source for static and alternate lighting profile candidates.

Potential stock/profile fields:

- Ambient, Red, Green, Blue, Level, Ground;
- IonAmbient, IonRed, IonGreen, IonBlue, IonLevel, IonGround;
- Dominator* fields in RA2/YR-oriented and extension-aware tooling;
- unknown fields.

Ownership: scenario environment metadata.

Not owned by `[Lighting]`:

- palette bytes;
- Theater identity;
- House remap;
- current storm activation;
- object-local light placement;
- current visibility;
- Trigger execution;
- audio playback.

### `[SpecialFlags]`

Mixed scenario capability/configuration metadata. Environment-related candidates include:

- IonStorms / editor-labeled Weather Storms;
- FogOfWar / editor-labeled Shroud;
- Meteorites in TS-oriented profiles;
- inherited or extension weather candidates.

The same section also contains non-presentation gameplay candidates such as resource growth, bridge destruction, MCV deployment, alliance locking, and veterancy. The environment reader must not claim ownership of the entire section.

Recommended classification per key:

```text
PresentationOnlyCandidate
SimulationAffectingCandidate
TriggerControlledCandidate
EditorOnlyCandidate
ExtensionCandidate
Unknown
```

### `[Basic]`

Environment-adjacent references include:

- Theme;
- Intro, Win, Lose, Brief, Action, and related media candidates;
- mission metadata that can influence presentation selection.

`[Basic]` remains a scenario metadata section. The environment adapter consumes only explicit references and their provenance. It does not reinterpret the rest of `[Basic]`.

### `[AudioVisual]`

Primarily a Rules-layer section candidate, not inherently a scenario section. It can define global sound, animation, visual, or effect defaults. A map may contain map-local Rules overrides, but that does not make every `[AudioVisual]` occurrence scenario metadata.

Required classification:

```text
Global Rules AudioVisual
Expansion Rules AudioVisual
Loose Rules AudioVisual
Game-mode AudioVisual
Map-local Rules AudioVisual
Unknown same-name section
```

No blind whole-section overwrite is allowed.

### Trigger sections

Relevant physical sections can include:

- `[Triggers]`;
- `[Events]`;
- `[Actions]`;
- `[Tags]`;
- editor-private trigger metadata.

The M3-R10 consumer receives raw Trigger graph records from the Trigger research boundary. It does not parse or execute them independently. It classifies only environment-domain action candidates such as Theme, Sound, Speech, Movie, reveal/shroud, lighting, weather, screen, or camera effects.

### Rules object-type sections

Local-light and sound candidates can reside on logical type sections, including building or object types. Examples seen in public tooling include:

- LightVisibility;
- LightIntensity;
- LightRedTint;
- LightGreenTint;
- LightBlueTint;
- HasSpotlight;
- active, idle, damage, deploy, ambient, or weather sound references in version-specific profiles.

These sections are Rules/type definitions. Their placement records are separate scenario inputs.

### Placement sections

Buildings, terrain objects, animations, overlays, and other placement records identify instances and initial authored state. They generally do not redefine type-level lighting or sound fields.

The binder may produce:

```text
PlacedObjectReference
+ bound ObjectTypeDescriptor
+ local-light candidate
+ authored placement-state candidate
```

It does not instantiate a light or sound emitter.

### `[Map] Theater`

Provides a raw logical Theater token. It is not part of `[Lighting]`, and it does not imply a numeric lighting profile.

Theater binding can later provide:

- control INI identity;
- ISO palette role;
- unit palette role;
- TMP extension role;
- profile provenance.

Unknown Theater does not invalidate raw environment parsing.

### Visibility and session configuration

Fog/Shroud settings can also be affected by:

- multiplayer dialog defaults;
- game-mode configuration;
- client/lobby options;
- session policy;
- Trigger actions;
- savegame runtime state.

These are separate layers and must not be silently folded into map-authored metadata.

## Ordered composition boundary

The existing ordered INI composition provides a candidate source chain:

```text
ra2
→ ra2md
→ expandmd01..99
→ loose content
→ game-mode layer where explicitly configured
→ map-local Rules layer where explicitly classified
```

Environment composition must be section- and key-aware.

Examples:

- `[Lighting]` in the scenario is scenario metadata, not a Rules override;
- `[AudioVisual]` in a scenario can be a map-local Rules override only under an explicit composition policy;
- a building type section containing LightIntensity is type data;
- a placed building record is not a type override;
- `[SpecialFlags]` remains scenario metadata even when a key resembles a Rules option;
- client game options do not become authored map values.

## Duplicate policy

The raw document retains all duplicates. A later explicit policy can produce candidates such as:

```text
FirstPhysicalOccurrence
LastPhysicalOccurrence
FirstValidParsedOccurrence
LastValidParsedOccurrence
ProfileSpecificEditorBehavior
Unresolved
```

The selected candidate must preserve:

- the winning occurrence;
- suppressed occurrences;
- raw values;
- diagnostics;
- evidence grade.

Default project policy does not choose last-wins merely because a dictionary-based tool does.

## Map-local Rules policy

Recommended type:

```text
ScenarioEnvironmentLocalCompositionPolicy
```

It classifies each map section as one of:

```text
ScenarioEnvironmentMetadata
ScenarioGeneralMetadata
TriggerGraphData
PlacementData
MapLocalRulesDefinition
EditorPrivateMetadata
ClientPrivateMetadata
Unknown
```

Rules composition is only applied to `MapLocalRulesDefinition` sections. Unknown sections remain raw and unclaimed.

## Resource-resolution boundary

Logical references are produced before resource discovery:

```text
ThemeRaw → LogicalThemeReference
SoundRaw → LogicalSoundReference
SpeechRaw → LogicalSpeechReference
MovieRaw → LogicalMovieReference
ObjectTypeRaw → LogicalObjectTypeReference
TheaterRaw → LogicalTheaterReference
```

Resource discovery can later return zero, one, or many candidates with provenance. Missing resources do not rewrite raw references or cause subsequent registry IDs to move.

## Trigger boundary

The environment subsystem receives:

```text
ActionOpcodeRaw
ActionParametersRaw
EditorActionDescriptorCandidate
VersionProfile
EvidenceGrade
```

It may emit:

```text
EnvironmentCommandCandidate
```

It never:

- springs the Trigger;
- mutates active lighting;
- starts/stops a storm;
- reveals cells;
- plays sound;
- fades the screen;
- schedules a timer.

## Presentation versus simulation ownership

| Input | Environment parser | Future presentation | Future simulation/session |
|---|---|---|---|
| Ambient/RGB/Level/Ground | retain raw and candidates | calculate/render selected profile | only if a gameplay mechanic explicitly consumes it |
| Ion/Weather fields | retain alternate profile | transition visuals | own storm lifetime/damage/radar effects |
| Theme/Sound | retain logical reference | none | audio scheduler/session trigger decides playback |
| Local-light type fields | bind logical descriptor | create renderer light | own active/damaged/powered state if relevant |
| Fog/Shroud metadata | retain authored value | render visibility result | own LOS/exploration/current visibility |
| Weather capability flag | classify capability | prepare effect resources | decide activation and deterministic state |

## Evidence-grade rules

- Official editor source confirms editor fields, labels, and authoring behavior only.
- Independent tools confirm their own parsers, models, importers, or renderers.
- Community documentation remains `CommunityDocumented`.
- Ares/Phobos features remain extension profiles.
- Future sanitized observations remain `ObservedByFutureProjectBaselineAudit`.
- Project decisions such as no fallback or raw preservation are `ConfiguredForProjectPolicy`.

## Roundtrip boundary

Lossless roundtrip may require retaining:

- duplicate `[Lighting]` sections;
- duplicate fields;
- numeric spelling;
- locale commas;
- invalid values;
- unknown environment keys;
- raw SpecialFlags booleans;
- media ID casing;
- Trigger raw parameters;
- extension fields;
- editor/private sections.

A canonical editor rewrite is a separate operation and must not be the default writer.
