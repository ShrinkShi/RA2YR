# Effects, depth, shadow, and fog presentation foundation

M6-C5 adds explicit presentation inputs for effects, alpha/depth policy,
shadows, and fog/shroud visibility. It is a provider-neutral layer downstream
of the M6 snapshots and object descriptors. It does not execute weather,
lighting, audio, gameplay, or Simulation state.

## Explicit contracts

`EffectPresentationDescriptor` preserves a stable visual identity, effect
kind, explicit anchor and bounds, elevation layer, alpha mode, depth-test mode,
visibility state, parent identity, and source ordinal. Translucency does not
implicitly choose a final pass or depth behavior.

`ShadowPresentationDescriptor` keeps caster identity, receiver layer, source
candidate, shadow anchor, bounds, and color profile separate. A shadow is never
an occupancy or pathfinding input, and missing geometry is diagnosed rather
than synthesized.

The composer uses a checked, deterministic effect depth tuple with explicit
duplicate policy and bounded effects/shadow/diagnostic budgets. Fogged,
shrouded, and unresolved entries remain in the logical result. An explicit
policy controls only visual submission annotation; visibility filtering never
deletes the logical entity or changes its depth identity.

## Compatibility boundary

- Effect, shadow, alpha/depth, and fog/shroud contracts: synthetic/configured.
- TMP depth bytes: not interpreted as Level, alpha, bridge state, or simulation
  elevation by this work package.
- Original runtime occlusion, per-pixel depth, shadow projection, palette
  parity, fog/shroud grid semantics, and draw order: not confirmed.
- ProjectBaseline packed visual data: not read.
- Renderer, weather, audio, gameplay, pathfinding, and Simulation authority:
  unchanged or deferred.
