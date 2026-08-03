> **来源声明 / Source notice:** 本研究由 **ChatGPT 网页版**完成并提交，**不是本地 Codex Agent 的产物**。 / Researched and submitted from **ChatGPT Web**, **not by the local Codex Agent**.

# SHP 家族边界

## 1. 名称不是格式标识

Westwood 及社区工具对多个互不兼容的文件家族都使用 `.shp` 扩展名。解析器必须由**调用上下文、文件结构和严格校验**共同选择家族，不能只看扩展名。

| 家族 | 典型游戏 | 关键结构 | 压缩/依赖 | 本任务状态 |
|---|---|---|---|---|
| TD/RA1 SHP | TD、RA1、Sole Survivor | 14 字节主头；`frames + 2` 个 8 字节索引项 | LCW/Format80、XOR delta/Format40、链式 Format20、参考偏移或帧号 | 明确排除，仅用于冲突边界 |
| TS/RA2/YR SHP(TS) | TS、FS、RA2、YR | 8 字节主头；每帧 24 字节目录；全局 canvas + 局部矩形 | 原始索引或逐行 RLE-Zero；未确认任何 delta/reference 字段 | 唯一研究目标 |
| Dune II SHP | Dune II | 帧偏移表 + 每帧独立头；16/32 位偏移版本 | RLE-Zero，外层可选 LCW，可能有 remap table | 明确排除 |
| 其他 Dune/Lands of Lore 同名格式 | Dune/Lands of Lore 系列 | 各自不同头、偏移和压缩层 | 可能套 CPS/LCW/RLE | 明确排除 |
| XCC internal format 4 | XCC 工具内部转换路径 | 6 字节全局头 + 每帧 4 字节 margin 描述 | 工具内部紧凑表示，转换后写成正常 SHP(TS) flags `3` | 不得作为 SHP(TS) on-disk variant |

## 2. 最危险的错误迁移

以下事实只在 TD/RA1 家族得到证据支持，**不得移植到 SHP(TS)**：

- `0x80` LCW key frame；
- `0x40` XOR base；
- `0x20` XOR chain；
- 每帧 reference offset/reference format；
- 通过前一帧或 key frame 恢复图像；
- `frames + 2` 目录尾项；
- SHP 内嵌 PAL 的旧家族标志；
- 以 delta buffer size 作为主头字段。

SHP(TS) 采用结构动画拆分、局部裁切和逐行 RLE-Zero，不需要旧家族的 XOR delta 机制。

## 3. “format 1/2/3/4”为什么冲突

不同来源使用的“format”并非同一命名空间：

1. OpenRA 的 `Format` 是读取 32 位 flags 的低字节后，对值 `0..3` 做分支。
2. XCC 将整个字段视作 32 位 `compression`，并以 `compression & 2` 判断是否进入 RLE 解码。
3. ModdingWiki 将其解释为 `HasTransparency` 与 `UsesRle` 两个位，因而 `0/1/2/3` 是位组合，不是四种算法。
4. XCC `shp_encode4`/`shp_decode4` 的“4”是另一种工具内部封装，不能写入 SHP(TS) flags。
5. TD/RA1 社区常把 Format20/40/80称为 frame format；这些数值来自目录高字节标志，不属于 SHP(TS)。

因此项目 API 不应暴露 `Format1/2/3/4` 这类名称。应保留 `RawFlags`，并派生 `HasTransparency`、`UsesRle` 与语义状态。

## 4. 家族识别建议

SHP(TS) 候选识别至少要求：

- 输入长度不少于 8；
- 首 `UINT16LE == 0`；
- `8 + frameCount * 24` 经 checked arithmetic 后位于窗口内；
- 每个非空目录项的 reserved 字段、矩形和 offset 满足策略；
- 空目录项满足一致的空帧规范；
- flags 未知位有诊断；
- 数据流能在帧高指定的行数内有界终止。

这只是候选识别。首字为 0 的任意文件不能因此被宣布为 SHP(TS)。

## 5. XCC 内部编号

XCC 工具或文档中的文件类型编号、菜单项编号、`format4` 中间表示都不是 Westwood 文件签名。项目文档和诊断必须写完整名称，例如：

- `ShpTsRawOpaque`
- `ShpTsRawTransparent`
- `ShpTsRleZero`
- `TdRa1Lcw`
- `TdRa1XorBase`
- `TdRa1XorChain`
- `XccInternalShp4`

不得使用脱离家族的“format 3”作为公开错误消息。
