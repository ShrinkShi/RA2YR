> **来源声明 / Source notice:** 本研究由 **ChatGPT 网页版**独立完成，**不是本地 Codex Agent 的产物**。本文是格式契约分析，不是实现草稿。 / Independently researched by **ChatGPT Web**, **not by the local Codex Agent**. This is a format-contract analysis, not an implementation draft.

# SHP(TS) flags 3 行宽契约

## 1. 必须分开的四个长度

当前冲突容易被“width”一词混淆。首版分析必须分开：

1. **Visible rectangle width**：descriptor `WidthRaw`，与 `XRaw` 一起定义局部矩形在 canvas 中的可见范围候选；
2. **Raw stride**：flags 0/1 的每行字节数，当前为 `WidthRaw`；
3. **Compressed line input length**：flags 3 行头的 `lineLength`，单位是压缩输入字节并包含2字节头；
4. **RLE produced span**：执行行 payload 后产生的索引数量，当前 ProjectBaseline聚合为 `WidthRaw + 1`。

不能在没有证据时把其中任意两个自动合并，也不能用输出裁剪掩盖它们的差异。

## 2. 已确认输入合同

### 2.1 行边界

强来源共同支持：

```text
rowStart:
    u16le lineLengthIncludingHeader
    payload[lineLength - 2]
```

因此一行的输入终点是：

```text
rowEnd = rowStart + lineLength
```

而不是：

- 遇到索引0；
- 输出达到width；
- 读到下一个frame offset；
- 读到EOF；
- 遇到某个未确认sentinel。

### 2.2 命令机械行为

公开实现最一致的机械模型：

- `b != 0`：消费1输入字节，产生1个索引 `b`；
- `00 count`：消费2输入字节，产生 `count` 个0；
- `00 00`：消费2输入字节，产生0个索引，但是否允许仍未确认。

当前冲突不应通过改变输入推进规则来回避。

## 3. 未确认输出合同

PR #11 当前要求每行：

```text
exactInputConsumption == true
producedOutput == WidthRaw
```

第一项是强安全要求。第二项是合理候选，但现有黄金聚合对所有257个非空flags 3帧都给出：

```text
producedOutput == WidthRaw + 1
```

这意味着至少有一个尚未建模的边界：

- 某一命令的输出语义不同；
- 某一行尾命令不属于 visible pixels；
- compressed span 比 visible width 多一列；
- descriptor width不是当前假设的输出计数；
- 行输入域多包含一个非像素项；
- 样本或format被系统性误分类。

## 4. raw flags 0/1 对推理的约束

raw payload当前按 `WidthRaw * HeightRaw` 成功读取。除非后续审计证明 raw路径也被错误正常化，否则：

- H2“descriptor width普遍是inclusive max”受到强烈反证；
- 任何把所有frame width改成`+1`的方案都会破坏raw；
- 如果RLE确实允许额外列，应把它表达为压缩路径的独立span/guard规则，而不能改写descriptor raw width；
- canvas rectangle仍应以目录字段为基准，直到原版证据证明相反。

## 5. XCC 的边界行为意味着什么

XCC decode3跟踪每行已输出 `x`：

- literal直接增加x并写出；
- zero-run如果将越过cx，则只写到cx；
- consumer随后每行只复制cx字节。

可得出的有限结论：

- XCC设计者预期或至少容忍zero-run在行右边界超出；
- 若ProjectBaseline第`width+1`个输出来自zero-run，XCC行为可以解释其可显示性；
- 这不是“最后一个像素总应丢弃”的证据；
- 如果额外输出来自literal，XCC的实现反而存在潜在越界/跨行问题，不能作为安全蓝图；
- XCC writer按cx个输入像素编码，不主动产生额外guard，因此XCC可读行为和XCC写出行为并不共同证明原版contract。

## 6. OpenRA 的边界行为意味着什么

固定版本OpenRA：

- 将奇数descriptor尺寸补成偶数data buffer尺寸；
- 每行从 `dataWidth * row` 开始写；
- RLE helper解码完整payload，不返回或验证row output count。

因此：

- 对奇数width，多出的一个输出可能落在补齐列；
- 对偶数width，多出的一个输出可能落入下一行起点或越过最后行缓冲，取决于实现细节；
- 本地偶数width失败120个，所以“OpenRA偶数补齐”不能解释完整数据集；
- OpenRA能显示只证明它没有严格发现该冲突，不证明extra output属于visible row。

## 7. frame range、padding 与 row conflict

`DataUpperBoundRelative`定义：

```text
upper = next distinct frame offset greater than current offset
        or file end
```

它用于限制frame读取，但RLE内部还有更窄层级：

```text
frame bound
  └─ height rows
       └─ each row's lineLength bound
```

因此需要分别记录：

- **row trailing input**：输出达到width时，当前row payload剩余的命令字节；
- **frame trailing bytes**：完成height行后，upper bound前剩余字节；
- **alignment padding**：有可复现模式的非语义对齐字节；
- **next-frame overlap**：实际消费越过下一个distinct offset。

本轮聚合只说明row 0输出溢出，不支持把extra output归因于frame padding。

## 8. 可接受的未来模型形状

只有证据通过后，production层才可在下列模型中选择：

### Model S：Strict visible width

- payload所有输出均为visible pixels；
- 必须恰好产生WidthRaw；
- 当前ProjectBaseline flags3保持未实现。

### Model G：Visible width + validated transparent guard

- 行payload可在可见width后产生一个满足严格条件的透明guard；
- guard必须由独立样本和公开行为共同证明；
- 输入仍必须精确结束；
- 不能接受literal guard、多个guard或任意overflow；
- 不能通过通用clamp实现。

### Model T：Explicit terminal command

- 某种最后命令只终止行而不属于visible output；
- 必须有公开证据或原版A/B行为；
- 不能把任意最后字节声明为sentinel。

### Model W：Separate compressed span

- descriptor width仍为visible rectangle；
- compressed scanline另有可推导或隐含span；
- 必须解释span来源、canvas写入、raw差异和writer行为。

在证据不足时，不添加这些模型到production API；它们只属于forensic probe分类器。

## 9. 必须保持的不变量

任何未来修正都必须保持：

- raw descriptor原样保留；
- frame rectangle不因RLE输出被静默扩大；
- 每条命令消费有界、命令预算存在；
- row输入精确消费；
- frame不越过可信window；
- 不因角色、文件名或SHA改变二进制语义；
- 不公开原始命令、像素或可还原轮廓；
- 不把裁剪和补零伪装成格式支持。
