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

## 2. Normalized ordinal evidence

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| The ordinary type byte is commonly treated as a zero-based `[OverlayTypes]` ordinal | `ConfirmedCommunityConvention` | ModEnc oldid 21267, OpenRA, WAE and other community tooling | Stable toolchain convention, not original-runtime source proof. | Expose an ordinal candidate only after applying the selected storage profile. | `NotRun` |
| FinalAlert/FinalSun's observed table handling | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2, where directly observed | Establishes official-editor behavior only. | Preserve source-specific behavior as evidence, not as the universal registry algorithm. | `NotRun` |
| One exact stock-runtime registry construction algorithm is established | `Unresolved` | No original-runtime source located | Numeric keys, physical order, gaps, duplicates, case, and map-local composition are not fully resolved. | Require explicit composition and comparison policies. | `NotRun` |
| WAE's sequential-list construction is the universal registry rule | `ImplementationSpecificBehavior` | World-Altering Editor | WAE appends previously unseen names and assigns sequential list indices; this is named-tool behavior. | Do not adopt it as the default ordinal model. | `NotRun` |
| Ordinals remain stable when Art or resources are missing | `DefensiveDesign` | Project policy | Stable identity is a project preservation rule, not an external runtime fact. | Never renumber after downstream lookup failure. | `NotRun` |

## 3. Numeric keys versus section order

A robust registry model must preserve the distinction between:

- numeric key identity, for example `2=SomeOverlay`;
- physical source order;
- normalized integer ordinal;
- value/section name;
- cross-layer winner and suppressed values.

WAE's inspected generic type loader iterates section keys, appends previously unseen names, and assigns sequential list indices; it does not use the numeric key as the assigned index. That is `ImplementationSpecificBehavior`, not proof of original-runtime construction.

The project default uses `DefensiveDesign` to build an explicit ordinal map from composed numeric keys and diagnose:

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

Public tools such as WAE read map-local type lists and properties into their model, but exact vanilla runtime behavior for changing `[OverlayTypes]` ordinals from a map remains `Underconfirmed`.

The project design therefore distinguishes:

- `GlobalComposedRegistry`;
- `MapLocalRegistryCandidate`;
- `EffectiveRegistryUnderPolicy`;
- provenance for every ordinal;
- evidence grade for whether map-local changes are allowed to add, override, or reorder entries.

The map-local layer may not silently renumber existing ordinals. That prohibition is `DefensiveDesign`.

## 6. Gaps and missing ordinals

Under the project preservation model, registry gaps are identity gaps, not list positions to remove.

Example:

```text
0=A
2=C
```

The effective registry contains ordinal 0 and 2; ordinal 1 is missing. It must not be normalized to `0=A, 1=C`.

When `OverlayTypeRaw == 1`, the project result is `MissingRegistryOrdinal`, not `C` and not empty. Exact original-runtime gap behavior remains `Unresolved`.

## 7. Duplicate ordinals

Duplicate normalized keys can arise from:

- repeated `2=` entries in one physical section;
- duplicate `[OverlayTypes]` sections;
- `2=` and `02=`;
- composition layers with conflicting values;
- a parser that normalizes case or whitespace differently.

The lossless INI and semantic-composition layers must distinguish same-layer duplicate keys from cross-layer overrides. The registry binder consumes their explicit result and must not apply last-wins again.

If composition leaves ambiguity, the raw type has no unique bound descriptor. Fail-closed ambiguity handling is `DefensiveDesign`; stock-runtime duplicate behavior remains `Unresolved`.

## 8. Name and case policy

Separate policies are required for:

- registry section name comparison;
- registry key parsing;
- Overlay logical section comparison;
- Art section comparison;
- resource filename lookup.

Case-insensitive behavior observed on Windows or in a tool is `ImplementationSpecificBehavior` unless stronger evidence applies. It does not prove original identifier semantics. The project preserves original spelling while using an explicit comparison profile.

## 9. Empty and unknown values

For the ordinary byte profile:

- `0xFF` is handled by `OverlayEmptyTypePolicy` before registry lookup;
- `0x00` is ordinal zero, not empty;
- `0xFE` is a possible ordinal 254 if the effective registry contains it;
- any other unbound raw value remains `UnknownOverlayType`.

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert/FinalSun treats `0xFF` as no Overlay in the ordinary byte profile | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Official-editor behavior only. | Preserve the raw byte and classify through the selected empty-type profile. | `NotRun` |
| `0xFF` is a stable ordinary-profile toolchain sentinel | `ConfirmedCommunityConvention` | ModEnc and multiple public tools | Confirms convention, not universal runtime exclusivity. | Use only in the ordinary byte profile. | `NotRun` |
| `0x00` and `0xFE` can be ordinary ordinal candidates | `Underconfirmed` | Registry convention and public tool behavior | Runtime acceptance depends on the composed registry and no original-runtime source resolves every case. | Never treat them as empty solely by value. | `NotRun` |
| Unknown values are preserved rather than rewritten to `0xFF` | `DefensiveDesign` | Project policy | Prevents data loss and false emptiness. | Retain raw type and data with diagnostics. | `NotRun` |

For an extended 16-bit profile, the sentinel candidate is `0xFFFF`. Its behavior is `ImplementationSpecificBehavior` for the named extension profile unless stronger evidence is supplied.

## 10. Missing Art or image resources

Binding proceeds in stages:

1. raw ordinal to registry entry;
2. registry entry to logical Overlay section;
3. Overlay descriptor to Art section/image candidate;
4. image candidate to resource provider result.

Failure at a later stage does not renumber or unbind earlier ordinals. Missing Art cannot shift later entries and cannot be used to infer a different raw ordinal. This is `DefensiveDesign`.

## 11. Ares/Phobos and extension boundary

Extensions may:

- add more Overlay types;
- permit indices above 254 through an extended map profile;
- add new flags and semantics;
- alter editor compatibility expectations.

These must be represented by explicit extension descriptors. Named extension behavior is `ImplementationSpecificBehavior` and does not redefine vanilla RA2/YR evidence.

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

The following are `DefensiveDesign` prohibitions. Do not:

- infer IDs from section enumeration order;
- compress gaps;
- reorder by object name;
- shift IDs after missing Art/TMP/SHP resources;
- select a type because its image exists;
- select a different ordinal because its OverlayData looks plausible;
- treat unknown high values as empty;
- merge vanilla and extension registries without an explicit policy;
- lose winner/suppressed provenance.

```text
AuditStatus: NotRun
FutureEvidenceSource: ProjectBaselineAggregateAudit
```
