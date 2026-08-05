# Locomotor, MovementZone and SpeedType

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明实现仅作行为、格式事实与冲突参考，未复制、翻译、逐句改写或机械移植代码、寻路算法、Locomotor 实现或测试夹具。`code_imported: false`。


## 1. Three logically separate inputs

```text
SpeedTypeRaw
≠ MovementZoneRaw
≠ LocomotorReferenceRaw
```

- `SpeedType`：地形速度表索引和可移动terrain候选；
- `MovementZone`：路径域、特殊可达规则、crusher/destroyer等策略候选；
- `Locomotor`：运动机制实现引用/家族候选。

三者可能相互约束，但不能合并为单一枚举。此处“separate”描述逻辑输入层，不主张来源谱系独立。

## 2. Raw token contract

每个属性保留：

```text
RawToken
OriginalCase
TrimmedCandidate
CaseFoldedLookupCandidate
SourceSectionAndKey
ResolvedCandidate?
ExtensionSource?
UnknownState
FallbackClaim?
EvidenceGrade
Diagnostics
```

禁止在parser阶段canonicalize或覆写原token。

## 3. MovementZone candidates

ModEnc社区资料记录的候选token包括：

```text
None
Normal
Crusher
Destroyer
AmphibiousDestroyer
AmphibiousCrusher
Amphibious
Subterrannean
Infantry
InfantryDestroyer
Fly
Water
WaterBeach
CrusherAll
```

注意：

- `Subterrannean` 是社区资料中的拼写，不能擅自改成 `Subterranean` 后覆盖raw；
- 不保证该列表完整；
- 不保证每项适用于TS、RA2和YR全部产品；
- extension值必须标其来源；
- ordinal不能从文档顺序推断。

推荐 `MovementZoneRaw`：

```text
RawToken
KnownNameCandidates
ProductApplicability
FamilyApplicability
CrusherPolicyCandidate
Water/ShoreDomainCandidate
PathRestrictionCandidate
RuntimeSpecialCases
EvidenceGrade
```

### 3.1 Unknown value

默认：

```text
UnknownMovementZone != Normal
```

输出unknown和diagnostic。fallback仅在显式兼容profile中执行，并保留raw token和fallback provenance。

### 3.2 Crusher/Destroyer

名称暗示的行为不足以形成完整规则。需要独立输入：

```text
CanCrush
CrushClasses
CanDestroy
DestroyableClasses
OmniCrusherCandidate
ActionRequired
PathPlanningPolicy
```

`Crusher`不等于Locomotor，也不等于“所有blocker都可穿越”。

## 4. SpeedType candidates

社区资料候选：

```text
Foot
Track
Wheel
Float
Winged
Hover
Amphibious
Creep
FloatBeach
```

产品/extension边界：

- `Creep`常被记录为TS相关；
- `FloatBeach`被记录为RA2新增候选；
- `Winged`的terrain table行为存在特殊社区描述；
- 上述均非官方runtime源码证明。

推荐 `SpeedTypeRaw`：

```text
RawToken
ProductProfile
TerrainSpeedTableKeyCandidate
RoadBonusCandidate
Water/ShoreCandidate
DefaultSourceCandidate
EvidenceGrade
UnknownState
```

### 4.1 What SpeedType does not mean

```text
SpeedTypeRaw != BaseSpeed
SpeedTypeRaw != CurrentVelocity
SpeedTypeRaw != Locomotor
SpeedTypeRaw != MovementZone
SpeedTypeRaw != FinalMovementCost
```

最终cost还需 terrain table、能力、edge、occupancy和dynamic state。

### 4.2 Zero and missing percentages

分别处理：

- explicit 0；
- missing key；
- invalid numeric；
- negative；
- >100；
- extension fixed-point；
- road bonus；
- per-unit override。

不允许自动把missing补100%。

## 5. Locomotor reference candidates

Rules中的 `Locomotor`通常是opaque CLSID-like token。社区资料列出若干家族/别名：

```text
Walk / foot
Drive / wheeled or tracked
Hover
Ship
Amphibious
Fly / aircraft
Jumpjet
Tunnel / subterranean
Teleport
Rocket
Mech
DropPod
Levitate
CustomExtension
```

这些是binding candidate，不是Core算法。

推荐：

```text
LocomotorReferenceRaw
- RawToken
- NormalizedGuidCandidate?
- AliasCandidates
- ProductProfile
- ExtensionProvider
- KnownFamilyCandidates
- EvidenceGrade
```

禁止：

- 在Core实例化COM；
- 按CLSID名称执行算法；
- 复制原版或GPL locomotor实现；
- invalid CLSID静默改成teleport/ground；
- 根据unit family强行改写Locomotor。

## 6. Capability model

`LocomotorCapabilityCandidate`只陈述能力，不包含运动算法：

```text
UsesGroundGraph
UsesWaterGraph
UsesBridgeDeck
UsesUnderBridge
UsesAirLayer
UsesSubterraneanLayer
CanEnterShore
CanClimbRamp
CanCrossCliff
CanCrush
CanDestroyBlocker
IgnoresGroundOccupancy
RequiresLandingCell
RequiresContinuousPath
SharesCellCandidate
SupportsSubCells
SupportsSpecialEdges
```

每个字段包含：

```text
ValueCandidate
Source
Policy
EvidenceGrade
ConflictSet
```

## 7. Family comparison

| Family | Candidate domains | Main unresolved boundary |
|---|---|---|
| foot/infantry | ground, ramp, selected shore | subcell, infantry-only zone |
| wheeled | ground/road/ramp | rough/cliff restrictions |
| tracked | ground/rough/ramp | crusher interaction |
| hover | ground/water candidates | shore and bridge behavior |
| ship | water | beach/landing/bridge-underwater |
| amphibious | ground+water+shore | transition/path consistency |
| aircraft | air + landing nodes | docking and landing restrictions |
| jumpjet | air/elevated + ground | takeoff/landing graph |
| subterranean | underground + entry/exit | stock YR evidence |
| teleport | special transition | target-cell validation |
| rocket/special air | scripted/special | continuous graph applicability |
| extension | profile-specific | never treated as vanilla |

## 8. Cross-property conflicts

典型冲突：

- SpeedType允许water，但MovementZone不允许；
- MovementZone规划shore，但SpeedType速度为0；
- Locomotor使用air layer，但MovementZone为Normal；
- crusher zone允许规划穿过对象，但unit没有相应crush capability；
- water-bound unit没有shore transition；
- aircraft placement有cell但没有landing capability。

输出：

```text
MovementBindingConflict
- PropertySet
- CandidateCapabilities
- Contradictions
- Severity
- Evidence
```

不自动修复。

## 9. Defaults

社区文档可能描述默认值推导，例如按type family、Crusher或Locomotor选择SpeedType/MovementZone。这些只能作为显式compatibility profile：

```text
DefaultingPolicy = None | CommunityCompatible | EditorCompatible | ProjectStrict
```

严格模式下missing就是missing。

## 10. Case and spelling

lookup可配置为case-insensitive，但roundtrip必须保留original case。unknown、typo和extension token不应因大小写处理而消失。

## 11. Runtime special cases

社区讨论表明部分行为可能硬绑定MovementZone，例如docking、beach或path selection。由于缺少完整original-runtime source：

- 长期稳定社区约定使用`ConfirmedCommunityConvention`；
- runtime适用范围不足的具体行为保持`Underconfirmed`；
- 不推导通用算法；
- 不从TS无条件迁移到YR；
- 不从Ares/Phobos无条件迁移到vanilla。

## 12. Recommended result

```text
MovementCapabilityProfile
- SpeedTypeRaw
- MovementZoneRaw
- LocomotorReferenceRaw
- CapabilityCandidates
- TerrainTableCandidates
- CrushDestroyCandidates
- LayerAccessCandidates
- TransitionCandidates
- Conflicts
- EvidenceGrades
- Diagnostics
```

## 13. Evidence grades

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| MovementZone、SpeedType和Locomotor token的长期社区列表 | `ConfirmedCommunityConvention` | ModEnc/community fixed pages | 约定不证明完整性、ordinal或全部产品适用性。 | 原token与product profile保留。 | `NotRun` |
| 单个公开目标引擎分离terrain speed、capability和occupancy | `ImplementationSpecificBehavior` | Named target engines | 单一实现行为。 | 仅作比较profile。 | `NotRun` |
| 多工具/社区对terrain speed与movement domain分离的共同趋势 | `Underconfirmed` | Public tools/community | 谱系独立性和stock runtime适用性不足。 | 不按来源数量提升。 | `NotRun` |
| 来源对WaterBeach/FloatBeach、crusher、defaults和product applicability的说明 | `ConflictingSources` | Community and extension sources | 直接存在版本和语义差异。 | 保留candidate/conflict set。 | `NotRun` |
| exact stock runtime CLSID、fallback、path和execution行为 | `Unresolved` | No original-runtime source located | 无可靠完整候选。 | future simulation adapter负责。 | `NotRun` |
| raw token、explicit capability profile、no default repair和fail-closed binding | `DefensiveDesign` | Project policy | 项目保真与安全策略。 | missing/unknown不自动Normal或100%。 | `NotRun` |
