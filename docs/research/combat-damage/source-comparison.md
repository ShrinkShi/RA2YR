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

No complete original combat runtime source was found and no claim has proven independent implementation lineages sufficient for the multiple-independent grade.

```text
AuditStatus: NotRun
FutureEvidenceSource: ProjectBaselineAggregateAudit
```

## Sources

| Source | Support | Limits/grade |
|---|---|---|
| EA FinalSun/FinalAlert 2 `6abf0f…` | editor catalogs/fields/validation | `ConfirmedByOfficialToolSource`; not runtime |
| OpenRA `a52098…` | explicit target-engine combat algorithms | `ImplementationSpecificBehavior` |
| WAE `b4c948…` | Rules/editor fields and validation | `ImplementationSpecificBehavior` |
| Chrono Divide/CnCNet/MapTool/CNCMaps | supplementary named behavior | `ImplementationSpecificBehavior`/absence evidence |
| Ares/Phobos/Vinifera | extension Armor, Verses, projectile/status behavior | `ImplementationSpecificBehavior`; extension-only |
| ModEnc/PPM/RA2 DIY | field, Armor, Verses and combat conventions | `ConfirmedCommunityConvention` or `Underconfirmed` |
| XCC/openra2 lineage | shared format/tool knowledge | not independent runtime evidence |

## Retained conflicts

- stock Armor order and named-extension mapping;
- positional Verses length, missing/extra token behavior and percentage representation;
- Verses multiplier versus force-fire/retaliation/passive-acquire side effects;
- weapon fallback/elite/death/special slots;
- projectile flight, tracking, collision and terrain/bridge interaction;
- CellSpread shape, distance, falloff and multi-cell enumeration;
- AffectsAllies, owner/self and special-effect eligibility;
- damage stage order, rounding, minimum/negative damage and RNG;
- ammo, burst, reload, target snapshots, status and death sequencing.

Direct differences are `ConflictingSources`; common candidates without runtime/lineage proof are `Underconfirmed`; complete runtime behavior is `Unresolved`.

## Evidence summary

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert combat fields/catalogs | `ConfirmedByOfficialToolSource` | EA editor | Official tool only. | Named editor profile. | `NotRun` |
| Named engine/tool/extension combat models | `ImplementationSpecificBehavior` | Public implementations | Separate profiles. | No source-count voting. | `NotRun` |
| Stable field/Armor/Verses conventions | `ConfirmedCommunityConvention` | Community docs | Convention only. | Product/provider provenance. | `NotRun` |
| Common record and arithmetic candidates | `Underconfirmed` | Tools/community | Runtime strictness and independence unproven. | Explicit profiles. | `NotRun` |
| Algorithms and side effects | `ConflictingSources` | Sources above | Direct model differences. | Preserve alternatives. | `NotRun` |
| Exact runtime combat state machine | `Unresolved` | No runtime source | No complete contract. | Future simulation adapter. | `NotRun` |
| Raw preservation/no execution/determinism | `DefensiveDesign` | Project policy | Safety/architecture. | Fail closed. | `NotRun` |

## License boundary

Do not copy combat algorithms, switches, formulas, RNG, source-shaped pseudocode or proprietary fixtures. Use factual field/reference observations, neutral original schemas, provenance and independent synthetic tests.
