# ADR 0011: PAL raw model and explicit display conversion

- Status: Accepted
- Date: 2026-08-03

## Context

Westwood PAL data stores indexed RGB channels in a six-bit range. The raw
layout is well supported by XCC, independent implementations, and the three
controlled ProjectBaseline samples, but observable display expansion differs:
XCC scales to 255 with integer floor, OpenRA replicates high bits, and other
documentation uses a two-bit left shift. Nearest rounding is another sensible
mathematical mapping but is not the observed XCC behavior.

Choosing one silently would convert a format-parser result into an unverified
visual-compatibility claim. Mutating the raw values during conversion would
also prevent later original comparison.

## Decision

- The authoritative palette model stores exactly 256 immutable raw RGB
  entries, each in the range 0 through 63 and in original index order.
- Parsing requires exactly 768 bytes and full consumption. Invalid channels,
  truncation, trailing data, read failures, and budget failures reject the
  complete result.
- Display conversion is separate and explicitly named. WP-02D retains
  `ShiftLeftTwo`, `ReplicateHighBits`, `ScaleToFullRangeRounded`, and
  `XccScaleToFullRangeFloor`.
- There is no unlabeled or `OriginalYR` default until a controlled original
  visual comparison supports one.
- The ProjectBaseline audit identifies `XccScaleToFullRangeFloor` only as its
  XCC reference strategy. It does not render or rewrite PAL data.
- Model fingerprints include a schema tag, explicit indices, and raw RGB only.
  Complete colors remain in memory and are not published.

## Consequences

Format parsing and local golden comparison can advance independently of
rendering. Callers must deliberately select a display strategy, preventing an
accidental compatibility claim. Future rendering evidence may select or add a
compatibility policy without changing stored raw values or invalidating the
raw-model fingerprint.

WP-02D does not provide PAL writing, Texture2D creation, shaders, player-color
remapping, theater selection, or original visual comparison.

## Alternatives

- Always use `value << 2`: rejected as an unlabeled default because XCC and
  OpenRA observably use different mappings.
- Always scale to 0-255: rejected because floor, nearest rounding, and bit
  replication differ for valid inputs.
- Store only converted bytes: rejected because it destroys the authoritative
  raw values and entangles parsing with a disputed display policy.
- Accept channels above 63 by masking or clamping: rejected because XCC and the
  controlled samples support strict range validation, while corrupt original
  behavior remains unproven.
