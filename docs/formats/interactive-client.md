# Interactive client foundation

M6-C7 adds the bounded interactive seam between Unity input and the
authoritative Simulation layer.

## Contracts

- `VisibilitySnapshotBuilder` keeps visible, fogged, shrouded, and unknown
  states explicit. Duplicate and unknown policies are caller-selected and
  diagnostics are bounded without fail-open behavior.
- `SelectionService` returns deterministic, immutable entity selections. It
  never invents or removes simulation entities.
- `IsometricPointerInterpreter` uses an explicit tile geometry, viewport, and
  pan profile. It does not infer axis order from visual plausibility.
- `ClientCommandGateway` emits only `CommandRequest` values with
  `CommandSource.Human` and submits them to the existing Simulation
  `CommandQueue`. The gateway does not tick or otherwise mutate the world.
- HUD, production availability, placement preview, and lighting/weather
  profiles are read-only presentation models. Production blockers remain
  visible rather than being silently hidden.
- `UnityInteractiveClient` is an adapter for screen input and bounded pick
  targets. It has no per-entity simulation `Update` loop.

## Compatibility boundary

This is synthetic/configured client behavior. No ProjectBaseline packed data,
original UI, network protocol, palette, TMP/theater data, or gameplay runtime
was read or reproduced. Real camera raycast semantics, widget layout, audio,
weather rendering, and original interaction parity remain unresolved.
