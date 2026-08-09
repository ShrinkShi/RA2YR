# ADR 0025: Overlay packed arrays remain raw, bounded, and section-explicit

## Status

Accepted for M3-C3 synthetic/configured implementation.

## Decision

M3-C3 adds an `OverlayPackedArrayReader` that composes the existing packed
fragment, strict Base64, chunk-envelope, and Format80 stages without changing
their contracts. `OverlayPack` and `OverlayDataPack` are selected as separate
section occurrences; missing, empty, selected, and ambiguous section states
remain distinct. Section names are validated against the explicit requested
section kind and are never used to infer a codec or a map role.

Only the ordinary `512 x 512 x 1` byte-array profile is implemented. The raw
array is required to have exactly 262,144 decoded bytes. Bytes are retained in
an immutable defensive-copy view and are exposed through explicit storage
index formulas only:

- `X + 512 * Y` for the external row-major candidate;
- `Y + 512 * X` for the official-editor transposed comparison.

The indexed view does not interpret `0xFF`, bind Rules or Art registries, or
construct Overlay, Preview, TMP, theater, palette, texture, sprite, or map
objects. `RawLzo1X` is explicitly rejected at this layer because M3-C1 still
provides a contract-only injected backend and no LZO algorithm.

Execution state is independent of the bounded diagnostic list. Packed and raw
array failures are fail-closed even when diagnostics are suppressed or the
diagnostic budget is zero. Provenance and child packed-stage results remain
available to callers.

## Consequences

The implementation proves only synthetic raw-array behavior, exact length,
bounded budgets, provenance, and explicit coordinate indexing. It does not
prove stock YR Overlay semantics, section precedence, resource meaning,
ProjectBaseline packed compatibility, or any visual/runtime behavior.

## Evidence boundary

The M3-C3 evidence uses synthetic fragments and fake/injected backends only.
No ProjectBaseline packed bytes, decoded map payload, image, palette, or
third-party binary is included in the repository. Existing GPL sources remain
reference-only and `code_imported: false`.
