# Transition determinism and lifecycle

> **Source notice:** Public-source research only. ProjectBaseline was not read. `code_imported: false`.

## Lifecycle split

```text
CommandRequest
AcceptanceResult
QueueMutationCommand
MissionTransitionCommand
RuntimeMissionSnapshot
Substate/Path/Target State
Completion/Failure/Interrupt Result
FollowUpCommand
PresentationEvent
```

Every stage carries stable identity, tick, source, actor order, profile and reason. Parser does not advance stages.

## Evidence

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert supplies authored mission/script records, not runtime lifecycle | `ConfirmedByOfficialToolSource` | EA editor | Official tool evidence is limited. | No lifecycle inference. | `NotRun` |
| OpenRA/Chrono Divide/extensions implement activities/orders/state transitions | `ImplementationSpecificBehavior` | Named implementations | Target/profile-specific. | Comparison only. | `NotRun` |
| Common command-completion/interruption conventions | `ConfirmedCommunityConvention` | Community docs | Convention only. | Preserve applicability. | `NotRun` |
| Explicit request→transition→result lifecycle as architecture candidate | `Underconfirmed` | Tools/community | Exact runtime state boundaries unproven. | Named lifecycle profile. | `NotRun` |
| Queue mutation, interruption priority, target loss, retry, return and completion chaining | `ConflictingSources` | Engines/extensions/community | Models differ directly. | Preserve alternatives. | `NotRun` |
| Exact stock runtime lifecycle, state serialization and arbitration | `Unresolved` | No runtime source | No complete contract. | Future deterministic simulation adapter. | `NotRun` |
| Stable IDs/order, explicit RNG/tick and no frame/UI dependence | `DefensiveDesign` | Project policy | Determinism/architecture. | Canonical actor/command ordering. | `NotRun` |

## Determinism

Do not use dictionary enumeration, object addresses, Unity instance IDs, frame rate, animation callbacks, pathfinding thread completion or camera state. Runtime spawn/command/transition ordinals come from deterministic simulation and survive save/load/replay. A rejected or ambiguous command remains a recorded result; it is not silently converted into Guard/Stop or dropped.
