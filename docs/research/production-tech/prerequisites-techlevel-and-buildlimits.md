> **Source notice:** This document was prepared by **ChatGPT Web** from public sources only. ProjectBaseline was not read. This is not a Codex artifact. GPL and unclear-license implementations are reference-only; no code was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

# Prerequisites, TechLevel and BuildLimit

## Raw prerequisite model

```text
PrerequisiteExpressionRaw
- OriginalText
- TokenOccurrences[]
- Delimiters[]
- Whitespace
- EmptyTokens
- SourceLayer
- ProviderProfile
- EvidenceGrade

PrerequisiteTokenRaw
PrerequisiteGroupCandidate
PositiveRequirementCandidate
NegativeRequirementCandidate
AlternativeCandidate
GenericPrerequisiteCandidate
```

Parsing preserves text and token boundaries. It never evaluates owned assets.

## Candidate prerequisite families

- stock `Prerequisite`;
- generic power/factory/barracks/radar/tech/refinery groups;
- YR refinery alternate candidate;
- `PrerequisiteOverride` candidate;
- Secret Lab and stolen-tech candidates;
- upgrade prerequisites;
- equivalent-building candidates;
- Ares negative prerequisites;
- Ares multiple alternative lists;
- Ares theater restrictions;
- Ares factory-owner plans;
- extension expression providers.

Comma, whitespace, OR/AND grouping and special tokens are profile-defined, not globally assumed.

## Missing and unknown values

Core does not silently decide that:

- missing `Prerequisite` means always buildable;
- an empty list is satisfied;
- an unknown token means false;
- duplicate tokens can be removed;
- tokens can be sorted;
- generic groups can be expanded in place;
- map assets may rewrite the expression.

These are later `PrerequisitePolicy` decisions.

## TechLevel separation

```text
AuthoredTechLevel
SessionTechLevel
PlayerTechCapability
PrerequisiteSatisfaction
ScenarioRestriction
AvailabilityResult
SidebarVisibility
```

Research retains negative, zero, extreme, invalid and overflowing values, campaign restrictions, lobby level, map-local overrides and AI profiles.

`TechLevel=-1` is not normalized by the parser to universal permanent unavailability. Passing TechLevel does not bypass prerequisites. UI hiding does not prove simulation unavailability.

## BuildLimit separation

```text
AuthoredBuildLimit
OwnedAliveCount
QueuedCount
ReservedCompletionCount
CapturedCount
MindControlledCount
DeployedEquivalentCount
UpgradeEquivalentCount
ScriptCreatedCount
AvailabilityLimitResult
```

Explicit policy questions:

- Do queued items count?
- Do completed-but-unplaced buildings count?
- Do captured or mind-controlled objects count?
- Do deploy/undeploy pairs share a limit?
- Do upgrades count with their host?
- Do clones and scripted products count?
- Is a negative value a current-count limit, a lifetime restriction or another profile?
- Is the limit per player, House, alliance or global?

ModEnc documents player-owned instance gating and non-construction acquisition paths. Ares documents fixes/extensions for AI, deploy equivalents and special cases. These remain separate evidence profiles.

## Binding result

```text
PrerequisiteBindingResult
- SatisfiedCandidates[]
- UnsatisfiedCandidates[]
- UnknownCandidates[]
- PositiveBlockers[]
- NegativeBlockers[]
- AlternativeGroups[]
- Diagnostics[]
```

One unknown token does not erase other evidence.

## Source anchors

- Ares 3.0 `Prerequisites`: negative lists, alternative lists, generic groups, stolen tech, theater and factory-owner plans.
- ModEnc `BuildLimit`, community documentation.
- PPM BuildLimit discussions, conflict evidence.
- OpenRA `Buildable.cs`, independent prerequisite model.

No evaluator code was imported; `code_imported: false`.
