# VXL/HVA synthetic test matrix

> Total: **96 cases**. No original voxel body, matrix sequence or reconstructable geometry is required.

## Labels

- **F — confirmed format fact:** directly supported by multiple strong sources or exact packed layout.
- **D — defensive check:** safety/fail-closed behavior; not claimed as stock-runtime rejection behavior.
- **U — unconfirmed hypothesis:** distinguishes competing interpretations and must not promote compatibility by itself.

## A. VXL file and section layout — 24 cases

| ID | Basis | Case | Expected contract |
|---|---|---|---|
| VXL-001 | F | Minimum one-section VXL with 802-byte header, one 28-byte header, bounded body and one 92-byte tailer | Parses ordered raw model |
| VXL-002 | U | Structurally exact zero-section VXL | Parses safely with legality-unconfirmed diagnostic |
| VXL-003 | F | Single section with nonzero dimensions | Header/tailer candidate pair retained |
| VXL-004 | F | Multiple ordered sections | All names, ordinals and tailers preserve file order |
| VXL-005 | D | Input truncated inside 802-byte header | Fails at exact field/offset |
| VXL-006 | F | Identifier does not start with `Voxel Animation` | Rejects target family without trying unrelated VXL family |
| VXL-007 | U | Palette count differs from common value 1 | Raw value retained; unexpected-count diagnostic |
| VXL-008 | F | Remap bytes 16/31 and raw word `0x1f10` | Both byte and word views stable |
| VXL-009 | D | Section-header and tailer counts differ | No unsafe ordinal pairing; structured mismatch |
| VXL-010 | D | Section count exceeds budget | Fails before arrays/records allocation |
| VXL-011 | D | Declared body crosses input end | Fails before tailer read |
| VXL-012 | D | Body size exceeds input/allocation budget | No snapshot or body allocation |
| VXL-013 | F | 16-byte NUL-padded section name | Raw bytes and decoded candidate preserved |
| VXL-014 | D | Duplicate section names | All sections retained; duplicate diagnostic |
| VXL-015 | D | Duplicate section numbers | No winner or reorder; diagnostic |
| VXL-016 | U | Section metadata `Unknown1/Unknown2` differs from 1/2 | Raw values retained; no fixed-value rewrite |
| VXL-017 | D | Tailer truncated at every field boundary | Exact failure without partial tailer model |
| VXL-018 | U | Zero dimension section and all-empty nonzero section | Distinct classifications retained |
| VXL-019 | D | Dimension exceeds configured limit | Fails before column-table arithmetic |
| VXL-020 | D | `sizeX × sizeY × sizeZ` checked overflow/budget breach | No dense allocation or wraparound |
| VXL-021 | D | All scale/transform/bound floats finite | Raw bits and finite candidates stable |
| VXL-022 | D | Componentwise min bound greater than max | Values retained with inverted-bounds diagnostic |
| VXL-023 | D | NaN/Infinity in scale, transform or bounds | Invalid numeric diagnostic; no propagation to adapter |
| VXL-024 | D | Tailer tables/data point outside declared body or overlap another section | Fails/diagnoses before span decode |

## B. VXL span directory and decoder — 28 cases

| ID | Basis | Case | Expected contract |
|---|---|---|---|
| SPN-001 | F | Empty column `start=-1,end=-1` | No span read; zero voxels |
| SPN-002 | D | `-1` on only one of start/end | Inconsistent-empty failure |
| SPN-003 | D | Negative offset below `-1` | Invalid signed offset; no unsigned reinterpretation |
| SPN-004 | F | One dense chunk covering exact Z | Decodes color/normal pairs and duplicate count |
| SPN-005 | F | Multiple chunks with internal holes | Skip/count sequence yields sparse ordered Z positions |
| SPN-006 | F/U | Final `skip>0,count=0,duplicate=0` reaches `sizeZ` | Accepted candidate; separately counted for golden audit |
| SPN-007 | D | `skip=0,count=0` before completion | No-progress failure; no loop |
| SPN-008 | D | Start or end table truncated | Fails before entry enumeration |
| SPN-009 | D | Body-relative table/data addition overflows | Checked arithmetic failure |
| SPN-010 | F/D | Nonempty end below start | Reversed inclusive range failure |
| SPN-011 | D | Column range crosses span-data/body bound | Bounded-range failure |
| SPN-012 | D | Column range points into start/end table | Ownership failure |
| SPN-013 | U | Two columns use exact same nonempty range | Preserve alias diagnostic; no shared mutable decode |
| SPN-014 | D | Two nonempty ranges partially overlap | Reject unresolved overlap |
| SPN-015 | F/D | Disjoint ranges appear in descending/nonmonotonic column order | Decode by explicit ranges; preserve order diagnostic only |
| SPN-016 | D | Range ends after skip byte but before count | Truncated command failure |
| SPN-017 | D | `count` records exceed remaining range | Fails before reading incomplete voxel record |
| SPN-018 | D | Duplicate count byte absent | Exact truncation failure |
| SPN-019 | D/U | Trailing duplicate count differs from leading count | Fail-closed mismatch; stock tolerance remains unconfirmed |
| SPN-020 | D | `z + skip` exceeds `sizeZ` | No clamp; overflow diagnostic |
| SPN-021 | D | `z + skip + count` exceeds `sizeZ` | No partial run; overflow diagnostic |
| SPN-022 | D/U | Input exactly exhausted before logical Z reaches `sizeZ` | Underfilled-column failure, no implicit air padding |
| SPN-023 | D/U | Logical Z reaches `sizeZ` with bytes remaining in inclusive range | Trailing-column-data failure |
| SPN-024 | D | Dense section at configured voxel budget | Completes without dense-volume amplification |
| SPN-025 | D | Extremely sparse maximum-dimension section | Allocation proportional to columns + stored voxels, within limits |
| SPN-026 | D | Cumulative stored voxel count would exceed bounds volume or budget | Fails before append/allocation |
| SPN-027 | F | Color indices 0 and 255 | Preserved as raw colors; occupancy unaffected |
| SPN-028 | F | Normal indices 0 and 255 | Preserved raw; table-range validation deferred to normal layer |

## C. Normal and palette behavior — 10 cases

| ID | Basis | Case | Expected contract |
|---|---|---|---|
| NRM-001 | F | Normal mode 2 with index 0 | Resolves TS-table candidate |
| NRM-002 | F | Normal mode 2 with index 35 | Highest 36-table index accepted |
| NRM-003 | F/D | Normal mode 2 with index 36 | Out-of-table diagnostic; no clamp/modulo |
| NRM-004 | F | Normal mode 4 with index 243 | Highest 244-table index accepted |
| NRM-005 | F/D | Normal mode 4 with index 244 | Out-of-table diagnostic |
| NRM-006 | U | Unknown normal-mode byte | Raw mode/index retained; no guessed table |
| NRM-007 | F | Embedded palette contains exactly 256 RGB triples | Raw palette and canonical hash stable |
| NRM-008 | D/U | Remap start greater than end | Preserve bytes; reversed-range diagnostic |
| NRM-009 | F | Same color index paired with different normal indices | Color and normal remain independent fields |
| NRM-010 | D | Approved normal table verification fixture | Count and canonical Float32LE SHA verified without embedding table in public evidence |

## D. HVA file and transform behavior — 20 cases

| ID | Basis | Case | Expected contract |
|---|---|---|---|
| HVA-001 | F | Minimum 1-frame/1-section exact-size HVA | Raw name and one 12-float record parse |
| HVA-002 | U | Zero-frame exact-size HVA | Safe parse or controlled legality diagnostic; no identity frame |
| HVA-003 | U | Zero-section exact-size HVA | Safe parse or controlled legality diagnostic |
| HVA-004 | F/U | Multiple frames, one section | Record sequence stable but cannot distinguish flattening order |
| HVA-005 | F/U | One frame, multiple sections | Names/records stable but cannot distinguish flattening order |
| HVA-006 | U | 2 frames × 2 sections with unique records under frame-major fixture | Candidate F mapping asserted, S differs |
| HVA-007 | U | Same 2×2 raw record sequence interpreted section-major | Candidate S mapping asserted, F differs |
| HVA-008 | D | Header truncated at bytes 0–23 | Exact field failure |
| HVA-009 | D | Section-name table truncated | Fails before transform allocation |
| HVA-010 | D | Transform truncated at each float boundary | No partial transform model |
| HVA-011 | F/D | Declared exact file size | Fully consumed success |
| HVA-012 | D/U | Bytes remain after declared transforms | Strict trailing-data diagnostic/failure |
| HVA-013 | D | 16-byte section name without NUL | Raw field retained; missing-NUL diagnostic |
| HVA-014 | D | Non-ASCII section-name bytes | No lossy identity replacement |
| HVA-015 | D | Duplicate exact section names | All names retained; later binding ambiguous |
| HVA-016 | U | Names differ only by ASCII case | Case-conflict candidate; no automatic folding |
| HVA-017 | D | Any transform component is NaN | Raw payload retained in failure diagnostic; no runtime matrix |
| HVA-018 | D | Any transform component is Infinity | Invalid finite-transform failure |
| HVA-019 | D/U | Extremely large finite transform/translation | Preserve with magnitude diagnostic; no clamp |
| HVA-020 | U | Finite singular 3×4 affine candidate | Parse succeeds with semantic diagnostic; stock rejection unresolved |

## E. VXL/HVA binding — 8 cases

| ID | Basis | Case | Expected contract |
|---|---|---|---|
| BND-001 | F/D | Equal counts and unique exact names in matching order | Complete unique binding |
| BND-002 | D | HVA has fewer named sections | Unbound VXL sections retained; incomplete |
| BND-003 | D | HVA has additional named sections | Unbound HVA sections retained; incomplete |
| BND-004 | D | Duplicate exact name on either side | Ambiguous group; no first/last winner |
| BND-005 | U | Only case-insensitive matches exist | Ambiguous/unresolved under strict policy |
| BND-006 | D | Counts equal but names differ and ordinals align | No default index fallback; incomplete |
| BND-007 | U | No HVA supplied | VXL remains successful; binding `NotAttempted`; upper policy decides |
| BND-008 | U | Names bind but HVA transform order is unresolved | Identity binding retained; frame transform view remains unresolved |

## F. Input-mode, safety and evidence — 6 cases

| ID | Basis | Case | Expected contract |
|---|---|---|---|
| XIO-001 | D | VXL Memory, seekable Stream and MIX window | Same raw model hash, diagnostics and sparse result |
| XIO-002 | D | HVA Memory, seekable Stream and MIX window | Same raw model/order-candidate hashes and diagnostics |
| XIO-003 | D | Corrupt span input with no-progress commands and hostile counts | Terminates within explicit command/read budgets |
| XIO-004 | D | Hostile dimensions/counts/body sizes | No unbounded allocation or arithmetic wraparound |
| XIO-005 | D | Core assembly/reference inspection | No `UnityEngine` or `System.Drawing`; no Unity matrix/vector types |
| XIO-006 | D | Public research/audit serialization | Contains no voxel body, coordinate list, per-voxel hash, full normal table, matrix list, Base64, hex, image or absolute path |

## Coverage summary

| Group | Cases |
|---|---:|
| VXL file/section layout | 24 |
| VXL span directory/decoder | 28 |
| Normals/palette | 10 |
| HVA layout/transforms | 20 |
| Binding | 8 |
| Cross-cutting safety/evidence | 6 |
| **Total** | **96** |

## Fixture independence requirement

Fixture builders must not choose derived fields using the production parser's own formulas without independent assertions.

Required independent builders:

- byte-offset VXL header/section/tailer writer with explicit expected offsets;
- column span builder that returns independently calculated inclusive start/end ranges;
- malformed-range builder not using production validation helpers;
- HVA raw writer with explicitly selected frame-major or section-major record order;
- 2×2 HVA differentiator with unique scalar markers in every record;
- binding fixture with raw names, duplicates and case-only collisions.

Passing synthetic fixtures proves parser consistency and safety. Only the later sanitized golden audit can support ProjectBaseline compatibility claims.
