# M5-C7 performance and correctness closeout

M5-C7 adds a Unity-free, synthetic stress harness for the existing simulation
contracts. It exercises bounded harvesting, production, combat, occupancy,
targeting, autonomy, and a mixed workload at 500, 1000, and 2000 entities.

The harness keeps one `EconomyAuthority`. Each tick has a read-only proposal
phase followed by a stable source/target/kind ordered commit phase. Spatial
queries use the existing deterministic occupancy index, targeting uses stable
pairing rather than an all-to-all scan, and production descriptor parsing is
cached per entity. The canonical hash includes credits, cargo, resources,
queue progress, ownership, power, and structure health.

This is a bounded performance/correctness contract, not a wall-clock benchmark
and not a claim about stock YR complexity. It does not load ProjectBaseline
packed data, maps, or renderers and does not add M6 behavior.

Current synthetic evidence is recorded in
`docs/compatibility/evidence/m5c7-performance-closeout-synthetic-20260814.yml`.
