# AITriggerType record layout

> **Source notice:** ChatGPT Web public-source research. No local `ProjectBaseline` access. Not a Codex artifact. No GPL or unclear-license code was copied, translated, mechanically rewritten, or ported. `code_imported: false`.

## 1. Separate identity domain

An AITriggerType is not a map Trigger.

```text
AITriggerType definition
≠ Trigger/Tag graph
≠ TeamType
≠ runtime AI decision
≠ runtime Team instance
```

Its key is an AITrigger ID candidate.

## 2. Common 18-field profile

WAE declares and writes this candidate layout:

| Index | Raw field | Candidate meaning |
|---:|---|---|
| 0 | `NameRaw` | editor/display name |
| 1 | `PrimaryTeamRaw` | primary TeamType ID |
| 2 | `OwnerRaw` | House/owner selector |
| 3 | `TechLevelRaw` | minimum tech level candidate |
| 4 | `ConditionTypeRaw` | AI condition type/opcode candidate |
| 5 | `ConditionObjectRaw` | Rules type or sentinel |
| 6 | `ComparatorRaw` | encoded comparator blob |
| 7 | `InitialWeightRaw` | initial weight candidate |
| 8 | `MinimumWeightRaw` | minimum weight candidate |
| 9 | `MaximumWeightRaw` | maximum weight candidate |
| 10 | `MultiplayerRaw` | skirmish/multiplayer enable candidate |
| 11 | `Field11Raw` | unused/unknown candidate |
| 12 | `SideRaw` | side selector candidate |
| 13 | `BaseDefenseRaw` | base-defense/unused candidate |
| 14 | `SecondaryTeamRaw` | secondary TeamType ID or sentinel |
| 15 | `EasyRaw` | easy difficulty candidate |
| 16 | `MediumRaw` | medium difficulty candidate |
| 17 | `HardRaw` | hard difficulty candidate |

WAE requires exactly 18 nonempty tokens in its reader. Core must preserve empty tokens and unknown tails instead of inheriting that destructive behavior.

## 3. AITrigger key

Preserve:

```text
AITriggerIdRaw
SourceOccurrence
GlobalOrLocalCandidate
DuplicateIdentityGroup
CaseCollisionGroup
```

Community documentation states that AITriggerTypes can be global or map-local and differ from TaskForce/Script composition behavior. This remains profile-scoped community evidence.

## 4. Primary and secondary TeamType edges

```text
AITrigger.PrimaryTeamRaw   → TeamType candidate
AITrigger.SecondaryTeamRaw → TeamType candidate or sentinel
```

Missing TeamTypes remain dangling edges. The parser does not delete the AITrigger or create a Team.

## 5. Owner and side

`OwnerRaw` and `SideRaw` are different fields.

Potential candidates:

- House identity or selector;
- all/none sentinel;
- side ordinal;
- extension-defined selector.

Do not infer one from the other or create a player.

## 6. Condition fields

`ConditionTypeRaw` and `ConditionObjectRaw` form an opcode/profile-dependent condition candidate.

Possible object candidates include:

- TechnoType;
- BuildingType;
- special condition object;
- no-object sentinel;
- extension-defined registry.

Core does not evaluate ownership, power, cash, or object counts.

## 7. Comparator blob

WAE expects a 64-character comparator string and parses:

- an initial quantity segment;
- an operator candidate at a fixed textual position;
- remaining bytes/characters that it writes in a canonical form.

Community documentation describes the blob as quantity, comparison operator, and unresolved tail data.

Core model:

```text
AITriggerComparatorRaw
- RawText
- Length
- QuantityCandidates[]
- OperatorCandidate
- ReservedOrUnknownTailRaw
- ParseDiagnostics
- EvidenceGrade
```

Never discard the unknown tail or replace the blob with a canonical string during parsing.

## 8. Weights

The three weight fields are decimal-text candidates:

```text
InitialWeightRaw
MinimumWeightRaw
MaximumWeightRaw
```

Keep:

- original decimal spelling;
- invariant-culture numeric candidates;
- NaN/infinity/overflow diagnostics where relevant;
- ordering consistency analysis.

Do not clamp, reorder, or update weights.

## 9. Weight execution boundary

Not implemented:

- current dynamic weight;
- reward/penalty after team completion;
- random selection;
- minimum/maximum enforcement;
- multiplayer AI scheduling;
- Phobos/Ares weight extensions.

Phobos Script actions that modify AITrigger weights are extension execution features, not parser behavior.

## 10. Difficulty fields

`EasyRaw`, `MediumRaw`, and `HardRaw` remain independent boolean candidates.

Invalid values are not replaced by defaults. `AITriggerTypesEnable` is a separate map-local enablement layer and does not overwrite these raw fields.

## 11. Enablement section

Candidate:

```ini
[AITriggerTypesEnable]
AITriggerId=BooleanRaw
```

Model separately:

```text
AITriggerEnableRecordRaw
- IdRaw
- ValueRaw
- BooleanCandidate
- ResolutionToAITrigger
```

Missing or duplicate AITrigger IDs remain explicit.

## 12. Field 11 and base-defense conflicts

Public sources call field 11 unused/unknown and field 13 base-defense or effectively unused in some versions/tools.

Do not delete either. Store exact raw tokens and profile-specific semantic candidates.

## 13. Count and token handling

Diagnostics include:

- fewer than 18 tokens;
- more than 18 tokens;
- empty token;
- invalid weight;
- comparator length mismatch;
- invalid comparator operator;
- missing TeamType;
- missing condition object;
- invalid difficulty boolean;
- duplicate AITrigger ID;
- extension tail present.

No editor default is inserted.

## 14. Global/local composition

Preserve:

- `ai(md).ini` contribution;
- map-local contribution;
- exact source order;
- duplicate identity;
- map enablement override;
- winner and suppressed provenance under a selected policy.

Do not infer scope from `-G` alone.

## 15. Recommended model

```text
AITriggerTypeRaw
- IdRaw
- Tokens[]
- ExtraTokens[]
- SourceLayer
- SourceOccurrence

AITriggerInterpretation
- TeamEdges
- OwnerCandidate
- ConditionCandidate
- ComparatorCandidate
- WeightCandidates
- DifficultyCandidates
- EnablementCandidate
- Diagnostics
```

## 16. Extension boundary

Ares/Phobos may add condition types, Script actions, target groups, and AI behavior. Use an explicit extension profile. Unknown fields and opcodes remain raw.

## 17. Execution boundary

The parser does not:

- inspect current enemy assets;
- compare cash or power;
- calculate production decisions;
- create either TeamType;
- modify production queues;
- adapt weights;
- choose difficulty;
- schedule attacks.

A future deterministic AI subsystem consumes the declarative definition.
