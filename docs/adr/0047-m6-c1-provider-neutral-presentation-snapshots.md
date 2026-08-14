# ADR-0047 — Provider-neutral presentation snapshots

## Status

Accepted for synthetic/project-enhancement implementation.

## Decision

Presentation consumes immutable Simulation snapshots and emits provider-neutral
descriptors. A stable logical `VisualAssetId`, explicit semantic render pass,
raw integer position, deterministic tuple ordering, bounded diagnostics, and
independent execution state form the M6-C1 contract. Missing or ambiguous asset
providers fail closed unless an explicit policy preserves an unresolved
descriptor. Interpolation is checked integer fixed-point arithmetic and cannot
mutate Simulation state.

`RA2YR.Presentation` remains Unity-free. Unity adapters are downstream and may
create Texture2D, Sprite, mesh, material, animation, or renderer objects only
after a provider has resolved a logical asset. No Simulation type refers back to
Presentation.

## Evidence and limits

The render-pass vocabulary and ordering are project policy informed by the
isometric-rendering-order dossier; they are not a confirmed original-runtime
draw list. M6-C1 tests are synthetic and do not read ProjectBaseline packed
data. Palette binding, SHP/VXL/HVA visual conversion, TMP/theater semantics,
camera, fog, UI, rendering, gameplay, and original-runtime confirmation remain
deferred.
