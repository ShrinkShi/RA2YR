> **Source notice:** Prepared by **ChatGPT Web** from public sources only. ProjectBaseline was not read. This is not a Codex artifact. GPL and unclear-license implementations are reference-only; no code was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

# Movement, Attack, Chase, and Leash

## Command families

- move to cell or world position;
- attack actor;
- attack cell / force fire;
- attack-move;
- move then attack;
- guard actor or area;
- follow;
- retreat;
- patrol;
- scripted move/attack;
- deploy or enter approach;
- transport embark/unload approach.

## Required identities

```text
IssuedCommand
AcceptedCommand
CurrentMission
MovementIntent
PathRequest
PathResult
TargetingIntent
WeaponFiringIntent
AutonomousChaseIntent
```

These must remain distinct throughout logging, save/load, replay, and diagnostics.

## Attack lifecycle candidate

```text
command issued
→ command authority and target validation
→ accepted command
→ target snapshot
→ movement-to-range intent
→ path request
→ target visibility/validity refresh
→ weapon range/minimum-range query
→ facing/aim candidate
→ firing intent
→ combat command
→ persistence or mission completion
```

## Target changes

Explicit policies are required when the target:

- enters weapon range;
- leaves weapon range;
- moves inside MinimumRange;
- cloaks or becomes disguised;
- enters limbo;
- changes owner or alliance;
- dies or is destroyed;
- enters a transport/building;
- changes bridge/air/subterranean layer;
- becomes unreachable;
- exits the map.

## Chase versus attack

```text
AttackCommandTarget
!= CurrentWeaponTarget
!= AutonomousChaseTarget
!= PathDestination
!= LeashOrigin
```

Chase is a derived movement intent that can terminate while the underlying target is still valid.

## Pursuit termination candidates

- target invalid;
- target outside leash;
- path impossible;
- hold-position restriction;
- higher-priority explicit command;
- mission changed;
- target hidden according to profile;
- timeout;
- unit damaged/retaliation override;
- transport/deploy transition;
- aircraft fuel/ammo/return policy;
- map or layer boundary.

## Leash model

```text
LeashPolicy
- OriginKind: command-start | guard-origin | hold-origin | actor-follow | waypoint
- RadiusCandidate
- DistanceMetric
- LayerPolicy
- ReturnPolicy
- ReacquirePolicy
- TimeoutCandidate
```

Weapon range, guard range, acquisition scan range, chase range, and leash radius are independent.

## Movement environment

Ground, naval, aircraft, subterranean, bridge deck, under-bridge, shore, and transport-contained states require separate capability queries. The mission layer does not implement pathfinding and must not use Unity NavMesh as source semantics.
