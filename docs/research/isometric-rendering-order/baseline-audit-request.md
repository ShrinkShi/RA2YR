# Future ProjectBaseline sanitized audit request

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明的实现仅作行为与冲突参考，未复制、翻译或机械移植其代码、公式实现或测试夹具。`code_imported: false`。


## 1. 状态

本文件只设计未来只读脱敏审计；本任务没有读取、枚举、运行或散列 ProjectBaseline。

```text
AuditStatus: NotRun
FutureEvidenceSource: ProjectBaselineAggregateAudit
```

这些字段不是formal evidence grade，不表示ProjectBaseline已经被读取、Observed或Confirmed。未来aggregate observation不能替代公开来源，也不能自动提升compatibility或成为`ConfirmedByOriginalRuntimeSource`。

## 2. 目标

验证公开研究形成的候选是否能覆盖项目私有样本的**类别与聚合范围**，而不泄漏地图布局、资源身份或可重建信息。

## 3. SelectionBasis

未来审计先公开：

- audit tool version；
- policy/profile IDs；
- theater/map broad categories；
- input mode；
- selection basis（代表性类别，不含名称/路径）；
- 样本数量区间；
- 拒绝/跳过原因类别。

## 4. 允许公开

- `SelectionBasis`；
- broad theater/map categories；
- coordinate range聚合（min/max bucket，不给序列）；
- Level/ramp类别计数；
- entity family数量；
- anchor/bounds presence类别；
- render-pass类别；
- depth-key collision数量；
- stable tie-break结果聚合；
- depth-plane presence/shape分类；
- foundation尺寸范围聚合；
- bridge/elevation类别；
- aircraft候选数量；
- diagnostics；
- non-linkable aggregate hashes；
- input-mode equivalence。

## 5. 禁止公开

- 地图名称和路径；
- INI正文；
- coordinates序列；
- object positions；
- type名称；
- SHP/VXL/TMP资源名称；
- frame offsets逐项；
- foundations逐对象值；
- draw order序列；
- bridge位置；
- aircraft位置；
- depth plane bytes；
- screenshots；
- rendered images；
- meshes；
- per-map/per-object hash；
- graph topology；
- hex/Base64；
- 可重建地图布局的信息。

## 6. Audit phases

### A. Input equivalence

对同一已授权logical sample比较 Memory、Stream、short-read Stream、MIX window结果，只公开：

- equal/not equal；
- aggregate descriptor hash；
- diagnostic code counts；
- window-boundary violations count。

### B. Coordinates

公开bucket：

- raw coordinate sign/range class；
- canvas axis range class；
- nonzero Level count bucket；
- projection overflow/rounding diagnostic count；
- parity class counts。

不得输出coordinate pair。

### C. Entities

公开family count与presence matrix，不输出type/position/source record。

### D. Anchors/bounds/foundation

公开：

- anchor kind presence；
- missing/fallback counts；
- bounds kind presence；
- visual/conservative relation分类；
- foundation cell-count与extent buckets；
- irregular candidate count。

不得输出per-object shape、offset或polygon。

### E. Depth/pass

公开：

- pass category counts；
- layer category counts；
- collision group size histogram；
- tie-break stage histogram；
- unresolved tie count；
- deterministic rerun equality。

不得输出draw sequence或entity key。

### F. Depth/occlusion/shadow

公开plane presence、dimensions bucket、truncated/unknown diagnostics、shadow source category。不得输出bytes、frame indices或resource identity。

### G. Bridges/aircraft

公开bridge/elevation category count、layer completeness、aircraft candidate/altitude source category。不得输出locations或type。

## 7. Non-linkable aggregate hash

hash输入必须：

- 跨整个SelectionBasis聚合；
- 加固定schema version；
- 排除path/name/type/position；
- 排除per-map partition；
- 不公开salt；
- 不允许从单对象反查；
- 数量过小则只输出`insufficient anonymity set`。

## 8. Small-count policy

可能识别对象的稀有bucket：

- 合并为`other`；
- 或只输出presence；
- 或抑制；
- 阈值由audit policy配置，不在报告中泄漏具体小样本细节。

## 9. Output schema

```text
AuditVersion
AuditStatus = NotRun
FutureEvidenceSource = ProjectBaselineAggregateAudit
SelectionBasis
PublicCategoryCounts
RangeBuckets
PresenceMatrices
CollisionHistograms
DiagnosticCounts
InputModeEquivalence
AggregateNonLinkableHash?
RedactionSummary
CurrentEvidenceGrade
```

`CurrentEvidenceGrade`只记录审计前的公开来源等级，并使用九项封闭词汇；它不能表示未来审计已经执行。

## 10. 禁止的行为

- 不启动Unity/游戏/editor/tool；
- 不截图/渲染；
- 不导出地图或asset；
- 不输出raw snippets；
- 不自动修复；
- 不修改compatibility status；
- 不把audit观察标为official/runtime-confirmed；
- 不将private样本制作成公开test fixture。

以上均为`DefensiveDesign`审计要求。

## 11. 审计停止条件

发现任何：

- path/name泄漏；
- coordinate/object sequence；
- per-map hash；
- 可重建foundation/bridge topology；
- depth bytes；
- image/mesh；
- tool尝试写入；

立即停止并只报告redaction failure类别。

## 12. 预期价值

未来audit只回答“候选model是否覆盖观察到的类别、是否确定、是否有结构性冲突”，不回答“原版runtime一定如何实现”。
