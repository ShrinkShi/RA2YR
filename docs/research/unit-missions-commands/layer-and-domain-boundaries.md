# Layer and domain boundaries

> **Source notice:** Public-source research only. ProjectBaseline was not read. `code_imported: false`.

## Domains

Keep distinct: authored placement Mission, Rules mission default, Script action/argument, Team/AI order, player command, Trigger command, runtime mission identity, command queue entry, locomotor/path state, combat target state, animation/cursor and Unity/UI object.

## Evidence

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert exposes mission/script/editor fields | `ConfirmedByOfficialToolSource` | EA editor | Official tool only. | Named editor profile. | `NotRun` |
| Named engines/tools/clients map fields into runtime command domains | `ImplementationSpecificBehavior` | Public implementations | Target-specific architecture. | Comparison only. | `NotRun` |
| Common authored Mission and Script-to-command candidates | `Underconfirmed` | Tools/community | Runtime mapping and lineage independence unproven. | Domain-tagged binding. | `NotRun` |
| Similar labels reused for source records, requests, state and UI | `ConflictingSources` | Editors/engines/clients/community | Layer meanings differ. | Never merge by name alone. | `NotRun` |
| Exact stock runtime domain/state ownership | `Unresolved` | No runtime source | No complete model. | Future simulation adapter. | `NotRun` |
| Immutable descriptors, no execution and no Unity/UI authority | `DefensiveDesign` | Project policy | Architecture boundary. | Fail closed. | `NotRun` |

A mission name does not create locomotion/combat state. A cursor/animation does not prove command acceptance. A Script argument matching a waypoint/object does not select the target domain by plausibility. Runtime state never rewrites authored records.
