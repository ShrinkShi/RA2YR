# Selection, control groups, and UI boundaries

> **Source notice:** Public-source research only. ProjectBaseline was not read. `code_imported: false`.

## Separation

```text
LocalSelection
ControlGroupMembership
CommandAuthority
Owned/Controllable Actor Set
IssuedCommandBatch
PerActorAcceptance
Cursor/Hotkey/Feedback
RuntimeMissionState
```

Selection and control groups do not establish ownership, command authority or simulation state. UI proposes commands; simulation validates each actor deterministically.

## Evidence

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| Official manuals/editor expose command names, hotkeys and authoring labels | `ConfirmedByOfficialToolSource` | EA manuals and FinalAlert | Official tool/documentation behavior only. | Named UI/editor profile. | `NotRun` |
| CnCNet/OpenRA/other clients implement selection and command UI | `ImplementationSpecificBehavior` | Named clients/engines | Client/target-specific. | Keep separate profiles. | `NotRun` |
| Stable selection, control-group and command-feedback conventions | `ConfirmedCommunityConvention` | Manuals/community docs | Convention only. | Preserve surface provenance. | `NotRun` |
| Batch command with per-actor validation as an architecture candidate | `Underconfirmed` | Clients/engines/community | Exact stock authority and lineage independence unproven. | Explicit session/command profile. | `NotRun` |
| Hotkeys, mixed-selection behavior, control-group persistence and feedback timing | `ConflictingSources` | Clients/engines/platforms | Public models differ directly. | Preserve alternatives. | `NotRun` |
| Exact stock multiplayer/local authority and UI synchronization | `Unresolved` | No original-runtime source located | No complete contract. | Future session/UI adapters. | `NotRun` |
| UI state is non-authoritative and never rewrites Mission/queue state | `DefensiveDesign` | Project policy | Architecture boundary. | UI emits explicit requests only. | `NotRun` |

## S/H/G policy

Project-specific S/H/G bindings are represented in an independent Policy field or as `DefensiveDesign`; they are not external runtime evidence. Cursor, animation, sound, health bars, route lines and command feedback are downstream presentation and cannot trigger authoritative transitions.
