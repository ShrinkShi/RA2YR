# M2-R3 — RA2/YR content discovery, archive precedence, and INI composition

> **Source notice / 来源声明:** Prepared by **ChatGPT Web** from pinned public sources and repository context. No local ProjectBaseline, original game directory, loose asset, archive entry body, or INI body was read. This is a research and implementation-design dossier, not code.

## 1. Scope

This dossier separates four concerns that are often conflated:

1. root-level content-provider discovery;
2. explicit mounting of known nested MIX archives;
3. deterministic virtual-file candidate ordering;
4. semantic composition of ordered INI documents.

It targets Red Alert 2 / Yuri's Revenge legacy content while leaving room for later modern packages, user mods, high-resolution images, models, PBR materials, and dynamic-lighting assets.

## 2. Frozen repository basis

- Repository: `ShrinkShi/RA2YR`
- Base branch: `main`
- Base commit at task start: `62de09d0822f3da4fac7e8ed863b8812a0979a5f`
- Research branch: `research/m2-ra2yr-content-load-order`

The project configuration supplies exactly one authoritative runtime root. The research documents refer to it symbolically as `ConfiguredRuntimeRoot` and intentionally do not publish an absolute local path.

The following are permanently excluded as runtime providers:

- FinalAlert 2 / FinalSun tool directories;
- tutorials, code dictionaries, and research references;
- XCC hand-export directories;
- unpacked mirrors such as `YR1001_Unpacked`;
- `Cache`;
- other game installations or incidental copies.

## 3. Corrected project policy

The baseline **ConfiguredProjectPolicy**, from low to high priority, is:

```text
ra2.mix
→ ra2md.mix
→ expandmd01.mix
→ expandmd02.mix
→ ...
→ expandmd99.mix
→ loose files
```

Missing sequence numbers do not stop discovery. Filesystem enumeration order must not affect the normalized result.

This ordering has two different consumers.

### Ordinary binary/resource lookup

For SHP, VXL, HVA, audio, maps, and other whole-file resources, the resolver may select one highest-priority candidate while retaining all suppressed candidates and provenance in `ContentResolutionTrace`.

### INI logical-document lookup

For INI documents, discovery does **not** discard lower same-named candidates. It returns an ordered layer sequence. The INI resolver then performs semantic composition:

```text
Content discovery
→ ordered logical-document layers
→ lossless INI documents
→ semantic composition policy
→ effective typed views
```

Identity is `(SectionName, KeyName)`. A higher layer replaces the lower value for the same identity; absent keys and sections remain inherited; new keys and sections are added. Every effective key retains its winner and suppressed candidate chain.

There is no whole-file winner for a configured composable INI document.

## 4. Evidence labels

Every conclusion uses one of:

- `ConfirmedByMultipleIndependentImplementations`
- `ConfirmedByOriginalOrOfficialSource`
- `ConfirmedCommunityConvention`
- `ConfiguredProjectPolicy`
- `ImplementationSpecificBehavior`
- `Underconfirmed`
- `ConflictingSources`
- `Unresolved`

Multiple reimplementations agreeing does not automatically establish original game behavior.

## 5. High-confidence boundaries

- A MIX parser reads an individual archive. It does not decide global load order.
- Discovery, mounting, query, and INI composition are separate components.
- Root discovery is not arbitrary recursive discovery of every entry that parses as MIX.
- Known child archives such as local/cache/theater packages require explicit family descriptors or mount edges.
- `expand*`, `ecache*`, and `elocal*` are separate archive families and must not share one guessed numeric ordering rule.
- The project must normalize candidates independently of filesystem enumeration order.
- Generic file resolution and INI semantic composition are distinct policies.
- Map/mode overlays and Ares/Phobos include/inheritance features are additional semantic layers, not proof of vanilla cross-MIX behavior.
- All winning and suppressed provenance remains serializable and auditable.

## 6. Documents

1. [archive-family-boundaries.md](archive-family-boundaries.md)
2. [root-archive-discovery.md](root-archive-discovery.md)
3. [numbered-expansion-order.md](numbered-expansion-order.md)
4. [virtual-file-resolution.md](virtual-file-resolution.md)
5. [nested-mix-boundaries.md](nested-mix-boundaries.md)
6. [ini-resolution-implications.md](ini-resolution-implications.md)
7. [source-comparison.md](source-comparison.md)
8. [implementation-boundaries.md](implementation-boundaries.md)
9. [test-matrix.md](test-matrix.md)
10. [baseline-audit-request.md](baseline-audit-request.md)
11. [unresolved-questions.md](unresolved-questions.md)

## 7. Non-goals

This research does not:

- implement C#;
- run Unity or the original executable;
- read ProjectBaseline;
- inspect original archive entries or INI bodies;
- modify compatibility status or third-party ledgers;
- claim community conventions are original source facts;
- hard-code a long `expandmdXX.mix` if/else chain;
- merge or auto-merge its PR.
