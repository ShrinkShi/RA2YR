# M4-C5 combat, abilities, and tactical proposals

M4-C5 adds a Unity-free deterministic combat foundation above the C1-C4
simulation contracts. `AttackProposal` is validated against an explicit weapon
descriptor, range, target-validity and cooldown state. Accepted proposals emit
`DamageEvent` values; only the bounded canonical commit stage mutates the
health ledger and emits ordered `Death` values. Workers never mutate health.

The generic ability contract (`AbilityDescriptor`, `AbilityState`, target
candidates, decision profile and use proposal) is policy-driven and bounded.
It supports synthetic defensive/area/retreat utility examples, friendly-fire
rejection, resource/cooldown opportunity cost and the existing
`AutonomyEnvelope`. `RetreatProposal` and `CrushThreatCandidate` remain
movement proposals, not Unity physics or renderer behavior.

No Rules/Art parser, projectile flight, warhead/Verses/armor semantics,
veterancy, stock special weapon, animation, VFX, renderer, writer or gameplay
loop is introduced. C5 adds no codec or writer. All evidence is synthetic
project enhancement evidence and no ProjectBaseline packed data was read.
