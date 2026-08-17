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
filename catalog. Each resolved role owns its own SHP frame or section-aware
VXL presentation. VXL raw coordinates use the explicit
`RawXToWorldX_RawYToWorldZ_RawZToWorldY` basis, a bounds-center pivot, and
bounded normalization rather than a fixed scale constant. HVA frame 0 is
applied to the actual section hierarchy. Voxel color indices become vertex
colors through the source PAL; the format-layer PAL remains raw 0..63 and the
configured `PaletteDisplayProfile` is applied exactly once at the Unity display
boundary by the shared PAL conversion adapter. SHP texture colors and VXL mesh
colors therefore use the same raw-to-display semantics. Owner identity uses a
separate marker ring and does not recolor the external mesh. VXL/HVA bindings are not used for SHP
buildings, and an unavailable VXL role does not fall back to an unrelated SHP
asset.

The local real-content gate is stage-based and fail-closed. It distinguishes
configuration/source availability, typed Rules, typed Art, role descriptors,
VFS lookup, strict decode, and final external role resolution. The current
sanitized configured-source result reaches `ExternalVisualsResolved`: two
distinct VXL/HVA battlefield roles (one human and one enemy) are used by the
scene. Both pass the presentation sanity gate (section-aware 2, HVA-applied 2,
palette-colored 2, maximum width 0.85 cells, maximum height 0.372 cells),
while six strict SHP failures and one strict VXL/HVA failure retain per-role
synthetic fallback. The scene composes both providers so a partial external
result never produces a null or misleading visual entry.

## Scene and entry point

Open `Assets/Scenes/RA2YRSyntheticSkirmish.unity` with Unity
2022.3.60f1c1, or run it from the Player build scene list. The scene contains
one `UnitySyntheticSkirmishBootstrap` component. The bootstrap owns the
presentation-only camera, uses the same centered isometric basis as the
synthetic terrain chunk, prefers configured external indexed visuals, and
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

* LMB selects a friendly unit or structure; drag selects a box; Shift adds to
  selection. A Harvester is a selectable, controllable mobile entity.
* With a selection, LMB on ground issues Move, on an opponent issues Attack,
  and on the contextual resource cell issues Harvest. RMB deselects and
  cancels targeting; it is not a Move command.
* `S` Stop, `H` Hold, `M` Manual autonomy, `T` Assisted autonomy, `O`
  Automatic autonomy, `P` queue production.
* `Esc` pauses/resumes; `R` restarts the synthetic match. `Alt` modifies a
  ground order into AttackMove.
* Mouse-edge scrolling is primary camera pan; arrow keys are auxiliary and
  the mouse wheel zooms. WASD is not camera input.

## Boundary and limitations

The configured visual probe is read-only and does not publish payload bytes,
decoded pixels, filenames, paths, or per-asset records. It reuses the existing
managed RawLzo1X/presentation readers; this package adds no codec or writer.
The Art section-identity fallback is enabled only by the named
`ExplicitOrSectionIdentifier` configured policy and still requires an active
typed Art record; it is not inferred from a physical file lookup.
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

## 2026-08-17 human visual failure closure

The maintainer's Unity Game View inspection of commit
`1767cc708f79535c9fcd0ddd9f7fa757f7706f9d` was **FAIL**, despite the
automated gates being green. The observed blockers were regular blue holes
between synthetic terrain diamonds and VXL units that read as an unlit/debug
voxel viewer. Missing RA2 sidebar/HUD fidelity and audio/EVA remain deferred
observations, not this PR's scope.

The terrain repair uses one checked doubled-unit projection contract for tile
centers, diamond extents, entity placement, pointer inverse, and camera
centering. It does not overlap tiles, add filler quads, or special-case the
28x22 fixture. VXL now preserves `NormalIndex` and `NormalTypeRaw` and uses the
explicitly named `DerivedGeometryNormalPresentation` mode because the
repository does not publish a complete evidence-backed Westwood normal table.
The mesh emits finite normalized normals and uses the self-authored
`RA2YR/ExternalLegacyVxlLit` vertex-color lit shader with stable ambient and
directional terms. Original Westwood normal/light semantics remain
unconfirmed.

The repaired tree is only a signal for the next maintainer check. Automated
results must be reported from the exact final HEAD; this document never
converts them into a human visual pass or an original-runtime claim. A future
content architecture requirement is recorded separately: MIX-first logical
lookup across project, external, MOD, and modern sources, with legacy/modern
providers and provenance-based policy. That architecture is not implemented
by this PR.

## 2026-08-17 client-rescue status

The current rescue work separates `SyntheticOnly`, `ExternalLegacyPreferred`,
and `StrictRealContent`. Strict mode never creates the blue procedural sprite
or green procedural terrain when the configured original-content route is
incomplete. It reports a fail-closed preflight result instead.

The configured patched-development corpus now passes the explicit SHP
`ValidatedTrailingTransparentGuard` profile for six structure frames and the
managed VXL/HVA route resolves three roles. The default SHP decoder remains
strict; this profile accepts only a terminal zero-run producing exactly one
transparent guard byte and is not a general flags-3 compatibility claim.
VXL/HVA partial section binding is also an explicit strict-content policy: an
unbound VXL section is allowed only when every HVA section still binds and the
unbound section remains raw/visible without an invented transform.

The real terrain/TMP/theater and real ore/resource presentation bindings are
not yet connected to the runtime. `StrictOriginalContentPreflight` therefore
returns `PresentationRouteIncomplete` and the human scene is not reported as
playable-ready. The new RA2 input seam removes WASD camera ownership, uses
edge-scroll plus arrow fallback, treats right-click as cancel/deselect, shows
the drag rectangle and selection health/cargo bars, and gives the Harvester an
explicit player-command precedence over synthetic economy automation. These
changes are automated readiness evidence only; a maintainer must still launch
Unity and perform the human visual check after the real terrain/resource route
is completed.
