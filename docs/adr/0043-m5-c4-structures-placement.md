# ADR-0043 - M5-C4 structures and placement foundation

Structure raw definitions, footprint validation, power projections, and
interaction candidates remain separate Core layers. The only placement profile
is explicit rectangular bounds/overlap validation. Repair, sell, capture, and
deploy return policy candidates and diagnostics; they do not mutate simulation
state, create actors, or infer stock runtime semantics. ProjectBaseline data is
not read.
