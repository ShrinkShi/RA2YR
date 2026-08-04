> **Source notice:** Prepared by **ChatGPT Web** from public sources only. ProjectBaseline was not read. This is not a Codex artifact. GPL and unclear-license implementations are reference-only; no code was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

# Selection, Control Groups, and UI Boundaries

## Fundamental separation

```text
SelectionState
!= CommandAuthority
!= MissionState
!= SimulationOwnership
```

A locally selected actor may not be commandable. A commandable actor may not be selected. Observer and replay selection never grants authority.

## Selection candidates

- click selection;
- box selection;
- type selection;
- double-press map-wide type selection;
- Shift add/remove;
- rank/health filters;
- select-all combat units;
- control groups;
- camera follow;
- observer/replay inspection;
- selected-only indicators.

The official manual documents type selection, control groups, deploy, guard, waypoints, and advanced-command-bar controls. These are UI contracts, not simulation mission storage.

## Recommended models

```text
SelectionDescriptor
- LocalPlayerIdentity
- SelectedActorStableIds[]
- PrimarySelectionCandidate
- SelectionOrdinal
- Context
- ObserverPolicy

ControlGroupDescriptor
- GroupSlot
- ActorStableIds[]
- SelectionPolicy
- PersistencePolicy
```

## Command cursors

Possible cursor results:

```text
Select
Move / NoMove
Attack / NoAttack
ForceFire
Enter / NoEnter
Repair
Sell
Deploy / NoDeploy
Capture
Garrison
Unload
GuardArea
Extension
Unknown
```

Cursor resolution is a presentation of command-capability queries. It must not mutate mission state.

Ares cursor customization demonstrates that one cursor may represent several underlying commands and that hiding a cursor does not necessarily disable scripts or AI behavior. This supports strict UI/simulation separation.

## Project behavior GUI

The G-key GUI consumes and edits a project autonomous-policy profile. It displays:

- guard/auto-attack;
- retaliation;
- pursuit and leash;
- target classes;
- persistence;
- return-to-origin;
- Hold Position interaction.

The GUI does not own autonomous decisions and cannot use `Update()` timing as authoritative simulation.

## Health/load indicators

Selected-only health, ammunition, cargo, or harvester-load bars are presentation snapshots. They do not determine capacity, damage, or command legality.

## Hotkeys

Stock, client, user-remapped, and project hotkeys are distinct profiles. Duplicate hotkeys, locale differences, text-input focus, observer mode, and accessibility mappings require explicit conflict resolution. Deterministic simulation receives normalized commands, not raw keyboard callback order.
