# Recommended PreviewPack Core boundaries

> Source notice: researched by **ChatGPT Web** from public sources; no local `ProjectBaseline`; not a Codex Agent artifact; no code copied, translated, mechanically rewritten, or imported (`code_imported: false`).

This document proposes data and responsibility boundaries only. It contains no implementation or source-derived pseudo-code.

## 1. Pipeline components

```text
LosslessIniDocument
→ PreviewMetadataReader
→ PreviewFragmentCollector
→ StrictBase64Decoder
→ ChunkedLzoEnvelopeReader
→ LzoDecodeBackend
→ PreviewDecodedLengthValidator
→ PreviewPixelLayoutInterpreter
→ PreviewConsistencyAnalyzer
→ consumer adapter
```

All dependencies point downward. Unity and image libraries are outside Core.

## 2. Candidate data models

### `PreviewMetadataRaw`

Retains:

- every `[Preview]` occurrence;
- every `Size` occurrence;
- four raw token candidates;
- exact text/provenance;
- duplicate diagnostics;
- parse status.

### `PreviewMetadataInterpretation`

Contains:

- selected `PreviewSizeInterpretationProfile`;
- derived origin candidate;
- derived width/height;
- validation results;
- evidence grade;
- source trace.

It does not modify raw metadata.

### `PreviewFragmentCollection`

Contains:

- every physical `[PreviewPack]` occurrence;
- raw keys/values;
- numeric key candidates;
- physical order;
- normalized order;
- duplicate/gap groups;
- aggregate character count;
- selected ordering policy;
- provenance.

### `PreviewPackDocument`

Aggregates metadata, fragments, Base64 result, envelope result, decoded stream, interpretations, diagnostics, and round-trip information. It is not an image or Unity object.

### `PreviewDecodedStream`

Contains:

- immutable exact bytes or a bounded read-only owner;
- actual length;
- aggregate hash;
- block descriptors;
- compressed input consumption;
- decode diagnostics;
- source identity.

### `PreviewPixelRaw`

A three-byte view:

- `Component0Raw`
- `Component1Raw`
- `Component2Raw`

### `PreviewPixelLayout`

Contains:

- component count;
- width/height;
- row order profile;
- channel profile;
- row stride in source payload;
- origin interpretation;
- evidence grades;
- checked indexing status.

### `PreviewParseResult`

Separates:

- structural parse status;
- exact decode status;
- metadata status;
- length status;
- interpretation status;
- source-preview availability;
- warnings versus failures.

### `PreviewDiagnostic`

Structured fields include:

- stable code;
- severity;
- layer;
- section/key occurrence;
- byte or character span;
- expected/actual bounded values;
- evidence grade;
- remediation policy identifier;
- no original content dump.

### `PreviewReadLimits`

Independent limits:

- section occurrences;
- fragment count;
- fragment characters;
- aggregate Base64 characters;
- compressed bytes;
- chunk count;
- compressed bytes per block;
- output bytes per block;
- aggregate output;
- metadata width/height;
- pixel count;
- diagnostics count;
- retained unknown/trailing bytes.

### `PreviewConsistencyAnalysis`

Reports metadata/payload presence, expected/actual length, profile availability, source placeholder candidates, and whether a semantic pixel view can be created.

### `PreviewConsumerDescriptor`

Describes display behavior outside Core: native channel order, origin, vertical flip, alpha, scaling, crop, interpolation, fallback, and cache.

### `PreviewRoundtripDescriptor`

Tracks which identities can be preserved:

- lossless INI;
- fragments;
- compressed stream;
- chunk structure;
- decoded stream;
- semantic pixels;
- consumer display.

## 3. Explicit policies

### `PreviewFragmentOrderingPolicy`

Defines canonical numeric acceptance, physical-order handling, gaps, duplicates, leading zeroes, and multiple section occurrences.

### `PreviewSizePolicy`

Defines allowed interpretation profiles and width/height budgets. It never guesses from image content.

### `PreviewDecodedLengthPolicy`

Defines exact-length requirement and handling of underflow/overflow. Production default is exact.

### `PreviewChannelOrderPolicy`

Selects RGB, BGR, or unknown using caller/profile evidence only.

### `PreviewRowOrderPolicy`

Selects top-down, bottom-up, column-major, or unknown. No automatic rendering trials.

### `PreviewMissingSectionPolicy`

Defines whether missing/inconsistent input is reported, tolerated for document loading, or rejected for a target export. It cannot fabricate source bytes.

### `PreviewTrailingBytesPolicy`

Production default rejects compressed and decoded trailing bytes. Audit modes can classify and hash bounded unknown tails without marking success.

### `PreviewRoundtripPolicy`

Selects preserve-source, intentional recompress, canonical target rewrite, or generated preview. The default does not rewrite unchanged content.

## 4. Required invariants

- checked arithmetic everywhere;
- width and height validated before multiplication;
- input windows are bounded;
- every loop advances or fails;
- chunk count and output budgets are enforced before allocation;
- exact per-block output;
- exact aggregate output for the selected metadata profile;
- no clamp, truncate, fill, or partial success;
- raw data and derived interpretations coexist;
- profile selection is explicit and serializable;
- every interpretation carries an evidence grade;
- unknown data is preserved or fails according to policy, never silently discarded.

## 5. Reader interfaces versus responsibilities

### Metadata reader

Receives selected lossless section entries. Does not locate files or decode payloads.

### Fragment collector

Receives physical entries. Does not parse image dimensions, Base64 bytes, or LZO.

### Base64 decoder

Receives a bounded character stream. Does not know fragments or image semantics.

### Envelope reader

Receives bounded compressed bytes. Does not know pixels or UI.

### LZO backend

Receives one bounded block and exact output target. Does not allocate from metadata or choose block profiles.

### Length validator

Receives validated dimensions and decoded length. Does not decode.

### Pixel interpreter

Receives immutable decoded bytes and explicit layout profiles. Does not create images.

### Consumer adapter

Receives validated pixel views. It may create bitmaps or Unity resources but cannot alter the Core document.

## 6. Input-mode equivalence

One parsing state machine supports:

- `ReadOnlyMemory<byte>`;
- seekable stream;
- non-seekable/short-read stream;
- bounded MIX window.

The result must be identical for:

- consumed bytes;
- block descriptors;
- decoded hash;
- diagnostics;
- failure point;
- length status.

The MIX provider supplies only a bounded window and logical provenance. It does not select Preview profiles.

## 7. Memory ownership

Large buffers should use bounded pooled or owned memory behind read-only views. Lifetime is explicit. A diagnostic must not retain the entire input or duplicate decoded pixels.

No per-pixel object allocation is required for parsing; `PreviewPixelRaw` can be a view concept.

## 8. Determinism

Results cannot depend on:

- filesystem enumeration;
- dictionary order;
- stream read chunk size;
- processor endianness;
- locale;
- Unity graphics backend;
- bitmap stride;
- image plausibility;
- installed theater assets.

## 9. Fixture independence

Synthetic fixture builders must not share production formulas for:

- fragment ordering;
- chunk encoding;
- expected length;
- channel swaps;
- row indexing.

Fixtures specify tiny explicit byte sequences and independently computed expected results. No original map preview bytes are published.

## 10. Unity boundary

Core assemblies reference no `UnityEngine`. A future Unity adapter may construct:

- `Texture2D`;
- `Sprite`;
- UI material;
- cache asset.

That adapter owns vertical orientation, alpha insertion, texture format, filtering, mipmaps, and lifecycle.

## 11. No writer default yet

The evidence is insufficient to define a universal canonical writer. A future writer requires a named target profile containing:

- section placement;
- metadata semantics;
- fragment ordering/wrapping;
- chunk output size;
- sentinel policy;
- channel order;
- row order;
- missing-preview behavior.

Without that profile, only source-preserving round-trip is recommended.

## 12. Exclusions

No Preview reader, LZO backend, Base64 decoder, image, Unity adapter, writer, test code, or compatibility status is implemented here.