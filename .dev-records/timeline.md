# 开发时间线

## 2026-08-01 17:26 - 启动 WP-00 和 WP-01

### 用户目标
- 建立安全、独立的 Unity 正式仓库。
- 完成许可证、文档、程序集边界、测试入口和只读外部内容索引底座。
- 在验证后提交并创建首个草稿 PR。

### 本轮处理
- 只读调查 Unity 工程、YR 内容和 FinalAlert 2。
- 将外层异常 Git 数据可恢复地备份，并保留空目录备份。
- 在 `RA2YR` 初始化正式 Git 仓库。
- 开始实施 WP-00 和 WP-01。

### 关键结论
- 正式 Git 根只能是 Unity 工程 `RA2YR`。
- 原版内容、工具和参考资料必须位于正式仓库外。
- 第一阶段存档和回放采用引擎自有版本化格式，不要求原版 `.sav` 二进制互读。

### 影响文件
- 外层 Git 目录仅执行可恢复改名。
- 正式仓库内 WP-00/WP-01 文件正在创建。

### 后续事项
- 完成代码和测试验证。
- 确认 GitHub 远程仓库后创建草稿 PR。

## 2026-08-01 19:43 - WP-00/WP-01 验证收口

### 本轮处理
- 修复版权扫描器的 index blob、忽略物理目录、路径编码和合成夹具登记边界。
- 收紧 manifest 可信构造入口，增加扫描后二次树元数据验证。
- 由 Unity 自动生成并核对全部新增资源 `.meta`。
- 跑通 EditMode 25 项和 PlayMode 1 项正式 Unity 测试。
- 将三项内容底座能力提升为兼容矩阵 `可解析`，其余 WP 范围保持 `未实现`。

### 关键结论
- 本地 YR 基线配置可加载，启用目录存在，但本轮未索引或读取其文件正文。
- `-quit` 会让 Unity Test Framework 1.1.33 在调度前退出，不能用于本项目测试命令。
- 当前 headless Editor 写完结果后可能挂起，入口只在完整 XML 出现并等待 30 秒后终止自身子进程，且明确输出警告。

### 后续事项
- 完成首次提交前清单和最终版权扫描。
- 创建本地基线提交与 WP-00/WP-01 功能分支提交。
- 获取正式远程 URL/可见性后推送并创建首个草稿 PR。

## 2026-08-01 20:22 - 仓库根边界回归

### 本轮处理
- 根据独立最终审计，修复不存在或文件型仓库根可能削弱目录边界判断的问题。
- 在配置读取前验证正式仓库根，在索引开始前再次验证其仍存在。
- 拒绝 `C:relative` 一类 Windows 驱动器相对路径。
- 将逐级路径检查从会吞掉权限错误的 `.Exists` 改为 `File.GetAttributes`；仅确实不存在时退到父级。
- 新增对应合成回归，重新跑通 EditMode 29 项和 PlayMode 1 项。

### 关键结论
- 缺失、文件型或索引前消失的正式仓库根全部 fail-closed。
- 元数据访问拒绝会向上传播并触发拒绝，不再被误判为安全路径。
- 最新测试结束后无 Unity 进程和锁文件残留。

### 后续事项
- 执行版权扫描、元数据、兼容矩阵和 Git 边界最终复核。
- 以追加提交保留既有本地提交历史。
- 获取正式远程 URL/可见性后推送并创建首个草稿 PR。

## 2026-08-01 22:14 - WP-00/WP-01 远程交付收口

### 用户目标
- 修正远程 `main + feature + Draft PR` 结构。
- 补齐根 README、开发基线命名和可重复静态门禁。
- 不开始格式、地图或游戏逻辑开发。

### 本轮处理
- 以非强制推送发布 `main` 到初始 Unity 提交，并将 GitHub 默认分支改为 `main`。
- 保持 `feature/wp00-wp01-foundation` 和原三个提交不变。
- 将当前 patched 开发内容源统一命名为 `YR1001_ProjectBaseline`，明确它不是纯净 YR 1.001 黄金基线。
- 新增根 README、仓库验证器及双 PowerShell 合成回归，并接入 `repository-safety.yml`。
- 重新跑通 Unity EditMode 29 项、PlayMode 1 项和仓库验证回归 46 项；矩阵顶层未知键、重复键、游离内容和空测试项均按 fail-closed 拒绝。

### 关键结论
- 远程 `main` 指向 `1caf8a8`，默认分支为 `main`；feature 仍指向 `e5c39af`，提交历史未重写。
- 静态门禁只证明仓库结构、Unity 元数据、Core 边界和兼容矩阵结构，不提升任何格式兼容状态。
- 本机配置加载只验证路径和来源角色，未索引、哈希或读取原版内容正文。

### 影响文件
- `README.md`
- `Config/ExternalContent.example.xml`
- `Tools/Repository/Invoke-RepositoryValidation.ps1`
- `Tools/Repository/Tests/Invoke-RepositoryValidation.Tests.ps1`
- `.github/workflows/repository-safety.yml`
- 相关需求、架构、兼容证据、策略和开发记录文件

### 后续事项
- 完成 staged-blob 版权扫描和最终本地门禁。
- 创建单一远程收口提交，推送 feature，并创建首个 Draft PR。
- 观察并如实记录 GitHub Actions 是否触发及其结果。

## 2026-08-01 23:20 - GitHub Actions ignore probe 兼容性修复

### 远程证据
- Draft PR #1 已触发 `Repository safety`。
- GitHub runner 使用 Git 2.55.0.windows.3；前两次运行均在首个 required ignore probe 处失败，第二次运行输出了确切相对路径规则。
- 本机 Git 2.45.1 的同一批 NUL 输入为 13/13 通过，因此没有把失败误写为版权违规或工作流成功。

### 修复
- 将批量 NUL `check-ignore` 改为逐探针、退出码驱动的检查，避免跨版本首记录差异。
- 版权扫描负例现在同时锁定非零退出码和 `Passed=false`，断言输出相对路径违规摘要。
- 本机双 PowerShell 回归更新为 22/22；未读取或报告原版素材正文，未提升兼容矩阵状态。
- 第三次远程运行的 22 个用例全部 PASS 后仍因负例遗留 `$LASTEXITCODE=1` 失败；版权扫描和仓库验证两个回归入口均增加显式成功退出。

## 2026-08-02 - 启动 WP-02A

### 用户目标
- 从 PR #1 合并后的最新 `main` 创建独立分支。
- 只实现目录型逻辑内容解析、显式优先级、完整 provenance 和受控 ProjectBaseline 清单。
- 完整 manifest 留在仓库外，公开仓库只保留脱敏证据；不开始任何格式或游戏逻辑开发。

### 本轮处理
- 确认 PR #1 已合并，`main` 为合并提交 `0f22a510`，从该提交创建 `feature/wp02a-content-resolution-baseline`。
- 审计 WP-01 路径、索引、manifest 和不可伪造边界，并只读检查本机基线目录元数据。
- 实现逻辑路径、目录来源抽象、确定性解析、完整来源链和仓库外 resolved manifest。
- 用合成来源覆盖批准的 20 类测试场景，首轮 EditMode 为 69/69。
- 执行受控基线索引；157 个文件、683,178,144 字节、0 诊断、无扫描期变化。
- 验证外部 manifest 的内容寻址 SHA-256，公开证据仅转录汇总和批准代表项。

### 关键结论
- `content.precedence` 仅因合成目录来源门禁通过而提升到 `可解析`。
- ProjectBaseline 目录清单完成不等于 MIX 内部内容可见，也不等于纯净原版或行为对照通过。
- 目录源未看见松散目标文件时，统一报告“未在当前已挂载的目录型内容源中发现；MIX 内容尚未解析。”

### 后续事项
- 完成代码复核、最终 Unity/PowerShell/版权/矩阵门禁。
- 创建单独提交、推送分支并创建不自动合并的 Draft PR。

## 2026-08-02 - WP-02A 远程交付

### 本轮处理
- 创建实现提交 `806335f78b6fc3b8fc026c582999c12f888a7def` 并非强制推送到独立 feature 分支。
- 创建以 `main` 为 base 的 Draft PR #2，保持未合并。
- 验证远程 `main` 仍为 PR #1 合并提交，feature 文件与本地提交一致。
- GitHub Actions `Repository safety` 首轮运行 `30736714207` 全部通过。

### 关键结论
- 远程安全工作流真实触发并成功，不以本机结果代替远程状态。
- `actions/checkout` 报告 Node.js 20 弃用提示，但没有失败步骤或兼容门禁违规。
- 两份外层 Git 备份继续保留，未删除或改写。

### 后续事项
- 等待用户审查并批准 Draft PR #2；不得自动合并。
- 获批后再确定 WP-02B/WP-03 的具体范围，不在本 PR 中开始格式解析。
