# M4-C2 terrain, occupancy, and spatial foundation

M4-C2 adds raw and candidate terrain topology contracts above M4-C1. It does
not implement a pathfinder or claim stock YR terrain semantics.

## Terrain boundary

`TerrainTopologyBuilder` preserves source-order cell records, duplicate groups,
sparse/dense classification, raw tile/subtile/level/ramp values, explicit
passability state, and bounded diagnostics. Coordinates are candidates only;
out-of-domain and invalid SubTile values are diagnosed. `Unknown` remains
`Unknown`; no visual, filename, neighboring-cell, or community-table heuristic
promotes it to passable. `MovementNode`, `MovementEdgeCandidate`, and
`MovementGraphCandidate` are candidate graph data, not pathfinding results.

## Occupancy authority

`SimulationOccupancy` owns static, foundation, dynamic, and reservation state.
Static blockers, dynamic blockers, and reservations are separate contributors.
Acquire and move operations are explicit and fail closed on collision. Unity
Physics is not consulted and cannot become simulation authority.

## Spatial index

`DeterministicSpatialIndex` uses ordered cell buckets and ordered generation-
checked entity IDs. Insert, remove, move, and bounded neighbor query results are
stable across insertion order. It is a query foundation for future targeting,
threat, collision-neighbor, and perception systems; it is not a global O(N2)
scan replacement for a pathfinder.

The research-documented bridge, tunnel, ramp, water, locomotor, and cost
conflicts remain explicit unresolved/profile inputs. No ProjectBaseline terrain
semantics are inferred or promoted by C2.
