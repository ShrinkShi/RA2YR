# Future ProjectBaseline sanitized audit request

> **Source notice:** ChatGPT Web public-source research. No local `ProjectBaseline` access. Not a Codex artifact. No GPL or unclear-license code was copied, translated, mechanically rewritten, or ported. `code_imported: false`.

This document designs a future read-only Codex audit. It does not authorize this branch to access ProjectBaseline, run games/editors, or publish scenario content.

## 1. Status and objective

```text
AuditStatus: NotRun
FutureEvidenceSource: ProjectBaselineAggregateAudit
```

These fields are not a formal evidence grade and do not imply that ProjectBaseline was read, observed, or confirmed. A future aggregate audit cannot automatically become `ConfirmedByOriginalRuntimeSource`, alter project policy, or promote compatibility.

The future audit may collect non-reconstructable aggregates about section presence, record shapes, count/tuple consistency, anonymous opcode ranges, identity/reference anomalies, Team/TaskForce/Script/AITrigger categories, variables, difficulty fields, extension tails and input-mode equivalence.

## 2. Isolation

A future local task may read a bounded authoritative root with read-only tooling and compare Memory, Stream, short-read Stream and exact MIX-window adapters. It must not commit source content, modify maps, execute scenario logic, create teams or units, load assets for display, publish per-map graph data, or send original records into this research branch.

## 3. Sample selection

Use broad categories rather than identities: campaign/skirmish, theater, size band, Trigger-heavy, Team/TaskForce/Script-heavy, AITrigger-heavy, local-variable, CellTag, object-Tag, no-script, extension, duplicate, dangling and unknown-opcode candidates. No map names, filenames, titles or IDs are published.

## 4. Allowed aggregates

- selection and broad provenance categories;
- section presence and record-count ranges;
- field-count, empty/trailing-token, duplicate-key and unknown-tail histograms;
- declared/parsed Event and Action count mismatch categories;
- anonymous coarse opcode ranges and parameter-slot occupancy counts;
- identity duplicate, case-collision, dangling, ambiguity and cycle counts;
- TeamType listed/section/global-local categories and binding status counts;
- TaskForce entry-count/gap/count/type-binding aggregates;
- Script step-count/gap/unknown-action/argument aggregates;
- AITrigger field-count, team-binding, comparator-class and coarse weight categories;
- local/global/unknown variable-reference and difficulty-flag aggregates;
- diagnostic-code counts, evidence-grade counts and non-linkable aggregate hashes;
- Memory/Stream/short-read/MIX equivalence.

No ordered opcode sequences, parameter tuples, graph topology, team composition, Script steps or exact comparator/weight data may be published.

## 5. Forbidden output

Never publish map names or paths, INI bodies, raw records/tokens/keys, IDs, display text, exact comparator blobs or weights, variable names/values, ordered Events/Actions/TaskForce/Script data, graph topology, coordinates, object locations, team composition, type-to-resource mappings, bytes, Base64, hex, images, per-map/per-record/per-ID hashes, or any reconstructable scenario logic.

## 6. Anti-reconstruction

Use coarse bins, suppress small categories, merge rare opcodes into anonymous ranges, never emit row-per-map output, do not pair many exact dimensions for one sample, omit timestamps/source order, and retain no reversible pseudonyms.

## 7. Profile comparisons

Only explicitly selected TS, RA2/YR, official-editor, WAE, Ares, Phobos, case-normalization and count-contract profiles may be compared. Do not try every profile and select the one with fewer errors. Multiple successful profiles produce an ambiguity category.

## 8. Safety

The audit sets bounds for files, bytes, sections, records, tokens, declared counts, graph nodes/edges, diagnostics and runtime; supports cancellation and no-progress detection; has no network or write access where possible; and performs no execution.

These are `DefensiveDesign` audit requirements.

## 9. Required declaration

Every report states that ProjectBaseline was not modified, no map content was published, no scenario execution or Unity/game process occurred, no IDs/records/sequences/topology/coordinates were emitted, and no compatibility status changed.

## 10. Suggested output

```text
AuditReport
- MethodVersion
- AuditStatus = NotRun
- FutureEvidenceSource = ProjectBaselineAggregateAudit
- SelectedProfiles
- SelectionCategoryAggregates
- SectionPresenceAggregates
- ShapeAggregates
- OpcodeBinAggregates
- ReferenceStatusAggregates
- TeamAiAggregates
- VariableDifficultyAggregates
- DiagnosticCounts
- InputModeEquivalence
- CurrentEvidenceGrade
- DisclosureChecks
```

`CurrentEvidenceGrade` records only the pre-audit public-source grade from the nine-item vocabulary.

## 11. Stop conditions

Stop without publication when sanitization cannot remove raw values/paths, a category identifies a map, an artifact contains sequences/topology/IDs, resource limits are exceeded, input modes disagree without a bounded diagnostic, or any operation would modify ProjectBaseline.
