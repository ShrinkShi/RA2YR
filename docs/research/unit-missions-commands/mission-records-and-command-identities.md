> **Source notice:** Prepared by **ChatGPT Web** from public sources only. ProjectBaseline was not read. This is not a Codex artifact. GPL and unclear-license implementations are reference-only; no code was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

# Mission Records and Command Identities

## Mission source families

Mission-like values may originate from:

- map placement records;
- Rules mission-control sections;
- TeamType/ScriptType actions;
- Trigger actions;
- AI configuration;
- type defaults;
- player-issued commands;
- autonomous simulation transitions;
- deploy/repair/transport subsystems;
- extension providers.

These sources are not interchangeable.

## Candidate mission tokens

Preserve raw occurrences for at least:

```text
Sleep
Attack
Move
QMove / Waypoint-like queued move
Retreat
Guard
Sticky
Enter
Capture
Eaten
Harvest
Area Guard
Return
Stop
Ambush
Hunt
Unload
Sabotage
Construction
Selling
Repair
Rescue
Missile
Harmless
Open
Patrol
Paradrop Approach
Paradrop Overfly
Wait
Spyplane Approach
Spyplane Overfly
Deploy
special/extension missions
```

Community sources disagree across TS, RA2, and YR about numeric positions because RA2 inserts additional missions. Therefore ordinal binding requires an explicit product profile and cannot be inferred from one shared enum.

## Recommended raw models

```text
MissionTypeRaw
- RawToken
- RawNumericCandidate
- SourceKind
- SourceSection
- SourceKey
- OccurrenceOrdinal
- ProductProfile
- ExtensionProvider
- EvidenceGrade
- Diagnostics

MissionProfile
- StableMissionProfileId
- CanonicalFamilyCandidate
- ApplicableActorCategories
- PerpetualCandidate
- MovementCandidate
- TargetingCandidate
- FiringCandidate
- CompletionCandidate
- TransitionPermissions
```

## Command identity

```text
UnitCommandRaw
- RawCommandKind
- RawTarget
- ModifierKeys
- QueueModifier
- IssuingPlayer
- LocalInputSequence
- SourceUI
- ProductProfile

UnitCommandDescriptor
- StableCommandType
- RequiredCapabilities
- TargetDomain
- QueuePolicyCandidate
- MissionTransitionCandidate
```

## Explicit distinctions

```text
MapPlacementMissionRaw
!= RulesDefaultMission
!= ScriptAssignMission
!= IssuedCommand
!= AcceptedCommand
!= CurrentMission
!= PendingMissionTransition
```

A placed unit with `Mission=Guard` can later receive Move, Attack, Enter, or scripted commands. Parsing must never treat the placement token as current savegame state.

## Script mission evidence

The official EA mission editor exposes Team Script operations such as Attack, Move to waypoint/cell, Guard area for a duration, Unload, Deploy, Load onto transport, Patrol, Scatter, and “Do this” mission assignment. This is editor/script evidence, not the original unit mission executor.

## Invalid and unknown values

Unknown strings, negative numbers, out-of-range numbers, empty values, duplicate values, case collisions, and extension missions remain structured. They are not converted to Guard, Sleep, Stop, or NoOp without a named policy.
