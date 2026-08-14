# ADR-0053: M6-C7 interactive client foundation

## Status

Accepted for synthetic/configured implementation.

## Decision

Keep interactive client state as a presentation boundary over immutable
Simulation snapshots. Visibility, selection, pointer projection, HUD values,
production availability, placement previews, and lighting/weather profiles are
explicit, bounded contracts. Human input becomes Simulation `CommandRequest`
values through an injected `CommandQueue`; Unity input does not advance the
simulation and no UI state becomes simulation authority.

## Consequences

The client can provide deterministic selection and isometric cell picking,
explicit fog/shroud handling, command submission, HUD snapshots, production
panel candidates, placement previews, and environment profiles without adding
renderer-specific semantics to Core or mutating the world from a presentation
loop. Camera raycasts, real UI widgets, palette/TMP/theater binding, network
input, and original-runtime interaction parity remain unresolved.
