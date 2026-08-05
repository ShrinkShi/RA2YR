# Source comparison and evidence boundaries

> **Source notice:** ChatGPT Web public-source research. ProjectBaseline was not read. All implementations are reference-only. `code_imported: false`.

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

No complete original RA2/YR economy runtime source was located and no reviewed resource claim has proven independent implementation lineages sufficient for `ConfirmedByMultipleIndependentImplementations`.

```text
AuditStatus: NotRun
FutureEvidenceSource: ProjectBaselineAggregateAudit
```

## Sources

| Source | Direct support | Limitations/grade |
|---|---|---|
| EA FinalSun/FinalAlert 2 `6abf0f…` | resource Overlay ranges, arrays, map-money estimate, growth/spread fields | `ConfirmedByOfficialToolSource`; editor only |
| WAE `b4c948…` | editor/resource models and writer behavior | `ImplementationSpecificBehavior` |
| OpenRA `a52098…` | harvesting, storage/cash, resource simulation model | `ImplementationSpecificBehavior`; target engine |
| CNCMaps/MapTool/CnCNet/Chrono Divide/openra2 | supplementary readers/clients/reimplementations | named behavior; lineage-sensitive |
| ModEnc/PPM/RA2 DIY | field/family/value/growth/spread authoring conventions | `ConfirmedCommunityConvention` or `Underconfirmed` |
| Ares/Phobos/Vinifera | extension storage/resource/harvester behavior | `ImplementationSpecificBehavior`; extension-only |
| XCC lineage | historical format knowledge | not independent from descendants/editor components |

## Lineage warning

XCC-derived readers, WAE/CNCMaps/community knowledge, OpenRA-inspired projects and shared documentation are not counted repeatedly. Extension implementations prove only their named profiles.

## Retained conflicts

- OverlayData as visual frame/stage versus quantity/yield;
- hardcoded RA2/YR ranges versus registry-driven TS/extensions;
- resource values, growth/spread, depletion and regrowth;
- harvester capacity units, mixed cargo and pips;
- refinery acceptance, docking, unload and credit rounding;
- stock cash versus physical storage/silo models;
- economy-source precedence and lobby/campaign overrides.

Direct disagreements are `ConflictingSources`; practical candidates without runtime proof are `Underconfirmed`; complete runtime behavior is `Unresolved`.

## Evidence summary

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert editor ranges/estimate/growth-spread fields | `ConfirmedByOfficialToolSource` | EA editor | Official tool only. | Named editor profile. | `NotRun` |
| Named tool/engine/extension economy models | `ImplementationSpecificBehavior` | Public implementations | Separate profiles. | No source voting. | `NotRun` |
| Stable resource/value/growth/storage terminology | `ConfirmedCommunityConvention` | Community docs | Convention only. | Product provenance. | `NotRun` |
| Common ore/gem stage/value and harvester/refinery candidates | `Underconfirmed` | Tools/community | Runtime applicability/lineage independence unproven. | Explicit profiles. | `NotRun` |
| Product/storage/stage/economy disagreements | `ConflictingSources` | Sources above | Direct model differences. | Preserve alternatives. | `NotRun` |
| Exact runtime quantity, harvesting, unloading, RNG and credits | `Unresolved` | No runtime source | No complete state machine. | Future simulation adapter. | `NotRun` |
| Raw preservation, no mutation and deterministic contracts | `DefensiveDesign` | Project policy | Safety/architecture. | Fail closed. | `NotRun` |

## License boundary

Do not copy harvesting/economy algorithms, switches, RNG, AI/pathfinding logic, source-shaped pseudocode or proprietary fixtures. Use factual fields/relationships, original neutral schemas, provenance and independent synthetic tests.
