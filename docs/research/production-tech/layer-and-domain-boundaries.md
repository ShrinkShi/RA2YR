> **Source notice:** This document was prepared by **ChatGPT Web** from public sources only. ProjectBaseline was not read. This is not a Codex artifact. GPL and unclear-license implementations are reference-only; no code was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

# Layer and domain boundaries

## Responsibility map

| Layer | Owns | Must not own |
|---|---|---|
| lossless INI | occurrences, spelling, ordering, duplicates | typed availability |
| registry view | ordinal entries and section references | runtime instances |
| type binder | logical identity and provenance | player assets or queues |
| prerequisite binder | raw expressions and candidate groups | satisfaction evaluation |
| factory binder | category/capability candidates | queue creation |
| availability query | snapshot inputs and blocker set | credits mutation |
| queue simulation | requests, progress and reservations | sidebar rendering |
| completion adapter | completed product contract | automatic placement/spawn |
| placement/exit simulation | buildability, reservation, exit selection | UI preview authority |
| sidebar adapter | entries, tooltip and progress projection | tech-tree truth |
| AI adapter | observations and commands | Rules parsing |
| Unity adapter | visuals and input | authoritative simulation |

## Candidate pipeline

```text
lossless composed Rules
→ registry occurrence collection
→ raw type definitions
→ ownership and product profile
→ prerequisite and factory binding candidates
→ player/session availability query
→ queue command/result contracts
→ completion and placement/exit contracts
→ UI/AI adapters
```

## Availability is not one boolean

```text
Available
VisibleButUnavailable
Hidden
BlockedByPrerequisite
BlockedByTechLevel
BlockedByOwnership
BlockedByFactory
BlockedByBuildLimit
BlockedByCredits
BlockedByPower
BlockedByScenario
BlockedByPlacement
Unknown
```

Several states can coexist. `Hidden` is a sidebar policy and does not erase the underlying blockers.

## Product and session separation

```text
authored Rules
→ composed logical Rules
→ selected stock/extension profile
→ scenario restrictions
→ lobby/session overrides
→ player technology snapshot
→ availability result
```

Session values never rewrite raw Rules.

## Cross-research boundaries

- PR #33 owns House, Country, Side, player-slot and alliance identities.
- PR #31 owns scenario placement records.
- PR #35 owns render anchors and presentation ordering.
- PR #37 owns buildability, occupancy and movement layers.
- PR #38 owns credits and economic accounts.
- PR #32 owns Trigger and AI record graphs.

This dossier consumes those contracts but does not modify or duplicate them.

## Determinism constraints

Authoritative production must not depend on:

- Unity frame time or wall clock;
- dictionary or hash iteration;
- Unity hierarchy order;
- `UnityEngine.Random`;
- UI callback arrival order;
- renderer completion;
- Unity instance IDs.

Candidate stable keys include player ID, factory ID, queue ID, request ID, queue ordinal, completion ordinal and placement-reservation ordinal.

## Source anchors

- EA FinalSun / FinalAlert 2 `6abf0f557469baea73079c6bf6550709e2e3584e`, GPL-3.0-or-later, official editor evidence only.
- OpenRA `a520984d91eda9de48a62b1d15c1e3bad0d4fb1a`, GPL-3.0-or-later, independent implementation.
- Ares 3.0 docs, extension-only behavior.
- ModEnc and PPM, community documentation.

`code_imported: false`.
