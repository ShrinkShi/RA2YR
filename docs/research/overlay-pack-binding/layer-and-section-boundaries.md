# Overlay section and layer boundaries

> **Research provenance:** This document was produced by ChatGPT Web from public sources. It did not read ProjectBaseline, is not a local Codex Agent artifact, and imports no code. GPL or unclear-license sources are reference-only; no source was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

## 1. Logical section identity

`[OverlayPack]` and `[OverlayDataPack]` are independent logical map sections.

Each section has its own:

- lossless INI source range;
- numbered key sequence;
- source-order and normalized-number views;
- Base64 text;
- decoded compressed stream;
- chunk descriptors;
- Format80 payloads;
- exact decoded output;
- diagnostics and provenance.

The streams are paired only after both have been decoded to storage arrays.

## 2. Public-source agreement

### EA FinalSun / FinalAlert 2

The published editor separately concatenates values from `OverlayPack` and `OverlayDataPack`, separately Base64-decodes them, and separately calls its Format80 decoder. It owns distinct `m_Overlay` and `m_OverlayData` arrays.

Evidence grade: `ConfirmedByOfficialEditorSource`.

### OpenRA

The Gen2 map importer obtains each section independently, performs independent Base64 conversion, allocates separate `1 << 18` outputs, and invokes the LCW/Format80 path twice.

Evidence grade: `ConfirmedByIndependentImplementation`, with shared XCC/community ancestry warning.

### World-Altering Editor

WAE requires both sections before it constructs typed overlays, but the two sections are still individually collected, decoded, and stored. Its reader returns without overlays when either section is absent.

Evidence grade: `ConfirmedByIndependentImplementation`; missing-section behavior is implementation-specific.

### CNCMaps and MapTool

Both allocate and decode two independent arrays. CNCMaps returns early if either section is unavailable. MapTool collects two error results and only materializes overlay objects after both succeed.

Evidence grade: `ConfirmedByIndependentImplementation`, subject to code-lineage notes.

## 3. Numbered fragments

The numbered-fragment collector belongs below the lossless INI document and above Base64.

It must preserve:

- raw key spelling;
- parsed integer candidate;
- source order;
- duplicate normalized numbers;
- leading zeros;
- whitespace and comments in the lossless source;
- per-fragment source provenance;
- section identity.

The collector may produce a normalized numeric-order candidate, but it must not silently apply ordinary INI key override rules to duplicate normalized fragment numbers.

Cases requiring structured diagnostics include:

- empty section;
- missing key `1` under a one-based profile;
- numeric gaps;
- duplicate raw key;
- distinct raw keys that normalize to the same number, such as `1` and `01`;
- key `0`;
- negative, signed, or nonnumeric keys;
- invalid Base64 characters;
- conflicting source-order and numeric-order interpretations.

Fragment policy is shared infrastructure and is not owned by the Overlay parser.

## 4. Missing-section states

The document model must distinguish:

| State | Meaning |
|---|---|
| both present | both streams can proceed independently |
| type present, data absent | raw type storage may be preserved; semantic pairing unavailable |
| type absent, data present | raw data storage may be preserved; type binding unavailable |
| both absent | no Overlay packed documents were supplied |
| section present but empty | explicit empty source, not identical to absence |
| fragment collection failed | source exists but cannot produce a canonical compressed window |
| Base64 failed | compressed bytes unavailable |
| decode failed | compressed bytes preserved; decoded array unavailable |

No missing state authorizes creating a synthetic partner array.

## 5. Pairing boundary

After successful independent decoding, an `OverlayArrayConsistencyAnalysis` may compare:

- selected storage profile;
- expected and actual lengths;
- coordinate-index profile;
- type/data array availability;
- trailing and missing bytes;
- per-index raw combinations;
- registry availability;
- semantic-profile coverage.

The consistency analyzer reports; it does not repair.

## 6. Invalid automatic behaviors

The project must not:

- copy the length of one section to the other;
- synthesize an all-zero data array when `OverlayDataPack` is absent;
- synthesize an all-`0xFF` type array when `OverlayPack` is absent;
- drop one successfully decoded array because its partner failed;
- concatenate the two compressed streams;
- run one decoder over text from both sections;
- select fragment order by whichever output looks plausible;
- treat a map with no typed overlays as proof that both sections are optional at runtime.

## 7. Map-local INI composition interaction

The packed sections themselves are map-document sections. They are not merged with global `rulesmd.ini` packed sections. Lossless map parsing retains duplicate sections and keys; a packed-section selection policy must explicitly identify which map-local section occurrence participates.

This is separate from cross-layer semantic composition of `[OverlayTypes]` and Overlay object sections in Rules/Art.

## 8. Roundtrip layers

Roundtrip claims must be qualified:

- **source roundtrip:** lossless INI section text and fragment layout retained;
- **compressed roundtrip:** exact Base64-decoded compressed bytes retained;
- **decoded roundtrip:** exact decoded arrays retained;
- **semantic roundtrip:** bound type and derived semantics retained;
- **canonical rewrite:** data intentionally re-fragmented/recompressed;
- **FinalAlert reopen:** editor accepts rewritten map;
- **runtime acceptance:** original game accepts rewritten map.

Success at one layer does not prove another.