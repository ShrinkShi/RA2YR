# VXL sparse column and span encoding

> Prepared by ChatGPT Web from pinned public implementations. This document describes a validation candidate, not a frozen stock-runtime specification.

## 1. Coordinate storage unit

For a section with dimensions `(sizeX, sizeY, sizeZ)`, the section contains one sparse column directory entry for each X/Y pair:

```text
columnCount = sizeX × sizeY
columnIndex = x + sizeX × y
```

The public readers examined use this indexing order. Coordinate-system conversion performed by editors/renderers is outside the byte reader.

`sizeZ` is the logical length of each column. The section need not store `sizeX × sizeY × sizeZ` voxel records because empty Z ranges are represented by skip counts.

## 2. Three body-relative section offsets

The tailer gives:

- `spanStartOffset`: byte offset from the start of the whole VXL body;
- `spanEndOffset`: byte offset from the start of the whole VXL body;
- `spanDataOffset`: byte offset from the start of the whole VXL body.

At `spanStartOffset` are `columnCount` signed little-endian 32-bit entries.
At `spanEndOffset` are another `columnCount` signed little-endian 32-bit entries.
At `spanDataOffset` begins the section's variable span-data region.

The offsets are bytes, not element indexes.

## 3. Per-column directory entries

For column `i`:

- `start[i] == -1` denotes an empty column in XCC, OpenRA, VSE and vengi-derived behavior;
- a nonnegative start is a byte offset relative to the section's `spanDataOffset`;
- a nonnegative end is also relative to `spanDataOffset`;
- XCC/vengi writers use an **inclusive** end offset;
- candidate compressed length is therefore `end - start + 1`.

A robust reader must retain signed raw values. Reinterpreting `-1` as unsigned `0xffffffff` before sentinel handling can cause overflow and out-of-range seeks.

## 4. Empty-column consistency

Proposed strict classifications:

| Start | End | Classification |
|---|---|---|
| `-1` | `-1` | confirmed empty sentinel candidate |
| `-1` | other | inconsistent empty directory; fail |
| other | `-1` | inconsistent empty directory; fail |
| `< -1` | any | invalid negative offset; fail |
| nonnegative | nonnegative and end < start | reversed range; fail |
| nonnegative | nonnegative | validate inside section/body bounds |

Do not infer an empty column solely because `start == end` or because its first count is zero.

## 5. Span command structure

A nonempty column is a sequence of chunks:

```text
u8 skipCount
u8 voxelCount
repeat voxelCount times:
    u8 colorIndex
    u8 normalIndex
u8 duplicateVoxelCount
```

Logical decoding state begins at `z = 0`.

For each chunk:

1. advance `z` by `skipCount`;
2. read `voxelCount` records;
3. assign records to consecutive Z positions;
4. advance `z` by `voxelCount`;
5. read the duplicated trailing count byte.

The duplicate byte is written by XCC, vengi and VSE-family tools. Public readers often skip it without checking equality. Core should preserve and validate it rather than silently accepting mismatch.

## 6. Holes and multiple chunks

Multiple chunks are required to represent holes:

```text
skip=2, count=3, records...  -> voxels at z 2..4
skip=4, count=1, record...   -> voxel at z 9
```

The number of chunks is independent of the number of stored voxels. A dense column may use one chunk; an alternating sparse column may use many.

The decoder must budget both:

- total command/chunk count;
- total materialized voxel count.

## 7. Column termination

Sources use two overlapping termination signals:

1. logical Z reaches `sizeZ`;
2. input reaches the inclusive `end` entry.

Writers may emit a final zero-voxel chunk to represent trailing empty Z:

```text
skip = sizeZ - z
count = 0
duplicateCount = 0
```

This is not a sentinel independent of Z; it is a chunk that makes logical progress through its skip count.

Proposed strict contract:

- the input range is bounded by `start..end` inclusive;
- each chunk must fit entirely in that range;
- `z + skip + count` must not exceed `sizeZ`;
- the duplicate count must equal the leading count;
- decoding succeeds only when logical Z reaches exactly `sizeZ` and input is exactly exhausted;
- a chunk with `skip=0` and `count=0` before completion makes no progress and must fail;
- reaching `sizeZ` with unconsumed bytes is trailing column data, not padding;
- exhausting bytes before `sizeZ` is unterminated/underfilled column data.

Because some historical readers ignore end tables or duplicate-count equality, this exact dual-termination contract still requires golden validation before compatibility promotion.

## 8. Range ownership and overlap

The raw directory permits several malformed relationships:

- two columns with the same start/end range;
- partially overlapping ranges;
- one range nested inside another;
- ranges that point into either offset table;
- a column range that crosses into another section's tables/data;
- reversed or descending ranges;
- valid ranges listed in non-monotonic column order.

Non-monotonic order is not inherently invalid because directory order and data placement are separate. Overlap, however, must be diagnosed. Initial implementation should not infer deduplication, shared spans or references.

Recommended policy:

- exact duplicate nonempty ranges: preserve, report unresolved aliasing, do not decode into shared mutable storage;
- partial overlap: error;
- range into a directory table: error;
- cross-section ownership overlap: error unless later golden evidence establishes legal sharing;
- out-of-order but disjoint ranges: warning or accepted fact, preserving ordinal.

## 9. Actual voxel count

```text
storedVoxelCount = Σ voxelCount over validated chunks
boundsVolume = sizeX × sizeY × sizeZ
```

These are not expected to be equal. `storedVoxelCount` may be zero for an all-empty section and must never exceed the bounds volume.

A sparse representation should store only validated voxels. A dense `sizeX × sizeY × sizeZ` allocation must not be the parser's default because it creates avoidable memory amplification.

## 10. Color and normal records

Each stored voxel contributes exactly two bytes:

- `colorIndex`: independent palette/remap input;
- `normalIndex`: index into the selected normal table.

No bit packing is supported by the examined sources. The format layer retains both bytes unchanged. Validation of `normalIndex` depends on the section's normal-table selector; color interpretation is deferred to palette/remap/rendering.

## 11. Arithmetic and budgets

All calculations require checked arithmetic:

- `sizeX × sizeY`;
- table byte length `columnCount × 4`;
- body-relative plus table/data offsets;
- `end - start + 1`;
- `2 × voxelCount + 3` chunk size;
- cumulative chunks and voxels;
- cumulative sparse model allocation.

Suggested limits:

- maximum sections;
- maximum body bytes;
- maximum dimensions and bounds volume;
- maximum columns per section;
- maximum chunks per column and section;
- maximum stored voxels per section/document;
- maximum diagnostics and subranges.

## 12. Forbidden permissive behavior

Do not:

- ignore the end table;
- scan until a plausible next section;
- clamp `z` to `sizeZ`;
- truncate a voxel run;
- treat malformed negative offsets as empty;
- pad an underfilled column with implicit air;
- ignore mismatched duplicate counts;
- accept a no-progress loop;
- materialize an unbounded dense volume;
- resolve overlapping spans by last-write-wins.
