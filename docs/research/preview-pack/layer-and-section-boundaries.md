# Preview metadata and payload layer boundaries

> Source notice: researched by **ChatGPT Web** from public sources; no local `ProjectBaseline` access; not a Codex Agent product; no GPL or unclear-license code copied, translated, mechanically rewritten, or imported (`code_imported: false`).

## 1. Two logical sections

`[Preview]` and `[PreviewPack]` are separate logical inputs.

### `[Preview]`

Carries metadata, most notably a four-component `Size=` value. It does not contain compressed pixels.

### `[PreviewPack]`

Carries numbered Base64 fragments. It does not independently define image dimensions.

A parser must retain these states separately:

| Metadata | Payload | Result category |
|---|---|---|
| missing | missing | `PreviewAbsent` |
| present and valid | missing | `MetadataOnly` |
| missing | present | `PayloadOnly` |
| invalid | present | `MetadataInvalidPayloadPresent` |
| valid | invalid | `MetadataValidPayloadInvalid` |
| valid | valid | eligible for decode and consistency analysis |

No missing state authorizes fabrication in Core.

## 2. Required processing boundary

```text
LosslessIniDocument
├─ locate every physical [Preview] occurrence
├─ retain duplicate Size keys and provenance
└─ locate every physical [PreviewPack] occurrence

PreviewMetadataView
├─ preserve four raw tokens
├─ parse integer candidates
└─ produce interpretation candidates

PreviewFragmentCollection
├─ preserve section occurrence
├─ preserve key spelling and source order
├─ identify numeric keys
├─ diagnose duplicates/gaps/leading zeroes
└─ produce one explicitly ordered character stream

StrictBase64
→ ChunkEnvelopeReader
→ LzoBackend
→ PreviewDecodedStream
→ PixelLayoutInterpreter
```

The ordinary ordered INI composition rule must not silently collapse numbered fragment occurrences. The fragment collector consumes lossless section entries, not a generic effective key dictionary.

## 3. Duplicate section policy

Duplicate `[Preview]` or `[PreviewPack]` sections are not normalized away by the lossless layer.

The research default is:

- preserve every physical occurrence;
- preserve source order;
- diagnose duplicate section names;
- require an explicit `PreviewSectionSelectionPolicy` before semantic interpretation;
- never concatenate duplicate sections merely because their names match;
- never choose the section that produces a plausible image.

Possible future policies include strict single-section, source-order concatenation for known editor output, or an extension-specific profile. None is currently proven as original runtime behavior.

## 4. Duplicate `Size=` policy

A generic last-wins INI view can hide evidence. The metadata layer therefore receives all `Size` occurrences and records:

- raw key spelling;
- raw value;
- section occurrence;
- source ordinal;
- composed-layer provenance;
- winner and suppressed chain if a higher-level INI composition policy has already selected a semantic key;
- whether physical duplicates existed in the same document.

Same-document duplicates and cross-layer override are separate diagnostics.

## 5. Fragment ordering boundary

The collector must distinguish:

- numeric key value;
- raw key text (`1`, `01`, `+1`, whitespace forms);
- source occurrence order;
- normalized numeric order;
- duplicate normalized key groups;
- missing numbers;
- key `0`;
- nonnumeric keys;
- empty fragment values;
- comments and whitespace outside the value.

The default project policy is strict and deterministic:

1. accept only canonical decimal keys unless a profile explicitly permits otherwise;
2. diagnose `1` versus `01` as a normalized duplicate;
3. do not let dictionary enumeration determine order;
4. do not let higher-layer ordinary key override erase a fragment;
5. preserve original fragment grouping for possible lossless round-trip;
6. produce a separate canonical ordered stream only after policy approval.

Whether the original runtime follows numeric order, physical order, or parser-container order remains unresolved. CnCNet's fast consumer appends physical occurrences; WAE and other tools usually write canonical `1..N` order, so common files do not distinguish the alternatives.

## 6. Payload decode boundary

`[PreviewPack]` characters are first joined according to the selected fragment policy, then decoded as one Base64 payload. Fragments are not independently Base64-decoded.

Strict Base64 requirements:

- bounded total characters;
- explicit whitespace policy;
- valid alphabet;
- valid final padding;
- no silent character deletion;
- no partial byte result;
- exact source-span diagnostics.

The decoded Base64 bytes are still compressed container bytes, not pixels.

## 7. Chunk and codec boundary

The outer chunk reader knows only:

```text
u16le compressedSize
u16le uncompressedSize
byte[compressedSize]
```

It does not know width, height, RGB, rows, or image consumers. The LZO backend receives a bounded payload window and an exact block-output budget. Pixel semantics begin only after all blocks produce an exact aggregate decoded stream.

## 8. Metadata/payload consistency

Consistency analysis is a separate object. It reports:

- metadata presence and validity;
- payload presence and validity;
- expected decoded size candidates;
- actual decoded size;
- missing/trailing bytes;
- selected channel and row profiles;
- profile evidence grades;
- whether a preview image model can be constructed;
- whether raw round-trip material is complete.

It does not mutate either source layer.

## 9. Non-authoritative boundary

Preview parsing never validates map gameplay content. A valid preview can be unrelated to the map; an invalid or absent preview does not prove the scenario data is invalid.

## 10. Implementation exclusions

This document proposes no C#, no Unity objects, no bitmap creation, no preview generation, and no compatibility status change.