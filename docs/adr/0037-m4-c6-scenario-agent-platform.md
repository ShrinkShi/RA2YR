# ADR-0037 - M4-C6 scenario spawn and agent boundaries

## Decision

Preserve scenario placement raw data and family identity, validate explicit
owner bindings, and emit bounded structural `SpawnRequest` values before any
entity creation. Expose computer players only through immutable
`AgentObservation` and normal command requests. Keep RuleBased as a deterministic
fallback and represent Neural policy as an unavailable-capable backend contract.

## Boundary

This is synthetic project-enhancement infrastructure. It does not implement a
full scenario parser, map-local Rules/Art binding, fog of war, economy,
renderer, original AI behavior, ProjectBaseline audit, or trained neural model.
