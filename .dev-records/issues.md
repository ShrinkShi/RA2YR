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
初版运行时审计将 whole-file selection 保持未决，实现只接受显式策略。
该临时结论已由 ADR 0022 取代：ProjectBaseline 现在采用明确的有序多文档
逐值组合，但证据等级仅为 `ConfiguredForProjectBaseline`，不声称原版运行时
对照。没有启动游戏、XCC、FinalAlert 2 或 GUI 自动化，也没有创建会被
原版加载的测试 MOD。

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
## 2026-08-03 - WP-02G2 ProjectBaseline typed results remain incomplete

- 现象：两个 Rules 聚合与 Art 聚合均为 `Incomplete`。
- 原因：原始文档含大量已保留 Opaque 行，部分注册值含未决行内分号，且显式测试重复策略会影响候选。
- 处理：继续保留 typed 文档用于脱敏聚合，但不将其标为完整语义，不选择 stock winner，不实现默认值或回退。
- 后续：只有获得 stock runtime 语法与 precedence 证据后，才能缩小风险影响范围或提升 original comparison。

## 2026-08-03 - PR #10 审查发现 typed view 仍隐式选择首项

- 现象：Art 多重匹配虽报告 `ArtSectionAmbiguous`，仍把 `matches[0]` 作为 Present 字段并生成资源引用；Rules 的 `0` 与 `00` 也未标记解析 ordinal 冲突。
- 根因：诊断与字段状态未形成同一 fail-closed 契约，ordinal 保留原始拼写但缺少解析身份分组。
- 修复：引入无单值赢家的 Ambiguous 候选集合和同 registry ordinal 冲突诊断；增加枚举顺序稳定性与跨 registry 隔离测试。
## 2026-08-03 - M2-SHP1 ProjectBaseline flags 3 conflict

- Symptom: 257 non-empty flags 3 frames fail strict row-width validation at row 0; each produces exactly one index beyond the descriptor width.
- Distribution: 120 even widths and 137 odd widths, across widths 14 through 202.
- Resolution: preserve `RleOutputOverflow`, publish only aggregate evidence, and keep the audit status `CompleteWithDecodeFailures`. No clamp, padding, width expansion, or file-specific exception was added.
- Limitation: because strict decoding stops at row 0, the observed `00 00` count of zero is not a whole-file exhaustion result.

## 2026-08-03 - PowerShell 7 timestamp deserialization changed audit verification

- Symptom: PowerShell 7 converted an ISO UTC JSON timestamp during `ConvertFrom-Json`, so a later textual `Z` check could reject a valid wrapper result.
- Resolution: validate the raw JSON timestamp representation before deserialization, then accept the typed PowerShell 7 value without weakening the UTC requirement.
- Verification: SHP wrapper regression passes 9/9 under Windows PowerShell 5.1 and PowerShell 7.

## 2026-08-04 - Unity test wrapper read a null exit code after passed XML

### Symptom
- The full EditMode XML reported 694/694 passed, Unity was no longer running,
  and no lock file remained, but the wrapper returned shell exit 1 and printed
  an empty Unity exit code.

### Root cause
- The wrapper used timed `WaitForExit` polling and read `Process.ExitCode`
  without the final parameterless `WaitForExit()` handshake required to finish
  process state collection reliably under Windows PowerShell 5.1.

### Resolution
- Call parameterless `WaitForExit()`, refresh the process, store the integer
  exit code, and report it separately from wrapper exit and forced shutdown.

### Verification
- Final EditMode and PlayMode wrappers both returned shell exit 0, reported
  Unity exit 0, and explicitly reported controlled post-result shutdown.

## 2026-08-04 - PowerShell 7 JSON date coercion broke UTC suffix validation

### Symptom
- The CSF ProjectBaseline audit completed in Unity with exit 0 under PowerShell
  7, but the wrapper rejected `startedUtc` because the converted JSON value no
  longer rendered with a trailing `Z`.

### Root cause
- PowerShell 7 `ConvertFrom-Json` coerces ISO UTC strings into `DateTime`
  values. Casting that value back to string uses host formatting and removes
  the source JSON suffix, while Windows PowerShell 5.1 leaves it as a string.

### Resolution
- Timestamp validators now accept UTC `DateTime`/zero-offset `DateTimeOffset`
  objects and retain the exact trailing-`Z` requirement for string inputs.

### Verification
- CSF and PAL regression suites pass in both hosts (11/11 and 10/10), and both
  real PowerShell 7 audits complete with Unity exit 0.

## 2026-08-04 - M3-C1 local delivery blocked by host and GitHub authentication

### Symptom
- `feature/m3-c1-packed-map-compression-foundation` is pushed at `5bbe88b`, but no Draft PR exists for the branch.
- `gh auth status` reports the configured GitHub token as invalid; ordinary `git push` cannot obtain credentials because the local prompt helper is unavailable.
- No Unity.exe is discoverable on the host, so current-head Unity XML cannot be regenerated.

### Root cause
- External authentication/session state and the Unity installation are unavailable in the current execution environment; this is not a source-code failure.

### Resolution
- Do not fabricate a PR URL, Actions result, or current-head Unity pass result.
- Preserve the already-pushed commits and record the exact static verification that remains reproducible.

### Verification
- Repository validation: 236 assets, 236 meta files, 148 matrix entries, 110 evidence references, 0 violations.
- Repository validation and copyright regression suites pass under Windows PowerShell 5.1 and PowerShell 7.
- `git diff --check` is clean and the worktree has no tracked or untracked changes.

## 2026-08-04 - M3-C1 delivery record correction and contract gaps

### 现象
- 原始记录误写为 M3-C1 没有 Draft PR。
- 审查发现 chunk sentinel 枚举含有未实现的单零字段策略，且 LZO backend 合同未完整拒绝输入消费、输出、身份、诊断和异常边界。
- 之前的 Base64 参数化 case 数量不能代表独立行为覆盖。

### 修复
- PR #36 已由外部 GitHub connector 创建，继续保持 Draft；本地 `gh auth` 仍无效，不能把 PR 创建归因于本地 gh 恢复。
- 删除无效的 `AllowOneZeroField`，单零字段统一结构化失败，`0/0` 仅显式 terminator policy 接受。
- LZO 请求和 pipeline 增加 RawLzo1X、输入/输出预算、取消、身份、精确 consumed/produced、diagnostic、provenance 和异常 fail-closed 合同；仍不实现 LZO 算法。
- 将测试重构为 108 个独立执行 case，不使用等价 Base64 输入填充数量。

### 当前验证边界
- Git push 已在先前交付完成；本轮修复尚未提交或推送。
- Unity Hub 记录并确认 Unity 2022.3.60f1c1 可执行文件存在；当前 HEAD Unity 测试必须重新生成，历史 XML 不作为当前证据。
- PR #36 保持 Draft，未 Ready、未合并；后续 M3-C2、IsoMap、Overlay、Preview、TMP、palette 和 renderer 均未开始。
## 2026-08-04 - M3-C2 Unity and remote delivery remain environment-blocked

### Symptom
- Current M3-C2 HEAD has no newly generated Unity focused/full XML.
- This branch has not yet completed remote push or Draft PR creation; no PR URL or Actions result is claimable.

### Root cause
The current host has unstable Unity process-launch behavior, and the GitHub credential/prompt session is unavailable. This is not evidence of a production-code failure.

### Resolution
Keep synthetic evidence and compatibility boundaries explicit, do not reuse historical XML as current-head proof, and defer remote delivery until a usable Unity/GitHub host is available.

### Verification
Current reproducible checks are limited to clean source diff validation and repository state. Unity, wrappers, copyright, and Repository safety remain unverified for this HEAD.
## 2026-08-05 - PR #42 diagnostic budget fail-open review

### 现象
- reader/indexer 的 `IsSuccess` 可被诊断列表容量间接影响；当 `MaxDiagnostics=0` 或诊断预算已满时，错误可能无法进入列表。

### 根因
- 执行结果状态仅由已保存诊断推导，诊断列表不是可靠的完成状态载体。

### 解决方案
- 引入独立 `IsoMapExecutionState`，错误先更新失败状态，再按预算保存或抑制诊断；暴露最高严重级别和抑制计数。
- reader、coordinate analyzer 和 packed result 统一使用完成状态。

### 验证方式
- 新增零诊断预算、预算填满后错误、duplicate policy 和 trailer overflow 测试；修复后必须重新生成当前 HEAD XML。

## 2026-08-07 - M3-C2 P1 resolution state

- Resolved the remaining packed-result aggregation gap: child packed,
  record, and coordinate execution state now contributes to top-level status,
  fatal flag, highest severity, and suppressed count.
- Current-head Unity focused/full EditMode and PlayMode remain `NotRun`; no
  historical XML is being reused. The required Unity Editor executable is not
  installed on this host.
- PS5.1 and PS7 repository validation/copyright scans pass; regression suites
  pass with 46 repository-validation cases and 22 copyright cases.

## 2026-08-08 - M3-C3 final-tree gate status

### 现象

M3-C3 raw Overlay array code and focused tests are present on
`feature/m3-c3-overlay-packed-array-foundation`, but the final documentation
and evidence commit has not yet been pushed.

### 根因

The implementation was intentionally staged before documentation and gate
refresh so the code/test commit remains independently reviewable.

### 解决方案

Finish the ADR, format document, compatibility/evidence updates, and
development records; then run the current final tree through Unity, repository,
copyright, wrapper, and safety gates. Fail closed if any host gate is blocked.

### 验证方式

The focused worktree XML is 51/51 passed and includes the new
`OverlayPackedArrayTests`; full-suite results are now current for validation
commit `82fa0239edafd7174a6386a1fc80f43b6440f169`: focused 51/51, EditMode
1148/1148, and PlayMode 1/1. The remaining gate is Repository safety for the
pushed documentation-only result-record head.

## 2026-08-08 - M3-C3 finding-closure execution provenance

### 现象

独立复审发现 unknown nested packed-policy enum 可在 adapter 边界发生默认化或被
宽泛异常包装，`OverlaySectionInput` 还会无界 `ToArray()` 任意 occurrence source，
且 packed byte getter 会暴露可变内部数组。旧 evidence 也将祖先 commit 的 Unity XML
称为 current-head 结果。

### 处理

- 所有 nested packed policy 先返回 `InvalidPolicy`/`InvalidPackedPolicy`，再读取
  occurrence；无默认 profile 或 fallback。
- Overlay reader 仅在 `MaxFragments + 1` 的有界 probe 内快照 occurrence；超限、null
  occurrence 和 source exception 均 fail-closed。
- `DecodedBytes` 与 `BlockOutputs` 均改为防御性 snapshots；Overlay raw copy 与 packed
  snapshot 不再可被调用者 mutation 破坏。
- evidence 改为明确 historical execution commit，并将 post-finding-closure Unity 与
  Repository safety 标为 `NotRun`，直至新的 code/test HEAD 实际完成。

### 当前边界

- 当前主机未发现 Unity 2022.3.60f1c1 Editor；不得复用旧 XML 作为本次修复后的通过
  证据。
- 当前 candidate 的 PS5.1 repository validation 通过（244 assets、244 meta、149 matrix、
  114 evidence、0 violations）。PS7 为 `NotRun / EnvironmentBlocked`；repository-
  validation regressions 仅有 PS5.1 fixture phase 的 23 项通过，PS7 child 以 Windows
  error 1312 阻塞，整体不能记为通过。
- 当前 candidate 的 Unity、copyright、copyright regressions 和 content wrappers 均为
  `NotRun / EnvironmentBlocked`。implementation candidate 的 Repository safety 为
  run `31312939491`，对应 `141aed104a4c572f61f011541fa6929318388dbd`，
  `completed / success`；docs/evidence-only 新 HEAD 仍需新的 exact-head safety。

## 2026-08-09 - M3-C3 P2-2 evidence/provenance closure

### 现象

`m3c3-overlay-packed-array-synthetic-20260808.yml` 将祖先执行结果与当前 finding
candidate 混在同一个 `verification` 层级，并把 PS7、copyright、wrapper 和
post-finding Repository safety 写成与当前事实不符的状态。

### 处理

- 将 evidence 拆为绑定 `82fa0239...` 的 `historical` 与绑定 `141aed1...` 的
  `current_candidate` 两组。
- 保留历史 Unity、双宿主静态门禁和 wrapper 结果，但不再将其解释为当前候选通过。
- 当前候选明确记录 Unity/PS7/copyright/wrapper 的 `NotRun / EnvironmentBlocked`，
  并记录 PS5.1 validation 的真实通过结果与 23 项 PS5.1 fixture partial pass。
- 将 run `31312939491` 记录为 implementation candidate pre-docs safety；最终
  docs-only safety 不写入 evidence，避免形成自引用提交递归。

### 验证方式

- 本轮仅修改 evidence 和开发记录；没有 C#、NUnit、Unity asset、Packages、
  ProjectSettings 或研究正文变更。
- 新 docs-only HEAD 推送后必须取得新的 exact-head Repository safety；PR 继续保持
  Open/Draft/Unmerged。

## 2026-08-10 - M3-C4 audit and gate status

### 已解决

- M3-C4 managed RawLzo1X backend 已加入现有 bounded packed pipeline；不含
  miniLZO/GPL/native/PInvoke/NuGet 或 writer。
- 当前 EditMode 已由 Unity `2022.3.60f1c1` 在当前工作树真实执行：1185/1185
  passed，Unity exit 0，forced post-result shutdown false，lockfile 已清理。

### 保留的限制

- 最近 ProjectBaseline audit status 为 `CompleteWithFailures`，不是 Complete：
  8 roots、282 mounted entries、200 candidates、200 successful sections、1
  mount-level failure；fingerprint before/after 相同。该状态和失败事实必须继续
  对外公开为失败携带的 aggregate 观察。
- audit 输入是仓库外 patched development source；没有 clean YR 1.001 或
  original-runtime confirmation。没有发布 packed/decoded bytes、records、coordinates、
  images、路径或可重建内容。
- PlayMode、双 PowerShell repository/copyright/wrapper 门禁和 Repository safety
  仍需在提交后的 exact HEAD 上执行；不得用历史 XML 代替。
## 2026-08-12 - M3-C5 verification blocker

### Observed

- The initial PreviewPack EditMode run compiled the new code but reported six
  PreviewPack failures plus one stale IsoMap guard failure. The successful
  metadata path remained `NotRun` because it did not mark execution when no
  diagnostics were emitted.
- The metadata execution-state fix and the narrowed TMP-only guard are now in
  the working tree.
- Subsequent wrapper/direct Unity invocations were blocked by the host's
  duplicate `Path`/`PATH` environment and then produced no valid result XML
  after normalization. No passing result is claimed.

### Boundary

No ProjectBaseline packed PreviewPack data was read. This issue is an
environment/test-execution blocker, not an original-runtime compatibility
claim.

## 2026-08-12 - M3-C5 closeout state

The configured read-only PreviewPack ProjectBaseline audit is implemented and
executed. It returned `CompleteWithFailures` with 184 candidates, 184 exact
decoded streams, zero section failures, and one MIX mount-level failure. The
failure-bearing aggregate is retained and is not promoted to compatibility.

Current Unity EditMode on the repaired tree is 1210/1210 passed. The original
1198/1205 XML remains historical pre-fix evidence. PlayMode and final pushed
HEAD delivery gates are still pending until their own current-head artifacts
exist; no historical XML is reused.

## 2026-08-14 - M6-C1 verification state

- M6-C1 provider-neutral snapshot code compiled and current EditMode completed
  1487/1487 with 22 presentation behavior methods.
- The evidence is synthetic/project-enhancement only. No ProjectBaseline packed
  data was read, and no original-runtime presentation or renderer claim is made.
- Full M6 delivery gates, PlayMode, and exact-head Repository safety remain
  pending until the M6-C1 branch is pushed and its Draft PR is created.
# 2026-08-12 - M3-C6 delivery notes

- The configured patched ProjectBaseline TMP/theater audit completed with
  failures because no named TMP candidate was available and one aggregate
  failure remained. This is recorded as an audit result, not hidden or
  promoted to success.
- Unity 2022.3.60f1c1 current-head EditMode completed 1249/1249 passed after
  the audit namespace fix. Full delivery gates remain separately reportable.
M3-C7 ProjectBaseline map-driven audit currently reports CompleteWithNoCandidates for the configured patched source; no map binding is fabricated and no compatibility claim is promoted.
## 2026-08-13 - M3-C8 ProjectBaseline integration limitation

The configured patched ProjectBaseline exposes packed IsoMap/Preview candidates
but no named TMP candidates and no map-driven terrain candidates. The C8
aggregate therefore remains `CompleteWithFailures`; this is not original-runtime
evidence and does not claim a fully bound real map.
## M6-C2 limits

- VXL normal tables, axis conversion, scale/transform composition, and HVA
  frame-major versus section-major semantics remain unresolved.
- SHP flags 2 and the previously documented ProjectBaseline flags-3 row-width
  conflict remain strict decoder boundaries.
- ProjectBaseline packed visual data was not read; no original-runtime or pixel
  parity claim is made.

## M6-C3 limits

- C3 synthetic projection and chunking do not establish original YR camera,
  depth-sort, TMP/theater, palette, or renderer parity.
- The Unity adapter is geometry-only and intentionally creates one bounded
  Mesh per chunk rather than tile GameObjects. No ProjectBaseline packed
terrain/visual data was read.

## M6-C4 limits

- Public research does not establish the original runtime family comparator,
  foundation/depth semantics, or palette parity; C4 remains synthetic/configured.
- Draw commands are an adapter seam, not a complete renderer, and no
  ProjectBaseline packed visual payload was read.

## M6-C5 limits

- M6-C5 is synthetic/configured only. Per-pixel depth, occlusion, projected
  shadow behavior, fog-grid semantics, palette parity, weather/audio, and
  original-runtime draw order remain unresolved.
- No ProjectBaseline packed visual payload was read or published; no renderer,
  writer, or new LZO algorithm was added.
