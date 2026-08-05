# Packed array layout and decoded-length contract

> **Research provenance:** This document was produced by ChatGPT Web from public sources. It did not read ProjectBaseline, is not a local Codex Agent artifact, and imports no code. GPL or unclear-license sources are reference-only; no source was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

## 1. Ordinary storage candidate

For the ordinary RA2/YR map profile, the strongest storage model is two arrays:

```text
OverlayPack:     512 × 512 × 1 byte = 262144 bytes
OverlayDataPack: 512 × 512 × 1 byte = 262144 bytes
```

Each index represents one storage coordinate. The arrays are parallel by index but remain separate decoded artifacts.

## 2. Source behavior matrix

| Source | Type output | Data output | Grade | Notes | Policy | AuditStatus |
|---|---:|---:|---|---|---|---|
| EA FinalSun/FinalAlert 2 | explicit 262144-byte target | explicit 262144-byte target | `ConfirmedByOfficialToolSource` | Official editor preinitializes type with `0xFF` and data with `0x00`; this does not establish original-runtime strictness. | Preserve editor behavior as source evidence, not parser padding policy. | `NotRun` |
| OpenRA importer | `1 << 18` | `1 << 18` | `ImplementationSpecificBehavior` | Named importer behavior with XCC/community lineage caveats. | Use only as a comparison implementation. | `NotRun` |
| WAE ordinary profile | `512 × 512` bytes | `512 × 512` bytes | `ImplementationSpecificBehavior` | Named editor profile; writer initializes type to `0xFF` and data to zero. | Keep separate from vanilla runtime claims. | `NotRun` |
| WAE extended profile | `512 × 512 × 2` bytes | `512 × 512` bytes | `ImplementationSpecificBehavior` | Enabled by `NewINIFormat >= 5`; this is an extension profile with 16-bit type ordinals. | Require explicit extended profile selection. | `NotRun` |
| CNCMaps | `1 << 18` | `1 << 18` | `ImplementationSpecificBehavior` | Fixed renderer/tool arrays; not runtime proof. | Comparison only. | `NotRun` |
| MapTool | `1 << 18` | `1 << 18` | `ImplementationSpecificBehavior` | Fixed map-tool arrays; not runtime proof. | Comparison only. | `NotRun` |
| ModEnc oldid 21267 | 262144 bytes | 262144 bytes | `ConfirmedCommunityConvention` | Stable community documentation for the ordinary storage convention, not original-runtime source. | Cite as convention only. | `NotRun` |

## 3. Normalized storage claims

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert/FinalSun uses 262144-byte ordinary type and data targets | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Confirms official-editor behavior. | Preserve as source-pinned profile evidence. | `NotRun` |
| Several public tools use the same one-byte 512 × 512 storage dimensions | `Underconfirmed` | OpenRA, WAE, CNCMaps, MapTool, ModEnc | The convergence is substantial, but implementation independence and stock-runtime universality are not proven. | Treat 262144 as an explicit ordinary-profile candidate. | `NotRun` |
| WAE supports a 524288-byte extended OverlayPack type stream | `ImplementationSpecificBehavior` | World-Altering Editor | The behavior is tied to `NewINIFormat >= 5`; it is an extension profile and not an ordinary vanilla candidate. | Never infer it as vanilla from decoded length alone. | `NotRun` |
| Every original-runtime path requires exact 262144-byte ordinary output | `Underconfirmed` | Official editor and public tool behavior | No original-runtime source establishes strict rejection or permissive padding behavior. | Enforce exact length as a project policy, not a runtime claim. | `NotRun` |
| Exact length, no padding, no truncation, and no partial success | `DefensiveDesign` | Project policy | This is the project's fail-closed contract. | Validate before semantic pairing or binding. | `NotRun` |

## 4. Is 512 a format boundary or a convention?

The editor and major tools allocate a fixed 512-by-512 storage plane irrespective of scenario `Size` and `LocalSize`. This supports a fixed packed-storage candidate, but it does not prove independent discovery or universal original-runtime enforcement.

What remains unresolved:

- whether every original runtime path rejects non-262144 output;
- whether the engine silently tolerates shorter decoder output because its destination was prefilled;
- whether later engine extensions reinterpret the type stream as 16-bit;
- whether a compressed stream may produce extra decoded bytes that are ignored by a caller.

The project strict profile does not inherit permissive tool behavior.

## 5. Required length metadata

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

A value of 262144 is a profile input, not a hidden allocation constant inside the decoder.

## 6. Strict success conditions

The following are `DefensiveDesign` conditions. For the ordinary one-byte profile, successful exact decode requires:

```text
ActualDecodedLength == 262144
DeclaredDecodedLength == 262144, if declarations exist
no unread compressed payload
no bytes after the permitted Format80 terminator
no missing output
no output beyond budget
```

For an explicit WAE-compatible extended type profile:

```text
OverlayPack actual length == 524288
OverlayDataPack actual length == 262144
```

The two profiles may not be inferred by output length after trying multiple decoders. Profile selection must come from the caller and map/version evidence.

## 7. Public leniency findings

EA's editor initializes its output buffers before decoding. A short type output can therefore leave `0xFF` bytes in the remainder, and a short data output can leave zero bytes. This is `ConfirmedByOfficialToolSource` for editor behavior, not proof that the original format defines implicit padding.

Other public implementations similarly allocate fixed outputs and may rely on decoder APIs that do not expose exact production. Those are `ImplementationSpecificBehavior` and do not become the Core default.

## 8. Forbidden repairs

The following are `DefensiveDesign` prohibitions. The parser must not:

- pad short type output with `0xFF`;
- pad short data output with `0x00`;
- truncate output longer than the selected profile;
- declare success with only part of the array;
- resize according to map `Size` or `LocalSize`;
- use the partner array's length as the expected length;
- reinterpret a 524288-byte type array as two consecutive ordinary arrays;
- interpret a 262144-byte type array as the low bytes of an extended array without explicit policy;
- discard trailing bytes because most cells are empty.

## 9. Storage representation

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

## 10. Roundtrip implications

A byte-identical map roundtrip may require retention of:

- original fragment spelling and order;
- exact compressed bytes;
- chunk boundaries;
- Format80 command selection;
- decoded bytes, including map-domain-external storage;
- any trailing compressed or decoded bytes, even when rejected semantically.

A canonical writer that only serializes active scenario cells cannot claim byte-identical or lossless roundtrip.
