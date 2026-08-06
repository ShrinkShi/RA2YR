# Future ProjectBaseline sanitized audit request

> **Source notice:** This future audit was not run and ProjectBaseline was not read. `code_imported: false`.

## Status

```text
AuditStatus: NotRun
FutureEvidenceSource: ProjectBaselineAggregateAudit
```

These fields are not an evidence grade. A future aggregate audit cannot automatically become `ConfirmedByOriginalRuntimeSource`, alter project policy or promote compatibility.

## Allowed aggregates

A future read-only task may report broad map/theater/environment categories; section/field presence; raw numeric-shape histograms and coarse value bins; normal/Ion/Dominator completeness; duplicate/invalid/locale-ambiguous categories; static/dynamic/time/weather-capability classifications; logical media-reference binding status; local-light property-presence counts; Fog/Shroud metadata presence; environment-command categories; diagnostics; non-linkable aggregate hashes; and Memory/Stream/short-read/MIX equivalence.

## Forbidden output

No map names/paths, INI text, exact Lighting tuples or per-map component values, Theme/Sound/Speech/Movie/EVA IDs, object/type names, local-light positions, Trigger IDs/opcodes/ordered commands, palettes, image/audio bytes, screenshots/renders, per-map/per-field hashes, topology, hex/Base64 or reconstructable environment information.

## Profile discipline

Only preselected numeric, normal/Ion/Dominator, composition, color-space, clamp, weather, media, local-light and visibility profiles may be compared. Do not select by familiar screenshot, rendered similarity, fewer diagnostics, available resource or successful playback.

## Safety

Read-only access; bounded files/bytes/sections/tokens/diagnostics/runtime; no network; no map modification; no Unity/game/editor/audio/render execution; no resource extraction; no generated images/audio; no weather/audio/visibility simulation. These are `DefensiveDesign` requirements.

## Report

```text
AuditMetadata
- AuditStatus = NotRun
- FutureEvidenceSource = ProjectBaselineAggregateAudit
- SelectionBasis
- SelectedProfiles

EnvironmentAggregate
- SectionAndFieldCounts
- NumericShapeCategories
- ValueBuckets
- ProfileCompleteness
- TimeWeatherCategories
- MediaBindingCategories
- LocalLightCategories
- VisibilityMetadataCategories
- CommandCategories
- DiagnosticCounts
- InputModeEquivalence
- AggregateHash
- CurrentEvidenceGrade

DisclosureReview
- ForbiddenFieldCheck
- MinimumGroupSizeCheck
- ReconstructabilityReview
```

`CurrentEvidenceGrade` uses only the nine-item closed vocabulary and records pre-audit public evidence.

## Stop conditions

Stop without publication if sanitization cannot remove exact values/identities, a category identifies a map, a hash is linkable, ordered commands or object positions appear, resource limits fail, input modes disagree without bounded diagnostics, or any operation would modify ProjectBaseline.
