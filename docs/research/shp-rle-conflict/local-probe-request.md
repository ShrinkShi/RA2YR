> **来源声明 / Source notice:** 本请求由 **ChatGPT 网页版**设计，交由本地 **Codex Agent** 在用户授权的本地环境执行；ChatGPT Web 不读取 ProjectBaseline。 / Designed by **ChatGPT Web** for execution by the local **Codex Agent** in the user-authorized local environment. ChatGPT Web does not read ProjectBaseline.

# 本地脱敏探针请求

## 1. 目的

在不公开原始 SHP、行字节、命令序列或像素的前提下，回答：

1. 第 `WidthRaw + 1` 个输出由哪类命令产生；
2. 它是否为行末、是否为0、是否属于最后zero-run；
3. 达到 `WidthRaw` 时输入是否已经结束；
4. 按不同公开实现语义，输出长度和输入消费如何分类；
5. 哪些H1–H10可以由聚合直接证伪。

该探针不是生产decoder修改，不得改变兼容矩阵状态。

## 2. 执行边界

- 只在仓库外或现有受控 ProjectBaseline 只读入口中运行；
- 不修改 ProjectBaseline；
- 不启动原版游戏、FinalAlert 2、XCC GUI或其他未经授权程序；
- 不导出图片、indexed frame、scanline、hex dump、Base64或逐命令日志；
- 内存中的原始命令只用于即时计数，完成后释放；
- 仓库提交只能包含聚合计数、范围、分类和不可逆hash；
- 不把资源逻辑名与逐帧结果一一对应；只按用途类别聚合；
- 建筑、步兵、动画、地图增补至少分别报告聚合，但类别过小时应合并以避免指纹化；
- 不根据文件名、角色或SHA改变decoder语义。

## 3. 样本集合

沿用PR #11固定样本选择和provenance，但本轮只对：

- `RawFlags == 3`；
- 非canonical empty；
- 当前严格decoder在row 0产生`RleOutputOverflow`；

进行forensic replay。

必须先验证总体聚合仍为：

- 候选257帧；
- width 14–202；
- 奇数137、偶数120；
- 当前标准机械语义下row0最终输出全部为width+1。

若这些前置聚合变化，停止并报告基线漂移，不继续推导。

## 4. 内存内逐行分析器

探针使用独立于production decoder的只读分析器。它可以读取原始行，但不得返回命令或像素数组。每行只累计标量：

- 输入命令数；
- literal数；
- zero-run数；
- 机械输出长度；
- 达到width的输入位置类别；
- 额外输出来源类别；
- 最后命令类别；
- 输入是否精确结束。

不得调用production encoder或synthetic fixture builder来构造预期结果。

## 5. 每个失败帧 row 0 的内部字段

以下字段只在内存内逐帧产生；公开时必须聚合：

- descriptor width；
- row 0 `lineLength`；
- row 0 command count；
- row 0 literal command count；
- row 0 zero-run command count；
- 首次达到width时剩余command bytes数；
- 第width+1个输出来自 `Literal` / `ZeroRun` / `None` / `Malformed`；
- 若来自zero-run：该run count与开始前rowOutput；
- 达到width时输入是否已到行尾；
- 最后一个命令类型；
- 最后命令消费字节数（1或2）；
- 最后一个输出索引是否为0；
- 忽略最后一个输出后输入是否精确结束；
- standard mechanical语义输出长度；
- OpenRA-style完整payload语义输出长度；
- XCC-style zero-run边界裁短后的可见输出长度和原始机械输出长度；
- lineLength含头解释的行终点分类；
- lineLength不含头解释的行终点分类；
- 输出长度类别：`width-...`、`width`、`width+1`、`width+2+`。

注意：OpenRA与standard机械命令本身接近，差别主要在输出缓冲和缺少逐行验证。报告必须避免把两者误写成两个独立格式规范。

## 6. 必须公开的聚合表

### 6.1 Extra output来源

| 维度 | 允许公开值 |
|---|---|
| 总失败帧数 | count |
| 第width+1输出来源 | Literal / ZeroRun / Malformed / None 的count |
| extra是否最后输出 | true/false count |
| extra是否来自最后命令 | true/false count |
| extra索引是否为0 | true/false/unknown count |
| 忽略extra后输入是否精确结束 | true/false count |

### 6.2 达到width时的输入状态

- 行输入已经结束：count；
- 剩1字节：count；
- 剩2字节：count；
- 剩3字节以上：count与范围；
- 剩余字节无法形成完整命令：count。

不得公开剩余字节的值。

### 6.3 最后命令

- literal：count；
- zero-run：count；
- `00 00`：count；
- dangling zero：count；
- 其他malformed：count；
- final zero-run count 的 min/max 和低基数直方图区间，例如 `1`, `2`, `3–4`, `5–8`, `9–16`, `17+`；
- final zero-run开始前rowOutput与width差值的聚合区间，不公开绝对位置序列。

### 6.4 语义对照

对每种候选语义只公开输出长度分类：

| 语义 | width | width+1 | 其他 | 输入精确结束 | malformed |
|---|---:|---:|---:|---:|---:|
| PR11 strict mechanical | | | | | |
| OpenRA-style full payload | | | | | |
| XCC zero-run clip | | | | | |
| lineLength含头 | | | | | |
| lineLength不含头 | | | | | |
| 假设性final-transparent-guard分类器 | | | | | |

“假设性guard分类器”只分类，不生成成功frame，也不进入production。

### 6.5 角色与宽度交叉

允许公开：

- 建筑/步兵/动画/地图增补四类中extra来源类型的count；
- 奇数/偶数width中extra来源类型的count；
- width范围；
- 每类样本数。

禁止公开具体文件名、frame index、width列表或可用于定位单个资源的组合。

## 7. H1–H10 直接判定规则

| 观测 | 提升 | 降低/证伪 |
|---|---|---|
| 257/257 extra均来自最后zero-run，extra为0，忽略1后输入精确结束 | H6、H7 | H3 literal sentinel、H9 |
| extra均来自最后literal且输入精确结束 | H3 | H6、H7 |
| 达到width时仍有固定1个非产出尾字节 | H4 | H6/H7，视命令类别而定 |
| zero-run不是最后命令或中间run也需count-1 | H5 | H6/H7的单一final规则 |
| 机械span总是width+1且extra命令类型不稳定 | H8 | H3/H6单命令解释 |
| lineLength不含头解释跨入下一行或产生非稳定长度 | 支持含头契约 | H4的简单“未减头”版本 |
| rowEnd始终在next distinct offset前且无跨界 | 反对H9 | — |
| raw路径独立统计仍严格width | 反对H2 | — |
| 不同角色需要不同规则 | D门槛/家族误判调查 | 单一production默认 |

## 8. 安全预算

探针必须有：

- 最大文件数、frame数、row数；
- 每行最大输入字节；
- 每行最大命令数；
- 每帧和累计命令预算；
- checked输入位置和输出计数；
- diagnostic上限；
- 失败即停止当前帧，不读取越过声明rowEnd；
- 无像素缓冲分配需求，最多保留标量和固定大小聚合桶。

## 9. 建议输出文件

可由本地Codex在后续单独授权的实现/审计PR中生成：

- 脱敏JSON摘要；
- Markdown解释；
- 运行环境、base/head、输入catalog hash和聚合hash；
- Memory/Stream/MIX window三路径聚合是否一致。

禁止包含：

- 原始entry正文；
- scanline bytes；
- 命令序列；
- pixel indices；
- frame图；
- Base64/hex；
- 每样本明细；
- 绝对路径；
- 用户名或本机目录。

## 10. 探针后的动作

- 符合A门槛：另开production修复PR；
- 只符合B门槛：只加入显式实验策略和合成分类测试，不设默认；
- 只能靠通用裁剪：按C门槛拒绝；
- 角色间分裂：按D门槛先查家族/flags/样本分类；
- 仍不确定：按E门槛维持ProjectBaseline flags3未实现。
