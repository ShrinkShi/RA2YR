# Burst, ammo, reload and firing state

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明的实现仅作行为、接口与冲突参考，未复制、翻译或机械移植其代码、公式实现、switch 表或测试夹具。`code_imported: false`。


## 1. Required state split

```text
AuthoredAmmoCapacity
RuntimeAmmoCount
WeaponCooldown
ReloadState
BurstDescriptor
BurstState
FiringCommand
FiringResult
PresentationState
```

`ROF`, `Burst`, `Ammo` and reload fields are configuration candidates, not the state machine itself.

## 2. Burst descriptor

Candidate fields:

```text
AuthoredBurstCount
BurstDelayCandidates
PerShotDamageCandidate
PerShotAmmoCandidate
PerBurstAmmoCandidate
MuzzleSequence
TargetSnapshotPolicy
RetargetBetweenShotsPolicy
AbortPolicy
EliteProfile
ExtensionProvider
```

`Burst=0`, negative and overflow values remain diagnostics, not parser repairs.

## 3. Firing readiness

Candidate blockers:

- weapon cooldown;
- actor disabled/EMP/frozen;
- no ammo;
- reload in progress;
- turret not aligned;
- deploy-to-fire not ready;
- powered state;
- mission/command invalid;
- target invalid/out of range/minimum range;
- weapon slot unavailable;
- gattling/charge state;
- extension condition.

## 4. Command/result boundary

```text
FiringCommand
- tick
- source
- slot/mount
- target snapshot
- command ordinal
- requested burst

FiringResult
- accepted/rejected
- reason diagnostics
- spawned projectile commands
- immediate impact commands
- ammo mutation candidate
- cooldown mutation candidate
- presentation events
```

Parser creates neither object.

## 5. Ammo

Questions preserved:

- capacity unit;
- shared or per-weapon ammo;
- per-shot or per-burst consumption;
- zero/negative capacity;
- infinite sentinel;
- reload source;
- aircraft reload;
- partial magazine;
- savegame representation;
- elite weapon interaction;
- no-ammo fallback weapon;
- extension magazines.

## 6. ROF and reload

Keep separate:

```text
WeaponROFCandidate
BurstInterShotDelayCandidate
MagazineReloadCandidate
AircraftReloadCandidate
ActorRateModifierCandidate
GameSpeedConversionCandidate
```

OpenRA tick units and Ares aircraft reload extensions are implementation/extension evidence only.

## 7. Target snapshots during burst

Candidate policies:

```text
LockTargetAtBurstStart
RevalidateEveryShot
TrackCurrentTarget
UseLastKnownPosition
AbortOnInvalid
Retarget
ContinueAtCell
```

No stock policy is selected.

## 8. Deterministic burst identity

```text
StableFiringCommandId
BurstOrdinal
ShotOrdinal
MountOrdinal
ProjectileOrdinal
RandomDrawOrdinal
```

Save/load stores enough state to resume without duplicate/missing shots.

## 9. Presentation

Charge, muzzle flash, recoil, report, turret animation and shell casing are outputs. They do not trigger authoritative shots.

## 10. Non-goals

No firing state machine, ammo mutation, reload timer, coroutine, animation event, turret controller or AI selection is implemented.


## Evidence grades

- `ConfirmedByOfficialRuntimeSource`
- `ConfirmedByOfficialEditorSource`
- `ConfirmedByIndependentImplementation`
- `CommunityDocumented`
- `ObservedByFutureProjectBaselineAudit`
- `ConfiguredForProjectPolicy`
- `Unresolved`

没有完整公开的 RA2/YR 原版战斗运行时源码。官方 FinalSun/FinalAlert 2 只能提供编辑器、字段目录和 authoring 行为证据，不能替代 `gamemd.exe` 运行时证据。
