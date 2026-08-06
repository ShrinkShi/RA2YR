# Harvester capacity and load state

> **Source notice:** ChatGPT Web public-source research. ProjectBaseline was not read. `code_imported: false`.

## Separation

```text
AuthoredTypeCapacity
≠ CurrentCargo
≠ CargoComposition
≠ CargoEconomicValue
≠ PipCount
≠ DisplayedLoadFraction
≠ HarvesterVisualFrame
```

Raw Rules/type fields are configuration candidates; current cargo is runtime state; pips/bars/frames are presentation.

## Evidence

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert/editor exposes resource/harvester-related fields and estimates | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Editor behavior only. | Named editor profile. | `NotRun` |
| OpenRA or extension cargo/storage models | `ImplementationSpecificBehavior` | Named implementations | Target/profile-specific. | Keep separate. | `NotRun` |
| Common capacity, PipScale and harvester conventions | `ConfirmedCommunityConvention` | ModEnc/community docs | Convention does not prove runtime units or mixed-cargo behavior. | Preserve raw fields/product applicability. | `NotRun` |
| Type capacity as a stock cargo-limit candidate | `Underconfirmed` | Tools/community | Units, defaults and rounding remain incomplete. | Explicit capacity profile. | `NotRun` |
| Exact runtime cargo units, mixed resources, pips and unload rounding | `Unresolved` | No original-runtime source located | No reliable complete contract. | Future simulation/UI adapters. | `NotRun` |
| UI/visual state used as cargo authority | `ConflictingSources` | Tool visuals and data models | Display frames/pips are not canonical state. | Canonical runtime cargo only. | `NotRun` |
| No clamp/default repair and UI separation | `DefensiveDesign` | Project policy | Preservation/architecture. | Checked capacity and explicit state. | `NotRun` |

## Model

```text
HarvesterCapacityCandidate
- RawCapacity
- UnitsProfile
- ResourceFamilyApplicability
- ProductProfile
- Evidence

RuntimeCargoSnapshot
- EntriesByResourceFamily
- TotalQuantity
- TotalEconomicValueCandidate
- CapacityReference
- SimulationTick
```

Missing, invalid, negative and overflowed capacity values remain distinct. Core does not fill capacity from visuals, class names or common defaults.

## UI boundary

The proposed selected-unit load indicator:

```text
black outline + yellow fill
```

is `DefensiveDesign` UI policy, not stock behavior. It consumes an explicit cargo/capacity snapshot and never writes cargo, changes simulation, reads sprite frames, infers resource type from color or becomes a compatibility claim.
