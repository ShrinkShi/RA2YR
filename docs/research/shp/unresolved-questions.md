> **来源声明 / Source notice:** 本研究由 **ChatGPT 网页版**完成并提交，**不是本地 Codex Agent 的产物**。 / Researched and submitted from **ChatGPT Web**, **not by the local Codex Agent**.

# 未决问题

以下问题未解决前不得硬编码为兼容真值。

| ID | 问题 | 当前证据 | 所需验证 | 临时策略 |
|---|---|---|---|---|
| U-01 | 0帧是否被原版接受 | XCC拒绝；部分解析器结构上可读 | 本地扫描 + 原版行为实验 | 严格失败，研究模式诊断 |
| U-02 | X/Y究竟是 signed还是unsigned | XCC struct signed；其他来源unsigned | 黄金样本原始范围；必要时构造原版实验 | 保存raw ushort，validated int非负 |
| U-03 | flags 2的真实行为 | XCC按RLE；OpenRA特殊raw scanline；位语义矛盾 | 找到真实样本或官方/编辑器写入证据 | 不作为正常编码；严格拒绝 |
| U-04 | flags未知高位是否存在 | 公开资料只说明低2位 | 本地全库统计 | 保留raw并诊断 |
| U-05 | FrameColor第四字节含义 | 文档称前三字节RGB；XCC整体unknown | 样本分布和编辑器源码 | 原样保存，不解释第四字节 |
| U-06 | Reserved非零是否兼容扩展 | 主流称始终0 | 全库统计/工具写入实验 | warning或strict error，保留raw |
| U-07 | 8字节data alignment是否游戏要求 | 原版常对齐；文档称非必要 | 合成文件原版加载实验 | 不对齐warning，不作为解析失败 |
| U-08 | 重复offset是否允许共享帧数据 | 无强证据 | 本地统计 + 合成实验 | 检测并诊断，不自动拒绝相同可信区间 |
| U-09 | offsets逆序是否允许 | 目录长度自描述，不必理论单调 | 本地统计/原版实验 | 不按顺序解码；区间分析warning |
| U-10 | trailing bytes是否允许 | 工具可能容忍 | 原版样本尾部统计 | PreserveOpaque/AllowWithWarning |
| U-11 | zero-run count 0语义 | 无可靠证据 | 工具/原版实验 | 非法无进展控制码 |
| U-12 | 宽度>255的全透明run如何编码 | 可拆分多个run，未见原版证据 | 合成round-trip | decoder支持多个run；writer后续决定 |
| U-13 | 阴影帧组织是否对所有techno一致 | 只是常见约定 | Rules/Art角色审计 | 不在格式层自动识别 |
| U-14 | 精确玩家色remap范围 | 属于palette/runtime；不同角色可能不同 | PAL/remap研究 | SHP只输出索引 |
| U-15 | 鼠标RLE是否必然无效 | 社区文档声称原版鼠标不用RLE | 光标样本 + 原版合成实验 | 不按文件名拒绝；记录角色诊断 |
| U-16 | SHP(TS)是否存在任何delta/reference扩展 | 强来源均无字段 | 先排除家族误判，再查扩展工具 | API只预留，不实现 |
| U-17 | XCC SourceForge SVN r1201与OmniBlade commit映射 | 当前未取得稳定文件级r1201快照 | 本地/人工下载r1201并比较hash/diff | 不声称等价 |
| U-18 | OS SHP Builder可引用revision和许可证 | SVN/压缩包历史复杂 | 固定源码包hash/revision和许可文本 | reference-only |
| U-19 | CnCNet公开项目是否有独立SHP(TS)读取器 | 本轮未定位 | 组织级代码检索 | 不伪造来源覆盖 |
| U-20 | Chrono Divide实际parser行为 | 公开mod-sdk不含引擎parser | 上游公开说明或源码 | 仅记录“无公开证据” |
