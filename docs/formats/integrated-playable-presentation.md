# Integrated playable presentation closeout

M6-C8 composes the existing Simulation, Presentation, Unity renderer, and
interactive client seams for bounded synthetic runs.

## Contracts

- `PresentationCadenceProfile` is explicit and checked. The 30/60/144 values
  are scheduling profiles, not a claim about measured GPU frame rate.
- `PlayablePresentationCloseoutHarness` advances two equivalent Simulation
  worlds, assembles immutable presentation snapshots from one world, and
  compares state hashes to prove that presentation does not mutate authority.
- Descriptor budgets, entity tiers, tick counts, rendered-frame counts, and
  aggregate hashes are retained as structured results.
- `UnityPlayablePresentationController` owns the existing central world and
  interactive client adapter. It has no per-entity `Update` loop and does not
  create a second simulation.

## Compatibility boundary

This is synthetic/configured integration. It does not establish wall-clock
performance, GPU/shader parity, original UI behavior, network play, map
loading, ProjectBaseline runtime compatibility, or original-runtime
equivalence. No ProjectBaseline packed data was read.
