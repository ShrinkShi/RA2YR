# Future ProjectBaseline sanitized audit request

> **Source notice:** This audit was not run and ProjectBaseline was not read. `code_imported: false`.

## Status

```text
AuditStatus: NotRun
FutureEvidenceSource: ProjectBaselineAggregateAudit
```

These fields are not an evidence grade. Future aggregates cannot become original-runtime evidence, alter policy or promote compatibility.

## Allowed aggregates

Broad product/unit-family categories; Mission/Script/command field presence; anonymous command-kind and target-kind counts; recognized/unknown mission/action categories; queue/replace/modifier and lifecycle-state categories; capability/target-validation diagnostic counts; guard/hold/stop/engagement classifications; non-linkable aggregate hashes; Memory/Stream/short-read/MIX equivalence.

## Forbidden output

No map/type/actor/House/Team/Script names or IDs, INI text, exact mission/action tokens or argument sequences, ordered command queues, target identities/positions/routes, state-machine traces, AI/Trigger data, Rules/Art/resource IDs, screenshots, bytes/hex/Base64, per-map/per-actor/per-command hashes or reconstructable behavior.

## Discipline

Compare only preselected mission-catalog, command, target, queue, guard/hold/stop, Script, AI arbitration, transition and lifecycle profiles. Never select by successful movement/attack, familiar animation, fewer diagnostics or one game's visible behavior. Multiple successes remain ambiguous.

## Safety

Read-only; bounded files/bytes/sections/tokens/commands/diagnostics/runtime; no game/editor/Unity execution; no actor commands, movement, combat, harvesting or AI simulation; no ProjectBaseline modification. These are `DefensiveDesign` requirements.

## Report

```text
AuditMetadata
- AuditStatus = NotRun
- FutureEvidenceSource = ProjectBaselineAggregateAudit
- SelectionBasis
- SelectedProfiles

MissionCommandAggregate
- MissionAndActionCategories
- CommandTargetBuckets
- QueueTransitionCategories
- GuardEngagementCategories
- DiagnosticCounts
- InputModeEquivalence
- AggregateHash
- CurrentEvidenceGrade

DisclosureReview
- ForbiddenFieldCheck
- MinimumGroupSizeCheck
- ReconstructabilityReview
```

Stop without publication if identities/sequences cannot be removed, a category identifies behavior/map, a hash is linkable, limits fail, input modes diverge without bounded diagnostics, or any operation would modify ProjectBaseline.
