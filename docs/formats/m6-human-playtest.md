# M6 Human Playtest Delivery

This is a bounded synthetic skirmish scene for manually exercising the M6
Simulation, Presentation, Unity renderer, and interactive-client seams. It is
not a stock Yuri's Revenge map loader and it does not assert original-runtime
compatibility.

## Scene and entry point

Open `Assets/Scenes/RA2YRSyntheticSkirmish.unity` with Unity
2022.3.60f1c1, or run it from the Player build scene list. The scene contains
one `UnitySyntheticSkirmishBootstrap` component. The bootstrap owns the
presentation-only camera and procedural placeholder sprites, creates a bounded
28 by 22 synthetic battlefield, and advances the existing deterministic
`HumanPlaytestRuntime` at a 15 Hz simulation cadence. Input is translated to
Human `CommandRequest` values through `UnityInteractiveClient`; input never
writes Simulation transforms directly.

The synthetic setup includes two human units, a harvester, refinery, factory,
power and base, plus an opponent base, factory and rule-based unit. Resource
settlement, production, autonomy, movement, combat, defeat/victory state, fog
visibility, selection, and HUD state are deliberately small and observable.

## Controls

* LMB selects a unit; drag selects a box; Shift adds to selection.
* RMB issues Move. Press `A` first for AttackMove; RMB on an opponent issues
  Attack.
* `S` Stop, `H` Hold, `M` Manual autonomy, `T` Assisted autonomy, `O`
  Automatic autonomy, `P` queue production.
* `Esc` pauses/resumes; `R` restarts the synthetic match.
* WASD or arrow keys pan the camera; the mouse wheel zooms.

## Boundary and limitations

The scene uses procedural placeholder textures and sprites. It does not read
ProjectBaseline packed content, PAL/SHP/TMP/VXL/HVA assets, or map files. It
does not implement a writer, LZO writer, map loading, original UI/network
input, palette/theater binding, minimap, replay, pathfinding, or gameplay
parity. The rule-based opponent is a local synthetic controller, not the
original YR AI. The scene proves an executable project-enhancement path only;
manual playability does not promote any compatibility-matrix status to
original-runtime confirmation.

## Verification

`M6HumanPlaytestRuntimeTests` covers headless command, economy, production,
autonomy, combat, reset, and Simulation-to-Presentation hash equivalence.
`M6HumanPlaytestSceneSmokeTests` loads the actual scene and covers bootstrap,
selection/move/production, opponent combat, and restart target rebuilding.
The final evidence records the exact current-head XML paths and separates
defined NUnit executions from executed results.
