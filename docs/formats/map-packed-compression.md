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

Format80 supports the five documented command classes with explicit absolute or
relative medium/long reference profiles, exact expected output, required
terminator policy, bounded overlap expansion, and structured failures.  It does
not guess a variant or clamp output.

All chunk results retain the supplied provenance chain. Window, Stream and
materialized input paths apply the same maximum-input budget before allocation;
the production core never reads an unbounded stream into memory first.

LZO has a contract only.  Without a backend the pipeline returns
`BackendUnavailable` and never produces placeholder bytes.

## Compatibility boundary

- fragment collector: synthetic/configured policy;
- strict Base64: implemented and synthetic tested;
- chunk envelope: implemented and synthetic tested;
- Format80: implemented only for explicit synthetic profiles;
- LZO, IsoMap, Overlay, Preview, TMP, palette, rendering and original runtime:
  not implemented or not confirmed.

No ProjectBaseline packed payload, decoded bytes, image, coordinate, or map
record is published or used by this work package.
