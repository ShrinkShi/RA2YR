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

### 3.1 Two independent logical documents

`OverlayPack` and `OverlayDataPack` are separate numbered-fragment sections and separate compressed streams. The first carries a raw type identifier per storage cell; the second carries an independent raw data byte at the same storage index. A missing section, failed stream, or length mismatch is not repaired from its partner.

Evidence grade: `ConfirmedByOfficialEditorSource` plus multiple implementation observations.

### 3.2 Storage length

For the ordinary one-byte profile, the strongest candidate is exactly:

```text
512 × 512 = 262144 bytes
```

EA's published editor uses an explicit 262144-byte array. OpenRA, CNCMaps, MapTool, and WAE independently allocate the same one-byte storage size. Some tools prefill short output; that tolerance is editor/tool behavior and is not the strict project default.

WAE also supports an extension profile where `OverlayPack` uses two bytes per cell when `NewINIFormat >= 5`, while `OverlayDataPack` remains one byte per cell. This must not be silently conflated with the vanilla RA2/YR one-byte profile.

### 3.3 Coordinate indexing

The dominant external-coordinate candidate is:

```text
index = X + 512 × Y
```

OpenRA, WAE, CNCMaps, MapTool, and ModEnc use or document this form. EA's editor maps its square internal field array through `internalY + 512 × internalX`, exposing an axis/transposition conflict in naming or representation. The project therefore requires an explicit coordinate profile and never chooses an axis order because one interpretation happens to fall inside map bounds.

### 3.4 Empty value

For the ordinary byte profile, `0xFF` is a strong no-overlay sentinel. `0x00` remains the first possible registry ordinal. Values not found in the composed registry are unknown types, not automatically empty cells.

### 3.5 Registry binding

A raw type byte is interpreted against a composed `[OverlayTypes]` registry. Numeric ordinal identity, gaps, duplicate ordinals, case, source layers, map-local changes, and suppressed candidates must remain visible. Section enumeration order and asset availability may not renumber the registry.

### 3.6 OverlayData is not globally one semantic

The only format-wide fact is one raw byte. Community tools often call it a frame index, but resources, connected walls, bridges, and hardcoded families can assign different or coupled meanings. Interpretation requires an `OverlaySemanticProfile` selected from the bound type and an evidence-bearing policy.

## 4. Evidence grades

Every conclusion uses one of:

- `ConfirmedByOfficialRuntimeSource`;
- `ConfirmedByOfficialEditorSource`;
- `ConfirmedByIndependentImplementation`;
- `CommunityDocumented`;
- `ObservedByFutureProjectBaselineAudit`;
- `ConfiguredForProjectPolicy`;
- `Unresolved`.

No reviewed public source qualifies as official RA2/YR runtime source. EA's repository is official editor source, not the game runtime.

## 5. Strict project defaults

Until golden evidence resolves remaining conflicts, the design recommends:

- explicit one-byte versus extended OverlayPack profiles;
- exact decoded-length validation;
- no clamp, padding, truncation, or partial success;
- no automatic Format80 variant probing;
- no trial axis swap;
- raw preservation for all 262144 storage positions;
- independent diagnostics for the two sections;
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
- claim community convention or official-editor behavior as official runtime source evidence.