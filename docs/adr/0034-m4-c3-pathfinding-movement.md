# ADR-0034 - M4-C3 pathfinding and movement foundation

## Decision

Use an independently implemented managed A* reference over explicit C2
movement graph candidates. Keep all path tie-breakers, budgets, cancellation,
cache invalidation, capabilities, and immutable result contracts explicit. Route
following uses integer cells with Simulation-owned occupancy and reservations;
local avoidance emits ordered proposals only.

## Boundary

This is synthetic project-enhancement behavior. It does not copy GPL/unclear
pathfinders, infer stock Locomotor or cost precedence, use Unity Transform,
Rigidbody, NavMesh, or Physics, or implement combat/gameplay.
