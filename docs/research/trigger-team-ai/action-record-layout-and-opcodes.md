# Action record layout and opcode boundaries

> **Source notice:** ChatGPT Web public-source research. No local `ProjectBaseline` access. Not a Codex artifact. No GPL or unclear-license code was copied, translated, mechanically rewritten, or ported. `code_imported: false`.

## 1. Aggregate record

The common candidate is:

```ini
[Actions]
TriggerId=DeclaredCount,ActionTuple1,ActionTuple2,...
```

The key is a Trigger ID candidate. Action order is preserved.

## 2. Fixed raw tuple candidate

WAE uses:

```text
ActionOpcodeRaw
Parameter0Raw
Parameter1Raw
Parameter2Raw
Parameter3Raw
Parameter4Raw
Parameter5Raw
Parameter6Raw
```

That is one opcode plus seven parameter slots, eight tokens per Action tuple.

This is strong independent-editor evidence for TS/RA2/YR map storage. It is not complete original-runtime source evidence.

## 3. Why all seven slots must remain

Different opcodes reuse the same physical slots for different meanings. Some slots are unused for a specific opcode but still exist in the record.

Core must retain:

- nonzero unused slots;
- string-looking values in numeric-looking slots;
- sentinel spellings;
- invalid values;
- extension values;
- the seventh parameter's exact text.

It must not create an opcode-specific compact record that discards unused slots.

## 4. Special seventh-slot evidence

WAE's writer contains editor compatibility handling for the final parameter slot:

- if the editor catalog declares the slot used, it writes the stored value;
- otherwise it writes `A`, described by the editor as a representation for waypoint 0;
- for unknown Action types it preserves the stored seventh slot.

This demonstrates that:

- a parameter slot may be encoded as a nonnumeric string;
- editor defaults can rewrite data;
- unknown opcodes need lossless preservation;
- `A` cannot be globally parsed as a normal integer.

The Core parser must not copy WAE's rewrite policy.

## 5. Count contract

Record:

```text
DeclaredCountRaw
DeclaredCountCandidate
ParsedTupleCount
ExpectedTokenCountCandidate = 1 + count × 8
MissingTokens[]
ExtraTokens[]
```

Use checked arithmetic before calculating expected token counts.

Diagnostics:

- invalid/negative count;
- budget-exceeding count;
- truncated tuple;
- extra tuple or tail tokens;
- duplicate Actions key;
- count mismatch;
- token numeric overflow.

No partial success after a truncated Action.

## 6. Opcode evidence layers

For every numeric opcode, distinguish:

1. numeric value in source;
2. official editor display name;
3. editor parameter labels;
4. community name;
5. independent implementation behavior;
6. extension documentation;
7. original runtime behavior.

Agreement between UI labels does not prove runtime semantics.

## 7. Action descriptor candidate

```text
ActionOpcodeDescriptor
- NumericValue
- ProfileId
- DisplayNames[]
- ParameterSlots[7]
- ExtensionOwner
- EvidenceGrade
- Sources[]
```

A descriptor may explain candidates, but contains no execution callback.

## 8. Parameter families

Action slots may represent candidates such as:

- numeric amount;
- House ID;
- TeamType ID;
- Trigger ID;
- Tag ID;
- Waypoint;
- ScenarioCellId;
- local/global variable ID;
- text/string table key;
- movie, theme, sound, speech, or briefing reference;
- superweapon index/type;
- Rules object type;
- boolean or bit field;
- duration;
- percentage;
- extension-specific string;
- unused/unknown.

Each slot can have multiple simultaneous candidates.

## 9. Unknown and extension opcodes

Unknown Action opcodes must not become NoOp.

Required behavior:

- preserve opcode and all seven slots;
- report catalog miss;
- preserve source order;
- keep the owning Trigger and Tag graph;
- mark future execution ineligible without a selected extension profile;
- permit lossless inspection and roundtrip.

Ares documents additional Trigger Actions, and Phobos documents further Trigger and Script actions. They are extension evidence only.

## 10. Strings and numeric ambiguity

Some Action slots are textual by opcode profile. Therefore:

- CSV tokenization must preserve text exactly;
- numeric parsing is a candidate, not a destructive conversion;
- empty text is different from `0`;
- `A` is different from numeric zero;
- whitespace and case may matter for string-table or logical IDs;
- values exceeding 32-bit remain raw with overflow diagnostics.

## 11. Ordering

Action sequence is semantically significant candidate data.

Do not:

- sort by opcode;
- group similar actions;
- remove apparent NoOps;
- deduplicate byte-identical Actions;
- move extension Actions to the end;
- execute Actions while parsing.

## 12. Duplicate `[Actions]` keys

Duplicate key policy is explicit:

- preserve all occurrences;
- form a duplicate identity group;
- do not concatenate by default;
- do not use last-wins;
- record any composition policy and evidence grade.

## 13. Difficulty and Trigger control

Difficulty booleans are stored in the Trigger record candidate, not repeated per Action in the common layout. An Action interpreter must not independently infer difficulty from parameters unless its explicit opcode profile says so.

## 14. Variables

Variable-related Action profiles may represent:

- set;
- clear;
- toggle;
- arithmetic edit;
- local/global selection;
- numeric values wider than boolean.

Core only emits variable-reference and operation candidates. It does not mutate a variable store.

## 15. Execution adapter boundary

A future Action executor would need:

- validated Action descriptor;
- resolved references;
- deterministic command sink;
- world and House state;
- simulation clock;
- object identity registry;
- variable state;
- audio/video/UI adapters where applicable;
- savegame state.

None belongs in the parser or declarative graph builder.

## 16. Safety

- bound Action count;
- bound tokens and token length;
- checked `count × 8`;
- no recursive Trigger execution during graph construction;
- no resource loading from parameter strings;
- no filesystem access based on Action text;
- no allocations controlled by an unvalidated amount parameter.

## 17. Roundtrip

Preserve:

- count spelling;
- opcode spelling;
- all seven slot spellings;
- empty/unused slots;
- `A` and other string sentinels;
- unknown opcodes;
- extra/trailing tokens;
- duplicate Actions records;
- physical order.

Canonical editor rewrite, runtime acceptance, and gameplay equivalence are separate later validations.
