# Future ProjectBaseline sanitized audit request

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明实现仅作行为、格式事实与冲突参考，未复制、翻译、逐句改写或机械移植代码、寻路算法、Locomotor 实现或测试夹具。`code_imported: false`。


## 1. Status

本文件只设计未来只读审计。**本任务未读取、枚举或运行 ProjectBaseline。**

任何未来结果只能标记：

```text
ObservedByFutureProjectBaselineAudit
```

不得提升compatibility或runtime证据。

## 2. Purpose

未来审计仅回答：

- public research profiles在真实项目样本上出现哪些类别；
- raw/binding/graph candidate是否有结构冲突；
- input modes是否等价；
- budgets和diagnostics是否合理。

不验证像素、玩法或原版runtime等价。

## 3. Selection basis allowed

可公开：

- broad theater类别；
- broad map类别；
- file/container class；
- selection count；
- sampling policy version；
- no-name ordinal buckets。

不可公开具体地图名、路径或可链接ID。

## 4. Allowed aggregate outputs

### Cells and coordinates

- total cell count；
- sparse/dense/mixed分类；
- duplicate/missing/explicit-default计数；
- coordinate magnitude bucket；
- domain-valid/invalid计数；
- Level粗粒度bucket；
- parity/category counts。

### TMP/theater

- TerrainTypeRaw粗粒度value class/count；
- RampTypeRaw known/unknown/category counts；
- HeightRaw coarse range；
- TMP present/missing/malformed counts；
- theater role binding success/conflict/unknown counts；
- no filenames。

### Overlays and objects

- Overlay family aggregate；
- missing data count；
- resource/wall/gate/bridge/other broad counts；
- Terrain/Smudge broad category counts；
- foundation dimension range bucket；
- irregular/rectangular/unknown foundation count；
- static occupancy counts。

### Movement properties

- MovementZone binding category counts；
- SpeedType binding category counts；
- Locomotor family category counts；
- known/unknown/extension/missing；
- property conflict count；
- no complete token/name list。

### Movement results

- PassabilityState aggregate counts；
- movement-domain count；
- layer category counts；
- node/edge totals；
- edge family aggregate；
- dangling/malformed transition counts；
- bridge/elevation category counts；
- cost buckets；
- collision/duplicate counts。

### Safety

- diagnostic code counts；
- read-limit usage buckets；
- Memory/Stream/short-read/MIX equivalence；
- non-linkable aggregate hash。

## 5. Forbidden outputs

禁止：

- map names/paths；
- INI text；
- coordinates sequence；
- IsoMap/Overlay arrays；
- object positions；
- type names；
- full Locomotor CLSIDs/names；
- exact foundations；
- bridge/tunnel locations；
- path graph topology；
- per-cell passability；
- exact movement costs；
- TMP/SHP/VXL names/content；
- screenshots/rendered images；
- per-map/per-cell/per-object hash；
- hex/Base64；
- absolute paths/usernames；
- reconstructable layout information。

## 6. Hash policy

只允许：

```text
AggregateSchemaVersion
SelectionPolicyVersion
CategoryCounts
CanonicalAggregateHash
```

hash需：

- 使用跨样本aggregate；
- 加入schema/policy version；
- 不对单map、单cell、单object；
- 不可由公开数据反推布局；
- 不公开salt或可链接manifest。

## 7. Proposed audit stages

```text
read bounded input
→ produce raw aggregate categories
→ bind public profiles
→ build aggregate movement candidates
→ redact identifiers and sequences
→ validate forbidden-field detector
→ compare input modes
→ publish aggregate report
```

## 8. Required diagnostics

允许公开code/count，例如：

```text
DuplicateCellConflict
MissingTmp
UnknownTerrainType
UnknownMovementZone
UnknownSpeedType
MissingLocomotor
RampLevelMismatch
ShoreCapabilityConflict
OverlayBindingConflict
FoundationOutOfRange
BridgeDanglingEntrance
TunnelInvalidDirection
CostOverflow
BudgetExceeded
InputModeMismatch
```

不可附coordinates、token text或resource name。

## 9. Input equivalence

同一样本经：

- Memory；
- seekable Stream；
- short-read Stream；
- exact MIX window

必须得到相同aggregate categories、diagnostic counts和aggregate hash。

## 10. No compatibility promotion

审计发现“未出现冲突”只能说明selected samples，不能证明：

- original runtime算法；
- 所有theater/map；
- 所有extension；
- gameplay equivalence；
- final passability正确；
- pathfinding正确。

## 11. Execution boundary

未来执行者不得：

- 修改ProjectBaseline；
- 启动Unity/RA2/YR/editor；
- render截图；
- 导出graph；
- 创建地图；
- 提交原始审计数据到仓库。

## 12. Public report template

```text
SelectionBasis
SchemaVersion
ProfileVersions
BroadCategories
AggregateCounts
RangeBuckets
ConflictCounts
DiagnosticCounts
InputModeEquivalence
NonLinkableAggregateHash
EvidenceGrade = ObservedByFutureProjectBaselineAudit
Limitations
```
