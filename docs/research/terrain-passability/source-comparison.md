# Source comparison and evidence register

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明实现仅作行为、格式事实与冲突参考，未复制、翻译、逐句改写或机械移植代码、寻路算法、Locomotor 实现或测试夹具。`code_imported: false`。


## 1. Formal evidence labels

本研究使用九项封闭集合：

```text
ConfirmedByOriginalRuntimeSource
ConfirmedByOfficialToolSource
ConfirmedByMultipleIndependentImplementations
ConfirmedCommunityConvention
ImplementationSpecificBehavior
DefensiveDesign
ConflictingSources
Underconfirmed
Unresolved
```

未找到完整公开的原版RA2/YR runtime source，因此本专题没有claim达到`ConfirmedByOriginalRuntimeSource`。当前也没有充分证明多个实现格式发现谱系独立，因此不使用`ConfirmedByMultipleIndependentImplementations`。

Future ProjectBaseline work单独记录：

```text
AuditStatus: NotRun
FutureEvidenceSource: ProjectBaselineAggregateAudit
```

## 2. Pinned source table

| Source | Pin / permanent URL | Paths / pages | License | Category | Relevant evidence | Shared lineage / limits |
|---|---|---|---|---|---|---|
| EA FinalSun / FinalAlert 2 | `6abf0f557469baea73079c6bf6550709e2e3584e` | `MissionEditor/MapData.h/.cpp`, `Structs.h`, `Defines.h`, `Tube.h/.cpp`, `IsoView.cpp`, `Loading.cpp` | GPL-3.0-or-later | official editor, reader, map tools | IsoMap fields, tile/height data, slope editor logic, object placement, Tube metadata | editor, not game runtime; some format code shares XCC lineage |
| XCC public repository | `6f91bf8b00d3acabb1be765118a37c0cb74e85ec` | TMP/ISO structures and readers | GPL headers / historical SourceForge lineage | reader/tool | TMP raw fields, IsoMap structures | not movement runtime; descendants must not be double-counted |
| OpenRA | `a520984d91eda9de48a62b1d15c1e3bad0d4fb1a` | `MapGrid.cs`, `Locomotor.cs`, `IPathGraph.cs`, `DensePathGraph.cs`, `TerrainTunnelLayer.cs`, `SubterraneanActorLayer.cs`, `ElevatedBridgeLayer.cs` | GPL-3.0-or-later | target engine, simulation/pathfinding | separation of terrain speed, cost, occupancy flags, layers, bridge/tunnel candidates | reimplementation, not stock behavior; reference-only |
| World-Altering Editor | `b4c9481e9b00fb0a38739049a046f528b6054ce2` | TMP, theater, tile, map/object editors | GPL-3.0-or-later | editor/reader | modern semantic binding, terrain/ramp names, sparse maps | editor defaults and extensions are not runtime facts |
| CNCMaps | `afb9c1ec118f5128cbc1f3fb5e35c7dfa0e422fb` | map/TMP/overlay/render readers | repository MIT default with imported GPL exceptions | reader/renderer | coordinate, tile and overlay behavior | mixed provenance; no movement-runtime proof |
| MapTool | `f85f2226905496139f1258b5854fad915f9bbac6` | map record and object utilities | GPL-2.0-or-later | editor/reader | raw placement and map transformations | defaults/normalization are tool behavior |
| CnCNet XNA client | `e6e367bbe04c1a0dc1e34a8fed2856ea3ab7e8c4` | client/map consumers | GPL-3.0 | client/editor consumer | higher-level map usage | no load-bearing stock locomotor proof used |
| Chrono Divide SDK | `5943c4ae6c19897929d348a417d6d2f1481b75fd` | public SDK packages | project-specific/public repo terms | browser reimplementation | architecture and binding leads | no complete stock-compatible movement source treated as proof |
| openra2 / Vanguard | `8ba59f0bcd48ba0c89892c0455eeca7da4408f4c` | format packages | GPL-3.0 | reader/reimplementation | defensive format handling | explicitly shares XCC knowledge for some formats |
| ModEnc MovementZone | permanent community page | MovementZone | community wiki terms | documentation | candidate names, defaults, crusher/destroyer/path roles | not official runtime source |
| ModEnc SpeedType | permanent community page | SpeedType | community wiki terms | documentation | Foot/Track/Wheel/etc., product distinctions | not ordinal/runtime proof |
| ModEnc Locomotor | permanent community page | Locomotor | community wiki terms | documentation | CLSID aliases/families and community behavior | no algorithm copied |
| ModEnc LandTypes | permanent community page | LandTypes | community wiki terms | documentation | terrain percentages and SpeedType relationship | precedence/runtime details unresolved |
| Ares docs | fixed docs | custom foundations, gates | project terms | extension documentation | irregular foundations, gate extensions | extension-only, never vanilla |
| Phobos docs | fixed stable docs | passable/buildable TerrainTypes | extension terms | extension documentation | extension fixes | extension-only |
| PPM discussions | fixed topics | MovementZone and bridges | forum/community | community investigation | product conflicts and bridge observations | anecdotes/reverse engineering, not official proof |
| RA2 DIY | fixed tutorial | TMP terrain tutorial | community terms | Chinese community tutorial | TMP terrain, RampType, extra/depth responsibility leads | no runtime proof |
| Vinifera / TS++ | fixed repositories/submodule | ramp, land and movement naming | source license requires separate review | TS extension/research | TS-specific candidates | not RA2/YR proof |

所有实现型来源均`reference-only`，`code_imported: false`。

## 3. Independence analysis

- EA editor的TMP/format部分可能复用XCC知识。
- openra2/Vanguard明确与XCC布局谱系相关。
- WAE、MapTool、CNCMaps会共享社区和既有工具知识。
- OpenRA的movement system是自身目标引擎设计；它是`ImplementationSpecificBehavior`，不是原版runtime证据。
- ModEnc、PPM和RA2 DIY彼此引用或共享modding知识，不构成多个独立runtime确认。
- Ares/Phobos是extension实现，只能证明其命名extension capability。

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

| Topic | Community | Public implementations | Current decision |
|---|---|---|---|
| MovementZone names | broad candidate list including crusher/water/fly | engines use their own domain/capability models | preserve raw token; product profile |
| SpeedType names | Foot/Track/Wheel/Float/Winged/Hover/Amphibious/etc. | terrain-speed table concepts | preserve raw; bind table candidate |
| Locomotor | CLSID/alias family descriptions | engine-specific locomotor traits | opaque reference + capability candidate |
| defaults | community describes family-dependent defaults | implementation configs have defaults | strict=no default; compatibility profile optional |
| invalid values | anecdotal fallbacks | usually validation failure | unknown, never Normal |
| product differences | TS/RA2/YR/extension mixed | mod-specific | explicit applicability and evidence |

## 6. Occupancy comparison

OpenRA公开设计将moving、stationary、movable、crushable、temporary、transit-only等cell状态分开，并把terrain cost与actor blocking分开。该行为为`ImplementationSpecificBehavior`，支持多维合同设计比较，但不能证明Westwood内部位布局或优先级。

Ares custom foundations和gates、Phobos passable TerrainTypes为命名extension行为。它们证明扩展生态需要区分authored foundation、movement blocker、buildability、dynamic gate和Terrain object passability，但不得无条件迁移到vanilla。

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

这些直接差异使用`ConflictingSources`；存在候选但证据不足的跨工具收敛使用`Underconfirmed`。

## 9. Normalized evidence summary

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert公开raw tile/height/terrain/slope/Tube编辑行为 | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | 只确认official tool。 | 保留raw输入和editor profile。 | `NotRun` |
| OpenRA terrain cost、occupancy、tunnel、subterranean和bridge layers | `ImplementationSpecificBehavior` | OpenRA | 单一目标引擎实现。 | 仅作comparison profile。 | `NotRun` |
| ModEnc/PPM/RA2 DIY长期terrain和movement约定 | `ConfirmedCommunityConvention` | Fixed community sources | 不确认runtime完整性或优先级。 | product applicability显式。 | `NotRun` |
| 多工具分离surface、speed、occupancy和graph的趋势 | `Underconfirmed` | Public tools/community | 谱系独立性和runtime适用性不足。 | 不按来源数量提升。 | `NotRun` |
| TerrainType/Rules/Overlay优先级、MovementZone defaults、bridge和cost规则 | `ConflictingSources` | Tools/community/extensions | 来源直接分歧。 | 保留冲突，不自动修复。 | `NotRun` |
| exact stock Locomotor/path/cost/dynamic occupancy算法 | `Unresolved` | No original-runtime source located | 无可靠完整候选。 | future simulation/pathfinding adapter负责。 | `NotRun` |
| raw preservation、multi-state query、checked cost和fail-closed | `DefensiveDesign` | Project policy | 项目保真与安全策略。 | Policy列记录行为。 | `NotRun` |

## 10. License handling

- 不复制GPL switch、pathfinder、Locomotor或测试。
- 不逐句翻译社区wiki。
- 只记录事实、冲突、字段名称、抽象合同和evidence。
- synthetic fixtures必须独立手写。
- 未来代码实现需重新从合同出发独立完成。
