# Weather, Ion Storm, and SpecialFlags

> **Source notice:** ChatGPT Web public-source research. ProjectBaseline was not read. Not a Codex artifact. No GPL or unclear-license code was copied, translated, mechanically rewritten, or ported. `code_imported: false`.

## Scope

This file separates authored capability flags, alternate lighting profiles, Trigger commands, superweapon/weather runtime state, presentation effects, audio, and gameplay effects.

## Official editor evidence

The official FinalSun/FinalAlert `SpecialFlags` dialog exposes raw keys including:

```text
IonStorms
FogOfWar
Meteorites
TiberiumGrows
TiberiumSpreads
TiberiumExplosive
DestroyableBridges
MCVDeploy
InitialVeteran
FixedAlliance
HarvesterImmune
Inert
Visceroids
```

In RA2 mode the UI relabels:

```text
Ion Storms → Weather Storms
Fog Of War → Shroud
Tiberium → Ore
```

and hides several TS-specific controls. This confirms editor profile and label behavior, not the exact game runtime contract.

## Capability versus state

A SpecialFlags value is first modeled as an authored capability/configuration candidate.

```text
Weather capability flag
≠ currently active weather

Ion lighting profile
≠ active Ion/Lightning Storm

Trigger action
≠ executed action

cloud/bolt assets
≠ storm simulation
```

Recommended types:

```text
ScenarioWeatherCapabilityRaw
ScenarioWeatherStateCandidate
ScenarioSpecialEnvironmentFlagsRaw
```

## Environment-related classification

| Candidate | Presentation | Audio | Simulation | Notes |
|---|---|---|---|---|
| `IonStorms` / Weather Storms | possible alternate lighting/clouds | possible storm sounds | possible weather/superweapon state | authored capability/profile candidate |
| `FogOfWar` / Shroud | visibility presentation | none inherent | LOS/exploration/session state | gameplay visibility, not screen fog |
| `Meteorites` | visual impact candidate | impact sound candidate | possible damage/spawn | TS-oriented; hidden in RA2 editor profile |
| Ion lighting fields | alternate color/level inputs | none inherent | none by themselves | do not activate storm |
| `DestroyableBridges` | debris/animation possible | sound possible | bridge damage/state | not owned by weather subsystem |
| resource growth flags | presentation changes possible | none inherent | resource simulation | not weather merely because environmental |

## Candidate weather domains

```text
PresentationOnlyCandidate
SimulationAffectingCandidate
TriggerControlledCandidate
EditorOnlyCandidate
ClientOnlyCandidate
ExtensionCandidate
Unknown
```

Each raw field can have more than one candidate domain until a version profile is selected.

## Ion versus Weather terminology

Evidence indicates shared field names and editor relabeling across TS and RA2/YR contexts. The safe model is:

```text
Raw field family: Ion*
Logical alternate-lighting role candidate
Version display name: Ion Storm or Weather Storm
Runtime weather type: separately resolved
```

Do not rename raw keys to `Weather*`. Do not assume TS Ion Storm gameplay and RA2/YR Lightning Storm gameplay are identical.

## Ares extension evidence

Ares documents Lightning Storm superweapon behavior as a multi-domain system with separate parameters for:

- lighting enable and components;
- duration;
- radar outage;
- target range;
- damage and warhead;
- hit/scatter timing;
- clouds and bolts;
- bolt explosion;
- debris;
- activation and strike sounds.

Ares also documents that superweapon Light fields can default to the scenario's Ion lighting values. This provides strong boundary evidence:

```text
Scenario Ion profile
→ potential default input to a weather/superweapon profile
```

It does not prove that stock RA2/YR stores an active storm in `[Lighting]` or `[SpecialFlags]`.

Evidence grade: extension documentation only.

## Weather state graph

Recommended declarative graph:

```text
ScenarioSpecialEnvironmentFlagsRaw
      └─ capability candidates

ScenarioLightingRaw
      └─ alternate lighting profile candidates

Rules/AudioVisual/General references
      └─ logical cloud/bolt/sound/damage references

Trigger environment commands
      └─ start/stop/change candidates

future session/simulation
      └─ authoritative runtime weather state
             ├─ timing
             ├─ deterministic targeting
             ├─ damage/radar effects
             ├─ savegame/replay state
             └─ presentation/audio notifications
```

The parser does not connect every edge automatically.

## Initial state

Potential initial-state sources:

- explicit Trigger/campaign setup;
- extension fields;
- superweapon activation state;
- savegame state;
- game-mode/session configuration;
- engine internal defaults.

`IonStorms=yes` alone is not treated as `Active=true`.

Suggested analysis:

```text
CapabilityKnown
InitialStateKnown
ActivationSourceKnown
DeactivationSourceKnown
AlternateLightingAvailable
AudioReferencesAvailable
SimulationProfileKnown
```

## Visual weather candidates

Potential visual layers include:

- global tint/alternate Lighting;
- clouds;
- bolts;
- rain or snow particles;
- wind-driven particles;
- debris;
- impact animations;
- screen flash;
- local light flashes;
- visibility or radar effects.

No universal stock map fields for arbitrary rain/snow/wind were confirmed by the load-bearing sources used here. Such fields remain extension or unresolved candidates.

## Audio weather candidates

Potential audio references include:

- global storm activation sound;
- random strike sounds;
- looping rain/wind ambience;
- impact/explosion sounds;
- EVA/speech notification.

They remain independent logical references. A missing sound does not disable a parsed weather capability unless an explicit runtime profile establishes that requirement.

## Damage and gameplay

Possible gameplay effects:

- damage/warhead application;
- radar outage;
- unit disabling;
- movement or visibility effects;
- deterministic target selection;
- bridge or terrain interactions;
- Trigger conditions/events.

These belong to future simulation. The presentation adapter cannot authoritatively create damage because it rendered lightning.

## Trigger boundary

Potential environment commands:

```text
StartWeatherCandidate
StopWeatherCandidate
ActivateIonStormCandidate
DeactivateIonStormCandidate
SetLightingFieldCandidate
ChangeAmbientCandidate
PlayStormSoundCandidate
RevealOrShroudCandidate
ScreenFlashCandidate
UnknownEnvironmentCommand
```

Each retains raw opcode and parameters. Editor display names are descriptive metadata, not complete runtime semantics.

## SpecialFlags boolean parsing

Raw values can include:

```text
yes/no
true/false
1/0
on/off
mixed case
whitespace
empty
invalid
```

The raw parser preserves text. Boolean interpretation requires a version/profile policy. Invalid values are not converted to false.

Duplicate keys and duplicate sections remain distinct occurrences.

## Version boundaries

### TS profile

- Ion Storm terminology;
- Meteorites and inherited environmental flags may be relevant;
- TS-specific triggers and rules may differ.

### RA2 profile

- official editor uses Weather Storm label;
- stock Lightning Storm superweapon and map behavior require separate runtime evidence;
- TS-only flags cannot be assumed functional.

### YR profile

- Dominator alternate lighting is a candidate;
- YR-specific superweapon and extension interactions exist;
- multiple Houses/Countries and client modes do not alter raw weather parsing.

### Extension profiles

- Ares and Phobos can add fields, actions, superweapon settings, sounds, and visual effects;
- all extension semantics are tagged with extension/version provenance;
- unknown extension fields are retained in vanilla mode.

## Consistency analysis

Cases to report without repair:

- capability enabled but no alternate lighting;
- alternate lighting present but capability absent;
- partial Ion profile;
- weather start command but no stop command;
- stop command with no known start;
- visual references without simulation profile;
- simulation profile without visual references;
- unknown weather opcode;
- RA2 map containing TS-only fields;
- extension fields under vanilla profile;
- FogOfWar and client Shroud options conflicting.

## Roundtrip

Future lossless writing preserves:

- raw SpecialFlags spelling;
- duplicate values;
- unknown fields;
- partial alternate profiles;
- Trigger raw parameters;
- extension settings;
- disabled or orphaned commands.

No default writer removes an apparently unused Ion profile or rewrites Ion terminology to Weather terminology.

## Policies

- `WeatherProfilePolicy`;
- `SpecialEnvironmentFlagsPolicy`;
- `EnvironmentCommandPolicy`;
- `WeatherStateInitializationPolicy`;
- `EnvironmentRoundtripPolicy`.

## Diagnostics

- `WeatherCapabilityWithoutStateSource`;
- `WeatherStateWithoutCapability`;
- `AlternateLightingWithoutActivation`;
- `PartialIonProfile`;
- `StormVisualSimulationBoundaryUnresolved`;
- `WeatherAudioReferenceMissing`;
- `ExtensionWeatherFieldInVanillaMode`;
- `TsOnlyFlagInRa2Profile`;
- `FogOfWarVisibilityConflict`;
- `UnknownWeatherCommand`;
- `BooleanValueInvalid`.

## Non-goals

No weather instance, storm timer, cloud, bolt, rain, snow, wind, particle, radar outage, damage, sound, light transition, Trigger execution, or Unity object is created.
