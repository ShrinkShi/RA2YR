# Object record field layouts

> **Source notice:** ChatGPT Web public-source research only. ProjectBaseline was not read. This is not a Codex Agent artifact. GPL and unclear-license code was not copied, translated, mechanically rewritten, or ported (`code_imported: false`).

## Raw token model

Before any layout is selected, every comma-delimited candidate value is represented as:

```text
ScenarioPlacementTokenRaw
- RawTokenIndex
- RawTokenText
- LeadingWhitespaceRaw
- TrailingWhitespaceRaw
- EmptyToken
- ParsedIntegerCandidate
- ParsedBooleanCandidate
- SourceSpan
```

`RawValueText` remains the source of truth. Tokenization must preserve consecutive commas, a trailing comma, spaces, empty values, unknown fields, and numeric text that fails parsing.

Public WAE and MapTool readers use `Split(... RemoveEmptyEntries)` for several object families. That behavior shifts later fields when an empty token is present and is therefore classified as lenient/destructive tool behavior, not the project token contract.

## Structure candidate: 17 fields

The strongest RA2/YR editor/community candidate is:

| Index | Raw candidate | Common name | Notes |
|---:|---|---|---|
| 0 | string | Owner | House identity token |
| 1 | string | Type | BuildingType logical ID |
| 2 | integer text | Health | commonly documented as 1/256 Strength units |
| 3 | integer text | X | scenario cell X |
| 4 | integer text | Y | scenario cell Y |
| 5 | integer text | Facing | 256-based candidate |
| 6 | string | Tag | Tag ID or none sentinel |
| 7 | boolean/integer text | AISellable | editor/gameplay state candidate |
| 8 | boolean/integer text | AIRebuildable | WAE loader calls it leftover but writer preserves it |
| 9 | boolean/integer text | PoweredOn | building state candidate |
| 10 | integer text | UpgradeCount | count candidate |
| 11 | integer text | Spotlight | spotlight mode candidate |
| 12 | string | Upgrade1 | BuildingType reference candidate |
| 13 | string | Upgrade2 | BuildingType reference candidate |
| 14 | string | Upgrade3 | BuildingType reference candidate |
| 15 | boolean/integer text | AIRepairable | candidate |
| 16 | boolean/integer text | Nominal | candidate |
| 17+ | raw | ExtensionTail | preserved, not shifted or discarded |

WAE writer and MapTool agree on this 17-field order. ModEnc documents the same RA2/YR shape. WAE requires at least 17 non-empty tokens and then clamps/defaults values; MapTool behaves similarly. These are implementation behaviors, not permission to erase empties or repair invalid source.

## Vehicle/Unit candidate: 14 fields

| Index | Raw candidate | Common name | Notes |
|---:|---|---|---|
| 0 | string | Owner | House identity |
| 1 | string | Type | VehicleType logical ID |
| 2 | integer text | Health | raw first |
| 3 | integer text | X | scenario cell X |
| 4 | integer text | Y | scenario cell Y |
| 5 | integer text | Facing | body-facing candidate |
| 6 | string | Mission | mission token, not executed here |
| 7 | string | Tag | opaque Tag reference |
| 8 | integer text | Veterancy | raw experience candidate |
| 9 | integer text | Group | group candidate, often `-1` sentinel |
| 10 | boolean/integer text | High | bridge/high-state candidate |
| 11 | integer text | FollowsIndex | ambiguous source-order/record-index reference |
| 12 | boolean/integer text | AutocreateNoRecruitable | team-recruitment candidate |
| 13 | boolean/integer text | AutocreateYesRecruitable | team-recruitment candidate |
| 14+ | raw | ExtensionTail | preserve |

WAE serializes `FollowsIndex` using the current in-memory Units list index while writing keys sequentially. This strongly supports an editor-list-index interpretation for that writer, but does not prove that arbitrary raw keys are runtime object identities.

## Infantry candidate: 14 fields

| Index | Raw candidate | Common name | Notes |
|---:|---|---|---|
| 0 | string | Owner | House identity |
| 1 | string | Type | InfantryType logical ID |
| 2 | integer text | Health | raw first |
| 3 | integer text | X | scenario cell X |
| 4 | integer text | Y | scenario cell Y |
| 5 | integer text | SubCell | infantry occupancy slot, not TMP/IsoMap subtile |
| 6 | string | Mission | raw mission token |
| 7 | integer text | Facing | raw body-facing candidate |
| 8 | string | Tag | opaque Tag reference |
| 9 | integer text | Veterancy | raw |
| 10 | integer text | Group | raw/sentinel candidate |
| 11 | boolean/integer text | High | bridge/high-state candidate |
| 12 | boolean/integer text | AutocreateNoRecruitable | candidate |
| 13 | boolean/integer text | AutocreateYesRecruitable | candidate |
| 14+ | raw | ExtensionTail | preserve |

The position of `Mission` and `Facing` differs from Unit/Aircraft. A generic mobile-object CSV structure would misparse infantry.

## Aircraft candidate: 12 fields

| Index | Raw candidate | Common name | Notes |
|---:|---|---|---|
| 0 | string | Owner | House identity |
| 1 | string | Type | AircraftType logical ID |
| 2 | integer text | Health | raw first |
| 3 | integer text | X | scenario cell X |
| 4 | integer text | Y | scenario cell Y |
| 5 | integer text | Facing | candidate |
| 6 | string | Mission | raw mission token |
| 7 | string | Tag | opaque Tag reference |
| 8 | integer text | Veterancy | raw |
| 9 | integer text | Group | raw/sentinel candidate |
| 10 | boolean/integer text | AutocreateNoRecruitable | candidate |
| 11 | boolean/integer text | AutocreateYesRecruitable | candidate |
| 12+ | raw | ExtensionTail | preserve |

Aircraft does not contain the Unit `High` and `FollowsIndex` positions in the common 12-field profile. Treating Unit and Aircraft as identical would shift the final flags.

## Terrain candidate

```ini
[Terrain]
ScenarioCellId=TerrainTypeId
```

WAE writer uses `Y * 1000 + X` as the key and the logical TerrainType name as the value. Its loader decodes the last three decimal digits as X and the leading digits as Y, then looks up the logical type in composed Rules. This is editor evidence; raw key/value preservation remains mandatory.

## Smudge candidate

WAE writer emits:

```ini
[Smudge]
Ordinal=SmudgeTypeId,X,Y,0
```

The fourth field is commonly called data, unused, or zero by tools. The format-level model must call it `SmudgeField3Raw` until a profile supplies semantics. Smudge records are not equivalent to Terrain records because the coordinate is in the value and the key is list-like.

## Waypoint candidate

```ini
[Waypoints]
WaypointId=ScenarioCellId
```

WAE writes `ScenarioCellId = Y * 1000 + X`. The key is a waypoint slot/identity and can be referenced by scenario logic. It must not be renumbered merely to remove gaps.

## CellTag candidate

```ini
[CellTags]
ScenarioCellId=TagId
```

The key is a cell coordinate encoding, while the value is an opaque Tag identity. This is distinct from a Tag field embedded in a techno record.

## Minimum counts versus exact counts

A profile may define a canonical field count, but Core should report separately:

- `FieldCountBelowProfileMinimum`;
- `FieldCountMatchesCanonical`;
- `UnknownTrailingFieldsPresent`;
- `EmptyKnownFieldPresent`;
- `AmbiguousExtensionLayout`.

A missing field is not silently filled with the default used by FinalAlert, WAE, or MapTool. A record with extra fields remains parseable as raw data even when semantic interpretation is incomplete.

## Quoting and comments

No load-bearing source established a quoted CSV grammar for these sections. The strict default therefore treats commas as separators after the lossless INI layer has established the value span, while preserving all source text. A future extension profile may add quoting only with explicit evidence.

Semicolons are handled by the lossless INI layer. The placement tokenizer must not independently truncate at semicolons and thereby destroy a value or comment boundary.
