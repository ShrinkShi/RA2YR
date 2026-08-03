> **来源声明 / Source notice:** 本研究由 **ChatGPT 网页版**完成并提交，**不是本地 Codex Agent 的产物**。 / Researched and submitted from **ChatGPT Web**, **not by the local Codex Agent**.

# WP-02G2 之后的本地黄金审计请求

## 1. 目的

ChatGPT 网页版无法读取本地 `ProjectBaseline`。本清单交给本地 Codex，在 WP-02G2 Rules/Art 最小资源引用视图完成后，结合**实际配置来源**和**明确标记的 catalog survey**选择样本，验证本研究中的冲突点。

不得预设固定黄金文件名，也不得声称所有样本都来自 Rules/Art 或都代表运行时实际选择结果。每个样本必须记录选择依据、MIX provenance和用途角色。

## 2. 样本来源分类

### 2.1 WP-02G2 Rules/Art 资源引用

用于选择：

- 建筑；
- 步兵；
- Techno 主体动画、附属动画、damaged/buildup等由 Rules/Art 明确引用的资源。

这些样本可以声明为“由当前 Rules/Art 解析结果引用”，但仍不得把静态引用等同于已经验证的游戏运行时选中行为。

### 2.2 UI/resource 配置或明确 content catalog

用于选择：

- UI；
- cameo；
- 其他不由 Rules/Art 管理、但有实际 UI/resource 配置或明确 catalog 角色的资源。

必须记录具体的逻辑配置来源或 catalog 分类依据。

### 2.3 Mouse/UI 配置或已验证资源目录

用于选择：

- 鼠标；
- 光标。

优先使用实际 mouse/UI 配置；若只能使用已验证资源目录，必须标记为目录/catalog 证据，不能声称是运行时实际选择结果。

### 2.4 Catalog survey 补充样本

当某种 flags、压缩、调色盘角色或异常字段没有现成配置来源时，可以在已解析 content catalog 中做补充抽样。此类样本必须明确标记：

- `SelectionBasis = CatalogSurvey`；
- 只证明该资源存在于被审计内容中；
- 不证明游戏、UI或Rules/Art在运行时实际选择了它。

## 3. 样本覆盖

整体审计至少覆盖：

- 单帧；
- 多帧；
- 建筑；
- 步兵；
- Techno动画；
- UI/cameo；
- 鼠标或光标；
- 玩家色 remap候选；
- 阴影候选；
- raw flags 0/1；
- RLE flags 3；
- 若存在，flags 2或其他未知flags；
- 不同调色盘角色：unit、iso/terrain、anim、UI、theater-specific。

每个角色至少选择一个可证明 provenance 和 selection basis 的样本；不要因为文件名“看起来像”某角色就归类。

## 4. 审计步骤

1. 从对应来源导出 logical image reference 或 catalog candidate：Rules/Art、UI/resource、mouse/UI、verified directory/catalog survey；
2. 记录 `SelectionBasis`，并明确它是否是配置引用还是仅 catalog survey；
3. 由 content resolver记录选中的 MIX ID 和完整 logical provenance；
4. 仅在本地读取 entry；
5. 解析 header和24字节目录；
6. 记录 raw flags、FrameColorRaw、reserved、offset和矩形；
7. 验证 parser 没有从 FrameColor、Reserved、offset或帧顺序发明dependency；
8. 对每帧在预算内解码，仅在内存中保留完整索引；
9. 生成不可逆统计和规范化模型 hash；
10. 比较 Memory、seekable Stream、MIX window三条路径；
11. 记录失败诊断，不为通过率而放宽；
12. 删除任何临时像素导出，不提交原版文件或图片。

## 5. 允许公开/提交的脱敏字段

每个样本最多包含：

- 逻辑名称；
- selection basis分类；
- 是否为配置引用或catalog survey；
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
- parser invented dependency计数，预期必须为0；
- 解压索引最小/最大值；
- remap索引使用统计（只给计数，不给位置）；
- shadow候选统计（只给0/1索引计数与帧段摘要）；
- 规范化模型 SHA-256；
- 诊断数量和诊断码计数；
- 三种输入后端是否等价。

## 6. 明确禁止

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

## 7. 冲突验证任务

本地审计必须回答：

- 是否存在 `frameCount == 0` 的实际配置引用或catalog候选？两类结果必须分开报告；
- X/Y原始高位是否曾置位；按 signed解释是否产生负值？
- `RawFlags` 实际集合是什么？是否出现 2 或 >=4？
- 若出现flags 2，其selection basis是什么，字节流更符合XCC RLE-Zero还是OpenRA length-prefixed raw scanlines，或两者都不符合？
- `0x0C` 四字节的第四字节是否稳定为0？前三字节是否符合 FrameColor用途？
- `Reserved` 是否始终为0？
- data offset是否始终8字节对齐？
- offsets是否单调、重复或重叠？
- RLE行长度是否包含2字节头？
- `00 00` 是否出现？若出现，只报告数量、用途类别和严格解析结果，不公开行数据；
- 行输入是否精确消费，行输出是否总是恰好width？
- 阴影候选是否确实位于后半且只用0/1？
- 鼠标/光标样本来自实际mouse/UI配置还是目录/catalog survey？
- parser是否在任何样本中发明了dependency？预期答案必须为否；若出现，先视为实现缺陷或家族误判。

## 8. 结果门槛

- 不得用单个样本宣布完整格式规律；
- 不得把 catalog survey 宣称为运行时实际选择结果；
- 每个新结论至少需要两个不同用途样本，或一个样本加第二公开实现；
- 若结果与本文冲突，先记录冲突，不修改兼容矩阵状态；
- 公开审计只提交脱敏摘要和合成复现，不提交原版证据本体。
