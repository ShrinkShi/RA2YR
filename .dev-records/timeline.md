# 开发时间线

## 2026-08-03 - 冻结 runtime root、社区语义参考和现代资产边界

### 用户目标
- 将权威游戏安装与所有研究/工具目录严格分离，并建立面向现代资产的长期架构边界。

### 本轮处理
- 新增 ADR 0021、视觉资产管线和工程可维护性规则。
- 登记 RA2 DIY 2025 教程目录的 18 项脱敏元数据。
- 更新外部内容、第三方台账、README 和开发决策记录。

### 关键结论
- legacy 格式是 import adapters；simulation 只引用 `VisualAssetId`。
- 教程目录是 `CommunitySemanticReference`，不是 runtime content source。

### 影响文件
- `docs/adr/0021-legacy-formats-are-import-adapters.md`
- `docs/architecture/visual-asset-pipeline.md`
- `docs/architecture/engineering-maintainability.md`
- `docs/third-party/ra2-diy-2025-community-semantic-reference.yml`
- 架构索引、第三方台账、README 和开发记录

### 后续事项
- 以独立提交实现 ProjectBaseline ordered multi-document INI semantic composition。

## 2026-08-03 18:20 - M2-SHP1F flags-3 row-width forensic probe

### User goal
- Keep the production SHP decoder unchanged while classifying the locked 257-frame row-width conflict.

### Work completed
- Added an independent bounded scalar analyzer, conditional all-row audit, sanitized evidence, and PS 5.1/7 wrapper.
- Locked the existing row-zero aggregate before inference.
- Analyzed 9,495 declared rows after Stage A passed its five conditions.

### Key conclusion
- Decision B: 1,331 rows equal WidthRaw and 8,164 rows equal WidthRaw+1; every frame contains both classes.
- No production repair is recommended and flags-3 ProjectBaseline compatibility remains unimplemented.

### Files affected
- `Assets/RA2YR/Core/Content/ShpTs/Forensics/`
- `Assets/RA2YR/Tests/EditMode/Content/ShpTs/Forensics/`
- `Tools/Content/Invoke-ShpTsRleForensicAudit.ps1`
- `docs/adr/0020-shp-ts-rle-forensic-probe-remains-non-production.md`
- `docs/compatibility/evidence/m2-shp1f-rle-forensic-20260803.yml`

### Follow-up
- Run the final full repository gates, commit the forensic work independently, and keep PR #11 Draft.

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

## 2026-08-02 - 启动 WP-02B 安全有界二进制读取基础

### 用户目标
- 从 PR #2 合并后的最新 `main` 创建独立分支。
- 仅实现 Core 通用二进制读取、统一资源预算、结构化诊断、尾部策略和合成测试。
- 补齐 WP-02A 的 Unicode 相等—哈希契约与 `TotalBytes` 溢出回归，不改变来源优先级语义。

### 本轮处理
- 确认 PR #2 已合并，从合并提交 `bd4237c8` 创建 `feature/wp02b-bounded-binary-foundation`。
- 实现 Memory、可 seek Stream、不可 seek Stream 的同一有界快照与 little-endian 整数读取语义。
- 建立输入、单次读取、累计分配、记录、字符串、嵌套和子区间七类共享预算。
- 建立精确偏移、子区间、短读、EOF、I/O、尾部处理和不可伪造完成状态的合成回归。
- 独立审查发现并在提交前修复诊断历史 O(N²) 复制、seekable 工厂重复 Dispose、深度溢出和负分配记账风险。
- 修正逻辑路径补充平面大小写的相等—哈希契约，并将来源总字节溢出转为受控不完整解析。

### 当前验证
- 最新 EditMode XML：152/152 通过，其中二进制命名空间 64 项；相对 WP-02A 基线新增 79 个 NUnit case。
- 本轮无原版内容读取、无外部 manifest 生成，也未实现任何具体 YR 格式。
- 该轮 Unity 在完整结果写出后未自行退出，封装器按已知策略收尾；无 Unity 进程或锁残留。

### 后续事项
- 补齐 WP-02B 合成证据和兼容矩阵，再运行 PlayMode、双 PowerShell 仓库/版权门禁。
- 提交并创建独立 Draft PR，观察实际 GitHub Actions，不自动合并。

## 2026-08-02 - WP-02B 远程交付

### 本轮处理
- 创建实现提交 `d127ce665112f978b128672a6c8d467c42bb14a1`，以普通非强制推送发布独立 feature 分支。
- 创建 Draft PR #3，base 为 `main`、head 为 `feature/wp02b-bounded-binary-foundation`，未自动合并。
- GitHub Actions `Repository safety` 首个 PR head 运行 `30739931593` 全部通过。

### 关键结论
- 远程只将 `format.bounded-reader` 提升到合成测试支持的 `可解析`；具体 YR 格式、原版对照和往返状态未提升。
- Unity EditMode/PlayMode XML 通过与严格封装器缺失退出码的非零结果均已在 PR 正文和证据中保留。
- 两份外层 Git 备份继续存在，未删除或改写。

### 后续事项
- 等待最终文档记录 head 的 Actions 结果和用户审查；不得自动合并 Draft PR #3。

## 2026-08-02 17:35 - WP-02C 内容链路预检与 MIX 路线确认

### 用户目标
- 从 PR #3 合并后的最新 `main` 创建独立 WP-02C 分支。
- 以 `YR1001_ProjectBaseline` 为权威基线，建立 MIX 读取、写入、加密、嵌套挂载与 XCC Mixer 往返链路；PAL 数据解释仍不在本轮实现。

### 本轮处理
- 确认 PR #3 已合并，快进本地 `main` 到合并提交 `e9777044`，创建分支后安全重命名为 `feature/wp02c-mix-read-write`。
- 只读检查被忽略的本机内容配置、已配置来源和工作区已知外部内容目录；未扫描整盘、未写入或更改外部文件。
- 配置的 Clean 与 Unpacked 来源目录不存在；ProjectBaseline 存在但三个目标 PAL 均无松散文件匹配。
- Reference 工具集合含多组 RA1/RA2/TS/TD 混合来源的同名 768 字节 PAL；这些文件不替代权威基线内容链路。

### 关键结论
- 松散 PAL 不存在，权威基线内容主要位于 MIX；MIX 因此成为正式运行链路的必要前置。
- 不再要求 `YR1001_Unpacked`，不实现 PAL 数据解释，`format.pal` 保持 `未实现`。
- XCC/XCC 源码仅用于格式事实、算法行为、测试向量和黑盒结果；GPL 代码不得进入 Apache-2.0 仓库。

### 影响文件
- `.dev-records/timeline.md`
- `.dev-records/issues.md`
- `.dev-records/backlog.md`

### 后续事项
- 完成本机 XCC 静态识别、XCC 源码与许可证固定、MIX 头部研究和基线只读调查，再开始自主 C# 实现。

## 2026-08-02 18:35 - WP-02C MIX 受控研究完成

### 本轮处理
- 固定 OmniBlade/xcc `encoding` 精确 commit、SourceForge 原始源码包与 SVN r1201 快照；全部存于正式仓库外。
- 静态识别本机 XCC Mixer，不启动进程；确认工具箱再分发、内嵌 1.47 字符串、无版本资源、未签名。
- 只读检查 ProjectBaseline 的 8 个根级 MIX，并以前后元数据指纹和二次 SHA-256 确认未写入。
- 交叉确认经典/新式头、加密目录、文件名 ID 和尾部校验布局；`langmd.mix` 的 20 字节尾部与 payload-only SHA-1 一致。

### 关键结论
- XCC 源码严格 reference-only、`code_imported: false`；正式实现必须自主编写。
- OmniBlade 固定提交存在大写返回值未接收的回归，不能作为唯一 ID 行为依据。
- XCC Editor 可复用 key source 写加密目录，但未证明可生成新 key source；checksum 读取和保存也未形成算法证据。
- 研究证据不提升任何兼容矩阵状态，`format.pal` 仍未实现。

### 后续事项
- 先提交 Research/ADR，再实现共享预算的 seekable file window。
- 逐提交实现 reader、writer、嵌套挂载和 XCC/基线交付证据。

## 2026-08-02 22:45 - WP-02C MIX 实现与 XCC A-D 验证收口

### 用户目标
- 将 WP-02C 从 PAL 预检切换为 MIX 读取、完整重建写入、加密/校验、嵌套挂载、ProjectBaseline 目标定位和固定 XCC Mixer 双向往返。
- 严格保持 Apache-2.0/GPLv2 reference-only 边界，所有原版内容、XCC 工具和完整 manifest 留在仓库外。

### 本轮处理
- 独立实现 seekable file window、经典/扩展 MIX、文件名 ID、Westwood key envelope、Blowfish、payload-only SHA-1、确定性与保持顺序写入器。
- 建立目录到外层 MIX、内层 MIX、最终条目的有界虚拟挂载，保留每层 provenance、显式 priority 和未知数字 ID。
- 只读审计 ProjectBaseline 的全部根 MIX，递归挂载并定位七个批准目标；完整文件级结果写入仓库外 cache。
- 使用固定 XCC Mixer 1.47 完成 A-D：读取基线副本、生成合成 MIX、本工具输出由 XCC 打开/提取、XCC 输出经 PreserveEntryOrder 重建后再次由 XCC 提取。
- 依据真实 XCC 零 flags 输出补充扩展头兼容；把语义往返与字节一致分开记录。

### 关键结论
- 8 个根 MIX 中 7 个可解析，零字节 `movmd03.mix` 受控失败；共挂载 55 个归档、13,281 个条目，最大嵌套深度 1。
- 七个目标均定位；`rulesmd.ini` 有两条不同来源链，因未验证原版归档层优先级而明确保持歧义。
- XCC 可提取项目生成的 classic、checksum、encrypted 和 nested 合成归档；12 个 C 类输出和 4 个 D 类输出哈希全部一致。
- XCC 生成归档与本项目 PreserveEntryOrder 重建归档字节不同，但条目顺序、集合和负载一致；只提升语义往返。

### 影响文件
- `Assets/RA2YR/Core/Binary/Seekable/`
- `Assets/RA2YR/Core/Formats/Mix/`
- `Assets/RA2YR/Core/Content/Mix/`
- `Assets/RA2YR/Tests/EditMode/`
- `Tools/Content/`
- `docs/architecture/`、`docs/formats/`、`docs/compatibility/`、`docs/third-party/`
- `README.md`

### 后续事项
- 完成远程 Draft PR 和 Actions 实际状态验证，不自动合并。
- PAL 解释继续保持未实现，下一工作包使用已定位的三个 768 字节条目做本地黄金样本。

## 2026-08-03 - WP-02D PAL 研究与黄金来源预检

### 用户目标
- 从 PR #4 合并后的最新 `main` 创建独立 WP-02D 分支，仅实现原始 PAL 解析、显式显示转换策略和三个 MIX 内黄金样本验证。

### 本轮处理
- 快进 `main` 到 PR #4 合并提交 `10f75e95`，创建 `feature/wp02d-pal-format`。
- 只读确认三个目标仍唯一位于 `ra2.mix -> cache.mix`，长度与固定 SHA-256 均未变化，且没有松散同名 PAL。
- 交叉检查 XCC、OpenRA、独立实现及三个实际样本；确认无头、768 字节、256 个 RGB 三元组和 0..63 通道范围。
- 记录 XCC 向下取整、OpenRA 位复制、左移和最近取整之间的差异；未启动 XCC GUI。

### 关键结论
- 三个样本均无越界通道；`isotem`/`temperat` 各有 256 个不同颜色，`unittem` 有 210 个。
- 当前证据不足以指定原版 YR 显示转换默认值；研究本身不提升兼容矩阵。
- GPL 来源继续保持 reference-only、`code_imported: false`。

### 影响文件
- `docs/formats/pal.md`
- `docs/adr/0011-pal-raw-model-and-explicit-display-conversion.md`
- `docs/compatibility/evidence/wp02d-pal-research-20260803.yml`
- `docs/third-party/sources.yml`
- `THIRD_PARTY.md`
- `.dev-records/`

### 后续事项
- 提交研究边界后实现 Core 不可变模型、严格 reader、合成测试和受控黄金验证入口。

## 2026-08-03 00:55 - WP-02D PAL 实现与 ProjectBaseline 黄金验证完成

### 本轮处理
- 独立实现 UnityEngine-free PAL reader、不可变原始颜色模型、结构化诊断、预算和四个命名显示转换策略。
- 通过最小 MIX 名称目录和 `StructureOnly` 挂载只读解析 `ra2.mix -> cache.mix` 中的三个固定 PAL entry window。
- 前后重建目录索引并核对 fingerprint；固定 payload SHA 与规范化模型 SHA 任一变化均阻止发布。
- 新增外部 Cache 完整 manifest、仓库内脱敏摘要、Editor 命令和 PS5.1/PS7 包装器回归。

### 关键结论
- `isotem.pal`、`temperat.pal`、`unittem.pal` 的来源链、768 字节长度和固定 SHA 全部一致。
- 三份文件均为 256 色、通道范围 0..63、非法通道 0；不同颜色数分别为 256、256、210。
- EditMode 413/413、PlayMode 1/1、黄金审计 Unity 进程退出码 0。
- 合并测试包装器在 EditMode XML 完成后读取到空退出码并 fail-closed 返回 1；单独 PlayMode 包装器返回 0，未伪造结果。

### 后续事项
- 完成分层提交、Draft PR 和 Actions 验证；不自动合并。
- 后续视觉工作必须先建立原版截图/捕获证据，再决定显示转换默认策略。

## 2026-08-03 - WP-02E CSF 严格解析与 ProjectBaseline 黄金验证

### 用户目标
- 从 PR #5 合并后的最新 `main` 创建独立分支，只实现 CSF v3 严格只读解析、保真文档模型、合成测试和固定 `ra2md.csf` 黄金验证。

### 本轮处理
- 从合并提交 `6b378b66` 创建 `feature/wp02e-csf-format`，没有继续叠加 WP-02D 分支。
- 交叉检查 ProjectBaseline、XCC r1201、OmniBlade 固定提交和两个独立实现；所有第三方源码保持 reference-only。
- 实现有序、可保留重复标签和多值的不可变文档模型；原始 UTF-16 code-unit 顺序、普通/扩展值类别和独立 extra 字段均保留。
- 通过 `langmd.mix` 的受限 MIX entry window 验证固定 ID、长度、payload SHA 和规范化模型 SHA；完整逐记录审计只写仓库外 Cache。
- 独立审查发现并修复记录 marker 字节序及“截断长度在边界验证前分配”问题，新增固定字节、EOF 优先级和非零窗口偏移回归。

### 关键结论
- 黄金样本为 CSF v3、语言代码 9、5211 标签/值；普通值 4007、扩展值 1204、空主值 4、重复标签 0、诊断 0。
- EditMode XML 489/489、PlayMode XML 1/1；测试包装器因 Unity 返回空进程退出码真实退出 1，XML 结果未被伪造。
- 黄金审计包装器真实退出 0；external manifest SHA-256 为 `84DA14CFA5F26333CFE8C137453B2176B2B5A4E32E4911E1D6B2507264B77F71`。
- 只将 `format.csf` 提升为 `可解析`；写入、原版对照、运行时本地化、语言回退、字体和 UI 仍未实现。

### 后续事项
- 完成仓库门禁、分层提交、Draft PR 和 Actions 实际状态验证，不自动合并。
- 后续本地化工作必须先研究原版标签大小写、重复项胜出、语言覆盖和缺失标签回退规则。

## 2026-08-03 - WP-02F INI 无损字节文档与黄金 identity 往返

### 用户目标
- 从 PR #6 合并后的 `main` 创建独立分支，仅实现 INI 原始字节文档、结构分类、编码边界和未修改逐字节往返。

### 本轮处理
- 从合并提交 `7bb74992` 创建 `feature/wp02f-lossless-ini-document`，没有叠加 WP-02E 分支。
- 交叉研究 ProjectBaseline、FinalAlert 2 开源实现、OpenRA 和既有 XCC 参考；第三方代码保持 reference-only。
- 实现 UnityEngine-free 不可变原始字节 store、物理行、结构节点、Opaque 节点、严格显式编码视图、预算、诊断和 identity writer。
- 通过 MIX 虚拟源固定 `artmd.ini`、`ai.ini` 和两个独立 `rulesmd.ini`；完整逐行记录和 identity 文件只发布到仓库外 Cache。
- 新增受控 Editor 命令、PS5.1/PS7 包装器及固定 payload/model fail-closed 验证。

### 当前结论
- 四个固定候选全部精确 identity roundtrip；两个 `rulesmd.ini` 长度、payload 和结构统计不同，继续不选择胜出者。
- 无 BOM 样本只认定为 ASCII-compatible 单字节原始数据，不推断 UTF-8 或主机代码页。
- Opaque 保留证明可无损保存，不代表未知语法已经可执行。
- ProjectBaseline 审计 Unity 进程退出码 0；外部 manifest SHA-256 为 `1D7ACFE624D9F3575DC9391F6FE070B2A351F0DA560B89606129E530AD9A3F35`。
- 全量 EditMode XML 560/560、PlayMode XML 1/1；EditMode 包装器因空 `Process.ExitCode` fail-closed 返回 1，PlayMode 包装器收尾后真实返回 0。

### 后续事项
- 完成全量 Unity、仓库、版权和双 PowerShell 门禁后创建 Draft PR，不自动合并。
- WP-02G 单独研究 MIX 层级和运行时配置覆盖；本轮不实现 Rules/Art/AI 强类型语义。
- 2026-08-03: 从合并 PR #7 后的 `main` 创建 `feature/wp02g1-ini-runtime-resolution`。
- 2026-08-03: 完成官方编辑器、独立实现、扩展文档和本地 2025 教程的受控静态研究；均按证据等级记录。
- 2026-08-03: 完成显式 INI 加载计划、独立策略、逐值来源链和 50 个新增 EditMode 测试。
- 2026-08-03: 通过 MIX 只读审计 ProjectBaseline；`rulesmd.ini` 与 `soundmd.ini` 胜出者保持 Unresolved。
- 2026-08-03: 真实审计 Unity 退出码 0；脱敏摘要 SHA-256 为 `4DA1FAF7DE29995C8EDF4CAEFEDB9D7FBF801A1DFACD2AB09D626A1629D877CB`。
- 2026-08-03: 在现有 PR #8 分支追加审查修复，不改写原有三个提交；修复
  UTF-16 字节序、候选有界枚举和 source ID 精确身份。
- 2026-08-03: 聚焦 EditMode 66/66、全量 EditMode 627/627、PlayMode 1/1；
  PS5.1/7 RuntimeResolution 审计聚合一致且 winners 保持 Unresolved。
- 2026-08-03: PR #8 squash 合并为 `b3707172`，main Repository safety run `30794186808` 成功；从该 main 创建 `feature/wp02g2-minimal-rules-art-resource-views`。
- 2026-08-03: 完成 WP-02G2 typed scalar、最小 Rules/Art 资源视图和 47 个新增 EditMode case，阶段性全量 674/674。
- 2026-08-03: 只读审计两个独立 Rules 候选和一个 Art 候选；不选择 stock winner，最终脱敏摘要 SHA-256 为 `2F9FF23716D0524139F781D0CAB36178BE26DEE897BFADF692995F66BDB89BF4`。
- 2026-08-03: PR #9 squash 合并后 main 为 `aea6b550`，Repository safety run `30799700783` 成功。
- 2026-08-03: 删除经审计无 PR、无独有提交、无仓库引用的远端临时分支 `research/m2-shp-format-dossier-merge-probe`、`noop`、`noop2`。
- 2026-08-03: 以普通 merge `e60f7beb` 将最新 main 合入 PR #10；完成 Art ambiguity 和 duplicate registry ordinal 的 fail-closed 修复，新增 4 个 EditMode case。
- 2026-08-03: 最终本地门禁得到 EditMode 678/678、PlayMode 1/1、WP-02G2 聚焦 51/51；PS7/PS5.1 typed audit 聚合和三个模型哈希保持不变。

## 2026-08-03 - M2-SHP1 core reader and ProjectBaseline audit

- Created `feature/m2-shp-ts-core-reader` from merged main `7e43b513`.
- Implemented the 8-byte SHP(TS) header, ordered 24-byte descriptors, raw flags 0/1 decode, strict flags 3 RLE-Zero decode, bounded inputs, immutable local indexed frames, and structured diagnostics.
- Added 97 EditMode tests: 47 reader, 38 decoder, and 12 ProjectBaseline audit cases.
- Audited six fixed MIX-backed SHP entries without publishing pixels or frame bodies. The aggregate contains 988 frames: raw0=1, raw1=477, flags3=510, canonical-empty=253.
- Strict decoding succeeded for all raw frames and failed closed for 257 non-empty flags 3 frames. Every observed failure was a row-0 output overflow by one index; production behavior was not widened or sample-special-cased.
- Final local results: EditMode 775/775, PlayMode 1/1, both Unity wrappers exit 0 with controlled post-result shutdown; SHP/INI/CSF/PAL wrapper regressions pass in PowerShell 5.1 and 7.
- Repository validation reports 214 Assets/214 meta, 147 matrix entries, and 95 evidence references. Copyright scans report zero violations and 13/13 ignored external probes in both hosts.
