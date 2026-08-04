# Packed-section envelope

## 1. Shared textual envelope

`IsoMapPack5`, `OverlayPack`, `OverlayDataPack` and `PreviewPack` are stored as numbered INI values containing Base64 fragments.

A shared envelope reader may perform only:

1. occurrence collection;
2. numeric-key validation and deterministic ordering;
3. bounded concatenation;
4. strict Base64 decoding;
5. source-range/provenance mapping;
6. dispatch to the section-specific compression decoder.

It must not decide map-cell semantics.

## 2. Fragment ordering

Writers commonly emit decimal keys beginning with `1` and lines of at most about 70 characters. These are writer conventions, not sufficient reasons to ignore other source occurrences.

Required classifications:

- unique positive decimal keys;
- zero key;
- negative or signed text;
- leading-zero variants;
- duplicate normalized key;
- nonnumeric key;
- missing/gapped sequence;
- empty fragment;
- invalid Base64 character/padding.

Gaps are not automatically fatal if the ordered fragments still form valid Base64. Duplicate normalized keys are ambiguous and cannot be resolved by last-write-wins without an explicit profile.

## 3. Chunked LZO envelope

`IsoMapPack5` and `PreviewPack` use a repeating candidate block structure:

```text
u16le compressedSize
u16le uncompressedSize
byte[compressedSize] compressedPayload
```

The next block begins immediately after the payload.

Checks per block:

- at least four bytes remain;
- compressed and output sizes fit configured limits;
- payload fits remaining input;
- cumulative output uses checked arithmetic;
- decompressor reports exact/acceptable consumption under an explicit contract;
- output size equals the declared size;
- block count is bounded;
- no zero-progress loop.

Some preview consumers stop if either size is zero; some writers simply end at input exhaustion. Zero-size termination therefore remains section/profile-specific and must be diagnosed rather than silently generalized.

## 4. Format80/LCW envelope

`OverlayPack` and `OverlayDataPack` are commonly represented as Format80/LCW-compressed arrays. Public tools differ in whether they expose one stream or helper-level subblocks.

The section-specific decoder must receive:

- exact compressed bytes;
- exact expected output size from the selected map format/profile;
- an output budget;
- command/read budget;
- strict trailing-input policy;
- structured diagnostics.

It must not allocate a fixed output merely because a permissive tool does so before validating the family.

## 5. Output ownership

Decoded buffers are immutable section results:

```text
PackedSectionResult
- SectionKind
- OrderedFragmentOccurrences
- Base64Status
- CompressionKind
- CompressedLength
- DeclaredOutputTotal?
- ActualOutputLength
- ConsumedInput
- RawOutputHash
- Diagnostics
```

The raw output is not exposed publicly by the audit serializer.

## 6. Error locality

Diagnostics should distinguish:

- INI fragment ambiguity;
- Base64 syntax failure;
- block-header truncation;
- compressed payload truncation;
- decompressor failure;
- declared-size mismatch;
- output budget breach;
- trailing compressed bytes;
- section-specific decoded-size mismatch.

Do not collapse these into `InvalidMap`.

## 7. Decompression safety

All compression helpers require:

- bounded source windows;
- bounded destination windows;
- checked cumulative counters;
- maximum block and command counts;
- no pointer arithmetic outside the window;
- cancellation/timeout boundary at the caller where appropriate;
- deterministic diagnostics independent of input transport.

No decoder may treat a following INI section, file tail or adjacent MIX entry as extra compressed input.

## 8. Writer boundary

A later writer should take a validated semantic buffer and independently:

- split it into configured block sizes;
- compress each block;
- emit little-endian lengths;
- Base64-encode;
- split into numbered lines.

The writer must not call production decode helpers to compute expected fields in tests. Independent fixture builders and oracles are required.
