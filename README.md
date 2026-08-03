# RA2YR

基于 Unity 的《红色警戒 2：尤里的复仇》v1.001 数据驱动兼容引擎。

> 项目目前处于 WP-02G2 最小 Rules/Art 资源引用视图阶段，尚不可玩。仓库不包含原版游戏素材，也不提供临时单位、临时地图或替代素材。

## 项目定位

RA2YR 的目标是读取用户在仓库外提供的本地游戏内容，逐项实现并验证 YR 1.001 的格式、配置、地图、确定性逻辑、行为和视觉兼容性。它不是“类似红警”的通用 RTS，也不是 Electronic Arts 或其关联方的官方产品。

当前已建立的能力限于：

- 正式 Git 仓库、许可证、第三方来源台账和版权门禁；
- 核心程序集与 Unity 集成层的依赖边界；
- 只读外部内容配置、目录文件发现、SHA-256 和版本化 manifest；
- `OrdinalIgnoreCase` 逻辑路径、显式来源优先级和完整 provenance chain；
- 仓库外完整 manifest 与仓库内脱敏目录级基线证据；
- 与 Unity 无关的有界二进制读取、统一资源预算、结构化诊断和尾部策略；
- seekable 文件窗口、Westwood MIX 经典/扩展头、文件名 ID、加密目录和 payload-only SHA-1 校验；
- 确定性/保持条目顺序的完整重建式 MIX 写入，以及有界嵌套挂载和逐层 provenance；
- `YR1001_ProjectBaseline` 的只读 MIX 聚合审计和仓库外完整 audit manifest；
- 固定 XCC Mixer 1.47 的合成归档双向语义往返记录；
- 严格的 768 字节 Westwood PAL 原始调色盘解析、不可变 RGB 模型、显式显示转换策略和三个 MIX 内黄金样本审计；
- 严格的 Westwood CSF v3 有序文档解析、原始 UTF-16 code-unit 保留，以及 `langmd.mix` 内 `ra2md.csf` 黄金样本审计；
- Westwood INI 原始字节文档、物理行结构与 Opaque 保留，以及未修改文档的逐字节 identity roundtrip；
- 通过 MIX 虚拟源分别验证 `artmd.ini`、`ai.ini` 和两个不同的 `rulesmd.ini` 候选，不选择运行时胜出者；
- 显式、证据分级的 INI 加载计划与独立的文件组合、名称比较、重复项、分号、空白和空值策略；
- 确定性的逐值候选链与完整来源追踪；`rulesmd.ini` 和 `soundmd.ini` 的 ProjectBaseline 胜出者仍保持歧义；
- 只消费显式 `Complete` INI resolution 的最小 typed scalar、Rules 类型注册表和 Art 资源路由视图；Ambiguous/Failed 输入拒绝，Opaque/分号风险不会被静默标为完整；
- EditMode、PlayMode、仓库静态验证和 CI 入口；
- 明确区分“未实现”“可解析”和原版对照等兼容状态。

文件能被发现、读取或显示不代表已经实现原版行为。当前兼容状态以[机器可读兼容矩阵](docs/compatibility/matrix.yml)为准。

## 技术栈

| 层级 | 技术 |
|---|---|
| 引擎 | Unity 2022.3.60f1c1 |
| 语言 | C# |
| 测试 | Unity Test Framework 1.1.33、NUnit |
| 仓库工具 | Windows PowerShell 5.1、PowerShell 7 |
| CI | GitHub Actions（不依赖原版素材） |

Windows 是当前优先平台。核心格式、内容和确定性逻辑的设计不应依赖 Unity 帧循环或 Windows 专属表现层；尚未实现的跨平台物理路径 identity 检查已记录为已知限制。

## 外部内容与开发基线

原版内容、解包素材、FinalAlert 2、参考工具、缓存和本地黄金样本必须保留在正式 Git 仓库之外。Unity 工程只通过本机配置中的绝对路径或相对配置文件的路径进行只读访问，不得把这些文件复制到 `Assets`。

当前开发内容源的统一逻辑名称是 `YR1001_ProjectBaseline`，其 `ContentSourceKind` 为 `Patched`。该基线包含官方地图增补包、音乐包和 Windows 兼容补丁，因此不等于纯净、未修改的 YR 1.001，也不能作为“纯净原版黄金基线”的兼容证明。

本机配置步骤：

1. 将 [Config/ExternalContent.example.xml](Config/ExternalContent.example.xml) 复制为 `Config/ExternalContent.local.xml`。
2. 将 source 和 cache 路径改为仓库外的本机目录。
3. 不要移动、重命名或写入原版内容；本机配置和缓存已被 Git 忽略。

WP-02A 建立目录型来源、显式优先级、来源链和仓库外 manifest；WP-02B 建立有界二进制输入、预算和诊断；WP-02C 在这些边界上增加 MIX 容器读写、加密目录、校验、嵌套挂载及 XCC 合成互操作；WP-02D 增加严格的 PAL 原始 RGB 解析；WP-02E 增加严格、只读的 CSF v3 文档解析；WP-02F 增加 INI 原始字节、物理行结构、显式编码边界和未修改 identity writer；WP-02G1 增加显式 INI 加载计划、可配置比较/重复/读取策略和逐值来源链，但不猜测 stock YR 的胜出规则；WP-02G2 仅增加来源可追踪的最小 Rules/Art 显式资源引用视图。SHP、PCX、VXL/HVA、TMP、地图 Pack、Texture2D、Shader、玩家色、剧院选择、完整 Rules/Art 语义、默认值、回退与原版运行时覆盖优先级，以及 CSF 写入和运行时本地化仍未实现，也不证明视觉或游戏行为兼容。受控基线命令只读访问必要字节并计算摘要；公开证据不包含文件正文、完整颜色表、字符串表、绝对路径或完整文件级清单。

本轮的 XCC `往返通过` 是明确的语义结果：条目集合、要求保留的顺序和提取负载 SHA-256 一致。XCC 生成归档与本项目重建归档的文件字节并不相同，因此不宣称字节级复原。

## 本地验证

运行 Unity 测试前必须关闭 Unity Editor：

```powershell
./Tools/Testing/Invoke-UnityTests.ps1 `
    -UnityEditorPath 'C:\Path\To\Unity.exe' `
    -TestPlatform All
```

生成本机 `YR1001_ProjectBaseline` 的受控目录级清单：

```powershell
./Tools/Content/Invoke-ContentBaselineManifest.ps1 `
    -UnityEditorPath 'C:\Path\To\Unity.exe'
```

该命令读取被 Git 忽略的 `Config/ExternalContent.local.xml`。完整 resolved manifest 只写入配置指定的仓库外 cache；脱敏摘要只写入被忽略的 `TestResults`，经人工核验后才可转录到公开兼容证据。

生成本机 ProjectBaseline 的只读 MIX 结构审计：

```powershell
./Tools/Content/Invoke-MixBaselineAudit.ps1 `
    -UnityEditorPath 'C:\Path\To\Unity.exe'
```

完整归档/条目 audit manifest 只写入仓库外 cache。仓库证据仅转录归档聚合、七个批准目标的逻辑名称、ID、大小、SHA-256、容器链和诊断。

通过 MIX 虚拟内容源严格验证三个 ProjectBaseline PAL：

```powershell
./Tools/Content/Invoke-PaletteProjectBaselineAudit.ps1 `
    -UnityEditorPath 'C:\Path\To\Unity.exe'
```

完整逐项原始颜色清单只写入仓库外 Cache；被忽略的本机摘要和仓库证据仅包含逻辑来源链、大小、SHA-256、范围统计、不同颜色数和规范化模型 SHA-256。`XccScaleToFullRangeFloor` 仅是命名的 XCC 参考策略，不是原版视觉默认值。

通过 MIX 虚拟内容源严格验证 ProjectBaseline 的 `ra2md.csf`：

```powershell
./Tools/Content/Invoke-CsfProjectBaselineAudit.ps1 `
    -UnityEditorPath 'C:\Path\To\Unity.exe'
```

完整逐记录字符串审计只写入仓库外 Cache；被忽略的本机摘要和仓库证据仅包含固定 MIX 身份、数量与长度统计、规范化模型 SHA-256 和诊断计数，不包含标签列表或字符串正文。该命令不实现 CSF 写入、运行时标签查找、语言回退、字体或 UI 渲染。

通过 MIX 虚拟内容源验证四个固定 INI 候选并执行未修改字节往返：

```powershell
./Tools/Content/Invoke-IniProjectBaselineAudit.ps1 `
    -UnityEditorPath 'C:\Path\To\Unity.exe'
```

完整逐行审计和 identity 输出只写入仓库外 Cache。公开摘要只含逻辑来源、大小、哈希、行/节点聚合和诊断计数，不含节名、键名、值、注释或原始行。这里的“往返通过”仅指未修改输入逐字节一致；语义编辑、原版 writer、跨 MIX 覆盖和 FinalAlert 2 编辑往返仍未实现。

生成 ProjectBaseline 的 INI 候选、Opaque 和分号脱敏审计：

```powershell
./Tools/Content/Invoke-IniRuntimeResolutionAudit.ps1 `
    -UnityEditorPath 'C:\Path\To\Unity.exe'
```

该命令不会启动游戏、XCC 或 FinalAlert 2，也不会选择 `rulesmd.ini` 或 `soundmd.ini` 的胜出者。完整逐行清单仍位于仓库外 Cache；公开摘要只含候选来源、哈希和结构聚合。静态资料尚不足以证明 stock YR 的容器优先级、文件组合、重复项、大小写、行内分号、空白和空值语义，因此这些状态仍为未实现，等待另行授权且只在副本上执行的黑盒对照。

分别生成两个 `rulesmd.ini` 候选和一个 `artmd.ini` 候选的最小资源引用聚合：

```powershell
./Tools/Content/Invoke-IniMinimalResourceAudit.ps1 `
    -UnityEditorPath 'C:\Path\To\Unity.exe'
```

该入口只接受由 `ConfiguredForTesting` 显式计划产生的完整单文档 resolution，两个 Rules 候选不会合并或选择赢家。仓库内证据只包含注册项、显式资源字段、路由候选、诊断和来源完整率的聚合及单向模型哈希；不包含对象名、资源名、节/键/值正文或绝对路径。

XCC 合成互操作使用 `Prepare`、`VerifyXccCreated` 和 `VerifyXccExtractions` 三个受控阶段。包装器不会启动或证明 XCC 进程；操作员必须只使用外部 cache 中的自主合成文件，并以固定工具哈希另行记录真实 GUI 操作。命令和固定目录契约见 [Tools/Content/README.md](Tools/Content/README.md)。

运行版权扫描及其双 PowerShell 回归：

```powershell
./Tools/Repository/Invoke-CopyrightScan.ps1
./Tools/Repository/Tests/Invoke-CopyrightScan.Tests.ps1
```

运行 Unity 元数据、版本、Core 边界和兼容矩阵静态验证：

```powershell
./Tools/Repository/Invoke-RepositoryValidation.ps1
./Tools/Repository/Tests/Invoke-RepositoryValidation.Tests.ps1
```

静态验证器和回归套件均支持 Windows PowerShell 5.1 与 PowerShell 7，不下载 YAML 模块或其他运行时网络依赖。公开 CI 只使用仓库文本和独立生成的临时合成样本。

Unity 2022.3.60f1c1 的当前无头测试进程可能在写出完整结果后无法自行退出。测试包装器仅在验证结果 XML 完整后终止自己启动的子进程，并明确输出警告。

## 关键目录

```text
Assets/RA2YR/Core/              UnityEngine-free 核心边界
Assets/RA2YR/UnityIntegration/  Unity 输入、显示、声音和平台集成边界
Assets/RA2YR/Editor/            Unity Editor 工具入口
Assets/RA2YR/Tests/             EditMode 与 PlayMode 测试
Config/                         外部内容配置示例；本机配置不跟踪
docs/                           需求、架构、兼容矩阵、ADR 和第三方台账
Tools/Repository/               版权扫描与仓库静态门禁
Tools/Content/                  受控目录/MIX manifest 与 XCC 合成互操作入口
Tools/Testing/                  Unity 命令行测试入口
```

## 文档

- [第一阶段需求基线](docs/requirements/三阶段开发需求分析.md)
- [架构边界](docs/architecture/README.md)
- [外部内容系统](docs/architecture/external-content.md)
- [逻辑内容解析与来源优先级](docs/architecture/content-resolution.md)
- [安全有界二进制读取基础](docs/architecture/bounded-binary-reading.md)
- [MIX 内容架构](docs/architecture/mix-content.md)
- [Westwood MIX 格式研究与验证](docs/formats/mix.md)
- [Westwood PAL 格式研究与严格解析](docs/formats/pal.md)
- [Westwood CSF 格式研究与严格解析](docs/formats/csf.md)
- [Westwood INI 原始字节文档与严格边界](docs/formats/ini.md)
- [RA2/YR INI 运行时加载计划与证据边界](docs/formats/ini-runtime-resolution.md)
- [兼容矩阵说明](docs/compatibility/README.md)
- [机器可读兼容矩阵](docs/compatibility/matrix.yml)
- [架构决策记录](docs/adr/README.md)
- [第三方来源说明](THIRD_PARTY.md)
- [机器可读第三方台账](docs/third-party/sources.yml)

## 许可证与声明

仓库中独立编写的项目代码和文档采用 [Apache License 2.0](LICENSE)，附加声明见 [NOTICE](NOTICE)。该许可证不授予原版游戏、FinalAlert 2 或其他第三方内容的权利。

研究 GPL 项目时只能参考公开格式和可观察行为，不得复制 GPL 代码进入本仓库。任何外部代码、生成物或二进制依赖进入项目之前，都必须先完成来源、版本、许可证、用途和批准记录。
