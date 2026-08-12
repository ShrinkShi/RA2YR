# TMP and theater registry foundation

M3-C6 adds a bounded, Unity-free raw foundation for TMP assets and theater
TileSet control data. It is deliberately not a renderer or terrain model.

## TMP raw reader

`TmpRawReader` accepts memory, bounded streams, and bounded data windows through
the existing input abstractions. It reads a 16-byte file header, a checked
cell-offset table, and exactly 52 bytes for each non-empty cell header. The raw
model preserves all fields, including flags, Height/Terrain/Ramp candidates,
radar component bytes, trailing header bytes, and declared plane offsets.

Plane windows are created under one explicit policy:

- `DeclaredOffsets` uses raw offsets relative to the cell header;
- `SequentialWithZ` uses canonical sequential diamond/depth/extra ordering;
- `SequentialWithoutZ` omits depth from the sequential candidate.

The reader does not select whichever policy happens to parse, pad truncated
planes, crop overlaps, or attach semantic palette/terrain meaning. Canonical
diamond length is checked as `tileWidth * tileHeight / 2`; extra area is
checked as `extraWidth * extraHeight`. All allocations and diagnostics are
bounded, and raw byte getters return defensive copies.

## Theater registry and asset candidates

Six explicit profiles are available: Temperate, Snow, Urban, NewUrban, Desert,
and Lunar. The control reader consumes an already-composed lossless INI
resolution and retains value provenance. Numeric `[TileSet####]` sections are
ordered deterministically; gaps are diagnosed and retained, while duplicate
normalized indices are rejected instead of choosing a winner. Checked
`TilesInSet` values allocate cumulative GlobalTileId ranges beginning at zero.
Missing assets never compress or shift those ranges.

Asset resolution uses the registry's `FileName`, a one-based two-digit local
ordinal, the selected theater extension, and explicit variation/fallback
policies. NewUrban's `.urb` editor candidate is opt-in only. Provider case
collisions and ambiguous candidates fail closed with a trace; no host path is
stored in the model.

## Compatibility boundary

TMP structure and theater registry behavior are synthetic/configured. Channel,
palette, row, height, terrain, ramp, cliff, water, bridge, LAT, passability,
and original-runtime semantics remain unresolved or unimplemented. The
ProjectBaseline audit is read-only and aggregate-only; its current configured
run is `CompleteWithFailures` with zero named TMP candidates and one mount-level
failure, so it is not a compatibility claim.
