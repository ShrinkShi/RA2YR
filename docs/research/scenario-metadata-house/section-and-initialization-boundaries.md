# Section and initialization boundaries

> **Source notice:** ChatGPT Web public-source research. ProjectBaseline was not read. `code_imported: false`.

## Initialization graph

```text
raw Basic/Map/SpecialFlags/House/Country sections
→ geometry and Theater candidates
→ Country/House identity graph
→ raw House property binding
→ alliance and start candidates
→ game-mode/session initialization descriptor
→ future simulation/session adapters
```

No parser stage creates players, Houses, alliances, credits, units, teams, network peers or Unity objects.

## Section classes

Keep distinct:

- scenario metadata sections;
- House-instance and Country/HouseType definitions;
- map-local Rules-like definitions;
- editor/client-private metadata;
- lobby/session configuration;
- unknown sections.

A shared section name across ordered INI layers does not authorize whole-section replacement. Per-key provenance and an explicit `ScenarioLocalCompositionPolicy` are required.

## Identity and state boundaries

```text
House instance ≠ Country/HouseType ≠ Side
≠ player slot ≠ controller ≠ network peer
≠ TeamType ≠ lobby team
```

Authored starting Credits, TechLevel, IQ, Edge, Color, Allies and base nodes are raw/configuration candidates. Runtime economy, diplomacy, production, AI state and score are separate.

## Evidence classification

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert editor sections, fields and repair/default workflows | `ConfirmedByOfficialToolSource` | EA editor | Official tool only. | Named editor profile. | `NotRun` |
| WAE/CnCNet/OpenRA composition or session behavior | `ImplementationSpecificBehavior` | Named implementations | Tool/client/engine-specific. | Keep each profile separate. | `NotRun` |
| Common metadata/House/Country section conventions | `ConfirmedCommunityConvention` | Community documentation | Does not establish runtime precedence. | Preserve source applicability. | `NotRun` |
| Runtime initialization order and cross-layer precedence | `Underconfirmed` | Tool convergence and documentation | Original-runtime applicability is incomplete. | Explicit dependency graph and composition policy. | `NotRun` |
| Authored map state versus lobby/session/runtime state | `ConflictingSources` | Map, editor and client terminology | Similar fields serve different layers. | Never merge by name alone. | `NotRun` |
| Exact runtime House/player/session initialization | `Unresolved` | No original-runtime source located | No complete algorithm. | Future adapter. | `NotRun` |
| Raw/provenance preservation and no object creation | `DefensiveDesign` | Project policy | Architecture boundary. | Fail closed on ambiguity. | `NotRun` |
