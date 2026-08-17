# ADR 0057: M6 human visual failure closure

## Status

Accepted for the M6 project-enhancement presentation boundary.

## Context

The automated gates for PR #77 were green at `1767cc708f79535c9fcd0ddd9f7fa757f7706f9d`, but maintainer Game View inspection found regular gaps between synthetic terrain diamonds and VXL units that looked like an unlit/debug voxel viewer. This was a presentation defect, not evidence that TMP/theater or original-runtime compatibility had been achieved.

## Decision

Use one checked doubled-unit isometric projection for terrain centers, diamond
geometry, entity placement, pointer inverse, and camera centering. Preserve
VXL `NormalIndex` and section `NormalTypeRaw` as raw provenance. Because the
repository does not publish a complete evidence-backed Westwood normal table,
the only supported lighting mode is explicitly named
`DerivedGeometryNormalPresentation`; it derives finite normalized face normals
and is not an original-runtime normal-semantics claim. Use the self-authored
`RA2YR/ExternalLegacyVxlLit` shader with vertex-color albedo and stable ambient
plus directional lighting. Keep PAL raw/display conversion, HVA frame 0,
section hierarchy, axis basis, bounds pivot, and owner marker separation.

## Consequences

The repaired tree is eligible for a new maintainer visual inspection only after
exact-head automated gates. UI/sidebar, audio/EVA, complete SHP animation, and
TMP/theater terrain remain deferred. Original Westwood normal and lighting
parity remains unresolved.

The future content architecture requirement is recorded but not implemented:
MIX is a first-class runtime source, logical assets can resolve from project,
external YR, MOD, or modern providers, and provenance-based policy replaces
extension-only decisions in a later M7-C0 package.
