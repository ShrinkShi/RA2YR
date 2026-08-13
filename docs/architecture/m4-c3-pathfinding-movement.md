# M4-C3 pathfinding and movement foundation

M4-C3 adds an independently written deterministic managed A* reference and a
bounded movement contract above the C2 candidate graph. It is a project
enhancement, not an original YR pathfinding claim.

The path request preserves request ID, entity, start/goal node, capability
mask, request tick, and explicit limits. Search ties are ordered by `f`, `g`,
canonical node ID, and insertion sequence. Unknown, blocked, temporary, and
destructible nodes are not silently traversed. Results are immutable and never
return partial routes on failure. Cancellation, per-request expansion/route
budgets, a per-tick request/expansion budget, and an invalidatable cache are
explicit contracts.

Movement follows a returned integer-node route through simulation-owned
occupancy and reservations. A blocked transition does not release the source.
Local avoidance orders proposals by priority/entity/sequence and provides a
small deterministic yield candidate. Tactical movement remains an interface;
attack-move, combat, rendering, and Unity physics are not implemented here.
