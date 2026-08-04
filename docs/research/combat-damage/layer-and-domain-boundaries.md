# Layer and domain boundaries

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明的实现仅作行为、接口与冲突参考，未复制、翻译或机械移植其代码、公式实现、switch 表或测试夹具。`code_imported: false`。

## 1. Layer graph

```text
LosslessIniDocument
→ CombatRawSectionCollection
→ Weapon/Projectile/Warhead raw views
→ ProductProfile + ExtensionProvider
→ CombatReferenceGraph
→ CombatSemanticCandidates
→ FutureCombatSimulation
→ FutureCombatPresentation
```

每一层只能增加derived信息，不能覆盖raw identity。

## 2. Required domains

### Raw configuration domain

保留section/key occurrence、casing、whitespace、numeric spelling、empty value、duplicates、unknown fields和source-layer provenance。

### Type registry domain

包含TechnoType、WeaponType、ProjectileType、WarheadType、Armor、Animation/Sound/Particle和Trigger parameter候选身份，不创建runtime对象。

### Mount and usage domain

包含firing slot、mount index、muzzle/FLH、turret/body/barrel关系、elite/veteran候选、death/special/deploy/spawner用途。

### Target-query domain

```text
CanSelectTarget
CanFireAtTarget
CanTrackTarget
CanCollideWithTarget
CanApplyWarhead
CanDamageTarget
CanAcquireAutomatically
CanForceFire
CanRetaliate
```

### Projectile simulation domain

包含logical source/target、current state、flight/tracking/collision候选和stable projectile identity。

### Damage-expression domain

包含immutable inputs和候选运算；不修改health、armor、status、terrain、wall、bridge或resource。

### Mutation domain

未来simulation负责health/healing、status、death、wall/bridge/resource mutation、ammo/cooldown、score/statistics和Trigger notification。

### Presentation domain

负责muzzle flash、projectile visual、beam/laser、trail、report、impact animation、particle、light、shadow、screen shake和UI反馈。

## 3. Cross-PR boundaries

- PR #31 placement只提供authored type/owner/health/facing候选。
- PR #32 Trigger graph只提供opaque combat opcode/parameter候选。
- PR #33 House/alliance只提供authored关系，不决定runtime friendly fire。
- PR #34 environment/audio只提供media references和presentation profiles。
- PR #35 presentation只提供anchors/passes/bounds，不做hit detection。
- PR #37 movement/topology只提供layers/occupancy，不做projectile physics。
- PR #38 resource/economy只提供resource cells/economy state，不做warhead mutation。

## 4. Raw/derived/runtime separation

```text
RawWeaponSection != WeaponTypeDescriptor
WeaponTypeDescriptor != RuntimeWeaponInstance
RawProjectileSection != FlightModel
FlightModelCandidate != ProjectileRuntimeState
RawWarheadSection != ImpactCommand
ArmorRaw != TargetArmorSnapshot
DamageResolutionDescriptor != DamageResult
DamageResult != PresentationEffect
```

## 5. Failure behavior

- dangling refs保留并诊断；
- duplicate sections/keys保持ordered occurrences；
- unknown product/extension保持raw；
- invalid numeric text不修复；
- missing field不静默补默认；
- 禁止plausibility-based profile selection；
- 不运行地图/游戏来“试哪个能用”。

## 6. Core engine boundary

Core只使用plain immutable values、checked integers和bounded streams，不引用UnityEngine、Rigidbody、Collider、Physics、Transform、GameObject、MonoBehaviour、ParticleSystem、AudioSource、frame time、wall clock、Unity random或instance ID。

## Evidence grades

- `ConfirmedByOfficialRuntimeSource`
- `ConfirmedByOfficialEditorSource`
- `ConfirmedByIndependentImplementation`
- `CommunityDocumented`
- `ObservedByFutureProjectBaselineAudit`
- `ConfiguredForProjectPolicy`
- `Unresolved`

没有完整公开的 RA2/YR 原版战斗运行时源码。官方编辑器证据不能替代runtime证据。
