# TS/RA2/YR VXL file layout

> Research-only description prepared by ChatGPT Web. All multibyte fields are little-endian unless stated otherwise.

## 1. Canonical candidate layout

```text
+-------------------------------+
| VXL header              802 B |
+-------------------------------+
| section headers      N × 28 B |
+-------------------------------+
| shared body          bodySize |
+-------------------------------+
| section tailers      M × 92 B |
+-------------------------------+
| optional trailing bytes?      |
+-------------------------------+
```

The strongest sources use `N == M`, but a strict reader should first retain both raw counts and then diagnose mismatch. It must not index one array using the other count before validating budgets and ranges.

## 2. Header — 802 bytes

| Offset | Size | Raw field | Current interpretation |
|---:|---:|---|---|
| 0 | 16 | `FileTypeRaw` | NUL-padded identifier; confirmed family starts with `Voxel Animation` |
| 16 | 4 | `PaletteCountRaw` | commonly `1`; historical XCC calls it `one` |
| 20 | 4 | `SectionHeaderCountRaw` | count of 28-byte section headers |
| 24 | 4 | `SectionTailerCountRaw` | count of 92-byte section tailers |
| 28 | 4 | `BodySizeRaw` | declared shared body length in bytes |
| 32 | 1 | `StartPaletteRemapRaw` | commonly 16 |
| 33 | 1 | `EndPaletteRemapRaw` | commonly 31 |
| 34 | 768 | `PaletteRaw` | 256 RGB triples |

### Header conflict

XCC models bytes 32–33 as one packed signed 16-bit field and commonly validates it as `0x1f10`. Voxel Section Editor III and vengi model the same bytes as two remap-range indices, 16 and 31. The byte layout is not in conflict; only field naming is.

Core should preserve:

- both individual raw bytes;
- a raw 16-bit little-endian view for diagnostics;
- the candidate remap-range interpretation.

It should not call this field a version number.

### 802 versus 804

OpenRA, XCC, Voxel Section Editor III and vengi agree on 802 bytes. The examined `iron-curtain-engine/cnc-formats` implementation declares 804 and shifts palette parsing by two bytes after representing the remap bytes as two `u16` fields. That behavior is treated as a documented implementation defect, not an alternate VXL family.

## 3. Section header — 28 bytes

| Offset | Size | Raw field | Notes |
|---:|---:|---|---|
| 0 | 16 | `NameRaw` | fixed-width byte field, normally ASCII and NUL-padded |
| 16 | 4 | `SectionNumberRaw` | usually ordinal/index |
| 20 | 4 | `Unknown1Raw` | commonly `1` |
| 24 | 4 | `Unknown2Raw` | commonly `2` in VSE/vengi writers; some older names/comments call it zero |

Names must remain raw 16-byte values. A decoded ASCII candidate is useful, but invalid bytes, missing NUL and trailing bytes after NUL must not be discarded from the canonical raw model.

Duplicate names and duplicate section numbers are diagnostics. They are not parser-level reasons to drop records or pick a winner.

## 4. Shared body

The body begins immediately after all section headers:

```text
bodyStart = 802 + 28 × sectionHeaderCount
bodyEnd   = bodyStart + bodySize
```

Each tailer contains three offsets into this body. Those offsets locate the section's start-offset table, end-offset table and span-data base. Multiple sections may occupy disjoint subranges in the same declared body.

`bodySize` is a hard outer bound, not proof that all bytes are reachable or uniquely owned. A validation pass should classify:

- unreferenced body bytes;
- overlapping section regions;
- identical offsets;
- reversed table/data ordering;
- ranges extending outside the body;
- arithmetic overflow while calculating table sizes.

## 5. Section tailer — 92 bytes

| Offset | Size | Raw field | Candidate interpretation |
|---:|---:|---|---|
| 0 | 4 | `SpanStartOffsetRaw` | body-relative byte offset to `sizeX × sizeY` signed start entries |
| 4 | 4 | `SpanEndOffsetRaw` | body-relative byte offset to matching signed end entries |
| 8 | 4 | `SpanDataOffsetRaw` | body-relative byte offset to this section's span bytes |
| 12 | 4 | `ScaleRawBits` | little-endian IEEE-754 `float32`; called scale/det in different sources |
| 16 | 48 | `TransformRaw[12]` | raw 3×4 float record; interpretation deferred |
| 64 | 12 | `MinBoundsRaw[3]` | three float32 values |
| 76 | 12 | `MaxBoundsRaw[3]` | three float32 values |
| 88 | 1 | `SizeXRaw` | grid dimension |
| 89 | 1 | `SizeYRaw` | grid dimension |
| 90 | 1 | `SizeZRaw` | grid dimension / logical column height |
| 91 | 1 | `NormalTypeRaw` | commonly 2 or 4 |

The parser should preserve every float's raw `uint32` bit pattern in addition to a finite-value candidate. This allows deterministic hashes and distinguishes NaN payloads and signed zero.

## 6. Palette presence

The format does contain a 256-entry RGB palette. Whether stock rendering uses that palette directly is a separate runtime question. A reader must:

- retain all 768 bytes;
- not replace it with a theater palette;
- not apply remap;
- not convert color index zero to transparency as a format rule;
- avoid interpreting the normal index through this palette.

## 7. Empty file and empty section questions

### Zero sections

Public implementations disagree mostly by assumption rather than explicit evidence. A zero-section VXL can be structurally represented by an 802-byte header with zero body/tailers, but stock legality is unconfirmed. Proposed behavior:

- parse safely if counts and lengths are internally valid;
- emit `ZeroSectionCountUnconfirmed`;
- do not claim original compatibility.

### Empty section

A section can have zero dimensions or all `-1` columns. Their legality and equivalence are not confirmed. Preserve the descriptor and distinguish:

- dimension-empty section;
- nonzero dimensions with all columns empty;
- nonempty directory with zero stored voxels;
- malformed partial section.

## 8. Section order

The file order is confirmed evidence and must remain stable. No static source establishes that names may be freely reordered without affecting all consumers. Recommended model:

- section headers retain file ordinal;
- tailers retain file ordinal;
- candidate header/tailer pairing is ordinal only after count/range validation;
- HVA binding is a later operation using both names and ordinals;
- Art.ini body/turret/barrel composition does not live here.

## 9. Exact-size policy

XCC validates the total file size as:

```text
802 + 28 × headerCount + bodySize + 92 × tailerCount
```

Other readers may ignore trailing bytes. The initial Core reader should offer a strict default:

- declared structure beyond input: error;
- trailing bytes after complete structure: diagnostic and failure unless a golden exception is demonstrated;
- no automatic scanning for a second embedded object.
