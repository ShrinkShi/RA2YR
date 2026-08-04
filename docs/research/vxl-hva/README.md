# M4-R — Westwood VXL/HVA format dossier

> 来源声明 / Source notice: 本研究由 **ChatGPT 网页版**基于公开资料和 GitHub 仓库独立整理，未读取本地 ProjectBaseline，也不是本地 Codex Agent 的产物。 / Prepared independently by **ChatGPT Web** from public sources and repository context; no local ProjectBaseline was read and this is not a local Codex Agent artifact.

## Purpose

This directory is the research-only handoff for implementing TS/RA2/YR VXL and HVA support after the SHP(TS) work. It records confirmed byte layout, source conflicts, strict-reader boundaries, a synthetic test matrix, and a sanitized local golden-audit request.

This is not an implementation and does not promote compatibility status.

## Frozen repository basis

- Repository: `ShrinkShi/RA2YR`
- Base branch: `main`
- Base commit at task start: `7e43b5138c4c0042196203da6d22e1e05bad3707`
- Research branch: `research/m4-vxl-hva-format-dossier`

## Documents

1. [family-boundaries.md](family-boundaries.md) — distinguishes VXL, HVA, Art.ini, simulation and rendering.
2. [vxl-file-layout.md](vxl-file-layout.md) — header, section headers, body and tailers.
3. [vxl-span-encoding.md](vxl-span-encoding.md) — sparse column directory and span command model.
4. [normals-and-lighting.md](normals-and-lighting.md) — normal selector, table-size evidence and lighting boundary.
5. [hva-file-layout.md](hva-file-layout.md) — HVA names and raw 3×4 transforms.
6. [section-binding.md](section-binding.md) — fail-closed VXL/HVA binding design.
7. [coordinate-and-matrix-conventions.md](coordinate-and-matrix-conventions.md) — raw storage versus engine conversion.
8. [source-comparison.md](source-comparison.md) — pinned sources, licenses, behavior and conflicts.
9. [implementation-boundaries.md](implementation-boundaries.md) — proposed Core model and module split.
10. [test-matrix.md](test-matrix.md) — 96 synthetic and structural cases.
11. [baseline-audit-request.md](baseline-audit-request.md) — aggregate-only ProjectBaseline audit for local Codex.
12. [unresolved-questions.md](unresolved-questions.md) — issues that public evidence does not settle.

## High-confidence conclusions

- The supported family is the TS/RA2/YR `Voxel Animation` VXL layout, not every file named `.vxl` and not older or unrelated voxel formats.
- VXL and HVA are independent binary documents. HVA binding is a separate operation. Art.ini resource composition and runtime vehicle behavior are higher layers.
- The strongest VXL header model is 802 bytes, followed by `sectionHeaderCount × 28`, a declared body region, and `sectionTailerCount × 92`.
- A VXL section body is sparse. It contains two signed 32-bit offset tables for `sizeX × sizeY` columns and a variable span-data stream.
- A voxel record is two independent bytes: palette/color index and normal index.
- The normal-selector byte commonly uses value `2` for the 36-vector TS table and `4` for the 244-vector RA2/YR table. The vectors are engine/tool constants, not embedded normal tables.
- HVA has a 24-byte header, `sectionCount × 16` raw names and `frameCount × sectionCount × 48` transform bytes.
- Each HVA transform is preserved as twelve little-endian `float32` values in file order. Core must not directly construct `UnityEngine.Matrix4x4`.

## Principal unresolved conflict

The byte layout of an individual HVA 3×4 record is broadly agreed, but the ordering of records across frames and sections is not:

- OpenRA, Voxel Section Editor III, vengi and `cnc-formats` read/write **frame-major** order.
- XCC's accessor and CSV writer address the same storage as **section-major**.

No production default should be frozen from static research alone. Synthetic distinguishing fixtures and an aggregate-only local golden audit are required.

## Non-goals

This research deliberately does not:

- implement C#;
- change tests or compatibility metadata;
- run Unity;
- read ProjectBaseline or original-game assets;
- publish voxel bodies, reconstructable geometry, matrices or complete normal tables;
- resolve Art.ini naming/default behavior;
- implement projection, z-buffering, shadows, slope tilt, turret rotation or barrel recoil;
- treat a permissive viewer as proof of original runtime semantics;
- merge or auto-merge the research PR.
