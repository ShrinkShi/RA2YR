# Trigger and Tag identity model

> **Source notice:** ChatGPT Web public-source research. No local `ProjectBaseline` access. Not a Codex artifact. No GPL or unclear-license code was copied, translated, mechanically rewritten, or ported. `code_imported: false`.

## 1. Identity chain

The strongest public model is:

```text
placement Tag field ──> Tag ID
CellTags value ───────> Tag ID
Tag record ───────────> Trigger ID
Trigger key ──────────> Events key candidate
Trigger key ──────────> Actions key candidate
```

Tag and Trigger are separate objects. A Tag can be attached to a cell or object and points to a Trigger. A Trigger owns declarative conditions and actions.

## 2. Trigger raw record

A common TS/RA2/YR editor profile writes:

```ini
[Triggers]
TriggerId=Owner,LinkedTrigger,Name,Disabled,Easy,Normal,Hard,ReservedOrRepeating
```

Candidate token map:

| Index | Raw field | Candidate meaning | Evidence |
|---:|---|---|---|
| 0 | `OwnerRaw` | House/owner identity | official-editor and independent-editor evidence |
| 1 | `LinkedTriggerRaw` | linked Trigger ID or sentinel | editor evidence |
| 2 | `NameRaw` | display/editor name | editor evidence |
| 3 | `DisabledRaw` | disabled boolean candidate | editor evidence |
| 4 | `EasyRaw` | easy difficulty candidate | editor evidence |
| 5 | `NormalRaw` | normal difficulty candidate | editor evidence |
| 6 | `HardRaw` | hard difficulty candidate | editor evidence |
| 7 | `TailRaw` | repeating/reserved/unused candidate | conflicting editor/community terminology |

WAE parses at least seven fields and writes eight. EA's public editor contains a `RepairTrigger` routine that fills empty indices 3–7 with editor defaults. This proves an editor-repair path, not an original-runtime requirement.

## 3. Trigger key

The left-hand key is the strongest Trigger identity candidate.

Core saves:

```text
TriggerIdRaw
SourceOccurrence
NormalizedCandidate
DuplicateIdentityGroup
CaseCollisionGroup
```

It must not:

- require a GUID-like form;
- assume `-G` means a runtime type;
- derive identity from the display name;
- regenerate an ID because it is short or nonnumeric;
- merge two IDs differing only by case without an explicit profile.

## 4. Events and Actions association

WAE resolves:

```text
[Events][TriggerId]
[Actions][TriggerId]
```

using the Trigger ID. This is strong editor/reimplementation evidence that Events and Actions keys share the Trigger identity domain.

The raw model nevertheless preserves:

- Events records without a Trigger record;
- Actions records without a Trigger record;
- a Trigger without Events;
- a Trigger without Actions;
- duplicate Events keys;
- duplicate Actions keys.

No missing record is fabricated.

## 5. Linked Trigger field

The Trigger token at index 1 is a candidate reference to another Trigger.

Possible interpretations include:

- explicit link for chained Trigger behavior;
- sentinel indicating no link;
- version/editor compatibility field;
- extension-defined semantics.

Core creates an opaque Trigger→Trigger edge. It does not execute chaining or infer ordering.

Cycle handling:

- preserve every edge;
- report self-loop;
- report multi-node cycle;
- do not delete or break a cycle;
- do not infer runtime infinite-loop behavior.

## 6. Tag raw record

WAE writes:

```ini
[Tags]
TagId=RepeatRaw,NameRaw,TriggerIdRaw
```

Candidate map:

| Index | Raw field | Candidate meaning |
|---:|---|---|
| 0 | `RepeatRaw` | repetition/persistence/control type |
| 1 | `NameRaw` | display/editor name |
| 2 | `TriggerReferenceRaw` | Trigger ID or sentinel |

WAE exposes `Repeating` with a UI maximum candidate of 2, but this is editor behavior. The parser must preserve any raw token.

## 7. Tag key

The Tag key is distinct from:

- Trigger ID;
- display name;
- placement record key;
- TaskForce ID;
- runtime object ID.

Community documentation warns that several historical ID domains may interact with pointer-remapping conventions. This remains community evidence, not a reason to parse IDs as pointers.

## 8. Object attachment

Placement sections commonly contain a Tag field. M3-R7 established that this field remains raw until Tag binding.

Binding states:

- known no-tag sentinel;
- uniquely resolved Tag;
- missing Tag;
- duplicate Tag identity;
- case collision;
- malformed/empty token;
- unresolved extension sentinel.

Missing Tag never changes the placement record.

## 9. CellTag attachment

A common candidate is:

```ini
[CellTags]
ScenarioCellId=TagId
```

The CellTag key belongs to the scenario-cell identity domain, while its value belongs to the Tag identity domain.

Core must not:

- treat the CellTag key as a Tag ID;
- infer a Trigger directly from the CellTag value without traversing Tag;
- remove CellTags whose Tag is missing;
- rewrite coordinates to repair a dangling edge.

## 10. None/null sentinels

Public tools use several spellings such as `<none>`, `none`, empty strings, or editor constants.

Core stores:

```text
SentinelRaw
SentinelProfileCandidate
IsRecognizedBySelectedProfile
```

A sentinel profile is explicit and scoped to a field. A spelling recognized for Trigger links is not automatically accepted for TeamType, Tag, or House references.

## 11. Case sensitivity

Evidence is incomplete for original runtime identity matching.

Project policy:

- preserve exact case;
- generate case-folded candidates separately;
- report collisions;
- do not select a winner solely by source order;
- serialize the selected normalization policy with graph output.

## 12. Duplicate identities

For duplicate Trigger or Tag keys:

```text
DuplicateIdentityGroup
- RawId
- Occurrences[]
- CandidateNormalizedId
- Referrers[]
- ResolutionState
```

Default is ambiguity, not first-wins or last-wins.

## 13. FinalAlert canonicalization boundary

EA's public editor contains repair/default logic and may generate or normalize IDs through editor workflows. This establishes `ConfirmedByOfficialEditorSource` only.

It does not prove:

- original runtime requires editor-generated ID forms;
- re-saving is byte-identical;
- duplicate IDs are accepted;
- linked Trigger cycles are rejected;
- missing fields receive the same defaults in the game.

## 14. Recommended raw models

```text
ScenarioTriggerRaw
- IdRaw
- OwnerRaw
- LinkedTriggerRaw
- NameRaw
- DisabledRaw
- EasyRaw
- NormalRaw
- HardRaw
- TailRaw
- ExtraTokens[]

ScenarioTagRaw
- IdRaw
- RepeatRaw
- NameRaw
- TriggerReferenceRaw
- ExtraTokens[]
```

Both preserve original token spelling, empty tokens, whitespace, source line, section occurrence, and source order.
