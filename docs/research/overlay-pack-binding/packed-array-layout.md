# Packed array layout and decoded-length contract

> **Research provenance:** This document was produced by ChatGPT Web from public sources. It did not read ProjectBaseline, is not a local Codex Agent artifact, and imports no code. GPL or unclear-license sources are reference-only; no source was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

## 1. Ordinary storage candidate

For the ordinary RA2/YR map profile, the strongest storage model is two arrays:

```text
OverlayPack:     512 × 512 × 1 byte = 262144 bytes
OverlayDataPack: 512 × 512 × 1 byte = 262144 bytes
```

Each index represents one storage coordinate. The arrays are parallel by index but remain independent decoded artifacts.

## 2. Evidence matrix

| Source | Type output | Data output | Behavior |
|---|---:|---:|---|
| EA FinalSun/FinalAlert 2 | explicit 262144-byte target | explicit 262144-byte target | preinitializes type with `0xFF`, data with `0x00`; decoder writes into supplied storage |
| OpenRA importer | `1 << 18` | `1 << 18` | two fixed byte arrays |
| WAE ordinary profile | `512 × 512` bytes | `512 × 512` bytes | writer initializes type to `0xFF`, data to zero |
| WAE extended profile | `512 × 512 × 2` bytes | `512 × 512` bytes | enabled by `NewINIFormat >= 5`; 16-bit type ordinals |
| CNCMaps | `1 << 18` | `1 << 18` | fixed arrays |
| MapTool | `1 << 18` | `1 << 18` | fixed arrays |
| ModEnc oldid 21267 | 262144 bytes | 262144 bytes | community documentation |

The fixed one-byte length is therefore `ConfirmedByOfficialEditorSource` and reinforced by several implementations. It is not yet `ConfirmedByOfficialRuntimeSource`.

## 3. Is 512 a format boundary or a convention?

The evidence shows that the editor and major tools allocate a fixed 512-by-512 storage plane irrespective of scenario `Size` and `LocalSize`. This is strong evidence that the packed storage contract is fixed rather than dynamically sized to the map.

What remains unresolved:

- whether every original runtime path rejects non-262144 output;
- whether the engine silently tolerates shorter decoder output because its destination was prefilled;
- whether later engine extensions reinterpret the type stream as 16-bit;
- whether a compressed stream may produce extra decoded bytes that are ignored by a caller.

The project strict profile does not inherit permissive tool behavior.

## 4. Required length metadata

Each decoded array result should record:

- `DeclaredDecodedLength`: sum of outer chunk output declarations, when available;
- `ActualDecodedLength`: bytes actually produced by the selected Format80 profile;
- `ExpectedStorageLengthCandidate`: profile-specific expected length;
- `MissingByteCount`;
- `TrailingByteCount`;
- `TrailingBytesWindow` or hash, subject to public-output restrictions;
- exact compressed-window consumption;
- exact command termination status;
- evidence grade for the expected length;
- source and fragment provenance.

A value of 262144 is a policy input, not a hidden allocation constant inside the decoder.

## 5. Strict success conditions

For the ordinary one-byte profile, successful exact decode requires:

```text
ActualDecodedLength == 262144
DeclaredDecodedLength == 262144, if declarations exist
no unread compressed payload
no bytes after the permitted Format80 terminator
no missing output
no output beyond budget
```

For an explicit extended type profile:

```text
OverlayPack actual length == 524288
OverlayDataPack actual length == 262144
```

The two profiles may not be inferred by output length after trying multiple decoders. Profile selection must come from the caller and map/version evidence.

## 6. Public leniency findings

EA's editor initializes its output buffers before decoding. A short type output can therefore leave `0xFF` bytes in the remainder, and a short data output can leave zero bytes. This is an editor safety/tolerance behavior, not proof that the original format defines implicit padding.

Other community implementations similarly allocate fixed outputs and may rely on decoder APIs that do not expose exact production. These behaviors are classified as `ImplementationSpecificBehavior` in the source matrix and do not become the Core default.

## 7. Forbidden repairs

The parser must not:

- pad short type output with `0xFF`;
- pad short data output with `0x00`;
- truncate output longer than the selected profile;
- declare success with only part of the array;
- resize according to map `Size` or `LocalSize`;
- use the partner array's length as the expected length;
- reinterpret a 524288-byte type array as two consecutive ordinary arrays;
- interpret a 262144-byte type array as the low bytes of an extended array without explicit policy;
- discard trailing bytes because most cells are empty.

## 8. Storage representation

Recommended raw representation:

```text
OverlayArrayRaw
- SectionKind
- StorageElementWidth
- ExpectedStorageLengthCandidate
- DeclaredDecodedLength
- ActualDecodedLength
- Bytes
- MissingByteCount
- TrailingBytes
- Format80Profile
- DecodeTrace
- Provenance
- EvidenceGrade
```

The model does not expose a typed Overlay object until coordinate and registry binding succeed.

## 9. Roundtrip implications

A byte-identical map roundtrip may require retention of:

- original fragment spelling and order;
- exact compressed bytes;
- chunk boundaries;
- Format80 command selection;
- decoded bytes, including map-domain-external storage;
- any trailing compressed or decoded bytes, even when rejected semantically.

A canonical writer that only serializes active scenario cells cannot claim byte-identical or lossless roundtrip.