# Depth order and deterministic tie-breaking

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明的实现仅作行为与冲突参考，未复制、翻译或机械移植其代码、公式实现或测试夹具。`code_imported: false`。


## 1. 目标

排序输出必须在相同输入、profile 与 simulation snapshot 下完全确定，并在 Memory、Stream、short-read Stream 与 MIX window 输入下相同。排序只影响 presentation；不得影响 occupancy、targeting 或 pathfinding。

## 2. 候选数据模型

```text
RenderDepthComponents
- RenderPassOrdinal
- ElevationLayerOrdinal
- ProjectedGroundY
- ProjectedBottomYCandidate
- CellLevel
- ExplicitZAdjust
- FamilyPriority
- ParentStableId
- AttachmentOrdinal
- StableSourceOrdinal
- StableEntityIdentity

RenderDepthKey
- PolicyId
- Components
- ComparablePackedOrTupleView
- Diagnostics

RenderTieBreakPolicy
- ComponentOrder
- Signedness
- OverflowPolicy
- StableFallbackOrder
```

建议比较 tuple，而不是依赖一个易 overflow 的魔法整数。若需要 packed key，必须定义位宽、范围验证和 overflow failure。

## 3. 公开实现证据

OpenRA 在固定 revision 中使用 `Pos.Y + Pos.Z + ZOffset` 作为主比较 key，并把原始提交 index 拼入 key 进行稳定排序，注释明确指出稳定排序用于避免闪烁。该行为是 `ConfirmedByIndependentImplementation`，不是 RA2/YR runtime 算法。

EA editor公开坐标、frame offset和对象绘制代码提供输入候选，但未提供可证明原版 runtime 的完整统一 comparator。

## 4. 不足的单一输入

| 单一输入 | 为什么不足 |
|---|---|
| `Transform.position.y` | Unity adapter值；忽略 layer、attachment、tie |
| screen Y | 桥上/桥下、air、shadow、pass冲突 |
| raw X+Y | 忽略 Level、subcell、visual adjustment |
| cell coordinate | 同 cell 多 entity冲突 |
| image bottom | frame变化、透明裁剪和大图会移动 anchor |
| source order | 不表达 building/turret/shadow语义 |
| object center | 大 foundation与不规则 bounds错误 |
| hash | 枚举/版本不稳定 |
| Unity instance ID | save/load/network不稳定 |
| camera zoomed Y | camera不应改变逻辑排序 |

## 5. Projected ground 与 visual bottom

- `ProjectedGroundY`: 由 authored ground anchor + Level/ramp contact产生；
- `ProjectedBottomYCandidate`: visual bounds bottom，可作为 culling/局部冲突诊断或 evidence-gated候选；
- SHP negative offset可使 visual bottom变化，但不重定义 ground；
- damage/animation frame bounds变化不应让 structure“跳到”另一 cell排序；
- foundation bottom edge可用于 building family policy，但 foundation来源必须 authored。

## 6. Pass 与 layer 优先于局部 depth

比较建议：

1. `RenderPassOrdinal`
2. `ElevationLayerOrdinal`
3. family-specific ground/depth primary
4. explicit authored/render Z/Y adjustment
5. family priority
6. parent/attachment relationship
7. stable source ordinal
8. stable identity

此顺序是项目候选；不同 pass可选择不共享同一 depth comparator。

## 7. Stable source ordinal

`StableSourceOrdinal` 必须来自 canonical、可重放来源，例如：

- map section name canonical rank；
- section内原始 placement ordinal；
- overlay storage index；
- TMP cell/subtile ordinal；
- generated attachment的 parent stable ID + fixed role ordinal；
- runtime spawn的 deterministic sequence，由 simulation提供。

不能来自：

- dictionary迭代；
- object memory address；
- thread completion order；
- renderer batch order；
- asset load completion；
- camera可见列表顺序。

## 8. Stable entity identity

推荐组成：

```text
SourceKind
SourceDocumentOrdinal
SourceRecordOrdinal
ParentStableIdentity?
AttachmentRole?
DuplicateOrdinal
RuntimeDeterministicSpawnOrdinal?
```

禁止公开 baseline audit中的 per-object identity；Core内部可保存，但审计只聚合 collision与tie结果。

## 9. Exact tie policy

若前述组件完全相同：

1. 比较 `StableSourceOrdinal`；
2. 比较 family-defined attachment ordinal；
3. 比较 stable identity bytewise/canonical tuple；
4. 若 identity重复，保留 duplicate ordinal并诊断；
5. 仍不可区分则严格失败或由显式 `DuplicatePlacementPolicy`选择，不随机。

## 10. 场景化 tie

### 同 cell 多 Infantry

- subcell contact；
- infantry source ordinal；
- stable identity；
- 不使用当前 frame高度。

### Unit 与 Infantry 同 cell

- ground contact；
- family priority由 policy显式；
- exact tie再 source ordinal；
- 不能以“谁先进入 list”作为未记录规则。

### Terrain 与 Structure

- pass/family policy先决定；
- tree透明/occlusion可额外参与，但不改变 occupancy。

### Building 与 foundation

- foundation/bib通常在 body前；
- body key锚定 authored building anchor/foundation edge；
- foundation不成为独立 simulation entity。

### Body/turret/barrel

- parent key + attachment ordinal；
- turret facing与barrel recoil只改 transform/bounds；
- component顺序固定，不依赖asset load。

### Shadow/body

- ShadowPass/receiver layer显式；
- shadow key不可使其决定caster occupancy。

### Attached animation

- parent stable ID；
- explicit Z/Y adjust；
- attachment role ordinal；
- independent world effect才使用独立 anchor。

### Duplicate placement

保留每条记录与 ordinal；不得去重后掩盖输入冲突。

## 11. Network、save/load与replay

要保证：

- stable identity序列化或可重建；
- runtime spawn ordinal来自deterministic simulation；
- load后不使用新分配对象ID；
- replay camera不改排序；
- visibility filtering不改变剩余实体之间的相对 key；
- multithread collection后按 canonical source重新排序。

## 12. Camera independence

logical depth用未zoom的 projected logical pixel/world coordinate。zoom、DPI、screen shake、letterbox与viewport origin只应用在最终 display transform。

## 13. Diagnostics

- `DepthComponentOverflow`
- `DepthKeyCollision`
- `TieResolvedBySourceOrdinal`
- `DuplicateStableIdentity`
- `UnresolvedExactTie`
- `CameraDependentDepthRejected`
- `ImageHeightAnchorInferenceRejected`
- `UnstableEnumerationDetected`
- `AttachmentParentMissing`
- `ElevationLayerConflict`

## 14. 项目候选

`RenderDepthKey` 是结构化 tuple；`RenderTieBreakPolicy` 可序列化并带 evidence grade。首次实现前必须由 synthetic independent oracle测试，而不能调用 production projection/anchor/sort代码生成 expected值。
