# RA2/YR Overlay pack storage and registry binding dossier

> **Research provenance:** This document was produced by ChatGPT Web from public sources. It did not read ProjectBaseline, is not a local Codex Agent artifact, and imports no code. GPL or unclear-license sources are reference-only; no source was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

## 1. Scope

This dossier studies the storage and interpretation boundary for Red Alert 2 / Yuri's Revenge map sections:

- `[OverlayPack]`;
- `[OverlayDataPack]`;
- their numbered Base64 fragments;
- the map chunk envelope and explicit Format80 profile;
- fixed 512 × 512 storage candidates;
- coordinate-to-array indexing;
- `[OverlayTypes]` ordinal binding;
- type-specific interpretation of the raw OverlayData byte;
- resource, wall, bridge, and unknown-overlay boundaries;
- strict parsing, provenance, roundtrip, and audit requirements.

It is a research and implementation-design artifact only. It does not implement a decoder, registry, renderer, simulation, or writer.

## 2. Frozen pipeline boundary

```text
lossless map INI
→ numbered fragment collector
→ strict Base64
→ Westwood chunk envelope
→ explicit OverlayFormat80Profile
→ exact decoded OverlayPack storage
→ exact decoded OverlayDataPack storage
→ coordinate/index view
→ composed Overlay registry binding
→ type-specific semantic adapter
→ rendering/simulation/pathfinding adapters
```

Layer ownership is strict:

- the Overlay reader does not parse INI syntax;
- the Format80 decoder does not know OverlayType identities;
- the array layer does not read Rules or Art;
- the registry binder never rewrites decoded bytes;
- semantic adapters never erase unknown OverlayData values;
- Core creates no Unity texture, tilemap, mesh, collider, GameObject, navigation graph, or world coordinate.

## 3. Primary conclusions

### 3.1 Two logically separate documents

`OverlayPack` and `OverlayDataPack` are separate numbered-fragment sections and separate compressed streams. The first carries a raw type identifier per storage cell; the second carries a raw data byte at the same storage index.

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert/FinalSun reads the two sections and decoded arrays separately | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Establishes official-editor behavior only, not a stock-runtime contract. | Preserve separate section and stream provenance. | `NotRun` |
| Several public tools also collect and decode the two sections separately | `Underconfirmed` | OpenRA, WAE, CNCMaps, MapTool | Cross-tool convergence is useful, but XCC/OpenRA ancestry and knowledge transfer prevent assuming independent discovery. | Keep the two results separate. | `NotRun` |
| A missing, failed, or wrong-length stream is never synthesized from its partner | `DefensiveDesign` | Project policy | This is a fail-closed preservation rule, not an external runtime fact. | Return separate structured failure and preserve the successful partner result without fabricating data. | `NotRun` |

### 3.2 Storage length

For the ordinary one-byte profile, the storage candidate is exactly:

```text
512 × 512 = 262144 bytes
```

EA's published editor uses an explicit 262144-byte array. OpenRA, CNCMaps, MapTool, and WAE also allocate the same one-byte storage size, but this convergence does not prove independent implementation lineages or stock-runtime universality.

WAE also supports an extension profile where `OverlayPack` uses two bytes per cell when `NewINIFormat >= 5`, while `OverlayDataPack` remains one byte per cell. This is `ImplementationSpecificBehavior` for a named extension profile and must not be conflated with the ordinary vanilla candidate.

Exact-length validation, and refusal to pad, clamp, truncate, or report partial success, are `DefensiveDesign` project requirements.

### 3.3 Coordinate indexing

The dominant external-coordinate candidate is:

```text
index = X + 512 × Y
```

OpenRA, WAE, CNCMaps, MapTool, and ModEnc use or document this form. Its formal grade remains `Underconfirmed`: the implementations are not proven to be independent and no original-runtime source establishes it as the unique contract.

EA's editor maps its square internal field array through `internalY + 512 × internalX`. That internal mapping is `ConfirmedByOfficialToolSource`. The difference creates a `ConflictingSources` classification for any claim that one unique runtime-facing axis/index contract has already been established. The project retains explicit coordinate profiles and never selects one because it happens to fall inside map bounds.

### 3.4 Empty value

For the ordinary byte profile, FinalAlert/FinalSun's treatment of `0xFF` as no Overlay is `ConfirmedByOfficialToolSource`. Its widespread tool/community use is a `ConfirmedCommunityConvention`; stock-runtime exclusivity remains `Underconfirmed` because no runtime source was found.

`0x00` remains the first possible registry ordinal, and `0xFE` can remain an ordinal candidate when the composed registry contains it. Unknown values are not automatically empty cells.

### 3.5 Registry binding

A raw type byte is interpreted against a composed `[OverlayTypes]` registry. The zero-based ordinal model is a `ConfirmedCommunityConvention` supported by tool behavior, while exact stock-runtime gap, duplicate, case, and map-local composition behavior remains `Underconfirmed` or `Unresolved` as documented in the registry dossier.

Numeric ordinal identity, gaps, duplicate ordinals, case, source layers, map-local changes, winners, and suppressed candidates remain visible. Refusing to renumber because Art or resources are missing is `DefensiveDesign`.

### 3.6 OverlayData is not globally one semantic

The format-wide storage fact is one raw byte in the ordinary profile. `FrameIndex` is a stable community/tool candidate, not a universal runtime fact. Resources, connected walls, bridges, and extension families require separate evidence-bearing semantic profiles. Unknown values remain raw under `DefensiveDesign`.

## 4. Formal evidence grades

Every formal `Grade` field uses exactly one value from this closed vocabulary:

- `ConfirmedByOriginalRuntimeSource`
- `ConfirmedByOfficialToolSource`
- `ConfirmedByMultipleIndependentImplementations`
- `ConfirmedCommunityConvention`
- `ImplementationSpecificBehavior`
- `DefensiveDesign`
- `ConflictingSources`
- `Underconfirmed`
- `Unresolved`

`ConfirmedByOriginalRuntimeSource` is reserved for the original RA2/YR runtime or its actual source. No reviewed claim in this dossier currently has that grade.

`ConfirmedByOfficialToolSource` applies to FinalAlert, FinalSun, and other official editor/tool behavior. Official-tool evidence does not establish original-runtime behavior.

`ConfirmedByMultipleIndependentImplementations` requires demonstrably independent implementation lineages. XCC, FinalAlert's bundled XCC code, OpenRA and derived lineages, and implementations influenced by the same community knowledge are not counted repeatedly.

`ConfirmedCommunityConvention` records stable community or toolchain convention without promoting it to runtime fact. One named implementation is `ImplementationSpecificBehavior`; uncertain convergence is `Underconfirmed`; direct source disagreement is `ConflictingSources`.

Project choices such as exact-length enforcement, raw preservation, explicit profiles, refusal to guess, and fail-closed behavior are `DefensiveDesign`. Policy details belong in `Policy` or `PolicyClassification`, not in the evidence grade.

Future ProjectBaseline work is recorded as:

```text
AuditStatus: NotRun
FutureEvidenceSource: ProjectBaselineAggregateAudit
```

It is not an evidence grade and does not imply that ProjectBaseline was read.

## 5. Strict project defaults

Until stronger evidence resolves remaining conflicts, the design recommends:

- explicit one-byte versus extended OverlayPack profiles;
- exact decoded-length validation;
- no clamp, padding, truncation, or partial success;
- no automatic Format80 variant probing;
- no trial axis swap;
- raw preservation for all 262144 storage positions;
- separate diagnostics for the two sections;
- preservation of unknown type/data pairs and map-domain-external cells;
- structured ambiguity for registry and semantic conflicts;
- no default writer that destroys compressed, fragment, domain-external, or unknown data.

## 6. Deliverables

The directory contains 14 research documents:

1. `README.md`
2. `layer-and-section-boundaries.md`
3. `packed-array-layout.md`
4. `coordinate-indexing-and-map-domain.md`
5. `overlay-type-registry-binding.md`
6. `overlay-data-semantics.md`
7. `resource-overlay-boundaries.md`
8. `wall-bridge-and-state-boundaries.md`
9. `format80-profile-and-length-contract.md`
10. `source-comparison.md`
11. `implementation-boundaries.md`
12. `test-matrix.md`
13. `baseline-audit-request.md`
14. `unresolved-questions.md`

## 7. Explicit non-goals

This research does not:

- implement fragment collection, Base64, chunk parsing, Format80, Overlay reading, typed registries, rendering, resources, walls, bridges, or pathfinding;
- access ProjectBaseline or any user-local content;
- run Unity, RA2/YR, FinalAlert, XCC, or another editor;
- create or modify maps;
- modify tests, compatibility status, ADRs, formal third-party records, `.dev-records`, existing research, PR #25, PR #28, or any Codex branch;
- claim community convention or official-editor behavior as original-runtime source evidence.
