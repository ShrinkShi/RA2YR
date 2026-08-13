# ADR-0038 - M4-C7 integrated synthetic world

## Decision

Use a bounded headless synthetic battle as the M4 integration reference. Keep
integer state, proposal/commit ordering, input-order canonicalization, repeated
run hashes, and explicit unit budgets as the deterministic gate. Performance
measurements are diagnostic only and never compatibility evidence.

## Boundary

The integrated world is project-enhancement infrastructure. It does not claim
stock YR runtime, map, combat, AI, renderer, economy, or neural-model parity,
and reads no ProjectBaseline data.
