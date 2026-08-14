# Object visual presentation foundation

M6-C4 adds a Unity-free object presentation boundary for the presentation
snapshot pipeline. It preserves the distinction between logical ground
anchors, authored frame/pivot candidates, visual bounds, culling bounds,
selection bounds, occupancy bounds, foundation bounds, and shadow bounds.

## Explicit object and depth contracts

`ObjectVisualPresentationDescriptor` carries a stable `VisualAssetId`, object
family, render pass, elevation layer, logical ground anchor, separate bounds,
raw level/height candidates, explicit Z adjustment, parent/attachment identity,
and canonical source ordinal. No foundation or occupancy is inferred from an
image rectangle. Attachments require an explicit parent identity.

`RenderDepthKey` is a deterministic tuple. Pass and elevation are compared
before the explicit primary depth candidate, then Z adjustment, family,
parent/attachment fields, source ordinal, stable identity, and duplicate
ordinal. The policy rejects camera-dependent depth and uses checked arithmetic;
Unity instance IDs, hash enumeration, camera zoom, and random tie-breaking are
not inputs.

Duplicate stable identities are either preserved with a warning or rejected by
an explicit policy. The composer is bounded and fail-closed, including when
the diagnostic budget is zero. The presentation result remains downstream of
Simulation and does not mutate authoritative state.

The UnityIntegration adapter emits ordered draw commands with Unity anchor
vectors only. It creates no per-object GameObject, texture, material, palette
binding, or simulation component. Asset upload and renderer lifecycle remain
separate adapters.

## Compatibility boundary

- Object families, anchors, bounds, and depth tuples: synthetic/configured.
- Bridge and aircraft layers: explicit project candidates only.
- Original runtime pass list, depth comparator, foundation semantics, palette
  parity, and renderer draw order: not confirmed.
- ProjectBaseline packed visual data: not read in this work package.
- Writer, recompressor, gameplay, pathfinding, and Simulation authority:
  unchanged and out of scope.
