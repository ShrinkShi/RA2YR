# Ambient Audio, Theme, and Media

> **Source notice:** ChatGPT Web public-source research. ProjectBaseline was not read. Not a Codex artifact. No GPL or unclear-license code was copied, translated, mechanically rewritten, or ported. `code_imported: false`.

## Scope

This file defines logical media references and ownership boundaries. It does not resolve archives, decode audio, or initiate playback.

## Reference families

```text
LogicalThemeId
LogicalSoundId
LogicalSpeechOrEvaId
LogicalMovieId
LogicalAnimationId
CSFTextLabel
ClientPlaylistId
AudioResourceCandidate
MediaPlaybackState
```

These are separate identities.

## `[Basic] Theme`

`Theme` is a strong authored music-theme reference candidate in `[Basic]`.

Required raw model:

```text
ScenarioThemeReferenceRaw
- RawValue
- KeyOccurrence
- SectionOccurrence
- Casing
- Whitespace
- SourceProvenance
- VersionProfile
- EvidenceGrade
```

The semantic binder can produce zero, one, or many logical registry candidates. It cannot:

- scan MIX files;
- inspect AUD/WAV/MP3/OGG files;
- play music;
- replace an unknown Theme with the first registry entry;
- choose a random Theme;
- rewrite casing based on resource discovery;
- treat a client playlist category as a stock Theme ID.

## Other `[Basic]` media references

Candidates such as:

```text
Intro
Win
Lose
Brief
Action
```

can refer to movies, briefing media, action sequences, or campaign presentation depending on version/profile. They remain distinct from `Theme` and from Trigger playback actions.

The environment/media layer retains logical references only. Campaign progression and media sequencing remain outside this dossier.

## Trigger action evidence

WAE's public action definitions include separate parameter domains for:

- Play Movie;
- Play Sound;
- Play Music Theme;
- Play Speech;
- Text Trigger;
- reveal/shroud and other presentation actions.

This supports the declarative model:

```text
EnvironmentCommandCandidate
- CommandKind
- RawOpcode
- RawParameters
- LogicalMediaReferenceCandidate?
- TriggerReference
- VersionProfile
- EvidenceGrade
```

The editor action name is not a complete runtime specification. Numeric registry lookup, casing, fallback, playback priority, interruption, looping, and savegame behavior remain separate questions.

## Registry boundaries

Potential registries include:

- Theme.ini logical music registry;
- Sound/SoundMD registry;
- EVA/speech registry;
- Movies list;
- Rules `[AudioVisual]` global defaults;
- CSF string table;
- client playlist or game-mode metadata;
- extension-specific media registries.

Required identity chain:

```text
Raw reference
→ explicit registry profile
→ logical registry entry candidate
→ resource candidate(s)
→ future playback descriptor
```

Registry gaps, duplicate identifiers, case collisions, missing entries, and map-local overrides are preserved and diagnosed. Resource presence never back-propagates to rewrite the raw reference.

## Theme versus playlist

```text
Scenario Theme
≠ client playlist
≠ menu music
≠ loading-screen music
≠ UI preview music
≠ currently playing track
```

A launcher can choose a playlist based on game mode or user settings. That is session/client behavior and must retain separate provenance.

## Sound versus speech/EVA

```text
Sound effect
≠ speech clip
≠ EVA event
≠ CSF text label
```

An EVA event may combine text, voice, queue priority, and house/language selection. M3-R10 stores only the raw logical reference and candidate domain.

## Movie references

Movie identifiers can occur in Basic metadata and Trigger actions. They do not imply:

- a file extension;
- a specific archive;
- a codec;
- fullscreen versus embedded playback;
- game pause behavior across versions;
- client support.

Those belong to future media adapters and profile definitions.

## Ambient audio evidence

Potential sources of ambient or looping sound include:

- global Rules/AudioVisual defaults;
- weather activation and strike sounds;
- object type active/idle/ambient sounds;
- animations or particles;
- Trigger Play Sound actions;
- map-positioned object types whose runtime emits sound;
- client-side UI or playlist configuration;
- extension-defined spatial emitters.

No load-bearing source in this research establishes a universal stock scenario section named `[Ambient]` that directly stores arbitrary positioned emitters with radius and attenuation. Therefore:

```text
UniversalStockAmbientSection = Unresolved
```

The project must not invent a section schema.

## Spatial audio candidate model

```text
ScenarioSoundEmitterCandidate
- SourceKind
- LogicalSoundReference
- PlacementReference?
- ObjectTypeReference?
- LoopCandidate?
- RadiusRaw?
- AttenuationRaw?
- PriorityRaw?
- IntervalRaw?
- VersionProfile
- EvidenceGrade
- Diagnostics[]
```

The candidate is only emitted when an identified public format/profile supports the source. Missing fields are not guessed from art size or sound duration.

## Object-attached audio

Potential object-type fields include active, idle, damage, deploy, movement, ambient, and destruction sounds. Exact field names and applicability differ by type and extension.

Required separation:

```text
Object type sound property
Placement object
Runtime active/damaged/powered state
Logical emitter
Actual playback voice
```

A placed building does not automatically begin playing every sound reference on its type.

## Weather audio

Weather can involve:

- activation sound;
- looping ambience;
- randomized strike sounds;
- impact sounds;
- EVA notifications.

Ares extension documentation explicitly separates activation, lightning strike, animation, and damage inputs. This is boundary evidence only.

## 2D/3D audio

The stock scenario reference alone does not determine:

- 2D versus spatial playback;
- listener position;
- falloff curve;
- radius units;
- panning;
- occlusion;
- priority and voice stealing;
- random interval;
- loop points.

These properties require an explicit audio profile and future adapter.

## MIX and resource discovery

Resource lookup may traverse ordered providers, archives, expansions, loose files, map packages, and extension paths. M3-R10 does not execute that lookup.

Suggested later result:

```text
ScenarioMediaReference
- LogicalId
- RegistryKind
- RegistryEntryCandidate
- ResourceCandidates[]
- Winner?
- SuppressedCandidates[]
- Provenance
- Diagnostics[]
```

Unknown or missing resources remain bound-but-unresolved rather than being deleted.

## Case and normalization

The raw layer retains exact case. A registry policy can compare case-insensitively only when supported by evidence.

Cases to diagnose:

- exact duplicate ID;
- case collision;
- normalized collision;
- map-local/global collision;
- reference matches multiple entries;
- reference matches no entry;
- resource candidates disagree with registry candidates.

No filename probing determines identifier normalization.

## Playback state boundary

```text
Authored reference
≠ queued command
≠ currently playing media
≠ playback position
≠ loop iteration
≠ volume/fade state
```

Runtime playback state belongs to future audio/session/savegame layers.

## Roundtrip

Preserve:

- raw Theme/Sound/Speech/Movie text;
- case;
- whitespace;
- duplicate keys;
- invalid or unknown IDs;
- Trigger raw opcode/parameters;
- client/extension fields;
- map-local registry overrides;
- editor-private media metadata.

Do not canonicalize to a discovered filename.

## Policies

- `ThemeBindingPolicy`;
- `SoundBindingPolicy`;
- `SpeechBindingPolicy`;
- `MovieBindingPolicy`;
- `AmbientAudioProfilePolicy`;
- `MediaCasePolicy`;
- `EnvironmentCommandPolicy`;
- `EnvironmentRoundtripPolicy`.

## Diagnostics

- `ThemeReferenceUnknown`;
- `SoundReferenceUnknown`;
- `SpeechReferenceUnknown`;
- `MovieReferenceUnknown`;
- `MediaCaseCollision`;
- `MediaRegistryDuplicate`;
- `MediaResourceMissing`;
- `MultipleMediaResourceCandidates`;
- `ClientPlaylistNotStockTheme`;
- `AmbientEmitterFormatUnresolved`;
- `SpatialAudioParametersMissing`;
- `PlaybackStateNotAuthoredData`.

## Non-goals

No archive scan, audio decode, codec selection, `AudioClip`, `AudioSource`, playback queue, mixer, volume, fade, loop, 3D attenuation, movie player, CSF resolution, or media execution is implemented.
