# Future ProjectBaseline sanitized audit request

> **Source notice:** This audit was not run and ProjectBaseline was not read. `code_imported: false`.

## Status

```text
AuditStatus: NotRun
FutureEvidenceSource: ProjectBaselineAggregateAudit
```

These fields are not an evidence grade. Future aggregates cannot become original-runtime evidence, alter policy or promote compatibility.

## Allowed aggregates

Broad registry/product/factory categories; registry counts/gaps/duplicates/case collisions; anonymous ownership and type-binding states; prerequisite shape/token/group categories; TechLevel/BuildLimit/Cost/BuildTime coarse buckets; factory/category/capability counts; deploy/upgrade/power/sidebar field presence; diagnostics; non-linkable aggregate hashes; Memory/Stream/short-read/MIX equivalence.

## Forbidden output

No type/factory/House/Country/Side names, Rules text, exact registry lists or prerequisite expressions, exact Owner lists/Cost/BuildTime/TechLevel/BuildLimit, technology graph, queue/product order, placement coordinates/foundations/exits, cameo/Art/resource IDs, Trigger/AI contents, screenshots, bytes/hex/Base64 or per-type/per-map hashes.

## Discipline

Compare only preselected registry, ownership, prerequisite, TechLevel, BuildLimit, cost/time, factory, queue, power, capture, placement and sidebar profiles. Never choose by successful production, familiar UI, fewer diagnostics or available Art. Multiple successes remain ambiguous.

## Safety

Read-only; bounded files/bytes/sections/tokens/graphs/diagnostics/runtime; no map/game/editor/Unity execution; no queue/availability evaluation against actual player state; no asset extraction; no ProjectBaseline modification. These are `DefensiveDesign` requirements.

## Report

```text
AuditMetadata
- AuditStatus = NotRun
- FutureEvidenceSource = ProjectBaselineAggregateAudit
- SelectionBasis
- SelectedProfiles

ProductionAggregate
- RegistryBindingCounts
- PrerequisiteShapeBuckets
- AvailabilityFieldBuckets
- CostTimeFactoryCategories
- TransformPowerSidebarCategories
- DiagnosticCounts
- InputModeEquivalence
- AggregateHash
- CurrentEvidenceGrade

DisclosureReview
- ForbiddenFieldCheck
- MinimumGroupSizeCheck
- ReconstructabilityReview
```

Stop without publication if identities/exact expressions cannot be removed, a category identifies a type/map, a hash is linkable, limits fail, input modes diverge without bounded diagnostics, or any operation would modify ProjectBaseline.
