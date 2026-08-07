# Event record layout and opcode boundaries

> **Source notice:** ChatGPT Web public-source research. No local `ProjectBaseline` access. Not a Codex artifact. No GPL or unclear-license code was copied, translated, mechanically rewritten, or ported. `code_imported: false`.

## 1. Aggregate record

```ini
[Events]
TriggerId=DeclaredCount,EventTuple1,EventTuple2,...
```

The key is a Trigger ID candidate, not a separate Event object ID. Core preserves the Trigger reference, declared count, raw tokens, source occurrence and duplicate-key group.

## 2. WAE tuple model

At pinned WAE commit `b4c9481e9b00fb0a38739049a046f528b6054ce2`:

- the first token is the Event count;
- every Event begins with an opcode;
- two parameter slots are the base count;
- editor configuration can declare up to two additional parameter slots;
- tuple width is therefore profile/configuration dependent.

```text
EventOpcodeRaw
Parameter0Raw
Parameter1Raw
AdditionalParameter0Raw?
AdditionalParameter1Raw?
```

## 3. Normalized evidence

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| WAE uses two base parameters | `ImplementationSpecificBehavior` | World-Altering Editor | Named editor behavior only. | Preserve a WAE profile. | `NotRun` |
| WAE supports up to two configured additional parameters | `ImplementationSpecificBehavior` | WAE configuration | May include extension/editor catalog behavior. | Profile-scoped tuple width. | `NotRun` |
| Community and extension catalogs describe Event opcode/parameter shapes | `ConfirmedCommunityConvention` | ModEnc/Ares/Phobos documentation | Stable catalog conventions are not runtime execution proof. | Provenance per descriptor. | `NotRun` |
| A single universal vanilla Event tuple width is established | `ConflictingSources` | Official editor, WAE and extension catalogs | Base and additional slot models differ by profile. | Preserve count, tokens and unknown tails. | `NotRun` |
| Exact original-runtime Event tuple parsing and evaluation | `Unresolved` | No original-runtime source located | Runtime malformed-input and execution behavior remain unknown. | Future executor separate. | `NotRun` |
| No count repair, no zero synthesis, no profile probing and raw opcode retention | `DefensiveDesign` | Project policy | Preservation/fail-closed behavior. | Checked counts and explicit profile. | `NotRun` |

## 4. Count contract

Core records declared count, parsed tuple count, token count, expected-token candidates, missing tokens and extra tokens. It never reduces the count to what fits, discards tail tokens, synthesizes parameters, reports success after a partial tuple, or retries layouts until one appears plausible.

Diagnostics include invalid/negative/oversized count, tuple truncation, extra tokens, count mismatch and duplicate Events records. Arithmetic is checked before count × tuple-width calculations.

## 5. Opcode raw model

```text
EventOpcodeRawText
SignedIntegerCandidate
UnsignedIntegerCandidate
CatalogCandidates[]
SelectedDescriptor?
EvidenceGrade
```

Unknown, negative, overflowed and extension opcodes remain raw. They are not mapped to NoOp and do not delete the Trigger.

An opcode descriptor may contain numeric value, version/profile, neutral labels, parameter-slot descriptors, reference-kind candidates, evidence and provenance. It contains no executable callback.

## 6. Parameter slots

Each slot preserves raw text plus numeric, string and reference candidates. Possible meanings include House, waypoint, TeamType, Trigger, cell ID, Rules type, variable ID, comparison value, duration, difficulty/bit field, extension string or unknown. Matching a known identity does not select the interpretation.

## 7. Ordering and duplicates

Event source order is preserved. Duplicate `[Events]` keys form a duplicate identity group and are not concatenated, deduplicated, first-wins or last-wins without an explicit composition policy.

## 8. Editor validation boundary

WAE may refuse an opcode absent from its editor configuration. FinalAlert/WAE validation and display catalogs are tool behavior, not runtime legality. Core captures unsupported tuples, marks semantic execution ineligible under the selected catalog, and retains them for inspection and roundtrip.

## 9. Profiles

Suggested explicit profiles include TS, RA2/YR, FinalAlert editor, WAE configured, Ares extension, Phobos extension and unknown-raw layouts. Selection occurs before parsing; trial parsing and plausibility selection are prohibited.

## 10. Execution boundary

Polling world state, persistence, callbacks, timers, object/cell checks, credits, variable comparisons, difficulty and AND/OR behavior belong to a future executor, not this parser.

## 11. Roundtrip

Preserve original count, opcode and parameter text, empty/additional/tail tokens, tuple order, duplicate records, source-key spelling and unknown extension data. A canonical writer cannot silently repair mismatches by default.
