# M3-R14 — Combat damage and targeting research dossier

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明的实现仅作行为、接口与冲突参考，未复制、翻译或机械移植其代码、公式实现、switch 表或测试夹具。`code_imported: false`。

## Scope

本目录研究未来 RA2/YR 兼容战斗系统所需的声明式输入和边界：

- type → Weapon 引用和 firing slots；
- Weapon / Projectile / Warhead 原始记录；
- Armor 与 positional/named Verses；
- target eligibility、area effects 和 friendly fire；
- Damage、ROF、Range、Burst、Ammo、reload；
- projectile flight/collision 候选；
- special effects、death follow-ups、presentation/audio；
- deterministic ordering、RNG、save/load、replay、multiplayer；
- bounded Core contracts、测试矩阵和脱敏审计请求。

本目录不实现 combat parser、firing、projectile、collision、targeting、damage、armor runtime、status、death、AI、animation、audio 或 Unity 对象。

## Frozen candidate pipeline

```text
raw Rules/Art and type references
→ weapon/projectile/warhead raw records
→ explicit product and extension profiles
→ logical combat reference graph
→ target-eligibility candidates
→ damage-expression candidates
→ deterministic combat command/result contracts
→ future simulation and presentation adapters
```

## Non-collapse rules

```text
WeaponTypeDefinition != WeaponReferenceRaw
WeaponMount != RuntimeWeaponInstance
ProjectileTypeDefinition != ProjectileRuntimeState
WarheadTypeDefinition != AppliedStatus
ArmorIdentityRaw != selected armor
Verses multiplier != all targeting permissions
CanSelectTarget != CanFireAtTarget != CanApplyWarhead != CanDamageTarget
Damage expression != health mutation
Impact tick != animation frame
Logical projectile != rendered projectile
```

## Strongest conclusions

1. `Primary`, `Secondary`, elite slots, death/special weapons and extension slots remain explicit references with provenance; no parser fallback is implicit.
2. Weapon `Damage`, Projectile and Warhead references belong to the Weapon record, but current ammo/cooldown/selection belong to runtime state.
3. Projectile fields describe authored candidates, not Unity physics configuration.
4. Stock RA2/YR Armor is strongly documented as an ordered 11-entry profile, while Ares/Vinifera named armor extensions are separate provider profiles.
5. `Verses` carries at least a damage-multiplier candidate and, in RA2/YR community/extension evidence, force-fire/retaliation/passive-acquire side effects. These dimensions are stored separately.
6. CellSpread, distance metric, falloff, object enumeration and building multi-cell behavior are not one formula.
7. `AffectsAllies` is not equivalent to target acquisition and does not automatically define owner/self handling or all special effects.
8. Deterministic combat requires stable command/projectile/shot/impact identities, target ordering, integer/fixed-point policy and serializable RNG.
9. Presentation references never determine collision, impact, damage, status duration or target legality.
10. Core remains independent of `UnityEngine` and creates no Rigidbody, Collider, ParticleSystem, AudioSource or GameObject.

## Directory map

- `layer-and-domain-boundaries.md`
- `weapon-records-and-usage-binding.md`
- `projectile-flight-and-collision.md`
- `warhead-verses-and-armor.md`
- `damage-resolution-and-rounding.md`
- `targeting-area-effects-and-friendly-fire.md`
- `burst-ammo-reload-and-firing-state.md`
- `special-effects-and-presentation.md`
- `source-comparison.md`
- `implementation-boundaries.md`
- `test-matrix.md`
- `baseline-audit-request.md`
- `unresolved-questions.md`

## Source anchors

Pinned/reference source families:

- EA FinalSun / FinalAlert 2, commit `6abf0f557469baea73079c6bf6550709e2e3584e`;
- OpenRA, commit `a520984d91eda9de48a62b1d15c1e3bad0d4fb1a`;
- World-Altering Editor, commit `b4c9481e9b00fb0a38739049a046f528b6054ce2`;
- Chrono Divide mod SDK, revision `5943c4ae6c19897929d348a417d6d2f1481b75fd`;
- CnCNet client, revision `e6e367bbe04c1a0dc1e34a8fed2856ea3ab7e8c4`;
- Ares 3.0 documentation;
- Phobos stable/latest documentation;
- Vinifera latest/master documentation;
- ModEnc permanent/current combat pages;
- PPM and RA2 DIY tutorials/discussions as community evidence only.

Shared XCC/OpenRA/community lineage is not counted as multiple independent original-runtime proofs.

## Evidence grades

- `ConfirmedByOfficialRuntimeSource`
- `ConfirmedByOfficialEditorSource`
- `ConfirmedByIndependentImplementation`
- `CommunityDocumented`
- `ObservedByFutureProjectBaselineAudit`
- `ConfiguredForProjectPolicy`
- `Unresolved`

没有完整公开的 RA2/YR 原版战斗运行时源码。官方 FinalSun/FinalAlert 2 只能提供编辑器、字段目录和 authoring 行为证据，不能替代 `gamemd.exe` 运行时证据。
