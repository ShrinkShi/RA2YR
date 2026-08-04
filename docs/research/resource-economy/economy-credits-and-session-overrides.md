# Economy credits and session overrides

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明的实现仅作行为、冲突和架构参考，未复制、逐句翻译、机械改写或移植其采集、经济、AI、随机、寻路或测试代码。`code_imported: false`。

## 1. Economy-source graph

```text
Rules defaults
map House credits
Basic carry-over candidates
campaign persistence
multiplayer lobby credits
game-mode override
scenario/Trigger/crate mutations
refinery deliveries
runtime player account
AI observations
score/statistics
```

No single parser field defines final money.

## 2. Descriptor

```text
EconomySourceDescriptor
- SourceKind
- RawValue
- NumericCandidates
- SourceLayer
- Scope
- HouseOrPlayerReference?
- ScenarioModeCandidates
- ProductProfile
- Evidence
- Diagnostics
```

## 3. Override layers

```text
EconomyOverrideLayer
- LayerId
- PriorityCandidate
- ApplicabilityPredicate
- ValueCandidate
- MergeModeCandidate
- Provenance
- ConflictPolicy
```

Candidate merge modes:

```text
Replace
Add
Clamp
CarryOver
CapCarryOver
Ignore
ExtensionDefined
Unknown
```

The parser does not select precedence.

## 4. Starting credits candidates

Strictly distinct:
- House `Credits`;
- `[Basic] CarryOverMoney`;
- `CarryOverCap`;
- campaign previous-mission state;
- multiplayer/lobby starting money;
- game-mode/client override;
- runtime initialization account.

Map metadata can produce an authored candidate, not the final session value.

## 5. Runtime accounts

```text
RuntimeCreditAccount
- CashCandidate
- PhysicalStoredResourceCandidate
- TotalSpendableCandidate
- EarnedStatistic
- SpentStatistic
- CapacityCandidate
- SimulationTick
```

Whether stock RA2/YR merges physical ore and cash internally, and under which silo mode, remains product/profile scoped.

## 6. Refinery delivery

Refinery delivery produces an `EconomyMutationCandidate` only after cargo acceptance:

```text
ResourceType
AcceptedUnits
ValuePerUnitCandidate
ModifierCandidates
PhysicalStorageOutcome
CashOutcome
OverflowOutcome
Owner
Tick
```

Editor map-resource estimates do not authorize this mutation.

## 7. Storage and silo

Separate:
- harvester cargo;
- refinery acceptance;
- building storage capacity;
- player physical resource;
- cash;
- displayed credits;
- score.

Ares documentation describes optional restored storage behavior; OpenRA independently models cash/resources/capacity. Neither is automatically stock YR fact.

## 8. Storage capacity changes

Future simulation commands:

```text
AddStorageCapacity
RemoveStorageCapacity
DiscardExcess
TransferStoredResource
ConvertStoredResourceToCash
OwnerChanged
```

Sell/destruction/capture outcomes remain P0.

## 9. Crates

A crate may produce:
- credits;
- resource;
- actor;
- effect;
- extension behavior.

Crate Overlay is not a resource cell. Crate credit mutation belongs to runtime command execution.

## 10. Trigger boundary

Possible Trigger/Event/Action categories:
- compare credits;
- modify credits;
- compare resource amount;
- create/remove resource;
- enable growth/spread;
- create harvester/refinery;
- AI economy condition.

Only opcode/parameter candidates are recorded. Editor display names are not complete runtime semantics.

## 11. AI economy boundary

```text
ScenarioResourceState
PlayerEconomyState
AIEconomyObservation
AIDecision
ProductionCommand
```

Observations may include:
- harvester/refinery counts;
- resource scan;
- estimated field value;
- credits/storage;
- threatened harvester;
- difficulty;
- campaign/script refs.

AI does not live in format Core and is not implemented.

## 12. Score and statistics

```text
CurrentCredits
PhysicalStoredResource
CreditsDelivered
Earned
Spent
ResourceHarvested
Score
AIResourceEstimate
```

These values may coincide numerically but remain independent semantic fields.

## 13. Arithmetic and determinism

Future economy policy must define:
- integer/fixed-point;
- checked multiplication;
- modifier order;
- rounding;
- saturation vs failure;
- negative values;
- max credits;
- transaction order;
- replay/savegame serialization.

OpenRA’s choices are independent implementation evidence only.

## 14. Session precedence diagnostics

Required diagnostics:
- multiple starting-credit sources;
- lobby value outside allowed set;
- carry-over without campaign context;
- carry-over cap conflict;
- House reference missing;
- negative/overflow value;
- map-local override collision;
- unsupported merge mode;
- extension source on vanilla profile.

## 15. No implementation

No player account, session, lobby, Trigger executor, crate mutation, refinery credit transfer, AI decision or UI counter is implemented.
