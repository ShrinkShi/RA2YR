# OverlayPack and OverlayDataPack raw packed-array foundation

This is the M3-C3 Core boundary. It is a bounded raw-byte adapter, not an
Overlay semantic reader or renderer.

## Pipeline

```text
lossless section occurrences
  -> explicit OverlayPack or OverlayDataPack selection
  -> M3-C1 packed fragment/Base64/chunk/Format80 pipeline
  -> exact decoded byte array
  -> raw storage view and explicit coordinate index
```

The two sections are decoded independently. Missing, present-empty, selected,
and ambiguous section occurrences are represented explicitly. A selected
section must provide its own fragment occurrences; the adapter never combines
ambiguous candidates or applies a last-wins rule.

## Implemented profile

The only supported storage profile is `OrdinaryByte512`: exactly
`512 * 512 * 1 = 262144` bytes. Declared aggregate output and actual output
must both satisfy that length. The adapter does not pad, truncate, repair, or
use a partner section to fill a failed child.

The packed policy must explicitly select absolute Format80, a required
terminator, no initial marker, and no trailing compressed bytes. Relative
profiles and `RawLzo1X` are rejected at the adapter boundary; the latter has
no algorithm implementation in this repository.

## Raw and indexed views

`OverlayArrayRaw` stores a defensive copy and exposes individual bytes or a
fresh copy. `OverlayRawIndexedView` provides two explicit candidate formulas:

- external row-major: `X + 512 * Y`;
- official-editor transposed comparison: `Y + 512 * X`.

The view is deliberately byte-oriented. `0xFF` is not interpreted as an empty
cell, resource, overlay type, or any other semantic value. No Rules/Art lookup,
theater registry, palette, texture, sprite, renderer, or gameplay object is
created.

## Failure and provenance contract

All child packed results and diagnostics are preserved. Any fatal child state
prevents later raw-array construction and sets the parent execution state
before diagnostic-list admission. Suppressed diagnostics are counted with
saturating arithmetic, so a zero diagnostic budget cannot become success by
omission. Source and INI provenance are retained through the packed result.

## Compatibility boundary

- OverlayPack raw array: Synthetic/configured only.
- OverlayDataPack raw array: Synthetic/configured only.
- Storage index formulas: Explicit candidate profiles only.
- `0xFF` and overlay type meaning: Unresolved.
- ProjectBaseline packed decode: Not run.
- LZO algorithm: Not implemented.
- PreviewPack, TMP, theater, palette, rendering, writer, pathfinding, and
  gameplay: Not implemented.

The focused M3-C3 suite contains 51 NUnit executions (37 `[Test]` methods and
14 `[TestCase]` executions across 41 behavior methods). This count describes
the focused contract suite; full-suite results are recorded only after the
current final tree is executed.
