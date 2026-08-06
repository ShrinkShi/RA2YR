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

No complete original production runtime source was found and no claim has proven independent implementation lineages sufficient for the multiple-independent grade.

```text
AuditStatus: NotRun
FutureEvidenceSource: ProjectBaselineAggregateAudit
```

## Sources

| Source | Support | Limits/grade |
|---|---|---|
| EA FinalSun/FinalAlert 2 `6abf0f…` | editor registries/type fields/validation | `ConfirmedByOfficialToolSource`; not runtime |
| OpenRA `a52098…` | explicit production/queue/placement model | `ImplementationSpecificBehavior` |
| WAE/MapTool/CNCMaps/CnCNet | named editor/tool/client behavior | `ImplementationSpecificBehavior` |
| Ares/Phobos/Vinifera | extension prerequisites/factories/queues/upgrades | `ImplementationSpecificBehavior`; extension-only |
| ModEnc/PPM/RA2 DIY | registry, Owner, prerequisite, TechLevel, Cost/time, factory/sidebar conventions | `ConfirmedCommunityConvention` or `Underconfirmed` |
| XCC/openra2 lineage | shared format/tool knowledge | not independent runtime evidence |

## Retained conflicts

Registry enumeration and unlisted definitions; Owners/Country/Side/House identity; prerequisite grammar/generic groups/stolen tech; TechLevel/BuildLimit scope; cost/time units and modifier order; shared/per-factory/category queues; credits payment/refund; low-power/multiple-factory/capture behavior; completion/exit/placement; deploy/upgrade state transfer; sidebar visibility/order/resources.

Direct differences are `ConflictingSources`; common candidates without runtime/lineage proof are `Underconfirmed`; complete runtime behavior is `Unresolved`.

## Evidence summary

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert production fields/catalogs | `ConfirmedByOfficialToolSource` | EA editor | Official tool only. | Named editor profile. | `NotRun` |
| Named engine/tool/client/extension models | `ImplementationSpecificBehavior` | Public implementations | Separate profiles. | No source-count voting. | `NotRun` |
| Stable production/tech/sidebar conventions | `ConfirmedCommunityConvention` | Community docs | Convention only. | Product/provider provenance. | `NotRun` |
| Common registries/availability/factory candidates | `Underconfirmed` | Tools/community | Runtime strictness and independence unproven. | Explicit profiles. | `NotRun` |
| Grammar, queue, payment, modifiers and placement | `ConflictingSources` | Sources above | Direct model differences. | Preserve alternatives. | `NotRun` |
| Exact runtime production state machine | `Unresolved` | No runtime source | No complete contract. | Future simulation adapter. | `NotRun` |
| Raw preservation/no execution/determinism | `DefensiveDesign` | Project policy | Safety/architecture. | Fail closed. | `NotRun` |

## License boundary

Do not copy production/queue algorithms, prerequisite evaluators, switches, formulas, source-shaped pseudocode or proprietary fixtures. Use factual field/reference observations, neutral original schemas, provenance and independent synthetic tests.
