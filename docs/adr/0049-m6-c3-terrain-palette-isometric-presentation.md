# ADR-0049: M6-C3 terrain palette and isometric presentation foundation

## Status

Accepted for synthetic/configured implementation.

## Context

M6 needs a presentation boundary that can consume raw terrain and legacy visual
descriptors without turning presentation state into simulation authority. The
boundary must preserve unresolved TMP, palette, and raw tile facts while
remaining testable without ProjectBaseline packed payloads.

## Decision

Keep terrain presentation in two layers. Core owns immutable tile descriptors,
explicit palette binding metadata, checked isometric projection, and stable
chunk descriptors. `RA2YR.UnityIntegration` owns the optional one-mesh-per-
chunk adapter. Axis order, rounding, channel profiles, and row/palette
interpretations are explicit inputs; no plausibility or automatic fallback is
allowed.

The adapter is geometry-only. It does not create per-tile GameObjects, decode
new formats, write or recompress assets, bind TMP/theater data, or mutate the
Simulation assembly. ProjectBaseline packed terrain data is not read as part
of this decision.

## Consequences

The project gains a bounded synthetic foundation for deterministic terrain
presentation and a clear Unity integration seam. Original runtime draw order,
palette parity, camera/depth rules, and complete terrain rendering remain
evidence-gated follow-up work. The raw M3 format contracts remain unchanged.
