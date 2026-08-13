# RA2YR

## M3 final closeout: real-map integration and read-only foundation

M3 is complete at the read-only foundation and aggregate-observation level.
The final work package composes the existing bounded C1-C7 readers into a
read-only ProjectBaseline vertical integration observation. Packed candidates,
Preview exact streams, theater/TMP availability, and C7 terrain binding are
reported as separate aggregate stages; unresolved terrain binding remains a
failure rather than a compatibility claim. See [M3-C8 integration](docs/formats/m3-c8-real-map-integration.md)
and [ADR-0030](docs/adr/0030-m3-c8-real-map-integration.md).

The configured read-only ProjectBaseline integration executed against the
patched development source and publishes only sanitized aggregates:
8 roots, 282 mounted entries, 200 IsoMap candidates (200 successful, 1 failed),
184 Preview candidates (184 exact), and unresolved terrain binding recorded as
`CompleteWithFailures`. No payload, filename, path, cell, pixel, or
original-runtime compatibility claim is included.

基于 Unity 的《红色警戒 2：尤里的复仇》v1.001 数据驱动兼容引擎。

> M3 已完成仓库基础设施与只读聚合观察，但项目尚不可玩。原版运行时兼容、干净 YR 1.001 等价性、渲染、运行时 TMP/theater 语义、寻路、游戏逻辑和 writer 仍未完成。仓库不包含原版游戏素材，也不提供临时单位、临时地图或替代素材。

M4 governance now defines a Unity-free deterministic data-oriented ECS,
single-authority deterministic commit, first-class Manual/Assisted/Automatic
unit autonomy, and legal observation-to-command computer-agent interfaces.
M4 production implementation has not yet started; Neural/Hybrid policies remain
future contracts and no trained model is included. See [M4 simulation architecture](docs/architecture/m4-deterministic-simulation.md)
and [ADR-0031](docs/adr/0031-m4-deterministic-simulation-governance.md).

M4-C1 now provides the Unity-free deterministic ECS reference kernel: stable
generation-checked entities, bounded component stores, ordered structural
commands, explicit logical time and phases, seed-and-stream RNG, immutable
snapshots, canonical state hashes, deterministic proposals, and explicit
Manual/Assisted/Automatic autonomy contracts. This is synthetic
project-enhancement evidence only; it does not claim original-runtime
compatibility or implement pathfinding, combat, rendering, or gameplay.

M4-C2 adds synthetic terrain topology, explicit passability candidates,
simulation-owned occupancy and deterministic spatial queries. Pathfinding,
movement execution, terrain runtime binding, rendering, and gameplay remain
later work and no original-runtime compatibility is claimed.

M4-C3 adds an independently implemented bounded managed A* reference,
immutable path results, integer route following, reservations, cancellation,
cache invalidation, and deterministic local-avoidance proposals. It is synthetic
project-enhancement behavior; stock path/cost semantics and gameplay remain
unconfirmed.

M4-C4 adds declarative multi-source command requests, bounded canonical queues,
raw-preserving runtime mission snapshots, spatial-index perception,
profile-driven target scoring/hysteresis, deterministic arbitration, forced
player authority, and explicit Manual/Assisted/Automatic autonomy boundaries.
Combat, economy, transport, rendering, and original-runtime mission/AI parity
remain unimplemented and unconfirmed.

## 项目定位

RA2YR 的目标是读取用户在仓库外提供的本地游戏内容，逐项实现并验证 YR 1.001 的格式、配置、地图、确定性逻辑、行为和视觉兼容性。它不是“类似红警”的通用 RTS，也不是 Electronic Arts 或其关联方的官方产品。

当前已建立的能力限于：

- 正式 Git 仓库、许可证、第三方来源台账和版权门禁；
- 核心程序集与 Unity 集成层的依赖边界；
- legacy SHP/PAL、VXL/HVA 仅作为导入 provider，simulation 只引用逻辑 `VisualAssetId`，不直接判断文件扩展名；
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
- 通过 MIX 虚拟源验证 `artmd.ini`、`ai.ini` 及 `rulesmd.ini`/`soundmd.ini` 的多层来源；同名文档按显式 ProjectBaseline 层序逐 Section/Key 组合，不选择 whole-file winner；
- 显式、证据分级的 INI 加载计划与独立的文件组合、名称比较、重复项、分号、空白和空值策略；
- 确定性的逐值候选链与完整来源追踪；ProjectBaseline 层序固定为 `ra2 -> ra2md -> expandmd01..99 -> loose`，但仍不宣称原版运行时对照通过；
- 只消费显式 `Complete` INI resolution 的最小 typed scalar、Rules 类型注册表和 Art 资源路由视图；Art 多重匹配与 Rules 重复 ordinal 均保留全部候选并 fail-closed，不选择首项赢家；
- Westwood SHP(TS) 8 字节头、24 字节帧目录、不可变局部索引帧，以及 flags 0/1 raw 解码；严格 flags 3 RLE-Zero 已通过合成测试，独立探针进一步确认 257 个失败帧同时包含精确宽度行和多一个透明输出的行，因此保持门槛 B 和未提升的黄金兼容状态；
- M3-C1 codec-neutral packed foundation、M3-C2 IsoMapPack5 raw 11-byte records、M3-C3 OverlayPack/OverlayDataPack ordinary 512x512 raw byte-array adapters，以及 M3-C4 managed RawLzo1X backend 与脱敏 ProjectBaseline IsoMapPack5 audit；这些能力均保持显式 policy、bounded input、provenance 和 synthetic/configured compatibility boundary，不提升为原版 runtime 兼容；
- M3-C5 PreviewPack raw component foundation，以及 M3-C6 TMP 52-byte raw cell reader、六剧院 profile、TileSet registry 和 deterministic GlobalTileId ranges；这些能力保持显式 policy、bounded input、provenance 和 synthetic/configured compatibility boundary，不实现 palette、terrain semantics、rendering 或原版 runtime 兼容；
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

当前本机只有该逻辑来源所配置的游戏安装根可参与 runtime candidate discovery。FinalAlert 2、参考资料、手工解包目录、Cache 和工具临时目录即使包含同名文件，也只属于研究或操作边界，不是运行时内容层。`代码词典和文字教程` 已登记为 `CommunitySemanticReference`，仅用于后续区分 stock、扩展和未决语义，不提交教程正文，也不参与加载优先级。

本机配置步骤：

1. 将 [Config/ExternalContent.example.xml](Config/ExternalContent.example.xml) 复制为 `Config/ExternalContent.local.xml`。
2. 将 source 和 cache 路径改为仓库外的本机目录。
3. 不要移动、重命名或写入原版内容；本机配置和缓存已被 Git 忽略。

WP-02A 建立目录型来源、显式优先级、来源链和仓库外 manifest；WP-02B 建立有界二进制输入、预算和诊断；WP-02C 在这些边界上增加 MIX 容器读写、加密目录、校验、嵌套挂载及 XCC 合成互操作；WP-02D 增加严格的 PAL 原始 RGB 解析；WP-02E 增加严格、只读的 CSF v3 文档解析；WP-02F 增加 INI 原始字节、物理行结构、显式编码边界和未修改 identity writer；WP-02G1 增加显式 INI 加载计划、可配置比较/重复/读取策略和逐值来源链；当前 ProjectBaseline 已配置 ordered multi-document semantic composition，但原版运行时对照及单文档重复/大小写/分号/空值语义仍未完成；WP-02G2 仅增加来源可追踪的最小 Rules/Art 显式资源引用视图；M2-SHP1 增加 SHP(TS) 目录和 raw/RLE 局部索引帧边界，但不会为通过基线而放宽严格 RLE 行宽规则。SHP writer、PCX、VXL/HVA、TMP、地图 Pack、Texture2D/Sprite、RGBA、Shader、PAL 自动选择、玩家色、阴影配对、剧院选择、完整 Rules/Art 语义、默认值、回退与原版运行时对照，以及 CSF 写入和运行时本地化仍未实现，也不证明视觉或游戏行为兼容。受控基线命令只读访问必要字节并计算摘要；公开证据不包含文件正文、完整颜色表、字符串表、索引帧、绝对路径或完整文件级清单。

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

该命令不会启动游戏、XCC 或 FinalAlert 2。它将 `rulesmd.ini` 和 `soundmd.ini` 记录为低到高的 composition layers，并明确不产生 whole-file winner。完整逐行清单仍位于仓库外 Cache；公开摘要只含来源、哈希、层序和结构聚合。该 ProjectBaseline 策略标记为 `ConfiguredForProjectBaseline`，不是原版运行时对照；重复项、大小写、行内分号、空白和空值语义仍等待独立验证。

生成由两个 `rulesmd.ini` composition layers 和一个 `artmd.ini` layer
构成的最小资源引用聚合：

```powershell
./Tools/Content/Invoke-IniMinimalResourceAudit.ps1 `
    -UnityEditorPath 'C:\Path\To\Unity.exe'
```

该入口只接受完整 resolution。Rules 输入先按 `ConfiguredForProjectBaseline` 进行跨文档逐值组合，再对仍未确认的单文档语义使用显式 `ConfiguredForTesting` 策略；不会选择 whole-file winner。仓库内证据只包含注册项、显式资源字段、路由候选、诊断和来源完整率的聚合及单向模型哈希；不包含对象名、资源名、节/键/值正文或绝对路径。

通过 MIX 虚拟内容源审计六个固定 SHP(TS) 样本：

```powershell
./Tools/Content/Invoke-ShpTsProjectBaselineAudit.ps1 `
    -UnityEditorPath 'C:\Path\To\Unity.exe'
```

完整逐帧 manifest 只写入仓库外 Cache。公开摘要只包含选择依据、MIX ID 和逻辑来源链、大小与 SHA-256、帧/flags/几何/padding 聚合、规范化模型 SHA-256 和诊断计数。当前结果有 257 个严格 `RleOutputOverflow`，因此 flags 3 只保持“合成解析通过、ProjectBaseline 冲突”；raw flags 0/1 已通过本地样本。该命令不启动 XCC 或游戏，也不输出索引帧、像素或图片。

对这 257 个 flags 3 失败帧执行独立只读行宽探针：

```powershell
./Tools/Content/Invoke-ShpTsRleForensicAudit.ps1 `
    -UnityEditorPath 'C:\Path\To\Unity.exe'
```

探针分析 9,495 行，其中 1,331 行输出精确等于 `WidthRaw`，8,164 行由最后一个 zero-run 多输出一个零索引；257 帧全部同时含有两类行。结论为门槛 B，不修改 production decoder，不建议通用裁剪、丢弃末项或 `WidthRaw + 1`。

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
- [现代视觉资产管线与 legacy provider 边界](docs/architecture/visual-asset-pipeline.md)
- [工程可维护性规则](docs/architecture/engineering-maintainability.md)
- [Westwood MIX 格式研究与验证](docs/formats/mix.md)
- [Westwood PAL 格式研究与严格解析](docs/formats/pal.md)
- [Westwood CSF 格式研究与严格解析](docs/formats/csf.md)
- [Westwood INI 原始字节文档与严格边界](docs/formats/ini.md)
- [RA2/YR INI 运行时加载计划与证据边界](docs/formats/ini-runtime-resolution.md)
- [Westwood SHP(TS) 目录与局部索引帧](docs/formats/shp-ts.md)
- [RA2/YR 内容加载顺序研究](docs/research/content-load-order/README.md)
- [MAP/TMP 格式研究](docs/research/map-tmp/README.md)
- [Westwood 地图压缩研究](docs/research/map-compression/README.md)
- [Packed map compression foundation](docs/formats/map-packed-compression.md)
- [TMP and theater registry foundation](docs/formats/tmp-theater.md)
- [IsoMapPack5 raw record foundation](docs/formats/isomap-pack5.md)
- [OverlayPack and OverlayDataPack raw packed arrays](docs/formats/overlay-packed-arrays.md)
- [SHP(TS) 格式研究](docs/research/shp/README.md)
- [SHP(TS) RLE 行宽冲突研究](docs/research/shp-rle-conflict/README.md)
- [VXL/HVA 格式研究](docs/research/vxl-hva/README.md)
- [兼容矩阵说明](docs/compatibility/README.md)
- [机器可读兼容矩阵](docs/compatibility/matrix.yml)
- [架构决策记录](docs/adr/README.md)
- [第三方来源说明](THIRD_PARTY.md)
- [机器可读第三方台账](docs/third-party/sources.yml)

## 许可证与声明

仓库中独立编写的项目代码和文档采用 [Apache License 2.0](LICENSE)，附加声明见 [NOTICE](NOTICE)。该许可证不授予原版游戏、FinalAlert 2 或其他第三方内容的权利。

研究 GPL 项目时只能参考公开格式和可观察行为，不得复制 GPL 代码进入本仓库。任何外部代码、生成物或二进制依赖进入项目之前，都必须先完成来源、版本、许可证、用途和批准记录。
