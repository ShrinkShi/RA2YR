# Projectile flight and collision

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明的实现仅作行为、接口与冲突参考，未复制、翻译或机械移植其代码、公式实现、switch 表或测试夹具。`code_imported: false`。


## 1. Projectile raw fields

Candidate fields:

```text
Image
Inviso
Arcing
ROT
AA
AG
Shadow
Proximity
Ranged
SubjectToCliffs
SubjectToElevation
SubjectToWalls
VeryHigh
Level
Cluster
CourseLockDuration
Elasticity
Acceleration
Dropping
Floater
Parachuted
Degenerates
Arm
High
Airburst
AirburstWeapon
ShrapnelWeapon
ShrapnelCount
extension fields
```

Field presence does not prove stock applicability. Provider and product profile are mandatory.

## 2. Required model split

```text
ProjectileTypeDefinition
ProjectileSpawnCommand
ProjectileFlightProfileCandidate
ProjectileRuntimeState
ProjectilePresentation
TargetTrackingCandidate
CollisionQuery
ImpactCommand
```

`Image` is a visual reference, never a collision shape.

## 3. Flight families

Preserve candidates rather than selecting one physics algorithm:

- immediate/invisible delivery;
- straight;
- guided/homing;
- arcing;
- ballistic;
- dropping/falling;
- floating/parachuted;
- cluster/airburst;
- spawned or shrapnel child;
- beam/laser presentation with separate logical impact;
- aircraft-as-missile extension;
- custom trajectory extension.

`Inviso=yes` is not automatically hitscan. `Arcing=yes` is not automatically a Unity ballistic body. `ROT=0` is not interpreted as stationary.

## 4. Spawn contract

`ProjectileSpawnCommand` candidate inputs:

```text
StableProjectileIdentity
SourceActorIdentity
WeaponUsageIdentity
ShotOrdinal
BurstOrdinal
SourcePosition
MuzzleAnchor
SourceFacing
TargetSnapshot
PassiveTargetPosition
DamageModifierSnapshot
RangeModifierSnapshot
ProductProfile
RandomDrawInputs
```

No renderer completion or GameObject identity enters this contract.

## 5. Tracking

Separate:

```text
AuthoredTarget
CurrentTrackedTarget
LastKnownTargetPosition
PassiveTargetPosition
CourseLockState
TrackingTurnCandidate
RetargetCandidate
TargetLostPolicy
```

Target disappearance, movement, cloak, limbo, death, ownership change and layer transition are simulation events, not parser behavior.

## 6. Collision

`CollisionQuery` requires explicit dimensions:

```text
ProjectileSegmentOrVolume
SourceLayer
CurrentLayer
TargetLayer
TerrainSurface
Cliff/Wall candidates
BridgeDeck/UnderBridge candidates
Actor hit candidates
Map boundary
Proximity radius
Collision ordering policy
```

Do not derive collision from SHP/VXL pixels or use Unity Physics overlap as source semantics.

## 7. Layer handling

Candidate impact layers:

```text
Ground
UnderBridge
BridgeDeck
Air
Submerged
Subterranean
UnknownExtensionLayer
```

A projectile may travel visually over one layer while applying an impact to another. `High`, `VeryHigh`, placement `High`, aircraft altitude and bridge layer are distinct inputs.

## 8. Map edges and lifetime

Preserve candidates for:

- range exhausted;
- explicit projectile range;
- target reached;
- terrain hit;
- blocker hit;
- proximity;
- map edge;
- lifetime/degeneration;
- bounce/elasticity;
- parent removed;
- target invalidated.

No implicit destruction based on visual leaving the viewport.

## 9. Independent implementation evidence

OpenRA separates `WeaponInfo`, `ProjectileArgs`, projectile type, runtime projectile, collision/blocker queries, impact and rendering. Its specific world units, interpolation, random distribution and collision algorithms remain implementation-specific reference only.

## 10. Safety limits

`CombatReadLimits` candidates include:

- max projectile types;
- max fields per projectile;
- max child/cluster references;
- max flight profile candidates;
- max collision categories;
- max lifetime/range numeric magnitude;
- max diagnostics;
- max reference depth/cycles.


## Evidence grades

- `ConfirmedByOfficialRuntimeSource`
- `ConfirmedByOfficialEditorSource`
- `ConfirmedByIndependentImplementation`
- `CommunityDocumented`
- `ObservedByFutureProjectBaselineAudit`
- `ConfiguredForProjectPolicy`
- `Unresolved`

没有完整公开的 RA2/YR 原版战斗运行时源码。官方 FinalSun/FinalAlert 2 只能提供编辑器、字段目录和 authoring 行为证据，不能替代 `gamemd.exe` 运行时证据。
