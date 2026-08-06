# Command taxonomy and request model

> **Source notice:** Public-source research only. ProjectBaseline was not read. `code_imported: false`.

## Command sources

```text
AuthoredInitialMission
PlayerCommand
AICommand
ScriptCommand
TriggerCommand
InternalFollowUpCommand
```

Each command carries source, priority/arbitration candidate, tick, stable ordinal, queue/replace modifier, actor set, typed target, capability requirements and diagnostics.

## Evidence

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert exposes mission/command names and editor parameters | `ConfirmedByOfficialToolSource` | EA editor | Official tool only. | Named editor profile. | `NotRun` |
| OpenRA/clients/extensions implement command request and queue models | `ImplementationSpecificBehavior` | Named implementations | Target/client-specific. | Keep separate. | `NotRun` |
| Common Move/Attack/Guard/Harvest/Deploy/etc. command taxonomy | `ConfirmedCommunityConvention` | ModEnc/PPM/community docs | Names do not prove state transitions. | Preserve raw and product applicability. | `NotRun` |
| Typed requests with actor/target/modifier candidates | `Underconfirmed` | Tools/community | Exact runtime queue/arbitration and lineage independence unproven. | Explicit command profile. | `NotRun` |
| Replace/append/front queueing, mixed selection, partial acceptance and command priority | `ConflictingSources` | Engines/clients/extensions | Models differ directly. | Preserve alternatives and per-actor results. | `NotRun` |
| Exact stock runtime command protocol and arbitration | `Unresolved` | No runtime source | No complete contract. | Future simulation/session adapter. | `NotRun` |
| Stable IDs, declarative requests and no UI/animation authority | `DefensiveDesign` | Project policy | Determinism/architecture. | Fail closed. | `NotRun` |

## Results

`CommandAcceptanceResult` is per actor and separates syntax, capability, target, current-state and queue-policy outcomes. A command can be partially accepted without deleting rejected actors or rewriting the request. Parser/UI do not mutate missions; simulation emits explicit accepted/rejected/queued/replaced transition commands.
