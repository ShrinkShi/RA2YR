# Placement family and section boundaries

> **Source notice:** ChatGPT Web public-source research only. ProjectBaseline was not read. This is not a Codex Agent artifact. No GPL or unclear-license code was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

## Why the families must remain separate

The eight primary sections share an INI surface but do not share one record contract. A single `MapObjectCsv` abstraction would erase whether identity belongs to the key, the value, a coordinate, or a registry reference.

| Family | Key role candidate | Value shape candidate | Placement coordinate location | Primary binding |
|---|---|---|---|---|
| Structures | arbitrary/ordinal record key | comma-token record | separate X and Y tokens | BuildingType + House |
| Units | arbitrary/ordinal record key | comma-token record | separate X and Y tokens | VehicleType + House |
| Infantry | arbitrary/ordinal record key | comma-token record | X, Y, and infantry subcell tokens | InfantryType + House |
| Aircraft | arbitrary/ordinal record key | comma-token record | separate X and Y tokens | AircraftType + House |
| Terrain | scenario-cell ID | usually one logical type token | key | TerrainType |
| Smudge | ordinal/arbitrary record key | type, X, Y, data candidate | value tokens | SmudgeType |
| Waypoints | waypoint identity | scenario-cell ID | value | reference target only |
| CellTags | scenario-cell ID | Tag identity | key | opaque Tag edge |

These are independent reader profiles, even when a future implementation shares low-level token or numeric utilities.

## Lossless section occurrence collection

The input to every family is a collection of physical occurrences rather than a dictionary:

```text
ScenarioPlacementSectionOccurrence
- SectionNameRaw
- NormalizedSectionNameCandidate
- SectionOccurrenceIndex
- SourceSpan
- PhysicalOrder
- Records[]
```

Duplicate sections must remain visible. A semantic view may later select or combine them through an explicit policy, but the lossless document cannot silently merge them.

## Raw record contract

Every key/value entry first becomes:

```text
ScenarioPlacementRecordRaw
- SectionFamily
- KeyRaw
- ValueRaw
- SourceLine
- SourceOccurrence
- PhysicalOrder
- LeadingWhitespaceRaw
- KeyWhitespaceRaw
- ValueWhitespaceRaw
- InlineCommentRawCandidate
- LineEndingRaw
```

The parser must not assume that a semicolon is always an inline comment inside a placement value until the lossless INI grammar has classified it. No placement family introduces quoting unless an explicit extension profile proves that behavior.

## Section-specific layout profiles

A record layout profile identifies:

- expected minimum and canonical field counts;
- optional and extension fields;
- field indices and candidate meanings;
- raw numeric signedness candidates;
- accepted sentinels;
- whether the key is identity, ordinal, cell ID, or opaque text;
- applicable games and tools;
- evidence grade.

Suggested profile names:

- `Ra2YrStructure17`;
- `Ra2YrUnit14`;
- `Ra2YrInfantry14`;
- `Ra2YrAircraft12`;
- `ScenarioTerrainKeyCell`;
- `ScenarioSmudgeTypeXYData`;
- `ScenarioWaypointKeyIdentityValueCell`;
- `ScenarioCellTagKeyCellValueTag`;
- explicit TS, editor, Ares, Phobos, or unknown-tail profiles when supported.

Profiles are never selected by asking which interpretation produces the most valid objects.

## Record-key boundaries

The left-hand key has different meaning by family:

- public editors commonly write `0,1,2...` for Structures, Units, Infantry, Aircraft, and Smudge;
- ModEnc documents techno list keys as any unique string, which weakens the claim that the numeric key is a runtime object ID;
- WAE writes `Y * 1000 + X` as the key for Terrain and CellTags;
- WAE writes the waypoint identifier as the Waypoints key and a cell ID as its value.

Core must always retain:

```text
ScenarioRecordKey
- KeyRaw
- NumericKeyCandidate
- CanonicalDecimalCandidate
- SourceOccurrenceOrder
- DuplicateKeyGroup
- NormalizedCollisionGroup
```

`1` and `01` are distinct raw keys but collide under a decimal-normalization policy. That collision is diagnostic; it is not permission to overwrite one record.

## Duplicate and ordering boundaries

For every section, distinguish:

- duplicate raw key;
- duplicate normalized numeric key;
- different keys with byte-identical values;
- different keys that place objects at the same cell;
- source-order changes;
- numeric-order changes;
- canonical editor renumbering.

The strict default preserves all entries. It does not apply first-wins or last-wins. A later consistency analyzer may form groups such as:

- `DuplicateIdentity`;
- `ByteIdenticalDuplicate`;
- `CellConflictCandidate`;
- `LegalCooccupancyCandidate`;
- `UnresolvedOverlap`.

## Family-specific parse success

Parse success is not the same as binding success. Each record receives independent statuses:

1. raw line captured;
2. tokenization completed;
3. layout profile matched;
4. numeric fields parsed;
5. coordinate interpreted;
6. map-domain checks completed;
7. owner binding attempted;
8. type binding attempted;
9. opaque references resolved or left dangling;
10. typed descriptor available.

A syntactically valid record with an unknown type or owner remains a preserved raw record. A record outside the map domain can still be syntactically valid and coordinate-decodable.

## Cross-family relations are not inheritance

The following similarities do not justify a shared serialized base layout:

- Structures and mobile objects all have owner/type/health/X/Y fields;
- Units and Aircraft share many mobile-state fields;
- Terrain and CellTags both use a cell-like key;
- Waypoints and CellTags both point at scenario cells.

Shared semantic interfaces may exist after parsing, for example `IScenarioLocatedPlacement`, but raw family models must retain their exact key/value structure and layout profile.

## Extensions

Ares, Phobos, editor metadata, or map tools may add fields or sections. Extension handling must:

- be enabled by an explicit profile;
- preserve all unknown trailing tokens when disabled;
- never shift known field indices merely because empty tokens were removed;
- record the extension source and evidence grade;
- avoid presenting extension behavior as vanilla RA2/YR.

## Non-authoritative consumers

FinalAlert, WAE, MapTool, CNCMaps, CnCNet clients, converters, and future Unity adapters may normalize, repair, skip, or fabricate records. Their behavior is evidence about those consumers, not automatically the original game runtime contract.
