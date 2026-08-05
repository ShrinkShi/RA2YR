# Cell and coordinate encodings

> **Source notice:** ChatGPT Web public-source research only. ProjectBaseline was not read. This is not a Codex Agent artifact. No GPL or unclear-license code was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

## Coordinate spaces must remain distinct

The project must not expose one generic `MapCoordinate` for all legacy data. At minimum, distinguish:

- `ScenarioCellIdRaw`;
- `ScenarioCellCoordinate`;
- `IsoMapRawCoordinate`;
- `OverlayStorageCoordinate`;
- `DiamondCanvasCoordinate`;
- `TmpLocalCellCoordinate`;
- `InfantrySubCell`;
- `ScreenCoordinate`;
- `SimulationCoordinate`;
- `UnityCoordinate`.

A conversion between two spaces is an explicit operation with a profile and diagnostic result. A coordinate is never silently axis-swapped because one interpretation happens to land inside the current map.

## Strong scenario-cell candidate

The leading public tool and community candidate is:

```text
ScenarioCellId = Y × 1000 + X
```

with inverse:

```text
X = ScenarioCellId mod 1000
Y = ScenarioCellId div 1000
```

WAE writes this formula for:

- `[Terrain]` keys;
- `[CellTags]` keys;
- `[Waypoints]` values.

Its Terrain loader takes the last three decimal digits as X and the preceding digits as Y. ModEnc documents CellTags and Waypoints as combined map coordinates.

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| WAE writes and reads `Y × 1000 + X` for the named sections | `ImplementationSpecificBehavior` | World-Altering Editor | Named editor behavior only. | Keep as source-pinned profile. | `NotRun` |
| ModEnc documents combined scenario-cell coordinates | `ConfirmedCommunityConvention` | ModEnc | Stable community convention, not runtime proof. | Retain as documentation evidence. | `NotRun` |
| `Y × 1000 + X` is the leading RA2/YR external scenario-cell candidate | `Underconfirmed` | WAE and ModEnc | Shared community knowledge and no original-runtime source prevent stronger confirmation. | Explicit profile only. | `NotRun` |
| A unique original-runtime axis/radix contract is established | `Unresolved` | No original-runtime source located | Official editor internal axis naming and malformed behavior remain unsourced. | Never choose by bounds or lookup plausibility. | `NotRun` |

## Competing axis candidate

A conceptual alternative is:

```text
ScenarioCellId = X × 1000 + Y
```

No load-bearing source selected this as the normal external RA2/YR representation, but it remains an explicit comparison profile for future audit because official editors sometimes use internally swapped axes or differently named coordinates.

The project must not choose between the two based on:

- which result is inside `[Map] Size`;
- which result corresponds to an existing IsoMap cell;
- which result has a known terrain type;
- which result produces fewer overlap diagnostics;
- which result renders plausibly.

This prohibition is `DefensiveDesign`.

## Radix and numeric domain

`1000` is strongly documented as the decimal radix for these scenario-cell IDs. It implies that direct modulo/division cannot represent an X component greater than 999 without ambiguity.

However, this does not prove:

- that the runtime supports every X and Y in `0..999`;
- that negative IDs are legal;
- that signed 32-bit is the exact runtime storage type;
- that values above `999999` are valid;
- that editors and runtime reject overflow identically.

Core stores:

```text
ScenarioCellIdRaw
- TextRaw
- Signed64Candidate
- Unsigned64Candidate
- ParseStatus
- RadixProfile
```

It performs checked arithmetic before narrowing to any project coordinate type.

## Where the coordinate lives

| Section family | Coordinate form |
|---|---|
| Structures | X and Y are separate value tokens |
| Units | X and Y are separate value tokens |
| Infantry | X and Y are separate value tokens; SubCell is separate |
| Aircraft | X and Y are separate value tokens |
| Terrain | combined cell ID in the key |
| Smudge | X and Y are separate value tokens |
| Waypoints | combined cell ID in the value |
| CellTags | combined cell ID in the key |

The direct X/Y techno records do not need to be converted through the combined cell ID in order to preserve or validate them.

## Map-domain analysis

Coordinate interpretation produces a raw coordinate candidate. Validation then reports independent dimensions:

```text
ScenarioCoordinateDomainAnalysis
- StorageCoordinateParsed
- WithinMapSizeCandidate
- WithinLocalSizeCandidate
- IsoMapCellPresent
- LevelBindingAvailable
- BridgeOrHighCandidate
- CoordinateProfile
- EvidenceGrade
- Diagnostics
```

`LocalSize` is a playable/visible rectangle candidate, not the storage grammar. A record outside LocalSize can still be a valid scenario placement. A record outside Size or without an IsoMap cell is preserved and diagnosed rather than deleted.

## IsoMap relationship

The M3-R4 dossier keeps IsoMap raw X/Y distinct from normalized diamond canvas coordinates. Scenario techno X/Y and combined scenario-cell coordinates are commonly treated as map cell coordinates, but the exact runtime relationship remains evidence-gated.

The coordinate binder may compare a scenario coordinate with the IsoMap coordinate index, but it cannot:

- rewrite the placement coordinate;
- create a missing IsoMap record;
- choose an axis profile based on a successful lookup;
- infer tile, level, or TMP information from the placement record alone.

## Overlay relationship

Overlay uses a separate 512×512 storage array with the leading candidate index:

```text
index = X + 512 × Y
```

That storage formula must not be reused as the scenario-cell identity. `Y × 1000 + X` and `X + 512 × Y` serve different serialized structures.

## Infantry subcell

The Infantry `SubCell` token is an occupancy slot within a scenario cell. It is not:

- `IsoMapPack5.SubTileRaw`;
- a TMP offset-table cell index;
- a pixel offset;
- a formation group number;
- a waypoint ID.

Public editors expose finite subcell enumerations and can place multiple infantry in one scenario cell. Exact runtime-valid values and invalid-value behavior remain `Unresolved`.

The raw model is:

```text
InfantrySubCellRaw
- TextRaw
- SignedIntegerCandidate
- EnumCandidate
- Profile
- EvidenceGrade
```

No modulo, clamp, fallback slot, or automatic collision resolution occurs in Core. These are `DefensiveDesign` requirements.

## High/bridge state

Unit and Infantry common layouts contain a `High` field candidate. It must not be folded into X/Y or map Level. It may affect upper/lower bridge occupancy, but full bridge logic is outside this dossier.

Keep distinct:

- scenario X/Y;
- placement `HighRaw`;
- IsoMap `LevelRaw`;
- TMP local height/ramp/depth;
- final simulation layer;
- rendered Y offset.

## Waypoint coordinates

Waypoint keys are identity slots; waypoint values are scenario-cell IDs. The key must not be decoded as a cell. The value must not be treated as a record ordinal.

A waypoint can decode to a coordinate that is outside map or LocalSize. This yields a preserved waypoint plus domain diagnostics, not automatic removal or relocation.

## CellTags

CellTags invert the Waypoints key/value roles:

```text
key   = ScenarioCellId
value = TagId
```

Duplicate CellTags at one normalized coordinate form an ambiguity group. The parser must not choose first-wins or last-wins.

## Unity boundary

Core returns integer/raw coordinate models only. Converting to Unity `Vector2Int`, `Vector3Int`, Tilemap coordinates, world positions, isometric projection, or rendered pixels is an adapter responsibility.
