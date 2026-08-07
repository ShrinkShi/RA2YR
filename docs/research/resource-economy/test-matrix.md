# Test matrix — 174 research cases

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明的实现仅作行为、冲突和架构参考，未复制、逐句翻译、机械改写或移植其采集、经济、AI、随机、寻路或测试代码。`code_imported: false`。

## Rules

This file designs tests only; no C#/Unity test is implemented. Expected values use hand-written fixtures and must not reuse production stage, quantity, value, reservation, docking, growth/spread, RNG or economy-precedence logic. Every result records policy/profile/evidence. Memory, seekable Stream, short-read Stream and exact MIX-window inputs must be equivalent.

| Prefix | Category | Count |
|---|---|---:|
| RF | Overlay/resource family | 28 |
| RV | resource types/stages/value | 28 |
| HC | harvester capacity/cargo | 24 |
| CT | collection/target/reservation | 24 |
| RD | refinery/docking/unloading | 26 |
| GS | growth/spread/depletion | 22 |
| ES | credits/storage/AI/presentation/safety/audit | 22 |
| **Total** | | **174** |

## RF — Overlay/resource family (28)

**Category invariant:** Assert raw Overlay/Data, family candidates, profile, evidence and diagnostics are retained; no synthesis, deletion or global OverlayData meaning.

- `RF-01` — empty Overlay sentinel with absent data
- `RF-02` — empty Overlay with nonzero data
- `RF-03` — ore family candidate from ordinary registry
- `RF-04` — gems family candidate
- `RF-05` — TS green Tiberium under TS profile
- `RF-06` — TS blue Tiberium under TS profile
- `RF-07` — TS family rejected as automatic YR default
- `RF-08` — veins kept separate from ore
- `RF-09` — crate kept separate from resource
- `RF-10` — debris/rock kept separate from resource
- `RF-11` — wall connection data not treated as quantity
- `RF-12` — bridge state data not treated as quantity
- `RF-13` — rail/tunnel family not treated as resource
- `RF-14` — unknown Overlay ordinal retained
- `RF-15` — extension resource with explicit provider
- `RF-16` — extension resource without provider remains unresolved
- `RF-17` — missing OverlayData stream
- `RF-18` — short OverlayData stream
- `RF-19` — Overlay present but data element unavailable
- `RF-20` — Art missing while logical resource retained
- `RF-21` — duplicate resource family bindings
- `RF-22` — hardcoded editor range conflicts with registry
- `RF-23` — theater/control candidate conflicts with registry
- `RF-24` — map-local registry override provenance
- `RF-25` — storage coordinate boundary zero
- `RF-26` — storage coordinate boundary 511
- `RF-27` — out-of-domain storage-to-cell mapping
- `RF-28` — source order deterministic across duplicate candidates

## RV — resource types/stages/value (28)

**Category invariant:** Assert registry/raw numeric spelling and provenance survive; stage, quantity and value remain separate; missing/zero/negative/overflow are not repaired.

- `RV-01` — empty [Tiberiums] registry
- `RV-02` — registry gap preserved
- `RV-03` — duplicate registry ordinal
- `RV-04` — duplicate resource type section
- `RV-05` — case-colliding type identities
- `RV-06` — unknown resource property retained
- `RV-07` — missing resource section
- `RV-08` — missing Image/Overlay reference
- `RV-09` — stage zero preserved
- `RV-10` — stage maximum profile boundary
- `RV-11` — stage above profile maximum
- `RV-12` — stage byte 255 preserved
- `RV-13` — stage interpretation conflict frame vs quantity
- `RV-14` — ore and gems use distinct stage profiles
- `RV-15` — editor OverlayData+1 candidate recorded
- `RV-16` — editor estimate not promoted to runtime
- `RV-17` — missing Value
- `RV-18` — explicit zero Value
- `RV-19` — negative Value
- `RV-20` — very large Value
- `RV-21` — quantity times Value overflow
- `RV-22` — fractional/text Value retained raw
- `RV-23` — map-local Value override provenance
- `RV-24` — duplicate Value keys
- `RV-25` — Growth/Spread fields profile-scoped
- `RV-26` — PipIndex presentation-only
- `RV-27` — TS fields rejected as unconditional YR semantics
- `RV-28` — roundtrip exact numeric spelling

## HC — harvester capacity/cargo (24)

**Category invariant:** Assert type capability, authored capacity, runtime cargo, economic value and UI remain separate; binder performs no cargo mutation.

- `HC-01` — type name HARV without capability field
- `HC-02` — explicit harvester capability without name heuristic
- `HC-03` — missing Storage capacity
- `HC-04` — zero capacity
- `HC-05` — negative capacity raw candidate
- `HC-06` — capacity maximum configured boundary
- `HC-07` — capacity arithmetic overflow
- `HC-08` — capacity unit unresolved
- `HC-09` — empty cargo snapshot
- `HC-10` — partial single-resource cargo
- `HC-11` — full cargo
- `HC-12` — over-capacity cargo
- `HC-13` — mixed ore/gem cargo
- `HC-14` — unsupported resource cargo entry
- `HC-15` — duplicate cargo type entry
- `HC-16` — cargo sum overflow
- `HC-17` — cargo economic value separate
- `HC-18` — fullness rational exactness
- `HC-19` — UI fraction clamps display only
- `HC-20` — pip count not authoritative
- `HC-21` — art load frame not authoritative
- `HC-22` — script-created partial cargo
- `HC-23` — savegame cargo distinct from map placement
- `HC-24` — no cargo mutation in binder

## CT — collection/target/reservation (24)

**Category invariant:** Assert declarative target/approach/reservation/command/result output, deterministic ordering and no resource/cargo/path mutation.

- `CT-01` — target cell contains accepted resource
- `CT-02` — target cell contains unaccepted resource
- `CT-03` — target already depleted
- `CT-04` — target quantity unknown
- `CT-05` — harvester already full
- `CT-06` — harvester zero-capacity target query
- `CT-07` — target on non-ground layer candidate
- `CT-08` — adjacent approach candidate
- `CT-09` — same-cell collection profile
- `CT-10` — required facing unresolved
- `CT-11` — blocked approach
- `CT-12` — path failure leaves resource unchanged
- `CT-13` — reservation granted
- `CT-14` — reservation conflict
- `CT-15` — reservation partial amount
- `CT-16` — reservation expiration
- `CT-17` — actor destroyed releases reservation candidate
- `CT-18` — simultaneous harvesters stable tie-break
- `CT-19` — hash iteration cannot affect winner
- `CT-20` — collection amount exceeds cell quantity
- `CT-21` — collection amount exceeds cargo space
- `CT-22` — partial collection transaction
- `CT-23` — animation frame cannot trigger mutation
- `CT-24` — Unity deltaTime excluded from authority

## RD — refinery/docking/unloading (26)

**Category invariant:** Assert refinery capability, slot/queue, cargo/storage/credit mutation and presentation remain separate; no docking/unload execution.

- `RD-01` — type name PROC without refinery capability
- `RD-02` — explicit refinery capability
- `RD-03` — refinery accepts resource type
- `RD-04` — refinery rejects resource type
- `RD-05` — missing dock descriptor
- `RD-06` — single dock
- `RD-07` — multiple docks deterministic order
- `RD-08` — duplicate dock slot
- `RD-09` — dock cell distinct from foundation
- `RD-10` — exit cell distinct from foundation
- `RD-11` — blocked approach
- `RD-12` — occupied dock queue
- `RD-13` — allied docking candidate
- `RD-14` — enemy docking rejected/unknown by policy
- `RD-15` — refinery captured during approach
- `RD-16` — refinery destroyed during approach
- `RD-17` — harvester destroyed while queued
- `RD-18` — power lost during docking
- `RD-19` — zero cargo unload
- `RD-20` — partial cargo unload
- `RD-21` — mixed cargo unload
- `RD-22` — storage full reject candidate
- `RD-23` — storage full partial accept candidate
- `RD-24` — direct-cash vs physical-storage profiles
- `RD-25` — credit conversion overflow
- `RD-26` — save/load mid-unload state

## GS — growth/spread/depletion (22)

**Category invariant:** Assert capability/event candidates only, serialized deterministic policy, no RNG execution and no raw-map writeback.

- `GS-01` — growth disabled
- `GS-02` — growth enabled metadata only
- `GS-03` — spread disabled
- `GS-04` — spread enabled metadata only
- `GS-05` — RA2 editor ore label retained as editor evidence
- `GS-06` — missing Growth field
- `GS-07` — zero Growth interval
- `GS-08` — negative Growth raw
- `GS-09` — GrowthPercentage out of range
- `GS-10` — SpreadPercentage out of range
- `GS-11` — maximum-stage growth no-op candidate
- `GS-12` — growth on blocked cell
- `GS-13` — growth on water profile conflict
- `GS-14` — spread target outside map
- `GS-15` — spread target occupied by building
- `GS-16` — spread into different resource type
- `GS-17` — deterministic candidate ordering
- `GS-18` — serialized RNG state
- `GS-19` — Unity Random forbidden
- `GS-20` — quantity reaches zero
- `GS-21` — depleted target invalidation
- `GS-22` — runtime depletion leaves raw map unchanged

## ES — credits/storage/AI/presentation/safety/audit (22)

**Category invariant:** Assert economy-source provenance/conflicts, input equivalence, safety limits, no UnityEngine and no runtime mutation.

- `ES-01` — House credits candidate
- `ES-02` — Basic carry-over candidate
- `ES-03` — carry-over cap conflict
- `ES-04` — lobby starting credits override candidate
- `ES-05` — game-mode override candidate
- `ES-06` — multiple sources retain provenance
- `ES-07` — session parser does not choose final credits
- `ES-08` — refinery delivery distinct from starting credits
- `ES-09` — crate credits distinct from resource cell
- `ES-10` — Trigger credit opcode retained non-executable
- `ES-11` — AI resource estimate distinct from credits
- `ES-12` — building storage distinct from cargo
- `ES-13` — physical stored resource distinct from cash
- `ES-14` — storage capacity removal below contents
- `ES-15` — UI load bar project policy
- `ES-16` — resource movement cost separate from quantity
- `ES-17` — Memory input equivalence
- `ES-18` — seekable Stream equivalence
- `ES-19` — short-read Stream equivalence
- `ES-20` — exact MIX-window equivalence
- `ES-21` — budgets and no-progress termination
- `ES-22` — noEngineReferences and no Unity/runtime mutation

## Cross-cutting assertions

All applicable cases also verify checked arithmetic, bounded counts, exact MIX-window reads, short-read handling, no-progress termination, deterministic source/transaction ordering, lossless raw roundtrip, structured diagnostics, `noEngineReferences`, no Unity objects, and no Trigger/AI/harvesting/docking/growth/spread/pathfinding execution.

Synthetic fixtures use fictional resource IDs and tiny arrays. The oracle must not call production family, stage, quantity, value, cargo, target, reservation, dock, unload, RNG or economy-precedence functions.
