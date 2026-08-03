> **来源声明 / Source notice:** 本研究由 **ChatGPT 网页版**完成并提交，**不是本地 Codex Agent 的产物**。 / Researched and submitted from **ChatGPT Web**, **not by the local Codex Agent**.

# 压缩与 flags 变体

## 1. 先纠正命名

目录 `0x08` 是 32 位 flags。低两位的主流解释：

| Bit | 名称 | 行为 |
|---:|---|---|
| 0 (`0x1`) | `HasTransparency` | 绘制时 0 值应作为透明处理 |
| 1 (`0x2`) | `UsesRle` | 数据进入某种逐行路径；对值 `2` 的具体路径存在来源冲突 |

推荐模型：

```text
RawFlags: uint
HasTransparency = (RawFlags & 0x1) != 0
UsesRle         = (RawFlags & 0x2) != 0
UnknownFlags    = RawFlags & ~0x3
```

不要建立 `enum Format0, Format1, Format2, Format3, Format4` 并丢失组合和未知位。

## 2. 已确认与来源冲突组合

| RawFlags | 位模型 | 来源名称 | 实际数据行为 | SHP(TS)适用性 | 交叉确认 | 冲突 |
|---:|---|---|---|---|---|---|
| `0` | `UsesRle=false`, `HasTransparency=false` | opaque/raw、format 0 | `width*height` 原始索引；0不一定按透明画 | 已确认 | XCC、OpenRA、ModdingWiki | 无关键冲突 |
| `1` | `UsesRle=false`, `HasTransparency=true` | transparent/raw、format 1 | 同样是原始索引；0由 blitter当透明 | 已确认 | XCC位判断、OpenRA、ModdingWiki | 部分实现只把它叫“uncompressed” |
| `2` | `UsesRle=true`, `HasTransparency=false` | format 2、RLE without transparency | 结构上可表达；XCC按 `flags & 2` 使用 RLE-Zero；OpenRA使用 length-prefixed raw scanlines | source-conflicting / underconfirmed | 两个读取器都存在，但行为不一致 | 当前不能选择默认解码策略 |
| `3` | `UsesRle=true`, `HasTransparency=true` | transparent RLE、format 3 | 逐行 RLE-Zero | 已确认且主要压缩形式 | XCC、OpenRA、ModdingWiki、ModEnc | `cnc-formats`误标为 LCW |
| `>=4` | 存在未知位 | unknown flags | 未确认；可能是未知 blitter位或损坏 | 不得提前支持 | 无 | 应保留原值并诊断 |

`RawFlags == 2` 在位模型下是可表达状态。问题只在于公开来源对其字节流解释冲突，且当前没有足够黄金证据决定首版默认策略。

## 3. 原始数据

原始局部帧为 row-major `width × height` 字节，不含行长度。flags 0 和 1 的解码字节相同，差别在后续透明绘制语义。

防御性要求：

- checked `width * height`；
- data offset + area checked；
- 禁止不足时补零；
- 超出当前可信帧区间时失败；
- 不因数据中出现 0 自动改写 `HasTransparency`。

## 4. SHP(TS) RLE-Zero

### 4.1 每行结构

```text
u16le lineByteLengthIncludingHeader
byte commands[lineByteLength - 2]
```

执行 commands：

- `b != 0`：输出一个字面索引 `b`；
- `b == 0`：必须再读取一个 `count`，输出 `count` 个 0；
- 每个命令必须消费输入；
- 每行命令数必须受预算限制；
- 行输入必须精确消费声明的 command bytes；
- 行输出最终必须恰为 frame width；
- 共处理 frame height 行。

### 4.2 zero-run 与 `00 00`

`00 count` 是唯一得到交叉确认的运行控制。通常 `count > 0` 时输出对应数量的零索引。

`00 00` 的已确认机械行为只有：

- 消费两个输入字节；
- 输出零个像素；
- 不会让正确的 decoder 停留在同一输入位置。

其格式语义仍未解决：它可能是 no-op、padding，也可能是非法命令。当前不能确定严格模式是否接受。实现必须：

- 对每个命令计数并应用命令预算；
- 保证输入位置推进；
- 将零输出与输入推进分别记录；
- 在行结束时要求声明输入被精确消费；
- 在行结束时要求输出像素数恰为 width；
- 将是否接受 `00 00` 保留为显式策略或未支持状态，等待黄金审计。

### 4.3 literal-run

没有证据显示 SHP(TS)使用“长度 + N 个 literal”的独立 literal-run 命令。非零字节只是单个 literal。测试矩阵中的 literal-run 用例应验证**连续 literal 字节序列**，不能创造不存在的控制码。

### 4.4 空行

宽度为 0 的 canonical empty frame 不应进入逐行解码。对非零宽度，合法全透明行可编码为：

```text
04 00 00 width
```

仅当 `width <= 255` 且 `width > 0`。更宽行如何分割 zero-run，应由多个 `00 count` 完成。行长度为 2 且非零宽度会输出不足，应失败。

### 4.5 终止

正常终止不是遇到 sentinel，而是：

- 行输入达到该行 `lineByteLength`；
- 行输出达到 width；
- 完成 height 行。

这三个条件必须一致。任一提前或超出都产生诊断。命令预算是额外的防御边界，不替代输入和输出的精确终止条件。

## 5. 同文件混合

目录按帧保存独立 flags，因此同一 SHP(TS) 可以混合原始帧和 RLE 帧。实现不得从首帧推断整个文件的压缩方式。

## 6. 不属于 SHP(TS) 的压缩

| 名称 | 实际归属 | 处理 |
|---|---|---|
| LCW / Format80 | TD/RA1 SHP、其他 Westwood格式 | SHP(TS)解析器不得调用 |
| XOR Delta / Format40 | TD/RA1 SHP | 不得建立 SHP(TS) reference |
| XOR Chain / Format20 | TD/RA1 SHP | 不得建立 SHP(TS) dependency chain |
| XCC format 4 | XCC内部紧凑中间格式 | 不作为 `.shp` flags 4 接受 |
| Dune II RLE + optional LCW | Dune II SHP | 单独家族解析器 |
| `cnc-formats` compression 3 = LCW | 该实现当前行为 | 与强来源冲突，不能采纳 |

## 7. flags 2 的处理建议

首版策略：

- 解析目录并原样保留 `RawFlags == 2`；
- 记录 `SourceConflictingFlags2` 或等价诊断；
- 不将它作为首版正常写入目标；
- 在本地黄金审计前，不选择 XCC RLE-Zero 或 OpenRA length-prefixed raw scanlines 作为默认解码策略；
- 默认读取路径应返回“来源冲突、策略未选择”的结构化结果，而不是静默猜测；
- 研究模式可以在显式调用下分别尝试两种受限实验路径，但实验结果不能自动写回或提升兼容状态。

这能保留位模型事实，同时避免把任何单一读取器的兼容行为误写成格式规范。
