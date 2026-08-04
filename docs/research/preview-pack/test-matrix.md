# PreviewPack research test matrix — 120 cases

> Source notice: designed by **ChatGPT Web** from public research; no local `ProjectBaseline`; not a Codex Agent artifact; no GPL or unclear-license code copied, translated, mechanically rewritten, or imported (`code_imported: false`).

This is a design matrix, not test implementation. Synthetic fixtures contain no original map pixels.

## Basis labels

- `Fact` — confirmed by official editor or executable public implementation.
- `Conflict` — public sources disagree.
- `Policy` — defensive project requirement.
- `Unresolved` — hypothesis requiring future evidence.

## A. Metadata and `Size=` — 20

| ID | Scenario | Expected design result | Basis |
|---|---|---|---|
| M01 | one `[Preview]`, `Size=0,0,1,1` | four raw fields; width/height candidate 1×1 | Fact |
| M02 | `Size=0,0,106,61` | valid ordinary dimensions; no dummy inference without payload identity | Fact |
| M03 | nonzero field 0 | preserve; do not force zero | Conflict |
| M04 | nonzero field 1 | preserve; do not force zero | Conflict |
| M05 | both first fields nonzero | preserve `OffsetAndDimensions` candidate | Unresolved |
| M06 | field 0 negative | raw parse succeeds; selected standard profile diagnoses origin | Policy |
| M07 | field 1 negative | raw parse succeeds; no clamp | Policy |
| M08 | width zero | invalid dimensions; no allocation | Fact/Policy |
| M09 | height zero | invalid dimensions; no allocation | Fact/Policy |
| M10 | width negative | invalid dimensions; no absolute-value repair | Policy |
| M11 | height negative | invalid dimensions | Policy |
| M12 | three fields | malformed metadata | Policy |
| M13 | five fields | preserve raw; standard profile rejects | Policy |
| M14 | empty `Size` | metadata present but invalid | Policy |
| M15 | noninteger field | token-specific diagnostic | Policy |
| M16 | signed integer overflow | checked parse failure | Policy |
| M17 | duplicate `Size`, identical | preserve both; duplicate diagnostic | Policy |
| M18 | duplicate `Size`, conflicting | no implicit same-document winner | Policy |
| M19 | duplicate `[Preview]` sections | preserve occurrences; selection required | Unresolved/Policy |
| M20 | extreme valid integers causing product overflow | fail before allocation | Policy |

## B. Fragments, Base64, chunk envelope, and LZO — 22

| ID | Scenario | Expected design result | Basis |
|---|---|---|---|
| P01 | keys `1..N` in physical/numeric order | deterministic aggregate stream | Fact |
| P02 | keys physically shuffled | policy-defined numeric result; physical order retained | Conflict |
| P03 | physical order required comparison profile | separate result, no auto-selection | Unresolved |
| P04 | missing key in sequence | gap diagnostic; no silent termination | Policy |
| P05 | key `0` | diagnosed; rejected by standard policy | Unresolved/Policy |
| P06 | leading-zero key `01` | raw preserved; noncanonical diagnostic | Policy |
| P07 | keys `1` and `01` | duplicate normalized group | Policy |
| P08 | duplicate key `1`, identical values | both occurrences preserved; ambiguous collection | Policy |
| P09 | duplicate key `1`, conflicting values | no ordinary INI override | Policy |
| P10 | nonnumeric key | excluded from standard stream with diagnostic | Policy |
| P11 | empty fragment value | preserved; profile decides validity | Policy |
| P12 | 70-character lines | accepted writer convention | Fact |
| P13 | longer line within total budget | accepted unless target profile restricts wrapping | Policy |
| P14 | aggregate invalid Base64 character | strict failure, no partial bytes | Policy |
| P15 | Base64 padding in middle | strict failure | Policy |
| P16 | valid padding only at aggregate end | success | Fact |
| P17 | chunk header truncated 1–3 bytes | envelope failure | Policy |
| P18 | payload shorter than compressed size | truncated payload | Policy |
| P19 | exact one block | exact input/output success | Fact |
| P20 | multiple blocks with final short block | exact aggregate success | Fact |
| P21 | `0/0` block | explicit sentinel profile required | Unresolved |
| P22 | one size zero, other nonzero | malformed under strict policy | Conflict/Policy |

## C. Decoded length and budgets — 16

| ID | Scenario | Expected design result | Basis |
|---|---|---|---|
| L01 | exact `w×h×3` | `LengthStatus=Exact` | Fact |
| L02 | one byte short | underflow failure | Policy |
| L03 | one byte long | trailing-byte failure | Policy |
| L04 | arbitrary 1–10 byte tail | preserve bounded tail classification; no success | Policy |
| L05 | empty decoded stream for positive dimensions | underflow | Policy |
| L06 | width multiplication overflow | checked failure | Policy |
| L07 | pixel count within range, times-three overflow | checked failure | Policy |
| L08 | width above independent limit | budget failure before decode | Policy |
| L09 | height above independent limit | budget failure before decode | Policy |
| L10 | pixel count above limit | budget failure | Policy |
| L11 | compressed aggregate above limit | reject before allocation | Policy |
| L12 | chunk count above limit | bounded failure | Policy |
| L13 | one block output above per-block limit | reject header before backend | Policy |
| L14 | codec short output | block failure; no zero-fill | Conflict/Policy |
| L15 | codec extra output | block failure | Policy |
| L16 | exact output reached with trailing compressed bytes | failure, no ignored tail | Policy |

## D. Channel order — 14

| ID | Scenario | Expected design result | Basis |
|---|---|---|---|
| C01 | one pixel bytes `01 02 03`, RGB profile | derived R=1,G=2,B=3 | Fact candidate |
| C02 | same bytes, BGR profile | derived B=1,G=2,R=3 | Community conflict |
| C03 | profile unspecified | raw pixel available; semantic color unavailable | Policy |
| C04 | official-editor RGB fixture | RGB interpretation matches declared expected tuple | Fact |
| C05 | WAE comment versus assignment | conflict diagnostic retained | Conflict |
| C06 | CnCNet RGB-to-BGR adapter | packed identity unchanged | Fact |
| C07 | CNCMaps misleading local names | API memory-order annotation required | Conflict |
| C08 | MapTool helper not inspected | no independent channel vote | Unresolved |
| C09 | automatic RGB/BGR trial prohibited | deterministic profile-required failure | Policy |
| C10 | color-plausibility detector attempted | architecture test rejects dependency | Policy |
| C11 | alpha-255 consumer view | derived adapter result; raw remains 3 bytes | Policy |
| C12 | palette lookup attempted in Core | forbidden dependency | Policy |
| C13 | gamma/sRGB transform attempted in Core | forbidden | Policy |
| C14 | cache key differs by channel profile | distinct semantic cache identities | Policy |

## E. Row order and coordinate interpretation — 14

| ID | Scenario | Expected design result | Basis |
|---|---|---|---|
| R01 | 2×2 explicit top-down bytes | `RowMajorTopDown` coordinate mapping | Fact candidate |
| R02 | same bytes bottom-up profile | distinct derived mapping | Unresolved candidate |
| R03 | column-major profile | explicit comparison only | Unresolved |
| R04 | row profile unknown | no pixel-coordinate interpretation | Policy |
| R05 | official DIB bottom-up source conversion | encoded destination is top-down candidate | Fact |
| R06 | CnCNet row loop | no Core vertical flip | Fact |
| R07 | WAE texture linear order | preserve API order; no runtime conclusion | Unresolved |
| R08 | payload width not 4-byte aligned | no source scanline padding | Fact |
| R09 | consumer bitmap adds stride bytes | adapter-only bytes excluded from decoded identity | Fact |
| R10 | metadata origin nonzero | no payload offset applied automatically | Policy |
| R11 | Unity texture appears inverted | adapter correction only | Policy |
| R12 | automatic vertical trial prohibited | profile remains explicit | Policy |
| R13 | row boundary crosses LZO block | aggregate pixels unchanged | Fact/Policy |
| R14 | block ends mid-pixel | aggregate interpretation succeeds only after full decode | Policy |

## F. Missing, fabrication, and section order — 14

| ID | Scenario | Expected design result | Basis |
|---|---|---|---|
| S01 | both sections absent | `BothSectionsMissing` | Policy |
| S02 | metadata only | `MetadataOnly` | Policy |
| S03 | payload only | `PayloadOnly`; no guessed dimensions | Policy |
| S04 | both present but empty | explicit empty statuses | Policy |
| S05 | valid metadata, corrupt payload | payload failure retained | Policy |
| S06 | corrupt metadata, decodable payload | no pixel interpretation | Policy |
| S07 | WAE fixed hidden payload | candidate recognized, bytes preserved | Fact |
| S08 | editor chooses dummy generation | generated artifact separate from source | Fact/Policy |
| S09 | editor chooses map rerender | generated provenance required | Policy |
| S10 | `[Preview]` and `[PreviewPack]` first | accepted placement category | Fact tool behavior |
| S11 | sections after `[Basic]` | accepted placement category | Fact tool behavior |
| S12 | sections in middle/end | lossless parse; runtime acceptance unresolved | Unresolved |
| S13 | PreviewPack not adjacent to Preview | physical relation retained | Unresolved |
| S14 | duplicate section occurrences separated | no implicit merge | Policy |

## G. Consumers, round-trip, safety, and audit — 20

| ID | Scenario | Expected design result | Basis |
|---|---|---|---|
| A01 | CnCNet missing preview | consumer null result, Core source status unchanged | Fact |
| A02 | UI fallback icon | adapter artifact only | Policy |
| A03 | consumer vertical flip | transform provenance retained | Policy |
| A04 | consumer scale-to-fit | decoded identity unchanged | Policy |
| A05 | consumer crop | display identity differs from pixel identity | Policy |
| A06 | bilinear versus nearest | consumer setting only | Policy |
| A07 | opaque alpha insertion | adapter-only | Policy |
| A08 | stale preview versus regenerated map | discrepancy report; no repair | Policy |
| A09 | preview depicts unrelated image | structurally valid remains valid | Policy |
| A10 | source-preserving no-op save | physical sections/fragments/bytes unchanged | Policy |
| A11 | decoded-preserving recompress | decoded hash equal; compressed/text identity differs | Policy |
| A12 | canonical rewrite changes line wrapping | fragment identity differs | Policy |
| A13 | row/channel profile change without byte change | semantic identity differs | Policy |
| A14 | Memory input | baseline result | Policy |
| A15 | seekable Stream | identical to Memory | Policy |
| A16 | one-byte short-read Stream | identical state machine/result | Policy |
| A17 | bounded MIX window | identical result and bounded consumption | Policy |
| A18 | no-progress backend | controlled failure, no loop | Policy |
| A19 | diagnostic limit exceeded | capped diagnostics and terminal summary | Policy |
| A20 | sanitized audit output review | no pixels, names, per-map tuples, or reconstructable data | Policy |

## Count

```text
Metadata/Size                         20
Fragments/Base64/chunk/LZO            22
Decoded length/budgets                16
Channel order                         14
Row order/coordinates                 14
Missing/fabrication/section order     14
Consumer/roundtrip/safety/audit       20
                                      ---
Total                                120
```

## Required fixture discipline

- no original PreviewPack bytes;
- no screenshot or thumbnail fixtures;
- tiny synthetic component values only;
- fixture encoders do not share production decoder code or formulas;
- expected channel/row mappings are written explicitly;
- randomized stream read segmentation cannot change results;
- no test creates Unity types.

## Compatibility discipline

Passing this matrix does not change `docs/compatibility/matrix.yml`. Original runtime and editor acceptance require later evidence and explicit review.