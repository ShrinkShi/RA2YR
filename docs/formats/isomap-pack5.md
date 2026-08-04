# IsoMapPack5 raw record foundation

The current synthetic matrix contains 127 independent execution cases
(81 `[Test]` methods and 46 parameterized `[TestCase]` executions). This count
does not represent ProjectBaseline compatibility or original-runtime proof.

This document describes the M3-C2 Core boundary. It is not a claim that the
repository can load a complete RA2/YR map or that any coordinate or tile
interpretation is confirmed by the original runtime.

## Record layout

Each decoded record is exactly 11 bytes, little-endian where applicable:

| Offset | Size | Preserved view |
|---:|---:|---|
| 0 | 2 | `XRawU16LittleEndian` |
| 2 | 2 | `YRawU16LittleEndian` |
| 4 | 4 | `TileRawU32LittleEndian` |
| 4 | 2 | `TileLowU16LittleEndian` |
| 6 | 2 | `TileHighU16LittleEndian` |
| 8 | 1 | `SubTileRaw` |
| 9 | 1 | `LevelRaw` |
| 10 | 1 | `TailRaw` |

The reader also retains source ordinal, absolute source offset, a defensive
copy of all 11 bytes, and packed provenance. `TailRaw` has no IceGrowth or
other semantic name. No `ResolvedTileId`, `FinalTileIndex`, theater object, or
TMP reference is created.

## Trailing data

The default policy rejects every decoded remainder that is not a multiple of
11. `PreserveRemainderWithDiagnostic` retains opaque remainder bytes and their
absolute offset. `AllowExactFourZeroTrailer` accepts only exactly four zero
bytes; non-zero four-byte and five-byte zero remainders fail closed. The reader
does not silently truncate or try multiple policies.

## Coordinate analysis

`IsoMapCoordinateIndexer` uses the raw X/Y pair as an identity candidate while
preserving source order and every occurrence. Duplicate policies are explicit:

- `PreserveAllAndDiagnose`;
- `RejectAnyDuplicate`;
- `AllowByteIdenticalDuplicatesButDiagnose`.

Validation profiles explicitly carry axis order, signedness candidate, and
optional width/height. No automatic axis swap, winner selection, repair,
missing-cell synthesis, or dense-map assumption is applied.

## Packed adapter boundary

`IsoMapPack5PackedSectionReader` requires an explicit `RawLzo1X` policy and an
injected backend. It retains fragment, Base64, chunk, backend, decoded-stream,
record, and coordinate-stage results. Upstream failures stop record parsing.
The adapter never reads OverlayPack, PreviewPack, TMP, or ProjectBaseline data.

## Compatibility

- IsoMapPack5 record reader: Synthetic.
- Tile interpretation: Unresolved.
- Four-zero decoded trailer: ExplicitProfileOnly.
- Coordinate runtime semantics: NotConfirmed.
- Real packed ProjectBaseline decode: NotRun.
- LZO algorithm: NotImplemented.
- OverlayPack, PreviewPack, TMP, palette, rendering, writer: NotImplemented.
