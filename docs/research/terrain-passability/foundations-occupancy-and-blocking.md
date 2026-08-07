# Foundations, occupancy and blocking

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明实现仅作行为、格式事实与冲突参考，未复制、翻译、逐句改写或机械移植代码、寻路算法、Locomotor 实现或测试夹具。`code_imported: false`。


## 1. Foundation is authored data

`Foundation`、`Foundation.X/Y`及extension irregular foundation来自Rules/Art binding。禁止从SHP/VXL尺寸、透明像素或selection bounds推导。

推荐：

```text
FoundationOccupancyDescriptor
- PlacementOrigin
- FoundationOrigin
- AuthoredCells
- RectangularCandidate?
- IrregularCandidate?
- FoundationXRaw?
- FoundationYRaw?
- ExtensionProfile?
- StableCellOrder
- Diagnostics
```

## 2. Separate masks

同一Structure可拥有不同mask：

```text
SimulationOccupancyMask
MovementBlockingMask
BuildabilityMask
PlacementValidationMask
BibCells
FactoryExitCells
DockingCells
FactoryBayCells
GateCells
WallConnectionCells
UpgradeAttachmentCells
SelectionExtent
VisualExtent
ShadowExtent
```

不得合并。

## 3. Static occupancy candidate

```text
StaticOccupancyCandidate
- CellIdentity
- ElevationLayer
- OccupantStableId
- OccupantFamily
- OccupancyClass
- BlockingClassCandidates
- CrushabilityCandidate
- DestructibilityCandidate
- Source
- EvidenceGrade
```

它描述authored/start-state，不执行collision。

## 4. Dynamic occupancy snapshot

future runtime adapter可提供：

```text
DynamicOccupancySnapshot
- SimulationTick
- CellAndLayer
- ActorStableId
- SubCell?
- MotionState
- GateState?
- LandingState?
- BurrowState?
- TemporaryBlockerClass?
```

Core地图parser不创建或更新该snapshot。

## 5. Units and movement reservations

严格区分：

```text
AuthoredPlacement
CurrentRuntimeCell
CurrentSubCell
MovingReservation
DestinationReservation
FormationSlot
CollisionRadius
PathNodeOccupancy
```

本轮只定义接口，不实现reservation、avoidance或formation。

## 6. Infantry SubCell

Infantry同cell可能占不同SubCell。`SubCell`是occupancy granularity candidate，不是新的IsoMap coordinate，也不能通过SHP偏移推断。

需要显式：

- valid index profile；
- unknown/out-of-range；
- duplicate subcell occupancy；
- full-cell blocker interaction；
- sharing policy。

## 7. Gates

gate需要两层事实：

```text
StaticGateDescriptor
DynamicGateState
```

动态状态候选：

```text
Closed
Opening
Open
Closing
Destroyed
Unknown
```

path blocking取决于runtime state、owner/access policy和movement capability。raw placement parser不能把gate永久阻塞。

Ares gate文档只证明extension能力和社区行为，不提升vanilla状态。

## 8. Walls and fences

分别保存：

- overlay/type family；
- connection data；
- blocker class；
- crushability；
- destructibility；
- owner；
- damage/destroyed state；
- future action cost。

连接frame不等于blocking state。

## 9. Factory exits and docking

factory exit、dock、bay和rally point：

- 不属于foundation occupancy；
- 可以要求保持clear；
- 可以是temporary reservation；
- 不必作为永久path blocker；
- 由future production/placement adapter使用。

## 10. Bib and Smudge

Bib可占foundation周边视觉cells，但：

```text
BibVisualCells != FoundationCells != MovementBlockerCells
```

runtime smudge也不默认阻塞。

## 11. Upgrades

upgrade placement可能附着于building：

- 不产生独立ground foundation，除非extension显式；
- 可扩展visual/shadow bounds；
- occupancy变化需Rules/extension policy；
- missing Art不删除attachment或occupancy candidate。

## 12. Irregular foundations

Ares等扩展允许非矩形foundation候选。必须保存：

- authored cell set；
- origin；
- duplicate/gap；
- out-of-range offsets；
- profile version；
- extension provenance；
- ordering。

不把bounding rectangle当occupancy。

## 13. Aircraft

aircraft ground shadow不占cell。只有：

- landed state；
- landing reservation；
- hangar/dock state；
- crash/temporary blocker

可产生ground occupancy candidate。

## 14. Subterranean units

hidden/burrowed actor可能：

- 不占ground blocking；
- 占subterranean layer；
- 在entry/exit阶段临时占两层；
- 需要surface target validation。

具体规则留给runtime policy。

## 15. Crush and destroy

分别询问：

```text
CanOccupy
CanEnter
CanCrush
CanDestroy
CanPassAfterDestroy
PathCostBeforeAction
ActionRequired
```

一个destructible blocker不等于passable。pathfinder是否计划攻击后穿越是后续策略。

## 16. Temporary blockers

候选包括：

- closing gate；
- moving unit；
- deployment；
- scripted obstacle；
- production exit reservation；
- bridge repair；
- crate/pickup；
- mission lock。

必须与static surface和foundation分离。

## 17. Occupancy combination

候选组合不是简单OR：

```text
surface state
+ static occupants
+ dynamic snapshot
+ mover capabilities
+ ownership/access
+ crush/destroy policy
+ reservation policy
→ entry status
```

输出multi-state与action requirements。

## 18. Art and damage independence

禁止：

- image透明区域改变occupancy；
- building damage frame改变foundation；
- missing Art删除blocker；
- turret/barrel bounds扩展foundation；
- selection/click bounds决定occupancy。

## 19. Determinism

occupancy candidates按：

```text
CellIdentity
ElevationLayer
OccupantStableSourceOrdinal
OccupancyClassOrdinal
```

稳定排序。不得使用Unity instance ID或hash enumeration。

## 20. Evidence

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert公开placement/foundation相关编辑输入 | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | 只确认官方editor字段和工具行为。 | 保留authored fields/provenance。 | `NotRun` |
| OpenRA分离moving、stationary、crushable和temporary cell flags | `ImplementationSpecificBehavior` | OpenRA | 单一目标引擎设计。 | 仅作occupancy模型比较。 | `NotRun` |
| Ares/Phobos gate、custom foundation和TerrainType扩展 | `ImplementationSpecificBehavior` | Named extension projects | extension profile，不能反推vanilla。 | 显式隔离extension。 | `NotRun` |
| 原版runtime gate/crush/destroy、reservation和path planning规则 | `Unresolved` | No original-runtime source located | 现有工具/社区资料不足以形成可靠统一算法。 | future simulation/pathfinding adapter负责。 | `NotRun` |
| Foundation、movement blocker、buildability、dynamic occupancy和visual bounds的关系 | `ConflictingSources` | Editor/tool/community/extension models | 模型与优先级存在差异。 | 不合并mask，不从Art推断occupancy。 | `NotRun` |
| multi-mask、raw preservation、stable ordering和fail-closed occupancy | `DefensiveDesign` | Project policy | 项目保真与确定性设计。 | 不last-wins、不把initial placement固化为永久occupancy。 | `NotRun` |
