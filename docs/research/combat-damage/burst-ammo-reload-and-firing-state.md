# Burst, ammo, reload and firing state

> **Source notice:** Public-source research only. ProjectBaseline was not read. `code_imported: false`.

## State split

```text
AuthoredAmmoCapacity
RuntimeAmmoCount
WeaponCooldown
ReloadState
BurstDescriptor
BurstState
FiringCommand
FiringResult
PresentationState
```

`ROF`, `Burst`, `Ammo` and reload fields are configuration candidates, not a state machine.

## Evidence

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert exposes Weapon/Ammo/ROF/Burst fields and editor validation | `ConfirmedByOfficialToolSource` | EA editor | Official tool behavior only. | Named editor profile. | `NotRun` |
| OpenRA/Ares/Phobos/Vinifera burst, reload and ammo models | `ImplementationSpecificBehavior` | Named implementations | Product/extension-specific. | Keep separate profiles. | `NotRun` |
| Stable ROF/Burst/Ammo authoring conventions | `ConfirmedCommunityConvention` | ModEnc/PPM/community docs | Convention only. | Preserve raw fields and applicability. | `NotRun` |
| Common cooldown/burst/ammo candidates | `Underconfirmed` | Tools/community | Runtime units, defaults and lineage independence unproven. | Explicit timing/consumption profiles. | `NotRun` |
| Per-shot/per-burst consumption, retargeting, reload and fallback behavior | `ConflictingSources` | Engines/extensions/community | Models differ directly. | Preserve alternatives. | `NotRun` |
| Exact runtime timing, target snapshot, abort, save/load and aircraft reload | `Unresolved` | No runtime source | No complete state machine. | Future simulation adapter. | `NotRun` |
| Stable command/shot identities, no parser mutation and checked counters | `DefensiveDesign` | Project policy | Determinism/architecture. | No animation-driven authoritative fire. | `NotRun` |

## Contracts

`BurstDescriptor` preserves count, delays, ammo candidates, muzzle sequence, target-snapshot and abort/retarget policies. `FiringCommand` carries tick, source, slot/mount, target snapshot and stable ordinal. `FiringResult` separates acceptance, projectile/impact commands, ammo/cooldown mutations and presentation events.

Missing, zero, negative, overflow and sentinel candidates remain distinct. Presentation charge, muzzle flash, recoil, report and shell casing never trigger authoritative shots.
