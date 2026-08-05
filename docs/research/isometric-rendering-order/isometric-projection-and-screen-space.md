# Isometric projection and screen space

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明的实现仅作行为与冲突参考，未复制、翻译或机械移植其代码、公式实现或测试夹具。`code_imported: false`。


## 1. 不选择“看起来对”的公式

本专题禁止根据截图对齐、像素相似或某张地图能显示来自动选择投影。候选必须由来源、适用产品、editor/runtime 类别、tile metrics、origin、rounding 和失败行为共同标识。

## 2. 官方编辑器候选

EA 发布的 FinalSun/FinalAlert 2 编辑器在固定 revision 中使用：

```text
projectedX = (IsoSize - 2 - mapX + mapY) * tileWidth / 2
projectedY = (mapY + mapX - mapZ) * tileHeight / 2
```

指标：

| 模式 | tile width | tile height | half width | half height |
|---|---:|---:|---:|---:|
| TS editor | 48 | 24 | 24 | 12 |
| RA2 editor | 60 | 30 | 30 | 15 |

```text
EvidenceGrade: ConfirmedByOfficialToolSource
Source: EA FinalSun / FinalAlert 2
AuditStatus: NotRun
```

该等级只确认官方编辑器的公式、配置和inverse行为。编辑器inverse使用浮点除法、`±0.5` bias和整型转换；不能据此断言游戏executable的负数、边界、midpoint rounding、picking或所有对象族的排序完全相同。

## 3. Normalized canvas 候选

项目候选分两步：

```text
DiamondCanvasX = AxisXX * rawX + AxisXY * rawY + BiasX
DiamondCanvasY = AxisYX * rawX + AxisYY * rawY + BiasY

ScreenX = ProjectionOriginX + DiamondCanvasX * HalfTileWidth
ScreenY = ProjectionOriginY
        + DiamondCanvasY * HalfTileHeight
        - CellLevel * HeightStepPixels
```

EA editor profile 的 axis candidate 是 `(-X + Y, X + Y)`，且 X bias 与 `IsoSize` 相关。其他实现可能使用轴交换、不同 origin 或 world-space 中间层，所以 profile 必须保存矩阵，而非只保存 `isRa2=true`。

## 4. `IsometricProjectionProfile`

建议字段：

```text
ProfileId
ProductFamily
SourceEvidence
RawCoordinateDomain
AxisMatrix2x2
CanvasBiasPolicy
MapOriginPolicy
ProjectionOriginPolicy
LogicalTileMetricsId
HeightOffsetPolicyId
ParityPolicy
ArithmeticMode
IntermediateBitWidth
RoundingMode
NegativeDivisionMode
OverflowMode
InverseProjectionPolicy
```

默认安全要求：

- 64-bit checked intermediate；
- 最终目标范围显式验证；
- 不 wrap；
- inverse 不是 roundtrip 证据，除非 policy 明确；
- negative coordinate 与 midpoint 单独测试；
- fixed-point/float 候选不能静默互换。

## 5. Tile metrics 不是 TMP 尺寸别名

推荐拆分：

- `LogicalTileMetrics`: 投影网格宽高、half metrics、height step；
- `AssetPixelMetrics`: 某 TMP/SHP/VXL raster result 的像素 bounds；
- `ViewportScaleProfile`: zoom、DPI、letterbox、UI scale；
- `IsometricProjectionProfile`: 轴、origin、rounding。

TMP header 中的宽高：

- 决定 TMP base diamond plane 几何和长度验证；
- 可与 selected logical metrics 做一致性检查；
- 不自动覆盖全局 map projection；
- 不因 extra graphics 增大 logical tile；
- 不承担 DPI 或客户端缩放。

## 6. 60×30 与 48×24 的结论

- `48×24`: 官方 TS editor 配置、多个公开 TS reader/renderer 的常见值；
- `60×30`: 官方 RA2 editor 配置、多个公开 RA2/YR tool 的常见值；
- “theater 决定 tile metric”：当前无充分证据；
- “每个 TMP 自己决定 map projection”：拒绝作为默认；
- “renderer profile 决定 logical metric”：项目推荐；
- physical texture upscale/downscale 与 logical metric 分离。

跨工具收敛的正式等级为`Underconfirmed`：这些实现的谱系独立性与原版runtime适用性均未证明。

## 7. TMP extra graphics

extra rectangle 可以越出 base diamond：

- 改变 `VisualBounds` 与 `ConservativeCullingBounds`；
- 可能提供 cliff face、shore extension 或其他视觉内容；
- 不改变 cell footprint；
- 不改变 projection anchor；
- 不自动改变 Level；
- 不据其边界推断 occluder 高度。

## 8. Screen、camera 与 viewport

官方编辑器公开代码把：

1. map → projected；
2. projected − `viewOffset`；
3. window/client offset；
4. view scale；

分开处理。OpenRA 也把 world-to-screen 与 viewport zoom/UI scale 分开。项目必须保持：

```text
ScreenProjectionResult
+ CameraOrigin
+ ViewportOffset
+ ScreenShakeOffset
→ ClientPixel
→ DPI / UI scaling
→ PhysicalDisplayPixel
```

zoom、screen shake、letterboxing、高 DPI 不能进入 `RenderDepthKey`。

## 9. Parity 与 origin

潜在冲突：

- even/odd tile dimensions；
- odd canvas coordinate 乘 half metric；
- object subcell offsets；
- map-size bias；
- LocalSize 非零 origin；
- editor scroll surface origin；
- runtime world origin；
- minimap origin。

默认 policy 不假设 parity 无关。每个 profile 记录：

- 是否要求 even tile width/height；
- half metric 是否整数；
- odd coordinate 的 exact arithmetic；
- origin 是否 cell center、diamond top、left corner 或 map surface top-left。

## 10. 负坐标、overflow 与 rounding

必须测试：

- `int16` raw min/max；
- normalized sum/difference 超出 16-bit；
- map-size bias 乘 half width；
- Level 减法；
- negative inverse；
- midpoint `±0.5`；
- `floor`、`truncate`、nearest-even、away-from-zero；
- camera offset造成负 client pixel但 logical projection有效。

建议 `ScreenProjectionResult`：

```text
LogicalPixelX64
LogicalPixelY64
RoundedPixelX
RoundedPixelY
RoundingDelta
ProfileId
Diagnostics[]
```

## 11. VXL、SHP 与 pixel scale

SHP frame raster、VXL rasterizer future output 与 TMP raster 可以被同一 renderer compositing，但“不等于格式本身共享全局 pixel scale”。由 asset binding 提供：

- native asset pixel metrics；
- authored offsets；
- renderer logical pixel scale；
- optional asset scale override；
- evidence/provenance。

VXL/HVA 的 world/voxel transform 不能被 SHP frame metrics 替代。

## 12. Mini-map 与 replay

minimap 通常使用独立 map-to-minimap transform；spectator/replay 共享 presentation snapshot 但可拥有不同 camera。两者不得改变：

- raw map identity；
- selected projection profile；
- entity stable order；
- simulation coordinate。

## 13. 推荐决定

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert的RA2 axis/origin family、`60×30`和`15` level step | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | 只确认官方编辑器行为。 | 保存为命名editor-compatible profile。 | `NotRun` |
| 该profile是原版RA2/YR唯一projection/runtime contract | `Underconfirmed` | 官方编辑器和公开工具收敛 | 缺少原版runtime source，工具谱系也未证明独立。 | 初始候选可选，但不能自动提升。 | `NotRun` |
| 64-bit checked arithmetic、显式rounding/profile和禁止“最像截图”选择 | `DefensiveDesign` | Project policy | 项目安全与确定性设计。 | profile ID必须显式记录。 | `NotRun` |
| 原版runtime对负数、边界、inverse picking和midpoint的精确行为 | `Unresolved` | 未找到原版runtime source | editor inverse不足以确认runtime。 | 保留多个命名rounding profile。 | `NotRun` |
