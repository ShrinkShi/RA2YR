# Source comparison

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明的实现仅作行为、接口与冲突参考，未复制、翻译或机械移植其代码、公式实现、switch 表或测试夹具。`code_imported: false`。


## Source ledger

| Source | Revision/version | Path/topic | License | Category | Product/profile | Use | code_imported |
|---|---|---|---|---|---|---|---|
| EA FinalSun/FinalAlert 2 | `6abf0f557469baea73079c6bf6550709e2e3584e` | editor source, ScriptTypes, Rules/editor catalogs | GPL-3.0-or-later | official editor | TS/RA2 editor | authoring and absence boundary; not runtime combat | false |
| OpenRA | `a520984d91eda9de48a62b1d15c1e3bad0d4fb1a` | `WeaponInfo.cs`, `Armament.cs`, `Bullet.cs`, `DamageWarhead.cs`, `SpreadDamageWarhead.cs` | GPL-3.0-or-later | independent implementation | OpenRA engine | architecture and conflict comparison only | false |
| World-Altering Editor | `b4c9481e9b00fb0a38739049a046f528b6054ce2` | Rules/editor views | GPL-3.0-or-later | editor/tool | TS/RA2/YR + extensions | field catalog and authoring behavior where located | false |
| Chrono Divide mod SDK | `5943c4ae6c19897929d348a417d6d2f1481b75fd` | supported Rules flags and incompatibilities | repository-specific/public docs | independent implementation docs | Chrono Divide | consumer behavior, not stock proof | false |
| CnCNet client | `e6e367bbe04c1a0dc1e34a8fed2856ea3ab7e8c4` | client/game-mode consumer | GPL-3.0 | client | RA2/YR ecosystem | supplementary only | false |
| Ares docs | 3.0 | ArmorTypes, Verses, Warheads, EMP, weapons | public project docs | official extension docs | YR+Ares | extension/bug-fix boundary | false |
| Phobos docs | stable/latest | projectile, warhead, targeting and effect extensions | GPL/project docs | official extension docs | YR+Ares+Phobos | extension boundary | false |
| Vinifera docs | latest/master | TS ports, armor, warhead, CellSpread | project docs | official extension docs | TS+Vinifera | port/extension boundary | false |
| ModEnc | fixed/current revisions | Weapon, Projectile, Warhead, Verses, CellSpread, PercentAtMax, Armor | community site | community docs | TS/RA2/YR | terminology and conflict discovery | false |
| PPM | fixed discussions when cited | projectile/weapon/warhead observations | community posts | community | mixed | conflict reports only | false |
| RA2 DIY | fixed tutorials when cited | Rules weapon tutorials | community | community | RA2/YR modding | supplementary semantic reference | false |
| XCC/openra2 lineage | pinned where applicable | readers/tools | GPL/mixed | shared lineage | mixed | not counted as independent runtime proof | false |

## Evidence findings

### Official editor

No complete original game combat executor was located in EA's released mission editor source. The editor can expose field names, catalogs and authoring behavior, but does not confirm final firing, flight, collision, targeting or damage arithmetic.

### OpenRA

OpenRA explicitly separates Weapon definitions, projectile spawn/runtime state, target validity, Warhead execution, damage versus armor, spread/falloff and presentation. This supports the proposed responsibility boundaries. Its units, formulas, data model and algorithms are not Westwood runtime facts.

### Ares

Ares documents:

- 11 existing RA2/YR armor types;
- named armor extensions;
- Verses targeting special values and bug fixes;
- independent force-fire/retaliate/passive-acquire controls;
- effects versus damage gates;
- additional weapons/warheads/EMP and other effects.

Ares behavior is never promoted to stock YR unless specifically described as a bug fix, and even bug-fix documentation is extension evidence about the original behavior, not source code proof.

### Phobos

Phobos exposes additional target/effect gates, projectile interception, shields, critical hits, extra warheads, trajectories and building AOE merge policies. These prove extension diversity and reinforce profile isolation.

### Vinifera

Vinifera ports RA2-like CellSpread and warhead controls to TS and adds named armor behavior. A “ported from RA2” claim remains project documentation, not original RA2 runtime source.

### Community documentation

ModEnc provides the strongest consolidated community descriptions for stock fields and the positional armor order. Pages also document contradictions and historical corrections. Evidence remains `CommunityDocumented`.

## Shared-lineage rule

Implementations or documents derived from XCC, OpenRA or one another are one evidence family for independence counting. Agreement among descendants does not become official runtime confirmation.

## Source URLs

- https://github.com/electronicarts/CNC_TS_and_RA2_Mission_Editor
- https://github.com/OpenRA/OpenRA
- https://github.com/Starkku/World-Altering-Editor
- https://github.com/chronodivide/mod-sdk
- https://ares-developers.github.io/Ares-docs/
- https://phobos.readthedocs.io/
- https://vinifera.readthedocs.io/
- https://modenc.renegadeprojects.com/


## Evidence grades

- `ConfirmedByOfficialRuntimeSource`
- `ConfirmedByOfficialEditorSource`
- `ConfirmedByIndependentImplementation`
- `CommunityDocumented`
- `ObservedByFutureProjectBaselineAudit`
- `ConfiguredForProjectPolicy`
- `Unresolved`

没有完整公开的 RA2/YR 原版战斗运行时源码。官方 FinalSun/FinalAlert 2 只能提供编辑器、字段目录和 authoring 行为证据，不能替代 `gamemd.exe` 运行时证据。
