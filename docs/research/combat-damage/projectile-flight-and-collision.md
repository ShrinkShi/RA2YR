# Projectile flight and collision

> **Source notice:** Public-source research only. ProjectBaseline was not read. `code_imported: false`.

## Separation

```text
ProjectileDefinitionRaw
ProjectileCommand
ProjectileRuntimeState
Tracking/FlightProfile
CollisionQuery
ImpactCommand
RenderedProjectile
```

`Image`, `Inviso`, `Arcing`, `ROT`, velocity, acceleration, height and subject-to fields are authored candidates, not Unity physics configuration or an algorithm selector by themselves.

## Evidence

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert exposes Projectile fields and editor validation | `ConfirmedByOfficialToolSource` | EA editor | Official tool only. | Named editor profile. | `NotRun` |
| OpenRA/Chrono Divide/extensions implement flight/collision profiles | `ImplementationSpecificBehavior` | Named implementations | Target/profile-specific algorithms. | Comparison profiles. | `NotRun` |
| Common Inviso/Arcing/ROT/subject-to terminology | `ConfirmedCommunityConvention` | ModEnc/PPM/community docs | Convention only. | Preserve raw fields/product applicability. | `NotRun` |
| Several projectile families and tracking/collision candidates | `Underconfirmed` | Tools/community | Exact runtime mapping and independent lineage unproven. | Explicit profile selection. | `NotRun` |
| ROT/tracking, gravity, speed, detonation and terrain/bridge collision | `ConflictingSources` | Engines/extensions/community | Public algorithms differ directly. | Preserve alternatives; no trial simulation. | `NotRun` |
| Exact runtime integration, substeps, collision shape/order, range termination and save state | `Unresolved` | No runtime source | No complete contract. | Future deterministic simulation adapter. | `NotRun` |
| Logical collision, stable projectile/impact IDs and no Image/Unity inference | `DefensiveDesign` | Project policy | Determinism/architecture. | Renderer and physics adapters are non-authoritative. | `NotRun` |

## Contracts

`ProjectileCommand` carries source, weapon, target snapshot, launch tick/position, facing and stable ordinals. Runtime state carries deterministic position/velocity/profile/RNG/state. `CollisionQuery` explicitly identifies terrain, target, bridge/elevation and object candidates. `ImpactCommand` records reason, logical position, target/cell candidates and stable order.

Line-of-fire, obstacle tests, proximity, multi-cell buildings, bridge layers and terrain interaction require explicit world-query profiles. Missing Art does not remove the logical projectile; an invisible projectile can still be simulated without a rendered object.
