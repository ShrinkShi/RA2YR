# Terrain presentation foundation

M6-C3 adds an explicit, provider-neutral terrain presentation boundary. It
consumes raw or configured tile descriptors and produces deterministic chunk
descriptors. The Core contract does not create Unity objects, mutate
simulation authority, resolve a palette by visual plausibility, or select a
TMP/theater interpretation implicitly.

## Explicit contracts

`TerrainTilePresentationDescriptor` keeps logical tile identity, raw SubTile
and Level values, optional raw TMP candidates, explicit tile-set/local ordinal,
optional palette binding, source ordinal, and logical grid coordinates. The
raw fields are not renamed to a resolved theater tile or TMP reference.

`IsometricProjectionProfile` requires explicit tile dimensions, height step,
axis order, and rounding policy. Projection and inverse candidates use checked
arithmetic. Camera zoom is not part of logical depth, and no Size field from a
packed format is used as a coordinate offset.

`TerrainPresentationComposer` groups cells into deterministic chunk identities,
orders cells by source ordinal, and stops at an explicit cell or diagnostic
budget. Null descriptors and budget failures are reported as structured
failures; diagnostics cannot turn a failed build into success.

The Unity adapter in `RA2YR.UnityIntegration` builds one bounded `Mesh` per
chunk. It creates no per-tile `GameObject`, material, texture, palette
conversion, or simulation component. The adapter is downstream of Core and is
not a complete map renderer.

## Compatibility boundary

- Terrain presentation and isometric projection: synthetic/configured.
- TMP/theater binding: explicit candidates only; original runtime semantics
  remain unconfirmed.
- Palette channel/conversion selection: explicit provider input; no fallback.
- Chunk mesh batching: Unity adapter foundation only.
- ProjectBaseline packed terrain/visual data: not read by this work package.
- Original-runtime draw order, depth sorting, camera, fog, palette parity,
  renderer parity, and gameplay authority: not confirmed or not implemented.

The M3 raw readers remain authoritative for raw format facts. M6-C3 does not
rewrite their semantics or claim that a complete RA2/YR map can be rendered.
