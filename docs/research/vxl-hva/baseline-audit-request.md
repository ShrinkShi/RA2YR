# Local ProjectBaseline golden-audit request

> **Execution boundary:** ChatGPT Web does not read ProjectBaseline. This file is a request for a later local Codex run after an implementation and synthetic tests exist.

## 1. Purpose

The audit should determine whether the proposed VXL/HVA reader contracts match controlled ProjectBaseline samples without publishing voxel geometry, transform values or normal tables.

It must not be used to choose implementation behavior merely because one permissive strategy decodes more files. Every promoted rule needs source support, cross-role samples and an explicit decision record in a later implementation PR.

## 2. Candidate selection basis

Select samples through existing typed/resource evidence rather than hard-coded filenames:

1. start from G2 records with an explicit, unambiguous `Voxel=yes` result;
2. resolve the corresponding logical VXL candidate through the bounded content catalog;
3. locate same-resource HVA candidates through the catalog without inventing fallback precedence;
4. preserve complete MIX provenance and source identity;
5. reject ambiguous or changing candidates;
6. verify candidate length and SHA-256 before and after audit;
7. record `SelectionBasis` explaining the role/coverage reason.

Do not infer stock winners from unresolved Rules/Art candidates.

## 3. Recommended coverage

Choose a bounded set that collectively covers:

- a single-section VXL;
- a body with multiple internal sections;
- a vehicle body plus separately resolved turret resource;
- a barrel resource;
- a multi-frame HVA;
- a one-frame static HVA;
- normal mode/table 2 and mode/table 4 if both are present;
- sparse and comparatively dense sections;
- low and high dimension/bounds ranges;
- a resource with no located HVA candidate;
- VXL/HVA section-name disagreement;
- different resource roles or theater/catalog provenance where available.

Do not publish exact logical names when repository policy considers them sensitive; use stable sample role IDs.

## 4. Required preconditions

- implementation branch is separate from this research PR;
- synthetic matrix in `test-matrix.md` passes;
- Memory/Stream/MIX-window paths share one bounded parser;
- output serializer has an allowlist and rejects forbidden fields;
- audit runs read-only against a fingerprinted source;
- source fingerprint is rechecked after processing;
- no result changes compatibility status automatically.

## 5. Allowed public fields

The sanitized summary may contain only:

### Selection and provenance

- `SelectionBasis`
- stable sample role/category
- logical source ID
- MIX/archive provenance chain in approved logical form
- input length
- input SHA-256

### VXL aggregates

- section-header and section-tailer counts
- complete/incomplete/failed status
- section dimension minima/maxima and aggregate range
- bounds component minima/maxima as aggregate ranges, not per-section tuples
- finite/nonfinite scale/transform/bounds counts
- total column count
- empty/nonempty column counts
- span/chunk count aggregate and range
- stored voxel count aggregate and range
- bounds-volume aggregate and range
- color-index observed min/max
- normal-index observed min/max
- normal-table category counts
- duplicate/reversed/overlap/out-of-range diagnostic counts
- canonical VXL directory/model hash

### HVA aggregates

- frame count
- section count
- exact/case-only/duplicate name aggregate counts
- raw transform record count
- finite, NaN and Infinity component counts
- singular/non-singular candidate count if evaluated without publishing values
- extreme finite magnitude category counts
- frame-major candidate model hash
- section-major candidate model hash
- whether the two interpretations differ for the sample
- canonical raw-file-order HVA hash

### Binding aggregates

- complete, ambiguous, incomplete or not-attempted status
- exact-name binding count
- exact-name-and-ordinal count
- case-only candidate count
- unbound VXL/HVA section counts
- ambiguous-group count
- canonical binding model hash

### Cross-mode evidence

- Memory/Stream/MIX-window equivalence boolean/count
- diagnostic codes and aggregate counts
- parser/fixture version identifiers

## 6. Forbidden public fields

Never include:

- VXL body bytes;
- start/end directory entry arrays;
- per-column or per-span offsets;
- X/Y/Z coordinates;
- voxel lists, runs or occupancy bitmaps;
- color/normal sequence or frequency histogram fine enough to reconstruct geometry;
- per-section voxel hashes;
- section-level geometry hashes;
- complete embedded palette bytes;
- complete normal tables or vector values;
- individual HVA matrix components;
- per-frame transform lists;
- transformed basis points;
- screenshots, rendered images or meshes;
- Base64, hex dumps or byte excerpts;
- absolute filesystem paths;
- local usernames or machine identifiers.

The audit may compute these internally only when necessary for canonical model validation, but the public serializer must not expose them.

## 7. VXL structural audit

For every selected file, aggregate:

- header-size interpretation: 802 candidate versus any alternative;
- raw remap-byte pair categories;
- section header/tailer count equality;
- section metadata common-value counts without treating them as mandatory;
- tailer offset ranges relative to body;
- empty sentinel pair categories;
- inclusive-end exact-consumption result;
- duplicate-count equality categories;
- logical-Z exact completion categories;
- no-progress command count;
- overlap/alias classifications;
- sparse voxel count versus bounds volume.

Public output must give only totals and ranges across the selected set.

## 8. HVA order probe

The audit must not publish individual transform values.

For samples with both `frameCount > 1` and `sectionCount > 1`:

1. retain the raw record sequence;
2. build deterministic candidate views under frame-major and section-major formulas;
3. calculate candidate canonical hashes;
4. compare section-name/ordinal continuity and finite-value statistics;
5. where an independently validated consumer exists locally, compare only aggregate success/diagnostic status;
6. report counts of samples supporting, contradicting or unable to distinguish each interpretation.

Single-frame or single-section HVA files are marked `OrderIndistinguishable` and cannot vote for either contract.

No default order may be promoted if all selected samples are indistinguishable.

## 9. Normal-table audit

Report only:

- selector raw categories;
- candidate table kind;
- observed normal-index min/max;
- count of indices inside/outside candidate table bounds;
- approved table identifier/count/hash, if available.

Do not publish normal vectors or per-voxel normal assignments.

## 10. Binding audit

Apply strict unique exact-name binding first.

Aggregate how many resources are:

- complete under exact names;
- incomplete because of count/name mismatch;
- ambiguous because of duplicate names;
- case-only candidates;
- only resolvable by index fallback;
- missing HVA entirely.

Index fallback and case folding may be evaluated as named experiments, but their result cannot become the default in the audit PR.

## 11. Input-mode equivalence

For every sample, run:

- memory input;
- seekable stream input with short-read behavior where supported;
- the exact MIX virtual-entry window.

Require equality of:

- raw/canonical model hashes;
- status;
- diagnostic code/order;
- aggregate counts;
- bytes consumed and trailing-data classification.

No input mode may use a more permissive reader.

## 12. Suggested result schema

```text
VxlHvaProjectBaselineAuditSummary
- AuditVersion
- SourceFingerprintBefore
- SourceFingerprintAfter
- SampleCount
- SelectionBasisCounts
- VxlAggregate
- HvaAggregate
- NormalAggregate
- BindingAggregate
- InputModeEquivalence
- DiagnosticCounts
- SanitizedSummarySha256
- ExternalManifestReference?  // cache-relative only, never absolute path
```

The serializer should use an explicit public-field allowlist and fail if an unknown field is introduced.

## 13. Promotion gates

### VXL layout/span

Promote only when:

- multiple public sources agree;
- multiple local roles and sections agree;
- exact start/end and logical-Z contracts agree;
- no success depends on clipping, padding or ignored bytes.

### HVA order

Promote only when:

- at least one distinguishing multi-frame/multi-section sample exists;
- candidate interpretation is consistent across multiple resources/roles;
- section binding remains stable;
- an independent source supports the same order;
- the alternative is positively contradicted, not merely less convenient.

### Binding

Promote a comparison/fallback policy only when local evidence includes conflicts and demonstrates a single stable stock behavior. Otherwise remain incomplete/ambiguous.

## 14. Expected audit conclusion states

- `ConfirmedForSelectedSamples`
- `StructurallyParsedButSemanticsUnresolved`
- `AmbiguousCandidateStrategies`
- `IncompleteCoverage`
- `FailedClosed`

None of these states modifies `docs/compatibility/matrix.yml` automatically.
