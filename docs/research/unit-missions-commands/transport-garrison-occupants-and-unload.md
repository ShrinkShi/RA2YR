# Transport, garrison, occupants, and unload

> **Source notice:** Public-source research only. ProjectBaseline was not read. `code_imported: false`.

## State split

```text
Transport/Garrison Capability
Passenger Eligibility
Enter/Embark Command
Occupancy Reservation
Runtime Occupant List
Capacity Usage
Unload Command
Exit/Placement Candidates
Passenger Survival/Ownership State
Presentation Pips
```

Capacity fields, passenger lists, occupancy slots, UI pips and rendered occupants are separate. The parser does not embark, eject, kill, transfer or place actors.

## Evidence

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert exposes transport/garrison/passenger-related fields and editor validation | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Official editor behavior only. | Named editor profile. | `NotRun` |
| OpenRA, clients and extensions implement occupancy and unload models | `ImplementationSpecificBehavior` | Named implementations | Target/profile-specific. | Keep separate profiles. | `NotRun` |
| Stable transport/garrison/passenger authoring conventions | `ConfirmedCommunityConvention` | ModEnc/PPM/RA2 DIY | Convention only. | Preserve raw fields/product scope. | `NotRun` |
| Capacity, eligibility, reservation and ordered occupant candidates | `Underconfirmed` | Tools/community | Exact runtime units and lineage independence unproven. | Explicit occupancy profile. | `NotRun` |
| Size counting, nested transport, garrison firing, unload placement, ownership and destruction outcomes | `ConflictingSources` | Engines/extensions/community | Public models differ directly. | Preserve alternatives. | `NotRun` |
| Exact stock embark/unload ordering, cancellation, save/load and passenger survival | `Unresolved` | No original-runtime source located | No complete state machine. | Future deterministic occupancy adapter. | `NotRun` |
| UI pips are non-authoritative, stable occupant IDs/order and no parser mutation | `DefensiveDesign` | Project policy | Determinism/architecture. | Fail closed. | `NotRun` |

## Runtime boundary

Enter and unload requests use typed target/exit domains, relationship and eligibility checks, path/approach results, occupancy reservations and deterministic reason codes. Missing exits, full targets, capture/destruction, moving transports and save/load conflicts remain explicit outcomes; no synthetic occupant or exit is invented.
