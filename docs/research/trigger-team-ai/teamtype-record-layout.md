# TeamType record layout

> **Source notice:** ChatGPT Web public-source research. No local `ProjectBaseline` access. Not a Codex artifact. No GPL or unclear-license code was copied, translated, mechanically rewritten, or ported. `code_imported: false`.

## 1. Storage shape

TeamTypes are not stored as one CSV value per team.

Common structure:

```ini
[TeamTypes]
0=TeamTypeIdA
1=TeamTypeIdB

[TeamTypeIdA]
Name=...
House=...
TaskForce=...
Script=...
...
```

The list section and per-ID section are separate layers.

## 2. Identities

Preserve:

```text
TeamTypeListKeyRaw
TeamTypeIdRaw
TeamTypeSectionNameRaw
SourceOccurrence
GlobalOrLocalCandidate
DuplicateIdentityGroup
```

The list key is not automatically the TeamType ID. Gaps are not compressed. A missing per-ID section is a dangling definition edge.

## 3. Global and local candidates

Public community documentation states that TeamTypes can be read from `ai(md).ini` and a map file. The `-G` suffix is widely used as a global naming convention but is not sufficient to establish runtime scope by itself.

The graph records:

- source layer;
- global/local candidate;
- exact ID;
- duplicate across layers;
- selected composition policy;
- suppressed definitions.

## 4. Key-value profile

WAE models TeamType as a key-value section and writes standard properties plus configured boolean flags.

Strong candidate fields include:

- `Name`;
- `House`;
- `TaskForce`;
- `Script`;
- `Tag`;
- `Waypoint`;
- `TransportWaypoint`;
- `Group`;
- `VeteranLevel`;
- `Priority`;
- `Max`;
- `TechLevel`;
- `MindControlDecision` candidate;
- extension/profile-defined flags.

The presence, meaning, and version range of each field remain profile-scoped.

## 5. Flag families

Common community/editor flags include candidates such as:

- Loadable;
- Full;
- Annoyance;
- GuardSlower;
- Recruiter;
- Autocreate;
- Prebuild;
- Reinforce;
- Droppod;
- Whiner;
- LooseRecruit;
- Aggressive;
- Suicide;
- OnTransOnly;
- AvoidThreats;
- extension flags.

Do not freeze this list as the vanilla protocol. WAE reads its flag catalog from editor configuration, and extensions can add flags.

## 6. Raw key-value preservation

For each TeamType section preserve:

- every key occurrence;
- raw value;
- duplicate key group;
- physical order;
- unknown keys;
- comments/provenance;
- map-local and global source layers.

Typed views are overlays on the raw section.

## 7. House binding

`HouseRaw` is a reference candidate to a House/HouseType identity.

Do not:

- create a player;
- substitute Neutral;
- bind by display name;
- infer side from the TeamType ID;
- treat `<all>` or `<none>` without a field-specific profile.

## 8. TaskForce and Script binding

```text
TeamType.TaskForceRaw → TaskForce ID candidate
TeamType.ScriptRaw    → ScriptType ID candidate
```

Resolution includes global and map-local definitions. Missing targets remain dangling references and do not delete the TeamType.

## 9. Tag binding

A TeamType Tag field is a Tag reference candidate. It is not a Trigger ID unless a selected profile explicitly says otherwise.

The Tag edge remains separate from:

- placement Tag;
- Trigger linked-trigger field;
- AITrigger primary/secondary Team references.

## 10. Waypoints

`Waypoint` and `TransportWaypoint` may use numeric or alphabetic editor representations and sentinel values.

Preserve raw text and conversion candidates. Do not move a team or validate map reachability during parsing.

## 11. Group

`GroupRaw` may be:

- numeric grouping metadata;
- recruitment grouping candidate;
- extension-defined behavior;
- sentinel such as `-1`.

It is distinct from placement Unit `Group` until a later semantic profile establishes a relationship.

## 12. Veteran level

WAE exposes a candidate `VeteranLevel` and UI interpretations for veteran/elite values. This is editor/reimplementation evidence, not a reason to clamp raw input or create promoted units.

## 13. Boolean encodings

TeamType booleans may appear as:

- `yes/no`;
- `true/false`;
- `0/1`;
- invalid raw values.

The typed view records recognition under a profile. It never rewrites the raw spelling.

## 14. Missing list or section

Distinguish:

- TeamType ID appears in `[TeamTypes]`, section missing;
- section exists but ID absent from list;
- duplicate list entries;
- duplicate per-ID section;
- global and local definitions share an ID;
- TeamType references missing TaskForce or Script.

Each is a separate diagnostic.

## 15. Order

Preserve:

- `[TeamTypes]` physical order;
- numeric key candidate order;
- per-section key order.

Original runtime dependence on list key order versus source order remains unresolved.

## 16. Extension boundary

Ares/Phobos/editor extensions may add TeamType flags or reinterpret existing fields.

Required model:

```text
TeamTypeLayoutProfile
- GameVersion
- ExtensionId
- KnownKeys
- BooleanSpellings
- ReferenceKinds
- DefaultsForEditorOnly
- EvidenceGrade
```

Unknown keys are retained when no extension profile is selected.

## 17. Recommended raw model

```text
TeamTypeRaw
- ListKeyRaw
- IdRaw
- SectionOccurrences[]
- Properties[]
- UnknownProperties[]
- SourceLayers[]

TeamTypeBindingView
- HouseReference
- TaskForceReference
- ScriptReference
- TagReference
- WaypointReferences
- FlagCandidates
- Diagnostics
```

## 18. Execution boundary

Parsing a TeamType does not:

- recruit units;
- instantiate a TaskForce;
- start a Script;
- reserve a transport;
- assign ownership;
- calculate priority;
- enforce TechLevel;
- execute aggression or threat behavior.

Those belong to a future AI/team subsystem.
