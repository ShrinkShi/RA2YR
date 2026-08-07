# Cell surface and topology model

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明实现仅作行为、格式事实与冲突参考，未复制、翻译、逐句改写或机械移植代码、寻路算法、Locomotor 实现或测试夹具。`code_imported: false`。


## 1. Required raw cell model

推荐 `TerrainCellRaw` 保存：

```text
StableSourceOrdinal
CellIdentityCandidate
AuthoredIsoMapRecord
RawTileFieldBytes
TileFieldViews
SubTileRaw
LevelRaw
FinalByteRaw
DuplicateGroupId?
SourceWindow
Diagnostics
```

其中 `TileFieldViews` 必须允许：

- raw 32-bit；
- signed/unsigned 32-bit candidate；
- low/high 16-bit candidate；
- byte 6/7 raw view。

不以“哪个能成功绑定 TMP”自动选择解释。

## 2. Logical cell descriptor

`TerrainTopologyDocument` 不是 dense tile array，而是：

```text
RawRecordSequence
CoordinateDomainProfile
CellIdentityIndex
DuplicateGroups
MissingCellClassification
ExplicitDefaultCells
OutOfDomainRecords
TheaterBindingReference
OverlayBindingReference
PlacementBindingReference
Diagnostics
```

它必须保留 source order。

## 3. Surface candidate

推荐 `TerrainSurfaceCandidate`：

```text
CellIdentity
SurfaceClassCandidates
AuthoredLevelRaw
GlobalTileIdCandidate
SubTileRaw
TmpCellReference?
TmpTerrainTypeRaw?
TmpRampTypeRaw?
TmpHeightRaw?
TileSetRoleCandidates
LandTypeBindingCandidates
Water/Shore/Cliff/Ramp/Ice/Rail/Tunnel candidates
CornerHeightCandidates
EvidenceSet
ConflictSet
Diagnostics
```

它不包含：

- current unit；
- collider；
- reservation；
- pathfinding result；
- renderer mesh。

## 4. Cell-state decomposition

不得压缩为 `Walkable`。最低应分别表示：

| Dimension | Example states |
|---|---|
| raw presence | missing / explicit / duplicate / conflicting |
| asset binding | bound / missing / invalid SubTile / ambiguous |
| surface semantics | clear / road / rough / water / shore / cliff / unknown |
| elevation | Level raw / corner candidate / unresolved |
| capability requirement | ground / water / amphibious / fly / subterranean |
| cost | exact raw percentage / multiplier candidate / unknown |
| static occupancy | none / foundation / terrain object / overlay blocker |
| dynamic occupancy | free / moving / stationary / temporary / reserved |
| action requirement | crush / destroy / open gate / repair bridge |
| graph state | node candidate / no node / quarantined / unresolved |

## 5. TMP metadata boundaries

### `TerrainTypeRaw`

公开资料和工具常将其关联到 LandType-style类别，但：

- field名称、编号和 runtime使用仍缺乏完整官方 runtime证明；
- theater roles、rules LandTypes和Overlay `Land=`都可能进一步影响语义；
- unknown/extension values必须保留；
- raw parser不读取 Rules。

输出应是：

```text
TmpTerrainTypeRaw
TerrainTypeNumericCandidate
LandTypeCandidateSet
EvidenceGrade
```

而不是最终 cost。

### `RampTypeRaw`

应产生：

- ramp enum/name candidate；
- corner-height profile candidate；
- orientation candidate；
- malformed/extension diagnostic。

不单独产生 edge。

### `HeightRaw`

应保存 raw byte和signed/unsigned candidate view，不自动：

- 加到 IsoMap Level；
- 改 cell identity；
- 改 graph layer；
- 改 object anchor。

### flags, extra and depth

- extra graphics：visual extension；
- depth plane：renderer occlusion；
- damaged data：format/art state candidate；
- none of the above：默认不参与 passability。

## 6. TileSet role binding

推荐 `TerrainRoleBindingResult`：

```text
GlobalTileId
TileSetIndex
TileIndexInSet
TheaterProfile
RoleCandidates
ControlIniProvenance
TmpTerrainCandidate
UnknownOrExtensionRole
EvidenceGrade
Diagnostics
```

候选角色：

- clear；
- rough；
- sand；
- road；
- pave；
- water；
- shore；
- cliff；
- ramp；
- bridge；
- tunnel；
- ice；
- rail；
- LAT transition；
- custom extension。

文件名只允许作为显式 `FilenameHeuristicProfile`。稳定社区命名最多为`ConfirmedCommunityConvention`；项目选择是否启用该启发式为`DefensiveDesign`，不能把文件名直接当作runtime passability事实。

## 7. Missing asset policy

区分：

```text
MissingTmpFile
MissingVariation
InvalidSubTile
TruncatedTmp
UnknownTileField
UnboundGlobalTileId
```

默认：

- GlobalTileId range不移动；
- raw cell仍存在；
- surface为 `Unknown`/`Unresolved`；
- 不因 Art缺失自动 blocked；
- 不因 Art缺失自动 passable；
- 由调用方的 strictness/policy决定是否允许生成 graph candidate。

## 8. Duplicate cell policy

重复 coordinate可以是：

- byte-identical；
- semantically equivalent under selected tile profile；
- different Level；
- different tile；
- different SubTile；
- different final byte；
- conflict。

默认 project policy：

- raw全部保留；
- source order稳定；
- equivalent可产生一个candidate并附全部 provenance；
- conflicting不 last-wins；
- graph builder fail-closed或隔离该 cell；
- writer不规范化。

这些为`DefensiveDesign`。

## 9. Missing cell policy

missing cell与explicit default不同。

候选策略必须显式命名：

- `NoSurfaceNode`
- `ImplicitDefaultSurface`
- `OutsideLogicalDomain`
- `EditorDenseCompatibility`
- `StrictSparsePreservation`

不得由 dense array初始化值隐式决定。

## 10. Elevation and topology

推荐 surface elevation结构：

```text
BaseLevelRaw
LevelHeightProfile
CornerHeightCandidates
RampProfile
BridgeLayerCandidate
SurfaceNormalCandidate
DiscontinuityDiagnostics
```

movement edge需要同时检查：

- source/target存在；
- layer compatibility；
- Level delta；
- ramp orientation/corner；
- surface capability；
- dynamic conditions。

## 11. Surface consistency analysis

只报告：

- Level与ramp不连续；
- 同Level但cliff role；
- 不同Level却无ramp；
- ramp指向missing cell；
- shore无water/land counterpart；
- bridge entrance无deck；
- unknown LandType；
- overlay Land override冲突；
- duplicate cell冲突；
- TMP role与theater role冲突。

不自动修复。

## 12. Bounded topology

`MovementReadLimits` 至少包含：

- max raw records；
- max unique cells；
- max duplicate group size；
- max coordinate magnitude；
- max role candidates per cell；
- max surface conflicts；
- max bridge/tunnel layers；
- max diagnostics；
- max raw token length；
- max foundation cells；
- max nodes/edges。

超限产生结构化失败，不截断后继续成功。

## 13. Roundtrip

必须保留：

- raw 11-byte record；
- record order；
- duplicate records；
- unknown tile views；
- invalid SubTile；
- raw Level/final byte；
- unresolved binding；
- selected semantic profile作为外部 metadata，而不是回写 raw值。

## 14. Evidence boundary

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert公开IsoMap/TMP raw cell字段 | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | 只确认官方editor/reader输入。 | 保留raw fields。 | `NotRun` |
| 多工具把TMP terrain、theater role和surface binding分层 | `Underconfirmed` | Public tools/community | 收敛不证明独立谱系或runtime优先级。 | 保留candidate/conflict sets。 | `NotRun` |
| 文件名、颜色或extra/depth可唯一决定runtime surface/passability | `ConflictingSources` | Tool heuristics and semantic sources | 视觉与语义来源可能不一致。 | 禁止视觉/filename自动选择。 | `NotRun` |
| 原版runtime对TerrainTypeRaw、RampTypeRaw和HeightRaw的完整语义 | `Unresolved` | No original-runtime source located | 当前无可靠唯一候选。 | raw保留，typed interpretation显式。 | `NotRun` |
| duplicate/missing/missing-art fail-closed与bounded topology | `DefensiveDesign` | Project policy | 保真和安全设计。 | 不last-wins、不自动Clear/blocked/passable。 | `NotRun` |
