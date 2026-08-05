# Layer and domain boundaries

> **Source notice:** Public-source research only. ProjectBaseline was not read. `code_imported: false`.

## Domains

Keep distinct: Rules definition identity, Art/presentation identity, actor runtime identity, weapon mount/slot, projectile runtime identity, target object/cell, Armor identity, status instance, simulation coordinate, renderer coordinate and Unity coordinate.

## Ownership

Weapon definitions own authored numeric/reference candidates; Projectile definitions own authored flight/collision candidates; Warheads own damage/effect candidates; actors own mounts and runtime state; simulation owns targeting, movement, collision, health and status; presentation owns art/audio/effects.

## Evidence

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert exposes combat section/key catalogs | `ConfirmedByOfficialToolSource` | EA editor | Official tool only. | Named editor profile. | `NotRun` |
| Named engines/tools map fields into their combat domains | `ImplementationSpecificBehavior` | Public implementations | Target-specific architecture. | Comparison only. | `NotRun` |
| Common Weapon/Projectile/Warhead reference graph | `Underconfirmed` | Tools/community | Runtime missing-reference and slot behavior remain unsourced. | Explicit binding profile. | `NotRun` |
| Field names reused across configuration, runtime and presentation | `ConflictingSources` | Tools/extensions/community | Similar names have different ownership. | Domain-tag every value. | `NotRun` |
| Complete stock runtime domain/state ownership | `Unresolved` | No runtime source | No complete state model. | Future simulation adapter. | `NotRun` |
| Immutable descriptors, no execution and no Unity dependency | `DefensiveDesign` | Project policy | Architecture boundary. | Fail closed. | `NotRun` |

Missing references produce unresolved graph edges rather than fallback weapons/projectiles/warheads. Image, animation and sound never define collision, targeting or damage. Runtime snapshots never rewrite authored Rules/Art data.
