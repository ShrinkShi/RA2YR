# Overlay pack binding test matrix

> **Research provenance:** This document was produced by ChatGPT Web from public sources. It did not read ProjectBaseline, is not a local Codex Agent artifact, and imports no code. GPL or unclear-license sources are reference-only; no source was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

## 1. Matrix scope

This design defines **124** research-driven tests. It is not executable test code.

Evidence/policy labels:

- `CF` — confirmed format/editor/implementation fact at the stated evidence level;
- `PP` — configured project policy;
- `DC` — defensive check;
- `UA` — unresolved assumption or source conflict.

A `CF` label does not imply official runtime source unless the cited conclusion explicitly has that grade.

## 2. Category totals

| Category | Count |
|---|---:|
| Fragment, chunk, and Format80 | 22 |
| Array length and layout | 18 |
| Coordinate, index, and domain | 22 |
| Registry binding | 18 |
| OverlayData semantic profiles | 18 |
| Resources, walls, and bridges | 14 |
| Safety, inputs, architecture, and audit | 12 |
| **Total** | **124** |

## F. Fragment, chunk, and Format80 (22)

| ID | Case | Class |
|---|---|---|
| T001 | Both sections use canonical 1..N fragments and decode exactly | `CF` |
| T002 | OverlayPack section absent while data section exists | `DC` |
| T003 | OverlayDataPack section absent while type section exists | `DC` |
| T004 | Both packed sections absent | `DC` |
| T005 | OverlayPack section present but empty | `DC` |
| T006 | OverlayDataPack section present but empty | `DC` |
| T007 | Fragments appear in reverse source order but normalize deterministically | `PP` |
| T008 | Fragments appear in randomized source order with stable numeric ordering | `PP` |
| T009 | Fragment numbering starts at 0 | `UA` |
| T010 | Numeric fragment gap | `UA` |
| T011 | Duplicate raw fragment key | `DC` |
| T012 | Normalized duplicate keys 1 and 01 | `DC` |
| T013 | Negative fragment key | `DC` |
| T014 | Nonnumeric fragment key | `DC` |
| T015 | Invalid Base64 character | `DC` |
| T016 | Invalid Base64 padding | `DC` |
| T017 | Truncated map-chunk header | `DC` |
| T018 | Truncated declared chunk payload | `DC` |
| T019 | 0/0 chunk header handled only by explicit envelope policy | `UA` |
| T020 | Explicit absolute-position Format80 profile decodes fixture | `PP` |
| T021 | Explicit backward-distance Format80 profile remains separate | `UA` |
| T022 | Missing Format80 terminator fails closed | `PP` |

## L. Array length and layout (18)

| ID | Case | Class |
|---|---|---|
| T023 | Both ordinary arrays decode to exactly 262144 bytes | `CF` |
| T024 | OverlayPack output is 262143 bytes | `PP` |
| T025 | OverlayPack output is 262145 bytes | `PP` |
| T026 | OverlayDataPack output is 262143 bytes | `PP` |
| T027 | OverlayDataPack output is 262145 bytes | `PP` |
| T028 | OverlayPack output is empty | `PP` |
| T029 | OverlayDataPack output is empty | `PP` |
| T030 | Decoded array lengths differ | `PP` |
| T031 | Declared output differs from actual codec production | `PP` |
| T032 | One decoded trailing byte is preserved and rejected | `PP` |
| T033 | One missing decoded byte is reported without padding | `PP` |
| T034 | Map Size does not shrink expected storage length | `CF` |
| T035 | LocalSize does not shrink expected storage length | `CF` |
| T036 | Explicit extended type profile accepts 524288-byte type array | `UA` |
| T037 | Extended profile still requires 262144-byte data array | `CF` |
| T038 | Ordinary profile rejects 524288-byte type output | `PP` |
| T039 | Extended profile rejects 262144-byte type output | `PP` |
| T040 | Partner array length never repairs failed array | `PP` |

## C. Coordinate, index, and domain (22)

| ID | Case | Class |
|---|---|---|
| T041 | Storage coordinate X=0,Y=0 maps to index 0 | `CF` |
| T042 | Storage coordinate X=511,Y=0 maps to index 511 | `CF` |
| T043 | Storage coordinate X=0,Y=511 maps to index 261632 | `CF` |
| T044 | Storage coordinate X=511,Y=511 maps to index 262143 | `CF` |
| T045 | X=512 is rejected | `DC` |
| T046 | Y=512 is rejected | `DC` |
| T047 | Negative X is rejected before unsigned conversion | `DC` |
| T048 | Negative Y is rejected before unsigned conversion | `DC` |
| T049 | External row-major profile uses X + 512Y | `CF` |
| T050 | Explicit swapped-axis profile uses Y + 512X | `UA` |
| T051 | A coordinate yielding different valid bytes under two profiles is ambiguous | `PP` |
| T052 | Parser does not auto-select axis by map-domain plausibility | `PP` |
| T053 | Nonempty storage cell outside full scenario domain is preserved | `PP` |
| T054 | Nonempty storage cell outside LocalSize is preserved | `PP` |
| T055 | Overlay storage cell without IsoMap record is preserved and diagnosed | `PP` |
| T056 | Empty type plus nonzero data outside map domain is retained | `PP` |
| T057 | Map-resize residual bytes are not cleaned during parse | `PP` |
| T058 | Object-section 1000Y+X identity is not used as array index | `CF` |
| T059 | Raw IsoMap coordinate requires explicit adapter before storage indexing | `PP` |
| T060 | Normalized diamond canvas coordinate requires explicit conversion | `PP` |
| T061 | Index multiplication and element-width arithmetic are checked | `DC` |
| T062 | Random input enumeration produces same normalized domain hash | `PP` |

## R. Overlay registry binding (18)

| ID | Case | Class |
|---|---|---|
| T063 | Raw type 0 binds ordinal 0 | `CF` |
| T064 | Raw type 0xFE binds ordinal 254 when present | `CF` |
| T065 | Raw type 0xFE remains unknown when ordinal 254 is absent | `PP` |
| T066 | Raw type 0xFF is empty under ordinary profile | `CF` |
| T067 | Registry gap is preserved | `PP` |
| T068 | Raw type targeting a gap returns MissingRegistryOrdinal | `PP` |
| T069 | Same-layer duplicate ordinal is distinct from cross-layer override | `PP` |
| T070 | Cross-layer ordinal override retains winner and suppressed provenance | `PP` |
| T071 | Keys 1 and 01 create normalized ordinal conflict | `DC` |
| T072 | Nonnumeric registry key is diagnosed | `DC` |
| T073 | Negative registry key is diagnosed | `DC` |
| T074 | Same logical name at two ordinals is retained and diagnosed | `PP` |
| T075 | Same ordinal with different names is ambiguous when composition is unresolved | `PP` |
| T076 | Name case conflict follows explicit comparison policy | `UA` |
| T077 | Missing logical Overlay section does not renumber later ordinals | `PP` |
| T078 | Missing Art/image does not shift registry IDs | `PP` |
| T079 | Map-local registry contribution retains composition provenance | `UA` |
| T080 | Extension registry is isolated from vanilla profile | `PP` |

## S. OverlayData semantic profiles (18)

| ID | Case | Class |
|---|---|---|
| T081 | Empty type with data 0 is classified conventionally empty | `CF` |
| T082 | Empty type with nonzero data is preserved and diagnosed | `PP` |
| T083 | Bound type with data 0 remains a valid typed raw pair | `CF` |
| T084 | Bound type with data 255 remains raw pending profile validation | `PP` |
| T085 | Unknown type with arbitrary data remains opaque | `PP` |
| T086 | Generic frame profile derives frame candidate without mutating raw byte | `CF` |
| T087 | Frame candidate beyond Art frame count is not clamped | `PP` |
| T088 | Resource type requires resource-specific profile | `PP` |
| T089 | Wall type requires wall-specific profile | `PP` |
| T090 | Bridge type requires bridge-specific profile | `PP` |
| T091 | Unknown family yields UnknownSemanticProfile | `PP` |
| T092 | Multiple applicable profiles produce structured ambiguity | `PP` |
| T093 | Type is not inferred from image availability | `PP` |
| T094 | Profile is not selected by raw-value plausibility | `PP` |
| T095 | Semantic adapter never rewrites type array | `PP` |
| T096 | Semantic adapter never rewrites data array | `PP` |
| T097 | Raw-to-derived trace retains evidence grade and source pins | `PP` |
| T098 | Only one decoded array available yields partial document, not typed success | `PP` |

## B. Resources, walls, and bridges (14)

| ID | Case | Class |
|---|---|---|
| T099 | Resource type/data pair is not treated directly as credits | `PP` |
| T100 | Resource stage candidate is not equated with remaining harvest value | `PP` |
| T101 | Growth/spread is outside storage parser | `PP` |
| T102 | Stored wall frame and derived neighbor candidate remain separate | `PP` |
| T103 | OverlayData is not universally a wall bitmask | `PP` |
| T104 | Wall health, owner, and pathing are not synthesized from packed bytes | `PP` |
| T105 | Low-bridge editor profile retains three-cell piece candidate | `CF` |
| T106 | High-bridge editor profile retains central/single-cell storage candidate | `UA` |
| T107 | OverlayData is not universally bridge damage state | `PP` |
| T108 | TMP/theater bridge art remains separate from bridge simulation | `PP` |
| T109 | Bridge control objects belong to upper runtime composition | `PP` |
| T110 | Water or shore placement does not auto-classify a bridge | `PP` |
| T111 | Extension-defined resource/wall/bridge profiles remain scoped | `PP` |
| T112 | Unknown family is not inferred from name substring or image appearance | `PP` |

## A. Safety, inputs, architecture, and audit (12)

| ID | Case | Class |
|---|---|---|
| T113 | Memory input produces canonical result | `PP` |
| T114 | Seekable Stream produces same result as Memory | `PP` |
| T115 | Short-read Stream produces same result and diagnostics | `PP` |
| T116 | Bounded MIX window produces same result and provenance | `PP` |
| T117 | Truncated input fails without partial success | `DC` |
| T118 | Valid overlapping Format80 copy follows selected bytewise semantics | `CF` |
| T119 | Invalid back-reference fails before reading output history | `DC` |
| T120 | Aggregate output budget is enforced | `DC` |
| T121 | Command, chunk, and diagnostic budgets are enforced | `DC` |
| T122 | No-progress codec or parser loop is stopped | `DC` |
| T123 | Fixture builder does not reuse production decoder/index formulas | `PP` |
| T124 | Future ProjectBaseline observation never auto-promotes to official runtime evidence | `PP` |

## 3. Cross-cutting assertions

Every successful parser test also asserts:

- exact bounded input-window behavior;
- checked offset and length arithmetic;
- deterministic diagnostics and provenance;
- no silent clamp, padding, truncation, or partial success;
- no file-driven unbounded allocation;
- no no-progress loop;
- raw bytes remain separate from derived interpretations;
- selected policies and evidence grades are serializable;
- no dependency on `UnityEngine`;
- no archive discovery, INI composition, Rules/Art loading, rendering, simulation, or pathfinding inside the codec/array layers.

## 4. Fixture independence

Synthetic fixture builders must not reuse production:

- fragment sorting;
- chunk parsing;
- Format80 command formulas;
- coordinate/index conversion;
- registry construction;
- semantic-profile selection.

Fixtures should be specified with literal byte windows, independently calculated expected indices, and canonical model hashes. Tests must include permutations of input enumeration and Stream read sizes.

## 5. Golden-audit boundary

Future ProjectBaseline observations can satisfy `ObservedByFutureProjectBaselineAudit` expectations, but cannot silently relabel a test as `ConfirmedByOfficialRuntimeSource`. Public golden outputs are limited by `baseline-audit-request.md`.
