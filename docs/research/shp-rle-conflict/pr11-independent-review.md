> **来源声明 / Source notice:** 本研究由 **ChatGPT 网页版**独立完成，**不是本地 Codex Agent 的产物**。只做静态审查和行为级资料比较；未复制或移植第三方源码。 / Independently researched by **ChatGPT Web**, **not by the local Codex Agent**. This is a static, behavioral review only; no third-party source was copied or ported.

# PR #11 独立代码审查

审查目标固定为：

- PR：`https://github.com/ShrinkShi/RA2YR/pull/11`
- Base：`7e43b5138c4c0042196203da6d22e1e05bad3707`
- Head：`3bf87937b523df6acd3376cd124cfe8ae9fb7634`

本文件不要求修改 PR #11。本轮结论用于解释 flags 3 行宽冲突和设计下一轮脱敏审计。

## 1. 分类定义

| 分类 | 含义 |
|---|---|
| 正确且证据充分 | 与当前仓库结构、多个公开来源和安全边界一致 |
| 防御性合理 | 即使格式事实仍未完全确认，也能安全失败且不丢失原始信息 |
| 可能固化未确认语义 | 实现内部一致，但把尚未由原版证据确认的行为设为唯一合法契约 |
| 需要本地探针 | 静态代码和公开资料不足，必须用不可逆聚合区分 |
| 不能仅靠静态研究确定 | 涉及原版 encoder/blitter 或实际样本行为，公开资料没有决定性证明 |

## 2. 审查总表

| 审查点 | 当前行为 | 分类 | 结论 |
|---|---|---|---|
| 8 字节 header / 24 字节 descriptor | checked arithmetic、有界读取、保留 raw 字段 | 正确且证据充分 | 与 XCC、OpenRA 和社区格式表一致。 |
| `ShpTsFrameDescriptor` | 保留 `WidthRaw`、`HeightRaw`、`RawFlags`、offset 和 upper bound；不发明 dependency | 正确且证据充分 | 适合后续重新解释，不应改成预先修正后的宽度。 |
| flags 0/1 | 精确读取 `WidthRaw * HeightRaw` | 正确且证据充分 | 当前审计可正常处理 raw；这也是反对“所有 descriptor width 都是 inclusive max”的重要内部证据。 |
| flags 2 | 保存并拒绝默认解码 | 防御性合理 | 来源冲突仍未解决，本轮不应顺带处理。 |
| flags 3 行头 | `lineLength = u16le`；`payloadLength = lineLength - 2` | 正确且证据充分，但仍需实样复核 | 多个来源一致支持长度包含 2 字节头。它不是当前最可疑环节。 |
| 非零命令 | 每个非零字节输出一个 literal | 正确且证据较强 | XCC、OpenRA、ModdingWiki一致。是否存在特殊“最后一个 literal”例外没有证据。 |
| `00 count` | 输出 `count` 个 0；`00 00` 结构化拒绝 | 一般命令正确；`00 00` 防御性合理 | `count > 0` 主流一致。`00 00` 的接受语义仍未确认。 |
| `rowOutput <= WidthRaw` | literal 或 zero-run 一旦超过 width 立即失败 | 防御性合理，但可能固化未确认语义 | 防止越界和静默数据损失是正确的；但它同时假设所有 payload 输出都属于可见矩形。257/257 失败说明该假设尚未被原版证据支持。 |
| 行结束 `rowOutput == WidthRaw` | 精确相等，否则失败 | 可能固化未确认语义 | 对自定义 fixture 是清晰契约；对原版 flags 3 目前被系统性反证。不能通过删除检查来“修复”。 |
| `DataUpperBoundRelative` | 使用下一个更大的 distinct offset，否则 EOF | 防御性合理 | 适合作为硬上界和 overlap 诊断。它不是压缩长度事实，不应把 upper bound 内所有剩余字节都视为当前帧语义数据。 |
| 帧尾 padding | 完成 height 行后，将 upper bound 内剩余字节计为 padding | 防御性合理，需统计 | 可保留并报告；不能据此决定行末多出的输出。 |
| duplicate / descending offsets | 警告，不推断共享帧或依赖 | 正确且证据充分 | 避免把 offset 关系误当 reference。 |
| synthetic fixture | 行长写成 `commands.Length + 2`，descriptor width 由测试指定 | 可能共享错误假设 | fixture 和 production decoder在争议点上同源，不能充当独立 oracle。 |
| decoder tests | 正向用例均构造为恰好 width；overflow/underflow 必须失败 | 防御性测试充分，原版语义证明不足 | 证明实现按设计工作，不证明原版每行必须精确输出 width。 |
| Memory/Stream/MIX window 等价 | 比较三后端结果 | 正确且证据充分 | 证明 I/O 后端一致，不证明压缩语义正确。 |
| ProjectBaseline audit | 记录首个严格失败及聚合 | 防御性合理，但信息不足 | 已证明冲突高度稳定；还不能区分额外输出命令类型、行尾位置和输入消费状态。 |

## 3. `WestwoodShpTsReader`

### 3.1 正确部分

- 保持 family marker、canvas、frame count 和 descriptor raw 值；
- 对 frame count、canvas、descriptor、allocation 和 diagnostics 使用显式预算；
- canonical empty 与 partial empty 分开；
- 对矩形、目录内 offset、文件外 offset 做 checked 校验；
- 对 flags 2、高位 flags、reserved、alignment、duplicate 和 descending offset保留诊断；
- 不从相同 offset、FrameColor、Reserved 或目录顺序构造 dependency。

这些行为不应因本轮 row-width 冲突被削弱。

### 3.2 `DataUpperBoundRelative`

当前算法把所有非空帧 offset 去重排序，并为每帧选择第一个严格大于本帧 offset 的值作为 upper bound；没有后继则使用文件窗口末尾。

这个策略的合理用途：

- 防止任意读取越过下一个已知数据起点；
- 为重复、逆序和重叠提供诊断；
- 给 raw payload 和 RLE 行读取提供文件级硬边界。

它不能证明：

- 当前帧压缩数据一直延伸到 upper bound；
- upper bound 前的所有字节都是有效 payload；
- alignment bytes 或尾部填充属于某一行；
- 下一帧 offset 是 RLE 行终止机制。

由于本次 257 个失败都发生在 row 0，且 row 0 自带 `lineLength`，H9 只有在 row 0 的行长本身错误、offset 落点错误或样本家族误判时才成立。单纯“帧窗口过宽”不会让 row decoder多消费一个命令。

## 4. `WestwoodShpTsDecoder`

### 4.1 行输入契约

实现先读取 2 字节行长，再创建恰为 `lineLength - 2` 的行子窗口。该设计确保：

- 行命令不会静默借用下一行；
- dangling zero、truncation 和预算错误可定位到 frame/row/offset；
- 行 payload 必须被精确消费。

这是强防御边界，应保留在后续任何实验实现中。

### 4.2 输出契约

争议点不在输入推进，而在输出归属：

- 当前实现把 payload 中每个可产出索引的命令都视为可见 row pixel；
- `WidthRaw` 同时是 allocation stride、literal 上限、zero-run 上限和最终相等目标；
- 没有“guard output”“blitter-only skip”“row terminator”或“compressed span”状态。

该模型是最简洁的严格模型，但 ProjectBaseline 聚合已系统性反证其作为**当前默认兼容真值**的充分性。正确处理不是先删除 overflow 检查，而是先知道第 `width+1` 个输出来自什么，以及它在独立实现中为何未造成可见错位。

### 4.3 `00 00`

当前 decoder 消费两个字节后返回 `ZeroOutputCommandSemanticsUnresolved`。这不会死循环，也没有把它错误描述成输入无进展。当前聚合在严格失败前未看到它，但不穷尽所有行。保持结构化拒绝是合理的，直到单独审计。

### 4.4 padding

完成 descriptor height 行后，frame window 剩余字节被记为 padding。后续探针应分别统计：

- 行内 payload 在达到 width 后还剩多少命令字节；
- 完成所有行后 frame upper bound 前还剩多少字节；
- 两者不得混为同一种 padding。

## 5. `ShpTsFrameDescriptor` 与模型

模型以 raw `ushort WidthRaw/HeightRaw` 为事实，以 validated geometry 为派生结果。这是正确的可逆设计。不要在 model constructor 中做以下处理：

- `WidthRaw + 1`；
- 奇数宽度对齐；
- RLE 专用宽度覆盖；
- 按资源角色修正宽度。

若未来证据确认 compressed span 与 visible width 不同，应增加有证据标签的独立概念，而不是改写 raw 字段。

## 6. synthetic fixture 与测试闭环

fixture builder 对每行写入：

1. 2 字节总行长；
2. 由测试直接提供的 command bytes；
3. descriptor width 由测试作者指定。

production decoder随后按完全相同的规则解释。这使以下测试有效：

- checked 输入和输出边界；
- line truncation；
- command budget；
- dangling zero；
- zero count 0；
- exact-consumption；
- 不 clamp、不 padding。

但它不能独立证明：

- 原版 encoder 是否在右侧加 guard；
- `WidthRaw` 是否等于 RLE 产出 span；
- 最后一条命令是否属于可见像素；
- 原版 blitter是否裁掉行末透明输出；
- XCC/OpenRA 的宽松行为究竟是兼容规则还是 bug。

后续测试应使用两个相互独立的 fixture 层：

- production-contract fixture：继续证明当前严格模型；
- forensic raw-layout fixture：允许构造 H1–H10 的差异，不调用 production encoder，也不把任何候选语义设为默认。

## 7. 审查决定

PR #11 不应因本研究而被概括为“RLE decoder 全错”。更准确的结论是：

- 目录和 raw 路径总体可用；
- flags 3 的输入解析和安全边界大体合理；
- exact row-width 是一个清晰但尚未通过原版样本的候选契约；
- 宽松裁剪不是可接受的临时默认；
- 下一步必须是本地不可逆逐命令聚合，而不是生产代码猜测。
