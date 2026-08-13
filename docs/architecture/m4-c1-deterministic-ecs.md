# M4-C1 deterministic ECS kernel

M4-C1 adds a Unity-free deterministic simulation kernel. It is a project
enhancement and does not claim compatibility with the original YR simulation.

## Authority and data

`RA2YR.Simulation` references only `RA2YR.Core` and has
`noEngineReferences: true`. `EntityId` combines a stable slot index with a
generation. The registry allocates the lowest available slot, rejects stale
handles, and advances the generation on destroy/reuse. Bounded component stores
hold value-type data and reject stale entities when attached to a world.

Structural changes are queued in a sequence-numbered command buffer and are
committed by the world in order. A world can publish an immutable
`SimulationReadSnapshot`; proposal computation receives that snapshot rather
than a mutable world. The managed sequential backend is the reference contract
for future parallel proposal backends.

## Time, scheduling, and randomness

Logical time advances only through `SimulationClock.AdvanceOneTick()` and an
explicit `SimulationTimeProfile`. Systems are ordered by phase, explicit order,
and ordinal system identifier. The phase list is Input, Command, Perception,
Decision, MovementPlanning, MovementCommit, CombatPlanning, CombatCommit,
Lifecycle, and Finalize.

Randomness is an explicit seed and stream identity. The bounded RNG exposes its
call count and has no hidden global or Unity random source. State hashes include
only the canonical tick, entity identity, and present component values; object
identity, pointers, host paths, frame timing, and machine data are excluded.

## Tactical autonomy boundary

`Manual`, `Assisted`, and `Automatic` are explicit modes. Resolution precedence
is unit, group, player, then global. Manual mode leaves legal movement/player
commands available but disables autonomous tactical capabilities. Assisted mode
retains acquisition while disabling fully automatic kite/cast/retreat actions.
The envelope and capabilities are data contracts; no gameplay, renderer,
pathfinding, or agent model is implemented by C1.

## Evidence boundary

The C1 tests are synthetic project-enhancement tests. They prove deterministic
contracts in this implementation only. They do not read ProjectBaseline packed
data, reproduce a stock YR runtime, or promote any compatibility matrix entry.
