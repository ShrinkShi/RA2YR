# Growth, spread and depletion

> **Source notice:** ChatGPT Web public-source research. ProjectBaseline was not read. `code_imported: false`.

## Capability versus runtime state

```text
GrowthCapabilityRaw
SpreadCapabilityRaw
Growth/SpreadProfile
RuntimeSchedulerState
RngState
EligibleCellSet
CurrentResourceState
```

These are separate. Parsing flags does not run timers, RNG, placement, mutation or depletion.

## Evidence

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert reads/writes `TiberiumGrows` and `TiberiumSpreads` and relabels them for RA2 ore | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Official editor metadata/UI behavior only. | Preserve named editor profile. | `NotRun` |
| OpenRA/Ares/Phobos/Vinifera growth/spread algorithms and fields | `ImplementationSpecificBehavior` | Named engines/extensions | Product/extension-specific. | Explicit profile isolation. | `NotRun` |
| Stable community Growth/Spread terminology | `ConfirmedCommunityConvention` | ModEnc/PPM/RA2 DIY | Convention only. | Product applicability retained. | `NotRun` |
| Stock RA2/YR has growth/spread capability controlled by authored fields | `Underconfirmed` | Editor/community/tool evidence | Exact runtime applicability and defaults remain incomplete. | Capability descriptor only. | `NotRun` |
| TS, RA2/YR and extension growth/spread/depletion models | `ConflictingSources` | Products/extensions | Resource families, timing and selection differ directly. | Never merge into one vanilla algorithm. | `NotRun` |
| Exact runtime timers, probabilities, eligible-cell selection, RNG, caps and depletion/regrowth | `Unresolved` | No original-runtime source located | No complete deterministic state machine. | Future simulation adapter. | `NotRun` |
| No parser mutation/RNG, explicit seed/profile and raw preservation | `DefensiveDesign` | Project policy | Determinism/architecture. | Commands/results separate. | `NotRun` |

## Command boundary

A future simulation may consume explicit `GrowResourceCommand`, `SpreadResourceCommand`, `DepleteResourceCommand` and `RegrowResourceCommand` candidates with deterministic tick, seed/state, source/target cells, profile and result. Parser, renderer and pathfinder do not execute them.

## Eligibility

Surface, resource family, current occupancy, map bounds, theater/product profile, neighboring resource state and dynamic blockers may affect eligibility, but no single visual frame or OverlayData value proves it. Missing/unknown evidence yields ambiguity, not mutation.
