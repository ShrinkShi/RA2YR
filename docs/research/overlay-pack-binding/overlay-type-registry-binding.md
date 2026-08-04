# Overlay type registry binding

> **Research provenance:** This document was produced by ChatGPT Web from public sources. It did not read ProjectBaseline, is not a local Codex Agent artifact, and imports no code. GPL or unclear-license sources are reference-only; no source was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

## 1. Binding objective

The type byte in ordinary `OverlayPack` is a raw storage value. Its semantic identity is resolved through an ordered, composed Overlay registry.

Candidate pipeline:

```text
OverlayTypeRaw
→ selected storage profile
→ ordinal candidate
→ composed [OverlayTypes] registry
→ logical Overlay section
→ typed Overlay descriptor
→ Art/resource references
→ type-specific semantic adapter
```

The binding layer does not modify raw arrays and does not decide rendering, resources, wall health, bridge passability, or pathfinding.

## 2. Zero-based ordinal evidence

ModEnc oldid 21267 states that `OverlayPack` stores a zero-based index into `[OverlayTypes]`. OpenRA and WAE treat the raw value as an index into an overlay-type table/list. Community tooling and editor code consistently use an ordinal concept.

Evidence grade:

- zero-based ordinal model: `CommunityDocumented` and `ConfirmedByIndependentImplementation`;
- exact original runtime registry construction: `Unresolved`;
- official editor table behavior: `ConfirmedByOfficialEditorSource` where directly observed.

## 3. Numeric keys versus section order

A robust registry model must preserve the distinction between:

- numeric key identity, for example `2=SomeOverlay`;
- physical source order;
- normalized integer ordinal;
- value/section name;
- cross-layer winner and suppressed values.

WAE's inspected generic type loader iterates section keys, appends previously unseen names, and assigns sequential list indices; it does not use the numeric key as the assigned index. That is useful evidence of WAE behavior but is not a safe project rule and is not proof of original runtime construction.

The project default should instead build an explicit ordinal map from composed numeric keys and diagnose:

- gaps;
- duplicate normalized ordinals;
- negative or nonnumeric keys;
- leading-zero aliases;
- duplicate names at different ordinals;
- same ordinal with different names across layers;
- name case conflicts;
- missing target object section.

## 4. Cross-layer composition

The registry is built from the already-composed INI semantic view established by the content-load-order research.

For each ordinal, the composition result must retain:

- effective name/value;
- winning document layer;
- suppressed same-key candidates;
- raw key spelling;
- section/key case;
- map-local contributions;
- extension profile contributions;
- diagnostics.

The Overlay binder must not scan archives, loose files, or map text. It consumes a completed composed registry descriptor.

## 5. Map-local Rules effects

A map can contain Rules-like sections. Public tools such as WAE read map-local type lists and properties into their model, but exact vanilla runtime behavior for changing `[OverlayTypes]` ordinals from a map remains underconfirmed.

The project design therefore distinguishes:

- `GlobalComposedRegistry`;
- `MapLocalRegistryCandidate`;
- `EffectiveRegistryUnderPolicy`;
- provenance for every ordinal;
- evidence grade for whether map-local changes are allowed to add, override, or reorder entries.

The map-local layer may not silently renumber existing ordinals.

## 6. Gaps and missing ordinals

Registry gaps are real identity gaps, not empty list positions to remove.

Example:

```text
0=A
2=C
```

The effective registry contains ordinal 0 and 2; ordinal 1 is missing. It must not be normalized to `0=A, 1=C`.

When `OverlayTypeRaw == 1`, the result is `MissingRegistryOrdinal`, not `C` and not empty.

## 7. Duplicate ordinals

Duplicate normalized keys can arise from:

- repeated `2=` entries in one physical section;
- duplicate `[OverlayTypes]` sections;
- `2=` and `02=`;
- composition layers with conflicting values;
- a parser that normalizes case or whitespace differently.

The lossless INI and semantic-composition layers must distinguish same-layer duplicate keys from cross-layer overrides. The registry binder consumes their explicit result and must not apply last-wins again.

If composition leaves ambiguity, the raw type has no unique bound descriptor.

## 8. Name and case policy

Separate policies are required for:

- registry section name comparison;
- registry key parsing;
- Overlay logical section comparison;
- Art section comparison;
- resource filename lookup.

Case-insensitive behavior observed on Windows or in a tool does not prove original identifier semantics. The project should preserve original spelling while using an explicit comparison profile.

## 9. Empty and unknown values

For the ordinary byte profile:

- `0xFF` is handled by `OverlayEmptyTypePolicy` before registry lookup;
- `0x00` is ordinal zero, not empty;
- `0xFE` is a possible ordinal 254 if the effective registry contains it;
- any other unbound raw value remains `UnknownOverlayType`.

Unknown values are preserved with raw data. They are not converted to `0xFF`.

For an extended 16-bit profile, the sentinel candidate is `0xFFFF`; its evidence and allowed games/extensions must be attached to the profile.

## 10. Missing Art or image resources

Binding proceeds in stages:

1. raw ordinal to registry entry;
2. registry entry to logical Overlay section;
3. Overlay descriptor to Art section/image candidate;
4. image candidate to resource provider result.

Failure at a later stage does not renumber or unbind earlier ordinals. Missing art cannot shift later entries and cannot be used to infer a different raw ordinal.

## 11. Ares/Phobos and extension boundary

Extensions may:

- add more Overlay types;
- permit indices above 254 through an extended map profile;
- add new flags and semantics;
- alter editor compatibility expectations.

These must be represented by explicit extension descriptors. They do not redefine vanilla RA2/YR evidence.

No complete Ares/Phobos extension behavior is frozen in this dossier without pinned, load-bearing evidence.

## 12. Recommended descriptors

```text
OverlayRegistryDescriptor
- SourceCompositionId
- OrdinalEntries
- Gaps
- DuplicateOrdinalGroups
- DuplicateNameGroups
- ComparisonProfile
- ExtensionProfile
- Diagnostics

OverlayRegistryEntry
- Ordinal
- RawKeySpellings
- LogicalName
- WinnerProvenance
- SuppressedCandidates
- LogicalSectionCandidate
- ArtCandidate
- EvidenceGrade

OverlayTypeBindingResult
- RawType
- EmptyClassification
- OrdinalCandidate
- RegistryEntry
- LogicalDescriptor
- BindingStatus
- Diagnostics
- ResolutionTrace
```

## 13. Forbidden binding shortcuts

Do not:

- infer IDs from section enumeration order;
- compress gaps;
- reorder by object name;
- shift IDs after missing Art/TMP/SHP resources;
- select a type because its image exists;
- select a different ordinal because its OverlayData looks plausible;
- treat unknown high values as empty;
- merge vanilla and extension registries without an explicit policy;
- lose winner/suppressed provenance.