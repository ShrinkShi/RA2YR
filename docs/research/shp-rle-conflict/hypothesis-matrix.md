> **来源声明 / Source notice:** 本研究由 **ChatGPT 网页版**独立完成，**不是本地 Codex Agent 的产物**。矩阵只定义待证伪假设，不选择 production 行为。 / Independently researched by **ChatGPT Web**, **not by the local Codex Agent**. This matrix defines falsifiable hypotheses and does not select production behavior.

# H1–H10 假设矩阵

## 1. 判定符号

- **是**：该假设自然产生观测；
- **部分**：需额外条件才能产生观测；
- **弱/否**：与观测或其他已知路径冲突；
- **未公开**：没有可定位公开实现明确支持；
- **必须探针**：只能依靠本地不可逆逐命令聚合区分。

## 2. 矩阵

| ID | 假设 | 能解释所有样本稳定 +1 | 与奇偶宽度无关 | 公开实现支持 | 影响raw 0/1 | 需要本地逐命令聚合 | 关键证伪结果 |
|---|---|---|---|---|---|---|---|
| H1 | descriptor width就是实际输出宽度；当前decoder对某个命令解释错误 | 是 | 是 | 通用结构文档支持width为frame宽，但没有指出哪条命令错误 | 若修正仅限RLE命令，不影响raw | 必须 | 额外输出命令在标准literal/zero-run语义下无任何特殊位置或模式；替代命令解释导致其他行不等width。 |
| H2 | descriptor width是最大X或inclusive bound，实际像素数是width+1 | 是 | 是 | 无强支持；大多数来源称width/cx且writer使用cx个输入像素 | 会；raw也应需要+1，和当前raw成功冲突 | 需要，但静态证据已明显反对 | raw payload长度、canvas rectangle和独立writer均严格为width；或extra仅来自最终zero-run。 |
| H3 | 每行最后有一个不属于可见像素的terminal literal/sentinel | 是 | 是 | 未公开；OpenRA/XCC/ModdingWiki均未定义SHP(TS) sentinel | 不影响raw | 必须 | `width+1`输出不是最后命令、命令类型不稳定、达到width后还有多个命令，或最后输出来自zero-run。 |
| H4 | lineLength范围解释错误，当前多读取一个命令 | 是，若恰多读一个产出命令 | 是 | 强来源反对“长度不含头”；但可能存在尾字节不属于命令域的特殊约定 | 不影响raw | 必须 | 用含头解释时输入恰好到行尾且额外输出由同一合法命令产生；排除头解释会跨到下一行或造成多种非+1结果。 |
| H5 | `00 count`语义是count-1、count+1或其他偏移 | 部分；若所有行只有一个相关run可稳定+1 | 是 | 主流实现一致使用count；无一般offset支持 | 不影响raw | 必须 | extra来自literal；或行内多个zero-run按offset解释会产生多次漂移；XCC writer round-trip使用精确count。 |
| H6 | 最后一条zero-run有特殊终止语义 | 是 | 是 | XCC对越界zero-run裁到cx提供行为支持，但没有规范说明 | 不影响raw | 必须 | extra不是zero-run、不是最后命令、最后run没有越界，或不同样本需不同规则。 |
| H7 | 原版encoder固定写右侧透明保护像素，renderer忽略 | 是 | 是 | XCC decoder兼容该形状；OpenRA宽松缓冲也可能吸收；XCC writer本身不写guard | 不影响raw，若只用于RLE encoder | 必须 | 第width+1输出非0；不是最后输出；某些原版行没有guard而仍需相同规则；原版A/B显示guard可见。 |
| H8 | frame visible width与compressed scanline width分属不同字段或约定 | 是 | 是 | OpenRA将dataWidth与descriptor width分开但原因是偶数对齐；没有第二个on-disk字段 | 不必影响raw | 必须 | compressed span没有稳定可推导关系；extra只是一条特定zero-run；canvas/offset实验不支持独立span。 |
| H9 | DataUpperBound/next offset使decoder读入padding或下一数据区 | 弱；row 0有自身lineLength，单纯frame upper过宽不够 | 是 | 没有来源支持用next offset定义行长度 | raw也可能受frame padding统计影响，但不改raw面积 | 需要offset/lineLength聚合，不需像素 | 达到width+1时仍在row0声明payload内且rowEnd小于next offset；257帧均无跨界。 |
| H10 | synthetic fixture和production decoder共享同一错误假设 | 能解释测试为何全绿，不能单独解释原版+1 | 是 | PR #11静态审查直接支持 | 不直接影响raw | 不需要原始样本即可确认测试局限；修正语义仍需探针 | 独立fixture/oracle按不同候选语义验证production且得到原版证据；现有fixture不再是唯一oracle。 |

## 3. 各假设详细判断

### H1：某个命令解释错误

**支持：** raw成功、RLE系统性失败，说明差异可能局限在命令层。  
**反对：** literal和`00 count`机械语义在公开实现间高度一致。  
**最小探针：** 统计第`width+1`输出的命令类型、命令位置、索引0与否。  
**允许结论：** 只有发现稳定、可复现的特殊命令类别，才能修改其语义。

### H2：inclusive width

**支持：** 数值上最简单解释`+1`。  
**反对：** raw 0/1按width面积工作；XCC encoder以cx个输入像素结束；格式文档称width而非maxX。  
**当前等级：** 低。禁止直接`WidthRaw + 1`。

### H3：terminal literal/sentinel

**支持：** 每行固定一个尾项可解释稳定+1。  
**反对：** RLE-Zero已有lineLength终止，不需要sentinel；无独立来源描述。  
**证伪优先：** extra若来自zero-run，H3立即大幅降级。

### H4：lineLength域错误

**支持：** 若最后一个payload字节不是像素命令，当前完整消费会多输出。  
**反对：** XCC/OpenRA/ModdingWiki均把`lineLength-2`作为命令区。  
**探针：** 同一行分别按“含头”“不含头”“排除最后1字节”只做长度分类，禁止输出像素；比较是否跨下一行和最终输入位置。

### H5：count偏移

**支持：** extra若总来自zero-run且count只多1。  
**反对：** 一般count-1会影响每个zero-run，不应只在行末稳定+1；XCC/OpenRA writer/reader主流都用精确count。  
**探针：** 统计行内zero-run数量、extra run是否最后、开始前rowOutput与count。

### H6：final zero-run特殊

**支持：** XCC明确裁短越过cx的zero-run；只影响行尾，能保持中间坐标不漂移。  
**反对：** 没有公开规范称最后run特殊。  
**探针：** extra必须100%来自最后zero-run，且忽略超出1后输入精确结束。

### H7：右侧透明guard

**支持：** 角色和奇偶无关、稳定+1；额外值若为0非常吻合；旧blitter可能为快速绘制保留保护列。  
**反对：** XCC encode3不生成guard；没有原版encoder源码。  
**探针：** extra必须为0、最后输出、所有行/多个角色一致；后续需要原版A/B或第二独立来源。

### H8：双width约定

**支持：** 可解释raw和RLE不同；OpenRA存在dataWidth适配概念。  
**反对：** on-disk descriptor没有第二width；OpenRA适配只为偶数偏移，偶数样本仍失败。  
**探针：** 检查RLE produced span是否始终width+1而不依赖最后命令类型，并验证canvas写入不扩大可见rect。

### H9：frame bound问题

**支持：** next offset/padding是未确认边界。  
**反对：** row0由lineLength自限；所有样本相同+1不符合随机padding/offset错误。  
**探针：** 仅记录rowEnd到nextOffset距离范围、是否跨界，不公开offset或原始数据。

### H10：共享fixture假设

**支持：** fixture写`commands+2`且由作者指定width；decoder按同一方式验证。  
**影响：** 说明测试覆盖不足，不说明应采用哪种原版语义。  
**下一步：** forensic fixture必须能表达H3/H4/H6/H7/H8，并由数据表驱动预期分类，而不是复用production encoder。

## 4. 当前优先级

1. H6/H7：最高探针优先级；
2. H3/H4：第二优先级；
3. H8：第三优先级；
4. H1：作为上位集合保留；
5. H5：仅当extra来自zero-run后再细分；
6. H2/H9：当前低概率但必须有明确证伪字段；
7. H10：已确认的测试独立性问题，不是格式结论。
