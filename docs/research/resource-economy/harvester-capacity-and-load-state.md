# Harvester capacity and load state

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明的实现仅作行为、冲突和架构参考，未复制、逐句翻译、机械改写或移植其采集、经济、AI、随机、寻路或测试代码。`code_imported: false`。

## 1. Identity boundary

```text
VehicleType
HarvesterCapability
CurrentHarvesterInstance
CargoState
ResourceTypeFilter
HarvestMission
DockingState
UnloadState
VisualAnimationState
```

No name heuristic (`HARV`, `MINER`, `ORE`) can create a harvester capability.

## 2. Candidate Rules/Art inputs

Profile-scoped candidates include:

```text
Harvester
ResourceGatherer
Storage
PipScale
Dock
Refinery
UnloadingClass
TiberiumProof
TiberiumHeal
TiberiumRemains
Crusher
MovementZone
SpeedType
Locomotor
Owner
Prerequisite
AI fields
extension fields
```

Each property records raw token, case, source layer, selected profile, evidence and unknown behavior.

## 3. Capability descriptor

```text
HarvesterCapabilityDescriptor
- VehicleTypeReference
- IsHarvesterCandidate
- AcceptedResourceTypeCandidates[]
- RequiresGroundResourceCandidate
- CollectionAnimationRefs[]
- DockTypeCandidates[]
- UnloadingClassCandidates[]
- MovementCapabilityRef
- AIObservationTags[]
- ProductProfile
- Evidence
- Diagnostics
```

This descriptor does not contain current cargo or mission.

## 4. Capacity descriptor

```text
HarvesterCapacityDescriptor
- CapacityRaw
- CapacityUnitCandidate
- SharedCapacityCandidate
- PerTypeCapacityCandidates[]
- ZeroCapacityMeaning
- NegativeValueMeaning
- OverflowPolicy
- ProductProfile
- Evidence
```

Missing, zero and negative values remain distinct.

## 5. Cargo model

```text
HarvesterCargoEntry
- ResourceTypeId
- CanonicalAmount
- AmountUnit
- EconomicValueCandidate
- SourceEventOrdinal
- Diagnostics

HarvesterCargoSnapshot
- ActorStableId
- Entries[]
- TotalCanonicalAmount
- CapacityDescriptorRef
- IsEmptyCandidate
- IsFullCandidate
- OverflowState
- SimulationTick
```

The model supports multiple entries to avoid baking in an unproven single-resource assumption. Stock ore/gem mixed-cargo behavior remains unresolved.

## 6. Load fraction

```text
HarvesterLoadFraction
- Numerator
- Denominator
- RationalReduction
- ClampedPresentationFraction?
- OverflowDiagnostic
```

Canonical state preserves exact integers. UI may clamp display to `[0,1]`, but simulation does not silently clamp cargo.

## 7. Required distinctions

```text
AuthoredVehicleTypeCapacity
!= CurrentRuntimeCargo
!= CargoEconomicValue
!= UI PipCount
!= UI BarWidth
!= ArtLoadFrame
```

A cargo snapshot can be partial, full, over-capacity due to malformed/savegame/extension input, or unknown.

## 8. Independent implementation evidence

OpenRA at pinned revision separates:
- a `StoresResources` capacity and accepted-resource list;
- per-resource cargo dictionary;
- total content;
- harvester resource filters;
- full/empty/fullness queries;
- unload delay and unload amount;
- refinery acceptance.

This is useful architectural evidence, not stock RA2/YR behavior.

## 9. UI boundary

Canonical inputs:

```text
CanonicalCargoAmount
CanonicalCapacity
NormalizedLoadFraction
CargoTypeBreakdown
OwnerVisibilityPolicy
ObserverVisibilityPolicy
```

Presentation candidates:

```text
UIPipDescriptor
UIBarDescriptor
ArtLoadFrameCandidate
TooltipCargoDescriptor
SidebarCargoDescriptor
```

Original-game feedback may include selection pips and/or vehicle art changes depending on product/type. Exact YR behavior is evidence-scoped.

The planned project style:

```text
black outline + yellow fill load bar
```

is explicitly `ConfiguredForProjectPolicy`. It is not a format or stock-runtime fact.

## 10. Visibility

UI policy must define:
- selection-only vs always visible;
- owner-only vs allied/enemy;
- observer/replay;
- shroud/fog;
- remap/color;
- damaged/disabled state;
- mixed cargo visualization.

Core does not evaluate camera, selection or renderer state.

## 11. Save/load and scripted cargo

Future savegame/state import may provide current cargo. It must not be interpreted as authored map placement. Script-created cargo and extension over-capacity state remain runtime inputs with diagnostics.

## 12. No mutation

This research defines no:
- `AddCargo`;
- `RemoveCargo`;
- collection tick;
- full-state mission transition;
- unload loop;
- UI progress bar component.
