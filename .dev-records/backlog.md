# Backlog

## 2026-08-12 - M3-C6 TMP/theater foundation

- [x] Add bounded TMP raw header, offset-table, 52-byte cell-header, and
  explicit plane-directory models.
- [x] Add six explicit theater profiles, composed-INI control reader, numeric
  TileSet registry, checked GlobalTileId ranges, and explicit asset candidates.
- [x] Execute the configured read-only ProjectBaseline TMP/theater audit. It
  completed with failures: 8 root archives, 282 mounted entries, zero named
  TMP candidates, and one failed aggregate. The source fingerprint was stable;
  no payload or per-entry data was published.
- [x] Current-head TMP/theater EditMode XML: 1249/1249 passed. PlayMode and
  remaining delivery gates are tracked by the final validation record and are
  not inferred from this focused result.

## 2026-08-12 - M3-C5 PreviewPack raw foundation

- [x] Add bounded Preview metadata and PreviewPack raw component models.
- [x] Preserve all four signed `Size` fields and require explicit section and
  duplicate selection; fields 0/1 remain unresolved.
- [x] Reuse the M3-C1 packed pipeline with an explicit `RawLzo1X` policy and
  injected backend; fail closed before layout interpretation on upstream error.
- [x] Keep decoded bytes immutable and expose RGB/BGR and row-order views only
  through explicit profiles.
- [x] Add synthetic PreviewPack tests and evidence definitions. Current-head
  Unity execution remains `NotRun` because the local Unity invocation was
  environment-blocked before producing a valid XML; no historical XML is
  reused.
- [x] Confirm no packed ProjectBaseline PreviewPack data was read or published.
- [x] Run current-head PS5.1 repository validation, copyright, wrapper, and
  exact-head Repository safety gates after the additive commits.
- [x] Execute the configured read-only ProjectBaseline PreviewPack audit and
  retain only sanitized aggregates (`CompleteWithFailures`: 184 candidates,
  184 exact decoded streams, zero section failures, one MIX mount failure).
- [x] Run current-head Unity EditMode/full EditMode; the authoritative XML is
  1210/1210 passed. PlayMode remains a separate delivery gate until executed
  on the final pushed HEAD.

## 2026-08-07 - M3-C2 authoritative closeout status

- [x] Draft PR #42 exists and remains Open/Draft/Unmerged.
- [x] Repository safety run `31178723783` completed successfully for review head `6551e01472404886da8f8a3aad4a514d863f8406`.
- [x] Final independent-review code and regression corrections are implemented locally.
- [x] Final correction push completed; exact-head Repository safety run `31245165415` completed successfully for HEAD `c23601084b944c71d06ffd9c2ab89e9df67f9c63`.
- [x] Run focused/full EditMode and PlayMode on Unity 2022.3.60f1c1; current-head XMLs are authoritative and all executed cases passed.
- Historical unchecked Draft PR and Repository safety entries below describe earlier delivery state and are superseded by this section.

## M3-C2 follow-up (historical review-head checklist)
- [x] 在可用 Unity 主机上从当前 HEAD 重新生成 focused/full EditMode 与 PlayMode XML；历史 XML 不可替代。
- [x] 在可用 GitHub 凭据下推送 `feature/m3-c2-isomap-pack5-record-foundation` 并创建 Draft PR `format: implement IsoMapPack5 raw record foundation`。
- [x] 完成 M3-C2 的双 PowerShell repository validation、copyright、content wrapper regressions 和 exact final-head Repository safety；run `31245165415` 为 `completed/success`。

# M3 COMPLETE

## Final closeout (merged main `82e2c6a46f842d09ee9786657065c942753cc435`)

M3 is complete at the repository-foundation and read-only aggregate level. The
following work packages are merged: M3-C1 packed-map foundation, M3-C2
IsoMapPack5, M3-C3 OverlayPack/OverlayDataPack, M3-C4 managed RawLzo1X,
M3-C5 PreviewPack, M3-C6 TMP/theater raw registry, M3-C7 read-only terrain
composition, and M3-C8 real ProjectBaseline aggregate integration.

Final validation recorded for the merged M3 tree:

- EditMode 1260/1260 passed; PlayMode 1/1 passed.
- Windows PowerShell 5.1 and PowerShell 7 repository/copyright/regression and
  wrapper gates passed.
- Post-merge Repository safety run `31670085049` completed successfully.

M3 COMPLETE does not mean original-runtime compatibility, clean YR 1.001
equivalence, visual rendering, palette RGB runtime, TMP/theater runtime
semantic binding, passability, pathfinding, unit movement, deterministic
simulation, gameplay, writer, or roundtrip compatibility. The configured
ProjectBaseline remains a patched development corpus.

## M4 governance

- [x] Refresh the tracked three-stage requirements and record its external
  synchronization source and evidence-state layers.
- [x] Define the Unity-free deterministic ECS, deterministic commit barrier,
  tactical autonomy, and legal computer-agent boundary.
- [ ] Implement M4-C1 deterministic ECS/intelligence kernel.

## 待处理
- [x] 使用 Unity 2022.3.60f1c1 从 M3-C1 交付 HEAD 重新运行 focused/full Unity 门禁（历史 M3-C1 交付记录）。
- [x] 提交并推送 M3-C1 contract、behavior matrix 和 delivery-state 修复，更新 Draft PR #36；PR #36 已合并。
- [x] 完成 M3-C2 IsoMapPack5 raw-record foundation，并开始 M3-C3 OverlayPack/OverlayDataPack raw-array foundation；IsoMap/Overlay 语义和 TMP 仍需独立证据，PreviewPack 现有 raw foundation 不提升为语义兼容。

- [x] 在具备 managed RawLzo1X backend 后执行 ProjectBaseline packed-section
  audits；M3-C4/C5/C8 仅发布脱敏聚合，不宣称地图兼容性。
- [x] 完成 IsoMapPack5 raw record 与显式 coordinate/trailing policy foundation。
- [x] 完成 OverlayPack/OverlayDataPack ordinary raw-array adapter foundation；当前分支仅保留 synthetic/configured 兼容边界。
- [x] 完成 M3-C4 managed RawLzo1X backend、注入式 packed integration 与脱敏
  ProjectBaseline IsoMapPack5 audit；managed backend 已实现，但不等于原版 runtime
  确认。
- [x] 完成 PreviewPack raw component foundation；保持 synthetic/configured 边界。
- [x] 完成 TMP raw parser foundation、theater registry foundation 和
  read-only terrain composition foundation。
- [ ] 实现 TMP/theater runtime semantic binding；不得把 foundation 或
  managed decoder 当作运行时兼容。

- [ ] 获取并登记干净、解包的 YR 1.001 内容基线。
- [ ] 建立 FinalAlert 2 A/B/C/D 往返黄金样本。
- [ ] 后续研究原版 YR `.sav` 二进制格式，不纳入第一阶段当前实现。
- [ ] 对 YR 1.001 原版内容覆盖行为建立可复现对照。
- [ ] 研究新 Westwood 加密 key source 的生成；当前只支持显式复用 80 字节 key source。
- [ ] 实现完整嵌套 MIX 树的语义重写；当前只支持读取树和独立生成子 MIX 字节。
- [ ] 研究 CSF 原版标签大小写、重复项胜出、语言包覆盖和缺失标签回退规则。
- [ ] 实现 CSF writer 与语义安全 roundtrip；当前仅严格只读解析。
- [ ] 为非 Windows 平台实现并验证 realpath/device 路径 identity。
- [ ] 调查 Unity 2022.3.60f1c1 中国版 headless 测试完成后的退出异常。
- [ ] 获得单独授权后，在仓库外一次性副本上执行 stock YR INI precedence、重复项、大小写、分号、空白和空值 A/B 黑盒对照。
- [ ] 在单独授权的黑盒对照中验证已冻结的 ProjectBaseline composition 是否匹配原版运行时；在此之前证据等级保持 `ConfiguredForProjectBaseline`。
- [ ] Resolve the ProjectBaseline SHP(TS) flags 3 row-width conflict with independently authorized original-runtime or reference-tool observations; do not relax the strict decoder from corpus shape alone.
- [ ] Audit real flags 2, high-bit coordinates, and `00 00` command semantics if future fixed samples expose them.
- [ ] Implement palette binding, remap, shadow pairing, visual comparison, and Unity rendering only in later work packages.

## M4-C1 deterministic ECS kernel

- [x] Add the Unity-free `RA2YR.Simulation` deterministic ECS reference kernel.
- [x] Cover entity generations, bounded components, structural ordering,
  logical time, scheduler phases, RNG streams, hashes, snapshots, proposals,
  autonomy modes, and deterministic decision staggering with synthetic tests.
- [ ] Continue to C2 only after the C1 branch receives its exact-head safety
  result and merge closeout; no original-runtime compatibility is implied.

## M4-C2 terrain / occupancy / spatial foundation

- [x] Add bounded terrain topology and explicit passability/movement candidates.
- [x] Add simulation-owned static, dynamic, foundation, and reservation
  occupancy with deterministic spatial insert/remove/move/query contracts.
- [ ] Continue to C3 pathfinding and movement only after C2 exact-head safety
  and merge closeout; no stock terrain semantics are confirmed.

## M4-C3 pathfinding / movement foundation

- [x] Add bounded deterministic managed pathfinding and immutable route results.
- [x] Add integer route following, occupancy/reservation conflict handling,
  cache invalidation, per-tick workload limits, and deterministic local
  avoidance proposals.
- [ ] Continue to C4 commands/missions/targeting only after C3 exact-head
  safety and merge closeout; no stock path semantics are confirmed.

## M4-C4 commands / missions / targeting / autonomy

- [x] Add declarative multi-source command requests, bounded replace/append
  queues, and raw-preserving runtime mission snapshots.
- [x] Add spatial-index perception, profile target scoring/hysteresis,
  deterministic arbitration, forced player authority, and explicit hold/
  autonomy boundaries.
- [ ] Continue to C5 combat/abilities only after C4 exact-head safety and merge
  closeout; no stock mission or AI parity is confirmed.

## 已放弃

## 2026-08-10 - M3-C4 delivery status

- [x] managed RawLzo1X decoder、synthetic tests、packed integration、audit command
  和 sanitized summary 已完成。
- [x] 当前 EditMode XML：1185/1185 passed，Unity exit 0，forced shutdown false。
- [x] 外部 patched development source audit 已执行并保留失败事实：status
  `CompleteWithFailures`，200 candidates、200 successes、1 mount-level failure。
- [ ] PlayMode、双 PowerShell repository/copyright/wrapper gates、Repository safety
  和 Draft PR 仍需在提交后的 exact HEAD 上验证。
- [ ] 不开始 M3-C5、Preview、TMP、Overlay semantics、palette、renderer 或 gameplay。

## 已完成

- [x] 将正式 Git 根限定为 Unity 工程 `RA2YR`。
- [x] 可恢复备份外层异常 Git 数据和空目录。
- [x] 建立 WP-00 仓库、许可证、文档、程序集和版权门禁底座。
- [x] 建立 WP-01 只读外部配置、内容索引和版本化 SHA-256 manifest 骨架。
- [x] 跑通 Unity EditMode 与 PlayMode 测试入口。
- [x] 配置正式 GitHub origin、发布 `main` 并将其设为默认分支。
- [x] 建立 README 和可重复的双 PowerShell 仓库静态门禁。
- [x] 合并并远程验证 WP-00/WP-01 Draft PR #1。
- [x] 建立 WP-02A 目录来源逻辑路径、优先级和 provenance 基础。
- [x] 完成 `YR1001_ProjectBaseline` 目录级清单与仓库外完整 manifest。
- [x] 推送 WP-02A 实现并创建、验证独立 Draft PR #2。
- [x] 合并并以最新 `main` 作为 WP-02B 分支基线。
- [x] 合并并远程验证 WP-02B Draft PR #3。
- [x] 实现 MIX、加密 MIX、校验、文件名 ID 和有界虚拟内容源。
- [x] 完成 ProjectBaseline 根级/嵌套 MIX 审计和七个目标定位。
- [x] 完成固定 XCC Mixer 的 A-D 合成语义往返验证。
- [x] 实现 PAL 原始数据解析并完成三个 ProjectBaseline 黄金样本验证。
- [x] 实现 CSF v3 严格只读解析并完成 `ra2md.csf` ProjectBaseline 黄金样本验证。
## 待处理
- [x] 已在代码 HEAD `c23601084b944c71d06ffd9c2ab89e9df67f9c63` 完成 focused/full EditMode、PlayMode、PS5.1/PS7 validation、copyright、wrappers 和 Repository safety；本次仅 backlog 文档提交，未在新纯文档 HEAD 重跑 Unity 矩阵。

## 2026-08-07 - M3-C2 delivery state (historical review head)

- [x] 修复 packed/record/coordinate execution aggregation 与零诊断预算 fail-closed 状态。
- [x] 在该历史 review head 重新扫描 M3-C2 测试计数：146 defined NUnit executions、103 behavior methods。
- [x] 在该历史 review head 完成 PS5.1/PS7 repository validation、copyright scan 和 regression gates；Unity 与依赖 Unity 的 wrappers 当时保持 NotRun。
- [x] 随后在代码 HEAD `c23601084b944c71d06ffd9c2ab89e9df67f9c63` 生成 focused/full EditMode 与 PlayMode XML；本次 backlog-only 提交未重复执行这些测试。

## 2026-08-09 - M3-C3 P2-2 evidence closure

- [x] 将 M3-C3 evidence 的 historical execution 与 current finding candidate 分离，
  不再把旧 PS7/copyright/wrapper/Unity 结果当作当前通过。
- [x] 记录当前候选 `141aed104a4c572f61f011541fa6929318388dbd` 的真实 PS5.1、PS7、
  copyright、wrapper 和 Unity 状态；implementation candidate safety 为
  `31312939491` completed/success。
- [x] 本轮保持 docs/evidence-only；没有修改代码、测试、兼容语义或研究正文。
- [x] PR #43 evidence/provenance closure was pushed, received exact-head
  Repository safety, and was merged; this historical pending note is superseded
  by the merged PR record.
M3-C7 read-only terrain composition was completed and merged in PR #47. Future
deferred scope remains renderer, passability/runtime, writer, clean YR 1.001
comparison, and original-runtime black-box validation.
## M3-C8 real-map integration closeout

- [x] Execute the configured read-only ProjectBaseline C1-C7 integration aggregate.
- [x] Preserve source fingerprint checks and sanitized aggregate output.
- [x] Record `CompleteWithFailures` when packed observations do not yield fully bound terrain.
- [x] PR #48 merged into main; C8 remains `CompleteWithFailures` with 200
  IsoMap candidates, 184 Preview candidates, unresolved terrain binding, and
  stable source fingerprints. Only sanitized aggregates are published.
- [ ] Clean YR 1.001 baseline comparison, original-runtime black-box validation, writer, renderer, passability/runtime, and gameplay remain future work.

## M5-C2 resource economy foundation

- [x] Add raw resource/type and explicit quantity/value candidate contracts.
- [x] Add bounded harvester capacity/cargo and refinery acceptance/dock descriptors.
- [x] Focused EditMode current tree: 1384/1384 passed; ProjectBaseline packed data not read.
- [ ] Runtime harvest movement, docking queues, unload timing, storage mutation,
  renderer/UI, and original-runtime compatibility remain deferred.

## M5-C3 production and technology foundation

- [x] Add raw definitions and explicit prerequisite/TechLevel/BuildLimit availability.
- [x] Add bounded deterministic FIFO queue candidates and checked progress.
- [x] Current EditMode tree: 1397/1397 passed; ProjectBaseline packed data not read.
- [ ] Payment/refund, factory runtime, placement/exits, completion actors, UI,
  campaign triggers, and original-runtime compatibility remain deferred.

## M5-C4 structures and placement foundation

- [x] Add raw structure definitions, footprint bounds/overlap, and power projection.
- [x] Add explicit repair/sell/capture/deploy interaction candidates.
- [x] Current EditMode tree: 1410/1410 passed; ProjectBaseline packed data not read.
- [ ] Actor creation, map occupancy mutation, renderer/UI, and original-runtime
  building compatibility remain deferred.
