# Special effects and presentation

> **Source notice:** Public-source research only. ProjectBaseline was not read. `code_imported: false`.

## Separation

```text
WarheadEffectCapability
StatusApplicationCommand
RuntimeStatusInstance
PeriodicEffectState
Removal/ImmunityCandidate
DeathFollowUpCommand
PresentationEvent
Art/Sound/AnimationReference
```

Fire, radiation, EMP, temporal, mind control, parasite, mutation, locomotor, destroy-anim and death-weapon concepts require named product/extension profiles. Presentation does not drive authoritative status or death.

## Evidence

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert exposes effect-related fields/catalogs | `ConfirmedByOfficialToolSource` | EA editor | Official editor behavior only. | Named editor profile. | `NotRun` |
| Ares/Phobos/Vinifera/OpenRA effect/status models | `ImplementationSpecificBehavior` | Named implementations | Extension/target-specific. | Isolate provider/version. | `NotRun` |
| Stable community effect and death-field conventions | `ConfirmedCommunityConvention` | ModEnc/PPM/community docs | Convention only. | Preserve raw references. | `NotRun` |
| Common effect capability candidates | `Underconfirmed` | Tools/community | Runtime state, stacking and immunity remain incomplete. | Explicit effect profile. | `NotRun` |
| Status stacking, owner/allies, periodic damage, removal and death sequencing | `ConflictingSources` | Engines/extensions/community | Models differ directly. | Preserve alternatives. | `NotRun` |
| Exact stock runtime status/death/savegame behavior | `Unresolved` | No runtime source | No complete state machine. | Future simulation adapter. | `NotRun` |
| Presentation never authorizes status/death and unknown effects remain raw | `DefensiveDesign` | Project policy | Architecture/fail-closed boundary. | Logical commands and visual events separate. | `NotRun` |

## Presentation boundary

Animations, reports, impact sounds, particles, tint, shake, debris, death art and UI feedback consume explicit simulation results. Missing Art/Sound does not cancel logical damage or status. Visual duration does not define status duration; animation events do not create authoritative impacts.
