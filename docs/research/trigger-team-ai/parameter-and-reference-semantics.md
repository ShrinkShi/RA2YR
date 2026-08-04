# Parameter and reference semantics

> **Source notice:** ChatGPT Web public-source research. No local `ProjectBaseline` access. Not a Codex artifact. No GPL or unclear-license code was copied, translated, mechanically rewritten, or ported. `code_imported: false`.

## 1. Raw-first rule

Every Event, Action, Script, TeamType, and AITrigger parameter begins as raw text.

```text
ScenarioParameterRaw
- RawText
- TokenIndex
- SourceRecord
- SourceLine
- EmptyState
- WhitespaceState
- NumericCandidates[]
- StringCandidate
- ReferenceCandidates[]
- SelectedInterpretation?
- EvidenceGrade
```

Derived interpretation never replaces raw text.

## 2. Candidate parameter kinds

A slot may be a candidate for:

- signed integer;
- unsigned integer;
- floating-point decimal;
- raw string;
- boolean;
- difficulty mask;
- duration/timer;
- credits or quantity;
- percentage/fixed-point value;
- enum;
- bit field;
- House ID;
- Trigger ID;
- Tag ID;
- TeamType ID;
- TaskForce ID;
- ScriptType ID;
- AITrigger ID;
- Waypoint ID;
- ScenarioCellId;
- Rules type ID or ordinal;
- local variable ID;
- global variable ID;
- text, sound, theme, movie, speech, or briefing reference;
- extension-defined value;
- sentinel;
- unknown.

## 3. No coincidence binding

A numeric parameter may simultaneously match:

- Waypoint 3;
- House ordinal 3;
- local variable 3;
- global variable 3;
- Rules type ordinal 3;
- quantity 3.

The graph must not bind it to whichever registry was searched first.

Binding requires:

1. selected opcode/layout profile;
2. slot descriptor;
3. target identity domain;
4. normalization policy;
5. unique target or explicit ambiguity result.

## 4. Reference edge model

```text
ScenarioReferenceEdge
- SourceIdentity
- SourceFieldPath
- RawTarget
- CandidateTargetKinds[]
- CandidateTargets[]
- SelectedTarget?
- ResolutionState
- EvidenceGrade
- SourceProvenance
```

Reference kinds include Trigger→Trigger, Tag→Trigger, object→Tag, CellTag→Tag, TeamType→TaskForce, TeamType→Script, TeamType→Tag, AITrigger→TeamType, ScriptStep→Waypoint/type, and Event/Action parameter edges.

## 5. Resolution states

Recommended states:

- `ResolvedUnique`;
- `ResolvedSentinel`;
- `MissingTarget`;
- `DuplicateTarget`;
- `CaseCollision`;
- `AmbiguousTargetKind`;
- `AmbiguousNormalization`;
- `InvalidTargetSyntax`;
- `ExtensionProfileRequired`;
- `NotInterpreted`.

No dangling edge is deleted.

## 6. Identity normalization

Candidate policies:

- exact ordinal/case-sensitive;
- ASCII case-insensitive;
- engine-style logical-name normalization;
- profile-specific sentinel handling;
- no normalization.

The selected policy is serialized with the graph. Raw IDs remain unchanged.

## 7. House and owner references

A House-like parameter may refer to:

- map House identity;
- country/HouseType;
- special sentinel such as all/none;
- player-controlled House;
- owner of the Trigger;
- extension-defined selector.

It is not a runtime player instance during parsing.

## 8. Waypoint references

Waypoint values can be stored or displayed in different representations:

- numeric ID;
- alphabetic editor representation;
- scenario-cell value in `[Waypoints]`;
- sentinel for none.

A parameter descriptor may convert an alphabetic candidate to an ID, but raw text and conversion trace must remain.

## 9. Type references

A parameter may point to:

- BuildingType;
- VehicleType;
- InfantryType;
- AircraftType;
- TechnoType union;
- SuperWeaponType;
- AnimType or another Rules family;
- editor catalog ordinal;
- extension registry.

Binding uses already-composed Rules registries. It does not scan MIX files or Art resources.

## 10. Text and media references

Textual Action parameters may identify:

- CSF/string-table key;
- movie;
- theme;
- sound;
- speech;
- briefing entry;
- raw literal text in an extension profile.

Core records logical candidates only. Missing media is not a parse failure.

## 11. Variable references

Public community documentation distinguishes:

- `[VariableNames]` in maps for local variables;
- Rules-level variable definitions for global variables;
- numeric IDs/indices used by Events and Actions;
- engine/version differences in persistence.

Potential fields:

```text
ScenarioVariableReference
- ScopeRaw
- ScopeCandidate(Local|Global|Unknown)
- IdRaw
- IdCandidate
- NameCandidate
- ValueTypeCandidate(Boolean|Int32|Unknown)
- OperationCandidate
- EvidenceGrade
```

The parser does not own variable values.

## 12. Variable persistence boundary

Do not conflate:

- map-local variable definition;
- current scenario value;
- campaign-persistent global value;
- savegame persistence;
- editor display name;
- trigger comparison parameter.

These require future runtime/save-state research.

## 13. Difficulty fields

Difficulty may appear as:

- three independent Trigger booleans;
- three AITrigger booleans;
- a bit mask in an extension;
- yes/no or true/false text;
- `0`/`1` numeric values;
- invalid raw text.

Core retains each raw field. It does not substitute editor defaults.

## 14. Boolean parsing

Boolean candidates must record:

- raw spelling;
- recognized spelling set/profile;
- parsed candidate;
- invalid-state diagnostic.

`yes`, `true`, and `1` may be equivalent under one profile, but the lossless writer must preserve original spelling.

## 15. Sentinel handling

Sentinels are field and profile specific. Examples include:

- `<none>`;
- `none`;
- `-1`;
- `0`;
- `A` for an editor waypoint encoding candidate;
- empty string.

No global sentinel table is safe.

## 16. Ambiguous strings

A raw string can be both:

- logical ID;
- display name;
- text-table key;
- enum label;
- extension literal.

Descriptors enumerate candidates. They do not replace the token.

## 17. Numeric safety

For every numeric candidate:

- parse with invariant culture;
- distinguish signed/unsigned;
- report overflow;
- do not clamp;
- preserve leading sign and zeros;
- do not allocate based on the value before budget checks;
- keep decimal/floating raw spelling.

AITrigger weights and comparator fields require separate parsing profiles from integer parameter slots.

## 18. Graph cycles

Potential cycles include:

- Trigger links;
- Tag/Trigger patterns through extension references;
- Script jump candidates;
- Team/AITrigger reference structures.

Cycle detection is analytical. The parser does not execute or break cycles.

## 19. Map-local and global resolution

TeamTypes, TaskForces, ScriptTypes, Rules types, and House identities may exist in global and map-local layers.

Resolution must preserve:

- layer;
- winner;
- suppressed candidate;
- duplicate identity;
- global/local classification;
- exact ID spelling.

The `-G` suffix is preserved as text and may be annotated as a community convention; it is not stripped by default.

## 20. Execution boundary

References are not actions. A resolved TeamType edge does not create a Team. A resolved Waypoint does not issue movement. A resolved variable does not read or write state.

Future execution receives resolved candidates through a stable interface and performs deterministic validation against current world state.
