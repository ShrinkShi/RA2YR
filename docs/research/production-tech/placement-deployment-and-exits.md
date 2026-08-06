# Placement, deployment and exits

> **Source notice:** Public-source research only. ProjectBaseline was not read. `code_imported: false`.

## Transactions

```text
ProductionCompletion
ExitRequest
SpawnRequest
BuildingPlacementRequest
PlacementPreview
PlacementValidation
PlacementReservation
Placement/SpawnResult
Deploy/Undeploy/UpgradeTransformation
```

Completion creates a request, not an actor/building.

## Evidence

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert exposes foundation/deploy/upgrade/exit-related fields | `ConfirmedByOfficialToolSource` | EA editor | Official field/editor behavior only. | Named editor profile. | `NotRun` |
| OpenRA/Ares/Phobos implement placement, exits and transformations | `ImplementationSpecificBehavior` | Named implementations | Target/extension-specific. | Keep separate profiles. | `NotRun` |
| Stable foundation, deploy and exit authoring conventions | `ConfirmedCommunityConvention` | ModEnc/PPM/community docs | Convention only. | Preserve authored fields. | `NotRun` |
| Completion-to-placement/exit as a common architecture candidate | `Underconfirmed` | Tools/community | Exact runtime queue retention and retry semantics unproven. | Explicit transaction profile. | `NotRun` |
| Exit blocking, placement cancel/refund, deploy transfer and upgrade occupancy | `ConflictingSources` | Engines/extensions/community | Models differ directly. | Preserve alternatives. | `NotRun` |
| Exact runtime reservation, retry, spawn facing, transformation and save/load behavior | `Unresolved` | No runtime source | No complete contract. | Future simulation adapter. | `NotRun` |
| Preview is non-authoritative, authored foundation only and no implicit actor creation | `DefensiveDesign` | Project policy | Architecture/fail-closed boundary. | Simulation validates occupancy/buildability. | `NotRun` |

## Boundaries

Visual placement preview, SHP/VXL bounds and cameo do not define foundation/buildability. Exit/dock/factory bay are authored candidates separate from foundation and queue ownership. Deployment/undeployment/upgrades preserve source/target definitions, actor identity candidates, owner/health/ammo/veterancy/passenger/mission transfer policies and occupancy transactions without selecting defaults during parsing.
