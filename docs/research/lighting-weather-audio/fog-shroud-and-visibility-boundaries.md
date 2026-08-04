# Fog, Shroud, and Visibility Boundaries

> **Source notice:** ChatGPT Web public-source research. ProjectBaseline was not read. Not a Codex artifact. No GPL or unclear-license code was copied, translated, mechanically rewritten, or ported. `code_imported: false`.

## Scope

This file separates authored metadata, gameplay visibility, explored state, radar/minimap visibility, editor display, and purely visual fog.

## Required concepts

```text
FogOfWar metadata
initial shroud metadata
explored state
current line-of-sight visibility
radar visibility
minimap visibility
spectator visibility
replay visibility
editor visibility
lighting darkness
weather fog visual effect
screen alpha overlay
post-processing fog
```

No pair is automatically equivalent.

## Official editor evidence

The official SpecialFlags dialog reads and writes the raw key `FogOfWar`. In RA2 mode its UI label is changed to `Shroud`.

This confirms:

- the raw key exists in the editor's scenario metadata model;
- TS and RA2 editor terminology differs;
- editor input is raw text/boolean-like data.

It does not confirm:

- exact runtime boolean syntax;
- current per-cell visibility state;
- savegame representation;
- multiplayer override precedence;
- whether the same semantics apply to TS, RA2, and YR;
- visual rendering algorithm.

## Gameplay versus presentation

Gameplay Fog/Shroud can affect:

- what cells and units a player can see;
- targetability;
- command availability;
- radar/minimap information;
- AI or spectator rules;
- exploration persistence.

Visual fog can affect only the displayed image. A renderer blur, haze, alpha layer, or dark tint does not establish gameplay visibility.

```text
FogOfWar gameplay state
≠ weather fog shader
≠ dark Lighting tuple
≠ transparent overlay
```

## Authored metadata model

```text
ScenarioVisibilityMetadataRaw
- FogOfWarOccurrences[]
- ShroudNamedClientOrEditorCandidates[]
- RelatedSpecialFlags[]
- SourceProvenance
- VersionProfile
- EvidenceGrade
- Diagnostics[]
```

The raw parser does not allocate a grid.

## Initial state candidates

Potential authored/session sources:

- `[SpecialFlags] FogOfWar`;
- multiplayer dialog defaults;
- game-mode rules;
- lobby Shroud/Fog option;
- campaign invocation;
- Trigger actions that reveal or grow shroud;
- extension fields;
- savegame state.

Recommended type:

```text
VisibilityInitializationCandidate
- SourceKind
- RawValue
- AppliesToPlayersCandidate
- PriorityCandidate
- EvidenceGrade
```

No implicit precedence is selected.

## Trigger evidence

Public editor action metadata includes candidates such as:

- Reveal All Map;
- Reveal Around Waypoint;
- Reveal Zone of Waypoint;
- Grow Shroud One Step;
- extension actions that reveal, reshroud, or manipulate radar.

M3-R10 receives the raw opcode and parameters from the Trigger boundary and emits declarative `EnvironmentCommandCandidate` values. It does not reveal cells.

## Directed consumer model

Visibility is player-relative.

```text
Visibility state
- observer/player identity
- cell/domain
- explored flag
- currently visible flag
- radar/minimap state
- source/event history where required
```

A single global map bool cannot represent current runtime visibility.

## Session and lobby overrides

Multiplayer clients can expose options named Shroud or Fog of War. These settings may:

- override a Rules default;
- influence a spawn/session INI;
- change initial exploration;
- be disabled by game mode;
- differ between skirmish and network play.

Client behavior is not silently written back as authored map metadata.

Required provenance:

```text
MapAuthored
RulesDefault
GameMode
ClientDefault
LobbySelection
SpawnOverride
RuntimeTrigger
Savegame
```

## Radar and minimap

Radar visibility may depend on:

- current cell visibility;
- explored state;
- radar building availability;
- radar outage/weather effects;
- jammer effects;
- spectator/replay mode;
- map preview metadata.

It is not identical to Lighting or Fog metadata. Radar colors and minimap pixels are presentation outputs.

## Preview boundary

A map preview can be:

- authored bitmap data;
- client-generated image;
- editor-generated image;
- rendered without Shroud;
- rendered with only map bounds;
- annotated with start positions.

Preview pixels cannot be used to infer authored visibility or current exploration state.

## Theater and palette

Fog/Shroud can be rendered using palette operations, darkening, masks, sprites, or shaders. That implementation does not change the raw metadata model.

Unknown Theater or missing palette does not make `FogOfWar` unparsable.

## Lighting boundary

Lighting darkness affects visible colors. Visibility masking decides whether content is shown. Their order is renderer-specific.

Potential conceptual order:

```text
asset/palette output
→ environment lighting
→ gameplay visibility mask
→ optional presentation fog/post-process
```

This is a future adapter design, not a stock-runtime claim.

## Weather boundary

Weather can reduce visibility visually or through gameplay, but those are independent effects.

```text
Storm darkening
Storm clouds
Radar outage
Reduced sight range
FogOfWar
```

must be separately represented unless a profile explicitly links them.

## Spectator and replay

Spectator/replay views can reveal more information than a normal player. This belongs to session/replay policy and must not rewrite map metadata or the underlying per-player state.

## Savegame boundary

Current explored and visible cells are runtime state. A future savegame system may need:

- explored-cell data per player;
- current visibility reconstruction inputs;
- active reveal/shroud effects;
- radar outage state;
- Trigger progress.

The scenario environment document stores none of this current state.

## Consistency analysis

Report without repair:

- FogOfWar raw value invalid;
- duplicate FogOfWar keys disagree;
- map metadata and lobby selection conflict;
- Trigger reveal action without valid waypoint;
- grow-shroud action under a profile that does not support it;
- visibility command target House missing;
- radar outage weather state without radar profile;
- visual fog extension field under vanilla profile;
- Shroud disabled but reveal actions present;
- client preview behavior inconsistent with authored metadata.

## Roundtrip

Preserve:

- raw FogOfWar value and casing;
- duplicate SpecialFlags entries;
- client/private Shroud fields;
- Trigger raw commands;
- unknown visibility fields;
- invalid references;
- extension settings.

Do not serialize current runtime visibility into the scenario file by default.

## Policies

- `VisibilityMetadataPolicy`;
- `VisibilityInitializationPolicy`;
- `SessionVisibilityOverridePolicy`;
- `EnvironmentCommandPolicy`;
- `EnvironmentRoundtripPolicy`.

## Diagnostics

- `FogOfWarValueInvalid`;
- `VisibilityMetadataDuplicateConflict`;
- `MapLobbyVisibilityConflict`;
- `VisibilityGridNotScenarioMetadata`;
- `RevealCommandReferenceMissing`;
- `VisualFogNotGameplayFog`;
- `LightingDarknessNotShroud`;
- `RadarVisibilityBoundaryUnresolved`;
- `SpectatorVisibilityIsSessionState`;
- `SavegameVisibilityNotMapMetadata`.

## Non-goals

No visibility grid, LOS, exploration, radar, minimap mask, spectator mode, replay visibility, fog shader, post-process, alpha overlay, Trigger execution, or Unity object is implemented.
