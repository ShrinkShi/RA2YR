# Sanitized ProjectBaseline audit request

> **Research provenance:** This document was produced by ChatGPT Web from public sources. It did not read ProjectBaseline, is not a local Codex Agent artifact, and imports no code. GPL or unclear-license sources are reference-only; no source was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

## 1. Status and purpose

This is a design for a future local Codex read-only audit. ChatGPT Web did not access or enumerate ProjectBaseline, did not start RA2/YR or an editor, and did not inspect original map contents.

The audit should test public-source candidates without disclosing reconstructable map information. Its observations receive the evidence grade:

```text
ObservedByFutureProjectBaselineAudit
```

They must not be automatically promoted to `ConfirmedByOfficialRuntimeSource`.

## 2. Authoritative input boundary

The future audit must use only the project's frozen authoritative game content root and already approved map-selection process.

It must explicitly exclude:

- FinalAlert 2 installation and output directories;
- tutorial and code-dictionary directories;
- XCC manual export directories;
- unpacked mirrors;
- cache directories;
- other game installations;
- generated fixtures except in a separately labeled synthetic pass.

The audit is read-only. It must not modify timestamps, map files, archives, indexes, or configuration.

## 3. Selection matrix

Select sanitized samples by category rather than public map name.

Required coverage:

- all six RA2/YR theater categories where available;
- official-map and official-add-on-map provenance categories;
- small, medium, and large map-size buckets;
- no-Overlay or nearly-empty candidate;
- resource-heavy candidate;
- wall/fence-heavy candidate;
- bridge candidate, including high/low categories where safely identifiable;
- water/shore-heavy candidate;
- unknown or extension-defined Overlay candidate, if present in approved content;
- candidate with nonempty storage outside scenario domain;
- candidate with `0xFF` type and nonzero data;
- candidate registry gap or high type ID;
- ordinary NewINIFormat profile and any explicitly identified extended profile;
- maps with and without map-local Rules-like Overlay contributions.

`SelectionBasis` may describe these categories, but must not publish map names.

## 4. Audit phases

### Phase A — lossless packed-section inventory

For each selected map and each of the two sections, collect privately:

- section presence and occurrence count;
- fragment count;
- raw and normalized fragment-key classifications;
- numeric-order and source-order relationship;
- duplicate/gap diagnostics;
- Base64 character count;
- Base64 decoded compressed length and SHA-256.

Do not publish keys, values, Base64 text, or section source lines.

### Phase B — chunk and Format80 aggregate probe

Using the explicit candidate profiles, record privately:

- chunk count;
- compressed and declared-output length ranges;
- total compressed and declared-output lengths;
- command-kind aggregate counts;
- terminator classifications;
- maximum back-reference distance;
- overlapping-copy aggregate count;
- exact input/output consumption status;
- structured diagnostics.

Do not publish command sequences or compressed bytes.

Profiles must be selected before decode. The audit must not try variants and choose the output that looks most plausible.

### Phase C — decoded-array contract

For each stream, record:

- actual decoded length and SHA-256;
- expected storage profile;
- missing/trailing byte counts;
- exact ordinary or extended profile classification;
- whether both arrays have compatible coordinate and element-width profiles.

Do not publish decoded bytes or partial arrays.

### Phase D — raw pair aggregates

For the selected coordinate profile, compute aggregates only:

- count of `0xFF` type cells;
- count of nonempty type cells;
- observed raw type minimum/maximum;
- limited frequency buckets that cannot expose a full registry/name list;
- count of empty type with nonzero data;
- count of nonempty type with zero data;
- data-byte minimum/maximum and bounded histogram summary;
- count of unknown or unbound raw types.

Do not publish per-cell tuples or complete type-frequency tables if they could identify a map.

### Phase E — coordinate and domain analysis

Compute, without publishing positions:

- storage-domain element count;
- full scenario-domain count;
- LocalSize-domain count;
- nonempty cells inside/outside each domain;
- cells with and without corresponding IsoMap records;
- aggregate disagreement between row-major and transposed comparison profiles;
- count of domain anomalies.

Do not auto-select the coordinate profile from these results. Publish only aggregate counts and an evidence-gated comparison result.

### Phase F — composed registry binding

Using the completed INI composition pipeline, record:

- registry ordinal count;
- gap count and range summaries;
- duplicate normalized ordinal count;
- duplicate-name count;
- map-local contribution count;
- raw type binding success/failure counts;
- unknown and sentinel counts;
- winner/suppressed provenance-category aggregates;
- extension-profile requirement count.

Do not publish complete OverlayType names, names by ordinal, section contents, or type-to-resource mappings.

### Phase G — semantic-profile aggregate analysis

Classify bound records only into broad categories:

- generic/decoration;
- resource;
- wall/fence;
- bridge;
- road/track candidate;
- crate/interactive candidate;
- TS-inherited candidate;
- extension-defined;
- unknown.

Record aggregate counts and diagnostic classes. Do not publish specific object names or locations.

### Phase H — input-mode equivalence

Run the same parser state machine over:

- Memory input;
- seekable Stream;
- adversarial short-read Stream;
- bounded MIX-entry window.

Compare canonical aggregate model hashes, diagnostics, provenance categories, and decode lengths. Input modes must not change winner, profile, or array data.

## 5. Allowed public outputs

The audit may publish only:

- `SelectionBasis` categories;
- logical provenance categories;
- section presence classification;
- fragment counts;
- Base64-decoded compressed lengths and SHA-256;
- chunk-count and Format80 command aggregates;
- compressed/uncompressed length ranges;
- decoded array lengths and SHA-256;
- selected ordinary/extended profile category;
- `0xFF` count;
- nonempty type count;
- raw type range and limited non-reconstructable aggregates;
- empty-type plus nonzero-data count;
- nonempty-type plus zero-data count;
- registry binding success/failure counts;
- registry gap/duplicate aggregate counts;
- semantic-profile category counts;
- storage/full-map/LocalSize/IsoMap-presence aggregate counts;
- resource/wall/bridge broad-category aggregates;
- diagnostics and evidence grades;
- canonical aggregate hashes;
- Memory/Stream/short-read/MIX equivalence result.

## 6. Forbidden public outputs

The audit must not publish:

- map names, filenames, display names, or scenario titles;
- a complete OverlayType name list;
- OverlayType names by ordinal;
- INI section text, keys, or values;
- Base64 text;
- compressed bytes;
- decoded bytes;
- complete arrays or slices;
- coordinate sequences or positions;
- per-cell type/data tuples;
- raw type-ID sequences;
- type ID to logical name, Art, SHP, resource, or bridge mapping;
- bridge positions or layouts;
- resource/ore/gem positions;
- wall/fence layouts;
- overlay positions outside map bounds;
- map previews, screenshots, images, or meshes;
- TMP, palette, SHP, or Art contents;
- per-cell or per-entry hashes;
- Base64 or hexadecimal dumps;
- absolute paths, usernames, machine identifiers, or drive names;
- any combination sufficient to reconstruct or fingerprint a specific map beyond approved aggregate hashes.

## 7. Privacy-preserving hashes

Allowed SHA values apply to whole logical packed inputs or complete decoded arrays under a sanitized selection identifier. Do not publish hashes for individual coordinates, ranges, Overlay types, fragments, registry entries, or assets.

Canonical aggregate hashes should encode only approved aggregate fields, sorted deterministically, with a documented schema version.

## 8. Randomization checks

The audit should repeat normalization after:

- randomizing filesystem enumeration;
- randomizing supplied fragment enumeration while preserving source metadata;
- varying Stream read sizes;
- varying MIX window chunk reads.

The canonical result must remain identical. The audit must not randomize or mutate the actual map data.

## 9. Candidate comparison rules

For unresolved coordinate, Format80, registry, or semantic profiles:

- execute each profile only when its use is explicitly approved for comparison;
- report separate aggregate results;
- do not choose a winner because output has more `0xFF`, fewer diagnostics, more registry bindings, or more in-map cells;
- require independent evidence plus aggregate observations before changing configured policy;
- retain `Unresolved` when profiles remain indistinguishable.

## 10. Budget and failure reporting

Record whether the audit hits:

- fragment budget;
- Base64-character budget;
- compressed-byte budget;
- chunk budget;
- command budget;
- aggregate output budget;
- diagnostic budget;
- registry-entry budget;
- semantic-profile-candidate budget.

Budget failure must produce a sanitized diagnostic, not partial success.

## 11. No-execution guarantees

The future audit must not:

- launch RA2/YR;
- launch Unity;
- launch FinalAlert/FinalSun;
- launch XCC GUI;
- write or repack a map;
- update a MIX;
- generate screenshots or previews;
- invoke runtime simulation;
- attempt resource harvesting, wall connection, bridge damage, or pathfinding;
- modify the compatibility matrix or research conclusions automatically.

## 12. Expected audit report structure

```text
AuditSchemaVersion
SelectionBasis
InputProvenanceCategory
PackedSectionSummary
CodecAggregateSummary
DecodedArrayContractSummary
CoordinateDomainAggregateSummary
RegistryBindingAggregateSummary
SemanticCategoryAggregateSummary
InputModeEquivalence
Diagnostics
EvidenceGrade = ObservedByFutureProjectBaselineAudit
PolicyImpactRecommendation
```

`PolicyImpactRecommendation` may propose follow-up review but may not alter project policy or claim original-runtime confirmation.