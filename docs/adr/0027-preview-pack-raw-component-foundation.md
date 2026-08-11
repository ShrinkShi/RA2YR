# ADR-0027: PreviewPack remains a raw, explicitly profiled component boundary

- Status: Accepted for synthetic/configured implementation
- Date: 2026-08-12

## Context

PreviewPack combines lossless INI occurrences, the shared packed-section
pipeline, and a decoded byte stream whose metadata and visual semantics are
not fully confirmed. Treating the bytes as a rendered image would silently
choose channel order, row order, dimensions, or palette semantics.

## Decision

Implement only a bounded raw foundation. Preserve the four signed `Preview`
`Size` fields, require explicit section and duplicate selection, require an
injected `RawLzo1X` backend, and require the exact checked
`width * height * 3` decoded length. Expose raw component triples separately
from explicit RGB/BGR and row-order derived views. Keep execution state,
diagnostics, suppression counts, and provenance as independent result data.

## Consequences

Synthetic/configured PreviewPack behavior can be tested without introducing
UnityEngine, palettes, textures, sprites, TMP, theater semantics, or map
loading. A real ProjectBaseline packed audit and original-runtime comparison
remain future, explicitly authorized work; this ADR does not promote either
to confirmed compatibility.
