# M6 Human Playtest Delivery

This is a bounded human-playtest scene for exercising the M6 Simulation,
Presentation, Unity renderer, and interactive-client seams. When the local
`Config/ExternalContent.local.xml` enables `YR1001_ProjectBaseline`, the Unity
adapter performs a read-only, bounded probe of configured legacy MIX content
and prefers indexed SHP frames plus palette data for object visuals. Without
that local source it uses the explicit synthetic fallback. This is not a stock
Yuri's Revenge map loader and it does not assert original-runtime compatibility.

External object selection is typed and explicit: configured Rules registry type
ids are joined to Art records, then to logical image/voxel and palette assets
through the bounded Content/MIX VFS. The provider does not contain a physical
filename catalog. Each resolved role owns its own SHP frame or VXL cell set;
VXL/HVA bindings are not used for SHP buildings, and an unavailable VXL role
does not fall back to an unrelated SHP asset.

## Scene and entry point

Open `Assets/Scenes/RA2YRSyntheticSkirmish.unity` with Unity
2022.3.60f1c1, or run it from the Player build scene list. The scene contains
one `UnitySyntheticSkirmishBootstrap` component. The bootstrap owns the
presentation-only camera, prefers configured external indexed visuals, and
falls back to procedural placeholders. It creates a bounded 28 by 22
battlefield and advances the existing deterministic
`HumanPlaytestRuntime` at a 15 Hz simulation cadence. Input is translated to
Human `CommandRequest` values through `UnityInteractiveClient`; input never
writes Simulation transforms directly.

The bounded setup includes two human units, a harvester, refinery, factory,
power and base, plus an opponent base, factory and rule-based unit. Resource
settlement, production, autonomy, movement, combat, defeat/victory state, fog
visibility, selection, and HUD state are deliberately small and observable.
External visual status is exposed separately from Simulation state; the
terrain remains an explicit synthetic isometric chunk because TMP/theater
binding is not part of this work package.

## Controls

* LMB selects a unit; drag selects a box; Shift adds to selection.
* RMB issues Move. Press `A` first for AttackMove; RMB on an opponent issues
  Attack.
* `S` Stop, `H` Hold, `M` Manual autonomy, `T` Assisted autonomy, `O`
  Automatic autonomy, `P` queue production.
* `Esc` pauses/resumes; `R` restarts the synthetic match.
* WASD or arrow keys pan the camera; the mouse wheel zooms.

## Boundary and limitations

The configured visual probe is read-only and does not publish payload bytes,
decoded pixels, filenames, paths, or per-asset records. It reuses the existing
managed RawLzo1X/presentation readers; this package adds no codec or writer.
Palette remapping defaults to the explicit `SourcePaletteOnly` profile. Any
implementation-specific remap offsets must be supplied as a separate
configured profile and are not YR-authenticity claims.
The scene does not load ProjectBaseline maps, Overlay/Preview map semantics,
TMP/theater data, or a full scenario. It does not implement a writer, LZO
writer, original UI/network input, minimap, replay, pathfinding, or gameplay
parity. The rule-based opponent is a local synthetic controller, not the
original YR AI. Manual playability and external visual availability do not
promote any compatibility-matrix status to original-runtime confirmation.

Headless Unity smoke tests start paused so asynchronous scene integration cannot
advance the synthetic simulation before the test drives it; an interactive
Editor/Player launch starts unpaused.

## Verification

`M6HumanPlaytestRuntimeTests` covers headless command, economy, production,
autonomy, combat, reset, and Simulation-to-Presentation hash equivalence.
`M6HumanPlaytestSceneSmokeTests` loads the actual scene and covers bootstrap,
selection/move/production, opponent combat, and restart target rebuilding.
The final evidence records the exact current-head XML paths, the configured
external preflight result, and separates defined NUnit executions from
executed results. The sanitized ProjectBaseline PreviewPack audit is recorded
separately; it is aggregate evidence only and remains `NotConfirmed` for the
original runtime.
