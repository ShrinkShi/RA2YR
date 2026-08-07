# Implementation boundaries and candidate Core API

> **Source notice:** ChatGPT Web public-source research. No local `ProjectBaseline` access. Not a Codex artifact. No GPL or unclear-license code was copied, translated, mechanically rewritten, or ported. `code_imported: false`.

## 1. Architectural objective

The Core should transform lossless INI input into a raw-preserving, immutable, declarative scenario graph. It should not execute that graph.

```text
INI bytes / bounded stream
→ lossless INI document
→ scenario section occurrences
→ raw records and tokens
→ profile-scoped structural views
→ identities and reference candidates
→ consistency analysis
→ immutable graph result
```

A later runtime consumes the graph through explicit interfaces.

## 2. Assembly boundaries

Suggested assemblies or namespaces:

```text
RA2YR.Core.Ini
RA2YR.Core.Scenario.Raw
RA2YR.Core.Scenario.Layout
RA2YR.Core.Scenario.Graph
RA2YR.Core.Scenario.Catalogs
RA2YR.Core.Scenario.Diagnostics
RA2YR.Runtime.ScenarioExecution      // future, not this milestone
RA2YR.UnityAdapters                  // future, not Core
```

Core must not reference `UnityEngine`.

## 3. Primary document model

```text
ScenarioTriggerDocument
- SourceProvenance
- SectionOccurrences
- Triggers
- Tags
- EventLists
- ActionLists
- TeamTypes
- TaskForces
- ScriptTypes
- AITriggerTypes
- VariableDefinitions
- RawUnclassifiedRecords
- Diagnostics
```

The document is raw-preserving. It is not the runtime state container.

## 4. Raw identity model

```text
ScenarioIdentity
- Kind
- RawId
- ExactKeyOccurrence
- NormalizedCandidates[]
- SourceLayer
- SourceOrder
- DuplicateGroupId?
```

Identity kinds are explicit. The same raw string may exist in multiple domains without collision.

## 5. Raw record types

### Trigger and Tag

- `ScenarioTriggerRaw`
- `ScenarioTagRaw`

### Event and Action

- `ScenarioEventListRaw`
- `ScenarioEventRaw`
- `ScenarioActionListRaw`
- `ScenarioActionRaw`
- `ScenarioParameterRaw`

### Team AI

- `TeamTypeRaw`
- `TaskForceRaw`
- `TaskForceEntryRaw`
- `ScriptTypeRaw`
- `ScriptStepRaw`
- `AITriggerTypeRaw`

Every raw record stores original value text, tokens, source line/occurrence, empty fields, extra fields, and parse diagnostics.

## 6. Token model

```text
ScenarioTokenRaw
- RawText
- Index
- IsEmpty
- LeadingWhitespace
- TrailingWhitespace
- SourceSpan
- NumericCandidates
```

Tokenization does not trim or discard empty fields.

Quoted-token behavior remains profile-specific because stock evidence is insufficient. Unknown quote characters remain ordinary raw text under the default profile.

## 7. Layout profiles

```text
TriggerLayoutPolicy
EventLayoutPolicy
ActionLayoutPolicy
TeamTypeLayoutPolicy
TaskForceLayoutPolicy
ScriptTypeLayoutPolicy
AITriggerLayoutPolicy
ExtensionProfilePolicy
```

A layout profile specifies structural expectations, not execution behavior.

Example profile metadata:

```text
ProfileId
GameFamily
GameVersionRange
ExtensionId
ExpectedFields
TupleShape
CountContract
KnownSentinels
BooleanSpellings
EvidenceGrade
Sources
```

Profiles are supplied explicitly. No trial-until-success probing.

## 8. Opcode catalog model

```text
ScenarioOpcodeCatalog
- EventDescriptors
- ActionDescriptors
- ScriptActionDescriptors
- ProfileId
- Provenance

ScenarioOpcodeDescriptor
- Domain
- NumericValue
- DisplayNameCandidates
- ParameterSlots
- VersionRange
- ExtensionId
- EvidenceGrade
```

Catalogs contain declarative facts and labels only. They do not include delegates, callbacks, or switch-based execution logic.

Trigger Event, Trigger Action, and Script Action catalogs are separate types.

## 9. Parameter descriptors

```text
ParameterSlotDescriptor
- SlotIndex
- CandidateKinds
- ReferenceTargetKinds
- SentinelProfile
- NumericRangeCandidate
- IsUnusedCandidate
- EvidenceGrade
```

Unused is a semantic candidate. The raw slot is always preserved.

## 10. Reference graph

```text
ScenarioReferenceGraph
- Nodes
- Edges
- DuplicateIdentityGroups
- StronglyConnectedComponents
- ResolutionDiagnostics

ScenarioReferenceEdge
- SourceIdentity
- SourceFieldPath
- RawTarget
- CandidateTargetKinds
- CandidateTargets
- SelectedTarget
- ResolutionState
- EvidenceGrade
```

Resolution is deterministic under an explicit policy. Ambiguity is a valid result.

## 11. Reference policy

```text
ReferenceResolutionPolicy
- CasePolicy
- SentinelProfiles
- SourceLayerPrecedence
- GlobalLocalComposition
- DuplicateTargetPolicy
- MissingTargetPolicy
```

Default duplicate policy is `Ambiguous`, not first/last wins. Missing targets remain edges.

## 12. Variable model

```text
ScenarioVariableReference
- ScopeRaw
- ScopeCandidates
- IdRaw
- NameCandidate
- ValueTypeCandidates
- OperationCandidate
- ResolutionState
```

Variable definitions and references remain declarative. Current values belong to future execution/save-state modules.

## 13. Diagnostics

```text
ScenarioGraphDiagnostic
- Code
- Severity
- Family
- SourceLocation
- Identity
- TokenIndex
- RawSummary
- EvidenceGrade
- RelatedDiagnostics
```

Suggested code families:

- duplicate identity;
- case collision;
- malformed count;
- tuple truncation;
- extra tokens;
- unknown opcode;
- numeric overflow;
- invalid boolean;
- missing reference;
- ambiguous reference;
- cycle;
- extension profile required;
- budget exceeded;
- input made no progress.

Diagnostics must not contain copyrighted map text in sanitized audit output.

## 14. Read limits

```text
ScenarioGraphReadLimits
- MaxSectionOccurrences
- MaxRecordsPerFamily
- MaxTokensPerRecord
- MaxTokenCharacters
- MaxTotalTokenCharacters
- MaxDeclaredEvents
- MaxDeclaredActions
- MaxReferenceEdges
- MaxTaskForceEntries
- MaxScriptSteps
- MaxIdentityLength
- MaxDiagnostics
```

All count arithmetic is checked before allocation.

## 15. Input modes

The same parser state machine must support:

- `ReadOnlyMemory<byte>` or character memory;
- seekable Stream;
- non-seekable Stream where practical;
- deliberately short-read Stream;
- bounded MIX entry window.

The MIX layer only provides bytes, bounds, and provenance. It does not select scenario layouts or execute references.

## 16. No-progress protection

Every loop must prove progress through one of:

- input offset advanced;
- record consumed;
- bounded state transition completed;
- structured failure returned.

A Stream returning zero before EOF is handled according to the Stream contract/profile and cannot cause an infinite loop.

## 17. Result models

```text
ScenarioGraphParseResult
- RawDocument
- StructuralViews
- ReferenceGraph
- ConsistencyAnalysis
- Diagnostics
- IsStructurallyComplete
- IsSemanticallyInterpreted
- IsExecutionEligibleCandidate

ScenarioGraphConsistencyAnalysis
- OrphanEventLists
- OrphanActionLists
- DanglingTags
- DuplicateIds
- ReferenceCycles
- CountMismatches
- UnknownOpcodes
- MissingTeamComponents
- VariableReferenceIssues
```

Execution eligibility is only a candidate flag. It never executes the graph.

## 18. Roundtrip descriptor

```text
ScenarioGraphRoundtripDescriptor
- OriginalSectionOrderPreserved
- DuplicateSectionsPreserved
- DuplicateKeysPreserved
- RawTokensPreserved
- UnknownOpcodesPreserved
- CountMismatchPreserved
- UnusedSlotsPreserved
- ExtensionFieldsPreserved
- CanonicalizationApplied
- ByteIdenticalCandidate
```

Lossless identity, semantic identity, editor reopen, runtime acceptance, and gameplay equivalence are different properties.

## 19. Synthetic fixtures

Synthetic test builders must not call production:

- CSV tokenizer;
- count/tuple parser;
- opcode descriptor lookup;
- ID normalization;
- reference-resolution algorithm.

Fixtures should be constructed independently from written specifications to avoid self-confirming tests.

## 20. Future executor interface

A future executor might accept:

```text
IScenarioExecutionContext
- SimulationClock
- WorldQueries
- HouseState
- ObjectRegistry
- VariableState
- SaveState
- Difficulty
- DeterministicCommandSink
```

And expose deterministic commands rather than direct Unity calls.

It remains outside this research and outside initial parser implementation.

## 21. Prohibited dependencies

Core must not depend on:

- UnityEngine;
- scene objects;
- coroutines;
- pathfinding;
- rendering;
- audio/video playback;
- AI behavior trees;
- network transport;
- savegame implementation;
- SHP/VXL/HVA/TMP/palette readers;
- filesystem probing to resolve IDs.

## 22. Policy serialization

For reproducibility, graph output records:

- selected layout profiles;
- opcode catalog versions;
- extension profiles;
- normalization policies;
- sentinel policies;
- source composition policy;
- evidence grades.

A result without policy provenance is unsuitable for compatibility promotion.

## 23. Fail-closed rules

Default fail-closed or ambiguous outcomes:

- duplicate identity with referenced target;
- count arithmetic overflow;
- truncated required tuple;
- unsupported explicit extension profile;
- budget excess;
- invalid input-state progress;
- contradictory normalization winners.

Unknown opcode alone does not erase the raw record; it blocks semantic/execution eligibility.

## 24. Implementation sequence candidate

Future implementation order, not executed here:

1. consume existing lossless INI model;
2. implement raw family collectors;
3. implement lossless token views;
4. add profile-scoped structural interpretation;
5. add identity graph without opcode semantics;
6. add declarative catalogs;
7. add consistency analysis;
8. add sanitized audit tooling;
9. only later design executor interfaces.

No compatibility status changes should occur from documentation alone.
