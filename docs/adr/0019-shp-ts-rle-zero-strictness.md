# ADR 0019: SHP(TS) RLE-Zero remains strict despite baseline tolerance evidence

## Status

Accepted for M2-SHP1 with a recorded ProjectBaseline compatibility conflict.

## Context

Public sources agree that flags 3 uses scanline RLE-Zero, but implementations
differ in tolerance. The work-package contract requires declared row input to
be fully consumed and decoded output to equal the descriptor width exactly.
It explicitly forbids clamping, padding, truncation, and reading another frame.

The fixed ProjectBaseline audit found 257 non-empty flags 3 frames across
building, infantry, map add-on, and animation roles. Each first row would emit
exactly `widthRaw + 1` indices. This affects both even and odd widths. XCC's
observable implementation clamps runs that cross the row boundary, while the
strict contract requires a controlled failure.

## Decision

- Keep the independent flags 3 decoder strict: a literal or zero run that
  crosses the local row width returns `RleOutputOverflow`.
- Do not add a hidden tolerant mode or specialize behavior by logical name,
  sample role, width parity, MIX provenance, or ProjectBaseline identity.
- Record the 257 baseline failures as sanitized evidence and return audit
  status `CompleteWithDecodeFailures`.
- Keep flags 2 unresolved rather than choosing either XCC RLE-Zero or OpenRA
  length-prefixed raw behavior.
- Keep `00 00` unresolved. It consumes input and counts as a command, but the
  default path does not claim no-op or corruption semantics.
- Do not promote ProjectBaseline flags 3 compatibility until a separately
  approved compatibility policy is defined and tested against original
  behavior.

## Consequences

Synthetic valid RLE-Zero inputs are decoded deterministically and malformed
rows fail closed. Raw baseline frames remain usable. Compressed baseline
frames are deliberately unavailable to later rendering work through this
strict path, preventing a permissive compatibility assumption from becoming a
format fact.

The zero count for `00 00` is not exhaustive because strict failures stop on
row 0 before later rows are inspected.

## Rejected alternatives

- Clamp overflowing zero runs as XCC does.
- Increase every declared width by one.
- Apply odd-width padding; even-width samples fail the same way.
- Treat the next data offset as permission to emit additional pixels.
- Mark flags 3 ProjectBaseline support as passed because directories parse.
