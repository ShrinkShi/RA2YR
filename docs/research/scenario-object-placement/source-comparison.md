# Source comparison and license boundary

> **Source notice:** ChatGPT Web public-source research only. ProjectBaseline was not read. This is not a Codex Agent artifact. Every implementation listed here is reference-only unless separately approved. No GPL or unclear-license code was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

## Evidence policy

A source can establish only what its category supports:

- original-runtime source can support `ConfirmedByOriginalRuntimeSource`;
- official editor/tool source supports `ConfirmedByOfficialToolSource` for that tool's behavior;
- a named tool, writer, reader, converter, client, or reimplementation supports `ImplementationSpecificBehavior`;
- stable community documentation supports `ConfirmedCommunityConvention`;
- cross-tool convergence without proven independent lineage remains `Underconfirmed`;
- direct disagreement is `ConflictingSources`;
- project preservation/fail-closed choices are `DefensiveDesign`;
- unsupported runtime semantics remain `Unresolved`.

Shared XCC/OpenRA/community lineage is not counted as multiple independent runtime witnesses. This dossier currently uses neither `ConfirmedByOriginalRuntimeSource` nor `ConfirmedByMultipleIndependentImplementations` for placement claims.

## Pinned sources

| Project | Revision | Relevant path(s) | License/category | What it supports | Independence/lineage |
|---|---|---|---|---|---|
| EA FinalSun / FinalAlert 2 | `6abf0f557469baea73079c6bf6550709e2e3584e` | `MissionEditor/MapData.cpp`, `MapData.h`, `Structs.h`, editor data INIs | GPL-3.0-or-later source header; official editor | map-local type lookup, editor field/index behavior, coordinate/editor handling | official editor, not game runtime; includes XCC components |
| World-Altering Editor | `b4c9481e9b00fb0a38739049a046f528b6054ce2` | `src/TSMapEditor/Initialization/MapLoader.cs`, `MapWriter.cs`, `Models/Waypoint.cs` | GPL-3.0-or-later; editor | 17/14/14/12 field profiles, Terrain/Smudge/Waypoint/CellTag writing, reader defaults and repairs | editor with CNCMaps/file-format dependencies and community knowledge |
| MapTool | `f85f2226905496139f1258b5854fad915f9bbac6` | `MapTool.Logic/MapObject.cs`, `MapFile.cs` | GPL-2.0-or-later; tool | object field layouts, health comments, mission enum normalization, preview/packed infrastructure | tool; community format knowledge may overlap |
| OpenRA | `a520984d91eda9de48a62b1d15c1e3bad0d4fb1a` | legacy import/conversion paths searched | GPL-3.0-or-later; reimplementation/importer | supplementary conversion behavior only | may share community/XCC knowledge; no runtime claim |
| CNCMaps | `afb9c1ec118f5128cbc1f3fb5e35c7dfa0e422fb` | map/parser/render object paths searched | repository default MIT with explicit imported-code exceptions | supplementary map-consumer behavior | some imported OpenRA/XCC-derived files; reference-only |
| CnCNet XNA client | `e6e367bbe04c1a0dc1e34a8fed2856ea3ab7e8c4` | multiplayer map/client paths | GPL-3.0; consumer | consumer-level map metadata/reference behavior | client, not runtime parser authority |
| XCC / OmniBlade mirror | `6f91bf8b00d3acabb1be765118a37c0cb74e85ec` lineage | XCC library/editor ancestry | GPL lineage; reference-only | lineage and low-level INI/tool behavior where located | not independent from EA editor's bundled XCC components |
| Chrono Divide public SDK | `5943c4ae6c19897929d348a417d6d2f1481b75fd` | public map documentation/SDK paths searched | repository-specific/unclear; reference-only | implementation-specific extensions only when documented | public reimplementation, not Westwood source |
| ModEnc | permanent/current articles for Structures, Units, Infantry, Aircraft, Terrain, Smudge, Waypoints, CellTags, Tags | community wiki | community documentation | field names, 1/256 health, facing conventions, key/cell descriptions | articles may share historical guides |
| Project Perfect Mod | fixed topic URLs where used | public forum | community discussion | supplementary practices and extension context | not source code/runtime proof |
| RA2 DIY | only pinned stable public pages | tutorials/community | community material | Chinese modding terminology and observations | no local code dictionary read here |

## EA official editor findings

The released editor source header identifies it as EA/Matthias Wagner code under GPL-3.0-or-later. It is official editor source, not the original RA2/YR executable source.

Relevant observed behavior includes:

- Rules registry lookup followed by map-local registry lookup for BuildingTypes and TerrainTypes;
- editor-internal ranges for map-local types;
- explicit map field structures for placed objects;
- use of a parameter-extraction helper for comma-delimited data;
- internal array/coordinate transformations.

```text
EvidenceGrade: ConfirmedByOfficialToolSource
Source: EA FinalSun / FinalAlert 2
AuditStatus: NotRun
```

## WAE findings

WAE declares these common minimum field counts:

```text
Structures: 17
Units:      14
Infantry:   14
Aircraft:   12
```

Its writer also emits Terrain key = `Y * 1000 + X`, CellTags key = the same cell form, Waypoint value = the same cell form, and Smudge value = `SmudgeType,X,Y,0` under sequential keys.

Its reader often removes empty entries, clamps health/facing, creates or recovers houses, skips unknown types, and skips out-of-map objects. Those are editor recovery choices, not strict parser semantics.

```text
EvidenceGrade: ImplementationSpecificBehavior
Source: World-Altering Editor
AuditStatus: NotRun
```

## MapTool findings

MapTool models the same primary techno layouts and documents health as 1/256 of total health. It removes empty tokens, clamps health, normalizes mission strings and rewrites comma records. This is `ImplementationSpecificBehavior` for MapTool. Agreement with WAE makes the common field order `Underconfirmed`, not independently proven runtime behavior.

## ModEnc findings

Community pages document the common field counts, health in 1/256 Strength units, 256-based facing examples, techno record key conventions, Waypoint key significance, CellTag cell-key behavior, and Terrain/Smudge roles.

```text
EvidenceGrade: ConfirmedCommunityConvention
Source: ModEnc fixed/current pages
AuditStatus: NotRun
```

Older pages copied from historical guides are not treated as independent sources from newer pages.

## Conflicts and limitations

### Tokenization

- Core lossless handling requires empty-token preservation;
- WAE and MapTool delete empty tokens;
- official editor parameter helpers may return defaults for missing parameters.

This source disagreement is `ConflictingSources`; project raw preservation is `DefensiveDesign`.

### Record keys

- public writers use sequential numeric keys;
- community documentation allows any unique string for techno lists;
- Waypoints use meaningful numeric IDs;
- Terrain/CellTags use cell IDs as keys.

Key semantics remain family-specific. Core preserves raw keys.

### Health and facing

- community/tool conventions support health 0..256 and 256-based facing;
- tools clamp or normalize;
- no original-runtime source confirms malformed-value behavior.

The normal interpretation is `Underconfirmed`; malformed behavior is `Unresolved`; no-clamp raw preservation is `DefensiveDesign`.

### Missing owner/type

WAE may create a house or skip an unknown type. Strict Core preserves unresolved records. WAE's behavior is `ImplementationSpecificBehavior`; project preservation is `DefensiveDesign`.

### Mission

Tools parse named strings and normalize spacing, but full runtime mission semantics are not established. Tool behavior is implementation-specific; the runtime state machine remains unresolved.

## Normalized evidence summary

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert map-local type and placement editor behavior | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Official tool only. | Source-specific profile. | `NotRun` |
| WAE and MapTool parsing/writing/defaulting | `ImplementationSpecificBehavior` | Named tools | Each tool recorded separately. | Do not inherit destructive recovery. | `NotRun` |
| Common 17/14/14/12 layouts and 1/256 health convention | `Underconfirmed` | Tools plus ModEnc | Convergence does not prove independent lineage or runtime strictness. | Explicit layout/state profiles. | `NotRun` |
| Long-standing field/key descriptions | `ConfirmedCommunityConvention` | ModEnc | Convention only. | Preserve applicability/provenance. | `NotRun` |
| Empty-token/default/unknown-owner handling | `ConflictingSources` | Official editor and public tools | Sources differ directly. | Raw-first fail-closed policy. | `NotRun` |
| Exact malformed-value, key identity, mission and runtime object-state behavior | `Unresolved` | No original-runtime source located | No reliable unique candidate. | Simulation adapter remains separate. | `NotRun` |
| Duplicate/raw/unknown-tail preservation | `DefensiveDesign` | Project policy | Preservation design. | No repair, clamp, deletion or last-wins. | `NotRun` |

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
