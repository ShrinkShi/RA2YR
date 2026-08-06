# Prerequisites, TechLevel and BuildLimit

> **Source notice:** Public-source research only. ProjectBaseline was not read. `code_imported: false`.

## Separation

```text
PrerequisiteExpressionRaw
PrerequisiteBinding
TechnologySnapshot
TechLevelCandidate
BuildLimitDescriptor
AvailabilityResult
```

Parsing an expression does not query current buildings, captured tech, allies, power, credits or queues.

## Evidence

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert exposes Prerequisite/TechLevel/BuildLimit fields and validation | `ConfirmedByOfficialToolSource` | EA editor | Official tool only. | Named editor profile. | `NotRun` |
| OpenRA/Ares/Phobos implement prerequisite grammars and availability | `ImplementationSpecificBehavior` | Named implementations | Target/extension-specific. | Separate profiles. | `NotRun` |
| Common comma-list, alternate-group, negative BuildLimit and TechLevel conventions | `ConfirmedCommunityConvention` | ModEnc/PPM/community docs | Convention only. | Preserve raw syntax/applicability. | `NotRun` |
| Standard prerequisite and TechLevel/BuildLimit candidates | `Underconfirmed` | Tools/community | Runtime grammar/defaults/count semantics and independence unproven. | Explicit expression/count profiles. | `NotRun` |
| AND/OR/group syntax, generic prerequisites, upgrade counts, negative limits and captured/allied satisfaction | `ConflictingSources` | Engines/extensions/community | Models differ directly. | Preserve alternatives and blockers. | `NotRun` |
| Exact runtime evaluation order, count scope, refund/death/capture and mode interaction | `Unresolved` | No runtime source | No complete contract. | Future simulation/session adapter. | `NotRun` |
| Raw punctuation/empties, no expression repair and multi-reason availability | `DefensiveDesign` | Project policy | Preservation/fail-closed design. | No boolean collapse. | `NotRun` |

`ProductionAvailabilityResult` reports visibility, requestability, all blockers, unknown reasons and matching factories. `BuildLimit` counts type/family/runtime instances only under an explicit profile; queued, deployed, limbo, captured, upgrade and dead instances are not assumed.
