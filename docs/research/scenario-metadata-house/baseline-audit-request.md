# Future ProjectBaseline sanitized audit request

> **Source notice:** ChatGPT Web public-source research. The local ProjectBaseline was not read or accessed. This is not a Codex artifact. No GPL or unclear-license code was copied, translated, or mechanically ported. `code_imported: false`.

## Purpose

This document designs a future read-only, sanitized audit for Codex or another authorized local agent. It does not execute that audit and does not request publication of map content.

The audit is intended to test research hypotheses through aggregate observations while preventing reconstruction or identification of a specific scenario.

## Evidence ceiling

All results from this future audit are graded:

```text
ObservedByFutureProjectBaselineAudit
```

They cannot automatically become:

```text
ConfirmedByOfficialRuntimeSource
```

They also cannot, by themselves, promote compatibility status or authorize implementation shortcuts.

## Audit execution constraints

A future audit must be:

- read-only;
- deterministic;
- bounded;
- non-rendering;
- non-executing;
- non-networked;
- free of map modification;
- free of Unity, RA2/YR, FinalAlert, WAE, or XCC execution;
- free of image, preview, or spatial-output generation;
- limited to approved aggregate output.

## Selection principles

Select samples by broad category, not by publicly naming files.

Recommended `SelectionBasis` categories:

- campaign candidate;
- skirmish candidate;
- multiplayer candidate;
- cooperative candidate;
- tutorial/challenge candidate;
- official-content category;
- official-add-on category;
- custom/extension category where authorized;
- small geometry category;
- medium geometry category;
- large geometry category;
- malformed or edge-case candidate category.

The output may state only category labels and sample counts.

## Theater coverage

Include aggregate coverage for:

- Temperate;
- Snow;
- Urban;
- NewUrban;
- Desert;
- Lunar;
- unknown/extension theater candidates, if present and authorized.

Do not publish per-map theater assignments.

## Geometry coverage

Include examples with:

- zero and nonzero Size origins;
- zero and nonzero LocalSize origins;
- LocalSize equal to Size;
- LocalSize smaller than Size;
- malformed rectangle candidates;
- extreme but bounded dimensions;
- LocalSize containment conflicts;
- missing Size/LocalSize candidates where present.

Output only ranges, buckets, and counts.

## House and Country coverage

Include categories with:

- no explicit Houses;
- one House;
- multiple Houses;
- multiple Houses sharing a Country candidate;
- Neutral/Special/civilian candidates;
- map-local Countries;
- modified global Countries;
- House list gaps;
- duplicate/case-collision candidates;
- listed-but-missing sections;
- unlisted House/Country section candidates;
- missing or unknown Country references.

No House or Country name may be published.

## Property coverage

Aggregate presence and shape for:

- Color;
- Credits;
- IQ;
- Edge;
- TechLevel;
- PercentBuilt;
- PlayerControl/Human candidates;
- NodeCount/base-node candidates;
- unknown/extension fields;
- statistics-like fields;
- starting-location fields.

Do not publish exact per-map property values.

## Alliance coverage

Include:

- no Allies property;
- self-only alliances;
- symmetric pairs;
- asymmetric pairs;
- duplicate ally tokens;
- missing ally targets;
- case-collision references;
- FixedAlliance candidates;
- Neutral/civilian alliance candidates.

Only aggregate edge counts and pair classifications may be published. No adjacency or specific pair is allowed.

## Multiplayer/start coverage

Include categories with:

- complete low-numbered start candidates;
- missing start candidates;
- duplicate Waypoint identity;
- duplicate start cell;
- fixed-start candidates;
- random-start/client override candidates;
- starts outside LocalSize;
- starts outside Size;
- starts with no IsoMap cell candidate;
- no MultiplayerDialogSettings;
- map/client/Rules extension fields;
- player-count conflicts.

No Waypoint IDs, cell values, or coordinates may be published.

## Mode coverage

Include broad candidate categories:

- campaign;
- skirmish;
- multiplayer;
- co-op;
- tutorial/challenge;
- conflicting mode evidence;
- unclassified.

Output candidate-resolution counts only.

## SpecialFlags coverage

Aggregate section/key presence for recognized and unknown fields.

Do not publish per-map values or correlations that identify maps.

## Digest coverage

Record only:

- section presence;
- duplicate occurrence count;
- number of keys;
- raw-value length bucket;
- shape classification candidate;
- whether multiple input modes agree.

Do not publish Digest text or per-map hashes.

## Map-local composition coverage

Aggregate:

- number of sections classified as scenario metadata;
- House-instance sections;
- Country-definition sections;
- map-local Rules candidate sections;
- ambiguous shared-name sections;
- winner/suppressed count categories;
- unknown sections.

Do not publish section names or values.

# Allowed public output

## Selection and broad categories

Allowed:

- `SelectionBasis` category;
- total sample count;
- broad scenario-mode candidate counts;
- broad theater category counts;
- small/medium/large geometry buckets;
- official/add-on/custom category labels without filenames.

## Section presence

Allowed:

- presence counts for `[Basic]`, `[Map]`, `[SpecialFlags]`, `[MultiplayerDialogSettings]`, `[Houses]`, `[Countries]`, `[Digest]`;
- duplicate section counts;
- missing required-candidate counts;
- field-count histograms.

## Basic and Map aggregates

Allowed:

- recognized-field presence counts;
- unknown-field counts;
- boolean/numeric parse category counts;
- Size/LocalSize field-count histograms;
- origin zero/nonzero/negative category counts;
- width/height min/max ranges only if coarse and non-identifying;
- rectangle relation classifications;
- malformed/overflow counts.

## Theater aggregates

Allowed:

- logical profile category count;
- unknown binding count;
- ambiguous binding count;
- extension-profile-required count.

## House/Country identity aggregates

Allowed:

- number of House and Country entries per sample as coarse histograms;
- list-gap count;
- duplicate raw-key count;
- normalized ordinal collision count;
- duplicate logical identity count;
- case-collision count;
- listed-missing-section count;
- unlisted-section-candidate count;
- unique/missing/ambiguous Country-binding counts;
- Neutral/Special/civilian category counts without names.

## House property aggregates

Allowed:

- property-presence counts;
- invalid numeric/boolean count;
- negative/overflow category counts;
- Color binding success/missing/ambiguous counts;
- player-control candidate count;
- base-node count histogram;
- NodeCount mismatch count;
- extension-tail/unknown-field count.

Exact credits, colors, names, base-node positions, or property tuples are prohibited.

## Alliance aggregates

Allowed:

- total directed edge count;
- self-edge count;
- duplicate edge count;
- missing-target count;
- symmetric/asymmetric pair counts;
- case-collision count;
- FixedAlliance presence/parse category count.

## Multiplayer and start aggregates

Allowed:

- presence count of multiplayer fields;
- setting-source category count;
- player-count evidence conflict count;
- start-slot candidate count histogram;
- resolved/missing/duplicate/out-of-domain start counts;
- LocalSize-inside/outside category counts;
- fixed/random/client-override evidence counts.

## Game-mode aggregates

Allowed:

- candidate mode counts;
- ambiguous resolution count;
- evidence-source category count;
- field-versus-client/control conflict count.

## SpecialFlags and Digest aggregates

Allowed:

- SpecialFlags recognized/unknown property counts;
- invalid-boolean count;
- Digest presence and shape classifications;
- duplicate Digest count;
- raw length buckets.

## Diagnostics

Allowed:

- diagnostic code counts;
- severity counts;
- policy/profile category counts;
- no raw message arguments that reveal source content.

## Aggregate hashes

Allowed only when all conditions are met:

- hash input is an approved normalized aggregate, not raw map content;
- hash is non-linkable to a single file;
- multiple samples are combined;
- no per-map hash list is published;
- normalization schema is documented but cannot reconstruct the maps;
- the hash is called an aggregate consistency hash, not scenario identity.

## Input-mode equivalence

Allowed:

- Memory versus Stream equality count;
- short-read Stream equality count;
- MIX-window equality count;
- mismatch diagnostic count;
- bounded-read and no-progress results.

# Forbidden public output

The audit must never publish:

- map name;
- scenario title;
- filename;
- archive/container entry name;
- absolute or relative path;
- username, machine name, or local directory;
- INI text;
- section or key text beyond the approved generic section/key categories;
- raw values;
- Basic display text;
- author names;
- media names;
- campaign control IDs;
- House names;
- Country names;
- Side names;
- Color names;
- player names;
- exact House/Country registry entries;
- list order or ordinal-to-name mapping;
- per-House property tuples;
- exact credits or economy values;
- alliance adjacency;
- specific alliance pairs;
- Waypoint IDs;
- ScenarioCell IDs;
- coordinates or coordinate sequences;
- start positions;
- base-node positions or composition;
- starting-unit or starting-building details;
- map-local Rules text or logical type names;
- SpecialFlags values on a per-map basis;
- Digest contents;
- per-map hash;
- per-record or per-section hash;
- Preview, image, screenshot, pixel, mesh, or map layout;
- hex, Base64, compressed, decompressed, or binary bytes;
- any combination of aggregates narrow enough to identify or reconstruct one scenario.

## Correlation restrictions

Even individually allowed aggregates must not be combined when the joint result becomes identifying.

Examples of prohibited combined rows:

- exact dimensions + theater + House count + alliance count + start count for one map;
- exact field-presence vector for one map;
- exact Digest length + mode + official/add-on category for one map;
- exact start-domain anomaly + House count + SpecialFlags vector;
- per-sample aggregate hashes.

Use sufficiently large category groups and suppress low-count buckets.

## Minimum aggregation and suppression

Recommended policy:

- no public bucket with fewer than a configured minimum sample count;
- merge or suppress rare categories;
- publish ranges rather than exact unique values;
- avoid stable sample ordering;
- do not expose cross-run sample identifiers;
- salt or redesign aggregate hashes when linkability is possible.

## Audit output schema

Suggested approved output:

```text
ScenarioMetadataAuditSummary
- AuditVersion
- SelectionBasisCounts
- SectionPresenceCounts
- BasicFieldCategoryHistogram
- RectangleCategorySummary
- TheaterBindingSummary
- HouseCountryIdentitySummary
- HousePropertySummary
- AllianceSummary
- MultiplayerStartSummary
- ScenarioModeSummary
- SpecialFlagsSummary
- DigestShapeSummary
- CompositionSummary
- DiagnosticCounts
- InputModeEquivalenceSummary
- AggregateConsistencyHashes
```

No per-sample records are emitted.

## Canonical aggregate hash candidates

Permitted canonical aggregate inputs may include sorted totals such as:

```text
(sectionCategory, presenceCount)
(diagnosticCode, count)
(rectangleRelationCategory, count)
(bindingState, count)
(allianceClassification, count)
(modeCandidateCategory, count)
```

They must not include source values or names.

## Audit implementation boundaries

The future audit reader should:

- reuse production read-only parser states only where authorized;
- use an independent aggregate reducer;
- enforce output allowlists;
- reject unexpected fields before serialization;
- enforce read and output budgets;
- avoid logging raw exceptions containing source text;
- avoid temporary images or decoded assets;
- avoid network access;
- write no changes to source files.

## No behavior execution

The audit must not:

- create players;
- assign local control;
- apply alliances;
- choose starts;
- run a lobby;
- spawn units;
- execute SpecialFlags;
- calculate campaign carry-over;
- verify Digest as trusted security;
- render Lighting;
- run Trigger or AI logic.

## Required audit report declaration

Every future report must state:

- ProjectBaseline was accessed locally under authorization;
- no source content was published;
- output is aggregate and sanitized;
- no map was modified;
- no game/editor was executed;
- evidence grade is `ObservedByFutureProjectBaselineAudit`;
- no compatibility promotion follows automatically;
- all raw values, names, coordinates, and graph topology remain private.
