# M4-C7 integrated synthetic world and performance contracts

C7 composes the existing deterministic ECS, occupancy/path/movement,
command/autonomy, combat, and agent foundations into a tiny headless synthetic
battle. Units use integer coordinates, bounded attack proposals and canonical
damage/death commit. Input-order permutations and repeated runs are compared
through a stable aggregate state hash; no Unity frame, physics, renderer, or
external map content is authoritative.

This is an integration and performance contract, not a playable YR engine.
It does not add scenario parsing, economy, stock special mechanisms, writer,
renderer, neural training, or ProjectBaseline reads. Benchmarks must report
environment-specific observations rather than compatibility evidence.
