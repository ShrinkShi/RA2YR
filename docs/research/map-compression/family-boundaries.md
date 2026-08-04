# Compression family boundaries

## 1. Names are not sufficient contracts

The labels `Format80`, `LCW`, `Format5`, `LZO`, and `miniLZO` are used inconsistently across tools. The production design must identify a format by:

- command or bitstream family;
- reference-position convention;
- stream direction;
- outer envelope;
- expected output contract;
- termination contract;
- strictness profile.

A decoder selected only by a historical name is not evidence-safe.

## 2. Family register

| Family | This dossier | Boundary |
|---|---|---|
| Westwood Format80 / LCW | Primary | Five-command LZ/RLE stream, with absolute/relative position variants |
| Map chunked Format80 (`Format5` with codec 80) | Primary | Repeated size header plus one independent Format80 payload per block |
| Raw LZO1X-compatible block | Primary | Payload codec inside map LZO chunks |
| Map chunked LZO | Primary | Repeated size header around raw LZO1X-compatible payloads |
| Numbered Base64 INI fragments | Primary | Text transport envelope before chunk parsing |
| Format40 | Boundary only | XOR/delta stream; not a Format80 mode |
| SHP(TS) RLE | Boundary only | Row-oriented SHP command stream |
| MIX archive compression/encryption | Boundary only | Archive-level concerns; unrelated to map pack codecs |
| Format20/other Westwood labels | Boundary only | Separate historical algorithms |
| zlib/Deflate | Excluded | Different bitstream and wrapper |
| generic LZO container formats | Excluded | Map blocks are raw payloads with a Westwood size envelope, not `.lzo` files |
| encode strategy | Secondary | Any compatible encoder may emit a subset; decoder coverage is broader |
| reverse/decode-into-end APIs | Conflict boundary | API/storage convention, not automatically a different command byte set |

## 3. Format80 versus LCW

Community documentation and OpenRA use `LCW` and `Format80` for the same core command classes. This equivalence is limited:

- a stream may use absolute output-start positions;
- another API may treat the same two-byte fields as backward distances;
- an initial marker may select relative mode;
- a caller may decode forward or into a reverse-positioned buffer;
- a map block adds its own size header;
- some implementations require only a terminator, others rely on a declared output size.

Therefore the Core type is not a parameterless `LcwDecoder`. It requires an explicit `Format80Variant`.

## 4. Format80 versus Format40

Format40 is an XOR/delta family commonly applied after or before other compression in animation/video pipelines. It has different commands and prior-frame dependencies. No Format80 command may be interpreted as Format40 and no shared “Westwood decoder” switch statement is proposed.

## 5. Map compression versus SHP compression

- Overlay packs: chunked Format80.
- IsoMap and preview packs: chunked raw LZO1X-compatible payloads.
- SHP(TS) flags/RLE: separate frame/row format.
- TMP: generally raw indexed planes in a directory structure.
- MIX: container and optional archive mechanisms.

The fact that multiple formats use back-references does not justify code or semantic unification beyond generic bounded-window helpers.

## 6. Decode versus encode

The decoder must accept every command allowed by the selected variant. An encoder may intentionally emit only:

- literal commands;
- fill commands;
- selected back-reference forms;
- a canonical terminator.

OpenRA and WAE encoders are simplified literal/fill writers. Their limited output does not narrow the legal decoder command set.

## 7. Proposed identifiers

```text
PackedCodecKind
- Format80
- RawLzo1X

Format80Variant
- AbsolutePositions
- RelativePositions
- MarkerSelected
- ReverseApiCandidate

PackedEnvelopeKind
- WestwoodMapChunked

PackedSectionRole
- IsoMapPack5
- PreviewPack
- OverlayPack
- OverlayDataPack
```

Each identifier is serializable and appears in diagnostics and audit results.
