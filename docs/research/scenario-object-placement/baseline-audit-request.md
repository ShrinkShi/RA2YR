# Sanitized future ProjectBaseline audit request

> **Source notice:** ChatGPT Web public-source research only. ProjectBaseline was not read. This is not a Codex Agent artifact. No GPL or unclear-license code was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

## Purpose

This document specifies a future read-only Codex audit that can test public-source hypotheses against the user's local baseline without exposing maps or reconstructable placement data.

This audit was not run by ChatGPT Web.

## Evidence grade

Every local result must be labeled:

```text
ObservedByFutureProjectBaselineAudit
```

It must not automatically become:

```text
ConfirmedByOfficialRuntimeSource
```

Local observations can support project policies and identify fixtures for private testing, but they do not prove universal runtime behavior.

## Selection basis

The local audit should select aggregate samples across:

- all six theater categories where present;
- campaign and skirmish/standard multiplayer categories;
- small, medium, and large map-size buckets;
- Structure-heavy maps;
- Unit-heavy maps;
- Infantry-heavy maps;
- maps containing Aircraft placements;
- maps containing Terrain objects;
- maps containing Smudges;
- maps with Waypoints and CellTags;
- neutral/civilian/special owner categories;
- maps with unknown or extension type candidates;
- out-of-domain record candidates;
- duplicate-key candidates;
- unknown trailing fields;
- dangling reference candidates.

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

The audit must use the future production parser only for one side of equivalence checks. Independent probe calculations or fixture formulas must be used where comparison is intended.

## Allowed public output

### Selection and provenance

- `SelectionBasis`;
- theater category counts;
- campaign/skirmish/other map-category counts;
- source container/provenance categories without filenames or paths;
- map-size bucket counts without individual tuples.

### Section presence and record aggregates

- presence counts for Structures, Units, Infantry, Aircraft, Terrain, Smudge, Waypoints, and CellTags;
- total and per-category record-count ranges, sums, and coarse histograms;
- duplicate-section occurrence counts;
- zero-record and nonzero-record categories.

### Token and key aggregates

- raw field-count histograms per section family;
- minimum/maximum/coarse token-count buckets;
- empty-token and trailing-empty-token counts;
- unknown trailing-field counts;
- duplicate raw key counts;
- normalized numeric collision counts;
- gap counts;
- nonnumeric key counts;
- key-length ranges.

No key text is published.

### Coordinate aggregates

- coordinate decode success/failure counts under explicitly named profiles;
- aggregate X/Y minima and maxima by coarse category;
- negative/overflow counts;
- inside Size/outside Size counts;
- inside/outside LocalSize counts;
- IsoMap cell present/missing counts;
- axis-profile ambiguity counts;
- Infantry subcell range and frequency summary in coarse buckets;
- multiple-infantry/cooccupancy category counts.

No coordinate sequence or per-map coordinate set is published.

### Owner and type binding aggregates

- owner binding status counts: unique, special candidate, unknown, duplicate;
- type registry binding success/failure counts by family;
- registry gap and duplicate category counts;
- typed section missing counts;
- map-local contribution counts;
- missing Art/visual binding counts;
- extension-profile candidate counts.

Owner and type names are forbidden.

### State aggregates

- health raw minima/maxima and coarse buckets;
- counts below 0, in 0..256, and above 256;
- facing raw range and coarse buckets;
- recognized/unknown/empty mission category counts;
- veterancy and group coarse ranges;
- High and Follows presence/sentinel/ambiguity counts;
- structure upgrade-count consistency categories;
- recruitment flag combination counts.

No raw mission or state token list is published.

### Reference and overlap aggregates

- Tag-reference present/none/dangling/ambiguous counts;
- CellTag reference counts;
- missing Trigger second-stage counts;
- Follows reference-basis classification counts;
- duplicate waypoint counts;
- overlap/cooccupancy category counts;
- cycle-candidate counts;
- total diagnostic counts by code/severity.

### Hashes and equivalence

Allowed hashes must be aggregate and non-reconstructable:

- canonical aggregate hash of sorted category/count tuples;
- category-level parser result hashes after removing names, paths, values, coordinates, and order-sensitive source data;
- Memory/Stream/short-read Stream/MIX equivalence booleans and aggregate mismatch counts.

Do not publish per-map, per-record, per-owner, per-type, or per-section-value hashes.

## Forbidden public output

The audit must not expose:

- map names;
- scenario titles;
- file names;
- container entry names;
- absolute or relative local paths;
- usernames, machine names, or volume labels;
- INI section bodies;
- placement record text;
- raw key text;
- raw token text;
- owner names or complete owner lists;
- type names or complete type lists;
- object names;
- coordinates or coordinate sequences;
- per-object tuples;
- per-map field histograms where they identify a map;
- waypoint IDs or values;
- Tag/Trigger IDs;
- TeamType, TaskForce, ScriptType, or AITrigger IDs;
- type-to-Art/SHP/VXL/HVA mappings;
- unit, infantry, aircraft, building, terrain, smudge, or CellTag positions;
- map layouts;
- overlay arrays;
- IsoMap, TMP, palette, SHP, VXL, HVA, or Preview contents;
- images, screenshots, thumbnails, or rendered maps;
- Base64 or hex dumps;
- compressed or decoded bytes;
- per-object, per-record, per-map, or per-resource hashes;
- any information sufficient to reconstruct or identify a map.

## Minimum privacy thresholds

Aggregate categories should be suppressed or merged when they contain too few samples and could identify a map. The local audit should define a minimum publication group size before execution.

Rare unknown type, owner, mission, or extension categories should be reported only as counts in a broad “other/unknown” bucket.

## Audit profile comparisons

Run explicit, non-auto-selecting comparisons for:

- lossless token preservation versus common tool `RemoveEmptyEntries` behavior;
- `Y*1000+X` versus `X*1000+Y` coordinate profiles;
- raw key versus source-occurrence Follows target profiles;
- case-sensitive versus case-insensitive owner/type candidate matching;
- strict minimum field counts versus unknown-tail preservation;
- strict unknown owner/type retention versus editor skip/fabrication behavior.

If two profiles both succeed, report ambiguity. Do not vote based on success counts or plausibility.

## Required sample roles

The private local run should ensure coverage of:

- records with no optional references;
- records with Tags;
- records with `None`/`<none>` variants;
- health/facing boundary values;
- Unit High/Follows fields;
- Infantry nonzero subcells;
- Structure upgrades;
- Terrain key-as-cell;
- Smudge value coordinates;
- waypoint and CellTag cell identities;
- map-local type and house candidates;
- missing Art candidates;
- duplicate and gap candidates if present.

## Execution safety

- read-only access;
- no map save or editor launch;
- no Unity, RA2/YR, FinalAlert, WAE, or XCC execution;
- no extraction or publication of original assets;
- bounded file count, bytes, records, tokens, diagnostics, and runtime;
- no network upload of baseline content;
- temporary outputs contain only sanitized aggregates;
- temporary raw data is deleted locally after aggregate generation.

## Suggested report schema

```text
AuditMetadata
- ToolVersion
- PolicyIds
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

PrivacyReview
- ForbiddenFieldCheck
- MinimumGroupSizeCheck
- ReconstructabilityReview
```

## Stop conditions

The audit must stop without publication if:

- sanitization cannot remove raw values or paths;
- a generated hash is per-map or linkable to public map data;
- a result includes coordinate or identity sequences;
- bounded allocation or runtime limits are exceeded;
- parser output cannot be aggregated without retaining source records;
- any step would modify ProjectBaseline.
