> **来源声明 / Source notice:** 本研究由 **ChatGPT 网页版**独立完成，**不是本地 Codex Agent 的产物**。本文件定义证据门槛，不授权自动修改 production 或兼容状态。 / Independently researched by **ChatGPT Web**, **not by the local Codex Agent**. This file defines evidence gates and does not authorize automatic production or compatibility changes.

# 决策门槛

## 1. 总原则

- 安全失败优先于静默裁剪；
- 聚合一致性不等于格式规范；
- 单一工具可显示不等于原版运行时证明；
- production默认必须能够解释输入消费、输出span、visible rectangle、raw路径和多个用途样本；
- 任何行为提升都应在独立实现PR中完成，本研究PR不改代码和兼容矩阵。

## 2. A–E 等级

### A. 多来源 + 本地多个角色样本一致

**条件：**

- 至少两个相互独立、可固定revision的实现或一个公开实现加原版可复现A/B行为；
- 本地建筑、步兵、动画和地图增补多个角色样本产生同一命令级模式；
- 该模式解释奇数和偶数width；
- raw flags 0/1不受破坏；
- row输入精确结束；
- visible rectangle和canvas写入有明确边界；
- 不依赖文件名、SHA或角色特判。

**允许动作：**

- 在单独production PR中修正decoder；
- 添加独立forensic fixture和黄金聚合回归；
- 运行完整门禁；
- 仅在实现和脱敏黄金审计均通过后，另行评估兼容矩阵。

**示例：** 若257/257 extra均是最后zero-run产生的单个透明输出，忽略该透明guard后输入精确结束，并有第二独立来源或原版实验确认该guard不属于visible rectangle，可设计严格`ValidatedTrailingTransparentGuard`规则。它不能实现成任意clamp。

### B. 单一公开实现 + 本地样本一致

**条件：**

- 一个固定公开实现表现出明确候选行为；
- 本地聚合高度一致；
- 尚无第二独立来源或原版行为确认。

**允许动作：**

- 只添加显式、默认关闭的实验策略；
- 策略名称必须描述证据状态，不得叫“Original”或“Compatible”；
- 只在合成fixture和受控本地审计中启用；
- 保持默认strict路径和ProjectBaseline flags3兼容状态未实现。

**禁止：**

- 直接复制XCC/OpenRA代码；
- 默认启用；
- 写回文件；
- 提升兼容矩阵。

### C. 仅能通过裁剪适配

**表现：**

- 没有稳定命令类型；
- extra可能是literal或zero-run；
- 输入结束状态不一致；
- 只有“输出多了就砍到width”能通过。

**决定：** 不接受。

- 继续保持未决；
- 不把clamp称为兼容；
- 不允许padding、drop-last或resize掩盖；
- 扩大样本分类或寻找原版行为证据。

### D. 不同样本需要不同语义

**表现：**

- 角色、来源容器、flags高位、尺寸段或工具谱系出现稳定分裂；
- 一类需要final zero-run guard，另一类需要literal sentinel或不同lineLength；
- 同一RawFlags==3下存在互斥契约。

**决定：** 先调查：

- 格式家族误判；
- flags高位被忽略；
- flags 2/3混淆；
- 工具生成的非原版扩展；
- catalog样本分类错误；
- damaged/empty/bogus frame特殊结构。

不得按建筑/步兵/动画直接选择decoder，也不得把角色当二进制格式字段。

### E. 证据仍不足

**表现：**

- 聚合不能区分H3/H4/H6/H7/H8；
- 公开实现互相矛盾；
- Chrono Divide/原版运行时行为不可定位；
- 样本只证明strict失败，未证明正确替代。

**决定：**

- 保持 flags3 ProjectBaseline compatibility 未实现；
- 保持strict diagnostic；
- 保留研究文档和probe计划；
- 不修改production、writer、matrix或第三方台账。

## 3. 伪修复拒绝表

| 建议 | 决定 | 原因 |
|---|---|---|
| `WidthRaw + 1` | 拒绝 | 会改变raw/canvas语义，H2缺乏支持。 |
| 丢弃每行最后一个输出 | 拒绝 | 未知它是literal、zero-run、visible pixel还是guard。 |
| 输出超过width直接clamp | 拒绝 | 将损坏、误解和真实guard混为一谈。 |
| 最后非零字节当sentinel | 拒绝 | 无公开证据，可能删除真实颜色。 |
| 忽略lineLength最后一字节 | 拒绝 | 破坏输入精确消费且可能把命令拆开。 |
| 对固定SHA/文件名特判 | 拒绝 | 不可泛化且污染格式层。 |
| 按资源角色选择decoder | 拒绝 | 角色不是on-disk格式字段。 |
| 复制XCC裁剪 | 拒绝 | XCC只裁zero-run且自身不验证literal边界；GPL reference-only。 |
| 使用OpenRA偶数padding | 拒绝 | 偶数width样本同样失败；这是渲染适配。 |
| 使用cnc-formats补零/裁剪 | 拒绝 | 其compression映射与强来源冲突，且静默正常化。 |

## 4. Production修正前必答问题

1. extra输出命令类型是什么？
2. extra是否始终索引0？
3. extra是否始终最后输出、最后命令？
4. 忽略extra后输入是否恰好到rowEnd？
5. 中间zero-run是否完全按count工作？
6. 每一行还是仅row0存在相同模式？
7. raw路径为何不需要同样修正？
8. visible rectangle是否保持WidthRaw？
9. writer应否生成相同结构？如果不确定，writer继续不实现。
10. 两个独立证据来源是什么？

任一关键问题未答，不达到A门槛。

## 5. 兼容状态边界

本研究只允许：

- 记录`flags3-row-width-contract`冲突；
- 设计probe；
- 分类公开实现；
- 保持strict失败可诊断。

本研究不允许：

- 修改`docs/compatibility/matrix.yml`；
- 宣称ProjectBaseline flags3可执行；
- 宣称SHP(TS)完整支持；
- 实现writer；
- 进入VXL/HVA研究。
