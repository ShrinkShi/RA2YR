# Bridges, tunnels and elevated movement

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明实现仅作行为、格式事实与冲突参考，未复制、翻译、逐句改写或机械移植代码、寻路算法、Locomotor 实现或测试夹具。`code_imported: false`。


## 1. Explicit movement layers

冻结候选：

```text
GroundLayer
UnderBridgeLayer
BridgeDeckLayer
AirLayer
SubterraneanLayer
```

layer是semantic identity，不是Unity layer、render pass或screen Y。

## 2. Bridge inputs

完整bridge movement descriptor可能需要：

```text
OverlayBridgePieces
OverlayDataStateCandidates
TheaterBridgeTileRoles
TmpBridgeArtCandidates
AuthoredHighFlags
PlacementHighCandidates
DeckCellCandidates
EntranceExitCandidates
UnderBridgeSurfaceCandidates
DamageStateCandidates
RepairStateCandidates
BridgeHeightCandidate
Diagnostics
```

单一输入不足以生成桥拓扑。

## 3. Low bridge

社区资料候选表明low bridge overlay可改变覆盖cell的ground/land行为，但未必增加Level。该结论只标 `CommunityDocumented`。

`LowBridgeDescriptor`至少分：

- visual pieces；
- covered ground cells；
- movement surface override candidate；
- damage grouping；
- repair grouping；
- entrance alignment；
- underlay/water relation。

不得从visual frame自动生成movement cells。

## 4. High bridge

高桥必须产生独立deck nodes：

```text
(CellIdentity, BridgeDeckLayer)
```

而ground/under-bridge node仍保留：

```text
(CellIdentity, GroundLayer or UnderBridgeLayer)
```

社区资料描述high bridge piece可能覆盖多个cells且OverlayPack仅存中心位置；这需要explicit expansion profile和provenance，不能成为无条件parser事实。

## 5. Entrance and exit

bridge deck与ground只在明确入口/出口连接：

```text
GroundNode
↔ BridgeEntranceEdge
↔ BridgeDeckNode
```

禁止根据相邻screen position或相同X/Y自动连接。

entry/exit可以非对称，destroyed state可禁用edge而不删除raw descriptor。

## 6. Under-bridge movement

`UnderBridgeLayer`需要自己的surface、occupancy和cost。桥上wall、桥下wall、水面、resource等不能由一个Overlay slot完整表达，因此冲突必须保留。

不得：

- 仅靠screen Y分桥上/下；
- 将bridge deck和ground node合并；
- 让bridge shadow占用under-bridge；
- 用Unit `High`字段生成完整bridge topology。

## 7. Damage and destruction

分开：

```text
RawBridgeStateCandidate
VisualDamageState
MovementDeckAvailability
EntranceAvailability
UnderSurfaceRestorationCandidate
RepairCandidate
DynamicOccupancyChange
```

destroyed visual frame不能直接改graph。future runtime adapter根据explicit policy产生dynamic graph delta。

partially destroyed bridge可以：

- 保留部分deck nodes；
- 禁用某些edges；
- 保留下层surface；
- 产生destructible blocker/action-required状态。

本轮不实现。

## 8. Bridge elevation

bridge elevation与：

- IsoMap Level；
- TMP HeightRaw；
- renderer visual offset；
- unit High field；
- path layer identity

分别保存。

社区资料中的固定“高桥增加若干height”属于低等级candidate；项目Core不硬编码。

## 9. Tunnels and Tube metadata

EA公开编辑器包含`Tube`描述：

```text
TubeId
StartCell
InitialDirection
EndCell
DirectionParts
```

并有八方向candidate、reverse/counterpart等editor操作。该来源证明官方editor识别authored Tube metadata，不证明YR runtime完整算法。

推荐：

```text
AuthoredTunnelDescriptor
- RawRecord
- StartCellCandidate
- EndCellCandidate
- DirectionSequenceRaw
- DirectionCandidates
- CounterpartCandidate
- ValidationDiagnostics
```

## 10. Tunnel graph boundary

authored Tube不是path graph本身：

```text
AuthoredTunnelMetadata
→ validated tunnel path candidate
→ entry/exit nodes
→ subterranean/tunnel layer edges
→ future movement adapter
```

需处理：

- invalid direction；
- out-of-domain part；
- missing endpoint；
- nonreciprocal counterpart；
- duplicate Tube ID；
- self-intersection；
- budget overflow；
- TS-only evidence。

RA2/YR stock适用性不足时标 `Unresolved`。

## 11. Subterranean locomotor

地下单位需要：

```text
UsesSubterraneanLayer
EntryCondition
ExitCondition
HiddenStateReference
SurfaceBlockingPolicy
Tunnel/FreeBurrowCandidate
TargetCellValidation
```

`AllowBurrowing`、Tunnel locomotor和extension行为来自社区资料，不能无条件迁移。

地下animation不等于underground graph。

## 12. Teleport and special transitions

teleport不是普通adjacency。候选edge为：

```text
SpecialTransitionEdge
- SourceNode
- TargetDomainCandidate
- RequiredCapability
- TargetValidationPolicy
- CostCandidate
- RuntimeCondition
```

不把所有cell两两连接，也不在Core执行teleport。

## 13. Air layer

aircraft/jumpjet需要：

- air node或continuous-domain candidate；
- landing/takeoff transitions；
- landing cell surface and occupancy；
- docking restrictions；
- shroud/mission policy；
- ground shadow separation。

地图 `[Aircraft]` placement只提供initial authored reference。

## 14. Independent implementation evidence

OpenRA公开实现把Tunnel、Subterranean、Jumpjet、ElevatedBridge作为custom movement layers，并把bridge入口、layer terrain与cell center分开。这是 `ConfirmedByIndependentImplementation`，不是stock runtime事实。

## 15. Deterministic expansion

bridge/tunnel expansion应：

- 以stable source ordinal排序；
- checked计算覆盖cells；
- bounded piece/node/edge数量；
- duplicate/conflict不last-wins；
- 输出canonical candidate order；
- 不依赖dictionary/hash iteration。

## 16. Evidence summary

| Topic | Grade |
|---|---|
| EA editor Tube metadata and direction model | `ConfirmedByOfficialEditorSource` |
| explicit custom movement layers in OpenRA | `ConfirmedByIndependentImplementation` |
| high/low bridge storage and behavior reports | `CommunityDocumented` |
| exact stock YR bridge destruction graph | `Unresolved` |
| explicit multi-layer Core model | `ConfiguredForProjectPolicy` |
