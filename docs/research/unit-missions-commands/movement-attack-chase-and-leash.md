# Movement, attack, chase, and leash

> **Source notice:** Public-source research only. ProjectBaseline was not read. `code_imported: false`.

## State split

```text
MoveCommand
AttackCommand
MovementIntent
TargetingIntent
FiringIntent
PathDestination
WeaponTarget
AutonomousChaseTarget
LeashOrigin
ReturnIntent
```

A command request does not itself authorize movement or fire. Pathfinding, targeting and combat validate separate snapshots and produce deterministic results.

## Evidence

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert exposes Move/Attack/Guard/Hunt/Patrol mission names and parameters | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Official catalog only. | Named editor profile. | `NotRun` |
| OpenRA, clients and extensions implement movement, attack and pursuit models | `ImplementationSpecificBehavior` | Named implementations | Target/profile-specific. | Keep separate profiles. | `NotRun` |
| Stable Move/Attack/Hunt/Guard/chase terminology | `ConfirmedCommunityConvention` | ModEnc/PPM/community docs | Convention only. | Preserve raw identity/product scope. | `NotRun` |
| Explicit movement/target/firing intents and leash candidates | `Underconfirmed` | Tools/community | Exact stock lifecycle and lineage independence unproven. | Explicit engagement profile. | `NotRun` |
| Attack-move, target persistence, pursuit distance, lost-target behavior and return-to-anchor | `ConflictingSources` | Engines/clients/extensions/community | Public models differ directly. | Preserve alternatives. | `NotRun` |
| Exact runtime transition, path retry, chase and leash behavior | `Unresolved` | No original-runtime source located | No complete state machine. | Future deterministic movement/combat adapters. | `NotRun` |
| No path/target selection during parsing and stable tie-breaking | `DefensiveDesign` | Project policy | Determinism/architecture. | Fail closed. | `NotRun` |

## Boundaries

Hold Position can forbid movement while an independent engagement policy permits in-place aiming or firing. Stop, target loss and new explicit commands may clear different subsets under an explicit profile. Camera, animation, cursor and renderer state never authorize movement, chase or attack.
