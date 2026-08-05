# Layer and coordinate boundaries

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明的实现仅作行为与冲突参考，未复制、翻译或机械移植其代码、公式实现或测试夹具。`code_imported: false`。


## 1. 目的

表现层不能把“地图里的一个坐标”当成单一结构。RA2/YR 公开实现同时存在菱形地图索引、方形存储数组、对象 section cell、TMP 局部像素、投影像素、视口像素和运行时世界位置；字段数值相同也不代表语义相同。

## 2. 输入层与拥有者

| 输入 | 原始拥有者 | 表现层可读取 | 表现层不得决定 |
|---|---|---:|---|
| IsoMap raw X/Y、Level | map reader | 是 | pathfinding、occupancy |
| TMP SubTile、HeightRaw、RampTypeRaw | TMP reader / theater binder | 是 | 全局 Level、simulation slope |
| TMP color/depth/extra planes | TMP reader | 是 | terrain passability |
| Overlay type/data | Overlay reader + registry binder | 是 | bridge simulation 完整状态 |
| Terrain、Smudge placement | map object reader | 是 | 规则效果 |
| Structure/Unit/Infantry/Aircraft placement | scenario reader | 是 | runtime AI、movement |
| SHP frame bounds/offsets | SHP reader | 是 | foundation |
| VXL/HVA transforms/bounds | VXL/HVA reader | 是 | occupancy |
| Rules/Art foundation/ZAdjust/shadow hints | composed INI typed view | 是 | 原始字段回写 |
| runtime altitude/pose | simulation adapter | 仅通过显式快照 | parser 语义 |
| camera/viewport/zoom | renderer adapter | 是 | logical depth |

## 3. 坐标域

| Domain | 建议结构 | 单位 | 允许转换 | 禁止隐式转换 |
|---|---|---|---|---|
| `IsoMapRawCoordinate` | raw signed/unsigned-preserving pair | map raw units | → normalized canvas by profile | → Unity world |
| `DiamondCanvasCoordinate` | normalized diagonal axes | logical half-tile steps | → projected logical pixel | → overlay index |
| `ScenarioCellCoordinate` | section-authored cell | cell/subcell | → map lookup by explicit codec | → TMP local pixel |
| `OverlayStorageCoordinate` | 512×512 or extension storage | array index axes | → map domain by storage profile | → screen |
| `TmpLocalCoordinate` | cell-local pixel | asset pixel | → visual rect relative to cell anchor | → map Level |
| `ScreenPixelCoordinate` | projected world pixel | logical/physical pixel | → camera/client pixel | → simulation |
| `CameraCoordinate` | viewport-relative pixel | physical pixel | → display | → depth key |
| `SimulationCoordinate` | runtime world/lepton/cell | simulation unit | → presentation snapshot | → raw map mutation |
| `UnityWorldCoordinate` | adapter-owned | Unity units | renderer-only | → Core serialization |

`MapPresentationCoordinate` 应携带 domain tag；跨域函数输入与输出都显式声明 profile 和 checked-arithmetic 结果。

## 4. Raw 与 derived

必须保留：

- raw X/Y/Level 原值和来源 ordinal；
- normalized canvas 作为可丢弃 derived view；
- projection profile ID 与 version；
- derived screen result、overflow/rounding diagnostics；
- 原始对象 placement 与 presentation anchor 分离；
- raw frame offset 与 binder 生成的 pivot 分离。

不得为“对齐画面”回写 raw 坐标或 SHP offset。

## 5. Diamond canvas 候选

官方编辑器公式可分解为：

```text
axisU = -rawX + rawY
axisV =  rawX + rawY
projectedX = originBiasX + axisU * halfTileWidth
projectedY = originBiasY + (axisV - level) * halfTileHeight
```

其中官方编辑器的 `originBiasX` 包含 `(IsoSize - 2) * halfTileWidth`。项目候选不把该 bias 固化为格式事实，而将下列项放进 `IsometricProjectionProfile`：

- axis matrix/sign；
- X/Y source order；
- map-size-dependent bias；
- parity/bias rule；
- logical half width/height；
- height step；
- integer/fixed/float arithmetic；
- rounding；
- overflow policy。

## 6. Layer 不是坐标

以下概念必须独立：

- `RenderPass`
- `ElevationLayer`
- `DepthKey`
- `PerPixelDepthPolicy`
- `PaletteRole`
- `AlphaMode`
- `PostProcessStage`

例如 aircraft 可以处于 `AirLayer`，仍以 authored cell 作为 ground reference；shadow 可以在 `ShadowPass`，但锚定 ground projection；high bridge deck 需要显式 elevation layer，而不是给 screen Y 加一个魔数。

## 7. Map、presentation 与 simulation

```text
raw map and asset descriptors
→ explicit coordinate-domain views
→ logical presentation entities
→ anchor and bounds binding
→ render-pass classification
→ deterministic depth-order candidate
→ visibility and occlusion inputs
→ future renderer adapter
```

边界规则：

- parser 只产生 raw descriptor；
- projection 只产生 coordinate view；
- anchor binder 读取 map/asset/INI descriptor，不读取 AI、velocity 或 occupancy；
- simulation adapter 若提供 runtime altitude，必须标记来源与 tick；
- renderer adapter 消费 pass/depth/occlusion，不反向修改地图；
- visibility/culling 只决定“是否提交绘制”，不删除 logical entity。

## 8. LocalSize、Size 与 map bounds

`Size`/`LocalSize` 是 map-domain 元数据。它们可以成为可见域、编辑域或 gameplay domain 的输入，但不能直接等同：

- camera clamp rectangle；
- texture dimensions；
- minimap bounds；
- selection domain；
- simulation occupancy domain。

必须由 `ViewportScaleProfile`/camera adapter 明确选择。

## 9. 一致性诊断

建议结构化诊断：

- `CoordinateDomainMismatch`
- `AxisOrderConflict`
- `OriginPolicyUnresolved`
- `ParityPolicyUnresolved`
- `ProjectionOverflow`
- `ProjectionRoundingLoss`
- `MapDomainOutsideLocalSize`
- `UnexpectedNegativeCoordinate`
- `CameraValueUsedInLogicalDepth`
- `UnityCoordinateEscapedIntoCore`

## 10. 证据

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert显式区分map与projected坐标并提供投影/逆投影 | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | 官方编辑器类型与公式，不是原版runtime全部坐标合同。 | 保留命名editor profile。 | `NotRun` |
| Overlay方形storage与IsoMap菱形domain存在不同坐标视图 | `Underconfirmed` | 官方编辑器、公开reader和既有研究 | 多来源收敛，但谱系独立性及runtime边界未完全证明。 | 使用domain tag和显式转换profile。 | `NotRun` |
| 一个数值可在不同坐标domain间隐式互换 | `ConflictingSources` | 工具命名、轴顺序和目标引擎视图存在差异 | 相同数值不保证相同语义。 | 禁止隐式转换。 | `NotRun` |
| domain-tagged Core结构、raw/derived分离和camera不进入logical depth | `DefensiveDesign` | Project policy | 架构与保真策略。 | checked conversion并保留provenance。 | `NotRun` |
