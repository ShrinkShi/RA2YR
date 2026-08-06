# Implementation boundaries and presentation contracts

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明的实现仅作行为与冲突参考，未复制、翻译或机械移植其代码、公式实现或测试夹具。`code_imported: false`。


## 1. 本轮不实现

无 projection、renderer、depth sorting代码、anchor binder、foundation、occlusion、shadow、bridge renderer、VXL renderer、Texture、Sprite、Mesh、Material、Shader、Camera、Light、GameObject、C#、PowerShell、Unity测试或配置变更。

## 2. Core dependency rule

Core：

- 不引用 `UnityEngine`；
- 不保存 SortingLayer、Material、Shader、GameObject、Transform、Camera名称；
- 不创建GPU/Unity资源；
- 不读取simulation singleton；
- 不读取文件系统隐式全局状态；
- 接受已边界化的 Memory/Stream/MIX window输入；
- 输出immutable/raw/derived descriptor与structured diagnostics。

## 3. Contract pipeline

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

每步只增加derived信息，不覆盖raw identity。

## 4. Candidate model contracts

### `IsometricProjectionProfile`

轴、origin、logical metrics、height、arithmetic、rounding、inverse与证据。

### `LogicalTileMetrics`

logical width/height、half metrics、height step与允许范围；与asset raster尺寸分离。

### `MapPresentationCoordinate`

domain-tagged coordinate、raw provenance、checked conversion history。

### `ScreenProjectionResult`

64-bit logical result、rounded result、rounding delta、profile、diagnostics；不含camera zoom后的depth。

### `PresentationEntityDescriptor`

stable identity、family、raw placement引用、anchor refs、bounds refs、asset refs、pass/elevation/depth inputs、simulation snapshot refs。

### `PresentationAnchor`

anchor kind、coordinate domain、units、source、policy、evidence。

### `PresentationBounds`

bounds kind、domain、frame/state scope、conservative flag、source、limits。

### `FoundationDescriptor`

authored origin/cells/rectangle/irregular shape、extension profile、render/selection references；不含自动occupancy决定。

### `RenderPassDescriptor`

semantic pass ID、ordering group、palette/alpha/depth policy refs；不含Unity名字。

### `RenderDepthKey`

结构化components、policy、collision/tie diagnostic。

### `ElevationLayer`

semantic layer ID与source；不等于screen Y或Unity layer。

### `RawDepthPlane`

raw bytes/window/geometry/flags；无simulation语义。

### `OcclusionDescriptor`

receiver/occluder、screen region candidate、per-pixel policy、scope。

### `ShadowDescriptor`

source/geometry/anchor/projection/color/pass/receiver layer。

### bridge/aircraft descriptors

只绑定presentation inputs与runtime snapshot，不实现movement。

### diagnostics/limits/roundtrip

所有failure可结构化、可序列化、可聚合审计。

## 5. Explicit policies

每个policy有：

```text
PolicyId
Version
ProductProfile
EvidenceGrade
SourceReferences
Strictness
Limits
UnknownValueBehavior
DiagnosticsBehavior
```

不得使用全局bool代替：

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

## 6. Raw/derived separation

```text
RawMapIdentity != LogicalPresentationDescriptor
LogicalPresentationDescriptor != RendererImplementation
VisualEquivalence != PixelIdenticalOutput
RuntimeAcceptance != GameplayEquivalence
```

Core roundtrip目标是保留raw identity与已知字段，不保证不同renderer像素一致。

## 7. Checked arithmetic

必须checked：

- X±Y；
- map-size bias；
- half metric multiplication；
- Level/altitude subtraction；
- frame offset + bounds；
- foundation cell count；
- attachment aggregate bounds；
- depth key packing；
- plane length/offset；
- camera/client conversion（adapter）。

overflow产生diagnostic/failure，不wrap/clamp，除非显式profile且保留原值。

## 8. Read limits

`PresentationReadLimits`候选：

- max entities；
- max attachments per entity；
- max bounds per entity；
- max foundation cells/edges；
- max bridge pieces/edges；
- max depth plane dimensions/bytes；
- max animation frames referenced；
- max diagnostics；
- max coordinate magnitude；
- max stable tie collision group；
- max source string length。

## 9. Input equivalence

相同逻辑输入通过：

- `ReadOnlyMemory<byte>`
- seekable Stream
- short-read Stream
- exact MIX entry window

必须得到相同：

- raw descriptors；
- stable source ordinals；
- projection/anchor inputs；
- diagnostics；
- consistency analysis；
- canonical aggregate hash。

不得越出MIX window或依赖一次Read填满buffer。

## 10. Synthetic fixtures

synthetic fixtures：

- 不复用production projection函数生成expected screen值；
- 不复用production anchor binder生成expected pivot；
- 不复用production comparator生成expected order；
- 手写小整数、边界和冲突案例；
- source ordinal与duplicate显式；
- depth plane用小型虚构bytes，不含游戏资产；
- irregular foundation使用虚构cell set。

## 11. Diagnostics

`PresentationDiagnostic`：

```text
Code
Severity
Stage
SourceReference
EntityStableId? (internal only)
CoordinateDomain?
PolicyId
EvidenceGrade
NumericContext
MessageTemplateId
```

公开audit必须移除可链接entity/object数据。

## 12. Consistency analysis

只分析，不修复：

- TMP metrics vs selected logical metrics；
- Level范围；
- missing anchor；
- foundation/art bounds矛盾；
- pass/elevation conflict；
- depth collision；
- attachment cycle；
- bridge layer缺失；
- aircraft altitude缺失；
- culling bounds不足；
- input mode equivalence。

## 13. Renderer adapter

未来adapter可选择：

- Unity Renderer/SortingGroup；
- CPU software compositing；
- GPU depth buffer；
- stencil/mask；
- custom batcher。

但必须消费相同 Core descriptor，且adapter选择不回写Core格式语义。

## 14. Architectural acceptance

- noEngineReferences；
- noUnityObjects；
- parser不创建presentation renderer resource；
- sorting不访问pathfinding；
- UI bounds不访问occupancy writer；
- camera不进入logical depth；
- deterministic save/load/replay；
- structured diagnostics；
- bounded input；
- evidence grade可序列化。
