# Format80 / LCW variant conflict matrix

## 1. Variant dimensions

Independent dimensions must not be collapsed:

1. command byte classes;
2. medium/long reference field interpretation;
3. optional stream marker;
4. output direction/API convention;
5. outer map chunk envelope;
6. caller-provided output length;
7. tolerance of malformed input.

## 2. Source matrix

| Source | Pin/path | Command model | Position mode | Terminator | Length contract | Notable tolerance |
|---|---|---|---|---|---|---|
| ModdingWiki Format80 | permanent revision `oldid=12721` | five classes; optional initial relative marker described | absolute or relative selected by marker/context | `0x80` | output often caller/context supplied | descriptive, not executable |
| OpenRA | `a520984...`; `LCWCompression.cs` | five classes | bool selects absolute or relative for medium/long; short is relative | `0x80` | destination array supplied | one overflow path returns partial output; other invalid states throw generic exceptions |
| WAE | `b4c948...`; `Format80.cs` | five classes | absolute medium/long only | `0x80` | unsafe destination pointer | no input/output window checks; return-value defect is ignored by caller |
| OpenRA map importer | same pin; `ImportGen2MapCommand.cs` | delegates to OpenRA LCW | default absolute | payload terminator expected implicitly | chunk header declares output; decoder not bounded to compressed size | copies declared output even if decoder result differs |
| WAE map Format5 | same WAE pin; `Format5.cs` | delegates to WAE Format80 | absolute | decoder terminator | chunk header declares output | breaks if either size field is zero; unsafe payload reads |
| EA FinalAlert/FinalSun | `6abf0f...`; `MissionEditorPackLib.cpp` plus XCC lineage | `encode80/decode5` integration | inherited XCC behavior | payload-specific | pre-scans chunk size headers | aggregate-size check but exact per-block status is not exposed |
| XCC Utilities 1.46 | SourceForge release; GPL-2.0 lineage | historical reference implementation | absolute/relative APIs exist in lineage | standard candidate | tool-specific | reference-only; exact mirror/release file mapping unresolved |
| Chrono Divide | `5943c4...` public mod SDK | no public pinned codec implementation located | no vote | no vote | no vote | documentation is not compression evidence |
| CnCNet client | `e6e367...`; preview extractor | LZO only in inspected path | N/A | N/A | expected preview output supplied | single stream read not independently proven exact |

## 3. Absolute versus relative medium/long copies

The core conflict is not the short-copy command: it is always a backward reference. The conflict applies to the two-byte field in medium and long copies.

### Absolute candidate

```text
sourceIndex = field
```

The source is relative to the beginning of decoded output.

### Relative candidate

```text
sourceIndex = currentOutput - field
```

The field is a backward distance.

The parser must not infer the variant by trying both and selecting whichever succeeds. Variant selection belongs to the caller/profile and remains in provenance.

## 4. Initial marker

Community descriptions report an initial `0x00` marker for a relative-copy stream. This is in tension with the ordinary command table because `0x00` is also structurally a short copy with distance zero. A marker-aware profile may consume it only at byte zero before command decoding.

OpenRA exposes a `reverse`/relative boolean externally rather than detecting the marker in its decoder. WAE’s inspected map path uses absolute interpretation and has no marker handling. Original RA2/YR OverlayPack marker behavior remains underconfirmed.

## 5. Forward and reverse APIs

“Reverse” may mean:

- reference fields are backward distances;
- output is decoded into the end of a buffer;
- input is traversed differently;
- a caller prepositions the destination.

These are not interchangeable. Core stores them as separate flags/candidates and initially supports only forward output with explicit absolute or backward-distance references for the map profile.

## 6. Shared ancestry warning

- WAE Format80 is derived from old OpenRA/XCC community code.
- EA’s released editor integrates XCC code.
- CnCNet and WAE share ecosystem components.

Agreement among these repositories does not count as fully independent evidence.

## 7. Strict project default

For RA2/YR map Overlay packs, the proposed experimental default is:

- independent chunk payload;
- forward output;
- absolute medium/long positions;
- short references relative;
- required `0x80` terminator;
- exact declared output;
- exact compressed-window consumption;
- no partial success.

This remains `ConfiguredProjectPolicy` until multiple local stock samples and independent evidence distinguish the variant.
