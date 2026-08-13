# ADR-0032 - M4-C1 deterministic ECS kernel

## Context

M4 requires an authoritative simulation boundary that can run without Unity
frame timing or scene objects. The first implementation slice must establish
stable identity, bounded component storage, fixed logical time, explicit
ordering, reproducible randomness, immutable read access, and a legal autonomy
boundary before terrain, movement, combat, or gameplay systems are added.

## Decision

Add `RA2YR.Simulation` as a Unity-free assembly referencing Core only. Use
generation-checked entity handles, bounded value-type component stores, ordered
structural commands, explicit simulation phases, seed-and-stream RNG, canonical
state hashes, immutable snapshots, and stable action-proposal ordering. Keep
`Manual | Assisted | Automatic` autonomy and capability envelopes as explicit
contracts. Use a managed sequential proposal backend as the deterministic
reference; parallel backends remain optional future implementations.

## Consequences

Simulation state is independent of GameObjects, MonoBehaviours, rendering,
physics, NavMesh, and display frame rate. Stale handles and invalid structural
operations fail closed. Synthetic tests can run headless and provide a stable
reference for later work packages. This is not original-runtime evidence and
does not implement pathfinding, combat, map loading, rendering, or gameplay.
