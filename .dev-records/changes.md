# 变更记录

## 2026-08-13 - M4-C4 commands missions targeting autonomy

- Added declarative Human/ComputerAI/Script/Trigger/Internal command requests,
  bounded replace/append queues, raw-preserving runtime mission snapshots,
  spatial-index perception, profile target scoring, target-memory hysteresis,
  deterministic arbitration, forced player authority, and hold/autonomy rules.
- Focused EditMode: 1331/1331 passed, including 18 C4 methods; no combat or
  ProjectBaseline read was added.

## 2026-08-04 - M3-C1 contract fixes and behavioral matrix

### 变更范围
- PackedMap chunk sentinel、LZO backend/pipeline、Strict Base64、fragment collector 和 synthetic EditMode matrix。

### 具体改动
- 移除 `AllowOneZeroField`；单零字段返回 `ChunkZeroFieldInvalid`，`0/0` 只接受显式 terminator policy。
- LZO request 验证 codec、bounded input、exact output、output budget、cancellation 和 provenance；pipeline 拒绝 consumed mismatch、长度 mismatch、空 identity、error diagnostic、null、异常、取消和不匹配 provenance。
- Strict Base64 增加 canonical pad-bit 校验；collector 保留 raw values、source order、numeric diagnostics、预算和 provenance。
- 测试矩阵达到 109 个独立执行 case（89 `[Test]` + 20 参数化执行 case），不使用等价 Base64 输入填充数量；pipeline 对未知 codec 先行结构化拒绝。

### 验证情况
- 最终当前 HEAD PackedMap 聚焦 EditMode `109/109`，完整 EditMode `933/933`，PlayMode `1/1`；XML 均完整且无失败/跳过/不确定项。
- Unity 聚焦与 PlayMode 在结果写出后需要受控 post-result shutdown；Unity 退出码均为 0，零字节普通 `Temp/UnityLockfile` 已按既有规则清理。
- Windows PowerShell 5.1 与 PowerShell 7 repository validation、copyright scan 及全部回归均 exit 0；仓库统计 236 assets/236 meta、148 matrix、110 evidence、0 violations。
- `git diff --check` 通过；本机系统 csc 不支持项目使用的现代 C# 语法，不能替代 Unity Roslyn。
- ProjectBaseline packed audit 仍未执行；无 LZO 算法、无原版 payload、无 IsoMap/Overlay/Preview/TMP/palette/renderer。

### 风险
- 当前修复尚未提交/推送；PR #36 body 和 Actions 状态需在可用 GitHub connector/凭据下更新，不能伪造。

## 2026-08-04 - M3-C1 core hardening

- 为 chunk envelope 保存完整 provenance chain，并在 window/stream/materialized 输入路径执行显式输入预算。
- 为 Format80 输入预算和 LZO request 增加结构化合同；collector 增加 `MaxFragments + 1` 惰性枚举停止测试。
- 新增 provenance、预算和结果不可变性聚焦测试；未实现 LZO 算法、IsoMap/Overlay/Preview/TMP、palette 或渲染。

## 2026-08-04 - M3-C1 packed map compression foundation

### 变更范围
- 新增 `PackedMap` Core 模型、诊断、预算、fragment collector、strict Base64、chunk envelope、Format80 decoder、LZO backend contract 和 pipeline。
- 新增 122 个合成 EditMode 用例及 Unity `.meta`。
- 新增 ADR、格式说明、合成 evidence、compatibility matrix 条目和 README 入口。

### 具体改动
- Collector 保留 occurrence/raw key/value/source/provenance，支持 source order、numeric ascending unique、strict sequential policies。
- Base64 在调用 .NET primitive 前执行 alphabet、padding、长度、whitespace 和预算验证。
- Chunk reader 保留 block ordinal/offset/sizes/payload，`0/0` 仅在显式 sentinel policy 下接受。
- Format80 支持五类命令、overlap copy、exact output、terminator、trailing input 和结构化诊断。
- LZO 无 backend 时返回 `BackendUnavailable`，不产生占位数据。

### 验证情况
- PackedMap 聚焦 EditMode XML：122/122 passed，0 failed。
- ProjectBaseline packed audit：未运行，符合本轮边界。

### 风险
- 原版运行时 codec profile、LZO 算法和地图特定 record 语义仍未确认。

## 2026-08-04 - M3-C1 packed map compression foundation

### 变更范围
- New standalone Core foundation for packed INI fragments, strict Base64, chunk envelopes, explicit Format80 profiles, LZO backend contracts and codec-neutral orchestration.

### 限制
- No ProjectBaseline packed audit, miniLZO, native plugin, IsoMap/Overlay/Preview/TMP, writer or rendering.

### 验证情况
- To be updated after focused synthetic tests and repository gates.

## 2026-08-03 - 项目级架构和来源边界冻结

### 变更范围
- 仅文档、第三方元数据台账和开发记录；不修改 SHP forensic 或 production decoder。

### 具体改动
- 新增 legacy visual provider ADR、现代视觉资产管线和工程可维护性规范。
- 固定唯一 ProjectBaseline runtime root，并排除 FinalAlert 2、参考工具、手工解包和临时目录。
- 以文件名、长度和 SHA-256 登记 18 个社区教程文件，正文不进入仓库。

### 验证情况
- 教程目录复核为 18 个文件、1,034,682 字节，元数据与登记值一致。

### 风险
- 社区教程许可证未确认，只能 reference-only；其语义不能单独证明 stock runtime 行为。

## 2026-08-03 - M2-SHP1F independent RLE row-width probe

### Change scope
- Audit-only SHP(TS) flags-3 classification; no production decoder changes.

### Concrete changes
- Added bounded scalar row analysis, baseline drift lock, conditional Stage B, decision gates, sanitized serializer, Editor command, wrapper, and tests.
- Added ADR, format notes, compatibility evidence, matrix references, README status, and development records.

### Verification
- Focused EditMode: 33/33 passed.
- Full EditMode XML: 808/808 passed; wrapper exit 1 because Process.ExitCode was empty.
- PlayMode XML: 1/1 passed; wrapper exit 1 for the same reason.
- PS 5.1 and PS 7 wrapper contract tests: 6/6 passed in each host.
- Actual PS 5.1 and PS 7 forensic wrappers: exit 0, Unity exit 0, forced shutdown false, decision B.
- Repository validation and copyright scan passed in PS 5.1 and PS 7; copyright violations: 0.

### Risk
- The result narrows the local conflict but does not prove original runtime behavior or authorize a general crop rule.

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

### 远程交付
- 实现提交：`806335f78b6fc3b8fc026c582999c12f888a7def`。
- Draft PR：`https://github.com/ShrinkShi/RA2YR/pull/2`，base `main`，未自动合并。
- GitHub Actions 首轮 `Repository safety`：通过；运行 ID `30736714207`。

## 2026-08-02 - WP-02B 安全有界二进制读取基础

### 变更范围
- 仅包含 Core 有界二进制输入、资源预算、诊断、尾部策略、合成测试及 WP-02A 指定回归。

### 具体改动
- 新增明确 little-endian 的 8/16/32/64 位有符号与无符号读取、精确读取、跳过、窥视、绝对偏移和有界子区间。
- Stream 快照循环支持短读、零读 EOF、不可 seek 输入的调用方声明边界、可 seek 输入的剩余范围推断和 `leaveOpen` 所有权。
- 所有子 reader 共用七类有限预算；文件驱动长度和数量先校验，再以 checked arithmetic 转换、累计或分配。
- 新增 15 类结构化诊断码和四种显式尾部策略，不复制物理路径、正文或原始 I/O 错误消息。
- session、reader、尾部策略和完成证明保持 Core internal；根或子完成后封闭，未决子区间阻止父完成。
- 修复 `LogicalContentPath` 补充平面大小写散列契约，并为 `ContentResolutionSource.TotalBytes` 提供受控溢出诊断。
- 新增架构文档、ADR、兼容证据和矩阵状态更新；未提升任何具体文件格式。

### 审查加固
- 子完成只携带本次诊断，最终 root 复用封存的只读会话视图，避免诊断历史 O(N²) 复制。
- `childDepth` checked 溢出映射到 `ArithmeticOverflow`，负 allocation reservation 映射到 `InvalidLength`。
- seekable 工厂委托快照后不再重复 Dispose，并结构化处理 `CanSeek` 的 `NotSupportedException`。

### 验证状态
- 当前 EditMode 结果 XML：152/152 通过；二进制测试 64 项，WP-02A 新回归 15 项，共新增 79 个 NUnit case。
- PlayMode 结果 XML：1/1 通过；两次 Unity 调用均在日志结束后未提供进程退出码，严格封装器按失败关闭返回非零，且无残留进程或锁。
- 双 PowerShell 仓库验证通过：51 个 Assets 条目、51 个 `.meta`、120 个矩阵条目、11 个证据引用、0 违规；合成回归 46/46。
- 双 PowerShell 暂存态版权扫描一致：163 个候选、13/13 ignore probes、0 个禁止物理根、0 违规；合成回归 22/22。
- 实现提交 `d127ce665112f978b128672a6c8d467c42bb14a1` 已普通推送；Draft PR #3 以 `main` 为 base 创建且保持未合并。
- 首个 PR head 的 GitHub Actions `Repository safety` run `30739931593` 全部通过；远程交付记录提交后的最终 head 状态在交付报告中如实报告。

## 2026-08-02 - WP-02C MIX 研究与独立实现边界

### 变更范围
- 仅包含受控格式研究、第三方来源身份、许可证边界、架构决策和未实现兼容条目。

### 具体改动
- 固定 SourceForge XCC 原始源码包、SVN r1201 和 OmniBlade encoding commit；全部仅在仓库外保存。
- 登记本机 XCC Mixer 的脱敏静态身份和工具箱再分发事实，不将二进制提交到仓库。
- 记录经典/扩展 MIX、加密目录、文件名 ID、payload-only SHA-1 和嵌套窗口事实。
- 明确 XCC/OpenRA GPL 代码只可 reference-only，`code_imported: false`；通用 Blowfish 依据作者公开的 license-free 定义独立实现。
- 新增 MIX 细分兼容条目但全部保持 `未实现`；研究证据不构成状态提升。

### 验证情况
- 双 PowerShell 仓库验证均通过：129 个矩阵条目、21 个证据引用、0 违规。
- ProjectBaseline 研究前后元数据指纹一致；未启动 XCC，未修改或复制权威内容。

## 2026-08-02 - WP-02C MIX 读写、加密、嵌套与 XCC 互操作

### 变更范围
- 只实现 MIX 容器基础、外部内容挂载、ProjectBaseline 审计、XCC 合成互操作、文档和测试；不解释 PAL 或其他 payload，不实现游戏逻辑。

### 具体改动
- 新增 64 位有界 seekable window 会话，子窗口共享读/分配/范围预算并受父区间限制。
- 新增经典、扩展、零 flags 扩展、加密目录和 checksum MIX reader，损坏输入无部分成功。
- 新增 ASCII invariant 文件名 ID、未知数字 ID、XCC 名称数据库受控候选解析。
- 新增 DeterministicRebuild/PreserveEntryOrder writer、0 字节条目、显式 key source 加密、checksum 写入、外部临时文件与原子发布。
- 新增嵌套挂载、循环/深度/归档/条目预算、混合 loose/MIX priority 和完整 provenance。
- 新增基线审计与 XCC 三阶段受控入口；完整 manifest、工具副本和提取物全部留在仓库外。
- 新增 MIX 架构、格式说明、ADR 0010、三份交付证据、来源台账和兼容矩阵状态。

### 验证情况
- Unity EditMode 343/343、PlayMode 1/1；相对 WP-02B 新增 191 个 NUnit case，编译错误 0。
- Unity 测试包装器退出码 0；两个 Editor 子进程均在结果写出后由包装器执行 30 秒收尾。
- Windows PowerShell 5.1/PowerShell 7 仓库门禁通过：107 Assets/107 meta、129 矩阵项、38 证据引用、0 duplicate GUID/违规；回归 46/46。
- 双 PowerShell 版权扫描 0 违规、13/13 ignore probes；回归 22/22。
- ProjectBaseline 完整外部 MIX manifest SHA-256 `D2CA24651D68FA1AE1DF90B366CD20F07D67889D5F0B9F5CCC7F9278BA8321D4`。
- XCC A-D 语义往返通过；XCC archive 与 PreserveEntryOrder rebuild 字节不一致，未宣称 byte identity。

### 风险
- ProjectBaseline 含增补包和兼容补丁，不构成 clean YR 1.001 原版对照。
- XCC GUI 无稳定 CLI，真实执行留作受控本地门禁，CI 只能验证合成合约。
- 新 key source 生成、完整嵌套树重写和 PAL payload 解释仍未实现。

## 2026-08-03 - WP-02D PAL 研究边界

### 变更范围
- 仅记录 PAL 格式、严格性、显示转换分歧、许可证来源和黄金样本来源预检；不含解析器或渲染实现。

### 具体改动
- 新增 PAL 格式说明和 ADR 0011，固定不可变原始模型与显式转换策略。
- 固定独立 `cnc-formats` commit，并扩展 XCC/OpenRA reference-only 用途记录。
- 记录三个 ProjectBaseline 样本来源、长度和哈希仍与 WP-02C 证据一致。

### 验证情况
- 未启动 XCC GUI，未修改权威基线，未提交 PAL 正文或完整颜色表。
- `format.pal` 暂不提升，等待实现、合成测试、正式黄金验证和全部仓库门禁。

### 风险
- 当前没有原版 YR 视觉捕获支持唯一显示转换默认值。

## 2026-08-03 - WP-02D PAL 严格解析与黄金验证

### 变更范围
- 仅实现 Core PAL 原始模型、严格解析、显式显示转换、MIX entry window 黄金审计、合成测试和脱敏交付；不实现任何渲染或游戏逻辑。

### 具体改动
- 新增 256×RGB 不可变原始模型，严格要求 768 字节、0..63 通道和完整消费，不返回部分成功。
- 复用 WP-02B 预算/诊断和 WP-02C seekable window/MIX 虚拟源，支持 Memory、Stream 与嵌套 MIX entry window。
- 保留 `ShiftLeftTwo`、`ReplicateHighBits`、`ScaleToFullRangeRounded`、`XccScaleToFullRangeFloor` 四个显式策略，不设置原版默认。
- 固定规范化模型哈希 schema，并对三个 ProjectBaseline payload 的 ID、来源链、长度、文件哈希和模型哈希 fail-closed。
- 完整逐项颜色清单仅原子写入仓库外 Cache；公开摘要和证据不含正文、完整颜色表或绝对路径。
- 新增受控 Editor/PowerShell 黄金审计入口及双 PowerShell 回归。

### 验证状态
- EditMode XML 413/413、PlayMode XML 1/1；相对 WP-02C 新增 70 个 NUnit case。
- PAL 审计包装器真实退出 0，三个固定样本全部通过且诊断为 0。
- 双 PowerShell 仓库验证为 128 Assets/128 meta、129 个矩阵项、41 个证据引用、0 违规；版权扫描 316 个暂存候选、13/13 ignore probes、0 违规。

### 风险
- ProjectBaseline 包含地图、音乐和兼容补丁，不是 clean YR 1.001。
- 当前没有视觉对照，`format.pal` 只能提升到可解析；写回、Texture2D、Shader、玩家色和剧院选择仍未实现。

## 2026-08-03 - WP-02E CSF 严格解析与黄金验证

### 变更范围
- 仅实现 CSF v3 只读格式层、受控黄金审计和交付证据；不实现 writer、运行时本地化、UI 或游戏逻辑。

### 具体改动
- 新增精确 marker/version/count/tail 校验、独立资源预算、结构化诊断和 Memory/Stream/MIX window 输入。
- 新增不可变有序模型，保留标签大小写、重复标签、多值顺序、原始 UTF-16 code units、普通/扩展类别和独立 ASCII extra。
- 新增 `RA2YR.CSF.RAW.V1` 规范化模型哈希及固定合成向量。
- 新增固定 `langmd.mix -> ra2md.csf` 审计、仓库外完整逐记录 manifest、脱敏摘要、Editor 命令和 PS5.1/7 包装器。
- 新增 66 个格式合成用例、10 个黄金审计用例和双 PowerShell 10 项包装器回归。

### 验证状态
- EditMode XML 489/489、PlayMode XML 1/1；黄金审计 Unity 进程退出码 0。
- 两个 Unity 测试包装器均因空 `Process.ExitCode` fail-closed 返回 1，完整 XML 分别为 Passed。
- 固定 payload 和模型 SHA 命中，5211 标签/值完整消费且诊断为 0。
- 双 PowerShell 仓库验证为 149 Assets/149 meta、129 个矩阵项、44 个证据引用、0 违规；最终版权扫描为 359 个暂存候选、0 个未跟踪候选、13/13 ignore probes、0 违规。

### 风险
- 黄金样本来自 patched ProjectBaseline，不是 clean YR 1.001。
- 原版运行时查找、重复项胜出、语言回退、写回和 UI 表现均未验证。

## 2026-08-03 - WP-02F INI 原始字节文档和 identity writer

### 变更范围
- 仅实现 INI 物理字节、结构分类、Opaque 保留、显式编码视图、未修改 identity writer 和 ProjectBaseline 审计；不实现语义编辑或运行时规则。

### 具体改动
- 新增 BOM/逐行换行/原始偏移/空白/重复项/未知行保留的不可变模型。
- 新增 Memory、seekable/short-read Stream 和 MIX entry window 输入，统一限制输入、行、节点、原始字节和分配预算。
- 未确认的行内分号保留在值中并报告诊断；未确认的节尾内容成为 Opaque 且不会激活节。
- 新增只返回完整文档或失败结果的严格 reader，以及逐字节复制、预算和哈希自证的 identity writer。
- 固定四个黄金 payload 与 canonical model SHA；外部 Cache 保存完整逐行 manifest 和 identity 文件，公开证据只含聚合。
- 新增 55 个格式用例、16 个审计用例和双 PowerShell 9 项包装器回归。

### 验证状态
- EditMode XML 560/560、PlayMode XML 1/1；新增 71 个 NUnit case。
- INI 黄金审计包装器真实退出 0；四个固定文档均通过 payload、model 和 identity 验证。
- 双 PowerShell 仓库验证为 170 Assets/170 meta、133 个矩阵项、56 个证据引用、0 违规；版权扫描和 13/13 ignore probes 通过。

### 风险
- 无 BOM 单字节文件的原版代码页、分号语义、重复项胜出和跨 MIX precedence 未确认。
- `往返通过` 仅限未修改 byte identity，不覆盖编辑、FinalAlert 2 或原版 writer 行为。
## 2026-08-03 - WP-02G1 INI runtime-resolution foundation

- Added UnityEngine-free explicit INI load layers, evidence-labelled policy
  dimensions, deterministic candidate resolution, budgets, diagnostics, and
  complete per-value provenance traces.
- Added aggregate Opaque/semicolon auditing without original text publication.
- Added a controlled ProjectBaseline runtime-resolution audit mode and shared
  PowerShell 5.1/7 wrapper gates.
- Preserved both `rulesmd.ini` and both `soundmd.ini` candidates without a
  winner; no original program or GUI tool was started.
- Added ADR 0015, runtime-resolution format notes, research/synthetic/local
  evidence, third-party reference records, and compatibility entries.
- Full EditMode result after implementation: 611/611 passed; WP-02G1 adds 51
  cases over the WP-02F 560-test baseline.

## 2026-08-03 - PR #8 review-fix commit

- 修复 UTF-16LE/BE 分号、ASCII 空格和 Tab 的精确字节序识别，并让名称、
  值读取和 syntax audit 共享规则。
- 将候选输入改为 `MaxDocuments + 1` 有界枚举；收窄 `IniLoadPlan` 构造契约。
- 将 source ID provenance 比较改为 `Ordinal`。
- 新增 16 个 EditMode case、修复 evidence、ADR/格式说明、兼容矩阵引用和
  开发记录；不改变 ProjectBaseline precedence 或任何 typed view 状态。

## 2026-08-03 - WP-02G2 最小 Rules/Art 资源引用视图

### 变更范围
- 只实现显式 typed scalar、五类资源注册表、十个 Art 资源字段、来源追踪和脱敏 ProjectBaseline 聚合；未实现完整 Rules/Art、SHP、VXL 或游戏逻辑。

### 具体改动
- typed view 只接受 `Complete` resolution；Ambiguous/Failed 不返回文档。
- 保留 winner、overridden candidates、physical line ID、source ID 和完整 MIX provenance。
- 缺失、非法和风险影响分别记录；不执行节名 Image fallback、扩展名补全、默认值或拼写修复。
- 两个 `rulesmd.ini` 候选分别按 `ConfiguredForTesting` 审计，不合并、不选择 stock winner。
- 新增只读 Editor/PS5.1/PS7 审计入口、ADR 0016、格式说明、合成证据和脱敏本地证据。

### 当前验证
- 新增 47 个 EditMode case，最终全量结果为 EditMode 674/674、PlayMode 1/1；总包装器退出 0，两种模式均在完整 XML 后执行受控收尾。
- ProjectBaseline typed audit Unity 真实退出 0；三个输入来源追踪覆盖完整。
- 所有 ProjectBaseline typed 结果因保留的 Opaque/分号/重复风险而明确为 `Incomplete`，未伪造完整语义。

## 2026-08-03 - PR #10 review fixes

- 合并 PR #9 后的 `main`，不改写 WP-02G2 原有三个提交，也不修改或实现 SHP。
- Art 多重名称匹配改为真正的 `Ambiguous` 字段：无单一 `Parsed`、保留全部候选和来源链、不生成 reference 或 route。
- Rules registry 新增按解析后整数判断的 `DuplicateRegistryOrdinal`；冲突仅限同一 registry，所有原始条目继续保留。
- 新增 4 个 EditMode case；阶段性全量结果为 678/678，ProjectBaseline 三个规范化模型哈希和既有聚合保持不变。

## 2026-08-03 - M2-SHP1 SHP(TS) core indexed reader

- Added UnityEngine-free SHP(TS) header and directory models, bounded reader, strict raw/RLE indexed decoder, diagnostics, budgets, and canonical hashing.
- Added Memory, seekable Stream, short-read Stream, and bounded MIX-entry-window equivalence.
- Added six fixed ProjectBaseline audit profiles, repository-external per-frame manifests, sanitized repository evidence, and a controlled Editor/PowerShell entry point.
- Added 97 Unity EditMode tests and 9 SHP wrapper regression cases.
- Added format documentation, ADR 0018/0019, third-party reference records, compatibility matrix entries, README status, and delivery evidence.
- Final local validation: EditMode 775/775, PlayMode 1/1, repository regression 46/46, copyright regression 22/22, and zero wrapper or scan exit failures in PowerShell 5.1/7.

## 2026-08-03 - ProjectBaseline INI ordered semantic composition revision

- Added a ProjectBaseline-only load-plan builder with the explicit low-to-high
  order `ra2 -> ra2md -> expandmd01..99 -> loose`.
- Same-name INI documents now compose per `SectionName + KeyName`; higher layers
  override matching values while lower unique values and higher additions remain.
- Every resolved value retains its winning layer and complete overridden
  candidate chain, physical line IDs, source ID, and logical MIX provenance.
- Invalid or duplicate expand numbers and non-ProjectBaseline sources fail with
  structured diagnostics; discovery enumeration order cannot change results.
- Rules typed auditing consumes the composed two-layer result. Intradocument
  name, duplicate, semicolon, whitespace, and empty-value behavior remains an
  explicit testing policy and is not original-runtime confirmation.
- Final validation: composition 16/16, full EditMode 824/824, PlayMode 1/1,
  INI wrapper 15/15 in both PowerShell hosts, repository validation 46/46,
  copyright regression 22/22, and all real audit/gate exit codes zero.

## 2026-08-04 - PR #20 synchronized validation closeout

### Change scope
- Merged current `main` with a normal two-parent merge and semantically retained
  MAP/TMP research, map-compression research, visual-asset architecture, and
  ProjectBaseline INI composition records.
- Clarified that `IniProjectBaselineLoadPlanBuilder` is a fixed audit adapter,
  not generic runtime archive discovery or mount authority.
- Fixed the Unity test wrapper exit handshake so XML status, Unity exit code,
  wrapper exit code, and post-result shutdown are reported independently.

### Verification
- Focused EditMode: load plan 16/16, composition audit 17/17, typed audit 1/1.
- Final full Unity: EditMode 694/694 and PlayMode 1/1; Unity and wrapper exits
  were zero, with controlled post-result shutdown in both modes.
- ProjectBaseline Rules/Art aggregates and normalized model hashes remained
  stable in PowerShell 5.1 and 7.
- Repository validation passed with 194 assets, 194 meta files, 143 matrix
  entries, and 91 evidence references; regressions passed 46/46.
- Copyright scan passed with zero violations and regressions passed 22/22.
- CSF and PAL wrapper timestamp validation now accepts PowerShell 7's strict
  UTC `DateTime` coercion without weakening the PS5.1 string `Z` requirement;
  each wrapper gained one regression case.

### Risk
- The synchronized `main` does not yet contain an SHP audit wrapper; PR #20 did
  not migrate one from the separate SHP workstream.
- Original-runtime INI syntax and precedence confirmation remains unimplemented.

## 2026-08-04 - M3-C2 IsoMapPack5 raw-record foundation

### Change scope
- Added UnityEngine-free IsoMapPack5 11-byte raw records, defensive raw-byte
  views, source offsets/order, and packed provenance.
- Added explicit decoded-stream trailing policies, including exact four-zero
  trailer acceptance only under its named profile.
- Added separate coordinate occurrence/index analysis with explicit duplicate,
  axis, signedness, domain, sparse, and dense-count candidate policies.
- Added the packed-section adapter over the existing injected RawLzo1X pipeline;
  upstream failures stop record parsing and no LZO algorithm was added.
- The original M3-C2 delivery recorded 127 defined NUnit executions. At the
  previous PR #42 review head, the source defined 139 executions
  (89 `[Test]`, 50 `[TestCase]`) across 96 behavior-method declarations.

## 2026-08-05 - PR #42 review correction scope

### 变更范围
- 修复 IsoMapPack5 reader/coordinate analyzer 的诊断预算 fail-open 风险。
- 冻结三种 duplicate policy 的成功/失败语义，补充零诊断预算覆盖。
- 修复测试中的强类型字节拼接、显式 section 传递、独立 backend length mismatch 覆盖和真实 assembly 引用边界检查。
- 修复 trailer 防御性返回与 checked offset overflow 诊断。
- 更正 M3-C2 evidence 的测试计数和当前 HEAD 执行状态。

### 验证情况
- 当前尚未生成修复后 Unity XML；未将历史 XML 作为当前 HEAD 证据。
- Repository safety 需要在新提交后重新运行。

### 风险
- Unity、PS5.1/PS7、wrapper 和 copyright 门禁需在新 HEAD 上重新执行。

### Verification boundary
- `git diff --check` is clean.
- Current-head Unity XML was not generated: the desktop host's PowerShell
  process launcher fails on duplicate case-insensitive `PATH`/`Path` variables,
  and direct Unity launches made no progress before controlled termination.
- Historical M3-C1 XML is not used as M3-C2 evidence.

### Compatibility
- IsoMapPack5 reader: Synthetic; tile interpretation and coordinate runtime
  semantics: Unresolved/NotConfirmed.
- Real packed ProjectBaseline decode: NotRun; LZO remains contract-only.
- OverlayPack, PreviewPack, TMP, palette, rendering, writer, pathfinding, and
  gameplay remain outside this work package.

## 2026-08-07 - M3-C2 packed execution aggregation follow-up

- Aggregated packed, record, and coordinate child execution state at the
  IsoMapPack5 adapter boundary.
- Preserved fatal status and highest severity when diagnostic storage is full,
  including a zero-diagnostic budget; suppressed counts use saturating merge.
- Added current source coverage for packed-child failure, coordinate-child
  failure, multi-child suppression, warning severity, consumed-length stop,
  null-record termination, and deterministic duplicate-group ordering.
- At that review head, the source defined 146 NUnit executions (96 `[Test]`,
  50 `[TestCase]`) across 103 behavior-method declarations. Unity execution remains NotRun
  because the required Unity 2022.3.60f1c1 Editor executable is unavailable.
- PS5.1/PS7 repository validation and copyright gates pass with zero violations.

## 2026-08-07 - M3-C2 final independent-review corrections

- Enforced the injected RawLzo1X backend requirement before fragment or chunk
  processing; empty fragment input and a zero-block envelope are separate
  structured failures and never reach the record stage.
- Validated every M3-C2 policy/profile enum at its configuration boundary.
  Coordinate rectangle bounds require positive width and height together;
  dense-count candidates require the same complete rectangle.
- Added direct regressions for record-child failure under a zero diagnostic
  budget, suppressed-count saturation, missing coordinate stage, truncated
  chunk input, empty input, zero chunks, and invalid policy/profile values.
- At the review head before the current-head gate refresh, the source defined
  164 NUnit executions (110 `[Test]`, 54 `[TestCase]`) across 118
  behavior-method declarations; Unity was then `NotRun`.

## 2026-08-08 - M3-C2 current-head verification refresh

### Change scope
- Added bounded first-occurrence peek/replay in the packed adapter so an empty
  occurrence sequence returns `EmptyPackedInput` without unbounded materialization.
- Updated synthetic evidence and compatibility metadata with current-head XML
  results; compatibility claims remain synthetic and unconfirmed.

### Verification
- Focused IsoMapPack5 EditMode: 164/164 passed.
- Full EditMode: 1097/1097 passed; Unity exit 0; shell exit 0;
  forced post-result shutdown false.
- PlayMode: 1/1 passed; Unity exit 0; shell exit 0;
  forced post-result shutdown false.
- Repository validation, copyright, regressions, and content wrapper regressions
  passed under PS5.1 and PS7.

## 2026-08-08 - M3-C3 Overlay raw packed-array foundation

### Change scope

- Added UnityEngine-free `OverlayPackedArrayModels` and
  `OverlayPackedArrayReader` for explicit `OverlayPack` and `OverlayDataPack`
  section selection.
- Reused the M3-C1 packed pipeline with an explicit absolute Format80 profile;
  RawLzo1X remains rejected at this layer because no LZO algorithm is present.
- Added exact ordinary `512 x 512` raw-array validation, defensive byte views,
  explicit candidate storage indexes, provenance retention, and fail-closed
  child/parent execution state.
- Added 51 focused NUnit executions (37 `[Test]`, 14 `[TestCase]`) and narrowed
  the old IsoMap prohibition to Preview/TMP-only assertions.

### Verification boundary

- Focused M3-C3 worktree XML: 51/51 passed; this is synthetic behavior only.
- Final-code-tree gates at `82fa0239edafd7174a6386a1fc80f43b6440f169` passed:
  focused 51/51, EditMode 1148/1148, PlayMode 1/1, PS5.1/PS7 validation and
  copyright, repository regressions 46, copyright regressions 22, and content
  wrapper regressions 51 in each host.
- The result-record commit is documentation-only; no C# or test source changed
  after the validated tree. Repository safety remains pending until push.
- No ProjectBaseline packed data was read; no Overlay semantic registry,
  Preview, TMP, palette, renderer, gameplay, or real LZO implementation was
  added.

## 2026-08-08 - M3-C3 independent-review finding closure

### Change scope

- Packed decode policy validation now rejects unknown fragment ordering, Base64,
  chunk sentinel, codec, and Format80 variant values before source enumeration.
- Overlay section input no longer materializes arbitrary occurrence enumerables
  in its constructor; the reader takes only the configured bounded budget probe.
- Packed decode byte getters now return defensive snapshots for aggregate and
  per-block bytes.

### Evidence boundary

- The focused source now defines 61 NUnit executions (47 `[Test]`, 14
  `[TestCase]`) across 51 behavior methods.
- The prior Unity XML belongs to commit
  `82fa0239edafd7174a6386a1fc80f43b6440f169`; it is historical evidence, not
  a pass claim for this finding-closure tree. Post-closure Unity and Repository
  safety evidence remains `NotRun` until this code/test change is committed and
  executed.
- No ProjectBaseline packed data, real LZO, Overlay semantics, Preview, TMP,
  palette, rendering, writer, pathfinding, or gameplay was added.

## 2026-08-10 - M3-C4 managed RawLzo1X backend and sanitized audit

### 变更范围

- 新增 UnityEngine-free managed `RawLzo1X` decode backend，复用既有 bounded
  packed pipeline；不引入 miniLZO、GPL source、native plugin、P/Invoke、NuGet
  dependency 或 writer。
- 新增 ProjectBaseline `IsoMapPack5` 脱敏聚合 audit service、Editor command、
  wrapper 和 synthetic regression；审计读取仓库外 patched development source，
  不把 payload、记录、坐标、路径或原始片段写入仓库或 summary。
- 更新 map-packed/IsoMap 文档、ADR、兼容矩阵、third-party source ledger 和
  M3-C4 evidence；不修改 `docs/research/`。

### 验证事实

- 当前 EditMode XML：`TestResults/20260810T040346729Z-d320a64364074b80a15db4e540f9ccaf/EditMode/results.xml`，
  `1185/1185` passed，Unity exit `0`，forced post-result shutdown `false`。
- 最近一次真实外部审计：8 roots、282 mounted entries、200 candidates、200
  successful sections、1 mount-level failure、36,? decoded bytes and records are
  published only as sanitized aggregate values; status is `CompleteWithFailures`.
  Source fingerprint before/after is identical. This failure-bearing result is not
  promoted to original-runtime compatibility.
- Aggregate values are `37,166,225` decoded bytes and `3,378,675` decoded
  records; ProjectBaseline source remains external patched development content; no packed
  payload is committed.

## 2026-08-09 - M3-C3 P2-2 evidence/provenance closure

### 变更范围
- 仅修正 M3-C3 machine-readable evidence 与开发记录的 historical/current
  provenance 分层。
- 没有修改 production C#、NUnit tests、Unity assets、Packages、ProjectSettings、
  compatibility semantics 或研究正文。

### 具体改动
- `verification.historical` 绑定 validation commit
  `82fa0239edafd7174a6386a1fc80f43b6440f169`。
- `verification.current_candidate` 绑定 finding candidate
  `141aed104a4c572f61f011541fa6929318388dbd`，明确记录当前环境阻塞。
- implementation candidate safety 保留为 run `31312939491`；docs-only final safety
  留给推送后的 PR delivery metadata，避免 evidence 自引用递归。

### 验证情况
- `git diff --check` 与 docs-only 文件范围检查待提交后执行。
- Unity、PS7、copyright、wrapper 本轮不重跑；保持真实 `NotRun / EnvironmentBlocked`。

### 风险
- 新 docs-only HEAD 必须取得独立 exact-head Repository safety；旧 run
  `31312939491` 不能复用。
## 2026-08-12 - M3-C5 PreviewPack raw component foundation

### Changes

- Added `PreviewPackModels`, `PreviewMetadataReader`, and
  `PreviewPackSectionReader` under the UnityEngine-free packed-map Core.
- Added explicit metadata selection, four-field raw `Size` preservation,
  checked exact three-component length, immutable decoded bytes, channel/row
  profiles, provenance, limits, and independent execution state.
- Added 20 PreviewPack test methods and a synthetic evidence record. The old
  IsoMap architecture guard now forbids TMP only; PreviewPack is the current
  work package and is not a rendering implementation.
- Added ADR-0027 and the PreviewPack format/compatibility documentation.

### Verification status

- The previous failed EditMode XML identified a missing successful-execution
  mark in the metadata reader; that fix is included here.
- A post-fix Unity invocation was attempted with a normalized `Path`/`PATH`
  environment but produced no valid current-head XML. Unity results remain
  `NotRun`; no historical XML is reused.
- Post-commit PS5.1 static gates and hosted exact-head Repository safety run
  `31516022532` completed successfully for the recorded HEAD.
- ProjectBaseline packed PreviewPack data was not read.

## 2026-08-12 - M3-C5 maintainer closeout and configured PreviewPack audit

### Changes

- Added the read-only `PreviewPackProjectBaselineAuditService`, sanitized
  Editor command, wrapper, and wrapper regression. It reads only the enabled
  `YR1001_ProjectBaseline` source from the configured external-content root,
  reuses the M3-C4 managed `RawLzo1X` backend, and emits aggregate counts and
  hashes only.
- The configured audit completed with failures: 184 candidate entries, 184
  exact decoded streams, zero section failures, one MIX mount-level failure,
  all 184 dimensions positive, fields 0/1 zero for all 184 entries, fragment
  range 54..1138, and chunk range 2..15. The result remains
  `CompleteWithFailures`; it is not runtime proof.
- The first current-tree Unity XML was a pre-fix failure (1198/1205). After
  fixing metadata execution state, cancellation policy handling, section
  occurrence test data, chunk-declared-length fixtures, and the stale IsoMap
  guard, the current tree produced 1210/1210 EditMode passes. PlayMode and
  final pushed-head gates remain separately reported until executed.

### Boundary

M3-C5 adds no new LZO codec or writer. No rendering, palette, TMP, theater,
map loading, or gameplay behavior was added, and no per-map ProjectBaseline
payload or path is published.
# 2026-08-12 - M3-C6 TMP/theater foundation

- Added Unity-free TMP raw models and bounded reader for the 16-byte file
  header, offset table, exact 52-byte cell header, raw flags, and explicit
  declared/sequential plane profiles.
- Added composed-theater control views, six profile descriptors, deterministic
  TileSet ranges, GlobalTileId lookup, and explicit variation/fallback asset
  resolution traces.
- Added sanitized read-only ProjectBaseline audit command and wrapper. The
  current configured source yielded `CompleteWithFailures` with zero named TMP
  candidates and one failure; this is not original-runtime evidence.
## 2026-08-12 — M3-C7 terrain composition foundation

- Added immutable Unity-free MapTerrain composition models and explicit tile, overlay, ramp, and terrain profiles.
- Added map-driven sanitized ProjectBaseline audit; current configured source completed with no map candidates.
## 2026-08-13 - M3-C8 real-map integration

- Added a read-only C1-C7 vertical integration service and sanitized wrapper.
- Executed the configured ProjectBaseline source: 200 IsoMap candidates (200 successful, 1 failed), 184 exact Preview candidates, zero named TMP candidates, and zero fully bound terrain documents.
- Classified the result as `CompleteWithFailures`; no original-runtime or clean YR 1.001 claim was promoted.
## 2026-08-13 - M3 final closeout

- Recorded the merged M3 chain through PR #46, PR #47, and PR #48 on final main
  `82e2c6a46f842d09ee9786657065c942753cc435`.
- Recorded final validation facts: EditMode 1260/1260, PlayMode 1/1,
  PS5.1/PS7 repository and copyright gates, regressions, wrappers, and
  post-main Repository safety `31670085049` all passed.
- Kept the C8 ProjectBaseline result truthful as `CompleteWithFailures` with
  200 IsoMap candidates, 184 Preview candidates, unresolved terrain binding,
  stable source fingerprint, and sanitized aggregate output only.
- This is a docs/dev-records closeout only. No code, tests, compatibility
  evidence level, or research dossier was changed; M4 and runtime semantic
  work were not started.
## 2026-08-13 - M4 P0 governance refresh

### 变更范围

- Modernized the tracked canonical three-stage requirements document without
  deleting its long-term compatibility, legal, FinalAlert, or YR 1.001 goals.
- Added M1-M3 status, explicit evidence layers, deterministic ECS/simulation
  architecture, single logical authority plus deterministic commit, Unit Tactical
  Autonomy, Manual/Assisted/Automatic modes, legal AI observation/command
  boundaries, and future Neural/Hybrid policy contracts.
- Added M4 architecture documentation and ADR-0031. No production simulation,
  Unity scene, renderer, or AI model code was added.

### 验证情况

- External and tracked requirements copies have identical SHA-256.
- This remains a documentation/governance change; Unity simulation tests were
  not run because no production simulation code changed.
## 2026-08-13 - M4-C1 deterministic ECS kernel

- Added the Unity-free `RA2YR.Simulation` assembly with generation-checked
  entities, bounded value-type component stores, ordered structural commands,
  fixed logical time, deterministic scheduling and explicit RNG streams.
- Added immutable snapshots, canonical state hashes, stable proposal ordering,
  managed sequential proposal evaluation, and Manual/Assisted/Automatic
  autonomy contracts.
- Current EditMode result for the implementation tree: 1275/1275 passed;
  these are synthetic project-enhancement tests and not original-runtime
  compatibility evidence. No ProjectBaseline packed data was read.
## 2026-08-13 - M4-C2 terrain occupancy spatial foundation

- Added bounded source-order terrain topology candidates with sparse/dense and
  duplicate diagnostics, explicit passability states, movement node/edge
  candidates, capability raw fields, and no automatic Unknown-to-Passable rule.
- Added simulation-owned static, foundation, dynamic, and reservation occupancy
  plus a deterministic ordered spatial index for insert/remove/move/neighbor
  queries.
- Current focused EditMode result: 1292/1292 passed, including 17 C2 methods;
  synthetic project-enhancement evidence only, with no ProjectBaseline reads.
## 2026-08-13 - M4-C3 pathfinding movement foundation

- Added deterministic managed A* over C2 candidate graphs, immutable results,
  explicit capability checks, cancellation, request/route/expansion budgets,
  per-tick batch limits, and invalidatable cache contracts.
- Added integer route following through Simulation-owned occupancy/reservation
  state and deterministic local-avoidance proposal ordering.
- Focused EditMode: 1313/1313 passed, including 19 C3 methods; no ProjectBaseline
  terrain data or original-runtime path claim was added.

## 2026-08-13 - M4-C5 minimal combat and abilities

- Added Unity-free weapon range/cooldown/target validation, attack proposals,
  bounded damage events, canonical health/death commit, and immutable health
  records.
- Added generic ability descriptors/state/candidates/proposals, explicit
  AutoCast/autonomy gating, retreat utility, and crush-threat proposals.
- Focused EditMode current tree: 1342/1342 passed, including 11 C5 methods;
  synthetic project-enhancement evidence only, with no ProjectBaseline reads.

## 2026-08-13 - M4-C6 scenario and agent platform

- Added raw family placement preservation, bounded owner-checked SpawnRequest
  generation, immutable legal AgentObservation, RuleBased policy fallback,
  Neural descriptor/backend contract, and headless deterministic stepping.
- Current EditMode tree: 1353/1353 passed, including 10 C6 methods; no
  ProjectBaseline data or original-runtime AI claim was added.

## 2026-08-13 - M4-C7 integrated synthetic world

- Added a bounded integer headless battle composing unit state, attack
  proposals, damage/death commit, cooldown, canonical hashing, repeated-run
  and input-order determinism checks.
- Current EditMode tree: 1361/1361 passed, including 8 C7 methods; no
  ProjectBaseline reads or renderer/gameplay parity claim was added.

## 2026-08-13 - M5-C2 resource economy foundation

- Added Unity-free raw resource cell/type contracts, explicit quantity/value
  candidates, bounded harvester cargo/capacity validation, and refinery
  acceptance/docking descriptors.
- Focused EditMode current tree: 1384/1384 passed, including 12 C2 methods;
  no ProjectBaseline packed data or original-runtime economy claim was added.
