# VXL normals, palette and lighting boundary

> Prepared by ChatGPT Web. Complete normal-vector tables are intentionally not reproduced.

## 1. Stored voxel fields

Every validated VXL voxel record contains two independent bytes:

```text
colorIndex  : u8
normalIndex : u8
```

Neither byte is a packed flag field in the examined implementations. The binary reader must preserve them unchanged.

## 2. Normal table selector

The final byte of each 92-byte VXL section tailer is commonly treated as a normal-table mode:

| Raw value | Strongest current interpretation | Candidate table size |
|---:|---|---:|
| `2` | Tiberian Sun normal table | 36 vectors |
| `4` | Red Alert 2 / Yuri's Revenge normal table | 244 vectors |
| other | unknown/unconfirmed mode | unknown |

OpenRA models the values directly as `TiberianSun = 2` and `RedAlert2 = 4`. Voxel Section Editor III records the same 36/244 sizes, and vengi writes 2 for a 36/TS palette and 4 otherwise.

The reader should expose a candidate enum without losing the raw byte:

```text
VxlNormalTableKind.TiberianSun36
VxlNormalTableKind.RedAlert2Yuri244
VxlNormalTableKind.Unknown(raw)
```

## 3. Normal vectors are not embedded in VXL

VXL embeds:

- a color palette;
- a remap range;
- a one-byte normal-table selector per section;
- one normal index per stored voxel.

It does **not** embed the 36 or 244 vector triples. Those vectors are engine/tool constants or external reference data.

The normal table is also not a PAL color palette and not a VPL lookup table.

## 4. Pinned table references without copying them

For future implementation verification, use pinned source files as reference-only evidence:

- OpenRA commit `a520984d91eda9de48a62b1d15c1e3bad0d4fb1a`
  - path: `OpenRA.Mods.Cnc/Traits/World/VoxelNormalsPalette.cs`
  - Git blob: `9767ac4ed6ca3b1ca7d6c1610a530f9088b9f660`
  - license: GPL-3.0-or-later, reference-only
  - declares TS and RA2 tables and converts them for a renderer palette.
- Voxel Section Editor III mirror commit `fde704b01cb4de3adeaf1a151bbeee0994a04b99`
  - path: `vxlseiii14x/source/constants/NormalsConstants.pas`
  - Git blob: `4a99084f819c834f6276785c5a73288368e2cc4f`
  - repository license not clearly located; reference-only
  - explicitly declares `TS_NORMAL_CNT = 36` and `RA2_NORMAL_CNT = 244`.

No implementation should mechanically translate either source.

## 5. Safe table-generation/verification strategy

A later implementation PR should either:

1. use independently documented constants with provenance and license approval; or
2. generate/import an approved data artifact outside GPL copying restrictions.

Whichever route is selected, record:

- table kind;
- vector count;
- canonical coordinate convention;
- serialization rule, e.g. each component encoded as IEEE-754 Float32 little-endian in table order;
- SHA-256 of that canonical serialization;
- maximum vector-length error from unit length;
- comparison result against at least two independent reference sources.

This research PR does not calculate or publish those complete-table hashes because that would require materializing the full tables.

## 6. Normal-index validation

For a confirmed table kind:

- TS mode: valid index candidate `0..35`;
- RA2/YR mode: valid index candidate `0..243`.

An out-of-range index must be retained in diagnostics and must not be:

- reduced modulo table length;
- clamped to the last vector;
- converted to zero;
- silently interpreted using the other table;
- replaced with a generated face normal.

For unknown normal modes, all indices remain raw and vector resolution is unresolved.

## 7. Color palette and remap are independent

The 802-byte VXL header contains 256 RGB triples. It also contains two remap-range bytes, commonly 16 and 31.

The format reader may report:

- color-index range observed in validated voxels;
- whether indices fall inside the declared remap range;
- raw embedded palette hash.

It must not:

- recolor remap entries;
- select player color;
- substitute a theater palette;
- turn color index zero into air/transparency;
- use color values to derive a normal.

Sparse occupancy comes from the span structure, not from a particular color index.

## 8. VPL and lighting

Community documentation describes VPL as the lookup controlling how voxel colors are lit for normal directions. That is a separate resource and later rendering concern.

Outside the VXL reader:

- ambient intensity;
- diffuse intensity;
- light direction;
- camera/view direction;
- VPL selection and lookup;
- palette conversion;
- player remap;
- shadow color and projection;
- gamma or post-processing.

A normal-resolution service can map `(tableKind, normalIndex)` to a vector after parsing. A lighting service can consume that vector later.

## 9. Coordinate convention conflict

OpenRA comments that Westwood voxel coordinates are `x=forward, y=right, z=up`. VSE and vengi perform explicit axis permutations when converting to OpenGL/scene-graph conventions.

These conversions do not change the raw normal index. The normal-vector table itself must have a declared coordinate convention, and any axis permutation belongs to an adapter, not the binary parser.

## 10. Diagnostics

Suggested codes:

- `UnknownNormalTableKind`
- `NormalIndexOutsideTable`
- `NormalTableUnavailable`
- `PaletteRemapRangeReversed`
- `PaletteCountUnexpected`
- `EmbeddedPaletteRetainedButRuntimeUseUnconfirmed`

None of these should cause the parser to fabricate geometry or lighting values.
