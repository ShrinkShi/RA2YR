# Combat research test matrix — 184 design cases

> **Source notice:** Public-source research only. ProjectBaseline was not read. Synthetic design only; no original assets or implementation code. `code_imported: false`.

## Coverage

| Category | Cases |
|---|---:|
| Weapon records and references | 28 |
| Projectile flight and collision | 28 |
| Warhead, Armor and Verses | 32 |
| Damage, rounding and determinism | 28 |
| Targeting, area effects and friendly fire | 24 |
| Ammo, burst, reload and firing state | 20 |
| Effects, presentation, safety and audit | 24 |
| **Total** | **184** |

## Required coverage

Raw/duplicate/case/unknown-tail preservation; missing and ambiguous references; Primary/Secondary/elite/death/special slot policies; numeric overflow and invalid values; projectile Inviso/Arcing/ROT/tracking/collision profiles; Armor identity and positional/named Verses lengths; empty/extra/malformed percentages; force-fire/retaliation/acquire side effects; damage-stage/rounding/minimum/negative candidates; CellSpread shape/falloff/multi-cell ordering; AffectsAllies/owner/self; line-of-fire/terrain/bridge layers; ammo/ROF/burst/reload/target snapshots; effect/status/death boundaries; deterministic command/shot/projectile/impact IDs and RNG; bounded Memory/Stream/short-read/MIX equivalence; no Unity dependency.

## Evidence discipline

Expected results use one normalized grade. Official-editor fixtures confirm official-tool behavior only; named engine/extension fixtures are implementation-specific; community conventions do not prove execution; conflicting algorithms remain conflicting; runtime-unsourced behavior remains underconfirmed/unresolved; project safety expectations are defensive design.

```text
AuditStatus: NotRun
FutureEvidenceSource: ProjectBaselineAggregateAudit
```

## Fixture rules

Use tiny independent synthetic values; no original Rules/Art/maps or source-derived switch tables; expected damage/Verses/flight/collision/RNG/target order must not call production logic; no trial profile selection; no rendering/audio/Unity; passing tests never promotes runtime compatibility.
