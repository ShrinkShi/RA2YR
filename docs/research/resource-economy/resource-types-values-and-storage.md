# Resource types, values and storage

> **Source notice:** ChatGPT Web public-source research. ProjectBaseline was not read. `code_imported: false`.

## Distinct values

```text
OverlayTypeRaw
OverlayDataRaw
ResourceFamily
VisualStage
StoredQuantityCandidate
RemainingAmount
RulesResourceValue
CellYieldCandidate
DeliveredCredits
PhysicalStorage
Cash
DisplayedCredits
```

No equality is assumed between these values.

## Official-editor estimate

FinalAlert uses an editor estimate for recognized resource ranges:

```text
(OverlayData + 1) × RulesResourceValue
```

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| The formula is used for an editor map-money estimate | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | It does not prove runtime cell quantity, harvester units, unload ticks or credit rounding. | Store as a named estimate profile. | `NotRun` |
| Community reports compare editor estimates with runtime observations | `ConfirmedCommunityConvention` | PPM/ModEnc/community | Reports do not establish a universal formula. | Keep separate from runtime settlement. | `NotRun` |
| `OverlayData+1` is the leading quantity/yield candidate for standard resource cells | `Underconfirmed` | Official editor plus tool/community evidence | Runtime applicability and custom-resource coverage are incomplete. | Explicit product/family profile. | `NotRun` |
| TS/RA2/YR/extension registries, stages and values share one model | `ConflictingSources` | Products/tools/extensions | Resource types and semantics differ. | Product/provider isolation. | `NotRun` |
| Exact runtime remaining amount, harvest decrement, value modifiers, rounding and storage conversion | `Unresolved` | No original-runtime source located | No complete contract. | Future simulation/economy adapter. | `NotRun` |
| Preserve raw fields, unknown resources and missing/invalid registry bindings | `DefensiveDesign` | Project policy | No default/fallback/renumbering. | Fail closed. | `NotRun` |

## Registry boundary

`[Tiberiums]`, resource subsections, Overlay ordinals, editor hardcoded ranges, theater/control INI, Rules values and extension registries retain independent provenance. Missing Art or resource definitions do not renumber Overlay identities or delete raw cells.

## Storage boundary

Harvester cargo, refinery acceptance, building/silo capacity, player physical resource, cash and displayed credits are separate. Ares/Phobos/Vinifera/OpenRA storage behavior is `ImplementationSpecificBehavior`; stock RA2/YR silo semantics remain `Unresolved`.
