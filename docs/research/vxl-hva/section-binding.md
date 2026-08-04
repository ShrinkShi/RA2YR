# VXL/HVA section binding

> Binding is a separate model operation. Neither the VXL reader nor the HVA reader receives the other document as a parse prerequisite.

## 1. Required pipeline

1. Read and validate VXL independently.
2. Read and validate HVA independently.
3. Preserve VXL and HVA section names as raw 16-byte values plus decoded candidates.
4. Build all candidate matches.
5. Return complete, incomplete or ambiguous binding without silently choosing a winner.
6. Leave resource composition and runtime transform use to higher layers.

## 2. Candidate identity evidence

For every section retain:

- file ordinal;
- raw 16-byte name;
- decoded ASCII candidate;
- first-NUL position;
- section number from the VXL section header;
- duplicate-name and duplicate-number groups.

Binding should compare raw/decoded names under explicitly named policies. It must not mutate names by trimming arbitrary whitespace, changing punctuation, appending suffixes or applying Art.ini fallback rules.

## 3. Recommended default matching policy

### Step A — exact raw-name candidate

A VXL and HVA section are exact candidates when their normalized fixed-field identity is byte-equal up to the first NUL and both fields have valid padding.

- exactly one VXL and one HVA member: unique candidate;
- duplicate on either side: ambiguous group;
- no exact candidate: remain unbound.

### Step B — ordinal evidence

Ordinal agreement may strengthen a unique exact-name match. It must not resolve duplicate names or replace a missing name match by default.

### Step C — case-only candidates

A case-insensitive-only match is reported as a candidate conflict:

- preserve all candidates;
- do not select a winner;
- allow a future explicitly configured experimental strategy to test case folding.

### Step D — index fallback

Some tools fall back to matching section ordinal when name lookup fails. This is unsafe when sections were reordered or names are duplicated. Index fallback is therefore:

- disabled by default;
- never part of strict compatibility;
- only available as a named experimental binding strategy after golden evidence;
- required to report that it ignored name evidence.

## 4. Count mismatch

`VXL sectionHeaderCount`, `VXL sectionTailerCount` and `HVA sectionCount` are separately retained.

Possible outcomes:

- all counts equal and all names uniquely bind: `Complete`;
- HVA has fewer sections: unmatched VXL sections retained, `Incomplete`;
- HVA has more sections: unmatched HVA transforms retained, `Incomplete`;
- VXL header/tailer count mismatch: VXL structural diagnostic before HVA binding;
- equal counts with ambiguous names: `Ambiguous`, not index-selected.

Count equality alone never proves name identity.

## 5. Duplicate and malformed names

| Condition | Binding behavior |
|---|---|
| duplicate exact VXL names | all matching candidates retained; ambiguous |
| duplicate exact HVA names | all matching candidates retained; ambiguous |
| missing NUL in 16 bytes | raw identity retained; warning; exact 16-byte comparison allowed only by explicit policy |
| invalid non-ASCII bytes | no lossy replacement identity; raw bytes retained |
| nonzero bytes after first NUL | malformed padding diagnostic; raw evidence retained |
| empty name | multiple empty names ambiguous; no ordinal winner |
| names differ only by case | case-conflict diagnostic; no default winner |

## 6. Missing HVA

A missing HVA is not a VXL parse failure. The result should distinguish:

- VXL document parsed successfully;
- no HVA document supplied/found;
- no binding performed;
- resource-policy consequence unresolved.

Community sources state that stock games expect paired HVA resources for voxel models, while modern tools may load a VXL without one. That is a content/runtime policy conflict, not evidence that the HVA binary reader should synthesize identity transforms.

The upper resource layer may later decide that a particular role requires HVA and report a missing dependency.

## 7. Multi-file body/turret/barrel composition

A unit may use separate body, turret and barrel VXL/HVA resource pairs. This is not the same as a multi-section VXL.

The binder handles only one supplied VXL document and one supplied HVA document at a time. Art/resource composition decides:

- which files form body/turret/barrel pairs;
- naming suffixes;
- optional or required pairs;
- object role and animation route.

Simulation/rendering decides independent turret/barrel rotation.

## 8. Binding model

Suggested result:

```text
VxlHvaBindingResult
- Status: Complete | Incomplete | Ambiguous | NotAttempted
- VxlDocumentIdentity
- HvaDocumentIdentity
- Bindings[]
- UnboundVxlSections[]
- UnboundHvaSections[]
- AmbiguousGroups[]
- Diagnostics[]
- StrategyUsed
- CanonicalBindingModelSha256
```

Each binding entry retains:

- VXL section ordinal/name identity;
- HVA section ordinal/name identity;
- match basis (`ExactNameAndOrdinal`, `ExactNameOnly`, experimental basis);
- no geometry or matrix values.

## 9. Matrix-order dependency

Binding names and determining the HVA transform flattening order are separate questions.

The binder should receive an HVA document that either:

- has a confirmed transform-order interpretation; or
- exposes multiple candidate interpretations without selecting one.

A successful name binding must not silently choose frame-major or section-major storage.

## 10. Diagnostics

Suggested codes:

- `VxlHvaSectionCountMismatch`
- `DuplicateVxlSectionName`
- `DuplicateHvaSectionName`
- `CaseOnlySectionMatch`
- `MalformedSectionNamePadding`
- `UnboundVxlSection`
- `UnboundHvaSection`
- `AmbiguousSectionBinding`
- `IndexFallbackDisabled`
- `MissingHvaResource`
- `HvaTransformOrderUnresolved`

## 11. Forbidden behavior

Do not:

- select the first matching name;
- use dictionary last-write-wins;
- automatically compare case-insensitively;
- silently bind by ordinal;
- drop unbound sections;
- rename sections from Art.ini inside the binder;
- synthesize identity HVA frames;
- combine body/turret/barrel files here;
- construct Unity transforms.
