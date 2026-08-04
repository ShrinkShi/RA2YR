# Special effects and presentation

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明的实现仅作行为、接口与冲突参考，未复制、翻译或机械移植其代码、公式实现、switch 表或测试夹具。`code_imported: false`。


## 1. Special-effect families

Candidate families:

```text
NormalHealthDamage
Healing
Fire
Radiation
MindControl
Temporal
Psychic/Berserk
Parasite
IvanBomb
EMP/Stun
LocomotorChange
DisguiseRemoval
Veterancy
ResourceDamage
WallDamage
BridgeDamage
TerrainDeformation
Spawning
DeathWeapon
ExtensionEffect
```

## 2. Required separation

For each effect retain:

```text
WarheadCapability
ImpactCommandCandidate
RuntimeStatusState
PeriodicEffectDescriptor
RemovalConditionCandidate
SavegameStateCandidate
PresentationEffect
EvidenceGrade
```

A flag such as `Temporal=yes` is not current temporal state.

## 3. Damage gating

Special effects may depend on:

- target eligibility;
- verses;
- positive/zero damage;
- relationship;
- immunity;
- health threshold;
- armor/shield;
- effect-specific capability.

Ares and Phobos explicitly provide `EffectsRequireDamage`/`EffectsRequireVerses` style extensions, confirming the need for independent gates.

## 4. Death boundary

Candidate death chain:

```text
LethalDamageCandidate
→ DeathResolutionCommand
→ InfDeath / death animation reference
→ explosion/debris candidates
→ death weapon candidate
→ crew/pilot/survivor candidates
→ husk/rubble candidate
→ wall/bridge/resource consequences
→ score/statistics
→ Trigger event candidate
```

No order is claimed as stock runtime fact.

## 5. Presentation references

Separate logical references:

- projectile Image;
- muzzle `Anim`;
- firing `Report`;
- impact `AnimList`;
- `InfDeath`;
- laser/beam color and geometry;
- Bright/combat light;
- particle/particle system;
- smoke/contrail;
- shadow;
- debris;
- screen shake/camera;
- UI hit feedback.

Core resolves identities only. It does not load SHP, VXL, HVA, WAV, palette or particle assets.

## 6. Presentation does not decide

- spawn tick;
- projectile path;
- collision shape;
- impact tick;
- damage;
- armor;
- area radius;
- effect duration;
- death;
- target legality;
- random outcome.

## 7. Immediate and invisible delivery

A laser, beam or invisible projectile can have immediate-looking presentation while simulation still uses a logical firing/impact contract. Conversely, a visible projectile may be decorative under extension profiles. Presentation type is not a flight-model oracle.

## 8. Audio ownership and capture

Report/impact/death sounds consume event data containing logical source, owner visibility and position. Audio playback does not feed combat simulation.

## 9. Save/load

Savegame state belongs to active projectiles, cooldowns, ammo, periodic statuses, bombs, temporal/mind control and deterministic RNG. Static Art references remain configuration.

## 10. Screen shake and lights

Camera shake, combat light and Bright are presentation events. They do not change damage or reveal targets unless a separate simulation rule explicitly says so.


## Evidence grades

- `ConfirmedByOfficialRuntimeSource`
- `ConfirmedByOfficialEditorSource`
- `ConfirmedByIndependentImplementation`
- `CommunityDocumented`
- `ObservedByFutureProjectBaselineAudit`
- `ConfiguredForProjectPolicy`
- `Unresolved`

没有完整公开的 RA2/YR 原版战斗运行时源码。官方 FinalSun/FinalAlert 2 只能提供编辑器、字段目录和 authoring 行为证据，不能替代 `gamemd.exe` 运行时证据。
