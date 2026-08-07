# Source comparison and evidence boundaries

> **Source notice:** ChatGPT Web public-source research. ProjectBaseline was not read. All code sources are reference-only. `code_imported: false`.

## Formal evidence vocabulary

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

No complete original RA2/YR runtime source was found and no metadata/House claim currently has proven independent implementation lineages sufficient for `ConfirmedByMultipleIndependentImplementations`.

```text
AuditStatus: NotRun
FutureEvidenceSource: ProjectBaselineAggregateAudit
```

## Source matrix

| Source | Pin/category | Supports | Limitations/lineage |
|---|---|---|---|
| EA FinalSun / FinalAlert 2 | `6abf0f557469baea73079c6bf6550709e2e3584e`; official editor | LocalSize/Theater UI, House/SpecialFlags fields, editor preparation/defaults | not runtime; editor/UI constraints only; XCC-related repository lineage |
| WAE | `b4c9481e9b00fb0a38739049a046f528b6054ce2`; editor | Size/LocalSize, Countries/Houses, House fields, Allies, start/mode metadata | named implementation; defaults/repairs not runtime facts |
| OpenRA | `a520984d91eda9de48a62b1d15c1e3bad0d4fb1a`; reimplementation | supplementary map/session architecture | target engine differs from Westwood runtime |
| CnCNet XNA client | `e6e367bbe04c1a0dc1e34a8fed2856ea3ab7e8c4`; client | lobby player/side/color/start/team and map categorization | client/session behavior, not authored map facts |
| MapTool | `f85f2226905496139f1258b5854fad915f9bbac6`; tool | map transformation/canonicalization | community-derived conventions, no runtime proof |
| CNCMaps | `afb9c1ec118f5128cbc1f3fb5e35c7dfa0e422fb`; parser/renderer | supplementary map metadata behavior | mixed/imported lineage |
| ModEnc/PPM/RA2 DIY | fixed community sources | field names, authoring conventions, product observations | community evidence only |
| Ares/Phobos | versioned extension docs | extension House/metadata behavior | extension-only, never vanilla |

All implementation sources are `ImplementationSpecificBehavior` unless the EA editor supports `ConfirmedByOfficialToolSource`. Stable community conventions use `ConfirmedCommunityConvention`. Cross-tool candidates remain `Underconfirmed` when independence/runtime applicability are not proven.

## Key conflicts

- Size origins and LocalSize meaning across editor, client and map domains;
- House instance versus Country/HouseType/Side/player terminology;
- House/Country list order, gaps, duplicates and map-local precedence;
- authored `Player`/`PlayerControl` versus lobby/network control;
- directed authored Allies versus symmetric lobby/gameplay teams;
- authored Credits versus carry-over, lobby money and runtime economy;
- Waypoint start conventions and mode/player-count authority;
- SpecialFlags applicability and inherited TS labels;
- Digest generation/enforcement and Lighting/environment boundaries.

Direct disagreements are `ConflictingSources`; missing complete runtime behavior is `Unresolved`.

## Evidence summary

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert exposes LocalSize/Theater/House/SpecialFlags behavior | `ConfirmedByOfficialToolSource` | EA editor | Official tool only. | Source-pinned editor profile. | `NotRun` |
| WAE metadata/House/Allies behavior | `ImplementationSpecificBehavior` | WAE | Named editor. | Preserve raw instead of repairs. | `NotRun` |
| CnCNet lobby/session assignment | `ImplementationSpecificBehavior` | CnCNet client | Client state, not map runtime state. | Outside Core map semantics. | `NotRun` |
| Stable metadata and House field conventions | `ConfirmedCommunityConvention` | Community docs | Convention only. | Product/profile annotations. | `NotRun` |
| Common rectangle and start-location candidates | `Underconfirmed` | Tools/community | Runtime strictness and lineage independence unproven. | Explicit profiles. | `NotRun` |
| Precedence, symmetry, player control and mode classification | `ConflictingSources` | Editors/clients/community/extensions | Similar fields represent different layers. | Preserve candidates/provenance. | `NotRun` |
| Exact runtime initialization, diplomacy, defaults and Digest enforcement | `Unresolved` | No runtime source | No complete contract. | Future adapters. | `NotRun` |
| No repair/renumber/symmetrize/infer | `DefensiveDesign` | Project policy | Preservation and layering. | Fail closed. | `NotRun` |

## License boundary

Do not port source code, translate parser/control flow, copy catalogs or source-shaped tests. Use factual field/layout observations, original schemas, neutral labels, provenance and independently designed synthetic fixtures.
