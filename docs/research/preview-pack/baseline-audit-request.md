# Sanitized future ProjectBaseline audit request for PreviewPack

> Source notice: designed by **ChatGPT Web** from public research. ChatGPT Web did **not** access local `ProjectBaseline`; this is not a Codex Agent artifact; no source implementation or original asset was copied, translated, mechanically rewritten, or imported (`code_imported: false`).

This is a request specification for a future local Codex run. It is not executed by this PR.

## 1. Objective and status

Collect bounded aggregate evidence about Preview metadata, packed fragments, chunk envelopes, decoded lengths, channel/row candidates, physical section order, and missing/fabricated preview categories without publishing any map identity, pixels, compressed content, or reconstructable information.

```text
AuditStatus: NotRun
FutureEvidenceSource: ProjectBaselineAggregateAudit
```

These fields describe planned work. They are not a formal evidence grade and do not imply that ProjectBaseline was read, observed, or confirmed. Current claims retain the public-source grades recorded in the research dossier, normally `Underconfirmed`, `ConflictingSources`, or `Unresolved`. A future aggregate audit cannot by itself become `ConfirmedByOriginalRuntimeSource`.

## 2. Authoritative root boundary

The future audit may read only the configured authoritative `ProjectBaseline` root supplied locally to Codex.

It must not search:

- parent directories;
- alternate installations;
- editor tutorial directories;
- downloads;
- XCC exports;
- unpacked mirrors;
- caches;
- screenshots;
- user image folders;
- other drives;
- registry-discovered paths.

No file is modified.

## 3. Sample-selection basis

Select samples by opaque categories, not by public map names.

Coverage should include:

- all six theater categories where available;
- official-map and official-add-on categories;
- small, medium, and large map dimensions;
- multiple Preview dimensions;
- candidates with nonzero first two Size fields;
- both sections present;
- one or both sections missing;
- invalid or inconsistent metadata/payload candidates, if present;
- old-editor and newer-tool generation categories inferred only from sanitized structural traits;
- differing physical Preview section positions;
- single-block and multi-block payloads;
- final short blocks;
- unusual compression ratios;
- hidden/dummy preview candidates;
- channel-order conflict candidates;
- row-order conflict candidates.

Selection should be deterministic from opaque hashes and category quotas, not hand-picked by visual content.

## 4. Read-only pipeline

```text
select logical map candidates
→ parse lossless INI
→ collect Preview metadata occurrences
→ collect PreviewPack fragments
→ strict Base64 in audit mode
→ inspect bounded chunk descriptors
→ decode with one explicitly configured LZO profile
→ validate exact aggregate length candidates
→ compute sanitized component/row aggregates
→ compare Memory/Stream/short-read/MIX results
→ emit aggregate-only report
```

Do not launch RA2/YR, Unity, FinalAlert, WAE, XCC, image viewers, or thumbnail generators.

## 5. Allowed public fields

### Selection and provenance

- `SelectionBasis` category;
- theater category only;
- official/add-on/other authorized category;
- logical provider/provenance category;
- source-format family;
- opaque sample count per category.

### Section and metadata aggregates

- `[Preview]` presence count;
- `[PreviewPack]` presence count;
- duplicate section count;
- duplicate `Size` count;
- four-field parse success count;
- bounded min/max and bucketed classification for each field across the aggregate;
- count of zero/nonzero/negative first fields;
- width/height min/max and coarse buckets;
- no per-map complete Size tuple.

### Fragment and compressed aggregates

- fragment count range and histogram buckets;
- canonical/noncanonical key counts;
- key-zero, leading-zero, gap, duplicate, and nonnumeric aggregate counts;
- aggregate Base64-decoded compressed length range;
- one aggregate SHA per selected corpus/category, not a list of per-map hashes;
- chunk count range;
- compressed/uncompressed size ranges;
- compression-ratio buckets;
- zero-size-header category counts;
- final-short-block counts;
- exact compressed-consumption counts.

### Decoded and pixel-layout aggregates

- decoded length range;
- aggregate corpus hash or canonical category hash;
- expected-equals-actual count;
- underflow/overflow/trailing category counts;
- component 0/1/2 min/max;
- coarse component histograms that cannot reconstruct an image;
- RGB/BGR candidate classification count based on source/profile evidence only, not visual inspection;
- top-down/bottom-up/unknown profile classification counts based on source/profile metadata only;
- no per-pixel or per-row results.

### Physical-order and fallback aggregates

- section physical-order category counts, such as first, after Basic, middle, end, duplicate;
- Preview/PreviewPack adjacency count;
- known hidden/dummy payload candidate count using an approved exact signature comparison performed locally;
- missing/fabricated-candidate category counts;
- no payload signature or fragment text.

### Validation and determinism

- structured diagnostic counts by code;
- maximum bounded values reached;
- canonical aggregate result hashes;
- Memory/seekable Stream/short-read Stream/MIX-window equivalence result;
- audit tool version and policy identifiers.

## 6. Forbidden public output

Never publish:

- map filename;
- map display name;
- scenario title;
- relative or absolute path;
- username, machine name, drive, or installation path;
- INI section body;
- exact per-map `Size` tuple;
- fragment keys or values for a specific map;
- Base64 text;
- compressed bytes;
- chunk payload bytes;
- decoded bytes;
- pixels;
- image, thumbnail, screenshot, or texture;
- per-pixel tuple;
- scanline or row data;
- component sequence;
- visual color distribution;
- per-map hash list;
- per-map component histogram;
- map coordinates;
- IsoMap records;
- Overlay arrays;
- TMP, palette, SHP, VXL, or HVA content;
- any mapping between a sample and an original map;
- any output sufficient to reconstruct a preview or identify a map.

No hex or Base64 dump is permitted, even for malformed samples.

## 7. Hash policy

Allowed hashes are aggregate and non-enumerative:

- one hash for all canonical decoded streams in a category after length-prefixing and deterministic ordering by opaque internal selector;
- one hash for an equivalence result corpus;
- no per-sample public hashes;
- no hash of tiny fragments that could enable dictionary matching;
- salt/key policy remains local and is not published if used for selection.

## 8. Component histogram privacy

Component summaries must be coarse enough to prevent reconstruction:

- fixed broad buckets;
- aggregate over multiple samples;
- minimum category sample threshold;
- suppress categories below threshold;
- never publish joint RGB triplet histograms;
- never publish spatial distributions;
- never publish row, column, quadrant, or coordinate buckets.

## 9. Channel-order audit rule

The audit must not render images or select RGB/BGR based on plausibility. It may report:

- which explicit source profile decodes exactly;
- whether bytes are identical under both semantic views;
- component-only aggregate summaries;
- profile conflicts.

It cannot claim the runtime profile solely from visual appearance or component statistics. The formal RGB/BGR classification remains `ConflictingSources` unless stronger source evidence resolves it.

## 10. Row-order audit rule

The audit must not create images, flips, or thumbnails. It may classify evidence based on known producer markers or structural provenance, but raw bytes alone normally cannot distinguish top-down from bottom-up without content inspection. Such cases remain `Unknown`.

## 11. Section-order audit rule

Record only categorical placement:

- first pair;
- after Basic;
- before/after Digest;
- middle;
- end;
- nonadjacent;
- duplicate occurrences.

Do not publish neighboring section names beyond approved generic categories if that could fingerprint a map.

## 12. Missing/fabricated audit rule

A known fixed hidden-preview signature may be compared locally. Public output is only an aggregate candidate count. The signature, Base64, compressed bytes, and decoded placeholder image are never output.

## 13. Error handling

- corrupted input produces diagnostics, not retries with guessed dimensions/profiles;
- no automatic RGB/BGR or row-order trial;
- no fallback to lenient zero-fill;
- no file writes;
- no generated previews;
- no map save;
- no external application launch;
- budgets remain active even for trusted baseline content.

These are `DefensiveDesign` audit requirements, not observations about the original runtime.

## 14. Reproducibility

The private audit record should retain:

- exact repository commit;
- audit tool commit;
- policy version;
- configured root identity without public path;
- selection algorithm version;
- sample-category counts;
- aggregate hashes;
- deterministic input-mode segmentation seed.

The public report contains no seed or identifier that maps back to individual files.

## 15. Expected decisions informed by audit

The audit may inform, but not automatically decide:

- whether nonzero Size origins occur;
- whether decoded length is always exact;
- typical block-output maxima;
- presence of zero-size terminators;
- physical section placement categories;
- known dummy-preview frequency;
- whether unusual dimensions occur;
- whether malformed/short consumer-tolerated streams exist.

Any production policy change requires separate review and source comparison.

## 16. Expected report structure

```text
AuditSchemaVersion
AuditStatus = NotRun
FutureEvidenceSource = ProjectBaselineAggregateAudit
SelectionBasis
InputProvenanceCategory
MetadataAggregateSummary
FragmentAndChunkAggregateSummary
DecodedLengthAggregateSummary
ChannelProfileAggregateSummary
RowProfileAggregateSummary
SectionPlacementAggregateSummary
MissingAndDummyAggregateSummary
InputModeEquivalence
Diagnostics
CurrentEvidenceGrade
PolicyImpactRecommendation
```

`CurrentEvidenceGrade` must use the normalized closed vocabulary and describe the public evidence available independently of the audit. `PolicyImpactRecommendation` cannot modify compatibility or project policy automatically.

## 17. Explicit statement

This PR did not run the audit, access ProjectBaseline, decode any local preview, or publish any original content.
