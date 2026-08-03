# Backlog

## 待处理

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
