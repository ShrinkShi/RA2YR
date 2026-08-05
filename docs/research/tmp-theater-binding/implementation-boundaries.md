# Implementation boundaries

## 1. Required layering

```text
ContentCandidate index
→ ordered theater INI layers
→ lossless INI documents
→ semantic INI composition
→ TheaterControlDocument
→ TheaterTileRegistry
→ TMP asset candidate resolution
→ TmpDocument and raw cell models
→ palette/LAT/terrain semantic binding
→ map terrain placement binding
→ renderer/pathfinder/simulation adapters
```

Each arrow is a distinct API boundary with structured diagnostics.

## 2. Proposed Core models

### TMP raw layer

- `TmpDocument`
- `TmpFileHeaderRaw`
- `TmpCellOffsetEntry`
- `TmpCellHeaderRaw`
- `TmpCellKnownFlags`
- `TmpCellPlaneDirectory`
- `TmpEncodedDiamondPlane`
- `TmpExtraPlane`
- `TmpParseResult`
- `TmpDiagnostic`
- `TmpReadLimits`

### Theater document and registry

- `TheaterProfileDescriptor`
- `TheaterControlDocument`
- `TheaterGeneralRawView`
- `TheaterTileSetDescriptor`
- `TheaterTileRegistry`
- `TheaterTileIdRange`
- `TheaterRegistryDiagnostic`

### Resource candidates

- `TmpAssetCandidate`
- `TmpVariationCandidate`
- `TmpAssetResolutionTrace`
- `TheaterPaletteBinding`
- `TheaterLatBinding`
- `TheaterSpecialTileSetBindings`

### Semantic results

- `TmpCellMetadataCandidateView`
- `RampSemanticBinding`
- `TerrainSemanticBindingResult`
- `MapTerrainPlacementBinding`
- `TmpTheaterBindingResult`

None of these types contains `UnityEngine`, texture, mesh, material, collider, navigation, or game-object types.

## 3. Raw-field preservation

`TmpCellHeaderRaw` retains all 52 bytes as named raw fields and bit-preserving views. It must not:

- clear unknown flags;
- rewrite trailing bytes;
- normalize offsets;
- convert height/ramp/terrain into final enums during parse;
- apply palette colors;
- precompose extra graphics.

Typed candidate views reference the raw model and their evidence profile.

## 4. Plane parsing

The plane parser receives:

- bounded TMP window;
- cell header offset;
- file dimensions;
- raw header;
- read limits;
- explicit layout policy.

It returns a plane directory before reading/allocating plane bodies.

Required checks:

- checked arithmetic;
- per-axis and area budgets;
- range containment;
- overlap classification;
- exact lengths;
- no clamping or zero padding;
- no file-window escape;
- no automatic offset/sequential fallback.

## 5. Theater INI composition

The theater typed reader consumes an already composed lossless INI result. It does not scan content providers or perform cross-file composition itself.

It retains source provenance for each effective key and all suppressed candidates.

Packed map-fragment collection rules do not apply to theater INIs.

## 6. Registry allocation

One component owns global tile-ID allocation.

Inputs:

- normalized unique TileSet descriptors;
- deterministic numeric order;
- validated `TilesInSet` values;
- configured total-ID budget.

Outputs:

- immutable ranges;
- role bindings;
- diagnostics;
- canonical serializable hash.

Missing TMP files cannot alter ranges.

## 7. Asset resolution

The resolver creates candidate chains from:

- TileSet `FileName`;
- 1-based tile number formatting;
- theater primary extension;
- variation policy;
- explicit fallback extension policy;
- content provider precedence.

The MIX reader does not construct these names or decide theater priority.

Every selected and suppressed candidate retains provenance.

## 8. Semantic binding

A semantic binder can associate raw TMP bytes with candidate definitions for:

- ramp topology;
- terrain/land type;
- local height;
- special theater roles;
- LAT relationships;
- palette selection.

Unknown values remain unknown. No first-match fallback is allowed for ambiguous registries.

## 9. Map-placement boundary

The map binder receives:

- `GlobalTileId`;
- `SubTile`;
- map `Level`;
- resolved theater registry;
- parsed TMP asset;
- semantic profiles.

It returns references and derived candidates, not renderer objects.

It distinguishes missing TileSet, missing TMP, empty SubTile, metadata ambiguity, and unresolved movement semantics.

## 10. Renderer and simulation

Rendering may use color/depth planes, palettes, height, extra rectangles, and overlays.

Simulation/pathfinding may use map Level, ramp topology, terrain category, bridges, overlays, and Rules locomotion data.

They are separate adapters. Rendering success does not imply movement semantics are known.

## 11. Diagnostics and limits

`TmpReadLimits` should include:

- maximum template grid dimensions;
- maximum cell slots;
- maximum tile pixel dimensions and area;
- maximum extra dimensions and area;
- maximum distinct plane windows;
- maximum diagnostics;
- maximum file/window length;
- maximum registry sections and global tile IDs.

Diagnostics are structured codes with source offsets and provenance, but public audit serialization is allowlisted.

## 12. Determinism

- filesystem/INI enumeration order does not affect normalized results;
- sorting logic exists in one component;
- all sort keys are serializable;
- candidate chains are stable;
- Memory/Stream/MIX inputs share one state machine;
- synthetic fixture builders do not call production formulas or registry sorters;
- canonical hashes exclude absolute paths and unstable object identities.

## 13. Writer boundary

Read support does not imply writer support.

A future writer must separately define:

- raw-preserving versus canonical mode;
- unknown flags/padding handling;
- plane offset layout;
- variation and registry updates;
- FinalAlert compatibility profile;
- byte, structural, and semantic roundtrip targets.

No writer design is approved by this dossier.
