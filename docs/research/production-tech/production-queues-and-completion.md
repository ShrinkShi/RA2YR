> **Source notice:** This document was prepared by **ChatGPT Web** from public sources only. ProjectBaseline was not read. This is not a Codex artifact. GPL and unclear-license implementations are reference-only; no code was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

# Production queues and completion

## Queue identities

```text
ProductionRequest
ProductionQueueDescriptor
ProductionQueue
ProductionQueueEntry
ProductionProgress
CreditsReservation
FactoryAssignmentCandidate
CompletionCandidate
SpawnOrPlacementRequest
```

A request is not yet a queue entry. A completed entry is not yet an actor or placed building.

## Queue ownership candidates

- per factory instance;
- per factory type;
- per production category;
- per player shared sidebar queue;
- classic shared-category queue;
- parallel queues;
- AI-only cloning or parallel behavior;
- extension bulk queue.

PPM discussions commonly describe a shared queue for factories of one category in stock-style RA2/YR. Ares documents parallel-AI and load-sharing changes. This conflict is retained as a product-profile question.

## Entry states

```text
Requested
Accepted
WaitingForFactory
WaitingForCredits
Active
Paused
Held
CancelPending
CompletedAwaitingExit
CompletedAwaitingPlacement
BlockedAtExit
TransferredAfterCapture
Cancelled
Failed
```

Candidate fields:

- stable request identity;
- owner/player identity;
- type identity;
- production category;
- queue ordinal;
- quantity and active ordinal;
- factory-assignment candidates;
- authored/current cost;
- reserved and deducted credits;
- progress numerator/denominator;
- start and completion ticks;
- pause reasons;
- capture/destruction provenance;
- diagnostics.

## Credits transaction candidates

Unresolved policies include:

- reserve all credits up front;
- deduct progressively;
- stop when funds are insufficient;
- reject debt;
- refund exact undeducted amount;
- refund a fraction of paid amount;
- order simultaneous queue deductions deterministically.

Availability can report insufficient credits but cannot mutate accounts.

## Completion contracts

Unit completion creates a `SpawnOrExitRequest`. Building completion creates a `PlacementRequest`.

Unit questions:

- Which factory provides the exit?
- Is the active producer required?
- Can another compatible factory provide the exit?
- What happens when all exits are blocked?
- How are naval and aircraft layers selected?
- How is a rally point consumed?
- What if capture or destruction shares the completion tick?

Building questions:

- Can several completed buildings wait for placement?
- Can the queue continue while one is ready?
- Is there a placement timeout?
- Which owner receives the ready item after capture?
- What if the construction yard is destroyed?

## Stable ordering

Suggested tuple:

```text
CompletionTick
PlayerStableId
QueueStableId
QueueEntryOrdinal
FactoryStableId
RequestStableId
```

Capture, destruction, credit, completion, exit and placement-reservation order require explicit policies.

## Non-goals

No queue algorithm, credits transaction, progress timer, actor spawn, rally-point execution or savegame state is implemented.

## Source anchors

- OpenRA production queue families and `Production`/`Exit` traits: independent implementation.
- Ares UI bulk queue and parallel-AI/load-sharing documentation: extension behavior.
- PPM multiple-factory discussions: community conflict evidence.

`code_imported: false`.
