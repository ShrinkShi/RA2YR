# Unresolved questions

> **Source notice:** ChatGPT Web public-source research. No local `ProjectBaseline` access. Not a Codex artifact. No GPL or unclear-license code was copied, translated, mechanically rewritten, or ported. `code_imported: false`.

Editor behavior, community documentation, tool convergence and future aggregate observations must not be promoted to original-runtime facts without stronger evidence.

## P0 — strict layout and identity blockers

- exact stock Trigger and Tag field counts, tail meanings, sentinel spellings, case sensitivity and duplicate-ID behavior;
- orphan or duplicate Events/Actions behavior and whether every Trigger requires both lists;
- exact Event tuple widths, additional parameters, count mismatch handling, unknown opcode behavior and source-order semantics;
- exact Action tuple width, final-slot `A` meaning, unused-slot validation, count mismatch and unknown opcode behavior;
- Tag persistence values, Trigger-link execution/cycles and missing-reference behavior;
- global/map-local composition and identity conventions for TeamTypes, TaskForces, ScriptTypes and AITriggerTypes;
- exact AITrigger fields 11/13, comparator representation, weight representation and enablement composition.

All remain `Unresolved` unless a narrower tool/community claim has an explicit grade.

## P1 — parameter and parser behavior

Unresolved areas include numeric signedness, overflow, whitespace, empty-token handling, quoted tokens, comments inside values, extra-token behavior, string-versus-numeric Action slots, reference target domains, sentinel forms, variable identity/state, difficulty boolean parsing, TeamType key sets and flags, TaskForce limits/gaps/counts/type families, Script limits/gaps/jumps/arguments, and AITrigger owner/side/condition/weight semantics.

Public editor or tool behavior for a specific item is `ConfirmedByOfficialToolSource` or `ImplementationSpecificBehavior`; stable community naming is `ConfirmedCommunityConvention`; direct disagreements are `ConflictingSources`; cross-tool candidates without proven independent lineage are `Underconfirmed`.

## P2 — roundtrip, extension and executor research

Further work must resolve:

- physical ordering and duplicate preservation through editors;
- unknown opcode/flag/tail preservation and canonicalization;
- byte-identical versus semantic roundtrip contracts;
- Ares/Phobos versioned opcode/profile selection and legal catalog distribution;
- runtime Event persistence, evaluation order, callbacks, Trigger recursion and save-state;
- deterministic Team creation, AITrigger weight changes, recruitment/production integration and Script instruction state;
- world-query and deterministic command-sink interfaces;
- campaign/skirmish and RA2/YR executable differences;
- multiplayer synchronization and unknown-opcode execution eligibility;
- evidence thresholds for parse, binding, editor reopen, runtime acceptance and gameplay-equivalence compatibility claims.

## Resolution discipline

For every future answer record source, permanent locator, version/commit, reader/writer/editor/runtime category, license, lineage, one of the nine formal evidence grades, and whether the result changes only a profile or a public compatibility status.

Future ProjectBaseline work remains separate:

```text
AuditStatus: NotRun
FutureEvidenceSource: ProjectBaselineAggregateAudit
```

These fields do not mean that ProjectBaseline was read, observed, or confirmed. Aggregate observations cannot alone close an original-runtime question or become `ConfirmedByOriginalRuntimeSource`.
