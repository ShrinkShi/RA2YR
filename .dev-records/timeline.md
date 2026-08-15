# 开发时间线

## 2026-08-04 - M3-C2 IsoMapPack5 raw-record foundation

### 用户目标
- 从精确 main 开始 M3-C2，只实现 IsoMapPack5 raw record、trailing-data、
  coordinate-index 和 packed-section adapter foundation。

### 本轮处理
- 创建 `feature/m3-c2-isomap-pack5-record-foundation`，基线和 `origin/main`
  均为 `6b6cf581cd2e58c05c33952d5ead2546f4842554`。
- 实现 11-byte raw record reader、三种 trailing policy、坐标 occurrence/index
  和注入式 RawLzo1X packed adapter。
- 将 focused synthetic tests 扩展到 127 个独立执行 case。

### 关键结论
- 不选择 tile 解释，不创建 TMP/renderer/map object。
- 不实现真实 LZO，不读取 ProjectBaseline packed 内容。
- 当前 Unity headless XML 受宿主 `PATH`/`Path` 变量冲突和无进展启动阻塞，
  不把历史 XML 当作当前 HEAD 证据。

### 影响文件
- `Assets/RA2YR/Core/Formats/PackedMap/`
- `Assets/RA2YR/Tests/EditMode/Formats/PackedMap/IsoMapPack5Tests.cs`
- `docs/adr/0024-isomap-pack5-raw-record-foundation.md`
- `docs/formats/isomap-pack5.md`
- `docs/compatibility/evidence/m3c2-isomap-pack5-synthetic-20260805.yml`

### 后续事项
- 静态门禁通过后推送分支并创建 Draft PR；Unity 与 Repository safety 需在可用主机/连接器上重跑。

## 2026-08-04 - M3-C1 contract review fixes

### 用户目标
- 在既有 M3-C1 分支和 Draft PR #36 上修复 chunk sentinel、LZO backend 合同和行为测试矩阵，不开始后续地图/渲染工作。

### 本轮处理
- 确认 HEAD `49ca8e3`、`origin/main` `c4db651`，main 未推进；保留三个既有提交，不 rebase。
- 删除未实现的单零字段 policy；补齐 `0/0` 显式 terminator、单零 fail-closed、LZO 精确合同和 109 个独立 synthetic case。
- Unity Hub 安装记录指向并确认 Unity 2022.3.60f1c1；历史 XML 不作为当前 HEAD 证据。

### 关键结论
- PR #36 已由外部 connector 创建；本地 `gh auth` 仍无效，不将其误写为本地 gh 恢复。
- ProjectBaseline packed audit、LZO 算法、IsoMap、Overlay、Preview、TMP、palette、renderer 均未实现。

### 影响文件
- `Assets/RA2YR/Core/Formats/PackedMap/PackedMapModels.cs`
- `Assets/RA2YR/Core/Formats/PackedMap/PackedIniFragmentCollector.cs`
- `Assets/RA2YR/Core/Formats/PackedMap/PackedSectionDecodePipeline.cs`
- `Assets/RA2YR/Core/Formats/PackedMap/StrictBase64Decoder.cs`
- `Assets/RA2YR/Core/Formats/PackedMap/WestwoodChunkEnvelopeReader.cs`
- `Assets/RA2YR/Tests/EditMode/Formats/PackedMap/PackedMapCoreTests.cs`
- `docs/formats/map-packed-compression.md`
- `docs/compatibility/evidence/m3c1-packed-map-synthetic-20260804.yml`
- `.dev-records/issues.md`
- `.dev-records/changes.md`
- `.dev-records/timeline.md`

### 后续事项
- 使用当前 HEAD 运行 Unity focused/full 和仓库门禁；完成后分三次独立提交并推送到 PR #36，保持 Draft。

## 2026-08-04 - M3-C1 packed map compression foundation

- 从 `c4db6516eaa4e971f8bfe20cd3462dd397f39a55` 创建并保持独立分支 `feature/m3-c1-packed-map-compression-foundation`。
- 实现 lossless INI fragment collection、strict Base64、codec-neutral chunk envelope、显式 Format80 profiles、LZO backend contract 和分阶段 pipeline；未读取 ProjectBaseline packed 内容。
- Unity Roslyn Core/EditMode 编译检查均为 exit 0；当前桌面宿主的 Unity wrapper 受 `Start-Process` PATH/Path 冲突阻塞，旧 focused XML 不作为本轮结果。

## 2026-08-04 17:35 - M3-C1 packed map compression foundation

### 用户目标
- 从 `c4db6516eaa4e971f8bfe20cd3462dd397f39a55` 开始独立实现 packed map compression 基础。

### 本轮处理
- 新增 lossless INI fragment collection、strict Base64、codec-neutral chunk envelope、显式 Format80 decoder、LZO backend contract 和 pipeline。
- 新增 bounded Memory/Stream/window 入口与 122 个合成 EditMode 用例。
- 未读取 ProjectBaseline packed 内容，未实现 LZO、IsoMap、Overlay、Preview、TMP、palette 或 rendering。

### 关键结论
- Format80 只按显式 profile 解码，禁止 variant guessing、clamp、padding 和 partial success。
- LZO 无 backend 时结构化返回 `BackendUnavailable`。
- 当前证据等级为 synthetic/configured，不是 original runtime confirmation。

### 影响文件
- `Assets/RA2YR/Core/Formats/PackedMap/`
- `Assets/RA2YR/Tests/EditMode/Formats/PackedMap/`
- `docs/formats/map-packed-compression.md`
- `docs/adr/0023-packed-map-compression-foundation.md`
- `docs/compatibility/evidence/m3c1-packed-map-synthetic-20260804.yml`
- `docs/compatibility/matrix.yml`
- `README.md`

### 后续事项
- 完成全量 EditMode、PlayMode、仓库验证、版权扫描及双 PowerShell 宿主回归。

## 2026-08-04 - M3-C1 packed map compression foundation started

### 用户目标
- 从 `c4db6516eaa4e971f8bfe20cd3462dd397f39a55` 开始实现通用 packed INI fragment、严格 Base64、chunk envelope、Format80 和 LZO backend contract 基础。

### 本轮边界
- 不读取 ProjectBaseline packed map 内容。
- 不实现 miniLZO、IsoMapPack5、Overlay、Preview、TMP、palette、Unity rendering 或地图逻辑。
- Core 继续保持 UnityEngine-free，并复用现有 bounded window/stream 抽象。

### 当前处理
- 已核对本地与远端 `main` 均为固定 SHA，并创建 `feature/m3-c1-packed-map-compression-foundation`。
- 已阅读 map-compression 研究资料、INI lossless occurrence 模型和 bounded input 实现。

### 后续事项
- 分阶段提交模型、collector/Base64、chunk envelope、Format80、LZO contract/pipeline、测试和文档。

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

## 2026-08-10 - M3-C4 managed RawLzo1X backend

### 用户目标
- 在既有 M3-C1/M3-C2 packed boundary 上实现 managed RawLzo1X decode backend，
  并提供 ProjectBaseline IsoMapPack5 的脱敏聚合审计；不开始 M3-C5 或任何
  Preview/TMP/Overlay 语义、渲染和 gameplay。

### 本轮处理
- 新增 bounded、exact-length、cancellation-aware、UnityEngine-free managed
  decoder，identity 为 `ra2yr-managed-raw-lzo1x-v1`。
- 新增只输出 aggregate 的 ProjectBaseline audit service/Editor command/wrapper，
  使用外部 patched development source，不发布 map names、records、coordinates、
  compressed bytes、decoded bytes、images 或 absolute paths。
- 更新 ADR 0026、格式文档、兼容矩阵、evidence、third-party ledger 和开发记录。

### 关键结论
- 当前 EditMode 真实 XML 为 `1185/1185` passed，Unity exit 0；这不是 ProjectBaseline
  audit 的成功声明。
- 最近 audit status 为 `CompleteWithFailures`：200 candidate、200 successful、
  1 mount-level failure；fingerprint before/after 相同。该失败必须保留。
- managed decoder 实现不等于原版 runtime confirmation；LZO writer、Preview、TMP、
  Overlay semantics、palette、renderer、pathfinding 和 gameplay 仍未实现。

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

## 2026-08-03 - ProjectBaseline INI composition correction

- Replaced the interim whole-file winner model with ordered multi-document
  semantic composition: `ra2 -> ra2md -> expandmd01..99 -> loose`.
- Added deterministic expand-number parsing, per-key overlay tests, complete
  overridden-candidate traces, and fail-closed source/layer diagnostics.
- Re-routed the WP-02G2 Rules audit through the composed two-document result;
  `artmd.ini` remains a one-layer regression input.
- Kept original-runtime comparison and all intradocument syntax policies
  unresolved and independently evidence-gated.
- Final ProjectBaseline audits were stable across PowerShell 5.1/7: Rules has
  22,720 resolved values, 22,709 overridden chains, five registries and 1,171
  entries; Art remains 880 records. Final Unity XML is 824/824 and 1/1.

## 2026-08-04 14:35 - PR #20 synchronized closeout

### User goal
- Synchronize the INI composition branch with current `main`, preserve all
  merged research and architecture records, rerun local gates, and merge PR #20
  only after Repository safety succeeds.

### Work completed
- Resolved the ADR index conflict semantically and reviewed all auto-merged
  development records and README entries for cumulative preservation.
- Kept the active PR diff limited to INI composition, Rules/Art audit, tests,
  wrappers, documentation, evidence, compatibility data, and development logs.
- Re-ran focused and full Unity tests, content audits, repository validation,
  copyright checks, and PowerShell 5.1/7 composition audits.
- Corrected the Unity wrapper's Windows PowerShell exit-code handshake after a
  passed XML run exposed a null `Process.ExitCode` read.
- Corrected CSF/PAL UTC timestamp validation for PowerShell 7 JSON date
  coercion, then reran both audits and their dual-host wrapper regressions.

### Key results
- EditMode 694/694; PlayMode 1/1; both Unity and wrapper exit codes were zero.
- Rules remained 22,720 resolved values and 22,709 override chains; Art remained
  880 records. Both normalized model hashes were unchanged across shell hosts.
- The evidence level remains `ConfiguredForProjectBaseline`, never
  `ConfirmedByOriginalRuntime`.

### Files affected
- `Tools/Testing/Invoke-UnityTests.ps1`
- `docs/compatibility/evidence/wp02g1g2-project-baseline-composition-20260803.yml`
- `.dev-records/changes.md`
- `.dev-records/timeline.md`

### Next step
- Commit and push the normal merge, verify PR #27 state, wait for PR #20
  Repository safety, then squash merge PR #20 without starting PR #21 work.
# 2026-08-05 PR #42 审查修复

- 在 `feature/m3-c2-isomap-pack5-record-foundation` 上继续修复 active diff 审查问题；保持原五个提交不变。
- 修复诊断预算 fail-open、duplicate policy、trailer 防御性返回、offset overflow 和测试 helper 确定性问题。
- 当前 HEAD 的 Unity/包装器/版权/Repository safety 结果尚待新提交后重新执行，未使用历史 XML 冒充证据。

## 2026-08-07 - M3-C2 P1 aggregation and gate refresh

- Audited the remote feature branch at `3e6a8b623dac916daf5ff80a3c64705f477b1159`; no unpushed local M3-C2 commits were present before this fix.
- Added top-level execution aggregation for packed, record, and coordinate stages with saturating suppressed-count merge.
- Recomputed source coverage as 146 defined NUnit executions and 103 behavior methods.
- Repository validation and copyright gates passed under Windows PowerShell 5.1 and PowerShell 7; Unity execution remains NotRun because the required Editor executable is unavailable.

## 2026-08-07 - M3-C2 final independent-review correction

- Started from synchronized review head `6551e01472404886da8f8a3aad4a514d863f8406` with `behind=0` against `ea1eb5505a71d9da28cb1cef6ed8b089bd7e193a`.
- Added unconditional RawLzo1X backend gating, explicit empty-input and zero-chunk failures, deterministic enum validation, and a positive both-or-neither coordinate rectangle contract.
- Expanded the source matrix to 164 defined NUnit executions and 118 behavior
  methods; Unity was `NotRun` at that review head pending the required editor.

## 2026-08-08 - M3-C2 current-head gates

### User goal
- Re-run the current-head gates after the packed empty-input boundary fix and
  retain accurate evidence for PR #42.

### Work completed
- Added a bounded first-occurrence peek/replay before packed pipeline decode;
  empty input now fails as `EmptyPackedInput` and never reaches record parsing.
- Re-ran focused M3-C2 EditMode, full EditMode, and PlayMode on Unity
  2022.3.60f1c1, plus PS5.1/PS7 repository, copyright, regression, and content
  wrapper gates.

### Key results
- Focused: 164/164; full EditMode: 1097/1097; PlayMode: 1/1.
- Static and wrapper gates passed; compatibility remains synthetic and no
  ProjectBaseline packed data was read.

## 2026-08-08 - M3-C3 Overlay raw-array foundation

### 用户目标

- 从 exact main 开始 M3-C3，只实现 OverlayPack/OverlayDataPack raw packed
  array、trailing/length、防御性结果和显式 storage index foundation。

### 本轮处理

- 建立 `feature/m3-c3-overlay-packed-array-foundation`，实现独立 section
  selection、ordinary 512x512 exact-length raw arrays、candidate index
  profiles、provenance 和 fail-closed execution state。
- 复用 M3-C1 packed pipeline；RawLzo1X、Preview、TMP、registry、palette、
  rendering、pathfinding、gameplay 和 ProjectBaseline packed audit 均未做。
- 新增 focused M3-C3 suite，当前 worktree XML 为 51/51 passed。

### 关键结论

- `OverlayPack` 与 `OverlayDataPack` 必须保持两个独立 child；缺失、空、选中、
  歧义和失败不能互相合成。
- `0xFF` 与坐标方向仍是 raw/candidate policy，不提升为原版 runtime 事实。

### 影响文件

- `Assets/RA2YR/Core/Formats/PackedMap/OverlayPackedArrayModels.cs`
- `Assets/RA2YR/Core/Formats/PackedMap/OverlayPackedArrayReader.cs`
- `Assets/RA2YR/Tests/EditMode/Formats/PackedMap/OverlayPackedArrayTests.cs`
- `docs/adr/0025-overlay-packed-array-foundation.md`
- `docs/formats/overlay-packed-arrays.md`
- `docs/compatibility/evidence/m3c3-overlay-packed-array-synthetic-20260808.yml`

### 后续事项

- 已在 validation commit `82fa0239edafd7174a6386a1fc80f43b6440f169` 完成 Unity、
  PS5.1/PS7 static/copyright、repository regression 和 content wrapper regression。
- 推送文档结果记录并创建 Draft PR，等待 exact pushed HEAD Repository safety；
  通过后停止，不开始 M3-C4。

## 2026-08-08 - M3-C3 independent-review finding closure

- 在既有 M3-C3 分支上只修复 policy validation、bounded occurrence input 和
  packed result defensive snapshot；没有新增 Overlay 语义或后续 work package。
- unknown nested policy 在 occurrence source 之前 fail-closed；Overlay 输入只在
  `MaxFragments + 1` probe 内消费；`DecodedBytes`/`BlockOutputs` getter 不再暴露
  可修改内部数组。
- focused source definition 更新为 61 NUnit executions、51 behavior methods；旧
  51/51 XML 明确属于祖先 commit `82fa0239...`，本次修复后的 Unity 执行保持
  `NotRun`，不复用旧 XML。
- 当前主机未发现 Unity 2022.3.60f1c1 Editor；仅 PS5.1 validation 通过，PS7、
  copyright、wrapper 和当前-head Unity 仍为 `NotRun / EnvironmentBlocked`。
  implementation candidate safety run `31312939491` 已对 `141aed1...` 成功，
  docs-only 新 HEAD 仍需新 run。

## 2026-08-09 - M3-C3 P2-2 evidence/provenance closure

### 用户目标
- 只关闭 PR #43 当前唯一剩余的 P2-2 evidence/provenance consistency finding，
  不重做已关闭的代码 finding，不开始 M3-C4。

### 本轮处理
- 将 M3-C3 synthetic evidence 的历史执行与当前 finding candidate 分成独立的
  `historical` / `current_candidate` 结构。
- 纠正当前 PS7、repository regression、copyright、wrapper 和 Unity 状态；保留
  PS5.1 validation 的真实通过结果。
- 记录 implementation candidate safety `31312939491`，不把它冒充即将产生的
  docs-only HEAD safety。

### 关键结论
- 本轮是 docs/evidence-only；不修改 production C#、NUnit、Unity assets、Packages、
  ProjectSettings、compatibility semantics 或 research dossier。
- 推送后等待新 exact-head Repository safety，PR 保持 Open/Draft/Unmerged。

### 影响文件
- `docs/compatibility/evidence/m3c3-overlay-packed-array-synthetic-20260808.yml`
- `.dev-records/issues.md`
- `.dev-records/timeline.md`
- `.dev-records/changes.md`
- `.dev-records/backlog.md`
## 2026-08-12 - M3-C5 PreviewPack foundation

- Added the raw metadata reader, packed adapter, immutable decoded stream, and
  explicit channel/row layout views.
- Added ADR-0027, format documentation, compatibility evidence, and matrix
  entry. All compatibility claims remain synthetic/configured or unresolved.
- Corrected successful metadata execution state after the first current-tree
  compile/test run exposed the fail-closed result bug.
- Attempted a normalized-environment Unity rerun; no valid current-head XML was
  produced, so Unity and dependent gates remain NotRun.
- No ProjectBaseline packed PreviewPack data, rendering code, or M3-C6 work was
  introduced.

## 2026-08-12 - M3-C5 maintainer closeout

- Implemented and ran the configured, read-only PreviewPack ProjectBaseline
  aggregate audit using the existing MIX/content layer and M3-C4 managed
  RawLzo1X backend.
- Sanitized result: `CompleteWithFailures`, 184 candidates, 184 exact decoded
  streams, zero section failures, one MIX mount-level failure, all dimensions
  positive, fields 0/1 zero, fragments 54..1138, and chunks 2..15. No
  payload, filename, path, pixel, or original-runtime claim was emitted.
- Repaired current-tree behavior now passes the full EditMode XML 1210/1210;
  the earlier 1198/1205 result remains explicitly pre-fix history.
# 2026-08-12 - M3-C6

- Branched from `c5739f485d24e9db62b2e1dcf9ddad6216ddc339`.
- Implemented TMP raw and theater registry foundation, then executed the
  configured sanitized ProjectBaseline audit and current-head EditMode gate.
2026-08-12 — M3-C6 merged after PS7 recovery; M3-C7 branch created from squash main and terrain composition/audit implementation started.
## 2026-08-13 - M3-C8 real-map integration

Created the C8 branch from main squash `30a0d59e`. Reused C1-C7 bounded audits
and executed the configured ProjectBaseline integration with truthful aggregate
failure classification. No renderer, gameplay, writer, or compatibility
promotion was introduced.
## 2026-08-13 - M3 final repository closeout

- PR #46 (TMP/theater foundation), PR #47 (read-only terrain composition), and
  PR #48 (real-map aggregate integration) are merged.
- Final main is `82e2c6a46f842d09ee9786657065c942753cc435`.
- C8 validation recorded EditMode 1260/1260 and PlayMode 1/1, with PS5.1/PS7
  repository, copyright, regression, and wrapper gates passing.
- The configured patched ProjectBaseline aggregate remains
  `CompleteWithFailures`: 200 IsoMap candidates, 184 Preview candidates,
  unresolved terrain binding, stable source fingerprint, and sanitized output
  only. This is not original-runtime confirmation.
- M3 is closed at the repository-record/foundation level. Runtime semantic
  binding, rendering, passability, pathfinding, gameplay, writer/roundtrip,
  clean YR 1.001 equivalence, and original-runtime validation remain future
  scope; M4 has not started.
## 2026-08-13 - M4 P0 governance refresh

- Confirmed the external three-stage requirements document exists at
  `E:\时锐\RA2\RA2YR-unity\三阶段开发需求分析.md` and matches the tracked
  canonical copy `docs/requirements/三阶段开发需求分析.md` byte-for-byte.
- Added explicit Requirement / Compatibility Target / Current Implementation
  State / Evidence State / Project Enhancement / Future Research layers.
- Recorded M3 as complete at foundation/read-only aggregate level and added the
  M4 deterministic ECS, single-authority commit, tactical autonomy, legal-agent
  observation, and future neural policy boundaries. No simulation production
  code was added.
## 2026-08-13 - M4-C1 deterministic ECS kernel

- Created `feature/m4-c1-deterministic-ecs-kernel` from the exact P0 squash
  main and added the Unity-free simulation kernel.
- Current EditMode XML passed 1275/1275, including 15 C1 behavior methods.
- C1 remains project-enhancement/synthetic evidence only; no ProjectBaseline
  packed data, renderer, pathfinding, combat, or gameplay was introduced.
## 2026-08-13 - M4-C2 terrain occupancy spatial foundation

- Created the C2 branch from exact main `12fd0755418e3b3c83ac655f02add0368bbef3ab`.
- Implemented raw terrain topology, candidate movement graph data,
  simulation-owned occupancy, and deterministic spatial indexing.
- Focused EditMode passed 1292/1292; pathfinding and movement execution remain
  intentionally out of scope.
## 2026-08-13 - M4-C3 pathfinding movement foundation

- Created C3 from exact main `a10eafde4fbee5ab31bde5be9ad5c272b9e84b19`.
- Implemented bounded managed pathfinding, movement route following,
  reservations, cache invalidation, and deterministic local avoidance.
- Focused EditMode passed 1313/1313; stock path semantics remain unresolved.

## 2026-08-13 - M4-C5 combat abilities foundation

- Created the C5 branch from exact C4 main and added deterministic proposal /
  commit combat and generic ability contracts.
- Current EditMode XML passed 1342/1342; no renderer, writer, ProjectBaseline,
  or original-runtime compatibility claim was added.

## 2026-08-13 - M4-C6 scenario agent platform

- Created C6 from post-main C5 safety SHA `321f1e5b84d1b0d62c3bcb477b0c31858a3f892e`.
- Added bounded spawn, explicit owner binding, legal observation/policy
  interfaces, and headless synthetic environment; EditMode passed 1353/1353.

## 2026-08-13 - M4-C7 integrated world

- Created C7 from C6 squash main `3abf57fc525aa63d55613426740e00af735b6888`.
- Added a bounded synthetic battle and deterministic repeated-run/input-order
  checks; current EditMode passed 1361/1361.

## 2026-08-13 - M5-C2 resource economy

- Created from exact post-M5-C1 main `eb9b2c719a72945fb80698b119ae90af55e65828`.
- Added bounded raw resource, cargo, and refinery Core contracts; focused
  EditMode passed 1384/1384 with no ProjectBaseline reads.

## 2026-08-14 - M5-C3 production and technology

- Created from exact post-M5-C2 main `c8029c746e19cb4c2722fc0192acc1be08ad8295`.
- Added bounded raw definition/availability/queue contracts; EditMode passed
  1397/1397 with no ProjectBaseline reads.

## 2026-08-14 - M5-C4 structures and placement

- Created from exact post-M5-C3 main `0b05a688d9458aab3f4a180deac44d819afa9ca1`.
- Added bounded structure/placement and interaction candidates; EditMode passed
  1410/1410 with no ProjectBaseline reads.

## 2026-08-14 - M5-C5 economic computer-agent

- Created the C5 branch from exact main `b2f3b6082f61e01ab4b65821bbde97e909ebea5e`.
- Added deterministic legal economic observations and bounded action proposals;
  EditMode passed 1422/1422. No ProjectBaseline packed data was read.

## 2026-08-14 - M5-C6 integrated headless skirmish

- Created C6 from post-C5 squash main `2ad850a236055a3ab45e08844261360472c95317`.
- Added the synthetic two-player economy/production/combat chain and manual
  command-stream path; EditMode passed 1438/1438 with no ProjectBaseline reads.
## 2026-08-13 - M4-C4 commands missions targeting autonomy

- Created C4 from exact main `230ad313257b73e62b7ef48ec8afadc9c8b83e43`.
- Added declarative command queues, mission snapshots, perception/targeting,
  action arbitration, and explicit autonomy/hold policies.
- Focused EditMode passed 1331/1331; combat/economy remain out of scope.
## 2026-08-14 - M5-C7 performance and correctness closeout

- Created the M5-C7 branch from post-merge main `1ea94fe31d07c3f7eb4e708db6dce6b3fefbea3d`.
- Added bounded stress workloads at 500/1000/2000 entities with a single
  authoritative economy, deterministic proposal/commit phases, operation
  budgets, and canonical aggregate hashes.
- Current exact-head Unity result: EditMode 1465/1465 (27 C7 executions) and
  PlayMode 1/1; no ProjectBaseline packed data was read.

## 2026-08-14 - M5 final documentation closeout

- Created docs-only branch `docs/m5-final-closeout` from exact main
  `2b08c02f22a18d56a07c5746f62219100d325403`.
- Recorded M5 COMPLETE without changing production semantics or compatibility
  evidence. Final Unity and post-main safety facts remain tied to the merged
  M5 code HEAD.
- Set M6 current target to Presentation/Renderer/Interactive Client and kept
  full YR parity, renderer parity, networking, replay/save, Neural training,
  and original-runtime confirmation deferred.

## 2026-08-14 - M6-C1 presentation snapshot foundation

- Created `feature/m6-c1-presentation-snapshot-foundation` from post-M5 main
  `8d95291badddc9df748b1a8e212010fcc3ac3420`.
- Added the Unity-free presentation snapshot boundary and synthetic tests;
  current EditMode is 1487/1487 passed, with no ProjectBaseline read.
## 2026-08-14 — M6-C2

Created `feature/m6-c2-legacy-visual-import-readiness` from exact post-M6-C1
main `72a9dbc1cfd17bb49d67d7a7077920e5fd093663`. Added raw PAL/SHP reuse and
VXL/HVA readiness contracts with synthetic tests. Full EditMode current-head
XML is 1510/1510 passed; ProjectBaseline packed visual data was not read.

## 2026-08-14 — M6-C3

Created `feature/m6-c3-terrain-palette-isometric-presentation` from exact
post-M6-C2 main `5def9de6164594f95e32febcc299b9abe28f181b`. Added checked
isometric terrain presentation contracts, deterministic chunking, and the
UnityIntegration one-mesh-per-chunk adapter. Current EditMode is 1528/1528
passed; ProjectBaseline packed terrain/visual data was not read.

## 2026-08-14 — M6-C4

Created `feature/m6-c4-object-visual-presentation-foundation` from exact
post-M6-C3 main `5b42f9c3de5befd0673b37a1eb269370a56c30ae`. Added explicit
object anchors/bounds/families, deterministic depth tuples, attachment checks,
and Unity draw commands. Current EditMode is 1548/1548 passed; no
ProjectBaseline packed visual data was read.

## 2026-08-15 - M6-C5

Created `feature/m6-c5-effects-depth-fog-presentation-foundation` from exact
post-M6-C4 main `3316d30f99dfe38e15a1ffaeb888619392b4194e`. Added explicit
effect/depth/alpha, shadow separation, and fog/shroud visibility contracts.
The current synthetic EditMode run is 1572/1572 passed, including 24 C5
behavior methods; ProjectBaseline packed
visual data was not read.

## 2026-08-15 - M6-C6

Created `feature/m6-c6-unity-renderer-integration-foundation` from exact
post-M6-C5 main `7df9055d71226a1cb49b64fcca04d738017a11a6`. Added the central
Unity presentation world, bounded cache, indexed/palette resources, terrain
chunk lifecycle, camera adapter, and synthetic VXL exposed-face mesh. Current
EditMode is 1603/1603 passed and PlayMode is 2/2 passed; ProjectBaseline packed
visual data was not read.

## 2026-08-15 - M6-C7 interactive client foundation

### 用户目标
- Establish a bounded interactive client seam without starting M7.

### 本轮处理
- Created the C7 branch from exact post-C6 main.
- Added visibility, selection, pointer, command, HUD, production, placement,
  environment models and the Unity adapter.
- Added synthetic evidence, ADR, compatibility matrix entry, README, and dev
  records.

### 关键结论
- Presentation remains Unity-free and commands enter the authoritative queue
  as Human `CommandRequest` values only.
- Current-head EditMode 1640/1640 and PlayMode 2/2 passed.

### 影响文件
- `Assets/RA2YR/Presentation/InteractiveClientModels.cs`
- `Assets/RA2YR/UnityIntegration/UnityInteractiveClientAdapter.cs`
- `Assets/RA2YR/Tests/EditMode/M6C7InteractiveClientTests.cs`
- `docs/compatibility/evidence/m6c7-interactive-client-synthetic-20260815.yml`

### 后续事项
- Run full static gates, push Draft PR, and wait for exact-head Repository safety.

## 2026-08-15 - M6-C8 integrated playable presentation closeout

### 用户目标
- Complete the M6 integrated presentation/performance closeout without M7.

### 本轮处理
- Created the C8 branch from exact post-C7 main.
- Added cadence scheduling, state-hash equivalence, bounded stress tiers, and
  the Unity controller composition seam.
- Added ADR, format docs, matrix/evidence, README, and development records.

### 关键结论
- Presentation does not mutate Simulation authority; 30/60/144 are explicit
  deterministic scheduling profiles, not GPU FPS claims.
- Current-head EditMode 1657/1657 and PlayMode 3/3 passed.

### 影响文件
- `Assets/RA2YR/Presentation/PlayablePresentationCloseout.cs`
- `Assets/RA2YR/UnityIntegration/UnityPlayablePresentationController.cs`
- `Assets/RA2YR/Tests/EditMode/M6C8PlayablePresentationTests.cs`
- `Assets/RA2YR/Tests/PlayMode/M6C8PlayableSmokeTests.cs`
- `docs/compatibility/evidence/m6c8-integrated-playable-synthetic-20260815.yml`

### 收口结果
- Final static gates and current-head Unity evidence completed; PR #74 was
  opened as Draft, marked Ready only after all gates passed, and squash-merged
  as `0a8b834f496509bb34f6ebbfaf673f58c4c98367`.
- Exact-head Repository safety was run `31832327388`; post-main safety was
  run `31832713652`. M6 remains a foundation/project-enhancement milestone;
  M7 was not started.

## 2026-08-15 - M6 Human Playtest Delivery

Created `feature/m6-human-playtest-delivery` from exact post-M6 main
`8129777ff889509a4822980664473b7077f048ed`. Added the centralized synthetic
skirmish bootstrap, enabled scene, deterministic runtime harness, controls
documentation, and current-head runtime/scene smoke tests. This is the M6
manual delivery seam only; no ProjectBaseline packed data or M7 work was
started.
