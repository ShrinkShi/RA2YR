# Future ProjectBaseline sanitized audit request

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明的实现仅作行为、接口与冲突参考，未复制、翻译或机械移植其代码、公式实现、switch 表或测试夹具。`code_imported: false`。


## Status

Design only. This audit was not run and no ProjectBaseline data was read.

## Evidence label

All future observations use only:

```text
ObservedByFutureProjectBaselineAudit
```

They do not automatically promote compatibility.

## SelectionBasis

A future local agent may select broad anonymous categories such as:

- type family;
- scenario/rules layer category;
- stock-looking versus extension-looking section shape;
- weapon/projectile/warhead presence combinations;
- armor/Verses shape categories;
- special-effect categories.

Selection criteria must not contain names or paths in public output.

## Allowed public aggregates

- broad type/category counts;
- Weapon/Projectile/Warhead section presence;
- reference binding status counts;
- duplicate/case-collision/dangling counts;
- numeric-field presence and coarse shape buckets;
- Damage/ROF/Range/Burst presence;
- projectile profile categories;
- armor/Verses length distributions;
- percentage spelling categories;
- CellSpread/falloff presence categories;
- targeting capability categories;
- ammo/reload field-presence categories;
- special-effect category counts;
- diagnostics by code/severity;
- bounded-input results;
- Memory/Stream/short-read/MIX equivalence;
- non-linkable aggregate hash.

## Forbidden public output

- type, Weapon, Projectile, Warhead or Armor names;
- INI text or exact section/key/value records;
- full reference graph;
- exact Damage, ROF, Range, MinimumRange or Burst;
- exact Verses or armor order from samples;
- exact projectile parameters;
- Trigger IDs/opcodes/parameters;
- object/resource names;
- SHP/VXL/audio references;
- positions, maps or graph topology;
- screenshots/rendered effects;
- per-type/per-resource/per-map hash;
- hex/Base64;
- absolute paths/usernames;
- information sufficient to reconstruct Rules configurations.

## Coarse buckets

Examples only:

```text
numeric sign: negative / zero / positive / invalid
magnitude: tiny / small / medium / large / extreme
list length: 0 / 1 / 2..5 / 6..10 / 11 / 12+
binding: unique / missing / ambiguous / extension
projectile: invisible-like / arcing-like / guided-like / mixed / unknown
effect: conventional / fire / radiation / control / temporal / EMP / other
```

Never publish exact bucket thresholds if combined with counts that could identify a type.

## Input-mode equivalence

The future audit should compare canonical aggregate results across:

- memory;
- seekable stream;
- deliberate short-read stream;
- exact MIX entry window.

No read may escape the MIX window.

## Hashing

Use a one-run aggregate salt or a project-approved non-linkable aggregation strategy. Do not publish per-record hashes or stable hashes reusable for cross-dataset matching.

## Audit output structure

```text
AuditVersion
SelectionBasisCategory
ProductProfileCategories
AggregateCounts
ShapeHistograms
BindingCategories
DiagnosticCounts
InputModeEquivalence
NonLinkableAggregateHash
EvidenceGrade
```

## Prohibited actions

The audit does not:

- execute the game/editor;
- fire weapons;
- simulate projectiles;
- calculate damage against real types;
- render effects;
- publish configuration;
- change compatibility;
- modify ProjectBaseline;
- create Unity objects.


## Evidence grades

- `ConfirmedByOfficialRuntimeSource`
- `ConfirmedByOfficialEditorSource`
- `ConfirmedByIndependentImplementation`
- `CommunityDocumented`
- `ObservedByFutureProjectBaselineAudit`
- `ConfiguredForProjectPolicy`
- `Unresolved`

没有完整公开的 RA2/YR 原版战斗运行时源码。官方 FinalSun/FinalAlert 2 只能提供编辑器、字段目录和 authoring 行为证据，不能替代 `gamemd.exe` 运行时证据。
