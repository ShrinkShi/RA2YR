# ProjectBaseline sanitized audit request

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明的实现仅作行为、冲突和架构参考，未复制、逐句翻译、机械改写或移植其采集、经济、AI、随机、寻路或测试代码。`code_imported: false`。

## 1. Status

This file designs a possible future read-only audit. The audit was **not run**. This research did not access ProjectBaseline.

Any future result is labeled only:

```text
ObservedByFutureProjectBaselineAudit
```

It cannot promote compatibility status by itself.

## 2. Selection basis

A future auditor may publish a non-identifying `SelectionBasis` describing:
- broad product/theater categories;
- broad scenario class;
- presence/absence strata;
- sampling method;
- deduplication category;
- input mode set.

It must not publish names, paths or identifiers.

## 3. Allowed public aggregates

Allowed:

```text
resource-related section presence
Overlay/resource-family aggregate counts
unknown-family count
resource-cell count range/bucket
stage/value shape and coarse buckets
missing/invalid data category counts
resource registry count/gap/duplicate categories
harvester/refinery binding status categories
capacity/value field presence
growth/spread capability counts
credits-source presence categories
storage-field presence
dock-count coarse bucket
diagnostic code counts
non-linkable aggregate hash
Memory/Stream/short-read/MIX equivalence
```

Additional safe shapes:
- min/max/median only when buckets cannot identify a map;
- broad `zero / positive-small / positive-medium / positive-large / invalid` buckets;
- anonymous product-profile compatibility category;
- aggregate conflict counts.

## 4. Forbidden public data

Forbidden:

```text
map names or paths
INI text or raw values
OverlayPack or OverlayDataPack arrays
resource-cell coordinates or coordinate sequences
resource-field layout
Overlay/type IDs
resource type names
exact stage, quantity or value
harvester/refinery names
exact capacities
exact credits
docking/exit coordinates
foundation or path topology
Trigger IDs, opcodes or parameters
AI team/script data
screenshots or rendered resource fields
resource asset names/content
hex, Base64 or byte excerpts
per-map/per-cell/per-object hashes
anything that can reconstruct map economy layout
```

Absolute paths, usernames and local machine information are also forbidden.

## 5. Audit queries

Safe query families:

1. Does each selected input expose resource-related sections?
2. Are Overlay and OverlayData input modes equivalent?
3. What broad resource-family categories bind?
4. How many unknown/ambiguous bindings occur in aggregate?
5. What coarse stage/value shapes occur?
6. Are harvester/refinery candidate fields present in broad categories?
7. Are growth/spread flags present?
8. How many distinct economy-source categories coexist?
9. Do diagnostics remain stable across input modes?
10. Do limits and no-progress guards terminate?

## 6. Hash rules

Allowed hashes:
- one non-linkable aggregate hash over already sanitized aggregate rows;
- randomized/salted audit-run namespace;
- no stable per-map key;
- no coordinate/type/value tuple in hash input.

Forbidden:
- raw file SHA;
- map-level SHA;
- per-cell hash;
- hash that permits dictionary attacks against known map files.

## 7. Minimum aggregation

Before publication:
- merge small buckets;
- suppress unique/rare combinations;
- remove exact maxima if identifying;
- round ranges;
- strip source identifiers;
- ensure no ordering reveals a coordinate sequence.

## 8. Input equivalence

The future audit may report only pass/fail/counts for:

```text
Memory
seekable Stream
short-read Stream
exact MIX window
```

No decoded bytes or entry names may be disclosed.

## 9. Diagnostics publication

Publish:
- diagnostic code;
- severity;
- aggregate count;
- broad stage/category.

Do not publish:
- message text containing names/coordinates;
- source span;
- raw numeric context;
- per-map grouping.

## 10. Operational constraints

The future auditor:
- is read-only;
- does not run Unity or game executables;
- creates no map;
- modifies no baseline;
- exports no asset;
- runs no harvesting/economy simulation;
- does not infer gameplay compatibility from aggregate success.

## 11. Compatibility boundary

Audit observations can:
- identify conflicts;
- prioritize P0 questions;
- refine test buckets;
- justify further public-source research.

They cannot:
- declare runtime compatibility;
- define vanilla behavior;
- replace official/runtime evidence;
- authorize code import;
- expose proprietary content.
