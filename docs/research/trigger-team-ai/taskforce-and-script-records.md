# TaskForce and ScriptType records

> **Source notice:** ChatGPT Web public-source research. No local `ProjectBaseline` access. Not a Codex artifact. No GPL or unclear-license code was copied, translated, mechanically rewritten, or ported. `code_imported: false`.

## 1. Separate template families

A TaskForce describes a composition template. A ScriptType describes ordered instructions. A TeamType links them.

```text
TaskForce template ≠ TeamType ≠ runtime Team instance
ScriptType template ≠ Trigger Action list ≠ unit Mission token
```

## 2. TaskForce list and subsection

Common structure:

```ini
[TaskForces]
0=TaskForceId

[TaskForceId]
0=Count,TechnoType
1=Count,TechnoType
Group=-1
Name=EditorName
```

The list key, TaskForce ID, entry key, count, and Rules type are separate raw fields.

## 3. TaskForce entry order

WAE models six slots. ModEnc documents indices 0–5 read in ascending order.

Project view preserves:

- physical key order;
- numeric key candidate;
- gaps;
- duplicates;
- keys outside 0–5;
- unknown named properties;
- raw entry value.

The six-entry limit is strong community/editor evidence, not elevated to official runtime source.

## 4. TaskForce entry layout

Candidate:

```text
TaskForceEntryRaw
- EntryKeyRaw
- CountRaw
- TypeRaw
- ExtraTokens[]
```

WAE requires two tokens and rejects counts below 1 in its editor parser. Core does not inherit that repair/rejection as semantic truth.

Core records:

- signed/unsigned count candidates;
- zero/negative count;
- overflow;
- unknown type;
- duplicate type entries;
- extra tokens;
- missing tokens.

## 5. Type binding

A TaskForce entry may target a union of:

- AircraftType;
- VehicleType;
- InfantryType;
- extension-defined TechnoType families.

Binding uses composed Rules registries. It does not load art or create units.

If the same logical type appears multiple times, preserve each entry. Community reports runtime limitations, but parser does not deduplicate.

## 6. Group and Name

- `GroupRaw` is preserved as a numeric/sentinel candidate;
- `NameRaw` is editor/display metadata candidate;
- neither determines TaskForce identity;
- unknown named keys remain raw.

## 7. TaskForce list composition

Global and map-local TaskForces can coexist. Preserve:

- source layer;
- list occurrence;
- ID spelling;
- global/local candidate;
- duplicate ID;
- winner/suppressed provenance under any selected composition policy.

Do not strip `-G` or interpret hexadecimal-looking prefixes as pointers.

## 8. ScriptType list and subsection

Common structure:

```ini
[ScriptTypes]
0=ScriptTypeId

[ScriptTypeId]
0=Action,Argument
1=Action,Argument
Name=EditorName
```

Each step has a two-token candidate layout.

## 9. Script step identity and order

```text
ScriptStepRaw
- StepKeyRaw
- ActionRaw
- ArgumentRaw
- ExtraTokens[]
- SourceOccurrence
```

WAE reads contiguous numeric keys until the first missing index. ModEnc documents indices 0–49 in ascending order. These behaviors conflict with a fully lossless view.

Core preserves:

- gaps;
- duplicate step keys;
- out-of-range keys;
- source order;
- numeric-order candidate;
- steps after a gap.

No step is silently dropped because an earlier index is missing.

## 10. Script action opcodes

Script action opcodes form their own catalog. They are not:

- Trigger Event opcodes;
- Trigger Action opcodes;
- placement Mission strings;
- superweapon cursor Actions.

Suggested descriptor:

```text
ScriptActionDescriptor
- NumericValue
- ArgumentKindCandidates[]
- VersionProfile
- ExtensionProfile
- EvidenceGrade
```

It contains no execution code.

## 11. Argument candidates

A Script argument may be:

- target category;
- waypoint;
- Rules type/ordinal;
- duration;
- distance;
- mission;
- jump target;
- transport behavior;
- boolean/enum;
- extension-defined integer;
- unknown.

Negative and oversized values remain raw.

## 12. Extension actions

Phobos documents Script actions in high numeric ranges, including `10000+`, `12000+`, and `14000+`. This demonstrates that:

- action ranges are extensible;
- argument semantics are opcode-specific;
- vanilla and extension catalogs must be separate;
- unknown high opcodes cannot be rejected as malformed solely by range.

No extension action becomes available without an explicit profile.

## 13. Script loops and jumps

Community documentation describes primitive loops/jumps for some Script actions. The parser only produces candidate edges such as:

```text
ScriptStep → ScriptStepKey candidate
```

It does not:

- change an instruction pointer;
- validate termination;
- move units;
- resolve targets;
- execute transport behavior.

Cycle detection is analytical only.

## 14. Unknown action handling

WAE's editor parser rejects negative action values and may stop at gaps. Project Core instead:

- preserves unknown action and argument;
- reports catalog miss;
- keeps subsequent steps;
- marks execution ineligible without a profile;
- supports lossless roundtrip analysis.

## 15. Count versus entry list

TaskForce has per-entry counts but no separate total-count token in the common subsection profile. ScriptType has no declared step count in the common profile.

Do not fabricate counts from the highest key without retaining gaps and source order.

## 16. Roundtrip boundary

Preserve:

- list keys and IDs;
- subsection names;
- entry/step keys;
- physical order;
- gaps and duplicates;
- raw count/type/action/argument;
- unknown named properties;
- editor metadata;
- extension fields.

Canonical reindexing is a later explicit writer policy.

## 17. Safety

- bounded list, entry, and step counts;
- bounded token length;
- checked numeric parsing;
- no allocation of unit instances from Count;
- no recursion for Script jumps;
- no asset lookup based on TypeRaw;
- no-progress protection for Stream readers.

## 18. Recommended models

```text
TaskForceRaw
TaskForceEntryRaw
TaskForceBindingResult

ScriptTypeRaw
ScriptStepRaw
ScriptStepInterpretation
```

All retain raw and derived views separately.
