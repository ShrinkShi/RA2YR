# HVA file layout

> Prepared by ChatGPT Web from pinned public sources. The matrix-record ordering conflict remains intentionally unresolved.

## 1. Candidate binary layout

```text
+----------------------------------+
| raw filename/label          16 B |
| frameCount                 u32le |
| sectionCount               u32le |
+----------------------------------+
| section names       count × 16 B |
+----------------------------------+
| transforms      frames × sections|
|                     × 48 B each  |
+----------------------------------+
```

The fixed header is 24 bytes. The exact declared payload size is:

```text
24 + 16 × sectionCount + 48 × frameCount × sectionCount
```

XCC validates this as the exact file length. Other implementations only require sufficient data. Initial Core design should reject truncation and retain trailing-data diagnostics rather than silently accepting extension bytes.

## 2. First 16 bytes are not a strong magic

Different writers use a source filename, `NONE`, an empty string or another label. Therefore the field is:

```text
HvaHeader.FileNameRaw : byte[16]
```

It is not safely validated against a fixed `HVA Animation` signature.

A decoded ASCII candidate may be exposed, but the raw bytes and first-NUL position remain canonical evidence.

## 3. Counts

`frameCount` and `sectionCount` are unsigned 32-bit little-endian fields in the strongest sources.

Public tools commonly reject zero frames and zero sections, but an empty structural document can still be represented. Proposed behavior:

- apply explicit count and multiplication budgets before allocation;
- parse zero-count files safely when exact length permits;
- report legality as unconfirmed;
- do not invent an identity frame or section.

## 4. Section names

Each HVA section name is exactly 16 raw bytes, normally ASCII and NUL-padded.

The parser retains:

- raw bytes;
- ordinal;
- decoded candidate up to first NUL;
- whether a NUL exists;
- whether nonzero trailing bytes follow a NUL;
- ASCII validity and duplicate-name diagnostics.

Names are not consumed or discarded merely because a VXL file has not yet been supplied. HVA is independently readable.

## 5. Raw transform record

Each record is 48 bytes:

```text
12 × IEEE-754 float32 little-endian
```

The common structural view is three rows by four columns:

```text
m00 m01 m02 m03
m10 m11 m12 m13
m20 m21 m22 m23
```

Community descriptions commonly place translation in the fourth value of each row (`m03`, `m13`, `m23`). The Core model should preserve all twelve raw bit patterns and expose a candidate 3×4 view without expanding it to a particular engine matrix type.

## 6. Record-order conflict

For `frameCount > 1` and `sectionCount > 1`, two incompatible flattening formulas exist in pinned implementations.

### Frame-major candidate

```text
index = frame × sectionCount + section
```

Supported by:

- OpenRA's nested frame/limb read loops;
- Voxel Section Editor III indexing;
- vengi read/write loops;
- `iron-curtain-engine/cnc-formats` documentation and accessor.

### Section-major candidate

```text
index = section × frameCount + frame
```

Supported by XCC's accessor and CSV writer.

Files with one frame or one section cannot distinguish the formulas. A synthetic 2-frame/2-section fixture with unique values in every record is mandatory. Public static evidence favors frame-major numerically, but source lineage and tool conversions mean the production default remains gated on local golden evidence.

## 7. Matrix layout is not runtime storage

Several sources describe the file as row-major 3×4. OpenRA immediately transposes values into a column-major 4×4 array. VSE permutes axes for OpenGL. vengi writes into GLM-style column vectors and performs conversion helpers.

Those are adapter choices. Core stores:

```text
HvaRawTransform3x4
- RawBits[12]
- FiniteValues[12] candidate
- FileRecordOrdinal
- CandidateFrameIndex
- CandidateSectionIndex
```

No `UnityEngine.Matrix4x4` and no implicit transpose.

## 8. Float validation

Every component should be classified using its raw bits:

- finite normal value;
- positive/negative zero;
- subnormal;
- positive/negative infinity;
- NaN with preserved payload.

NaN and Infinity are invalid for transform interpretation. They should produce a structured failure rather than propagating into rendering or hashes.

Very large but finite values require explicit policy. The parser can retain them while issuing a range diagnostic. It should not clamp.

A singular matrix is not necessarily a byte-level parse error. OpenRA rejects non-invertible expanded matrices, but that is a semantic/runtime constraint not independently confirmed as an HVA file rule. Keep singularity as a later validation diagnostic until golden evidence establishes stock behavior.

## 9. Frame semantics

The format stores an ordered sequence. It does not itself define:

- frames per second;
- interpolation method;
- which game state selects a frame;
- loop mode;
- locomotion or firing timing;
- composition with body/turret/barrel facing.

A one-frame HVA is a valid static-transform candidate. A multi-frame HVA is transform animation data, not simulation logic.

## 10. Strict length and overflow checks

Use checked arithmetic for:

- `sectionCount × 16`;
- `frameCount × sectionCount`;
- transform count × 48;
- header plus names plus transforms;
- model allocation estimates.

Suggested limits:

- maximum frames;
- maximum sections;
- maximum transforms;
- maximum total input bytes;
- maximum allocated bytes;
- maximum diagnostics.

No loop may be driven by unvalidated raw counts.
