# Proposed Core implementation boundaries

> Design only. No C# implementation or mechanical port of a reference reader is included.

## 1. Goals

- preserve the lossless map shell before semantic interpretation;
- decode packed sections through bounded, section-specific components;
- keep raw disputed fields and explicit candidate views;
- keep MAP, TMP, theater binding, scenario graph and rendering separate;
- support Memory, seekable Stream and MIX-entry windows through one parse core;
- preserve provenance and all suppressed/unknown evidence;
- make repair and normalization opt-in and reportable;
- keep Core free of `UnityEngine`.

## 2. Map shell types

Suggested models:

- `Ra2MapDocument`
- `Ra2MapIdentity`
- `Ra2MapHeaderView`
- `Ra2MapSectionOccurrence`
- `Ra2MapDiagnostic`
- `Ra2MapReadLimits`
- `Ra2MapParseResult`

`Ra2MapDocument` references the repository lossless INI model. It does not duplicate or flatten raw text.

## 3. Packed-section types

- `MapPackedSectionFragments`
- `MapPackedSectionEnvelopeResult`
- `ChunkedLzoBlockDirectory`
- `Format80DecodeResult`
- `MapPackedSectionDiagnostic`

Envelope parsing, Base64 decoding, LZO and Format80 are independent components. The section-specific layer supplies the expected output contract.

## 4. IsoMap types

- `IsoMapPack5Document`
- `IsoMapPack5RecordRaw`
- `IsoMapPack5TerminalRaw`
- `IsoMapCanvasResult`
- `IsoMapCoordinatePolicy`

`IsoMapPack5RecordRaw` retains the 32-bit tile field and both 16-bit views. Canvas composition retains duplicate/out-of-range records and returns status rather than mutating them.

## 5. Overlay and preview types

- `OverlayPackDocument`
- `OverlayDataPackDocument`
- `OverlayElementWidth`
- `OverlayPairBindingResult`
- `PreviewPackDocument`
- `PreviewPixel3Raw`
- `PreviewColorOrderPolicy`

The overlay binder joins only validated arrays. The preview parser returns raw components and metadata; image construction belongs to an adapter.

## 6. Mission graph types

- `MapObjectPlacementRaw`
- `MapHouseRegistry`
- `MapTriggerRecordRaw`
- `MapEventRecordRaw`
- `MapActionRecordRaw`
- `MapTagRecordRaw`
- `MapTeamTypeRecordRaw`
- `MapTaskForceRecordRaw`
- `MapScriptRecordRaw`
- `MapScenarioGraphResult`

Every typed record retains raw positional fields and occurrence provenance. The graph resolver never executes opcodes.

## 7. Map-local configuration types

- `MapLocalIniLayer`
- `MapLocalSectionClassification`
- `MapRulesCompositionResult`
- `MapArtCompositionResult`
- `MapEffectiveEntry`

These consume the content-load-order project's effective global documents. The map parser never scans providers or archives.

## 8. TMP types

- `TmpDocument`
- `TmpHeaderRaw`
- `TmpCellOffsetEntry`
- `TmpCellHeaderRaw`
- `TmpCellMetadataCandidate`
- `TmpDiamondPlane`
- `TmpExtraPlane`
- `TmpParseResult`
- `TmpDiagnostic`
- `TmpReadLimits`

Pixel/depth planes may be represented as bounded immutable slices or arrays. No Unity texture is constructed.

## 9. Binding types

- `MapTheaterBindingResult`
- `MapTileRegistryView`
- `MapTileTmpBinding`
- `TerrainCellBinding`
- `MapScenarioTypedView`

Binding is allowed to remain incomplete or ambiguous. It does not replace invalid IDs with tile zero.

## 10. Suggested modules

1. **Map shell reader** — lossless INI and family identity.
2. **Packed fragment collector** — ordering and Base64.
3. **Compression layer** — bounded LZO and Format80.
4. **IsoMap reader** — raw records and terminal.
5. **Overlay/preview readers** — section-specific output contracts.
6. **Mission-record readers** — raw positional schemas.
7. **Scenario graph resolver** — identities and references.
8. **Map-local INI composer** — global plus map-local semantic layers.
9. **TMP directory reader** — header and offsets.
10. **TMP cell reader** — raw metadata and planes.
11. **Theater/TMP binder** — tile identity to resource.
12. **Renderer/editor adapters** — previews, terrain mesh, UI and Unity.

Do not create one `MapLoader` that owns all layers.

## 11. Limits

### `Ra2MapReadLimits`

- input bytes and line length;
- section/key/raw occurrence counts;
- packed Base64 characters;
- compressed/decompressed bytes;
- LZO/Format80 blocks and commands;
- IsoMap records and map dimensions;
- mission records and graph edges;
- overlay/preview bytes;
- diagnostics and allocated bytes.

### `TmpReadLimits`

- input bytes;
- template dimensions/cells;
- pixel dimensions;
- cell offsets/ranges;
- normal and extra-plane areas;
- total decoded plane bytes;
- diagnostics and allocation.

Limits are configuration, not format constants.

## 12. Canonical hashes

Potential local-only hashes:

- lossless map shell model;
- packed compressed and decompressed section models;
- IsoMap raw-record order and canvas candidate;
- overlay pair;
- preview raw components;
- mission graph identities/edges;
- TMP directory/raw cell model;
- theater/TMP bindings.

Public summaries expose only approved aggregate hashes, never per-cell/TMP/pixel hashes that assist reconstruction.

## 13. Diagnostics

Every diagnostic should include:

- severity/code;
- logical source and provenance;
- bounded absolute/relative offset or INI occurrence;
- section/record/cell ordinal where safe;
- expected/actual lengths;
- policy/profile used;
- concise nonsecret message.

Diagnostic count and message size are budgeted.

## 14. Writer boundary

A future writer is a separate service with mutation tracking. It must support:

- preserve-raw mode;
- regenerate-selected-section mode;
- explicit compatibility ordering;
- transactional ID remapping;
- atomic output;
- normalization/lost-evidence report.

Parsing success does not imply write support.

## 15. No Unity leakage

Core public/internal types must not expose:

- `Texture2D`, `Mesh`, `Material`;
- Unity `Vector`, `Matrix`, `Color` or `GameObject` types;
- renderer vertex/index buffers;
- editor window/control types;
- scene/simulation entities.
