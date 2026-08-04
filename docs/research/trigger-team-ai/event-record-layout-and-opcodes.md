# Event record layout and opcode boundaries

> **Source notice:** ChatGPT Web public-source research. No local `ProjectBaseline` access. Not a Codex artifact. No GPL or unclear-license code was copied, translated, mechanically rewritten, or ported. `code_imported: false`.

## 1. Aggregate record

The common candidate is:

```ini
[Events]
TriggerId=DeclaredCount,EventTuple1,EventTuple2,...
```

The key is a Trigger ID candidate, not a separate Event object ID.

Core preserves:

```text
TriggerReferenceRaw
DeclaredCountRaw
RawTokens[]
SourceOccurrence
DuplicateKeyGroup
```

## 2. WAE tuple model

At pinned WAE commit `b4c9481e9b00fb0a38739049a046f528b6054ce2`:

- the first token is parsed as the Event count;
- every Event begins with `ConditionIndex`/opcode;
- two parameter slots are the default/base count;
- the editor configuration can declare up to two additional parameter slots;
- therefore the stored tuple has `1 + 2 + AdditionalParams` tokens in that profile.

Candidate tuple:

```text
EventOpcodeRaw
Parameter0Raw
Parameter1Raw
AdditionalParameter0Raw?
AdditionalParameter1Raw?
```

The additional count comes from editor configuration, not from an independent length token inside each tuple.

## 3. Evidence classification

- base count of two parameters: `ConfirmedByIndependentImplementation` for WAE;
- up to two configured additions: `ConfirmedByIndependentImplementation` and extension/editor configuration evidence;
- exact original RA2/YR runtime tuple rule: `Unresolved`;
- Ares extended Event parameter forms: extension documentation, not vanilla;
- Event display names: editor/community labels only.

## 4. Count contract

Core records:

```text
DeclaredCount
ParsedTupleCount
TokenCount
ExpectedTokenCountCandidates
MissingTokens[]
ExtraTokens[]
```

It must not:

- reduce the declared count to what fits;
- discard tokens after the declared count;
- synthesize zero parameters;
- report success after a partial final tuple;
- reinterpret the first tuple token as the count if the actual count token is invalid.

Suggested diagnostics:

- `EventCountNotNumeric`;
- `EventCountNegative`;
- `EventCountBudgetExceeded`;
- `EventTupleTruncated`;
- `EventTupleExtraTokens`;
- `EventCountTupleMismatch`;
- `DuplicateEventsRecord`.

## 5. Opcode raw model

```text
EventOpcodeRawText
SignedIntegerCandidate
UnsignedIntegerCandidate
CatalogCandidates[]
SelectedDescriptor?
EvidenceGrade
```

Unknown, negative, or extension opcodes remain raw.

The parser must not map unknown opcodes to NoOp or delete the whole Trigger.

## 6. Opcode catalog boundary

An `EventOpcodeDescriptor` may contain:

- numeric value;
- version/extension profile;
- editor display name;
- community name;
- persistent/incidental/situational labels;
- parameter-slot descriptors;
- reference-kind candidates;
- evidence grade;
- source provenance.

It must not contain executable logic.

Ares documentation demonstrates that extension Events can use forms such as:

```text
opcode,parameter-mode,base-value,additional-value
```

and that semantic labels such as persistent or incidental affect future execution. This proves extension growth and the need for profile-scoped descriptors; it does not make Ares behavior vanilla.

## 7. Parameter slots

Each slot preserves:

```text
ParameterRaw
NumericCandidate
StringCandidate
ReferenceCandidates[]
SelectedInterpretation?
EvidenceGrade
```

Possible Event parameter candidates include:

- House;
- waypoint;
- TeamType;
- Trigger;
- ScenarioCellId;
- Rules type;
- local/global variable ID;
- count/comparison value;
- time/duration;
- difficulty or bit field;
- extension string;
- unknown.

A numeric value matching a Waypoint and House ordinal creates ambiguity; it is not automatically bound.

## 8. Event ordering

Event tuple source order is preserved.

Potential runtime questions include:

- whether all Events are evaluated in source order;
- whether AND/OR behavior is governed by Trigger fields, Event persistence, or engine state;
- whether incidental Events short-circuit;
- whether duplicate Event opcodes are legal.

These are executor questions. Parser output only records sequence.

## 9. Duplicate `[Events]` keys

Duplicate keys can represent:

- lossless duplicate records;
- editor corruption;
- layered-map collision;
- extension behavior;
- intentional but unsupported data.

Default result is a duplicate identity group. Do not concatenate tuple lists unless an explicit composition profile says so.

## 10. Unknown opcode handling

WAE refuses to load an Event index absent from its editor configuration to prevent data loss. This is a defensible editor strategy, but the project Core should be more lossless:

- capture the raw tuple;
- retain declared count and position;
- mark the opcode unknown;
- preserve all parameter slots and extra tokens;
- prevent semantic execution eligibility;
- allow lossless inspection and roundtrip policy decisions.

## 11. Negative and oversized values

- negative opcode: structurally numeric but semantically unresolved;
- value beyond signed 32-bit: preserve raw, report numeric overflow candidate;
- huge declared count: reject semantic expansion before allocating;
- extremely long token: enforce byte/character budgets while preserving diagnostic location.

No unchecked multiplication of count × tuple width.

## 12. Version and extension profiles

Suggested profiles:

- `TsVanillaEventLayoutCandidate`;
- `Ra2YrVanillaEventLayoutCandidate`;
- `FinalAlertEditorEventLayout`;
- `WaeConfiguredEventLayout`;
- `AresEventExtensionProfile`;
- `PhobosEventExtensionProfile`;
- `UnknownRawEventLayout`.

Profiles are selected externally. Do not probe profiles until one parses successfully.

## 13. Variables

Variable Events may refer to:

- map-local variables;
- Rules/global variables;
- numeric IDs;
- boolean or integer values depending on engine/extension;
- comparison operations.

Core produces variable-reference candidates only. It does not read or mutate current variable state.

## 14. Execution boundary

Not implemented here:

- polling world state;
- Event persistence memory;
- incidental callbacks;
- elapsed timers;
- object-entered-cell checks;
- object-destroyed checks;
- credit comparisons;
- local/global variable comparison;
- difficulty evaluation;
- AND/OR or short-circuit behavior.

## 15. Roundtrip requirements

A lossless roundtrip candidate retains:

- original count token;
- every opcode token;
- every parameter token;
- empty fields;
- extra tokens;
- tuple order;
- duplicate records;
- source key spelling;
- unknown extension data.

A canonical writer may be designed later, but cannot silently repair count mismatches by default.
