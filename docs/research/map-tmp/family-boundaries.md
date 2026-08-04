# MAP/TMP family and responsibility boundaries

## 1. Target families

The target map family is the Tiberian Sun / Red Alert 2 generation of INI-shell map documents, with RA2/YR-specific interpretation where evidence differs.

### `.map`

A general/official scenario document. Discovery may depend on PKT or campaign control data. The extension does not prove single-player or multiplayer semantics by itself.

### `.mpr`

A custom multiplayer-map extension used by RA2-era engines and tools. Community documentation associates it with standalone multiplayer discovery and transfer behavior.

### `.yrm`

A Yuri's Revenge custom multiplayer extension with a role similar to `.mpr`. It is not a distinct packed-data codec.

### Containers that are not plain map documents

- `.mmx`: RA2 map-pack MIX/container candidate containing map and PKT resources.
- `.yro`: YR map-pack MIX/container candidate.
- ordinary MIX entries containing `.map`, `.mpr` or `.yrm` documents.

These are content-discovery/package layers. They must be resolved before the map reader receives a bounded logical-document window.

## 2. INI shell versus packed regions

A map document contains two different representation classes:

1. ordinary lossless INI sections and key/value occurrences;
2. packed binary regions encoded as numbered INI values.

The lossless INI parser owns section/key ordering, comments, duplicates and raw text. A packed-section decoder owns only the decoded bytes for a specifically recognized section.

The decoder must not reparse the whole file or normalize unrelated sections.

## 3. Map document versus game database

A map can contain local definitions and references to global definitions. The map reader does not decide whether a type exists in effective Rules/Art.

Separate layers are required:

```text
bounded map bytes
→ lossless map document
→ recognized packed-section views
→ map identity/reference model
→ global + map-local semantic composition
→ typed scenario view
→ simulation/editor/rendering
```

## 4. MAP versus TMP

### MAP owns

- map dimensions, local playable rectangle and theater name;
- terrain-cell placement records through `IsoMapPack5`;
- overlay type/frame arrays;
- preview metadata and preview image pack;
- object placements and mission/scripting records;
- map-local configuration sections;
- unknown sections and occurrences as raw evidence.

### TMP owns

- template grid dimensions and per-cell offsets;
- per-cell normal diamond image and depth image;
- optional extra color/depth rectangles;
- raw per-cell metadata fields;
- file-local cell ordering.

A map tile record selects a template/tile identity and subtile. The TMP does not know where that subtile is placed in a particular map.

## 5. Theater and tileset are upper binding layers

The map `[Map] Theater=` value selects a theater profile. Theater/control INIs map numeric template IDs and filenames to TMP resources.

The following are not inferred from TMP bytes alone:

- which file corresponds to an `IsoMapPack5` tile index;
- random variants or suffix selection;
- land passability and speed classes;
- shore, water, cliff and bridge grouping;
- LAT/connected-tile relationships;
- theater-specific palettes;
- renderer texture-atlas placement.

## 6. Terrain behavior versus appearance

A TMP cell may carry raw ramp/terrain/height-related fields and depth images, but complete behavior also depends on theater metadata and game logic.

Keep separate:

- map cell elevation from `IsoMapPack5`;
- TMP cell-local height/ramp metadata;
- visual color/depth pixels;
- logical land type/passability;
- cliff and bridge overlay/object logic;
- runtime pathfinding and unit slope transforms.

## 7. Preview is not terrain

`PreviewPack` is a cached/embedded presentation image. It must not be used as terrain evidence, collision data or a fallback when `IsoMapPack5` is corrupt.

A missing or invalid preview can be diagnosed independently from terrain and mission validity.

## 8. Mission graph versus runtime executor

The format model may preserve:

- Houses/Countries;
- Triggers, Events, Actions and Tags;
- TeamTypes, TaskForces and ScriptTypes;
- AITriggerTypes;
- object placements and attached identifiers.

It must not execute scripts, create simulation entities, resolve victory logic or silently delete unsupported event/action opcodes.

## 9. FinalAlert and other editors

FinalSun/FinalAlert 2 and World-Altering Editor are source evidence for practical loading/writing. Their repair, normalization and UI restrictions are tool behavior.

The Core reader must not:

- clamp to an editor's UI limit as a file-format rule;
- discard fields an editor does not expose;
- regenerate packed sections during read;
- assume editor save order is original order;
- treat an editor-generated preview as canonical terrain.

## 10. Input source boundary

Memory, seekable Stream, loose file and MIX-entry window are transport modes. They cannot alter:

- format detection;
- maximum output budgets;
- diagnostics;
- packed-section semantics;
- canonical parsed-model hashes.

The content layer supplies source identity and provenance; the map/TMP readers consume one bounded window.
