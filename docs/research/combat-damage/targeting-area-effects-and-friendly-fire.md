# Targeting, area effects and friendly fire

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明的实现仅作行为、接口与冲突参考，未复制、翻译或机械移植其代码、公式实现、switch 表或测试夹具。`code_imported: false`。


## 1. Eligibility dimensions

```text
CanSelectTarget
CanIssueAttackCommand
CanFireAtTarget
CanTrackTarget
CanCollideWithTarget
CanApplyWarhead
CanDamageTarget
CanAcquireAutomatically
CanForceFire
CanRetaliate
```

A single `CanTarget` bool is forbidden.

## 2. Target categories

Candidate dimensions include:

- infantry, vehicle, aircraft, building;
- wall, terrain, overlay, resource;
- ground, water, shore, bridge deck, under bridge, air, submerged, subterranean;
- ally, enemy, owner/self, neutral;
- cloaked, disguised, mind-controlled;
- temporal, invulnerable, limbo, dead;
- projectile target under extensions;
- actor target, cell target, area target.

## 3. AA and AG

Projectile `AA`/`AG` are target-profile candidates. They do not alone answer:

- whether the Weapon can select a target;
- whether Verses forbids armor;
- whether a cell may be force-fired;
- whether the projectile can collide;
- whether the Warhead affects airborne targets;
- whether the target changed altitude after launch.

## 4. CellSpread

Store independently:

```text
CellSpreadRaw
AreaShapeCandidate
DistanceMetricCandidate
FalloffCandidate
CenterDefinitionCandidate
AffectedCellCandidate
AffectedObjectCandidate
DamageApplicationOrder
```

Potential metrics:

- continuous world distance;
- cell-center distance;
- diamond/manhattan;
- euclidean horizontal;
- 3D volumetric;
- foundation-cell enumeration;
- closest hit shape;
- product/extension-specific profile.

No metric is chosen by parser.

## 5. PercentAtMax

Community documentation describes linear falloff between center and maximum radius, but original exact units, interpolation, rounding, vertical handling and building enumeration remain unresolved.

Preserve raw numeric spelling and explicit formula profile. Never reuse a production formula as the synthetic test oracle.

## 6. Multi-cell objects

Buildings or bridges may appear in multiple affected cells. Policies:

```text
PerObjectOnce
PerOccupiedCell
ClosestCellOnly
MergeDamage
MaxAffectN
ExtensionSpecific
Unresolved
```

Ares `CellSpread.MaxAffect` and Phobos `MergeBuildingDamage` are extension evidence, not stock behavior.

## 7. Friendly fire

Required relationship snapshot:

```text
SourceOwner
CurrentTargetOwner
CurrentAllianceRelation
OriginalProjectileOwner?
MindControlInvoker?
CapturedSource?
NeutralRelation
SelfIdentity
```

`AffectsAllies` does not define all dimensions. Ares adds `AffectsEnemies` and `AffectsOwner`, demonstrating that owner, allies and enemies need separate policy fields.

## 8. Effects and allies

Separate:

```text
ConventionalDamageAllowed
HealingAllowed
StatusEffectAllowed
TerrainEffectAllowed
PresentationAllowed
TargetAcquisitionAllowed
```

Community evidence indicates some side effects may not follow `AffectsAllies` identically. This remains product/effect scoped.

## 9. Walls, cliffs and bridges

Projectile obstruction, Warhead wall damage, bridge-deck targeting and area enumeration are separate:

```text
ProjectileCanCrossWall
ProjectileCanCrossCliff
ProjectileCanReachLayer
WarheadCanDamageWall
WarheadCanDamageBridge
AreaIncludesUnderBridge
AreaIncludesDeck
```

## 10. Stable affected-object order

Future area resolution sorts candidates by explicit tuple, for example:

```text
Layer
DistanceBucketOrExactDistance
CellIdentity
ObjectStableId
HitShapeOrdinal
```

The chosen tuple remains a policy and must be serialized.


## Evidence grades

- `ConfirmedByOfficialRuntimeSource`
- `ConfirmedByOfficialEditorSource`
- `ConfirmedByIndependentImplementation`
- `CommunityDocumented`
- `ObservedByFutureProjectBaselineAudit`
- `ConfiguredForProjectPolicy`
- `Unresolved`

没有完整公开的 RA2/YR 原版战斗运行时源码。官方 FinalSun/FinalAlert 2 只能提供编辑器、字段目录和 authoring 行为证据，不能替代 `gamemd.exe` 运行时证据。
