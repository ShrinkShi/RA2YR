# Mission state candidates

> **Source notice:** Public-source research only. ProjectBaseline was not read. `code_imported: false`.

## Raw identity

`MissionRaw` preserves exact text, case, whitespace, empty/sentinel candidates, source field/section, product/profile and unknown state. It is not replaced by an enum during parsing.

## Candidate catalog

Common names include Sleep, Harmless, Guard, Area Guard, Move, Attack, Hunt, Harvest, Enter, Capture, Repair, Deploy, Patrol, Scatter, Stop, Return, Unload and product/extension-specific entries. Catalog order is not an ordinal and completeness is not assumed.

## Evidence

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert exposes named Mission options and editor defaults | `ConfirmedByOfficialToolSource` | EA editor | Official catalog/default behavior only. | Named editor profile. | `NotRun` |
| WAE/OpenRA/extensions implement mission catalogs and state logic | `ImplementationSpecificBehavior` | Named implementations | Target/profile-specific. | Keep separate. | `NotRun` |
| Stable mission-name descriptions | `ConfirmedCommunityConvention` | ModEnc/PPM/community docs | Naming convention only. | Preserve raw and applicability. | `NotRun` |
| Common mission names as semantic candidates | `Underconfirmed` | Tools/community | Runtime aliases, defaults and lifecycle unproven. | Explicit mission catalog profile. | `NotRun` |
| Missing/unknown defaults, Guard/Hunt/Attack distinctions and mission aliases | `ConflictingSources` | Editors/engines/community | Models differ directly. | Preserve alternatives. | `NotRun` |
| Exact runtime state/substate/transition and save behavior | `Unresolved` | No runtime source | No complete state machine. | Future simulation adapter. | `NotRun` |
| Unknown Mission remains raw; no fallback to Guard/Sleep | `DefensiveDesign` | Project policy | Preservation/fail-closed design. | Semantic execution ineligible until profile selected. | `NotRun` |

`RuntimeMissionSnapshot` is separate and may include mission, phase/substate, target/path, source command, entered tick, interruptibility, completion reason and deterministic state. It never overwrites `MissionRaw`.
