# ADR-0035 - M4-C4 commands, missions, targeting, and autonomy

## Decision

Use declarative typed command requests for every source, bounded canonical
queues, raw-preserving runtime mission snapshots, spatial-index perception,
profile-driven target scoring with hysteresis, and a single deterministic action
arbitration stage. Forced player intent is explicit authority. Manual,
Assisted, and Automatic autonomy capabilities are resolved before tactical
proposals.

## Boundary

The models are synthetic project-enhancement contracts. They do not parse or
rewrite authored mission text, execute combat/economy/transport/capture state,
or assert original-runtime lifecycle, AI, or compatibility behavior.
