# Layer and domain boundaries

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明的实现仅作行为、冲突和架构参考，未复制、逐句翻译、机械改写或移植其采集、经济、AI、随机、寻路或测试代码。`code_imported: false`。

## 1. Frozen dependency direction

```text
lossless map INI and packed arrays
→ raw Overlay and Rules registries
→ family-specific semantic candidates
→ logical resource cells
→ type capability descriptors
→ runtime protocol inputs
→ simulation state
→ presentation/UI projections
```

Dependencies only move downward. Runtime state never writes back into raw descriptors unless an explicit save/export operation is requested.

## 2. Identity domains

The following identifiers are independent:

| Domain | Meaning | Must not be conflated with |
|---|---|---|
| `OverlayStorageCoordinate` | index into 512×512 packed arrays | scenario cell or Unity position |
| `ScenarioCellCoordinate` | scenario object/trigger cell identity | Overlay storage |
| `IsoMapRawCoordinate` | authored map tile coordinate | resource storage index |
| `LogicalResourceCellId` | semantic cell after explicit mapping | raw storage index |
| `ResourceTypeId` | registry identity | Overlay ordinal |
| `OverlayTypeOrdinal` | `[OverlayTypes]` ordinal candidate | resource type |
| `VehicleTypeId` | Rules vehicle type | current harvester actor |
| `BuildingTypeId` | Rules building type | runtime refinery |
| `RuntimeActorId` | simulation actor identity | Rules type name |
| `CargoEntryId` | resource-type cargo entry | UI pip |
| `DockingSlotId` | refinery runtime slot | foundation cell |
| `PlayerEconomicAccountId` | current simulation account | House metadata section |
| `SessionOptionId` | lobby/game-mode setting | map Basic/House value |

Coordinate conversion requires an explicit profile. “能落在地图范围内”不得用于选择轴序或公式。

## 3. Raw map layer

Raw map facts:

```text
OverlayPack decoded bytes
OverlayDataPack decoded bytes
storage length and indexing profile
missing/short/invalid stream state
source fragment and chunk provenance
map-local Rules/Overlay registry contributions
duplicate and unresolved INI entries
```

The raw layer does not know ore, gems, quantity, value, cargo, refinery or credits.

## 4. Resource semantic layer

Semantic binding consumes raw arrays and composed Rules views and produces candidates:

```text
ResourceOverlayBindingResult
ResourceTypeBindingResult
ResourceStageCandidate
ResourceQuantityCandidate
ResourceValueCandidate
ResourceCellDescriptor
```

It does not:
- alter Overlay bytes;
- clamp invalid stages;
- delete cells whose Art is missing;
- choose a harvest target;
- run growth/spread;
- mutate path cost.

## 5. Type capability layer

Vehicle and building type binding produces immutable capability descriptors:

```text
HarvesterCapabilityDescriptor
HarvesterCapacityDescriptor
RefineryCapabilityDescriptor
DockingSlotTemplate
ResourceAcceptanceProfile
BuildingStorageCapacityCandidate
```

A descriptor is not a runtime actor and carries no current mission, cargo, owner, power state, destruction state or queue.

## 6. Runtime simulation layer

Future runtime state, explicitly outside this research implementation:

```text
RuntimeResourceCellState
HarvesterCargoSnapshot
HarvestReservationSnapshot
DockingReservationSnapshot
RuntimeRefineryInstance
RuntimeCreditAccount
GrowthTimerState
SpreadTimerState
DeterministicRandomState
```

This layer may consume immutable descriptors but must not mutate the raw map document.

## 7. Economy-source layer

Economy inputs retain provenance:

```text
RulesDefaultCreditsCandidate
HouseCreditsCandidate
BasicCarryOverCandidate
CampaignPersistenceCandidate
LobbyCreditsCandidate
GameModeOverrideCandidate
TriggerEconomyCommandCandidate
CrateEconomyCandidate
RefineryDeliveryCandidate
RuntimeCreditMutation
```

Precedence is not a parser concern.

## 8. Presentation layer

Presentation consumes snapshots only:

```text
ResourcePresentationDescriptor
CargoPresentationDescriptor
UIPipDescriptor
UIBarDescriptor
UnloadAnimationDescriptor
EconomyUIState
RadarResourceColorCandidate
```

The following are forbidden as authority:
- sprite frame as quantity;
- visible pile size as harvest yield;
- pip count as cargo amount;
- yellow bar pixels as simulation state;
- unload animation frame as credit-transfer tick;
- floating credits text as account value.

## 9. Movement boundary

Resource movement effects are queries into M3-R12 contracts:

```text
ResourceSurfaceCandidate
ResourceMovementCostCandidate
HarvesterEntryCapability
TemporaryResourceReservation
DockApproachMovementRequest
```

Overlay parser never:
- creates a movement node;
- marks every resource passable;
- changes resource quantity when pathfinding;
- reserves a cell;
- grants harvesters occupancy immunity.

## 10. Trigger and AI boundary

Trigger/Event/Action records retain opaque opcode and parameters. AI consumes observations:

```text
ScenarioResourceState
PlayerEconomyState
AIEconomyObservation
AIDecision
ProductionCommand
```

Trigger parser and AI observer never execute mutations in Core.

## 11. Roundtrip identities

```text
RawMapIdentity
!= SemanticResourceDescriptor
!= RuntimeResourceState
!= SavegameState
!= CanonicalEditorRewrite
!= GameplayEquivalence
```

Roundtrip preserves raw arrays, unknown values, numeric spelling, duplicate registry entries and unresolved references. Runtime depletion and cargo never rewrite the original map automatically.
