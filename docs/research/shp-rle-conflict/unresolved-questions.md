> **来源声明 / Source notice:** 本研究由 **ChatGPT 网页版**独立完成，**不是本地 Codex Agent 的产物**。下列问题在没有新证据前不得硬编码为兼容真值。 / Independently researched by **ChatGPT Web**, **not by the local Codex Agent**. These questions must not be hard-coded as compatibility truth without new evidence.

# 未决问题

| ID | 问题 | 当前证据 | 为什么公开资料不足 | 下一步 |
|---|---|---|---|---|
| U-01 | 第`WidthRaw+1`个输出来自literal还是zero-run？ | 当前只有最终输出长度聚合 | PR #11现有审计在overflow即停止，没有输出来源聚合 | 执行本地脱敏逐命令probe |
| U-02 | extra输出是否始终索引0？ | 未知 | 没有公开原始行，也不得公开像素 | 只公开true/false计数 |
| U-03 | extra是否始终由最后命令产生？ | 未知 | 宽松实现不报告命令位置 | 聚合最后命令类别与extra来源 |
| U-04 | 忽略extra后输入是否恰好结束？ | 未知 | “能显示”实现通常不验证row input/output双终止 | 聚合exact-end count |
| U-05 | 所有row还是只有row0具有`+1`？ | 当前严格decoder全部在row0先失败 | 后续row未被穷尽 | forensic analyzer不生成图像，只统计所有行；公开行号应按“row0/other”聚合 |
| U-06 | final zero-run是否有特殊语义？ | XCC只对zero-run越界裁短 | XCC行为可能是兼容防护或bug，不是规范 | 先证明extra来源，再找第二来源/原版A/B |
| U-07 | 原版encoder是否写右侧透明guard？ | 稳定+1与XCC裁zero-run相容 | 没有TS/RA2游戏encoder源码；XCC writer不写guard | 本地命令聚合 + 后续独立encoder样本或原版实验 |
| U-08 | descriptor width是否为inclusive bound？ | 数值上可解释+1 | raw 0/1成功和XCC writer明显反对普遍inclusive解释 | 对raw长度、RLE span、canvas矩形做脱敏交叉统计 |
| U-09 | RLE compressed span是否独立于visible width？ | OpenRA有dataWidth适配，但只因偶数对齐 | on-disk没有第二width字段；偶数width同样失败 | 检查span与命令类型是否稳定 |
| U-10 | lineLength是否包含2字节头？ | 多来源高度一致为“是” | ProjectBaseline冲突可能诱发错误怀疑，但尚无反证 | probe同时分类两种解释，预期“不含头”应跨界/失配 |
| U-11 | payload最后一个字节是否是非像素尾项？ | 无直接证据 | 所有主流实现都把完整payload当命令区 | 统计达到width时剩余字节和最后命令结构，不公开值 |
| U-12 | `00 count`是否存在一般count偏移？ | 主流实现都使用精确count | 只有extra来源和run位置能区分一般偏移与final特殊 | 聚合每行zero-run数、final run count、开始前输出 |
| U-13 | `00 00`是否在后续行出现？ | 严格失败前观察为0但不穷尽 | row0先失败阻止完整扫描 | forensic analyzer统计所有行，只公开总count |
| U-14 | next distinct offset/EOF是否错误界定frame数据？ | 作为硬上界合理 | 不能说明行内容；row0有自己的lineLength | 聚合rowEnd与upper bound关系、是否跨界 |
| U-15 | frame尾部剩余字节是alignment还是隐藏数据？ | PR #11计为padding | 没有独立长度字段；offset可能重复/逆序 | 分开报告完成height行后的剩余长度分布，不发布内容 |
| U-16 | OpenRA奇数补偶数是否有原版依据？ | 注释称避免half-integer offset | 这是OpenRA渲染模型，不是Westwood格式证明 | 不用于production contract；仅做行为比较 |
| U-17 | XCC zero-run裁短是原版规则还是防御补丁？ | 固定源码明确存在 | 无注释或原版源码说明动机 | 本地extra来源 + 第二独立证据 |
| U-18 | XCC literal overflow会怎样？ | 固定代码没有对等zero-run裁短 | 可能跨行或越界；不能运行本轮未授权工具/原版 | 不复制；探针若发现literal extra则XCC证据降级 |
| U-19 | OS SHP Builder末期revision如何解码row end？ | SVN与revision历史可定位 | 本轮公开网页未稳定返回rev85文件正文；许可证也不是标准OSI文本 | 后续人工固定SVN导出hash与具体procedure，reference-only |
| U-20 | XCC SourceForge r1201与OmniBlade commit是否等价？ | 同谱系、同名文件 | 未取得可复现r1201文件级快照 | 不声称等价；以OmniBlade commit为行为pin |
| U-21 | EA FinalSun/FinalAlert 2是否有独立于XCC的row blitter？ | 官方编辑器打包并调用XCC类 | 当前定位内容主要是bundled工具库，不是独立算法 | 若后续发现独立绘制路径，单独固定并比较 |
| U-22 | Chrono Divide的实际SHP parser行为是什么？ | 公共mod-sdk确认消费RA2资源 | 公共org未定位engine parser源码或source map | 请求上游公开代码/说明；未取得前留空 |
| U-23 | ModdingWiki严格示例为何与257/257样本冲突？ | 示例在达到行宽前payload未结束时报错 | 示例可能未以这些原版资源回归，且不是官方规范 | 用probe确定extra命令，再向文档维护者提交可复现非版权统计 |
| U-24 | ProjectBaseline固定样本是否混入工具生成扩展？ | 角色广且含地图增补 | 聚合不暴露谱系；不同容器可能来自不同发行包 | 按selection basis和容器类别做低粒度聚合，不公开文件名 |
| U-25 | 原版renderer是否忽略右边界透明输出？ | XCC/OpenRA宽松行为间接相容 | 没有原版游戏blitter源码或已授权A/B观察 | 只有用户另行授权后，在仓库外一次性副本做黑盒A/B |
| U-26 | writer应采用什么规则？ | 尚无production decode结论 | reader兼容行为不等于canonical writer | 本轮明确不实现writer；达到A门槛后另做writer研究 |

## 不能靠公开资料解决的核心

1. 原版Westwood encoder对每行最后一个transparent run的真实写法；
2. RA2/YR运行时blitter对`cx`边界、末尾zero-run和保护列的实际处理；
3. ProjectBaseline第`width+1`输出的命令类型与行尾位置；
4. 257个失败帧后续行是否保持相同模式；
5. 不同发行/地图增补资源是否来自同一工具链；
6. Chrono Divide闭源/未公开parser的具体容忍行为；
7. OS SHP Builder固定revision的可复现源文件内容与许可边界。

## 保持未决时的默认状态

- production decoder继续strict失败并输出可定位diagnostic；
- 不clamp、不drop-last、不补零；
- 不改变WidthRaw或frame rectangle；
- 不实现writer；
- ProjectBaseline flags3 compatibility保持未实现；
- 不进入VXL/HVA研究以分散当前主路径。
