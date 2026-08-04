# RA2/YR map chunk envelope

## 1. Candidate structure

For each block:

```text
u16le compressedSize
u16le uncompressedSize
byte payload[compressedSize]
```

Blocks are adjacent with no demonstrated inter-block padding.

The same four-byte envelope is used with:

- raw LZO1X payloads for `IsoMapPack5` and `PreviewPack`;
- Format80 payloads for `OverlayPack` and `OverlayDataPack`.

The envelope reader is codec-neutral.

## 2. End conditions in public tools

Observed strategies include:

- stop when decoded destination reaches caller-expected output;
- stop when source bytes are exhausted;
- stop if either size field is zero;
- pre-scan all headers and sum output sizes.

These are implementation behaviors, not one confirmed original contract.

## 3. Zero-size fields

Current evidence:

- WAE writers do not emit a terminal zero-size chunk; they end at input exhaustion.
- OpenRA comments refer to four final decoded IsoMap bytes as a no-more-data header, but those bytes are inside the decompressed IsoMap payload, not necessarily a chunk-envelope header.
- WAE/CnCNet readers break when either header size is zero.

Strict project policy:

| Header | Default |
|---|---|
| `0/0` | reject as underconfirmed unless explicit profile permits a final sentinel |
| `0/nonzero` | reject |
| `nonzero/0` | reject |
| nonzero/nonzero | validate and decode |

A future `AllowFinalZeroZeroChunk` profile must require that it is final and has no trailing bytes.

## 4. Chunk size convention

Modern writers commonly split uncompressed data into blocks of at most 8192 bytes. This is strong writer convention and useful defensive default.

Proposed fields:

- `MaxDeclaredBlockOutput = 8192` for the vanilla-map candidate profile;
- a separate hard safety limit that cannot be raised by file data;
- final block may be shorter;
- a non-final short block is accepted structurally unless original semantics later require canonical chunking.

Decoder correctness must not depend on canonical writer chunk boundaries.

## 5. Exact block validation

For every block:

1. four header bytes fit;
2. compressed payload fits;
3. declared output fits policy and aggregate budget;
4. backend receives only that payload window;
5. backend reports exact compressed input consumed;
6. backend produces exactly declared output;
7. diagnostics are recorded with block ordinal;
8. aggregate offsets advance with checked arithmetic.

## 6. Aggregate completion

The envelope reader receives an optional expected aggregate output.

Success requires:

- all source bytes consumed, unless an explicit final sentinel profile applies;
- block count within budget;
- total output within budget;
- if expected output is supplied, exact equality;
- every block successful.

Extra blocks after expected output are not ignored.

## 7. IsoMap versus Preview

They share the envelope, but not the content contract:

### IsoMapPack5

- output is interpreted as 11-byte records;
- many writers append four zero bytes to decoded content;
- dense versus sparse record count remains a map-level question.

### PreviewPack

- expected output is `width × height × 3`;
- no four-byte decoded suffix is expected by the strongest writer/reader evidence;
- channel interpretation is outside the codec.

The envelope reader must not know either rule.

## 8. Overlay chunks

Overlay sections use the same size envelope with Format80 payloads. A Format80 payload’s in-stream terminator is validated independently from the envelope’s payload length.

## 9. Limits

`ChunkedEnvelopeReadLimits`:

- max block count;
- max compressed bytes per block;
- max output bytes per block;
- max aggregate compressed bytes;
- max aggregate output bytes;
- max diagnostics;
- allowed zero-size policy.

No limit is derived from untrusted multiplication without checked arithmetic.
