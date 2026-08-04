# Source comparison and evidence register

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明实现仅作行为、格式事实与冲突参考，未复制、翻译、逐句改写或机械移植代码、寻路算法、Locomotor 实现或测试夹具。`code_imported: false`。


## 1. Evidence labels

本研究使用：

```text
ConfirmedByOfficialRuntimeSource
ConfirmedByOfficialEditorSource
ConfirmedByIndependentImplementation
CommunityDocumented
ObservedByFutureProjectBaselineAudit
ConfiguredForProjectPolicy
Unresolved
```

未找到完整公开的原版 RA2/YR runtime source，因此不得人为使用第一项确认核心移动算法。

## 2. Pinned source table

| Source | Pin / permanent URL | Paths / pages | License | Category | Relevant evidence | Shared lineage / limits |
|---|---|---|---|---|---|---|
| EA FinalSun / FinalAlert 2 | `6abf0f557469baea73079c6bf6550709e2e3584e` | `MissionEditor/MapData.h/.cpp`, `Structs.h`, `Defines.h`, `Tube.h/.cpp`, `IsoView.cpp`, `Loading.cpp` | GPL-3.0-or-later | official editor, reader, map tools | IsoMap fields, tile/height data, slope editor logic, object placement, Tube metadata | editor, not game runtime; some format code shares XCC lineage |
| XCC public repository | `6f91bf8b00d3acabb1be765118a37c0cb74e85ec` | TMP/ISO structures and readers | GPL headers / historical SourceForge lineage | reader/tool | TMP raw fields, IsoMap structures | not movement runtime; descendants must not be double-counted |
| OpenRA | `a520984d91eda9de48a62b1d15c1e3bad0d4fb1a` | `MapGrid.cs`, `Locomotor.cs`, `IPathGraph.cs`, `DensePathGraph.cs`, `TerrainTunnelLayer.cs`, `SubterraneanActorLayer.cs`, `ElevatedBridgeLayer.cs` | GPL-3.0-or-later | independent engine, simulation/pathfinding | separation of terrain speed, cost, occupancy flags, layers, bridge/tunnel candidates | reimplementation, not stock behavior; reference-only |
| World-Altering Editor | `b4c9481e9b00fb0a38739049a046f528b6054ce2` | TMP, theater, tile, map/object editors | GPL-3.0-or-later | editor/reader | modern semantic binding, terrain/ramp names, sparse maps | editor defaults and extensions are not runtime facts |
| CNCMaps | `afb9c1ec118f5128cbc1f3fb5e35c7dfa0e422fb` | map/TMP/overlay/render readers | repository MIT default with imported GPL exceptions | reader/renderer | coordinate, tile and overlay behavior | mixed provenance; no movement-runtime proof |
| MapTool | `f85f2226905496139f1258b5854fad915f9bbac6` | map record and object utilities | GPL-2.0-or-later | editor/reader | raw placement and map transformations | defaults/normalization are tool behavior |
| CnCNet XNA client | `e6e367bbe04c1a0dc1e34a8fed2856ea3ab7e8c4` | client/map consumers | GPL-3.0 | client/editor consumer | higher-level map usage | no load-bearing stock locomotor proof used |
| Chrono Divide SDK | `5943c4ae6c19897929d348a417d6d2f1481b75fd` | public SDK packages | project-specific/public repo terms | browser reimplementation | architecture and binding leads | no complete stock-compatible movement source treated as proof |
| openra2 / Vanguard | `8ba59f0bcd48ba0c89892c0455eeca7da4408f4c` | format packages | GPL-3.0 | reader/reimplementation | defensive format handling | explicitly shares XCC knowledge for some formats |
| ModEnc MovementZone | `https://modenc.renegadeprojects.com/MovementZone` | fixed community page | community wiki terms | documentation | candidate names, defaults, crusher/destroyer/path roles | not official runtime source |
| ModEnc SpeedType | `https://modenc.renegadeprojects.com/SpeedType` | fixed community page | community wiki terms | documentation | Foot/Track/Wheel/etc., product distinctions | not ordinal/runtime proof |
| ModEnc Locomotor | `https://modenc.renegadeprojects.com/Locomotor` | fixed community page | community wiki terms | documentation | CLSID aliases/families and community behavior | no algorithm copied |
| ModEnc LandTypes | `https://modenc.renegadeprojects.com/LandTypes` | fixed community page | community wiki terms | documentation | terrain percentages and SpeedType relationship | precedence/runtime details unresolved |
| Ares 3.0 docs | `https://ares-developers.github.io/Ares-docs/` | `new/buildings/custombuildingfoundations.html`, `new/buildings/gates.html` | documentation/source project terms | extension documentation | irregular foundations, gate extensions | extension-only, never vanilla |
| Phobos docs | `https://phobos.readthedocs.io/en/stable/Fixed-or-Improved-Logics.html` | fixed stable page | extension documentation | extension documentation | passable/buildable TerrainTypes and fixes | extension-only |
| PPM MovementZone discussion | `https://ppmforums.com/topic-36711/reqenable-multiple-movementzones-or-restore-canbeach/` | fixed topic | forum/community | community discussion | SpeedType/MovementZone conflict, WaterBeach/landing craft leads | conflicting anecdotes; TS/YR distinctions |
| PPM bridge overlay discussion | `https://ppmforums.com/topic-45898/info-on-bridge-overlay-types/` | fixed topic | forum/community | community investigation | high/low bridge piece/storage/elevation observations | reverse-engineering reports, not official proof |
| RA2 DIY | `https://bbs.ra2diy.com/forum.php?mod=viewthread&tid=18134` | 2021-07-12 fixed TMP terrain tutorial | forum/community terms | Chinese community tutorial | TMP terrain type、RampType、extra/depth responsibility leads | no runtime proof；reference-only |
| Vinifera / TS++ | `https://github.com/Vinifera-Developers/Vinifera`, TSpp submodule `95db6fa` | TS++ headers referenced indirectly by WAE/Vinifera | source/submodule license requires separate review | TS extension/research | ramp、land、movement naming leads | TS-only；not RA2/YR proof；reference-only |

所有实现型来源均 `reference-only`，`code_imported: false`。

## 3. Independence analysis

- EA editor的TMP/format部分可能复用XCC知识。
- openra2/Vanguard明确与XCC布局谱系相关。
- WAE、MapTool、CNCMaps会共享社区和既有工具知识。
- OpenRA是本研究中较强的独立架构证据，但其movement system是自身设计。
- ModEnc、PPM和RA2 DIY属于社区资料，彼此引用不构成独立runtime确认。
- Ares/Phobos是扩展实现，只能证明extension capability。

## 4. Cell and terrain comparison

| Topic | EA editor | WAE/tools | OpenRA | Community | Decision |
|---|---|---|---|---|---|
| IsoMap record identity | 11-byte raw/editor structure | dense/sparse variants | importer model | documented | preserve all raw records |
| TMP TerrainType | raw field/tool classification | typed enum candidates | maps to terrain indexes in importer/mod | land names documented | raw + profile binding |
| TMP RampType | editor slope/ramp tools | modern enum candidates | explicit ramp/corner model | TS++ tables | profile, no direct passable |
| TMP HeightRaw | raw TMP metadata | typed field | not identical to map height | conflicting descriptions | separate from Level |
| extra/depth planes | renderer/editor data | renderer import | renderer-specific | visual documentation | no movement meaning |
| missing art | editor may show placeholders | tool-specific | implementation-specific | — | does not erase surface identity |

## 5. Movement property comparison

| Topic | ModEnc/community | Independent implementation | Current decision |
|---|---|---|
| MovementZone names | broad candidate list including crusher/water/fly | engines use their own domain/capability models | preserve raw token; product profile |
| SpeedType names | Foot/Track/Wheel/Float/Winged/Hover/Amphibious/etc. | terrain-speed table concepts | preserve raw; bind table candidate |
| Locomotor | CLSID/alias family descriptions | engine-specific locomotor traits | opaque reference + capability candidate |
| defaults | community describes family-dependent defaults | implementation configs have defaults | strict=no default; compatibility profile optional |
| invalid values | anecdotal fallbacks | usually validation failure | unknown, never Normal |
| product differences | TS/RA2/YR/extension mixed | mod-specific | explicit applicability and evidence |

## 6. Occupancy comparison

OpenRA公开设计将 moving、stationary、movable、crushable、temporary、transit-only等cell状态分开，并把terrain cost与actor blocking分开。这支持多维合同，但不能证明Westwood内部位布局或优先级。

Ares custom foundations和gates、Phobos passable TerrainTypes证明扩展生态需要独立：

- authored foundation；
- movement blocker；
- buildability；
- dynamic gate；
- Terrain object passability。

它们不得无条件迁移到vanilla。

## 7. Bridge and tunnel comparison

| Topic | Official editor | OpenRA | Community | Decision |
|---|---|---|---|---|
| Tube metadata | start/end/direction parts | tunnel layer implementation | TS/YR reports | authored descriptor separate from graph |
| high bridge layer | editor/render knowledge | explicit elevated layer | overlay/storage/elevation reports | explicit deck/under layers |
| low bridge | overlay/editor representation | mod-specific | ground-type override reports | profile candidate |
| destruction | editor data only | implementation incomplete/different | reverse-engineering reports | runtime dynamic unresolved |
| Unit High | raw placement field | N/A/model differs | community usage | not sufficient topology |

## 8. Conflicts retained

P0 conflicts include：

- MovementZone负责terrain域还是只负责path restriction；
- SpeedType为0、missing entry和impassable的区别；
- `Subterrannean`拼写与runtime token；
- WaterBeach和FloatBeach的产品适用；
- low bridge是否只改变land type；
- high bridge高度与under-bridge edge；
- Tube在RA2/YR stock的支持程度；
- crusher/destroyer是否允许action-aware path；
- gate/open state的vanilla graph更新；
- exact foundation occupancy precedence。

## 9. License handling

- 不复制GPL switch、pathfinder、Locomotor或测试。
- 不逐句翻译社区wiki。
- 只记录事实、冲突、字段名称、抽象合同和evidence。
- synthetic fixtures必须独立手写。
- 未来代码实现需重新从合同出发独立完成。
