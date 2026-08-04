# Warhead, Verses and Armor

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明的实现仅作行为、接口与冲突参考，未复制、翻译或机械移植其代码、公式实现、switch 表或测试夹具。`code_imported: false`。


## 1. Warhead candidate fields

```text
Verses
CellSpread
PercentAtMax
Wall
Wood
Ore
Conventional
Rocker
Sparky
Fire
ProneDamage
AffectsAllies
AnimList
InfDeath
Deform
DeformThreshhold
Ripple
Bright
CombatLightSize
Particle
ParticleSystems
Radiation
MindControl
Temporal
PsychicDamage
Parasite
BombDisarm
IvanBomb
EMP / EMEffect
Tiberium
Locomotor / IsLocomotor
extension fields
```

Every field is product/provider scoped. TS, RA2/YR, Ares, Phobos, Vinifera and OpenRA behavior must not be merged into one vanilla profile.

## 2. Warhead identity

```text
WarheadTypeRaw
WarheadTypeDescriptor
WarheadReferenceRaw
WarheadCapability
ImpactCommandCandidate
SpecialEffectDescriptor
CombatPresentationReference
```

A Warhead flag describes capability/configuration, not an active runtime status.

## 3. Stock armor profile candidate

Strong community and Ares extension documentation identifies the RA2/YR ordered 11-entry profile:

```text
0 none
1 flak
2 plate
3 light
4 medium
5 heavy
6 wood
7 steel
8 concrete
9 special_1
10 special_2
```

This is `CommunityDocumented` plus extension documentation evidence, not official runtime source.

## 4. Positional Verses

Preserve:

```text
ArmorIdentityRaw
ArmorProfileId
VersesTokenRaw
TokenOrdinal
NumericCandidate
PercentageCandidate
ForceFireCandidate
RetaliateCandidate
PassiveAcquireCandidate
ProductProfile
EvidenceGrade
Diagnostics
```

Cases retained:

- missing tokens;
- extra tokens;
- empty tokens;
- invalid text;
- percentages and decimal forms;
- negative values;
- values above 100%;
- duplicate armor;
- map-local override;
- trailing flags or extension syntax;
- positional/named profile conflict.

Forbidden:

- fill missing with 100%;
- discard extra entries;
- map unknown armor to `light`;
- clamp percentage;
- mix positional and named armor silently.

## 5. Targeting side effects

RA2/YR community documentation and Ares bug-fix/extension documentation associate special Verses values with force-fire, retaliation and passive acquisition. The exact stock behavior is not promoted to official runtime fact.

Store damage and targeting dimensions independently:

```text
DamageMultiplierCandidate
ForceFireCandidate
RetaliateCandidate
PassiveAcquireCandidate
```

A multiplier of zero does not by itself answer whether an area warhead, scripted detonation or non-damage effect can apply.

## 6. Named armor extensions

Ares defines `[ArmorTypes]` and named `Versus.<armor>` entries; Vinifera defines its own armor registry/default model; Phobos adds shield/projectile interactions. These are separate extension profiles.

Required extension metadata:

```text
ExtensionProvider
ExtensionVersion
BaseArmorCandidate
DefaultModifierCandidate
CasePolicy
CycleDiagnostic
NamedVersesEntries
TargetPermissionOverrides
```

## 7. Armor snapshot

Damage resolution consumes a runtime `TargetArmorSnapshot`, which may include:

- base authored armor;
- current transform/deploy state;
- active shield armor;
- extension override;
- hit-shape-specific armor;
- invulnerability/status;
- product profile.

The parser never selects current armor.

## 8. Warhead effects versus damage

Separate:

```text
CanApplyWarhead
VersesAllowsDamage
DamageAfterVerses
EffectsRequireVersesCandidate
EffectsRequirePositiveDamageCandidate
```

Ares and Phobos explicitly expose extensions where effects may be gated independently from conventional damage. This confirms the architectural need for separation but not stock defaults.


## Evidence grades

- `ConfirmedByOfficialRuntimeSource`
- `ConfirmedByOfficialEditorSource`
- `ConfirmedByIndependentImplementation`
- `CommunityDocumented`
- `ObservedByFutureProjectBaselineAudit`
- `ConfiguredForProjectPolicy`
- `Unresolved`

没有完整公开的 RA2/YR 原版战斗运行时源码。官方 FinalSun/FinalAlert 2 只能提供编辑器、字段目录和 authoring 行为证据，不能替代 `gamemd.exe` 运行时证据。
