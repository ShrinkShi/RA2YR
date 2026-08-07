# Sanitized future ProjectBaseline audit request

> **Source notice:** ChatGPT Web public-source research only. ProjectBaseline was not read. This is not a Codex Agent artifact. No GPL or unclear-license code was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

## Purpose and status

This document specifies a future read-only Codex audit that can test public-source hypotheses against the user's local baseline without exposing maps or reconstructable placement data. This audit was not run by ChatGPT Web.

```text
AuditStatus: NotRun
FutureEvidenceSource: ProjectBaselineAggregateAudit
```

These fields are not a formal evidence grade and do not imply that ProjectBaseline was read, observed, or confirmed. Local observations cannot automatically become `ConfirmedByOriginalRuntimeSource`, change project policy, or promote compatibility.

## Selection basis

The local audit should select aggregate samples across:

- all six theater categories where present;
- campaign and skirmish/standard multiplayer categories;
- small, medium, and large map-size buckets;
- Structure-, Unit-, Infantry-, Aircraft-, Terrain-, Smudge-, Waypoint-, and CellTag-bearing maps;
- neutral/civilian/special owner categories;
- unknown or extension type candidates;
- out-of-domain, duplicate-key, unknown-tail, and dangling-reference candidates.

The report publishes selection rules and aggregate counts, never map names.

## Required local processing stages

```text
bounded map source
→ lossless INI aggregate scan
→ placement section occurrence counts
→ raw token-count classification
→ explicit layout candidates
→ coordinate aggregate classification
→ composed owner/type binding aggregate
→ opaque reference aggregate
→ consistency diagnostics
→ canonical aggregate hashes
```

The audit must not auto-select profiles by success count or plausibility.

## Allowed public output

### Selection and provenance

- `SelectionBasis`;
- theater and campaign/skirmish/other category counts;
- source container/provenance categories without filenames or paths;
- map-size bucket counts without individual tuples.

### Section and record aggregates

- presence and record-count aggregates for Structures, Units, Infantry, Aircraft, Terrain, Smudge, Waypoints, and CellTags;
- duplicate-section counts;
- zero/nonzero-record categories;
- raw field-count and empty/trailing-token histograms;
- unknown-tail, duplicate-key, normalized-collision, gap, nonnumeric-key, and key-length aggregates.

No key or token text is published.

### Coordinate aggregates

- decode success/failure counts under named profiles;
- coarse X/Y minima/maxima and negative/overflow counts;
- Size/LocalSize/IsoMap-presence categories;
- axis-profile ambiguity counts;
- Infantry subcell coarse ranges and cooccupancy counts.

No coordinate sequence or per-map coordinate set is published.

### Owner and type binding aggregates

- owner binding status counts;
- type registry binding success/failure by family;
- registry gap/duplicate categories;
- map-local contribution and missing Art/visual-binding counts;
- extension-profile candidate counts.

Owner and type names are forbidden.

### State aggregates

- coarse health, facing, mission, veterancy, group, High, Follows, upgrade, recruitment and unknown-tail categories;
- recognized/unknown/empty mission counts;
- sentinel/ambiguity counts.

No raw mission or state token list is published.

### Reference and overlap aggregates

- Tag, CellTag, Trigger, Follows, waypoint and opaque-reference status counts;
- duplicate/cycle/overlap/cooccupancy categories;
- diagnostic counts by code and severity.

### Hashes and equivalence

Allowed hashes must be aggregate and non-reconstructable:

- canonical aggregate hashes of sorted category/count tuples;
- category-level result hashes after removing names, paths, values, coordinates and source-order data;
- Memory/Stream/short-read Stream/MIX equivalence booleans and aggregate mismatch counts.

No per-map, per-record, per-owner, per-type or per-section-value hashes.

## Forbidden public output

The audit must not expose:

- map/scenario/file/container names or paths;
- usernames, machine names or volume labels;
- INI bodies, placement records, raw keys or raw tokens;
- owner/type/object names or complete lists;
- coordinates, object positions, per-object tuples or map layouts;
- waypoint, Tag, Trigger, TeamType, TaskForce, ScriptType or AITrigger IDs;
- type-to-Art/SHP/VXL/HVA mappings;
- Overlay, IsoMap, TMP, palette, SHP, VXL, HVA or Preview content;
- images, screenshots, thumbnails or rendered maps;
- Base64, hex, compressed or decoded bytes;
- per-object, per-record, per-map or per-resource hashes;
- any reconstructable or identifying information.

## Privacy thresholds

Small categories that could identify a map must be suppressed or merged. Rare unknown type, owner, mission or extension categories are reported only as broad counts. The minimum publication group size is configured before execution.

## Audit profile comparisons

Run explicit comparisons for:

- lossless token preservation versus tool empty-token deletion;
- `Y*1000+X` versus `X*1000+Y`;
- raw key versus source-occurrence Follows profiles;
- case-sensitive versus case-insensitive owner/type matching;
- strict field counts versus unknown-tail preservation;
- strict unresolved owner/type retention versus editor skip/fabrication.

If two profiles succeed, report ambiguity. Do not select by plausibility.

## Required sample roles

Private coverage should include records with and without optional references, Tag sentinels, health/facing boundaries, Unit High/Follows, Infantry subcells, Structure upgrades, Terrain cell keys, Smudge coordinates, Waypoint and CellTag identities, map-local type/house candidates, missing Art, duplicates and gaps where present.

## Execution safety

- read-only access;
- no map save, editor launch, Unity, RA2/YR, FinalAlert, WAE or XCC execution;
- no extraction or publication of original assets;
- bounded file count, bytes, records, tokens, diagnostics and runtime;
- no network upload of baseline content;
- temporary outputs contain only sanitized aggregates;
- temporary raw data is deleted locally after aggregation.

These are `DefensiveDesign` audit requirements.

## Suggested report schema

```text
AuditMetadata
- ToolVersion
- PolicyIds
- AuditStatus = NotRun
- FutureEvidenceSource = ProjectBaselineAggregateAudit
- SelectionBasis
- SampleCategoryCounts

PlacementAggregate
- SectionPresence
- RecordCounts
- FieldCountBuckets
- KeyAnomalyCounts
- CoordinateStatusCounts
- OwnerBindingCounts
- TypeBindingCounts
- StateBuckets
- ReferenceCounts
- OverlapCounts
- DiagnosticCounts
- AggregateHash
- InputEquivalence
- CurrentEvidenceGrade

PrivacyReview
- ForbiddenFieldCheck
- MinimumGroupSizeCheck
- ReconstructabilityReview
```

`CurrentEvidenceGrade` records only the pre-audit public-source grade from the nine-item vocabulary.

## Stop conditions

The audit must stop without publication if:

- sanitization cannot remove raw values or paths;
- a hash is per-map or linkable to public map data;
- a result includes coordinate or identity sequences;
- bounded allocation or runtime limits are exceeded;
- parser output cannot be aggregated without retaining source records;
- any step would modify ProjectBaseline.
