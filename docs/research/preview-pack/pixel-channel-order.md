# Preview pixel component and channel-order conflict

> Source notice: researched by **ChatGPT Web** from public sources; no local `ProjectBaseline`; not a Codex Agent artifact; no implementation copied, translated, mechanically rewritten, or imported (`code_imported: false`).

## 1. Raw format model

Before interpreting color, every pixel is represented as:

```text
PreviewPixelRaw
- Component0Raw: byte
- Component1Raw: byte
- Component2Raw: byte
```

No field is named red, green, blue, alpha, palette index, luminance, or opacity at this layer.

## 2. Competing profiles

### `RGB24`

```text
R = Component0Raw
G = Component1Raw
B = Component2Raw
```

### `BGR24`

```text
B = Component0Raw
G = Component1Raw
R = Component2Raw
```

### `UnknownThreeComponent`

The three bytes are preserved without selecting color semantics.

No alpha channel is present in any inspected standard implementation.

## 3. EA official editor evidence

Pinned source: EA mission editor commit `6abf0f557469baea73079c6bf6550709e2e3584e`, `MissionEditor/MapData.cpp`.

The editor receives a 24-bit Windows DIB. DIB pixel memory is handled as BGR. For every destination pixel it writes:

- destination component 2 from DIB component 0;
- destination component 1 from DIB component 1;
- destination component 0 from DIB component 2.

Accounting for DIB BGR layout, the encoded raw stream is `R,G,B`. This is the strongest inspected executable evidence and is graded `ConfirmedByOfficialEditorSource`.

It is not official game runtime source.

## 4. WAE source/comment conflict

Pinned source: WAE commit `b4c9481e9b00fb0a38739049a046f528b6054ce2`, `MapWriter.WriteActualPreview`.

The comment says:

```text
Preview is in BGR888 format
```

But the writer appends:

```text
Color.R, Color.G, Color.B
```

The code behavior supports raw RGB while the prose supports BGR. The dossier records both and gives executable behavior more weight for describing WAE output, without turning that into runtime proof.

## 5. CnCNet consumer evidence

Pinned source: `e6e367bbe04c1a0dc1e34a8fed2856ea3ab7e8c4`, `MapPreviewExtractor.cs`.

The method describes the input as “raw image pixel data in 24-bit RGB format.” It converts each decoded triplet into BGR only when constructing a GDI/ImageSharp-compatible buffer. This supports `RGB24` and shows that consumer-native BGR is an adapter concern.

## 6. CNCMaps naming trap

Pinned source: `afb9c1ec118f5128cbc1f3fb5e35c7dfa0e422fb`, `ThumbInjector.cs`.

The tool comments “invert rgb->bgr,” but it reads `Format24bppRgb` memory, whose byte order is BGR. Its local variable names label the first byte `r`, which is misleading. Accounting for the bitmap memory layout, the generated packed stream is consistent with RGB and the extraction path is symmetric.

This source demonstrates why variable names and comments cannot be accepted without tracing the API's native storage order.

## 7. MapTool evidence limit

MapTool uses `GraphicsUtils.GetRawImageDataFromBitmap` and `CreateBitmapFromImageData`. The pinned `MapFile.cs` establishes three bytes per pixel and symmetric helper usage, but the inspected file alone does not expose the helper's channel contract. It therefore does not independently resolve RGB versus BGR.

## 8. ModEnc conflict

ModEnc PreviewPack permanent revision `oldid=28503` says the LZO payload contains BGR888 pixels. It is graded `CommunityDocumented`.

This conflicts with the official-editor conversion and CnCNet's explicit reader contract. The page cites the CnCNet extractor even though that extractor calls its input RGB, so the documentation may have inherited Windows bitmap terminology rather than packed-stream byte order.

The conflict remains explicit rather than silently “corrected.”

## 9. Shared implementation ancestry

Agreement among WAE, CNCMaps, MapTool, CnCNet, and older XCC/OpenRA-derived helpers is not automatically independent evidence. Some projects share map-pack or image utility history. Source comparison records likely ancestry and counts implementation behavior separately from runtime proof.

## 10. Project selection policy

Initial project policy:

- raw bytes are always retained;
- `RGB24` is the leading candidate for standard RA2/YR PreviewPack;
- its grade is not above official-editor/independent implementation evidence;
- `BGR24` remains selectable for comparison and extension profiles;
- no auto-detection;
- no plausibility scoring;
- no comparison against theater colors, sky, water, grass, ore, faction colors, or skin tones;
- no “choose the version that looks right” behavior;
- profile identity and evidence grade are included in cache keys and diagnostics.

## 11. No Core color processing

Core does not:

- append alpha;
- premultiply alpha;
- apply gamma;
- mark sRGB;
- perform palette lookup;
- color-grade;
- sharpen;
- dither;
- resize;
- repair compression artifacts;
- transform into RGBA during parsing.

A consumer may create an opaque RGBA view with alpha 255, but that is a derived adapter result and must not replace raw decoded bytes.

## 12. Color-space status

No inspected format evidence declares:

- sRGB transfer function;
- linear RGB;
- ICC profile;
- gamma value;
- chromaticities;
- alpha semantics;
- transparent key;
- theater palette dependency.

The default model is three uninterpreted 8-bit components plus an explicit channel-order candidate.

## 13. Diagnostics

Useful structured diagnostics include:

- `ChannelProfileNotSelected`;
- `ChannelProfileConflictingSources`;
- `UnknownThreeComponentLayout`;
- `ConsumerNativeOrderDiffersFromPackedOrder`;
- `ProfileEvidenceInsufficient`;
- `ColorSpaceUnspecified`.

None of these alters the decoded stream.