# PreviewPack fragments, chunk envelope, LZO, and decoded-length contract

> Source notice: researched by **ChatGPT Web** from public sources; no local `ProjectBaseline`; not a Codex Agent artifact; no source implementation copied, translated, mechanically rewritten, or imported (`code_imported: false`).

## 1. Packed pipeline

```text
PreviewPack physical entries
→ explicit fragment ordering
→ one Base64 character stream
→ strict Base64 bytes
→ repeated chunk headers and payload windows
→ bounded raw LZO1X-compatible backend
→ aggregate decoded byte stream
→ exact pixel-length validation
```

Each stage returns its own result and diagnostics. No downstream success erases an upstream anomaly.

## 2. Numbered fragments

Common writers use decimal keys beginning at `1`, with values wrapped at 70 Base64 characters. EA's editor and WAE both produce 70-character fragments; WAE explicitly increments from key `1`. CNCMaps does the same. This is a writer/toolchain convention, not sufficient proof that the runtime numerically sorts arbitrary keys.

The collector retains:

- section occurrence;
- raw key spelling;
- parsed integer candidate;
- source occurrence order;
- normalized numeric key;
- value text before and after allowed trimming;
- empty values;
- duplicate normalized groups;
- missing-number ranges;
- nonnumeric entries;
- comments and line provenance;
- original Base64 wrapping.

### Strict configured policy

- canonical unsigned decimal keys only;
- default start key `1`;
- key `0` diagnosed and rejected unless a profile explicitly allows it;
- leading zeroes diagnosed;
- `1` and `01` form a duplicate normalized group;
- gaps diagnosed rather than silently terminating;
- nonnumeric keys are not treated as pixels;
- values are joined once, then decoded once;
- dictionary enumeration cannot define ordering;
- ordinary semantic INI key override cannot delete a physical fragment.

These strict collection rules are `DefensiveDesign`. Original runtime handling of gaps, duplicates, key zero, and physical versus numeric order remains `Unresolved`.

## 3. Fragment evidence

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert writes 70-character PreviewPack fragments | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Official-editor writer behavior only. | Preserve as a named writer profile. | `NotRun` |
| WAE and CNCMaps write keys `1..N` with 70-character values | `ImplementationSpecificBehavior` | Named writers | Tool-specific output conventions. | Accept through explicit writer/reader profiles. | `NotRun` |
| Keys `1..N` and 70-character wrapping are a common toolchain convention | `ConfirmedCommunityConvention` | Public writers and community documentation | Does not establish runtime sorting or line-length requirements. | Preserve source wrapping; do not enforce as a runtime fact. | `NotRun` |
| Original runtime numerically sorts arbitrary fragment keys or requires 70-character lines | `Unresolved` | No original-runtime source located | Common canonical files do not distinguish numeric, physical, or container order. | Keep source and normalized order as separate views. | `NotRun` |

## 4. Base64 contract

Strict Base64 processing requires:

- maximum fragment count;
- maximum characters per fragment;
- maximum aggregate characters;
- explicit ASCII-whitespace policy;
- no hidden removal of punctuation or comments from value content;
- valid alphabet;
- valid padding only at the end of the aggregate stream;
- no decoding of fragments independently;
- no partial output on invalid input;
- exact source-span diagnostics.

These are `DefensiveDesign` requirements. Original wrapping is preserved separately from canonical decoded bytes.

## 5. Chunk envelope candidate

The public implementations use repeated little-endian blocks:

```text
u16le compressedSize
u16le uncompressedSize
byte[compressedSize] compressedPayload
```

This is shared with the map packed-section family but the Preview pipeline owns a distinct logical document and output budget.

### Source behavior

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert routes the full raw preview through its packed-section encoder | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Official-editor caller behavior; the bundled low-level lineage is XCC-derived and not runtime source. | Keep source attribution explicit. | `NotRun` |
| WAE writes repeated `u16le compressedSize + u16le uncompressedSize + payload` blocks | `ImplementationSpecificBehavior` | World-Altering Editor | Named writer behavior. | Use as a source-pinned comparison profile. | `NotRun` |
| CnCNet reads the two 16-bit sizes and advances through bounded payloads | `ImplementationSpecificBehavior` | CnCNet XNA client | Named consumer behavior, including its own zero-size termination policy. | Do not inherit leniency automatically. | `NotRun` |
| CNCMaps and MapTool use their generic map-pack helpers for PreviewPack | `ImplementationSpecificBehavior` | Named tools | Helper ancestry and precise backend details remain tool-specific. | Keep Preview compatibility separate from generic pack implementation status. | `NotRun` |
| The chunk envelope is a stable PreviewPack toolchain convention | `ConfirmedCommunityConvention` | Public tools and ModEnc | Strong convention, but no original-runtime source was found. | Require an explicit envelope profile. | `NotRun` |

## 6. The 8192 question

WAE uses `8192` as maximum uncompressed bytes per emitted block.

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| WAE emits blocks with at most 8192 uncompressed bytes | `ImplementationSpecificBehavior` | World-Altering Editor | Writer-specific behavior. | Preserve as an optional target-writer profile. | `NotRun` |
| 8192 is a common PreviewPack block-output convention | `Underconfirmed` | WAE and community/tool knowledge | The reviewed sources do not establish sufficiently independent writer lineages or universal use. | Keep configurable. | `NotRun` |
| 8192 is an original-runtime hard maximum or an LZO bitstream limit | `Unresolved` | No original-runtime source located | No evidence establishes a universal reader limit. | Do not bake 8192 into the backend. | `NotRun` |

Core therefore has a configurable per-block output limit. A profile may set 8192 for compatibility testing, but the value is not baked into the codec backend.

## 7. Zero-size blocks

CnCNet stops when either size is zero. That means it treats `0/positive`, `positive/0`, and `0/0` similarly. This is `ImplementationSpecificBehavior`, not a proven format rule.

The strict project policy distinguishes:

- `0/0`: terminal-sentinel candidate, only accepted by an explicit profile;
- `0/nonzero`: malformed;
- `nonzero/0`: malformed;
- input exhaustion exactly after the final payload: valid termination candidate;
- partial header: truncated input.

This separation is `DefensiveDesign`. No one-zero block is silently treated as success by default.

## 8. Exact block contract

For every block:

- four header bytes must be available;
- `compressedSize` must fit the remaining input window;
- `uncompressedSize` must fit the per-block and aggregate output budgets;
- the backend receives exactly `compressedSize` bytes;
- the backend must report exact input consumption if its API exposes it;
- the backend must produce exactly `uncompressedSize` bytes;
- short output, extra output, codec error, or no progress is failure;
- a failed block does not contribute partial aggregate output;
- arithmetic is checked.

These are `DefensiveDesign` requirements. They do not assert stock-runtime strictness.

## 9. Aggregate termination

The envelope reader stops only according to the selected explicit profile:

- exact input exhaustion;
- aggregate output reaching the validated expected pixel length **and** exact input exhaustion;
- accepted `0/0` sentinel with no forbidden trailing bytes.

Reaching `width×height×3` does not authorize ignoring additional chunks or bytes. Explicit profile selection and refusal of trial decoding are `DefensiveDesign`.

## 10. Decoded pixel-length contract

Leading candidate:

```text
ExpectedDecodedLength = Width × Height × 3
```

Source observations:

- EA's editor allocates and encodes exactly `width × height × 3` bytes;
- WAE allocates `texture pixel count × 3`;
- CnCNet allocates that destination and its image builder requires equality;
- CNCMaps allocates that size;
- MapTool allocates that size;
- ModEnc says each scanline is exactly `3 × width` with no padding.

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert emits exactly three bytes per preview pixel | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Official-editor writer behavior. | Source-pinned writer profile. | `NotRun` |
| Named public tools use `width × height × 3` buffers | `ImplementationSpecificBehavior` | WAE, CnCNet, CNCMaps, MapTool | Each row describes a named implementation; source-count convergence is assessed separately. | Comparison evidence only. | `NotRun` |
| `width × height × 3` is the leading standard storage candidate | `Underconfirmed` | Official editor, public tools, ModEnc | Strong convergence exists, but independent lineages and stock-runtime strictness are not proven. | Use under an explicit standard metadata/pixel profile. | `NotRun` |
| Stock runtime rejects every non-exact decoded length | `Underconfirmed` | Public writer and consumer behavior | No runtime source determines zero-fill, truncation, or trailing-byte behavior. | Enforce exactness as project policy. | `NotRun` |
| Checked multiplication, exact per-block and aggregate output, no padding/clamp/truncation/partial success | `DefensiveDesign` | Project policy | Fail-closed length contract. | Validate before pixel interpretation. | `NotRun` |

No fixed decoded trailer was found in the reviewed PreviewPack sources. No inspected standard path added alpha, palette indices, decoded scanline padding, or a trailer. These are bounded absence statements for the reviewed evidence set, not absolute runtime proof. IsoMap's debated four-byte remainder is not imported into the preview format.

## 11. Length result model

Record separately:

- `MetadataDimensionsCandidate`;
- `ExpectedDecodedLength`;
- `ActualDecodedLength`;
- `MissingByteCount`;
- `TrailingByteCount`;
- bounded retained trailing-byte provenance or digest policy;
- `LengthStatus`;
- evidence grade;
- block count and size aggregates;
- exact compressed input consumption.

Statuses include:

- `Exact`;
- `Underflow`;
- `Overflow`;
- `MetadataInvalid`;
- `BudgetExceeded`;
- `ArithmeticOverflow`;
- `CodecFailure`;
- `EnvelopeFailure`;
- `TrailingCompressedInput`.

## 12. Lenient public behavior

CnCNet preallocates the full destination and can return it after input exhaustion without verifying that every byte was written; unwritten bytes remain zero. This is `ImplementationSpecificBehavior` useful for consumer robustness but cannot become production Core semantics.

Any public implementation that pads, ignores a short read, truncates, or accepts trailing input remains tool-specific behavior.

## 13. Pixel rows versus blocks

LZO block boundaries do not define scanlines. A block can end:

- in the middle of a pixel;
- in the middle of a row;
- exactly at a row boundary;
- after multiple rows.

Only the aggregate decoded stream is interpreted as pixels.

## 14. Input modes

Memory, seekable Stream, short-read Stream, and bounded MIX window use the same envelope state machine and must produce identical:

- bytes;
- diagnostics;
- consumed lengths;
- aggregate hashes;
- failure categories.

No input-driven unbounded allocation or no-progress loop is permitted. These are `DefensiveDesign` implementation requirements and do not promote PreviewPack compatibility status.
