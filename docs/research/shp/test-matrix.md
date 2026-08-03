> **来源声明 / Source notice:** 本研究由 **ChatGPT 网页版**完成并提交，**不是本地 Codex Agent 的产物**。 / Researched and submitted from **ChatGPT Web**, **not by the local Codex Agent**.

# SHP(TS) 合成测试矩阵

本矩阵包含 **51 个首版 SHP(TS) 正式测试**。另有 **6 个 deferred 项**置于附录，不计入首版测试数。所有测试样本必须自主生成；不得包含原版像素或从原版逐帧变形得到的可还原数据。

状态规则：

- “已确认格式要求”决定当前正向样本；
- “防御性检查”可以在证据未完全收敛时先实现；
- 最后一列中的内容禁止为了测试方便而变成默认格式事实；
- 首版没有 dependency 模型或 resolver；唯一正式依赖相关测试 `DEP-01` 只证明 parser 不会发明依赖；
- deferred appendix 属于未来扩展或其他 SHP 家族研究，不得计入首版覆盖率。

## 首版正式测试：51 项

| ID | 场景 | 已确认格式要求 | 防御性检查 | 尚未确认、不得提前编码的假设 |
|---|---|---|---|---|
| HDR-01 | 最小候选 SHP | 8字节头+至少1个24字节空目录项；首u16=0 | checked目录尺寸；canonical empty | “原版接受最小空帧文件”未确认 |
| HDR-02 | 0帧 | 布局可表达frameCount=0 | 结构化失败、无分配 | 不得标记合法 |
| HDR-03 | 单帧 raw | flags0/1，area字节 | offset/area边界 | 透明绘制不在decoder |
| HDR-04 | 多帧 | 每帧独立descriptor | 逐项预算和独立解码 | 不从首帧推全局压缩 |
| HDR-05 | canonical空帧 | w=h=offset=0由XCC接受 | 返回空local frame | x/y是否必须0未确认 |
| HDR-06 | partial空帧 | 无一致合法证据 | 拒绝w=0 xor h=0等组合 | 不得自动修复 |
| HDR-07 | canvas与局部矩形 | 局部rect写入全局canvas | checked x+w/y+h | pivot不在格式层 |
| HDR-08 | 边缘坐标 | rect末端可等于canvas | 半开区间校验 | signed解释未确认 |
| HDR-09 | 家族marker非0 | 非SHP(TS)候选 | 明确family diagnostic | 不得尝试TD decoder |
| HDR-10 | 目录截断 | 目录固定24*count | checked乘加和EOF | 不得降低frameCount |
| OFF-01 | data offset越界 | offset必须位于窗口或canonical empty | 子窗口创建失败 | 不得借到外部stream |
| OFF-02 | offset+raw length溢出 | raw长度=area | checked uint/long算术 | 不得wrap |
| OFF-03 | 帧超出canvas | rect应位于canvas | 拒绝而非裁剪 | 负offset解释待审计 |
| OFF-04 | 重复offset | 格式未明确禁止 | 检测并报告 | 不得自动认定共享合法 |
| OFF-05 | 重叠帧区间 | 各帧消费区间应可分析 | 实际消费跨next distinct offset诊断 | 允许性未确认 |
| OFF-06 | 逆序offset | 无单调性强证据 | 按绝对offset访问，不顺序假设 | 是否兼容待审计 |
| OFF-07 | 异常尾部 | 尾部策略由format决定 | PreserveOpaque/Warning | 不得静默吞掉 |
| OFF-08 | 非8字节对齐offset | 常见原版对齐但非解码必要 | warning统计 | 不得先硬拒绝 |
| CMP-01 | flags 0 raw opaque | area原始字节 | 精确消费 | 0是否显示为颜色属renderer |
| CMP-02 | flags 1 raw transparent | area原始字节 | 精确消费 | decoder不改索引 |
| CMP-03 | flags 3 RLE-Zero | 逐行长度+00 run | 严格行/像素边界 | 已确认 |
| CMP-04 | flags 2来源冲突 | 位模型可表达`UsesRle=true`, `HasTransparency=false` | 保留raw并返回source-conflicting/underconfirmed；默认不解码 | 不得提前选择OpenRA length-prefixed raw或XCC RLE-Zero |
| CMP-05 | unknown flags >=4 | 未确认 | 保留raw、诊断、拒绝解码 | 不得mask后继续 |
| CMP-06 | 同文件混合raw/RLE | per-frame flags | 独立dispatch | 已确认结构允许 |
| RLE-01 | zero-run | 00,count输出count个0 | 需要count字节且不越行 | count0语义未确认 |
| RLE-02 | 连续literal序列 | 每个非0字节输出1像素 | 不创建literal-run控制码 | “literal-run”仅描述序列 |
| RLE-03 | 全透明行 | 一个或多个zero-run | 输出恰为width | width>255需分run |
| RLE-04 | 空行/宽0 | 只应随空帧处理 | 不进入普通row decoder | 非零高+宽0非法 |
| RLE-05 | scanline精确结束 | lineLen含2字节头 | 输入和输出同时到界 | 不得读下一行补足 |
| RLE-06 | lineLen<2 | 无合法payload | 立即失败 | 不得saturating_sub |
| RLE-07 | 行数据截断 | 声明end越窗口 | 失败并定位row | 不得补透明 |
| RLE-08 | 帧数据截断 | 不足height行 | 失败 | 不得返回部分成功 |
| RLE-09 | 解压像素不足 | 行payload结束但输出<width | 失败 | 不得padding |
| RLE-10 | 解压像素过多 | run/literal越过width | 失败 | 不得clamp |
| RLE-11 | 悬空00控制 | 00后无count | 失败 | 不得break后padding |
| RLE-12 | `00 00` zero-output command | 消费2个输入字节、输出0像素 | 命令预算；输入推进；行输入精确消费；最终输出必须等于width | no-op、padding或非法命令；严格模式是否接受未确认 |
| RLE-13 | 超长lineLen | u16但受单行预算 | 预算失败 | 不得分配lineLen缓冲 |
| RLE-14 | 尾随行payload | 输出已满但声明仍有字节 | 失败/专用诊断 | 不得忽略隐藏控制 |
| DEP-01 | parser never invents a dependency | SHP(TS)目录无已确认reference/delta字段 | 断言descriptor/document不产生dependency；FrameColor、Reserved、offset仅保留其本字段语义 | 不得实现TD/RA1语义或从原始字段猜引用 |
| IO-01 | Memory/Stream/MIX window等价 | 同字节同结果/诊断offset | canonical model hash一致 | 不包含绝对路径 |
| BUD-01 | 输入预算 | 现有BinaryReadLimits模式 | 早期失败 | 默认值需实现阶段决定 |
| BUD-02 | 帧数预算 | count可达u16但不可盲分配 | reserve records | 阈值需基准 |
| BUD-03 | canvas/像素预算 | 面积checked | max canvas/local/total pixels | 不以ushort上限直接分配 |
| BUD-04 | 单行/命令预算 | lineLen与命令数独立受限 | stream decode或有界读取；`00 00`仍计一个命令 | 阈值需基准 |
| BUD-05 | 损坏输入有界终止与分配 | 任何命令消费输入；所有文件驱动分配先验证 | 无死循环、无无界分配、预算触发可定位诊断 | 不得以补零或截断成功掩盖损坏 |
| ARC-01 | Core无UnityEngine | 格式/索引/PAL分层 | assembly/reference测试 | pivot/Texture在adapter |
| LEG-01 | 公开证据不含原版像素 | 合成数据与不可逆统计 | 仓库扫描 | 必需 |
| PIX-01 | 透明索引0 | flags与RLE以0为核心 | 保留索引和透明policy分离 | flags0时绘制语义不同 |
| PIX-02 | PAL缺失 | SHP不内嵌PAL | 无palette也可解析索引 | 不得默认unittem.pal |
| PIX-03 | remap统计 | remap属外部服务 | 仅统计索引命中数 | 精确range未确认 |
| PIX-04 | shadow候选 | 常见后半0/1但无元数据 | 不自动合并；角色层验证 | 偶数帧不充分 |

## Deferred appendix：6 项，不计入首版测试数

这些项目不属于首版 SHP(TS) 实现。只有未来发现有许可证和样本支持的 SHP(TS) 扩展，或另行研究 TD/RA1 等其他 SHP 家族时，才可重新立项。

| ID | Deferred 场景 | 所属范围 | 当前处理 |
|---|---|---|---|
| DEP-02 | 前向引用 | 未来扩展或其他 SHP 家族 | 不建立resolver；不计入首版 |
| DEP-03 | 自引用 | 未来扩展或其他 SHP 家族 | 不建立resolver；不计入首版 |
| DEP-04 | 循环引用 | 未来扩展或其他 SHP 家族 | 不建立visited-set逻辑；不计入首版 |
| DEP-05 | 不存在的参考帧 | 未来扩展或其他 SHP 家族 | 不建立reference index模型；不计入首版 |
| DEP-06 | delta深度预算 | 未来扩展或其他 SHP 家族 | 首版`ShpReadLimits`不含该预算 |
| DEP-07 | 累计依赖预算 | 未来扩展或其他 SHP 家族 | 首版`ShpReadLimits`不含该预算 |

## 生成器约束

合成 fixture builder 应提供原始字段级控制：

- header raw values；
- descriptor raw x/y/w/h/flags/frameColor/reserved/offset；
- raw payload；
- RLE逐行payload与声明长度；
- padding/tail；
- 手工重叠和错误offset。

builder 必须与 production reader分离，避免用同一实现同时制造和验证错误。

## 断言层级

每项首版测试至少断言：

1. success/failure；
2. diagnostic code；
3. frame/row index；
4. absolute offset；
5. bytes consumed；
6. allocation/record/window/command预算；
7. normalized model hash（成功时）；
8. Memory/Stream/MIX window等价（适用时）。
