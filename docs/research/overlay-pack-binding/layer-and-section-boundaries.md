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

The streams are paired only after both have been decoded to storage arrays. Here, “independent” describes architectural section/stream ownership; it does not claim independent source lineages.

## 2. Public-source evidence

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalSun/FinalAlert 2 separately collects, Base64-decodes, Format80-decodes, and stores the two sections | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Confirms official-editor behavior only. The editor owns distinct `m_Overlay` and `m_OverlayData` arrays. | Preserve separate provenance and results. | `NotRun` |
| OpenRA obtains and decodes the two sections separately | `ImplementationSpecificBehavior` | OpenRA Gen2 importer | OpenRA's behavior is source-pinned, but shared XCC/community ancestry prevents treating it as an independent runtime discovery. | Keep as a comparison implementation. | `NotRun` |
| WAE individually collects, decodes, and stores both sections, and returns without typed overlays if either is absent | `ImplementationSpecificBehavior` | World-Altering Editor | The missing-section response belongs to WAE and is not a universal format rule. | Do not copy WAE's early return into the raw document model. | `NotRun` |
| CNCMaps and MapTool allocate and decode two arrays before materializing typed overlays | `Underconfirmed` | CNCMaps and MapTool | The two named tools corroborate the separation, but implementation independence and stock-runtime applicability are not established. | Preserve separate array results. | `NotRun` |
| Stock RA2/YR requires the exact same missing-section behavior as the reviewed tools | `Unresolved` | No original-runtime source located | Tools differ in when they return or materialize typed overlays. | Report section presence and failure separately. | `NotRun` |
| A failed or missing stream is never synthesized from its partner | `DefensiveDesign` | Project policy | This is a fail-closed preservation decision, not a runtime claim. | Never fabricate an all-zero or all-`0xFF` partner array. | `NotRun` |

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
| both present | both streams can proceed separately |
| type present, data absent | raw type storage may be preserved; semantic pairing unavailable |
| type absent, data present | raw data storage may be preserved; type binding unavailable |
| both absent | no Overlay packed documents were supplied |
| section present but empty | explicit empty source, not identical to absence |
| fragment collection failed | source exists but cannot produce a canonical compressed window |
| Base64 failed | compressed bytes unavailable |
| decode failed | compressed bytes preserved; decoded array unavailable |

No missing state authorizes creating a synthetic partner array.

## 5. Pairing boundary

After successful separate decoding, an `OverlayArrayConsistencyAnalysis` may compare:

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

The following are `DefensiveDesign` prohibitions. The project must not:

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
