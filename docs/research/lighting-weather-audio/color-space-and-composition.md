# Color space and lighting composition

> **Source notice:** ChatGPT Web public-source research. ProjectBaseline was not read. `code_imported: false`.

## Raw versus composed state

```text
raw Lighting text
≠ parsed numeric candidate
≠ logical lighting parameter
≠ palette sample
≠ renderer multiplier
≠ final RGB output
```

Core stores raw values and named interpretation profiles. It does not render, sample palettes, apply gamma, clamp, normalize or choose a profile by visual similarity.

## Source comparison

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert exposes and writes Lighting/Ion fields | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Field/editor behavior only; no final runtime formula or color space. | Preserve source profile. | `NotRun` |
| WAE multiplies RGB by Ambient and applies an editor cap | `ImplementationSpecificBehavior` | WAE | Editor-preview composition. | Comparison profile only. | `NotRun` |
| OpenRA maps fields into TerrainLighting and combines Ground differently | `ImplementationSpecificBehavior` | OpenRA | Target-engine conversion. | Comparison profile only. | `NotRun` |
| Community descriptions of brightness/tint/height fields | `ConfirmedCommunityConvention` | ModEnc/community docs | Naming convention, not executable formula. | Neutral field semantics. | `NotRun` |
| One unique stock runtime formula, clamp, color space and Ground composition | `ConflictingSources` | WAE, OpenRA, editor/community evidence | Public algorithms directly differ and runtime source is absent. | Explicit composition/color-space profiles. | `NotRun` |
| Exact runtime numeric range, rounding and palette interaction | `Unresolved` | No original-runtime source located | No reliable complete candidate. | Preserve raw and defer to renderer adapter. | `NotRun` |
| No trial rendering, no screenshot scoring and raw/component separation | `DefensiveDesign` | Project policy | Determinism and evidence boundary. | Profile chosen before composition. | `NotRun` |

## Required profiles

- `LightingNumericPolicy`
- `LightingCompositionProfile`
- `LightingColorSpaceProfile`
- `LightingClampPolicy`
- `LightingLayerPolicy`
- `PaletteRoleBindingPolicy`

Normal, Ion/Weather, Dominator and extension profiles have independent completeness. Missing alternate fields are not filled from normal Lighting.

## Layer boundaries

ISO palette, unit palette, House remap, shadow tint, local lights, TMP depth, Fog/Shroud, radar/minimap colors, Preview pixels and post-processing remain separate inputs. Darkness does not create Shroud; palette failure does not invalidate raw Lighting; House Color does not alter environment Lighting identity.
