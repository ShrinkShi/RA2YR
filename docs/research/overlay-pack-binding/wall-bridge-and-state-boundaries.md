# Wall, bridge, and state boundaries

> **Research provenance:** This document was produced by ChatGPT Web from public sources. It did not read ProjectBaseline, is not a local Codex Agent artifact, and imports no code. GPL or unclear-license sources are reference-only; no source was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

## 1. Wall and fence families

A wall/fence cell can involve several independent concepts:

- raw Overlay type;
- raw OverlayData byte;
- connected-neighbor mask;
- selected visual frame;
- damage state;
- gameplay health;
- ownership;
- gate/open state;
- targetability;
- passability/building placement;
- rendering and shadows.

The packed arrays directly provide only type and data bytes.

## 2. Editor-derived connection behavior

WAE's connected-overlay mutation examines neighboring overlay cells and replaces the current Overlay object with a selected type/frame pair. This proves that at least one editor computes connected visual frames from neighborhood context.

Evidence grade: `ConfirmedByIndependentImplementation` for WAE editor behavior.

It does not prove either of these universal claims:

- the original runtime always recomputes wall connectivity;
- the stored data byte is always a neighbor bitmask.

The original runtime may trust stored frames, recompute some families, combine both, or apply hardcoded logic.

## 3. Wall semantic candidates

A wall-specific adapter may expose:

```text
WallSemanticCandidate
- StoredFrameCandidate
- DerivedNeighborMaskCandidate
- DamageVisualCandidate
- GateCandidate
- OwnerCandidateSource
- PassabilityCandidateSource
- EvidenceGrade
```

Owner and health generally require data outside the two packed arrays or runtime-created state. They must not be invented from OverlayData.

## 4. Wall diagnostics

Useful diagnostics:

- wall type with no wall semantic profile;
- stored frame inconsistent with derived neighbor candidate;
- data value outside known Art frames;
- gate-like type without required companion object/state;
- wall outside scenario domain;
- connected wall adjacent to unknown type;
- extension wall evaluated with vanilla profile.

The analyzer may compare stored and derived values but cannot overwrite the raw byte.

## 5. Bridge systems are multi-source

Bridge behavior can depend on:

- Overlay type and data;
- TMP bridge deck or approach art;
- theater `BridgeSet`-related registry information;
- map cell level;
- high versus low occupancy;
- bridge control/hut objects;
- intact, damaged, destroyed, repaired states;
- hardcoded Overlay ordinals;
- pathing and collision;
- water/shore/terrain beneath the bridge;
- debris and animations.

Therefore:

```text
BridgeSet or TMP art ≠ complete bridge simulation
Overlay type/data ≠ complete bridge simulation
```

This is consistent with the M3-R3 theater/TMP boundary research.

## 6. WAE bridge placement evidence

WAE's bridge placement mutation demonstrates editor-specific construction:

- low bridges place Overlay cells across three-cell-wide pieces and write frame indices based on lateral position;
- high bridges place a single Overlay cell per bridge piece along the selected line and choose from direction-specific frame ranges;
- placement considers map cell levels;
- the editor stores the result as ordinary Overlay type/frame pairs.

Evidence grade: `ConfirmedByIndependentImplementation` for WAE behavior. It is not original runtime source evidence.

## 7. Community high/low bridge observations

A fixed PPM discussion documents a community reverse-engineering model:

- certain high bridge families store only the middle cell of a visually three-cell-wide piece;
- the game extrapolates the visual width;
- low bridge pieces are represented by three neighboring frames/cells;
- specific bridge behavior is tied to hardcoded Overlay ordinals.

Evidence grade: `CommunityDocumented`. It is useful for test/audit design but cannot be promoted to official runtime source.

## 8. Bridge semantic profiles

Separate profiles are required at minimum for:

- low bridge piece;
- high bridge piece;
- wood/train-derived high bridge family;
- bridge approach/end pieces;
- damaged/destroyed candidates;
- extension-defined bridge families;
- unknown bridge-like overlays.

A profile can derive candidates but must preserve the original type/data pair and source coordinate.

## 9. High and low occupancy

The packed arrays have only one type/data pair per 512 × 512 storage coordinate and no explicit height field. Any high/low occupancy result must combine the Overlay record with map/TMP/runtime context.

Do not infer:

- upper-level occupancy from a type name alone;
- bridge height from OverlayData alone;
- pathing layer from visual frame alone;
- intact state from presence alone.

## 10. Damage and repair

Bridge damage/repair may use:

- hardcoded Overlay identities;
- bridge control objects;
- changed frames/types;
- scenario triggers or runtime state;
- TMP replacements/debris;
- pathing updates.

The map parser exposes inputs only. It does not initialize a complete damage model.

## 11. Water, shore, and debris

Water/shore responsibility is shared among:

- theater TMP tiles and LAT/shore transitions;
- Overlay pieces;
- terrain passability;
- bridge state;
- runtime rendering and pathing.

An Overlay over water does not by itself establish a valid bridge. Debris or decorative overlays likewise should not be upgraded to bridge semantics from appearance.

## 12. Required raw-versus-derived separation

```text
OverlayTypeRaw
OverlayDataRaw
StorageCoordinate
ScenarioDomainResult
RegistryBinding
BridgeOrWallProfileCandidate
DerivedNeighborOrPieceState
RenderingCandidate
SimulationCandidate
```

Each step retains evidence grade and diagnostics.

## 13. Forbidden shortcuts

Do not:

- treat every wall data byte as a bitmask;
- recompute and overwrite all wall frames during parse;
- treat every bridge data byte as damage state;
- infer bridge semantics from Art filename or rendered appearance;
- use missing Art to renumber Overlay types;
- construct navigation/collision in the Core parser;
- claim WAE or PPM behavior is official runtime source;
- publish bridge positions in the future sanitized audit.