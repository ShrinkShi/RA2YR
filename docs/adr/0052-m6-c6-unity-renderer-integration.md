# ADR-0052: M6-C6 Unity renderer integration foundation

## Status

Accepted for synthetic/configured implementation.

## Decision

Keep the Presentation and Simulation assemblies Unity-free. Centralize Unity
resource ownership in `UnityPresentationWorld`, use bounded deterministic asset
cache keys, share material profiles, and map object/effect depth through one
host adapter. Terrain is one mesh per chunk and synthetic VXL geometry is one
exposed-face mesh, with no voxel or tile GameObjects.

## Consequences

The project can exercise resource lifecycle, indexed texture/palette lookup,
camera pan/zoom, terrain mesh reuse, object/effect submissions, and bounded
voxel geometry without making Unity state authoritative. Full renderer parity,
shader parity, TMP/theater binding, flags-2/flags-3 SHP compatibility, and
original runtime comparison remain unresolved.
