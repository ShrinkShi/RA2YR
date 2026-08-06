# Target typing and validation

> **Source notice:** Public-source research only. ProjectBaseline was not read. `code_imported: false`.

## Target kinds

```text
None
Cell
Object/Actor
Building
Unit/Infantry/Aircraft
ResourceCell
Waypoint
House/Player
Transport/Dock/Refinery/RepairFacility
Mission/Script-specific reference
Unknown
```

Raw target text/identity, candidate domains, resolved references and runtime legality remain separate.

## Evidence

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert exposes command/script target parameter catalogs | `ConfirmedByOfficialToolSource` | EA editor | Official editor behavior only. | Named editor profile. | `NotRun` |
| OpenRA/clients/extensions implement typed target validation | `ImplementationSpecificBehavior` | Named implementations | Target/profile-specific. | Keep separate. | `NotRun` |
| Common object/cell/waypoint/House target conventions | `ConfirmedCommunityConvention` | ModEnc/PPM/community docs | Convention only. | Preserve raw/reference provenance. | `NotRun` |
| Typed target candidates based on command kind | `Underconfirmed` | Tools/community | Exact runtime conversion and lineage independence unproven. | Explicit target profile. | `NotRun` |
| Cell/object conversion, missing target, target loss, fog, ownership and command-specific legality | `ConflictingSources` | Engines/clients/extensions | Models differ directly. | Preserve alternatives and diagnostics. | `NotRun` |
| Exact stock runtime target resolution and validation order | `Unresolved` | No runtime source | No complete contract. | Future world-query/simulation adapter. | `NotRun` |
| Never select a target domain because lookup/path/render succeeds | `DefensiveDesign` | Project policy | Plausibility probing prohibited. | Fail closed on ambiguity. | `NotRun` |

`CommandTargetCandidate` records raw source, target-kind candidates, identity/coordinate profiles, resolution candidates and ambiguity. `TargetValidationResult` separately reports existence, capability, ownership, range/path/line-of-fire, occupancy, visibility and runtime-state reasons without mutating the request.
