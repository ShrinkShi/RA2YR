> **来源声明 / Source notice:** 本研究由 **ChatGPT 网页版**完成并提交，**不是本地 Codex Agent 的产物**。 / Researched and submitted from **ChatGPT Web**, **not by the local Codex Agent**.

# M2-R：Westwood SHP(TS) 格式研究索引

本目录只研究 **Tiberian Sun / Red Alert 2 / Yuri's Revenge 使用的 SHP(TS)**，不实现解码器，不改变兼容矩阵，不包含任何原版像素或可还原原版图像的数据。

## 结论摘要

- SHP(TS) 没有独立 FourCC；首个 `UINT16LE` 固定为 `0`，只能作为家族判别线索，不能单独证明文件合法。
- 固定文件头为 8 字节；每帧目录项为 24 字节；所有数值按小端解释。
- 目录项中的 32 位值应建模为 **flags/raw flags**，而不是四种互斥压缩算法编号：
  - `0`: 无透明标志、无 RLE，原始局部矩形；
  - `1`: 有透明标志、无 RLE，原始局部矩形；
  - `2`: RLE 位开启但透明位关闭，属于矛盾/可疑组合；部分读取器仍会解码；
  - `3`: 有透明标志且使用逐行 RLE-Zero。
- RLE-Zero 每行以包含自身的 `UINT16LE` 行长度开头；`0x00, count` 表示零值运行；其他字节是单个字面像素。没有得到证据支持“literal-run 长度控制码”。
- SHP(TS) 目录没有显式帧数据长度，也没有已确认的参考帧字段。TD/RA1 的 LCW、XOR delta、format 20/40/80、参考帧链不属于 SHP(TS)。
- XCC 的“format 4”是工具内部紧凑转换格式，不是 SHP(TS) 目录中的压缩值 `4`。
- SHP 存储 8 位调色盘索引，不内嵌 PAL。透明、玩家色、阴影和最终颜色属于索引/调色盘/Art.ini/运行时语义的组合。
- Unity Sprite pivot 不是文件格式字段；格式层只提供全局 canvas 与局部 frame rectangle。

## 文件

| 文件 | 用途 |
|---|---|
| [format-dossier.md](format-dossier.md) | 文件头、目录、像素、坐标和边界事实 |
| [shp-family-boundaries.md](shp-family-boundaries.md) | TD/RA1、SHP(TS)、Dune 与 XCC 内部格式边界 |
| [compression-variants.md](compression-variants.md) | flags、原始帧、RLE-Zero、编号冲突 |
| [source-comparison.md](source-comparison.md) | 固定 revision、路径、许可证和冲突表 |
| [test-matrix.md](test-matrix.md) | 58 项合成测试与防御性检查 |
| [implementation-boundaries.md](implementation-boundaries.md) | 对当前 Core/MIX/PAL/diagnostic 架构的接入设计 |
| [baseline-audit-request.md](baseline-audit-request.md) | 交给本地 Codex 的脱敏黄金审计清单 |
| [unresolved-questions.md](unresolved-questions.md) | 不得提前编码的未决问题 |

## 本研究没有做的事情

- 没有实现 SHP；
- 没有运行 Unity；
- 没有读取 `ProjectBaseline`；
- 没有读取或提交原版 SHP 像素；
- 没有修改 `docs/compatibility/matrix.yml`；
- 没有提升任何兼容状态。
