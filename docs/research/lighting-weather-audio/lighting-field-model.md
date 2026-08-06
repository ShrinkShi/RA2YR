# Lighting Field Model

> **Source notice:** ChatGPT Web public-source research. ProjectBaseline was not read. Not a Codex artifact. No GPL or unclear-license code was copied, translated, mechanically rewritten, or ported. `code_imported: false`.

## Scope

This file defines raw and candidate models for scenario `[Lighting]` data. It does not select a rendering formula.

## Public-source field inventory

### Normal profile candidates

| Raw key | Candidate role | Evidence |
|---|---|---|
| `Ambient` | global/ambient brightness input | official editor field; WAE model; OpenRA importer; community docs |
| `Red` | red-channel/tint input | official editor field; WAE model; OpenRA importer |
| `Green` | green-channel/tint input | official editor field; WAE model; OpenRA importer |
| `Blue` | blue-channel/tint input | official editor field; WAE model; OpenRA importer |
| `Level` | height/level-related lighting input | official editor field; WAE model; OpenRA importer |
| `Ground` | ground-layer correction/input | WAE model; OpenRA importer; community docs; not exposed by the old official Lighting dialog examined |

### Ion/Weather profile candidates

| Raw key | Candidate role | Evidence |
|---|---|---|
| `IonAmbient` | alternate storm-profile ambient | official editor; WAE; community docs |
| `IonRed` | alternate storm-profile red | official editor; WAE |
| `IonGreen` | alternate storm-profile green | official editor; WAE |
| `IonBlue` | alternate storm-profile blue | official editor; WAE |
| `IonLevel` | alternate storm-profile level | official editor; WAE |
| `IonGround` | alternate storm-profile ground | WAE and community/tool evidence; old official dialog examined does not expose it |

In RA2 mode, the official editor changes the visible label from Ion Storm settings to Weather Storm settings. This is editor terminology evidence, not proof that `Ion*` fields mean a currently active storm.

### Psychic Dominator/profile-extension candidates

WAE exposes nullable RA2/YR-oriented candidates:

```text
DominatorAmbient
DominatorAmbientChangeRate
DominatorRed
DominatorGreen
DominatorBlue
DominatorLevel
DominatorGround
```

The official editor's bundled authoring scripts also reference several Dominator fields. Their presence establishes authoring/profile evidence, but exact stock runtime defaults, interpolation, and applicability remain unresolved without runtime source.

### Unknown fields

Every unknown key remains:

```text
ScenarioLightingFieldRaw
{
    RawKey
    RawValue
    SectionOccurrence
    KeyOccurrence
    PhysicalOrder
    SourceProvenance
}
```

Unknown fields are not discarded, renamed, or copied into a known field.

## Raw numeric model

Recommended raw type:

```text
ScenarioLightingFieldRaw
- RawKey
- RawValue
- TrimmedCandidate
- NumericCandidate[]
- SelectedNumericProfile? 
- ParseDiagnostics[]
- EvidenceGrade
- SourceOccurrence
```

The raw text is authoritative for roundtrip. Numeric candidates are derived views.

## Required spelling preservation

The parser must distinguish and retain:

| Example | Notes |
|---|---|
| `1` | integer spelling, possible decimal value candidate |
| `1.0` | explicit fractional spelling |
| `.75` | missing leading zero, seen in official editor scripts |
| `0.75` | invariant decimal point |
| `0,75` | locale-comma candidate; may also be a delimiter error |
| `1e-1` | scientific notation candidate |
| `+0.75` | explicit sign |
| `-0.25` | negative candidate |
| ` 0.75 ` | whitespace-bearing raw value |
| `0.750000 ` | trailing whitespace, also observed in bundled scripts |
| empty | authored empty value |
| invalid text | parse failure without repair |

## Numeric interpretation profiles

Suggested explicit profiles:

### `InvariantDecimalProfile`

- optional sign;
- decimal point `.`;
- optional exponent if policy permits;
- no thousands separator;
- exact decimal candidate retained;
- no range clamp.

### `LegacyLocaleCommaCandidateProfile`

- recognizes a single comma as a possible decimal separator only when explicitly selected;
- never silently replaces commas in the raw value;
- records ambiguity with list/delimiter syntax;
- does not become the default because one importer repairs commas for local lights.

### `ToolSpecificDoubleProfile`

- records a known tool's use of `double` or `float`;
- useful for compatibility tests;
- not treated as stock-format proof.

### `OpaqueNumericTextProfile`

- raw field retained;
- no semantic numeric value selected;
- suitable for invalid or unsupported notation.

## Signedness and range

Public evidence supports decimal values below zero and above one as syntactically meaningful candidates. Official editor scripts contain values above `1.0`; WAE's model does not intrinsically limit every field to `0..1`; OpenRA converts values into a different model.

Therefore default project policy is:

```text
No clamp
No automatic absolute value
No automatic zero floor
No automatic one ceiling
No fallback to editor defaults
```

Range evaluation is a diagnostic analysis, not raw parse behavior.

Suggested diagnostic categories:

```text
NegativeCandidate
ZeroCandidate
UnitIntervalCandidate
AboveOneCandidate
LargeMagnitudeCandidate
NonFiniteCandidate
ExponentCandidate
LocaleAmbiguousCandidate
InvalidCandidate
```

`NaN`, positive infinity, and negative infinity should not be accepted as finite semantic values unless an explicit compatibility profile documents them. Raw text remains preserved.

## Defaults

Potential defaults are profile-specific and require evidence. The following must not be conflated:

- field absent from the map;
- editor UI displaying zero because its in-memory model was initialized to zero;
- tool fallback to white/no lighting;
- community-documented default of `1.0` for some color/ambient values;
- runtime default from global rules or internal constants;
- canonical writer emitting a value;
- preview renderer choosing a safe fallback.

Recommended representation:

```text
DefaultCandidate
- Value
- AppliesWhen
- VersionProfile
- Source
- EvidenceGrade
```

No missing field is filled during raw parsing.

## Duplicate sections and keys

The raw document can contain:

```ini
[Lighting]
Ambient=0.8
Ambient=0.9

[Lighting]
Red=1.1
```

The field reader produces three occurrences, not one dictionary value.

A later policy can analyze:

- first physical occurrence;
- last physical occurrence;
- first valid numeric occurrence;
- last valid numeric occurrence;
- editor/tool-specific behavior;
- unresolved ambiguity.

The selection result must include suppressed candidates and diagnostics.

## Partial profiles

A valid raw document may contain only some profile fields:

```ini
[Lighting]
Ambient=0.7
IonRed=0.5
```

This is not automatically repaired. The semantic layer records:

```text
NormalProfileCompleteness
IonProfileCompleteness
DominatorProfileCompleteness
UnknownFieldCount
```

Potential states:

```text
Absent
Partial
CompleteBySelectedProfile
CompleteOnlyWithDefaults
Ambiguous
Invalid
```

## Field/profile compatibility

The same field name can have different applicability:

- TS normal lighting;
- TS Ion Storm lighting;
- RA2/YR Weather/Lightning Storm alternate lighting;
- YR Psychic Dominator alternate lighting;
- Ares/Phobos extension lighting;
- editor-preview-only profile;
- importer-specific conversion.

Each field candidate carries a `VersionProfile`. A field is never deleted merely because it is unknown to the selected vanilla profile.

## Reader and writer observations

### Official editor

The examined official Lighting dialog reads and writes the normal and Ion fields as text. This confirms field names and authoring behavior. It does not reveal a numeric parser, runtime range, or composition formula.

The bundled scripts write fixed decimal strings, including leading-dot and above-one values. Script content is authoring evidence and can contain script-specific errors or mismatched names; it is not a normative format specification.

### World-Altering Editor

WAE stores fields as `double`, writes them through its INI property system, and displays three decimal places in the lighting UI. UI formatting is a canonical editor presentation and can destroy original numeric spelling if used as a default rewrite.

WAE also returns white/no-lighting fallback when an optional Dominator component group is incomplete. That is editor preview behavior, not raw parser policy.

### OpenRA importer

OpenRA reads recognized normal-profile fields as `float`, ignores unrecognized global lighting keys for its target format, maps them to OpenRA-specific names, and merges Ground into Ambient. For local lamp values it explicitly replaces commas with periods. This is importer behavior and demonstrates why global raw parsing and compatibility conversion must be separate.

## Candidate result model

```text
ScenarioLightingRaw
- SectionOccurrences[]
- Fields[]
- NormalProfileCandidate
- IonProfileCandidate
- DominatorProfileCandidate
- UnknownFields[]
- DuplicateAnalysis
- NumericDiagnostics[]
- SourceProvenance

ScenarioLightingProfileCandidate
- ProfileKind
- FieldCandidates
- Completeness
- SelectedNumericProfile?
- DefaultCandidates[]
- Evidence[]
- Diagnostics[]
```

## Policy objects

- `LightingFieldPolicy` — known-field registry and version applicability.
- `LightingNumericPolicy` — decimal syntax, locale, exponent, finite-value rules.
- `LightingDuplicatePolicy` — occurrence selection without raw loss.
- `LightingDefaultPolicy` — explicit, evidence-graded default candidates.
- `EnvironmentRoundtripPolicy` — identity versus canonical rewrite.

## Safety limits

Recommended budgets:

- maximum `[Lighting]` section occurrences;
- maximum fields per section;
- maximum raw key length;
- maximum raw value length;
- maximum numeric candidate count per field;
- maximum diagnostics;
- checked arithmetic for indexes and source spans;
- no-progress protection for streaming tokenization.

Exceeding a budget returns a structured failure or truncation diagnostic according to explicit policy. It never silently drops later fields.
