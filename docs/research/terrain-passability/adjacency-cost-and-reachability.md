# Adjacency, cost and reachability contracts

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明实现仅作行为、格式事实与冲突参考，未复制、翻译、逐句改写或机械移植代码、寻路算法、Locomotor 实现或测试夹具。`code_imported: false`。


## 1. Graph is derived, not raw

```text
RawMapIdentity != MovementGraphCandidate
```

graph builder消费surface、layers、capability、occupancy candidates；parser不创建最终path graph。

## 2. Movement node

```text
MovementNode
- StableNodeId
- CellIdentity
- MovementLayer
- SurfaceProfile
- ElevationCandidate
- StaticOccupancyProfile
- DynamicOccupancyReference?
- Provenance
- EvidenceGrade
- Diagnostics
```

同一cell可有多个layer node。

## 3. Movement edge

```text
MovementEdge
- StableEdgeId
- SourceNode
- TargetNode
- Direction
- ElevationDeltaCandidate
- RequiredCapabilities
- SurfaceTransitionKind
- BaseCostCandidates
- DynamicConditionCandidates
- ActionRequiredCandidates
- Provenance
- EvidenceGrade
```

## 4. Neighbor candidates

`AdjacencyPolicy`显式声明：

- raw/canvas domain conversion；
- diamond orthogonal neighbors；
- diagonal candidates；
- parity behavior；
- boundary handling；
- missing cell；
- duplicate cell；
- map outside record；
- edge direction order；
- layer transition order。

不以screen adjacency决定graph。

## 5. Special edges

独立family：

```text
RampEdge
ShoreEdge
BridgeEntranceEdge
UnderBridgeTransition
TunnelEdge
SubterraneanEntryExit
AirTakeoffLanding
TeleportEdge
ScriptedExtensionEdge
```

每类有自己的validation和required capabilities。

## 6. Missing and duplicate cells

- missing cell不是implicit Clear；
- explicit default cell与missing不同；
- duplicate coordinates全部保留；
- conflicting duplicate默认fail-closed或Unknown；
- graph node不能last-wins；
- source order不应改变semantic result，但用于stable diagnostics/tie。

## 7. Passability query

推荐返回：

```text
MovementQueryResult
- State: Passable | TraversableWithCost | Blocked |
         RequiresCapability | TemporarilyBlocked |
         DestructibleBlocker | Unknown
- RequiredCapabilities
- ActionRequired
- CostCandidates
- BlockingContributors
- Evidence
- Diagnostics
```

## 8. Terrain cost inputs

```text
LandType/SurfaceCostCandidate
SpeedTypePercentageCandidate
RoadBonusCandidate
RampTransitionCandidate
ShoreTransitionCandidate
BridgeLayerCandidate
DiagonalCandidate
DynamicOccupancyCandidate
Crush/DestroyActionCandidate
MissionOrAiModifierCandidate
```

最终cost不在本轮确定。

## 9. Numeric representation

未来simulation建议deterministic integer或fixed-point，但研究层保留：

```text
RawNumericText
ParsedIntegerCandidate
ParsedPercentageCandidate
RationalCandidate
OverflowState
MissingState
```

float行为若作为来源证据必须标 implementation-specific。

禁止依赖Unity frame time或浮点iteration顺序。

## 10. Impassable sentinel

`Blocked/Unknown`不应仅编码成一个magic integer。可同时保留来源sentinel candidate，但semantic state独立。

OpenRA使用明确unreachable cost是具名公开引擎实现行为；其相对共享社区或工具谱系的独立性未获证明，也不是stock constant。

## 11. Zero and negative cost

分别定义：

- zero transition cost；
- zero speed（可能impassable）；
- missing speed；
- negative invalid/extension；
- overflow；
- saturating profile；
- action cost。

不允许除零或wrap。

## 12. Cost composition

必须由policy声明：

```text
base surface
→ directional/diagonal factor
→ transition modifier
→ road/terrain modifier
→ action or dynamic modifier
```

不同顺序可能产生不同rounding，因此policy/version进入descriptor和hash。

## 13. Reachability domains

`MovementDomain`可表示：

```text
Ground
Water
AmphibiousComposite
BridgeDeck
UnderBridge
Air
Subterranean
Tunnel
SpecialExtension
```

domain membership与单edge passability分开。

## 14. Buildability

buildability不参与movement graph node存在性。单独query：

```text
BuildabilityState
FoundationFit
TerrainBuildable
ResourceRestriction
OwnershipRestriction
ScenarioRestriction
ExitAndAdjacencyRequirements
```

## 15. Dynamic occupancy

graph base topology可以稳定，dynamic snapshot在query时叠加；是否生成增量graph或query filter属于未来adapter。

不得把moving actor永久写入graph。

## 16. Crush/destroy planning

两种合法后续policy：

- conservative：blocker视为blocked；
- action-aware：生成action-required transition。

本轮只保存候选，不选择。

## 17. Deterministic ordering

nodes：

```text
MovementLayerOrdinal
CellCanonicalKey
StableSourceOrdinal
```

edges：

```text
SourceNodeId
EdgeFamilyOrdinal
DirectionOrdinal
TargetNodeId
StableSourceOrdinal
```

不得依赖dictionary、thread timing或Unity object order。

## 18. Network/save/load

同一raw+policies必须得到：

- 相同node/edge集合；
- 相同canonical order；
- 相同cost candidates；
- 相同diagnostics codes；
- 相同aggregate hash。

dynamic snapshots需simulation tick和stable actor identity。

## 19. Bounds and budgets

`MovementReadLimits`：

- max cells；
- max duplicates per coordinate；
- max surface candidates per cell；
- max layers per cell；
- max nodes；
- max edges；
- max special transitions；
- max foundation cells；
- max occupants per cell；
- max diagnostics；
- max numeric magnitude；
- max source token length。

budget exceed产生structured failure，不截断成功。

## 20. Input equivalence

Memory、seekable Stream、short-read Stream和exact MIX window必须输出相同raw descriptors与graph candidates。不得越出MIX entry window或假设单次Read填满。

## 21. No algorithm selection

本文件不选择：

- A*；
- JPS；
- flow field；
- hierarchical pathfinder；
- path cache；
- NavMesh；
- heuristic；
- steering/reservation。

graph contract对未来算法保持中立。

## 22. Evidence

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| editor/map formats expose cells and Tube metadata | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Official editor evidence only; it does not establish runtime adjacency or cost semantics. | Preserve raw cells and Tube metadata under a named tool profile. | `NotRun` |
| OpenRA separates cost, occupancy and custom layers | `ImplementationSpecificBehavior` | OpenRA | Named public engine implementation; independence from shared community/tool lineage is unproven. | Comparison profile only. | `NotRun` |
| exact stock edge/cost formula | `Unresolved` | No original-runtime source located | No reliable complete formula was found. | Preserve explicit alternatives for future simulation work. | `NotRun` |
| deterministic graph contract | `DefensiveDesign` | Project policy | Project architecture and determinism choice, not external runtime evidence. | Stable node/edge ordering, checked arithmetic and fail-closed ambiguity handling. | `NotRun` |
