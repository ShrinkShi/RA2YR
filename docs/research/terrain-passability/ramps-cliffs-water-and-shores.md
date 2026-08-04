# Ramps, cliffs, water and shores

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明实现仅作行为、格式事实与冲突参考，未复制、翻译、逐句改写或机械移植代码、寻路算法、Locomotor 实现或测试夹具。`code_imported: false`。


## 1. Surface transition, not tile label

一个movement edge由两个node和transition inputs构成：

```text
source surface
+ target surface
+ Level delta
+ ramp/corner candidates
+ TileSet roles
+ movement capability
+ overlay/bridge state
→ Ramp/Shore/Cliff transition candidate
```

只看任一字段均不足。

## 2. Ramp inputs

```text
SourceCellIdentity
TargetCellIdentity
SourceLevelRaw
TargetLevelRaw
SourceRampTypeRaw
TargetRampTypeRaw
SurfaceCornerCandidates
Direction
RequiredCapability
EvidenceGrade
Diagnostics
```

### 2.1 Level delta

`LevelRaw`差值是重要输入，但不是cliff真值：

- 同Level可能存在visual cliff或blocked role；
- 不同Level可能由合法ramp连接；
- malformed maps可能有跳变；
- bridge deck elevation不等于ground Level；
- underground/air layer不服从ground delta。

### 2.2 RampTypeRaw

`RampTypeRaw`必须结合方向和corner profile。不能只要非零就passable。

### 2.3 Bidirectionality

默认不假定双向。候选可能因为：

- ramp orientation；
- bridge entrance；
- tunnel direction；
- malformed reciprocal data；
- runtime restriction

形成one-way或conflicting edge。

## 3. Malformed ramps

分别诊断：

```text
RampToMissingCell
RampLevelMismatch
RampDirectionConflict
RampRoleConflict
UnsupportedRampExtension
DiscontinuousLevelWithoutTransition
DuplicateRampCandidate
```

不自动修改Level，不自动替换TMP，不生成平滑坡。

## 4. Diagonal and corner transitions

isometric diamond neighbor不应直接等同于屏幕上下左右。`AdjacencyPolicy`需声明：

- canonical neighbor directions；
- diagonal候选；
- corner-cut规则；
- parity/domain映射；
- ramp edge方向；
- edge ordering。

若支持diagonal，需要额外检查两个正交边或corner surface，而不是仅按欧氏距离。

## 5. Cliff distinctions

```text
VisualCliffExtra
TileSetCliffRole
LevelDiscontinuity
MovementCliffCandidate
ProjectileLosCandidate
RendererOccluder
```

这些是不同事实。

extra graphics不能自动阻塞；renderer orientation不能成为movement truth；cliff destruction属于future dynamic policy。

## 6. Water model

严格分开：

```text
VisualWater
WaterSurfaceCandidate
NavalMovementDomain
AmphibiousTransition
ShoreTransition
ResourceOrOverlay
SimulationWaterState
```

水域不能根据palette或蓝色像素识别。

候选来源：

- theater Water/shore TileSet role；
- TMP TerrainTypeRaw；
- Rules land type；
- Overlay特殊类型；
- map/runtime water state；
- extension profile。

## 7. Shore model

`ShoreTransitionDescriptor`候选：

```text
SourceNode
TargetNode
WaterSideCandidate
LandSideCandidate
ShoreRoleCandidate
Direction
RequiredCapabilities
SpeedTypeCompatibility
MovementZoneCompatibility
LocomotorCompatibility
EntryCostCandidate
ExitCostCandidate
EvidenceGrade
```

`CanEnterShore`需与terrain速度和path domain一致。

## 8. Amphibious units

amphibious不是单一bool。至少分：

```text
UsesGroundGraph
UsesWaterGraph
HasShoreTransitions
CanRemainOnWater
CanRemainOnLand
CanTargetLandFromWater
CanDockAtShore
RequiresBeachCell
```

社区记录显示landing craft/WaterBeach存在特殊path行为和版本差异，因此必须保留profile和P0 unresolved。

## 9. Ships

ship通常只消费water domain候选，但不能从family名推导：

- 是否可进入shore；
- 是否允许浅水；
- 是否可在bridge下；
- 是否需要naval yard/dock；
- 是否能在ice状态移动；
- destroyed bridge后water恢复。

## 10. Hover

Hover的water/land能力应由SpeedType、MovementZone和Locomotor共同绑定。不能因为视觉“悬浮”就忽略cliff、foundation或bridge topology。

## 11. Foot, wheel and track

不同SpeedType可对Clear/Rough/Road/Water等有不同percentage。是否可走需先判断entry存在和value，再叠加MovementZone/Locomotor。

road bonus是cost/speed modifier，不是新坐标域。

## 12. Ice and shallow water

`Ice`、shallow water、beach、water-to-ice destruction都可能包含dynamic state。默认模型：

```text
BaseSurface
RuntimeSurfaceModifier
TransitionState
MovementDomainCandidate
```

不得用TMP final byte、visual crack或palette直接改变surface，除非有显式证据profile。

## 13. Bridge over water

ground water cell、under-bridge water node和bridge deck node必须并存。ship通过桥下、ground unit在桥上和aircraft飞越是不同layer query。

## 14. Cost candidate

transition cost不是单个terrain百分比：

```text
SurfaceCost
DirectionCost
ElevationCost
RampCost
ShoreTransitionCost
RoadModifier
CapabilityActionCost
DynamicBlockerCost
```

顺序和数值表示由 `TerrainCostPolicy` / `MovementDeterminismPolicy`显式定义。

## 15. Evidence

- EA editor的slope工具和raw字段：`ConfirmedByOfficialEditorSource`，不等于runtime path规则。
- OpenRA ramp/height transition：`ConfirmedByIndependentImplementation`。
- ModEnc land/SpeedType与PPM shore/bridge讨论：`CommunityDocumented`。
- exact vanilla YR ramp、shore和cliff edge算法：`Unresolved`。
- descriptor separation：`ConfiguredForProjectPolicy`。
