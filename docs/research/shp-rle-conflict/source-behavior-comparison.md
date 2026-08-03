> **来源声明 / Source notice:** 本研究由 **ChatGPT 网页版**独立完成，**不是本地 Codex Agent 的产物**。下表只概括可观察行为；GPL 或许可证不明源码均为 reference-only，未复制、逐句翻译或机械改写。 / Independently researched by **ChatGPT Web**, **not by the local Codex Agent**. The table records behavior only; no GPL or unclear-license source was copied, line-translated, or mechanically rewritten.

# 公开来源行为比较

## 1. 使用规则

- revision 必须固定；无法取得文件级 revision 时明确写“只固定发布版本/页面 oldid”。
- “能打开/能显示”不是格式正确性的充分证据。
- 宽松读取器只能证明一种兼容行为存在，不能自动成为 RA2YR 默认规则。
- GPL、GPL-compatible 或许可证不明源码只用于行为研究。

## 2. 核心读取实现

| 来源 | 固定版本 | 路径 | 许可证 | 行长含头 | width处理 | 最后命令/`width+1` | flags 2/3 | 可信度与缺陷 |
|---|---|---|---|---|---|---|---|---|
| OpenRA current pin | commit `a520984d91eda9de48a62b1d15c1e3bad0d4fb1a` | `OpenRA.Mods.Common/SpriteLoaders/ShpTSLoader.cs`; `OpenRA.Mods.Common/FileFormats/RLEZerosCompression.cs` | GPL-3.0-or-later；reference-only | 是，读取 `u16-2` payload | descriptor 奇数宽/高先补到偶数缓冲尺寸；每行写入以补齐宽度为 stride；不核验实际输出数 | 解码到 payload 结束；没有逐行 width 比较。多出输出可能写入补齐列或下一行区域；不明确接受，也不明确丢弃 | 2 为 length-prefixed raw scanlines；3 为 RLE-Zero | 活跃独立引擎，结构价值高；行宽行为宽松，不能证明 `+1` 合法。 |
| OpenRA 初始加入 | PR #3193，head `2d685ab07d22eab9a60d1c83accf5a93e4cdfde7`，merge `0767aaa045f6f97040eb40f433150098d976edfa` | 当时的 `OpenRA.FileFormats/Graphics/ShpTSReader.cs` | GPL；reference-only | 是 | zero-run 超过 cx 时裁短；异常被捕获；literal没有对等的可靠行边界证明 | 可容忍最终 zero-run超宽；不是严格契约 | 分开处理2与3 | 早期实现明显宽松，后续被大幅重写；只能作为历史行为。 |
| OpenRA sprite rewrite | PR #4185，merge `1f5744ed8f8744c233d7b9930c881a51e785e324` | 当时的 `OpenRA.FileFormats/Graphics/ShpTSReader.cs` | GPL；reference-only | 是 | 3 解码整个 payload；2 逐行 raw；不验证行输出等于 width | 无专用 sentinel/末像素逻辑 | 明确区分2/3 | 删除旧实现大量特殊逻辑，但仍未建立 strict width contract。 |
| OpenRA bogus-frame fix | PR #13882，head `b3a6c58392723972830d55a54eb6998668fffba4`，merge `5b16bb952f6071eb6edd3e3ff9678d194f021dd0` | `ShpTSLoader.cs` | GPL-3.0-or-later；reference-only | 未改变 | 只修复类型探测和零尺寸帧遍历 | 不涉及 row width | 保留 `<4` 类型检测 | 不能作为本冲突的支持或反证。 |
| XCC / OmniBlade encoding | commit `62bb77080f13bdf65c79c84837b7cc264bdd432d` | `xcc/misc/shp_decode.cpp`; `xcc/misc/shp_ts_file.cpp` | SourceForge项目元数据 GPL-2.0；GitHub导出未定位独立根许可证；reference-only | 是 | nonzero literal直接写；zero-run若 `x+count>cx` 则裁到 `cx-x`；每行 x 重置；消费者只复制每行 cx 字节 | 明确容忍/丢弃**zero-run**越界部分；没有一般“丢最后一个像素”规则；literal越界没有同等安全裁剪 | `compression & 2` 进入 decode3，因此2/3都走RLE位路径 | 与本冲突最相关的宽松行为。若额外输出来自最终 zero-run，可解释 XCC为何显示正常；仍不能证明原版规范要求裁剪。 |
| XCC SourceForge | 可定位发布：XCC Utilities 1.46（2008-05-02）；项目最后更新2013；历史目标 SVN r1201未能从公开网页稳定取得文件级内容 | SourceForge project/files；历史同名 decode files | 项目元数据 GPL-2.0；reference-only | 不能用仅有发布页独立确认 | 不能用二进制发布页独立确认 | 不能独立确认 | 不能独立确认 | 作为 XCC 谱系和许可证/发布时间 pin；具体行为以 OmniBlade 固定源码为准，不声称二者逐文件等价。 |
| EA FinalSun/FinalAlert 2 | commit `6abf0f557469baea73079c6bf6550709e2e3584e` | `MissionEditorPackLib/MissionEditorPackLib.cpp` 及 bundled XCC `shp_ts_file` / `shp_decode` | GPL-3.0-or-later；reference-only | 继承 bundled XCC | 通过 `Cshp_ts_file` 和 `shp_decode` 读取，不构成独立算法证据 | 继承工具库行为；未发现官方游戏 blitter width contract | 继承 XCC家族 | 官方公开的是编辑器源码，不是 RA2/YR 游戏运行时；价值在确认工具谱系。 |
| iron-curtain-engine/cnc-formats | commit `77da596ed72a1201740e054855bf2ff60640bfa9` | `src/shp_ts/mod.rs` | MIT OR Apache-2.0 | 是 | while输出小于cx；zero-run裁到剩余宽度；不足时补零；截断行/剩余行也补透明；最终 resize 到 area | 实际接受任何能裁成/补成cx的结果，`width+1`被静默截断 | 错把 1/2 当 scanline RLE、3 当 LCW | 安全工程方向可参考，但格式映射与强来源冲突，且宽度行为过于宽松；不能作为兼容真值。 |
| Chrono Divide | 公共 org 检索固定到 `chronodivide/mod-sdk` commit `5943c4ae6c19897929d348a417d6d2f1481b75fd` | 公共仓库只有 mod SDK、配置和资源说明；未定位公开 engine SHP parser | mod-sdk 许可证以仓库文件为准；没有取得 engine parser 许可证 | 无公开实现可核对 | 无公开实现可核对 | 无公开实现可核对 | 无公开实现可核对 | 只能证明引擎消费原 RA2 资源；不得虚构不可见 parser 行为。需要上游公开源码、source map或维护者说明。 |

## 3. OS SHP Builder 与工具历史

| 来源 | 固定版本 | 路径/证据 | 许可证 | 行宽相关结论 |
|---|---|---|---|---|
| Open Source SHP Builder | 可定位正式版 3.37（2014-01）；SVN 历史至少固定 rev 29、47、85；本轮以 rev 85（2015-05-24）作为可定位末期 revision | `svn://svn.ppmsite.com/shp_builder`; `Shp_Engine.pas`, `Shp_File.pas`, `SHP_Image_Save_Load.pas` | 项目公开允许分析、修改和再发布并要求署名；论坛明确称“不遵循 GNU GPL”；不是清晰 OSI license，reference-only | 历史记录显示 compression 3 保存/空帧/offset 有多次缺陷，且部分代码由 XCC 转换。公开网页未稳定提供本轮可复现的 rev85 文件正文，因此不能据此断言最后命令、width+1 或 sentinel。 |
| OS SHP Builder 3.37 发布说明 | 2014-01-03 | PPM 发布帖 | 同上 | 声称与 XCC/其他工具兼容并修复 radar color，不是 row-width contract 证据。 |
| PPM compression 3 历史 | 固定主题：`topic-6531`, `topic-11878`, `topic-42483`, `topic-5497`, `topic-6369` 等 | 论坛主题与发布日期 | 无统一代码许可证；reference-only | 证明社区工具长期存在 offset way、blank-frame、compression 3 保存和超大宽高问题。论坛讨论不能替代可复现实现或原版实验。 |

## 4. 格式文档

| 来源 | 固定页面 | 许可证 | 行长 | width/最后命令 | 评价 |
|---|---|---|---|---|---|
| ModdingWiki SHP(TS) | `https://moddingwiki.shikadi.net/w/index.php?title=Westwood_SHP_Format_(TS)&oldid=10936` | 页面未明确展示可直接用于代码的许可证；事实参考 | 表示每行有 UINT16LE 输入长度 | 把 Width/Height 描述为局部 frame尺寸；不定义 `width+1`、sentinel或guard | 结构说明强，不能解决本冲突。 |
| ModdingWiki RLE-Zero | `https://moddingwiki.shikadi.net/w/index.php?title=Westwood_RLE-Zero&oldid=11565` | 同上 | 明确长度包含2字节头 | 示例 decoder 对达到行宽前仍有 payload会报对齐错误；不接受 width+1；同时页面对早期Dune变体提到overflow可忽略 | 示例是社区实现，不是原版源码。与 ProjectBaseline聚合直接冲突，说明问题未解决。 |
| ModEnc SHP | `https://modenc.renegadeprojects.com/index.php?title=SHP&oldid=20503` | 社区百科；事实参考 | flags bit 1表示RLE，行结构说明有限 | 把 frame width*height作为raw尺寸；未定义RLE额外列、末像素或sentinel | 支持结构边界，不决定行末。 |
| Project Perfect Mod | 固定具体主题而非论坛首页；例如 compression 3 格式、损坏文件、工具修复帖 | 无统一许可证 | 多数讨论接受行前长度 | 存在“line first 2 bytes是width/length”等混用措辞、工具缺陷和个人推测 | 只能生成假设与定位历史工具行为，不能提升为规范。 |

## 5. 行为维度汇总

| 行为问题 | OpenRA | XCC | cnc-formats | ModdingWiki示例 | PR #11 strict |
|---|---|---|---|---|---|
| lineLength包含2字节头 | 是 | 是 | 是 | 是 | 是 |
| 非零=一个literal | 是 | 是 | 是 | 是 | 是 |
| `00 count`=count个0 | 是 | 是 | 是 | 是 | 是 |
| 输出超过width | 不逐行检查；可能跨写 | zero-run裁短，literal不等价保护 | 裁短 | 报错 | 报错 |
| 输出不足width | 缓冲区预置0，未明确逐行验证 | return值未验证；缓冲用途可能残留 | 补零 | 最终数组预置0，但行输入对齐检查为主 | 报错 |
| 接受width+1 | 隐式可能 | zero-run来源时隐式接受 | 是，静默裁剪 | 否 | 否 |
| 丢弃最后一个像素 | 无一般规则 | 仅zero-run越界部分 | 通过上限裁剪 | 无 | 无 |
| 末尾0作为sentinel | 无 | 无 | 无 | 无 | 无 |
| 最后命令特殊 | 未定义 | 只有zero-run边界裁短 | 无 | 无 | 无 |
| 区分flags2/3 | 是 | 位判断不区分算法 | 映射错误 | 文档认为有效RLE通常3 | 是，2拒绝 |

## 6. 来源权重决定

1. **不能把任何一个宽松读取器当规范。**
2. XCC 的 final zero-run裁短是目前最能解释稳定 `+1` 的公开行为，但只在本地探针证明额外输出来自最终 zero-run后，才可作为“候选兼容语义”的支持。
3. OpenRA current对奇数尺寸补偶数是渲染/offset适配，不是 descriptor width 为 inclusive bound 的证明；偶数宽度样本同样失败进一步削弱该解释。
4. `cnc-formats` 的裁剪/补零只能用作负面比较：它展示了如何把损坏或未知语义静默正常化，不应复制。
5. EA FinalSun/FinalAlert 2 与 OS SHP Builder不能作为独立原版运行时来源；它们主要沿用 XCC 或社区工具链。
6. Chrono Divide未公开可定位 parser时必须保持空白，不以“它能运行RA2资源”倒推具体行末规则。
