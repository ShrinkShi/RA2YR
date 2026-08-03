> **来源声明 / Source notice:** 本研究由 **ChatGPT 网页版**完成并提交，**不是本地 Codex Agent 的产物**。 / Researched and submitted from **ChatGPT Web**, **not by the local Codex Agent**.

# 来源清单与冲突表

## 1. 固定来源

| 来源 | URL | revision | 相关路径/页面 | 许可证 | SHP家族/支持 | 可采用事实 | 已知缺陷或宽松行为 |
|---|---|---|---|---|---|---|---|
| OpenRA/OpenRA | `https://github.com/OpenRA/OpenRA` | `a520984d91eda9de48a62b1d15c1e3bad0d4fb1a` | `OpenRA.Mods.Common/SpriteLoaders/ShpTSLoader.cs` | GPL-3.0-or-later；reference-only | SHP(TS)：raw 0/1、特殊2、RLE 3 | 8+24布局、offset、空帧、format 3逐行RLE | format 2解释与flags模型冲突；对bogus frame有兼容绕过；不能移植代码 |
| OmniBlade/xcc `encoding` | `https://github.com/OmniBlade/xcc/tree/encoding` | `62bb77080f13bdf65c79c84837b7cc264bdd432d` | `xcc/misc/cc_structures.h`, `shp_ts_file.h/.cpp`, `shp_decode.*`, `shp_file.cpp` | 原XCC SourceForge项目标注GPL-2.0；GitHub导出快照未定位根LICENSE；reference-only | 同时含TD/RA1与SHP(TS) | 8/24结构、32位flags/unknown/reserved/offset、`flags & 2` RLE判定、空帧、边界；清楚区分旧SHP | 多处只做宽松`is_valid`；signed声明不等于格式语义；internal format4易被误读 |
| XCC SourceForge | `https://sourceforge.net/projects/xccu/` | 指定核对目标：SVN `r1201` | 原CVS/SVN中的同名`cc_structures`, `shp_ts_file`, `shp_decode` | SourceForge项目元数据GPL-2.0；reference-only | 多家族 | 作为OmniBlade导出谱系核对目标 | 当前公开网页未稳定提供可复现的r1201文件级内容；不得宣称与GitHub commit等价 |
| Electronic Arts FinalSun/FinalAlert2 | `https://github.com/electronicarts/CNC_TS_and_RA2_Mission_Editor` | `6abf0f557469baea73079c6bf6550709e2e3584e` | `MissionEditorPackLib/MissionEditorPackLib.cpp`, bundled XCC headers | GPL-3.0-or-later；reference-only | TS/RA2编辑器 + XCC | 官方公开编辑器确实依赖`Cshp_ts_file`/`shp_decode`；可验证工具谱系 | 不是游戏运行时源码；打包XCC经过裁剪和补丁 |
| Electronic Arts CnC Remastered | `https://github.com/electronicarts/CnC_Remastered_Collection` | 仓库归档`master`；实现前应记录确切commit | `TIBERIANDAWN/*`, `REDALERT/*` shape/LCW相关代码 | GPL-3.0 + additional terms；reference-only | TD/RA1 SHP | 旧家族LCW/XOR/reference边界 | 不含TS/RA2/YR SHP(TS)运行时，不能证明SHP(TS)行为 |
| ModdingWiki SHP(TS) | `https://moddingwiki.shikadi.net/wiki/Westwood_SHP_Format_(TS)` | `oldid=10936` | 格式页 | 引用页面未显式展示内容许可证；事实型参考，不复制正文/代码 | SHP(TS)：flags 0/1/3 | 8/24字段表、flags位、FrameColor、reserved、offset、阴影说明 | 社区整理而非官方规范 |
| ModdingWiki RLE-Zero | `https://moddingwiki.shikadi.net/wiki/Westwood_RLE-Zero` | `oldid=11565` | Tiberian Sun小节 | 同上 | SHP(TS)/Dune对比 | 行长包含2字节头、`00,count`、行边界 | 示例代码不是本项目实现蓝图 |
| ModdingWiki SHP(TD) | `https://moddingwiki.shikadi.net/wiki/Westwood_SHP_Format_(TD)` | `oldid=10933` | 格式页 | 同上 | TD/RA1 | LCW/XOR/20/40/80/reference明确只属旧家族 | 仅用于排除 |
| ModEnc SHP | `https://modenc.renegadeprojects.com/SHP` | `oldid=20503` | SHP(TS)教程 | 页面允许署名镜像但非标准代码许可证；事实型参考 | SHP(TS) | 外部PAL、`00,count`、用途列表 | 字段表不完整，旧教程措辞含混 |
| iron-curtain-engine/cnc-formats | `https://github.com/iron-curtain-engine/cnc-formats` | `77da596ed72a1201740e054855bf2ff60640bfa9` | `src/shp_ts/mod.rs`, tests | MIT OR Apache-2.0 | 意图支持SHP(TS)：raw/RLE/LCW映射 | 安全预算、fuzz方向、8/24基础 | 将compression 3当LCW，与强来源冲突；截断时补透明；帧数据借到EOF；不可作为兼容真值 |
| OS SHP Builder | `svn://svn.ppmsite.com/shp_builder`；浏览入口见PPM工具页 | 尚未建立可复现revision pin | `Shp_File.pas`, `Shp_Engine.pas`等 | 未定位清晰OSI许可证；reference-only | TD/RA1与SHP(TS) | 后续用于flags2、写入对齐、FrameColor交叉验证；PPM revision 29记录曾将误称Transparent字段更名为RadarColor | 部分代码由XCC转换；历史帖子自述存在flaws；不能机械移植 |
| Project Perfect Mod | `https://ppmforums.com/` | 具体主题固定URL/发布日期 | SHP/RLE讨论、工具发布、SVN变更帖 | 无统一代码许可证；reference-only | 多家族 | 记录社区命名和工具行为 | 论坛陈述不是规范，必须交叉确认 |
| Chrono Divide mod-sdk | `https://github.com/chronodivide/mod-sdk` | `5943c4ae6c19897929d348a417d6d2f1481b75fd` | 仓库资源/配置SDK | 以仓库许可文件为准；本轮未取得公开引擎parser | RA2资源生态，不是格式实现来源 | 仅证明公开mod生态与原版资源引用 | 无公开SHP解析器路径，不能提供格式事实 |
| CnCNet相关项目 | `https://github.com/CnCNet` | 本轮未找到可固定的独立SHP(TS) parser | 待补 | 待补 | 待补 | 无 | 不应为了覆盖来源名单而虚构证据 |

## 2. 来源权重

1. 同家族的独立运行读取器 + 明确结构文档；
2. 官方公开编辑器/打包工具的结构使用；
3. XCC/OS SHP Builder等历史工具；
4. 社区格式文档；
5. 论坛陈述；
6. 与强来源冲突的单个现代库。

许可证更宽松不等于格式事实更可靠；`cnc-formats`虽为MIT/Apache，但其LCW映射仍不能覆盖多来源一致的RLE-Zero结论。

## 3. 主要冲突

| 主题 | 多来源一致 | 只是命名不同 | 字段/编号实质冲突 | 当前决定 |
|---|---|---|---|---|
| 文件头 | 8字节、首u16=0、canvas、帧数 | zero/magic/empty | 0帧是否合法 | 布局确认；0帧严格拒绝、保留审计问题 |
| 目录项 | 24字节、x/y/w/h、32位字段、offset | compression vs flags | x/y signed/unsigned；0x0C unknown vs FrameColor | 保存raw；几何按非负范围验证；FrameColor语义可选 |
| flags 0/1 | 都是raw bytes | opaque/transparent raw | 无 | 解码相同，绘制语义分离 |
| flags 2 | XCC按RLE位处理 | format2 | OpenRA特殊length-prefixed raw路径；flags模型称矛盾组合 | 不作为正常编码；等待黄金样本 |
| flags 3 | RLE-Zero | format3/compressed | `cnc-formats`称LCW | 采纳RLE-Zero；将LCW映射标为实现缺陷 |
| literal-run | 连续非零字节形成literal序列 | 有来源误称literal-run | 无独立长度控制码证据 | 不实现虚构控制码 |
| data length | 目录无显式长度 | 可由下一offset推断候选区域 | 部分读取器直接借到EOF | 按raw面积/RLE行自描述解码；下一offset只做结构检查 |
| reference/delta | SHP(TS)强来源均无字段 | “format20/40/80”常混用 | 用户要求dependency测试，但它们属于旧家族/未来契约 | dependency API仅预留；相关测试标记非本家族 |
| shadow | 文件无shadow元数据 | second-half convention | 偶数帧不必然是shadow | 通用解码不自动合并 |
| PAL | 外部PAL | shared palette | 无 | SHP Core只输出索引 |
| XCC format 4 | 工具内部中间格式 | 名字含format | 容易误认成flags值4 | 明确排除 |

## 4. 许可证使用规则

- GPL来源只能reference-only：不复制代码、不逐句翻译、不机械改写、不生成接近源码结构的C#草稿。
- 许可证不明确的论坛/工具源码只做reference-only。
- 即使来源是MIT/Apache，也不能用其与强证据冲突的行为覆盖本项目格式结论。
- 最终实现必须由本目录中的行为规范、合成样本和本地黄金审计共同驱动。
