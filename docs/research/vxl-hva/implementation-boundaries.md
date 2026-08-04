# Proposed Core implementation boundaries

> Design only. This file does not contain C# implementation code or a mechanical translation of any reference reader.

## 1. Architectural goals

- VXL and HVA parse independently.
- Core retains raw fields, offsets, names and float bit patterns.
- Sparse geometry remains sparse by default.
- Validation is bounded, deterministic and fail-closed.
- Binding is explicit and can remain incomplete/ambiguous.
- Normal-vector lookup is separate from binary parsing.
- Art.ini composition, simulation and rendering remain outside format readers.
- Core has no `UnityEngine` or `System.Drawing` dependency.

## 2. Reuse existing binary infrastructure

A future implementation should reuse the repository's existing abstractions where applicable:

- `BinarySourceContext`
- `BinaryReadSession`
- `BoundedBinaryReader`
- `ReadOnlyDataWindow`
- MIX virtual-entry windows and provenance chains
- checked read/allocation/record/subwindow budgets
- stable structured diagnostics

Memory, seekable Stream and MIX-window entry points should converge on one bounded parse core.

## 3. VXL raw model

### `VxlHeader`

Retains:

- `FileTypeRaw[16]`
- `PaletteCountRaw`
- `SectionHeaderCountRaw`
- `SectionTailerCountRaw`
- `BodySizeRaw`
- `StartPaletteRemapRaw`
- `EndPaletteRemapRaw`
- `RemapRangeWordRaw`
- `PaletteRaw[768]`

Derived candidate properties must never replace raw values.

### `VxlSectionHeader`

- file ordinal
- absolute descriptor offset
- `NameRaw[16]`
- decoded-name candidate and padding state
- `SectionNumberRaw`
- `Unknown1Raw`
- `Unknown2Raw`

### `VxlSectionTailer`

- file ordinal and absolute tailer offset
- three raw signed/unsigned offset views
- `ScaleRawBits`
- twelve transform raw float bit patterns
- six bounds raw float bit patterns
- `SizeXRaw`, `SizeYRaw`, `SizeZRaw`
- `NormalTypeRaw`
- finite-value candidates and diagnostics

### `VxlDocument`

- source/provenance/input length/absolute start
- raw header
- ordered section headers
- ordered section tailers
- validated body window metadata
- candidate ordinal header/tailer pairs
- canonical raw-directory model hash
- document diagnostics

The initial document need not materialize voxels.

## 4. Span layer

### `VxlSpanDirectory`

Per section:

- column count
- raw start/end entries as signed 32-bit values
- validated column-range classifications
- table and data absolute/body-relative ranges
- overlap/alias diagnostics

### `VxlVoxel`

Sparse immutable record:

- `X`, `Y`, `Z` as bounded integer coordinates
- `ColorIndexRaw`
- `NormalIndexRaw`
- source column/chunk ordinal for diagnostics only

Do not expose mutable arrays that allow overlapping columns to alias decoded storage.

### Decode result

A `VxlSectionDecodeResult` or equivalent should return:

- success/failure;
- optional sparse section model;
- stored voxel count;
- chunk count;
- consumed ranges;
- diagnostics.

No default dense 3D array is required.

## 5. Normal table boundary

### `VxlNormalTableKind`

Candidate enum with raw fallback:

- `TiberianSun36`
- `RedAlert2Yuri244`
- `Unknown`

### Separate resolver

A `VxlNormalResolver` can later map `(kind, index)` to an engine-neutral vector type owned by Core math or a small immutable triple. The parser only validates range when an approved table definition is available.

No palette, VPL or light calculation belongs in this resolver.

## 6. VXL results and diagnostics

Suggested types:

- `VxlParseResult`
- `VxlDiagnostic`
- `VxlDiagnosticCode`
- `VxlReadLimits`
- `VxlDecodedSection`
- `VxlDecodeDocumentResult`

Suggested diagnostic fields mirror existing binary diagnostics:

- severity and code;
- source/provenance;
- absolute offset;
- requested and remaining length;
- section, column, chunk and voxel ordinals where safe;
- field/section identifier;
- concise message.

Diagnostics must not include voxel values, original names beyond already public logical identifiers, body bytes or reconstructable geometry.

## 7. VXL limits

`VxlReadLimits` should independently bound:

- input bytes;
- section headers/tailers;
- body bytes;
- dimensions and per-section volume;
- columns;
- chunks per column/section/document;
- sparse voxels per section/document;
- total allocated bytes;
- records/subwindows/diagnostics;
- single read size.

Limits are not format constants and must not alter valid values.

## 8. HVA raw model

### `HvaHeader`

- `FileNameRaw[16]`
- decoded candidate/padding state
- `FrameCountRaw`
- `SectionCountRaw`

### `HvaSectionName`

- ordinal
- raw 16 bytes
- decoded candidate
- NUL/padding/ASCII diagnostics

### `HvaRawTransform3x4`

- record ordinal
- twelve raw `uint32` float bit patterns
- optional finite float values
- no transpose, multiplication or target-library matrix

### `HvaDocument`

- source/provenance/input metadata
- header
- ordered names
- ordered raw transform records
- candidate order interpretations
- canonical raw-model hash
- diagnostics

### Results

- `HvaParseResult`
- `HvaDiagnostic`
- `HvaDiagnosticCode`
- `HvaReadLimits`

HVA limits independently bound frames, sections, transform count, input/allocation and diagnostics.

## 9. Transform-order interpretation

Use an explicit model such as:

```text
HvaTransformRecordOrder
- FrameMajor
- SectionMajor
- Unresolved
```

The raw records remain unchanged. A resolver returns a view mapping `(frame, section)` to raw record ordinal under a selected strategy.

Production default cannot become FrameMajor or SectionMajor until the evidence gate in `unresolved-questions.md` and the local audit is satisfied.

## 10. VXL/HVA binding

### `VxlHvaBindingResult`

- binding status;
- strategy used;
- unique bindings;
- unbound VXL/HVA sections;
- ambiguous candidate groups;
- count/name/order diagnostics;
- canonical binding hash.

Binding does not copy voxel or matrix payloads into a combined monolith. It holds stable document identities and ordinal references.

## 11. Module split

Recommended conceptual layers:

1. **VXL directory reader** — header, section headers, body/tailer ranges.
2. **VXL span-directory reader** — tables and validated column windows.
3. **VXL sparse decoder** — chunk validation and sparse voxels.
4. **Normal resolver** — approved mode/index to engine-neutral vector.
5. **HVA reader** — header, names and raw records.
6. **HVA order view** — candidate flattening interpretation.
7. **VXL/HVA binder** — name/order matching with ambiguity.
8. **Content composition** — Art/resource pair selection.
9. **Transform/render adapter** — coordinate conversion and Unity objects.

Do not combine these into one `VoxelLoader` class.

## 12. Canonical hashes

Safe deterministic hashes can cover:

- VXL raw header and section/tailer metadata;
- VXL validated sparse model in canonical coordinate order, for local non-public manifests;
- HVA raw header/names/record bit patterns;
- candidate HVA order model;
- binding identities and match basis.

Public audit summaries may report only the allowed canonical model hashes. They must not expose per-section voxel hashes or matrix value lists that aid reconstruction.

## 13. No Unity boundary leakage

Core tests should assert its assembly references do not include `UnityEngine` and public/internal model types do not expose:

- `Texture2D`, `Mesh`, `Material`;
- `Vector3`, `Quaternion`, `Matrix4x4` from Unity;
- renderer-facing vertex buffers;
- game-object or scene-node references.

A later adapter can consume immutable Core models.
