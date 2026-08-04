# M3-R10 — Map Lighting, Weather, Ambient Audio, Theme, and Presentation-Input Boundaries

> **Source notice:** This dossier was produced by **ChatGPT Web** from public sources. ProjectBaseline was not read. This is not a local Codex Agent artifact. GPL and unclear-license sources were reference-only; no code was copied, translated, mechanically rewritten, or ported. `code_imported: false`.

## Status

- Work type: public-source research only.
- Active directory: `docs/research/lighting-weather-audio/`.
- Implementation: none.
- Compatibility promotion: none.
- ProjectBaseline audit: designed only, not run.
- Unity, RA2/YR, FinalAlert, WAE, XCC, audio, rendering, and map execution: not run.

## Frozen pipeline

```text
lossless map INI
→ raw environment sections and references
→ explicit lighting/weather/audio profiles
→ logical environment descriptor
→ resource-reference candidates
→ presentation-state inputs
→ future renderer/audio/simulation adapters
```

The boundaries are strict:

- the lossless INI layer retains section occurrence, physical order, key occurrence, casing, raw text, whitespace, empty values, and duplicates;
- the Lighting reader does not load a palette or evaluate final colors;
- the palette binder does not execute lighting math;
- the weather reader does not create a storm, particle, sound, damage event, or timer;
- Theme, Sound, Speech, Movie, and EVA references remain logical identifiers;
- local-light binding does not create a Unity `Light`;
- Fog/Shroud metadata does not allocate or mutate a visibility grid;
- Trigger environment references produce declarative command candidates and are not executed;
- Core has no dependency on `UnityEngine` and creates no `Texture`, `Material`, `Shader`, `AudioClip`, `AudioSource`, `Light`, `RenderTexture`, particle object, or `GameObject`.

## Primary conclusions

### 1. `[Lighting]` is a raw profile source, not a final RGB color

Strong stock field candidates are:

```text
Ambient
Red
Green
Blue
Level
Ground
IonAmbient
IonRed
IonGreen
IonBlue
IonLevel
IonGround
```

RA2/YR-oriented tooling also exposes Psychic Dominator profile candidates:

```text
DominatorAmbient
DominatorAmbientChangeRate
DominatorRed
DominatorGreen
DominatorBlue
DominatorLevel
DominatorGround
```

The official FinalSun/FinalAlert editor source exposes normal and Ion/Weather fields as editable raw strings. Its old dialog does not establish a numeric range, clamp, or runtime formula. World-Altering Editor models the values as `double`, includes Ground and Dominator candidates, and implements an editor-preview composition. OpenRA's Gen2 importer converts the same source fields into OpenRA-specific `TerrainLighting` values and merges Ground into Ambient. These implementations disagree at the semantic-adapter layer and therefore cannot be combined by source voting.

Project policy:

```text
Raw lighting text
≠ parsed decimal candidate
≠ logical lighting parameter
≠ palette component
≠ renderer multiplier
≠ final RGB output
```

### 2. Numeric spelling is format evidence

The raw model retains all of the following distinctly:

```text
1
1.0
.75
0.75
0,75
1e-1
+0.750
-0.25
 0.75 
invalid
(empty)
```

No locale-dependent silent parse, fallback-to-one, clamp-to-zero, clamp-to-one, or duplicate last-wins behavior is permitted in the raw reader. Numeric interpretation is performed only by an explicit `LightingNumericPolicy`, with the selected profile and evidence grade serialized in the result.

### 3. No single public implementation proves the stock lighting formula

Observed public behavior includes:

- FinalAlert/FinalSun editor: raw text editing and authoring scripts;
- WAE: multiply Red/Green/Blue by Ambient, scale down when the highest resulting component exceeds an editor-selected cap, then multiply source colors by the resulting map color;
- OpenRA importer: map fields into OpenRA's tint/intensity/height-step model and subtract Ground from Ambient;
- community documentation: describes fields as brightness, tint, level, or height-related parameters, but does not provide original runtime source.

Therefore this dossier proposes explicit profiles:

- `LightingCompositionProfile`;
- `LightingColorSpaceProfile`;
- `LightingClampPolicy`;
- `LightingLayerPolicy`.

No profile is chosen because it produces an image that subjectively resembles the original.

### 4. Static authored lighting is the default model

The map's `[Lighting]` section is best modeled as an authored static profile plus optional alternate profiles. Public official-editor scripts named morning, afternoon, evening, night, and day/night loop are authoring helpers. The day/night loop script constructs triggers and writes action parameters; it does not prove an autonomous stock clock in the map parser.

Recommended separation:

```text
EnvironmentTimeProfileRaw
StaticLightingDescriptor
DynamicLightingCandidate
TimeOfDayEvidence[]
```

No time progression, interpolation, coroutine, frame-driven cycle, or deterministic simulation clock is implemented.

### 5. Weather capability, weather state, visual effect, sound, and damage are separate

`IonStorms` and the RA2 editor label `Weather Storms` are strong authored-capability candidates. Ion lighting fields are alternate presentation inputs. Neither means that a storm is currently active.

```text
Authored capability
≠ initial active state
≠ Trigger Event
≠ Trigger Action
≠ runtime weather instance
≠ cloud/bolt animation
≠ lighting transition
≠ sound playback
≠ radar outage
≠ damage
```

Ares documents an extension-level Lightning Storm model that explicitly separates duration, lighting, activation sound, bolt sounds, clouds, bolts, debris, radar outage, warhead, and damage. This is useful boundary evidence but is not vanilla RA2/YR proof.

### 6. Theme and media are logical references

`[Basic] Theme` is a logical theme candidate. Trigger action metadata publicly exposes separate parameter domains for Sound, Theme, Speech, Movie, and Text. The parser must not scan MIX archives, probe files, load audio, play media, or replace an unknown identifier with a random or first available entry.

```text
LogicalThemeId
LogicalSoundId
LogicalSpeechOrEvaId
LogicalMovieId
CSF label
Audio resource candidate
Playback state
```

remain separate concepts.

No reliable evidence was found for a universal stock map section named `[Ambient]` that directly defines arbitrary positioned ambient emitters. The project must not invent such a format. Spatial ambient audio remains `Unresolved` unless tied to an identified stock section, placement record, Rules type property, or Trigger action.

### 7. Local lights are type/reference inputs, not Unity lights

Public tooling recognizes building/type candidates such as:

```text
LightVisibility
LightIntensity
LightRedTint
LightGreenTint
LightBlueTint
HasSpotlight
```

These belong to type data or explicit extension profiles. They do not establish placement activity, damage-state activation, ownership tint, range conversion, shadow behavior, or Unity representation. Spotlight/searchlight logic is not automatically the same as a generic radial light.

### 8. Fog, Shroud, visual fog, and darkness are different systems

The project distinguishes:

```text
FogOfWar metadata
initial shroud metadata
explored state
current visibility
line of sight
radar visibility
minimap visibility
spectator/replay visibility
editor visibility
weather fog visual effect
lighting darkness
alpha overlay/post-process fog
```

Fog/Shroud gameplay state belongs to future session/simulation/savegame systems. The environment parser retains authored metadata and command candidates only.

### 9. Theater and palette binding remain downstream

Lighting raw parsing can succeed with an unknown Theater or missing palette. Later binding may provide ISO and unit palette roles, but it cannot rewrite Lighting text.

```text
logical Theater
→ palette-role candidates

Lighting raw
→ lighting-profile candidates

palette role + selected lighting profile
→ future presentation descriptor
```

ISO palette, unit palette, House remap, radar colors, preview pixels, shadows, TMP depth, and post-processing are distinct inputs and outputs.

### 10. Trigger environment actions are declarative commands

Candidate domains include:

- music/theme playback;
- sound or speech playback;
- movie/text/fade presentation;
- reveal/shroud changes;
- ambient/light changes;
- weather or Ion-storm activation;
- camera/screen effects;
- extension-only environment actions.

The model retains raw opcode and parameters, editor display-name evidence, version/profile, and target environment domain. It does not execute the command or directly mutate a `ScenarioLightingDescriptor`.

## Responsibility matrix

| Concern | Parser | Semantic binding | Renderer | Audio | Simulation/session | Savegame |
|---|---|---|---|---|---|---|
| Lighting raw text | retain | interpret by policy | consume selected descriptor | none | only if gameplay-coupled profile says so | retain runtime state separately |
| Palette | logical reference only | bind palette role | sample/apply | none | none | normally no |
| Weather capability | retain | classify | visual consumer | audio consumer | creates deterministic runtime state | persist active state if required |
| Theme/Sound | retain logical ID | resolve registry/resource candidate | none | playback | scheduling/trigger ownership | persist playback only if required |
| Local light | retain type/reference | produce logical source | render light | optional hum/loop reference | active/inactive state owner | persist runtime state if required |
| Fog/Shroud metadata | retain | bind policy | display visibility result | none | owns visibility/exploration grid | persist explored/current state |
| Trigger command | retain raw command | classify domain | execute presentation command later | execute audio command later | schedule deterministic effects | persist action/runtime state |

## Recommended dependency graph

```text
ScenarioLightingRaw
→ LightingProfileCandidate

ScenarioTheaterBinding
→ PaletteRoleCandidates

LightingProfileCandidate + PaletteRoleCandidates
→ FuturePresentationLightingDescriptor

ScenarioThemeReferenceRaw / ScenarioSoundReferenceRaw
→ LogicalMediaReference

ScenarioSpecialEnvironmentFlagsRaw + Trigger environment edges
→ EnvironmentCapabilityAndCommandGraph

Rules/object-light fields + placed object references
→ LocalLightSourceCandidates

Visibility metadata + future session rules
→ future visibility initialization inputs
```

No node in this dossier creates final renderer, audio, simulation, visibility, or Unity state.

## Evidence grades

Every material conclusion uses one of:

- `ConfirmedByOfficialRuntimeSource`;
- `ConfirmedByOfficialEditorSource`;
- `ConfirmedByIndependentImplementation`;
- `CommunityDocumented`;
- `ObservedByFutureProjectBaselineAudit`;
- `ConfiguredForProjectPolicy`;
- `Unresolved`.

The official editor is not labeled as original game runtime source. WAE, OpenRA, CNCMaps, MapTool, and clients are not treated as independent runtime proof merely because they agree. Extension documentation is never promoted to vanilla behavior.

## File map

- `layer-and-section-boundaries.md` — section ownership and composition.
- `lighting-field-model.md` — raw fields, numeric candidates, defaults, duplicates, and diagnostics.
- `color-space-and-composition.md` — conflicting lighting algorithms and palette boundaries.
- `day-night-and-time-profiles.md` — static presets, trigger-driven changes, and time evidence.
- `weather-ionstorm-and-specialflags.md` — capability/state/effect separation.
- `ambient-audio-theme-and-media.md` — logical media references and no invented ambient section.
- `local-light-sources-and-object-binding.md` — type, placement, runtime, and rendering boundaries.
- `fog-shroud-and-visibility-boundaries.md` — gameplay visibility versus presentation fog.
- `source-comparison.md` — source lineage, commit, license, and evidence role.
- `implementation-boundaries.md` — Core candidate APIs, policies, diagnostics, limits, and adapters.
- `test-matrix.md` — 160 research-derived test designs.
- `baseline-audit-request.md` — future sanitized read-only audit contract.
- `unresolved-questions.md` — prioritized evidence gaps.

## Explicit non-goals

This dossier does not implement or execute:

- a Lighting reader or numeric parser;
- palette loading or binding;
- lighting formulas, gamma conversion, interpolation, or color grading;
- weather, Ion Storm, clouds, rain, snow, wind, lightning, particles, or damage;
- dynamic day/night;
- Fog/Shroud or visibility grids;
- Theme, Sound, Speech, EVA, Movie, or ambient playback;
- local lights, spotlights, shadows, weapon flashes, or alpha lights;
- Trigger environment actions;
- Unity `Light`, `AudioSource`, `Shader`, `Material`, `Texture`, `RenderTexture`, particle system, or `GameObject`;
- ProjectBaseline reading;
- RA2/YR, FinalAlert, WAE, XCC, or Unity execution;
- image, audio, screenshot, preview, or map generation;
- compatibility matrix, ADR, formal third-party ledger, `.dev-records`, code, tests, or configuration changes.
