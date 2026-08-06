# Move, attack, harvest, enter, repair and capture

> **Source notice:** Public-source research only. ProjectBaseline was not read. `code_imported: false`.

## Typed command families

- Move: cell/object/formation target, path request, arrival radius, queue policy.
- Attack: object/cell target, weapon/targeting eligibility, approach/line-of-fire candidates.
- Harvest: resource target/search, reservation, cargo/refinery follow-up candidates.
- Enter: building/transport/refinery/dock target and acceptance policy.
- Repair: repair facility/object/self candidates and cost/ownership policy.
- Capture: capturable target, engineer/capability, ownership and post-capture transition.
- Deploy/Undeploy: transformation/placement transaction.
- Patrol: waypoint/path cycle and engagement policy.
- Scatter: short displacement policy, not a complete mission state by name alone.

## Evidence

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert exposes these Mission names and authoring parameters | `ConfirmedByOfficialToolSource` | EA editor | Official catalog only. | Named editor profile. | `NotRun` |
| OpenRA/clients/extensions implement each command family | `ImplementationSpecificBehavior` | Named implementations | Target/profile-specific. | Keep separate. | `NotRun` |
| Stable community descriptions of command families | `ConfirmedCommunityConvention` | ModEnc/PPM/community docs | Convention only. | Preserve raw names/product scope. | `NotRun` |
| Typed target/capability request model | `Underconfirmed` | Tools/community | Exact runtime acceptance/lifecycle unproven. | Explicit command profiles. | `NotRun` |
| Approach, queueing, target loss, resource/refinery, transport, repair, capture and deploy semantics | `ConflictingSources` | Engines/extensions/community | Models differ directly. | Preserve alternatives. | `NotRun` |
| Exact stock runtime state transitions and side effects | `Unresolved` | No runtime source | No complete contracts. | Future simulation adapters. | `NotRun` |
| Parser creates no path/combat/economy/ownership mutation | `DefensiveDesign` | Project policy | Architecture boundary. | Commands/results separate. | `NotRun` |

A target/reference resolving successfully does not prove legality. Capability, current state, ownership, path, occupancy, weapon, cargo, refinery, transport and mode checks remain simulation queries. Partial or failed results preserve the original request and deterministic reason codes.
