# M3-R3 — TMP metadata and theater resource binding dossier

> **Source notice:** Prepared independently by **ChatGPT Web** from pinned public sources and repository context. No local ProjectBaseline, original TMP body, theater INI body, palette bytes, map placement data, rendered image, or Unity project was read or executed.

## 1. Purpose

This directory narrows the earlier MAP/TMP dossier to the boundary between:

1. the raw 52-byte TMP cell metadata header;
2. diamond and optional extra image/depth planes;
3. theater control INI composition;
4. `[TileSet####]` registry and cumulative tile-number allocation;
5. theater-specific TMP filename, palette, LAT, ramp, cliff, shore, water, and bridge bindings;
6. later map-placement, rendering, movement, and simulation adapters.

It is research and implementation design only. It does not implement a TMP reader, theater loader, renderer, or pathfinder and does not promote compatibility status.

## 2. Frozen repository basis

- Repository: `ShrinkShi/RA2YR`
- Base branch: `main`
- Base commit after PR #22: `c0fa378a690eafd79e1687a83501bb6571887a2a`
- Research branch: `research/m3-tmp-theater-binding-dossier`

## 3. Architectural boundary

```text
content discovery and ordered INI composition
→ lossless theater-control documents
→ typed theater and TileSet registry
→ deterministic global tile-ID ranges
→ TMP asset candidates and variation policy
→ bounded TMP raw reader
→ palette, LAT, ramp, terrain-role binding
→ map placement and semantic terrain view
→ rendering, pathfinding, bridge and simulation adapters
```

The TMP reader does not scan MIX archives, parse theater INIs, allocate global tile IDs, select palettes, infer bridge logic, or create Unity objects.

## 4. High-confidence findings

- A TS/RA2-generation TMP cell header is 52 bytes, not 48.
- The strongest field map is nine 32-bit values, a 32-bit flag word, three one-byte semantic candidates, six radar-color bytes, and three trailing raw bytes.
- Flag bits `0x01`, `0x02`, and `0x04` are broadly labelled extra data, Z data, and damaged data; higher bits and trailing bytes must remain raw.
- The diamond color plane is `tileWidth × tileHeight / 2` bytes and is encoded by scanlines that widen and narrow in four-pixel steps.
- A present diamond depth plane has the same encoded length.
- Extra color and extra depth candidates use `extraWidth × extraHeight` bytes each.
- TMP image bytes are palette indices. The TMP does not embed the theater palette.
- Theater control INIs provide ordered TileSet registries and special set references such as ramps, cliffs, water, shores, bridges, and LAT transitions.
- Global map tile IDs are allocated cumulatively over the resolved TileSet registry; missing assets must not silently shift later ranges.
- Map `Level`, TMP cell `height`, TMP `ramp_type`, TMP depth pixels, and final movement height are different layers.
- Cliffs, shores, water, and bridges cross TMP, theater INI, map tiles, overlays, and runtime logic.

## 5. Principal conflicts

- WAE declares a 48-byte minimum-header constant but actually reads 52 bytes. This is an implementation defect, not a second TMP layout.
- Public readers disagree on whether to follow stored `z_ofs`, `extra_ofs`, and `extra_z_ofs` or assume canonical sequential placement.
- WAE reports uninitialized/trash flag and padding bytes in Westwood files, while many typed models expose only known bits.
- Some readers always consume a diamond depth plane; others gate it on flag bit `0x02`.
- `height`, `terrain_type`, and `ramp_type` names are strongly conventional but not established by original game source in the reviewed material.
- WAE stops TileSet enumeration at the first missing `[TileSet####]`; a lossless project implementation must preserve and diagnose gaps rather than discard later sections.
- NEWURBAN `.ubn` to `.urb` fallback is confirmed editor behavior and must not become an unlabelled original-runtime rule.
- Ramp enum names and corner geometry come through TS++/community lineage, not the original RA2/YR executable source.

## 6. Documents

1. [tmp-cell-header-field-map.md](tmp-cell-header-field-map.md)
2. [tmp-flags-and-extra-data.md](tmp-flags-and-extra-data.md)
3. [diamond-and-depth-planes.md](diamond-and-depth-planes.md)
4. [theater-control-ini.md](theater-control-ini.md)
5. [tileset-and-template-registry.md](tileset-and-template-registry.md)
6. [palette-and-lat-binding.md](palette-and-lat-binding.md)
7. [ramp-height-and-terrain-semantics.md](ramp-height-and-terrain-semantics.md)
8. [cliff-water-bridge-boundaries.md](cliff-water-bridge-boundaries.md)
9. [source-comparison.md](source-comparison.md)
10. [implementation-boundaries.md](implementation-boundaries.md)
11. [test-matrix.md](test-matrix.md)
12. [baseline-audit-request.md](baseline-audit-request.md)
13. [unresolved-questions.md](unresolved-questions.md)

## 7. Evidence labels

Material conclusions use exactly one normalized grade:

- `ConfirmedByOriginalRuntimeSource`
- `ConfirmedByOfficialToolSource`
- `ConfirmedByMultipleIndependentImplementations`
- `ConfirmedCommunityConvention`
- `ImplementationSpecificBehavior`
- `DefensiveDesign`
- `ConflictingSources`
- `Underconfirmed`
- `Unresolved`

Grade fields contain only one label from this closed vocabulary. Source details, caveats, conflicts, and implementation notes belong in separate Evidence or Notes fields.

`ConfirmedByOriginalRuntimeSource` is reserved for evidence from the original game runtime or its actual source. No reviewed FinalAlert/FinalSun evidence is promoted to this grade.

`ConfirmedByOfficialToolSource` covers official editors and tools such as FinalAlert/FinalSun. Official-tool behavior does not automatically establish original-runtime behavior.

`ConfirmedByMultipleIndependentImplementations` requires demonstrably independent implementation lineages. FinalAlert's bundled XCC TMP support, the openra2 XCC port, and other shared XCC/OpenRA descendants are not counted as independent confirmations merely because they appear in separate repositories.

## 8. Non-goals

This dossier does not:

- implement C#, C++, TypeScript, shaders, textures, or navigation;
- run Unity, FinalAlert, XCC, or the original game;
- read ProjectBaseline or publish original resources;
- copy, translate, or mechanically port GPL code;
- modify compatibility metadata, ADRs, formal source ledgers, or prior research;
- infer semantics from file names or rendered appearance alone;
- claim byte-identical writer support;
- merge or auto-merge its research PR.
