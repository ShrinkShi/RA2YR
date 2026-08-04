> **Source notice:** Prepared by **ChatGPT Web** from public sources only. ProjectBaseline was not read. This is not a Codex artifact. GPL and unclear-license implementations are reference-only; no code was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

# Implementation Boundaries and Candidate Core Design

## Candidate Core models

```text
MissionTypeRaw
MissionProfile
UnitCommandRaw
UnitCommandDescriptor
CommandValidationResult
CommandQueueDescriptor
CommandQueueEntry
MissionTransitionCandidate
AutonomousBehaviorProfile
AcquisitionPolicy
RetaliationPolicy
PursuitPolicy
LeashPolicy
HoldPositionPolicy
TargetPersistencePolicy
MovementIntent
TargetingIntent
FiringIntent
WaypointRouteDescriptor
DeployCommandDescriptor
EnterCommandDescriptor
TransportOccupancyDescriptor
GarrisonOccupancyDescriptor
SelectionDescriptor
UnitCommandDiagnostic
UnitCommandReadLimits
UnitCommandConsistencyAnalysis
UnitCommandRoundtripDescriptor
```

Supporting candidates:

```text
StableCommandIdentity
StableMissionTransitionIdentity
TargetDescriptor
CommandCapabilitySnapshot
CommandAuthoritySnapshot
PathRequestDescriptor
OccupancyReservationCandidate
PassengerEntryDescriptor
AutonomousDecisionDescriptor
ProjectControlBindingProfile
```

## Explicit policies

```text
MissionBindingPolicy
CommandValidationPolicy
CommandQueuePolicy
StopPolicy
HoldPositionPolicy
GuardPolicy
AcquisitionPolicy
RetaliationPolicy
PursuitPolicy
LeashPolicy
TargetPersistencePolicy
WaypointPolicy
PatrolPolicy
DeployPolicy
EnterPolicy
TransportPolicy
GarrisonPolicy
SelectionPolicy
HotkeyPolicy
UnitCommandDeterminismPolicy
UnitCommandOrderingPolicy
UnitCommandRoundtripPolicy
```

## Command validation result

Must support multiple reasons:

```text
Accepted
UnknownCommand
NotAuthorized
ActorUnavailable
CapabilityMissing
TargetInvalid
RelationshipInvalid
OutOfDomain
PathUnavailable
CapacityFull
OccupancyBlocked
HoldRestriction
MissionRestriction
ScenarioRestriction
ExtensionStateMissing
Unknown
```

Validation returns data; it does not mutate actors.

## Deterministic command processing

Recommended ordering tuple:

```text
SimulationTick
PlayerStableId
CommandSequence
ActorStableId
QueueOrdinal
MissionTransitionOrdinal
TargetStableId
AutonomousDecisionOrdinal
```

Simultaneous commands require an explicit conflict policy. No Unity frame time, wall clock, dictionary iteration, Unity instance ID, input callback arrival, render event, or animation event may determine authoritative order.

## Autonomous update contract

```text
ActorSnapshot
+ CurrentMissionSnapshot
+ CommandQueueSnapshot
+ Hold/Guard profile
+ NearbyTargetCandidates in stable order
+ TargetPersistenceSnapshot
+ LeashSnapshot
→ AutonomousDecisionCandidate[]
```

The autonomous subsystem emits intents. Movement, targeting, and combat systems validate and execute separately.

## Save/load and replay

Serialize:

- stable actor/player/command IDs;
- queue entries and ordinals;
- current mission profile and transition state;
- target identity and last-known candidate;
- Hold/Guard/autonomous policy;
- leash origin;
- waypoint route and active node;
- transport/garrison occupancy order;
- deterministic RNG state if used.

Do not serialize UI widget references, Unity object IDs, cursor names as authority, or renderer animation progress as mission state.

## Roundtrip

Lossless writer preserves raw mission tokens, numeric spelling, unknown values, duplicates, extension fields, map placement values, and source order. Semantic descriptors and runtime state are separate outputs. No default canonicalization or repair.
