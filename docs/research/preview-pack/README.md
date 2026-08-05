# RA2/YR PreviewPack metadata and pixel-layout dossier

> Source notice: this research was completed by **ChatGPT Web** from public materials. It did not read local `ProjectBaseline`, is not a local Codex Agent artifact, and imports no code. GPL and unclear-license implementations were used only as behavioral references; no implementation was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

## Purpose

This dossier isolates the research needed before implementing RA2/YR map preview support. It covers `[Preview]`, `[PreviewPack]`, numbered fragments, strict Base64, chunked LZO, decoded pixel length, component order, row order, section placement, missing previews, consumer behavior, round-trip preservation, defensive limits, and a sanitized future audit.

It does **not** implement a reader, decoder, image, Unity adapter, minimap, renderer, writer, or compatibility claim.

## Frozen layer boundary

```text
lossless map INI
→ [Preview] metadata view
→ [PreviewPack] numbered fragment collection
→ strict Base64
→ Westwood chunk envelope
→ raw LZO1X-compatible decode backend
→ exact decoded preview byte stream
→ explicit pixel-layout interpretation
→ preview image model
→ UI/export/editor adapters
```

The layers are intentionally one-way:

- the Preview reader does not parse a complete INI file;
- the fragment collector does not interpret pixels;
- the Base64 layer does not interpret LZO headers;
- the chunk reader does not know width, height, channels, rows, or Unity;
- the LZO backend does not know map metadata;
- the pixel interpreter never rewrites decoded bytes;
- the preview model is non-authoritative for terrain, overlays, simulation, fog, pathfinding, or map validity;
- Core creates no `Texture2D`, `Sprite`, `RenderTexture`, `Material`, `GameObject`, bitmap, PNG, or JPEG.

## Formal evidence grades

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

`ConfirmedByOriginalRuntimeSource` is reserved for actual RA2/YR runtime behavior or original runtime source. No reviewed claim in this dossier has that grade because no public original-runtime source was located.

`ConfirmedByOfficialToolSource` applies to FinalSun, FinalAlert, and other official editor/tool behavior. It does not establish game-runtime behavior.

`ConfirmedByMultipleIndependentImplementations` requires demonstrably independent implementation lineages. Related community tools, shared map-pack helpers, XCC-derived knowledge, and cross-tool knowledge transfer are not counted repeatedly.

Stable community/toolchain conventions use `ConfirmedCommunityConvention`. A named reader, writer, client, or extension behavior uses `ImplementationSpecificBehavior`. Uncertain convergence uses `Underconfirmed`; direct disagreement uses `ConflictingSources`.

Exact-length validation, raw preservation, explicit profiles, refusal to guess or repair, and fail-closed behavior use `DefensiveDesign`. Project choices belong in `Policy` or `PolicyClassification`, not in external evidence claims.

Future ProjectBaseline work is recorded separately:

```text
AuditStatus: NotRun
FutureEvidenceSource: ProjectBaselineAggregateAudit
```

These fields are not evidence grades and do not imply that ProjectBaseline was read.

## Main conclusions

### Separate metadata and payload layers

`[Preview]` and `[PreviewPack]` are separate artifacts. Metadata can exist without payload and payload can exist without valid metadata.

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| Public tools represent metadata and packed payload as separate sections | `Underconfirmed` | FinalAlert, WAE, CnCNet, CNCMaps, MapTool | Cross-tool convergence is useful, but implementation lineages and stock-runtime behavior are not proven independent. | Keep section states and provenance separate. | `NotRun` |
| Core never fabricates one layer when the other is absent | `DefensiveDesign` | Project policy | This is a preservation and failure-boundary decision, not a runtime fact. | Report missing or inconsistent layers explicitly. | `NotRun` |

### Raw metadata first

The four `Size=` values are retained as four signed raw integer candidates:

```text
PreviewSizeFieldRaw0
PreviewSizeFieldRaw1
PreviewSizeFieldRaw2
PreviewSizeFieldRaw3
```

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert copies `[Map] Size` and replaces fields 2 and 3 with generated preview width and height | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Confirms official-editor writer behavior only. It also shows that fields 0 and 1 are not inherently forced to zero by that writer. | Preserve all four raw fields. | `NotRun` |
| WAE, CNCMaps, and MapTool write `0,0,width,height` | `ImplementationSpecificBehavior` | Named writers | Each entry is tool-specific canonicalization, not one runtime contract. | Do not rewrite nonzero first fields automatically. | `NotRun` |
| Fields 2 and 3 are the width/height candidate used across public consumers and writers | `Underconfirmed` | FinalAlert, WAE, CnCNet, CNCMaps, MapTool, ModEnc | Strong convergence exists, but original-runtime semantics are not directly established. | Require an explicit metadata interpretation profile. | `NotRun` |
| Original-runtime meaning of fields 0 and 1 | `Unresolved` | No original-runtime source located | Candidate interpretations include origin, offset, inherited map fields, ignored fields, or unknown tuple members. | Preserve raw values without forcing zero. | `NotRun` |

### Three bytes per pixel

The leading decoded-length candidate is:

```text
expectedDecodedLength = width × height × 3
```

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert allocates and encodes three bytes per generated preview pixel | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Official-editor output only. | Preserve as source-pinned writer evidence. | `NotRun` |
| WAE, CnCNet, CNCMaps, and MapTool use `width × height × 3` storage | `Underconfirmed` | Public tools | Convergence does not prove independent lineages or a universal runtime rule. | Use as an explicit standard-profile candidate. | `NotRun` |
| Stock runtime strictly requires exact `width × height × 3` output | `Underconfirmed` | Public writer/consumer behavior | No runtime source establishes rejection, padding, or truncation behavior. | Enforce exact output as project policy. | `NotRun` |
| Checked multiplication, exact output, no padding, no clamp, no truncation, and no partial success | `DefensiveDesign` | Project policy | Fail-closed safety contract. | Validate before pixel interpretation. | `NotRun` |

No inspected source path added alpha, palette indices, scanline padding inside the decoded stream, or a decoded trailer. That absence is limited to the reviewed evidence set and is not an absolute runtime proof.

### Channel-order conflict

The sources do not establish one uncontested runtime channel order.

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert converts bottom-up Windows DIB BGR memory into raw `R,G,B` output | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Official-editor writer behavior, not game-runtime reader proof. | Retain as the leading writer profile. | `NotRun` |
| CnCNet consumes raw RGB and swaps only for BGR-native bitmap memory | `ImplementationSpecificBehavior` | CnCNet XNA client | Named consumer behavior. | Keep consumer conversion outside Core. | `NotRun` |
| WAE writes `R,G,B` despite a `BGR888` comment | `ImplementationSpecificBehavior` | World-Altering Editor | Source comment and executable assignment conflict within the named tool. | Preserve both facts in source notes. | `NotRun` |
| CNCMaps behavior is consistent with raw RGB when GDI BGR memory is accounted for | `ImplementationSpecificBehavior` | CNCMaps | Variable names/comments are not sufficient without API memory-order analysis. | Treat as tool-specific evidence. | `NotRun` |
| ModEnc documents BGR888 | `ConfirmedCommunityConvention` | ModEnc PreviewPack `oldid=28503` | Stable community documentation, but it conflicts with executable writer/consumer behavior. | Keep as an explicit comparison profile. | `NotRun` |
| One unique original-runtime channel order is established | `ConflictingSources` | RGB executable evidence versus BGR documentation | No runtime source selects a unique contract. | Store `Component0Raw/1Raw/2Raw`; require explicit channel profile; never select by visual plausibility. | `NotRun` |

### Row-order candidate

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert vertically reverses bottom-up DIB rows into top-down packed output | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Confirms generated editor output only. | Retain a named top-down writer profile. | `NotRun` |
| CnCNet consumes increasing rows without a Core-level vertical flip | `ImplementationSpecificBehavior` | CnCNet XNA client | Named consumer behavior. | Keep consumer-native orientation outside Core. | `NotRun` |
| `RowMajorTopDown` is the leading standard candidate | `Underconfirmed` | FinalAlert and public consumers | Original-runtime row behavior is not directly sourced. | Require explicit row profile. | `NotRun` |
| Original-runtime unique row order | `Unresolved` | No original-runtime source located | Third-party and extension payloads may differ. | Retain top-down, bottom-up, column-major, and unknown profiles; Unity flips remain adapter behavior. | `NotRun` |

### Section placement is compatibility-sensitive

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| WAE moves Preview sections to the file front | `ImplementationSpecificBehavior` | World-Altering Editor | The accompanying original-runtime claim is not source-confirmed. | Keep as a named writer compatibility profile. | `NotRun` |
| CNCMaps places generated PreviewPack after Basic in the documented release behavior | `ImplementationSpecificBehavior` | CNCMaps and fixed PPM release discussion | Conflicts with WAE placement policy. | Preserve as a separate writer profile. | `NotRun` |
| CnCNet scans Preview sections wherever they occur | `ImplementationSpecificBehavior` | CnCNet XNA client | Consumer behavior only. | Do not infer game-runtime acceptance. | `NotRun` |
| Preview must be first for original YR runtime | `Underconfirmed` | WAE comment and community reports | Tool and community claim without original-runtime source. | Preserve physical section order losslessly; canonical relocation requires an explicit writer profile. | `NotRun` |

### Missing preview is not parse success

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| WAE injects a fixed 106×61 dummy preview when sections are missing | `ImplementationSpecificBehavior` | World-Altering Editor | Named editor compatibility behavior. | Treat as generated content, not source truth. | `NotRun` |
| CnCNet recognizes the fixed payload and hides it | `ImplementationSpecificBehavior` | CnCNet XNA client | Named consumer behavior. | Preserve bytes while exposing a diagnostic candidate. | `NotRun` |
| The dummy payload is the original-runtime standard representation of a missing preview | `Unresolved` | No original-runtime source located | Tool cooperation does not establish a runtime standard. | Core never fabricates a source preview. | `NotRun` |
| Missing, inconsistent, or fabricated-preview handling in Core | `DefensiveDesign` | Project policy | Parser reports source state and preserves provenance. | Placeholder generation and fallback UI remain adapter behavior. | `NotRun` |

### Preview is non-authoritative

Preview pixels cannot validate, repair, or replace:

- IsoMap records;
- Overlay arrays;
- TMP or theater resources;
- map coordinates;
- collision or movement;
- fog-of-war;
- minimap simulation state;
- rendering correctness.

This separation is `DefensiveDesign` and an architectural boundary. A stale or deliberately misleading preview can still be structurally valid.

## Pinned public sources

| Source | Pin | Role | License boundary |
|---|---|---|---|
| EA FinalSun / FinalAlert 2 | `6abf0f557469baea73079c6bf6550709e2e3584e`, `MissionEditor/MapData.cpp` | official editor writer | GPL-3.0-or-later header; reference only |
| World-Altering Editor | `b4c9481e9b00fb0a38739049a046f528b6054ce2`, `MapWriter.cs`, `Map.cs` | editor writer/fallback | GPL-3.0-or-later; reference only |
| CnCNet XNA client | `e6e367bbe04c1a0dc1e34a8fed2856ea3ab7e8c4`, `MapPreviewExtractor.cs`, `FastMapPreviewExtractor.cs` | consumer reader | GPL-3.0; reference only |
| CNCMaps | `afb9c1ec118f5128cbc1f3fb5e35c7dfa0e422fb`, `ThumbInjector.cs` | reader/writer tool | repository default and imported-code boundaries require reference-only treatment |
| MapTool | `f85f2226905496139f1258b5854fad915f9bbac6`, `MapFile.cs` | reader/writer tool | GPL-2.0-or-later; reference only |
| ModEnc PreviewPack | permanent revision `oldid=28503` | community documentation | documentation only |
| ModEnc Preview | permanent revision `oldid=21306` | community documentation | documentation only |
| PPM CNCMaps release discussion | fixed topic `36021` | section-placement/tool behavior | community documentation only |
| OpenRA | `a520984d91eda9de48a62b1d15c1e3bad0d4fb1a` | searched; no load-bearing PreviewPack path located | no vote |
| Chrono Divide SDK | `5943c4ae6c19897929d348a417d6d2f1481b75fd` | searched; no load-bearing PreviewPack path located | no vote |
| XCC / OmniBlade | pinned public mirrors searched | no pinned low-level PreviewPack path used | no vote |

Agreement among related community tools is not counted as multiple independent runtime proofs.

## Files in this dossier

- `layer-and-section-boundaries.md`
- `preview-metadata-and-size.md`
- `packed-stream-and-length-contract.md`
- `pixel-channel-order.md`
- `row-order-origin-and-coordinate.md`
- `missing-preview-and-fabrication.md`
- `section-order-and-roundtrip.md`
- `preview-consumer-boundaries.md`
- `source-comparison.md`
- `implementation-boundaries.md`
- `test-matrix.md`
- `baseline-audit-request.md`
- `unresolved-questions.md`

## Explicit non-goals

No code, test, Unity, image generation, map execution, ProjectBaseline access, compatibility-matrix update, ADR, formal third-party ledger, `.dev-records`, or existing research was changed or produced by this dossier.
