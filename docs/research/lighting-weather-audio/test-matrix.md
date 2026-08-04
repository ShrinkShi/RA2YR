# Test Matrix

> **Source notice:** ChatGPT Web public-source research. ProjectBaseline was not read. Not a Codex artifact. No GPL or unclear-license code was copied, translated, mechanically rewritten, or ported. `code_imported: false`.

## Matrix contract

This document defines **160** design cases. It does not implement tests. Synthetic fixtures must not copy production map values, palette tuples, media identifiers, object names, Trigger sequences, or external source formulas.

| Group | Count |
|---|---:|
| A — Lighting fields and numeric parsing | 30 |
| B — Composition, color space, and palette boundaries | 24 |
| C — Day, night, time, weather, and SpecialFlags | 26 |
| D — Theme, sound, speech, movie, and ambient media | 20 |
| E — Local light and object binding | 18 |
| F — Fog, Shroud, and visibility | 18 |
| G — Safety, roundtrip, architecture, and audit | 24 |
| **Total** | **160** |

Expected assertions can inspect raw preservation, selected candidates, evidence grades, provenance, diagnostics, budgets, and architecture boundaries. They must not assert a stock rendering formula that remains unresolved.

## A — Lighting fields and numeric parsing

| ID | Design case |
|---|---|
| A01 | Missing `[Lighting]` section produces an absent-profile result without synthesized defaults. |
| A02 | Present but empty `[Lighting]` section is distinct from a missing section. |
| A03 | Two physical `[Lighting]` sections are retained in physical order. |
| A04 | Duplicate `Ambient` keys in one section remain separate occurrences. |
| A05 | Duplicate `Ambient` keys with identical text remain separate but can be classified as equivalent. |
| A06 | Duplicate `Ambient` keys with conflicting values produce a conflict diagnostic. |
| A07 | `Ambient=1` preserves integer spelling and yields an exact decimal candidate. |
| A08 | `Ambient=1.0` remains distinct from `Ambient=1` for roundtrip. |
| A09 | `Ambient=.75` parses only under a policy that permits a missing leading zero. |
| A10 | `Ambient=0.75` parses under the invariant decimal profile. |
| A11 | `Ambient=0,75` produces locale/delimiter ambiguity without silent replacement. |
| A12 | `Ambient=1e-1` is accepted or rejected only by explicit exponent policy. |
| A13 | `Ambient=+0.75` preserves explicit sign. |
| A14 | `Ambient=-0.25` remains negative and is not clamped. |
| A15 | `Ambient=0` remains zero and is not replaced with a default. |
| A16 | `Ambient=2.5` remains above one and is not truncated. |
| A17 | Extremely large finite decimal triggers range diagnostics without overflow allocation. |
| A18 | Exponent overflow candidate is rejected as non-finite while raw text remains. |
| A19 | `NaN` remains raw invalid text unless an explicit compatibility profile supports it. |
| A20 | `Infinity` remains raw invalid text. |
| A21 | Whitespace around a numeric value is retained while trimmed candidate is separate. |
| A22 | Empty numeric value remains empty and is not converted to zero. |
| A23 | Invalid text produces a structured numeric diagnostic. |
| A24 | Missing Red with other normal fields present yields a partial normal profile. |
| A25 | Missing one Ion RGB component yields a partial Ion profile. |
| A26 | Only `Ground` present is preserved without synthesizing Ambient. |
| A27 | Only `IonGround` present is preserved without synthesizing IonAmbient. |
| A28 | Unknown Lighting field is retained with version applicability unresolved. |
| A29 | Known field with case variant is retained and resolved only under explicit key-case policy. |
| A30 | Memory, Stream, short-read Stream, and MIX-window inputs produce identical raw field occurrences. |

## B — Composition, color space, and palette boundaries

| ID | Design case |
|---|---|
| B01 | No composition profile selected returns semantic ambiguity rather than guessed output. |
| B02 | WAE-style preview profile is selectable only by explicit policy and remains labeled independent-tool behavior. |
| B03 | OpenRA-import profile is selectable only as target conversion, not stock runtime behavior. |
| B04 | Ground-subtracted-from-Ambient candidate records full raw-to-derived trace. |
| B05 | Ground retained independently candidate records a conflicting interpretation. |
| B06 | Level-as-height-step candidate is distinct from Level-as-generic-lighting parameter. |
| B07 | Negative Level is preserved without clamp. |
| B08 | Large Ground value that would invert an importer intensity yields a diagnostic. |
| B09 | RGB multiplied by Ambient candidate retains exact decimal intermediates. |
| B10 | Channel-cap policy reports when it would alter a value. |
| B11 | No-clamp policy preserves values exceeding renderer range. |
| B12 | Clamp-to-byte target conversion is downstream and cannot alter Core semantic values. |
| B13 | Unknown color-space profile prevents final color generation. |
| B14 | Indexed-palette-space candidate is distinct from linear RGB candidate. |
| B15 | ISO palette missing does not fail raw Lighting parse. |
| B16 | Unit palette missing does not fail raw Lighting parse. |
| B17 | Unknown Theater does not fail raw Lighting parse. |
| B18 | Palette role binding can fail after successful Lighting semantic parsing. |
| B19 | ISO and unit palette roles remain separate. |
| B20 | House remap color is not substituted for a missing Lighting tint. |
| B21 | Radar/minimap color handling remains a separate layer candidate. |
| B22 | Preview RGB pixels are not used to infer Lighting values. |
| B23 | Shadow/depth processing remains separate from environment Lighting. |
| B24 | Screenshot-similarity heuristic is rejected as a composition selector. |

## C — Day, night, time, weather, and SpecialFlags

| ID | Design case |
|---|---|
| C01 | Static Lighting with no time evidence resolves as static profile only. |
| C02 | Official editor Morning preset is recorded as authoring evidence, not runtime time state. |
| C03 | Official editor Night preset is recorded as authoring evidence, not an autonomous clock. |
| C04 | Bundled day/night Trigger script yields Trigger-cycle candidates without executing them. |
| C05 | Incomplete Trigger day/night chain remains incomplete and is not repaired. |
| C06 | Dark numeric values are not automatically classified as Night. |
| C07 | Bright numeric values are not automatically classified as Day. |
| C08 | Theater token is not used to infer time of day. |
| C09 | Campaign label suggesting night is stored as contextual evidence only. |
| C10 | Client map category suggesting night is stored as client evidence only. |
| C11 | Dynamic profile remains unresolved when no supported Trigger/action profile is selected. |
| C12 | `IonStorms=yes` creates a capability candidate, not active state. |
| C13 | `IonStorms=no` does not delete Ion Lighting fields. |
| C14 | Ion Lighting fields without IonStorms flag remain preserved. |
| C15 | IonStorms flag without Ion Lighting fields yields a capability/profile mismatch diagnostic. |
| C16 | RA2 editor Weather Storm label does not rename raw `IonStorms`. |
| C17 | `WeatherStorms` extension key is retained separately from stock `IonStorms` candidate. |
| C18 | Meteorites field under TS profile is retained as a capability candidate. |
| C19 | Meteorites field under RA2 vanilla profile is marked profile-inapplicable, not deleted. |
| C20 | Partial Ion profile is not completed from normal Lighting. |
| C21 | Dominator partial profile is not completed from normal or Ion fields. |
| C22 | Weather start command candidate does not activate weather during parsing. |
| C23 | Weather stop command without known start remains a declarative orphan. |
| C24 | Visual storm references without simulation profile remain presentation candidates only. |
| C25 | Simulation weather profile without visual resources remains semantically valid with diagnostics. |
| C26 | Weather timing that affects gameplay is marked deterministic-simulation responsibility, not Unity-frame responsibility. |

## D — Theme, sound, speech, movie, and ambient media

| ID | Design case |
|---|---|
| D01 | Missing Basic Theme yields no logical Theme reference and no fallback. |
| D02 | Known Theme raw value binds to a logical registry candidate without loading audio. |
| D03 | Unknown Theme remains an unresolved logical reference. |
| D04 | Theme case collision returns multiple candidates and diagnostics. |
| D05 | Duplicate Theme keys preserve both occurrences. |
| D06 | Client playlist selection does not overwrite Basic Theme. |
| D07 | Play Music Theme Trigger action yields a declarative Theme command candidate. |
| D08 | Play Sound Trigger action yields a Sound-domain command candidate. |
| D09 | Play Speech action remains distinct from Sound. |
| D10 | Play Movie action remains distinct from Theme and Sound. |
| D11 | Text/CSF label remains distinct from speech or movie ID. |
| D12 | Unknown Sound ID remains raw and unresolved. |
| D13 | Unknown Speech/EVA ID remains raw and unresolved. |
| D14 | Unknown Movie ID remains raw and unresolved. |
| D15 | Media resource missing does not rewrite or delete the logical reference. |
| D16 | Multiple resource candidates preserve winner and suppressed provenance only after explicit policy. |
| D17 | File existence is not used to infer logical registry identity. |
| D18 | No universal stock `[Ambient]` section is synthesized when none is present. |
| D19 | Object-attached loop candidate remains type/runtime-state dependent. |
| D20 | Weather activation sound, strike sound, and looping ambience remain separate logical references. |

## E — Local light and object binding

| ID | Design case |
|---|---|
| E01 | Object type with complete LightVisibility/Intensity/RGB fields produces a logical local-light candidate. |
| E02 | Object type with LightIntensity but no visibility yields a partial local-light diagnostic. |
| E03 | Object type with visibility but no intensity yields a partial local-light diagnostic. |
| E04 | Partial local-light tint triplet remains partial. |
| E05 | Negative LightIntensity remains raw and is not clamped. |
| E06 | Above-one LightIntensity remains raw and is not clamped. |
| E07 | Locale-comma local-light value is not silently repaired by the source parser. |
| E08 | LightVisibility unit remains unresolved without explicit profile. |
| E09 | OpenRA range conversion is available only as target-engine conversion evidence. |
| E10 | Missing object type leaves placement and raw reference intact. |
| E11 | Missing placement leaves type-level local-light descriptor intact. |
| E12 | Art/resource missing does not delete local-light fields. |
| E13 | HasSpotlight and generic Light fields produce a profile conflict candidate, not automatic equivalence. |
| E14 | Placement Spotlight field and type HasSpotlight remain distinct. |
| E15 | House Color does not replace local-light tint. |
| E16 | Building art dimensions do not infer light radius. |
| E17 | Powered/damaged/active runtime state is not inferred from field presence. |
| E18 | Extension alpha-light field remains separate from numeric local light and weapon flash. |

## F — Fog, Shroud, and visibility

| ID | Design case |
|---|---|
| F01 | Missing FogOfWar metadata yields no authored visibility override. |
| F02 | FogOfWar valid boolean candidate remains map-authored metadata only. |
| F03 | Invalid FogOfWar text is preserved and not converted to false. |
| F04 | Duplicate FogOfWar values that conflict produce diagnostics. |
| F05 | RA2 editor Shroud label does not rename raw FogOfWar key. |
| F06 | Lobby Shroud option remains session provenance and does not rewrite map metadata. |
| F07 | Map FogOfWar and lobby Shroud conflict is reported without precedence guess. |
| F08 | Reveal All Map action yields a declarative visibility command candidate. |
| F09 | Reveal Around Waypoint with missing waypoint remains dangling. |
| F10 | Reveal Zone action remains distinct from Reveal Around radius semantics. |
| F11 | Grow Shroud One Step remains a command candidate and is not executed. |
| F12 | FogOfWar is not treated as a screen fog shader. |
| F13 | Lighting darkness is not treated as Shroud. |
| F14 | Weather haze is not treated as gameplay FogOfWar. |
| F15 | Current explored cells are not parsed from scenario metadata. |
| F16 | Current visible cells are not parsed from scenario metadata. |
| F17 | Radar outage remains distinct from Fog/Shroud visibility. |
| F18 | Spectator/replay visibility remains session/replay state. |

## G — Safety, roundtrip, architecture, and audit

| ID | Design case |
|---|---|
| G01 | Environment section-count budget is enforced before unbounded allocation. |
| G02 | Field-count budget is enforced with structured diagnostics. |
| G03 | Raw key-length budget is enforced. |
| G04 | Raw value-length budget is enforced. |
| G05 | Numeric-candidate budget per field is enforced. |
| G06 | Media-reference budget is enforced. |
| G07 | Local-light binding budget is enforced. |
| G08 | Environment-command budget is enforced. |
| G09 | Reference-graph node budget is enforced. |
| G10 | Reference-graph edge budget is enforced. |
| G11 | Diagnostic-count budget is enforced. |
| G12 | Checked arithmetic catches occurrence-index overflow. |
| G13 | Checked arithmetic catches source-span length overflow. |
| G14 | Short-read Stream never assumes a full buffer read. |
| G15 | Non-seekable Stream follows the same parser state machine. |
| G16 | Truncated input returns structured failure without no-progress loop. |
| G17 | No-progress protection terminates a stalled tokenizer/state machine. |
| G18 | Raw objects remain unchanged after failed semantic binding. |
| G19 | Lossless roundtrip preserves duplicate Lighting sections and keys. |
| G20 | Lossless roundtrip preserves numeric spelling, whitespace, invalid values, and unknown fields. |
| G21 | Lossless roundtrip preserves media ID casing and Trigger raw parameters. |
| G22 | Canonical editor rewrite is opt-in and distinct from lossless identity. |
| G23 | Core assembly/API surface contains no UnityEngine, Light, AudioSource, Shader, Material, Texture, or GameObject dependency. |
| G24 | Sanitized audit output contains only allowed aggregate categories and cannot reconstruct a map. |

## Required cross-mode equivalence

The matrix includes explicit equivalence checks for:

```text
Memory
Stream
non-seekable Stream
short-read Stream
bounded MIX window
```

All paths must use the same logical state machine and produce equivalent raw occurrences, source order, numeric candidates, reference candidates, and diagnostics.

## Expected architecture assertions

- no environment parser API exposes Unity types;
- no parse or binding test creates a Light, AudioSource, Shader, Material, Texture, particle, or GameObject;
- weather and visibility tests do not create gameplay state;
- command tests do not execute Trigger actions;
- media tests do not load or play resources;
- composition tests require an explicit profile;
- audit fixtures cannot emit prohibited per-map values.

## Evidence discipline

A passing synthetic test establishes implementation behavior only. It does not upgrade an evidence grade to official runtime confirmation. Future ProjectBaseline observations remain `ObservedByFutureProjectBaselineAudit`.
