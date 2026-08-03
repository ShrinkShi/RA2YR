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

## 2026-08-02 - 松散 PAL 不存在揭示 MIX 运行链路前置

### 现象
本机配置中的 `YR1001_Unpacked` 与 Clean 目录不存在；现有 `YR1001_ProjectBaseline` 内找不到松散的 `isotem.pal`、`temperat.pal`、`unittem.pal`。

### 初步判断
三个文件位于权威基线 MIX 内容链路中的可能性很高，但具体归档链必须由 MIX 解析和目标 ID 定位验证，不能凭目录缺失推断为原版文件缺失。

### 排查过程
- 只读解析被忽略的外部内容配置，不记录绝对路径。
- 在已配置 ProjectBaseline 内按三个文件名递归精确查找，均为 0。
- 工作区 ExternalContent 只有 cache，没有已知完整解包来源。
- Reference 工具集合存在多组 RA1/RA2/TS/TD 混合来源的 768 字节同名文件，但无法证明其对应当前 ProjectBaseline。

### 根因
`YR1001_ProjectBaseline` 以原版 MIX 容器为主要内容载体，目录型来源只能观察根级 MIX，无法直接发现内部逻辑文件。

### 解决方案
将 WP-02C 路线改为 MIX 读取、重建写入、加密/校验、嵌套挂载和 XCC 往返；所有写入仅针对仓库外合成副本，权威基线保持只读。Reference 工具内 PAL 候选仍不得冒充基线内容。

### 验证方式
读取全部根级 MIX 并保留嵌套 provenance，按经 XCC 固定向量确认的文件名 ID 定位七个目标文件；仅提交大小、SHA-256、归档链和诊断等脱敏摘要。

## 2026-08-02 - OmniBlade encoding 文件名大写回归

### 现象
固定的 `encoding` commit 将大写辅助函数改为返回新字符串，但 MIX 文件名 ID 调用点没有接收返回值。

### 风险
若直接按该提交实现，小写和大写输入可能产生不同 ID，既偏离 r1201 意图行为，也无法稳定定位 ProjectBaseline 文件。

### 处理
将该提交作为证据冲突记录。正式语义以 SourceForge r1201、本机 XCC 黑盒、固定合成向量和实际基线条目共同确认；不复制或修补 GPL 实现。

### 验证
至少对七个目标文件、大小写对、`/` 与 `\\`、完整路径与基名建立独立测试，并用 XCC 对合成名称结果进行黑盒核对。

## 2026-08-02 - XCC 创建行为与零 flags 头部不符合初始假设

### 现象
首次用三个合成文件让 XCC 创建归档时，零字节输入未进入目录；XCC 输出同时使用值为 0 的扩展 flags，而初版 reader 将其判为经典头。

### 初步判断
问题不是合成输入损坏，而是 XCC GUI 的实际创建策略和初版非零 flags 判别假设均不完整。

### 排查过程
- 用 XCC 列表确认首次归档只有两个非空输入和自动加入的 `local mix database.dat`。
- 固定首次归档长度/SHA-256，未把缺项伪造为成功。
- 将零字节输入替换为一个字节后重新创建，项目观察到 4 个条目并保留 XCC 实际顺序。
- 对 6、7-9、10 字节零头和带条目的零 flags 扩展头增加独立测试。

### 根因
XCC GUI 创建流程忽略零字节 dropped file，并将无功能 flag 的 RA2 MIX 仍写成扩展头；初始实现只依据非零 flag 位识别扩展头。

### 解决方案
采用 ADR 0010 的长度消歧；XCC 创建合约使用三个非空自主样本。项目 writer 的零字节条目支持不回退，并用 XCC 对项目归档的零字节条目提取单独验证。

### 验证方式
最终 XCC 提取三个项目归档中的零字节条目成功；所有相应 SHA-256 均为标准空内容哈希。Unity EditMode 相关回归包含在最终 343/343 中。

## 2026-08-02 - PowerShell JSON 路径清洗递归进入值类型

### 现象
基线/XCC 包装器在验证合法脱敏 JSON 时递归进入整数等值类型的 PSObject 属性，导致错误拒绝。

### 根因
清洗器仅把字符串视为叶节点，没有把 `System.ValueType` 明确视为不可递归的 JSON 标量。

### 解决方案
值类型直接返回；非字符串、非值类型、非 enumerable、非 PSCustomObject 的对象 fail-closed 拒绝。

### 验证方式
基线审计和 XCC 三阶段包装器均以真实 Unity 退出码 0 完成，且公开结果只含逻辑路径、大小、哈希和诊断。

## 2026-08-03 - CSF 记录 marker 字节序与截断分配顺序

### 现象
首版草案把源码中便于阅读的多字符标记值直接写成了相反字节序；独立审查还发现字符串声明长度在确认剩余输入前进入分配预算。

### 根因
源码常量的字符书写顺序与 little-endian 文件中的 UInt32 数值混淆；解析顺序只验证了格式预算，没有先验证声明字段实际落在当前边界内。

### 解决方案
- 以实际文件字节固定 `0x4C424C20`、`0x53545220`、`0x53545257`，并增加逐字节测试。
- 对主文本先 checked 计算 `codeUnits * 2`，对 ASCII 字段使用声明 byte length；两者均先比较 `RemainingLength`，再记预算和分配。
- 低报标签数在下一个 marker 明确为 `LBL` 时给出 `DeclaredLabelCountMismatch`。

### 验证方式
固定黄金模型 SHA 与独立计算一致；500000 长度的零负载夹具优先返回准确 EOF，非零 MIX window 的损坏 marker 返回 `base + 24` 偏移。Unity EditMode 489/489 通过。

## 2026-08-03 - 已完成 headless 测试留下空 UnityLockfile

### 现象
INI 聚焦测试已写出 Passed XML 并进入 Shutdown，但 Unity 进程未退出；终止该已完成进程后，`Temp/UnityLockfile` 仍存在，黄金审计包装器按设计拒绝继续。

### 核验与处理
- 确认不存在 Unity 进程；锁文件为 0 字节普通文件、非 ReparsePoint，时间与刚才测试一致。
- 仅删除这个可再生的空临时锁；未触碰 Assets、仓库外 Cache 或 ProjectBaseline。
- 重新运行黄金审计，Unity `-executeMethod` 真实退出码为 0。

### 结论
测试 XML、包装器退出状态和进程收尾必须继续分开记录；审计入口对任何现存 lock fail-closed 是正确行为。
## 2026-08-03 - 静态资料无法证明 ProjectBaseline INI 胜出规则

### 现象
`rulesmd.ini` 和 `soundmd.ini` 各有两个不同长度、不同 SHA-256 的 MIX
候选。官方编辑器源码给出编辑器搜索/解析行为，但独立实现和扩展资料不能
证明 stock YR 游戏运行时采用相同规则。

### 处理
运行时审计将 `selectedWinner` 保持为 null，证据等级标为 `Unresolved`；
实现只接受显式策略。没有启动游戏、XCC、FinalAlert 2 或 GUI 自动化，也
没有创建会被原版加载的测试 MOD。

### 后续验证
按 `docs/formats/ini-runtime-resolution.md` 中的 A/B 黑盒计划，在用户另行
授权后对仓库外一次性副本执行，并逐项固定输入哈希、观察结果和基线未变证明。

## 2026-08-03 - PR #8 审查发现 UTF-16、枚举预算和来源身份边界错误

### 现象
- UTF-16 ASCII 辅助函数仅按宽度匹配，同时接受 LE/BE 字节排列，导致反向
  code unit 可能被误判为分号、空格或 Tab。
- resolver 在检查 `MaxDocuments` 前对任意 `IEnumerable` 调用 `ToArray`，预算
  不能限制枚举和分配。
- provenance source ID 使用 `OrdinalIgnoreCase`，与精确身份语义不符。

### 根因与修复
- 统一由物理编码感知读取器识别 ASCII code unit：LE 仅 `XX 00`，BE 仅
  `00 XX`；名称、值转换和语法审计共享该规则。
- 候选最多读取 `MaxDocuments + 1`，多出的一个仅用于确认超限，随后立刻
  返回 `DocumentBudgetExceeded`；`IniLoadPlan` 收窄为可信的已物化
  `IReadOnlyList`。
- source ID 改为 `StringComparison.Ordinal`。

### 验证
聚焦 EditMode 66/66、全量 EditMode 627/627、PlayMode 1/1。惰性枚举在
`MaxDocuments + 1` 停止，未触发下一项的故意异常，trace 只包含已保留项。

## 2026-08-03 - PR #8 修复验证中的两次调用错误

### 现象
- 首次聚焦编译因公开 NUnit 测试方法暴露 internal enum 参数而报 CS0051，
  没有生成可声明通过的 XML。
- 首次仓库/版权回归调用向测试脚本传入不存在的 `-RepositoryRoot` 参数，
  四次 shell 均 exit 1；随后首次实际仓库门禁因新 evidence 尚未创建而按
  设计报告 3 个缺失引用。

### 处理
- 参数化测试改用公开 `bool` 输入并在方法内映射物理编码；重新运行通过。
- 按脚本真实契约无参数重跑回归；创建修复 evidence 后重新运行实际门禁。
  所有失败状态均保留在执行记录中，不将失败调用伪装为成功。
