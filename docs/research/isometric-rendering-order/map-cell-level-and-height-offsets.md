# Map cell Level, TMP height and ramp offsets

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明的实现仅作行为与冲突参考，未复制、翻译或机械移植其代码、公式实现或测试夹具。`code_imported: false`。


## 1. 必须分开的高度概念

| 概念 | 来源 | 单位/域 | 表现用途 | 不得推导 |
|---|---|---|---|---|
| IsoMap `Level` | map cell | map height step | cell base screen-Y offset | TMP depth bytes |
| TMP `HeightRaw` | TMP cell header | raw byte | asset-local metadata candidate | global Level |
| TMP `RampTypeRaw` | TMP cell header | raw byte | ramp semantic lookup candidate | automatic object movement |
| TMP depth pixels | TMP plane | byte per visible pixel | renderer occlusion/depth candidate | simulation elevation |
| object visual Z offset | Rules/Art/presentation | draw-order/logical pixel | attachment/layer adjustment | occupancy |
| bridge elevated state | bridge binder/runtime | explicit layer | deck/under ordering | map Level rewrite |
| aircraft altitude | runtime snapshot | simulation/world unit | visual offset/shadow relation | authored Aircraft placement |
| projectile altitude | runtime snapshot | simulation/world unit | trajectory/effect projection | cell terrain |
| terrain passability | typed rules/terrain | simulation semantic | none directly | render pass |
| simulation elevation | runtime | simulation unit | presentation snapshot input | raw map mutation |

## 2. Level 到 screen Y

官方编辑器候选：

```text
screenY = (mapX + mapY - Level) * tileHeight / 2
```

因此：

- TS editor: 每 Level `12` logical pixels；
- RA2 editor: 每 Level `15` logical pixels。

```text
EvidenceGrade: ConfirmedByOfficialToolSource
Source: EA FinalSun / FinalAlert 2
AuditStatus: NotRun
```

该等级只确认官方编辑器的Level投影候选。原版runtime是否在所有对象族、inverse picking、cliff clipping和桥梁层中使用相同单位及舍入仍为`Unresolved`。

`HeightOffsetPolicy` 至少包含：

```text
LevelStepNumerator
LevelStepDenominator
PositiveLevelMovesScreenUp
BaseLevel
CheckedRange
RoundingMode
AppliesToCellAnchor
AppliesToObjectGroundAnchor
```

## 3. TMP HeightRaw

公开 TMP reader 一致表明该字段存在，但对 signedness、运行时用途与 ramp 的关系并不完整一致。Core 应保存：

- `HeightRawByte`
- optional signed/unsigned views
- source cell/subtile ordinal
- evidence grade
- interpretation profile ID

不得：

- `CellLevel += HeightRaw`；
- 用 HeightRaw 调整全局 map origin；
- 因 HeightRaw 与 Level 不一致而修改 raw map；
- 把它作为 passability。

该字段存在属于工具/reader证据；其唯一runtime语义为`Underconfirmed`或`Unresolved`，不能由工具数量提升。

## 4. RampTypeRaw

ramp 是 cell surface/corner geometry 的候选，而不是另一个简单 Level。公开实现提供 0..20 ramp 枚举与角点解释，但来源存在 TS++/社区谱系。

建议：

```text
RampDescriptor
- RampTypeRaw
- InterpretationProfileId
- CornerHeightCandidates
- CenterContactOffsetCandidate
- PoseOrientationCandidate
- Evidence
- Diagnostics
```

`RampTypeRaw` 未知值可保留；typed interpretation 失败不使 TMP raw parse 失败。

## 5. Ramp 是否改变 anchor

分层决定：

- `LogicalGroundAnchor`: 仍属于 map cell/subcell；
- `SurfaceContactPoint`: 可由 ramp + local offset 派生；
- `RenderPivot`: asset/binder；
- `SimulationPosition`: simulation；
- `VisualPose`: future adapter 的 pitch/roll/yaw。

默认项目 policy：

- ramp 不改 authored cell identity；
- 可改变 ground contact screen offset；
- 可提供 vehicle pitch/roll candidate；
- infantry foot point可吸附 surface contact；
- building 是否允许坡面放置由 simulation/rules决定；
- renderer 不能借由视觉 ramp 修改 occupancy。

以上为`DefensiveDesign`，不是runtime事实。

## 6. Building on slope

需显式处理四种结果，而不是让 renderer 猜：

1. placement 被 simulation 拒绝；
2. building 采用 cell/foundation reference Level；
3. custom policy 允许并选择 foundation surface；
4. map/runtime 已提供合法 pose。

presentation binder只消费已选结果。不得从 building SHP 底边“拟合”坡面。

## 7. Vehicle pose

VXL/HVA future renderer 可能消费：

- ground contact normal；
- facing；
- pitch/roll candidate；
- suspension/impact offset；
- body/turret/barrel transforms。

这些是 presentation/simulation adapter 输入，不属于 TMP reader。OpenRA 的 ramp corner interpolation是`ImplementationSpecificBehavior`，不是vanilla算法证明。

## 8. Infantry foot point与subcell

同 cell 多 Infantry 需要：

- scenario subcell raw；
- selected subcell profile；
- logical ground anchor；
- ramp surface contact；
- SHP frame offset；
- stable tie ordinal。

subcell offset不应被 image center覆盖；ramp 接触点不应改变 scenario placement。

## 9. Shadow落点

shadow 接收：

```text
GroundReferenceAnchor
SurfaceContactCandidate
CasterVisualAltitude
ShadowProjectionProfile
```

aircraft shadow优先 ground reference，而非 aircraft screen anchor。ramp 是否改变投影形状与落点由 profile控制，不由 raw shadow帧决定。

## 10. Bridge、water、shore 与 cliff

- high bridge deck有独立 elevation layer；
- under-bridge entity仍可具有相近 screen Y；
- water/shore height语义来自 map/theater/simulation binding，不由palette或depth plane推断；
- cliff extra graphics扩展视觉 bounds，可参与 occlusion，但不修改邻接 cell Level；
- destroyed bridge state不能由缺图或 screen Y反推。

## 11. Diagnostics

- `TmpHeightInterpretationUnknown`
- `RampTypeUnknown`
- `LevelOutsideConfiguredRange`
- `LevelProjectionOverflow`
- `RampContactOutsideCell`
- `BuildingSlopePolicyRequired`
- `BridgeElevationLayerMissing`
- `AircraftAltitudeSourceMissing`
- `VisualOffsetUsedAsSimulationHeight`
- `DepthPixelUsedAsLevel`

## 12. Normalized evidence and policy

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert按half tile height投影Level | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | 只确认editor profile。 | 保存为命名height profile。 | `NotRun` |
| 多个工具/社区资料采用相近Level与ramp解释 | `Underconfirmed` | public tools、TS++/community material | 谱系独立性、signedness和runtime适用性不足。 | 不合并raw Level、HeightRaw和RampTypeRaw。 | `NotRun` |
| OpenRA ramp corner interpolation | `ImplementationSpecificBehavior` | OpenRA | 单一目标引擎算法。 | 仅作comparison profile。 | `NotRun` |
| 原版runtime的TMP HeightRaw、RampType和object contact统一算法 | `Unresolved` | 未找到原版runtime source | 当前没有可靠唯一候选。 | adapter-owned，未知值raw保留。 | `NotRun` |
| `15 logical pixels`初始配置、raw字段分离、禁止视觉反推simulation | `DefensiveDesign` | Project policy | 项目保真和架构边界。 | 显式profile与checked arithmetic。 | `NotRun` |
