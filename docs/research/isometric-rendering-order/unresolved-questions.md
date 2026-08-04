# Unresolved questions and evidence gates

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明的实现仅作行为与冲突参考，未复制、翻译或机械移植其代码、公式实现或测试夹具。`code_imported: false`。


## P0 — 实现前必须解决或显式选择 policy

| ID | 问题 | 当前证据 | 风险 | 关闭条件 |
|---|---|---|---|---|
| P0-01 | RA2/YR runtime精确投影矩阵、origin、map-size bias是否与FA2 editor完全一致？ | official editor formula | 全图平移/轴反转 | runtime source或明确project profile |
| P0-02 | negative coordinate、inverse picking与midpoint的精确rounding？ | editor float+cast；实现各异 | 边界cell错选 | 固定policy +独立fixture |
| P0-03 | logical tile metrics是否永远60×30，是否有theater/asset例外？ | editor与tools常见 | asset/map尺度漂移 | public corpus证据或strict configured profile |
| P0-04 | IsoMap Level的vanilla pixel step与所有family是否统一？ | editor H/2 | object/terrain错层 | runtime evidence或project policy |
| P0-05 | TMP HeightRaw的准确runtime语义与signedness？ | readers冲突/不完整 | 重复加高 | typed profile；默认不参与global position |
| P0-06 | RampTypeRaw如何影响object contact、vehicle pitch/roll和shadow？ | community/independent engine | 坡面漂浮/穿插 | adapter contract + evidence |
| P0-07 | original runtime的完整render pass与family priority？ | tools/independent renderers | 遮挡错误 | explicit project pass policy |
| P0-08 | exact depth comparator：screenY、cell、foundation edge、Z/YAdjust如何组合？ | community与实现分散 | flicker/错层 | source proof或configurable tuple |
| P0-09 | same-cell Unit/Infantry/Terrain/Structure的vanilla tie规则？ | underdocumented | nondeterminism | deterministic project tie policy |
| P0-10 | SHP frame X/Y offset与family ground anchor的准确绑定？ | raw offsets已知 | object整体偏移 | family anchor profile |
| P0-11 | VXL/HVA origin、body/turret/barrel pivot与ground anchor关系？ | renderer/tool-specific | turret错位 | VXL presentation binding research |
| P0-12 | building foundation origin、Foundation.X/Y和YSortAdjust符号/单位？ | community/extension docs | large building错层 | typed INI research + profile |
| P0-13 | custom/irregular foundation是否进入vanilla scope或仅extension？ | Ares/Phobos |错误兼容声明 | extension profile隔离 |
| P0-14 | TMP depth/extra depth sample的scale、compare、zero/out-of-range政策？ | implementations conflict | feet/cliff clipping错误 | renderer experiment/source；不由audit升级 |
| P0-15 | SHP shadow family：separated frame、palette mask、frame index选择？ | community/tool evidence | shadow丢失/错帧 | per-family ShadowSource profile |
| P0-16 | VXL shadow投影、cache和part selection？ | community flags | 多part shadow错误 | VXL shadow专项 |
| P0-17 | high bridge deck精确elevation、under/deck layer和destroyed transition？ | explicit layers in independent engine | bridge上下错层 | bridge semantic contract |
| P0-18 | aircraft authored placement到runtime altitude/landing状态的初始化关系？ | map record不足 | aircraft落点错 | simulation interface |
| P0-19 | aircraft ground shadow receiver是ground、water还是bridge deck？ | underdocumented | shadow穿桥 | receiver-layer policy |
| P0-20 | fog/shroud与shadow/aircraft/attached effects的visibility先后？ | renderer-specific |信息泄漏/视觉错误 | explicit visibility policy |
| P0-21 | `Size`/`LocalSize`与runtime camera clamp的关系？ | editor/clients不同 |滚屏边界错误 | camera专项，不由map parser猜 |
| P0-22 | translucent effects相对world depth与post-process的精确排序？ | renderer-specific |混合错误 | alpha/pass policy |
| P0-23 | source order是否具有vanilla视觉语义，还是仅稳定fallback？ |不完整 |save/load变化 | canonical source ordinal policy |
| P0-24 | duplicate placements是否应全部呈现、覆盖或reject？ |格式可表示 |不确定结果 | duplicate policy |
| P0-25 | damage/animation frame bounds变化时culling上界如何得到？ | asset-specific |弹出/裁剪 | bounded conservative profile |

## P1 — 可延后但必须保留扩展点

- local light与shadow color/opacity；
- detail level对particles/shadows/animations；
- minimap独立projection；
- spectator/replay camera；
- screen shake/letterbox；
- click alpha hit-testing；
- rally point与target line；
- projectile trail bounds；
- wall/gate/bib特殊pass；
- factory bay/docking/exit marker；
- high-DPI physical scale；
- odd-sized/custom logical tile metrics；
- future non-Unity software renderer。

## Evidence gates

任何结论升级必须满足：

- `ConfirmedByOfficialRuntimeSource`: 可审查、明确对应目标runtime路径；
- `ConfirmedByOfficialEditorSource`: 只描述editor；
- `ConfirmedByIndependentImplementation`: 标明目标引擎与差异；
- `CommunityDocumented`: 固定revision/topic；
- baseline只可 `ObservedByFutureProjectBaselineAudit`；
- project选择标为 `ConfiguredForProjectPolicy`；
- 无证据保持 `Unresolved`。

## 禁止的“关闭方式”

- 截图看起来对；
- 某一张地图不报错；
- Unity默认sorting能显示；
- 把OpenRA/WAE/CNCMaps行为称为vanilla；
- 用ProjectBaseline私有输出替代公开证据；
- 从图片尺寸反推foundation；
- 从shadow/depth反推occupancy；
- 用camera zoom修正depth。

## 建议首批决策

即使P0未全部获得runtime proof，项目可安全选择：

1. explicit RA2 editor-compatible projection profile；
2. 60×30 logical metrics和15px level step作为配置；
3. raw/derived分离；
4. stable tuple depth + canonical source ordinal；
5. explicit elevation layers；
6. authored foundation only；
7. raw depth plane + unresolved interpretation；
8. camera/UI/Unity adapter完全隔离。

这些必须标为 `ConfiguredForProjectPolicy`，不得宣称pixel-identical vanilla。
