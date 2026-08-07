# Local Light Sources and Object Binding

> **Source notice:** ChatGPT Web public-source research. ProjectBaseline was not read. Not a Codex artifact. No GPL or unclear-license code was copied, translated, mechanically rewritten, or ported. `code_imported: false`.

## Scope

This file studies logical local-light inputs associated with object types and placements. It does not create renderer lights or gameplay effects.

## Public field candidates

World-Altering Editor's building-type model exposes:

```text
LightIntensity
LightVisibility
LightRedTint
LightGreenTint
LightBlueTint
HasSpotlight
```

OpenRA's Gen2 importer recognizes the five `Light*` numeric fields for a configured list of lamp actor types and converts them into its target-engine `TerrainLightSource` representation. It converts `LightVisibility` from a source unit into an OpenRA range and repairs comma decimals for the other local-light values.

These facts establish:

- public tools recognize type-level local-light candidates;
- local-light fields are not part of the map-global `[Lighting]` tuple;
- conversion units and formulas can be target-engine-specific;
- locale repair performed by an importer is not a raw parser contract.

They do not establish a complete stock runtime formula.

## Type versus placement versus runtime state

```text
ObjectType light property
≠ placed object record
≠ runtime object instance
≠ current powered state
≠ damage state
≠ active animation state
≠ rendered light
```

Recommended graph:

```text
Rules object type
→ ScenarioLocalLightSourceRaw candidate

placement record
→ placed logical object reference

bound type + placement
→ ScenarioLocalLightBindingResult

future simulation state
→ active/inactive local-light state

future renderer
→ rendered light or palette effect
```

## Candidate raw model

```text
ScenarioLocalLightSourceRaw
- ObjectTypeLogicalId
- FieldOccurrences[]
- LightVisibilityRaw?
- LightIntensityRaw?
- LightRedTintRaw?
- LightGreenTintRaw?
- LightBlueTintRaw?
- SpotlightRaw?
- UnknownLightFields[]
- SourceProvenance
- VersionProfile
- EvidenceGrade
```

Raw numeric spelling and duplicates follow the same preservation rules as global Lighting.

## Binding model

```text
ScenarioLocalLightBindingResult
- PlacementReference
- ObjectTypeReference
- BoundTypeDescriptor?
- LocalLightCandidate?
- InitialStateCandidates[]
- ResourceReferences[]
- Evidence[]
- Diagnostics[]
```

The binder does not create a point light, range, shadow map, cookie, material, alpha image, or audio emitter.

## LightVisibility

Candidate interpretations include:

- source-radius unit;
- diameter or visibility distance;
- cell, pixel, lepton, or engine-specific distance;
- lookup-table or falloff threshold;
- editor-preview radius.

OpenRA converts a value for its own target-engine range. That conversion is not adopted as the Core source-unit formula.

Required representation:

```text
RawVisibility
UnitCandidates[]
SelectedUnitProfile?
ConvertedTargetValues[]
EvidenceGrade
```

No radius is inferred from building art or foundation size.

## LightIntensity and tints

Candidate roles:

- normalized multiplier;
- additive intensity;
- palette contribution;
- editor-preview factor;
- target-engine terrain-light value.

No clamp to `0..1` is allowed in raw or semantic parsing. Negative and above-one values remain candidates with diagnostics.

Tints do not become House color when missing. House ownership does not alter local-light tint unless a specific profile provides that behavior.

## Spotlight/searchlight boundary

`HasSpotlight`, spotlight-related placement fields, trigger actions, and generic Light* fields are not equivalent.

Potential distinctions:

```text
Generic radial/local light
Building spotlight/searchlight system
Trigger-controlled spotlight behavior
Weapon/projectile spotlight
Editor visualization
Extension spotlight logic
```

A future binder must select a profile based on type and version evidence. It cannot treat every spotlight as a stationary radial light.

## Active and inactive states

Potential state inputs:

- powered versus unpowered;
- constructed versus under construction;
- active versus deactivated;
- intact versus damaged/destroyed;
- owner or house state;
- animation frame/state;
- Trigger command;
- day/night or weather state;
- extension-specific toggles.

The map placement typically supplies only authored initial object information. Current state belongs to simulation and savegame.

Recommended candidate:

```text
LocalLightInitialStateCandidate
- SourceKind
- RawState
- Confidence/EvidenceGrade
```

No default active state is inferred solely from the existence of LightIntensity.

## Animation-attached and alpha lights

Ares documents extension and restored behaviors involving alpha-light images and moving object effects. These are separate from numeric Light* fields.

```text
AlphaImage/light overlay
≠ numeric local-light source
≠ weapon flash
≠ projectile glow
≠ particle light
```

Any such feature is tagged as an extension or separate object-effect profile.

## Weapon flashes and projectiles

Weapon muzzle flashes, lasers, beams, projectile glows, impact flashes, and radiation effects belong to weapon/projectile/animation systems. They can generate presentation-light candidates later, but are not scenario local-light fields.

## Terrain and overlay lights

Potential light-emitting terrain or overlay objects require explicit type-property evidence. The placement itself does not define intensity or tint unless a specific format profile says so.

Missing object type or missing Art does not delete the placement or local-light raw data.

## Resource boundary

A local-light descriptor may reference:

- no resource, only numeric lighting;
- an alpha SHP/image;
- an animation;
- a spotlight graphic;
- a sound loop;
- an extension-defined resource.

Resource binding is downstream. Missing resources return diagnostics and unresolved candidates.

## Renderer boundary

Future renderer responsibilities can include:

- choosing palette-based versus true-color light representation;
- range/falloff conversion;
- layer masks;
- shadow behavior;
- batching;
- light activation transitions;
- interpolation;
- performance budgets;
- Unity representation.

Core does not decide whether the final object is a Unity 2D Light, shader parameter, palette modification, additive sprite, or custom tile illumination field.

## Simulation boundary

Future simulation/session owns:

- power and deactivation state;
- damage/destruction state;
- Trigger-controlled state;
- deterministic timing;
- ownership where gameplay relevant;
- savegame/replay state.

Renderer state follows authoritative simulation state when coupled.

## Consistency analysis

Report without repair:

- LightIntensity without LightVisibility;
- visibility without intensity;
- partial tint triplet;
- invalid or locale-ambiguous numeric text;
- object type missing;
- placement missing;
- object placed outside map domain;
- multiple type definitions;
- Spotlight and generic-light conflict;
- extension local-light field under vanilla profile;
- resource reference missing;
- multiple placements sharing one type descriptor;
- active-state source conflict.

## Roundtrip

Preserve:

- raw type section and fields;
- duplicates;
- numeric spelling;
- unknown fields;
- placement records;
- map-local type overrides;
- extension fields;
- unresolved type references.

No default writer writes OpenRA target fields back into the source map.

## Policies

- `LocalLightBindingPolicy`;
- `LocalLightNumericPolicy`;
- `LocalLightUnitPolicy`;
- `LocalLightInitialStatePolicy`;
- `SpotlightProfilePolicy`;
- `EnvironmentRoundtripPolicy`.

## Diagnostics

- `LocalLightTypeMissing`;
- `LocalLightPlacementMissing`;
- `LocalLightIntensityInvalid`;
- `LocalLightVisibilityUnitUnresolved`;
- `LocalLightTintPartial`;
- `LocalLightStateUnresolved`;
- `SpotlightProfileConflict`;
- `LocalLightResourceMissing`;
- `ExtensionLocalLightInVanillaMode`;
- `TargetEngineConversionNotSourceSemantics`.

## Non-goals

No Unity `Light`, radius, attenuation curve, shadow, cookie, alpha image, shader, material, particle, object activation, or local audio emitter is created.
