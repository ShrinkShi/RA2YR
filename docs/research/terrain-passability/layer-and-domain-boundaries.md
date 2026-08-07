# Layer and domain boundaries

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明实现仅作行为、格式事实与冲突参考，未复制、翻译、逐句改写或机械移植代码、寻路算法、Locomotor 实现或测试夹具。`code_imported: false`。


## 1. Why domain tags are mandatory

相同的整数对在 RA2/YR 数据链中可能代表完全不同的空间：

| Domain | Meaning | Typical producer | Forbidden shortcut |
|---|---|---|---|
| `IsoMapRawCoordinate` | IsoMapPack5 record X/Y | map reader | 不因落入范围而自动交换轴 |
| `DiamondCanvasCoordinate` | normalized diamond/canvas view | coordinate adapter | 不写回 raw X/Y |
| `ScenarioCellCoordinate` | object-section cell key/value | placement parser | 不假定与 IsoMap 编码相同 |
| `OverlayStorageCoordinate` | 512×512 packed array coordinate | Overlay reader | 不等于 map-local canvas |
| `TmpLocalCell` | TMP file/subtile local identity | TMP binder | 不当成 scenario cell |
| `FoundationCellOffset` | building authored footprint offset | Rules/Art binder | 不当成 absolute cell |
| `BridgeMovementCell` | bridge deck/entrance/underlay cell | bridge binder | 不与 ground node 合并 |
| `SimulationCell` | runtime cell/layer identity | simulation adapter | 不进入 raw parser |
| `PathGraphNode` | graph node identity | graph candidate builder | 不等同于 cell number |
| `UnityWorldCoordinate` | renderer/runtime adapter coordinate | Unity adapter | 不进入 Core |

所有 conversion 结果必须记录：

```text
SourceDomain
TargetDomain
PolicyId
InputRawValues
CheckedIntermediateValues
Result
EvidenceGrade
Diagnostics
```

## 2. Layer ownership

### Raw map layer

拥有：

- section occurrence；
- IsoMap record bytes；
- source order；
- duplicate coordinates；
- missing/explicit default distinction；
- raw Overlay arrays；
- raw scenario placements；
- unknown fields。

不拥有：

- final terrain role；
- passability；
- occupancy；
- movement cost；
- graph edge。

### Resource/theater binding layer

拥有：

- GlobalTileId candidate；
- TileSet range；
- TMP candidate；
- SubTile binding；
- TileSet logical role；
- raw TMP metadata。

不生成：

- final LandType；
- Locomotor capability；
- path graph；
- dynamic blocker。

### Rules binding layer

拥有：

- raw `MovementZone` token；
- raw `SpeedType` token；
- raw `Locomotor` reference；
- type properties；
- Foundation descriptor；
- overlay/terrain object properties；
- extension provenance。

不读取地图坐标，不创建单位实例。

### Surface layer

拥有：

- surface category candidates；
- Level/ramp/corner-height candidates；
- water/shore/cliff/ramp candidates；
- evidence conflicts；
- missing/unknown status。

不处理 current unit occupancy。

### Occupancy layer

拥有：

- static occupancy candidate；
- dynamic occupancy snapshot interface；
- temporary blocker；
- crushable/destructible action candidate；
- reservation references。

不执行 collision、steering 或 movement。

### Movement graph candidate layer

拥有：

- node/edge candidate；
- required capability；
- cost candidate；
- transition diagnostics；
- deterministic ordering。

不执行 A*、JPS 或 path cache。

## 3. Sparse, dense and duplicate cells

必须分别保存：

```text
MissingCell
ExplicitDefaultCell
SingleAuthoredCell
DuplicateEquivalentCells
DuplicateConflictingCells
OutOfDomainRecord
```

禁止：

- dense array assignment造成隐式 last-wins；
- 缺失 cell 自动复制邻居；
- duplicate 自动去重；
- out-of-domain record 删除；
- 为了生成图而改写原始记录。

`MovementGraphCandidate` 可以拒绝使用冲突 cell，但 `TerrainTopologyDocument` 仍须完整保留 raw records。

## 4. `Size` and `LocalSize`

候选边界：

- `Size`：scenario/map extent metadata；
- `LocalSize`：playable/local rectangle candidate；
- IsoMap raw domain：由 records 与 explicit profile共同解释；
- camera clamp：presentation/runtime policy；
- movement domain：由 map extent、surface、scenario restrictions和policy组成。

因此：

```text
LocalSize != raw record filter
LocalSize != camera clamp
LocalSize != automatic path graph extent
```

任何使用必须记录 profile 和 evidence。

## 5. Overlay coordinate boundary

Overlay storage通常是固定 packed storage view；其坐标转换必须独立于 IsoMap projection。

```text
OverlayStorageCoordinate
→ explicit overlay-to-scenario binding
→ ScenarioCellCoordinate candidate
→ CellIdentity
```

禁止：

- 因为 index 在 `512×512` 内就判定有效 map cell；
- 通过 color/art判断 Overlay 语义；
- missing OverlayData 合成零；
- Overlay reader直接查询 Rules。

## 6. Scenario cell boundary

`Y × 1000 + X` 是公开工具中最强的 RA2/YR scenario-cell候选，但仍应保留 axis/profile标识。Terrain、CellTag、Waypoint 和 placement records使用方式不同。

禁止使用：

```text
decodedValue in map bounds
```

作为轴序选择器，因为多个候选可能同时落入范围，且会破坏 roundtrip。

## 7. Foundation coordinate boundary

`FoundationCellOffset` 必须带有：

- authored origin；
- rectangle/custom profile；
- offset list source；
- outline/bib/exit/docking separation；
- rotation policy candidate；
- extension provenance。

转换为 absolute occupancy cells发生在 placement + type binding之后，不能在 Art parser 内完成。

## 8. Multi-layer cell identity

推荐：

```text
MovementNodeIdentity =
    CellIdentity
  + MovementLayer
  + NodeVariant
```

同一 cell可能同时存在：

- ground surface；
- under-bridge；
- bridge deck；
- air landing reference；
- subterranean/tube node。

单个 `(X,Y)` 不能作为唯一 graph key。

## 9. Raw/derived/dynamic separation

```text
TerrainCellRaw
→ TerrainSurfaceCandidate
→ StaticOccupancyCandidate
→ MovementGraphCandidate
→ DynamicOccupancySnapshot
→ PathQueryResult
```

这些结构的生命周期不同：

- raw：roundtrip稳定；
- surface：规则/资源绑定版本稳定；
- static occupancy：地图 placement + type binding稳定；
- graph candidate：policy/version稳定；
- dynamic snapshot：tick/save状态；
- query result：请求级临时值。

## 10. Buildability boundary

Buildability独立消费：

- terrain buildable；
- foundation fit；
- ownership/adjacency；
- resources；
- shroud；
- scenario restrictions；
- factory exit等。

它不应复用 `PassabilityState` 或 `MovementCostCandidate`。

## 11. Renderer separation

以下均不是 movement truth：

- screen Y；
- TMP depth plane；
- shadow；
- visual cliff face；
- selection bounds；
- local lighting；
- fog/shroud；
- sprite transparent region；
- bridge draw pass；
- aircraft ground shadow。

## 12. Domain diagnostics

候选 code：

```text
Movement.Domain.Unknown
Movement.Domain.AxisConflict
Movement.Domain.Overflow
Movement.Domain.ParityConflict
Movement.Domain.OutOfBoundsRecord
Movement.Domain.DuplicateEquivalent
Movement.Domain.DuplicateConflict
Movement.Domain.MissingCell
Movement.Domain.InvalidScenarioCell
Movement.Domain.OverlayBindingConflict
Movement.Domain.FoundationOffsetOverflow
Movement.Domain.LayerCollision
```

诊断只报告问题，不修复原始地图。
