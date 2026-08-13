# ADR-0036 - M4-C5 minimal combat and ability contracts

## Decision

Keep combat proposal computation separate from authoritative mutation:
`AttackProposal -> validation -> DamageEvent -> canonical health/death commit`.
Use immutable weapon/cooldown/health records, bounded damage event input and
stable tick/target/source/ordinal ordering. Add a generic ability candidate and
proposal contract whose use is gated by the resolved `AutonomyEnvelope` and
explicit capability/profile policy.

## Boundary

These are synthetic project-enhancement contracts. They do not claim original
YR damage, projectile, warhead, armor, ammo, veterancy, special ability,
renderer or AI compatibility. Missing or conflicting stock semantics remain
profile-explicit and unresolved; no ProjectBaseline packed data was read.
