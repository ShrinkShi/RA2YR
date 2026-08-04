# M3-R12 — Terrain passability and movement-topology dossier

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明实现仅作行为、格式事实与冲突参考，未复制、翻译、逐句改写或机械移植代码、寻路算法、Locomotor 实现或测试夹具。`code_imported: false`。


## 1. 任务定位

本目录只研究 RA2/YR 地图地表拓扑、通行性、`MovementZone`、`SpeedType`、`Locomotor`、静态/动态 occupancy 与未来 movement graph 之间的声明式合同。

本轮不实现：

- passability reader；
- movement graph；
- A*、JPS、flow field、NavMesh；
- Locomotor、移动模拟、碰撞、转向、避让、预约、编队；
- bridge destruction、building placement、resource harvesting；
- Unity `Grid`、`Tilemap`、`Collider`、`NavMesh`、`GameObject`；
- C#、PowerShell、Unity 测试或配置。

## 2. 冻结候选管线

```text
raw map and resource descriptors
→ coordinate and cell identity views
→ theater tile semantic binding
→ raw surface and elevation candidates
→ overlay/object occupancy candidates
→ explicit locomotor capability profile
→ movement-domain and adjacency candidates
→ future pathfinding and movement adapters
```

每一步只能增加 derived candidate，不能覆盖 raw identity。

## 3. 核心结论

### 3.1 不存在一个足够表达全部事实的 `bool Walkable`

每个逻辑 cell 至少需要分别保存：

```text
CellIdentity
AuthoredIsoMapRecord
GlobalTileIdCandidate
SubTileRaw
LevelRaw
TmpTerrainTypeRaw
TmpRampTypeRaw
TmpHeightRaw
OverlayTypeRaw
OverlayDataRaw
TerrainObjectReferences
SmudgeReferences
StructureOccupancyCandidates
BridgeLayerCandidates
Diagnostics
```

随后再派生：

```text
SurfaceCandidate
MovementDomainCandidate
StaticOccupancyCandidate
DynamicOccupancySnapshot
MovementNode
MovementEdge
TerrainCostCandidate
PassabilityState
```

### 3.2 三个 movement 属性必须独立

```text
MovementZoneRaw ≠ SpeedTypeRaw ≠ LocomotorReferenceRaw
```

- `SpeedType`：候选 terrain-speed table key，决定地形速度/不可达的一个输入；
- `MovementZone`：候选 path-domain、crusher/destroyer、water/shore 等路径语义；
- `Locomotor`：opaque CLSID/alias 引用，绑定运动机制能力，而不是 parser 中执行算法。

三者冲突时应输出 diagnostic，不自动修复。

### 3.3 passability 是查询结果，不是格式字段

推荐状态：

```text
Passable
TraversableWithCost
Blocked
RequiresCapability
TemporarilyBlocked
DestructibleBlocker
Unknown
```

并独立保存：

```text
CanOccupy
CanEnter
CanCrush
CanDestroy
CanPassAfterDestroy
ActionRequired
CostCandidate
```

### 3.4 movement 与 buildability 分离

同一 cell 可以：

- unit可走但不可建；
- 可建但某locomotor不可走；
- 水面可供naval移动但不可放ground building；
- bridge deck可走但不可建；
- surface本身可走但被dynamic blocker占用。

### 3.5 renderer 不定义 movement

以下均不能直接决定通行：

- TMP depth plane；
- extra graphics；
- shadow；
- palette/颜色；
- selection/click bounds；
- camera/screen Y；
- aircraft ground shadow；
- SHP/VXL图片尺寸。

## 4. Coordinate and identity domains

严格区分：

```text
IsoMapRawCoordinate
DiamondCanvasCoordinate
ScenarioCellCoordinate
OverlayStorageCoordinate
TmpLocalCell
FoundationCellOffset
BridgeMovementCell
SimulationCell
PathGraphNode
UnityWorldCoordinate
```

axis/profile不能通过“转换后落在地图范围内”自动选择。

## 5. Cell surface model

推荐 `TerrainSurfaceCandidate` 保存：

```text
CellIdentity
RawCellReference
GlobalTileIdCandidate
SubTileRaw
LevelRaw
TmpTerrainTypeRaw?
TmpRampTypeRaw?
TmpHeightRaw?
TileSetRoleCandidates
LandTypeBindingCandidates
OverlaySurfaceModifiers
ElevationCandidates
UnknownInputs
EvidenceGrades
Diagnostics
```

不把 `HeightRaw` 当 `LevelRaw`，不把 `RampTypeRaw` 当 passable，不把extra/depth当movement truth。

## 6. Terrain and theater roles

候选角色包括：

```text
Clear / Rough / Sand / Road / Pave / Water / Shore
Cliff / Ramp / Bridge / Tunnel / Ice / Rail
LatTransition / CustomExtension / Unknown
```

需要严格区分：

```text
TileSet logical role
TMP TerrainTypeRaw
Rules land type
SpeedType
MovementZone
Locomotor capability
Final movement state/cost
Visual appearance
```

文件名启发式只能是低等级显式profile。

## 7. Overlay and object passability

Overlay按family解释：resource、wall、gate、bridge、rock/debris、crate、veins、track、tunnel、unknown。

禁止：

- 所有非空Overlay都阻塞；
- 所有resource都可通行；
- 只凭OverlayData判bridge完整性；
- missing Art删除blocker；
- missing OverlayData合成默认状态。

Terrain object必须经Rules/type policy绑定；Smudge默认是visual/decal，不是blocker。

## 8. Foundations and occupancy

必须区分：

```text
AuthoredFoundation
FoundationOrigin
SimulationOccupancy
MovementBlockingMask
BuildabilityMask
Bib
ExitCell
DockingCell
FactoryBay
GateCells
UpgradeAttachment
VisualBounds
SelectionBounds
```

图片尺寸、透明像素、damage frame不能改变foundation。

initial Units/Infantry/Aircraft placement不等于永久动态occupancy。

## 9. Ramps, cliffs, water and shores

movement edge需联合：

```text
source/target surface
Level delta
RampType candidates
surface corners
TileSet roles
movement capabilities
overlay/bridge state
```

- 只看Level差不能定cliff；
- 只看RampType不能定passable；
- shore transition不是单纯water/land bool；
- palette颜色不能定water；
- amphibious需ground、water和shore transition能力同时一致。

## 10. Explicit movement layers

冻结候选：

```text
GroundLayer
UnderBridgeLayer
BridgeDeckLayer
AirLayer
SubterraneanLayer
```

同一cell可有多个node。high bridge不能与ground node合并，Unit `High`字段不足以生成bridge topology。

## 11. Adjacency graph contract

`MovementNode`：

```text
CellIdentity
ElevationLayer
SurfaceProfile
OccupancyProfile
Provenance
```

`MovementEdge`：

```text
SourceNode
TargetNode
Direction
ElevationDelta
RequiredCapabilities
BaseCostCandidate
DynamicConditionCandidates
EvidenceGrade
```

特殊edge：ramp、shore、bridge entrance、under-bridge、tunnel、subterranean、air takeoff/landing、teleport。

本研究不选择A*或任何寻路算法。

## 12. Determinism and safety

要求：

- checked arithmetic；
- bounded cell/node/edge/occupancy数量；
- deterministic graph ordering；
- stable source ordinals；
- unknown与duplicate不last-wins；
- Memory、Stream、short-read Stream、exact MIX window结果一致；
- synthetic fixtures不复用production坐标、邻接或cost公式；
- Core无`UnityEngine`。

## 13. Recommended models

```text
TerrainTopologyDocument
TerrainCellRaw
TerrainSurfaceCandidate
TerrainRoleBindingResult
MovementDomain
MovementLayer
MovementNode
MovementEdge
MovementGraphCandidate
MovementCapabilityProfile
MovementZoneRaw
SpeedTypeRaw
LocomotorReferenceRaw
LocomotorCapabilityCandidate
TerrainCostCandidate
PassabilityState
StaticOccupancyCandidate
DynamicOccupancySnapshot
FoundationOccupancyDescriptor
OverlayPassabilityCandidate
BridgeMovementDescriptor
ShoreTransitionDescriptor
RampTransitionDescriptor
MovementDiagnostic
MovementReadLimits
MovementConsistencyAnalysis
MovementRoundtripDescriptor
```

## 14. Explicit policies

```text
TerrainRolePolicy
SurfaceBindingPolicy
MovementZonePolicy
SpeedTypePolicy
LocomotorCapabilityPolicy
TerrainCostPolicy
RampTransitionPolicy
ShoreTransitionPolicy
OverlayPassabilityPolicy
FoundationOccupancyPolicy
BridgeMovementPolicy
DynamicOccupancyPolicy
AdjacencyPolicy
MovementDeterminismPolicy
MovementRoundtripPolicy
```

## 15. Evidence discipline

- EA FinalSun/FinalAlert 2：official editor evidence，不是runtime；
- OpenRA等：independent implementation evidence，不是stock事实；
- ModEnc、PPM、RA2 DIY：community documentation；
- Ares/Phobos：extension evidence；
- ProjectBaseline未来观察：只能`ObservedByFutureProjectBaselineAudit`；
- complete stock runtime算法：多数保持`Unresolved`。

## 16. Test and audit

`test-matrix.md`设计174项：

- cell/surface/topology：28；
- terrain/TMP/theater role：24；
- MovementZone/SpeedType/Locomotor：28；
- ramp/cliff/water/shore：24；
- Overlay/Terrain/Structure occupancy：26；
- bridge/tunnel/elevated/dynamic occupancy：24；
- graph/cost/safety/architecture/audit：20。

`baseline-audit-request.md`只设计脱敏aggregate审计，不运行、不读取ProjectBaseline。

## 17. Files

```text
README.md
layer-and-domain-boundaries.md
cell-surface-and-topology.md
terrain-overlay-and-object-passability.md
locomotor-movementzone-and-speedtype.md
ramps-cliffs-water-and-shores.md
bridges-tunnels-and-elevated-movement.md
foundations-occupancy-and-blocking.md
adjacency-cost-and-reachability.md
source-comparison.md
implementation-boundaries.md
test-matrix.md
baseline-audit-request.md
unresolved-questions.md
```
