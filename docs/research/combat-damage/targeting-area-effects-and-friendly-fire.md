# Targeting, area effects and friendly fire

> **Source notice:** Public-source research only. ProjectBaseline was not read. `code_imported: false`.

## Independent decisions

```text
CanSelectTarget
CanAcquireAutomatically
CanForceFire
CanRetaliate
CanFireAtTarget
CanTrackTarget
CanCollide
CanApplyWarhead
CanDamageTarget
CanApplyStatus
```

No single Verses value or `AffectsAllies` field defines every decision.

## Evidence

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert exposes targeting/warhead fields and editor catalogs | `ConfirmedByOfficialToolSource` | EA editor | Official tool only. | Named editor profile. | `NotRun` |
| OpenRA/extensions implement target filters, spread and ally rules | `ImplementationSpecificBehavior` | Named implementations | Target/profile-specific. | Keep separate. | `NotRun` |
| Community documents CellSpread, AffectsAllies and Verses side effects | `ConfirmedCommunityConvention` | ModEnc/PPM/community docs | Convention only. | Preserve field-level provenance. | `NotRun` |
| CellSpread/AffectsAllies/Verses as targeting and AoE candidates | `Underconfirmed` | Tools/community | Exact runtime scope and precedence unproven. | Explicit targeting/AoE profiles. | `NotRun` |
| Distance metric, falloff, building cells, owner/self/allies and force-fire rules | `ConflictingSources` | Engines/extensions/community | Public models differ. | Canonical enumeration; preserve alternatives. | `NotRun` |
| Exact runtime acquisition, line-of-fire, area target order and special-effect eligibility | `Unresolved` | No runtime source | No complete contract. | Future world-query/simulation adapter. | `NotRun` |
| No visual/lookup plausibility, stable target ordering and separate decisions | `DefensiveDesign` | Project policy | Determinism/architecture. | Fail closed. | `NotRun` |

## Area contract

`AreaEffectDescriptor` records center kind, shape, distance metric, radius/CellSpread, falloff, elevation/bridge policy, object enumeration, multi-cell building policy, ally/self/owner filters, stable ordering and effect scope. Parser does not query world objects or apply effects.

## Terrain and line of fire

Terrain, cliff, bridge, wall, building, projectile-height and subject-to fields are explicit candidates. Renderer depth, sprite transparency and Unity collider geometry are never authoritative line-of-fire or collision evidence.
