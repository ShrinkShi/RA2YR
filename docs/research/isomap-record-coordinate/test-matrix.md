> **Source notice:** This research was prepared by **ChatGPT Web** from public sources. It did not read the local ProjectBaseline and is not the product of the local Codex Agent. No GPL or unclear-license code was copied, translated, mechanically rewritten, or imported (`code_imported: false`).

# Test matrix — 114 cases

## Test-intent labels

The compact codes in this matrix are test-planning categories, not formal evidence grades:

- `F` — exercise a record/tool-format fact supported by the cited source set.
- `C` — exercise a source conflict or cross-implementation comparison.
- `P` — exercise a project defensive policy or architecture requirement.
- `U` — exercise a deliberately unresolved assumption or experimental profile.

Formal evidence grades use the normalized closed vocabulary defined in `README.md` and `source-comparison.md`. Each implementation test should cite the relevant research claim rather than treating `F/C/P/U` as evidence strength.

Fixtures are synthetic and independently authored. Fixture builders must not reuse production parsing, coordinate conversion, sorting, tile interpretation, or binding formulas.

## 11-byte record layout — 20

| ID | Test intent | Case |
|---:|:---:|---|
| 1 | `F` | Single exact 11-byte record with all raw fields zero parses one record and no trailer. |
| 2 | `P` | Ten-byte decoded input returns truncated-record failure, not partial success. |
| 3 | `P` | Twelve-byte decoded input returns one record plus one-byte trailer classification. |
| 4 | `F` | Two concatenated 11-byte records preserve source ordinal and byte offsets. |
| 5 | `P` | Empty decoded input is classified separately from a missing section. |
| 6 | `F` | Little-endian X value 0x1234 is exposed as raw16 0x1234. |
| 7 | `F` | Little-endian Y value 0x5678 is exposed as raw16 0x5678. |
| 8 | `C` | X raw 0xFFFF exposes unsigned 65535 and signed -1 without choosing either. |
| 9 | `C` | Y raw 0x8000 exposes unsigned 32768 and signed -32768. |
| 10 | `F` | Tile bytes 4..7 preserve exact raw32 and original four bytes. |
| 11 | `F` | Low16 and high16 views are derived without modifying raw32. |
| 12 | `F` | SubTile byte at offset 8 remains unsigned 0..255. |
| 13 | `F` | Level byte at offset 9 remains unsigned 0..255. |
| 14 | `F` | Final byte at offset 10 remains raw and nonzero values are retained. |
| 15 | `P` | Reader uses explicit little-endian decoding independent of host endianness. |
| 16 | `P` | Record byte offset arithmetic is checked for overflow. |
| 17 | `P` | Maximum record-count budget is enforced before list allocation. |
| 18 | `P` | Diagnostic budget exhaustion yields aggregate diagnostic, not unbounded output. |
| 19 | `P` | Raw record bytes or equivalent bounded slice permit lossless field preservation. |
| 20 | `P` | Fixture bytes are authored independently and do not call production record writer. |

## Tile field views — 18

| ID | Test intent | Case |
|---:|:---:|---|
| 21 | `C` | Raw tile 0x00000001 yields full32=1, low16=1, high16=0. |
| 22 | `C` | Raw tile 0x0000FFFF preserves full32 65535 and low signed -1. |
| 23 | `C` | Raw tile 0x00010000 preserves full32 65536, low16 0, high16 1. |
| 24 | `C` | Raw tile 0x7FFFFFFF exposes signed max and unsigned same value. |
| 25 | `C` | Raw tile 0x80000000 exposes signed negative and unsigned 2147483648. |
| 26 | `C` | Raw tile 0xFFFFFFFF exposes signed -1, unsigned max, low/high 0xFFFF. |
| 27 | `P` | Nonzero high16 emits high-bits diagnostic without masking. |
| 28 | `P` | Low16 profile retains high16 as suppressed metadata. |
| 29 | `P` | Full32 profile retains low/high derived views in trace. |
| 30 | `P` | When low16 and full32 bind to different valid registry entries, result is ambiguous. |
| 31 | `P` | When only one interpretation binds, binder does not use success alone to select it. |
| 32 | `P` | Explicit full32 profile can produce GlobalTileId above 65535 without truncation. |
| 33 | `P` | Explicit split16 profile never sign-extends into stored raw32. |
| 34 | `C` | Official-editor candidate models bytes 6..7 separately from tile low16. |
| 35 | `C` | WAE candidate models bytes 4..7 as one signed Int32. |
| 36 | `P` | Tile 0xFFFF sentinel handling occurs after raw parse and is policy-labeled. |
| 37 | `P` | Out-of-registry raw tile remains preserved and returns structured failure. |
| 38 | `U` | Candidate alternate split profile can coexist without becoming default. |

## Coordinate domain — 20

| ID | Test intent | Case |
|---:|:---:|---|
| 39 | `F` | Dimensions W=1,H=1 produce expected normalized canvas count 1. |
| 40 | `F` | Dimensions W=2,H=1 produce expected normalized canvas count 3. |
| 41 | `F` | Dimensions W=2,H=2 produce expected normalized canvas count 6. |
| 42 | `P` | Zero width is rejected before coordinate allocation. |
| 43 | `P` | Zero height is rejected before coordinate allocation. |
| 44 | `P` | 2W-1 overflow is detected with checked arithmetic. |
| 45 | `P` | Canvas-count multiplication overflow is detected. |
| 46 | `F` | Canvas (dx=0,row=0) converts using explicit raw-coordinate formula. |
| 47 | `F` | Canvas rightmost column converts and roundtrips under the selected profile. |
| 48 | `F` | Raw-to-canvas-to-raw roundtrip holds for every independently enumerated valid fixture cell. |
| 49 | `P` | Parity-invalid raw coordinate is diagnosed and not rounded. |
| 50 | `P` | Negative signed-coordinate view is retained but fails positive-domain policy. |
| 51 | `P` | Large unsigned coordinate outside domain is retained and diagnosed. |
| 52 | `P` | Record in raw diamond blank region is out-of-domain, not clamped. |
| 53 | `P` | Record at each valid domain boundary is accepted exactly. |
| 54 | `C` | Axis-swapped candidate is reported separately and never auto-selected. |
| 55 | `F` | LocalSize changes do not change raw full-domain record count. |
| 56 | `P` | Nonzero first two Size values are retained as map metadata and not silently added to raw coordinates. |
| 57 | `P` | Coordinate result uses scalar Core types, not Unity Vector types. |
| 58 | `P` | Fixture coordinate generator uses independent formulas from production analyzer. |

## Density, order, and duplicates — 20

| ID | Test intent | Case |
|---:|:---:|---|
| 59 | `F` | Complete unique coordinate set classifies dense. |
| 60 | `C` | Source record shuffle preserves coordinate index and aggregate semantic hash. |
| 61 | `P` | Source order remains available after coordinate indexing. |
| 62 | `C` | Sparse set omitting one default coordinate reports exactly one missing coordinate. |
| 63 | `P` | Missing coordinate is not materialized as explicit default during parsing. |
| 64 | `C` | Explicit tile0/subtile0/level0 record remains distinct from missing coordinate. |
| 65 | `P` | Duplicate byte-identical records form a duplicate group. |
| 66 | `P` | Duplicate semantic-identical records with differing final byte form a raw conflict group. |
| 67 | `P` | Duplicate coordinates with different tile bytes are conflicting and have no winner. |
| 68 | `P` | Duplicate coordinates with different SubTile are conflicting. |
| 69 | `P` | Duplicate coordinates with different Level are conflicting. |
| 70 | `P` | Duplicate coordinates with different final byte are conflicting. |
| 71 | `P` | First-wins compatibility profile is opt-in and traceable. |
| 72 | `P` | Last-wins compatibility profile is opt-in and traceable. |
| 73 | `P` | Default project policy fails closed for conflicting duplicates. |
| 74 | `P` | Out-of-domain records remain in source order but outside effective index. |
| 75 | `P` | Record count above theoretical canvas is analyzed for duplicates/out-of-domain, not rejected solely by count. |
| 76 | `C` | Record count below theoretical canvas can classify sparse only after coordinate analysis. |
| 77 | `C` | Dense count with one duplicate and one missing cell does not classify dense-unique. |
| 78 | `P` | Duplicate-group and missing-set budgets produce bounded aggregate results. |

## Decoded trailer and length contract — 14

| ID | Test intent | Case |
|---:|:---:|---|
| 79 | `F` | Decoded length exactly 11 has no trailer. |
| 80 | `F` | Decoded length exactly 22 has two records and no trailer. |
| 81 | `C` | Decoded length 15 yields one record plus four-byte trailer candidate. |
| 82 | `C` | Four zero trailer bytes classify all-zero candidate. |
| 83 | `C` | Four nonzero trailer bytes are retained and diagnosed. |
| 84 | `P` | One-byte trailer is not silently dropped. |
| 85 | `P` | Ten-byte trailer is not treated as a complete record. |
| 86 | `P` | Trailer policy requiring four zeros rejects nonzero four-byte tail. |
| 87 | `P` | Exact-records-only profile rejects any remainder. |
| 88 | `P` | Forensic profile returns raw trailer with incomplete semantic status. |
| 89 | `P` | Chunk-envelope 0/0 sentinel fixture is not passed to record trailer parser as decoded bytes. |
| 90 | `P` | Four decoded zeros inside final LZO block remain stream trailer candidate, not chunk header. |
| 91 | `P` | Empty decoded stream and four-byte-only stream have distinct classifications. |
| 92 | `P` | Partial record output is never marked complete success. |

## Theater tile binding — 12

| ID | Test intent | Case |
|---:|:---:|---|
| 93 | `P` | GlobalTileId at start of first range binds TileSet0 index0. |
| 94 | `P` | GlobalTileId at end-exclusive boundary binds next range, not previous. |
| 95 | `P` | Out-of-range GlobalTileId returns binding failure without replacement. |
| 96 | `P` | Missing TileSet range produces registry diagnostic. |
| 97 | `P` | Missing TMP keeps registry range and later GlobalTileIds unchanged. |
| 98 | `P` | Reserved range remains distinct from missing TMP. |
| 99 | `P` | Valid SubTile indexes populated TMP slot. |
| 100 | `P` | SubTile equal to slot count is out-of-range. |
| 101 | `P` | SubTile in range but offset-table slot zero reports empty slot. |
| 102 | `P` | Variation candidates do not change GlobalTileId or TileIndexInSet. |
| 103 | `P` | .ubn to .urb fallback is inactive in vanilla profile and explicit in editor profile. |
| 104 | `P` | Binding trace retains winner, suppressed candidates, evidence grade, and provenance. |

## Input modes, safety, and architecture — 10

| ID | Test intent | Case |
|---:|:---:|---|
| 105 | `P` | Memory input and seekable Stream produce identical document hash. |
| 106 | `P` | Short-read Stream produces identical result while making progress. |
| 107 | `P` | MIX entry window cannot read before or after its bounded window. |
| 108 | `P` | Truncated Stream returns structured diagnostic without infinite retry. |
| 109 | `P` | No-progress Stream read terminates under protection. |
| 110 | `P` | Decoded-byte and record budgets prevent input-driven unbounded allocation. |
| 111 | `P` | Coordinate, duplicate, trace, and diagnostic budgets are independently enforced. |
| 112 | `P` | Record reader does not call Base64, LZO, INI, TMP, or Unity services. |
| 113 | `P` | Shuffled provider/filesystem enumeration does not change registry binding result. |
| 114 | `P` | ProjectBaseline audit labels status and future source separately from original-runtime evidence. |

## Required assertions across the matrix

- Exact raw-byte preservation is tested independently from semantic success.
- No test accepts clamp, zero-fill, silent truncation, unreported trailing bytes, or partial success.
- Every loop either advances input/output state or fails under no-progress protection.
- Every allocation is preceded by checked size and budget validation.
- Duplicate and suppressed candidates retain complete provenance.
- Memory, Stream, short-read Stream, and MIX-window modes use the same state machine.
- Directory, provider, dictionary, and record input enumeration order cannot alter normalized results.
- Production sorters and transforms are not reused to generate expected fixture values.
- ProjectBaseline remains `AuditStatus: NotRun` until execution and never upgrades itself to original-runtime proof.

## Distribution check

| Category | Count |
|---|---:|
| 11-byte record layout | 20 |
| Tile field views | 18 |
| Coordinate domain | 20 |
| Density/order/duplicates | 20 |
| Decoded trailer/length | 14 |
| Theater tile binding | 12 |
| Input/safety/architecture | 10 |
| **Total** | **114** |
