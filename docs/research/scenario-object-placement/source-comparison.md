# Source comparison and license boundary

> **Source notice:** ChatGPT Web public-source research only. ProjectBaseline was not read. This is not a Codex Agent artifact. Every implementation listed here is reference-only unless separately approved. No GPL or unclear-license code was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

## Evidence policy

A source can establish only what its category supports:

- official editor source proves editor parsing/writing behavior;
- an independent tool proves that tool's behavior;
- a converter proves its import/export interpretation;
- community documentation records community knowledge;
- none of these automatically proves original game runtime behavior.

Shared XCC/OpenRA/community lineage is not counted as multiple independent runtime witnesses.

## Pinned sources

| Project | Revision | Relevant path(s) | License/category | What it supports | Independence/lineage |
|---|---|---|---|---|---|
| EA FinalSun / FinalAlert 2 | `6abf0f557469baea73079c6bf6550709e2e3584e` | `MissionEditor/MapData.cpp`, `MapData.h`, `Structs.h`, editor data INIs | GPL-3.0-or-later source header; official editor | map-local type lookup, editor field/index behavior, coordinate/editor handling | official editor, not game runtime; includes XCC components |
| World-Altering Editor | `b4c9481e9b00fb0a38739049a046f528b6054ce2` | `src/TSMapEditor/Initialization/MapLoader.cs`, `MapWriter.cs`, `Models/Waypoint.cs` | GPL-3.0-or-later; editor | 17/14/14/12 field profiles, Terrain/Smudge/Waypoint/CellTag writing, reader defaults and repairs | independent editor with CNCMaps/file-format dependencies |
| MapTool | `f85f2226905496139f1258b5854fad915f9bbac6` | `MapTool.Logic/MapObject.cs`, `MapFile.cs` | GPL-2.0-or-later; tool | object field layouts, health comments, mission enum normalization, preview/packed infrastructure | independent tool but community format knowledge may overlap |
| OpenRA | `a520984d91eda9de48a62b1d15c1e3bad0d4fb1a` | legacy import/conversion paths searched | GPL-3.0-or-later; reimplementation/importer | supplementary conversion behavior only; no exact RA2/YR runtime claim | may share community/XCC knowledge |
| CNCMaps | `afb9c1ec118f5128cbc1f3fb5e35c7dfa0e422fb` | map/parser/render object paths searched | repository default MIT with explicit imported-code exceptions; renderer/tool | supplementary map-consumer behavior | some imported OpenRA/XCC-derived files; reference-only |
| CnCNet XNA client | `e6e367bbe04c1a0dc1e34a8fed2856ea3ab7e8c4` | multiplayer map/client paths | GPL-3.0; consumer | consumer-level map metadata/reference behavior where present | client, not runtime parser authority |
| XCC / OmniBlade mirror | pinned in prior research (`6f91bf8b00d3acabb1be765118a37c0cb74e85ec` mirror lineage) | XCC library/editor ancestry | GPL lineage; reference-only | lineage and low-level INI/tool behavior where explicitly located | not independent from EA editor's bundled XCC components |
| Chrono Divide public SDK | `5943c4ae6c19897929d348a417d6d2f1481b75fd` | public map documentation/SDK paths searched | no repository-wide license file previously located; reference-only | implementation-specific extensions only when documented | public reimplementation, not Westwood source |
| ModEnc | permanent/current article URLs for Structures, Units, Infantry, Aircraft, Terrain, Smudge, Waypoints, CellTags, Tags | community wiki | community documentation | field names, 1/256 health, facing conventions, key/cell descriptions | not executable evidence; articles may share historical guides |
| Project Perfect Mod | fixed topic URLs where used | public forum | community discussion | supplementary practices and extension context | not source code/runtime proof |
| RA2 DIY | only stable public pages if available to future work | tutorials/code dictionary | community material | Chinese modding terminology and observations | must be pinned before use; no local code dictionary read here |

## EA official editor findings

The released editor source header identifies it as EA/Matthias Wagner code under GPL-3.0-or-later. It is valuable because it is official editor source, but the repository is not the original RA2/YR executable source.

Relevant observed behavior includes:

- Rules registry lookup followed by map-local registry lookup for BuildingTypes and TerrainTypes;
- editor-internal ranges for map-local types;
- explicit map field structures for placed objects;
- use of a parameter-extraction helper for comma-delimited data;
- internal array/coordinate transformations.

Evidence grade: `ConfirmedByOfficialEditorSource`.

It must never be described as `ConfirmedByOfficialRuntimeSource`.

## WAE findings

WAE declares these common minimum field counts:

```text
Structures: 17
Units:      14
Infantry:   14
Aircraft:   12
```

Its writer labels and emits the fields documented in this dossier. It also writes:

- Terrain key = `Y * 1000 + X`, value = TerrainType;
- CellTags key = `Y * 1000 + X`, value = Tag ID;
- Waypoint value = `Y * 1000 + X`, key = waypoint identifier;
- Smudge value = `SmudgeType,X,Y,0` under sequential keys.

Its reader often uses `Split(... RemoveEmptyEntries)`, clamps health/facing, creates or recovers houses, skips unknown types, and skips out-of-map objects. Those behaviors are classified as editor recovery, not strict format semantics.

Evidence grade for its own behavior: `ConfirmedByIndependentImplementation`.

## MapTool findings

MapTool's `MapObject.cs` independently models the same primary techno layouts and documents health as 1/256 of total health. It:

- removes empty tokens during splitting;
- clamps health to `0..256`;
- parses mission names through an enum after replacing spaces with underscores;
- writes normalized comma records.

This agreement strengthens the field-order candidate but does not strengthen destructive tokenization or normalization into a project requirement.

## ModEnc findings

Community pages document:

- 17-field Structures records for TS/RA2/YR-era maps;
- 14-field Units and Infantry and 12-field Aircraft records;
- health in 1/256 Strength units;
- 256-based facing examples;
- techno record keys as any unique list key;
- Waypoint key significance;
- CellTag key as combined X/Y and value as Tag ID;
- Terrain and Smudge section roles.

Evidence grade: `CommunityDocumented`.

Older pages copied from historical guides are not treated as independent sources from newer ModEnc pages.

## Conflicts and limitations

### Tokenization

- raw format needs empty-token preservation for losslessness;
- WAE and MapTool delete empty tokens;
- official editor parameter helpers may return defaults for missing parameters.

Project policy: preserve all tokens and diagnose profile mismatch.

### Record keys

- public writers use sequential numeric keys;
- ModEnc says any unique string for techno lists;
- Waypoints use meaningful numeric IDs;
- Terrain/CellTags use cell IDs as keys.

Project policy: preserve raw keys and select key semantics per family.

### Health and facing

- community/tool conventions support health 0..256 and 256-based facing;
- tools clamp or normalize;
- no complete official runtime source confirms malformed-value behavior.

Project policy: raw first, explicit interpretation, no clamp.

### Missing owner/type

- WAE may create a house or skip an unknown type;
- a strict reader must preserve unresolved records.

Project policy: binding failure is not raw parse failure.

### Mission

- tools parse named strings and normalize spacing;
- full runtime mission semantics are not established.

Project policy: opaque recognized/unknown token only.

## License isolation

Forbidden:

- copying source bodies;
- line-by-line translation;
- mechanically converting C++ or C# control flow into project C#;
- reproducing class organization or error paths as a near-structural port;
- generating pseudo-code that mirrors a GPL implementation;
- importing fixtures derived from proprietary or GPL map contents without approval.

Allowed:

- field-index facts;
- key/value shape facts;
- state-machine design principles;
- error and budget contracts;
- independently designed synthetic fixtures;
- aggregate interoperability observations.

## Source record template

Every future source addition should record:

```text
project
url
commit_or_revision
path
license
category: reader|writer|editor|runtime|consumer|documentation
independence
shared_lineage
reference_only: true
code_imported: false
supported_profiles
unsupported_claims
```
