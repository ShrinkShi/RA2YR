# Implementation boundaries and Core contracts

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明的实现仅作行为、接口与冲突参考，未复制、翻译或机械移植其代码、公式实现、switch 表或测试夹具。`code_imported: false`。


## 1. Explicit non-implementation

No combat parser, typed runtime binder, firing, projectile, collision, target selection, damage resolver, armor runtime, status effect, death, ammo/reload, AI, renderer, audio or Unity object is implemented.

## 2. Core dependency rule

Core:

- has no `UnityEngine` reference;
- stores no GameObject/Transform/Collider/Rigidbody/ParticleSystem/AudioSource;
- performs no Physics query;
- does not use frame time, wall clock or Unity random;
- accepts bounded Memory/Stream/short-read/MIX-window inputs;
- returns immutable raw/derived descriptors and structured diagnostics.

## 3. Recommended models

```text
CombatRulesDocument
WeaponTypeRaw
WeaponTypeDescriptor
WeaponReferenceRaw
WeaponMountDescriptor
ProjectileTypeRaw
ProjectileTypeDescriptor
ProjectileFlightProfileCandidate
WarheadTypeRaw
WarheadTypeDescriptor
ArmorIdentityRaw
ArmorProfile
VersesEntryRaw
VersesBindingResult
TargetEligibilityDescriptor
DamageExpressionCandidate
DamageResolutionDescriptor
AreaEffectDescriptor
FiringStateDescriptor
AmmoDescriptor
BurstDescriptor
SpecialEffectDescriptor
CombatPresentationReference
CombatDiagnostic
CombatReadLimits
CombatConsistencyAnalysis
CombatRoundtripDescriptor
```

Additional supporting contracts:

```text
CombatReferenceGraph
CombatProductProfile
ExtensionProviderDescriptor
TargetArmorSnapshot
ProjectileSpawnCommand
CollisionQueryDescriptor
ImpactCommandDescriptor
DamageMutationCommand
CombatRandomState
StableCombatIdentity
```

## 4. Explicit policies

```text
WeaponBindingPolicy
WeaponSlotPolicy
ProjectileBindingPolicy
ProjectileFlightPolicy
CollisionPolicy
WarheadBindingPolicy
ArmorBindingPolicy
VersesPolicy
TargetEligibilityPolicy
DamageResolutionPolicy
DamageRoundingPolicy
AreaEffectPolicy
FriendlyFirePolicy
AmmoPolicy
BurstPolicy
SpecialEffectPolicy
CombatDeterminismPolicy
DamageOrderingPolicy
CombatRandomPolicy
CombatRoundtripPolicy
```

Every policy contains:

```text
PolicyId
Version
ProductProfile
ExtensionProvider?
EvidenceGrade
SourceReferences
Strictness
UnknownValueBehavior
ArithmeticProfile
DiagnosticsBehavior
```

## 5. Checked arithmetic

Checked operations include:

- damage and multiplier products;
- percentage/fixed-point conversion;
- burst/shot counts;
- range/minimum-range conversion;
- CellSpread radius and candidate counts;
- area object enumeration;
- projectile lifetime and speed/length;
- ammo/cooldown timers;
- status durations;
- effect counts;
- source/reference string lengths.

Overflow yields a diagnostic/failure candidate, never wrap.

## 6. Read limits

`CombatReadLimits`:

```text
MaxWeaponTypes
MaxProjectileTypes
MaxWarheadTypes
MaxArmorTypes
MaxFieldsPerSection
MaxReferencesPerType
MaxWeaponSlotsPerType
MaxVersesEntries
MaxAreaCandidates
MaxEffectsPerWarhead
MaxBurstCount
MaxClusterChildren
MaxDiagnostics
MaxReferenceDepth
MaxStringLength
MaxNumericMagnitude
```

## 7. Input equivalence

Identical logical input through:

- `ReadOnlyMemory<byte>`;
- seekable Stream;
- short-read Stream;
- exact MIX entry window;

must produce identical raw records, occurrence ordinals, bindings, diagnostics, canonical aggregate hashes and consistency analysis.

## 8. Synthetic fixtures

Synthetic expected values must not call production:

- damage/Verses code;
- target eligibility;
- CellSpread/falloff;
- area enumeration;
- projectile flight;
- collision;
- RNG;
- burst/reload;
- roundtrip canonicalization.

Use hand-authored small integer cases and independent tables.

## 9. Diagnostics

`CombatDiagnostic` candidate:

```text
Code
Severity
Stage
SourceReference
TypeFamily
RawIdentity? (internal)
FieldName?
PolicyId
EvidenceGrade
NumericContext
MessageTemplateId
```

Public audit strips linkable identities and exact values.

## 10. Consistency analysis

Read-only analysis:

- dangling Weapon/Projectile/Warhead refs;
- duplicate/case-collision sections;
- range/minimum-range conflict;
- burst/ammo impossible shapes;
- projectile profile conflicts;
- armor/Verses length mismatch;
- positional/named profile collision;
- area/falloff invalid shape;
- special-effect conflicts;
- presentation-only missing resources;
- input-mode equivalence.

It never repairs.

## 11. Adapter interfaces

Future simulation adapter consumes logical descriptors and produces deterministic commands/results. Future presentation adapter consumes simulation events. Neither writes back format semantics.

## 12. Architectural acceptance

- `noEngineReferences`;
- no Unity objects;
- no runtime mutation during parse/bind;
- stable ordering;
- structured diagnostics;
- bounded input;
- serializable evidence and policy IDs;
- raw/derived separation;
- deterministic save/load/replay contracts.


## Evidence grades

- `ConfirmedByOfficialRuntimeSource`
- `ConfirmedByOfficialEditorSource`
- `ConfirmedByIndependentImplementation`
- `CommunityDocumented`
- `ObservedByFutureProjectBaselineAudit`
- `ConfiguredForProjectPolicy`
- `Unresolved`

没有完整公开的 RA2/YR 原版战斗运行时源码。官方 FinalSun/FinalAlert 2 只能提供编辑器、字段目录和 authoring 行为证据，不能替代 `gamemd.exe` 运行时证据。
