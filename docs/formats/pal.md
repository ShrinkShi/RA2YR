# Westwood PAL palettes

## Scope

WP-02D parses the raw palette payload used by the controlled Yuri's Revenge
content baseline. It does not select palettes for assets, render colors,
perform player-color remapping, or establish original-game visual parity.

The implementation is independently authored for this Apache-2.0 repository.
XCC and OpenRA source is GPL-licensed and was used only to observe format and
conversion behavior. No source, translation, or mechanical rewrite was
imported.

## Evidence

The format conclusions are cross-checked against:

- `isotem.pal`, `temperat.pal`, and `unittem.pal` reached through the bounded
  `YR1001_ProjectBaseline` chain `ra2.mix -> cache.mix -> entry`;
- XCC SourceForge SVN r1201 and OmniBlade/xcc commit
  `62bb77080f13bdf65c79c84837b7cc264bdd432d`;
- OpenRA commit `a520984d91eda9de48a62b1d15c1e3bad0d4fb1a`;
- the independently implemented `iron-curtain-engine/cnc-formats` commit
  `77da596ed72a1201740e054855bf2ff60640bfa9`.

Exact licenses and use boundaries are recorded in
`docs/third-party/sources.yml`.

## Raw layout

A standard Westwood PAL is exactly 768 bytes with no header, footer, magic, or
embedded role information:

```text
256 entries * 3 bytes = 768 bytes

entry 0: red, green, blue
entry 1: red, green, blue
...
entry 255: red, green, blue
```

Each channel is an unsigned raw value in the inclusive range 0 through 63.
The array position is the palette index. Duplicate colors are legal and remain
distinct indexed entries.

XCC rejects a file whose size differs from 768 or whose channel has either of
the upper two bits set. The three controlled ProjectBaseline samples are each
768 bytes and contain no channel above 63. OpenRA's loader is less strict: it
reads 256 triples but does not independently reject trailing bytes or invalid
channels. WP-02D deliberately fails closed and does not copy that permissive
behavior. Original YR behavior for corrupt channels remains unproven.

The parser therefore rejects truncation, trailing bytes, out-of-range
channels, binary read failures, arithmetic failures, and budget failures. It
never pads, truncates, clamps, repeats colors, or exposes a partial palette.

## Display conversion strategies

Raw six-bit values remain authoritative. Display conversion is a separate,
explicit operation over a validated raw channel.

| Strategy identifier | Integer mapping | Range | Evidence boundary |
|---|---|---:|---|
| `ShiftLeftTwo` | `v << 2` | 0-252 | VGA-style and independent implementation observation |
| `ReplicateHighBits` | `(v << 2) \| (v >> 4)` | 0-255 | Pinned OpenRA behavior |
| `ScaleToFullRangeRounded` | `(v * 255 + 31) / 63` | 0-255 | Explicit nearest-integer mathematical strategy |
| `XccScaleToFullRangeFloor` | `(v * 255) / 63` | 0-255 | Pinned XCC behavior; integer division rounds down |

The XCC conversion is not the same as nearest rounding. The observable sources
also disagree with each other, and no controlled original YR visual capture
has established which modern 8-bit expansion reproduces the final original
result. Consequently the Core API has no unlabeled or `OriginalYR` default.
The local audit records `XccScaleToFullRangeFloor` only as its named XCC
reference strategy, not as a claim about original rendering.

## Immutable model and fingerprint

`WestwoodPalette` retains all 256 validated `PaletteColorRaw` entries in file
order. Display conversion returns separate values and cannot mutate or replace
the raw model.

The normalized model fingerprint schema is `ra2yr.pal.raw-model.v1`:

```text
ASCII "RA2YR.PAL.RAW.V1\0"
UInt32LE color count (256)
for each index 0..255:
    UInt16LE index
    UInt8 red
    UInt8 green
    UInt8 blue
```

SHA-256 is calculated over that canonical sequence. Source paths, provenance,
display strategy, host state, and absolute paths are excluded. The fingerprint
is a non-reversible comparison value; the canonical bytes and complete color
table are not published.

## Content-source boundary

The PAL reader interprets bytes only. It does not infer that `isotem.pal`,
`temperat.pal`, or `unittem.pal` have interchangeable roles. A controlled
ProjectBaseline validation service mounts `ra2.mix`, resolves `cache.mix`, and
requires each target's fixed ID, unique chain, length, and SHA-256 before
passing its bounded entry window to the parser.

If any identity changes, the golden validation fails without promoting the
replacement. The authoritative content tree is never modified or extracted
into `Assets`.
