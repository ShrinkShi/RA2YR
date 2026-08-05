> **Source notice:** This research was prepared by **ChatGPT Web** from public sources. It did not read the local ProjectBaseline and is not the product of the local Codex Agent. No GPL or unclear-license code was copied, translated, mechanically rewritten, or imported (`code_imported: false`).

# Theater tile registry binding

## Boundary

IsoMap records do not contain TMP filenames, TileSet names, theater extensions, palette names, or variation policy. They carry raw tile and subtile fields that must be resolved against a separately constructed theater registry.

```text
IsoMapRecordRaw.TileFieldRaw32
→ IsoMapTileFieldViews
→ evidence-gated tile interpretation
→ GlobalTileId candidate
→ TheaterTileIdRange
→ TileSetIndex
→ TileIndexInSet
→ TMP logical candidate and variation profile
→ SubTileRaw
→ TMP cell-slot candidate
```

Every transition produces provenance and diagnostics.

## Inputs

The binder consumes:

- a parsed `IsoMapPack5Document`;
- a selected tile-field interpretation profile;
- a typed theater registry produced from composed theater control INIs;
- deterministic global tile-ID ranges;
- a content-resolution service for TMP logical candidates;
- optional parsed TMP metadata for SubTile validation;
- explicit binding limits and compatibility profiles.

It does not scan disks, parse INI, decode MIX, or parse TMP bytes itself.

## Evidence classification

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| Cumulative TileSet global numbering is a stable tool/community convention | `ConfirmedCommunityConvention` | ModEnc LastTilesInSet and editor/tool registry behavior | The convention is well established, but no original-runtime source was reviewed. | Build deterministic cumulative ranges with checked arithmetic. | `NotRun` |
| Missing TMP assets must not shift later global ranges | `DefensiveDesign` | Project preservation policy | This prevents resource availability from changing declared registry identity; it is not a runtime fact claim. | Reserve ranges from registry metadata and diagnose missing TMP separately. | `NotRun` |
| `.ubn → .urb` fallback is a universal vanilla behavior | `ImplementationSpecificBehavior` | WAE/editor compatibility behavior | The observed fallback belongs to a named compatibility path and is not stock-runtime proof. | Keep fallback opt-in and provenance-labeled. | `NotRun` |
| Exact stock-runtime TileSet gap, duplicate, and missing-resource behavior | `Unresolved` | No original-runtime source located | Tool readers differ and may repair or stop enumeration. | Fail closed on ambiguous registry construction. | `NotRun` |

## Global tile-ID ranges

For each typed TileSet:

```text
StartGlobalTileId = sum(TilesInSet of all preceding registered TileSets)
EndExclusive       = StartGlobalTileId + TilesInSet
```

The range exists from registry metadata even if some TMP files are absent.

```text
EvidenceGrade: DefensiveDesign
PolicyClassification: ProjectPolicy
```

- missing TMP assets never shift later ranges;
- missing TileSet sections are diagnosed by registry construction, not compacted away by the binder;
- duplicate normalized TileSet indices make the affected ranges ambiguous;
- allocation uses checked arithmetic;
- a global ID maps to at most one non-ambiguous range.

## Binding result states

```text
IsoMapTileBindingStatus
  Bound
  TileFieldInterpretationAmbiguous
  TileIdOutOfRegistryRange
  TileSetMissingOrAmbiguous
  ReservedTileIdRange
  TmpLogicalCandidateMissing
  TmpCandidateAmbiguous
  TmpParseUnavailable
  SubTileOutOfRange
  SubTileReferencesEmptySlot
  BoundWithEditorCompatibilityFallback
```

A missing TMP is not equivalent to a missing TileSet. A reserved registry range is not equivalent to an out-of-range ID.

## Tile field interpretation

The binder receives explicit candidate views:

- full unsigned/signed 32-bit;
- low unsigned/signed 16-bit;
- retained high 16-bit metadata.

It must not choose whichever candidate happens to resolve successfully. Resolution success is evidence about the current registry, not proof of the binary interpretation.

The result retains:

```text
IsoMapTileResolutionTrace
  RecordOrdinal
  RawTileViews
  InterpretationProfile
  InterpretationEvidenceGrade
  CandidateGlobalTileIds
  RangeCandidates
  SelectedRange?
  SuppressedCandidates
  TmpCandidates
  SubTileValidation
  Diagnostics
```

## TileSet and tile-in-set

For a selected range:

```text
TileIndexInSet = GlobalTileId - StartGlobalTileId
```

The result distinguishes:

- `TileSetIndex` — theater control registry identity;
- `TileIndexInSet` — ordinal inside that TileSet;
- `GlobalTileId` — cumulative map-facing ID;
- `TMP logical candidate` — usually derived from TileSet filename and a 1-based number;
- `TMP variation` — separate resource candidate;
- `SubTileRaw` — cell slot inside the selected TMP.

No field should be overloaded to carry another identity.

## TMP candidates and variation

The registry supplies a named resource-naming profile. A common candidate is:

```text
TileSet.FileName
+ one-based TileIndexInSet formatted with the profile
+ optional variation suffix
+ theater extension
```

Variation selection is outside the raw record. If a runtime or editor uses deterministic or random variations, that policy must be explicit and must not change the global tile ID.

The `.ubn → .urb` fallback documented in WAE remains an explicit editor-compatibility candidate. This dossier does not promote it to vanilla runtime default.

## SubTile validation

After a TMP candidate is selected and metadata is available:

1. calculate slot count using checked `CellsX × CellsY`;
2. verify `SubTileRaw < slotCount`;
3. inspect the referenced TMP offset-table slot;
4. distinguish an empty slot from a missing TMP;
5. retain the original SubTile value on failure.

No clamp to zero, nearest populated slot, or first valid slot is allowed in Core.

## Missing and invalid references

Modern tools may repair invalid references to tile zero. Project Core instead emits structured results and lets an upper compatibility/editor layer choose a repair.

Examples:

- raw tile resolves outside all ranges: `TileIdOutOfRegistryRange`;
- range exists but TMP is missing: `TmpLogicalCandidateMissing`;
- TMP exists but slot is empty: `SubTileReferencesEmptySlot`;
- low16 binds but raw32 does not: interpretation conflict, not automatic low16 selection;
- raw32 binds and low16 binds to a different tile: hard ambiguity.

## Height boundary

The binding result carries `LevelRaw` beside TMP metadata without combining them:

```text
Map LevelRaw
TMP HeightRaw
TMP RampTypeRaw
TMP depth-plane availability
```

A later terrain semantic service may compute corner or movement heights. Rendering may compute pixel offsets. Neither belongs to the binder.

## Palette and LAT boundary

Theater palette and LAT relationships are registry/context inputs for later semantic and rendering adapters. They do not alter the record's tile ID or SubTile.

## Determinism

Binding order is deterministic from serialized keys:

- interpretation profile ID;
- registry version/hash;
- GlobalTileId;
- TileSet index and range;
- normalized resource candidate order;
- variation profile;
- content provider priority;
- SubTile.

Filesystem enumeration order must not influence the winner.

## Security limits

- registry TileSet count;
- cumulative range arithmetic;
- candidates per tile ID;
- TMP candidates per logical name;
- resolution-trace entries;
- diagnostics per document;
- parsed TMP metadata budget.

Budget failures preserve the raw document and return an incomplete binding result, never a fabricated default map.
