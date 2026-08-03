# Westwood SHP(TS) indexed sprites

## Scope

M2-SHP1 implements the Unity-independent directory reader and local indexed
frame decoder for the TS/FS/RA2/YR SHP family. It covers the fixed header,
24-byte frame descriptors, raw flags 0 and 1, and strict flags 3 RLE-Zero.

It does not create a `Texture2D` or `Sprite`, bind a PAL, convert to RGBA,
apply player remap, pair shadows, infer pivots, select Art animations, write
SHP files, or implement VXL/HVA or gameplay behavior.

The C# implementation is independently authored for this Apache-2.0
repository. GPL sources were used only to compare observable layout and
behavior. No source, translation, or mechanical rewrite was imported.

## Evidence boundary

The formal implementation is based on the merged research dossier in
`docs/research/shp/`, cross-checked against:

- OmniBlade/xcc commit `62bb77080f13bdf65c79c84837b7cc264bdd432d`;
- XCC SourceForge SVN r1201;
- OpenRA commit `a520984d91eda9de48a62b1d15c1e3bad0d4fb1a`;
- Electronic Arts FinalSun/FinalAlert 2 source commit
  `6abf0f557469baea73079c6bf6550709e2e3584e`;
- ModdingWiki SHP(TS) oldid 10936 and RLE-Zero oldid 11565;
- `iron-curtain-engine/cnc-formats` commit
  `77da596ed72a1201740e054855bf2ff60640bfa9`, used only as an independent
  defensive-reading comparison where its behavior does not conflict with
  stronger sources;
- six fixed `YR1001_ProjectBaseline` entries reached through bounded MIX
  windows.

Exact source licenses and reference-only boundaries are recorded in
`docs/third-party/sources.yml`.

## Byte order and header

All multi-byte fields are little-endian. The file starts with an 8-byte
header:

```text
u16 familyMarkerRaw
u16 canvasWidthRaw
u16 canvasHeightRaw
u16 frameCountRaw
```

The strict reader requires `familyMarkerRaw == 0` and `frameCountRaw > 0`.
The directory size is calculated with checked arithmetic as:

```text
8 + frameCountRaw * 24
```

The reader does not reduce a declared count to fit available bytes. A
truncated header, truncated directory, count overflow, dimension budget
failure, or allocation budget failure returns a failed parse result without a
partial document.

## Frame descriptor

Each descriptor is exactly 24 bytes:

```text
u16 xRaw
u16 yRaw
u16 widthRaw
u16 heightRaw
u32 rawFlags
byte frameColorRaw[4]
u32 reservedRaw
u32 absoluteDataOffsetRaw
```

The model preserves descriptor order and every raw field. `FrameColorRaw`,
`ReservedRaw`, repeated offsets, and frame order are not interpreted as a
dependency or delta reference.

`absoluteDataOffsetRaw` is relative to the start of the current SHP content
window. The model also records the corresponding host-independent absolute
binary offset supplied by `BinarySourceContext`. A non-empty frame must not
point into the header or directory and must remain inside its parent input
window.

The directory has no explicit frame-data length. The next distinct data
offset is only a hard upper bound for one frame read and an overlap diagnostic
boundary. It is not treated as the amount of pixel data that must be consumed.
This permits alignment padding without exposing padding as pixels.

Non-zero `reservedRaw`, non-eight-byte-aligned offsets, duplicate offsets,
descending offsets, and coordinate high bits remain observable structured
diagnostics. They are not silently normalized.

## Coordinates and empty frames

The signedness of X and Y is not established. The authoritative fields are
`ushort` raw values. When the high bit is clear, the reader can validate the
local rectangle using non-negative arithmetic. When either high bit is set,
the document preserves the raw bits and emits
`CoordinateSignednessUnresolved`; it does not reinterpret them as negative
coordinates or perform canvas placement.

A canonical empty descriptor has:

```text
widthRaw == 0
heightRaw == 0
absoluteDataOffsetRaw == 0
```

Non-zero X/Y are retained and diagnosed. A partial empty descriptor is a
controlled parse failure. Empty frames decode to immutable zero-area local
frames and are never treated as references.

## Compression dispatch

The complete 32-bit `rawFlags` value remains authoritative.

| Raw value | Derived kind | M2-SHP1 decode status |
|---:|---|---|
| 0 | `RawOpaque` | implemented |
| 1 | `RawTransparent` | implemented |
| 2 | `SourceConflictingFlags2` | directory only; controlled unresolved decode |
| 3 | `RleZeroTransparent` | strict synthetic decode implemented; ProjectBaseline conflict recorded |
| >=4 | `UnknownFlags` | directory only; controlled unresolved decode |

Flags 0 and 1 both consume exactly `widthRaw * heightRaw` bytes in row-major
order. The pixel byte is retained unchanged, including index 0. Transparency
is a later rendering policy and does not rewrite the indexed frame.

Flags 2 remains source-conflicting: XCC enters the RLE-Zero path using the
RLE bit, while OpenRA treats value 2 as length-prefixed raw scanlines. M2-SHP1
does not select either interpretation.

## Strict flags 3 RLE-Zero

Each row begins with a little-endian length that includes its two-byte header:

```text
u16 lineByteLengthIncludingHeader
byte commands[lineByteLengthIncludingHeader - 2]
```

Commands are:

- a non-zero byte emits one literal palette index;
- `00 count` emits `count` zero indices.

For every row the decoder requires:

- line length at least 2;
- the complete declared row inside the frame and parent windows;
- every command to consume input and count against row and frame budgets;
- no dangling `00` byte;
- no output past the declared local width;
- declared command input fully consumed;
- output exactly equal to the declared local width;
- exactly the declared number of rows.

The decoder does not pad, truncate, clamp a zero run, borrow bytes from the
next frame, or return a partial indexed frame.

`00 00` is known to consume two input bytes and emit no pixels, but its stock
format meaning remains unresolved. The default decoder consumes it for
progress accounting and returns
`ZeroOutputCommandSemanticsUnresolved`; it does not call it a no-op or a
corrupt command.

## ProjectBaseline conflict

The fixed local audit parsed all six directories and decoded all raw frames.
It encountered 257 non-empty flags 3 frames. Every one failed on row 0 with
`RleOutputOverflow`, and every attempted zero run would have produced exactly
one index more than `widthRaw`. The set includes 120 even widths and 137 odd
widths, covering widths 14 through 202.

This is consistent with XCC-style tolerant clamping behavior but conflicts
with the explicit M2-SHP1 strict requirement that each row output exactly the
declared local width. The parser and decoder were not relaxed to accept the
baseline. Therefore:

- SHP(TS) directory parsing is ProjectBaseline-validated;
- raw flags 0/1 decoding is ProjectBaseline-validated;
- strict flags 3 decoding is synthetic-tested but not promoted as
  ProjectBaseline-compatible;
- the audit completes with structured decode failures instead of presenting a
  false all-green result.

All failures occurred on row 0, so later commands in those frames were not
examined. The public `00 00` count of zero means none was reached before strict
termination; it is not proof that all compressed rows lack `00 00`.

## Immutable model and canonical hashes

`ShpTsDocument` contains an immutable `ShpTsHeader`, ordered immutable
`ShpTsFrameDescriptor` values, logical provenance, input length, and absolute
offset origin. `ShpTsIndexedLocalFrame` contains only a local indexed rectangle
and returns copies rather than mutable backing storage.

The directory and decoded-document model hashes use domain-separated,
versioned canonical binary encodings. They include ordered raw descriptors or
ordered decoded local frames respectively. They exclude absolute host paths,
timestamps, external file names, renderer state, and PAL selection. The full
canonical bytes and per-frame index buffers are not published.

## Input and budget boundary

Memory, seekable stream, short-read stream, and bounded MIX-entry window paths
share the same strict parser and decoder semantics. Parent windows are never
escaped. Directory parsing reads only the header and descriptors. Frame access
reads only the descriptor's bounded data region.

`ShpTsReadLimits` independently bounds input bytes, frame count, canvas and
local area, cumulative decoded pixels, per-read bytes, per-row bytes,
per-frame compressed bytes, commands, allocations, descriptors/subwindows,
and diagnostics. All file-driven allocations and checked arithmetic occur
before allocation or copying.

## Privacy and content handling

ProjectBaseline files remain outside the repository and are never copied into
`Assets`. Public evidence contains only logical roles, selection-basis labels,
MIX IDs and logical provenance, lengths, SHA-256 values, aggregate geometry and
flags, irreversible model hashes, and diagnostic counts.

Complete indexed frames, original pixels, images, scanline/run details,
Base64, hex dumps, absolute paths, and per-frame hashes remain absent from the
repository. A complete per-frame audit manifest is written only to the
configured repository-external cache.
