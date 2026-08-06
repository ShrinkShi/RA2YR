# Local ProjectBaseline read-only audit request

> **Execution boundary:** ChatGPT Web does not read ProjectBaseline or the configured runtime root. This document specifies a later local Codex audit after bounded readers, registry binders, and the synthetic matrix exist.

## 1. Purpose

Validate TMP cell metadata, plane-directory interpretations, theater-control composition, cumulative TileSet allocation, TMP/palette/LAT binding, and terrain-role candidates without publishing original terrain graphics, theater configuration values, palette data, or reconstructable map resources.

The audit is read-only and cannot change compatibility status.

## 2. Preconditions

- implementation exists on a separate Codex branch;
- all 112 synthetic tests pass;
- original resources are opened read-only;
- public serializer is allowlist-based and fails on unknown output fields;
- authoritative content fingerprint is recorded before and after;
- no Unity, original game, FinalAlert, XCC GUI, or writer is launched;
- no files are extracted, repaired, regenerated, or saved;
- no compatibility matrix or source ledger is changed.

## 3. Selection basis

Use the existing content catalog and explicit theater registry rather than absolute paths or filename substring guesses.

Select a bounded set covering, where present:

### Theater profiles

- Temperate;
- Snow;
- Urban;
- NewUrban;
- Desert;
- Lunar.

### TMP structural roles

- 1×1 flat cell;
- multi-cell template;
- empty cell slot;
- nonzero local height;
- several ramp-type categories;
- terrain-type diversity;
- diamond depth present/absent candidates;
- extra color and extra depth;
- known and unknown flag bits;
- nonzero trailing metadata bytes;
- variations of one TileSet entry;
- missing TMP candidate where registry reserves an ID.

### Theater semantic roles

- clear/base terrain;
- LAT ground and transition sets;
- ramps;
- cliffs;
- water;
- shores/beaches;
- snow ice and ice shore;
- bridge, train bridge, and wood bridge candidates where configured.

Use stable role IDs in public output instead of resource names when names are not explicitly approved.

## 4. Allowed public fields

### Source/provenance

- `SelectionBasis`;
- stable sample role/category;
- theater profile category;
- approved logical provenance chain;
- provider/archive category;
- file length and SHA-256;
- candidate and suppressed-candidate counts.

### TMP file aggregates

- file-header dimension ranges;
- cell-slot/nonempty/empty counts;
- distinct/duplicate/overlap offset classifications;
- valid/truncated/out-of-range cell-header counts;
- canonical raw-document hash;
- diagnostic counts.

### Cell-header aggregates

- min/max/range classification for each raw numeric field;
- known flag-bit counts;
- unknown flag-bit nonzero count and OR-mask only if approved;
- trailing-raw all-zero/nonzero counts;
- signed-versus-unsigned interpretation category counts;
- declared-offset versus canonical-sequential relation counts;
- no per-cell tuples.

### Plane aggregates

- diamond byte-count range;
- declared/present depth counts;
- extra width/height/area ranges at coarse aggregation;
- extra color/depth presence counts;
- plane exact/underflow/overflow/overlap counts;
- depth-value bounded-range classifications without histograms;
- canonical plane-directory hash;
- no plane bytes.

### Theater INI and registry aggregates

- contributing logical-document count;
- effective section/key counts;
- TileSet candidate/valid/gap/duplicate counts;
- `TilesInSet` count and range summaries;
- total reserved global tile-ID count;
- special-role complete/incomplete counts;
- cumulative registry hash;
- composition provenance completeness;
- no INI values or full section lists.

### Asset resolution aggregates

- primary TMP present/missing counts;
- variation count ranges;
- fallback-extension candidate/use counts;
- ambiguous case/provider counts;
- resolved/incomplete binding counts;
- canonical candidate-chain hash.

### Palette and LAT aggregates

- approved logical palette names or stable roles;
- palette length and SHA-256;
- ISO/unit/resource role-binding counts;
- LAT complete/missing/ambiguous edge counts;
- palette/LAT binding hashes;
- no palette bytes or color table.

### Semantic aggregates

- HeightRaw/ramp/terrain candidate ranges;
- known/unknown ramp and terrain counts;
- registry-role versus metadata agreement/disagreement counts;
- cliff/water/shore/ice/bridge complete/incomplete counts;
- no cell coordinates, tile IDs, or per-resource lists.

### Cross-input equivalence

- Memory/Stream/short-read/MIX-window result equality;
- status and diagnostic-code equality;
- bytes-consumed equality;
- canonical model/binding hash equality;
- sanitized-summary SHA-256.

## 5. Forbidden public output

Never publish:

- TMP header bytes, color/depth/extra planes, or byte excerpts;
- per-cell field tuples or offsets;
- exact extra rectangles tied to resource identity;
- palette bytes, per-index RGB tables, or converted images;
- theater INI text, section values, comments, or complete section inventory;
- full TileSet/TMP filename lists;
- global tile ID to filename maps;
- map coordinates, IsoMap records, or SubTile sequences;
- overlay arrays, bridge placements, or movement data;
- screenshots, textures, meshes, or render hashes that identify assets;
- per-cell/per-tile/per-plane hashes;
- Base64 or hex dumps;
- absolute paths, usernames, machine identifiers.

## 6. Header-size conflict probe

For every selected nonempty cell, aggregate whether:

- byte 48 would start plane data under a 48-byte view;
- byte 52 agrees with color-plane start;
- declared `z_ofs` agrees with `52 + D`;
- the 48-byte model causes impossible plane lengths or overlaps.

Only publish aggregate counts and candidate hashes. Do not publish bytes.

Expected gate: 52-byte interpretation should be supported across multiple theaters and roles before production implementation proceeds.

## 7. Offset versus sequential probe

Compute three private candidate directories:

1. declared offsets;
2. canonical sequential with Z;
3. canonical sequential without Z.

Publish only:

- exact-agreement count;
- distinguishable-sample count;
- safe-valid count per strategy;
- overlap/out-of-range counts;
- aggregate candidate hashes.

A sample that succeeds under multiple strategies does not vote. No auto-fallback is authorized.

## 8. Flags and unknown bytes probe

Aggregate:

- known-bit combinations;
- unknown-bit nonzero prevalence;
- trailing-three-byte zero/nonzero prevalence;
- correlations only at coarse theater/role category level;
- plane presence versus known flags;
- damaged-bit prevalence without attempting reconstruction.

Do not publish raw words or per-cell combinations.

## 9. Theater registry probe

For each effective theater control document:

- compare source occurrence order and numeric TileSet order privately;
- identify gaps and duplicates;
- compute cumulative global ID ranges from `TilesInSet`;
- verify missing files do not change later ranges;
- resolve special `[General]` roles;
- validate composition provenance.

Publish counts, ranges, hashes, and diagnostics only.

## 10. Theater-profile probe

For six theater categories, report:

- control-document layer count;
- primary TMP extension category;
- palette-role completeness;
- content-provider category count;
- TileSet and global-ID aggregate ranges;
- missing/fallback asset counts;
- profile-specific diagnostic counts.

Do not publish unapproved file lists or INI values.

## 11. Palette and LAT probe

Validate:

- TMP uses ISO palette binding, not unit palette fallback;
- palette candidates follow content precedence;
- LAT source/transition/base TileSets exist;
- connected sets are valid and deterministic;
- extension-only LAT keys remain labelled.

Publish logical roles, lengths/SHA, counts, and aggregate hashes only.

## 12. Ramp/terrain/bridge probe

Aggregate:

- HeightRaw categories;
- ramp values inside/outside the candidate 0..20 table;
- terrain values inside/outside candidate profiles;
- map Level versus local height separation checks;
- special TileSet roles versus raw metadata agreement;
- bridge role and overlay/state completeness counts.

Do not publish placements, exact IDs, or per-tile metadata.

## 13. Input equivalence

Every selected logical resource is read through:

- immutable memory snapshot;
- seekable stream;
- short-read stream wrapper;
- exact MIX-entry window.

Require identical statuses, diagnostics, consumed lengths, raw-model hashes, and binding hashes. No transport-specific permissive path is allowed.

## 14. Suggested public schema

```text
TmpTheaterProjectBaselineAuditSummary
- AuditVersion
- SourceFingerprintBefore
- SourceFingerprintAfter
- SelectionBasisCounts
- TheaterProfileAggregates
- TmpFileAggregate
- TmpCellHeaderAggregate
- TmpPlaneDirectoryAggregate
- TheaterIniCompositionAggregate
- TileRegistryAggregate
- TmpAssetResolutionAggregate
- PaletteLatAggregate
- TerrainSemanticAggregate
- CrossInputEquivalence
- DiagnosticCounts
- SanitizedSummarySha256
```

## 15. Decision gates

### A — production candidate

Multiple independent public sources and multiple sanitized roles/theaters agree, with exact bounded parsing and no reliance on ignored offsets, clipping, fabricated assets, or editor repair.

### B — named experiment

One strong source plus distinguishing local evidence permits a default-off profile.

### C — unresolved

Sources conflict, local samples cannot distinguish candidate strategies, or semantics depend on rendering/pathfinding assumptions.

### D — profile/family split

Different theaters or resource roles require incompatible contracts; split profiles before adding exceptions.

None of these gates automatically changes compatibility metadata.
