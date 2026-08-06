# Implementation boundaries

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明的实现仅作行为、冲突和架构参考，未复制、逐句翻译、机械改写或移植其采集、经济、AI、随机、寻路或测试代码。`code_imported: false`。

## 1. Candidate Core models

```text
ResourceEconomyDocument
ResourceOverlayRaw
ResourceOverlayBindingResult
ResourceTypeRaw
ResourceTypeDescriptor
ResourceCellRaw
ResourceCellDescriptor
ResourceStageCandidate
ResourceQuantityCandidate
ResourceValueCandidate
HarvesterCapabilityDescriptor
HarvesterCapacityDescriptor
HarvesterCargoEntry
HarvesterCargoSnapshot
HarvesterLoadFraction
HarvestTargetCandidate
HarvestCollectionDescriptor
ResourceReservationCandidate
RefineryCapabilityDescriptor
DockingSlotDescriptor
DockingRequest
DockingReservation
UnloadDescriptor
EconomySourceDescriptor
EconomyOverrideLayer
StartingCreditsCandidate
ResourceGrowthDescriptor
ResourceSpreadDescriptor
ResourceDepletionDescriptor
ResourceEconomyDiagnostic
ResourceEconomyReadLimits
ResourceEconomyConsistencyAnalysis
ResourceEconomyRoundtripDescriptor
```

Models are discussed only; none are implemented.

## 2. Explicit policies

```text
ResourceOverlayBindingPolicy
ResourceStagePolicy
ResourceQuantityPolicy
ResourceValuePolicy
HarvesterCapabilityPolicy
CargoCapacityPolicy
HarvestTargetPolicy
CollectionPolicy
ResourceReservationPolicy
RefineryBindingPolicy
DockingPolicy
UnloadingPolicy
GrowthPolicy
SpreadPolicy
EconomyOverridePolicy
ResourceDeterminismPolicy
ResourceEconomyRoundtripPolicy
```

Each policy carries:

```text
PolicyId
Version
ProductProfile
ExtensionProvider?
EvidenceGrade
SourceReferences
Strictness
UnknownValueBehavior
ArithmeticProfile
DeterminismProfile
Limits
DiagnosticsBehavior
```

## 3. Raw/derived/runtime separation

```text
ResourceOverlayRaw != ResourceCellDescriptor
ResourceCellDescriptor != RuntimeResourceCellState
HarvesterCapacityDescriptor != HarvesterCargoSnapshot
RefineryCapabilityDescriptor != RuntimeRefineryInstance
EconomySourceDescriptor != RuntimeCreditAccount
CargoSnapshot != CargoPresentationDescriptor
```

Derived data never overwrites raw identity.

## 4. Parser boundaries

- Overlay reader only reads exact arrays.
- Rules composer preserves ordered sections/keys and provenance.
- resource binder joins registries without rewriting either source.
- harvester/refinery binders consume type descriptors only.
- no binder reads runtime actors.
- no parser reads selection/UI/camera state.
- no parser resolves lobby/session precedence.
- no parser runs Trigger or AI.

## 5. Checked arithmetic

Checked operations include:
- storage index conversion;
- resource-cell count;
- `OverlayData + offset`;
- stage/quantity conversion;
- quantity × value;
- modifier application;
- cargo sums;
- capacity comparison;
- unload amount;
- storage capacity changes;
- starting credits/carry-over;
- node/reference counts;
- aggregate audit counters.

Overflow produces a diagnostic or policy-defined failure. It never silently wraps. Saturation is allowed only in an explicit runtime policy and is not parser normalization.

## 6. Deterministic ordering

Stable ordering candidates:
- source section occurrence;
- key occurrence;
- Overlay storage index;
- logical cell identity;
- resource type registry ordinal plus source ordinal;
- stable actor identity;
- command tick and command ordinal;
- dock slot ordinal;
- economy mutation ordinal.

Forbidden ordering sources:
- hash/dictionary iteration;
- Unity instance ID;
- object address;
- frame time;
- thread scheduling;
- nonserialized RNG.

## 7. Read limits

```text
ResourceEconomyReadLimits
- MaxOverlayCells
- MaxResourceTypes
- MaxRegistryOccurrences
- MaxCandidatesPerCell
- MaxCargoTypesPerHarvester
- MaxCargoAmount
- MaxDockSlotsPerRefinery
- MaxDockRequests
- MaxReservations
- MaxPendingCollectionCommands
- MaxPendingGrowthSpreadEvents
- MaxEconomySources
- MaxDiagnostics
- MaxStringLength
- MaxNumericMagnitude
```

Limits are checked before allocation/multiplication where possible.

## 8. Diagnostics

```text
ResourceEconomyDiagnostic
- Code
- Severity
- Stage
- SourceReference
- LogicalCellReference? (internal only)
- TypeReference? (internal only)
- PolicyId
- ProductProfile
- EvidenceGrade
- NumericContext
- MessageTemplateId
```

Public audit removes linkable identifiers.

## 9. Consistency analysis

Analysis only, no repair:
- nonempty Overlay with missing data;
- resource family conflict;
- unknown/duplicate registry;
- Image/Overlay mismatch;
- stage outside profile;
- value missing/zero/negative/overflow;
- harvester capability without capacity;
- capacity without resource filter;
- refinery without dock;
- dock outside foundation relationship;
- accepted resource mismatch;
- multiple economy sources;
- growth/spread enabled without resource capability;
- presentation mapping without canonical state;
- input-mode disagreement.

## 10. Input equivalence

Identical logical input through:

```text
ReadOnlyMemory<byte>
seekable Stream
short-read Stream
exact MIX entry window
```

must yield identical:
- raw descriptors;
- source ordinals;
- family/type candidates;
- diagnostics;
- consistency analysis;
- canonical aggregate hash.

No read may cross the MIX window or assume one `Read` fills a buffer.

## 11. Synthetic fixtures

Synthetic expected values:
- use hand-written tiny Overlay/data arrays;
- use fictional resource types and values;
- do not call production stage/quantity/value formulas;
- do not call production reservation/tie-break logic;
- do not call production growth/spread RNG;
- do not reuse production economy precedence;
- manually enumerate cargo/dock transactions;
- contain no game assets or ProjectBaseline material.

## 12. Core dependency rule

Core:
- has no `UnityEngine` reference;
- creates no `GameObject`, `Sprite`, `ProgressBar`, `AudioSource`, particle, coroutine or random source;
- creates no harvester/refinery actor;
- mutates no credits, cargo or resource cell;
- invokes no pathfinding, AI or Trigger;
- exposes immutable/raw/derived descriptors and structured diagnostics.

## 13. Future adapters

Separate adapters may implement:
- deterministic simulation;
- pathfinding requests;
- unit missions/AI;
- savegame state;
- network/replay;
- Unity presentation;
- HUD/load bars;
- audio/particles;
- editor canonical export.

Adapters consume Core descriptors but may not redefine raw format facts.

## 14. Architectural acceptance

- `noEngineReferences`;
- `noUnityObjects`;
- deterministic serialization/order;
- bounded input;
- checked arithmetic;
- evidence grade serializable;
- raw/derived/runtime/UI separated;
- parser non-execution;
- no ProjectBaseline access in this research.
