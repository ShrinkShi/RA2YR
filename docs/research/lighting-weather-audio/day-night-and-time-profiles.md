# Day, night, and time profiles

> **Source notice:** ChatGPT Web public-source research. ProjectBaseline was not read. `code_imported: false`.

## Static authored input

The default model is static authored `[Lighting]` plus optional alternate/event profiles. No parser-level autonomous clock is assumed.

## Evidence

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert provides morning/afternoon/evening/night presets and a generated day-night Trigger script | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Official authoring helpers and generated declarative records only. | Preserve as editor preset/script profile. | `NotRun` |
| WAE/OpenRA dynamic-lighting behavior | `ImplementationSpecificBehavior` | Named implementations | Tool/target-engine behavior. | Comparison profiles only. | `NotRun` |
| Trigger-authored lighting changes are a common community/tool convention | `ConfirmedCommunityConvention` | Editor/community catalogs | Exact opcodes, interpolation and runtime timing remain profile-specific. | Declarative command candidates only. | `NotRun` |
| Stock RA2/YR has a general autonomous map-level day/night clock | `Unresolved` | No original-runtime source located | Editor script generation does not prove an autonomous runtime subsystem. | No automatic time progression in Core. | `NotRun` |
| Static versus dynamic/time/weather evidence | `Underconfirmed` | Public tools/community | Runtime applicability and version behavior are incomplete. | Explicit time/profile evidence. | `NotRun` |
| Parser-driven interpolation, wall-clock updates or visual plausibility selection | `DefensiveDesign` | Project policy | Prohibited architecture behavior. | Future deterministic simulation adapter owns timing. | `NotRun` |

## Model

```text
EnvironmentTimeProfileRaw
StaticLightingDescriptor
DynamicLightingCommandCandidate
TimeOfDayEvidence[]
RuntimeEnvironmentStateSnapshot
```

Authored initial Lighting, Trigger commands, campaign/session context, savegame state and renderer interpolation are separate. Real wall clock, frame rate and camera exposure never alter declarative map identity.

## Runtime boundary

A future simulation module may define deterministic ticks, transitions, persistence and replay behavior under an explicit profile. Core parsing does not schedule timers, interpolate fields or mutate static Lighting.
