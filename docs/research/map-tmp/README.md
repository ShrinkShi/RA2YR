# M3-R — RA2/YR MAP and TMP format dossier

> **Source notice:** Prepared independently by **ChatGPT Web** from public sources and repository context. No local ProjectBaseline, original map body, TMP body, rendered image or Unity project was read or executed.

## Purpose

This directory is a research-only handoff for future bounded support for Red Alert 2 / Yuri's Revenge map containers and TS/RA2/YR TMP terrain templates.

The dossier covers:

- `.map`, `.mpr` and `.yrm` family and discovery-role boundaries;
- the lossless INI shell and binary-data sections embedded as numbered text values;
- `IsoMapPack5`, `OverlayPack`, `OverlayDataPack` and `PreviewPack`;
- waypoints, map objects, houses and mission scripting graphs;
- map-local Rules/Art semantic override layers;
- TMP headers, tile offsets, normal image, depth image, optional extra image and raw metadata;
- theater, tile-set, slope, cliff, water and bridge responsibility boundaries;
- coordinate domains and deterministic conversion boundaries;
- safe FinalSun/FinalAlert 2 import/export and round-trip risks;
- Memory, seekable Stream and MIX-entry-window equivalence;
- read limits, corruption handling and a sanitized local golden-audit request.

This PR does not implement parsing or rendering and does not promote compatibility status.

## Frozen repository basis

- Repository: `ShrinkShi/RA2YR`
- Base branch: `main`
- Base commit: `c0ab4ff6cdb31d0ec4b3a38db1c24814c9391207`
- Research branch: `research/m3-map-tmp-format-dossier`

## Documents

1. [family-boundaries.md](family-boundaries.md)
2. [map-container-and-ini-shell.md](map-container-and-ini-shell.md)
3. [packed-section-envelope.md](packed-section-envelope.md)
4. [isomap-pack5.md](isomap-pack5.md)
5. [overlay-and-preview-packs.md](overlay-and-preview-packs.md)
6. [mission-object-sections.md](mission-object-sections.md)
7. [map-local-rules-art-overrides.md](map-local-rules-art-overrides.md)
8. [tmp-file-layout.md](tmp-file-layout.md)
9. [terrain-and-coordinate-conventions.md](terrain-and-coordinate-conventions.md)
10. [finalalert-roundtrip-boundaries.md](finalalert-roundtrip-boundaries.md)
11. [source-comparison.md](source-comparison.md)
12. [implementation-boundaries.md](implementation-boundaries.md)
13. [test-matrix.md](test-matrix.md)
14. [baseline-audit-request.md](baseline-audit-request.md)
15. [unresolved-questions.md](unresolved-questions.md)

## Evidence labels

Every material conclusion is classified as one of:

- `ConfirmedByOriginalOrOfficialSource`
- `ConfirmedByMultipleIndependentImplementations`
- `ConfirmedCommunityConvention`
- `ImplementationSpecificBehavior`
- `DefensiveDesign`
- `ConflictingSources`
- `Underconfirmed`
- `Unresolved`

An official editor is strong writer/tool evidence, but it is not automatically the original game runtime.

## High-confidence conclusions

- RA2/YR map files are INI-like documents with structured textual sections plus numbered Base64 fragments for packed binary regions.
- `.map`, `.mpr` and `.yrm` share the map-document family; their extensions primarily affect discovery, packaging and multiplayer role rather than defining three unrelated byte formats.
- `IsoMapPack5` is chunked LZO data. Its decompressed payload is a sequence of 11-byte records plus a four-byte terminal/padding candidate.
- `OverlayPack` and `OverlayDataPack` are separate fixed-canvas arrays compressed with Format80/LCW and represented through Base64 lines.
- `PreviewPack` uses chunked LZO and three bytes per preview pixel; width and height come from `[Preview] Size=`.
- Map object, mission and scripting sections form an identity/reference graph and must preserve unknown records and unresolved references rather than being flattened into anonymous dictionaries.
- Map-local Rules/Art overrides are semantic INI layers above the resolved global configuration; the map binary reader must not instantiate typed game objects while reading the shell.
- TMP is a theater-indexed terrain-template format. One file contains an ordered grid of optional cells, each with a diamond color image, diamond depth image, optional rectangular extra color/depth images, and raw metadata.
- Terrain identity, ramp class, land type, bridge behavior and theater lookup span TMP, theater INIs, map tile records and runtime logic. They do not belong to one binary format class.

## Principal conflicts and gates

- `IsoMapPack5` bytes 4..7 are represented as a 32-bit tile index by modern editors, while OpenRA's importer reads a 16-bit tile number plus a 16-bit unused field. Raw bytes must be preserved until local and source evidence settle the contract.
- Preview sources agree on three-byte pixels but use RGB/BGR terminology inconsistently because file-byte order and graphics-library memory order are frequently conflated.
- TMP sources agree on the broad layout but differ in which raw metadata bytes are named height, terrain, ramp, flags or colors.
- FinalAlert/FinalSun and modern editors may regenerate pack ordering, previews, object numbering and scripting-section order. Successful load/save is not proof of byte-identical or semantic-lossless roundtrip.

## Non-goals

This research deliberately does not:

- implement C# or modify tests;
- run Unity, FinalAlert, FinalSun, XCC or the original game;
- read ProjectBaseline or original map/TMP bodies;
- publish map text, packed data, tile arrays, preview pixels or reconstructable terrain;
- modify compatibility metadata, ADRs, the third-party ledger or existing research;
- define a renderer, navigation system, trigger VM or map editor;
- claim that one permissive importer reproduces original-runtime behavior;
- merge or auto-merge the research PR.
