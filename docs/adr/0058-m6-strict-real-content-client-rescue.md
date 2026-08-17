# ADR 0058: M6 strict real-content client rescue remains fail-closed

## Context

The M6 human-playtest scene previously allowed a successful simulation and a
partial external visual probe to coexist with synthetic blue sprites and a
procedural green terrain chunk. That state is not a real-content playable
vertical slice.

## Decision

Keep `SyntheticOnly` for deterministic tests and make `StrictRealContent` an
explicit runtime mode. Strict mode uses the configured read-only MIX source,
the existing Rules/Art/PAL/SHP/VXL/HVA readers, and the managed RawLzo1X
backend. It never silently creates a procedural visual when a required route
fails. `StrictOriginalContentPreflight` reports `PresentationRouteIncomplete`
until real TMP/theater terrain and ore/resource presentation are bound.

The strict SHP route uses an evidence-gated
`ValidatedTrailingTransparentGuard` profile. It accepts only a terminal zero
run whose decoded span is exactly one transparent guard byte beyond the
declared row width. The default strict decoder remains unchanged; this is not
a general flags-3 compatibility claim and does not add clamping, padding,
width widening, or a corpus-specific asset exception.

The strict VXL/HVA route allows an unbound VXL section only when every HVA
section still binds. The unbound section stays raw and does not receive an
invented transform. Input uses an explicit RA2-style state machine: edge
scroll/arrows for camera, left click/drag for selection and contextual orders,
right click for cancel/deselect, and an Alt modifier for AttackMove. Harvester
commands have precedence over synthetic economy automation.

## Status and limits

The configured patched-development corpus currently resolves six SHP frames
under the explicit profile and three VXL/HVA roles. This is sanitized local
evidence, not original-runtime confirmation. Real terrain/TMP/theater and
ore/resource bindings remain incomplete, so the human visual/playability gate
is still blocked and no M6 completion claim is made.
