# Anchors, foundations and bounds

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明的实现仅作行为与冲突参考，未复制、翻译或机械移植其代码、公式实现或测试夹具。`code_imported: false`。


## 1. Anchor vocabulary

| 类型 | 定义 | 来源 | 不得替代 |
|---|---|---|---|
| `AssetFrameOrigin` | frame local coordinate origin | SHP/TMP/VXL reader | ground anchor |
| `LogicalGroundAnchor` | entity接触地图/参考地面的逻辑点 | placement + family policy | image center |
| `RenderPivot` | raster/part围绕其绘制或旋转的点 | asset + binder | simulation position |
| `SimulationPosition` | runtime authoritative position | simulation snapshot | raw frame offset |
| `FoundationOrigin` | authored footprint坐标原点 | Rules/Art typed view | frame bounds |
| `SelectionAnchor` | UI decoration参考 | UI adapter | occupancy |
| `ShadowAnchor` | shadow投影参考 | shadow profile | caster center |
| `MuzzleAnchor` | projectile/effect发射点 | Art/HVA/attachment | unit ground anchor |

所有 anchor 都记录 domain、units、source、policy、evidence。

## 2. TMP anchor

TMP base cell候选：

- logical anchor属于 map cell；
- `sX/sY` 或 extra rectangle offset属于 `TmpLocalCoordinate`；
- extra graphics可越界，但不移动 cell identity；
- `HeightRaw`/depth plane不自动改 anchor；
- base diamond top-left、center和ground reference需由 TMP binding profile区分。

## 3. SHP frame offsets

公开 editor/reader保留每 frame 的 X/Y offset、drawn width/height和全 surface bounds。Core 必须保留原值：

```text
RawFrameBounds
RawFrameOffset
DecodedPixelWindow
UntrimmedCanvasMetrics?
FrameIndex
```

规则：

- 透明像素自动裁剪不得改 raw offset；
- frame bounds变化不得改 logical ground anchor；
- negative offset合法候选，参与 visual bounds；
- missing Art不可通过偷偷移动对象补偿；
- damaged、turret、shadow frame都保留自己的 bounds。

## 4. SHP anchor candidate

family binder选择：

```text
LogicalGroundAnchor
+ AuthoredFrameOffset
+ FamilyPivotProfile
+ ExplicitArtOffset
→ RenderPivot / VisualRect
```

图片中心只能作为明确配置的 fallback，且必须产生 diagnostic，不能作为默认事实。

## 5. VXL/HVA boundary

需要保留：

- `VisualAssetId`
- body/turret/barrel part role；
- voxel bounds；
- HVA frame transform；
- pivot/origin；
- facing；
- pitch/roll candidate；
- renderer raster output bounds；
- authored ground anchor；
- shadow source候选。

VXL bounds随rotation变化；`ConservativeCullingBounds`可取所有允许pose的上界，但 occupancy仍来自 simulation/foundation。

## 6. Building foundation

必须分开：

| 数据 | 责任 |
|---|---|
| authored `Foundation` | Rules/Art或extension typed view |
| `Foundation.X/Y` candidate | foundation origin/offset |
| simulation occupancy | simulation |
| buildability | rules/simulation |
| pathfinding footprint | pathfinding |
| renderer base extent | presentation |
| selection extent | UI |
| shadow extent | shadow system |
| art pixel bounds | asset |

绝不从 SHP/VXL 图片宽高推导 foundation。

## 7. Foundation variants

研究候选：

- rectangular `1x1`, `2x2`, `3x2`等；
- `Foundation.X/Y`偏移；
- bib/smudge独立 visual entity；
- wall/gate；
- factory bay、exit cell、dock；
- upgrade attachment；
- custom/irregular foundation扩展。

custom irregular形状必须保存 authored cell set与origin；不能压成 bounding rectangle 后丢失孔洞/缺口。扩展语义标记来源，不冒充 vanilla。

## 8. Foundation 与 draw depth

building family可选择：

- foundation最靠近屏幕的边；
- authored base point；
- cell anchor；
- explicit YSort/ZAdjust；

作为 primary depth candidate。选择必须写进 `DepthOrderingPolicy`。image bottom仅可作为 visual bounds，不自动覆盖 foundation anchor。

## 9. Bounds types

```text
RawFrameBounds
VisualBounds
ConservativeCullingBounds
SelectionBounds
ClickHitBounds
ShadowBounds
OccupancyBounds
FoundationBounds
AttachmentAggregateBounds
```

不得压成一个 `Rectangle`。

## 10. Dynamic bounds

需处理：

- damage frame尺寸变化；
- animation frame变化；
- turret/barrel超出body；
- VXL旋转；
- recoil；
- projectile trail；
- TMP extra graphics；
- aircraft altitude与shadow分离；
- selection decoration独立。

`VisualBounds`可逐frame变化；`ConservativeCullingBounds`必须安全覆盖；`LogicalGroundAnchor`保持稳定。

## 11. Click 与 drag selection

click/drag可以使用：

- dedicated hit polygon；
- selection bounds；
- visual alpha hit test候选；
- footprint-derived fallback。

这些是 UI policy，不回写 foundation/occupancy。OpenRA公开实现也把 mouse bounds 与 decoration bounds分开，属于独立实现证据。

## 12. Wall、gate与factory

- wall connectivity由overlay/building semantic binder或simulation产生；
- gate open/closed bounds变化不改变 authored foundation；
- factory bay和exit cell是行为/attachment数据；
- docking point不等于 render pivot；
- bib/smudge可在独立 pass，但不能扩张 occupancy，除非simulation另有 authored规则。

## 13. Selection bracket

`SelectionAnchor` 可来自：

- foundation projected polygon；
- explicit UI bounds；
- visual conservative bounds；
- family profile。

不得用 selection bracket“修正”对象渲染位置。

## 14. Diagnostics

- `GroundAnchorMissing`
- `ImageCenterFallbackUsed`
- `RawFrameOffsetAlterationRejected`
- `FoundationMissing`
- `FoundationInferredFromImageRejected`
- `IrregularFoundationCollapsed`
- `SelectionBoundsUsedAsOccupancyRejected`
- `DynamicFrameOutsideConservativeBounds`
- `VxlRotationBoundsExceeded`
- `AttachmentBoundsParentMissing`

## 15. Evidence

- SHP frame offset/bounds及 TMP local offsets：`ConfirmedByOfficialEditorSource` + reader evidence。
- foundation与custom foundation行为：`CommunityDocumented` / independent implementation；精确 vanilla runtime规则部分 `Unresolved`。
- 多 bounds Core模型：`ConfiguredForProjectPolicy`。
