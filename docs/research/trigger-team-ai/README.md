# M3-R8 — Scenario Trigger, Event, Action, Tag, and Team AI research

> **Source notice:** This document was produced by **ChatGPT Web** from public sources. Local `ProjectBaseline` content was not read. This is not a local Codex Agent artifact. GPL and unclear-license sources were used as reference only; no code was copied, translated, mechanically rewritten, or ported. `code_imported: false`.

## Purpose

This dossier records public evidence for the declarative scenario graph used by Tiberian Sun, Red Alert 2, and Yuri's Revenge maps and AI definitions. It focuses on raw storage, identity, references, versioned layouts, and parser/executor boundaries.

It does **not** implement a parser, opcode catalog, executor, AI, variable state machine, unit creation, or Unity integration.

## Frozen layer boundary

```text
lossless map INI
→ section occurrence collection
→ raw trigger/team records
→ family-specific token views
→ explicit layout profiles
→ identity and reference graph
→ opcode/parameter interpretation candidates
→ validated declarative scenario graph
→ future execution adapters
```

The following are prohibited inside Core parsing:

- firing a Trigger;
- polling an Event;
- scheduling an Action;
- creating a Team or unit;
- executing a ScriptType step;
- evaluating an AITrigger weight;
- mutating local or global variables;
- loading SHP, VXL, HVA, TMP, palette, or Unity assets;
- creating a `GameObject`, coroutine, behavior tree, timer, or navigation command.

## Sections in scope

Primary sections:

- `[Triggers]`;
- `[Events]`;
- `[Actions]`;
- `[Tags]`;
- `[CellTags]`;
- `[TeamTypes]` and each TeamType section;
- `[TaskForces]` and each TaskForce section;
- `[ScriptTypes]` and each ScriptType section;
- `[AITriggerTypes]`;
- `[AITriggerTypesEnable]` as a related enablement layer.

Boundary-only sections:

- `[VariableNames]` and possible local/global variable alternatives;
- `[Houses]` and map-local house sections;
- placement-section Tag, Group, and Follows fields;
- `[Waypoints]`;
- Rules type registries and map-local Rules;
- campaign and `[Basic]` metadata.

## Headline findings

1. A common Trigger writer profile emits eight CSV tokens:
   `owner,linkedTrigger,name,disabled,easy,normal,hard,repeating-or-reserved`.
2. A common Tag profile emits three CSV tokens:
   `repeat,name,triggerId`.
3. `[Events]` starts with a declared count. WAE uses an opcode plus two base parameter slots and up to two additional configured slots; tuple width is therefore profile/opcode dependent.
4. `[Actions]` starts with a declared count. WAE preserves a fixed tuple of one opcode plus seven raw parameter slots.
5. Unknown Event or Action opcodes must remain raw. Editor display names and parameter labels are not protocol truth.
6. TeamTypes are not one CSV list record. `[TeamTypes]` maps list keys to TeamType IDs; each ID has a separate key-value section.
7. TaskForces and ScriptTypes also use list sections plus per-ID sections. Their ordered numeric child keys are semantically significant candidates.
8. WAE and community references use up to six TaskForce entries and a two-field `count,type` entry model.
9. Script steps use a two-field `action,argument` candidate model, distinct from Trigger Events, Trigger Actions, and placement Mission tokens.
10. AITriggerTypes use an 18-field CSV candidate profile with two TeamType references, owner, condition fields, a comparator blob, three weights, side/state fields, and three difficulty booleans.
11. Ares and Phobos add Event, Action, and Script opcodes. Extensions require explicit profiles and cannot be treated as vanilla.
12. Parser output is an immutable declarative graph. A future executor is a separate module with simulation state and deterministic command interfaces.

## Evidence grades

Every conclusion uses one of:

- `ConfirmedByOfficialRuntimeSource`;
- `ConfirmedByOfficialEditorSource`;
- `ConfirmedByIndependentImplementation`;
- `CommunityDocumented`;
- `ObservedByFutureProjectBaselineAudit`;
- `ConfiguredForProjectPolicy`;
- `Unresolved`.

The current dossier does not claim complete `ConfirmedByOfficialRuntimeSource` coverage.

## Core project policy

The defensive default is:

- preserve every section occurrence, key, raw value, token, unknown field, opcode, and parameter slot;
- preserve duplicate IDs and dangling edges;
- do not select an identity or parameter interpretation because a value happens to match an existing object;
- do not repair counts, booleans, IDs, opcodes, or references;
- do not normalize case or regenerate IDs by default;
- require explicit version and extension profiles;
- report structural and semantic failures separately;
- use checked arithmetic, bounded collections, and no-progress protection.

## Documents

- `section-and-graph-boundaries.md` — section ownership and graph layers.
- `trigger-tag-identity-model.md` — Trigger, Tag, and identity edges.
- `event-record-layout-and-opcodes.md` — Event count and tuple candidates.
- `action-record-layout-and-opcodes.md` — Action count and parameter slots.
- `parameter-and-reference-semantics.md` — ambiguous parameter typing and variables.
- `teamtype-record-layout.md` — TeamType list and per-ID section model.
- `taskforce-and-script-records.md` — TaskForce and ScriptType ordered entries.
- `aitrigger-record-layout.md` — 18-field AITrigger candidate.
- `source-comparison.md` — pinned sources and license boundaries.
- `implementation-boundaries.md` — candidate Core models and policies.
- `test-matrix.md` — 160 design tests.
- `baseline-audit-request.md` — future sanitized audit request.
- `unresolved-questions.md` — evidence gaps.

## Explicit non-goals

This research does not:

- implement Trigger, Event, Action, Tag, TeamType, TaskForce, ScriptType, or AITrigger parsing code;
- implement a formal opcode enum or switch table;
- execute scenario logic;
- create teams or units;
- implement variables, AI weights, save-state, or multiplayer synchronization;
- modify the INI parser;
- read `ProjectBaseline`;
- run Unity, RA2/YR, FinalAlert, WAE, XCC, or any map;
- modify compatibility status, ADRs, formal third-party records, tests, code, or prior research.
