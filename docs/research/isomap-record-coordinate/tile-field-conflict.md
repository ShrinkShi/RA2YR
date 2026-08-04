> **Source notice:** This research was prepared by **ChatGPT Web** from public sources. It did not read the local ProjectBaseline and is not the product of the local Codex Agent. No GPL or unclear-license code was copied, translated, mechanically rewritten, or imported (`code_imported: false`).

# Bytes 4..7 — tile field conflict

## The conflict

The four bytes at record offsets `4..7` have two materially different public interpretations.

### Candidate A — one 32-bit tile index

```text
bytes 4..7 = little-endian signed or unsigned 32-bit GlobalTileId candidate
```

Observed in:

- World-Altering Editor: signed `Int32 TileIndex`, reader and writer;
- CNCMaps: signed `Int32 TileNum`, reader and writer;
- MapTool: signed `Int32 TileIndex`, reader and writer.

This candidate can represent values above `65535`, but public implementation capability is not proof that stock RA2/YR accepts those values.

### Candidate B — 16-bit tile plus two independent bytes

```text
bytes 4..5 = 16-bit tile / ground candidate
byte  6    = raw metadata / reserved / zero candidate
byte  7    = raw metadata / reserved / zero candidate
```

Observed in:

- EA FinalSun/FinalAlert 2 official editor: `WORD wGround` followed by two bytes retained through its adjacent map metadata field;
- XCC: signed 16-bit `tile`, then `zero1` and `zero2`;
- OpenRA importer: unsigned 16-bit tile, then one ignored signed 16-bit value.

This candidate does not define values above the chosen 16-bit interpretation.

### Candidate C — another split

No reviewed source established a third exact semantic split, but the raw bytes allow one. Examples that remain unresolved include:

- low tile bits plus high flags;
- tile plus theater/variation bits;
- tile plus editor-only metadata;
- two 16-bit values with non-tile semantics in the high half.

The project must not exclude Candidate C before aggregate evidence exists.

## Source matrix

| Source | Read view | Write view | High 16 behavior | Evidence role |
|---|---|---|---|---|
| EA FinalSun/FinalAlert 2 | `u16 wGround` + two raw bytes | preserves ground and adjacent metadata | preserved, not named as tile high bits | official editor |
| XCC | `i16 tile` + `u8 zero1` + `u8 zero2` | writes zero fields in conversion paths | canonicalized to zero | tool, XCC lineage |
| OpenRA importer | `u16 tile` + ignored `i16` | no stock-map writer in cited path | ignored | reimplementation/importer |
| WAE | `i32 TileIndex` | writes `i32` | used as high half of integer | modern editor |
| CNCMaps | `i32 TileNum` | writes `i32` | used as high half of integer | renderer/tool |
| MapTool | `i32 TileIndex` | writes `i32` | used as high half of integer | map tool |

## Why source count is not a vote

WAE, CNCMaps, and MapTool share community knowledge and may share implementation lineage. XCC is reused or cited by other tools. Agreement among descendants is useful compatibility evidence, but not independent proof of original runtime semantics.

EA's source is official editor source, not game runtime source. It has stronger provenance for FinalAlert behavior but still cannot settle stock runtime interpretation by itself.

## Required raw views

```text
IsoMapTileFieldViews
  Raw32LittleEndianBytes
  Unsigned32
  Signed32
  LowUnsigned16
  LowSigned16
  HighUnsigned16
  HighSigned16
  Byte6Raw
  Byte7Raw
```

A semantic result is separate:

```text
IsoMapTileInterpretation
  ProfileId
  EvidenceGrade
  InterpretedGlobalTileId?
  RetainedHighMetadata
  Diagnostics
```

## Signedness

The Core must not choose signedness while reading bytes.

Examples:

- `0x0000FFFF` is `65535` under both 32-bit signed and unsigned views, but may be `-1` under low signed 16-bit.
- `0x00010000` is `65536` as 32-bit, low 16 is zero, high 16 is one.
- `0xFFFFFFFF` is `4294967295` unsigned, `-1` signed, low/high 16 both `0xFFFF`.

Each view must remain available to diagnostics and audit.

## Values above 65535

Public 32-bit writers can serialize them. This proves only implementation capability.

The following remain unresolved:

- whether stock RA2/YR reads all 32 bits;
- whether the engine masks to 16 bits;
- whether high bits have another purpose;
- whether FinalAlert truncates or preserves such values on reopen;
- whether any stock or official map contains a nonzero high half.

Current theater size, common mod size, or practical TMP count cannot establish the binary field width.

## Sentinel handling

Several tools map `0xFFFF` or values at/above `65535` to tile zero. These are repair or renderer policies. The raw reader must not do this.

A later semantic profile may classify:

- low16 `0xFFFF` as empty/clear candidate;
- raw32 `0xFFFFFFFF` as sentinel candidate;
- out-of-registry values as unresolved references.

The original value remains retained in every case.

## Project policy

`ConfiguredForProjectPolicy`:

1. retain all four bytes;
2. publish both complete and split views;
3. do not mask to 16 bits;
4. do not sign-extend a 16-bit view into the stored raw value;
5. do not select the interpretation by checking which one resolves in the current theater;
6. do not use SHA, file length, TMP count, or local installation facts to choose a winner;
7. require an explicit interpretation profile with evidence grade;
8. emit `TileFieldInterpretationAmbiguous` when multiple permitted interpretations produce different IDs;
9. preserve the losing interpretations in the resolution trace.

## Golden evidence needed

A future sanitized ProjectBaseline audit should report only aggregates:

- count of records with nonzero high16;
- count by high16 range;
- count where raw32 and low16 resolve differently;
- count where each candidate binds to a theater registry range;
- canonical hashes of interpretation classifications;
- no per-record tile values or coordinates.
