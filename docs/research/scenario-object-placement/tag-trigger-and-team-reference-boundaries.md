# Tag, trigger, team, and reference boundaries

> **Source notice:** ChatGPT Web public-source research only. ProjectBaseline was not read. This is not a Codex Agent artifact. No GPL or unclear-license code was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

## Scope

This dossier identifies reference-bearing fields and target identity candidates. It does not decode trigger event/action opcodes, execute triggers, form teams, run scripts, or implement AI.

## Opaque reference model

```text
OpaqueScenarioReference
- SourceRecord
- SourceFieldIndexOrKey
- RawTargetText
- ScenarioReferenceKindCandidate
- TargetIdentityProfile
- ResolutionCandidates
- Status
- EvidenceGrade
- Diagnostics
```

Reference resolution never overwrites the raw token.

## Techno Tag field

Structures, Units, Infantry, and Aircraft common profiles contain a Tag token. Common sentinels include `None`, `<none>`, or tool-specific equivalents.

The candidate path is:

```text
Placement TagRaw
→ [Tags] key/ID candidate
→ Tag value's Trigger ID candidate
→ [Triggers] entry candidate
```

Only the first opaque edges belong here. Event and action records remain uninterpreted.

WAE looks up a Tag by ID and logs a missing target without attaching it. This is `ImplementationSpecificBehavior`. A strict Core result preserves the raw Tag and returns `DanglingTagReference`; it does not clear the field. That preservation rule is `DefensiveDesign`.

## CellTags

`[CellTags]` uses:

```text
ScenarioCellId = TagId
```

The cell and Tag sides are independently validated. A valid cell with a missing Tag and an invalid cell with a known Tag are different outcomes.

Duplicate CellTag keys can represent:

- duplicate raw keys;
- normalized cell collisions such as different decimal spellings;
- conflicting Tag targets;
- byte-identical duplicates.

All occurrences are preserved.

## Tags and Triggers

The Tag section itself has a layout and ID semantics outside the placement record. This dossier only needs a target descriptor:

```text
ScenarioTagIdentity
- KeyOrIdRaw
- SourceOccurrence
- TriggerReferenceRawCandidate
- DuplicateIdentityGroup
```

A missing Trigger produces a dangling second-stage edge. A circular relationship is reported as graph structure, not executed.

No same-name Tags are silently merged. Case matching is controlled by an explicit reference policy.

## Trigger execution is out of scope

The reference graph does not:

- parse Events/Actions opcodes;
- evaluate conditions;
- dispatch object events;
- mutate houses, teams, map cells, or variables;
- determine whether a Tag is legal for a type;
- remove dangling references.

A later M3-R8 package can consume the preserved graph.

## Waypoint references

Waypoints can be targets of campaign, reinforcement, camera, team, or multiplayer semantics. This dossier preserves waypoint identities and coordinates, but does not assign special gameplay meaning to waypoint 0, 1, or other ranges without an explicit profile.

A reference to a missing waypoint remains opaque and dangling.

## Unit FollowsIndex

`FollowsIndex` can target another Unit, but the target identity basis is unresolved. Candidate bases:

- source occurrence index in `[Units]`;
- numeric record key;
- writer-canonical order;
- runtime list index;
- another numeric state.

WAE writes the current in-memory Units list index and uses `-1` for no target.

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| WAE writes an in-memory Units list index and `-1` sentinel | `ImplementationSpecificBehavior` | World-Altering Editor | Named writer behavior only. | Preserve as a WAE profile. | `NotRun` |
| Original-runtime Follows target identity basis | `Unresolved` | No original-runtime source located | Numeric key, occurrence, writer order and runtime index can disagree. | Resolver must require explicit basis. | `NotRun` |
| Multiple candidate bases resolve to different targets | `ConflictingSources` | Tool behavior and identity candidates | A successful lookup is not proof. | Return `AmbiguousReferenceBasis`; never plausibility-select. | `NotRun` |

## Group

`GroupRaw` is not automatically a TeamType reference. It may be a recruitment grouping value. A numeric group that equals a record key or TeamType ID does not establish an edge.

The graph stores a `GroupSemanticCandidate` only when a profile supports it.

## TeamType, TaskForce, and ScriptType

These sections have their own IDs and relationships:

```text
TeamType
→ TaskForce candidate
→ ScriptType candidate
→ owner/house candidate
```

Placement records may affect recruitment through Group and recruitment flags, but this dossier does not connect them automatically to a TeamType.

Only explicit source fields create reference candidates.

## AITriggerTypes

`[AITriggerTypes]` is included only as a possible downstream graph target. Its fixed-width record and runtime behavior are outside scope.

No placement record is considered invalid solely because an AI trigger or team graph cannot be resolved.

## Structure upgrade references

Structure fields 12–14 in the common profile are BuildingType reference candidates. They use the Rules type-binding path, not the Tags/Triggers path.

The graph distinguishes:

- placement type reference;
- upgrade type reference;
- Tag reference;
- follows reference;
- waypoint reference;
- house reference.

## None and null sentinels

Sentinel recognition is profile-specific. Store:

```text
ReferenceRaw
SentinelCandidate
SentinelProfile
```

Do not canonicalize every spelling to `None` during parsing. An empty token is not automatically equivalent to a none sentinel.

## Duplicate and circular graph behavior

The graph can contain:

- duplicate target IDs;
- multiple candidate targets;
- missing targets;
- cycles;
- self references;
- multiple placements attached to one Tag;
- one CellTag and one techno record targeting the same Tag.

All are representable. No graph cleanup occurs in Core.

## Structured resolution outcomes

Suggested statuses:

- `ResolvedUnique`;
- `ResolvedToSentinel`;
- `DanglingReference`;
- `DuplicateTargetIdentity`;
- `AmbiguousReferenceBasis`;
- `InvalidTargetSyntax`;
- `UnsupportedReferenceKind`;
- `CycleDetected`;
- `NotAttempted`.

## Round-trip

Lossless round-trip preserves raw IDs, casing, sentinel spelling, unresolved targets, duplicate sections, and source order. A future canonical writer cannot rename Tags or renumber records without an explicit graph-aware migration policy. This is `DefensiveDesign`.
