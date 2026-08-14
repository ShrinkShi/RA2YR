# ADR-0050: M6-C4 object visual presentation foundation

## Status

Accepted for synthetic/configured implementation.

## Context

Object families need separate authored anchors, bounds, elevation layers, and
stable depth ordering. A single image rectangle or Unity transform cannot
represent foundation, occupancy, selection, shadow, attachment, bridge, and
aircraft relationships without losing raw provenance.

## Decision

Keep object presentation descriptors and deterministic tuple depth keys in the
Unity-free `RA2YR.Presentation` assembly. Require explicit family, pass,
elevation, logical ground anchor, visual/culling bounds, stable identity, and
source ordinal. Preserve optional selection, occupancy, foundation, and shadow
bounds as distinct values. Attachments require explicit parent identity.

Provide a bounded composer with explicit duplicate policy and camera-independent
depth policy. The UnityIntegration layer may turn an already ordered result
into draw commands, but it does not create GameObjects, textures, materials,
palette conversions, or simulation state.

## Consequences

The project can test object ordering and anchor/bounds separation without
ProjectBaseline payloads or renderer side effects. Original RA2/YR family
comparators, foundation semantics, palette parity, and full renderer behavior
remain evidence-gated follow-up work.
