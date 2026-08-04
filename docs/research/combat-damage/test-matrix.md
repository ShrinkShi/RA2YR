# Test matrix — 184 research cases

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明的实现仅作行为、接口与冲突参考，未复制、翻译或机械移植其代码、公式实现、switch 表或测试夹具。`code_imported: false`。

## Rules

- 本文件只设计测试，不新增C#/Unity测试。
- expected values使用手算、小型独立表或明确fixture，不调用production damage、Verses、targeting、flight、collision、RNG或roundtrip逻辑。
- 每项记录product profile、extension provider、policy ID和evidence grade。
- Memory、seekable Stream、short-read Stream和exact MIX-window输入必须等价。
- parse/bind不得创建runtime weapon、projectile、Collider、damage/status或Unity对象。
- 总数：`28 + 28 + 32 + 28 + 24 + 20 + 24 = 184`。

## Summary

| Prefix | Category | Count |
|---|---|---:|
| WR | Weapon records and references | 28 |
| PF | Projectile and flight/collision | 28 |
| WV | Warhead, Armor and Verses | 32 |
| DR | Damage, rounding and determinism | 28 |
| TA | Targeting, area effects and friendly fire | 24 |
| AB | Ammo, burst, reload and firing state | 20 |
| SP | Special effects, presentation, safety and audit | 24 |
| **Total** | | **184** |

## WR — Weapon records and references (28)

- `WR-01` missing Weapon section
- `WR-02` duplicate Weapon sections
- `WR-03` case-collision Weapon identities
- `WR-04` map-local Weapon override provenance
- `WR-05` unknown Primary reference
- `WR-06` same Weapon in Primary and Secondary
- `WR-07` Secondary missing without fallback
- `WR-08` ElitePrimary missing
- `WR-09` EliteSecondary missing
- `WR-10` elite fallback explicit profile
- `WR-11` Weapon1 extension slot
- `WR-12` DeathWeapon reference
- `WR-13` animation Weapon reference
- `WR-14` Nuke/missile payload Weapon
- `WR-15` unknown field retained
- `WR-16` duplicate Damage key
- `WR-17` Damage zero
- `WR-18` Damage negative
- `WR-19` Damage int overflow
- `WR-20` ROF zero
- `WR-21` ROF negative
- `WR-22` Range zero
- `WR-23` Range negative
- `WR-24` MinimumRange greater than Range
- `WR-25` Burst zero
- `WR-26` Burst negative
- `WR-27` Burst overflow
- `WR-28` no runtime Weapon object

Required assertion: raw/reference provenance remains visible; no implicit fallback, normalization or runtime object creation.

## PF — Projectile and flight/collision (28)

- `PF-01` missing Projectile ref
- `PF-02` unknown Projectile section
- `PF-03` duplicate Projectile
- `PF-04` case-collision Projectile
- `PF-05` Inviso candidate
- `PF-06` Arcing candidate
- `PF-07` ROT zero
- `PF-08` ROT positive homing candidate
- `PF-09` AA only
- `PF-10` AG only
- `PF-11` AA and AG
- `PF-12` neither AA nor AG
- `PF-13` SubjectToCliffs
- `PF-14` SubjectToElevation
- `PF-15` SubjectToWalls
- `PF-16` Image missing but logic retained
- `PF-17` Image present not collision size
- `PF-18` Speed zero
- `PF-19` Speed negative
- `PF-20` Speed overflow
- `PF-21` Acceleration invalid
- `PF-22` Elasticity invalid
- `PF-23` CourseLockDuration
- `PF-24` Proximity
- `PF-25` Ranged lifetime
- `PF-26` bridge deck/under-bridge layer
- `PF-27` target disappears
- `PF-28` map edge

Required assertion: flight, tracking, collision, impact and presentation candidates remain separate and deterministic; no Unity physics inference.

## WV — Warhead, Armor and Verses (32)

- `WV-01` missing Warhead ref
- `WV-02` unknown Warhead
- `WV-03` duplicate Warhead
- `WV-04` case-collision Warhead
- `WV-05` stock 11 armor profile
- `WV-06` unknown armor
- `WV-07` duplicate armor
- `WV-08` custom named armor
- `WV-09` positional/named profile conflict
- `WV-10` Verses exactly 11
- `WV-11` Verses missing entry
- `WV-12` Verses extra entry
- `WV-13` Verses empty token
- `WV-14` Verses invalid token
- `WV-15` Verses percentage
- `WV-16` Verses decimal
- `WV-17` Verses zero
- `WV-18` Verses one percent
- `WV-19` Verses two percent candidate
- `WV-20` Verses negative
- `WV-21` Verses above 100
- `WV-22` Verses overflow
- `WV-23` trailing targeting flags
- `WV-24` force-fire separate
- `WV-25` retaliate separate
- `WV-26` passive-acquire separate
- `WV-27` AffectsAllies missing
- `WV-28` ProneDamage
- `WV-29` CellSpread raw
- `WV-30` PercentAtMax raw
- `WV-31` special effect with zero damage
- `WV-32` no current armor selection in parser

Required assertion: raw tokens/profile/evidence are retained; no missing-entry fill, extra-entry discard, percentage clamp or armor guessing.

## DR — Damage, rounding and determinism (28)

- `DR-01` single positive damage
- `DR-02` single negative healing
- `DR-03` zero damage no-effect profile
- `DR-04` zero damage effect-allowed profile
- `DR-05` multiple percentage modifiers
- `DR-06` modifier-order conflict
- `DR-07` intermediate overflow
- `DR-08` final overflow
- `DR-09` toward-zero positive
- `DR-10` toward-zero negative
- `DR-11` floor negative
- `DR-12` nearest midpoint
- `DR-13` per-stage rounding
- `DR-14` single-final rounding
- `DR-15` minimum-damage candidate
- `DR-16` armor immunity
- `DR-17` invulnerability
- `DR-18` overkill
- `DR-19` healing cap
- `DR-20` target already dead
- `DR-21` attacker and target die same tick
- `DR-22` two simultaneous impacts
- `DR-23` stable impact ordering
- `DR-24` save during projectile flight
- `DR-25` save during burst
- `DR-26` replay arithmetic equality
- `DR-27` RNG state equality
- `DR-28` no direct health mutation by binder

Required assertion: independent expected arithmetic and ordering, checked intermediates, serializable state and no binder mutation.

## TA — Targeting, area effects and friendly fire (24)

- `TA-01` CanSelect versus CanFire
- `TA-02` CanTrack versus CanCollide
- `TA-03` CanApplyWarhead versus CanDamage
- `TA-04` automatic acquire versus force fire
- `TA-05` ally target
- `TA-06` owner/self target
- `TA-07` neutral target
- `TA-08` enemy target
- `TA-09` cloaked target
- `TA-10` disguised target
- `TA-11` limbo/dead target
- `TA-12` air target
- `TA-13` ground target
- `TA-14` submerged target
- `TA-15` bridge deck target
- `TA-16` under-bridge target
- `TA-17` CellSpread zero
- `TA-18` CellSpread very large
- `TA-19` PercentAtMax center/edge
- `TA-20` same-cell multiple targets
- `TA-21` multi-cell building per-cell candidate
- `TA-22` multi-cell building once candidate
- `TA-23` deterministic area order
- `TA-24` Unity Physics forbidden

Required assertion: eligibility dimensions, area shape/distance/falloff and stable affected-object ordering remain explicit.

## AB — Ammo, burst, reload and firing state (20)

- `AB-01` ammo empty
- `AB-02` ammo zero capacity
- `AB-03` ammo negative capacity
- `AB-04` infinite-ammo sentinel candidate
- `AB-05` per-shot ammo
- `AB-06` per-burst ammo
- `AB-07` partial magazine
- `AB-08` reload begins
- `AB-09` reload interrupted
- `AB-10` aircraft reload profile
- `AB-11` ROF cooldown
- `AB-12` burst inter-shot delay
- `AB-13` burst target invalidated
- `AB-14` burst retarget profile
- `AB-15` turret not aligned
- `AB-16` deploy-to-fire
- `AB-17` powered/EMP disabled
- `AB-18` gattling extension state
- `AB-19` save/load mid-burst
- `AB-20` no animation-driven firing

Required assertion: authored capacity, runtime count, cooldown, reload, burst command/result and presentation remain separate.

## SP — Special effects, presentation, safety and audit (24)

- `SP-01` fire effect
- `SP-02` radiation effect
- `SP-03` mind control
- `SP-04` temporal
- `SP-05` psychic/berserk
- `SP-06` parasite
- `SP-07` Ivan bomb
- `SP-08` EMP/stun
- `SP-09` locomotor change
- `SP-10` disguise removal
- `SP-11` resource damage
- `SP-12` bridge/wall damage
- `SP-13` death weapon chain
- `SP-14` InfDeath reference
- `SP-15` impact AnimList
- `SP-16` laser/beam presentation
- `SP-17` particle/audio missing does not remove logic
- `SP-18` Memory/Stream equivalence
- `SP-19` short-read equivalence
- `SP-20` MIX-window equivalence
- `SP-21` budgets
- `SP-22` no-progress protection
- `SP-23` noEngineReferences
- `SP-24` no Unity projectile/status objects

Required assertion: capability, runtime status, death chain, presentation, input safety and public-audit boundaries remain separate.

## Oracle constraints

- Damage expected values are handwritten small-integer examples.
- Verses expected bindings use independent positional tables.
- Area expected affected sets are enumerated manually on tiny synthetic maps.
- Projectile expected paths are explicit fixture points, not generated by production flight code.
- RNG fixtures provide exact recorded draws.
- Target sorting expected order is literal fixture data.
- Roundtrip tests compare raw spelling and occurrence identity.

## Required architecture assertions

- no `UnityEngine`;
- no Rigidbody/Collider/Physics;
- no GameObject/ParticleSystem/AudioSource;
- no health/ammo/cooldown/status mutation;
- bounded collections and arithmetic;
- deterministic ordering;
- structured diagnostics;
- no ProjectBaseline access.

## Evidence grades

- `ConfirmedByOfficialRuntimeSource`
- `ConfirmedByOfficialEditorSource`
- `ConfirmedByIndependentImplementation`
- `CommunityDocumented`
- `ObservedByFutureProjectBaselineAudit`
- `ConfiguredForProjectPolicy`
- `Unresolved`

没有完整公开的 RA2/YR 原版战斗运行时源码。官方 FinalSun/FinalAlert 2 只能提供编辑器、字段目录和authoring行为证据，不能替代 `gamemd.exe` 运行时证据。
