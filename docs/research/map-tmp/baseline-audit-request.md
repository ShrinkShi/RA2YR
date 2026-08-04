# Local ProjectBaseline golden-audit request

> **Execution boundary:** ChatGPT Web does not read ProjectBaseline. This is a request for a later local Codex run after bounded readers and the synthetic matrix exist.

## 1. Purpose

Validate selected RA2/YR map and TMP contracts without publishing map text, mission logic, terrain placement, preview pixels, TMP images or reconstructable resources.

The audit is read-only and cannot modify compatibility status automatically.

## 2. Selection basis

Use existing content catalog and provenance rather than hard-coded absolute paths.

Select a bounded set covering:

- official/package `.map` candidate;
- custom `.mpr` and `.yrm` candidates where present;
- multiplayer and mission-oriented maps;
- multiple theaters;
- small, medium and large dimensions;
- maps with and without complex mission graphs;
- maps with map-local Rules/Art candidates;
- IsoMap streams with varied tile-index/high-word categories;
- overlays including ore/walls/rails/bridge roles where selectable without publishing positions;
- valid preview packs with varied dimensions;
- single-cell, multicell, empty-slot and extra-data TMPs;
- slope/cliff/shore/water/bridge-related TMP roles;
- Memory, Stream and exact MIX-window inputs.

Selection reports a stable role ID, not sensitive logical names where policy forbids them.

## 3. Preconditions

- implementation is on a separate branch;
- all 104 synthetic tests pass;
- public serializer uses an allowlist and fails on unknown fields;
- authoritative source fingerprint is captured before and after;
- original files/MIX archives are opened read-only;
- no FinalAlert, XCC GUI, Unity or original game is launched;
- no save/repair operation is invoked;
- no compatibility matrix update is performed.

## 4. Allowed public fields

### Source and selection

- `SelectionBasis`
- stable sample role/category
- extension/family category
- approved logical provenance
- length and SHA-256
- provider/archive category

### Map shell aggregates

- section/key/occurrence counts and ranges;
- duplicate-section/key counts;
- recognized/unknown section counts;
- map size/local-size/theater categories and bounded ranges;
- NewINIFormat category;
- canonical shell model hash;
- diagnostics.

### Packed-section aggregates

- fragment count/range;
- numeric-key gap/duplicate/nonnumeric counts;
- compressed/decompressed length and block-count ranges;
- exact-consumption and size-match counts;
- compression status and canonical model hash;
- no Base64 text or decoded bytes.

### IsoMap aggregates

- record count;
- dense/sparse candidate classification;
- X/Y, tile-field, subtile, level and tail-byte min/max ranges;
- high-word-zero/nonzero aggregate counts;
- duplicate/out-of-range coordinate counts;
- terminal-four-byte category counts;
- canonical raw-record and canvas-candidate hashes;
- no coordinates or tile sequence.

### Overlay/Preview aggregates

- decoded-size categories;
- nonempty overlay count and type/data min/max only when coarse enough;
- unresolved registry-binding counts;
- preview width/height and component-order candidate hashes;
- finite success/failure counts;
- no overlay positions, arrays or preview pixels.

### Mission graph aggregates

- record counts by broad section family;
- unique/missing/ambiguous edge counts;
- supported/unknown opcode counts;
- map-local override section/key counts;
- global/map winner-origin counts;
- canonical identity/edge graph hash;
- no record values, names, text or parameters.

### TMP aggregates

- template and pixel dimension ranges;
- cell-slot/nonempty/empty counts;
- distinct/duplicate/overlap offset classifications;
- normal/extra plane byte-count ranges;
- extra-data and flag-category counts;
- raw metadata candidate category counts;
- canonical TMP directory/cell-model hash;
- no pixels, depth arrays, offsets, palettes or per-cell hashes.

### Cross-mode

- Memory/Stream/MIX-window equivalence;
- status and diagnostic-code equality;
- bytes consumed;
- parser/policy version;
- sanitized-summary SHA-256.

## 5. Forbidden public fields

Never include:

- original INI/map text, comments, section names beyond approved generic categories or key values;
- Base64 fragments, compressed bytes, decompressed pack bytes or hex excerpts;
- IsoMap record lists, coordinates, tile sequences or per-cell hashes;
- overlay arrays, positions or fine-grained histograms;
- preview pixels, images or channel samples;
- mission IDs, names, scripts, event/action parameters or dialogue;
- TMP offsets, color/depth pixels, extra images, palettes or reconstructable tile geometry;
- per-resource/per-section fine hashes that permit matching/extraction;
- absolute paths, usernames, machine identifiers;
- screenshots, meshes, Base64 or hex dumps.

## 6. IsoMap conflict probe

Aggregate across selected maps:

- count/range of records whose high tile word is zero/nonzero;
- values fitting u16 versus requiring full 32-bit field;
- final four-byte categories;
- decompressed size classified as `11n+4` or other;
- full-density expected count versus actual record count;
- duplicate/out-of-canvas categories;
- canonical hashes under 32-bit-field and split-word views.

Do not publish individual field values or coordinates.

Promotion requires multiple roles/theaters and independent public support. If all observed high words are zero, that does not by itself prove the field is only u16.

## 7. Preview color-order probe

For valid previews:

- build raw component-order model;
- build explicit RGB and BGR adapter candidates;
- calculate nonpublic image hashes and coarse sanity metrics;
- compare only approved aggregate success and candidate hashes;
- report how many samples distinguish the candidates.

Do not publish images or pixel samples. Visual plausibility alone cannot select the default.

## 8. TMP metadata probe

Aggregate:

- raw header prefix/suffix category hashes;
- flag values/ranges;
- extra-data presence;
- candidate height/terrain/ramp field ranges under each documented layout;
- consistency with theater/tile-set role categories;
- color/depth/extra exact-size validation;
- duplicate/overlap classifications.

No full raw header bytes or cell-level tuple is public.

## 9. Mission and local INI probe

Report only counts:

- map structural versus local override sections;
- global inherited, map-overridden and map-added effective keys;
- same-document duplicates versus cross-layer overrides;
- complete/missing/ambiguous references;
- unknown opcode records;
- extension-profile candidates such as `ART.`.

Do not disclose identities, values or mission content.

## 10. Input equivalence

For every selected logical source, parse through:

- memory snapshot;
- seekable stream with short-read simulation;
- exact content/MIX window.

Require equal:

- statuses;
- canonical model hashes;
- aggregate counts;
- diagnostic codes/order;
- consumed lengths and trailing-data classifications.

No input route may use a more permissive decoder.

## 11. Suggested result schema

```text
MapTmpProjectBaselineAuditSummary
- AuditVersion
- SourceFingerprintBefore
- SourceFingerprintAfter
- SampleCount
- SelectionBasisCounts
- MapShellAggregate
- PackedSectionAggregate
- IsoMapAggregate
- OverlayPreviewAggregate
- MissionGraphAggregate
- MapLocalIniAggregate
- TmpAggregate
- InputModeEquivalence
- DiagnosticCounts
- SanitizedSummarySha256
```

## 12. Decision gates

### A — production candidate

Multiple independent public sources and multiple sanitized local roles agree, with no repair/clipping/padding/ignored-byte dependency.

### B — explicit experiment

One strong source plus consistent local evidence permits a named default-off interpretation.

### C — unresolved

Competing views remain, samples cannot distinguish them, or success requires editor repair.

### D — family/classification issue

Different samples require incompatible layouts; investigate game family, NewINIFormat, extension profile, corruption or wrong resource binding before adding hacks.

## 13. Expected outcomes

- `ConfirmedForSelectedSamples`
- `StructurallyParsedSemanticsUnresolved`
- `AmbiguousCandidateStrategies`
- `IncompleteCoverage`
- `FailedClosed`

None changes compatibility metadata automatically.
