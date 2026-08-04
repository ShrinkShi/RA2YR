# Format80 / LCW command model

## 1. Candidate canonical command table

The table below is the convergent community/OpenRA/XCC-lineage model. All multi-byte integers are little-endian.

| Class | First-byte condition | Additional bytes | Output |
|---|---|---|---|
| Short relative copy | bit 7 = 0 | one low-distance byte | copy 3..10 bytes from a 12-bit backward distance |
| Literal | bits `10` | literal payload | copy `code & 0x3f` bytes; zero count (`0x80`) is terminator |
| Medium copy | bits `11`, low 6 bits `< 0x3e` | u16 position/distance | copy `(low6 + 3)` bytes |
| Fill | `0xfe` | u16 count, u8 value | repeat one byte `count` times |
| Long copy | `0xff` | u16 count, u16 position/distance | copy `count` bytes |

This is a format-fact model, not implementation pseudocode.

## 2. Short relative copy

For first byte `0cccpppp`:

- length candidate: `ccc + 3`, range 3..10;
- distance candidate: `(pppp << 8) | nextByte`, range 0..4095;
- source position: `outputPosition - distance`.

Strict policy:

- distance zero is invalid because it points at the current unwritten byte;
- distance must not exceed bytes already produced;
- checked arithmetic is required;
- overlapping source and destination is legal when the source begins in produced output.

## 3. Literal copy

For first byte `10cccccc`:

- count 1..63 copies that many following bytes;
- count zero (`0x80`) is the canonical terminator candidate;
- the literal payload must fit entirely in the declared compressed window;
- output must fit the remaining declared output budget.

The terminator consumes only its command byte. Any remaining bytes in the block are trailing input and must be reported.

## 4. Medium copy

For `11cccccc` where `cccccc < 0x3e`:

- length is `cccccc + 3`, range 3..64;
- next u16 is interpreted according to the selected variant:
  - absolute offset from output start; or
  - backward distance from current output position.

Absolute mode permits field value zero only after output byte zero exists and only when the referenced range starts strictly before the current output position. Relative mode rejects distance zero.

## 5. Fill

For command `0xfe`:

- next u16 is output count;
- next byte is the repeated value;
- zero count is structurally representable but its legality is underconfirmed.

Project defensive policy rejects zero-length fill because it consumes input without producing output and has no known map need. A compatibility profile may evaluate it only as an explicit experiment.

## 6. Long copy

For command `0xff`:

- next u16 is output count;
- following u16 is absolute position or backward distance by variant;
- zero count is structurally representable but rejected by the strict map profile;
- source must begin in already produced output;
- overlap is expanded bytewise in forward output order.

## 7. Overlapping back-references

Overlap is essential for repeated patterns. A correct decoder cannot implement every copy using an ordinary non-overlap bulk copy. The semantic rule is:

- each produced byte becomes immediately available to later bytes of the same command;
- source cursor advances with destination cursor;
- all reads still originate below the destination position at the time they occur.

## 8. Maximum command outputs

- literal: 63;
- short relative: 10;
- medium: 64;
- fill: 65535 candidate;
- long copy: 65535 candidate.

These maxima do not authorize allocation. The caller’s output window and `Format80ReadLimits.MaxOutputBytes` are authoritative.

## 9. Progress invariant

Each loop iteration must either:

- consume a complete valid command and produce positive output;
- consume the canonical terminator and finish;
- return a structured failure.

No malformed or zero-progress command may cause retry, resynchronization, or an infinite loop.

## 10. Result model

A successful result records:

- variant;
- bytes consumed including terminator;
- bytes produced;
- command count;
- command-kind counts;
- maximum reference distance;
- overlap-copy count;
- terminator seen;
- trailing byte count.

It does not expose decoded bytes in public audit output.
