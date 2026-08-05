# Bridges, aircraft and elevated layers

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明的实现仅作行为与冲突参考，未复制、翻译或机械移植其代码、公式实现或测试夹具。`code_imported: false`。


## 1. screen Y 不能表达拓扑

同一个 projected region可能同时存在：

- 地面/水面；
- bridge underlay/pillar；
- 桥下单位；
- bridge deck；
- 桥上单位；
- aircraft；
- aircraft ground shadow。

仅用 screen Y会在桥上/桥下、shadow receiver和aircraft间产生不可解歧义。

## 2. 显式 layer 候选

```text
ElevationLayer
- GroundLayer
- UnderBridgeLayer
- BridgeDeckLayer
- AirLayer
```

可扩展但不在本轮实现 simulation。layer ID 是 presentation/simulation binding输入，不是 Unity SortingLayer。

## 3. Bridge descriptor

```text
BridgePresentationDescriptor
- BridgeStableId
- AuthoredPieces
- LowOverlayPieces
- TmpArtBindings
- UnderlayEntities
- PillarEntities
- DeckEntities
- DamageStateView
- DestroyedStateView
- DeckElevationCandidate
- ElevationLayerBindings
- ShadowDescriptorRefs
- OccupancyReferenceOnly
- PathfindingReferenceOnly
- Diagnostics
```

## 4. 必须分开的桥梁内容

| 内容 | 来源/责任 |
|---|---|
| low bridge overlay pieces | Overlay storage/registry |
| high bridge pieces | bridge semantic binder |
| TMP bridge art | theater/TMP binder |
| bridge deck | presentation + simulation layer binding |
| underlay/pillar | visual entities |
| shadow | shadow policy |
| damage/destroyed | runtime/map state |
| unit on bridge | simulation layer snapshot |
| unit under bridge | simulation layer snapshot |
| aircraft above bridge | AirLayer |
| high-cell occupancy | simulation/pathfinding |
| renderer layer | presentation |
| pathfinding layer | pathfinding |

Overlay piece或TMP art单独都不足以建立完整bridge state。

## 5. 公开实现证据

OpenRA固定revision提供`ElevatedBridgeLayer` custom movement layer，显式保存桥面cell center、terrain index与入口/出口。这证明该命名目标引擎采用显式层模型，但不证明原版RA2/YR runtime内部使用相同结构。

EA editor对big bridge overlay有特殊分类，确认官方编辑器不会把所有桥梁输入都视为普通排序对象；仍不确认runtime拓扑、damage transition或pathfinding算法。

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| OpenRA使用`ElevatedBridgeLayer`显式描述桥面层 | `ImplementationSpecificBehavior` | OpenRA | 单一目标引擎实现。 | 仅作分层架构比较。 | `NotRun` |
| FinalAlert对部分big bridge overlay进行特殊分类 | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | 官方编辑器行为，不是runtime simulation证据。 | 保留source-pinned桥梁分类输入。 | `NotRun` |
| 原版runtime使用唯一已知的桥上/桥下/deck排序与拓扑模型 | `Underconfirmed` | 工具、社区和目标引擎资料 | 缺少原版runtime source，工具模型也可能存在共享知识传播。 | 使用显式命名layer和profile。 | `NotRun` |
| Ground/UnderBridge/BridgeDeck/Air显式层与禁止screen-Y猜测 | `DefensiveDesign` | Project policy | 项目稳定排序与fail-closed边界。 | 不由视觉位置推导simulation/pathfinding状态。 | `NotRun` |

## 6. Damage与destroyed

damage state可改变：

- selected art；
- deck可见性；
- shadow；
- culling bounds；
- pass/occlusion descriptor；
- simulation/pathfinding layer availability。

不得由“图片缺失”“depth plane为空”或screen Y推断destroyed。presentation只消费 typed state view。

## 7. Unit on/under bridge

排序 tuple至少包含：

```text
RenderPass
ElevationLayer
LayerLocalProjectedY
FamilyPriority
StableSourceOrdinal
StableIdentity
```

桥上unit的ground reference是deck surface；桥下unit是Ground/UnderBridge surface。二者可共享 authored X/Y，但layer不同。

## 8. Aircraft placement

地图 `[Aircraft]` placement提供：

- initial/authored cell；
- owner/type/strength/facing/action等scenario字段；
- stable source ordinal。

它不提供完整runtime altitude、takeoff、landing、hover、bank/pitch、velocity或shadow separation。

## 9. Aircraft descriptor

```text
AircraftPresentationDescriptor
- StableIdentity
- AuthoredGroundReference
- RuntimePositionSnapshot?
- VisualAltitude
- SimulationAltitudeReference?
- FlightState
- LandingState
- Facing/Pose
- AssetParts
- AirLayer
- ShadowDescriptor
- SelectionAnchor
- FogVisibilityPolicy
- Diagnostics
```

visual altitude可用于screen offset，但不能替代simulation height。

## 10. Takeoff、landing与hover

状态切换可能改变：

- ground/air pass；
- elevation layer；
- shadow offset/opacity；
- selection anchor；
- click bounds；
- rotor/animation attachments；
- fog/shroud policy。

切换由simulation snapshot提供，不由renderer根据frame名称猜测。

## 11. Aircraft shadow

shadow锚定 ground/deck receiver：

- aircraft在桥上空不自动意味着shadow落在桥面；
- receiver选择需要 explicit policy/scene query；
- shadow screen位置与caster altitude相关；
- shadow bounds独立cull；
- shadow不参与aircraft occupancy。

## 12. Projectile altitude

projectile需显式 runtime position/altitude与effect pass。target line、aim indicator属于UI；不得把target line bounds加入world culling或depth。

## 13. Fog/shroud与selection

aircraft world visibility、ground shadow visibility、selection bracket、target line可有不同policy。选择框可以在UI pass显示，但不能泄露被shroud隐藏的entity，具体由UI/fog adapter决定。

## 14. Bridge + aircraft tie

候选顺序：

- underlay/pillar按UnderBridge/Ground；
- deck按BridgeDeck；
- bridge上ground actors按BridgeDeck；
- aircraft按AirLayer；
- shadow按receiver layer的ShadowPass；
- UI最后独立。

不是简单“aircraft永远最后”：fog/shroud、effects和UI仍是独立pass。

## 15. Diagnostics

- `BridgePiecesIncomplete`
- `BridgeDeckLayerMissing`
- `UnderBridgeLayerMissing`
- `BridgeStateUnresolved`
- `BridgeVisualUsedAsPathfindingRejected`
- `AircraftRuntimeAltitudeMissing`
- `AircraftPlacementMisreadAsAltitude`
- `AircraftShadowReceiverUnresolved`
- `LayerLocalDepthCollision`
- `AirLayerUsedAsSimulationHeightRejected`

## 16. 项目决定

显式保留`ElevationLayer`/`BridgeDeckLayer`/`UnderBridgeLayer`/`AirLayer`候选；Core只描述，不实现bridge/pathfinding/aircraft simulation。该决定为`DefensiveDesign`。
