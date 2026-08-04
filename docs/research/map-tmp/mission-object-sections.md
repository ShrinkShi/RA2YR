# Map objects, houses and mission scripting sections

## 1. Two-stage model

Mission and placement sections should be handled in two stages:

1. parse lossless section-specific records;
2. build a reference graph without executing it.

A malformed reference must not cause the original record to disappear.

## 2. Waypoints and cell numbers

`[Waypoints]` commonly maps an identifier to a decimal packed cell number:

```text
cell = y × 1000 + x
x = cell mod 1000
y = cell / 1000
```

The same decimal cell convention appears in terrain-object and cell-tag keys.

Required model:

- raw key/value;
- parsed waypoint identifier candidate;
- raw packed-cell integer;
- derived X/Y candidate;
- range/overflow diagnostics;
- source occurrence.

Do not assume identifiers 0..7 are always multiplayer starts at the format layer; that is a typed scenario policy.

## 3. Object placements

Common sections:

- `[Structures]`
- `[Units]`
- `[Infantry]`
- `[Aircraft]`
- `[Terrain]`
- `[Smudge]`

Records may include owner, type ID, health, X/Y, facing, mission, tag, veterancy, group, upgrades and role-specific flags.

The parser should retain positional fields even when empty or extra. Typed validation is performed against an effective Rules/Art view later.

## 4. Houses and countries

RA2/YR maps can contain:

- `[Countries]` registry candidates;
- `[Houses]` instances/registry;
- one section per country/house identifier;
- object owner references;
- trigger/event/action house parameters.

Keep distinct:

- globally defined country/house type;
- map-local override of a global type;
- scenario house instance;
- textual owner reference.

Case normalization and aliases must be explicit and provenance-preserving.

## 5. Trigger graph

The core trigger family includes:

- `[Triggers]`
- `[Events]`
- `[Actions]`
- `[Tags]`
- `[CellTags]`
- attached tag fields on map objects.

Suggested identities:

```text
TriggerId
EventListId
ActionListId
TagId
CellCoordinate
```

Do not infer identity from display names. Preserve duplicate IDs as ambiguous groups rather than dictionary overwrite.

## 6. AI/team graph

The team/AI family includes:

- `[TeamTypes]` plus per-team sections;
- `[TaskForces]` plus per-task-force sections;
- `[ScriptTypes]` plus per-script sections;
- `[AITriggerTypes]`;
- `[AITriggerTypesEnable]`;
- house and waypoint references.

Graph building should classify:

- unique resolved edge;
- missing target;
- duplicate/ambiguous target;
- target of wrong kind;
- unsupported opcode with preserved raw parameters;
- cycle where the semantic layer forbids one.

## 7. Event/action opcode boundary

Opcode numbers and parameter schemas are game/profile data. The lossless parser retains raw comma fields and opcode candidate.

A later registry may provide:

- known RA2 schema;
- known YR schema;
- Ares/Phobos extension schema;
- unsupported-but-preserved schema.

Chrono Divide's public support table is reimplementation evidence about its own executor. It is not a complete vanilla opcode specification.

## 8. Record ordering and numeric keys

Editors often regenerate numeric list keys and reorder registries. Source key order can affect references when other fields use ordinal positions.

Default reader behavior:

- preserve every occurrence and original key;
- expose numeric-key candidate separately;
- diagnose duplicate numeric keys;
- never renumber during read;
- require an explicit writer profile for compaction/renumbering;
- update every dependent reference transactionally if an editor intentionally regenerates IDs.

## 9. Unknown fields

A record with extra fields may reflect:

- YR versus RA2 schema;
- editor metadata;
- engine extension;
- future mod logic;
- corruption.

Do not truncate it to the currently known field count. Preserve the tail and classify semantics as unresolved.

## 10. Safe graph result

```text
MapScenarioGraphResult
- Registries
- RawRecords
- ResolvedEdges
- MissingEdges
- AmbiguousEdges
- UnsupportedOpcodeRecords
- Diagnostics
- CanonicalGraphHash
```

The public audit hash may cover identities and aggregate edge status, but must not expose mission text or full parameter lists.

## 11. Forbidden behavior

Do not:

- execute triggers while parsing;
- instantiate simulation objects;
- discard records with unknown types/opcodes;
- silently create missing houses, tags or teams;
- select the first duplicate ID;
- remove editor-specific fields during read;
- renumber keys without updating all references;
- infer coordinate systems from display position.
