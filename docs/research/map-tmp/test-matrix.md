# MAP/TMP synthetic test matrix

> Total: **104 cases**. Labels: **F** confirmed format/community fact, **P** configured semantic policy, **D** defensive check, **U** unconfirmed/conflict distinguisher.

## A. Map family and lossless INI shell — 16

| ID | Basis | Case | Expected |
|---|---|---|---|
| MAP-001 | F | Minimal `.map` with Basic/Map | Lossless shell and family candidate |
| MAP-002 | F/P | Equivalent `.mpr` bytes | Same parse model, different discovery role |
| MAP-003 | F/P | Equivalent `.yrm` bytes | Same parse model, YR custom-map role |
| MAP-004 | D | Empty input | Controlled failure |
| MAP-005 | D | Oversized line/value | Limit before allocation |
| MAP-006 | D | Invalid encoding/BOM | Raw diagnostic, no lossy guessing |
| MAP-007 | F | Duplicate section names | All occurrences preserved |
| MAP-008 | F | Duplicate keys | All occurrences preserved |
| MAP-009 | D | Invalid `[Map] Size` field count | No terrain allocation |
| MAP-010 | D | Negative/overflow dimensions | Checked failure |
| MAP-011 | U | Unknown NewINIFormat | Raw value retained, profile unresolved |
| MAP-012 | F | Unknown section and comments | Preserved exactly in shell |
| MAP-013 | D | Section/key diagnostic budget | Bounded output |
| MAP-014 | P | Extension does not select codec | Family metadata separate |
| MAP-015 | D | Culture with comma/alternate digits | Invariant parsing |
| MAP-016 | D | Random occurrence enumeration | Canonical shell hash stable |

## B. Packed fragment and compression envelope — 16

| ID | Basis | Case | Expected |
|---|---|---|---|
| PCK-001 | F | Numeric fragments 1..N | Concatenated ascending |
| PCK-002 | U | Fragment key 0 | Preserved and policy-diagnosed |
| PCK-003 | D | Duplicate normalized numeric key | Ambiguous, no last winner |
| PCK-004 | U | Gap in numeric keys | Deterministic candidate, gap diagnostic |
| PCK-005 | D | Nonnumeric fragment | Not appended arbitrarily |
| PCK-006 | D | Invalid Base64 character | Exact syntax failure |
| PCK-007 | D | Base64 aggregate over budget | Fail before allocation |
| PCK-008 | F | One valid LZO block | Declared output exact |
| PCK-009 | F | Multiple valid LZO blocks | Ordered cumulative output |
| PCK-010 | D | Truncated four-byte block header | Exact failure |
| PCK-011 | D | Payload shorter than compressedSize | Bounded failure |
| PCK-012 | D | Decompressor output mismatch | No padding/truncation |
| PCK-013 | U | Zero compressed/output sizes | Profile-specific terminator result |
| PCK-014 | D | Output-total overflow | Checked failure |
| PCK-015 | D | Hostile Format80 no-progress input | Command budget termination |
| PCK-016 | D | Memory/Stream/MIX compressed section | Same status/hash/diagnostics |

## C. IsoMapPack5 — 18

| ID | Basis | Case | Expected |
|---|---|---|---|
| ISO-001 | F | One 11-byte raw record + four zeros | Raw record and terminal retained |
| ISO-002 | U | Tile high word nonzero | Both 32-bit and word views retained |
| ISO-003 | U | Signed-negative X/Y raw values | Raw signed/unsigned views, range diagnostic |
| ISO-004 | F | Subtile and level boundary bytes | Preserved unchanged |
| ISO-005 | U | Nonzero tail/ice byte | Preserved, profile diagnostic |
| ISO-006 | F/U | Dense expected record count | Canvas candidate complete |
| ISO-007 | U | Sparse clear-cell omission | Sparse candidate, not auto-failure |
| ISO-008 | D | Duplicate coordinate records | Ambiguous group, no overwrite |
| ISO-009 | D | Out-of-map coordinate | Preserved with diagnostic |
| ISO-010 | D | Invalid tile identity | No replacement with zero |
| ISO-011 | D | Invalid subtile candidate | Binding diagnostic later |
| ISO-012 | D | Decompressed bytes not `11n+4` | Structural failure |
| ISO-013 | U | Missing final four bytes | Explicit terminal-policy result |
| ISO-014 | U | Nonzero final four bytes | Retained and diagnosed |
| ISO-015 | F | Noncanonical record order | Source order preserved |
| ISO-016 | P | Regenerated canonical writer order | Explicit normalization only |
| ISO-017 | D | Dimension/cell-count overflow | No canvas allocation |
| ISO-018 | D | Independent fixture encodes 32-bit field | Does not reuse production parser formula |

## D. Overlay and Preview — 16

| ID | Basis | Case | Expected |
|---|---|---|---|
| OVP-001 | F | OverlayPack decodes to 262144 bytes | Accepted vanilla candidate |
| OVP-002 | F | OverlayDataPack decodes to 262144 bytes | Accepted separately |
| OVP-003 | F | Overlay type 0xFF | Empty overlay candidate |
| OVP-004 | U | OverlayData 0xFF | Preserved, not universal empty |
| OVP-005 | D | One array invalid, other valid | No pair binding |
| OVP-006 | D | Decoded length short/long | No padding/truncation |
| OVP-007 | U | 524288-byte extended overlay type array | Extension profile only |
| OVP-008 | D | Coordinate at 511,511 | Last fixed-canvas index valid |
| OVP-009 | D | Coordinate outside fixed canvas | Checked failure |
| PRV-001 | F | Valid Preview size and 3-byte pixels | Complete raw preview model |
| PRV-002 | D | Width×height×3 overflow | No allocation |
| PRV-003 | D | Output shorter/longer than expected | Exact failure |
| PRV-004 | U | RGB versus BGR adapters | Distinct canonical adapter hashes |
| PRV-005 | D | Preview metadata only / pack only | Separate incomplete states |
| PRV-006 | U | Zero-size LZO block | Explicit policy result |
| PRV-007 | P/U | Preview sections not first | Reader preserves; writer profile reports move |
| PRV-008 | P | Missing preview | No reader-fabricated dummy data |

## E. Mission graph and map-local INI — 14

| ID | Basis | Case | Expected |
|---|---|---|---|
| MIS-001 | F | Waypoint decimal cell | X/Y candidate derived |
| MIS-002 | D | Waypoint integer overflow | Raw value, failure diagnostic |
| MIS-003 | F | Structure/unit/infantry positional records | Empty and extra fields retained |
| MIS-004 | D | Unknown object type | Raw placement preserved |
| MIS-005 | F | Trigger→event/action/tag complete graph | Unique edges resolved |
| MIS-006 | D | Missing graph target | Dangling edge retained |
| MIS-007 | D | Duplicate identity | Ambiguous, no first winner |
| MIS-008 | U | Unknown opcode/extra parameters | Raw record preserved |
| MIS-009 | F | Team→taskforce/script references | Graph resolution separate |
| MIS-010 | D | Transactional ID regeneration fixture | All known references updated or abort |
| LOC-001 | P | Map overrides one global key | Map occurrence wins only that key |
| LOC-002 | P | Map omits global key | Global value inherited |
| LOC-003 | U | Empty map-local value | Preserved; delete/reset unresolved |
| LOC-004 | U | `ART.<section>` | Enabled only by selected extension profile |

## F. TMP layout and terrain binding — 18

| ID | Basis | Case | Expected |
|---|---|---|---|
| TMP-001 | F | Minimal one-cell TMP | Header/offset/cell parse |
| TMP-002 | F | Multi-cell TMP with empty zero-offset slots | Ordered slots preserved |
| TMP-003 | D | Header/offset table truncated | Exact failure |
| TMP-004 | D | Template dimension multiplication overflow | No offset array |
| TMP-005 | D | Nonzero offset outside file | Failure before seek |
| TMP-006 | D | Duplicate cell offset | Alias diagnostic, immutable decode |
| TMP-007 | D | Partial overlapping cell ranges | Fail closed |
| TMP-008 | F | Diamond color plane | Exact row widths and bytes |
| TMP-009 | F | Diamond depth plane | Separate raw plane |
| TMP-010 | D | Diamond dimensions/area overflow | No image allocation |
| TMP-011 | F/U | Extra-data flag bit 0 and valid rectangle | Color/depth extra planes retained |
| TMP-012 | D | Extra rectangle truncated | No clipping or partial plane |
| TMP-013 | D | Extra bounds arithmetic overflow | Checked failure |
| TMP-014 | U | Unknown flag bits | Raw flags retained |
| TMP-015 | U | Conflicting ramp/terrain metadata views | Raw bytes plus candidates |
| TMP-016 | P | Theater palette binding | Outside TMP reader |
| TMP-017 | D | Missing theater/TMP for map tile | Incomplete binding, map parse intact |
| TMP-018 | D | Memory/Stream/MIX TMP | Same model hash/diagnostics |

## G. Architecture, safety and audit — 6

| ID | Basis | Case | Expected |
|---|---|---|---|
| XIO-001 | D | Hostile map and TMP counters | No unbounded allocation/enumeration |
| XIO-002 | D | Diagnostic flood | Stable capped diagnostics |
| XIO-003 | D | Core assembly inspection | No Unity/editor dependencies |
| XIO-004 | P | Read-only source fingerprint | Unchanged before/after |
| XIO-005 | D | Public audit serializer | Forbidden content rejected |
| XIO-006 | D | Fixture builders versus production helpers | Independent size/order assertions |

## Coverage summary

| Group | Cases |
|---|---:|
| Map family / INI shell | 16 |
| Packed envelope | 16 |
| IsoMapPack5 | 18 |
| Overlay / Preview | 16 |
| Mission / local INI | 14 |
| TMP / terrain binding | 18 |
| Cross-cutting | 6 |
| **Total** | **104** |

## Golden evidence rule

Synthetic tests prove consistency and safety. They do not establish ProjectBaseline compatibility, original-runtime preview requirements, disputed IsoMap field semantics or TMP metadata names. Those require the sanitized local audit and a later implementation decision record.
