# Queued orders, waypoints, and patrol

> **Source notice:** Public-source research only. ProjectBaseline was not read. `code_imported: false`.

## Distinct structures

```text
IssuedCommandBatch
PerActorAcceptanceResult
SimulationCommandQueue
QueueEntry
ScenarioWaypointIdentity
UIWaypointNode
PatrolRoute
PathNode
RuntimeRouteProgress
```

UI waypoint lines and command batches are presentation/input artifacts; they are not the authoritative simulation queue or path.

## Evidence

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert exposes waypoint and mission/script authoring fields | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Official editor behavior only. | Named editor profile. | `NotRun` |
| OpenRA, clients and extensions implement queues, waypoints and patrols | `ImplementationSpecificBehavior` | Named implementations | Target/client-specific. | Keep separate profiles. | `NotRun` |
| Stable queued-order/waypoint/patrol authoring conventions | `ConfirmedCommunityConvention` | Manuals/ModEnc/PPM/community docs | Convention only. | Preserve raw order and identity. | `NotRun` |
| Ordered command entries and route-node candidates | `Underconfirmed` | Tools/community | Exact runtime limits and lineage independence unproven. | Explicit queue/route profile. | `NotRun` |
| Replace/append/front insertion, route looping, failure handling and patrol engagement | `ConflictingSources` | Engines/clients/extensions/community | Public models differ directly. | Preserve alternatives. | `NotRun` |
| Exact stock queue ownership, maximums, transition and save/load behavior | `Unresolved` | No original-runtime source located | No complete contract. | Future deterministic simulation adapter. | `NotRun` |
| Stable IDs/order, bounded queues and no UI/path-node authority | `DefensiveDesign` | Project policy | Determinism/architecture. | Fail closed. | `NotRun` |

## Determinism

Entries carry simulation tick, player/actor stable IDs, command sequence, queue ordinal, target identity and selected policy. Mixed selections may accept commands per actor without deleting rejected actors. Missing/duplicate waypoint references, route gaps and unknown modifiers remain explicit diagnostics rather than repaired routes.
