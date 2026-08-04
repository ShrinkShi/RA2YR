# Weapon records and usage binding

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明的实现仅作行为、接口与冲突参考，未复制、翻译或机械移植其代码、公式实现、switch 表或测试夹具。`code_imported: false`。


## 1. Type-to-weapon references

Candidate stock and extension usages include:

```text
Primary
Secondary
ElitePrimary
EliteSecondary
Weapon1..N
EliteWeapon1..N
DeathWeapon
AirstrikeWeapon
NukePayload / missile payload
Spawner weapon
Repair weapon
Deploy weapon
NoAmmoWeapon
animation weapon
super-weapon detonation weapon
extension-specific weapon references
```

No list above is assumed universally stock. Every usage stores product profile and extension provider.

## 2. Required identities

```text
WeaponTypeDefinition
WeaponReferenceRaw
WeaponUsageKind
WeaponMountDescriptor
FiringSlot
WeaponSelectionCandidate
RuntimeWeaponInstance
AmmoState
TargetingState
```

A weapon name reused by multiple slots points to one logical definition but retains independent usage edges.

## 3. Binding behavior

For every reference retain:

- raw source text;
- source type and field;
- section/key occurrence;
- normalized lookup candidate;
- composed Rules layer;
- all candidate matches;
- selected policy, if any;
- dangling/ambiguous diagnostics;
- elite/veteran applicability;
- extension provider.

Forbidden implicit behavior:

- unknown reference → silent no-weapon;
- missing Secondary → Primary;
- missing ElitePrimary/EliteSecondary → rookie weapon;
- case collision → arbitrary winner;
- name-based semantic inference;
- parser-created runtime weapon.

Fallback may exist only in `WeaponSlotPolicy` with evidence, product scope and diagnostics.

## 4. Weapon raw fields

Candidate fields to retain without assuming stock applicability:

```text
Damage
ROF
Range
MinimumRange
Projectile
Warhead
Speed
Burst
Report
Anim
Bright
Camera
Lobber
OmniFire
AreaFire
FireOnce
TurboBoost
Supress
UseSparkParticles
IsLaser
IsHouseColor
LaserInnerColor
LaserOuterColor
LaserOuterSpread
Beam
RadLevel
LimboLaunch
ProjectileRange
AmbientDamage
extension fields
```

Each field stores raw spelling, raw value, scalar/list/reference candidates, default candidate, product/provider profile and evidence.

## 5. Weapon numerical distinctions

```text
AuthoredBaseDamage
WeaponDamageCandidate
CurrentFirepowerMultiplier
VeterancyModifier
WarheadArmorMultiplier
DistanceFalloff
AreaFalloff
FinalAppliedDamage
DisplayedDamage
ScoreValue
```

`Damage=0` may be non-damaging or still carry effects under extension profiles. Negative damage is retained as healing/reversal candidate; it is not clamped.

## 6. ROF, Range and MinimumRange

Do not choose source units in the parser. Preserve:

- integer spelling and sign;
- zero/negative values;
- overflow;
- product conversion profile;
- game-speed relationship candidate;
- minimum/maximum conflict;
- range modifier provenance;
- target layer/range query inputs.

OpenRA's tick and world-distance units are implementation-specific and cannot define Westwood units.

## 7. Weapon lists and registration

Ares documents `[WeaponTypes]` as an extension registry for parsing weapons not otherwise referenced. This is extension evidence only. Stock discovery may include references, hard-coded lookups or registries; unresolved weapons are retained rather than fabricated.

## 8. Mount and muzzle boundary

A mount may refer to:

- body/turret/barrel;
- muzzle/FLH anchor;
- weapon slot;
- facing/arc;
- recoil/charge animation;
- fire ports;
- aircraft hardpoints.

Mount geometry is presentation/actor metadata. It does not alter raw Weapon identity or target legality.

## 9. Disabled and unavailable weapons

Keep separate candidates:

```text
ReferenceMissing
ProfileUnsupported
ActorDisabled
PoweredOff
EMPDisabled
NoAmmo
Cooldown
TurretNotAligned
TargetInvalid
RangeInvalid
MissionDisallows
ExtensionCondition
```

No single `WeaponDisabled` parser bool replaces these runtime reasons.


## Evidence grades

- `ConfirmedByOfficialRuntimeSource`
- `ConfirmedByOfficialEditorSource`
- `ConfirmedByIndependentImplementation`
- `CommunityDocumented`
- `ObservedByFutureProjectBaselineAudit`
- `ConfiguredForProjectPolicy`
- `Unresolved`

没有完整公开的 RA2/YR 原版战斗运行时源码。官方 FinalSun/FinalAlert 2 只能提供编辑器、字段目录和 authoring 行为证据，不能替代 `gamemd.exe` 运行时证据。
