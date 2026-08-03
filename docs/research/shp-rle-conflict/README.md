> **来源声明 / Source notice:** 本研究由 **ChatGPT 网页版**独立完成，**不是本地 Codex Agent 的产物**。只记录行为级研究结论；GPL 或许可证不明源码均为 reference-only，未复制、逐句翻译或机械移植。 / Independently researched by **ChatGPT Web**, **not by the local Codex Agent**. Source code is referenced only at the behavioral level and was not copied, line-translated, or mechanically ported.

# M2-R2：SHP(TS) flags 3 行宽契约冲突研究

## 1. 研究问题

PR #11 在 `RawFlags == 3` 的严格 RLE-Zero 解码中，把目录项 `WidthRaw` 作为每行必须精确产生的索引数，并把每行 `UINT16LE lineLength` 解释为**包含自身 2 字节头**。当前仓库脱敏审计显示：

- 固定样本总帧数：988；
- flags 3：510；
- canonical empty flags 3：253；
- 实际尝试的非空 flags 3：257；
- 257/257 全部在 row 0 失败；
- 每次最终输出都稳定为 `descriptor width + 1`；
- width 范围 14–202；奇数 137，偶数 120；
- 角色覆盖建筑、步兵、动画和地图增补；
- decoder 没有 clamp、padding 或 widen；
- 严格失败前观察到的 `00 00` 数量为 0，但该审计不穷尽。

这些只是当前仓库的聚合证据。本研究没有读取 ProjectBaseline、原始 SHP 行、命令字节或像素。

## 2. 当前结论

### 高可信

1. `lineLength` 包含 2 字节行头，是 OpenRA、XCC/OmniBlade、ModdingWiki 与多个社区实现之间最一致的部分。
2. PR #11 的目录、raw flags 0/1、有界输入、命令预算和“不自动裁剪”设计是合理的防御性基础。
3. PR #11 的 synthetic fixture 与 production decoder 共享“行 payload 全部是像素命令、最终输出必须恰等于 WidthRaw”的核心假设，因此现有正向测试只能证明该自定义契约内部一致，不能证明原版 SHP(TS) 语义。
4. `DataUpperBoundRelative = next distinct offset or EOF` 只能建立帧数据硬上界。row 0 的 `lineLength` 已经提供更窄的行边界，所以它不太可能单独造成跨 padding 或下一帧读取后稳定 `+1`。
5. 公开实现普遍存在宽松行为：XCC 裁短越界 zero-run；OpenRA 不核验逐行输出宽度；`cnc-formats` 会裁剪、补零并容忍截断。这些实现“能显示”不等于它们证明了格式契约。

### 尚未解决

- 第 `width + 1` 个输出究竟来自 literal 还是 zero-run；
- 它是否总是最后一条命令、是否总是索引 0；
- 达到 width 时行输入是否已经结束；
- 忽略额外输出后，行输入是否恰好结束；
- 原版编码器是否写入右侧透明保护像素；
- `WidthRaw` 是否仅对 RLE scanline 存在另一层约定；
- 原版游戏 blitter 是否有明确的行末裁剪、保护列或终止规则。

## 3. 最可能的三个解释

这些是待验证假设，不是实现建议。

| 排名 | 解释 | 对应假设 | 置信度 | 原因 |
|---:|---|---|---|---|
| 1 | 最后一条 zero-run 包含一个不进入可见矩形的透明保护输出，可能是 encoder guard 或特殊行尾约定 | H6 + H7 | 中等，约 0.50 | 能自然解释跨角色、跨奇偶宽度稳定 `+1`；XCC 恰好只对超出 `cx` 的 zero-run 裁短。必须由本地探针证明额外输出确实来自最终 zero-run且为0。 |
| 2 | 行 payload 尾部存在一个非像素终止命令/字节，或 `lineLength` 的消费域与“像素命令域”不完全相同 | H3 + H4 | 中低，约 0.25 | 稳定只多一个输出符合“每行一个尾项”，但没有强公开来源确认 SHP(TS) RLE-Zero sentinel；多数文档明确把整个 `lineLength - 2` 当 payload。 |
| 3 | RLE scanline 的有效 span 与目录 `WidthRaw` 分属不同约定，目录宽度仍描述可见矩形，而压缩行允许一个保护列 | H8（及受限 H2） | 低，约 0.15 | 能解释 raw 正常而 RLE 全部 `+1`，但公开结构没有第二个 scanline-width 字段；若宣称 WidthRaw 普遍是 inclusive bound，会与 raw 0/1 的成功和 XCC writer 行为冲突。 |

剩余概率保留给数据偏移/家族误判、命令计数偏移或尚未发现的原版 blitter 规则。

## 4. 禁止的伪修复

在决策门槛满足前，明确禁止：

- 总是把 `WidthRaw` 加 1；
- 总是丢弃每行最后一个输出；
- 输出超过 width 时直接 clamp；
- 把最后一个非零字节当 sentinel；
- 忽略 `lineLength` 的最后一个字节；
- 为 ProjectBaseline 的具体 SHA、文件或资源名特判；
- 根据建筑/步兵/动画/UI 等资源类型选择不同 decoder；
- 仅因为 XCC、OpenRA 或其他工具能显示就复制其宽松行为。

任何生产修正必须同时解释 raw、RLE、canvas、frame rectangle、输入消费和多个独立用途样本的一致性。

## 5. 文件索引

| 文件 | 内容 |
|---|---|
| [pr11-independent-review.md](pr11-independent-review.md) | PR #11 reader、decoder、model、fixture、tests 与 audit 的独立审查 |
| [source-behavior-comparison.md](source-behavior-comparison.md) | 固定公开来源、revision、许可证和行宽行为比较 |
| [row-width-contract.md](row-width-contract.md) | 行长、输出宽度、frame rectangle 与 padding 的契约边界 |
| [hypothesis-matrix.md](hypothesis-matrix.md) | H1–H10 支持、反证和本地探针需求 |
| [local-probe-request.md](local-probe-request.md) | 给本地 Codex 的脱敏逐命令聚合请求 |
| [decision-gates.md](decision-gates.md) | A–E 证据等级、允许动作和禁止动作 |
| [unresolved-questions.md](unresolved-questions.md) | 静态公开资料无法回答的问题 |

## 6. 明确非目标

- 没有实现或修改任何 C#；
- 没有修改测试；
- 没有读取 ProjectBaseline；
- 没有查看或重建原版像素；
- 没有修改兼容矩阵或第三方正式台账；
- 没有把裁剪、补零或丢弃最后一个输出作为兼容修复；
- 没有研究 VXL/HVA；
- 没有修改或合并 PR #11。
