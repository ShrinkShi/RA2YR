# AI and Script command boundaries

> **Source notice:** Public-source research only. ProjectBaseline was not read. `code_imported: false`.

## Separation

```text
ScriptTypeStepRaw
Team/AI order candidate
Player command request
Authored placement Mission
Runtime mission state
Runtime command queue
AI planner state
```

A Script step or editor list entry is declarative input, not an executed mission transition.

## Evidence

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert exposes Script/Mission catalogs and editor validation | `ConfirmedByOfficialToolSource` | EA editor | Official tool behavior only. | Named editor profile. | `NotRun` |
| OpenRA/WAE/Chrono Divide/extensions implement AI/Script command execution | `ImplementationSpecificBehavior` | Named implementations | Target/profile-specific. | Keep separate. | `NotRun` |
| Stable Script action and mission-name conventions | `ConfirmedCommunityConvention` | ModEnc/PPM/community docs | Convention only. | Preserve raw action/argument tokens. | `NotRun` |
| Script steps producing typed command candidates | `Underconfirmed` | Tools/community | Exact runtime mapping and lineage independence unproven. | Explicit Script profile. | `NotRun` |
| Team recruitment, Script advancement, retries, failure, target selection and player override | `ConflictingSources` | Engines/extensions/community | Models differ directly. | Preserve alternatives. | `NotRun` |
| Exact stock runtime AI planner/Script/mission transition behavior | `Unresolved` | No runtime source | No complete state machine. | Future AI/simulation adapter. | `NotRun` |
| Parser does not execute or repair Script commands and keeps stable step identity | `DefensiveDesign` | Project policy | Preservation/architecture. | Fail closed. | `NotRun` |

`ScriptCommandCandidate` records Script/step identity, raw action/argument, command-kind candidates, target/reference candidates, retry/completion profile and diagnostics. It never invokes actor methods. AI and player commands share an explicit arbitration interface but remain distinct sources with provenance and deterministic ordering.
