# Multiplayer settings and start locations

> **Source notice:** ChatGPT Web public-source research. ProjectBaseline was not read. `code_imported: false`.

## Layer separation

Keep separate:

```text
map-authored metadata
Rules multiplayer-dialog defaults
game-mode overrides
client defaults
lobby/session selections
spawn/runtime state
```

Identical key names do not imply common provenance or precedence.

## Multiplayer settings

Common candidates include player-count hints, money, unit count, TechLevel, Bases, crates, shroud/fog, ShortGame, superweapons, allied building, MCV redeploy, bridge destruction, AI settings and extension/client fields. Applicability varies by product and consumer.

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| CnCNet separates player/AI rows, side, color, start, lobby team and effective options | `ImplementationSpecificBehavior` | CnCNet XNA client | Client/session behavior, not immutable map metadata. | Keep outside Core authored state. | `NotRun` |
| Common multiplayer-dialog fields and low-numbered start-Waypoint convention | `ConfirmedCommunityConvention` | Community/client/editor documentation | Convention does not establish complete runtime precedence. | Explicit consumer profile. | `NotRun` |
| A map field, Rules default, mode file and lobby value with the same name share one runtime precedence | `ConflictingSources` | Editors, clients and launch contexts | Layers directly differ. | Preserve source kind/provenance. | `NotRun` |
| Exact stock runtime precedence and start assignment | `Unresolved` | No original-runtime source located | No reliable complete session algorithm. | Future session adapter. | `NotRun` |
| No synthetic starts/players/MCVs, no clamp and no lobby-to-map rewrite | `DefensiveDesign` | Project policy | Preservation and architecture boundary. | Report candidates/diagnostics only. | `NotRun` |

## Distinct state sources

- House Credits, carry-over money, dialog default money, lobby money and runtime credits;
- House TechLevel, Rules type TechLevel, mode/lobby TechLevel and runtime availability;
- map placements, starting-unit options, base nodes, campaign reinforcements and generated session units;
- House Allies, lobby team number and runtime diplomacy;
- authored player candidates, lobby player rows, AI rows, observers and network peers.

None are merged during parsing.

## Start-location model

```text
ScenarioStartLocationRaw
- WaypointIdRaw
- CellIdRaw
- SourceOccurrence

ScenarioStartLocationCandidate
- SlotCandidate
- CoordinateCandidate
- SizeStatus
- LocalSizeStatus
- IsoMapPresence
- ConsumerProfile
- EvidenceGrade
```

Low-numbered Waypoints are start candidates under a selected profile, not universal slot identities. Campaign/script Waypoints, extensions and client-removal/replacement behavior remain possible.

Missing, duplicate, shared-cell, out-of-domain or IsoMap-missing starts remain raw. The parser does not generate or move Waypoints, select random starts, assign players, infer observers, create AI slots or launch a session.

## Player-count evidence

`Basic.MinPlayer/MaxPlayer`, start-Waypoint count, House count, mode/client limits and launcher metadata remain separate evidence. No maximum is inferred from House count or start count alone.

## Roundtrip

Preserve authored settings, unknown fields, Waypoint IDs/cells, duplicates, missing/invalid starts and source-layer provenance. Transient lobby selections are not written into the map unless an explicit spawn/session output profile is selected.
