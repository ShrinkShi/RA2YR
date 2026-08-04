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

## Evidence grades

Every claim is labelled using one of:

- `ConfirmedByOfficialRuntimeSource`
- `ConfirmedByOfficialEditorSource`
- `ConfirmedByIndependentImplementation`
- `CommunityDocumented`
- `ObservedByFutureProjectBaselineAudit`
- `ConfiguredForProjectPolicy`
- `Unresolved`

No public RA2/YR game runtime source was located for PreviewPack. No finding in this dossier is promoted to `ConfirmedByOfficialRuntimeSource` merely because an editor or client displays a preview correctly.

## Main conclusions

### Independent metadata and payload

`[Preview]` and `[PreviewPack]` are separate artifacts. Metadata can exist without payload and payload can exist without valid metadata. Neither layer is fabricated by the parser when the other is absent.

### Raw metadata first

The four `Size=` values are retained as four signed raw integer candidates:

```text
PreviewSizeFieldRaw0
PreviewSizeFieldRaw1
PreviewSizeFieldRaw2
PreviewSizeFieldRaw3
```

The strongest consumer convention treats fields 2 and 3 as width and height. The exact runtime meaning of fields 0 and 1 remains unresolved. EA's released editor begins with `[Map] Size` and replaces only values 2 and 3, while WAE, CNCMaps, and MapTool writers emit `0,0,width,height`.

### Three bytes per pixel

The strongest candidate contract is:

```text
expectedDecodedLength = width × height × 3
```

Public writers allocate exactly three bytes per pixel. No evidence was found for alpha, palette indices, scanline padding inside the decoded payload, a decoded trailer, or row alignment. The strict project policy requires checked multiplication and exact output length.

### Channel-order conflict

The strongest executable evidence indicates raw `R,G,B` component order:

- EA's released editor converts bottom-up Windows DIB BGR bytes into top-down raw RGB;
- CnCNet explicitly documents raw 24-bit RGB and swaps into BGR only for its bitmap consumer;
- WAE writes `Color.R`, `Color.G`, `Color.B` even though its comment says “BGR888”;
- CNCMaps' variable names and comments are misleading, but its conversion is consistent with raw RGB when accounting for GDI's BGR memory layout.

ModEnc permanent revision `oldid=28503` describes BGR888. This remains a real source conflict. Core therefore stores `Component0Raw`, `Component1Raw`, and `Component2Raw` before applying an explicit `PreviewChannelOrderProfile`.

### Row-order candidate

EA's released editor reads a bottom-up DIB source, vertically reverses it, and writes a row-major destination. CnCNet consumes rows in increasing row order without a Core-level vertical flip. `RowMajorTopDown` is the strongest project candidate but remains evidence-gated; `RowMajorBottomUp`, `ColumnMajor`, and `Unknown` remain explicit alternatives.

### Section placement is compatibility-sensitive

WAE states that original TS and YR expect `[Preview]` and `[PreviewPack]` first and moves both sections to the front. CNCMaps instead inserts generated preview sections after `[Basic]`, and its release notes specifically discuss that placement. The original runtime rule is unresolved, so lossless physical section order must be preserved and canonical reordering must be an explicit writer policy.

### Missing preview is not parse success

WAE can inject a 106×61 dummy/hidden preview when sections are missing. CnCNet recognizes that payload and intentionally returns no visible preview. Such generation and fallback belong to editor or UI adapters. Core reports missing or inconsistent layers and never claims that a fabricated preview was present in the source.

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

A stale or deliberately misleading preview can still be structurally valid.

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