# Future ProjectBaseline sanitized audit request

> **Source notice:** ChatGPT Web public-source research. This audit was not run and ProjectBaseline was not read. `code_imported: false`.

## Status

```text
AuditStatus: NotRun
FutureEvidenceSource: ProjectBaselineAggregateAudit
```

These fields are not an evidence grade. A future aggregate audit cannot automatically become `ConfirmedByOriginalRuntimeSource`, change project policy, or promote compatibility.

## Purpose

A future read-only local task may collect non-reconstructable aggregates about Basic/Map/SpecialFlags/MultiplayerDialogSettings, rectangles, Theater, House/Country identities, property presence, player-control candidates, directed alliances, start slots, scenario-mode evidence, Digest categories and input-mode equivalence.

## Allowed output

- broad selection/theater/map-mode/size categories;
- section presence and anonymous field-presence counts;
- rectangle parse/range/containment classifications without per-map tuples;
- Theater known/unknown/profile-binding counts without tokens;
- House/Country counts, gaps, duplicate/case-collision/missing-section categories without identities;
- property-presence and invalid-value categories without exact names/credits;
- player-control candidate conflicts and source-layer categories;
- alliance self/duplicate/dangling/asymmetric/symmetric aggregate counts without adjacency pairs;
- start binding valid/missing/duplicate/domain categories without waypoint IDs or coordinates;
- mode evidence and SpecialFlags/Digest broad categories;
- diagnostic counts, non-linkable aggregate hashes and Memory/Stream/short-read/MIX equivalence.

## Forbidden output

No map names/paths, INI text, raw keys/values, House/Country/Color/player names, exact credits, alliance pairs/topology, waypoint IDs/coordinates, start positions, campaign/media text, per-map mode/SpecialFlags/Digest values, map-local Rules text, images, bytes, hex/Base64 or per-map/per-record hashes.

## Profile discipline

Compare only preselected rectangle, Theater, identity-composition, player-control, alliance and mode profiles. Do not choose by fewest errors, map-bound plausibility, known resource availability or lobby compatibility. Multiple successful profiles remain ambiguous.

## Safety

Read-only access; bounded files/bytes/sections/records/tokens/diagnostics/runtime; no network; no map modification; no Unity/game/editor execution; no generated players, Houses, starts, alliances or session state; no original content in logs/artifacts.

These are `DefensiveDesign` audit requirements.

## Output schema

```text
AuditMetadata
- AuditStatus = NotRun
- FutureEvidenceSource = ProjectBaselineAggregateAudit
- SelectionBasis
- PolicyVersions

MetadataAggregate
- SectionPresence
- RectangleCategories
- TheaterCategories
- IdentityCategories
- PropertyPresence
- PlayerControlCategories
- AllianceCategories
- StartCategories
- ModeAndFlagCategories
- DiagnosticCounts
- InputModeEquivalence
- AggregateHash
- CurrentEvidenceGrade

PrivacyReview
- ForbiddenFieldCheck
- MinimumGroupSizeCheck
- ReconstructabilityReview
```

`CurrentEvidenceGrade` uses the nine-item closed vocabulary and records only pre-audit public evidence.

## Stop conditions

Stop without publication if raw values or identities cannot be removed, a result can identify/reconstruct a map, a hash is linkable to a map/House/start graph, input windows escape bounds, resource limits fail, input modes diverge without a bounded diagnostic, or any operation would modify ProjectBaseline.
