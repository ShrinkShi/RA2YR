# Refinery docking and unloading

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明的实现仅作行为、冲突和架构参考，未复制、逐句翻译、机械改写或移植其采集、经济、AI、随机、寻路或测试代码。`code_imported: false`。

## 1. Identity separation

```text
BuildingType
RefineryCapability
DockingSlotTemplate
RuntimeRefineryInstance
RuntimeDockingSlot
StorageAccount
UnloadAnimation
CreditTransfer
```

A building name containing `PROC`, `REF` or `ORE` does not create a refinery capability.

## 2. Candidate type inputs

```text
Refinery
Storage
Dock
NumberOfDocks
DockingOffset
DockingDirection
ExitCoord
ExitCell
Foundation
resource acceptance fields
power state inputs
owner/capture inputs
extension fields
```

Factory, repair facility and refinery docking must remain distinct families even if they share generic dock infrastructure.

## 3. Capability descriptor

```text
RefineryCapabilityDescriptor
- BuildingTypeReference
- AcceptedResourceTypeCandidates[]
- DockTypeCandidates[]
- SlotTemplates[]
- StorageModeCandidates[]
- CreditConversionCandidates[]
- OwnershipPolicyCandidates[]
- PowerRequirementCandidate
- ProductProfile
- Evidence
- Diagnostics
```

No current queue, owner or destroyed state is stored here.

## 4. Docking slot

```text
DockingSlotDescriptor
- SlotId
- HostAnchor
- ApproachCellCandidates[]
- DockCellCandidates[]
- ExitCellCandidates[]
- RequiredFacingCandidates[]
- FoundationRelationship
- MovementLayer
- Capacity
- Evidence
```

Docking cells and exits are not foundation cells by definition.

## 5. Runtime requests

```text
DockingRequest
- HarvesterActorId
- RefineryActorId
- CargoSnapshotRef
- RequestedDockType
- RequestTick

DockingReservation
- SlotId
- ActorId
- ReservationTick
- Expiration
- QueueOrdinal
- State

DockingApproach
- PathRequest
- RequiredFacing
- AlignmentToleranceCandidate
- OccupancyConditions
```

Foundation parser creates none of these.

## 6. State machine contract

Candidate phases:

```text
Approach
Align
Dock
BeginUnload
TransferResource
FinishUnload
Exit
ResumeHarvest
```

Every phase transition is driven by deterministic simulation conditions. Animation and sound observe transitions.

## 7. Unload descriptor

```text
UnloadDescriptor
- ResourceType
- AvailableCargoAmount
- TransferAmountCandidate
- TransferIntervalCandidate
- RefineryAcceptanceCandidate
- StorageOutcomeCandidate
- CreditValueCandidate
- ProductProfile
- Evidence
```

One-shot, bale-by-bale and per-tick behaviors are separate profiles.

## 8. Mutation separation

```text
CargoMutation
EconomicCreditMutation
PhysicalStorageMutation
AnimationState
AudioState
MissionState
DockingState
```

They may be committed together by future simulation policy, but no parser or renderer performs them.

## 9. Independent implementation evidence

OpenRA’s pinned implementation provides:
- resource stores with capacity;
- harvester unload delay/amount;
- dock host/client separation;
- refinery resource acceptance;
- optional storage vs direct cash;
- excess-storage behavior;
- per-resource value and modifiers.

This confirms that a clean implementation can separate cargo, docking, acceptance, storage and credits. It is not evidence of exact Westwood timing or units.

## 10. Edge cases

Required explicit outcomes:
- zero cargo;
- mixed cargo;
- unaccepted resource type;
- partial acceptance;
- full storage;
- missing dock;
- blocked approach;
- multiple docks;
- competing queue entries;
- refinery captured;
- refinery destroyed;
- harvester destroyed;
- power lost;
- allied refinery;
- enemy refinery;
- cancel during unload;
- save/load mid-phase;
- overflow in value conversion.

## 11. Ownership and capture

Potential rules:
- owner-only;
- allied queueing;
- allied forced docking;
- captured host transfers future deliveries;
- reservation invalidated on owner change;
- in-progress unload completes/cancels.

No default is selected without evidence.

## 12. Credit conversion

Future ordered transaction candidate:

```text
acceptedUnits
→ cargo decrement
→ physical storage or cash conversion
→ account mutation
→ earned/statistics mutation
→ presentation event
```

The order, atomicity and rollback behavior remain P0.

## 13. Presentation

Unload animation, active refinery animation, sound, particles, floating money and lighting are adapters. They never determine accepted amount or transfer tick.

## 14. No implementation

No queue, movement, reservation, dock alignment, unload animation, cargo mutation, credit mutation, Unity coroutine or audio object is implemented.
