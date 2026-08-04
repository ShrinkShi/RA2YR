# M3-R13 — Resource harvesting and economy boundaries

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明的实现仅作行为、冲突和架构参考，未复制、逐句翻译、机械改写或移植其采集、经济、AI、随机、寻路或测试代码。`code_imported: false`。

## 1. Scope

本专题只定义 RA2/YR 资源 Overlay、资源类型、矿车载荷、采集、精炼厂 docking/卸载、增长/扩散、credits 与经济覆盖层的**声明式输入和职责边界**。不实现任何 runtime mutation。

冻结候选管线：

```text
raw map and Rules descriptors
→ resource-overlay family binding
→ raw resource-stage/value candidates
→ logical resource-cell descriptors
→ harvester/refinery capability binding
→ declarative collection and unloading contracts
→ economy-source descriptors
→ future deterministic simulation and UI adapters
```

## 2. Non-negotiable boundaries

- Overlay reader 只保留 `OverlayTypeRaw`、`OverlayDataRaw`、storage coordinate 和 provenance，不计算最终资源量。
- `OverlayDataRaw` 没有 format-wide 统一语义；resource、wall、bridge、crate 等 family 必须分开解释。
- resource binder 不修改地图，不删除 Overlay，不触发增长、扩散或耗尽。
- harvester binder 不创建单位；type capacity 与 current cargo 分离。
- collection contract 不执行采集、不寻路、不分配目标、不运行 reservation。
- refinery binder 不转移 cargo 或 credits；dock、animation、audio、mission 和 economy mutation 分离。
- economy parser 不覆盖 lobby/session state，不决定最终起始资金。
- UI 只消费 canonical state；pip、进度条、车体帧和黄色载荷条都不能成为 cargo 权威。
- Core 不依赖 `UnityEngine`，不创建 `GameObject`、`Sprite`、`ProgressBar`、`AudioSource`、粒子或协程。

## 3. Principal conclusions

### 3.1 Resource overlays

官方 FinalSun/FinalAlert 2 编辑器在固定版本源码中识别四组硬编码资源 Overlay 范围，并独立保存 Overlay 和 OverlayData 数组。该证据证明 editor profile，不证明原版 RA2/YR runtime 的完整资源 registry 或 harvest settlement。

资源 family 候选：

```text
Empty
Ore
Gems
TSGreenTiberium
TSBlueTiberium
Veins
Crate
DebrisOrRock
WallOrFence
Bridge
Rail
Tunnel
ExtensionResource
Unknown
```

`Veins`、crate 和 debris 不得作为普通 ore 处理。

### 3.2 Stage, quantity and value

每个资源 cell 同时保留：

```text
OverlayTypeRaw
OverlayDataRaw
ResourceFamilyCandidates
VisualStageCandidates
QuantityCandidates
YieldCandidates
InterpretationProfile
EvidenceGrade
Diagnostics
```

`visual frame != quantity != economic yield`。官方编辑器的 map-money 估算候选 `(OverlayData + 1) × Value` 只标记 `ConfirmedByOfficialEditorSource`。

### 3.3 Resource registry

`[Tiberiums]`、资源 subsection、Overlay ordinal、editor hardcoded range、theater/control INI 和 extension registry 都属于不同来源。TS Tiberium 与 RA2/YR ore/gems 采用显式 product profile，不能无条件共享一个 vanilla model。

### 3.4 Harvester cargo

```text
AuthoredVehicleTypeCapacity
!= CurrentRuntimeCargo
!= CargoEconomicValue
!= UI Pip Count
!= DisplayedLoadFraction
```

Core 模型允许 mixed cargo 和 per-resource entries，但是否为 stock YR 行为保持 `Unresolved`。

### 3.5 Collection and docking

采集和卸载均定义为未来 deterministic simulation 协议。parser、renderer、pathfinder 和 animation 不得修改资源、cargo 或 credits。

### 3.6 Economy sources

House credits、campaign carry-over、lobby money、game-mode override、refinery delivery、crate、Trigger、runtime account、AI estimate 和 score 分开保存。最终 session credits 由显式 `EconomyOverridePolicy` 在更高层选择。

## 4. Evidence grades

- `ConfirmedByOfficialRuntimeSource`
- `ConfirmedByOfficialEditorSource`
- `ConfirmedByIndependentImplementation`
- `CommunityDocumented`
- `ObservedByFutureProjectBaselineAudit`
- `ConfiguredForProjectPolicy`
- `Unresolved`

预计没有完整、公开的 RA2/YR runtime economy source。本专题不会人为提升证据。

## 5. Files

1. `README.md`
2. `layer-and-domain-boundaries.md`
3. `resource-overlay-and-data-model.md`
4. `resource-types-values-and-storage.md`
5. `harvester-capacity-and-load-state.md`
6. `harvest-targeting-and-collection.md`
7. `refinery-docking-and-unloading.md`
8. `growth-spread-and-depletion.md`
9. `economy-credits-and-session-overrides.md`
10. `source-comparison.md`
11. `implementation-boundaries.md`
12. `test-matrix.md`
13. `baseline-audit-request.md`
14. `unresolved-questions.md`

## 6. Explicit non-goals

No resource parser, harvesting, cargo mutation, harvester AI, resource reservation, docking, unloading, credits mutation, growth, spread, AI economy, UI load bar, Unity object, C#, PowerShell, test, configuration, ProjectBaseline audit, map execution or compatibility promotion.
