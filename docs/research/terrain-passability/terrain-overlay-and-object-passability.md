# Terrain, Overlay and object passability

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明实现仅作行为、格式事实与冲突参考，未复制、翻译、逐句改写或机械移植代码、寻路算法、Locomotor 实现或测试夹具。`code_imported: false`。


## 1. Binding chain

通行性不能从任一单独字段直接得出。候选链为：

```text
IsoMap raw cell
→ GlobalTileId / SubTile candidate
→ TMP raw cell fields
→ theater TileSet role
→ semantic surface candidates
→ Overlay family candidate
→ Terrain/Smudge/Structure candidates
→ locomotor capability query
→ passability state and cost candidates
```

任何阶段失败都必须保留上游 raw identity 与 diagnostics，不得用“看起来合理”的默认值完成绑定。

## 2. TMP terrain fields

### 2.1 `TerrainTypeRaw`

`TerrainTypeRaw` 是 byte-sized raw input。公开工具通常把它映射到 land/terrain 类别，但缺少完整原版 RA2/YR runtime source，因此 Core 应保存：

```text
TmpTerrainTypeRaw
SignedCandidate?
UnsignedCandidate
MappedLandTypeCandidates
ProfileId
EvidenceGrade
UnknownValueState
```

禁止：

- unknown value改成 Clear；
- 仅凭数值名建立最终 cost；
- 把 WAE/OpenRA 的枚举序号称为 stock runtime事实；
- 让该字段覆盖 theater role、Overlay或Rules输入。

### 2.2 `RampTypeRaw`

它是 surface geometry / transition 的候选输入，不是独立 `Passable`。需要与：

- IsoMap `LevelRaw`；
- 相邻cell；
- selected ramp profile；
- movement capability；
- bridge/shore特殊边；
- malformed transition policy

共同分析。

### 2.3 `HeightRaw`

`HeightRaw` 与 IsoMap `LevelRaw` 必须分离。候选用途包括TMP-local surface metadata、renderer或工具语义；不得自动相加、替代或反推地图Level。

### 2.4 flags、extra graphics、depth planes

默认冻结：

| Input | Movement meaning |
|---|---|
| extra graphics | visual extension；不自动等于cliff或blocker |
| color plane | indexed visual data；不决定movement |
| depth plane | renderer occlusion/depth input；不决定passability |
| damaged-data candidate | raw optional plane；不自动改变movement |
| unknown flag bits | preserve + diagnose；不推导terrain |
| missing optional plane | parser/profile问题；不改变surface role |

`depth bytes != movement height`。

## 3. Missing and invalid TMP bindings

分别报告：

```text
MissingGlobalTileBinding
MissingTmpAsset
MissingSubTile
SubTileOutOfRange
MissingVariation
MalformedTmpCell
UnknownTerrainTypeRaw
UnknownRampTypeRaw
```

默认行为不是“不可通行”，也不是“可通行”。应输出 `PassabilityState.Unknown`，由调用者选择 fail-closed、editor-preview 或 diagnostic-only policy。

Missing TMP不得：

- 改变后续 GlobalTileId；
- 删除raw map cell；
- 删除Overlay或occupancy；
- 根据相邻图片补画后决定地形。

## 4. Theater TileSet roles

候选逻辑角色：

```text
Clear
Rough
Sand
Road
Pave
Water
Shore
Cliff
Ramp
Bridge
Tunnel
Ice
Rail
LatTransition
CustomExtension
Unknown
```

角色来自 theater control INI、TileSet registry、显式extension profile和TMP raw字段的组合。文件名文本只能作为明确低等级 `FilenameHeuristicProfile`，不能作为默认事实。

严格区分：

```text
TileSetLogicalRole
TmpTerrainTypeRaw
RulesLandType
SpeedTypeRaw
MovementZoneRaw
LocomotorCapability
FinalPassability
FinalCost
```

LAT transition是TileSet关系，不等于一种唯一movement type；transition tile可能仍需其ground/base角色决定语义。

## 5. Rules land and speed tables

社区文档显示 `[LandTypes]`/Rules terrain speed百分比会按 `SpeedType` 选择不同值；该长期约定为`ConfirmedCommunityConvention`，不是完整runtime证明。

推荐：

```text
LandTypeBindingCandidate
- RawName
- RegistryOrdinalCandidate
- SpeedTableEntriesRaw
- BuildableCandidate
- ExtensionSource
- EvidenceGrade
```

百分比为0、缺失、负值、超范围值和extension值必须分开处理。不得把“缺失entry”静默视为100%。

## 6. Overlay families

每个Overlay先绑定family，再解释 `OverlayDataRaw`：

| Family | Visual role | Passability candidate |
|---|---|---|
| Empty | none | no overlay contribution |
| Resource | stage/frame/value | usually not universal blocker；policy required |
| Wall/Fence | connection + damage | blocker/destructible/crushable candidates |
| Gate | connection + dynamic state | static type + runtime open/closed state |
| Bridge | piece/state/damage | movement-layer topology input |
| Rock/Debris | frame | blocker/crush/destructible candidate |
| Crate | pickup visual/state | temporary occupancy candidate |
| Veins | growth/state | extension/runtime candidate |
| Track/Rail | visual/network | usually surface modifier, not universal blocker |
| Tunnel/Teleporter | marker | explicit special-edge candidate |
| Unknown | raw | `Unknown`, never assumed blocking/nonblocking |

禁止所有非空Overlay统一阻塞，也禁止所有resource统一可走。

## 7. OverlayData boundaries

`OverlayDataRaw` 只有family-specific解释：

```text
ResourceStageCandidate
WallConnectionCandidate
BridgeStateCandidate
FrameCandidate
DamageStateCandidate
UnknownRaw
```

缺失 OverlayData时：

- 不合成0；
- 不使用visual frame猜movement；
- 不根据Art是否存在删除blocker；
- 产生 `MissingOverlayData` diagnostic。

动态状态不得写回raw array。

## 8. Terrain objects

`[Terrain]` placement只产生type+cell candidate。类型绑定可能提供：

```text
StaticBlocker
DestructibleBlocker
CrushableBlocker
PassableDecoration
LightEmitterOnly
CustomPolicy
Unknown
```

树、岩石、路灯等名称不是格式事实。图片bounds、透明像素、SHP尺寸和selection bounds均不能定义occupancy。

Art缺失时仍保留placement和type-binding结果。

## 9. Smudge

默认合同：

```text
SmudgeVisualPresence != MovementBlocker
```

crater、scorch、bib、runtime generated smudge首先是decal/state候选。只有显式Rules/extension policy才能增加movement或buildability影响。

必须区分：

- authored smudge；
- building bib；
- runtime crater/scorch；
- visual frame；
- surface modifier；
- blocker candidate。

## 10. Structures

Structure placement经过Rules foundation binding后产生 `FoundationOccupancyDescriptor`，而不是由图片决定。

候选贡献：

```text
FoundationCells
PathBlockingMask
BuildabilityMask
PassableCells
GateCells
FactoryExitCells
DockCells
UpgradeAttachmentCells
DynamicStateReference
```

foundation cell不必全部永久阻塞。

## 11. Units, infantry and aircraft

地图placement只表示 authored initial state：

- Infantry可带SubCell candidate；
- Unit current runtime cell会变化；
- Aircraft placement不等于air graph node；
- landed aircraft、subterranean状态和bridge high状态属于runtime snapshot。

不得把initial placement永久固化到静态graph。

## 12. Passability result

推荐多状态：

```text
Passable
TraversableWithCost
Blocked
RequiresCapability
TemporarilyBlocked
DestructibleBlocker
Unknown
```

并附加：

```text
SurfaceContribution
OverlayContribution
StaticOccupancyContribution
DynamicOccupancyReference
RequiredCapabilities
ActionRequired
CostCandidates
EvidenceGrade
Diagnostics
```

## 13. Buildability separation

以下均合法：

- ground unit可走但不可建；
- cell可建但某locomotor不可走；
- naval unit可走但ground building不可建；
- bridge deck可走但不可建；
- surface passable但被temporary blocker占用；
- foundation中部分cell可穿越；
- resource阻止placement但不阻止movement。

不得共享一个 `Walkable/Buildable` bool。

## 14. Renderer separation

以下输入不自动影响movement：

- visual cliff face；
- TMP depth；
- shadow；
- local light；
- fog/shroud；
- palette颜色；
- selection/click bounds；
- aircraft ground shadow；
- damage frame pixel shape。

visual bridge与movement bridge必须是不同descriptor。

## 15. Evidence summary

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert公开raw tile/height/terrain字段 | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | 只确认official editor输入。 | 保留raw values。 | `NotRun` |
| 单个公开实现分离terrain types、cost和occupancy | `ImplementationSpecificBehavior` | Named tools/target engines | 单一实现行为。 | 单独记录source profile。 | `NotRun` |
| 多工具分离terrain、speed、overlay和occupancy的趋势 | `Underconfirmed` | Public tools/community | 谱系独立性和runtime适用性不足。 | 不按工具数量提升。 | `NotRun` |
| ModEnc land/speed tables与Overlay family长期约定 | `ConfirmedCommunityConvention` | Fixed community documentation | 不能确认runtime优先级。 | product/profile显式。 | `NotRun` |
| TMP/theater/Rules/Overlay优先级和视觉terrain到passability的映射 | `ConflictingSources` | Tools/community/extensions | 来源对语义和优先级存在差异。 | 禁止单字段或视觉推断。 | `NotRun` |
| exact stock YR precedence、dynamic gate/bridge和final passability | `Unresolved` | No original-runtime source located | 当前无可靠唯一候选。 | future simulation adapter负责。 | `NotRun` |
| multi-state passability、raw preservation、missing-art和buildability分离 | `DefensiveDesign` | Project policy | 项目保真与安全策略。 | Unknown不自动Clear/passable/blocked。 | `NotRun` |
