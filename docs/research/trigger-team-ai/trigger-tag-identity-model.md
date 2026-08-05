# Trigger and Tag identity model

> **Source notice:** ChatGPT Web public-source research. No local `ProjectBaseline` access. Not a Codex artifact. No GPL or unclear-license code was copied, translated, mechanically rewritten, or ported. `code_imported: false`.

## 1. Identity chain

```text
placement Tag field ──> Tag ID
CellTags value ───────> Tag ID
Tag record ───────────> Trigger ID
Trigger key ──────────> Events key candidate
Trigger key ──────────> Actions key candidate
```

Tag and Trigger are separate identity domains. A Tag attaches to a cell/object candidate and references a Trigger; the Trigger owns declarative Event and Action records.

## 2. Trigger raw record

A common tool profile writes:

```ini
[Triggers]
TriggerId=Owner,LinkedTrigger,Name,Disabled,Easy,Normal,Hard,ReservedOrRepeating
```

The final token has conflicting repeating/reserved/unused terminology. WAE writes eight fields and FinalAlert exposes repair/default behavior, but neither establishes the original-runtime malformed-input contract.

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert repairs/defaults Trigger fields and manages IDs in editor workflows | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Official editor behavior only. | Preserve as named editor profile; do not inherit repairs. | `NotRun` |
| WAE parses/writes common Trigger and Tag forms | `ImplementationSpecificBehavior` | World-Altering Editor | Named editor behavior. | Source-pinned comparison profile. | `NotRun` |
| Common Trigger/Tag shapes and sentinel spellings | `Underconfirmed` | WAE and community documentation | Convergence does not prove runtime strictness or independent lineage. | Preserve raw fields and tails. | `NotRun` |
| Trigger tail and linked/repeat semantics | `ConflictingSources` | Editors and community terminology | Meanings differ by source/profile. | Retain neutral raw names and conflict set. | `NotRun` |
| Runtime ID case, duplicate, repair, cycle and malformed behavior | `Unresolved` | No original-runtime source located | No reliable unique contract. | Explicit identity/reference policy. | `NotRun` |

## 3. Trigger identity

The left-hand key is the leading Trigger ID candidate. Core preserves raw spelling, source occurrence, normalized candidates, duplicate groups and case-collision groups. It does not require GUID forms, derive identity from display names, infer `-G` semantics, regenerate IDs, or merge case variants automatically.

## 4. Events and Actions association

WAE associates `[Events][TriggerId]` and `[Actions][TriggerId]` with the Trigger key. This is `ImplementationSpecificBehavior`. Core also preserves orphan Events/Actions, Triggers missing one side, and duplicate records. Missing data is never fabricated.

## 5. Linked Trigger

The linked field remains an opaque Trigger-reference candidate or sentinel. Core preserves self-loops and cycles, reports them, and does not execute or break chaining.

## 6. Tag raw record

A common WAE profile is:

```ini
[Tags]
TagId=RepeatRaw,NameRaw,TriggerIdRaw
```

WAE's repeat range is editor behavior. Any raw token, empty field, sentinel spelling and unknown tail remains preserved.

## 7. Attachments

Placement Tag fields and `[CellTags]` values reference Tag candidates. CellTag keys remain scenario-cell identities and never become Tag IDs. Missing/duplicate/case-colliding Tags produce structured reference states without mutating placement or coordinate data.

## 8. Sentinels and case

Sentinel recognition is field/profile-specific. Exact case and spelling remain raw; case-folded and sentinel candidates are derived views. An empty token is not automatically equivalent to `None`.

## 9. Duplicates

Duplicate Trigger/Tag identities preserve every occurrence and referrer. Default resolution is ambiguity, not first/last-wins. This behavior, together with raw preservation and no ID regeneration, is `DefensiveDesign`.

## 10. Recommended raw models

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

Both retain token spelling, empties, whitespace, source line, section occurrence and source order.
