# Implementation boundaries

> **Source notice:** Public-source research only. ProjectBaseline was not read. `code_imported: false`.

## Pipeline

```text
LosslessIniDocument
→ CombatDefinitionsRaw
→ reference graph and profile candidates
→ targeting/damage command descriptors
→ future deterministic simulation adapters
→ presentation adapters
```

Core creates no actors, projectiles, colliders, particles, audio, health mutation or Unity objects.

## Suggested models

`WeaponDefinitionRaw`, `WeaponReferenceRaw`, `ProjectileDefinitionRaw`, `WarheadDefinitionRaw`, `ArmorIdentityRaw`, `VersesProfileRaw`, `CombatReferenceGraph`, `TargetEligibilityResult`, `ProjectileCommand`, `ImpactCommand`, `DamageExpressionCandidate`, `DamageCommand`, `StatusCommand`, `DeathCommand`, `CombatDiagnostic`, `CombatReadLimits`, `CombatRoundtripDescriptor`.

## Formal grades

All evidence-bearing values serialize exactly one of the nine normalized grades. Source, Notes, Policy and AuditStatus are separate. No reviewed claim has original-runtime-source confirmation.

## Policy

```text
PolicyClassification: DefensiveDesign
AuditStatus: NotRun
```

Preserve raw fields/references/duplicates/unknown tails; require explicit product/extension/arithmetic/targeting/projectile profiles; never fill missing Verses with 100%, clamp values, guess Armor, infer collision from Image, select by plausible damage, execute combat during parsing or use Unity physics as authority. Use checked arithmetic, bounded collections, canonical target ordering and stable command/shot/impact identities.

## Adapter boundaries

Simulation owns world queries, targeting, projectile state, collision, health/status/death, ammo/reload, RNG and save/replay. Presentation owns art, animations, sounds, particles, muzzle flashes, impacts and UI. Parser/Core owns immutable authored descriptors and diagnostics only.

## Roundtrip

Preserve exact key/value text, percentage spelling, empty/extra Verses tokens, unknown references, extension fields, duplicates and source provenance. Canonical rewrite is explicit and never the default.
