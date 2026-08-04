# `[Basic]` and `[Map]` metadata candidates

> **Source notice:** ChatGPT Web public-source research. ProjectBaseline was not read. This is not a Codex artifact. No GPL or unclear-license code was copied, translated, or mechanically ported. `code_imported: false`.

## Scope

This document inventories raw metadata candidates and separates display metadata, format markers, campaign references, player selection, multiplayer hints, and geometry fields.

It does not define runtime defaults or execute campaign, media, player, or session behavior.

# `[Basic]`

## Raw model

```text
ScenarioBasicRaw
- SectionOccurrences[]
- KeyOccurrences[]
- RecognizedFieldCandidates[]
- UnknownFields[]
- DuplicateFieldGroups[]
```

Every key retains:

- raw key spelling;
- raw value text;
- occurrence index;
- physical order;
- source layer;
- numeric, boolean, or string candidates;
- selected profile, if any;
- evidence grade.

## Field families

### Display and catalog metadata

Candidates:

- `Name`;
- `Author`;
- client-private translated name fields;
- map-list descriptions;
- official/unofficial marker candidates.

These fields may affect editor/client display without changing scenario simulation.

`Name` must not be used as a stable scenario identity.

### Campaign media references

Candidates:

- `Brief`;
- `Intro`;
- `Win`;
- `Lose`;
- `Action`;
- `Theme`;
- `PreMapSelect`;
- next-scenario references;
- alternate-next-scenario references.

These remain logical media/control references. Core does not load movies, audio, text, animations, or campaign control files.

### Player-House candidate

Candidate:

- `Player`.

The official editor exposes a human-player selection UI associated with `[Basic] Player` and map Houses. WAE also stores `Player` in its Basic model.

Interpretation:

```text
BasicPlayerRaw
→ AuthoredPlayerHouseCandidate
```

It is not automatically:

- current local machine controller;
- multiplayer host;
- network peer;
- observer;
- first player slot;
- House with `PlayerControl=yes`.

### Campaign progression and carry-over

Candidates:

- `CarryOverMoney`;
- `CarryOverCap`;
- `Percent`;
- `EndOfGame`;
- `SkipScore`;
- `OneTimeOnly`;
- `SkipMapSelect`;
- `NextScenario`;
- `AltNextScenario`;
- `InitTime`.

The field names and editor models support campaign-oriented interpretations, but exact runtime semantics, units, bounds, and savegame interaction remain profile-specific.

Parser output must preserve original decimal spelling. It does not calculate carried credits or campaign progression.

### Format and add-on markers

Candidates:

- `NewINIFormat`;
- `RequiredAddOn`.

`NewINIFormat` is a format/profile marker candidate. Community documentation strongly associates value `4` with TS/RA2/YR-era packed sections and coordinate conventions. WAE defaults to `4` and uses `>=5` for an editor/extension overlay profile.

Core rules:

- retain raw integer text;
- do not clamp;
- do not use it as the only game/version detector;
- do not infer all section semantics solely from this value;
- select any extension behavior explicitly.

`RequiredAddOn` has version-specific meaning and must not be generalized across TS, RA2, and YR without a selected profile.

### Multiplayer and mode hints

Candidates:

- `Official`;
- `MultiplayerOnly`;
- `MinPlayer`;
- `MaxPlayer`;
- `GameMode`;
- `GameModes`;
- `MaxPlayers` spelling variants or client metadata;
- `FreeRadar`;
- crate-related flags;
- `TrainCrate`;
- `TruckCrate`;
- `IgnoreGlobalAITriggers`.

These may be used by game, editor, launcher, or client components differently.

WAE's writer derives `MaxPlayer` from low-numbered Waypoints when `Player` is empty. That is a WAE canonical-writer behavior, not evidence that `MaxPlayer` is always derived from Waypoints by stock runtime.

### Home and camera candidates

Candidates:

- `HomeCell`;
- `AltHomeCell`.

WAE defaults these to 98 and 99. That is an editor model default. Their exact relationship to camera positions, Waypoints, start locations, or campaign transitions remains unresolved.

### Growth/environment candidates stored in Basic by some tools

WAE exposes:

- `IceGrowthEnabled`;
- `VeinGrowthEnabled`;
- `TiberiumGrowthEnabled`;
- `TiberiumDeathToVisceroid`.

These demonstrate tool/profile breadth, not a universal RA2/YR Basic contract. Equivalent or overlapping behavior may reside in Rules or SpecialFlags depending on game and profile.

## WAE Basic model evidence

WAE commit `b4c9481e9b00fb0a38739049a046f528b6054ce2` models:

- `Name`;
- `Author`;
- `Player`;
- `Percent`;
- `GameMode` and `GameModes`;
- `HomeCell` and `AltHomeCell`;
- `Theme`;
- `InitTime`;
- `Official`;
- `EndOfGame`;
- `FreeRadar`;
- `MaxPlayer` and `MinPlayer`;
- score/crate/one-time flags;
- carry-over fields;
- `NewINIFormat`;
- scenario progression references;
- `RequiredAddOn`;
- `MultiplayerOnly`;
- growth and AI-trigger flags.

Evidence grade: `ConfirmedByIndependentImplementation` for WAE's model and writer behavior; field runtime meaning remains field-specific.

## Community evidence warning

ModEnc describes `[Basic]` as a mixed map-property section and provides per-field applicability/version notes. It is useful reference material, but:

- pages can combine multiple games;
- obsolete fields may remain documented;
- defaults can be runtime, editor, or Rules defaults;
- current page summaries are not official runtime source.

Evidence grade: `CommunityDocumented`.

## Boolean handling

Possible boolean spellings include:

- `0` / `1`;
- `yes` / `no`;
- `true` / `false`;
- mixed case;
- empty value;
- invalid text.

Recommended raw result:

```text
ScenarioBooleanRaw
- RawText
- NumericCandidate
- TextCandidate
- SelectedProfileValue?
- IsInvalidForSelectedProfile
```

No invalid value is rewritten to an editor default.

## Numeric handling

Numeric fields may contain:

- decimal integer;
- signed value;
- decimal floating point;
- leading zeroes;
- explicit plus sign;
- overflow text;
- locale-incompatible punctuation;
- empty value.

Core uses invariant candidates and checked arithmetic, while preserving raw spelling.

# `[Map]`

## Raw model

```text
ScenarioMapRaw
- SizeRaw
- LocalSizeRaw
- TheaterRaw
- OtherProperties[]
```

## Strong candidates

- `Size`;
- `LocalSize`;
- `Theater`.

WAE's reader requires `[Map]`, reads `Size` fields 2/3 into map width/height, requires four `LocalSize` fields, and reads `Theater` as a raw string. WAE's writer emits `Size=0,0,width,height` and preserves the four modeled `LocalSize` values.

Evidence grade: `ConfirmedByIndependentImplementation`.

The official editor displays:

- width and height;
- `LocalSize` as visible/usable size;
- `Theater` as editable text/list.

Evidence grade: `ConfirmedByOfficialEditorSource`.

## Additional Map candidates

Public/community sources may expose:

- `MapScale`;
- `VeteranRatio`;
- `BaseNormal`;
- `MaxPlayers` or spelling variants;
- older-game geometry flags;
- editor/client private keys;
- extension fields.

These are retained raw until a profile gives them meaning.

## Required versus optional

Project policy candidate:

- `[Map]` is required for a semantic geometry descriptor;
- missing `Size` prevents geometry resolution;
- missing `LocalSize` prevents playable-area resolution but need not destroy raw metadata;
- missing `Theater` prevents theater binding;
- unknown extra fields do not invalidate the raw document.

This is `ConfiguredForProjectPolicy`, not runtime-source proof.

## Duplicate Basic and Map sections

Duplicate sections produce ambiguity. They are not merged by default.

Potential semantic policies are explicit:

- reject duplicate semantic source;
- select first occurrence for a compatibility view;
- select last occurrence for a compatibility view;
- compose only non-conflicting keys while retaining duplicate conflicts;
- use a source-specific editor profile.

No policy alters the lossless source.

## `Official` and file transfer

Community documentation associates `Official=no` with transferable custom multiplayer maps and `.MPR`/`.YRM` behavior. That is useful for a mode evidence item, but not sufficient to classify every scenario.

`Official` can contribute:

```text
ScenarioModeEvidence
- Source: Basic.Official
- Candidate: custom multiplayer distribution hint
- Grade: CommunityDocumented
```

It cannot alone decide campaign versus multiplayer.

## `NewINIFormat` and coordinate interpretation

Community documentation links `NewINIFormat>=4` to `Y*1000+X` scenario-cell encoding. The M3-R7 research already treats that formula as a strong candidate with separate evidence.

This dossier does not duplicate or change M3-R7. It only records that Basic format metadata can be an input to coordinate-profile selection.

## Player-count conflicts

Potential sources:

- `Basic.MinPlayer`;
- `Basic.MaxPlayer`;
- `Map.MaxPlayers` candidate;
- number of low-numbered start Waypoints;
- launcher/client map database;
- lobby hard maximum;
- mode-specific maximum;
- House count.

No one source automatically repairs another.

Suggested consistency states:

- `Consistent`;
- `BasicRangeInvalid`;
- `BasicVsWaypointsMismatch`;
- `BasicVsClientMismatch`;
- `HouseCountExceedsMaxPlayerCandidate`;
- `NoStartLocationsForDeclaredPlayers`;
- `UnresolvedProfileConflict`.

## Metadata does not authorize execution

A successful Basic/Map semantic view does not:

- launch a mission;
- select a local player;
- allocate players;
- create Houses;
- load theater resources;
- verify a map;
- assign starts;
- apply credits;
- run AI;
- initialize Unity.

## Roundtrip requirements

A lossless writer may need to preserve:

- duplicate Basic/Map sections;
- duplicate keys;
- key casing;
- unknown fields;
- empty values;
- numeric spelling;
- boolean spelling;
- physical order;
- comments and whitespace where available.

A canonical editor rewrite is a separate operation and may legitimately normalize values, but must not be described as byte-identical roundtrip.
