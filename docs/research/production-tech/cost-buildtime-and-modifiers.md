> **Source notice:** This document was prepared by **ChatGPT Web** from public sources only. ProjectBaseline was not read. This is not a Codex artifact. GPL and unclear-license implementations are reference-only; no code was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

# Cost, build time and modifiers

## Independent values

```text
AuthoredCost
CurrentCostCandidate
DisplayedCost
CreditsReservation
CreditsDeduction
RefundCandidate
SellValueCandidate
BuildTimeCandidate
FactorySpeedModifier
CountryModifier
DifficultyModifier
PowerModifier
MultipleFactoryModifier
FactoryPlantModifier
FinalBuildDuration
DisplayedTime
```

They are not aliases.

## Cost candidates

Retain zero, negative, invalid and overflowing text. Potential consumers include:

- affordability;
- up-front reservation;
- progressive deduction;
- cancellation refund;
- sell/refund calculations;
- build-time input;
- sidebar ordering;
- AI budgeting;
- scenario starting units;
- extension effects.

ModEnc documents progressive stock-style deduction, cost-derived construction time and cost participation in sidebar ordering. This remains community evidence, not a complete runtime transaction trace.

## Build-time inputs

Candidate sources:

- `[General] BuildSpeed`;
- authored `Cost`;
- `BuildTimeMultiplier`;
- country/category multipliers;
- factory-count modifier;
- Factory Plant effect;
- low-power modifier;
- difficulty or campaign profile;
- game-speed presentation;
- Ares `BuildTime.*` fields;
- extension overrides.

Ares explicitly offers per-type speed, alternate time-cost, low-power and multiple-factory modifiers. They carry `ExtensionProvider=Ares` and are not stock defaults.

## Unit boundary

OpenRA uses its own tick and world models. UI clocks, OpenRA ticks and editor preview time do not establish Westwood units.

```text
BuildTimeExpressionCandidate
- BaseValueCandidate
- UnitProfile
- ModifierStages[]
- ClampCandidates[]
- RoundingPolicy
- EvidenceGrade
```

## Arithmetic requirements

- checked integer operations;
- bounded decimal token length;
- explicit rational/fixed-point scale;
- explicit modifier order;
- no locale-dependent parsing;
- no negative-to-zero repair;
- no overflow wrap;
- no floating-point authority unless an evidence-gated policy explicitly chooses it.

## Refund events

```text
QueueCancellation
PartialProgressCancellation
CompletedUnitAbort
CompletedBuildingPlacementCancel
FactoryDestruction
FactoryCapture
PlayerDefeat
SellBuilding
GrindUnit
```

Each event may use a different owner, basis and percentage. Presentation is downstream from the transaction.

## Factory Plant

Separate:

```text
FactoryPlantCapability
FactoryPlantRuntimeInstance
AffectedCategoryCandidate
AuthoredBonus
PerTypeExtensionMultiplier
FinalCostModifier
```

Ares `FactoryPlant.Multiplier` modifies an existing effect and is extension-only evidence. The stock order, stacking and rounding remain unresolved.

## Source anchors

- ModEnc `Cost`, `BuildTime`, `BuildTimeMultiplier` revisions.
- Ares 3.0 `Build Time` and `Factory Plant Effect` documentation.
- OpenRA production queue files at commit `a520984d91eda9de48a62b1d15c1e3bad0d4fb1a`, independent implementation only.

No formula code was copied; `code_imported: false`.
