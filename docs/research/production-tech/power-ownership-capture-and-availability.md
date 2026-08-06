# Power, ownership, capture and availability

> **Source notice:** Public-source research only. ProjectBaseline was not read. `code_imported: false`.

## Availability dimensions

```text
AuthoredOwners/Forbidden/Required candidates
CurrentRuntimeOwner
Country/Side/House identity
TechLevel/Prerequisites/BuildLimit
FactoryCapability/QueueState
Credits/Power
Capture/StolenTech/SecretLab/Upgrade candidates
Mode/Difficulty/Trigger overrides
```

These remain independent blockers/evidence inputs.

## Evidence

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert exposes Owner/Tech/Power/Capture fields and editor validation | `ConfirmedByOfficialToolSource` | EA editor | Official tool only. | Named editor profile. | `NotRun` |
| OpenRA/Ares/Phobos implement ownership, power and capture availability | `ImplementationSpecificBehavior` | Named implementations | Target/extension-specific. | Keep separate. | `NotRun` |
| Stable Owners/Required/Forbidden/Power/Tech conventions | `ConfirmedCommunityConvention` | ModEnc/PPM/community docs | Convention only. | Preserve identity domains. | `NotRun` |
| Authored ownership and power as availability candidates | `Underconfirmed` | Tools/community | Runtime precedence/defaults and lineage independence unproven. | Explicit availability policy. | `NotRun` |
| Country/Side/House matching, capture queues, low power, stolen tech and secret-lab behavior | `ConflictingSources` | Engines/extensions/community | Models differ directly. | Preserve alternatives. | `NotRun` |
| Exact runtime owner transfer, queue fate, power state and availability recomputation | `Unresolved` | No runtime source | No complete contract. | Future session/simulation adapter. | `NotRun` |
| No owner fabrication/fallback and multi-blocker results | `DefensiveDesign` | Project policy | Preservation/architecture. | Fail closed. | `NotRun` |

Current runtime owner never rewrites authored Owners. Low power may affect production/operation under a selected profile but parser does not stop queues or modify build times. Capture produces explicit queue/factory/credits/power transition candidates; secret/stolen-tech and mode overrides remain separate source layers.
