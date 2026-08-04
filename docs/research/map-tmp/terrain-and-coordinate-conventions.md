# Terrain semantics and coordinate conventions

## 1. Coordinate domains

At least six coordinate domains must remain distinct:

1. **Map storage/isometric coordinates** — X/Y records in `IsoMapPack5`.
2. **Decimal cell identifiers** — commonly `y × 1000 + x` in waypoints, terrain and cell tags.
3. **Overlay storage coordinates** — fixed `x + 512 × y` array indexing.
4. **TMP template coordinates** — `(u,v)` slot in a template's width×height grid.
5. **TMP pixel coordinates** — diamond/extra image positions.
6. **Engine/render coordinates** — world, screen, camera and Unity coordinates.

No public type should use an unqualified `Position` for all six.

## 2. Suggested raw types

```text
MapIsoCellRaw16
MapCellXY
PackedDecimalCell
OverlayCanvasCoordinate
TmpTemplateCellCoordinate
TmpPixelCoordinate
MapElevationRaw8
```

Conversion functions must declare source/target domains, checked ranges and whether the operation is reversible.

## 3. Map size and diamond canvas

`[Map] Size=x,y,width,height` describes the full isometric map rectangle. `[Map] LocalSize=` describes a local/playable rectangle. Public importers often create a different rectangular internal canvas and transform raw RX/RY coordinates into engine cells.

Those importer transforms are implementation behavior. Core should retain:

- raw size fields;
- local-size fields;
- raw IsoMap record coordinates;
- a separately selected coordinate-policy version;
- conversion diagnostics.

## 4. Elevation

Map cell elevation is stored in the IsoMap record's level byte. It interacts with:

- TMP ramp metadata;
- terrain template arrangement;
- cell-to-screen projection;
- object draw sorting;
- bridge and cliff logic;
- pathfinding and slope movement.

The map reader preserves the byte and validates configured range. It does not calculate Unity Y or terrain mesh vertices.

## 5. Theater binding

Theater selection connects map tile identities to:

- theater control INIs;
- tile-set registrations;
- theater-specific TMP extensions and MIX packages;
- palettes;
- LAT/connected terrain sets;
- land/terrain classes.

Missing theater resources are content-binding diagnostics, not map byte-parse failures.

## 6. Slopes and ramps

Ramp appearance/behavior is distributed:

- map record selects a tile and map level;
- TMP cell metadata provides raw ramp/height candidates;
- tile-set/theater metadata groups templates;
- runtime defines passability and unit orientation.

No single `RampType` byte should be converted directly into a renderer slope without a profile and evidence.

## 7. Cliffs

Cliffs commonly use:

- multi-cell TMP template arrangements;
- optional extra color/depth rectangles;
- map-level differences;
- theater tile-set groups;
- object/depth sorting rules.

The extra rectangle supports the visual face but does not independently define collision or legal cliff adjacency.

## 8. Water and shores

Water/shore behavior depends on selected tile/template, terrain class and connected-tile conventions. Color indices or depth pixels alone cannot identify water.

A later terrain binder can return:

```text
TerrainCellBinding
- MapRecord
- TheaterTileDefinition
- TmpDocument
- TmpSubTile
- EffectiveLandType
- RampCandidate
- Diagnostics
```

## 9. Bridges

Bridge behavior may combine:

- TMP bridge-head/terrain graphics;
- bridge overlay types and overlay data frames;
- map elevation and cells beneath the bridge;
- runtime locomotion and destruction logic.

Overlay packs do not store a second height plane and cannot represent arbitrary stacked overlays per cell. Do not infer complete bridge geometry from one source.

## 10. Projection boundary

The format dossier does not define:

- isometric camera matrices;
- pixel-to-world scale;
- terrain mesh triangulation;
- object anchor points;
- depth-buffer bias;
- shadows or water shaders;
- slope interpolation.

A later adapter must cite coordinate evidence and use basis-point fixtures. Core remains engine-neutral.

## 11. Determinism

Coordinate conversion results must not depend on:

- platform integer overflow;
- current culture;
- floating-point dictionary order;
- map record enumeration order;
- rendering frame state.

Prefer checked integers/rationals for reversible storage conversions. Floating-point conversion belongs to the adapter.

## 12. Diagnostics

Suggested codes:

- `MapSizeInvalid`
- `LocalSizeOutsideMapCandidate`
- `IsoCoordinateOutsideDeclaredCanvas`
- `PackedCellOverflow`
- `OverlayCoordinateOutsideFixedCanvas`
- `TmpTemplateCoordinateOutsideGrid`
- `MapElevationOutsideProfile`
- `TheaterBindingMissing`
- `TileIdentityUnresolved`
- `RampSemanticsUnresolved`
- `BridgeSemanticsRequireMultipleLayers`
