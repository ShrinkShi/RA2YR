# ADR 0024: IsoMapPack5 remains a raw-record and policy boundary

## Status

Accepted for M3-C2 synthetic/configured implementation.

## Decision

M3-C2 composes the existing lossless packed-fragment, strict Base64, chunk
envelope, and injected `RawLzo1X` backend stages with an 11-byte IsoMapPack5
record reader. The reader preserves every record byte, absolute source offset,
source order, and provenance. The 32-bit tile view and split 16-bit views are
both retained; no tile interpretation is selected.

Decoded stream remainders are controlled by an explicit trailing policy:
reject all remainders, preserve them with a diagnostic, or accept only an exact
four-byte all-zero trailer. Chunk `0/0` sentinel handling is a separate layer.

Coordinate indexing is a separate analysis stage. It preserves all duplicate
occurrences and reports equivalent or conflicting payloads. It never chooses a
winner, swaps axes, synthesizes missing cells, or turns sparse input into a
dense map. Domain and signedness interpretations are explicit profiles only.

## Consequences

The packed adapter cannot infer a codec from a section name and cannot continue
to record parsing after an upstream stage fails. LZO remains an injected
contract only: no algorithm, native library, P/Invoke, NuGet package, or GPL
implementation is included. OverlayPack, PreviewPack, TMP, palette, renderer,
writer, ProjectBaseline packed audit, and original-runtime claims remain out of
scope.

## Evidence boundary

The M3-C2 evidence is synthetic and structural. It may establish parser,
diagnostic, budget, input-equivalence, and policy behavior, but it does not
confirm stock YR coordinate semantics, tile meaning, trailer meaning, or packed
ProjectBaseline compatibility.
