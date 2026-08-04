# Local ProjectBaseline sanitized golden-audit request

> Execution boundary: ChatGPT Web does not read ProjectBaseline. This is a read-only request for a later local Codex run after an independently implemented candidate and synthetic tests exist.

## 1. Purpose

Determine whether selected RA2/YR map packed sections support the candidate:

- numbered-fragment policies;
- four-byte chunk envelope;
- Format80 variant;
- raw LZO1X decoder contract;
- strict input/output completion rules.

The audit must not promote behavior solely because a permissive strategy decodes more samples.

## 2. Preconditions

- implementation branch is separate from this research PR;
- all 108 synthetic specifications are represented by executable tests;
- codec dependency/license decision is recorded;
- Memory/Stream/MIX-window use one parser state machine;
- output serializer uses a public-field allowlist;
- source fingerprints are checked before and after;
- no compatibility metadata changes automatically.

## 3. Selection basis

Select a bounded sample set through the configured content catalog, not hard-coded absolute paths.

Coverage should include:

- `IsoMapPack5`;
- `PreviewPack`;
- `OverlayPack`;
- `OverlayDataPack`;
- single-block and multi-block sections;
- small and large maps;
- low and high compression ratios;
- sections exercising multiple Format80 command kinds;
- at least two theaters;
- campaign, multiplayer/skirmish, and official map-addition roles where available;
- Memory, Stream, and exact MIX-window access.

Record a non-sensitive `SelectionBasis` for each sample role.

## 4. Allowed public fields

### Selection and provenance

- stable sample role ID;
- `SelectionBasis`;
- logical packed-section name;
- approved logical archive provenance;
- packed input length;
- packed input SHA-256;
- parser/profile versions.

### Fragment aggregates

- raw occurrence count;
- accepted fragment count;
- numeric index min/max;
- gap count;
- duplicate/nonnumeric/leading-zero counts;
- source-order versus numeric-order disagreement count;
- total Base64 character count;
- Base64 decoded length;
- canonical fragment-directory hash.

### Chunk aggregates

- block count;
- compressed-size min/max;
- uncompressed-size min/max;
- total compressed and output lengths;
- compression-ratio range;
- zero-field category counts;
- short-final-block count;
- exact-input/exact-output counts;
- canonical block-directory hash.

### Format80 aggregates

- command count;
- command-kind aggregate counts;
- maximum back-reference distance;
- overlap-copy count;
- maximum command output;
- terminator count/status categories;
- trailing-input diagnostic count;
- absolute/relative candidate result categories;
- canonical decoded aggregate hash.

### LZO aggregates

- backend and codec kind;
- normalized return/status counts;
- exact-input and exact-output counts;
- block failure categories;
- canonical decoded aggregate hash.

### Cross-mode evidence

- Memory/Stream/MIX-window equality booleans;
- status and diagnostic counts;
- canonical aggregate hashes;
- source fingerprint before/after.

## 5. Forbidden public fields

Never publish:

- INI fragment values;
- Base64 text;
- compressed bytes;
- decoded bytes;
- Format80 command sequence for an individual block;
- command parameter values or offsets by block;
- LZO token/match sequence;
- Overlay or OverlayData arrays;
- map coordinates;
- Preview pixels or images;
- IsoMap records;
- per-cell/per-block decoded hashes that permit reconstruction;
- hex or Base64 dumps;
- absolute paths;
- usernames or machine identifiers.

The audit may compute internal data to validate a model, but the public serializer must reject it.

## 6. Fragment-order probe

For every section:

1. retain raw source occurrence order;
2. build `NumericAscendingUnique` candidate;
3. build `SourceOccurrenceOrder` candidate;
4. decode Base64 only when the candidate is unambiguous;
5. compare block-directory and decoded aggregate hashes;
6. report how many samples are identical, distinguishable, ambiguous, or invalid.

Do not publish the fragment order itself.

## 7. Format80 variant probe

For Overlay sections, evaluate named profiles without fallback:

- absolute medium/long positions;
- relative medium/long distances;
- marker-selected relative candidate where marker evidence exists.

Report only:

- success/failure counts;
- exact input/output counts;
- command-kind aggregates;
- canonical decoded aggregate hashes;
- samples that distinguish profiles.

A sample that succeeds under multiple profiles does not vote.

## 8. Termination probe

Aggregate:

- terminator exactly at block end;
- terminator before declared output;
- output reached before terminator;
- payload trailing bytes after terminator;
- missing terminator;
- zero-size chunk header categories;
- input-exhaustion completion.

No tolerant result becomes default unless supported across multiple roles and a public independent source.

## 9. LZO identification probe

For each LZO block:

- decode as raw LZO1X using the reviewed backend;
- record exact input/output and normalized status;
- do not attempt to infer compressor level from successful decode;
- classify whether a final decoded four-byte zero suffix exists at the map-content layer without publishing bytes.

Report counts by section role and sample role.

## 10. Input-mode equivalence

Require exact equality of:

- fragment-directory hash;
- block-directory hash;
- status;
- bytes consumed/produced;
- command-kind counts;
- diagnostics and order;
- decoded aggregate hash.

Short-read stream adapters must vary read boundaries deterministically.

## 11. Suggested summary schema

```text
MapCompressionAuditSummary
- AuditVersion
- SourceFingerprintBefore
- SourceFingerprintAfter
- SampleCount
- SelectionBasisCounts
- FragmentAggregate
- ChunkAggregate
- Format80Aggregate
- LzoAggregate
- VariantComparison
- InputModeEquivalence
- DiagnosticCounts
- SanitizedSummarySha256
```

## 12. Decision gates

### Gate A — production Format80 variant

Requires:

- at least two role-diverse distinguishing samples;
- one independent public source supporting the same reference convention;
- exact input/output and terminator agreement;
- no clipping, padding or variant fallback.

### Gate B — LZO backend

Requires:

- exact raw LZO1X compatibility across all selected LZO blocks;
- exact error reporting;
- license/security approval;
- Memory/Stream/MIX equivalence.

### Gate C — fragment policy

Requires:

- stable result under randomized filesystem/INI enumeration;
- duplicate/gap cases remain fail-closed;
- stock sample evidence for numeric or source order.

### Gate D — insufficient evidence

Keep compression compatibility unresolved and do not modify the compatibility matrix.
