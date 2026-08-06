> **Source notice:** This research was prepared by **ChatGPT Web** from public sources. It did not read the local ProjectBaseline and is not the product of the local Codex Agent. No GPL or unclear-license code was copied, translated, mechanically rewritten, or imported (`code_imported: false`).

# Decoded length, terminal padding, and trailer contracts

## Two independent termination questions

These layers must never be conflated:

1. **chunk-envelope termination** — whether compressed blocks end by input exhaustion, expected output length, or a `0/0` block;
2. **decoded IsoMap stream trailer** — whether bytes after the last complete 11-byte record have a defined structure.

A four-byte decoded suffix is not a chunk header merely because it is four bytes long. Conversely, a compressed `0/0` header is not an IsoMap record trailer.

## Public behavior conflict

### Four-byte trailer writers and consumers

- WAE writes all selected 11-byte records, then appends four zero bytes before chunking and LZO compression.
- CNCMaps allocates and writes `recordCount × 11 + 4`, describing the four bytes as padding/termination.
- MapTool uses the same target-size convention.
- OpenRA allocates `(2W-1) × H × 11 + 4` and comments that the final four bytes represent no more LZO data.

These implementations support a strong community/tool convention but do not establish a universal stock decoded-stream structure.

### Official editor behavior

The official FinalSun/FinalAlert 2 editor:

- sums chunk-declared output sizes;
- decodes into that exact total size;
- computes record count using integer division by 11;
- its writer sends `recordCount × 11` source bytes to the IsoMap encoder, with no explicit four-byte append in the reviewed save path.

This is a direct conflict with an unconditional `+4 zero trailer` rule. It also reveals permissive remainder handling in the official editor reader, which must not become the project's strict default.

## Length classification

Given decoded length `L`:

```text
FullRecordCount = L / 11
RemainderLength = L % 11
```

A strict classifier should produce one of:

- `ExactRecordsOnly` — remainder 0;
- `FourByteTrailerCandidate` — remainder 4;
- `ArbitraryTrailingBytes` — remainder 1..10 except 4;
- `EmptyDecodedStream` — length 0;
- `BudgetExceeded`;
- `LengthArithmeticOverflow`.

A four-byte remainder is only a structural candidate, not automatically padding.

## Trailer model

```text
IsoMapDecodedStreamTrailer
  ByteOffset
  Length
  RawBytes
  IsAllZero
  CandidateKinds[]
  EvidenceGrade
  Diagnostics
```

Candidate kinds:

- `ZeroPaddingCandidate`
- `TerminationMarkerCandidate`
- `WriterArtifactCandidate`
- `RecordCountCandidate`
- `ChecksumCandidate`
- `UnknownTrailer`

No reviewed source established record-count or checksum semantics. They remain explicit unresolved candidates rather than implied possibilities in production behavior.

## Strict success profiles

### Forensic profile

- parse every complete 11-byte record;
- retain any remainder as raw trailer;
- report its classification;
- do not call the document fully semantically valid unless the trailer policy accepts it.

### Strict project profile

Recommended initial policy:

- accept exact `N × 11` as structurally complete;
- accept `N × 11 + 4` only under an explicit profile that retains the four bytes and optionally requires all zero;
- reject other remainders as a complete semantic document;
- return partial raw records and trailer diagnostics only in a clearly marked forensic result, never as normal success.

### Compatibility profiles

Named profiles may emulate:

- official-editor integer-division tolerance;
- WAE four-zero writer convention;
- OpenRA/CNCMaps fixed dense allocation.

Compatibility behavior must remain opt-in and provenance-labeled.

## Forbidden repairs

- dropping the last 1–10 bytes to obtain divisibility;
- unconditionally deleting four bytes;
- appending zeros until divisible by 11;
- treating nonzero four-byte tails as zero padding;
- consuming a trailer as a record prefix;
- hiding trailer bytes behind a successful record count;
- returning partial records as complete success.

## Relationship to sparse streams

A sparse stream can still be exactly divisible by 11. Dense/sparse classification depends on coordinates and dimensions, not decoded length alone.

Likewise, a dense stream can have a four-byte trailer. The expected dense payload candidate is:

```text
((2W - 1) × H × 11) + TrailerLength
```

All multiplication is checked before comparison.

## Tail inside the final LZO block

WAE appends its four zeros before calling the block generator, so those bytes are part of decoded output and may reside in the final LZO block. This does not make them part of the LZO bitstream grammar or chunk envelope.

The envelope reader should report block output spans. The IsoMap classifier can then determine whether a trailer lies within the last block without assigning semantics based on block location.

## Empty sections

Distinct states:

- missing `[IsoMapPack5]` section;
- present section with no fragments;
- Base64 decodes to zero bytes;
- chunk envelope yields zero decoded bytes;
- decoded stream contains only a four-byte candidate trailer;
- decoded stream contains zero records plus arbitrary bytes.

No state should be silently converted into a full default map by the record reader.

## Roundtrip

Byte-identical preservation may require retaining:

- decoded trailer bytes;
- chunk boundaries and declared sizes;
- original compressed bytes or a separately authorized re-encode policy;
- Base64 fragment grouping and source order.

A semantically equivalent record rewrite is not automatically byte-identical, FinalAlert-stable, or stock-runtime equivalent.

## Audit requirements

Future aggregate audit should report:

- decoded length;
- quotient and remainder by 11;
- trailer length classification;
- all-zero/nonzero classification;
- whether trailer crosses a block output boundary;
- counts by map selection group;
- aggregate hashes only, with no trailer bytes published.
