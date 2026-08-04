> **Source notice:** This document was prepared by **ChatGPT Web** from public sources only. ProjectBaseline was not read. This is not a Codex artifact. GPL and unclear-license implementations are reference-only; no code was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

# Implementation boundaries

## Proposed Core models

```text
ProductionRulesDocument
TypeRegistryEntryRaw
ProducibleTypeRaw
ProducibleTypeDescriptor
FactoryCapabilityDescriptor
ProductionCategory
PrerequisiteExpressionRaw
PrerequisiteBindingResult
TechnologySnapshot
ProductionAvailabilityQuery
ProductionAvailabilityResult
BuildLimitDescriptor
CostDescriptor
BuildTimeDescriptor
ProductionQueueDescriptor
ProductionQueueEntry
ProductionProgressDescriptor
FactoryAssignmentCandidate
CompletionCandidate
PlacementRequest
PlacementResult
ExitDescriptor
TypeTransformationCandidate
SidebarEntryDescriptor
ProductionDiagnostic
ProductionReadLimits
ProductionConsistencyAnalysis
ProductionRoundtripDescriptor
```

Supporting candidates:

```text
OwnershipAvailabilityDescriptor
FactoryRuntimeSnapshot
PowerProductionSnapshot
CreditsTransactionCandidate
StableProductionIdentity
PlacementReservationCandidate
ProductionCommand
ProductionEvent
```

## Explicit policies

```text
TypeRegistryPolicy
FactoryBindingPolicy
OwnershipAvailabilityPolicy
PrerequisitePolicy
TechLevelPolicy
BuildLimitPolicy
CostPolicy
BuildTimePolicy
FactorySpeedPolicy
ProductionQueuePolicy
CompletionPolicy
PlacementPolicy
ExitPolicy
CaptureProductionPolicy
PowerProductionPolicy
SidebarPolicy
ProductionDeterminismPolicy
ProductionOrderingPolicy
ProductionTransactionPolicy
ProductionRoundtripPolicy
```

## Core constraints

- no `UnityEngine` references;
- immutable/read-only raw records;
- raw and derived values stored separately;
- serializable evidence grades;
- structured diagnostics carrying source occurrence;
- checked arithmetic;
- bounded registry, token, group, factory, queue and placement counts;
- deterministic collections;
- no hidden global runtime state;
- no UI or actor creation;
- no asset loading from semantic binders.

## Input equivalence

One parser state machine and one limits model serve:

- `ReadOnlyMemory<byte>`;
- seekable Stream;
- short-read Stream;
- exact MIX window.

Input mode must not change duplicate handling, tokenization, ordering or diagnostics.

## Suggested limits

```text
MaxRegistryFamilies
MaxRegistryEntriesPerFamily
MaxTypeDefinitions
MaxFieldsPerDefinition
MaxPrerequisiteTokens
MaxPrerequisiteGroups
MaxFactoriesPerType
MaxCategoriesPerFactory
MaxQueueEntries
MaxReadyPlacements
MaxFoundationCells
MaxExitCandidates
MaxDiagnostics
MaxInputBytes
```

Limit failures are explicit and cannot return partial semantic success.

## Availability result

```text
ProductionAvailabilityResult
- VisibilityCandidate
- IsRequestableCandidate
- BlockerSet[]
- UnknownReasons[]
- MatchingFactoryCandidates[]
- BuildLimitSnapshot
- CostCandidate
- TimeCandidate
- Evidence[]
- Diagnostics[]
```

UI adapters may simplify presentation but cannot rewrite this result.

## Roundtrip contract

A future writer preserves:

- registry gaps and original key spelling;
- duplicate sections and keys;
- unknown Owner values;
- raw prerequisite punctuation and empty tokens;
- invalid TechLevel and BuildLimit spelling;
- numeric spelling for Cost and time fields;
- map-local override provenance;
- unknown and extension fields;
- original section/occurrence ordering in lossless mode.

Canonicalization and repair are opt-in and separate from lossless output.

## Synthetic-test independence

Fixtures and oracles must not reuse production:

- prerequisite satisfaction;
- BuildLimit counting;
- cost/time formulas;
- queue ordering;
- placement validity;
- exit selection;
- transaction ordering.

## Explicit non-implementation

This dossier adds no parser, evaluator, queue, timer, credits transaction, spawn, placement, deployment, power, capture, sidebar, AI, `GameObject`, `Button`, `ProgressBar` or `Tilemap`.

## Source and license boundary

EA editor, OpenRA and extension implementations are reference-only. No public switch, queue algorithm, prerequisite evaluator, formula or UI code was copied or mechanically converted. `code_imported: false`.
