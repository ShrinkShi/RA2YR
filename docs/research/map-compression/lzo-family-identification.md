# LZO family identification

## 1. Strongest identification

The map payload codec is best described as:

```text
raw LZO1X-compatible block stream
```

Evidence:

- OpenRA names its decoder `LZO1xDecompress` and states it is a port of miniLZO 2.06.
- WAE includes a miniLZO 2.06-derived implementation and uses its compressor/decompressor in map chunks.
- miniLZO documents an `LZO1X-1` compressor and standard/safe LZO1X decoders.
- CnCNet preview extraction uses an LZO stream backend with the same Westwood chunk header.

This identifies the decoder family, not the original encoder level.

## 2. What is not established

Static research does not establish that stock RA2/YR maps were encoded specifically with:

- LZO1X-1;
- LZO1X-999;
- another LZO1X compressor level;
- the exact miniLZO 2.06 encoder;
- a Westwood-modified encoder.

LZO1X compressor variants can produce streams consumed by the same LZO1X decoder. Successful decoding cannot identify the compressor.

## 3. miniLZO relationship

miniLZO is a small source subset generated from the LZO project. Its documented codec set includes:

- LZO1X-1 compression;
- standard LZO1X decompression;
- safe LZO1X decompression;
- compatibility with streams created by higher-compression LZO1X encoders.

“miniLZO-compatible” therefore describes implementation lineage or decoder compatibility, not a distinct on-disk map format.

## 4. Map payload is not an `.lzo` container

The INI section provides:

- Base64 transport;
- Westwood chunk sizes;
- raw LZO1X payloads.

There is no evidence of a generic LZO file header, checksum container, filename metadata, or zlib wrapper. A backend receives exactly one bounded payload and a declared output capacity.

## 5. Backend contract

```text
LzoDecodeBackend.Decode(
    inputWindow,
    outputWindow,
    LzoCodecKind.RawLzo1X)
→ LzoDecodeResult
```

Required result fields:

- status;
- input consumed;
- output produced;
- backend-specific error code;
- normalized diagnostic;
- exact-input boolean;
- exact-output boolean.

A void decoder or stream API that cannot report exact consumption needs an adapter with independent verification or is unsuitable for strict production use.

## 6. Backend error normalization

At minimum:

- `InputOverrun`
- `OutputOverrun`
- `LookbehindOverrun`
- `InvalidEndMarker`
- `TrailingInput`
- `OutputSizeMismatch`
- `BackendFailure`
- `UnsupportedCodec`

Backend return codes are preserved in an optional raw field but never determine map semantics directly.

## 7. Output size

The chunk header supplies the expected uncompressed block size. The backend must not allocate from compressed data. The envelope reader validates the size against:

- per-block policy;
- remaining aggregate output;
- caller-provided destination window;
- checked integer limits.

## 8. Evidence level

- LZO1X decoder family: `ConfirmedByMultipleImplementations`.
- WAE writer uses miniLZO/LZO1X-1: `ImplementationSpecificBehavior`.
- original Westwood compressor level: `Unresolved`.
- no modified Westwood LZO bitstream observed in public sources: `Underconfirmed`, not proof of absence.
