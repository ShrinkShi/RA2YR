# Test matrix — 166 research cases

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明的实现仅作行为与冲突参考，未复制、翻译或机械移植其代码、公式实现或测试夹具。`code_imported: false`。


## 1. Rules

- 本文件只设计测试，不实现 Unity/C# 测试。
- expected values必须由独立手算、显式小表或第二套不共享production公式的oracle给出。
- synthetic fixtures不得复用production projection、anchor binder或sorting comparator来生成expected。
- 所有测试记录selected policy/profile/evidence。
- 输入模式等价要求覆盖 Memory、Stream、short-read Stream与exact MIX window。
- pass/depth结果不得影响simulation occupancy/pathfinding。
- UI/camera变化不得改变logical depth。
- 总数：`30 + 22 + 24 + 28 + 24 + 22 + 16 = 166`。

## 2. Category summary

| Prefix | Category | Count |
|---|---|---:|
| CP | coordinate/projection | 30 |
| LH | Level/height/ramp | 22 |
| EF | entity families/render passes | 24 |
| DT | depth ordering/ties | 28 |
| AB | anchors/foundations/bounds | 24 |
| OS | occlusion/shadow/bridge/aircraft | 22 |
| SA | safety/architecture/audit | 16 |
| **Total** | | **166** |

## Coordinate / projection (30)

| ID | Case | Required assertion |
|---|---|---|
| CP-01 | raw axis identity | 手写raw(2,3)，验证domain未变且不原地归一化 |
| CP-02 | raw/canvas axis sign candidate A | 按fixture表验证(-x+y,x+y)，不调用production projection |
| CP-03 | raw/canvas axis swap conflict | 同raw输入在交换轴profile下产生不同结果并标profile |
| CP-04 | map-size X bias | IsoSize偏置只进入profile结果，不写回raw |
| CP-05 | zero origin | raw(0,0), Level0的手写screen结果 |
| CP-06 | nonzero projection origin | origin平移只改screen，不改depth components |
| CP-07 | negative raw X | checked中间值与预期负canvas一致 |
| CP-08 | negative raw Y | 负轴与符号矩阵一致 |
| CP-09 | both raw negative | sum/difference手算，无unsigned wrap |
| CP-10 | int16 min coordinate | 64位中间计算或受控range failure |
| CP-11 | int16 max coordinate | 64位中间计算或受控range failure |
| CP-12 | X+Y overflow boundary | 禁止16位wrap |
| CP-13 | Y-X overflow boundary | 禁止16位wrap |
| CP-14 | map-size multiplication overflow | 产生ProjectionOverflow diagnostic |
| CP-15 | even tile metrics | 60×30 half metrics精确30/15 |
| CP-16 | TS tile metrics | 48×24 profile精确24/12 |
| CP-17 | odd tile width rejected/profiled | 不静默整数除法 |
| CP-18 | odd tile height rejected/profiled | height step分数由policy表示 |
| CP-19 | even canvas parity | 整数screen坐标手算 |
| CP-20 | odd canvas parity | 分数/rounding按profile |
| CP-21 | truncate rounding positive | 独立expected值 |
| CP-22 | truncate rounding negative | 明确向零而非floor |
| CP-23 | floor rounding negative | 与truncate区分 |
| CP-24 | nearest midpoint positive | 明确tie rule |
| CP-25 | nearest midpoint negative | 明确tie rule |
| CP-26 | inverse roundtrip interior | 只在profile承诺范围内成功 |
| CP-27 | inverse boundary ambiguity | 返回candidate/diagnostic而非任意cell |
| CP-28 | camera origin separation | camera offset不改logical projection |
| CP-29 | viewport/DPI/zoom separation | 物理缩放不改logical screen/depth |
| CP-30 | screen shake separation | shake仅final display transform |

## Level / height / ramp (22)

| ID | Case | Required assertion |
|---|---|---|
| LH-01 | Level zero | 无垂直偏移 |
| LH-02 | Level one RA2 | screenY上移15 logical pixels |
| LH-03 | Level one TS | screenY上移12 logical pixels |
| LH-04 | nonzero Level multiple | checked乘法手算 |
| LH-05 | negative Level candidate | 按policy接受或诊断，不wrap |
| LH-06 | Level max configured | 边界成功 |
| LH-07 | Level above configured | structured diagnostic |
| LH-08 | Level arithmetic overflow | 严格失败 |
| LH-09 | TMP HeightRaw zero preserved | 不并入Level |
| LH-10 | TMP HeightRaw 255 preserved | 保留raw byte与候选view |
| LH-11 | TMP HeightRaw signed conflict | profile切换不改raw |
| LH-12 | RampType zero flat | typed flat candidate |
| LH-13 | RampType 1 known | 只建立descriptor，不改raw |
| LH-14 | RampType 20 known candidate | community profile可绑定 |
| LH-15 | RampType 21 unknown | 保留raw并diagnose |
| LH-16 | ramp does not move cell identity | ScenarioCell稳定 |
| LH-17 | ramp contact offset | 手写surface contact expected |
| LH-18 | building slope policy required | binder不自行拟合SHP底边 |
| LH-19 | vehicle pose candidate | pitch/roll只进presentation snapshot |
| LH-20 | infantry subcell on ramp | foot contact与source ordinal分离 |
| LH-21 | water/shore Level separation | palette/terrain role不改Level |
| LH-22 | cliff extra graphic separation | extra bounds不改邻cell高度 |

## Entity families / render passes (24)

| ID | Case | Required assertion |
|---|---|---|
| EF-01 | TMP ground family | GroundColor pass与ISO palette |
| EF-02 | TMP extra family | extra visual bounds独立 |
| EF-03 | Overlay below-unit family | 独立pass候选 |
| EF-04 | Overlay terrain-object family | type policy选择pass |
| EF-05 | Smudge family | decal pass且无occupancy推导 |
| EF-06 | Terrain object family | ground anchor与frame bounds |
| EF-07 | Structure foundation entity | foundation/bib先于body |
| EF-08 | Structure body entity | damage state不改ground anchor |
| EF-09 | Unit SHP body | unit palette/remap与ground actor pass |
| EF-10 | Unit VXL body | transform/bounds独立 |
| EF-11 | Infantry family | subcell foot与same-cell ordinal |
| EF-12 | Aircraft family | AirLayer与ground reference |
| EF-13 | SHP animation attached | parent key+attachment ordinal |
| EF-14 | SHP animation world | 独立anchor/pass |
| EF-15 | VXL turret | parent pivot与fixed role ordinal |
| EF-16 | VXL barrel | recoil只改transform/bounds |
| EF-17 | Shadow family | ShadowPass且不占地 |
| EF-18 | Projectile family | runtime altitude明确 |
| EF-19 | Particle family | conservative bounds与effect pass |
| EF-20 | Bridge underlay | UnderBridgeLayer |
| EF-21 | Bridge deck | BridgeDeckLayer |
| EF-22 | Fog/shroud stage | visibility stage不改entity key |
| EF-23 | UI marker/selection | UIAnnotation不进入world depth |
| EF-24 | Debug overlay | Debug pass不写Core格式语义 |

## Depth ordering / ties (28)

| ID | Case | Required assertion |
|---|---|---|
| DT-01 | different screenY primary | 按policy升序 |
| DT-02 | same screenY different pass | pass先决 |
| DT-03 | same screenY different elevation | layer先决 |
| DT-04 | same cell two infantry | subcell/source ordinal稳定 |
| DT-05 | same cell duplicate infantry | duplicate ordinal诊断且稳定 |
| DT-06 | unit vs infantry same cell | family priority显式 |
| DT-07 | terrain vs structure overlap | pass/family policy显式 |
| DT-08 | building foundation vs body | foundation在body前 |
| DT-09 | body vs turret | attachment ordinal稳定 |
| DT-10 | turret vs barrel | fixed role order |
| DT-11 | body vs shadow | ShadowPass/receiver policy |
| DT-12 | attached anim vs parent | explicit Z/Y adjust + role |
| DT-13 | two exact same anchors | source ordinal解决 |
| DT-14 | two exact same full keys | stable identity解决 |
| DT-15 | duplicate stable identity | strict diagnostic/duplicate policy |
| DT-16 | source order stability | canonical source ordinal而非collection order |
| DT-17 | dictionary enumeration shuffled | 结果仍相同 |
| DT-18 | asset load completion shuffled | 结果仍相同 |
| DT-19 | parallel collection shuffled | canonical归并 |
| DT-20 | save/load stable order | 不使用新对象ID |
| DT-21 | network deterministic spawn | simulation ordinal固定 |
| DT-22 | replay camera changed | order不变 |
| DT-23 | zoom changed | order不变 |
| DT-24 | screen shake changed | order不变 |
| DT-25 | negative SHP offset | visual rect变、ground key不变 |
| DT-26 | damage frame bounds changed | ground key不跳 |
| DT-27 | ZAdjust signed extremes | checked/diagnostic |
| DT-28 | packed depth key overflow | 拒绝wrap，tuple仍可比较 |

## Anchors / foundations / bounds (24)

| ID | Case | Required assertion |
|---|---|---|
| AB-01 | TMP local origin | cell anchor与sX/sY分离 |
| AB-02 | TMP extra negative offset | visual bounds扩展 |
| AB-03 | SHP zero frame offset | pivot按profile |
| AB-04 | SHP negative X offset | raw保留 |
| AB-05 | SHP negative Y offset | raw保留 |
| AB-06 | SHP transparent crop | crop不改anchor |
| AB-07 | SHP changing frame bounds | ground anchor稳定 |
| AB-08 | missing Art offset | diagnostic而非移动对象 |
| AB-09 | unit foot point | 不使用image center |
| AB-10 | infantry subcell anchor | subcell profile手算 |
| AB-11 | aircraft center vs ground reference | 两个anchor并存 |
| AB-12 | VXL/HVA origin | part transform与ground分离 |
| AB-13 | turret pivot | parent attachment |
| AB-14 | barrel pivot | turret attachment |
| AB-15 | muzzle anchor | effect attachment不改unit anchor |
| AB-16 | building Foundation 1x1 | authored cell set |
| AB-17 | rectangular Foundation 3x2 | origin/bounds手写 |
| AB-18 | Foundation.X/Y offset | origin偏移不改art bounds |
| AB-19 | irregular foundation candidate | cell set保留孔洞 |
| AB-20 | foundation not inferred from SHP | 缺失时diagnose |
| AB-21 | visual vs culling bounds | conservative覆盖visual |
| AB-22 | selection vs occupancy bounds | 互不回写 |
| AB-23 | shadow vs caster bounds | 独立cull |
| AB-24 | VXL rotation bounds | conservative profile覆盖所有pose |

## Occlusion / shadow / bridge / aircraft (22)

| ID | Case | Required assertion |
|---|---|---|
| OS-01 | TMP depth absent allowed | raw parse按profile成功并标missing |
| OS-02 | TMP depth flag present exact | window长度精确 |
| OS-03 | TMP depth truncated | 严格失败/diagnostic，不padding |
| OS-04 | TMP extra depth absent | 不伪造 |
| OS-05 | TMP extra depth truncated | 严格边界 |
| OS-06 | depth bytes not Level | 无法影响projection Level |
| OS-07 | depth bytes not passability | simulation view不变 |
| OS-08 | depth zero sample policy | profile解释，raw不改 |
| OS-09 | depth out-of-range sample | 保留raw并diagnose |
| OS-10 | occlusion mask candidate | 只生成descriptor不改anchor |
| OS-11 | tree transparency | opacity变但depth key不变 |
| OS-12 | alpha/depth combination | 显式policy |
| OS-13 | SHP shadow frame source | ShadowSource分类 |
| OS-14 | in-frame palette shadow source | family profile分类 |
| OS-15 | VXL shadow source | future geometry ref |
| OS-16 | aircraft ground shadow | ground anchor与air anchor分离 |
| OS-17 | shadow on ramp | receiver surface profile |
| OS-18 | shadow on bridge deck | receiver layer显式 |
| OS-19 | low bridge overlay | 不自动建立deck simulation |
| OS-20 | unit above bridge | BridgeDeckLayer |
| OS-21 | unit under bridge | UnderBridgeLayer |
| OS-22 | aircraft above bridge | AirLayer且shadow receiver待policy |

## Safety / architecture / audit (16)

| ID | Case | Required assertion |
|---|---|---|
| SA-01 | Memory input equivalence | descriptor/order/diagnostics基准 |
| SA-02 | seekable Stream equivalence | 与Memory相同 |
| SA-03 | short-read Stream equivalence | 逐段读仍相同 |
| SA-04 | MIX window equivalence | 不越界且相同 |
| SA-05 | MIX window escape attempt | 严格拒绝 |
| SA-06 | bounded entity count | 超限结构化失败 |
| SA-07 | bounded bounds count | 超限失败 |
| SA-08 | bounded foundation edges | 超限失败 |
| SA-09 | bounded depth plane bytes | 超限失败 |
| SA-10 | noEngineReferences | Core assembly不引用UnityEngine |
| SA-11 | no Unity object creation | 无GameObject/Texture/Sprite/Mesh/Material |
| SA-12 | parser/renderer separation | reader只产raw descriptor |
| SA-13 | sorting/pathfinding separation | 排序不改变occupancy |
| SA-14 | UI/simulation separation | UI bounds不改footprint |
| SA-15 | independent synthetic oracle | expected不调用production公式/binder/sort |
| SA-16 | sanitized audit redaction | 禁止字段均不出现在public report |

## 3. Cross-cutting acceptance

Every applicable case additionally checks:

- raw input remains unchanged;
- derived descriptor carries profile/policy ID;
- evidence grade is serializable;
- diagnostics are structured;
- arithmetic is checked;
- no hash/dictionary/Unity instance ID ordering;
- no camera zoom in logical key;
- no image-size-derived foundation;
- no shadow/depth-derived occupancy;
- no Unity object creation.

## 4. Determinism repetition

`DT-01..28` and `SA-01..04` repeat with:

1. original collection order;
2. reversed collection order;
3. stable source records loaded in randomized batches;
4. save/load reconstructed identities;
5. alternate camera/zoom/viewport.

Only display-space bounds may change under camera transforms. Logical pass, elevation layer, depth tuple and tie resolution must remain byte-for-byte/canonically equal.

## 5. Negative tests

Failures must return deterministic diagnostic/failure classes, not partial success, for:

- arithmetic overflow;
- truncated claimed depth plane;
- MIX window escape;
- entity/bounds/foundation/depth limits;
- unresolved exact tie under strict policy;
- missing authored foundation when required;
- use of Unity/camera/UI data in Core semantics.

## 6. Future audit relation

ProjectBaseline cannot become a fixture source. A future sanitized audit may only report aggregate pass/layer/family/collision/diagnostic categories and input-mode equality with evidence `ObservedByFutureProjectBaselineAudit`.
