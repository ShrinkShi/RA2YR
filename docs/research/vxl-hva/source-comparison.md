# Source comparison and conflict register

> GPL and unclear-license code is reference-only. No source was copied, line-translated, mechanically rewritten or converted into a C# implementation design.

## 1. Source table

| Source | Pin / path | License | Scope and useful behavior | Known limits |
|---|---|---|---|---|
| OpenRA | commit `a520984d91eda9de48a62b1d15c1e3bad0d4fb1a`; `OpenRA.Mods.Cnc/FileFormats/VxlReader.cs`, `HvaReader.cs`, `Traits/World/VoxelNormalsPalette.cs` | GPL-3.0-or-later; reference-only | 802-byte VXL assumption; sparse column decode; mode 2/4 normals; HVA 12-float read and runtime transpose | VXL ignores end table and duplicate-count equality; weak bounds checks; HVA discards names and rejects noninvertible matrices as runtime policy |
| OmniBlade/XCC mirror | commit `62bb77080f13bdf65c79c84837b7cc264bdd432d`; `xcc/misc/cc_structures.h`, `vxl_file.*`, `hva_file.*` | SourceForge GPL-2.0 lineage; reference-only | Packed structs; 802/28/92 sizes; body-relative offsets; inclusive span end; writer for duplicate count; embedded palette; HVA exact-size formula | Mirror is not proven byte-identical to a specific SourceForge SVN revision; HVA accessor is section-major and conflicts with most other readers; some validation blocks disabled |
| Olaf van der Spek XCC mirror | commit `6f91bf8b00d3acabb1be765118a37c0cb74e85ec`; `misc/hva_file.cpp` and related files | GPL lineage; reference-only | Independent repository pin for XCC section-major HVA accessor behavior | Same project lineage as OmniBlade; not an independent semantic implementation |
| XCC SourceForge | XCC Utilities 1.46, released 2008-05-02; `XCC_Source.zip`; project page | GPL-2.0 | Locatable historical release and licensing origin | Public browser access did not establish a file-level SVN revision equivalent to the pinned mirrors; no equivalence claim is made |
| Voxel Section Editor III public mirror | commit `fde704b01cb4de3adeaf1a151bbeee0994a04b99`; `vxlseiii14x/source/document/Voxel.pas`, `HVA.pas`, `constants/NormalsConstants.pas` | clear repository-wide license not located; reference-only | Packed 802-byte header with two remap bytes; section fields; VXL/HVA coordinate conversion; frame-major HVA; explicit 36/244 counts | Editor performs OpenGL conversions and permissive repair/edit operations; these are not stock-runtime semantics |
| vengi | commit `d61613daac7311107c183baa2116f89631fc710b`; `.../commandconquer/VXLFormat.cpp`, `HVAFormat.cpp` | MIT | Read/write structure, body-relative offsets, inclusive end, duplicate count, frame-major HVA, name binding attempts | Importer silently falls back from missing name to ordinal and accepts missing HVA; those policies are rejected for Core compatibility |
| iron-curtain-engine/cnc-formats | commit `77da596ed72a1201740e054855bf2ff60640bfa9`; `src/vxl/mod.rs`, `src/hva/mod.rs` | MIT OR Apache-2.0 | Modern bounded model; HVA names and frame-major transforms | VXL declares 804-byte header and shifts palette by two bytes; does not decode spans; no NaN/Infinity rejection; not format truth |
| EA FinalSun/FinalAlert 2 | commit `6abf0f557469baea73079c6bf6550709e2e3584e`; `MissionEditorPackLib/MissionEditorPackLib.cpp` and bundled XCC headers | GPL-3.0-or-later; reference-only | Official editor integration explicitly uses XCC VXL/HVA classes | Not an independent game-runtime parser; shares XCC behavior and defects |
| Chrono Divide public mod SDK | commit `5943c4ae6c19897929d348a417d6d2f1481b75fd` | repository-specific/public SDK terms | Establishes public mod/resource surface | No public engine VXL/HVA reader was located; no behavior inferred |
| ModEnc | `HVA` oldid `22595`; `Voxel`, `Normals`, `File Types`, turret/barrel positioning pages | factual community reference; page license not relied upon for code | Terminology, roles, 3×4 matrix diagram, VPL/normal separation, upper-layer resource behavior | Community runtime claims and coordinate descriptions are not byte-layout proof |
| Project Perfect Mod forums | fixed threads `topic-19916`, `topic-19938`, `topic-61138`, `topic-14594`, `topic-63315` | discussion; unclear for code snippets | Historical conflicts about spans, translation scale, bounds and tools | Forum claims are evidence of uncertainty, not normative specification |
| ModdingWiki | site/category search as of 2026-08-04 | factual reference | Useful for other Westwood formats | No dedicated stable TS/RA2 VXL/HVA format page was located; absence is recorded instead of inventing a citation |
| RA2YR_ReSource | commit `e944cb51d6b58f9cae3106caf760a4e860d894e6`; `src/render/vxl_format.hpp` | license not located; reference-only | Example of reverse-engineered runtime notes | Describes a conflicting `Voxel Section` layout and mixed file/memory structures; cannot override convergent file readers |

## 2. Source independence warning

Counting repositories is misleading:

- EA FinalSun/FinalAlert 2 bundles/uses XCC classes.
- OmniBlade, OlafvdSpek and other XCC mirrors share ancestry.
- OpenRA-derived forks often reproduce the same normal tables and reader assumptions.
- vengi cites community documentation and performs renderer conversion.

A conclusion is considered multi-source only when implementations or documentary evidence are meaningfully independent.

## 3. Conflict table

| Topic | Competing evidence | Current decision |
|---|---|---|
| VXL header size | OpenRA/XCC/VSE/vengi: 802; `cnc-formats`: 804 | 802 confirmed candidate; 804 recorded as implementation defect |
| Header bytes 32–33 | XCC: one raw `0x1f10`; VSE/vengi: remap start/end 16/31 | preserve both bytes and raw u16 view; candidate remap interpretation |
| Header/version | some prose calls early fields version/unknown | only signature and raw fields retained; no invented version enum |
| Section-header final field | VSE/vengi writer: commonly 2; older XCC names/comments suggest zero | preserve raw; do not validate fixed value until local audit |
| Tailer offset unit | strongest writers: byte offsets from shared body start | confirmed candidate; checked body-relative bytes |
| Column offset unit | XCC/vengi: byte offsets from section span-data start | confirmed candidate |
| Span end | XCC/vengi writer and XCC size helper: inclusive | inclusive candidate; local audit must verify end-consumption equality |
| Empty sentinel | convergent sources: signed `-1` | confirmed candidate; only `-1/-1` pair accepted |
| Column termination | logical Z, end offset, or readers ignoring end | strict proposal requires both exact Z and exact input exhaustion; golden-gated |
| Duplicate count | writers emit trailing count; readers often skip unchecked | validate equality defensively; stock tolerance unconfirmed |
| Shared/overlapping spans | permissive pointer readers could display them | no reference semantics confirmed; preserve diagnostics and reject overlap |
| Embedded palette use | file contains palette; runtime may use external palettes/VPL | parser retains palette; runtime use outside reader |
| Normal selector | values 2/4, table sizes 36/244 | strong candidate; unknown raw values remain unresolved |
| Normal table location | engine/tool constants versus palette/VPL confusion | vectors are external constants; no full table in VXL |
| Bounds/scale | scale, determinant and translation-unit interpretations differ | preserve raw floats; no composition or multiplication in parser |
| HVA first 16 bytes | filename, blank, `NONE`, alleged signature | raw label/filename, not fixed magic |
| HVA record order | OpenRA/VSE/vengi/cnc-formats frame-major; XCC section-major | unresolved; dual interpretation plus golden gate |
| 3×4 layout | file rows broadly agreed; runtime storage transposed by some engines | retain 12 values in file order |
| Row/column-major | source language/math-library conventions differ | adapter concern; no Unity matrix in Core |
| Handedness/axis mapping | OpenRA labels axes; VSE/vengi permute for OpenGL/GLM | raw axes only; adapter declares mapping |
| Missing HVA | community says stock requires it; modern importers may accept | VXL parse succeeds independently; resource policy handles absence |
| Name comparison | exact, case-insensitive or ordinal fallback across tools | exact unique default; case/index alternatives unresolved and explicit |
| Singular matrices | OpenRA rejects; other raw readers accept | semantic diagnostic, not byte parse rule yet |
| Trailing bytes | XCC exact-size; others tolerate | strict default; exception requires evidence |

## 4. High-confidence shared facts

The following facts have strong convergent support:

- VXL signature field begins with `Voxel Animation`.
- VXL header is 802 bytes in the dominant TS/RA2/YR layout.
- section headers are 28 bytes and tailers 92 bytes.
- VXL contains an embedded 256×RGB palette.
- section body offsets are byte-based and relative to the shared body.
- each X/Y column uses signed start/end entries and `-1` empty sentinel.
- each stored voxel is color byte plus normal byte.
- the span chunk includes skip, count, `count` voxel records and a duplicate count byte.
- HVA header is 24 bytes, names are 16 bytes and transforms are 48 bytes.
- HVA floats are little-endian IEEE-754 values.

## 5. Facts that remain implementation gates

- legal zero-section/zero-frame files;
- exact validity of section-header constants;
- whether duplicate/overlapping column ranges are ever intentional;
- whether both span end and logical Z must be exact in stock assets;
- treatment of no-progress or count-mismatch chunks by stock runtime;
- HVA frame-major versus section-major ordering;
- case-sensitivity and ordinal fallback for section binding;
- VXL scale/bounds/transform and HVA composition order;
- coordinate handedness and projection basis;
- stock behavior for missing HVA;
- acceptance of singular/extreme finite transforms.

## 6. License handling

- GPL sources: facts and externally observable behavior only; no code import or mechanical port.
- unclear-license editor/community sources: reference-only.
- MIT/Apache sources: still not automatically authoritative; code reuse would require a separate approved implementation PR and attribution review.
- forum/wiki prose: paraphrased factual leads only.
- no complete normal table, voxel body or matrix series is included here.
