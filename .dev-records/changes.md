# 变更记录

## 2026-08-01 - WP-00/WP-01 基础建设

### 变更范围
- Git、许可证、文档、程序集、测试、外部内容索引和版权扫描。

### 具体改动
- 完成外层异常 Git 的可恢复备份与空目录备份，正式仓库根固定为 Unity 工程目录。
- 建立 Apache-2.0 `LICENSE`、`NOTICE`、第三方来源台账和 GPL 仅参考边界。
- 建立需求、架构、兼容矩阵、ADR 和开发过程记录目录。
- 建立 Core、UnityIntegration、Editor、EditMode 和 PlayMode 程序集；Core 启用 `noEngineReferences`。
- 建立 Unity 批处理测试入口，拒绝锁冲突、错误版本、缺失/空/零测试 XML 和超时假成功。
- 实现 schema 1 外部内容配置、只读文件发现、SHA-256、来源指纹和规范 manifest。
- 对 repository/cache/source 重叠、Windows 路径别名、reparse point、扫描中内容变化和不完整索引采取 fail-closed。
- 要求正式仓库根在配置加载和索引时均为现存目录，并拒绝 Windows 驱动器相对路径。
- 将测试哈希注入及索引结果构造限制为 EditMode 友元，阻止公开 API 伪造完整 manifest。
- 建立双 PowerShell 版本版权扫描回归、Git index/worktree 分离扫描和 CI 安全门禁。
- 建立本机忽略配置并只验证指定 YR 基线目录存在，未索引、哈希或报告原版文件正文。

### 验证情况
- Unity Core 与 EditMode 程序集使用 Unity 自带 Mono 编译器编译成功。
- Unity EditMode：29/29 通过；PlayMode：1/1 通过；零编译错误。
- 版权扫描回归：Windows PowerShell 5.1 和 PowerShell 7 合计 22/22 通过。
- 当前仓库版权扫描：零违规；外部目录和本机配置 ignore probes 全部通过。
- `Assets` 34 个资源/目录均有匹配 `.meta`，无孤立 `.meta` 或重复 GUID。

### 风险
- 历史风险（已解决）：GitHub 远程仓库最初尚未配置；本轮远程收口已发布 `main` 并修正默认分支。
- 中国版 Unity 2022.3.60f1c1 的两个无界面测试子进程在结果完成后未自行退出；包装器等待 30 秒后仅终止自身启动的 PID，并验证完整 XML，已记录警告。
- 非 Windows 的物理路径 identity 当前仅作规范化词法比较，需后续增加平台 realpath/device 实现。

## 2026-08-01 - WP-00/WP-01 远程交付收口

### 变更范围
- 远程分支、README、开发基线命名、静态仓库验证、CI 和交付证据。

### 具体改动
- 发布 `main` 到初始 Unity 提交并设为 GitHub 默认分支，不重写或删除现有提交和分支。
- 建立根 README，明确项目不可玩、外部内容边界、测试入口、许可证和非官方属性。
- 将当前 patched 开发源统一为 `YR1001_ProjectBaseline`；保留 Clean/Unpacked 技术角色和原版 YR 1.001 兼容目标。
- 将 `.meta`、GUID、Unity 版本、Core 引用边界和兼容矩阵结构检查实现为独立 PowerShell 工具。
- 为静态门禁建立 Windows PowerShell 5.1 与 PowerShell 7 合成回归并接入 GitHub Actions。

### 验证情况
- Unity EditMode：29/29；PlayMode：1/1；编译错误为 0。
- 静态门禁回归：46/46；负例同时验证非零退出码与 `Passed=false`，真实仓库在两个 PowerShell 主机下均为 0 违规。
- 本机外部配置：schema 1、3 个来源、1 个启用、0 诊断；启用角色为 `YR1001_ProjectBaseline`。
- 兼容矩阵仍为 120 项，其中仅 3 项为 `可解析`，未因基础门禁提升状态。

### 风险
- Draft PR 和 GitHub Actions 只能在本提交推送后创建/触发，结果需在远程交付报告中记录。
- Unity headless 退出和非 Windows path identity 限制保持不变。

## 2026-08-02 - WP-02A 内容解析优先级与基线清单

### 变更范围
- 仅包含目录型来源逻辑路径、确定性优先级、provenance、仓库外完整 manifest、脱敏基线证据、测试和文档。

### 具体改动
- 新增与 Unity 无关的逻辑路径类型，拒绝绝对路径、穿越、空段、无效 UTF-16、Windows 不安全名称和当前文化大小写行为。
- 建立内部内容源抽象及目录型实现边界，为后续 MIX、地图内嵌、模组和合成来源预留接口，但不读取 MIX。
- 实现 enabled-only、多来源高 priority 胜出、同源大小写冲突拒绝、最高 priority 并列歧义和稳定完整 provenance chain。
- 解析和 manifest 结果保持不可公开伪造；不完整、歧义或冲突结果拒绝序列化。
- 实现仓库外内容寻址 resolved manifest 写入、既有字节一致性验证和来源存储 identity 绑定。
- 新增受控 ProjectBaseline Unity/PowerShell 命令和脱敏汇总模型。
- 新增逻辑路径、20 项解析场景、manifest 安全及基线摘要测试。
- 更新外部内容架构、ADR、兼容矩阵和两份 WP-02A 证据。

### 本机基线结果
- `YR1001_ProjectBaseline`：157 个目录文件、683,178,144 字节、0 诊断、扫描期间未发现变化。
- 仓库外完整 manifest schema 1，SHA-256 `1443736cb554e4f2c96423e4a2d01368fc6b66fb75669682463bb1ebb8a65e68`，哈希复核通过。
- 目录内容以 MIX 容量为主；未解析 MIX 内部内容，也未进行纯净 YR 1.001 或行为对照。
- 仓库内未提交完整文件级 manifest、原版正文或本机绝对路径。

### 验证状态
- 最终 Unity EditMode 73/73、PlayMode 1/1 通过；零失败、零跳过、零残留 Unity 进程或锁文件。
- 最终两个 headless Editor 均正常退出；同一分支的较早通过轮次曾触发已记录的 30 秒收尾警告，因此该问题仍视为间歇性已知限制。
- Windows PowerShell 5.1 与 PowerShell 7 的受控基线命令均通过并得到相同 manifest SHA-256。
- 双 PowerShell 仓库验证均通过：44 个 Assets 条目、44 个 `.meta`、120 个矩阵条目、8 个证据引用、0 违规；合成回归 46/46。
- 暂存态版权扫描在两个主机下均为 147 个候选、13/13 ignore probes、0 违规；合成回归 22/22。

### 风险
- 便携扫描无法对抗同大小、同时间戳的恶意并发变更；来源和 cache 需在受控运行期间保持静止。
- 非 Windows 物理路径 identity 限制仍在；本轮逻辑路径语义本身固定为 YR/Windows `OrdinalIgnoreCase`。
