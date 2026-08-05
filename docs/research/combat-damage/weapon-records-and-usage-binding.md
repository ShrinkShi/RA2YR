# Weapon records and usage binding

> **Source notice:** Public-source research only. ProjectBaseline was not read. `code_imported: false`.

## Separation

```text
WeaponTypeDefinition
WeaponReferenceRaw
WeaponSlot/Mount
SelectedWeaponCandidate
RuntimeWeaponInstance
Ammo/Cooldown/BurstState
TargetingResult
FiringCommand
```

Primary, Secondary, elite, death, special and extension slots retain explicit provenance. Missing slots do not silently fall back.

## Evidence

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert exposes Weapon fields and type-slot editor catalogs | `ConfirmedByOfficialToolSource` | EA editor | Official tool only. | Named editor profile. | `NotRun` |
| WAE/OpenRA/Chrono Divide/extensions bind and select weapons | `ImplementationSpecificBehavior` | Named implementations | Target/profile-specific behavior. | Keep models separate. | `NotRun` |
| Common Damage/ROF/Range/Projectile/Warhead/Burst field conventions | `ConfirmedCommunityConvention` | ModEnc/PPM/community docs | Convention only. | Preserve raw fields and applicability. | `NotRun` |
| Standard Weapon reference graph and common numeric candidates | `Underconfirmed` | Tools/community | Runtime defaults, units and independent lineage unproven. | Explicit product/layout profile. | `NotRun` |
| Primary/Secondary/elite fallback, slot selection, range/target and death/special behavior | `ConflictingSources` | Engines/extensions/community | Public models differ. | No implicit fallback or plausibility selection. | `NotRun` |
| Exact runtime weapon selection, readiness, target evaluation and firing sequence | `Unresolved` | No runtime source | No complete state machine. | Future simulation adapter. | `NotRun` |
| Raw reference graph, no repair, checked numeric candidates and no execution | `DefensiveDesign` | Project policy | Preservation/architecture. | Fail closed. | `NotRun` |

## Raw model

`WeaponDefinitionRaw` retains exact key/value occurrences, numeric text, Projectile/Warhead references, report/animation references, unknown fields, duplicates and extension provenance. `WeaponUsageBinding` records actor type, slot/mount, elite/state conditions, candidate Weapon target, ambiguity and diagnostics.

Damage, ROF, Range, MinimumRange, Burst and Speed-like values are parsed candidates only. Missing Projectile/Warhead/Art/Sound references remain unresolved; no object is deleted and no default Weapon is manufactured.
