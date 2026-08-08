# Backlog

## 2026-08-07 - M3-C2 authoritative closeout status

- [x] Draft PR #42 exists and remains Open/Draft/Unmerged.
- [x] Repository safety run `31178723783` completed successfully for review head `6551e01472404886da8f8a3aad4a514d863f8406`.
- [x] Final independent-review code and regression corrections are implemented locally.
- [ ] Push the final correction commit and obtain Repository safety success for its exact HEAD.
- [x] Run focused/full EditMode and PlayMode on Unity 2022.3.60f1c1; current-head XMLs are authoritative and all executed cases passed.
- Historical unchecked Draft PR and Repository safety entries below describe earlier delivery state and are superseded by this section.

## M3-C2 follow-up (historical review-head checklist)
- [x] 在可用 Unity 主机上从当前 HEAD 重新生成 focused/full EditMode 与 PlayMode XML；历史 XML 不可替代。
- [x] 在可用 GitHub 凭据下推送 `feature/m3-c2-isomap-pack5-record-foundation` 并创建 Draft PR `format: implement IsoMapPack5 raw record foundation`。
- [ ] 完成 M3-C2 的双 PowerShell repository validation、copyright、content wrapper regressions 和 exact final-head Repository safety; static and wrapper gates are complete, final safety remains pending after the new push.

## 待处理
- [ ] 使用 Unity 2022.3.60f1c1 从当前 HEAD 重新运行 M3-C1 focused/full Unity 门禁。
- [ ] 提交并推送 M3-C1 contract、behavior matrix 和 delivery-state 三个独立提交，更新 Draft PR #36。
- [ ] 后续独立工作包研究并实现 IsoMap/Overlay/Preview/TMP，必须重新建立格式证据。

- [ ] 在授权并具备实际 backend 后设计 ProjectBaseline packed-section audit；不得在 M3-C1 伪造地图兼容性。
- [ ] 研究并实现后续 IsoMapPack5、OverlayPack、PreviewPack、TMP 和 LZO 读取工作包。

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

## 已放弃

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
- [ ] 在 PR #42 修复提交后重新运行 focused/full EditMode、PlayMode、PS5.1/PS7 validation、copyright、wrappers 和 Repository safety。

## 2026-08-07 - M3-C2 delivery state

- [x] 修复 packed/record/coordinate execution aggregation 与零诊断预算 fail-closed 状态。
- [x] 重新扫描 M3-C2 测试计数：146 defined NUnit executions、103 behavior methods。
- [x] 完成 PS5.1/PS7 repository validation、copyright scan 和 regression gates；Unity 与依赖 Unity 的 wrappers 保持 NotRun。
- [ ] 在可用 Unity 2022.3.60f1c1 主机上生成当前最终 HEAD 的 focused/full EditMode 与 PlayMode XML。
