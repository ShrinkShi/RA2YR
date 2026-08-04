> **Source notice:** This document was prepared by **ChatGPT Web** from public sources only. ProjectBaseline was not read. This is not a Codex artifact. GPL and unclear-license implementations are reference-only; no code was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

# Placement, deployment and exits

## Placement contract

```text
PlacementPreview
PlacementRequest
PlacementQuery
FoundationFit
BuildabilityResult
OccupancyReservation
ConstructionCommand
PlacementResult
RuntimeBuildingInstance
```

The UI preview is advisory. The authoritative query consumes a simulation snapshot and stable reservation order.

## Placement inputs

- authored Foundation, including irregular extension foundations;
- terrain buildability;
- static and dynamic occupancy;
- construction-yard adjacency/range candidates;
- current owner;
- scenario restrictions;
- water, shore and naval layers;
- bridges and elevated layers;
- resources and overlays;
- shroud/exploration policy;
- walls and gates;
- Bib and extension cells;
- upgrades and host attachment;
- optional rotation profile;
- current placement reservations.

Image dimensions, transparency and selection bounds do not define the foundation.

## Result dimensions

```text
InBounds
FoundationFits
TerrainAllowed
LayerAllowed
AdjacencySatisfied
OwnershipSatisfied
ScenarioAllowed
ShroudAllowed
NoStaticBlocker
NoDynamicBlocker
ReservationWon
UpgradeHostValid
```

Several failures may coexist.

## Unit exit contract

```text
ExitDescriptor
- FactoryTypeIdentity
- ExitCellCandidates[]
- ExitLayer
- RequiredFacingCandidates[]
- DockOrPadCandidates[]
- AlternateSearchPolicy
- RallyPointPolicy
- EvidenceGrade
```

Completion does not spawn at the foundation center. A blocked exit does not delete the product.

```text
Factory capability
!= Exit descriptor
!= Docking slot
!= Spawn cell
!= Rally point
```

## Naval and aircraft production

Naval products require explicit water/shore/layer candidates. Aircraft may require pads, docks or airborne delivery depending on profile. `NumberOfDocks` does not prove queue capacity or parallel product count.

## Deploy and undeploy

```text
TypeTransformationCandidate
- SourceType
- TargetType
- TriggerCapability
- PlacementCandidate
- TransferPolicyCandidate
- BuildLimitEquivalenceCandidate
- OwnerPolicyCandidate
- HealthTransferCandidate
- CargoTransferCandidate
- AmmoTransferCandidate
- VeterancyTransferCandidate
```

Candidate fields include `DeploysInto`, `UndeploysInto`, `DeployToLand`, `IsSimpleDeployer`, `PowersUpBuilding`, `PowersUnit` and extension attachment fields.

The binder does not perform the transformation or choose transfer semantics.

## Upgrades

Independent identities are required for:

- upgrade product type;
- host type requirements;
- host runtime actor;
- attachment slot;
- BuildLimit contribution;
- prerequisite contribution;
- power production/drain;
- removal and capture behavior;
- sidebar entry;
- placement/attachment command.

## PR dependencies

- PR #31: scenario placements.
- PR #35: render anchors and visual previews.
- PR #37: foundation occupancy and buildability.

These are consumed as boundaries only.

## Source anchors

- OpenRA `Building`, `Exit` and placement architecture, independent implementation.
- Ares irregular foundations, factories and deployment extensions, extension-only.
- ModEnc and PPM deploy/build-limit discussions, community evidence.

No placement or transformation code was imported; `code_imported: false`.
