> **Source notice:** This research was prepared by **ChatGPT Web** from public sources. It did not read the local ProjectBaseline and is not the product of the local Codex Agent. No GPL or unclear-license code was copied, translated, mechanically rewritten, or imported (`code_imported: false`).

# M3-R4 — IsoMapPack5 records, coordinates, and theater binding

## Scope

This dossier narrows the already documented MAP/compression and TMP/theater work to the exact boundary between an **exact decoded IsoMapPack5 byte stream** and a **theater-aware terrain-cell binding**.

```text
lossless map INI
→ numbered fragment collection
→ strict Base64
→ Westwood chunk envelope
→ raw LZO1X-compatible backend
→ exact decoded byte stream
→ IsoMapPack5 record reader
→ raw map-cell records
→ coordinate validation/indexing
→ theater tile registry binding
→ rendering/simulation adapters
```

This branch does not implement any stage.

## Strongest conclusions

1. The convergent record width is **11 bytes**.
2. Offsets `0..3`, `8`, and `9` have strong cross-source agreement as X, Y, SubTile, and Level fields, while signedness and runtime constraints remain evidence-gated.
3. Offsets `4..7` are a material conflict:
   - EA FinalSun/FinalAlert 2 and XCC model a 16-bit tile field plus two raw/zero bytes;
   - World-Altering Editor, CNCMaps, and MapTool model one 32-bit tile field.
4. Byte `10` is preserved raw. `IceGrowth` is a strong community/tool interpretation, especially for TS snow maps, but no official runtime source was found.
5. The common dense canvas count is `(2 × Width - 1) × Height`, but sparse streams are intentionally written by modern tools and community documentation says omitted level-0 clear cells are synthesized by the game. These are distinct evidence grades.
6. Record order is not identity. Coordinates are identity candidates; source order must still be preserved for forensic and lossless work.
7. Duplicate coordinates are ambiguous by project policy. The Core must not silently use first-wins or last-wins.
8. A decoded four-byte zero trailer is **not universal**:
   - WAE and several downstream tools write or expect it;
   - the official editor writer passes `recordCount × 11` bytes to its encoder and its reader divides total decoded length by 11, tolerating a remainder.
9. Chunk-envelope termination and decoded-stream trailing bytes are separate layers.
10. Tile resolution must preserve the raw 32-bit view and the low/high 16-bit views until an explicit evidence-gated policy selects an interpretation.

## Evidence grades

Every formal evidence-grade field uses exactly one value from this closed vocabulary:

- `ConfirmedByOriginalRuntimeSource`
- `ConfirmedByOfficialToolSource`
- `ConfirmedByMultipleIndependentImplementations`
- `ConfirmedCommunityConvention`
- `ImplementationSpecificBehavior`
- `DefensiveDesign`
- `ConflictingSources`
- `Underconfirmed`
- `Unresolved`

`ConfirmedByOriginalRuntimeSource` is reserved for evidence from the original game runtime or its actual source. No finding in this dossier currently has that grade.

`ConfirmedByOfficialToolSource` applies to FinalAlert, FinalSun, and other official editor/tool behavior. Official-tool evidence does not establish original-runtime behavior.

`ConfirmedByMultipleIndependentImplementations` requires demonstrably independent implementation lineages. XCC, FinalAlert's bundled XCC-derived code, OpenRA/openra2 XCC descendants, and implementations that acknowledge shared lineage are not counted repeatedly.

`ConfirmedCommunityConvention` records a stable community or toolchain convention without promoting it to runtime fact. A single implementation is `ImplementationSpecificBehavior`; uncertain cross-source convergence is `Underconfirmed`; direct disagreement is `ConflictingSources`.

Project choices such as preserving raw views, refusing repair, and failing closed are `DefensiveDesign`. Policy details belong in a separate `Policy` field. Future ProjectBaseline work is recorded with `AuditStatus` and `FutureEvidenceSource`, not as an evidence grade; the current status is `NotRun`.

## Project policies proposed for implementation planning

- preserve all 11 raw bytes;
- expose both 32-bit and split-16 tile views without masking;
- reject or diagnose incomplete records instead of truncating;
- classify trailing bytes explicitly;
- retain source order, coordinate index, duplicates, and out-of-domain records separately;
- fail closed on conflicting duplicate coordinates;
- never synthesize missing cells during parsing;
- never let missing TMP assets shift later global tile-ID ranges;
- keep `.ubn → .urb` behind an explicit editor-compatibility profile;
- keep Core independent of UnityEngine.

## Files

- `isomap-pack5-layer-boundaries.md`
- `record-layout-field-map.md`
- `tile-field-conflict.md`
- `coordinate-system-and-map-bounds.md`
- `record-order-density-and-duplicates.md`
- `subtile-level-and-final-byte.md`
- `terminal-padding-and-length-contract.md`
- `theater-tile-registry-binding.md`
- `source-comparison.md`
- `implementation-boundaries.md`
- `test-matrix.md`
- `baseline-audit-request.md`
- `unresolved-questions.md`

## Explicit non-goals

No code, decoder, writer, Unity execution, original-game execution, FinalAlert execution, XCC execution, local ProjectBaseline access, compatibility update, ADR, formal third-party ledger, rendering, pathfinding, map creation, or original asset extraction is part of M3-R4.
