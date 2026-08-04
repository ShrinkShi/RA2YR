> **Source notice:** This document was prepared by **ChatGPT Web** from public sources only. ProjectBaseline was not read. This is not a Codex artifact. GPL and unclear-license implementations are reference-only; no code was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

# Sidebar, cameo, hotkey and UI boundaries

## UI input contracts

```text
LogicalProductionAvailability
ProductionQueueSnapshot
SidebarEntryDescriptor
CameoReference
TooltipReference
HotkeyCandidate
ProgressPresentation
```

The sidebar may render available, disabled, hidden, queued, active, paused, ready or blocked states. It does not decide those states.

## Candidate references

- `Image`;
- `Cameo`;
- `AltCameo`;
- UI name / CSF label;
- category and tab;
- registry and explicit sort order;
- TechLevel;
- displayed Cost;
- displayed BuildTime;
- queue count;
- BuildLimit count;
- hotkey candidate;
- client/UI overrides;
- extension PCX or alternative cameo providers.

Core stores logical references and diagnostics. It does not load SHP, PCX, palettes or CSF text.

## Sorting candidates

```text
ProductionCategoryPriority
RegistryOrdinal
AuthoredSidebarOrder
TechLevelCandidate
DisplayedCostCandidate
LocalizedDisplayName
ProviderSpecificOrder
StableTypeIdentity
```

ModEnc documents cost ordering among objects of equal TechLevel as a community behavior candidate. The final comparator remains profile-specific.

Dictionary iteration, Unity hierarchy order and locale-dependent string order must not become implicit simulation ordering.

## Hotkeys

Preserve:

- raw key or character;
- locale;
- client override;
- category context;
- duplicate candidates;
- disabled command;
- platform mapping;
- extension command provider.

A duplicate hotkey is a UI diagnostic, not a type-registry collision.

## Queue interaction commands

```text
RequestProduction
RequestRepeat
PauseQueue
ResumeQueue
HoldQueue
CancelOne
CancelAllOfType
ReorderCandidate
EnterPlacementMode
CancelPlacement
```

Ares adds Shift-click bulk queueing for units. This is an extension UI command and does not redefine Core queue semantics.

## Progress presentation

Displayed clocks and bars consume normalized snapshots. Animation time does not advance production. A ready cameo means a completion contract awaits exit or placement; it does not prove that a runtime actor exists.

## Tooltip boundary

Tooltip data may include cost, estimated time, prerequisite blockers, BuildLimit state, power state, category, description and hotkey. Localized labels are not authoritative IDs.

## Observer and replay

Observer/replay UI consumes recorded simulation state. It cannot issue production commands and may use visibility policies different from active players without changing the underlying availability graph.

## Sidebar prohibitions

The sidebar does not:

- expand prerequisite groups;
- choose Owner/Country applicability;
- count BuildLimit objects;
- deduct credits;
- progress queues;
- choose factories;
- place buildings;
- create actors;
- decide capture policy.

## Source anchors

- Ares 3.0 UI feature documentation, including multi-unit Shift-click queueing and hotkey extensions.
- OpenRA production tooltip/widget files, independent implementation.
- ModEnc Cost and TechLevel ordering claims, community documentation.

No UI implementation or asset loading was added; `code_imported: false`.
