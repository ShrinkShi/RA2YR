# M3-R7 scenario object placement and binding dossier

> **Source notice:** This research was completed by **ChatGPT Web** from public materials. Local `ProjectBaseline` content was not read. This is not a local Codex Agent artifact. GPL and unclear-license implementations were used only as behavioral references; no code was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

## Purpose

This dossier documents the map-side placement records used by RA2/YR scenario objects and defines implementation boundaries for future Core work. It does not implement a parser, object registry, house system, trigger runtime, renderer, simulation, or Unity adapter.

The primary sections are:

- `[Structures]`;
- `[Units]`;
- `[Infantry]`;
- `[Aircraft]`;
- `[Terrain]`;
- `[Smudge]`;
- `[Waypoints]`;
- `[CellTags]`.

Related sections are considered only as binding targets: `[Houses]`, map-local house sections, `[Tags]`, `[Triggers]`, `[TeamTypes]`, `[TaskForces]`, `[ScriptTypes]`, `[AITriggerTypes]`, Rules type registries, and Art sections.

## Frozen layer boundary

```text
lossless map INI
→ section occurrence collection
→ raw placement record
→ section-specific token view
→ explicit record-layout profile
→ coordinate interpretation
→ logical owner and type binding
→ opaque reference graph
→ typed placement descriptor
→ simulation/rendering adapters
```

The layers are intentionally separate:

- the INI layer preserves physical text, duplicate sections, duplicate keys, comments, whitespace, and occurrence order;
- placement tokenization preserves empty and unknown fields;
- coordinate interpretation does not bind Rules types;
- owner and type binding does not create players or game objects;
- references remain opaque edges in this work package;
- rendering and simulation remain outside Core.

## Leading conclusions

1. **There is no safe universal placement CSV structure.** Structures, vehicles, infantry, aircraft, terrain objects, smudges, waypoints, and cell tags use different key and value contracts.
2. **The common RA2/YR writer layouts are editor/community candidates, not runtime proof.** WAE and MapTool use familiar 17-field Structure, 14-field Unit, 14-field Infantry, and 12-field Aircraft forms while also applying destructive token splitting and repairs that this project must not inherit.
3. **Placement keys are not uniformly identities.** WAE writes sequential numeric keys for techno and smudge records, cell IDs as keys for terrain and cell tags, and waypoint IDs as keys for waypoints. Core preserves raw key spelling and occurrence order rather than renumbering.
4. **`Y × 1000 + X` is the leading scenario-cell candidate, graded `Underconfirmed`.** WAE uses it for Terrain keys, CellTag keys, and Waypoint values; ModEnc documents the convention. Shared community knowledge and lack of runtime source prevent promotion.
5. **Owner and type binding are semantic stages.** Raw owner/type tokens remain valid source data even when a house, Rules type, or Art resource cannot be resolved.
6. **Health, facing, mission, veterancy, group, high/bridge state, follows, and recruitment flags remain raw first.** Editor clamping and default substitution are not parser semantics.
7. **Tag, trigger, team, follows, and script relations form an opaque reference graph here.** This dossier does not execute or fully decode them.

## Formal evidence grades

Every formal `Grade` field uses exactly one value from this closed vocabulary:

- `ConfirmedByOriginalRuntimeSource`;
- `ConfirmedByOfficialToolSource`;
- `ConfirmedByMultipleIndependentImplementations`;
- `ConfirmedCommunityConvention`;
- `ImplementationSpecificBehavior`;
- `DefensiveDesign`;
- `ConflictingSources`;
- `Underconfirmed`;
- `Unresolved`.

No complete RA2/YR game runtime source was found for these records, so no claim in this dossier reaches `ConfirmedByOriginalRuntimeSource`. FinalSun/FinalAlert evidence is `ConfirmedByOfficialToolSource`. WAE, MapTool, OpenRA, CNCMaps and other named implementations are graded individually as `ImplementationSpecificBehavior`; cross-tool agreement remains `Underconfirmed` unless independence is proven. Stable ModEnc/community field conventions use `ConfirmedCommunityConvention`. Raw preservation, explicit profiles, no clamp/default repair, and fail-closed behavior are `DefensiveDesign`.

Future ProjectBaseline work is recorded separately:

```text
AuditStatus: NotRun
FutureEvidenceSource: ProjectBaselineAggregateAudit
```

These fields do not imply that ProjectBaseline was read or observed and cannot automatically become original-runtime evidence.

## Normalized claim summary

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert exposes map-local type lookup and placed-object editor fields | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Official editor behavior only. | Preserve source-specific editor profile. | `NotRun` |
| WAE/MapTool field layouts and repair/default behavior | `ImplementationSpecificBehavior` | Named tools | Single-tool behavior; destructive repair is not inherited. | Raw tokens remain primary. | `NotRun` |
| Common 17/14/14/12 techno layouts | `Underconfirmed` | WAE, MapTool, ModEnc | Cross-tool convergence exists but lineage independence and runtime strictness are not proven. | Explicit family layout profiles. | `NotRun` |
| `Y × 1000 + X` scenario-cell convention | `Underconfirmed` | WAE and ModEnc | Strong tool/community candidate, not an original-runtime contract. | Never choose by map-bound plausibility. | `NotRun` |
| Unique original-runtime meanings for malformed health/facing/mission/tails and Follows basis | `Unresolved` | No original-runtime source located | Tool defaults and normalizations differ. | Preserve raw and return ambiguity. | `NotRun` |
| Lossless tokens, duplicate preservation, explicit binding and no Unity/runtime object creation | `DefensiveDesign` | Project policy | Preservation and architecture decisions. | Fail closed without deleting or repairing records. | `NotRun` |

## Default project policies

The proposed strict Core policy is to:

- preserve raw key and value text;
- preserve every token, including empty and trailing tokens;
- preserve duplicate records and duplicate sections;
- reject numeric overflow without replacing it with zero;
- keep unknown fields and extension tails;
- make record-layout, coordinate, owner, registry, extension, duplicate, and round-trip policies explicit inputs;
- produce structured diagnostics instead of deleting or repairing objects;
- validate syntax, coordinate domain, owner binding, type binding, references, and overlap as separate stages;
- use bounded record/token counts and checked arithmetic;
- share one state machine across Memory, Stream, short-read Stream, and MIX entry windows.

## Documents

- `family-and-section-boundaries.md` — per-section family contracts.
- `object-record-field-layouts.md` — field-index comparisons and raw layouts.
- `cell-and-coordinate-encodings.md` — scenario-cell and coordinate profiles.
- `identity-ownership-and-type-binding.md` — keys, houses, registries, and type identity.
- `health-facing-mission-and-state.md` — raw state fields and interpretation boundaries.
- `tag-trigger-and-team-reference-boundaries.md` — opaque reference graph.
- `terrain-smudge-and-waypoint-records.md` — non-techno placement families.
- `map-local-rules-art-binding.md` — ordered Rules/Art composition and visual references.
- `source-comparison.md` — pinned public sources, licenses, and lineage.
- `implementation-boundaries.md` — proposed Core models and policies.
- `test-matrix.md` — 140 design cases.
- `baseline-audit-request.md` — sanitized future local audit.
- `unresolved-questions.md` — evidence gaps and follow-up work.

## Non-goals

This dossier does not:

- implement placement or CSV parsing;
- modify the existing INI parser;
- build Rules or Art typed views;
- create houses, players, units, buildings, terrain objects, smudges, waypoints, or triggers;
- decode event/action opcodes or run teams/scripts;
- load SHP, VXL, HVA, TMP, palettes, or images;
- create Unity types or assets;
- implement collision, pathfinding, combat, AI, or rendering;
- read ProjectBaseline;
- run Unity, RA2/YR, FinalAlert, WAE, or XCC;
- modify maps, compatibility status, ADRs, formal source ledgers, tests, or existing research.
