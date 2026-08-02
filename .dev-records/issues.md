# 问题排查记录

## 2026-08-01 - 外层异常 Git 仓库

### 现象
调查期间，工作区外层出现未授权来源的 `.git`，包含大量 loose objects，且没有提交、远程或索引。

### 初步判断
该仓库边界会覆盖原版内容、工具和 Unity 生成目录，存在严重误提交风险。

### 排查过程
- 操作前确认 21,773 个文件、646,130,014 字节。
- 首次改名因 Hidden 属性报告权限错误，但数据已移动到完整备份，原路径只剩空目录。
- 对空目录检查文件、子目录、大小、ReparsePoint、隐藏/系统子项和 NTFS 备用数据流。
- 清除空目录 Hidden 属性后，将其可恢复改名。

### 根因
异常仓库的创建来源尚未确认；首次改名报错由根目录 Hidden 属性导致。

### 解决方案
- 完整备份：`E:\时锐\RA2\RA2YR-unity\.git.backup-20260801-162014`
- 空目录备份：`E:\时锐\RA2\RA2YR-unity\.git.empty-backup-20260801-171609`
- 正式仓库初始化在 `E:\时锐\RA2\RA2YR-unity\RA2YR`
- 在远程、提交和首个 PR 验证成功前不删除两个备份。

### 验证方式
- 正式仓库 `git rev-parse --show-toplevel` 返回 `RA2YR`。
- 从外层工作区运行相同命令返回“not a git repository”。
- 两个备份目录均存在。

## 2026-08-01 - Unity 批处理测试退出行为

### 现象
- 带 `-quit` 时 Unity 返回 0，但未创建测试 XML。
- 移除 `-quit` 后测试 XML完整且通过，但 headless Editor 在结果写出后持续高 CPU、不自行退出。

### 根因判断
Unity Test Framework 1.1.33 需要在 Editor update 中调度测试；通用 `-quit` 会抢先退出。当前 `2022.3.60f1c1` 中国版环境又存在结果完成后的 headless 退出异常，具体 Unity 内部原因尚未确认。

### 解决方案
- 禁止测试入口使用 `-quit`。
- 缺失、空白、不可解析或零测试 XML 一律失败。
- 完整 XML 出现后给本次启动的确切子进程 30 秒退出宽限；仍挂起时只终止该 PID，验证 XML 后输出警告。
- 强制收尾仅删除该子进程留下的零字节、非 reparse `Temp/UnityLockfile`；异常锁对象拒绝清理。

### 验证方式
最新同一入口连续执行 EditMode 和 PlayMode，结果分别为 29/29 与 1/1 通过，编译错误为零，结束后无 Unity 进程和锁文件残留。

## 2026-08-01 - 路径元数据检查必须 fail-closed

### 现象
独立审计发现 `Directory.Exists`、`File.Exists` 和 `FileSystemInfo.Exists` 会将部分权限或元数据检查错误折叠为 `false`，路径祖先可能因此被误判为不存在。

### 根因
ReparsePoint 检查依赖 `.Exists` 决定是否读取属性，上层异常处理无法看到被 API 吞掉的访问错误。

### 解决方案
- 逐级调用 `File.GetAttributes` 检查候选路径及其祖先。
- 仅捕获 `FileNotFoundException` 和 `DirectoryNotFoundException` 并退到父级。
- 让权限、I/O 和安全异常向上传播，由 Loader/Indexer 转换为拒绝诊断。

### 验证方式
注入合成元数据访问拒绝，确认异常不会返回“无 ReparsePoint”；该回归包含在最新 EditMode 29/29 结果中。

## 2026-08-01 - GitHub 默认分支错误

### 现象
远程最初只有 `feature/wp00-wp01-foundation`，GitHub 将它设为默认分支；远程没有 `main`、PR 或 Actions 运行。

### 初步判断
远程仓库是在 feature 已存在而 main 尚未发布的状态下建立。

### 排查过程
- 验证 origin 精确为 `https://github.com/ShrinkShi/RA2YR.git`。
- 验证本地 `main` 指向 `1caf8a8`，feature 指向 `e5c39af`，工作树干净。
- 验证远程只存在 feature，默认分支确为 feature。

### 根因
远程缺少 `main`，导致 GitHub 以当时唯一的 feature 分支作为默认分支。

### 解决方案
- 以普通非强制推送发布本地 `main`。
- 将 GitHub 默认分支改为 `main`。
- 不删除分支、不重写三个现有提交。

### 验证方式
远程 `main` 为 `1caf8a801de64e68db201279ab93a0dd3137be2f`，feature 为 `e5c39af30a79b6b877a29591afe6338abee4dbe7`，GitHub `defaultBranchRef.name` 为 `main`。

## 2026-08-01 - GitHub Actions 的首个 ignore probe 不一致

### 现象
本机 Git 2.45.1 下，版权扫描器通过一次 NUL 分隔的 `git check-ignore --stdin -z` 调用验证 13 个探针；GitHub Actions 的 Git 2.55.0.windows.3 环境仅漏报输入列表第一项，导致 PR 的前两次安全工作流失败。

### 证据边界
Git 官方文档仍规定 `--stdin -z` 使用 NUL 分隔输入。本轮只能确认两个 Git for Windows 版本的实测结果不同，未将其未经上游确认地归因为 Git 缺陷。

### 解决方案
- 每个受策略约束、仅含命令安全 ASCII 字符的探针单独调用 `git check-ignore --no-index -- <path>`。
- 只接受 Git 文档定义的退出码 0（已忽略）和 1（未忽略），其他退出码 fail-closed。
- 新增“每个 required ignore probe 都被检查”的合成负例。

### 验证方式
本机 Windows PowerShell 5.1 与 PowerShell 7 合计 22/22 回归通过；真实暂存区仍为 121 个候选、13/13 探针、0 违规和 0 个禁止物理根。修复后的远程工作流结论在 PR 交付报告中记录。

### 后续远程发现
第三次工作流中 22 个扫描回归均明确输出 `PASS`，但最后一个预期失败用例留下 `$LASTEXITCODE=1`。GitHub Actions 的 PowerShell 调用方式据此将整个步骤判为失败。两个回归入口现均在完成安全清理后显式 `exit 0`；任何断言或清理异常仍会在到达该语句前失败。

## 2026-08-02 - 逻辑路径补充平面散列契约缺陷

### 现象
WP-02A 的 `Equals` 使用 `OrdinalIgnoreCase`，旧哈希却逐 UTF-16 code unit 大写。若大小写映射跨 surrogate pair，两个相等逻辑路径可能产生不同哈希。

### 根因
逐 `char` 处理看不到完整 Unicode scalar，无法复现字符串级大小写映射。

### 解决方案
- 对完整路径字符串执行 invariant uppercase，再执行确定性 FNV-1a。
- 增加 BMP、补充平面和固定 ASCII 数值回归；不改变 priority 或歧义语义。

### 验证方式
当前 Unity EditMode 运行覆盖 9 个 BMP case、4 个补充平面 case、固定哈希和集合分组契约，全部通过。

## 2026-08-02 - 子区间完成诊断历史二次复制

### 现象
早期 WP-02B 实现让每个 child completion 多次复制全会话诊断。重复允许尾部警告时，累计引用数组可达到 O(N²)，且不受数据分配预算约束。

### 根因
完成对象被设计成携带全会话历史，而会话 `Diagnostics` getter 和完成构造器又分别复制数组。

### 解决方案
- 会话维护单一只读诊断视图，完成或故障后禁止追加。
- child completion 仅携带本次完成诊断；finalized root 复用封存会话视图。
- 增加 256 个连续 child warning 回归，断言每个 child 仅一项且 root 汇总 257 项。

### 验证方式
对应 EditMode 回归通过；独立代码审查未发现剩余中高严重度问题。
