# Mission and command research test matrix — 180 design cases

> **Source notice:** Public-source research only. ProjectBaseline was not read. Synthetic design only; no original assets or implementation code. `code_imported: false`.

## Coverage

| Category | Cases |
|---|---:|
| Mission identities, catalogs and raw parsing | 24 |
| Command requests, queueing and arbitration | 28 |
| Target typing and validation | 26 |
| Move/Attack/Guard/Stop/Patrol/Scatter | 28 |
| Harvest/Enter/Repair/Capture/Deploy | 26 |
| Script/AI commands and lifecycle | 26 |
| Determinism, safety, roundtrip and audit | 22 |
| **Total** | **180** |

## Required coverage

Raw/empty/unknown/case/duplicate Mission and Script tokens; catalog/profile conflicts; no fallback to Guard/Sleep; player/AI/Script/Trigger/internal command sources; replace/append/mixed-selection/partial acceptance; cell/object/waypoint/House/resource/facility target candidates; ambiguous/missing/lost targets; Guard/Area Guard/Hunt/Stop/Hold/acquire/pursuit/return; Move/Attack/Patrol/Scatter; Harvest/refinery/cargo; Enter/transport/dock; Repair/cost/ownership; Capture/post-capture; Deploy/Undeploy; Script action/argument/gaps/retries/failure/advance; stable command/queue/transition IDs; interruption/completion/save-load; bounded Memory/Stream/short-read/MIX equivalence; no Unity/UI/animation authority.

## Evidence discipline

Expected results use one normalized grade. Official-editor fixtures confirm tool behavior only; named engine/client/extension fixtures are implementation-specific; community names do not prove execution; conflicts remain conflicting; runtime-unsourced behavior remains underconfirmed/unresolved; project safety expectations are defensive design.

```text
AuditStatus: NotRun
FutureEvidenceSource: ProjectBaselineAggregateAudit
```

## Fixture rules

Use tiny independent synthetic records/requests; no original maps/Rules/Script sequences or source-derived state machines; expected target/queue/transition results must not call production logic; no trial profile selection, actor movement/combat/economy, rendering or compatibility promotion.
