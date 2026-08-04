# Render entity families and passes

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明的实现仅作行为与冲突参考，未复制、翻译或机械移植其代码、公式实现或测试夹具。`code_imported: false`。


## 1. 为什么不能用万能 `SpriteEntity`

不同 family 的 authored anchor、资产格式、palette、depth、遮挡、shadow、attachment 和 simulation 依赖不同。统一接口可以存在，但必须保留 family-specific descriptor。

## 2. Entity family matrix

| Family | logical anchor | bounds | base pass候选 | palette/remap | depth inputs | occlusion | shadow source | simulation dependency | evidence |
|---|---|---|---|---|---|---|---|---|---|
| TMP ground cell | cell base | base diamond | GroundColor | theater ISO palette | cell projection, Level, depth plane | receiver/occluder candidate | TMP/none | none | official editor + implementations |
| TMP extra graphics | same cell + TMP local offset | extra rect | TerrainObject/ground-extra | ISO palette | cell depth + extra depth | strong candidate | TMP candidate | none | implementations |
| Overlay below-unit | overlay cell | SHP/frame | OverlayBelowUnit | theater/overlay palette | cell + type/data + policy | candidate | type/art | none | community + tools |
| Overlay object | overlay cell | frame | TerrainObject | type palette/remap | cell + explicit class | candidate | art | none | implementations |
| Smudge/decal | cell | frame/decal | SmudgeDecal | theater palette | usually cell base | normally receiver-only | none | none | official editor |
| Terrain object | authored cell | SHP ground anchor | TerrainObject | theater/unit profile | bottom/anchor + ZAdjust | participates | SHP/art | none | official editor + community |
| Structure foundation | FoundationOrigin | footprint geometry | Foundation/Bib | theater/remap | foundation bottom edge | receiver | optional | authored foundation | community + implementations |
| Structure body | building ground anchor | dynamic frame bounds | BuildingBody | unit palette + remap | anchor/bottom + Z/Y adjustments | participates | SHP shadow frames/property | runtime damage/owner |
| Unit body | unit foot/ground | SHP/VXL bounds | GroundActor | unit palette/remap | contact + facing + ZAdjust | participates | SHP/VXL | pose/facing |
| Infantry | subcell foot | SHP frame bounds | GroundActor | unit palette/remap | subcell contact + stable ordinal | participates | SHP/in-frame profile | facing/action |
| Aircraft | ground reference + altitude | SHP/VXL bounds | AirActor | unit palette/remap | air layer + altitude | special | ground shadow | altitude/state |
| SHP animation | parent or map anchor | frame bounds | Effects/AttachedAnimation | palette/remap profile | parent depth + explicit adjust | policy | shadow frame/property | frame/tick |
| VXL body | ground anchor | transformed bounds | GroundActor/AirActor | voxel palette/remap | transformed contact + part role | participates | voxel shadow | facing/pitch/roll |
| turret | parent pivot | transformed bounds | parent/attachment | unit palette/remap | parent key + attachment ordinal + ZAdjust | parent policy | optional | turret facing |
| barrel | turret/barrel pivot | transformed bounds | parent/attachment | unit palette/remap | turret key + ordinal | parent policy | optional | recoil |
| shadow | ShadowAnchor | shadow bounds | Shadow | shadow profile | receiver layer + caster relation | not occupancy | n/a | caster pose |
| projectile | runtime position/muzzle | dynamic | Effects | effect palette | explicit altitude + projectile pass | policy | optional | runtime trajectory |
| particle | emitter/runtime | dynamic conservative | Effects | effect profile | emitter + explicit pass | usually no ground occlusion | optional | runtime |
| bridge underlay | bridge authored cells | piece bounds | TerrainObject/UnderBridge | theater/overlay | UnderBridgeLayer | participates | bridge profile | bridge state |
| bridge deck | deck anchor | deck bounds | HighBridgeDeck | theater/overlay | BridgeDeckLayer + deck ordinal | participates | bridge profile | damage/elevation |
| UI marker | Selection/UI anchor | UI bounds | UIAnnotation | UI color | no world depth or explicit overlay | none | none | selection state |
| selection | SelectionAnchor | SelectionBounds | UIAnnotation | owner/UI | separate UI priority | none | none | UI |
| health bar | UI anchor | UI rectangle | UIAnnotation | UI/owner | separate UI stack | none | none | health snapshot |
| debug overlay | chosen diagnostic domain | diagnostic bounds | Debug | UI | no logical depth | none | none | diagnostics |

## 3. Pass candidate

```text
GroundColor
GroundDepthInputs
SmudgeDecal
OverlayBelowUnit
TerrainObject
FoundationOrBib
BuildingBody
GroundActor
HighBridgeDeck
AirActor
Shadow
Effects
FogAndShroud
UIAnnotation
Debug
```

这不是 Unity SortingLayer 表，也不是已证实的原版 draw list。它是 `RenderPassPolicy` 的项目候选。

## 4. 必须分开的维度

| 维度 | 回答的问题 |
|---|---|
| pass | 进入哪个大阶段 |
| depth key | 同一可排序阶段的先后 |
| explicit layer | ground/bridge/under/air 等拓扑关系 |
| per-pixel depth | 一个 raster 内部如何与 receiver比较 |
| alpha/translucency | 混合与提交顺序 |
| palette/remap | 像素如何着色 |
| post-process | fog/shroud/lighting/屏幕后处理 |
| UI annotation | 是否脱离 world depth |

`Shadow` pass 的顺序不代表 shadow 可占地；`GroundDepthInputs` 不等于画一张可见 depth sprite。

## 5. Parent/attachment

body、turret、barrel、active animation、muzzle effect 必须通过显式 attachment graph：

```text
ParentStableId
AttachmentRole
AttachmentOrdinal
AuthoredPivot
RuntimeTransformSnapshot
InheritedPassPolicy
InheritedDepthPolicy
IndependentBounds
```

防止：

- turret 依赖创建顺序；
- barrel 与 body exact tie 随机；
- attached animation 被另一个 cell entity插入；
- damage frame换 bounds 后改变 ground anchor。

## 6. Palette 与 remap

palette/remap只影响颜色解释，不改变：

- cell坐标；
- ground anchor；
- foundation；
- depth key；
- pass；
- occupancy。

SHP indexed pixels、TMP ISO palette、VXL palette/normal lighting需要不同 binding profile。

## 7. Effects 与 translucency

projectile、particle、animation可有：

- explicit pass；
- parent-relative key；
- independent world anchor；
- alpha mode；
- depth write/test policy；
- fog/shroud visibility policy。

不能以“有 alpha”自动置于所有 world entity 之后；translucency sorting需单独 policy。

## 8. Fog/shroud

fog/shroud 是 visibility/post-process 边界。它可过滤 world pass，也可有 above-shroud overlay；不得：

- 改 raw map；
- 改 entity stable identity；
- 改 depth key；
- 把不可见 entity从 logical presentation集合永久删除。

## 9. UI separation

selection bracket、health bar、veterancy、hover label、target line、rally point、command cursor、debug geometry不进入 map parser，也不能作为 render/foundation/occupancy bounds 的校正依据。

## 10. Evidence grade

- 公开 renderer 对 terrain、world actor、shroud、overlay、annotation分阶段：`ConfirmedByIndependentImplementation`。
- EA editor 对 TMP、overlay、object、UI绘制存在分工：`ConfirmedByOfficialEditorSource`。
- 本 pass 枚举和 family contracts：`ConfiguredForProjectPolicy`。
- 原版 runtime 的精确 pass list与相互顺序：`Unresolved`。
