# Deploy, repair, sell, capture, and enter

> **Source notice:** Public-source research only. ProjectBaseline was not read. `code_imported: false`.

## Command boundaries

Deploy, undeploy, repair, sell, capture, infiltration, sabotage, ordinary Enter, transport entry, garrison entry, refinery docking and grinder entry are separate command families even when they share cursor, approach or occupancy logic.

```text
RawCommand
→ capability and relationship queries
→ typed target validation
→ path/approach candidate
→ reservation candidate
→ accepted command
→ mission transition candidate
→ future subsystem result
```

The parser and binder do not create/delete actors, transfer ownership, change health, charge/refund credits or mutate occupancy.

## Evidence

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert exposes deploy/repair/sell/capture/enter fields and command catalogs | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Official editor behavior only. | Named editor profile. | `NotRun` |
| OpenRA, clients and extensions implement these command families | `ImplementationSpecificBehavior` | Named implementations | Target/profile-specific. | Keep separate profiles. | `NotRun` |
| Stable command names and authoring conventions | `ConfirmedCommunityConvention` | ModEnc/PPM/RA2 DIY | Convention only. | Preserve raw tokens/product applicability. | `NotRun` |
| Typed capability/target/approach contracts | `Underconfirmed` | Tools/community | Runtime ordering and lineage independence unproven. | Explicit command profile. | `NotRun` |
| Capture versus infiltration, repair modes, sell/deploy interactions and Enter target domains | `ConflictingSources` | Engines/extensions/community | Public models differ directly. | Preserve alternatives. | `NotRun` |
| Exact stock runtime transitions, refunds, transfer state and cancellation behavior | `Unresolved` | No original-runtime source located | No complete state machine. | Future deterministic simulation adapter. | `NotRun` |
| No mutation during parsing and explicit transfer/reservation policies | `DefensiveDesign` | Project policy | Preservation/architecture. | Fail closed. | `NotRun` |

## Required separation

Deploy transformations preserve explicit owner, health, ammo, veterancy, cargo, passenger, mission and placement transfer candidates. Repair separates cursor, facility/weapon mode, cost and mission state. Sell separates UI mode, command, deconstruction, refund and survivor candidates. Capture/infiltration/sabotage never collapse into ordinary Enter. Cancellation preserves deterministic reason codes and the original request.
