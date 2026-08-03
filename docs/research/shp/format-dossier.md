> **来源声明 / Source notice:** 本研究由 **ChatGPT 网页版**完成并提交，**不是本地 Codex Agent 的产物**。 / Researched and submitted from **ChatGPT Web**, **not by the local Codex Agent**.

# Westwood SHP(TS) 格式档案

## 1. 范围与证据等级

本文只描述 TS/FS/RA2/YR 的 SHP(TS)。结论分为：

- **已确认**：至少两类独立来源一致，且与公开读取器行为相符；
- **高可信**：一个强来源加多个间接实现一致；
- **冲突**：来源对字段意义或宽松行为存在差异；
- **未确认**：必须等待合成实验或本地脱敏审计。

## 2. 文件头

固定大小：8 字节，小端。

| Offset | 类型 | 建议字段 | 状态 | 说明 |
|---:|---|---|---|---|
| `0x00` | `UINT16LE` | `ZeroOrFamilyMarker` | 已确认 | 正常 SHP(TS) 为 0；不是独立签名 |
| `0x02` | `UINT16LE` | `CanvasWidth` | 已确认 | 全局 canvas 宽 |
| `0x04` | `UINT16LE` | `CanvasHeight` | 已确认 | 全局 canvas 高 |
| `0x06` | `UINT16LE` | `FrameCount` | 已确认 | 后续 24 字节目录项数量 |

### 2.1 signed/unsigned

XCC C++ 结构将这些 16 位字段声明为 signed `__int16`，OpenRA 和 ModdingWiki按 unsigned 读取。文件位模式本身相同；Core 应先保留原始 `ushort`，再在几何校验层转换为非负 `int`。不要让 C# `short` 的符号扩展隐式决定格式语义。

### 2.2 零帧和“空文件”

- XCC `is_valid()` 明确拒绝 `frameCount < 1`。
- 一些通用读取器从结构上能读取 `frameCount == 0`，但这不证明游戏接受。
- 因此：0 帧必须作为边界测试；在原版证据出现前，严格模式不应把它标成“合法基线”，研究/诊断模式可返回结构化失败而非崩溃。
- “合法空 SHP”当前没有证据。

## 3. 帧目录

每帧固定 24 字节。

| Offset | 类型 | 建议字段 | 状态 |
|---:|---|---|---|
| `0x00` | `UINT16LE` raw | `XRaw` | 已确认布局；符号解释冲突 |
| `0x02` | `UINT16LE` raw | `YRaw` | 已确认布局；符号解释冲突 |
| `0x04` | `UINT16LE` | `Width` | 已确认 |
| `0x06` | `UINT16LE` | `Height` | 已确认 |
| `0x08` | `UINT32LE` | `RawFlags` | 已确认 32 位；部分实现只读低字节 |
| `0x0C` | 4 bytes | `FrameColorRaw` | 高可信；老 XCC命名 unknown |
| `0x10` | `UINT32LE` | `Reserved` | 高可信为 0 |
| `0x14` | `UINT32LE` | `DataOffset` | 已确认，绝对文件/窗口相对起点偏移 |

### 3.1 FrameColor 冲突

- XCC 结构称 `unknown`；
- OpenRA整体跳过 11 字节；
- 社区文档和专用编辑工具将 `0x0C..0x0F` 解释为雷达/帧平均色，前三字节为 8 位 RGB；
- 该字段不应在首版解析中被丢弃。建议保存原始 4 字节，语义属性命名为可选 `FrameColor`，并允许审计验证第四字节和实际用途。

### 3.2 Reserved

`0x10` 被 XCC命名 `zero`，公开文档称 Reserved。严格模式应对非零值报诊断，但在证据不足时不要静默覆盖或阻止原始值保留。

### 3.3 DataOffset 与长度

- `DataOffset` 是帧数据的绝对偏移，相对于 SHP 文件/当前内容窗口起点。
- 目录没有显式 data length。
- 原始帧长度由 `Width * Height` 确定。
- RLE 帧由 `Height` 行及每行 `UINT16LE` 长度自描述。
- 下一帧 offset **不应作为唯一解码终止条件**。
- 但目录中所有 offset 可用于构建候选区间，检测重复、逆序、重叠和越界；不得假设 offsets 一定单调。
- 文件尾可以作为最终硬边界，但不能用“读到 EOF”为正常行终止。

### 3.4 空帧

XCC接受 `Width == 0 && Height == 0 && DataOffset == 0` 的空目录项。其他组合（单维为 0、非零 offset、矩形不一致）没有得到一致支持。建议：

- canonical empty：`x/y` 原始保留，`width=0,height=0,offset=0`；
- partial empty：诊断并拒绝解码；
- 不为 canonical empty 分配 canvas-sized像素。

## 4. 坐标和 canvas

### 4.1 格式层

- 全局 canvas：`CanvasWidth × CanvasHeight`；
- 局部矩形：`X, Y, Width, Height`；
- 完整索引帧的默认重建：先以 0 填充 canvas，再把局部帧写入 `[X, X+Width) × [Y, Y+Height)`；
- `X + Width`、`Y + Height` 必须 checked；
- 局部矩形越界应失败，不裁剪；
- 原版数据常将 data offset 8 字节对齐，但对齐不是解码必要条件，首版应警告而不是硬拒绝。

### 4.2 不属于格式层

以下概念不得从 SHP header直接推导：

- 图像中心；
- 单位脚点；
- 建筑基础点；
- `Foundation`；
- `YSortAdjust` / `ZAdjust`；
- FLH；
- Unity `Sprite.pivot`；
- 世界坐标或等距地图高度。

这些由 Art.ini、对象类型、渲染排序和 Unity 适配器决定。

## 5. 像素语义

- 每个解码像素为 8 位调色盘索引；
- SHP(TS) 不内嵌 256 色 PAL；
- 颜色转换必须由外部 `WestwoodPalette` 与用途/剧院/规则选择；
- 索引 0 与透明强相关，也是 RLE-Zero唯一压缩目标；
- `HasTransparency` 是 blitter/绘制语义，不只是“使用哪种解压器”；
- 玩家色 remap 的精确索引范围不是 SHP 文件结构，应由 palette/remap 服务决定；
- Core SHP解析器只应保存/统计索引，不应直接改色。

## 6. 阴影

- 单位/建筑常把主帧放在前半、阴影帧放在后半；
- SHP 文件没有“这是阴影帧”的元数据；
- 公开文档称阴影帧通常只含索引 0 和 1；
- XCC 的 combine/split shadow 把工具预览中的 index 4 当作标记，这是转换约定，不是 SHP(TS) on-disk规则；
- “偶数帧 = 一定有阴影”是错误假设；
- 应由 Art/runtime语义与帧统计共同判定，不能在通用解码阶段自动合并或删除后半帧。

## 7. 用途差异

步兵、建筑、动画、UI、鼠标、cameo 都可以使用同一 SHP(TS) 文件层。差异主要来自：

- Art.ini帧分段、方向数、Start/LoopStart/LoopEnd/Rate；
- 建筑附属动画和 damaged/buildup图；
- UI/鼠标的绘制路径与 flags容忍度；
- 所属色 remap、阴影、半透明、闪烁等运行时效果；
- 选用的外部 PAL。

鼠标 SHP“不应 RLE”的说法有公开文档支持，但仍应通过本地审计确认其 flags 分布；解析器不能按逻辑名称改变二进制规则。

## 8. 损坏输入终止条件

每帧解码必须同时受以下硬边界限制：

1. 文件/窗口边界；
2. 帧目录与 data offset 合法性；
3. 行数恰为 frame height；
4. 每行声明长度至少 2；
5. 行结束不越过窗口；
6. 每行输出恰为 frame width；
7. zero-run 不越过本行；
8. 输出总像素不超过 width × height；
9. 操作数、行、帧和累计预算；
10. 任意循环每次必须推进输入或输出，否则立即失败。

宽松读取器把截断行补零的行为不应成为本项目默认兼容策略；损坏必须形成可定位诊断。
