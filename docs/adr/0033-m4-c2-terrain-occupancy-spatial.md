# ADR-0033 - M4-C2 terrain, occupancy, and spatial foundation

## Context

The deterministic simulation needs a bounded terrain and occupancy boundary
before pathfinding and movement are introduced. Public research distinguishes
raw map identity, terrain candidates, movement layers, occupancy contributors,
and graph/path decisions; unresolved runtime semantics must not be fabricated.

## Decision

Add source-order-preserving `TerrainTopologyDocument` and explicit candidate
movement node/edge models. Keep passability states and movement capability raw
or profile-selected. Make static, foundation, dynamic, and reservation
occupancy simulation-owned. Add an ordered spatial index keyed by cell and
generation-checked entity ID. Duplicate, sparse, out-of-domain, and unknown
inputs remain visible and bounded.

## Consequences

The C2 contracts are suitable for synthetic headless tests and future C3
path/movement adapters. They do not implement pathfinding, terrain runtime
binding, bridge/tunnel semantics, collision physics, rendering, or gameplay,
and they do not provide original-runtime evidence.
