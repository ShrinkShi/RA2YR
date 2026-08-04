# 技术决策记录

## 2026-08-03 - Legacy formats remain import adapters

### 背景
SHP/PAL 与 VXL/HVA 是兼容输入，但不应固化未来 simulation 或现代渲染资产结构。

### 决策
simulation 仅引用逻辑 `VisualAssetId`；legacy 格式由独立 provider 导入，Core 与 Unity adapter 分离。像素尺寸、世界尺寸、pivot、占地和碰撞分别建模，team color 使用抽象 mask/remap channel。

### 原因
避免游戏逻辑依赖 `.shp`/`.vxl`，并允许未来 RGBA、多材质、高分辨率动画和现代模型替换底层表示。

### 代价
后续必须实现显式 provider routing 和 provider-neutral visual asset contract，不能直接把 reader 结果交给 simulation。

### 替代方案
- 直接以 SHP/VXL 文件名作为运行时资产身份：拒绝，耦合 legacy 表示与游戏逻辑。

## 2026-08-03 - Runtime root and community reference separation

### 背景
本机同时存在权威游戏安装、FinalAlert 2、工具箱、手工解包、Cache 和社区教程资料。

### 决策
只有 `YR1001_ProjectBaseline` 配置根参与 runtime candidate discovery。RA2 DIY 2025 教程目录登记为 `CommunitySemanticReference`，只形成语义假设，不作为内容源。

### 原因
防止同名文件从研究或工具目录污染 provenance 和加载计划。

### 代价
教程结论必须继续按 stock、扩展和未决语义分类，并由独立证据确认。

### 替代方案
- 扫描所有本机 RA2/YR 相关目录：拒绝，来源身份和运行时语义不可控。

## 2026-08-01 - 正式仓库和外部内容边界

### 背景
工作区同时包含 Unity 工程、原版内容、FinalAlert 2 和参考工具，外层 Git 会造成版权文件误提交风险。

### 决策
正式 Git 根固定为 `RA2YR`。所有原版内容、解包文件、工具和参考资料位于该根目录之外。

### 原因
使用文件系统边界阻止 Git 接触版权内容，比仅依赖忽略规则更可靠。

### 代价
本地内容路径需要通过外部配置提供，CI 只能使用自主合成样本。

### 替代方案
- 在外层建立 deny-by-default 仓库；因误操作风险更高而未采用。

## 2026-08-01 - 核心逻辑禁止引用 UnityEngine

### 背景
格式、配置、地图和确定性模拟必须能在无场景、无渲染环境运行。

### 决策
Core 程序集启用 `noEngineReferences`；Unity 生命周期、输入、渲染和音频通过单向集成层适配。

### 原因
保证 15 Tick 权威逻辑可测试、可重放并不受显示帧率影响。

### 代价
Unity 类型和便利 API 不能进入核心模型。

### 替代方案
- 直接使用 MonoBehaviour 组织逻辑；因不可确定且难以无界面测试而未采用。

## 2026-08-01 - 许可证和参考代码规则

### 背景
本地参考资料包含 Apache-2.0、MIT、GPL 和专有工具。

### 决策
项目采用 Apache-2.0。GPL 项目仅用于公开格式和行为研究，不复制其代码；任何外部代码进入仓库前必须登记来源、许可证和使用方式。

### 原因
保持项目许可证清晰并避免不兼容代码污染。

### 代价
部分格式需要独立实现并以样本验证。

### 替代方案
- 直接复用 GPL 实现；因许可证边界与项目决策不符而未采用。

## 2026-08-01 - Manifest 可信构造边界

### 背景
公开的测试哈希注入和结果构造器可让调用方提供任意摘要或完整性标志，削弱 manifest 作为兼容证据的含义。

### 决策
生产公开入口只保留默认只读 SHA-256 索引器。测试注入、文件记录和索引结果构造器均为 internal，并只向 EditMode 友元开放；完整性由诊断和源结果派生，来源指纹在构造时重算。

### 原因
“64 位十六进制字符串”不是可信 SHA-256 证据，完整性必须来自受控索引流程。

### 代价
未来 manifest 反序列化需要在 Core 内提供显式验证工厂，不能由外部调用方直接拼装对象。

### 替代方案
- 保持 public 并依赖调用约定；因无法由类型系统和测试约束而未采用。

## 2026-08-01 - 当前开发内容源命名

### 背景
当前本地内容目录包含官方地图增补包、音乐包和 Windows 兼容补丁；将它笼统称为 YR 1.001 原版基线会与未来纯净原版黄金样本混淆。

### 决策
用户可见的当前开发内容源统一命名为 `YR1001_ProjectBaseline`，并保留 `ContentSourceKind.Patched`。权威兼容目标仍是原版 YR 1.001，纯净黄金样本需要单独登记和验证。

### 原因
名称必须同时表达项目当前采用的内容集合及其 patched 属性，避免把路径存在或本地开发结果误写为纯净原版兼容证据。

### 代价
旧的 `YR1001_Patched` 名称继续保留在版权防御策略中，但不再作为当前开发源的用户可见角色。

### 替代方案
- 继续称为 patched baseline；因无法明确区分“项目开发源”和“纯净原版黄金基线”而未采用。

## 2026-08-02 - 逻辑路径与来源优先级

### 背景
YR/Windows 内容名不区分大小写，但物理文件大小写、来源层级和扫描顺序可能不同。使用枚举顺序或来源 ID 破平会形成不可见优先级。

### 决策
逻辑路径统一使用 `/`，以 `OrdinalIgnoreCase` 确定身份，同时保留选中文件的实际相对路径大小写。数值更大的 priority 优先；同源大小写冲突拒绝解析；多个最高 priority 来源明确返回歧义。来源 ID 只用于身份与稳定报告顺序，不决定胜者。

### 原因
解析结果必须独立于当前区域文化、文件枚举、线程、Dictionary 和来源命名。

### 代价
相同最高优先级不能自动选择，必须由配置作者显式调整 priority 或来源集合。

### 替代方案
- 以来源 ID 或枚举顺序破平；因属于隐藏优先级而未采用。

## 2026-08-02 - 完整 manifest 与公开证据分离

### 背景
完整文件级来源链和 SHA-256 对本机验证有用，但把 ProjectBaseline 的完整清单提交到公开仓库不符合版权与披露边界。

### 决策
完整 resolved manifest 只能内容寻址地写入配置指定的仓库外 cache。仓库内只提交经审核的脱敏汇总，包括 manifest SHA-256、总量、扩展名聚合、扫描事实和少量明确批准的代表文件元数据。

### 原因
既保留可验证的本机内容身份，又避免提交原版正文、绝对路径或完整可枚举清单。

### 代价
公开 CI 不能独立重建本机 manifest，只能验证合成样本和证据结构；原版黄金验证必须在授权本机运行。

### 替代方案
- 提交完整 manifest；因披露范围超过必要证据而未采用。

## 2026-08-02 - 有界二进制会话与统一预算

### 背景
后续 YR 格式解析会遇到不可信的长度、偏移、数量、短读和未知尾部；若每个解析器自行分配和计数，嵌套即可重置限制，也无法证明错误偏移和完整消费。

### 决策
所有具体格式解析器必须在 Core internal `BinaryReadSession` 上工作。每个会话拥有一个明确输入边界、逻辑来源、稳定快照和统一有限预算；子区间共享账本。完成状态只证明指定字节范围已经按显式尾部策略处理，具体格式结果仍须由内部验证工厂构造。

### 原因
统一入口可以在转换和分配前验证文件驱动值，并使 Memory、seekable Stream 和 non-seekable Stream 得到一致、可复现的偏移与诊断。

### 代价
当前 Stream 必须在预算内形成完整快照；未来增量后端仍必须保持相同的预算、诊断和尾部语义。

### 替代方案
- 直接公开 `BinaryReader` 或 reader/session；因可绕过记录/字符串预算并伪造完成状态而未采用。
- 让每种格式自行管理限制；因嵌套账本不统一、审计成本高而未采用。

## 2026-08-02 - 逻辑路径确定性散列

### 背景
逐 UTF-16 `char` 大写不能正确覆盖补充平面大小写对，会出现 `OrdinalIgnoreCase` 相等但哈希不同。

### 决策
先对完整字符串执行 `ToUpperInvariant`，再对折叠后的 UTF-16 序列执行确定性 FNV-1a；该值只用于集合，不作为 manifest、存档、回放或网络协议身份。

### 原因
同时满足当前 Unity 运行时的相等—哈希契约和同算法/Unicode 表下的跨进程确定性。

### 代价
跨运行时持久身份仍必须使用 SHA-256 或显式规范序列，不能持久化集合哈希。

## 2026-08-02 - MIX 独立实现与容器边界

### 背景
权威 ProjectBaseline 主要通过 MIX 承载内容；XCC 是历史实现和黑盒基准，但源码为 GPLv2，不能进入 Apache-2.0 仓库。

### 决策
仅提取格式事实、算法行为、测试向量和兼容结果，自主实现 C#。大型 MIX 通过只读 seekable window 访问；结构解析、名称解析、挂载优先级和 provenance 分层。WP-02A schema-1 manifest 保持不变。加密读、复用 key-source 的加密写、校验读、校验写分别报告。

### 原因
避免 GPL 代码污染、整包内存复制、未知 ID 伪造名称、嵌套预算重置以及把归档枚举顺序变成隐藏优先级。

### 代价
需要独立验证 Blowfish、Westwood key envelope、SHA-1 覆盖范围和 XCC 字节序；XCC 宽松接受的重复/重叠目录在本项目中会 fail-closed。

### 替代方案
- 直接移植 XCC；因许可证边界禁止。
- 复用目录型 `IContentSource` 伪造路径；因会丢失数字 ID 和容器链。

## 2026-08-02 - XCC 零标志扩展头与语义往返

### 背景
固定 XCC Mixer 1.47 即使无 checksum/encryption 也写出值为 0 的四字节扩展 flags；十字节空扩展头与六字节经典空头需要显式消歧。XCC 还会自动加入本地名称数据库。

### 决策
六个零字节固定解释为经典空 MIX；十字节及以上的零首字按 XCC 扩展头解释；七至九字节按经典尾部异常拒绝。写入器允许扩展零 flags。XCC 往返以条目、顺序和 payload hash 为语义标准，归档 byte identity 单独报告。

### 原因
该规则能读取真实 XCC 输出，同时保留经典空归档的确定语义；分离语义与字节结论可避免把“XCC 能打开”夸大为 writer 克隆。

### 代价
人为构造的零条目经典归档若携带未引用数据，与零 flags 扩展形式无法从字节唯一辨别；该限制必须持续公开。

### 替代方案
- 拒绝零 flags 扩展头；与真实 XCC 输出不兼容。
- 以文件名或调用方模式强制指定；会把容器解析结果依赖外部猜测。

## 2026-08-03 - PAL 原始模型与显式显示转换

### 背景
PAL 的 768 字节、256 项 RGB 和 0..63 通道范围已有一致证据，但 XCC、OpenRA 和独立实现使用不同的 6 位到 8 位转换。

### 决策
解析结果只保存不可变原始通道；严格拒绝非 768 字节、尾部和越界通道。显示转换保留左移、位复制、最近取整和 XCC 向下取整四个命名策略，不设置无标签或 `OriginalYR` 默认值。黄金审计仅把 XCC 向下取整标为工具参考策略，不宣称原版视觉默认。

### 原因
格式解析证据足以建立原始模型，但当前没有原版 YR 视觉对照可以消除转换差异。显式策略可避免把数学选择误写成兼容结论。

### 代价
后续渲染调用者必须显式选择策略；在原版视觉证据完成前，PAL 只能提升到可解析，不能提升到可显示。

### 替代方案
- 固定使用 `value << 2`、位复制或满范围缩放；均因现有证据分歧而不作为原版默认。
- 解析时覆盖原始值；因不可逆且阻碍后续对照而未采用。

## 2026-08-03 - PAL 黄金身份与公开证据边界

### 背景
三个目标 PAL 必须从权威 ProjectBaseline 的嵌套 MIX 链获取；同名松散文件、参考工具素材或变化后的候选不能自动替代。完整逐项颜色又足以还原原始 payload，不适合进入公开仓库。

### 决策
黄金审计固定唯一 Patched `YR1001_ProjectBaseline`、小写根名 `ra2.mix`、`cache.mix` ID、三个目标 ID/长度/SHA、两层 provenance 和规范化模型 SHA。审计前后比较完整目录 fingerprint。逐项原始颜色只允许写入仓库外 Cache 的 content-addressed manifest；公开摘要只保留逻辑链、聚合统计和哈希。

### 原因
同时防止错误来源、静默内容变化、名称碰撞、解析回归和版权正文泄露，并让公开证据足以验证身份而不可直接还原调色盘。

### 代价
本地黄金审计依赖固定 ProjectBaseline 身份且不能在公开 CI 重放；任何合法的本地内容更新都必须先显式复核并更新证据，不能自动接受。

### 替代方案
- 使用 Reference 中同名 PAL；因来源不权威而拒绝。
- 手工解包后读取；因绕开 MIX provenance 而拒绝。
- 提交逐项颜色表；因可还原原始内容而拒绝。

## 2026-08-03 - CSF 有序原始模型与查询边界

### 背景
CSF 物理结构可表达标签顺序、重复标签、单标签多值、普通/扩展值和任意 UTF-16 code-unit 序列；常见工具的字典、大小写折叠或宽松解码会丢失这些证据。

### 决策
Core 仅保存不可变有序文档真相，逐 code unit 解混淆且不执行 normalization、Trim、换行替换或 decoder fallback。显式精确查询返回全部候选，不实现 first-wins、last-wins、忽略大小写或语言回退。黄金身份同时固定 payload SHA 和域分离的完整模型 SHA。

### 原因
这使后续能够在不重新解析或丢失原始信息的情况下研究原版查找行为、构建确定性审计并设计语义安全 writer。

### 代价
当前调用方不能把解析器直接当作运行时本地化字典；这些策略必须在独立工作包获得原版证据后实现。

### 替代方案
- 直接使用 Dictionary；因会丢失顺序和重复项而拒绝。
- 使用宽松 UTF-16 decoder；因可能替换孤立代理并改变原始序列而拒绝。
- 在解析时决定语言和回退；因超出格式证据而拒绝。

## 2026-08-03 - INI 原始字节是真相，结构视图不得替换原文

### 背景
ProjectBaseline INI 无 BOM，参考实现对无等号行、节头位置、重复项和分号行为存在差异；任何先解码再重建的模型都会丢失待研究证据。

### 决策
权威文档保存完整原始字节和逐物理行换行，结构节点只引用不可变 slice。确认不了的语法保留为 Opaque；无 BOM 文档不使用 `Encoding.Default` 或自动 UTF-8。只有未修改 identity writer 可以声明字节往返通过。

### 原因
该边界既能立即验证格式保真，也为 WP-02G 后续研究 runtime precedence、代码页和重复项胜出保留完整证据。

### 代价
当前不能用该模型直接实现 Rules/Art/AI 运行时字典或语义编辑；Opaque 数量也不能被误解为损坏文件。
## 2026-08-03 - INI runtime policies remain independent and evidence-gated

### 决策
容器优先级、同名文件组合、名称比较、重复节、重复键、行内分号、
空白和空值分别建模。缺少证据或出现同优先级时返回 Ambiguous，不能用
source ID、枚举顺序或一个笼统 LastWins 隐式决定。

### 原因
FinalAlert 2、OpenRA、Chrono Divide、Ares、Phobos 和社区教程分别属于
编辑器、独立实现、扩展或二手资料；它们不能单独证明 stock YR 运行时。
ProjectBaseline 中 `rulesmd.ini` 与 `soundmd.ini` 都存在内容不同的两个候选。

### 影响
显式测试计划和逐值 provenance 可执行，但 stock runtime precedence 与
typed Rules/Art/AI 保持未实现。后续黑盒对照必须另行授权并只使用副本。

## 2026-08-03 - INI 物理 ASCII、输入预算和来源身份必须是独立边界

### 决策
- 所有运行时 INI ASCII 标点和空白识别都显式携带物理编码，不再由字宽
  推测 UTF-16 字节序。
- lazy candidate 输入由 resolver 有界物化；load plan 只接受已物化可信
  集合，并明确 `MaxLayers` 不负责约束调用方枚举器的前置分配。
- source ID 采用 `Ordinal` 精确身份；节名和键名的比较仍由独立策略决定。

### 原因
字节序、资源预算和身份比较分别关系到语法正确性、拒绝服务边界和
provenance 完整性，不能借用字宽、延迟检查或运行时名称规则隐式决定。

## 2026-08-03 - 最小资源 typed view 只消费完整显式 resolution

### 决策
- Ambiguous/Failed resolution 不产生部分 typed 文档。
- Rules/Art 只投影明确列出的资源发现字段，并完整继承逐值来源链。
- Opaque、未决分号和重复策略影响使结果保持 Incomplete。
- ProjectBaseline 两个 Rules 候选分别使用 ConfiguredForTesting，绝不据此选择 stock winner。

### 原因
typed projection 不能反过来替 WP-02G1 猜测 precedence，也不能用便捷默认值掩盖尚未证实的原版语义。该边界可为后续 SHP/VXL 样本研究提供可审计候选，同时保持兼容声明保守。

## 2026-08-03 - Art 多重匹配和注册表 ordinal 冲突不产生隐式赢家

- G2 名称策略匹配多个 G1 值时，字段状态为 `Ambiguous`，单值属性为空；全部候选按稳定键排序后进入模型和哈希。
- Ambiguous 字段不进入资源引用，也不能产生 SHP/VXL route candidate。
- 同一 Rules registry 内相同解析 ordinal 的全部条目保留并标记 Incomplete；不同 registry 的相同 ordinal 相互独立。

## 2026-08-03 - ProjectBaseline same-name INI files compose by value identity

### Decision
- The configured layer order is `ra2 -> ra2md -> expandmd01..99 -> loose`.
- Same-name documents overlay low-to-high by `SectionName + KeyName`; they are
  not whole-file winners, fallback-only files, or concatenated text.
- The result preserves each value winner and all overridden candidates with
  document layer, physical line, source, and archive provenance.
- This policy is `ConfiguredForProjectBaseline`, not
  `ConfirmedByOriginalRuntime`.

### Independent unresolved boundaries
Section/key comparison, duplicate sections and keys, inline semicolons,
whitespace, and empty-value override/deletion remain separate policies. The
cross-document composition decision does not select them implicitly.
