# Packed map compression foundation

This document describes the M3-C1 Core boundary. It is not a claim that the
repository can load a RA2/YR map or that any profile is confirmed by the
original runtime.

## Stage contract

```text
lossless INI occurrences
  -> numbered fragment collection
  -> strict standard Base64
  -> u16le compressed/uncompressed chunk envelope
  -> explicitly selected Format80 or injected LZO backend
  -> exact aggregate decoded byte stream
  -> future map-specific reader
```

Fragment collection retains duplicate occurrences, raw keys and values,
physical order, line/source identity and INI provenance.  Ordering is explicit:
source order, numeric ascending unique, or strict sequential from one.

Strict Base64 rejects whitespace, URL-safe characters, invalid padding, invalid
lengths, and output over budget before invoking the .NET primitive.

The chunk reader is codec-neutral.  A zero field is not a terminator unless an
explicit sentinel policy allows `0/0`; one-zero fields remain unresolved.

The current contract is fail-closed for both one-zero forms: `compressed=0,
output>0` and `compressed>0, output=0` are invalid chunk fields and are never
treated as normal blocks, padding, or implicit terminators.  `0/0` is accepted
only by the explicit `AllowZeroZeroAsTerminator` policy; trailing bytes after
that sentinel are diagnosed.

Format80 supports the five documented command classes with explicit absolute or
relative medium/long reference profiles, exact expected output, required
terminator policy, bounded overlap expansion, and structured failures.  It does
not guess a variant or clamp output.

All chunk results retain the supplied provenance chain. Window, Stream and
materialized input paths apply the same maximum-input budget before allocation;
the production core never reads an unbounded stream into memory first.

The M3-C1 layer still exposes an injectable LZO contract, and M3-C4 supplies
one independently authored managed `RawLzo1X` backend. Requests remain bounded
by compressed-input and output budgets, carry exact expected output,
cancellation, backend identity, and provenance. The pipeline requires exact
consumed input, exact output length, non-empty identity, matching provenance,
and no error diagnostics. Backend exceptions, cancellation, null results/bytes,
and unavailable backends become structured failures. No miniLZO/GPL source,
native plugin, P/Invoke binding, NuGet LZO dependency, or writer is included.

## Compatibility boundary

- fragment collector: synthetic/configured policy;
- strict Base64: implemented and synthetic tested;
- chunk envelope: implemented and synthetic tested;
- Format80: implemented only for explicit synthetic profiles;
- managed RawLzo1X decode backend: implemented and synthetic tested;
- external LZO oracle comparison: independent validation only;
- ProjectBaseline IsoMapPack5 audit: executed against an external patched
  development source and published only as a sanitized aggregate; failures are
  retained as `CompleteWithFailures` where observed;
- IsoMap tile/coordinate runtime meaning, Overlay, TMP, palette, rendering and
  original runtime: not implemented or not confirmed. PreviewPack has only a
  separate M3-C5 raw component foundation with explicit profiles.

No ProjectBaseline packed payload, decoded bytes, image, coordinate, or map
record is published or used by this work package.

The M3-C1 synthetic behavioral matrix contains 109 independent execution cases
across fragment policies, strict Base64, chunk envelopes, Format80 profiles,
bounded input equivalence, and LZO backend/pipeline contracts. M3-C4 adds 23
managed decoder cases and 2 sanitized-audit service cases; the focused M3-C4
current-head XML therefore executes 25 cases. These counts are not inflated
with equivalent Base64 spellings.
