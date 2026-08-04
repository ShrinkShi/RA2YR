# Occlusion, depth planes and shadows

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明的实现仅作行为与冲突参考，未复制、翻译或机械移植其代码、公式实现或测试夹具。`code_imported: false`。


## 1. TMP planes

TMP cell可提供：

- base diamond color；
- base diamond depth/Z；
- extra rectangular color；
- extra rectangular depth/Z。

`RawDepthPlane` 保存原始样本、几何、offset、长度与flag provenance；parser不立即解释为世界高度。

## 2. Depth plane 不是什么

TMP depth bytes不是：

- IsoMap Level；
- TMP HeightRaw；
- RampTypeRaw；
- alpha；
- terrain passability；
- simulation elevation；
- object anchor；
- bridge state。

depth plane缺失不应令一个本来允许无depth的 TMP raw parse失败；如果flag/offset声明存在但窗口截断，则按 strict raw policy 诊断/失败，绝不padding。

## 3. Interpretation model

```text
RawDepthPlane
- Geometry
- RawBytes
- DeclaredOffset
- ActualWindow
- SourceFlags

DepthInterpretationProfile
- SampleDomain
- SampleScale
- ReferencePlane
- MissingPlanePolicy
- OutOfRangePolicy
- ZeroPolicy
- ExtraPlanePolicy

OcclusionMaskCandidate
- ReceiverEntity
- OccluderEntity
- ScreenRegion
- ComparisonInputs
- Evidence

PerPixelDepthPolicy
- CompareFunction
- WritePolicy
- AlphaInteraction
- PassScope
```

## 4. 公开 renderer 行为的限度

OpenRA等实现读取/过滤 TMP depth并可启用 depth buffer；CNCMaps、editor等也有自己的 compositing。它们证明“depth plane可以作为 renderer输入”，不证明 original runtime 的具体 compare、scale、stencil或mask算法。

禁止复制 GPL renderer公式；本项目只保留行为需求与可配置 profile。

## 5. Occlusion candidates

需分别研究/表达：

- ground object clipping；
- cliff face遮挡；
- unit foot与地面相交；
- building后遮挡；
- tree transparency；
- extra graphics与unit；
- binary mask；
- per-pixel z；
- renderer-only sample filtering。

同一 `OcclusionDescriptor` 不自动决定 pass；pass先分类，per-pixel policy在允许scope内执行。

## 6. Tree transparency

tree在单位后/前的透明化通常涉及 gameplay/UI visibility策略。它不能：

- 改 tree anchor；
- 改 unit occupancy；
- 把透明度写进 TMP depth；
- 以鼠标 hover状态改变 logical depth key。

## 7. Alpha 与 depth

需显式组合：

- alpha test/cutout；
- translucent blend；
- depth test；
- depth write；
- stable submission order；
- palette index zero transparency；
- shadow sample。

不能把所有非不透明像素当成“最后画”。

## 8. Shadow model

```text
ShadowDescriptor
- ShadowSource
- ShadowGeometry
- ShadowAnchor
- ShadowProjectionProfile
- ShadowColorProfile
- ShadowRenderPass
- ReceiverLayerPolicy
- CasterStableId
- Bounds
- Evidence
```

### `ShadowSource`

- TMP shadow candidate；
- SHP separated shadow frame；
- SHP in-frame palette/profile shadow；
- VXL generated/raster shadow；
- object `Shadow` property；
- terrain/static shadow；
- no-shadow。

### `ShadowGeometry`

raw indexed frame、future projected voxel silhouette、procedural polygon或none。当前专题不生成 geometry。

## 9. Shadow anchor/projection

- ground unit: ground contact；
- structure: authored foundation/ground anchor；
- aircraft: ground reference + altitude-aware separation；
- bridge entity: receiver layer显式；
- projectile/particle: effect policy；
- ramp: surface contact/profile。

shadow不决定caster occupancy或pathfinding。

## 10. Color 与 lighting

`ShadowColorProfile` 区分：

- palette shadow；
- alpha multiplied shadow；
- indexed non-zero mask；
- tint/opacity；
- local light interaction；
- global lighting；
- detail-level suppression。

社区资料显示不同 SHP family可能使用 separated frames或palette-index约定；这些是 `CommunityDocumented`，不能统一成一个raw format事实。

## 11. Shadow pass与排序

ShadowPass需要：

- receiver elevation layer；
- caster relationship；
- stable source/attachment ordinal；
- translucency policy；
- 是否depth test/write；
- ground/bridge deck选择。

它不能通过加减 camera pixel“追随”caster。

## 12. Missing/invalid inputs

- missing optional shadow：entity仍可呈现，diagnostic按policy；
- missing claimed frame：不伪造；
- invalid depth plane：保留raw parse failure/diagnostic；
- unknown depth samples：不clamp成Level；
- shadow bounds超限：culling safety diagnostic；
- VXL shadow不可用：不回退为occupancy rectangle，除非显式低保真profile。

## 13. Bounds

`ShadowBounds` 与 `VisualBounds`、`SelectionBounds`、`OccupancyBounds`独立。aircraft shadow在地面，caster visual bounds在空中，二者需分别cull。

## 14. Diagnostics

- `DepthPlaneMissing`
- `DepthPlaneTruncated`
- `DepthInterpretationUnresolved`
- `DepthSampleOutOfProfile`
- `PerPixelPolicyMissing`
- `OcclusionScopeConflict`
- `ShadowSourceMissing`
- `ShadowReceiverLayerMissing`
- `ShadowBoundsExceeded`
- `ShadowUsedForOccupancyRejected`

## 15. 项目决定

Core仅保存 raw plane与 interpretation descriptor；未来 renderer adapter决定 GPU depth、CPU mask、stencil或其他实现。Core不得保存 shader/material名称。
