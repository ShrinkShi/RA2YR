# Cliff, water, shore, and bridge boundaries

## 1. Cross-format principle

Cliffs, water, shores, and bridges are not represented by one TMP field. They combine:

- theater-control TileSet roles;
- one or more TMP files and subtile metadata;
- map tile ID, SubTile, and Level;
- overlay type/data where applicable;
- renderer depth and extra graphics;
- movement/pathfinding and bridge-state logic.

No Core format class should claim ownership of the complete feature.

## 2. Cliffs

Theater control INIs commonly identify cliff-related TileSets through keys such as `CliffSet` and slope-piece keys.

TMP extra graphics often provide vertical cliff faces, but:

- `HasExtraData` is generic;
- extra graphics can support non-cliff art;
- a cliff TileSet can contain cells without extra graphics;
- path blocking and height transitions are not encoded solely by the extra plane.

Recommended binding:

```text
CliffResourceBinding
- TheaterTileSetRole
- GlobalTileIdRange
- TmpCellMetadataCandidates
- ExtraPlanePresence
- MapPlacementContext
- SemanticStatus
```

## 3. Water

Water identity can involve:

- `WaterSet` or other theater registry roles;
- TMP `TerrainTypeRaw` candidate;
- animated or replacement TileSets;
- Rules locomotor behavior;
- map overlays/resources;
- theater-specific art.

The TMP parser does not mark a cell navigable by naval units. A semantic binder resolves a water candidate and later simulation policy decides movement.

## 4. Shores and beaches

`ShorePieces` and ice-shore keys identify TileSet relationships used to connect land and water. Shore art can include:

- beach terrain candidates;
- water-edge TMP graphics;
- LAT-like transition selection;
- local ramp/height data;
- overlay or animation support.

Do not infer shore semantics from a filename containing `shore` or from a TerrainType byte alone.

## 5. Ice

Snow profiles can reference `Ice1Set`, `Ice2Set`, `Ice3Set`, and `IceShoreSet`.

Ice combines:

- theater registry roles;
- TMP terrain candidate;
- map state and possible breakability;
- overlay/state systems;
- movement policy.

The raw binder records the registry and TMP candidate. It does not simulate ice strength or breaking.

## 6. Bridges

Bridges are explicitly cross-format:

- bridge deck and approach TMP TileSets;
- `BridgeSet`, `TrainBridgeSet`, or `WoodBridgeSet` registry roles;
- overlay entries and overlay data for bridge pieces/state;
- map placement and Level;
- passability and destroyed-state logic;
- potentially SHP animation and debris assets.

A TMP file cannot by itself determine whether a bridge is intact, destroyed, traversable, railroad, wood, or high/low.

## 7. YR and editor differences

WAE notes that some traditional bridge keys are absent or changed in YR terrain expansion profiles. FinalAlert also maintains editor-specific NewUrban configuration and fallback behavior.

Project policy:

- keep each theater/game profile explicit;
- retain missing-key state;
- do not copy a Temperate or TS bridge key into YR by default;
- do not fill missing registry roles by matching SetName text;
- extension profiles may add roles without changing vanilla evidence labels.

## 8. Overlay boundary

OverlayPack/OverlayDataPack supplies map-local overlay identity and frame/state. It does not replace TMP terrain underneath.

The final cell view can contain both:

```text
BaseTerrainPlacement
OverlayPlacement
BridgeSemanticStateCandidate
```

Content resolution and parsing preserve both independently before a simulation adapter combines them.

## 9. Renderer boundary

The renderer can use:

- diamond color and depth;
- extra color and depth;
- map Level;
- local TMP height;
- overlay frames;
- shadows and lighting.

It must not feed visual depth values back into terrain movement rules without an explicit semantic mapping.

## 10. Pathfinding boundary

Pathfinding inputs should come from a resolved terrain semantic view, not directly from:

- TileSet display name;
- palette index;
- extra-plane presence;
- depth-plane values;
- editor category strings.

The semantic view records provenance back to every contributing TMP, INI, map, and overlay candidate.

## 11. Diagnostics

Suggested diagnostics:

- special role references missing TileSet;
- bridge overlay without bridge terrain candidate;
- bridge terrain without compatible overlay/state candidate;
- shore role without water role;
- ice-shore role outside Snow-like profile;
- cliff role whose TMP binding is missing;
- conflicting TerrainType and registry-role candidates;
- extension-only role used in vanilla profile;
- fallback file extension used;
- pathing semantics unresolved despite renderable art.

## 12. Evidence status

| Claim | Status |
|---|---|
| special set keys bind TileSets | official editor + reimplementation evidence |
| extra data means cliff | rejected overgeneralization |
| bridge behavior is TMP-only | rejected boundary |
| water behavior equals TerrainType byte | underconfirmed/incomplete |
| overlay participates in bridge state | strong community/tool evidence |
| exact original runtime combination order | `Unresolved` |
