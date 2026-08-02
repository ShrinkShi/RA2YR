# RA2YR

基于 Unity 的《红色警戒 2：尤里的复仇》v1.001 数据驱动兼容引擎。

> 项目目前处于 WP-02A 内容解析基础设施阶段，尚不可玩。仓库不包含原版游戏素材，也不提供临时单位、临时地图或替代素材。

## 项目定位

RA2YR 的目标是读取用户在仓库外提供的本地游戏内容，逐项实现并验证 YR 1.001 的格式、配置、地图、确定性逻辑、行为和视觉兼容性。它不是“类似红警”的通用 RTS，也不是 Electronic Arts 或其关联方的官方产品。

当前已建立的能力限于：

- 正式 Git 仓库、许可证、第三方来源台账和版权门禁；
- 核心程序集与 Unity 集成层的依赖边界；
- 只读外部内容配置、目录文件发现、SHA-256 和版本化 manifest；
- `OrdinalIgnoreCase` 逻辑路径、显式来源优先级和完整 provenance chain；
- 仓库外完整 manifest 与仓库内脱敏目录级基线证据；
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

WP-02A 只建立目录型内容源的逻辑路径、显式优先级解析、来源链和仓库外 manifest。它尚未解析 MIX 载荷，也不证明 PAL、SHP、VXL/HVA、TMP、INI、地图、视觉或行为兼容。受控基线命令会只读读取文件字节以计算 SHA-256，但公开摘要不包含文件正文、绝对路径或完整文件级清单。

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
Tools/Content/                  受控本机目录级 manifest 入口
Tools/Testing/                  Unity 命令行测试入口
```

## 文档

- [第一阶段需求基线](docs/requirements/三阶段开发需求分析.md)
- [架构边界](docs/architecture/README.md)
- [外部内容系统](docs/architecture/external-content.md)
- [逻辑内容解析与来源优先级](docs/architecture/content-resolution.md)
- [兼容矩阵说明](docs/compatibility/README.md)
- [机器可读兼容矩阵](docs/compatibility/matrix.yml)
- [架构决策记录](docs/adr/README.md)
- [第三方来源说明](THIRD_PARTY.md)
- [机器可读第三方台账](docs/third-party/sources.yml)

## 许可证与声明

仓库中独立编写的项目代码和文档采用 [Apache License 2.0](LICENSE)，附加声明见 [NOTICE](NOTICE)。该许可证不授予原版游戏、FinalAlert 2 或其他第三方内容的权利。

研究 GPL 项目时只能参考公开格式和可观察行为，不得复制 GPL 代码进入本仓库。任何外部代码、生成物或二进制依赖进入项目之前，都必须先完成来源、版本、许可证、用途和批准记录。
