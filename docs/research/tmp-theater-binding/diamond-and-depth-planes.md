# Diamond and depth planes

## 1. Plane categories

A TMP cell can expose up to four byte planes:

1. base diamond color;
2. base diamond depth/Z;
3. extra rectangular color;
4. extra rectangular depth/Z.

They share byte-sized samples but have different geometry and consumers.

## 2. Base diamond encoded length

For tile pixel dimensions `W × H`, the strongest formula is:

```text
diamondByteCount = checked(W × H / 2)
```

Canonical TS/RA2 dimensions observed by public tools are:

- TS-family `48 × 24`;
- RA2/YR-family `60 × 30`.

The reader should not hard-code only those two values at the binary layer, but should require:

- positive dimensions;
- `W == 2 × H` for the canonical diamond profile;
- checked multiplication;
- configured maximum dimensions and area;
- a scanline schedule whose sum equals the derived byte count.

## 3. Scanline schedule

The encoded diamond consists only of visible diamond samples, not a dense rectangular raster.

Canonical row widths:

```text
start width = 4
for top half:    width += 4 each row
for bottom half: width -= 4 each row
```

For `60 × 30`, row widths are:

```text
4, 8, 12, ... 60, 56, ... 4
```

Their sum is `60 × 30 / 2 = 900` bytes.

A decoder may expand this into a dense `W × H` buffer for an adapter, but the Core raw model should preserve:

- encoded plane window;
- row directory or row-length formula;
- encoded byte count;
- dense expansion as an optional derived view.

## 4. Diamond color

Each byte is a palette index. The TMP file does not embed the palette.

The parser does not:

- resolve RGB;
- apply transparency policy;
- choose an ISO palette;
- merge extra graphics;
- generate textures.

Those operations require a theater palette binding and renderer adapter.

## 5. Diamond depth/Z

A present diamond depth plane uses the same encoded row schedule and expected byte count as the color diamond.

Strong canonical relation:

```text
DiamondDepthOffsetRaw == 52 + diamondByteCount
```

XCC asserts this relationship for its normal layout. It should be verified rather than assumed for every file.

Depth samples are renderer ordering information. They are not:

- map `Level`;
- TMP cell `HeightRaw`;
- ramp corner elevation;
- movement/pathfinding height;
- alpha.

No semantic adapter may derive navigable geometry directly from the depth bytes.

## 6. Extra rectangular planes

For valid positive dimensions:

```text
extraByteCount = checked(extraWidth × extraHeight)
```

Extra color and extra depth are row-major rectangular candidates.

They can extend above, below, or to either side of the base diamond. This supports visual features such as cliff faces, but extra graphics are generic and must not be classified as cliffs solely by their presence.

## 7. Plane directory model

Recommended structure:

```text
TmpCellPlaneDirectory
- HeaderWindow
- DiamondColorWindow
- DiamondDepthWindowCandidate
- ExtraColorWindowCandidate
- ExtraDepthWindowCandidate
- DeclaredOffsetRelations
- CanonicalSequentialRelations
- OverlapClassifications
- TrailingWindow
```

Each window records:

- relative and absolute bounded offset;
- expected length;
- actual available length;
- flag provenance;
- exact/underflow/overflow status;
- whether it agrees with canonical sequential order.

## 8. Canonical sequential candidates

Let:

```text
B = cellHeaderStart
D = diamondByteCount
E = extraByteCount
```

One common sequential layout is:

```text
base color: B + 52, length D
base depth: B + 52 + D, length D
extra color: next, length E
extra depth: next, length E
```

But flag-gated readers can produce variants where absent depth changes extra-color position. Stored offsets are therefore necessary evidence.

The parser should compare, not overwrite:

- offset-derived layout;
- sequential-with-Z layout;
- sequential-without-Z layout.

## 9. Range and overlap policy

Default defensive policy:

- no plane may start inside the 52-byte header;
- every present plane must fit the bounded TMP entry/file window;
- multiplication/addition is checked;
- plane overlaps are diagnostics and strict failures unless an evidence-gated alias policy exists;
- duplicate cell offsets do not imply plane aliasing is safe;
- trailing bytes are retained and reported;
- no truncated plane is padded;
- no oversized plane is clipped.

## 10. Transparency and compositing

Public tools commonly treat palette index zero in extra graphics as transparent. That behavior belongs to a rendering profile.

Core retains every color index. It does not erase zeroes or precompose extra data over the diamond.

Likewise, public tools may suppress out-of-range depth values. Core preserves them and emits diagnostics.

## 11. Input equivalence

The same plane directory and diagnostics must result from:

- memory input;
- seekable stream;
- short-read stream;
- exact MIX-entry window.

No input path may load the whole enclosing MIX or allow a plane to escape its logical entry window.

## 12. Evidence status

| Claim | Status |
|---|---|
| diamond length `W×H/2` | `ConfirmedByMultipleImplementations` |
| row widths change by four | `ConfirmedByMultipleImplementations` |
| depth diamond uses same length | strong multiple-implementation candidate |
| extra length `width×height` | `ConfirmedByMultipleImplementations` |
| all files place planes sequentially | `ConflictingSources` / `Underconfirmed` |
| stored offsets are authoritative | strongest design candidate, runtime behavior unresolved |
| depth values encode movement height | rejected boundary |
