# ADR 0055: M6 Human Playtest Delivery

## Status

Accepted for synthetic/project-enhancement delivery.

## Decision

Provide one explicit `RA2YRSyntheticSkirmish` scene and a centralized
`UnitySyntheticSkirmishBootstrap` as the M6 manual-playtest seam. The bootstrap
composes the existing Simulation, Presentation, UnityPresentationWorld, and
UnityInteractiveClient contracts. Human input becomes queued Human
`CommandRequest` values; Simulation remains the only authority for movement,
economy, production, autonomy, combat, and terminal match state. The synthetic
runtime and scene are bounded, deterministic, and independent of packed
ProjectBaseline content.

Procedural placeholder art is permitted only for this delivery seam. It is not
palette binding, original art import, renderer parity, map loading, or a claim
about the original runtime. The scene is an executable project-enhancement
proof, while compatibility remains synthetic/configured and M7 is not started.

## Consequences

The repository has a reproducible manual path for selecting units, issuing
commands, producing a unit, observing resource settlement, switching autonomy,
and watching a rule-based opponent engage. The scene does not provide stock YR
rules, map semantics, network input, replay, pathfinding, writer behavior, or
original visual assets. Those limitations remain explicit in the compatibility
matrix and evidence.
