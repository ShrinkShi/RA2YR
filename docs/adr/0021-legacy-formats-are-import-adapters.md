# ADR 0021: Legacy formats are import adapters, not canonical runtime assets

## Status

Accepted as a project-wide architecture constraint.

## Context

SHP/PAL and VXL/HVA are compatibility inputs. Their file layouts, indexed
pixels, palettes, frame rectangles, voxel transforms, and historical naming
rules are necessary for importing existing content, but they are not a stable
contract for simulation or for future rendering capabilities.

Allowing gameplay code to depend directly on `.shp` or `.vxl` would couple
simulation identity to one legacy representation. It would also make future
RGBA artwork, higher-resolution animation, modern models, additional material
channels, and non-Unity tools depend on accidental reader details.

## Decision

- Simulation and configuration-facing runtime models reference an opaque
  logical `VisualAssetId`; they do not inspect file extensions or format
  descriptors.
- SHP/PAL and VXL/HVA implementations are legacy import providers. They
  translate preserved format facts into provider-neutral visual asset data.
- Pixel dimensions, world dimensions, pivot/anchor, footprint, and collision
  are independent concepts with independent provenance.
- Team color is represented as an abstract mask/remap channel. A legacy
  palette range is one provider input, not the canonical runtime model.
- Dynamic lighting, shadows, water reflections, materials, and other render
  policy do not belong in format readers or decoders.
- Core owns format models, import contracts, logical identifiers, and
  deterministic metadata. Unity adapters consume those contracts and create
  Unity-specific resources without becoming authoritative simulation state.
- Future providers may supply RGBA textures, multiple material channels,
  high-resolution animation, or modern 3D models without changing gameplay
  code or logical visual identity.

## Consequences

Legacy readers remain small, testable, and evidence-driven. Rendering can
advance without rewriting simulation contracts, while compatibility-specific
facts remain available through provenance and raw fields.

Typed Rules/Art views may discover explicit legacy references, but they may
not make `.shp`/`.vxl` branching a gameplay rule. A later routing/import layer
must resolve those references to a `VisualAssetId` and provider result.

## Rejected alternatives

- Store SHP or VXL filenames directly in simulation entities.
- Treat indexed pixels or voxel data as the only canonical asset form.
- Infer world scale, pivot, collision, shadows, or material policy in a format
  reader.
- Put Unity `Texture2D`, `Sprite`, `Material`, or `GameObject` values in Core.

