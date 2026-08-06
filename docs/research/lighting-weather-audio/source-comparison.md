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

No complete original RA2/YR runtime source was located and no reviewed lighting/weather/audio claim has proven independent implementation lineages sufficient for `ConfirmedByMultipleIndependentImplementations`.

```text
AuditStatus: NotRun
FutureEvidenceSource: ProjectBaselineAggregateAudit
```

## Sources

| Source | Category/support | Limits/grade |
|---|---|---|
| EA FinalSun / FinalAlert 2 `6abf0f…` | official editor fields, labels, presets, generated Trigger scripts | `ConfirmedByOfficialToolSource`; not runtime formula/state |
| WAE `b4c948…` | editor numeric model, preview composition, local-light/media behavior | `ImplementationSpecificBehavior` |
| OpenRA `a52098…` | target-engine TerrainLighting/import conversion | `ImplementationSpecificBehavior` |
| CnCNet client `e6e367…` | client media/visibility/session behavior | `ImplementationSpecificBehavior` |
| Ares/Phobos docs | extension Lightning Storm, actions and environment features | `ImplementationSpecificBehavior`; extension-only |
| ModEnc/PPM/RA2 DIY | stable field/media/weather descriptions | `ConfirmedCommunityConvention` or `Underconfirmed` |
| CNCMaps/MapTool/Chrono Divide/XCC lineage | supplementary comparisons/absence records | named implementation or lineage only |

Shared XCC/OpenRA/community knowledge is not counted repeatedly.

## Retained conflicts

- WAE versus OpenRA composition and Ground treatment;
- clamp/range, numeric format and color-space assumptions;
- normal/Ion/Dominator completeness and applicability;
- editor presets versus autonomous runtime time progression;
- capability flags versus active weather state;
- stock versus extension storm effects, damage, radar and sound;
- Theme/Sound/Speech/Movie registry fallback and playback rules;
- local-light units/conversion and Spotlight behavior;
- Fog/Shroud metadata versus visibility, darkness and post-processing.

Direct disagreements are `ConflictingSources`; candidates lacking runtime applicability are `Underconfirmed`; complete runtime formulas/state machines remain `Unresolved`.

## Evidence summary

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert fields, labels and authoring scripts | `ConfirmedByOfficialToolSource` | EA editor | Official tool only. | Named editor profile. | `NotRun` |
| WAE/OpenRA/client/extension behavior | `ImplementationSpecificBehavior` | Named sources | Separate target/profile behavior. | Do not source-vote. | `NotRun` |
| Stable field/media/weather names | `ConfirmedCommunityConvention` | Community docs | Convention only. | Product/profile provenance. | `NotRun` |
| Common static Lighting and logical media-reference candidates | `Underconfirmed` | Tools/community | Runtime strictness and lineage independence unproven. | Explicit profiles. | `NotRun` |
| Composition/Ground/color-space/weather/visibility models | `ConflictingSources` | Public sources | Direct model differences. | Preserve alternatives. | `NotRun` |
| Exact runtime formulas, time/weather/visibility/audio execution | `Unresolved` | No runtime source | No complete contract. | Future adapters. | `NotRun` |
| Raw preservation, no trial rendering/execution/fallback | `DefensiveDesign` | Project policy | Safety/layering. | Fail closed. | `NotRun` |

## License boundary

No source code, switch/catalog bodies, algorithms, source-shaped pseudocode or proprietary fixtures are imported. Use factual fields/relationships, neutral original schemas, provenance and independent synthetic tests.
