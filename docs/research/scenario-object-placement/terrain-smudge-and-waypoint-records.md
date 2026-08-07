# Terrain, smudge, waypoint, and CellTag records

> **Source notice:** ChatGPT Web public-source research only. ProjectBaseline was not read. This is not a Codex Agent artifact. No GPL or unclear-license code was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

## Terrain records

The leading RA2/YR candidate is:

```ini
[Terrain]
ScenarioCellId=TerrainTypeId
```

WAE writes the key as `Y * 1000 + X` and the value as the logical `TerrainType` INI name. Its loader decodes the last three decimal digits as X and the leading digits as Y, binds the value through Rules, and skips unknown or out-of-map objects. Those skips are editor behavior and must not be inherited by the strict parser.

### Terrain raw model

```text
TerrainPlacementRaw
- RecordKeyRaw
- ScenarioCellIdRawCandidate
- TypeRaw
- UnknownValueTail
- SourceOccurrence
```

Although the common value is one token, Core keeps the complete raw value in case extensions or malformed records contain commas or suffixes.

### Terrain object boundary

A Terrain placement object is not:

- TMP `TerrainTypeRaw`;
- the ground tile in IsoMapPack5;
- an Overlay decoration;
- a theater TileSet role;
- a map cell itself.

It is a scenario object whose logical type is registered under `[TerrainTypes]`.

### State boundaries

Public map sections do not establish a universal owner, health, facing, burn, destroyed, or random-variation field for Terrain. Such behavior may come from Rules, Art, animation, editor rendering, or runtime state.

Unknown state must not be inferred from the visual resource.

## Smudge records

WAE's common writer emits:

```ini
[Smudge]
Ordinal=SmudgeTypeId,X,Y,0
```

Candidate fields:

| Index | Candidate |
|---:|---|
| 0 | SmudgeType logical ID |
| 1 | X |
| 2 | Y |
| 3 | Data/unknown/zero field |
| 4+ | extension tail |

The key is list-like rather than the coordinate. ModEnc documents preplaced SmudgeTypes and distinguishes their top-corner coordinates.

### Smudge categories

Possible source/runtime families include:

- scorch/crater decals;
- building bib candidates;
- manually placed static smudges;
- runtime-created damage smudges;
- extension-defined smudges.

The map parser only reads source placements. It does not synthesize runtime damage smudges or building bibs.

### Smudge boundary

Keep separate:

```text
SourceMapSmudge
RuntimeGeneratedSmudge
BuildingBibCandidate
VisualDecal
PassabilityCandidate
SimulationResidue
```

A visual decal does not automatically change collision or movement.

## Waypoint records

Leading shape:

```ini
[Waypoints]
WaypointId=ScenarioCellId
```

WAE's `Waypoint` writer uses:

```text
ScenarioCellId = Y * 1000 + X
```

and writes `Identifier` as the key. Its parser enforces an editor maximum waypoint range. That maximum is a tool constraint until confirmed elsewhere.

### Waypoint identity

Unlike common techno record keys, the waypoint key is semantically significant. ModEnc states that the game allocates waypoint slots and interprets the index as the waypoint number.

Therefore:

- gaps are preserved;
- key `0` is valid under the common profile;
- duplicate waypoint IDs are ambiguous;
- `1` and `01` can collide numerically while remaining distinct raw text;
- nonnumeric keys remain raw and diagnostically invalid under the numeric profile;
- canonical renumbering is forbidden by default.

### Gameplay meaning boundary

Community conventions associate waypoint numbers with multiplayer starts, camera/home positions, reinforcement paths, or mission logic. Those meanings require explicit scenario profiles.

The base model only provides:

```text
WaypointRaw
- WaypointIdRaw
- NumericWaypointIdCandidate
- ScenarioCellIdRaw
- CoordinateCandidate
```

It does not declare waypoint 0 to be a player start without a selected profile.

## CellTag records

Leading shape:

```ini
[CellTags]
ScenarioCellId=TagId
```

The key is a coordinate identity and the value is a Tag reference. WAE writes the same `Y * 1000 + X` cell formula.

### CellTag raw model

```text
CellTagRaw
- CellKeyRaw
- ScenarioCellIdCandidate
- TagIdRaw
- SourceOccurrence
```

### Error separation

Report independently:

- key numeric parse failure;
- coordinate decoding failure;
- outside map Size;
- outside LocalSize;
- missing IsoMap cell;
- missing Tag target;
- duplicate cell key;
- conflicting Tag targets.

The record remains preserved in every case.

## Duplicate coordinates and cooccupancy

Different families can legally or illegally share a coordinate. The parser does not decide by deletion.

Examples for later analysis:

- multiple Infantry in one cell: likely legal with distinct subcells;
- two Terrain objects in one cell: conflict candidate;
- Smudge and Structure: potentially legal visual overlap;
- Terrain and Structure: family-specific conflict candidate;
- CellTag and any placement: normally independent metadata;
- Waypoint and any placement: normally independent reference marker;
- Overlay and object: common but semantically separate.

Suggested classifications:

- `LegalCooccupancyCandidate`;
- `DuplicateIdentity`;
- `CellConflictCandidate`;
- `UnsupportedOverlap`;
- `Unresolved`.

## Map resize and out-of-domain data

Editors may clean, move, or drop records during resize. Core must preserve records that are:

- outside LocalSize;
- inside storage coordinate range but outside current scenario diamond;
- outside Size;
- on a coordinate with no IsoMap record;
- associated with a missing type.

These conditions affect semantic/domain validity, not lossless parse identity.

## Art and rendering boundary

Terrain and Smudge type binding may later lead to Art/SHP resources. Missing graphics do not invalidate the source placement. The parser does not choose random variants, damaged frames, palette, footprint, collision, or Unity objects.
