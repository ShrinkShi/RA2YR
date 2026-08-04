# M3-R2 — Westwood map compression codecs and envelopes

> Source notice: prepared by ChatGPT Web from pinned public sources and repository context. No local ProjectBaseline, original map payload, compressed byte stream, decoded map data, or Unity project was read. GPL and unclear-license implementations were used only as behavior-level references.

## Purpose

This dossier narrows the compression questions left open by the MAP/TMP research. It separates:

1. lossless INI collection;
2. numbered packed-fragment normalization;
3. strict Base64 decoding;
4. the RA2/YR map chunk envelope;
5. the payload codec (`Format80/LCW` or raw `LZO1X`);
6. exact output validation;
7. map-specific record parsing.

It is research and implementation design only. It does not implement a decoder and does not promote compatibility status.

## Frozen repository basis

- Repository: `ShrinkShi/RA2YR`
- Base branch: `main`
- Base commit: `68db56de1d74c970cd7de95ac8cb9dd65cbb4e1e`
- Research branch: `research/m3-westwood-map-compression-dossier`

## High-confidence findings

- `Format80` and `LCW` commonly name the same five-command byte stream, but the name alone does not select absolute versus relative position semantics, forward versus reverse API conventions, stream envelope, or strictness policy.
- RA2/YR `OverlayPack` and `OverlayDataPack` use a repeated four-byte chunk header around independent Format80 payloads.
- `IsoMapPack5` and `PreviewPack` use the same candidate chunk header around raw LZO1X-compatible payloads.
- The candidate chunk header is `{u16le compressedSize, u16le uncompressedSize}` followed immediately by `compressedSize` bytes.
- Public writers commonly limit uncompressed chunks to 8192 bytes and terminate by input exhaustion. This is strong writer convention, not yet proven as an original runtime hard limit.
- WAE and OpenRA use miniLZO-derived LZO1X code. That proves LZO1X-compatible decoding, not which original compressor level produced stock maps.
- Packed INI fragments must be concatenated before Base64 decode; common 70-character fragments are not independently Base64-decodable.
- A strict production codec must reject partial output, implicit zero padding, silent clipping, invalid references, ignored backend errors, and unreported trailing input.

## Documents

1. [family-boundaries.md](family-boundaries.md)
2. [format80-lcw-command-model.md](format80-lcw-command-model.md)
3. [format80-lcw-variants.md](format80-lcw-variants.md)
4. [format80-termination-and-length.md](format80-termination-and-length.md)
5. [lzo-family-identification.md](lzo-family-identification.md)
6. [chunked-lzo-envelope.md](chunked-lzo-envelope.md)
7. [base64-fragment-envelope.md](base64-fragment-envelope.md)
8. [source-comparison.md](source-comparison.md)
9. [implementation-boundaries.md](implementation-boundaries.md)
10. [legal-and-licensing-boundaries.md](legal-and-licensing-boundaries.md)
11. [test-matrix.md](test-matrix.md)
12. [baseline-audit-request.md](baseline-audit-request.md)
13. [unresolved-questions.md](unresolved-questions.md)

## Evidence labels

- `ConfirmedByOfficialToolSource`
- `ConfirmedByMultipleImplementations`
- `ConfirmedCommunityDescription`
- `ConfiguredProjectPolicy`
- `ImplementationSpecificBehavior`
- `ConflictingSources`
- `Underconfirmed`
- `Unresolved`

Shared code ancestry is not independent confirmation.

## Non-goals

This dossier does not:

- implement C# or native code;
- define Format40, SHP RLE, MIX compression, XOR/delta, Deflate, or every Westwood compression format;
- run Unity, RA2/YR, FinalAlert, XCC, or any original executable;
- read ProjectBaseline;
- publish compressed bytes, decoded arrays, coordinates, previews, or map records;
- copy, translate, or mechanically port GPL decoder code;
- select a production LZO dependency without a later dependency/security review;
- modify compatibility metadata.
