# M3-R8 — Scenario Trigger, Event, Action, Tag, and Team AI research

> **Source notice:** This document was produced by **ChatGPT Web** from public sources. Local `ProjectBaseline` content was not read. This is not a local Codex Agent artifact. GPL and unclear-license sources were used as reference only; no code was copied, translated, mechanically rewritten, or ported. `code_imported: false`.

## Purpose

This dossier records public evidence for the declarative scenario graph used by TS, RA2, and YR maps and AI definitions. It focuses on raw storage, identity, references, versioned layouts, and parser/executor boundaries. It does not implement a parser, opcode catalog, executor, AI, variable state machine, unit creation, or Unity integration.

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

Core parsing never fires Triggers, polls Events, schedules Actions, creates Teams or units, executes ScriptType steps, evaluates AITrigger weights, mutates variables, loads visual assets, or creates Unity objects.

## Sections in scope

Primary sections:

- `[Triggers]`, `[Events]`, `[Actions]`, `[Tags]`, `[CellTags]`;
- `[TeamTypes]` and TeamType sections;
- `[TaskForces]` and TaskForce sections;
- `[ScriptTypes]` and ScriptType sections;
- `[AITriggerTypes]` and related enablement data.

House, placement, waypoint, variable, Rules and campaign sections are reference boundaries only.

## Headline findings

1. Common tool profiles use an eight-token Trigger candidate and a three-token Tag candidate.
2. Events begin with a declared count; WAE uses an opcode, two base parameters, and profile-selected additional parameters.
3. Actions begin with a declared count; WAE uses an opcode plus seven raw parameter slots.
4. Unknown, negative, and extension opcodes remain raw and non-executable without an explicit catalog profile.
5. TeamTypes, TaskForces, and ScriptTypes use list sections plus per-ID sections; order, gaps, duplicates, missing sections, and unknown members remain visible.
6. The AITrigger 18-field layout is a strong tool/community candidate, not an original-runtime contract.
7. FinalAlert validation and repair rules are official-tool behavior only; they do not establish runtime acceptance or defaults.
8. Ares and Phobos opcodes and fields are extension-specific behavior and remain isolated from vanilla candidates.
9. Parser output is an immutable declarative graph. Full Event/Action/Team/Script/AI execution requires a separate deterministic simulation module.

## Formal evidence grades

Every formal `Grade` field uses exactly one value:

- `ConfirmedByOriginalRuntimeSource`;
- `ConfirmedByOfficialToolSource`;
- `ConfirmedByMultipleIndependentImplementations`;
- `ConfirmedCommunityConvention`;
- `ImplementationSpecificBehavior`;
- `DefensiveDesign`;
- `ConflictingSources`;
- `Underconfirmed`;
- `Unresolved`.

No complete original RA2/YR runtime source was located, so no reviewed claim reaches `ConfirmedByOriginalRuntimeSource`. FinalSun/FinalAlert behavior uses `ConfirmedByOfficialToolSource`. WAE, OpenRA, MapTool, CNCMaps, CnCNet and extension projects are recorded as named `ImplementationSpecificBehavior`. Cross-tool convergence remains `Underconfirmed` unless lineage independence is demonstrated. Stable ModEnc/PPM conventions use `ConfirmedCommunityConvention`; direct tuple, parameter, identity or default conflicts use `ConflictingSources`.

Raw preservation, explicit profiles, no count repair, no last-wins, no opcode fallback, no execution during parsing, bounded collections and fail-closed reference handling are `DefensiveDesign`.

Future ProjectBaseline work is separate:

```text
AuditStatus: NotRun
FutureEvidenceSource: ProjectBaselineAggregateAudit
```

These fields do not imply that ProjectBaseline was read or observed and cannot automatically promote compatibility or become original-runtime evidence.

## Normalized claim summary

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert editor catalogs, validation, repair and ID workflows | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Official editor behavior only. | Preserve a named editor profile; do not inherit repairs. | `NotRun` |
| WAE Trigger/Event/Action/Team/TaskForce/Script/AITrigger layouts | `ImplementationSpecificBehavior` | World-Altering Editor | Named editor implementation, including configurable extension data. | Source-pinned comparison profile. | `NotRun` |
| Common Trigger, Tag, Action, TaskForce, Script and AITrigger shapes | `Underconfirmed` | WAE, community docs and supplementary tools | Convergence does not prove independent lineage or runtime strictness. | Explicit family/version profiles. | `NotRun` |
| Stable community opcode and field names | `ConfirmedCommunityConvention` | ModEnc, PPM and fixed documentation | Names and descriptions are not runtime execution proof. | Provenance per catalog entry. | `NotRun` |
| Event tuple width, Trigger tail, Action slot defaults and unknown-opcode handling | `ConflictingSources` | Official editor, WAE, community and extensions | Sources directly differ by version/profile and recovery policy. | Preserve raw/count/tail data and report ambiguity. | `NotRun` |
| Complete runtime Event/Action evaluation, Team recruitment, Script state and AITrigger weighting | `Unresolved` | No original-runtime source located | No reliable complete state machine was found. | Future executor remains separate. | `NotRun` |
| Immutable raw graph, no execution, explicit references and no repair | `DefensiveDesign` | Project policy | Preservation and architecture boundary. | Fail closed without deleting records. | `NotRun` |

## Core project policy

The defensive default preserves every section occurrence, key, value, empty/trailing token, unknown field, opcode and parameter slot; preserves duplicate IDs and dangling edges; never chooses a reference because a numeric value happens to match an object; never repairs counts, booleans, IDs, opcodes or references; requires explicit version/extension profiles; separates structural and semantic diagnostics; and uses checked arithmetic, bounded collections and no-progress protection.

## Documents

- `section-and-graph-boundaries.md`
- `trigger-tag-identity-model.md`
- `event-record-layout-and-opcodes.md`
- `action-record-layout-and-opcodes.md`
- `parameter-and-reference-semantics.md`
- `teamtype-record-layout.md`
- `taskforce-and-script-records.md`
- `aitrigger-record-layout.md`
- `source-comparison.md`
- `implementation-boundaries.md`
- `test-matrix.md`
- `baseline-audit-request.md`
- `unresolved-questions.md`

## Explicit non-goals

No parser, opcode switch, executor, AI, variables, Teams, units, save state, multiplayer synchronization, INI-parser modification, ProjectBaseline access, Unity/game/editor execution, compatibility change, ADR, code, test, asset, or map modification is included.
