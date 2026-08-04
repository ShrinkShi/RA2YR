> **Source notice:** Prepared by **ChatGPT Web** from public sources only. ProjectBaseline was not read. This is not a Codex artifact. GPL and unclear-license implementations are reference-only; no code was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

# Future ProjectBaseline Sanitized Audit Request

## Status

Design only. Do not run in this research task. Any future result is graded only as:

```text
ObservedByFutureProjectBaselineAudit
```

It does not confirm original runtime compatibility.

## Selection basis

A future read-only Codex audit may select representative sanitized inputs by broad category only:

- placement families containing Mission fields;
- Rules mission-control presence;
- Team/Script mission-reference presence;
- actor-category capability fields;
- transport/garrison fields;
- deploy/enter-related fields;
- broad waypoint/queue-shape candidates.

The public report must not expose file names, map names, paths, type names, or record text.

## Permitted aggregate output

- mission section/token presence;
- broad actor categories;
- raw text versus numeric mission-shape counts;
- known/unknown/ambiguous binding counts;
- product-profile classification counts;
- command-capability category counts;
- autonomous-policy field-presence counts;
- transport/garrison field-presence counts;
- coarse queue/waypoint shape histograms;
- duplicate/case/empty/invalid counts;
- diagnostic category counts;
- bounded-input and no-progress outcomes;
- non-linkable aggregate hashes;
- Memory/Stream/short-read/MIX equivalence.

## Forbidden output

- type, unit, building, team, or transport names;
- map/scenario names or paths;
- INI text or token text;
- exact mission sequences or enum values per object;
- object positions;
- target or ownership graph;
- waypoint coordinates or route topology;
- transport passenger contents;
- garrison occupant contents;
- Trigger, Tag, Script, TeamType, or AI IDs;
- exact hotkeys or client configuration;
- screenshots, renders, cursors, or UI captures;
- per-map/per-type hashes;
- hex, Base64, compressed or decoded bytes;
- any information sufficient to reconstruct a mission, team script, unit configuration, or map.

## Audit checks

1. Raw input modes yield equivalent aggregate results.
2. No original content appears in diagnostics or exception messages.
3. Unknown mission tokens remain unknown.
4. Placement Mission is not reported as runtime state.
5. No command, path, target, mission transition, transport mutation, or selection is executed.
6. No Unity object is created.
7. Budgets and checked arithmetic fail closed.
8. Aggregate hashes are dataset-level and non-linkable.

## Publication gate

A human reviewer must verify the report against the forbidden-output list before publication. Audit observations cannot change compatibility matrices, ADRs, or implementation status from this branch.
