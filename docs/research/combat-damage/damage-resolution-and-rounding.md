# Damage resolution and rounding

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明的实现仅作行为、接口与冲突参考，未复制、翻译或机械移植其代码、公式实现、switch 表或测试夹具。`code_imported: false`。


## 1. Candidate resolution stages

```text
BaseDamage
→ weapon/type modifiers
→ attacker firepower/veterancy candidate
→ target eligibility
→ armor/Verses candidate
→ distance or CellSpread falloff
→ prone/building/special modifiers
→ deterministic rounding
→ applied health or special-effect command
→ statistics/presentation events
```

This is a research decomposition, not a frozen RA2/YR runtime order.

## 2. Inputs

`DamageResolutionDescriptor` candidate inputs:

```text
StableImpactIdentity
StableSourceActorIdentity
WeaponTypeId
ProjectileTypeId?
WarheadTypeId
AuthoredBaseDamage
DamageModifierCandidates
TargetSnapshot
TargetArmorSnapshot
ImpactPosition
ImpactLayer
DistanceCandidate
AreaFalloffCandidate
FriendlyFireCandidate
SpecialEffectCandidates
RoundingPolicy
ProductProfile
```

## 3. Arithmetic representation

Candidate policies:

- checked signed integer;
- checked 64-bit intermediate with bounded final integer;
- deterministic fixed-point;
- explicitly versioned percentage scale;
- explicit operation order.

Floating-point may be retained as raw/configuration candidate but should not become authoritative simulation arithmetic without deterministic proof.

## 4. Rounding candidates

```text
TowardZero
Floor
Ceiling
NearestHalfAwayFromZero
NearestHalfToEven
PerStageIntegerTruncation
SingleFinalRounding
ProductSpecificUnknown
```

Tests must distinguish negative healing values and positive damage.

## 5. Required edge cases

- base damage zero;
- negative damage/healing;
- minimum damage candidate;
- 0%/1%/2% Verses side effects;
- negative Verses;
- above-100% Verses;
- multiple modifiers;
- intermediate overflow;
- final overflow;
- exact divisibility;
- fractional result;
- overkill;
- healing cap;
- invulnerability;
- target already dead;
- attacker dies same tick;
- target changes armor before impact;
- simultaneous impacts.

## 6. Mutation contract

Damage evaluation outputs commands rather than directly modifying objects:

```text
HealthDamageCommand
HealingCommand
SpecialEffectCommand[]
Terrain/Wall/Bridge/ResourceCommand[]
DeathEvaluationCandidate
StatisticsEventCandidate
PresentationEventCandidate
Diagnostics
```

Mutation order is governed by a future `DamageOrderingPolicy`.

## 7. Simultaneous impact ordering

Candidate stable tuple:

```text
SimulationTick
CommandSequence
SourceActorStableId
WeaponUsageStableId
ProjectileStableId
BurstOrdinal
ShotOrdinal
ImpactOrdinal
TargetStableId
EffectOrdinal
```

No dictionary iteration, Unity instance ID or render ordering.

## 8. Negative damage

Negative values remain healing or effect-direction candidates. Questions kept unresolved:

- whether all Warhead paths accept healing;
- Verses sign interaction;
- area healing;
- allied/owner filtering;
- health cap order;
- statistics/scoring;
- death reversal prohibition.

## 9. OpenRA comparison

OpenRA applies ordered percentage modifiers to weapon damage and armor values in its own model. This demonstrates an explicit pipeline and checked model requirement, but its exact formula/order cannot be called Westwood runtime behavior.

## 10. Roundtrip

Lossless writer preserves original numeric spelling such as:

```text
100
+100
-25
1.0
100%
0.5
.5
1e2
empty
invalid
```

No canonicalization occurs by default.


## Evidence grades

- `ConfirmedByOfficialRuntimeSource`
- `ConfirmedByOfficialEditorSource`
- `ConfirmedByIndependentImplementation`
- `CommunityDocumented`
- `ObservedByFutureProjectBaselineAudit`
- `ConfiguredForProjectPolicy`
- `Unresolved`

没有完整公开的 RA2/YR 原版战斗运行时源码。官方 FinalSun/FinalAlert 2 只能提供编辑器、字段目录和 authoring 行为证据，不能替代 `gamemd.exe` 运行时证据。
