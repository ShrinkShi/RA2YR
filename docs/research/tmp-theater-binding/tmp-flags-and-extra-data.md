# TMP flags and extra-data boundaries

## 1. Raw flag word

The TMP cell header stores a 32-bit little-endian flag word at offset `0x24`.

Strongly conventional known bits:

| Mask | Conventional name | Minimum structural implication |
|---:|---|---|
| `0x00000001` | `HasExtraData` | extra rectangle metadata and extra color candidate are relevant |
| `0x00000002` | `HasZData` | diamond depth candidate is relevant; extra depth may also be relevant |
| `0x00000004` | `HasDamagedData` | damaged-state metadata/asset semantics may exist, but body layout is unresolved |

Recommended representation:

```text
FlagsRawU32
KnownFlags = FlagsRawU32 & 0x00000007
UnknownFlags = FlagsRawU32 & ~0x00000007
```

The raw word is always retained.

## 2. Uninitialized and unknown bits

WAE states that Westwood TMP flags and trailing bytes can contain uninitialized memory. This observation means a strict reader should not require `UnknownFlags == 0` as a universal validity rule.

Instead:

- parse all 32 bits;
- expose known bits;
- count and diagnose unknown bits;
- avoid using unknown bits to select additional plane layouts;
- preserve the original value for lossless or forensic models;
- defer rejection to a named policy if later evidence identifies forbidden bits.

This is not permission to ignore the flag word. Known bits still control structural expectations.

## 3. Extra rectangle metadata

When `HasExtraData` is set, the fields at offsets `0x14..0x23` are candidates for:

```text
ExtraXRaw      i32
ExtraYRaw      i32
ExtraWidthRaw  i32/u32
ExtraHeightRaw i32/u32
```

The expected extra color length is:

```text
extraByteCount = checked(extraWidth × extraHeight)
```

Defensive requirements:

- dimensions must be positive for a present extra plane;
- dimensions must remain below per-axis and area budgets;
- coordinate arithmetic must be checked;
- the resulting rectangle may extend outside the base diamond bounds;
- coordinate values are retained even if no rendering adapter currently supports them;
- the reader must not crop an extra rectangle to the base tile.

## 4. Extra color and extra depth

Strong candidate structure:

- extra color: `extraWidth × extraHeight` palette-index bytes;
- extra depth: the same number of bytes when present.

OpenRA treats zero color indices in extra graphics as transparent during composition. That is a renderer behavior. The raw plane still contains every byte.

OpenRA also ignores extra depth samples greater than or equal to 32 while composing its depth frame. That is permissive rendering behavior and must not be imported into the Core parser. Core preserves the byte and reports range aggregates later.

## 5. Offset-driven versus sequential readers

Public readers diverge:

### Offset-driven candidate

Use:

- `ExtraColorOffsetRaw`;
- `DiamondDepthOffsetRaw`;
- `ExtraDepthOffsetRaw`;

to define explicit bounded plane windows relative to the cell header.

### Sequential candidate

Consume color/depth/extra planes immediately after the 52-byte header in a fixed order based on flags.

The recommended parser uses declared offsets as the primary structural view and computes a canonical sequential comparison. It does not silently switch strategies when one fails.

Diagnostics should distinguish:

- declared offset absent;
- declared offset outside cell/file;
- declared offset overlaps metadata;
- declared planes overlap each other;
- declared offset differs from canonical sequential position;
- sequential bytes exist but flag is clear;
- flag is set but declared plane cannot fit.

## 6. HasZData ambiguity

The reviewed sources show conflicting consumption behavior:

- WAE reads diamond depth only when bit `0x02` is set.
- OpenRA reads a diamond depth plane unconditionally after the color plane.
- XCC's `get_z_image` exposes `z_ofs` and asserts its canonical position, but consumer behavior varies.

Therefore the project must distinguish:

```text
FlagDeclaredZ
DeclaredZOffsetPresent
CanonicalSequentialZFits
ObservedPlaneCandidate
```

A production default cannot be frozen solely because one reader displays known assets.

## 7. HasDamagedData ambiguity

The 52-byte header has no obvious second set of damaged-plane offsets. Public tool discussions and XCC naming expose the bit, but the reviewed material does not establish a complete body layout or how RA2/YR runtime consumes it.

Current policy:

- preserve the bit;
- do not synthesize damaged graphics;
- do not assume the extra plane is the damaged plane;
- do not skip unknown trailing bytes because the bit is set;
- classify samples and seek independent public evidence before implementing a damaged-data view.

## 8. Empty or inconsistent combinations

Strict structural diagnostics should cover:

- `HasExtraData` with zero width or height;
- extra dimensions without `HasExtraData`;
- nonzero extra offset without `HasExtraData`;
- `HasZData` with zero/out-of-range depth offset;
- extra depth offset without both relevant structural conditions;
- `HasDamagedData` without known representation;
- unknown flags with otherwise valid planes;
- offset aliasing between color, depth, and extra planes.

Unknown semantics are not the same as malformed bytes. The parser can return a raw success plus semantic diagnostics where all ranges are safe.

## 9. Writer boundary

A future writer must not normalize unknown flag bits or trailing bytes by default. Writer modes must be explicit:

- `PreserveRawHeader`
- `CanonicalizeKnownFields`
- `CreateNewAsset`

Canonicalization requires stronger evidence than read support and is outside this research PR.

## 10. Evidence status

| Claim | Grade | Notes |
|---|---|---|
| bit 0 means extra data | `ConfirmedByMultipleIndependentImplementations` | XCC and OpenRA independently expose compatible extra-data behavior; other XCC-derived tools are corroborating only. |
| bit 1 means Z data | `ConflictingSources` | The label is conventional, but WAE gates the plane on the bit while OpenRA consumes diamond depth unconditionally. |
| bit 2 means damaged data | `ConfirmedCommunityConvention` | The name is widespread; the damaged-data body and runtime behavior remain unresolved. |
| high bits may be trash | `ImplementationSpecificBehavior` | WAE reports this behavior; it is not a universal runtime rule. |
| extra plane length is width×height | `ConfirmedByMultipleIndependentImplementations` | Independent readers agree on the rectangle-area length. |
| extra depth values are limited to 0..31 | `ImplementationSpecificBehavior` | OpenRA renderer filtering only; not raw-format validity. |
