# Unity renderer integration foundation

M6-C6 is the first UnityIntegration layer that consumes provider-neutral
presentation descriptors and creates bounded Unity resources. `Presentation`
and `Simulation` remain free of Unity references.

## Contracts

- `VisualAssetCache` uses a deterministic composite key containing asset,
  provider, palette/profile, variant, representation, frame, and remap profile.
  Capacity and eviction are explicit; the cache is not part of simulation
  state or hashes.
- `IndexedTextureFactory` keeps indexed source data separate from an optional
  256x1 palette lookup texture. Display conversion is an explicit configured
  profile; unresolved profiles do not invent colors.
- `UnityPresentationWorld` is a central lifecycle owner. Object/effect
  submissions use shared materials and a common depth mapping; terrain uses one
  mesh object per chunk. It has no per-entity gameplay `Update` loop.
- `UnityIsometricCameraAdapter` owns only presentation pan/zoom and viewport
  application. Camera zoom never changes logical depth.
- `VxlExposedFaceMeshBuilder` emits one bounded mesh from synthetic exposed
  voxel faces, never one GameObject per voxel. HVA remains presentation-only.

## Compatibility boundary

This is synthetic/configured Unity integration. Generated textures, palette
lookups, meshes, materials, and camera state are repository-safe test resources.
Unsupported or unresolved legacy semantics remain diagnostic/placeholder paths;
no ProjectBaseline packed visual payload was read and no original-renderer or
pixel-parity claim is made. M6-C6 adds no LZO algorithm or writer.
