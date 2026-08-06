# Sidebar visibility and UI boundaries

> **Source notice:** Public-source research only. ProjectBaseline was not read. `code_imported: false`.

## Separation

```text
LogicalTypeIdentity
AvailabilityResult
SidebarVisibilityCandidate
SidebarCategory/Order
Cameo/Name/TooltipReferences
Hotkey/BuildTab
QueueProgressPresentation
RuntimeQueueState
```

A visible button is not proof of producibility; a missing cameo is not proof that a type is unavailable.

## Evidence

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert exposes type/UI-related authored fields | `ConfirmedByOfficialToolSource` | EA editor | Official editor behavior only. | Named editor profile. | `NotRun` |
| CnCNet/OpenRA/Ares/Phobos implement sidebar/category/filter behavior | `ImplementationSpecificBehavior` | Named implementations | Client/engine/extension-specific. | Keep separate UI profiles. | `NotRun` |
| Common Image/Cameo/UIName/BuildCat/sidebar conventions | `ConfirmedCommunityConvention` | ModEnc/PPM/community docs | Convention only. | Logical references and provenance retained. | `NotRun` |
| Visibility based on type/category/availability as a leading candidate | `Underconfirmed` | Tools/community | Runtime/client precedence and lineage independence unproven. | Explicit UI query profile. | `NotRun` |
| Hidden/show policies, sorting, alternate cameos, stolen tech, observer and mode behavior | `ConflictingSources` | Clients/extensions/community | Models differ directly. | Preserve alternatives. | `NotRun` |
| Exact stock sidebar visibility/order/hotkeys/progress behavior | `Unresolved` | No runtime/client source contract | No complete universal rule. | Future UI adapter. | `NotRun` |
| UI is non-authoritative and missing resources do not alter type identity | `DefensiveDesign` | Project policy | Architecture boundary. | UI consumes immutable availability/queue snapshots. | `NotRun` |

Sidebar adapters may group, filter, sort and display logical candidates but never create production eligibility, deduct credits, advance queues, renumber registries or rewrite Rules. Unknown/missing CSF/cameo/Art references remain diagnostics with fallback presentation policy outside Core.
