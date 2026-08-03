# ADR 0020: SHP(TS) row-width forensic evidence remains non-production

## Status

Accepted for M2-SHP1F. Supersedes no decoder behavior from ADR 0019.

## Context

The strict flags-3 decoder rejects 257 fixed ProjectBaseline frames on row 0
because mechanical RLE-Zero output is `WidthRaw + 1`. Public implementations
do not establish one stock row-width contract, so M2-R2 required an
independent scalar probe before any compatibility rule could be proposed.

The probe does not call the production row decoder, encoder, fixture builder,
or indexed-frame reconstruction path. It reads each bounded row payload,
advances commands, and retains only integer and enum aggregates.

## Decision

- Lock Stage A to the existing 257-frame failure aggregate before inference.
- Run Stage B only when every Stage A extra output is a final zero-run zero,
  overshoots by exactly one, and ends at the declared row input boundary.
- Classify the observed result as decision B: row 0 is uniform, but all 257
  frames contain a mixture of exact-width and width-plus-one rows.
- Do not interpret category count differences as distinct format contracts;
  building, infantry, animation, and map-add-on samples all contain both row
  classes.
- Keep the production decoder strict and unchanged. Do not clamp, drop the
  last output, widen `WidthRaw`, or specialize by resource identity.
- Keep flags-3 ProjectBaseline compatibility unimplemented and do not
  recommend a production repair from this evidence.

## Evidence

Stage B analyzed 9,495 declared rows: 1,331 mechanically equal `WidthRaw` and
8,164 mechanically equal `WidthRaw + 1`. Every width-plus-one row ends with a
final zero-run, the extra output is zero, the overshoot is one, and ignoring
that output leaves input exactly consumed. No literal overflow, `00 00`, or
malformed row was observed. Every frame contains both row classes.

Memory, seekable Stream, and bounded MIX-window scalar results are identical.
The canonical forensic model SHA-256 is
`97c981d4555854d05fea54d1698d09416ec38d076d53b73a601650007d3961da`.

## Consequences

The probe narrows the conflict but does not define a decoder rule. A future
production change requires additional format-family or original-runtime
evidence that explains both exact-width and width-plus-one rows without a
general crop operation.

