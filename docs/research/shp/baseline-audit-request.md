> **来源声明 / Source notice:** 本研究由 **ChatGPT 网页版**完成并提交，**不是本地 Codex Agent 的产物**。 / Researched and submitted from **ChatGPT Web**, **not by the local Codex Agent**.

# WP-02G2 之后的本地黄金审计请求

## 1. 目的

ChatGPT 网页版无法读取本地 `ProjectBaseline`。本清单交给本地 Codex，在 WP-02G2 Rules/Art 最小资源引用视图完成后，从**实际被 Rules/Art 引用的资源**选择样本，验证本研究中的冲突点。

不得预设固定黄金文件名。样本必须由资源解析结果、MIX provenance和用途角色选择。

## 2. 样本覆盖

至少覆盖：

- 单帧；
- 多帧；
- 建筑；
- 步兵；
- 通用动画；
- UI/cameo；
- 鼠标或光标；
- 玩家色 remap；
- 阴影；
- raw flags 0/1；
- RLE flags 3；
- 若存在，flags 2或其他未知flags；
- 若存在，任何被工具称为 delta/reference 的样本；
- 不同调色盘角色：unit、iso/terrain、anim、UI、theater-specific。

每个角色至少选择一个可证明 provenance 的样本；不要因为文件名“看起来像”某角色就归类。

## 3. 审计步骤

1. 由 Rules/Art视图导出 logical image reference；
2. 由 content resolver记录选中的 MIX ID 和完整 logical provenance；
3. 仅在本地读取 entry；
4. 解析 header和24字节目录；
5. 记录 raw flags、FrameColorRaw、reserved、offset和矩形；
6. 对每帧在预算内解码，仅在内存中保留完整索引；
7. 生成不可逆统计和规范化模型 hash；
8. 比较 Memory、seekable Stream、MIX window三条路径；
9. 记录失败诊断，不为通过率而放宽；
10. 删除任何临时像素导出，不提交原版文件或图片。

## 4. 允许公开/提交的脱敏字段

每个样本最多包含：

- 逻辑名称；
- MIX ID；
- provenance（逻辑路径/容器链，不含绝对路径）；
- 原始 entry长度；
- 原始 entry SHA-256；
- 帧数；
- canvas；
- frame rectangle最小/最大范围；
- raw flags/压缩类型计数；
- reserved非零计数；
- FrameColor第四字节分布摘要；
- reference/dependency深度（通常应为0；若非0需说明证据来源）；
- 解压索引最小/最大值；
- remap索引使用统计（只给计数，不给位置）；
- shadow候选统计（只给0/1索引计数与帧段摘要）；
- 规范化模型 SHA-256；
- 诊断数量和诊断码计数；
- 三种输入后端是否等价。

## 5. 明确禁止

不得公开或提交：

- 原版像素图；
- 完整索引帧；
- 每像素坐标/值列表；
- 可还原图像的逐帧行数据；
- Base64；
- hex dump；
- 原版文件正文；
- 原版 SHP/MIX/PAL；
- 绝对路径、用户名或机器目录；
- 任何可通过统计反演轮廓的高粒度 run/scanline数据。

## 6. 冲突验证任务

本地审计必须回答：

- 是否存在 `frameCount == 0` 的实际引用资源？
- X/Y原始高位是否曾置位；按 signed解释是否产生负值？
- `RawFlags` 实际集合是什么？是否出现 2 或 >=4？
- `0x0C` 四字节的第四字节是否稳定为0？前三字节是否符合 FrameColor用途？
- `Reserved` 是否始终为0？
- data offset是否始终8字节对齐？
- offsets是否单调、重复或重叠？
- RLE行长度是否包含2字节头？
- `00 00` 是否出现？
- 行输出是否总是恰好width？
- 阴影候选是否确实位于后半且只用0/1？
- 鼠标/光标是否均为raw transparent flags 1？
- 是否有任何真实 SHP(TS)目录表现出 reference/delta字段？若“有”，先证明不是家族误判。

## 7. 结果门槛

- 不得用单个样本宣布完整格式规律；
- 每个新结论至少需要两个不同用途样本，或一个样本加第二公开实现；
- 若结果与本文冲突，先记录冲突，不修改兼容矩阵状态；
- 公开审计只提交脱敏摘要和合成复现，不提交原版证据本体。
