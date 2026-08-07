# Harvest targeting and collection

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明的实现仅作行为、冲突和架构参考，未复制、逐句翻译、机械改写或移植其采集、经济、AI、随机、寻路或测试代码。`code_imported: false`。

## 1. Declarative contract

```text
HarvestTargetCandidate
→ HarvestApproachCandidate
→ HarvestReservationCandidate
→ HarvestCollectionCommand
→ HarvestCollectionResult
```

These are future simulation contracts. Parsers create none of them.

## 2. Target candidate

```text
HarvestTargetCandidate
- HarvesterActorId
- LogicalResourceCellId
- ResourceTypeCandidate
- QuantitySnapshot
- TargetSelectionReason
- SourceOrder
- Evidence
- Diagnostics
```

Target selection cannot rely on rendered resource frames.

## 3. Eligibility inputs

A future query may require:
- resource cell exists in runtime state;
- quantity greater than zero;
- harvester accepts resource type;
- movement layer and cell are reachable;
- cell is not excluded by scenario policy;
- harvester is not full;
- target visibility/shroud policy permits command;
- reservation policy permits claim.

Overlay parser never performs these checks.

## 4. Approach candidate

```text
HarvestApproachCandidate
- TargetCell
- CandidateActorCells[]
- RequiredFacingCandidates[]
- CollectionRadiusCandidate
- MovementLayer
- PathRequestDescriptor
- OccupancyRequirements
- Diagnostics
```

Adjacent-cell harvesting, same-cell harvesting and facing constraints are profile-scoped. No assumption is frozen as vanilla.

## 5. Reservation

```text
ResourceReservationCandidate
- ResourceCellId
- RequestingActorId
- RequestedAmount
- CreationTick
- ExpirationCandidate
- PriorityComponents
- StableTieBreakKey
- Status
```

Reservation is not resource deletion and not path occupancy.

Conflict outcomes:

```text
Granted
PartiallyGranted
Rejected
Expired
TargetDepleted
ActorInvalid
Unknown
```

## 6. Collection command

```text
HarvestCollectionCommand
- ActorId
- TargetCellId
- ResourceTypeId
- RequestedAmount
- CommandTick
- PolicyId
- PreconditionsHash
```

It is deterministic input to simulation, not a renderer callback.

## 7. Collection result

```text
HarvestCollectionResult
- RemovedFromCell
- AddedToCargo
- UnacceptedRemainder
- NewCellQuantity
- NewCargoAmount
- DepletedTransition
- ReservationOutcome
- Diagnostics
```

Resource removal and cargo addition should be one ordered deterministic transaction or an explicitly documented multi-step protocol. Partial failure semantics are P0.

## 8. Timing

Candidate inputs:
- simulation tick;
- collection interval;
- amount per collection;
- animation notification;
- mission state;
- pause/game-speed state.

Authority:
- simulation tick, not Unity `deltaTime`;
- deterministic integer/fixed-point arithmetic;
- animation observes event, not drives it;
- pause and game speed are simulation-clock policies.

## 9. Simultaneous harvesters

Tie-breaking must be stable and serializable. Candidates:
- command tick;
- reservation creation ordinal;
- stable actor identity;
- stable cell identity;
- explicit priority.

Forbidden:
- dictionary/hash iteration order;
- Unity object instance ID;
- frame arrival time;
- wall-clock time;
- random without serialized deterministic state.

## 10. Auto-target and retarget

Future AI/mission adapter may request:
- nearest eligible resource;
- highest value;
- least contested;
- refinery-direction-aware;
- mission-scripted target;
- fallback around last target.

These strategies are not parser semantics and do not belong in Core format readers.

## 11. Command interruption

Stop, hold, guard, enemy interruption, target depletion, path failure, actor destruction and refinery loss produce explicit cancellation/replan candidates. No command automatically resurrects a reservation.

## 12. Pathfinding boundary

Pathfinder consumes a target/approach request and returns path information. It does not:
- reduce resource;
- claim resource permanently;
- mutate cargo;
- decide unload credits;
- rewrite Overlay arrays.

## 13. Policies

```text
HarvestTargetPolicy
CollectionPolicy
ResourceReservationPolicy
ResourceDeterminismPolicy
```

Each policy records product/profile, evidence, limits, tie-break and unknown-value behavior.

## 14. Safety limits

- max target candidates per query;
- max reservations per cell/actor;
- max collection commands per tick;
- max cargo entries;
- max retarget attempts;
- checked amount arithmetic;
- bounded diagnostics;
- no-progress detection.
