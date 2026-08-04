# Implementation Boundaries

> **Source notice:** ChatGPT Web public-source research. ProjectBaseline was not read. Not a Codex artifact. No GPL or unclear-license code was copied, translated, mechanically rewritten, or ported. `code_imported: false`.

## Scope

This file proposes Core-facing models, explicit policies, diagnostics, read limits, dependency boundaries, and future adapters. It does not provide code or pseudocode derived from external implementations.

## Architecture rule

```text
Lossless INI document
→ raw environment document
→ evidence-graded semantic candidates
→ logical reference graph
→ consistency analysis
→ future presentation/audio/simulation/session adapters
```

Core has no `UnityEngine` reference.

## Recommended models

### `ScenarioEnvironmentDocument`

Aggregate root containing raw environment inputs and derived analyses.

Candidate members:

```text
SourceDocumentReference
LightingRaw
SpecialEnvironmentFlagsRaw
ThemeReferences
SoundReferences
MediaReferences
VisibilityMetadataRaw
LocalLightSourcesRaw
EnvironmentCommands
ReferenceGraph
ConsistencyAnalysis
Diagnostics
RoundtripDescriptor
```

It contains no current runtime state.

### `ScenarioLightingRaw`

Retains all `[Lighting]` section occurrences and field occurrences.

```text
SectionOccurrences[]
FieldOccurrences[]
NormalProfileCandidate
IonProfileCandidate
DominatorProfileCandidate
UnknownFields[]
DuplicateAnalysis
Diagnostics[]
```

### `ScenarioLightingFieldRaw`

```text
RawKey
RawValue
SourceSpan/Occurrence
NumericCandidates[]
SelectedNumericCandidate?
VersionApplicability[]
Evidence[]
Diagnostics[]
```

Raw text remains authoritative for roundtrip.

### `ScenarioLightingProfileCandidate`

```text
ProfileKind
FieldsByRole
Completeness
DefaultCandidates[]
CompositionCandidates[]
ColorSpaceCandidates[]
LayerCandidates[]
Evidence[]
Diagnostics[]
```

A profile can be partial or unresolved.

### `ScenarioLightingDescriptor`

A policy-selected logical descriptor suitable for downstream consumption. It still contains semantic values and provenance, not final GPU colors.

```text
SelectedProfile
ExactSemanticValues
CompositionProfile
ColorSpaceProfile
LayerPolicy
ClampPolicy
SourceTrace
EvidenceGrade
Diagnostics[]
```

### `ScenarioColorSpaceProfile`

Serializable profile identifying the selected logical color space or unresolved state.

### `ScenarioLightingCompositionProfile`

Serializable strategy identifier and parameters. It is not an executable implementation in the research dossier.

### `ScenarioEnvironmentTimeRaw`

Retains static/day/night/cycle evidence from Basic, Lighting, campaign, editor metadata, and Trigger references.

### `ScenarioWeatherCapabilityRaw`

Raw authored weather/Ion capability candidate with source and profile.

### `ScenarioWeatherStateCandidate`

Declarative initial/runtime-state candidate. It is not active state.

### `ScenarioSpecialEnvironmentFlagsRaw`

Contains only environment-related projections of `[SpecialFlags]` while retaining source references to the full lossless section.

### `ScenarioThemeReferenceRaw`

Raw Theme occurrence with exact spelling and provenance.

### `ScenarioSoundReferenceRaw`

Raw logical sound/speech/EVA occurrence with registry-domain candidate.

### `ScenarioMediaReference`

```text
RawReference
LogicalIdCandidate
RegistryKind
RegistryEntryCandidates[]
ResourceCandidates[]
Winner?
SuppressedCandidates[]
Evidence[]
Diagnostics[]
```

No resource bytes are loaded.

### `ScenarioLocalLightSourceRaw`

Type-level local-light fields and raw numeric candidates.

### `ScenarioLocalLightBindingResult`

Connects an object placement candidate, type descriptor, and logical local-light candidate without creating a renderer object.

### `ScenarioVisibilityMetadataRaw`

Retains FogOfWar/Shroud-authored metadata and source candidates. Contains no grid.

### `ScenarioEnvironmentCommandCandidate`

```text
TriggerReference
ActionOpcodeRaw
ActionParametersRaw
EditorDescriptorCandidate
CommandKindCandidate
TargetDomainCandidate
LogicalReferenceCandidates[]
VersionProfile
Evidence[]
Diagnostics[]
```

No execution method is part of this raw/candidate type.

### `ScenarioEnvironmentReferenceGraph`

Logical directed graph among:

- Lighting profiles;
- Theater/palette roles;
- SpecialFlags capabilities;
- Trigger commands;
- Theme/Sound/Media registries;
- object types and placements;
- visibility/session candidates.

Graph edges retain provenance and are not used to mutate raw nodes.

### `ScenarioEnvironmentDiagnostic`

Structured diagnostic with:

```text
Code
Severity
Category
MessageTemplateId
SourceReference
RelatedReferences[]
RawValueReference?
PolicyReference?
EvidenceGrade
```

No diagnostic requires publishing protected map content.

### `ScenarioEnvironmentReadLimits`

Budgets for sections, fields, references, commands, raw lengths, graph nodes/edges, numeric candidates, and diagnostics.

### `ScenarioEnvironmentConsistencyAnalysis`

Read-only analysis of missing, partial, conflicting, dangling, or cross-profile data.

### `ScenarioEnvironmentRoundtripDescriptor`

Separates:

```text
LosslessIniIdentity
RawEnvironmentIdentity
SemanticDescriptorIdentity
CanonicalEditorRewrite
ClientSessionOverride
RuntimeState
SavegameState
VisualEquivalence
AudioEquivalence
OriginalRuntimeAcceptance
```

## Explicit policies

### `LightingFieldPolicy`

- known-field registry;
- version applicability;
- unknown-field handling;
- duplicate occurrence visibility.

### `LightingNumericPolicy`

- decimal syntax;
- exponent support;
- locale-comma candidates;
- finite-value requirements;
- exact decimal representation;
- no implicit clamp.

### `LightingCompositionPolicy`

Selects an evidence-graded composition profile. No auto-probing by screenshot or output histogram.

### `LightingColorSpacePolicy`

Selects/records indexed, fixed-point, linear/gamma, editor-preview, or target-engine candidates.

### `TimeOfDayPolicy`

Resolves static style, authoring preset, Trigger cycle, campaign/client evidence, or unresolved state.

### `WeatherProfilePolicy`

Classifies weather capability, alternate profile, command, simulation, visual, audio, and extension boundaries.

### `SpecialEnvironmentFlagsPolicy`

Interprets boolean candidates and version applicability without owning non-environment SpecialFlags behavior.

### `ThemeBindingPolicy`

Selects registry and case policy, returning zero/multiple candidate diagnostics.

### `SoundBindingPolicy`

Distinguishes Sound, Speech/EVA, ambient, object, weather, and client references.

### `LocalLightBindingPolicy`

Binds type/placement/state candidates without target-engine unit conversion unless explicitly requested.

### `VisibilityMetadataPolicy`

Separates map-authored metadata from lobby/session/savegame visibility.

### `EnvironmentCommandPolicy`

Classifies raw Trigger actions by environment domain without execution.

### `EnvironmentRoundtripPolicy`

Controls identity-preserving versus explicit canonical output.

## Parsing boundaries

### INI ownership

The environment reader receives an already parsed lossless INI document. It does not tokenize the INI itself and does not change existing ordered composition.

### Stream ownership

When a future document reader accepts raw input, the same state machine must support:

- `ReadOnlyMemory<byte>` or equivalent memory input;
- seekable Stream;
- non-seekable Stream;
- short-read Stream;
- bounded MIX window Stream.

No code path may assume a single read fills a buffer.

### Encoding

Raw text decoding follows the existing INI layer. Environment parsing must not independently reinterpret bytes or replace invalid characters.

## Checked arithmetic and budgets

Use checked arithmetic for:

- occurrence indexes;
- source-span lengths;
- section/field totals;
- graph node/edge totals;
- command parameter counts;
- allocation-size calculations.

Suggested configurable limits:

```text
MaxEnvironmentSections
MaxLightingSections
MaxFieldsPerSection
MaxRawKeyChars
MaxRawValueChars
MaxNumericCandidatesPerField
MaxMediaReferences
MaxLocalLightTypes
MaxPlacementBindings
MaxEnvironmentCommands
MaxReferenceGraphNodes
MaxReferenceGraphEdges
MaxDiagnostics
```

Exceeding limits produces explicit diagnostics and failure/partial-document status according to policy. It never loops indefinitely or allocates from untrusted counts without bounds.

## No-progress protection

Every streaming/token iteration must verify that input position, output position, or state changes. Repeated no-progress states terminate with `NoProgressDetected`.

## Raw versus derived immutability

Derived binding does not mutate raw objects.

```text
Raw document
→ immutable semantic result
→ immutable consistency analysis
```

Unknown numeric text, unknown media IDs, and unknown commands remain available after failed binding.

## Evidence serialization

Every selected interpretation carries:

```text
EvidenceGrade
SourceIds[]
VersionProfile
PolicyId
ConflictIds[]
```

This allows future audits to distinguish project configuration from externally confirmed behavior.

## Presentation adapter boundary

Future presentation adapters may consume:

- selected Lighting descriptor;
- palette-role binding;
- local-light descriptors;
- visibility result from simulation;
- active weather state from simulation;
- environment commands from an authoritative scheduler.

They may create target-engine resources but cannot rewrite Core raw data.

## Audio adapter boundary

Future audio adapters may consume logical media references and runtime commands, resolve actual assets, and schedule playback. Audio playback does not affect semantic parsing success.

## Simulation/session boundary

Future simulation/session owns:

- active weather state and deterministic timing;
- storm damage/radar effects;
- visibility/exploration grids;
- Trigger scheduling and execution;
- object powered/damage/active state;
- player-relative state;
- lobby overrides;
- savegame/replay state.

## Unity boundary

A Unity adapter may eventually create:

- shader/material parameters;
- palette textures;
- 2D/custom lights;
- audio clips/sources;
- particles;
- visibility masks;
- post-processing state.

None of these types appear in Core public APIs.

## Synthetic fixtures

Tests use synthetic values and formulas that do not duplicate production formulas from external code. Fixtures should avoid copied preset tuples, object names, Trigger sequences, or real media identifiers.

## Consistency analysis categories

```text
LightingSectionAbsent
LightingDuplicateConflict
NumericProfileAmbiguous
ProfilePartial
CompositionUnresolved
PaletteBindingMissing
WeatherCapabilityStateMismatch
ThemeRegistryUnresolved
SoundRegistryUnresolved
LocalLightBindingDangling
VisibilitySourceConflict
EnvironmentCommandUnknown
RoundtripRisk
BudgetExceeded
```

No analysis automatically repairs the document.

## Failure model

Recommended result shape:

```text
ScenarioEnvironmentParseResult
- Status
- Document?
- Bytes/TextConsumed
- Diagnostics[]
- LimitState
```

Possible status values:

```text
Success
SuccessWithDiagnostics
InvalidInput
LimitExceeded
UnsupportedProfile
Ambiguous
TruncatedInput
InternalInvariantFailure
```

`SuccessWithDiagnostics` does not mean compatibility is confirmed.

## Non-goals

No implementation, code skeleton, C# signature, parser, renderer, audio engine, weather state machine, visibility grid, Trigger executor, Unity adapter, or compatibility update is created in this research.
