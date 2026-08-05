# `[Preview] Size=` metadata and interpretation candidates

> Source notice: researched by **ChatGPT Web** from public sources; no local `ProjectBaseline` access; not a Codex Agent artifact; no code copied, translated, mechanically rewritten, or imported (`code_imported: false`).

## 1. Raw representation

The parser must first retain the four textual fields without assigning geometry semantics:

```text
PreviewSizeFieldRaw0
PreviewSizeFieldRaw1
PreviewSizeFieldRaw2
PreviewSizeFieldRaw3
```

For each field retain:

- exact token text;
- parsed signed integer candidate;
- whitespace and sign;
- overflow/underflow result;
- source section and key occurrence;
- cross-layer provenance;
- same-document duplicate diagnostics.

A field name such as `Width` is a derived interpretation, not the raw API. Preserving all four fields is `DefensiveDesign`.

## 2. Public-source behavior

### EA FinalSun / FinalAlert 2 editor

Pinned source: `6abf0f557469baea73079c6bf6550709e2e3584e`, `MissionEditor/MapData.cpp`.

The writer:

1. copies `[Map] Size` into `[Preview] Size`;
2. replaces parameter 2 with generated bitmap width;
3. replaces parameter 3 with generated bitmap height.

```text
EvidenceGrade: ConfirmedByOfficialToolSource
Source: EA FinalSun / FinalAlert 2
AuditStatus: NotRun
```

This confirms that the official editor writes fields 2 and 3 as preview width and height. It also shows that the writer does not inherently force the first two fields to zero because it inherits them from `[Map] Size`. It does not prove what the game runtime does with fields 0 and 1.

### World-Altering Editor

Pinned source: `b4c9481e9b00fb0a38739049a046f528b6054ce2`, `MapWriter.WriteActualPreview` and `WriteDummyPreview`.

The writer emits:

```text
0,0,width,height
```

Its dummy preview uses `0,0,106,61`.

```text
EvidenceGrade: ImplementationSpecificBehavior
Source: World-Altering Editor
AuditStatus: NotRun
```

This is named editor behavior, not runtime source.

### CNCMaps

Pinned source: `afb9c1ec118f5128cbc1f3fb5e35c7dfa0e422fb`, `CNCMaps.Engine/Map/ThumbInjector.cs`.

The writer emits `0,0,width,height`. The reader constructs a rectangle from all four values but uses its width and height to allocate the preview. This supports an `x,y,width,height` interpretation in that tool.

```text
EvidenceGrade: ImplementationSpecificBehavior
Source: CNCMaps
AuditStatus: NotRun
```

### MapTool

Pinned source: `f85f2226905496139f1258b5854fad915f9bbac6`, `MapTool.Logic/MapFile.cs`.

The reader exposes fields 2 and 3 as `PreviewWidth` and `PreviewHeight`. Its setters rewrite the value as `0,0,width,height`, discarding nonzero first fields. This is canonicalizing tool behavior and cannot be used for byte-identical round-trip.

```text
EvidenceGrade: ImplementationSpecificBehavior
Source: MapTool
AuditStatus: NotRun
```

### CnCNet XNA client

Pinned source: `e6e367bbe04c1a0dc1e34a8fed2856ea3ab7e8c4`, both preview extractors.

The consumers parse only fields 2 and 3 and reject width or height below 1. Fields 0 and 1 are ignored.

```text
EvidenceGrade: ImplementationSpecificBehavior
Source: CnCNet XNA client
AuditStatus: NotRun
```

### ModEnc

Permanent `Preview` revision `oldid=21306` states that the section contains preview size information but does not resolve all four values. Permanent `PreviewPack` revision `oldid=28503` treats dimensions as belonging to `[Preview]`.

```text
EvidenceGrade: ConfirmedCommunityConvention
Source: ModEnc Preview and PreviewPack
AuditStatus: NotRun
```

The community convention supports Preview metadata carrying dimensions but does not settle fields 0 and 1 or runtime strictness.

## 3. Normalized evidence summary

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert copies Map Size and replaces fields 2/3 with generated width/height | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Official-editor writer behavior only. | Preserve all raw fields and source provenance. | `NotRun` |
| WAE writes `0,0,width,height` | `ImplementationSpecificBehavior` | World-Altering Editor | Named writer canonicalization. | Do not treat zero origins as universal. | `NotRun` |
| CNCMaps writes and reads an `x,y,width,height` tuple | `ImplementationSpecificBehavior` | CNCMaps | Named tool interpretation. | Keep behind an explicit metadata profile. | `NotRun` |
| MapTool reads fields 2/3 and rewrites origins to zero | `ImplementationSpecificBehavior` | MapTool | Named tool behavior is not lossless for nonzero origins. | Preserve source text separately. | `NotRun` |
| CnCNet consumers use only fields 2/3 | `ImplementationSpecificBehavior` | CnCNet XNA client | Consumer-specific behavior. | Do not discard fields 0/1 in Core. | `NotRun` |
| Fields 2/3 are the cross-tool width/height candidate | `Underconfirmed` | FinalAlert, WAE, CNCMaps, MapTool, CnCNet, ModEnc | Strong convergence exists, but implementation independence and original-runtime semantics are not established. | Require an explicit interpretation profile. | `NotRun` |
| `0,0,width,height` is the unique original-runtime contract | `Underconfirmed` | Tool writers and community convention | Official editor can inherit nonzero first fields; no runtime source requires zeros. | Never force fields 0/1 to zero during parse. | `NotRun` |
| Original-runtime meaning of fields 0/1 | `Unresolved` | No original-runtime source located | Origin, offset, inherited map values, ignored fields, and unknown tuple roles remain candidates. | Preserve raw values without guessing. | `NotRun` |
| Nonzero fields 0/1 occur in authorized ProjectBaseline samples | `Unresolved` | Audit not executed | Future observation only. | `FutureEvidenceSource: ProjectBaselineAggregateAudit`. | `NotRun` |
| Raw preservation, explicit profiles, checked validation, and no repair | `DefensiveDesign` | Project policy | Project design, not external evidence. | Apply before allocation or interpretation. | `NotRun` |

## 4. Interpretation profiles

### `OffsetAndDimensions`

```text
X      = raw0
Y      = raw1
Width  = raw2
Height = raw3
```

Supported by CNCMaps' rectangle construction and the common `0,0,w,h` writer convention. Overall applicability remains `Underconfirmed`.

### `MapOriginAndDimensions`

Same numeric mapping, but raw0/raw1 are understood as map-relative preview origin fields copied from `[Map] Size`. The official editor's writer behavior supports this as a distinct candidate, but runtime use remains `Unresolved`.

### `DimensionsOnlyLastTwo`

Only raw2/raw3 are semantically used. raw0/raw1 are preserved but ignored by the selected consumer. CnCNet and MapTool consumers demonstrate this `ImplementationSpecificBehavior`.

### `RectangleEdges`

```text
Left   = raw0
Top    = raw1
Right  = raw2
Bottom = raw3
```

No inspected executable source required this interpretation. It remains `Unresolved` because the four-value shape can be mistaken for edge coordinates.

### `UnknownFourTuple`

No geometry is selected; only raw values are exposed.

## 5. Default project policy

The research default is:

- parse all four fields as signed raw candidates;
- require exactly four components for the standard profile;
- interpret fields 2 and 3 as width and height only under an explicit profile;
- do not require fields 0 and 1 to be zero;
- do not use image plausibility to choose a profile;
- do not modify negative or nonzero offsets;
- reject width/height that are zero, negative, overflowed, or above configured limits before allocation;
- retain metadata even when payload is absent or invalid.

```text
PolicyClassification: DefensiveDesign
AuditStatus: NotRun
```

## 6. Numeric and budget constraints

Before multiplication:

- width and height are validated independently;
- maximum width, height, pixel count, and decoded bytes are separate limits;
- conversion to unsigned/allocation length is checked;
- `width × height` uses checked arithmetic;
- multiplying by three is separately checked;
- no allocation is made from an unvalidated product.

An original menu's display dimensions, editor default, or dummy preview dimensions are not format maxima.

## 7. Zero and negative values

Public consumers generally reject width/height below one. The format-layer status is still separate:

- negative raw fields are parseable metadata but invalid for standard dimensions;
- zero width or height produces an invalid dimension candidate;
- empty preview semantics are not inferred from `0×0`;
- negative raw0/raw1 remain preserved and unresolved;
- no absolute-value, clamp, or default substitution is permitted.

## 8. Duplicate and malformed values

Diagnostics distinguish:

- missing `Size`;
- empty `Size`;
- fewer than four fields;
- more than four fields;
- noninteger token;
- integer overflow;
- duplicate `Size` in one physical section;
- multiple `[Preview]` sections;
- cross-layer override;
- valid metadata with absent payload;
- valid payload with invalid metadata.

## 9. Consumer display size is not format size

Scaling, cropping, fitting, aspect-ratio preservation, interpolation, and thumbnail cache dimensions belong to consumers. They do not constrain raw metadata or decoded pixel dimensions.
