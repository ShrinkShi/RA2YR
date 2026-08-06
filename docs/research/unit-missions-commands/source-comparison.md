# Source comparison and evidence boundaries

> **Source notice:** Public-source research only. ProjectBaseline was not read. All implementations are reference-only. `code_imported: false`.

## Formal grades

```text
ConfirmedByOriginalRuntimeSource
ConfirmedByOfficialToolSource
ConfirmedByMultipleIndependentImplementations
ConfirmedCommunityConvention
ImplementationSpecificBehavior
DefensiveDesign
ConflictingSources
Underconfirmed
Unresolved
```

No complete original mission runtime source was found and no claim has proven independent implementation lineages sufficient for the multiple-independent grade.

```text
AuditStatus: NotRun
FutureEvidenceSource: ProjectBaselineAggregateAudit
```

## Sources

| Source | Support | Limits/grade |
|---|---|---|
| EA FinalSun/FinalAlert 2 `6abf0f…` | Mission/Script/editor catalogs and validation | `ConfirmedByOfficialToolSource`; not runtime lifecycle |
| OpenRA `a52098…` | explicit target-engine orders/activities/stances | `ImplementationSpecificBehavior` |
| WAE/MapTool/CNCMaps/CnCNet | named editor/tool/client behavior | `ImplementationSpecificBehavior` |
| Chrono Divide | browser reimplementation command model | `ImplementationSpecificBehavior` |
| Ares/Phobos/Vinifera | extension missions/scripts/commands | `ImplementationSpecificBehavior`; extension-only |
| ModEnc/PPM/RA2 DIY | mission/script/command naming conventions | `ConfirmedCommunityConvention` or `Underconfirmed` |
| XCC/openra2 lineage | shared format/tool knowledge | not independent runtime evidence |

## Retained conflicts

Mission catalogs/aliases/defaults; Guard/Area Guard/Hunt/Attack/Stop/Hold behavior; command queue replace/append and mixed-selection acceptance; target typing and cell/object/reference conversion; Script action mapping, retry/failure/advance; player/AI/Script/Trigger arbitration; Move/Attack/Harvest/Enter/Repair/Capture/Deploy/Patrol/Scatter lifecycle; target loss, interruption, return, save/load and deterministic ordering.

Direct differences are `ConflictingSources`; common candidates without runtime/lineage proof are `Underconfirmed`; complete runtime behavior is `Unresolved`.

## Evidence summary

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert mission/script catalogs | `ConfirmedByOfficialToolSource` | EA editor | Official tool only. | Named editor profile. | `NotRun` |
| Named engine/tool/client/extension models | `ImplementationSpecificBehavior` | Public implementations | Separate profiles. | No source-count voting. | `NotRun` |
| Stable mission/command names | `ConfirmedCommunityConvention` | Community docs | Naming convention only. | Product/provider provenance. | `NotRun` |
| Common request/target/mission candidates | `Underconfirmed` | Tools/community | Runtime strictness and independence unproven. | Explicit profiles. | `NotRun` |
| Queue, target, guard and lifecycle semantics | `ConflictingSources` | Sources above | Direct model differences. | Preserve alternatives. | `NotRun` |
| Exact runtime mission state machine | `Unresolved` | No runtime source | No complete contract. | Future simulation adapter. | `NotRun` |
| Raw preservation/no execution/determinism | `DefensiveDesign` | Project policy | Safety/architecture. | Fail closed. | `NotRun` |

## License boundary

Do not copy mission/AI/path/combat algorithms, switches, source-shaped pseudocode or proprietary fixtures. Use factual field/name/reference observations, neutral original schemas, provenance and independent synthetic tests.
