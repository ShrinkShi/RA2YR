# Recommended implementation boundaries

> **Research provenance:** This document was produced by ChatGPT Web from public sources. It did not read ProjectBaseline, is not a local Codex Agent artifact, and imports no code. GPL or unclear-license sources are reference-only; no source was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

## 1. Architectural objective

The implementation should preserve exact source and decoded data while allowing evidence-gated semantic interpretation. No layer may silently repair another layer's input.

```text
lossless map INI
→ PackedIniFragmentCollector
→ strict Base64
→ map chunk envelope
→ Format80 backend under explicit OverlayFormat80Profile
→ OverlayArrayRaw documents
→ coordinate/index views
→ Overlay registry binding
→ type-specific semantic profiles
→ upper adapters
```

## 2. Candidate Core models

### 2.1 `OverlayPackDocument`

Owns the paired research/runtime view without conflating the streams.

Candidate fields:

- `TypeSectionSource`;
- `DataSectionSource`;
- `TypeCompressedDocument`;
- `DataCompressedDocument`;
- `TypeArray`;
- `DataArray`;
- `StorageProfile`;
- `CoordinateProfile`;
- `ConsistencyAnalysis`;
- diagnostics and provenance.

It can exist when only one array succeeds.

### 2.2 `OverlayArrayRaw`

- section kind;
- element width;
- exact decoded bytes;
- declared/actual/expected lengths;
- missing/trailing counts;
- chunk and codec trace;
- source provenance;
- evidence grade.

### 2.3 `OverlayStorageCoordinate`

A named storage-domain pair with checked range 0..511. It is not an IsoMap coordinate, object cell ID, screen point, or Unity vector.

### 2.4 `OverlayCoordinateIndex`

- selected profile;
- storage coordinate;
- element index;
- element width;
- conversion trace;
- evidence grade.

### 2.5 Raw scalar wrappers

- `OverlayTypeRaw`;
- `OverlayDataRaw`.

Wrappers prevent accidental interchange and retain profile width.

### 2.6 Registry models

- `OverlayRegistryDescriptor`;
- `OverlayRegistryEntry`;
- `OverlayTypeBindingResult`;
- `OverlayTypeResolutionTrace`.

They consume a completed composed INI view and do not perform discovery or composition.

### 2.7 Semantic models

- `OverlaySemanticProfile`;
- `OverlaySemanticResult`;
- profile-specific derived candidate objects;
- evidence and source pins;
- diagnostics;
- raw-to-derived trace.

### 2.8 Analysis models

- `OverlayArrayConsistencyAnalysis`;
- `OverlayMapDomainAnalysis`;
- per-category aggregate summaries for audits.

## 3. Explicit policies

All project-selected policies in this section are `DefensiveDesign`. That grade records fail-closed, preservation, explicit-profile, and no-guessing decisions; it does not assert external runtime behavior.

### `OverlayArrayLengthPolicy`

Selects ordinary 262144-byte, extended type-array, or future profiles. It defines exact success, never hidden padding.

### `OverlayCoordinatePolicy`

Selects external row-major, official-editor-transposed comparison, or another named transform. No automatic trial swap.

### `OverlayEmptyTypePolicy`

Defines the sentinel for the selected element width, such as `0xFF` or explicit `0xFFFF` candidate.

### `OverlayRegistryPolicy`

Defines numeric-key parsing, case comparison, gap/duplicate handling, map-local eligibility, and extension scope.

### `OverlayDataSemanticPolicy`

Maps bound type families to candidate semantic profiles and defines ambiguity handling.

### `OverlayUnknownTypePolicy`

Controls whether unknown types are report-only, fatal for typed view, or allowed as opaque records. It never rewrites raw bytes.

### `OverlayDomainPolicy`

Defines which domain analyses are required and whether domain-external typed records block an upper adapter.

### `OverlayRoundtripPolicy`

Distinguishes source-preserving, compressed-preserving, decoded-preserving, semantic, and canonical rewrite modes.

## 4. Layer responsibilities

### Lossless INI

Owns source spelling, duplicate sections/keys, comments, whitespace, and line order.

### Fragment collector

Owns numbered-key normalization, ordering candidates, duplicate normalized numbers, and fragment budgets.

### Base64

Owns alphabet, padding, whitespace policy, exact input, and output budget.

### Chunk envelope

Owns size headers, payload windows, block count, aggregate lengths, and sentinel policy.

### Format80 backend

Owns command decoding under the selected profile only.

### Overlay array layer

Owns exact storage-length validation and raw bytes. It knows no Rules or Art.

### Coordinate layer

Owns storage coordinate/index conversion and domain analysis. It does not bind types.

### Registry binder

Owns raw ordinal to composed registry resolution. It does not load images or infer IDs from assets.

### Semantic adapter

Owns type-specific interpretation candidates. It never mutates raw bytes.

### Rendering/simulation/pathfinding adapters

Consume completed binding/semantic results and add theater, Art, world, ownership, health, movement, and Unity-specific behavior.

## 5. Evidence and policy separation

Formal evidence grades use the closed vocabulary defined in `README.md` and `source-comparison.md`.

- official-editor behavior is `ConfirmedByOfficialToolSource`;
- named public implementation behavior is `ImplementationSpecificBehavior`;
- uncertain cross-tool convergence is `Underconfirmed`;
- stable community convention is `ConfirmedCommunityConvention`;
- direct disagreement is `ConflictingSources`;
- project preservation and safety contracts are `DefensiveDesign`;
- missing reliable candidates remain `Unresolved`.

No implementation success, plausible output, Art lookup, or current registry binding can upgrade a claim to `ConfirmedByOriginalRuntimeSource`.

## 6. Diagnostics

Diagnostics should be structured by layer and include:

- stable code;
- severity;
- section kind;
- source provenance;
- compressed/chunk/decoded offsets where safe;
- storage coordinate/index where applicable;
- selected profile and evidence grade;
- suppressed interpretations;
- budget involved;
- no raw copyrighted content in public audit output.

Examples:

- `OverlaySectionMissing`;
- `OverlayFragmentOrdinalConflict`;
- `OverlayBase64Invalid`;
- `OverlayChunkLengthMismatch`;
- `OverlayFormat80Failure`;
- `OverlayArrayLengthMismatch`;
- `OverlayCoordinateProfileAmbiguous`;
- `OverlayStorageCoordinateOutOfRange`;
- `OverlayRegistryOrdinalMissing`;
- `OverlayRegistryOrdinalDuplicate`;
- `OverlayUnknownType`;
- `OverlaySemanticProfileMissing`;
- `OverlayEmptyTypeHasData`;
- `OverlayScenarioDomainMismatch`.

## 7. Limits

Candidate `OverlayPackReadLimits` fields:

- maximum section occurrences;
- maximum fragments per section;
- maximum total Base64 characters;
- maximum compressed bytes;
- maximum chunks;
- maximum compressed bytes per chunk;
- maximum output per chunk;
- maximum aggregate output;
- maximum Format80 commands;
- maximum diagnostics;
- maximum registry entries and duplicate candidates;
- maximum semantic profile candidates;
- maximum domain anomalies retained in detailed form.

The ordinary profile can require 262144 bytes without permitting arbitrary file-driven allocation.

## 8. Arithmetic and progress

All offset, length, element-width, and coordinate calculations use checked arithmetic.

Every parser loop must:

- consume input;
- produce bounded output;
- advance a record/index;
- or return a structured failure.

No no-progress loop or file-driven unbounded enumeration is allowed.

## 9. Input-mode equivalence

Memory, seekable Stream, short-read Stream, and MIX-window input must use one parsing state machine and produce equivalent:

- raw arrays;
- diagnostics;
- provenance;
- hashes;
- binding results.

The MIX window supplies bytes and provenance only. It does not choose Format80 profile, coordinate profile, or global priority.

## 10. Fixture independence

Synthetic fixture builders must not reuse production:

- Format80 command formulas;
- coordinate sorter/indexer;
- registry builder;
- semantic profile selector.

Fixtures should specify clean-room literal bytes, separately calculated expected indices, and expected model hashes. Here, independence describes fixture construction from production code, not source-lineage evidence.

## 11. Roundtrip design

Default read-only Core preserves enough for future decisions. It does not expose a default writer.

Potential future writer modes:

- exact source re-emission when nothing changed;
- exact compressed-byte reuse with outer INI relocation;
- decoded-array canonical recompression;
- semantic editor rewrite;
- explicit cleanup profile.

Each mode has a different guarantee and must not be called simply “roundtrip.”

## 12. Unity boundary

Core has no dependency on `UnityEngine`. Unity adapters may later convert bound results to textures, meshes, tilemaps, colliders, or navigation, but those outputs are disposable derived views and cannot become source truth.

## 13. Maintainability rules

- centralize storage and priority profiles;
- keep sorting/index rules in one component;
- serialize every selected profile and evidence grade;
- preserve all overridden and ambiguous candidates;
- make input enumeration order irrelevant after normalization;
- keep discovery, INI composition, codec, array, binding, and semantic logic separate;
- do not scatter magic values `512`, `262144`, `0xFF`, or `0xFFFF` outside profile descriptors;
- do not place global priority inside MIX or Format80 readers;
- do not let typed Rules/Art views read raw packed sections.

```text
AuditStatus: NotRun
FutureEvidenceSource: ProjectBaselineAggregateAudit
```
