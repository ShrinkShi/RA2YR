# Core implementation boundaries

> **Source notice:** ChatGPT Web public-source research. ProjectBaseline was not read. This is not a Codex artifact. No GPL or unclear-license code was copied, translated, mechanically rewritten, or ported. `code_imported: false`.

## Scope

This document proposes candidate data and policy boundaries only. It does not implement classes, interfaces, parsers, registries, players, sessions, networks, Unity adapters, or gameplay systems.

## Architectural modules

```text
LosslessIni
ScenarioMetadata.Raw
ScenarioMetadata.Semantics
ScenarioMetadata.Binding
ScenarioMetadata.Analysis
ScenarioMetadata.Roundtrip
FutureSessionInitialization
FutureSimulation
FutureUnityAdapters
```

The first six modules are declarative Core candidates. The final three are outside this research implementation scope.

## Dependency rule

```text
Raw → Semantic candidate → Binding candidate → Analysis
```

No reverse mutation is allowed.

- semantic interpretation does not rewrite raw text;
- binding does not modify identities;
- analysis does not repair graphs;
- roundtrip does not silently canonicalize;
- future adapters do not feed guessed values back into parse results.

## Candidate models

## `ScenarioMetadataDocument`

Top-level aggregate:

```text
ScenarioMetadataDocument
- LosslessDocumentReference
- BasicRaw
- MapRaw
- SpecialFlagsRaw
- MultiplayerSettingsRaw[]
- HouseRegistryRaw[]
- CountryRegistryRaw[]
- HouseSectionsRaw[]
- CountrySectionsRaw[]
- DigestRaw[]
- EnvironmentReferences[]
- Diagnostics[]
```

It retains section occurrences and does not require a unique semantic interpretation.

## `ScenarioBasicRaw`

```text
ScenarioBasicRaw
- SectionOccurrences[]
- Properties[]
- RecognizedFieldCandidates[]
- UnknownFields[]
- DuplicateGroups[]
```

Every property stores raw key/value, source occurrence, and typed candidates separately.

## `ScenarioMapRaw`

```text
ScenarioMapRaw
- SectionOccurrences[]
- SizeOccurrences[]
- LocalSizeOccurrences[]
- TheaterOccurrences[]
- OtherProperties[]
```

## `ScenarioMapRectangleRaw`

```text
ScenarioMapRectangleRaw
- RawText
- Field0Raw
- Field1Raw
- Field2Raw
- Field3Raw
- ExtraTokens[]
- MissingTokenCount
- SourceOccurrence
```

No `UnityEngine.Rect` or world-coordinate type is used.

## `ScenarioMapGeometryDescriptor`

```text
ScenarioMapGeometryDescriptor
- SelectedSizeInterpretation?
- SelectedLocalSizeInterpretation?
- CandidateInterpretations[]
- RelationshipAnalysis
- ArithmeticStatus
- EvidenceItems[]
- Diagnostics[]
```

It may be unavailable while raw metadata remains valid.

## `ScenarioTheaterRaw`

```text
ScenarioTheaterRaw
- RawText
- NormalizedCandidates[]
- SourceOccurrence
```

## `ScenarioTheaterBindingResult`

```text
ScenarioTheaterBindingResult
- RawTheater
- LogicalTheaterCandidate?
- SelectedProfileId?
- ControlIniLogicalNameCandidate?
- TmpExtensionCandidate?
- IsoPaletteRoleCandidate?
- UnitPaletteRoleCandidate?
- ResolutionState
- EvidenceGrade
- Diagnostics[]
```

No file, palette, TMP, or texture is loaded.

## `ScenarioHouseRegistryRaw`

```text
ScenarioHouseRegistryRaw
- SectionOccurrences[]
- Entries[]

Entry
- ListKeyRaw
- NumericOrdinalCandidate?
- LogicalNameRaw
- SourceOrder
- CollisionGroups[]
```

## `ScenarioHouseRaw`

```text
ScenarioHouseRaw
- IdentityRaw
- RegistryOccurrences[]
- SectionCandidates[]
- Properties[]
- SourceProvenance
```

## `ScenarioCountryRaw`

```text
ScenarioCountryRaw
- IdentityRaw
- RegistryOccurrences[]
- SectionCandidates[]
- ParentCountryRaw?
- SideRaw?
- Properties[]
- SourceProvenance
```

## `ScenarioHousePropertyRaw`

```text
ScenarioHousePropertyRaw
- KeyRaw
- ValueRaw
- Occurrence
- TypeCandidates[]
- SelectedInterpretation?
- EvidenceGrade
```

## `ScenarioHouseBindingResult`

```text
ScenarioHouseBindingResult
- HouseIdentity
- CountryCandidates[]
- SelectedCountry?
- SideCandidates[]
- SelectedSide?
- PropertyBindings[]
- ResolutionState
- ProvenanceTrace
- Diagnostics[]
```

No runtime House or player object is created.

## `ScenarioColorReferenceRaw`

```text
ScenarioColorReferenceRaw
- HouseIdentity
- ColorIdRaw
- RegistryCandidates[]
- ResolutionState
```

A separate binding result may identify a logical color. It never produces RGB, palettes, or remap ramps.

## `ScenarioAllianceEdgeRaw`

```text
ScenarioAllianceEdgeRaw
- SourceHouseRaw
- TargetTokenRaw
- SourcePropertyOccurrence
- TokenOccurrence
- ExactCase
- ResolutionState
```

## `ScenarioAllianceGraph`

```text
ScenarioAllianceGraph
- HouseIdentityGroups[]
- DirectedRawEdges[]
- ResolvedDirectedEdges[]
- DanglingEdges[]
- DuplicateEdgeGroups[]
- SymmetryAnalysis[]
- FixedAllianceRaw?
- Diagnostics[]
```

The graph is directed and immutable.

## `ScenarioPlayerControlRaw`

```text
ScenarioPlayerControlRaw
- BasicPlayerRaw[]
- HousePlayerControlRaw[]
- HouseHumanRaw[]
- OtherControllerCandidates[]
- Conflicts[]
```

## `ScenarioPlayerSlotDescriptor`

```text
ScenarioPlayerSlotDescriptor
- SlotIdentity
- HouseCandidates[]
- CountryCandidates[]
- ControllerKindCandidate?
- NetworkPeerCandidate?
- ColorCandidate?
- StartLocationCandidate?
- LobbyTeamCandidate?
- EvidenceItems[]
```

This is a future-session input/output model, not a parser-created runtime player.

## `ScenarioStartLocationRaw`

```text
ScenarioStartLocationRaw
- WaypointIdRaw
- ScenarioCellIdRaw
- CoordinateCandidates[]
- StartSlotCandidates[]
- SourceOccurrence
- EvidenceGrade
```

## `ScenarioMultiplayerSettingsRaw`

```text
ScenarioMultiplayerSettingsRaw
- SectionOccurrence
- SourceKindCandidate
- SourceLayer
- Properties[]
- UnknownFields[]
```

Overlapping setting names from map, Rules, game-mode, client, and lobby remain separate source records.

## `ScenarioGameModeCandidate`

```text
ScenarioGameModeCandidate
- ModeKind
- EvidenceItems[]
- Conflicts[]
- ConfidenceCandidate
```

## `ScenarioGameModeResolution`

```text
ScenarioGameModeResolution
- Candidates[]
- SelectedCandidate?
- SelectionPolicyId?
- IsAmbiguous
- Diagnostics[]
```

No content heuristic automatically selects a mode.

## `ScenarioSpecialFlagsRaw`

```text
ScenarioSpecialFlagsRaw
- SectionOccurrences[]
- PropertyOccurrences[]
- RecognizedCandidates[]
- UnknownFields[]
- DuplicateGroups[]
```

No flag behavior is executed.

## `ScenarioDigestRaw`

```text
ScenarioDigestRaw
- SectionOccurrences[]
- KeyOccurrences[]
- RawText
- ShapeCandidates[]
- VerificationStatus: NotAttempted
```

No cryptographic trust is implied.

## `ScenarioInitializationDescriptor`

```text
ScenarioInitializationDescriptor
- MetadataDocumentReference
- GeometryDescriptor?
- TheaterBinding?
- HouseIdentityGraph
- HouseStartingStateDescriptors[]
- AllianceGraph
- PlayerControlCandidates
- PlayerCountEvidence[]
- StartLocationCandidates[]
- MultiplayerSettingSources[]
- GameModeResolution
- SpecialFlagsRaw
- DigestRaw
- EnvironmentReferences[]
- Diagnostics[]
```

It is immutable and non-executable.

## `ScenarioMetadataDiagnostic`

Suggested fields:

```text
ScenarioMetadataDiagnostic
- Code
- Severity
- MessageTemplateId
- SourceLocation
- RelatedLocations[]
- RawValueReference?
- EvidenceGrade?
- PolicyId?
```

Diagnostics do not expose proprietary map content in sanitized audit output.

## `ScenarioMetadataReadLimits`

Candidate budgets:

```text
MaxSectionOccurrences
MaxKeysPerSection
MaxRawValueLength
MaxTotalMetadataTokens
MaxHouseEntries
MaxCountryEntries
MaxPropertiesPerIdentity
MaxAllianceTokensPerHouse
MaxAllianceEdges
MaxBaseNodesPerHouse
MaxWaypointReferences
MaxModeEvidenceItems
MaxDiagnostics
```

All budget arithmetic is checked.

## `ScenarioMetadataConsistencyAnalysis`

```text
ScenarioMetadataConsistencyAnalysis
- RectangleAnalysis
- TheaterAnalysis
- HouseRegistryAnalysis
- CountryBindingAnalysis
- PropertyAnalysis
- PlayerControlAnalysis
- AllianceAnalysis
- StartLocationAnalysis
- MultiplayerSettingsAnalysis
- ModeAnalysis
- SpecialFlagsAnalysis
- DigestAnalysis
- Diagnostics[]
```

Analysis never modifies source or descriptors.

## `ScenarioMetadataRoundtripDescriptor`

```text
ScenarioMetadataRoundtripDescriptor
- LosslessSourceAvailable
- SectionOrderPreserved
- DuplicateSectionsPreserved
- DuplicateKeysPreserved
- RawCasingPreserved
- NumericSpellingPreserved
- BooleanSpellingPreserved
- UnknownFieldsPreserved
- InvalidReferencesPreserved
- CanonicalRewriteProfile?
- ByteIdentityCandidate
- SemanticIdentityCandidate
```

## Explicit policy objects

## `BasicMetadataPolicy`

Defines:

- recognized key profiles;
- boolean/numeric parsing profiles;
- field applicability by game/version;
- unknown-field behavior;
- duplicate semantic-source behavior;
- editor/client extension profiles.

## `MapGeometryPolicy`

Defines:

- rectangle layout profile;
- signedness;
- zero/negative dimension handling;
- maximum read budgets;
- LocalSize containment analysis;
- overflow behavior.

It does not clamp rectangles.

## `TheaterBindingPolicy`

Defines:

- token comparison;
- allowed stock and extension profiles;
- unknown-theater handling;
- explicit editor compatibility fallbacks;
- evidence threshold.

## `HouseRegistryPolicy`

Defines:

- section-role profile;
- ordinal normalization candidates;
- duplicate/gap handling;
- listed/unlisted section analysis;
- case comparison;
- special identity profiles.

No gaps are compressed.

## `HousePropertyPolicy`

Defines:

- property catalogs by profile;
- numeric/boolean candidates;
- editor-statistics classification;
- base-node profile;
- extension handling.

## `CountryBindingPolicy`

Defines:

- global and map-local Country composition;
- ParentCountry interpretation;
- Side reference handling;
- missing reference behavior;
- duplicate resolution threshold.

No fallback to first Country occurs by default.

## `AlliancePolicy`

Defines:

- delimiter/token profile;
- exact/case-insensitive comparison candidates;
- symmetry analysis;
- self-reference classification;
- missing target behavior;
- FixedAlliance interpretation boundary.

No reverse edges are generated by default.

## `PlayerAssignmentPolicy`

Defines:

- campaign-authored Player interpretation;
- House PlayerControl/Human candidates;
- separation from session/lobby assignment;
- controller conflict reporting;
- observer and AI classification inputs.

It cannot create a player.

## `MultiplayerSettingsPolicy`

Defines:

- source-kind precedence candidates;
- map/Rules/game-mode/client/lobby separation;
- recognized setting profiles;
- invalid value handling;
- conflict reporting.

It does not apply settings.

## `StartLocationPolicy`

Defines:

- Waypoint range/profile;
- slot mapping candidates;
- geometry domain checks;
- LocalSize checks;
- IsoMap-presence checks;
- duplicate/missing start analysis;
- fixed/random start evidence.

It does not choose a start.

## `ScenarioModePolicy`

Defines:

- evidence-source weights or requirements;
- allowed classifications;
- conflict threshold;
- selection/no-selection behavior;
- extension/client mode recognition.

## `SpecialFlagsPolicy`

Defines:

- game/version field applicability;
- boolean profile;
- unknown fields;
- conflict with lobby/Rules settings;
- evidence grade.

It does not execute behavior.

## `ScenarioLocalCompositionPolicy`

Defines:

- metadata section classification;
- House instance sections;
- Country definition sections;
- allowed map-local Rules type sections;
- per-key versus whole-section composition;
- winner and suppressed provenance;
- collision behavior.

Not every map section is a Rules override.

## `ScenarioMetadataRoundtripPolicy`

Defines:

- lossless rewrite requirement;
- canonical editor profile, if explicitly requested;
- unknown-field preservation;
- duplicate preservation;
- ID/casing preservation;
- malformed-value preservation;
- source-order preservation.

## Evidence model

Every selected derived interpretation contains:

```text
EvidenceGrade
EvidenceSourceId
EvidenceLocation
ProfileId
```

Grades are serializable:

- `ConfirmedByOfficialRuntimeSource`;
- `ConfirmedByOfficialEditorSource`;
- `ConfirmedByIndependentImplementation`;
- `CommunityDocumented`;
- `ObservedByFutureProjectBaselineAudit`;
- `ConfiguredForProjectPolicy`;
- `Unresolved`.

## Core dependency restrictions

Core must not reference:

- `UnityEngine`;
- `Texture2D`, Sprite, Mesh, Material, GameObject, Camera, Light;
- lobby UI controls;
- sockets or network peers;
- runtime player classes;
- simulation House objects;
- AI managers;
- pathfinding;
- map renderers;
- media players.

## Structured failure

Parse/interpretation states are distinct:

```text
RawParseSuccess
SemanticInterpretationSuccess
UniqueBindingSuccess
ConsistencyValid
InitializationDescriptorAvailable
```

A failure at a later stage does not erase earlier raw results.

## Checked arithmetic

Checked operations include:

- rectangle addition and area;
- token/entry counts;
- edge counts;
- base-node counts;
- diagnostic accumulation;
- source offsets;
- string and allocation budgets.

Overflow produces a diagnostic and no derived value.

## Bounded allocation

Allocation is limited by policy before creating:

- section occurrence lists;
- key/token arrays;
- House/Country identity groups;
- alliance edges;
- base-node descriptors;
- mode evidence lists;
- diagnostics.

No input controls an unbounded collection.

## No-progress protection

Any streaming tokenizer/state machine must guarantee that each iteration:

- consumes input;
- emits a terminal state;
- or fails with a structured diagnostic.

No loop may retry the same offset indefinitely.

## Input-mode equivalence

The same parser state machine must serve:

- in-memory input;
- seekable Stream;
- non-seekable/short-read Stream where supported;
- bounded MIX entry window.

A MIX entry cannot select a metadata profile based on archive filename alone.

## Synthetic test independence

Synthetic fixture builders must not call production implementations for:

- rectangle formula generation;
- House ordinal normalization;
- alliance edge generation;
- Waypoint/start mapping;
- mode evidence resolution;
- boolean parsing expectations.

Expected values are encoded independently.

## Future session boundary

A future session initializer may consume:

```text
ScenarioInitializationDescriptor
RuntimeRulesDescriptor
ObjectPlacementGraph
TriggerGraph
SessionLobbySettings
NetworkPeerAssignments
DeterministicRandomSeed
```

and produce runtime players/Houses. That module is not part of the parser and must preserve deterministic, explicit precedence.

## Future simulation boundary

A future simulation module may apply:

- starting credits;
- alliances;
- SpecialFlags;
- starting units;
- player controllers;
- campaign carry-over;
- game-mode rules.

None are implemented or specified as executable algorithms here.

## Future Unity boundary

Unity adapters may later create UI, cameras, player objects, or visual environment components. They consume validated descriptors and cannot become the source of format interpretation.
