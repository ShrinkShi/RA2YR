> **Source notice:** Prepared by **ChatGPT Web** from public sources only. ProjectBaseline was not read. This is not a Codex artifact. GPL and unclear-license implementations are reference-only; no code was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

# Queued Orders, Waypoints, and Patrol

## Official manual evidence

The RA2 manual describes `Z` waypoint mode:

- selected groups receive multiple waypoint nodes;
- units wait until waypoint mode is exited;
- ordinary route nodes are deleted as they are completed;
- patrol routes can be looped and persist;
- multiple groups can be configured before synchronized release;
- nodes may be deleted and adjacent route segments reconnected.

These are player-facing behavior statements, not a complete savegame or network serialization format.

## Queue boundary

```text
UIWaypointList
!= IssuedCommandBatch
!= CommandQueueDescriptor
!= RuntimeCommandQueue
!= PathNodeList
```

## Recommended models

```text
CommandQueueDescriptor
- OwnerActorStableId
- QueuePolicyProfile
- Entries[]
- ActiveOrdinal
- Version
- EvidenceGrade

CommandQueueEntry
- CommandStableId
- QueueOrdinal
- CommandType
- TargetDescriptor
- AppendOrReplaceCandidate
- ValidationSnapshot
- Status

WaypointRouteDescriptor
- RouteStableId
- NodeDescriptors[]
- LoopCandidate
- ReleasePolicy
- AssociatedActors[]
```

## Queue policies requiring explicit profiles

- Shift append versus replace;
- normal right-click while queue exists;
- Stop clears active only versus entire queue;
- invalid queued target behavior;
- actor death;
- partial completion;
- new explicit command;
- deployment;
- entering transport;
- target ownership change;
- save/load;
- replay;
- multiplayer command batching.

## Waypoint node types

- movement cell;
- attack actor;
- attack cell;
- enter transport/building;
- repair facility;
- patrol loop connection;
- synchronized release marker;
- extension command;
- unknown.

A visual node can map to more than one command and is not the authoritative queue entry.

## Patrol

Patrol combines a looped route with an autonomous behavior profile. Route following, target acquisition, chase, leash/return-to-route, and node advancement are separate policies.

## Deterministic requirements

- stable route identity;
- stable node ordinal;
- stable actor ordering;
- stable batch-release tick;
- explicit invalidation and deletion order;
- no dictionary ordering or UI callback arrival order.
