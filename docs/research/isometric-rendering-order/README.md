# M3-R11 — Isometric rendering order dossier

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明的实现仅作行为与冲突参考，未复制、翻译或机械移植其代码、公式实现或测试夹具。`code_imported: false`。


## 1. 任务边界

本目录研究 RA2/YR 地图表现层所需的声明式输入、坐标域、实体族、锚点、边界、渲染 pass、确定性深度候选、遮挡输入、桥梁与高空层，以及未来 renderer adapter 的责任边界。

- task-start `main`: `c4db6516eaa4e971f8bfe20cd3462dd397f39a55`
- research branch: `research/m3-isometric-rendering-order-dossier`
- 仅新增本目录的 14 个 Markdown 文件。
- 未实现 projection、renderer、Tilemap、Mesh、Texture、Sprite、VXL rasterizer、shader、depth buffer、camera、shadow、particle、selection 或 Unity GameObject。
- 未读取或运行 ProjectBaseline、Unity、RA2/YR、FinalAlert、WAE、XCC。
- 未修改 PR #25、#28、#29、#30、#31、#32、#33、#34。

## 2. 冻结候选管线

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

冻结的责任约束：

- projection 不修改地图坐标；
- anchor binder 不读取 simulation state；
- renderer sorting 不决定 pathfinding；
- SHP/VXL/TMP reader 不创建 Unity 对象；
- foundation 不从图片尺寸推断；
- shadow 不决定 occupancy；
- aircraft visual altitude 不等于 simulation 高度；
- Core 不依赖 `UnityEngine`。

## 3. 核心结论

1. **坐标域必须显式。** `IsoMapRawCoordinate`、`DiamondCanvasCoordinate`、`ScenarioCellCoordinate`、`OverlayStorageCoordinate`、`TmpLocalCoordinate`、`ScreenPixelCoordinate`、`CameraCoordinate`、`SimulationCoordinate` 与 `UnityWorldCoordinate` 不可互换。
2. **投影采用 profile，而不是常量散落。** 官方编辑器在 TS 使用 `48×24`、RA2 使用 `60×30`，并把地图高度乘以半 tile 高度；这证明编辑器候选，不证明原版运行时所有舍入、origin 与 parity 细节。
3. **TMP 像素尺寸不是自动的地图全局尺度。** TMP header 的宽高首先是资产栅格及 plane 长度输入；是否与投影逻辑 tile metrics 一致由绑定 policy 验证。
4. **pass、depth key、显式 elevation layer、逐像素 depth、alpha、palette/remap 与 post-process 是不同维度。**
5. **深度排序必须稳定且可重放。** 不使用 Unity instance ID、哈希枚举顺序、相机 zoom 或随机数；同 key 由显式 family/attachment/source ordinal/identity 解决。
6. **锚点与 bounds 是多类型数据。** 图片中心、透明裁剪框、selection bounds 与 occupancy 都不能替代 authored ground anchor 或 foundation。
7. **桥上、桥下、地面与空中需要显式层候选。** 单独的 screen-Y 无法表达同一平面投影处的上下层关系。
8. **TMP depth bytes 是 renderer 输入候选，不是地图 Level、通行性或 simulation elevation。**
9. **UI 与地图表现分离。** 选择框、血条、悬停标签、命令光标和 debug visualization 属于 UI adapter。
10. **renderer independence。** Core 不保存 Unity SortingLayer、Material 或 Shader 名称作为格式语义。

## 4. 推荐 Core 候选模型

- `IsometricProjectionProfile`
- `LogicalTileMetrics`
- `MapPresentationCoordinate`
- `ScreenProjectionResult`
- `PresentationEntityDescriptor`
- `PresentationEntityFamily`
- `PresentationAnchor`
- `PresentationBounds`
- `FoundationDescriptor`
- `RenderPassDescriptor`
- `RenderDepthKey`
- `RenderDepthComponents`
- `RenderTieBreakPolicy`
- `ElevationLayer`
- `RawDepthPlane`
- `DepthInterpretationProfile`
- `OcclusionDescriptor`
- `ShadowDescriptor`
- `BridgePresentationDescriptor`
- `AircraftPresentationDescriptor`
- `PresentationDiagnostic`
- `PresentationReadLimits`
- `PresentationConsistencyAnalysis`
- `PresentationRoundtripDescriptor`

显式 policy：

- `ProjectionPolicy`
- `TileMetricPolicy`
- `HeightOffsetPolicy`
- `AnchorBindingPolicy`
- `FoundationBindingPolicy`
- `RenderPassPolicy`
- `DepthOrderingPolicy`
- `TieBreakPolicy`
- `DepthPlanePolicy`
- `OcclusionPolicy`
- `ShadowPolicy`
- `BridgeLayerPolicy`
- `AircraftLayerPolicy`
- `PresentationRoundtripPolicy`

## 5. 文档索引

| 文件 | 内容 |
|---|---|
| `layer-and-coordinate-boundaries.md` | layer、格式输入与坐标域边界 |
| `isometric-projection-and-screen-space.md` | 投影候选、tile metrics、origin、rounding、camera |
| `map-cell-level-and-height-offsets.md` | Level、TMP height/ramp、视觉与 simulation elevation |
| `render-entity-families-and-passes.md` | 实体族与 render-pass 分类 |
| `depth-order-and-tie-breaking.md` | 深度 key、稳定 tie-break 与确定性 |
| `anchors-foundations-and-bounds.md` | SHP/VXL/TMP anchor、foundation 与多类 bounds |
| `occlusion-depth-planes-and-shadows.md` | TMP depth/extra depth、遮挡与 shadow contracts |
| `bridges-aircraft-and-elevated-layers.md` | 桥上/桥下/空中显式 layer |
| `source-comparison.md` | 固定来源、许可证、共享谱系与冲突 |
| `implementation-boundaries.md` | Core/adapter 边界、diagnostics、limits、roundtrip |
| `test-matrix.md` | 166 项独立 oracle 测试设计 |
| `baseline-audit-request.md` | 未来 ProjectBaseline 脱敏只读审计设计 |
| `unresolved-questions.md` | P0/P1 未决问题与停止条件 |

## 6. Formal evidence grades

所有正式 `Grade` 字段只使用以下九项封闭集合：

- `ConfirmedByOriginalRuntimeSource`
- `ConfirmedByOfficialToolSource`
- `ConfirmedByMultipleIndependentImplementations`
- `ConfirmedCommunityConvention`
- `ImplementationSpecificBehavior`
- `DefensiveDesign`
- `ConflictingSources`
- `Underconfirmed`
- `Unresolved`

`ConfirmedByOriginalRuntimeSource` 仅用于真正的原版 RA2/YR runtime 或其实际 source。本专题没有找到可支撑该等级的公开原版 runtime source。

FinalSun、FinalAlert 等官方编辑器行为使用 `ConfirmedByOfficialToolSource`，不能据此确认原版 runtime。单个 renderer、reader、writer 或 extension 行为使用 `ImplementationSpecificBehavior`。多个公开工具的公式或排序模型即使收敛，只要谱系独立性或 runtime 适用性未证明，就使用 `Underconfirmed`；来源直接冲突时使用 `ConflictingSources`。

项目稳定 tuple key、显式 elevation layer、raw preservation、checked arithmetic、禁止截图/视觉合理性选择、禁止从图像推断 foundation 等均属于 `DefensiveDesign`，不是外部格式事实。

Future ProjectBaseline work 单独记录：

```text
AuditStatus: NotRun
FutureEvidenceSource: ProjectBaselineAggregateAudit
```

该状态不表示已经读取或观察 ProjectBaseline，也不能自动提升 compatibility 或成为 `ConfirmedByOriginalRuntimeSource`。

## 7. Normalized claim summary

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert 使用 RA2 `60×30`、TS `48×24` 的投影配置，并按半 tile height 处理 Level | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | 只确认官方编辑器公式、配置和作者工具行为；不确认原版 runtime 的全部 rounding、origin、picking 或 family comparator。 | 作为命名 editor-compatible profile 保存。 | `NotRun` |
| `60×30` 与 `15 px per Level` 是 RA2/YR 的主要公共候选 | `Underconfirmed` | FinalAlert 与多个公开工具/社区资料 | 工具收敛不证明实现谱系独立，也不建立唯一 runtime 合同。 | 显式 profile；不得按截图自动选择。 | `NotRun` |
| OpenRA 的 stable sort 和 elevated bridge layer | `ImplementationSpecificBehavior` | OpenRA | 目标引擎实现行为，不是 RA2/YR runtime 算法。 | 仅作比较输入。 | `NotRun` |
| 一个唯一的原版 runtime render pass list、depth comparator 和 tie rule 已被确定 | `Unresolved` | 未找到原版 runtime source | 公开工具模型、社区规则和 editor 输入不足以确定完整算法。 | 使用可序列化稳定 tuple 与 canonical source ordinal。 | `NotRun` |
| 公开 renderer 在 projection、pass、bridge layer 和 depth model 上存在不同实现 | `ConflictingSources` | FinalAlert、OpenRA、WAE、CNCMaps、社区资料 | 冲突可能来自目标引擎、坐标命名、family policy 或共享知识传播。 | 保留多个命名 profile，不通过视觉结果选胜者。 | `NotRun` |
| authored foundation、raw frame offset、TMP depth bytes、occupancy 和 simulation elevation 必须分层 | `DefensiveDesign` | Project policy | 保真与架构边界，不是 runtime 事实。 | 禁止从 SHP/VXL/TMP 视觉尺寸反推 foundation、passability 或 simulation state。 | `NotRun` |
