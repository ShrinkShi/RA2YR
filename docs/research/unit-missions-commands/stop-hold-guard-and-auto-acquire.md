# Guard, hold, stop and engagement policy

> **Source notice:** Public-source research only. ProjectBaseline was not read. `code_imported: false`.

## Distinct policies

```text
StopCommand
HoldPositionPolicy
GuardMission
AreaGuardMission
EngagementPolicy
AutoAcquirePolicy
PursuitPolicy
ReturnToAnchorPolicy
```

Stop interrupts/clears current orders under a profile; hold constrains movement; guard/area-guard describe ongoing behavior; auto-fire, pursuit and return are separate.

## Evidence

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert exposes Guard/Area Guard/Stop-related mission names | `ConfirmedByOfficialToolSource` | EA editor | Official catalog only. | Named editor profile. | `NotRun` |
| OpenRA/clients/extensions implement guard, stance, hold and pursuit | `ImplementationSpecificBehavior` | Named implementations | Target/client-specific. | Keep separate profiles. | `NotRun` |
| Stable Guard/Area Guard/Stop/Scatter conventions | `ConfirmedCommunityConvention` | ModEnc/PPM/community docs | Convention only. | Preserve raw mission/command identity. | `NotRun` |
| Guard with autonomous acquisition/pursuit as a common candidate | `Underconfirmed` | Tools/community | Exact ranges, return behavior and runtime applicability unproven. | Explicit engagement profile. | `NotRun` |
| Stop versus Guard reset, Hold attack behavior, pursuit and return-to-origin | `ConflictingSources` | Engines/clients/community | Models differ directly. | Preserve alternatives. | `NotRun` |
| Exact stock runtime mission interruption and guard lifecycle | `Unresolved` | No runtime source | No complete state machine. | Future simulation adapter. | `NotRun` |
| S/H/G-like controls remain UI policy and never rewrite source Mission | `DefensiveDesign` | Project policy | Architecture boundary. | UI emits explicit commands/policies. | `NotRun` |

Hold/stop units may still attack under an explicit engagement policy, but parser does not assume this as stock behavior. Auto-acquisition, retaliation, pursuit distance, lost-target handling and return anchor are separate deterministic inputs/results.
