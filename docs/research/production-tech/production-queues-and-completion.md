# Production queues and completion

> **Source notice:** Public-source research only. ProjectBaseline was not read. `code_imported: false`.

## State split

```text
AvailabilityQuery
QueueRequest
QueueEntry
FactoryAssignment
CreditsTransaction
ProgressState
CompletionCandidate
Exit/PlacementRequest
Spawn/PlacementResult
```

Queue acceptance does not equal payment, completion or actor creation.

## Evidence

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert exposes production-related type fields, not runtime queues | `ConfirmedByOfficialToolSource` | EA editor | Official tool evidence is limited to authored data. | No editor queue inference. | `NotRun` |
| OpenRA/Ares/clients implement queue, pause, multiple-factory and UI behavior | `ImplementationSpecificBehavior` | Named implementations | Target/extension/client-specific. | Separate queue profiles. | `NotRun` |
| Stable queue/progress/cancel authoring and gameplay conventions | `ConfirmedCommunityConvention` | Community docs | Convention only. | Preserve applicability. | `NotRun` |
| FIFO category queue as a common candidate | `Underconfirmed` | Tools/community | Runtime queue ownership and payment timing unproven. | Explicit queue policy. | `NotRun` |
| Shared versus per-factory queues, reassignment, capture/destruction, payment and ready-item retention | `ConflictingSources` | Engines/extensions/community | Models differ directly. | Preserve alternatives. | `NotRun` |
| Exact runtime queue state, progress timing, cancellation/refund and save/load | `Unresolved` | No runtime source | No complete state machine. | Future deterministic simulation adapter. | `NotRun` |
| Stable queue-entry IDs/order, no parser execution and separate transactions | `DefensiveDesign` | Project policy | Determinism/architecture. | Checked progress and canonical events. | `NotRun` |

A queue entry records requester/owner/type/category, selected factory candidate, cost/time profiles, progress, pause/block state, stable ordinal and diagnostics. Completion emits a request; blocked exits/placement remain explicit and do not silently spawn elsewhere or discard the item.
