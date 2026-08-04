# IsoMapPack5

## 1. Role

`IsoMapPack5` stores the map's placed isometric terrain cells. It selects tile/template identity, subtile and cell elevation; it does not embed TMP pixels.

The strong family model is:

```text
numbered INI fragments
→ Base64 bytes
→ repeated LZO blocks
→ decompressed 11-byte records
→ four trailing zero bytes / terminal-coordinate candidate
```

## 2. Expected record count candidate

OpenRA and community documentation calculate the full isometric record count as:

```text
((mapWidth × 2) - 1) × mapHeight
```

and candidate decompressed size as:

```text
recordCount × 11 + 4
```

Modern editors may omit clear level-zero cells when writing and rely on implicit clear terrain. Therefore exact dense record count versus sparse-record acceptance is a format/runtime conflict, not a safe universal assertion.

The Core model should support an ordered record sequence plus a later canvas validation pass.

## 3. Eleven-byte record

The common byte shape is:

| Offset | Size | Raw candidate |
|---:|---:|---|
| 0 | 2 | X / RX, signedness disputed |
| 2 | 2 | Y / RY, signedness disputed |
| 4 | 4 | tile identity field(s), interpretation disputed |
| 8 | 1 | subtile index |
| 9 | 1 | map-cell level/elevation |
| 10 | 1 | ice growth / reserved-family byte |

### Main conflict: bytes 4..7

- World-Altering Editor models them as one little-endian signed 32-bit `TileIndex`.
- OpenRA's Gen2 importer reads a 16-bit tile number followed by a 16-bit field it ignores.
- Some community descriptions call the second word zero/reserved.

Required initial model:

```text
IsoMapPack5Record
- XRaw16
- YRaw16
- TileFieldRaw32
- TileLowWordRaw16
- TileHighWordRaw16
- SubTileRaw
- LevelRaw
- TailByteRaw
- RecordOrdinal
```

No implementation should discard the high word before a golden audit.

## 4. Signedness and coordinate domain

WAE uses signed 16-bit X/Y fields; other readers expose unsigned values. The valid stock range normally remains nonnegative, but raw signed and unsigned views should both be retained.

These coordinates are isometric/map storage coordinates. They are not:

- TMP template-grid `(u,v)`;
- overlay array indices;
- screen pixels;
- Unity/world coordinates.

Conversion belongs to an explicit coordinate adapter.

## 5. Four terminal bytes

Community documentation describes four zero bytes after tile records as a `(0,0)` termination coordinate. OpenRA sizes its temporary buffer with `+4`. WAE writes four zero bytes but its reader loops while at least another 11-byte record remains.

Current classification: `ConfirmedByMultipleIndependentImplementations` for the presence/writer practice; `Underconfirmed` for exact stock-runtime sentinel semantics.

Required checks:

- length modulo 11 before the final four bytes;
- whether final four bytes are zero;
- nonzero tail preservation and diagnostic;
- no interpretation as an extra partial record;
- exact input consumption.

## 6. Duplicate and out-of-canvas cells

The record stream can theoretically contain:

- duplicate coordinates;
- descending/noncanonical order;
- cells outside declared `[Map] Size`;
- invalid tile/subtile identities;
- impossible elevation values;
- sparse omissions.

Do not choose dictionary last-write-wins inside the byte decoder. Preserve duplicates and let a named canvas-composition policy return `Complete`, `Sparse`, `DuplicateAmbiguous` or `OutOfRange`.

## 7. Ordering

WAE sorts saved nonclear cells by X, then level, then tile index to improve compression. That is writer optimization, not semantic map order.

A lossless roundtrip must retain source record order unless the caller explicitly requests regenerated canonical packing.

## 8. Tile binding

Tile-field interpretation requires:

- selected theater;
- theater/control INI tile-set registry;
- template identity and TMP resolution;
- subtile count validation.

The binary decoder only returns raw tile candidates. Invalid IDs are not silently replaced with tile zero; editor repair behavior must be explicit and traced.

## 9. Limits

Bound independently:

- map dimensions;
- expected full cell count;
- LZO block count;
- decompressed bytes;
- record count;
- duplicate coordinate groups;
- canvas allocation;
- diagnostics.

Use checked arithmetic for all dimension and byte-size formulas.

## 10. Forbidden behavior

Do not:

- assume the high tile word is always zero;
- reinterpret every payload as dense solely from declared size;
- discard the final four bytes without recording them;
- clamp coordinates/elevation;
- replace missing or invalid templates during parse;
- sort records during read;
- materialize an unbounded diamond canvas;
- use PreviewPack as terrain recovery.
