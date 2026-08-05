# M3-R13 — Resource harvesting and economy boundaries

> **Source notice:** ChatGPT Web public-source research only. ProjectBaseline was not read. This is not a Codex artifact. GPL and unclear-license implementations are reference-only; no harvesting, economy, AI, RNG, pathfinding or UI code was copied or ported. `code_imported: false`.

## Boundary

```text
raw map/Rules descriptors
→ resource-overlay family binding
→ raw stage/value candidates
→ logical resource-cell descriptors
→ harvester/refinery capability binding
→ declarative collection/unloading contracts
→ economy-source descriptors
→ future deterministic simulation/UI adapters
```

Overlay parsing does not calculate remaining resource, collection does not mutate cells/cargo, refinery binding does not transfer credits, growth/spread metadata does not run RNG, and Core creates no actors or Unity objects.

## Findings

- Overlay and OverlayData remain independent arrays; OverlayData has family-specific semantics.
- Ore, gems, TS Tiberium, veins, crates, debris, walls, bridges and extension resources require separate product/family profiles.
- `OverlayDataRaw`, visual stage, stored quantity candidate, remaining amount, cell yield, Rules value and delivered credits are distinct.
- FinalAlert's `(OverlayData + 1) × Value` is an official-editor estimate only.
- Type capacity, current cargo, cargo composition/value, pips and UI load fraction are separate.
- Harvest target, approach, reservation, command and result are separate deterministic simulation contracts.
- Refinery capability, dock/queue, unload animation, cargo mutation, storage mutation and credit mutation are separate.
- Growth/spread capability, interval/probability, eligible cells, RNG, depletion and regrowth state remain separate.
- House credits, carry-over, lobby/game-mode money, refinery/crate/Trigger mutations, runtime accounts and score keep independent provenance.
- A black-outline/yellow-fill load bar is project UI policy only.

## Formal grades

Formal `Grade` fields use only:

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

No complete original RA2/YR economy runtime source was found. FinalSun/FinalAlert behavior uses `ConfirmedByOfficialToolSource`; named tools/engines/extensions use `ImplementationSpecificBehavior`; stable community conventions use `ConfirmedCommunityConvention`; cross-tool candidates without proven lineage/runtime applicability remain `Underconfirmed`; direct product/formula/storage conflicts use `ConflictingSources`; complete runtime settlement/RNG/economy behavior remains `Unresolved`.

Raw preservation, explicit product/family/profile selection, no stage-to-quantity assumption, no simulation mutation, deterministic reservations, checked arithmetic, no UI-as-authority and fail-closed binding use `DefensiveDesign`.

```text
AuditStatus: NotRun
FutureEvidenceSource: ProjectBaselineAggregateAudit
```

These fields do not imply ProjectBaseline access and cannot promote compatibility or runtime evidence.

## Normalized claims

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert resource ranges, Overlay arrays, growth/spread fields and map-money estimate | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Official editor behavior only. | Named editor profile. | `NotRun` |
| OpenRA/WAE/CNCMaps and extensions' resource/economy behavior | `ImplementationSpecificBehavior` | Named implementations | Target/profile-specific. | Keep separate. | `NotRun` |
| Stable ore/gem/Tiberium/value/growth/spread conventions | `ConfirmedCommunityConvention` | ModEnc/PPM/RA2 DIY | Convention only. | Product applicability retained. | `NotRun` |
| Stage/value/capacity/refinery candidates shared by tools | `Underconfirmed` | Tools/community | Runtime applicability and independence unproven. | Explicit profiles. | `NotRun` |
| TS/RA2/YR/extension storage, stage, silo and economy models | `ConflictingSources` | Tools/community/extensions | Direct product/model differences. | Do not merge profiles. | `NotRun` |
| Exact runtime remaining amount, harvest settlement, unload, RNG and account precedence | `Unresolved` | No runtime source | No complete contract. | Future simulation adapter. | `NotRun` |
| No mutation/repair, canonical raw state and UI separation | `DefensiveDesign` | Project policy | Preservation/architecture. | Fail closed. | `NotRun` |

## Non-goals

No resource parser implementation, harvesting, cargo mutation, targeting/reservation, docking/unloading, growth/spread, credits/storage mutation, economy AI, UI load bar, Unity, code, tests, ProjectBaseline audit, game/editor execution or compatibility promotion is included.
