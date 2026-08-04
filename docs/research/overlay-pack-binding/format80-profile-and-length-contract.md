# Overlay Format80 profile and length contract

> **Research provenance:** This document was produced by ChatGPT Web from public sources. It did not read ProjectBaseline, is not a local Codex Agent artifact, and imports no code. GPL or unclear-license sources are reference-only; no source was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

## 1. Dependency on M3-R2

This dossier does not redefine Format80/LCW. It consumes the conflict model already documented in `docs/research/map-compression/` and adds an Overlay-specific caller profile.

The compression layers remain:

```text
numbered fragments
→ strict Base64
→ repeated map chunk envelope
→ bounded Format80 payload windows
→ exact decoded Overlay array
```

## 2. Explicit profile requirement

The caller supplies an `OverlayFormat80Profile`. The decoder must never try several variants and choose whichever produces 262144 bytes or many `0xFF` values.

Candidate profile fields:

```text
OverlayFormat80Profile
- ProfileId
- CommandModel
- MediumLongReferenceMode
- ShortReferenceMode
- InitialMarkerPolicy
- TerminatorPolicy
- ExactCompressedWindowPolicy
- ExactDecodedLengthPolicy
- OverlapCopyPolicy
- ChunkEnvelopeProfile
- StorageProfile
- EvidenceGrade
- SourcePins
```

## 3. Current project candidate

Consistent with M3-R2, the proposed experimental Overlay profile is:

- forward output;
- short back-references interpreted relative to current output;
- medium/long references interpreted as absolute output positions;
- standard command families including literal, short copy, medium/long copy, and fill;
- required `0x80` terminator within each bounded payload;
- command-defined overlap copies allowed where the format permits;
- exact input-window consumption;
- exact declared output production;
- no partial success.

Evidence grade: `ConfiguredForProjectPolicy` until local stock-map audits and independent evidence resolve remaining variant questions.

## 4. Outer map chunk envelope

The map compression research identifies a repeated chunk candidate with little-endian compressed and uncompressed sizes. Overlay sections use this outer envelope in major implementations.

The chunk reader owns:

- header availability;
- compressed payload window;
- declared output size;
- block count budget;
- aggregate output budget;
- zero-size/sentinel policy;
- exact payload consumption;
- trailing chunk bytes.

The Format80 decoder receives one bounded payload and one bounded destination window. It does not read the next chunk header.

## 5. Length contracts

For each chunk:

```text
actual compressed consumption == declared compressed size
actual produced output == declared uncompressed size
```

For the aggregate ordinary array:

```text
sum(chunk outputs) == 262144
```

For WAE's explicit extended type profile:

```text
OverlayPack aggregate output == 524288
OverlayDataPack aggregate output == 262144
```

The array layer validates aggregate storage length after codec success.

## 6. Terminator and trailing input

A decoder result must distinguish:

- terminator reached exactly at payload end;
- terminator reached with trailing bytes;
- declared output completed before terminator;
- input ended before terminator;
- command requires unavailable input;
- command would exceed output window.

The project default rejects terminator-following payload bytes and output completion without the selected terminator contract. Lenient tool behavior is recorded as implementation-specific.

## 7. Absolute versus backward-distance references

The medium/long offset conflict is preserved as an explicit profile dimension:

- absolute candidate: source index is measured from decoded-output start;
- relative candidate: field is a backward distance from current output.

Short references remain a separate command class. A stream is not classified by trying both interpretations.

## 8. Marker-selected variants

Community descriptions mention a possible initial marker for relative streams. An initial byte can also be a valid ordinary command under another profile.

Marker handling is therefore:

- allowed only at byte zero;
- enabled only by an explicit profile;
- recorded in the decode trace;
- never inferred from output plausibility.

The ordinary Overlay candidate currently does not auto-consume such a marker.

## 9. Overlap copies

Back-references may intentionally overlap their destination, allowing repeated patterns. The decoder must implement the selected format's bytewise semantics while validating that the initial source position is already produced and remains within the output window.

It must not replace overlap behavior with a bulk copy whose semantics differ.

## 10. Safety contract

Every command must either:

- advance input and/or output according to a valid command; or
- return a structured failure.

Required protections:

- bounded input and output windows;
- checked arithmetic;
- command-count budget;
- chunk-count budget;
- aggregate-output budget;
- back-reference validation;
- literal and fill length validation;
- no-progress detection;
- no silent clamp;
- no implicit zero fill;
- no ignored trailing input;
- no successful partial output.

## 11. Source conflict notes

- EA's editor integrates XCC-derived packing code and is official editor evidence, not independent runtime code.
- OpenRA and several community tools share XCC/OpenRA lineage.
- WAE's inspected map path uses an absolute Format80 implementation.
- ModEnc names the compression Format80/LCW but does not resolve every command-profile conflict.

Agreement among shared descendants does not become multiple independent runtime proof.

## 12. Diagnostics

Recommended diagnostics include:

- `OverlayChunkHeaderTruncated`;
- `OverlayChunkPayloadTruncated`;
- `OverlayChunkZeroSizeAmbiguous`;
- `OverlayFormat80ProfileMissing`;
- `OverlayFormat80MarkerUnexpected`;
- `OverlayFormat80CommandTruncated`;
- `OverlayFormat80BackReferenceOutOfRange`;
- `OverlayFormat80OutputOverflow`;
- `OverlayFormat80OutputUnderflow`;
- `OverlayFormat80TerminatorMissing`;
- `OverlayFormat80TrailingCompressedBytes`;
- `OverlayAggregateLengthMismatch`;
- `OverlayCodecBudgetExceeded`;
- `OverlayCodecNoProgress`.

## 13. Roundtrip boundary

Exact decode does not imply exact recompression. Byte-identical compressed roundtrip requires retaining original compressed bytes, chunk boundaries, and fragment layout. A future encoder may produce semantically equivalent arrays with different bytes and must label the result accordingly.