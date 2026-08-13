# M4 deterministic simulation architecture

M4 is the first project-enhancement milestone for a running deterministic
simulation world. It is not an assertion of YR 1.001 runtime compatibility.

## Layering

```text
Formats / Content Core
        ↓
Simulation descriptors
        ↓
RA2YR.Simulation (Unity-free authoritative state)
        ↓
Presentation Adapter / immutable snapshots and events
        ↓
Unity
```

The simulation owns logical ticks, entity identity, components, commands,
occupancy, path results, damage, autonomy decisions, and state hashes. Unity
renders snapshots and collects inputs but cannot define gameplay truth.

## Deterministic execution

```text
Tick N
  → Collect Inputs
  → Canonicalize
  → Read Snapshot
  → Parallel or sequential pure proposal computation
  → Barrier
  → Stable proposal ordering and deterministic commit
  → State Hash
```

Parallel workers may perform bounded read-only path, target, threat, visibility,
and utility queries. They must not mutate authoritative stores. Stable entity IDs,
explicit tie-breakers, fixed-point/integer consequential values, bounded RNG
streams, and canonical iteration order are required.

## Tactical autonomy

`Manual`, `Assisted`, and `Automatic` are first-class `UnitAutonomyMode` values.
Manual mode is a supported player mode, not a debug fallback. `AutonomyEnvelope`
expresses whether a command permits movement, acquisition, chase, retreat, kite,
ability use, or evade. Engine legality is highest; forced player commands normally
take precedence over optional tactical proposals.

Capabilities are resolved from unit profile, veterancy, faction doctrine,
technology unlock, temporary effects, player settings, and current command
envelope. A boolean `IsSmart` is not an acceptable authority model.

## Computer-agent boundary

```text
Observation → IAgentPolicy → AgentDecision → Validation
            → CommandRequest → Simulation
```

Agents receive legal observations, not mutable `SimulationWorld` or hidden enemy
state. RuleBased is a permanent reference/fallback. Neural and Hybrid are future
policy backends; M4 may define contracts and schema versions but does not download,
train, or execute a neural model. Training telemetry is explicitly non-authoritative
and cannot affect replay or state hashes.

## Performance and evidence

Hot/cold separation, immutable descriptors, spatial indexes, batch systems,
event-driven work, staggered decisions, bounded budgets, and benchmark harnesses
are engineering requirements. Benchmark numbers describe this implementation and
machine only; they are not compatibility evidence. Synthetic/headless M4 worlds
prove project-enhancement contracts, not original-runtime equivalence.
