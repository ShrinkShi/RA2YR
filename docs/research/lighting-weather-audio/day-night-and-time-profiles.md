# Day, Night, and Time Profiles

> **Source notice:** ChatGPT Web public-source research. ProjectBaseline was not read. Not a Codex artifact. No GPL or unclear-license code was copied, translated, mechanically rewritten, or ported. `code_imported: false`.

## Scope

This file distinguishes static authored lighting, editor presets, Trigger-driven lighting changes, superweapon/weather profiles, extension engines, and a hypothetical autonomous time system.

## Strongest current conclusion

For stock RA2/YR map parsing, `[Lighting]` is best treated as static authored environment input plus alternate event profiles. No public original-runtime source examined in this research establishes a general map-level clock that automatically advances morning, afternoon, evening, and night.

Evidence grade:

- static authored values and editor presets: `ConfirmedByOfficialEditorSource`;
- autonomous stock RA2/YR day/night clock: `Unresolved`;
- Trigger-authored dynamic changes: strong editor/community candidate, but exact opcode/runtime semantics remain profile-specific;
- Ares/Phobos dynamic or superweapon-related behavior: extension-only evidence.

## Candidate time sources

Potential evidence can come from:

- `[Lighting]` normal profile values;
- alternate Ion/Weather/Dominator profiles;
- `[Basic]` metadata or mission context;
- Theater identity;
- campaign control INIs;
- Trigger Events and Actions;
- SpecialFlags;
- official editor scripts and presets;
- client metadata;
- Ares/Phobos or other extension fields;
- explicit game-mode/session configuration.

No single source is automatically authoritative.

## Official editor preset evidence

The official repository includes authoring scripts with names such as:

- Create Morning Lighting;
- Create Afternoon Lighting;
- Create Evening Lighting;
- Create Night Lighting;
- Day-Night Loop.

The preset scripts write numeric values into `[Lighting]`. This confirms:

- map authors were expected to use static value sets;
- editor scripts can rewrite Lighting fields;
- preset names are authoring concepts;
- scripts may contain trailing whitespace, leading-dot decimals, mislabeled headers, or script errors.

It does not confirm:

- a runtime `TimeOfDay` field;
- a canonical stock preset table;
- automatic interpolation;
- a real-world clock;
- a per-frame lighting controller;
- identical values across theaters or game versions.

## Official editor day/night loop evidence

The examined bundled `Day-Night Loop.fscript`:

- adds Trigger, Action, and Tag records;
- asks the author to select linked triggers/actions;
- writes initial Lighting values;
- contains raw numeric action parameters;
- relies on map Trigger execution rather than a parser-owned clock.

This is direct official-editor evidence that a dynamic-looking day/night effect can be authored as scenario logic. It is not proof that every opcode, parameter encoding, or numeric transition formula in the script is valid for all RA2/YR versions. It also does not make the editor script itself part of the stock scenario format.

Recommended interpretation:

```text
EditorPreset
→ map rewrite

EditorDayNightScript
→ authored Trigger graph + static initial Lighting

Runtime Trigger execution
→ potential dynamic environment commands
```

## Static profile model

```text
StaticLightingDescriptor
- NormalLightingProfileCandidate
- AlternateProfileCandidates[]
- AuthoringPresetEvidence[]
- SourceProvenance
- Diagnostics[]
```

The descriptor contains no clock or interpolation state.

## Time-of-day evidence model

```text
EnvironmentTimeProfileRaw
- RawSourceKind
- RawIdentifier
- RawParameters
- VersionProfile
- EvidenceGrade

TimeOfDayEvidence
- CandidateKind
- SourceReference
- Confidence/EvidenceGrade
- Conflicts[]
```

Candidate kinds:

```text
StaticDayStyle
StaticNightStyle
StaticDawnStyle
StaticDuskStyle
EditorPresetName
TriggerDrivenCycle
CampaignContextCandidate
ClientDisplayCategory
ExtensionDynamicCycle
Unknown
```

A dark or blue Lighting tuple is not automatically classified as Night. Value-based classification is only suitable for coarse sanitized audit categories and must not alter semantic binding.

## Dynamic lighting candidate

```text
DynamicLightingCandidate
- CommandSource
- TargetProfileOrFields
- TransitionRaw
- DurationRaw
- TriggerReference
- VersionProfile
- EvidenceGrade
- Diagnostics[]
```

This candidate remains declarative. It does not mutate the static descriptor.

## Trigger-driven transitions

Potential action families include:

- set or change Ambient;
- set or change Red/Green/Blue;
- set or change Level/Ground;
- activate/deactivate a linked Trigger stage;
- start/stop a weather or superweapon state;
- fade screen or adjust presentation;
- play Theme/Sound during transition.

The exact opcode and parameter formats are delegated to the Trigger dossier. M3-R10 only classifies their environment target domain.

## Time and simulation boundaries

### Presentation-only transition candidate

If a selected profile proves that a lighting transition affects only presentation:

- Core can retain exact semantic decimal endpoints;
- a renderer adapter can convert to `float`;
- interpolation can be renderer-specific if visual equivalence is the only goal;
- raw authored timing is retained.

### Simulation-affecting environment candidate

If weather, visibility, radar, weapon disabling, or damage is coupled to the transition:

- authoritative state belongs to simulation/session;
- timing must be deterministic;
- Unity frame time cannot be authoritative;
- savegame/replay state may need exact persistence;
- presentation follows simulation state rather than driving it.

## Theater boundary

Theater may influence an author's chosen static style and palette role, but Theater does not define a universal time of day.

Examples that must remain separate:

```text
Snow Theater
≠ night

Lunar Theater
≠ permanently dark by format rule

NewUrban Theater
≠ automatic evening profile
```

Unknown Theater does not prevent raw time/lighting evidence collection.

## Campaign and client context

Campaign control data or launcher databases may categorize a mission, supply UI art, or select music. Such context may offer time-style evidence but does not rewrite the map's raw `[Lighting]`.

Client-side map filters named Day, Night, Storm, or similar are client metadata unless tied to a stock field by stronger evidence.

## Extension boundary

Ares and Phobos can add or alter superweapon lighting, object effects, actions, and state transitions. Any explicit dynamic time-of-day feature from an extension receives:

```text
ExtensionProfile
Version
DocumentationRevision
EvidenceGrade = CommunityDocumented or extension-source grade
```

It is never labeled vanilla.

## No heuristic selection

Forbidden heuristics:

- classify Night because Ambient is below a threshold;
- classify Day because Red/Green/Blue are near one;
- infer time from screenshot brightness;
- infer a cycle because multiple alternate profiles exist;
- infer active storm because Ion fields exist;
- infer time from Theater;
- infer canonical preset from the official editor script name;
- choose an interpolation algorithm because it looks smooth.

## Roundtrip requirements

Future writers may need to preserve:

- exact Lighting values and spelling;
- editor-script-generated unknown fields;
- Trigger raw actions and parameters;
- duplicate Lighting occurrences;
- transition-related extension fields;
- disabled or incomplete Trigger chains;
- authoring comments/private metadata when supported.

A canonical preset rewrite is a separate explicit operation.

## Candidate policies

- `TimeOfDayPolicy` — resolves static versus dynamic evidence.
- `DynamicLightingPolicy` — classifies declarative transition candidates.
- `LightingTransitionPrecisionPolicy` — retains exact semantic endpoints and timing.
- `EnvironmentCommandPolicy` — maps Trigger actions to domains without execution.
- `EnvironmentRoundtripPolicy` — preserves authoring identity.

## Diagnostics

- `NoTimeOfDayEvidence`;
- `StaticStyleOnly`;
- `EditorPresetEvidenceOnly`;
- `TriggerCycleCandidate`;
- `TriggerCycleIncomplete`;
- `DynamicProfileUnsupported`;
- `TimeClassificationConflict`;
- `ExtensionTimeProfileInVanillaMode`;
- `LightingValueHeuristicRejected`;
- `TransitionTimingUnresolved`;
- `SimulationCouplingUnresolved`.

## Non-goals

No time manager, clock, interpolation, coroutine, state machine, Trigger execution, fade, shader, Unity animation, or dynamic day/night loop is implemented.
