# Coordinate indexing and map-domain analysis

> **Research provenance:** This document was produced by ChatGPT Web from public sources. It did not read ProjectBaseline, is not a local Codex Agent artifact, and imports no code. GPL or unclear-license sources are reference-only; no source was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

## 1. Coordinate domains must remain distinct

The following are different domains:

1. 512 × 512 Overlay storage coordinates;
2. IsoMapPack5 raw record coordinates;
3. normalized diamond/canvas coordinates;
4. `[Map] Size` scenario bounds;
5. `LocalSize` playable/visible bounds;
6. numeric object-section cell identities such as `1000 × Y + X`;
7. screen projection coordinates;
8. simulation/world coordinates;
9. Unity coordinates.

No Core API should use a generic `X/Y` pair without a domain type.

## 2. Dominant storage formula

The dominant candidate is:

```text
StorageIndex = StorageX + 512 × StorageY
0 <= StorageX < 512
0 <= StorageY < 512
```

Supporting evidence:

- OpenRA indexes decoded arrays with `rawX + 512 × rawY` after converting map canvas positions to raw map coordinates;
- WAE writes at `tile.Y × 512 + tile.X`, equivalent to `X + 512 × Y` for its `MapTile.X/Y` domain;
- CNCMaps uses IsoMap raw coordinates and `rawX + 512 × rawY`;
- MapTool reconstructs coordinates with `x = index % 512`, `y = index / 512`;
- ModEnc oldid 21267 documents `X + 512 × Y`.

Evidence grade: `ConfirmedByIndependentImplementation` plus `CommunityDocumented`.

## 3. Official editor transposition conflict

EA's editor maps its internal square field array using a pattern equivalent to:

```text
field position = internalX + internalY × IsoSize
Overlay index = internalY + internalX × 512
```

It also copies in the reverse direction using the same transposed relationship.

This can mean:

- the editor's internal axis labels are opposite the external Overlay storage labels;
- its square field array is deliberately transposed for rendering;
- the editor uses `Y + 512 × X` as an actual external storage formula;
- an earlier coordinate conversion has already swapped axes.

The public editor source proves the internal mapping but not the original runtime's external naming. The project therefore records `OfficialEditorTransposedInternalView` rather than declaring a second confirmed runtime format.

## 4. Candidate coordinate profiles

### Profile A: external row-major

```text
index = X + 512 × Y
x = index mod 512
y = floor(index / 512)
```

This is the configured first project candidate.

### Profile B: explicit axis-swapped view

```text
index = Y + 512 × X
x = floor(index / 512)
y = index mod 512
```

This is retained for official-editor comparison and golden-audit analysis. It is not automatically tried after Profile A fails.

### Profile C: caller-supplied coordinate transform

A map-coordinate adapter can convert a named source domain into storage coordinates before the index formula. This supports explicit evidence without embedding diamond math in the array reader.

## 5. IsoMap relationship

OpenRA and CNCMaps provide a useful candidate relationship:

1. read or derive IsoMap raw `RX/RY`;
2. use the raw pair directly as Overlay storage `X/Y`;
3. compute `RX + 512 × RY`.

The normalized canvas coordinates are not themselves used directly as the storage pair. A diamond/canvas position must first convert to the raw map-cell coordinate domain.

This is implementation evidence, not official runtime source.

## 6. Object-section numeric cell IDs

Map object sections commonly encode a coordinate in a decimal identity similar to:

```text
CellId = 1000 × Y + X
```

That identity is text-level scenario data. It is not an Overlay storage offset. A consumer may parse it into a named map coordinate and then use an explicit adapter, but it may not use the decimal integer as an array index.

## 7. Storage versus scenario domains

For every storage cell, analysis should record independently:

- `StorageDomainValid`: both axes in 0..511;
- `ScenarioMapDomainValid`: coordinate belongs to the map's full diamond domain;
- `LocalPlayableDomainValid`: coordinate belongs to `LocalSize` after explicit conversion;
- `IsoMapCellPresent`: a corresponding IsoMap record exists under the selected coordinate profile;
- `OverlayTypeBound`: raw type resolves in the composed registry;
- `SemanticValid`: a selected type-specific profile accepts the raw data.

A storage cell can be valid while outside the current scenario. Those bytes remain part of the decoded map and must be preserved.

## 8. Domain-external and ghost data

Potential states include:

- nonempty Overlay type outside full map domain;
- nonzero data under an empty type outside map domain;
- Overlay at a coordinate with no IsoMap record;
- data inside full map domain but outside `LocalSize`;
- stale data left by map resize;
- conflicting type/data under different coordinate profiles.

The parser reports these states and preserves bytes. Cleanup belongs to an explicit editor/canonicalization operation, never parse success.

## 9. Boundaries and arithmetic

Strict checks:

- reject storage coordinates outside 0..511 before multiplication;
- compute `512 × Y + X` with checked arithmetic;
- reject index outside the selected array element count;
- do not cast negative coordinates to unsigned values;
- do not wrap 512 to 0;
- do not clamp to map bounds;
- do not use `LocalSize` to reduce storage allocation;
- do not accept a swapped axis merely because it becomes in-bounds.

## 10. Recommended types

```text
OverlayStorageCoordinate
- X
- Y

OverlayCoordinateIndex
- ProfileId
- StorageCoordinate
- ElementIndex
- ElementWidth
- EvidenceGrade
- ConversionTrace

OverlayMapDomainAnalysis
- StorageDomainValid
- ScenarioMapDomainValid
- LocalPlayableDomainValid
- IsoMapCellPresent
- TypeBound
- SemanticValid
- Diagnostics
```

## 11. Canonical ordering

A normalized array view naturally orders cells by element index. That order is not proof of any source writer's iteration order and does not replace preservation of fragment/chunk/compressed provenance.

## 12. Unresolved coordinate questions

The future golden audit must distinguish:

- whether official maps consistently match external row-major indexing;
- whether the EA editor's transpose is purely internal;
- whether any stock map stores meaningful bytes at coordinates outside the active diamond;
- whether map resize in FinalAlert preserves or clears those bytes;
- whether runtime ignores or processes nonempty storage cells with no IsoMap record.