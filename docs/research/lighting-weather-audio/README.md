# M3-R10 — Lighting, weather, ambient audio, and presentation-input boundaries

> **Source notice:** ChatGPT Web public-source research only. ProjectBaseline was not read. This is not a local Codex Agent artifact. GPL and unclear-license sources are reference-only; no code was copied, translated, mechanically rewritten, or ported. `code_imported: false`.

## Boundary

```text
lossless map INI
→ raw environment sections/references
→ explicit lighting/weather/audio profiles
→ logical environment descriptor
→ resource-reference candidates
→ presentation-state inputs
→ future renderer/audio/simulation adapters
```

Core does not create renderer, palette, audio, visibility, weather, Trigger, simulation, session or Unity state.

## Findings

- `[Lighting]` is raw authored profile input, not final RGB output. Normal, Ion/Weather, Dominator and extension fields retain exact text, duplicates and profile provenance.
- Numeric spelling, locale ambiguity, exponent/sign/empty/invalid forms remain distinct. No clamp or default repair occurs in the raw layer.
- FinalAlert exposes fields and authoring behavior only. WAE and OpenRA use different composition models; no unique runtime formula is established.
- Ambient/Red/Green/Blue/Level/Ground, palette roles, House remap, shadows, TMP depth and post-processing remain separate.
- Morning/afternoon/evening/night editor presets and generated Trigger chains are authoring behavior, not proof of an autonomous runtime day/night clock.
- `IonStorms`/`Weather Storms`, Ion fields and extension Lightning Storm data distinguish capability, active state, command, visual effects, audio, radar effects and damage.
- Theme, Sound, Speech/EVA, Movie, CSF text, resource candidates and playback state are separate logical identities.
- No universal stock positioned-audio `[Ambient]` contract was established; it remains unresolved rather than invented.
- Local-light type fields, object placement, runtime powered/damaged state, logical descriptor and rendered light are separate.
- FogOfWar, initial Shroud, explored/current LOS, radar/minimap visibility, spectator state, weather fog and post-process fog are separate systems.
- Trigger environment operations remain declarative command candidates; they do not mutate static descriptors during parsing.

## Formal evidence grades

Formal `Grade` fields use only:

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

No complete original RA2/YR runtime source was found, so no claim reaches `ConfirmedByOriginalRuntimeSource`. FinalSun/FinalAlert behavior is `ConfirmedByOfficialToolSource`. WAE, OpenRA, clients, renderers and extensions are named `ImplementationSpecificBehavior`. Stable community naming uses `ConfirmedCommunityConvention`; cross-tool candidates remain `Underconfirmed` without proven lineage/runtime applicability; composition, color-space, weather and visibility disagreements use `ConflictingSources`.

Raw preservation, explicit numeric/composition/color-space/profile selection, no trial rendering, no unknown-media substitution, no weather/audio/visibility execution and fail-closed binding are `DefensiveDesign`.

Future ProjectBaseline work is separate:

```text
AuditStatus: NotRun
FutureEvidenceSource: ProjectBaselineAggregateAudit
```

These fields do not imply that ProjectBaseline was read or observed and cannot automatically become runtime evidence or promote compatibility.

## Normalized claims

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert Lighting/Ion fields, labels and authoring scripts | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Official editor behavior only. | Named editor profile; no runtime formula inference. | `NotRun` |
| WAE preview composition and local-light behavior | `ImplementationSpecificBehavior` | WAE | Editor-specific. | Comparison profile only. | `NotRun` |
| OpenRA TerrainLighting conversion | `ImplementationSpecificBehavior` | OpenRA | Target-engine conversion, not source semantics. | Comparison profile only. | `NotRun` |
| Common Lighting fields and media-reference conventions | `ConfirmedCommunityConvention` | Tools/community docs | Convention only. | Preserve product/profile provenance. | `NotRun` |
| Stock numeric ranges, row-independent composition and weather/audio runtime semantics | `Underconfirmed` | Tool convergence/community | Runtime applicability is incomplete. | Explicit profile. | `NotRun` |
| WAE/OpenRA/editor composition, Ground handling and color-space models | `ConflictingSources` | Public implementations | Algorithms differ directly. | Never choose by screenshot plausibility. | `NotRun` |
| Exact runtime lighting formula, day/night clock, weather state machine, visibility and playback | `Unresolved` | No runtime source located | No complete contract. | Future adapters. | `NotRun` |
| Raw/environment layering and no execution/repair | `DefensiveDesign` | Project policy | Preservation and architecture. | Fail closed. | `NotRun` |

## Non-goals

No Lighting parser implementation, palette/composition algorithm, weather/Ion/day-night, Fog/Shroud grid, audio/media playback, local lights, Trigger execution, renderer, Unity, code, tests, configuration, ProjectBaseline access, compatibility change, map/image/audio generation or runtime/editor execution is included.
