# ADR 0048: M6-C2 legacy visual import readiness remains raw and explicit

## Status

Accepted for synthetic/configured implementation.

## Decision

Reuse the existing PAL and SHP readers. Add provider-neutral indexed-image and
palette-binding descriptors, plus bounded raw VXL/HVA readers and an exact-name
VXL/HVA binder. Preserve raw fields, source order, diagnostics, and canonical
hashes. Require explicit palette conversion and HVA record-order profiles.

## Consequences

- Core remains free of UnityEngine, UnityEditor, Texture2D, Sprite, Mesh, and
  renderer authority.
- No palette fallback, axis swap, matrix composition, normal-table guess, or
  frame-order trial is permitted.
- Unsupported SHP frames and unresolved VXL/HVA semantics remain explicit
  diagnostics/fallback candidates rather than compatibility claims.
- A future Unity adapter may consume these descriptors without changing
  simulation authority.

## Evidence boundary

Synthetic fixtures cover raw PAL binding, indexed-image immutability, VXL
headers/sections/spans, HVA records and explicit order candidates, and exact
name binding. No ProjectBaseline packed visual data was read; original-runtime
and pixel-parity claims remain unconfirmed.
