# Test matrix — 174 research cases

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明实现仅作行为、格式事实与冲突参考，未复制、翻译、逐句改写或机械移植代码、寻路算法、Locomotor 实现或测试夹具。`code_imported: false`。


## Rules

只设计测试，不实现C#/Unity。expected由独立手算/fixture表产生，不复用production坐标、邻接或cost逻辑。每项记录policy、product和evidence。Memory、Stream、short-read Stream、exact MIX window必须等价。

| Prefix | Category | Count |
|---|---|---:|
| CS | cell/surface/topology | 28 |
| TR | terrain/TMP/theater role | 24 |
| ML | MovementZone/SpeedType/Locomotor | 28 |
| RW | ramp/cliff/water/shore | 24 |
| OO | Overlay/Terrain/Structure occupancy | 26 |
| BT | bridge/tunnel/elevated/dynamic occupancy | 24 |
| GS | graph/cost/safety/architecture/audit | 20 |
| **Total** | | **174** |

## CS (28)

`CS-01` sparse record set

`CS-02` dense record set

`CS-03` mixed sparse/dense markers

`CS-04` duplicate identical cell

`CS-05` duplicate conflicting cell

`CS-06` explicit default cell

`CS-07` missing interior cell

`CS-08` out-of-domain record

`CS-09` negative raw X

`CS-10` negative raw Y

`CS-11` raw/canvas axis conflict

`CS-12` parity valid diamond cell

`CS-13` parity invalid cell

`CS-14` Size vs LocalSize

`CS-15` Scenario Y×1000+X candidate

`CS-16` Scenario axis conflict

`CS-17` Overlay X+512Y mapping

`CS-18` Overlay boundary 0/511

`CS-19` Overlay coordinate 512

`CS-20` tile raw32 candidate

`CS-21` tile low16/high16 conflict

`CS-22` invalid GlobalTileId

`CS-23` invalid SubTile

`CS-24` Level zero/nonzero

`CS-25` coordinate arithmetic overflow

`CS-26` source-order permutation

`CS-27` cell budget exact limit

`CS-28` cell budget exceeded

## TR (24)

`TR-01` known TMP TerrainTypeRaw

`TR-02` unknown TMP TerrainTypeRaw

`TR-03` extension TerrainTypeRaw

`TR-04` known RampTypeRaw

`TR-05` unknown RampTypeRaw

`TR-06` TMP HeightRaw differs from Level

`TR-07` TMP flags unknown bits

`TR-08` TMP extra graphics present

`TR-09` TMP depth plane present

`TR-10` TMP depth plane missing optional

`TR-11` TMP depth plane truncated

`TR-12` TMP damaged-data candidate

`TR-13` missing TMP asset

`TR-14` missing TMP variation

`TR-15` theater Clear role

`TR-16` theater Water role

`TR-17` theater Cliff role

`TR-18` theater Ramp role

`TR-19` LAT transition role

`TR-20` custom role extension

`TR-21` filename WATER heuristic disabled

`TR-22` Rules land binding missing

`TR-23` conflicting theater/TMP land roles

`TR-24` Art missing

## ML (28)

`ML-01` MovementZone Normal

`ML-02` MovementZone Crusher

`ML-03` MovementZone Destroyer

`ML-04` MovementZone Amphibious

`ML-05` MovementZone Water

`ML-06` MovementZone WaterBeach

`ML-07` MovementZone Fly

`ML-08` MovementZone Infantry

`ML-09` MovementZone Subterrannean spelling

`ML-10` unknown MovementZone

`ML-11` case-varied MovementZone

`ML-12` missing MovementZone

`ML-13` extension MovementZone

`ML-14` SpeedType Foot

`ML-15` SpeedType Track

`ML-16` SpeedType Wheel

`ML-17` SpeedType Float

`ML-18` SpeedType Winged

`ML-19` SpeedType Hover

`ML-20` SpeedType Amphibious

`ML-21` SpeedType Creep

`ML-22` SpeedType FloatBeach

`ML-23` unknown SpeedType

`ML-24` missing speed-table entry

`ML-25` zero terrain percentage

`ML-26` missing Locomotor

`ML-27` unknown/extension Locomotor CLSID

`ML-28` cross-property contradiction

## RW (24)

`RW-01` same Level flat neighbors

`RW-02` Level delta one with valid ramp

`RW-03` Level delta one without ramp

`RW-04` Level delta greater than one

`RW-05` malformed ramp Level

`RW-06` ramp to missing cell

`RW-07` ramp direction mismatch

`RW-08` one-way ramp candidate

`RW-09` diagonal flat neighbor

`RW-10` diagonal corner cut blocked

`RW-11` visual cliff extra only

`RW-12` TileSet cliff role same Level

`RW-13` different Level with bridge entrance

`RW-14` custom ramp extension

`RW-15` known water surface

`RW-16` blue non-water art

`RW-17` known shore transition

`RW-18` water-to-land without shore

`RW-19` amphibious matching properties

`RW-20` SpeedType/MovementZone shore conflict

`RW-21` ship on land node

`RW-22` hover over water

`RW-23` ice runtime state

`RW-24` bridge over water

## OO (26)

`OO-01` empty Overlay

`OO-02` resource Overlay

`OO-03` resource Overlay missing data

`OO-04` wall Overlay

`OO-05` fence crushable

`OO-06` gate closed snapshot

`OO-07` gate open snapshot

`OO-08` gate unknown state

`OO-09` bridge Overlay art only

`OO-10` bridge OverlayData damage

`OO-11` rock/debris Overlay

`OO-12` crate Overlay

`OO-13` unknown Overlay

`OO-14` Terrain tree blocker

`OO-15` Terrain rock crushable

`OO-16` Terrain visual-only light post

`OO-17` Terrain Art missing

`OO-18` Smudge crater

`OO-19` Smudge scorch

`OO-20` building rectangular foundation

`OO-21` irregular foundation

`OO-22` Foundation.X/Y offset

`OO-23` building damaged frame

`OO-24` factory exit cell

`OO-25` Bib cells

`OO-26` upgrade attachment

## BT (24)

`BT-01` low bridge pieces complete

`BT-02` low bridge pieces malformed

`BT-03` high bridge center-piece expansion

`BT-04` high bridge deck node

`BT-05` under-bridge node

`BT-06` bridge entrance valid

`BT-07` bridge entrance missing

`BT-08` Unit High field only

`BT-09` partially destroyed bridge

`BT-10` fully destroyed bridge

`BT-11` bridge repair state

`BT-12` water below bridge

`BT-13` wall below/at bridge conflict

`BT-14` aircraft above bridge

`BT-15` aircraft landed

`BT-16` aircraft shadow

`BT-17` Tube valid directions

`BT-18` Tube invalid direction

`BT-19` Tube out-of-domain part

`BT-20` Tube missing counterpart

`BT-21` subterranean entry/exit

`BT-22` free-burrow extension

`BT-23` teleport transition

`BT-24` dynamic occupancy tick change

## GS (20)

`GS-01` deterministic node order

`GS-02` deterministic edge order

`GS-03` duplicate edge

`GS-04` one-way special edge

`GS-05` cost raw percentage

`GS-06` cost multiplier order

`GS-07` cost fixed-point rounding

`GS-08` cost overflow

`GS-09` zero speed division

`GS-10` impassable state vs sentinel

`GS-11` movement/buildability split

`GS-12` static vs dynamic occupancy

`GS-13` crush action-aware policy

`GS-14` Memory input

`GS-15` seekable Stream input

`GS-16` short-read Stream input

`GS-17` exact MIX window

`GS-18` no-progress stream

`GS-19` architecture noEngineReferences

`GS-20` sanitized audit output

## Required assertions by category

- `CS`：raw identity、sparse/dense、duplicate/missing/default、domain/parity/overflow、budgets均显式且不last-wins。
- `TR`：TMP raw、theater role、missing asset和Art边界不直接生成passability。
- `ML`：unknown/missing/extension不默认Normal；MovementZone、SpeedType、Locomotor分离并报告冲突。
- `RW`：Level、ramp、cliff、water、shore和diagonal由显式transition policy组合。
- `OO`：Overlay family、Terrain type、Smudge、foundation、gate和Bib分开；图片不定义occupancy。
- `BT`：deck/under/air/subterranean层分开；damage与dynamic state不改raw。
- `GS`：deterministic ordering、cost overflow/rounding、movement/buildability分离、四种input等价、no-progress与架构隔离。

## Architecture assertions

```text
noEngineReferences
noUnityObjects
noNavMesh
noCollider
noTilemap
noGrid
noGameObject
noPathfindingImplementation
noMovementSimulation
```

sanitized audit禁止cell序列、坐标、graph topology、exact costs和per-map hash。
