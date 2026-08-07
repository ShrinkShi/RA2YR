# Section and graph boundaries

> **Source notice:** ChatGPT Web public-source research. No local `ProjectBaseline` access. Not a Codex artifact. No GPL or unclear-license code was copied, translated, mechanically rewritten, or ported. `code_imported: false`.

## 1. Why this is a graph, not one object

The scenario scripting surface contains several identity domains connected by references:

```text
CellTag ──references──> Tag ──references──> Trigger
                                        ├── owns Events list
                                        └── owns Actions list

TeamType ──references──> TaskForce
         ├─────────────> ScriptType
         ├─────────────> House
         ├─────────────> Tag candidate
         └─────────────> Waypoint candidates

AITriggerType ──references──> primary TeamType
              └─────────────> secondary TeamType
```

Placement records add more edges:

- object `Tag` field → Tag ID candidate;
- unit `Follows` field → unit/source-order candidate;
- object `Group` field → group number candidate;
- owner/type fields → House and Rules type candidates.

These identities cannot be collapsed into a single string dictionary.

## 2. Lossless INI layer

The INI layer must preserve:

- physical section order;
- duplicate section occurrences;
- raw section-name spelling;
- physical key order;
- duplicate keys;
- raw key spelling and case;
- raw value text;
- empty values;
- commas, empty CSV tokens, and trailing commas;
- comments and physical source lines where supported;
- source file/layer provenance.

The graph reader consumes this lossless representation. It must not ask a normal dictionary for an already-overwritten value and then claim losslessness.

## 3. Section occurrence collection

Each logical section family receives all source occurrences before semantic composition:

```text
TriggerSectionOccurrences
EventSectionOccurrences
ActionSectionOccurrences
TagSectionOccurrences
CellTagSectionOccurrences
TeamTypeListOccurrences
TaskForceListOccurrences
ScriptTypeListOccurrences
AITriggerTypeOccurrences
```

Duplicate-section policy is explicit. Candidate policies include:

- preserve-only;
- source-order concatenation;
- semantic key composition with winner/suppressed provenance;
- profile-specific rejection.

No family silently inherits ordinary Rules key-composition policy without evidence.

## 4. Raw record families

### 4.1 CSV-like list records

- Trigger record;
- Tag record;
- Event aggregate record;
- Action aggregate record;
- AITriggerType record.

### 4.2 List-to-subsection registries

- `[TeamTypes]` → per-TeamType section;
- `[TaskForces]` → per-TaskForce section;
- `[ScriptTypes]` → per-ScriptType section.

### 4.3 Direct reference records

- `[CellTags]`: cell identity candidate → Tag ID;
- `[AITriggerTypesEnable]`: AITrigger ID → boolean candidate;
- `[VariableNames]`: variable ID/index candidate → display name/value candidate.

These families require different tokenizers and identity policies.

## 5. Identity domains

Core preserves distinct `ScenarioIdentityKind` values:

- Trigger;
- Tag;
- EventList;
- ActionList;
- TeamType;
- TaskForce;
- ScriptType;
- AITriggerType;
- House;
- Waypoint;
- Variable;
- RulesType;
- PlacementRecord;
- ScenarioCell;
- UnknownExtensionIdentity.

A raw ID can be a candidate for multiple domains, but it cannot be automatically retyped because the same string appears elsewhere.

## 6. Reference edges

Every edge records:

- source identity and source token location;
- target-kind candidates;
- raw target text;
- normalization profile;
- candidate targets;
- chosen target, if policy permits;
- evidence grade;
- ambiguity diagnostics;
- source and target provenance.

Suggested edge states:

- `ResolvedUnique`;
- `ResolvedSentinel`;
- `MissingTarget`;
- `DuplicateTargetIdentity`;
- `CaseCollision`;
- `AmbiguousTargetKind`;
- `InvalidTargetSyntax`;
- `CycleDetected`;
- `NotInterpreted`.

Dangling edges remain in the graph.

## 7. Structural parse versus semantic graph

The following results are independent:

1. section structurally present;
2. raw record captured;
3. tokenization completed;
4. declared count interpreted;
5. tuple structure interpreted;
6. opcode recognized;
7. parameter candidates generated;
8. identity edge resolved;
9. graph consistency checked;
10. execution eligibility assessed by a future module.

An unknown opcode may still have a completely valid raw record and count structure.

## 8. Declarative graph output

The validated declarative graph is immutable and contains no runtime state:

- no current Trigger activation;
- no Event satisfaction bit;
- no Action queue;
- no timer deadline;
- no Team instance;
- no created units;
- no current Script instruction pointer;
- no AITrigger current weight;
- no local/global variable values;
- no savegame state.

It may contain diagnostics and unresolved references.

## 9. Future execution adapter boundary

A future executor requires separate inputs:

```text
ImmutableScenarioGraph
SimulationClock
WorldQueryInterface
HouseAndPlayerState
ObjectIdentityRegistry
ScenarioVariableState
DeterministicCommandSink
SavegameScenarioState
DifficultyProfile
MultiplayerDeterminismContext
```

The parser does not provide or own these services.

## 10. Count and order boundaries

Order can be meaningful at several independent levels:

- section physical order;
- record source order;
- Event tuple order;
- Action tuple order;
- TaskForce entry numeric order;
- ScriptType step numeric order;
- TeamType list source/numeric order;
- AITrigger record order.

Core preserves all source order even when a semantic view uses numeric order.

## 11. Extension boundaries

Ares, Phobos, editor extensions, and custom engines may add:

- new Event opcodes;
- new Action opcodes;
- additional Event parameters;
- new Script actions;
- TeamType flags;
- AITrigger fields or interpretation changes;
- new variable operations.

Extensions are enabled by an explicit profile. Unknown extension tokens remain raw when the profile is absent.

## 12. Prohibited shortcuts

Do not:

- merge Tag and Trigger identities;
- use Event/Action key presence to fabricate a Trigger;
- delete Events or Actions whose Trigger is missing;
- bind a number to whichever identity domain happens to contain that number;
- regenerate IDs to remove duplicates;
- reorder Actions by opcode;
- stop preserving unused parameter slots;
- execute anything during parsing;
- create Unity objects or engine callbacks.
