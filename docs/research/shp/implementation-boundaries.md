> **来源声明 / Source notice:** 本研究由 **ChatGPT 网页版**完成并提交，**不是本地 Codex Agent 的产物**。 / Researched and submitted from **ChatGPT Web**, **not by the local Codex Agent**.

# 项目接入与 API 边界设计

## 1. 当前仓库可复用基础

当前 `main` 已有：

- `BinarySourceContext`：parser、logical source id、logical path；
- `BinaryReadSession` / `BoundedBinaryReader`：小端读取、absolute offset、子范围、预算、tail policy；
- `ReadOnlyDataWindow` / session：seekable有界窗口、子窗口、SHA-256；
- MIX virtual content source：把嵌套 MIX entry暴露为有来源链的逻辑窗口；
- `WestwoodPalette`：严格PAL读取、raw/display转换、provenance；
- binary/PAL/MIX diagnostics：明确 code、offset、field、source、provenance。

SHP 接入应延续这些模式，不建立独立的 `BinaryReader`、绝对物理路径或 Unity依赖。

## 2. 建议模块分层

### 2.1 Header and directory parse

输入：`ReadOnlyDataWindow` 或可建立 `BinaryReadSession` 的只读内存。

输出：

```text
ShpDocument
  Header: ShpHeader
  Frames: IReadOnlyList<ShpFrameDescriptor>
  Source/Provenance
  Diagnostics
  OpaqueTrailingData policy
```

只解析 8+24 结构、raw fields、offset和几何；不解压。

### 2.2 Single-frame stream decode

```text
ShpFrameDecoder.Decode(
  frameDataWindow,
  descriptor,
  ShpReadLimits,
  diagnosticSink) -> ShpDecodeResult<ShpIndexedFrameLocal>
```

- raw与RLE分支独立；
- decoder只产生局部 `width × height` 索引；
- 每行创建/使用有界子范围；
- 每行输入、输出和命令数分别受限；
- 不读取下一帧像素；
- 不应用PAL/remap/shadow。

### 2.3 首版不建立 dependency 层

当前没有证据证明 SHP(TS) 目录存在 reference/delta 字段。因此首版：

- 不推荐或定义 `ShpFrameDependency`；
- 不实现 dependency resolver；
- 不增加 dependency depth 或 cumulative work budget；
- 不从 `FrameColorRaw`、`ReservedRaw`、`DataOffsetRaw`、重复offset或帧顺序推断依赖；
- 只增加一个负向测试：parser never invents a dependency。

若未来发现有许可证、可复现样本和第二来源支持的扩展格式，应另立研究工作包；不能在首版 API 中预埋看似确定的引用模型。

### 2.4 Canvas reconstruction

`ShpIndexedFrame` 建议表达完整 canvas：

- `CanvasWidth`, `CanvasHeight`;
- `FrameRectangle`;
- `ReadOnlyMemory<byte> Indices`;
- 是否为局部或规范化完整帧必须由类型区分，不能靠长度猜；
- canonical hash应包含版本化 domain、canvas、rect、raw flags、索引。

重建时以 0 填充并严格写入局部矩形，不做裁剪。

### 2.5 Palette conversion

单独服务：

```text
ShpPaletteRenderer.Render(
  ShpIndexedFrame,
  WestwoodPalette,
  transparencyPolicy) -> RGBA buffer
```

不得放入 `ShpDocumentReader`。PAL选择由资源语义层提供。

### 2.6 Player remap

单独服务接收：

- indexed frame；
- remap table/range；
- house color；
- palette role。

Core SHP不硬编码 remap范围，不改变原始 indices。

### 2.7 Unity adapter

位于 Unity integration assembly：

- RGBA/索引纹理创建；
- filtering、wrap、readability；
- Sprite rectangle；
- pivot、pixels-per-unit；
- main/shadow layer组合。

Core assembly必须不引用 `UnityEngine`。

### 2.8 Art.ini 和资源语义

Rules/Art资源引用层负责：

- logical `Image=`;
- SHP/VXL选择；
- 方向与帧段；
- Start/LoopStart/LoopEnd/Rate；
- 建筑 damaged/buildup/turret/anim；
- shadow组织；
- pivot/anchor/Foundation/YSort/ZAdjust。

UI/cameo、mouse/cursor 的资源选择可能来自其他配置或 catalog，不得强行归入 Rules/Art。不得将这些语义反向写进通用 SHP reader。

## 3. 建议类型

### `ShpHeader`

- `ushort FamilyMarkerRaw`
- `ushort CanvasWidthRaw`
- `ushort CanvasHeightRaw`
- `ushort FrameCountRaw`
- 已验证的 `int CanvasWidth/Height/FrameCount`

### `ShpFrameDescriptor`

- `int Index`
- raw `ushort XRaw/YRaw/WidthRaw/HeightRaw`
- `uint RawFlags`
- `byte[4] FrameColorRaw`
- `uint ReservedRaw`
- `uint DataOffsetRaw`
- validated rectangle / optional data window
- 不包含 Unity pivot
- 不伪造 data length
- 不包含 reference index、base frame 或 dependency 属性

### `ShpCompressionKind`

建议避免映射成 0..3算法：

```text
RawOpaque
RawTransparent
RleZeroTransparent
SourceConflictingFlags2
UnknownFlags
```

同时保留 `RawFlags`。`SourceConflictingFlags2` 表示位模型可表达，但默认字节流策略尚未选择。

### `ShpIndexedFrame`

- immutable；
- 明确 local/full canvas；
- 不含RGBA；
- 不含Texture2D；
- 可计算规范化 hash；
- 可统计 index min/max/histogram摘要。

### `ShpDecodeResult`

- success/failure二选一；
- frame或diagnostics；
- bytes consumed、rows consumed、commands consumed、pixel count；
- 不允许“成功但偷偷补零”；
- 对flags 2默认返回策略未选择，而不是猜测一种解码。

### `ShpDiagnostic`

建议包含：

- `ShpDiagnosticCode`
- `BinaryDiagnosticCode?`
- `BinarySourceContext`
- provenance chain
- frame index、row index
- absolute offset
- raw flags
- field/section
- message

诊断码至少覆盖：invalid marker、directory truncated、source-conflicting flags2、unknown flags、reserved nonzero、rectangle overflow/outside canvas、invalid offset、overlap、row length invalid、row truncated、zero-run truncated/overflow、zero-output command policy unresolved、command budget exceeded、pixel underflow/overflow、trailing data、budget exceeded。

### `ShpReadLimits`

至少：

- max input bytes
- max frame count
- max canvas dimension/area
- max local frame area
- max total decoded pixels
- max single row bytes
- max commands per row/frame
- max compressed bytes per frame
- max allocated bytes
- max records/subranges
- max diagnostic count

首版不包含 dependency depth 或 cumulative dependency work。转换为现有 `BinaryReadLimits` / `ReadOnlyDataWindowLimits` 时必须保留更严格者。

## 4. 解析流程

1. MIX/content resolver提供 logical window + provenance；
2. header parser读取8字节；
3. checked计算目录长度；
4. 逐项保存 raw descriptor；
5. 完成目录后统一验证矩形、offset、flags、reserved、候选数据区间；
6. 明确保证 descriptor 没有从任何raw字段派生dependency；
7. `ShpDocument`只在目录可信时暴露可解码帧；
8. 按需解码单帧；
9. 可选重建完整 canvas；
10. 可选PAL转换；
11. Art/UI/mouse/runtime层决定资源角色、阴影、remap、pivot和播放。

## 5. 区间策略

由于目录无显式长度：

- raw：可信消费长度=`area`；
- RLE：读取每行长度并累加，最终得到实际 consumed length；
- offset排序只用于计算 `next distinct offset` 上界和重叠诊断；
- 重复offset可以是损坏，也可能是共享数据兼容扩展；不得未经证据自动去重；
- 重复或重叠offset绝不能被解释为reference关系；
- 任何帧实际消费不得跨越文件窗口；
- 若跨越下一distinct offset，应报 overlap；是否允许共享完全相同区间由策略决定。

## 6. RLE命令边界

- 非零命令消费1字节并输出1像素；
- `00 count`消费2字节并输出`count`像素；
- `00 00`消费2字节并输出0像素，输入位置会前移；
- 每个命令计入命令预算；
- 每个命令必须推进输入；
- 行输入必须精确消费声明范围；
- 行输出最终必须等于width；
- `00 00`是否可接受由未来证据决定，不能靠终止安全性替代格式语义判断。

## 7. Provenance

`ShpDocument`、每个 diagnostic和本地审计记录都应带：

- logical source id；
- logical content path；
- MIX archive/entry chain；
- 资源选择依据类别：Rules/Art、UI/resource config、mouse/UI config、verified catalog survey等；
- 不含绝对物理路径；
- parser/version；
- 可选源entry SHA-256。

这与当前 PAL/MIX模型保持一致。
