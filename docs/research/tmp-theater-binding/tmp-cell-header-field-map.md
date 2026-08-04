# TMP cell header field map

## 1. Scope

This document records the strongest 52-byte TS/RA2/YR TMP cell-header candidate. It separates byte layout from semantic interpretation and retains every raw field even where public names agree.

## 2. Strongest byte layout

All integer fields are little-endian. The table is relative to the start of one non-empty cell record.

| Offset | Size | Raw type | Strong conventional name | Evidence and caution |
|---:|---:|---|---|---|
| `0x00` | 4 | `i32` | `XRaw` | screen/template-relative X candidate |
| `0x04` | 4 | `i32` | `YRaw` | screen/template-relative Y candidate |
| `0x08` | 4 | `i32/u32` | `ExtraColorOffsetRaw` | relative-to-cell-start candidate |
| `0x0C` | 4 | `i32/u32` | `DiamondDepthOffsetRaw` | XCC canonical assertion supports `52 + diamondBytes` |
| `0x10` | 4 | `i32/u32` | `ExtraDepthOffsetRaw` | relative-to-cell-start candidate |
| `0x14` | 4 | `i32` | `ExtraXRaw` | extra rectangle X candidate |
| `0x18` | 4 | `i32` | `ExtraYRaw` | extra rectangle Y candidate |
| `0x1C` | 4 | `i32/u32` | `ExtraWidthRaw` | signedness differs by implementation |
| `0x20` | 4 | `i32/u32` | `ExtraHeightRaw` | signedness differs by implementation |
| `0x24` | 4 | `u32` | `FlagsRaw` | preserve all 32 bits |
| `0x28` | 1 | `i8/u8` | `HeightRaw` | local cell-height candidate; signedness unresolved |
| `0x29` | 1 | `i8/u8` | `TerrainTypeRaw` | surface/pathing category candidate |
| `0x2A` | 1 | `i8/u8` | `RampTypeRaw` | slope topology candidate |
| `0x2B` | 1 | `u8` | `RadarLeftComponent0Raw` | commonly called red |
| `0x2C` | 1 | `u8` | `RadarLeftComponent1Raw` | commonly called green |
| `0x2D` | 1 | `u8` | `RadarLeftComponent2Raw` | commonly called blue |
| `0x2E` | 1 | `u8` | `RadarRightComponent0Raw` | commonly called red |
| `0x2F` | 1 | `u8` | `RadarRightComponent1Raw` | commonly called green |
| `0x30` | 1 | `u8` | `RadarRightComponent2Raw` | commonly called blue |
| `0x31` | 3 | `byte[3]` | `TrailingRaw` | reported as padding/trash by WAE; must be preserved |

Total: `0x34 = 52` bytes.

## 3. Why 52, not 48

Three independent lines converge on 52:

- XCC's packed `t_tmp_image_header` totals 52 bytes.
- OpenRA's detector reads `z_ofs` at offset 12 and expects `z_ofs == tileWidth × tileHeight / 2 + 52`.
- WAE's constructor actually consumes 52 metadata bytes before color data.

WAE defines `IMAGE_HEADER_SIZE = 48` only for a minimum-length precheck, then reads:

```text
36 bytes: nine 32-bit fields
 4 bytes: flags
 3 bytes: height/terrain/ramp
 6 bytes: two three-component radar colors
 3 bytes: trailing raw bytes
=52 bytes
```

The 48-byte constant is therefore an implementation defect, not a valid alternative header.

## 4. Offset signedness

XCC uses signed 32-bit fields; WAE exposes offset/size fields as unsigned in places; other readers mix both. The on-disk bytes do not change.

Recommended model:

```text
TmpCellHeaderRaw
- HeaderOffset
- XRawI32
- YRawI32
- ExtraColorOffsetBitsU32
- DiamondDepthOffsetBitsU32
- ExtraDepthOffsetBitsU32
- ExtraXRawI32
- ExtraYRawI32
- ExtraWidthBitsU32
- ExtraHeightBitsU32
- FlagsRawU32
- HeightRawU8
- TerrainTypeRawU8
- RampTypeRawU8
- RadarLeftRaw3
- RadarRightRaw3
- TrailingRaw3
```

Typed signed/unsigned views are derived without discarding the original bit patterns.

## 5. Offset base and canonical order

The strongest candidate is that the three plane offsets are relative to the start of the 52-byte cell header.

For a canonical sequential record:

```text
headerStart + 52
  diamond color, D bytes
headerStart + DiamondDepthOffsetRaw
  diamond depth, D bytes when present
headerStart + ExtraColorOffsetRaw
  extra color, E bytes when present
headerStart + ExtraDepthOffsetRaw
  extra depth, E bytes when present

D = checked(tileWidth × tileHeight / 2)
E = checked(extraWidth × extraHeight)
```

However, public readers differ:

- XCC exposes and asserts some stored offsets.
- OpenRA ignores stored plane offsets after reading metadata and consumes planes sequentially.
- WAE reads all planes sequentially and uses offset fields mostly as metadata/conditions.
- later defensive parsers may follow declared offsets.

The project should therefore produce both:

- a declared-offset directory;
- a canonical-sequential-layout comparison.

A mismatch is a diagnostic, not permission to silently choose whichever succeeds.

## 6. X/Y coordinate boundary

`XRaw` and `YRaw` are placement offsets inside a multi-cell template's rendered space. They are not map IsoMap coordinates.

The final render position can additionally depend on:

- the cell's index within the template grid;
- TMP `HeightRaw`;
- map `Level`;
- isometric projection conventions;
- extra rectangle coordinates.

The raw reader must not pre-apply these transformations.

## 7. Height, terrain, and ramp caution

The names at offsets `0x28..0x2A` appear in XCC-derived structures and modern editors. That is strong community/tool convention, but not original runtime source confirmation.

The raw reader may expose candidate typed views while preserving:

- exact byte value;
- evidence level;
- registry/profile used to interpret it;
- out-of-range state.

It must not clamp unknown ramp or terrain values.

## 8. Radar bytes

The six bytes are conventionally labelled two RGB triples for left/right radar halves. The reader retains component order and raw values.

Do not:

- replace them with average TMP colors;
- reinterpret them through the ISO palette;
- assume graphics-library BGR memory order changes file order;
- regenerate them during read.

## 9. Header validation

Defensive validation should check:

- cell header fits inside the bounded TMP window;
- multiplication and addition use checked arithmetic;
- extra dimensions fit configured limits;
- nonzero offsets resolve inside the same cell/file window;
- plane ranges do not overflow;
- declared ranges do not illegally overlap unless a later profile confirms aliasing;
- offset zero is interpreted only in the context of the relevant flag;
- unknown flag bits and trailing bytes are preserved and diagnosed, not rejected by default.

## 10. Evidence status

| Claim | Status |
|---|---|
| header is 52 bytes | `ConfirmedByMultipleIndependentImplementations` with shared-lineage caveat |
| listed byte offsets | `ConfirmedByMultipleImplementations` |
| offset base is cell start | strong candidate, `Underconfirmed` for all fields |
| bits 0..2 flag names | `ConfirmedCommunityConvention` / multiple implementations |
| height/terrain/ramp names | `ConfirmedCommunityConvention` |
| exact original runtime semantics | `Unresolved` |
| radar component order and use | `Underconfirmed` |
