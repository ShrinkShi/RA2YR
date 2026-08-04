# FinalSun/FinalAlert 2 safe read/write and round-trip boundaries

## 1. Evidence role

The Electronic Arts release of FinalSun/FinalAlert 2 source is official editor/tool evidence under GPL-3.0. It is highly valuable for:

- section names and practical writer behavior;
- Base64 and compression envelopes;
- preview generation;
- map/TMP integration;
- known UI and save constraints.

It is not the original game runtime and does not prove that every editor normalization is semantically harmless.

## 2. Round-trip levels

Distinguish four claims:

1. **Byte-identical:** saved bytes equal input bytes.
2. **Lossless document:** all occurrences, unknown data and packed payloads survive, but layout/encoding may differ.
3. **Semantic:** supported game-visible behavior remains equivalent.
4. **Editor-compatible:** the selected editor can reopen its output.

Passing level 4 does not establish levels 1–3.

## 3. Known normalization risks

Editors may:

- reorder sections, especially Preview/PreviewPack;
- renumber numeric records;
- regenerate `IsoMapPack5` record order;
- omit clear terrain records;
- recompress with different block boundaries;
- rewrite Base64 line lengths;
- regenerate Overlay/OverlayData arrays;
- regenerate or replace preview images;
- clamp or repair invalid tile/subtile IDs;
- drop unsupported event/action fields;
- remove unknown/editor-invisible sections;
- canonicalize booleans, numbers, case and whitespace;
- update dependent scripting IDs.

Every such action must be explicit in a future writer result.

## 4. Read-only default

Initial implementation should be read-only. Enabling save requires:

- a lossless shell representation;
- independent pack writers and tests;
- unknown-section preservation;
- reference-graph validation;
- mutation tracking;
- per-operation diagnostics;
- atomic output to a new destination;
- source fingerprint checks.

Never overwrite the source map by default.

## 5. Preservation strategies

### Untouched packed section

If semantic content is not changed, preserve the original numbered occurrences and compressed bytes.

### Modified packed section

Regenerate only the changed section and record:

- old/new compressed and decompressed hashes;
- writer policy/version;
- record ordering selected;
- block sizes and line-splitting policy;
- any dropped/implicit records.

### Unknown section

Preserve exact raw occurrence order unless the user explicitly removes it.

## 6. Scripting identity transactions

Renumbering triggers, tags, teams, scripts or task forces requires a graph transaction:

1. calculate a complete old→new identity mapping;
2. reject duplicate/ambiguous source identities;
3. update all known references;
4. retain unsupported raw records with unresolved-reference diagnostics;
5. validate no dangling references were introduced;
6. report every changed occurrence.

Partial renumbering is not acceptable.

## 7. Preview policy

A writer may:

- preserve the original preview;
- deliberately regenerate it;
- add an explicitly requested compatibility dummy preview.

It must not silently replace a valid preview because terrain was loaded. Preview generation belongs to a rendering/editor adapter, not Core.

## 8. TMP writing

TMP roundtrip is higher risk because cell offsets, metadata, image/depth planes and optional extra data must be rebuilt consistently.

Initial support should preserve raw TMP bytes. A future editor needs:

- exact known cell-header layout;
- independent size/offset calculations;
- nonoverlap validation;
- preservation of unknown metadata bits;
- theater palette handled outside the file;
- proof across empty/multicell/extra-data templates.

## 9. Atomic and provenance-safe output

Writer result:

```text
MapWriteResult
- DestinationIdentity
- SourceFingerprintBefore
- SourceFingerprintAfter
- OutputSha256
- ModifiedSections
- PreservedRawSections
- RegeneratedPackedSections
- Normalizations
- LostEvidence
- Diagnostics
```

Use a temporary file and atomic replace only after full validation. Original files and MIX archives remain read-only.

## 10. Golden round-trip gates

Before claiming semantic roundtrip:

- synthetic fixtures cover every known record class;
- multiple sanitized RA2/YR maps across roles pass;
- source and output parse to equivalent canonical semantic models;
- unknown occurrences are unchanged;
- packed-section decompressed hashes match when untouched;
- all mission references remain complete;
- Memory/Stream/MIX-window reads agree;
- no success depends on FinalAlert's repair behavior.

## 11. Forbidden claims

Do not claim roundtrip compatibility merely because:

- FinalAlert opens the output;
- the original game reaches the loading screen;
- preview looks correct;
- pack output length is similar;
- a map contains no obvious scripting;
- XCC can extract the file;
- one checksum changed only because of recompression.
