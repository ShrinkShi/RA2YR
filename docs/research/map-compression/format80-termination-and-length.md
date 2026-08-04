# Format80 termination and length contracts

## 1. Three independent boundaries

A map Format80 block has:

1. compressed payload length from the chunk header;
2. declared uncompressed output length from the chunk header;
3. an in-stream `0x80` terminator.

A strict decoder validates all three. None substitutes for another.

## 2. Successful completion

The strict map profile succeeds only when:

- a complete terminator command is seen;
- produced bytes equal declared uncompressed size;
- consumed bytes equal declared compressed payload size;
- no output command exceeded the output window;
- no input read exceeded the payload window;
- no invalid reference occurred;
- no codec diagnostic is fatal.

## 3. Terminator before expected output

If `0x80` appears before the declared output count:

- result is `OutputUnderflow`;
- partial bytes may be returned only in an explicitly marked failed result for debugging;
- downstream map parsers receive no successful output.

No zero padding is applied.

## 4. Output reached before terminator

If output reaches the declared count while compressed bytes remain:

- the decoder does not silently stop;
- it continues only far enough to determine whether the next command is the immediate terminator without producing output;
- a producing command is `OutputOverflow`;
- absent terminator is `MissingTerminator`;
- bytes after a valid terminator are `TrailingCompressedInput`.

## 5. Terminator trailing bytes

Public tools often ignore or cannot report payload bytes after termination because their decoder lacks an input-consumed result. The strict design treats any trailing byte within the declared payload as failure unless a later evidence-gated profile defines padding.

Trailing bytes after the last chunk are an envelope-level diagnostic, not a codec-level one.

## 6. Truncation classes

Separate diagnostics:

- command byte missing;
- command parameter truncated;
- literal payload truncated;
- fill value missing;
- short-copy low byte missing;
- medium position missing;
- long count or position missing.

This preserves forensic value without exposing payload bytes.

## 7. Reference validation

Before copying:

- source index/distance arithmetic is checked;
- source begins below current output;
- source range can be expanded under overlap rules;
- destination end fits the output window;
- zero-distance relative references fail;
- absolute references to current/future output fail.

## 8. Partial output

`Format80DecodeResult` can report `BytesProduced` on failure for diagnostics, but:

- `Success=false`;
- decoded storage is not passed to the map-specific layer;
- no canonical decoded hash is published for failed output;
- callers cannot opt into “best effort” through an untracked boolean.

## 9. Standalone versus map profile

A generic archival inspector may use a standalone profile with unknown expected output size and terminate solely on `0x80`, subject to a maximum output limit. RA2/YR map decoding is stricter because each block declares an output size.

The two APIs must be explicit; a map caller cannot accidentally select standalone behavior.

## 10. No resynchronization

The decoder does not scan forward for a later `0x80`, skip malformed commands, clamp command lengths, or reinterpret an invalid absolute copy as relative. Such behavior would make corruption and variant mismatch indistinguishable.
