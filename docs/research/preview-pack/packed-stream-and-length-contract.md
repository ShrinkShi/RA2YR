# PreviewPack fragments, chunk envelope, LZO, and decoded-length contract

> Source notice: researched by **ChatGPT Web** from public sources; no local `ProjectBaseline`; not a Codex Agent artifact; no source implementation copied, translated, mechanically rewritten, or imported (`code_imported: false`).

## 1. Packed pipeline

```text
PreviewPack physical entries
→ explicit fragment ordering
→ one Base64 character stream
→ strict Base64 bytes
→ repeated chunk headers and payload windows
→ bounded raw LZO1X-compatible backend
→ aggregate decoded byte stream
→ exact pixel-length validation
```

Each stage returns its own result and diagnostics. No downstream success erases an upstream anomaly.

## 2. Numbered fragments

Common writers use decimal keys beginning at `1`, with values wrapped at 70 Base64 characters. EA's editor and WAE both produce 70-character fragments; WAE explicitly increments from key `1`. CNCMaps does the same. This is a strong writer convention, not sufficient proof that the runtime numerically sorts arbitrary keys.

The collector retains:

- section occurrence;
- raw key spelling;
- parsed integer candidate;
- source occurrence order;
- normalized numeric key;
- value text before and after allowed trimming;
- empty values;
- duplicate normalized groups;
- missing-number ranges;
- nonnumeric entries;
- comments and line provenance;
- original Base64 wrapping.

### Strict configured policy

- canonical unsigned decimal keys only;
- default start key `1`;
- key `0` diagnosed and rejected unless a profile explicitly allows it;
- leading zeroes diagnosed;
- `1` and `01` form a duplicate normalized group;
- gaps diagnosed rather than silently terminating;
- nonnumeric keys are not treated as pixels;
- values are joined once, then decoded once;
- dictionary enumeration cannot define ordering;
- ordinary semantic INI key override cannot delete a physical fragment.

Original runtime handling of gaps, duplicates, key zero, and physical versus numeric order remains `Unresolved`.

## 3. Base64 contract

Strict Base64 processing requires:

- maximum fragment count;
- maximum characters per fragment;
- maximum aggregate characters;
- explicit ASCII-whitespace policy;
- no hidden removal of punctuation or comments from value content;
- valid alphabet;
- valid padding only at the end of the aggregate stream;
- no decoding of fragments independently;
- no partial output on invalid input;
- exact source-span diagnostics.

Original wrapping is preserved separately from canonical decoded bytes.

## 4. Chunk envelope candidate

The public implementations use repeated little-endian blocks:

```text
u16le compressedSize
u16le uncompressedSize
byte[compressedSize] compressedPayload
```

This is shared with the map packed-section family but the Preview pipeline owns a distinct logical document and output budget.

### Confirmed source behavior

- WAE writes this envelope and splits output into blocks of at most 8192 bytes.
- CnCNet reads the two 16-bit sizes, creates a bounded payload stream, and advances by the declared compressed length.
- EA's editor calls its packed-section encoder for the full raw preview and therefore supplies official-editor evidence for the same family, while the low-level library remains XCC-derived/reference-only.
- CNCMaps and MapTool route PreviewPack through their generic map-pack helpers.

## 5. The 8192 question

WAE uses `8192` as maximum uncompressed bytes per emitted block. This is `ConfirmedByIndependentImplementation` as a writer convention.

It is not established as:

- a runtime hard maximum;
- a bitstream limit of LZO1X;
- a required final-block size;
- a universal EA editor choice.

Core therefore has a configurable per-block output limit. A profile may set 8192 for compatibility testing, but the value is not baked into the codec backend.

## 6. Zero-size blocks

CnCNet stops when either size is zero. That means it treats `0/positive`, `positive/0`, and `0/0` similarly. This is a lenient consumer behavior, not a proven format rule.

The strict project policy distinguishes:

- `0/0`: terminal-sentinel candidate, only accepted by an explicit profile;
- `0/nonzero`: malformed;
- `nonzero/0`: malformed;
- input exhaustion exactly after the final payload: valid termination candidate;
- partial header: truncated input.

No one-zero block is silently treated as success by default.

## 7. Exact block contract

For every block:

- four header bytes must be available;
- `compressedSize` must fit the remaining input window;
- `uncompressedSize` must fit the per-block and aggregate output budgets;
- the backend receives exactly `compressedSize` bytes;
- the backend must report exact input consumption if its API exposes it;
- the backend must produce exactly `uncompressedSize` bytes;
- short output, extra output, codec error, or no progress is failure;
- a failed block does not contribute partial aggregate output;
- arithmetic is checked.

## 8. Aggregate termination

The envelope reader stops only according to the selected explicit profile:

- exact input exhaustion;
- aggregate output reaching the validated expected pixel length **and** exact input exhaustion;
- accepted `0/0` sentinel with no forbidden trailing bytes.

Reaching `width×height×3` does not authorize ignoring additional chunks or bytes.

## 9. Decoded pixel-length contract

Strong candidate:

```text
ExpectedDecodedLength = Width × Height × 3
```

Evidence:

- EA's editor allocates and encodes exactly `width × height × 3` bytes;
- WAE allocates `texture pixel count × 3`;
- CnCNet allocates that exact destination and its image builder requires equality;
- CNCMaps allocates that exact size;
- MapTool allocates that exact size;
- ModEnc says each scanline is exactly `3 × width` with no padding.

The standard strict profile requires:

```text
ActualDecodedLength == ExpectedDecodedLength
```

No fixed decoded trailer was found for PreviewPack. IsoMap's debated four-byte remainder is not imported into the preview format.

## 10. Length result model

Record separately:

- `MetadataDimensionsCandidate`;
- `ExpectedDecodedLength`;
- `ActualDecodedLength`;
- `MissingByteCount`;
- `TrailingByteCount`;
- bounded retained trailing-byte provenance or digest policy;
- `LengthStatus`;
- evidence grade;
- block count and size aggregates;
- exact compressed input consumption.

Statuses include:

- `Exact`;
- `Underflow`;
- `Overflow`;
- `MetadataInvalid`;
- `BudgetExceeded`;
- `ArithmeticOverflow`;
- `CodecFailure`;
- `EnvelopeFailure`;
- `TrailingCompressedInput`.

## 11. Lenient public behavior

CnCNet preallocates the full destination and can return it after input exhaustion without verifying that every byte was written; unwritten bytes remain zero. This is useful consumer robustness but cannot become production Core semantics.

Any public implementation that pads, ignores a short read, truncates, or accepts trailing input is recorded as lenient tool behavior.

## 12. Pixel rows versus blocks

LZO block boundaries do not define scanlines. A block can end:

- in the middle of a pixel;
- in the middle of a row;
- exactly at a row boundary;
- after multiple rows.

Only the aggregate decoded stream is interpreted as pixels.

## 13. Input modes

Memory, seekable Stream, short-read Stream, and bounded MIX window use the same envelope state machine and must produce identical:

- bytes;
- diagnostics;
- consumed lengths;
- aggregate hashes;
- failure categories.

No input-driven unbounded allocation or no-progress loop is permitted.