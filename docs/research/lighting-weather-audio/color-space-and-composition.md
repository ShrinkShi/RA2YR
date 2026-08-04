# Color Space and Composition

> **Source notice:** ChatGPT Web public-source research. ProjectBaseline was not read. Not a Codex artifact. No GPL or unclear-license code was copied, translated, mechanically rewritten, or ported. `code_imported: false`.

## Problem statement

The names `Red`, `Green`, `Blue`, `Ambient`, `Level`, and `Ground` do not specify a complete rendering algorithm. Public implementations expose incompatible adapters. This file records those conflicts without selecting a winner.

## Required separation

```text
Raw lighting value
Logical lighting parameter
Palette index
Palette RGB component
Color-space value
Renderer multiplier
Height correction
Post-process value
Unit remap
House color
Shadow
Depth plane
Fog/Shroud result
Preview pixel
Radar/minimap color
```

No arrow between these concepts is implicit.

## Public implementation profile: official editor

The examined FinalSun/FinalAlert Lighting dialog:

- exposes text controls for Ambient, Level, Red, Green, Blue and Ion counterparts;
- writes the entered text back into `[Lighting]`;
- changes the alternate-profile label to Weather Storm settings in RA2 mode;
- does not expose a final palette-composition formula in this dialog;
- does not establish a numeric clamp or color space.

Evidence grade:

- field names and editor write behavior: `ConfirmedByOfficialEditorSource`;
- game runtime composition: `Unresolved`.

## Public implementation profile: World-Altering Editor preview

WAE's examined model computes a preview color by:

1. multiplying each RGB field by Ambient;
2. finding the highest resulting component;
3. scaling all three down if the highest component exceeds an editor-selected total ambient cap;
4. multiplying source RGB components by the resulting map color;
5. clamping output channels to byte range for certain preview paths.

It also exposes:

- normal, Ion Storm, Dominator, and no-lighting preview modes;
- Level and Ground as separately retrievable values;
- average-channel ambient vectors for some renderer paths;
- optional extra-light additions.

This is valuable independent implementation evidence. It is not original runtime source. The cap, conversion to bytes, and preview-path choices must not be copied as the default Core formula.

Evidence grade: `ConfirmedByIndependentImplementation`.

## Public implementation profile: OpenRA Gen2 importer

The examined OpenRA importer maps:

```text
Red     → RedTint
Green   → GreenTint
Blue    → BlueTint
Ambient → Intensity
Level   → HeightStep
```

It does not directly emit Ground. Instead, when Ground is present, it subtracts Ground from Ambient before emitting OpenRA target data. It omits target fields that match its own target-format defaults.

This behavior is a conversion from Westwood map data into OpenRA's engine model, not a statement that the original runtime performs identical math.

Evidence grade: `ConfirmedByIndependentImplementation`.

## Conflict table

| Question | Official editor | WAE preview | OpenRA importer | Project result |
|---|---|---|---|---|
| Is Ambient multiplied into RGB? | not shown | yes | target intensity field | unresolved stock formula |
| Is Ground independent? | old dialog not exposed | retained independently | subtracted from Ambient | explicit profile required |
| Is Level a height step? | name only | separately consumed | converted to HeightStep | strong candidate, not runtime-confirmed |
| Is there a cap? | not shown | preview cap | target engine rules | no default clamp |
| Is output sRGB/linear? | not shown | XNA byte/vector paths | OpenRA target model | unresolved |
| Are palettes transformed or pixels tinted? | not shown | preview color multiplication | target trait | explicit layer policy |
| Are Ion values alternate profile data? | yes, editor group | yes | importer examined normal profile only | strong alternate-profile candidate |

## Candidate composition families

### Profile A — channel multiplier after palette lookup

```text
palette index
→ palette RGB
→ per-channel lighting multiplier
→ clamp/quantize
```

This resembles WAE preview paths but is not promoted to stock runtime.

### Profile B — palette transformation before indexed rendering

```text
base palette
→ generate lit palette
→ indexed sprite/tile lookup
```

This is plausible for an indexed renderer and may better match historical implementation constraints, but public evidence in this dossier does not confirm the exact stock pipeline.

### Profile C — ambient plus channel tint

```text
RGB tint contribution
+ ambient contribution
+ layer/height correction
→ output
```

Some community descriptions can be read this way. Exact algebra remains unresolved.

### Profile D — fixed-point component math

```text
raw decimal
→ fixed-point scale candidate
→ integer component math
→ bounded palette component
```

Potentially relevant for deterministic legacy code, but no original runtime source has been established.

### Profile E — importer/modern-renderer adaptation

```text
Westwood fields
→ target engine lighting trait
→ target engine color space and shader
```

Useful for compatibility adapters, never treated as a stock algorithm.

## Color-space profiles

Recommended explicit type:

```text
ScenarioColorSpaceProfile
```

Candidate values:

```text
UnknownIndexedPaletteSpace
LegacyIntegerComponentSpace
NormalizedLinearCandidate
NormalizedGammaEncodedCandidate
TargetEngineSrgb
EditorPreviewXnaColor
ConfiguredCustom
```

The selected profile includes evidence and conversion diagnostics. Core retains exact decimal candidates even when a renderer later converts to `float`.

## Palette roles

Theater binding can yield separate roles:

```text
IsoPaletteRole
UnitPaletteRole
OverlayPaletteRoleCandidate
AnimationPaletteRoleCandidate
CustomPaletteRoleCandidate
```

Open questions:

- whether normal lighting applies identically to ISO and unit palettes;
- whether remap ranges are transformed before or after House-color substitution;
- whether shadows are palette entries, alpha/dither operations, or separate draw modes;
- whether aircraft use the same Level/Ground correction;
- whether radar/minimap colors are lit;
- whether preview pixels are already baked.

No palette absence causes raw Lighting parse failure.

## Layer profiles

Recommended `LightingLayerPolicy` records independent candidates for:

- terrain/TMP pixels;
- overlays and resources;
- structures;
- vehicles and voxel output;
- infantry;
- aircraft;
- animations and particles;
- shadows;
- remap pixels;
- UI, radar, minimap, and preview.

A tool applying one multiplier to every rendered object is evidence of that tool, not proof that all stock layers are identical.

## Level and Ground

`Level` and `Ground` are particularly ambiguous.

Candidate roles include:

- per-height-step brightness adjustment;
- terrain-ground offset;
- object-versus-ground separation;
- palette generation parameter;
- target-engine intensity correction;
- unused or version-specific field.

OpenRA's `Level → HeightStep` and `Ambient -= Ground` conversion is explicit importer behavior. WAE retrieves Level and Ground separately. The stock relationship remains unresolved.

Recommended model:

```text
LightingHeightInputs
- LevelRaw
- GroundRaw
- HeightInterpretationCandidates[]
- SelectedProfile?
- Evidence[]
```

## Ion and Dominator composition

Alternate profiles should reuse neither the normal profile's defaults nor formula automatically.

```text
NormalProfile
IonOrWeatherProfile
DominatorProfile
ExtensionProfile[]
```

Each profile has independent completeness, numeric interpretation, and composition selection. Missing IonGreen is not filled from normal Green unless an explicit profile says so.

## Remap and House color

House remap is not a lighting input identity.

```text
House logical color
→ remap ramp/table candidate
→ object remap pixels
```

Lighting may later affect the remapped output, but the ordering is an unresolved rendering question. The environment binder must not:

- select a House from a tint;
- replace a missing Light tint with House color;
- generate a remap table;
- treat map RGB lighting as player color.

## Shadows and depth

TMP depth planes, voxel depth, sprite shadows, alpha images, and terrain height are separate from map lighting.

Potential renderer orderings include:

```text
shade/depth → lighting
lighting → shadow overlay
palette lighting → shadow palette mode
```

No ordering is selected in this research.

## Fog and post-processing

Gameplay visibility and visual post-processing are downstream, independent operations. A dark Lighting profile does not imply Shroud. A visual fog shader does not imply FogOfWar.

Suggested conceptual order only:

```text
asset decode
→ palette/remap resolution
→ environment lighting profile
→ layer-specific shadow/depth handling
→ visibility masking
→ optional renderer post-processing
```

This order is not a stock-runtime claim; adapters may expose alternative orderings.

## Determinism and precision

Raw semantic candidates should prefer an exact decimal representation. A future renderer may convert to float. A future gameplay-affecting weather system must use deterministic state and timing independent of Unity frame rate.

Risks to record:

- decimal-to-binary conversion;
- rounding mode;
- fixed-point scale;
- component multiplication order;
- clamp timing;
- gamma conversion timing;
- byte quantization;
- interpolation and easing;
- platform-specific shader precision.

## Explicit policies

```text
LightingCompositionPolicy
LightingColorSpacePolicy
LightingClampPolicy
LightingLayerPolicy
LightingPrecisionPolicy
```

Each policy is passed explicitly and serializable. No heuristic selects a policy based on histogram, screenshot similarity, palette availability, or whether the output looks plausible.

## Diagnostics

Recommended diagnostics include:

- `CompositionProfileNotSelected`;
- `ConflictingIndependentImplementations`;
- `GroundInterpretationUnresolved`;
- `LevelInterpretationUnresolved`;
- `ColorSpaceUnresolved`;
- `PaletteRoleMissing`;
- `PaletteRoleAmbiguous`;
- `LayerApplicabilityUnknown`;
- `ClampWouldAlterValue`;
- `QuantizationWouldLosePrecision`;
- `PartialAlternateProfile`;
- `RemapOrderingUnresolved`.

All diagnostics retain raw-to-derived trace.
