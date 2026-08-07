# Implementation boundaries and Core contracts

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明实现仅作行为、格式事实与冲突参考，未复制、翻译、逐句改写或机械移植代码、寻路算法、Locomotor 实现或测试夹具。`code_imported: false`。


## 1. Explicit non-implementation

本轮没有：

- passability reader；
- movement graph builder；
- MovementZone/SpeedType runtime；
- Locomotor；
- A*、JPS、flow field、NavMesh；
- collision、steering、avoidance、reservation、formation；
- bridge destruction、resource harvesting、building placement；
- C#、PowerShell、Unity测试或配置；
- Unity `Grid`、`Tilemap`、`Collider`、`NavMesh`、`GameObject`。

## 2. Dependency rules

Core：

- 不依赖 `UnityEngine`；
- 不保存Unity layer、Tag、NavMesh area、Collider、Tilemap名称；
- 不读取simulation singleton；
- 不创建单位或runtime actor；
- 不执行COM/CLSID Locomotor；
- 不依赖renderer、camera、screen-space；
- 接受边界化Memory/Stream/MIX window；
- 输出raw/derived immutable descriptors和structured diagnostics。

## 3. Pipeline contract

```text
raw map and resource descriptors
→ coordinate and cell identity views
→ theater tile semantic binding
→ raw surface and elevation candidates
→ overlay/object occupancy candidates
→ explicit locomotor capability profile
→ movement-domain and adjacency candidates
→ future pathfinding and movement adapters
```

每步保留上一步identity和provenance。

## 4. Candidate models

### `TerrainTopologyDocument`

raw records、coordinate profiles、cell index、duplicates/missing/default分类、binding references、diagnostics。

### `TerrainCellRaw`

11-byte record identity、raw tile views、SubTile、Level、final byte、source ordinal和window。

### `TerrainSurfaceCandidate`

TMP fields、TileSet roles、land type candidates、Level/elevation、surface state和conflicts。

### `TerrainRoleBindingResult`

theater registry、TileSet role、TMP raw和Rules land type之间的多候选绑定。

### `MovementDomain` / `MovementLayer`

ground/water/bridge/air/subterranean等semantic domain/layer；非Unity概念。

### `MovementNode`

cell+layer+surface+occupancy+provenance。

### `MovementEdge`

source/target/direction/elevation/capabilities/cost/dynamic conditions/evidence。

### `MovementGraphCandidate`

稳定有序node/edge集合、policy IDs、diagnostics和aggregate identity；不是pathfinder。

### `MovementCapabilityProfile`

MovementZone、SpeedType、Locomotor和扩展属性的能力合成结果。

### raw properties

```text
MovementZoneRaw
SpeedTypeRaw
LocomotorReferenceRaw
```

保留原token、case、unknown和source。

### `LocomotorCapabilityCandidate`

只陈述UsesGroundGraph等能力，不含算法。

### `TerrainCostCandidate`

raw percentage、rational/fixed candidate、modifier顺序、overflow和evidence。

### `PassabilityState`

```text
Passable
TraversableWithCost
Blocked
RequiresCapability
TemporarilyBlocked
DestructibleBlocker
Unknown
```

### occupancy models

```text
StaticOccupancyCandidate
DynamicOccupancySnapshot
FoundationOccupancyDescriptor
OverlayPassabilityCandidate
```

### transitions

```text
BridgeMovementDescriptor
ShoreTransitionDescriptor
RampTransitionDescriptor
```

### safety/analysis

```text
MovementDiagnostic
MovementReadLimits
MovementConsistencyAnalysis
MovementRoundtripDescriptor
```

## 5. Explicit policies

每个policy必须可序列化：

```text
PolicyId
Version
ProductProfile
EvidenceGrade
SourceReferences
Strictness
UnknownValueBehavior
FallbackBehavior
Limits
DiagnosticsBehavior
```

需要：

```text
TerrainRolePolicy
SurfaceBindingPolicy
MovementZonePolicy
SpeedTypePolicy
LocomotorCapabilityPolicy
TerrainCostPolicy
RampTransitionPolicy
ShoreTransitionPolicy
OverlayPassabilityPolicy
FoundationOccupancyPolicy
BridgeMovementPolicy
DynamicOccupancyPolicy
AdjacencyPolicy
MovementDeterminismPolicy
MovementRoundtripPolicy
```

不得用全局bool代替。

## 6. Raw/derived separation

```text
RawMapIdentity != SemanticSurfaceDescriptor
SemanticSurfaceDescriptor != MovementGraph
MovementGraph != DynamicOccupancy
RuntimeAcceptance != GameplayEquivalence
CanonicalRewrite != RoundtripPreservation
```

## 7. Parser boundaries

- map parser不算passability；
- TMP reader不读Rules；
- theater binder不建graph；
- Overlay reader不解释所有OverlayData；
- Rules binder不读map coordinates；
- Locomotor binder不创建unit；
- occupancy analysis不做collision；
- pathfinding不进入format Core。

## 8. Checked arithmetic

必须checked：

- coordinate conversions；
- `Y×1000+X`候选；
- Overlay `X+512Y`候选；
- cell counts；
- `(2W-1)×H`；
- GlobalTileId ranges；
- foundation offset + origin；
- bridge piece expansion；
- Tube direction walks；
- node/edge counts；
- terrain cost multiply/divide；
- fixed-point composition；
- canonical IDs。

overflow产生diagnostic/failure，不wrap/clamp。

## 9. Read limits

`MovementReadLimits`至少包含：

```text
MaxRawCells
MaxDuplicateRecordsPerCoordinate
MaxSurfaceCandidatesPerCell
MaxLayersPerCell
MaxNodes
MaxEdges
MaxSpecialTransitions
MaxFoundationCells
MaxStaticOccupants
MaxDynamicOccupantsPerCell
MaxOverlayBindings
MaxTubeParts
MaxBridgePieces
MaxDiagnostics
MaxTokenLength
MaxCoordinateMagnitude
MaxCostMagnitude
```

## 10. Determinism

canonical order：

- raw records：source ordinal；
- cells：domain-tagged canonical key + source ordinal；
- nodes：layer ordinal + cell key；
- edges：source + family + direction + target + source ordinal；
- occupants：cell/layer + stable occupant ID。

禁止hash order、thread completion order、Unity ID和frame time。

## 11. Input equivalence

以下输入必须一致：

- `ReadOnlyMemory<byte>`；
- seekable Stream；
- short-read Stream；
- exact MIX entry window。

一致项：

- raw records；
- source ordinals；
- bindings；
- nodes/edges candidates；
- diagnostics；
- consistency analysis；
- aggregate hash。

不允许读取MIX window外数据。

## 12. No-progress protection

所有stream/decoder/collector loop需有：

- progress计数；
- bounded iterations；
- short-read handling；
- zero-byte read policy；
- cancellation/diagnostic；
- exact declared window。

本轮只定义测试要求。

## 13. Synthetic fixtures

fixture：

- 不复用production coordinate conversion；
- 不复用production adjacency；
- 不复用production cost composer；
- 手写small cell sets、edges和cost expectations；
- 使用虚构Terrain/Overlay/type token；
- 不含ProjectBaseline或游戏资源；
- bridge/tunnel用小型虚构graph；
- duplicate/missing/overflow显式。

## 14. Diagnostics

`MovementDiagnostic`：

```text
Code
Severity
Stage
SourceReference
CellDomain?
Layer?
PolicyId
EvidenceGrade
NumericContext
MessageTemplateId
```

公开audit移除cell/object可链接数据。

## 15. Consistency analysis

只分析，不修复：

- raw/canvas axis conflict；
- sparse/missing/default；
- duplicate records；
- invalid tile/SubTile；
- TMP/theater role conflict；
- unknown MovementZone/SpeedType/Locomotor；
- impossible ramp；
- shore capability mismatch；
- overlay blocker conflict；
- foundation/occupancy conflict；
- bridge dangling entrance；
- tunnel invalid parts；
- deterministic order；
- input mode equivalence。

## 16. Future adapters

未来可以存在：

- graph builder；
- pathfinder；
- dynamic occupancy service；
- movement simulator；
- collision/steering；
- Unity presentation/debug adapter。

它们消费Core descriptors，不回写raw format semantics。

## 17. Roundtrip

必须保留：

- sparse/dense；
- duplicates；
- unknown tile views；
- invalid SubTile；
- Level；
- Overlay raw arrays；
- unknown properties；
- registry gaps；
- extension tokens；
- unresolved bindings。

不建议默认修复、排序重写或canonical map normalization。

## 18. Architectural acceptance

- noEngineReferences；
- noUnityObjects；
- noNavMesh/Collider/Tilemap/Grid/GameObject；
- no pathfinding in Core；
- no renderer-to-movement dependency；
- structured diagnostics；
- bounded inputs；
- deterministic graph candidates；
- evidence grade serializable；
- roundtrip-preserving raw model。
