# Visual asset pipeline

Legacy formats are import adapters, not canonical runtime assets. The visual
pipeline therefore separates logical identity, import, provider-neutral asset
data, and host rendering.

```text
Rules / Art / map references
  -> logical VisualAssetId
  -> provider routing and provenance
      -> legacy SHP + PAL provider
      -> legacy VXL + HVA provider
      -> future RGBA / animation provider
      -> future modern-model provider
  -> provider-neutral visual asset description
  -> Unity adapter
  -> Texture / mesh / material / animation runtime objects
```

## Authoritative identity

Simulation refers only to a logical `VisualAssetId`. It does not inspect
`.shp`, `.pal`, `.vxl`, `.hva`, texture extensions, or Unity resource types.
Format-specific routing belongs to import/content layers and retains complete
source provenance.

The logical identifier remains stable when an asset is replaced by a modern
representation. Provider selection must be explicit and deterministic; host
directory enumeration is never a priority rule.

## Independent geometry concepts

The following values are not interchangeable and must not be inferred from
one another without separate evidence:

- source pixel or voxel dimensions;
- world-space dimensions and scale;
- pivot or anchor;
- footprint and placement cells;
- collision or selection bounds.

A format reader reports only facts encoded by that format. Art, Rules, map,
provider, or rendering policy may contribute other values later, with their
own provenance.

## Color and material channels

Team color is an abstract mask/remap channel. A legacy palette range may
produce that channel, but the canonical model is not limited to palette
indices. Future assets may provide RGBA color plus one or more masks or
material channels.

Dynamic lights, shadow policy, water reflection behavior, shaders, material
selection, and post-processing are rendering concerns. They do not enter SHP,
PAL, VXL, or HVA readers and decoders.

## Core and Unity boundary

Core may contain:

- immutable raw format models;
- bounded readers and decoders;
- logical visual identifiers;
- provider-neutral import contracts and diagnostics;
- provenance and deterministic canonical hashes.

Unity integration may contain:

- `Texture2D`, `Sprite`, mesh, material, shader, and animation construction;
- renderer lifecycle and caching;
- platform-specific upload and display behavior.

Unity objects never become authoritative simulation state and are not exposed
back through legacy format models.

## Compatibility boundary

Parsing or decoding a legacy file proves only that format stage. It does not
prove palette selection, RGBA appearance, pivot, remap, shadow pairing,
animation behavior, world scale, or gameplay compatibility. Each later stage
requires its own evidence and compatibility entry.
