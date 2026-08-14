# M6-C1 provider-neutral presentation snapshots

M6-C1 introduces a Unity-free presentation boundary. It consumes an immutable
`RA2YR.Simulation.SimulationReadSnapshot` and produces an immutable
`RA2YR.Presentation.PresentationSnapshot`; it does not mutate the simulation,
create Unity objects, or decide gameplay legality.

## Contracts

- `VisualAssetId` is a stable logical identity. It does not contain a required
  SHP, VXL, HVA, PAL, TMP, texture, or Unity object interpretation.
- `PresentationEntityDescriptor` retains the simulation `EntityId`, logical
  asset id, raw integer position, semantic `PresentationRenderPass`, parent and
  attachment ordering, and a stable source ordinal.
- Semantic passes are `Terrain`, `TerrainOverlay`, `GroundShadow`,
  `GroundObject`, `Structure`, `Vehicle`, `Infantry`, `Projectile`,
  `Aircraft`, `Effect`, `FogShroud`, and `UIWorld`. They are project policy
  values, not a claim about the original renderer draw list.
- `PresentationSnapshotAssembler` sorts by explicit pass/position/attachment/
  source/entity tuple. It never uses Unity instance ids or dictionary
  enumeration order. Created, persisted, and despawned entity changes are
  retained between snapshots.
- `IVisualAssetProvider` is an injected provider contract. Missing assets fail
  closed by default; an explicit `PreserveUnresolved` policy may retain the
  descriptor with a diagnostic. More than one resolved provider is ambiguous
  and never selects a winner.
- Diagnostics are bounded, while `PresentationExecutionState` independently
  records fatal failure, highest severity, and suppressed diagnostics. A zero
  diagnostic budget therefore cannot turn an invalid snapshot into success.
- `PresentationInterpolator` uses checked integer fixed-point arithmetic and an
  explicit rational fraction. It does not use frame delta, camera state, or
  Unity transforms.

## Boundary and compatibility

The `RA2YR.Presentation` assembly has `noEngineReferences: true` and references
only Core and Simulation. Unity adapters may later consume these descriptors,
but Texture2D, Sprite, Material, GameObject, renderer lifecycle, palette
conversion, VXL/HVA rasterization, TMP/theater binding, and visual effects are
outside M6-C1.

The evidence is synthetic/project-enhancement evidence only. It does not claim
original-runtime render ordering, palette choice, visual appearance, map
loading, or ProjectBaseline packed-data compatibility. Existing M3 readers and
M3-C4 RawLzo1X remain upstream contracts; M6-C1 adds no codec or writer.
