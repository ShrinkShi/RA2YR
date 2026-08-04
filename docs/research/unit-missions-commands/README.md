> **Source notice:** Prepared by **ChatGPT Web** from public sources only. ProjectBaseline was not read. This is not a Codex artifact. GPL and unclear-license implementations are reference-only; no code was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

# M3-R16 Unit Missions, Commands, Autonomous Behavior, Waypoints, Deploy, and Transport Research

## Scope

This dossier studies declarative inputs and responsibility boundaries needed for a future RA2/YR-compatible unit-command system. It does not implement mission parsing, command execution, pathfinding, targeting, transport logic, selection, or Unity objects.

Frozen candidate pipeline:

```text
raw Rules/map/AI references
→ mission and command raw identities
→ explicit product/extension profiles
→ command validation candidate
→ deterministic command queue
→ mission transition candidate
→ autonomous targeting and movement policy
→ future simulation and UI adapters
```

## Non-collapsible identities

```text
MapPlacementMissionRaw
!= MissionTypeRaw
!= ScriptMissionArgumentRaw
!= IssuedCommand
!= AcceptedCommand
!= CurrentRuntimeMission
!= MovementIntent
!= TargetingIntent
!= FiringIntent
!= AutonomousChaseIntent
!= AnimationState
```

## Project S/H/G policy

These bindings are project policy, not stock RA2/YR facts:

- `S`: interrupt explicit command candidates, clear movement/path/explicit attack targets, but do not disable legal autonomous fire.
- `H`: Hold Position until the next explicit command; forbid autonomous movement and chase, while allowing in-place turning, aiming, and legal firing.
- `G`: open the project autonomous-behavior GUI; it does not directly issue stock Guard.
- Evidence grade: `ConfiguredForProjectPolicy`.

Stock public evidence differs: the RA2/YR manuals describe `S` as stopping movement, `G` as guarding the current area, `Z` as waypoint mode, `D` as deploy, and `H` as centering the camera on the base.

## Main conclusions

- Map placement `Mission` is an authored starting-state candidate, not a saved current runtime mission.
- Community mission-number lists are useful profile evidence but are not official runtime enum source.
- Guard, Area Guard, Sticky/Hold-like behavior, Hunt, Ambush, Stop, and Attack are distinct profiles.
- Stop, Hold Position, cease-fire, passive acquisition, retaliation, and chase are separate controls.
- UI cursors and hotkeys propose commands; simulation validates and accepts them.
- Queue presentation is not the simulation queue.
- Transport capacity, passenger eligibility, embark, occupancy, unload, and passenger survival are separate.
- Selection and control groups never establish command authority or simulation ownership.
- Core remains independent of `UnityEngine`.

## Evidence grades

- `ConfirmedByOfficialRuntimeSource`
- `ConfirmedByOfficialEditorSource`
- `ConfirmedByIndependentImplementation`
- `CommunityDocumented`
- `ObservedByFutureProjectBaselineAudit`
- `ConfiguredForProjectPolicy`
- `Unresolved`

## Files

The remaining files define layer boundaries, mission identities, autonomous behavior, movement and attack, waypoints, special commands, transports, UI, source comparison, implementation design, 184 test cases, a sanitized future audit, and P0 unresolved questions.
