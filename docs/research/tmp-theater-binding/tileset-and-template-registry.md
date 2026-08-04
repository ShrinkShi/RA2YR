# TileSet and template registry

## 1. Identity layers

The project must keep these identities separate:

1. `TileSetIndex` — numeric suffix of `[TileSet####]`;
2. `TileIndexInSet` — zero-based position inside one TileSet;
3. `GlobalTileId` — cumulative map-facing tile number;
4. `TmpLogicalName` — resolved TMP asset name;
5. `TmpCellIndex` — cell slot inside a multi-cell TMP file;
6. map `SubTile` — selected TMP cell slot in an IsoMap record.

No reader should collapse them into a single integer.

## 2. Cumulative global tile IDs

The strongest registry model is:

```text
start(TileSet0) = 0
start(TileSetN) = sum(TilesInSet for all earlier resolved TileSets)
GlobalTileId = start(TileSet) + TileIndexInSet
```

`TilesInSet` reserves the ID range. A missing TMP file does not shift later TileSet ranges.

WAE follows this cumulative model and increments the global tile ID even when a primary TMP asset is absent and represented by an empty placeholder.

## 3. Deterministic section ordering

Registry order is numeric `TileSetIndex`, independent of INI enumeration order.

The typed registry must diagnose:

- duplicate normalized index;
- gaps;
- negative or overflowed parsed values;
- `TilesInSet < 0`;
- cumulative ID overflow;
- special-set references to missing ranges;
- multiple effective sections produced by an ambiguous composition policy.

It must not choose a duplicate winner by file length, section order, or hash.

## 4. TMP filename candidate

A common filename construction is:

```text
baseName = FileName + decimal(TileIndexInSet + 1, width 2)
primary  = baseName + theater.TmpExtension
```

Variation candidates commonly append letters:

```text
baseName + "a" + extension
baseName + "b" + extension
...
```

WAE scans a bounded `a..f` family. This is useful implementation evidence, not proof that the original runtime supports exactly that range.

The project model should retain:

```text
TmpAssetCandidate
- TheaterProfile
- TileSetIndex
- TileIndexInSet
- GlobalTileId
- FileStem
- VariationId
- Extension
- Provider provenance
- Priority key
- Presence/status
```

## 5. Missing and duplicate assets

Missing assets are registry-binding diagnostics, not reasons to renumber:

- `MissingPrimaryTmp`
- `MissingAllVariations`
- `FallbackExtensionCandidate`
- `AmbiguousCaseVariant`
- `MultipleProvidersSameLogicalName`
- `TmpHeaderTileGridMismatch`

Content precedence decides which candidate file is visible. The registry still retains the full suppressed candidate chain.

## 6. Variation semantics

Variations can carry distinct TMP metadata, not merely different pixels. Therefore each variation must be parsed and bound independently.

Do not assume:

- identical ramp/terrain/height metadata;
- identical flags;
- identical cell count;
- identical palette usage;
- one variation can substitute for another during parsing.

Selection/randomization belongs to simulation or rendering policy.

## 7. Multi-cell TMP files

A TMP file has `blocksX × blocksY` cell slots. A slot offset of zero is the strongest empty-slot candidate.

Binding must validate:

- map `SubTile` is within the TMP slot range;
- selected slot is non-empty where required;
- the TileSet's `Only1x1` or similar editor classification is not mistaken for a file-format constraint;
- per-cell coordinates and heights remain local to the template.

## 8. TileSet semantic links

Fields such as these link TileSets, not TMP bytes:

- `Morphable`;
- `MarbleMadness`;
- `NonMarbleMadness`;
- `AllowTiberium`;
- special `[General]` references;
- LAT ground and transition sets.

A registry resolver returns explicit edges and ambiguity diagnostics. It does not alter TMP metadata to make a link succeed.

## 9. Recommended models

```text
TheaterTileSetDescriptor
- Index
- SourceSectionProvenance
- SetNameRaw
- FileNameRaw
- TilesInSetRaw
- MorphableRaw
- MarbleMadnessRaw
- NonMarbleMadnessRaw
- UnknownEntries

TheaterTileIdRange
- TileSetIndex
- StartInclusive
- Count
- EndExclusive

TheaterTileRegistry
- OrderedTileSets
- IdRanges
- SpecialRoleBindings
- Diagnostics
- CanonicalHash
```

## 10. Evidence status

| Claim | Status |
|---|---|
| cumulative global IDs | `ConfirmedByOfficialToolAndReimplementation` |
| `TilesInSet` reserves ranges | strong implementation/community evidence |
| filename uses 1-based two-digit number | `ConfirmedCommunityConvention` |
| variations are `a..f` | WAE implementation-specific |
| missing file shifts later IDs | rejected by recommended model |
| gap handling in original runtime | `Unresolved` |
