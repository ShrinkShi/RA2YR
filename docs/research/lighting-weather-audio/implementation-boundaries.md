# Implementation boundaries

> **Source notice:** ChatGPT Web public-source research. ProjectBaseline was not read. `code_imported: false`.

## Pipeline and models

```text
LosslessIniDocument
→ EnvironmentSectionsRaw
→ Lighting/Weather/Media/Profile candidates
→ LogicalEnvironmentDescriptor
→ resource-reference candidates
→ future renderer/audio/simulation adapters
```

Suggested models include `ScenarioLightingRaw`, `LightingProfileCandidate`, `WeatherCapabilityRaw`, `EnvironmentCommandCandidate`, `LogicalMediaReference`, `LocalLightSourceCandidate`, `VisibilityMetadataRaw`, `EnvironmentDiagnostic`, `EnvironmentReadLimits` and `EnvironmentRoundtripDescriptor`.

## Formal grades

All evidence values use exactly one of:

```text
ConfirmedByOriginalRuntimeSource
ConfirmedByOfficialToolSource
ConfirmedByMultipleIndependentImplementations
ConfirmedCommunityConvention
ImplementationSpecificBehavior
DefensiveDesign
ConflictingSources
Underconfirmed
Unresolved
```

Source, Notes, Policy and AuditStatus are separate fields. No reviewed claim has original-runtime-source confirmation.

## Project policies

```text
PolicyClassification: DefensiveDesign
AuditStatus: NotRun
```

- preserve exact numeric/media/profile text, duplicates and unknown fields;
- use invariant, explicit numeric policies without locale repair;
- keep normal/Ion/Dominator/extension profiles separate;
- require explicit composition, color-space, clamp and layer policies;
- never choose a formula by screenshot plausibility;
- do not invent an Ambient section or substitute unknown media;
- do not execute weather, Trigger, audio or visibility behavior;
- keep Theater/palette, House remap, local lights, fog/shroud and runtime state downstream;
- use bounded collections, checked arithmetic and deterministic input-mode-equivalent parsing.

## Adapter boundaries

Renderer owns palette/color conversion, local-light representation, shadows, particles and post-processing. Audio owns resource resolution and playback. Simulation/session owns weather instances, deterministic timing, damage, radar/visibility state, Trigger scheduling and save/replay state. Core references none of these services and creates no Unity objects.

## Roundtrip

Preserve raw sections/keys/values, numeric spelling, duplicates, profile incompleteness, unknown media IDs, command parameters and source provenance. Canonical rewrite is never implicit and cannot silently clamp, fill or normalize.
