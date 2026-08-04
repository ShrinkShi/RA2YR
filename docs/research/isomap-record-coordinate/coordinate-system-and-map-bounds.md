> **Source notice:** This research was prepared by **ChatGPT Web** from public sources. It did not read the local ProjectBaseline and is not the product of the local Codex Agent. No GPL or unclear-license code was copied, translated, mechanically rewritten, or imported (`code_imported: false`).

# Coordinate systems and map bounds

## Coordinate spaces that must remain distinct

| Space | Example model | Owner |
|---|---|---|
| Map INI full extent | `Size = left,top,width,height` | map document |
| Playable/local extent | `LocalSize` | scenario/gameplay policy |
| IsoMap raw coordinates | `XRaw16`, `YRaw16` | 11-byte record |
| Rectangular isometric canvas | `DisplayColumn`, `DisplayRow` | coordinate analysis |
| Diamond validity domain | valid subset of a larger raw coordinate plane | coordinate analysis |
| TMP-local cell | cell index inside one multi-cell TMP | theater/TMP binding |
| Projected screen coordinate | pixel X/Y after tile dimensions and height | renderer |
| Simulation/world coordinate | engine movement and physics position | simulation adapter |
| Array/index coordinate | implementation storage index | container implementation |

No conversion result should overwrite the source coordinates.

## Map `Size` and `LocalSize`

The third and fourth `Size` values provide the full map width `W` and height `H` used by the public IsoMap readers studied.

`LocalSize` defines a local/playable rectangle. It does not determine the binary record count or raw coordinate domain in the reviewed readers.

The first two `Size` values are commonly zero in writers, but this dossier does not assume they are always zero or silently fold them into IsoMap record coordinates.

## Rectangular canvas candidate

Multiple independent tools allocate a relative/display canvas with:

```text
CanvasWidth  = 2 × W - 1
CanvasHeight = H
CanvasCellCount = (2 × W - 1) × H
```

The rectangle contains one valid isometric cell for every `(column,row)` pair. It is a convenient normalized canvas, not the raw IsoMap coordinate system.

For a normalized canvas cell:

```text
DisplayColumn = dx, 0 .. 2W-2
DisplayRow    = row, 0 .. H-1
ExpandedY     = dy = 2 × row + (dx mod 2)
```

Candidate conversion to raw record coordinates, convergent in OpenRA and CNCMaps:

```text
RawX = (dx + dy) / 2 + 1
RawY = dy - RawX + W + 1
```

Inverse candidate:

```text
DisplayColumn = RawX - RawY + W - 1
ExpandedY     = RawX + RawY - W - 1
DisplayRow    = ExpandedY / 2
```

The inverse is valid only if the parity and bounds checks pass.

## Explicit domain predicate

A candidate normalized-domain check is:

```text
0 <= DisplayColumn <= 2W - 2
0 <= ExpandedY <= 2H - 1
ExpandedY mod 2 == DisplayColumn mod 2
DisplayRow = ExpandedY / 2
0 <= DisplayRow < H
```

Equivalent checks may be expressed directly in raw coordinates. Implementations must test candidate formulas against fixtures rather than share the production conversion code with fixture builders.

## Diamond and blank areas

EA FinalAlert uses a larger square backing grid based on an isometric size and filters coordinates with inequalities that form the map diamond. XCC also traverses a diamond-shaped range. Other implementations normalize the same cells into a `(2W-1) × H` rectangle.

Therefore:

- the raw coordinate plane contains positions outside the valid map diamond;
- a raw `(X,Y)` pair may be numerically small but still be outside the domain;
- the rectangular display canvas is dense even though the surrounding raw plane contains blank areas;
- a record in a diamond blank region is `OutOfDomainCoordinate`, not an array-clamp candidate.

## Raw signedness

Source declarations conflict:

- EA FinalAlert, XCC, OpenRA, CNCMaps, and MapTool use unsigned 16-bit reads for raw X/Y;
- WAE uses signed 16-bit properties and writes signed `Int16` values.

The reader retains `XRaw16` and `YRaw16` and exposes both signed and unsigned views. Domain policies choose a view explicitly.

Negative signed views must not cause the bytes to be discarded. Likewise, a large unsigned value must not be clamped into the map.

## Axis-name conflict

EA editor code sometimes stores or indexes `wX` and `wY` in an order that appears swapped relative to its internal array expression, and one save path assigns `wX = internalY`, `wY = internalX`. This is evidence that field names and internal row/column names are not reliable coordinate specifications.

Required names should include the source space:

- `RecordXRaw`
- `RecordYRaw`
- `DisplayColumn`
- `DisplayExpandedY`
- `DisplayRow`
- `MapArrayColumn`
- `MapArrayRow`

Avoid generic `x` and `y` across layer boundaries.

## Origin and center

Public formulas use a `+1` raw-coordinate bias and a width-dependent offset. This supports a raw origin outside the normalized canvas, but does not establish a single game-world origin or map center.

The Core should expose an explicit `IsoMapCoordinateTransformResult` containing:

- source raw views;
- selected coordinate profile;
- normalized canvas candidate;
- domain classification;
- parity classification;
- evidence grade;
- diagnostics.

Map center, screen center, camera origin, and simulation origin belong to adapters.

## Record order is not coordinate conversion

FinalAlert's writer traversal, XCC's traversal, WAE's compression-oriented sorting, and arbitrary source order may all emit the same coordinate set in different sequences. The coordinate validator must not require a particular order.

## Defensive arithmetic

All calculations require checked arithmetic before allocation or multiplication:

- `2 × W - 1`;
- `(2 × W - 1) × H`;
- `recordCount × 11`;
- square/backing-grid candidates;
- coordinate sums and differences.

Invalid dimensions yield structured diagnostics; no partial canvas is allocated.

## Project policy

- `Size`, `LocalSize`, raw coordinates, normalized canvas, and Unity coordinates remain separate.
- No automatic coordinate swap.
- No wrapping, modulo reduction, clamp, or nearest-cell repair.
- Out-of-domain records remain in the raw document and in a diagnostic collection.
- Conversion profiles are serializable and evidence-graded.
- Unity `Vector2`, `Vector3`, Tilemap coordinates, and projected pixels are outside Core.
