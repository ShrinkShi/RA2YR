# ADR-0051: M6-C5 effects, depth, shadow, and fog presentation foundation

## Status

Accepted for synthetic/configured implementation.

## Context

Effects, shadows, alpha/depth behavior, and fog/shroud visibility are related
at presentation time but are not the same semantic dimension. Logical entities
must remain stable even when visual submission is hidden by a visibility state.

## Decision

Keep explicit effect descriptors, alpha/depth policies, shadow descriptors,
receiver layers, and fog/shroud annotations in the Unity-free Presentation
assembly. Use checked deterministic tuples and bounded fail-closed composition.
Preserve logical entries for fogged, shrouded, and unresolved visibility; do
not infer alpha, depth, shadow geometry, or occupancy from appearance.

The UnityIntegration layer remains a downstream adapter seam. No TMP depth
reinterpretation, weather/audio execution, renderer shader, or Simulation
mutation is introduced.

## Consequences

The presentation pipeline can test visibility and shadow relationships without
deleting logical state or claiming original runtime parity. Per-pixel depth,
occlusion, shadow projection, fog grid behavior, and renderer implementation
remain evidence-gated follow-up work.
