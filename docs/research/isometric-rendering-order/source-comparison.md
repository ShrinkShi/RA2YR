# Source comparison and conflict register

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明的实现仅作行为与冲突参考，未复制、翻译或机械移植其代码、公式实现或测试夹具。`code_imported: false`。


## 1. 固定来源

| Source | URL / pin / paths | License | category | shared lineage | reference-only | code_imported |
|---|---|---|---|---|---:|---:|
| EA FinalSun / FinalAlert 2 | `https://github.com/electronicarts/CNC_TS_and_RA2_Mission_Editor`; `6abf0f557469baea73079c6bf6550709e2e3584e`; `MissionEditor/MapData.h`, `Defines.h`, `Vec2.h`, `IsoView.cpp`, `Structs.h`, `inlines.h` | GPL-3.0-or-later | official editor | bundles/reuses XCC format code in parts | yes | false |
| XCC | public mirror `6f91bf8b00d3acabb1be765118a37c0cb74e85ec`; SHP/TMP/VXL readers | file headers GPL-3-or-later; historical release lineage separately noted | reader/tool | parent of several descendants | yes | false |
| OpenRA | `https://github.com/OpenRA/OpenRA`; `a520984d91eda9de48a62b1d15c1e3bad0d4fb1a`; `MapGrid.cs`, `WorldRenderer.cs`, `Viewport.cs`, `SpriteRenderable.cs`, `IsometricSelectable.cs`, `ElevatedBridgeLayer.cs` | GPL-3.0-or-later | independent engine/renderer | independent architecture; community format knowledge | yes | false |
| World-Altering Editor | `https://github.com/Rampastring/WorldAlteringEditor`; `b4c9481e9b00fb0a38739049a046f528b6054ce2`; map renderer/TMP/theater/object classes | GPL-3.0-or-later | editor/renderer | uses community/XCC-derived knowledge | yes | false |
| CNCMaps / ccmaps-net | `https://github.com/zzattack/ccmaps-net`; `afb9c1ec118f5128cbc1f3fb5e35c7dfa0e422fb`; renderer/object/SHP/VXL paths | repository MIT default with explicit GPL/OpenRA/XCC exceptions requiring file review | renderer | mixed lineage | yes | false |
| MapTool | `https://github.com/Starkku/MapTool`; `f85f2226905496139f1258b5854fad915f9bbac6` | GPL-2.0-or-later | map tool/reader | community knowledge | yes | false |
| CnCNet XNA client | public repository pin `e6e367bbe04c1a0dc1e34a8fed2856ea3ab7e8c4` | repository-specific; review per path | client/editor integration | may share client libraries | yes | false |
| Chrono Divide SDK | `https://github.com/chronodivide/mod-sdk`; `5943c4ae6c19897929d348a417d6d2f1481b75fd` | repository license | SDK/data contracts | gameplay engine not open source | yes; no low-level proof used | false |
| openra2 / Vanguard | `8ba59f0bcd48ba0c89892c0455eeca7da4408f4c` | GPL-3.0 | reader/engine | explicitly XCC-derived in relevant format path | yes | false |
| TS++ material | cited ramp definitions / `TIBSUN_DEFINES.H` lineage | requires per-source license review | extension/research | community/reverse-engineering | yes | false |
| ModEnc | fixed oldid pages for `ZAdjust`, animation Z/Y sort, foundations, bridges, shadows, aircraft flags | community content terms | community documentation | community | yes | false |
| Project Perfect Mod | fixed topic URLs for shadows, bridges, custom foundations, SHP/VXL behavior | forum posts | community documentation | community | yes | false |
| Ares docs | custom foundations and extension behavior, fixed release docs | project-specific | extension docs | community/reverse-engineering | yes | false |
| Phobos docs | foundation, rendering/selection/aircraft extension behavior, fixed revision | project-specific | extension docs | community/reverse-engineering | yes | false |
| RA2 DIY tutorials | fixed public tutorials, including ZAdjust coordinate discussions where available | site/community | community documentation | community | yes | false |

## 2. Independence warning

- EA editor的TMP/SHP/VXL format组件部分沿用XCC，二者一致不自动算两个独立runtime证明。
- openra2相关format reader明确继承XCC布局。
- WAE、CNCMaps、MapTool与community docs之间可能共享长期modding知识。
- OpenRA在render architecture上较独立，但仍是目标引擎实现，不是原版runtime。
- Ares/Phobos描述扩展行为，不能反推vanilla默认。
- Chrono Divide公开SDK不是公开renderer源码。

## 3. Projection comparison

| Topic | EA editor | OpenRA | other tools/community | decision |
|---|---|---|---|---|
| coordinate types | map vs projected显式类型 | cell/world/screen显式 | 通常隐式或工具特有 | Core必须domain-tagged |
| RA2 logical tile | 60×30 | rules/configurable tile size | 常见60×30 | profile candidate，不是每asset决定 |
| TS logical tile | 48×24 | config | 常见48×24 | product profile |
| projection X | `(-x+y+bias)*W/2` | world-to-screen pipeline | axis可能换名/交换 | matrix profile |
| projection Y | `(x+y-z)*H/2` | `(worldY-worldZ)*scale` | 常见sum-height | profile |
| level step | H/2 | world height scaling | community常见 | editor-confirmed候选 |
| inverse rounding | float + ±0.5/cast | own cell picking | underdocumented | unresolved/profile |
| camera | viewOffset/viewScale分开 | viewport/zoom分开 | varies | camera不得进logical depth |

## 4. Depth/order comparison

| Topic | Evidence | Conflict/limit | decision |
|---|---|---|---|
| screen/world Y | many renderers use as primary | bridges/air/ties不足 | only one component |
| explicit ZAdjust/YSort | community + tools | family-specific, signs/units冲突 | typed raw + policy |
| stable sort | OpenRA explicit | original runtime未知 | project mandatory |
| source order | implementations use insertion as stable fallback | load/thread order may不稳定 | canonical source ordinal |
| per-pixel TMP depth | several renderers | compare/scale/filter冲突 | raw plane + interpretation profile |
| image bottom | tool heuristics/community | frame变化会跳 | not ground anchor |
| foundation edge | building render heuristics | irregular/custom扩展 | authored foundation candidate |
| Unity SortingLayer | none as format evidence | engine-specific | adapter only |

## 5. Anchor/foundation conflicts

| Topic | Competing evidence | decision |
|---|---|---|
| SHP X/Y offsets | raw frame header/tool surfaces | preserve raw; binder-derived pivot separate |
| transparent crop | tools may trim/expand surfaces | must not change anchor |
| building base | foundation/base cell/art conventions | explicit profile; no image inference |
| custom foundation | Ares/Phobos/community | extension profile, not vanilla |
| VXL origin | voxel/HVA tools/renderers | preserve transform/origin; ground anchor separate |
| infantry subcell | scenario field + renderer offsets | explicit subcell profile |
| selection bounds | footprint or custom UI | UI-only, not occupancy |

## 6. Occlusion/shadow conflicts

| Topic | Evidence | decision |
|---|---|---|
| TMP depth sample meaning | reader/renderer agreement on existence, semantics differ | `Unresolved` interpretation |
| missing depth | files/flags vary | raw reader permits policy-defined absence |
| tree transparency | gameplay/render implementations | UI/visibility policy |
| SHP shadow | separated frames and palette conventions both documented | source profile per family |
| VXL shadow | renderer-generated/cache flags documented | future adapter |
| shadow color | mask/palette/alpha variants | color profile |
| shadow receiver | ground/deck/ramp/aircraft | explicit receiver layer |

## 7. Bridge/aircraft conflicts

- bridge overlays、TMP bridge art、deck state与pathfinding不是一个数据源；
- official editor把某些bridge overlay特殊分类，但runtime topology未公开；
- independent engines使用custom movement/elevation layer；
- aircraft map placement只证明initial object record，不证明runtime altitude；
- exact vanilla bridge deck elevation、aircraft shadow offset和draw pass仍 `Unresolved`。

## 8. License boundary

本研究只记录事实、冲突、接口责任与独立设计要求。未来实现：

- 不从GPL source逐行改写；
- 不翻译 comparator/projection renderer代码；
- 不复用公开实现的fixture；
- synthetic expected值由独立手算/规范表产生；
- 每个 imported data table单独审查版权与许可证；
- `code_imported: false`。

## 9. Evidence grading examples

- EA editor精确公式：`ConfirmedByOfficialEditorSource`。
- OpenRA stable sort与elevated bridge layer：`ConfirmedByIndependentImplementation`。
- ModEnc/PPM flags和经验：`CommunityDocumented`。
- 本专题的tuple key、domain model和policy：`ConfiguredForProjectPolicy`。
- 原版runtime exact comparator：`Unresolved`。
