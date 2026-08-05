# Future ProjectBaseline sanitized audit request

> **Source notice:** This audit was not run and ProjectBaseline was not read. `code_imported: false`.

## Status

```text
AuditStatus: NotRun
FutureEvidenceSource: ProjectBaselineAggregateAudit
```

These fields are not an evidence grade. Future aggregates cannot become original-runtime evidence, alter policy or promote compatibility.

## Allowed aggregates

Broad product/type categories; Weapon/Projectile/Warhead/Armor section presence; anonymous reference-binding states; numeric-field presence and coarse magnitude/sign buckets; Armor/Verses length and spelling categories; projectile/targeting/area-effect/ammo/status categories; duplicate/case/dangling diagnostics; bounded-input outcomes; non-linkable aggregate hashes; Memory/Stream/short-read/MIX equivalence.

## Forbidden output

No type, Weapon, Projectile, Warhead or Armor names; Rules/Art text; exact Damage/ROF/Range/Burst/Verses/projectile fields; complete reference graphs; ordered shots/impacts; Trigger or AI records; object/resource names; positions/topology; SHP/VXL/audio references; screenshots; bytes/hex/Base64; per-type/per-record/per-map hashes; or reconstructable combat configuration.

## Discipline

Compare only preselected product, armor, Verses, projectile, collision, damage-order, rounding, targeting, CellSpread, friendly-fire, ammo/reload and status profiles. Never select by successful lookup, familiar damage result, rendered appearance or fewer diagnostics. Multiple successful profiles remain ambiguous.

## Safety

Read-only, bounded files/bytes/sections/tokens/diagnostics/runtime, no network, no game/editor/Unity execution, no combat simulation, no resource extraction and no ProjectBaseline modification. These are `DefensiveDesign` requirements.

## Report

```text
AuditMetadata
- AuditStatus = NotRun
- FutureEvidenceSource = ProjectBaselineAggregateAudit
- SelectionBasis
- SelectedProfiles

CombatAggregate
- SectionAndBindingCounts
- NumericShapeBuckets
- ArmorVersesCategories
- ProjectileTargetingCategories
- DamageAmmoStatusCategories
- DiagnosticCounts
- InputModeEquivalence
- AggregateHash
- CurrentEvidenceGrade

DisclosureReview
- ForbiddenFieldCheck
- MinimumGroupSizeCheck
- ReconstructabilityReview
```

Stop without publication if identities/exact values cannot be removed, a category identifies configuration, a hash is linkable, limits fail, input modes diverge without bounded diagnostics, or any operation would modify ProjectBaseline.
