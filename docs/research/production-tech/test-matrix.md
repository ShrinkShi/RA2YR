# Production research test matrix — 176 design cases

> **Source notice:** Public-source research only. ProjectBaseline was not read. Synthetic design only; no original assets or implementation code. `code_imported: false`.

## Coverage

| Category | Cases |
|---|---:|
| Registries, definitions and identity | 26 |
| Ownership, prerequisites and TechLevel | 30 |
| BuildLimit, cost and build time | 26 |
| Factory capability and availability | 26 |
| Queue, payment and completion | 28 |
| Placement, deployment, upgrade and power | 22 |
| Sidebar, safety, roundtrip and audit | 18 |
| **Total** | **176** |

## Required coverage

Registry gaps/duplicates/case/unlisted definitions/map-local layers; missing/ambiguous Owner/Country/Side/House references; prerequisite punctuation, empties, groups, generic and extension forms; TechLevel/BuildLimit boundary/count profiles; Cost/time units, rounding, overflow and modifiers; factory/category/cloning/capture/power candidates; visible/requestable/queued/paid/complete/placeable distinction; shared/per-factory queues, cancellation/refund/capture/destruction/save-load; exits/docks/foundation/placement reservations; deploy/undeploy/upgrades/state transfer; stolen tech/secret lab; sidebar resource absence; bounded Memory/Stream/short-read/MIX equivalence; no Unity dependency.

## Evidence discipline

Expected results use one normalized grade. Official-editor fixtures confirm tool behavior only; named engines/extensions/clients are implementation-specific; community conventions do not prove execution; conflicts stay conflicting; runtime-unsourced behavior stays underconfirmed/unresolved; project safety expectations are defensive design.

```text
AuditStatus: NotRun
FutureEvidenceSource: ProjectBaselineAggregateAudit
```

## Fixture rules

Use tiny independent synthetic definitions; no original Rules/maps/Art or source-derived evaluators; expected prerequisite/cost/time/queue/order results must not call production logic; no trial profile selection, actor/UI creation or compatibility promotion.
