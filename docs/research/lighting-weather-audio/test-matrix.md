# Lighting/weather/audio test matrix — 160 design cases

> **Source notice:** Public-source research only. ProjectBaseline was not read. No test implementation or original asset fixture is included. `code_imported: false`.

## Coverage

| Category | Cases |
|---|---:|
| Lighting fields and numeric parsing | 30 |
| Composition, color space and palette boundaries | 24 |
| Day/night, time, weather and SpecialFlags | 26 |
| Theme, Sound, Speech, Movie and ambient media | 20 |
| Local lights and object binding | 18 |
| Fog, Shroud and visibility | 18 |
| Safety, roundtrip, architecture and audit | 24 |
| **Total** | **160** |

## Required design coverage

Tests cover missing/duplicate/empty fields; invariant and locale-ambiguous decimal forms; signed, exponent, overflow and non-finite inputs; exact raw preservation; partial normal/Ion/Dominator profiles; unknown Theater and missing palettes; explicit WAE/OpenRA/editor comparison profiles; no screenshot/plausibility selection; static versus Trigger-authored lighting; editor presets versus autonomous-clock claims; weather capability versus active state/effect/audio/damage; unknown media and case collisions; no invented Ambient section; partial/unknown local-light properties; Spotlight separation; Fog/Shroud versus darkness/visual fog; declarative environment commands; extension isolation; bounds, checked arithmetic, cancellation and no-progress; lossless roundtrip; no Unity/resource/audio/render execution; and Memory/Stream/short-read/MIX equivalence.

## Evidence discipline

Each expected result carries exactly one formal grade from the nine-item vocabulary. Official-editor fixtures test `ConfirmedByOfficialToolSource` behavior only; named tool/extension fixtures test `ImplementationSpecificBehavior`; community-name fixtures test `ConfirmedCommunityConvention`; source disagreements remain `ConflictingSources`; runtime-unsourced behavior stays `Underconfirmed` or `Unresolved`; project safety expectations are `DefensiveDesign`.

A passing synthetic test confirms only the tested project/profile behavior. It does not upgrade original-runtime evidence.

Future aggregate work remains:

```text
AuditStatus: NotRun
FutureEvidenceSource: ProjectBaselineAggregateAudit
```

## Fixture rules

- tiny independent synthetic values only;
- no original maps, palettes, graphics, audio, screenshots or ordered Trigger command fixtures;
- expected numeric/composition results must not call production code;
- no auto-profile probing;
- no locale dependence;
- no raw-data repair;
- no Unity types;
- no compatibility promotion.
