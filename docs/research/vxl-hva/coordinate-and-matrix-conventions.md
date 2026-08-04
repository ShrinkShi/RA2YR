# Coordinate and matrix conventions

> This document separates confirmed file order from importer, graphics-library and runtime conventions.

## 1. Raw preservation rule

VXL and HVA readers expose numeric fields in file order and little-endian representation. They do not:

- swap axes;
- transpose matrices;
- change handedness;
- scale translations;
- compose transforms;
- convert to Unity types.

Every float field should retain its raw 32-bit bit pattern so a later adapter can be deterministic and auditable.

## 2. VXL section-local coordinate evidence

A section defines:

- a discrete `sizeX × sizeY × sizeZ` grid;
- sparse columns indexed by X/Y and advancing along Z in the common reader model;
- min/max bounds in three raw float components;
- a raw 3×4 transform in the section tailer;
- a scale/determinant-like float.

OpenRA comments that voxel coordinates are `x=forward, y=right, z=up`. VSE and vengi use different internal axes and explicitly permute them. These statements describe adapters, not additional bytes in the file.

The parser therefore names axes only according to the on-disk field order (`XRaw`, `YRaw`, `ZRaw`). A rendering adapter may later map them to world forward/right/up after independent validation.

## 3. HVA 3×4 raw record

The raw record is twelve sequential Float32LE values:

```text
r00 r01 r02 r03
r10 r11 r12 r13
r20 r21 r22 r23
```

The common affine interpretation treats the fourth value in each row as translation. That interpretation is strongly supported by community documents and editor code, but Core still retains raw file order.

Suggested raw type:

```text
HvaRawTransform3x4
- RawBits00 ... RawBits23
- TryGetFiniteValues()
- FileRecordOrdinal
```

It does not implement multiplication.

## 4. Row-major versus column-major

`row-major` can mean either:

1. the sequence in which twelve values appear in the file; or
2. the in-memory convention used by a math library.

Pinned sources mix these meanings:

- XCC and VSE declare a C/Pascal `float[3][4]` style record and index each row's four sequential values.
- OpenRA reads the same twelve values then transposes them into a sixteen-value column-major runtime array.
- vengi stores into GLM-style `matrix[column][row]` while reading file rows.

The format dossier calls the file record **3 rows × 4 sequential values**. It does not call any engine-expanded 4×4 representation canonical.

## 5. HVA record flattening order

This is separate from row/column-major within one matrix.

### Candidate F — frame-major

```text
record(frame, section) = frame × sectionCount + section
```

### Candidate S — section-major

```text
record(frame, section) = section × frameCount + frame
```

OpenRA, VSE, vengi and `cnc-formats` support F. XCC's accessor supports S. Both candidates yield identical order when either count is one.

Core should represent:

- raw record ordinal sequence;
- candidate frame/section mapping under F;
- candidate frame/section mapping under S;
- an unresolved status until evidence selects a contract.

Do not reorder the raw record array during parsing.

## 6. Translation and scale conflict

Community discussions sometimes multiply HVA translations by the VXL tailer's scale/determinant. Other implementations embed scale in bounds or convert using engine-specific constants.

Current safe boundary:

- VXL scale raw float remains in the VXL section tailer;
- VXL transform raw values remain separate;
- HVA raw translation values remain separate;
- no parser multiplies them;
- a later transform-composition design must cite local golden/runtime evidence.

## 7. VXL transform versus HVA transform

The VXL section tailer contains a 3×4 transform and HVA supplies per-frame 3×4 transforms. Sources disagree over whether HVA overrides, composes with or is converted relative to the VXL transform.

The binary models must preserve both. Proposed later composition input:

```text
SectionTransformInputs
- VxlTailerTransformRaw
- VxlScaleRaw
- VxlBoundsRaw
- HvaFrameTransformRaw? 
- SimulationTransform
- AdapterConvention
```

No composition order is selected in this research PR.

## 8. Handedness

The file does not contain an explicit handedness flag. Apparent handedness comes from:

- axis labels assigned by a source;
- cross-product/matrix convention in a renderer;
- camera projection;
- axis permutation and sign changes during import.

Therefore `left-handed` or `right-handed` is not a parser result. A later adapter must document a full basis mapping and validate it using known orientation evidence without publishing original geometry.

## 9. Bounds

Min/max bounds are six raw floats. Unresolved questions include:

- whether they are in voxel units, scaled units or an engine-space unit;
- whether max is inclusive or geometric extent;
- how they interact with `scale` and section transform;
- whether malformed/inverted bounds were tolerated by stock tools/runtime.

Core validates finiteness and reports componentwise `min > max`, but does not rewrite or sort bounds.

## 10. Numeric validation

### Parse errors

- truncated Float32LE;
- NaN or Infinity where a finite transform/bound is required by the selected strict policy;
- checked-size overflow.

### Diagnostics without mutation

- subnormal values;
- negative zero;
- singular candidate transform;
- extreme finite magnitude;
- inverted bounds;
- scale zero or negative;
- non-orthonormal rotation candidate.

The parser never normalizes vectors or matrices.

## 11. Adapter contract

A future non-Core adapter must declare:

- source raw axes;
- target axes;
- handedness change, if any;
- matrix vector convention (`M·v` or `v·M`);
- raw-to-4×4 expansion;
- transpose operations;
- translation units and scale;
- composition order of VXL, HVA and simulation transforms;
- proof fixtures and expected transformed basis points.

This explicit adapter is the only layer allowed to construct `UnityEngine.Matrix4x4`.
