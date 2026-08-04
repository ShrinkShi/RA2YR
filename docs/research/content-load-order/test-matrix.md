# Content discovery and INI composition test matrix

> Total: **96 cases**. No ProjectBaseline or original entry body is required.

Labels: **F** confirmed evidence, **P** configured project policy, **D** defensive check, **U** unresolved hypothesis.

## A. Root discovery and provider boundaries — 24

| ID | Basis | Case | Expected contract |
|---|---|---|---|
| DSC-001 | P | No expansion files | Base layers then loose only |
| DSC-002 | P | Only expandmd01 | Placed after ra2md and before loose |
| DSC-003 | P | expandmd01 and 02 | 02 later/higher; both INI layers retained |
| DSC-004 | P | expandmd01 and 99 | 99 later/higher |
| DSC-005 | P | Gaps 01,04,99 | All recognized; no early stop |
| DSC-006 | D | Randomized root enumeration | Canonical discovery hash unchanged |
| DSC-007 | D | Missing ra2.mix | Explicit missing-base diagnostic |
| DSC-008 | D | Missing ra2md.mix | Explicit missing-YR-base diagnostic |
| DSC-009 | D | FinalAlert directory contains matches | No candidates from excluded source |
| DSC-010 | D | Unpacked mirror contains matches | No candidates from excluded source |
| DSC-011 | D | Cache contains matches | No candidates from excluded source |
| DSC-012 | D | Alternate installation contains matches | No candidates from excluded source |
| DSC-013 | P | Loose file in configured root | Highest configured layer |
| DSC-014 | U | Loose file category not enabled | Not a candidate; scope diagnostic |
| DSC-015 | D | Root entry budget exceeded | Bounded failure |
| DSC-016 | D | Archive-open byte budget exceeded | No unbounded open |
| DSC-017 | D | Two normalized ra2md names | Ambiguous duplicate, no winner |
| DSC-018 | D | Unknown root .mix | Unclassified and unmounted |
| DSC-019 | P | User-mod provider explicitly registered | Participates only in declared scope |
| DSC-020 | P | Modern provider texture-only scope | Cannot override INI |
| DSC-021 | D | Provider count budget | Bounded result |
| DSC-022 | D | Stable serialization repeated | Same bytes/hash |
| DSC-023 | P | ConfiguredRuntimeRoot only | No parent/registry search |
| DSC-024 | D | Absolute path serializer | Rejects public output |

## B. Numbered-family grammar and ordering — 18

| ID | Basis | Case | Expected contract |
|---|---|---|---|
| SEQ-001 | P | expandmd01 exact | Sequence 1 accepted |
| SEQ-002 | P | expandmd99 exact | Sequence 99 accepted |
| SEQ-003 | U | expandmd00 | Outside default; unresolved diagnostic |
| SEQ-004 | D | expandmd1 | Wrong width |
| SEQ-005 | D | expandmd001 | Wrong width |
| SEQ-006 | D | expandmd-1 | Nondecimal |
| SEQ-007 | D | expandmd01x | Not family match |
| SEQ-008 | D | xexpandmd01 | Not family match |
| SEQ-009 | D | case-only name accepted by configured comparer | Raw casing retained |
| SEQ-010 | D | two case variants same sequence | Duplicate sequence ambiguity |
| SEQ-011 | P | numeric order 02 before 10 | Not lexical accident |
| SEQ-012 | D | reverse enumeration 99..01 | Normalized low-to-high unchanged |
| SEQ-013 | P | non-md expand present in YR profile | Not mixed into md sequence |
| SEQ-014 | U | ecachemd01 and 02 | No expansion sorter reuse |
| SEQ-015 | U | elocalmd01 and 02 | No expansion sorter reuse |
| SEQ-016 | D | malformed ecache wildcard | Family-specific diagnostic |
| SEQ-017 | D | duplicate sequence different length/SHA | Length/SHA cannot break tie |
| SEQ-018 | D | missing sequence at every second number | No probe termination |

## C. Virtual resolution and nested mounts — 16

| ID | Basis | Case | Expected contract |
|---|---|---|---|
| VFS-001 | P | Binary same-name across base/expansion/loose | One highest winner, full trace |
| VFS-002 | P | INI same-name across layers | Ordered documents, no whole-file winner |
| VFS-003 | D | Candidate count budget | No silent truncation |
| VFS-004 | D | Suppressed chain low-to-high | Stable and complete |
| VFS-005 | D | Archive-entry enumeration randomized | Trace hash unchanged |
| VFS-006 | D | Memory/Stream/MIX-window candidate | Priority unchanged by input mode |
| VFS-007 | P | Known localmd explicit child | Mounted through declared edge |
| VFS-008 | P | Known cachemd explicit child | Mounted through declared edge |
| VFS-009 | D | Arbitrary nested MIX entry | Opaque by default |
| VFS-010 | D | Nested depth exceeded | Edge stopped, diagnostic |
| VFS-011 | D | Logical mount cycle | Terminates |
| VFS-012 | D | Same physical mount reached twice | Deduped with both provenance paths |
| VFS-013 | D | Equal SHA distinct entries | Not deduped by SHA alone |
| VFS-014 | P | Higher root layer deeper child | Root precedence first |
| VFS-015 | U | Two known children with unknown original order | Configured order + unresolved-original diagnostic |
| VFS-016 | D | Nested entry out of parent window | Bounded failure |

## D. Ordered multi-document INI composition — 22

| ID | Basis | Case | Expected contract |
|---|---|---|---|
| INI-001 | P | Higher INI layer overrides one existing key | Only same section/key replaced |
| INI-002 | P | Higher layer omits key | Lower effective key inherited |
| INI-003 | P | Higher layer adds key | New effective key added |
| INI-004 | P | Higher layer adds section | New effective section added |
| INI-005 | P | Three layers overwrite same key | Highest occurrence wins; two suppressed |
| INI-006 | P | expandmd02 overrides expandmd01 | Other expandmd01 keys inherited |
| INI-007 | P | expandmd01 overlays ra2md | Unspecified ra2md keys inherited |
| INI-008 | P | ra2md overlays ra2 | Unspecified ra2 keys inherited |
| INI-009 | D | Randomized physical enumeration | Composition hash unchanged |
| INI-010 | P | Effective-key provenance | Correct winning document/occurrence |
| INI-011 | P | Suppressed chain order | Low-to-high and complete |
| INI-012 | D | Same-document duplicate key | Diagnosed separately from cross-layer override |
| INI-013 | U | Empty high-layer value | Preserved; deletion/reset unresolved |
| INI-014 | U | Case-only section/key variants | Explicit comparer/diagnostic |
| INI-015 | P | No whole-file winner field | Result contract enforces absence |
| INI-016 | P | Low Weapon/Strength plus high Weapon/Cost | AK47, inherited Strength, added Cost |
| INI-017 | P | Numeric list same index override | Higher numeric key replaces same index |
| INI-018 | P | Numeric list missing index | Lower numeric key inherited |
| INI-019 | U | Numeric list gap | Typed consumer decides; composer does not renumber |
| INI-020 | U | Rules/Art/Sound/AI/UI/theater policy registry | Shared core, explicit per-document semantics |
| INI-021 | U | Mode/map overlay after archive composition | Order explicit, not guessed |
| INI-022 | D | Lossless comments/raw spellings | Composition does not mutate source docs |

## E. Architecture, determinism, and safety — 10

| ID | Basis | Case | Expected contract |
|---|---|---|---|
| ARC-001 | D | Production sorter versus independent oracle | Same result without shared helper |
| ARC-002 | D | Production INI composer versus independent expected map | Same effective identities |
| ARC-003 | D | Diagnostic budget exceeded | Bounded truncation marker |
| ARC-004 | D | No infinite nested enumeration | Operation budget terminates |
| ARC-005 | D | No unbounded candidate allocation | Limit checked before append |
| ARC-006 | D | All priority-key fields serializable | Round-trip stable |
| ARC-007 | D | SHA/length/timestamp mutation | Ordering unchanged |
| ARC-008 | D | Core assembly references | No UnityEngine |
| ARC-009 | P | Future modern provider inserted | Legacy relative order unchanged |
| ARC-010 | D | Provider-specific diagnostic preserved | Not collapsed into generic error |

## F. Sanitized audit and evidence — 6

| ID | Basis | Case | Expected contract |
|---|---|---|---|
| AUD-001 | D | Audit enumerates only configured root | Exclusion counters prove boundary |
| AUD-002 | D | Public audit schema allowlist | No INI/entry bodies |
| AUD-003 | D | Candidate chain for fixed logical files | Only counts/provenance/keys |
| AUD-004 | D | Randomized enumeration audit | Same aggregate hash |
| AUD-005 | P | Configured winner and original evidence state separate | Both reported |
| AUD-006 | D | Before/after source fingerprint | Read-only equality |

## Coverage summary

| Group | Cases |
|---|---:|
| Root discovery/provider boundaries | 24 |
| Numbered-family grammar/order | 18 |
| Virtual resolution/nested mounts | 16 |
| INI composition | 22 |
| Architecture/safety | 10 |
| Audit/evidence | 6 |
| **Total** | **96** |

## Fixture independence

Fixtures must not reuse the production archive-family parser, priority comparer, nested graph builder, or INI composer to calculate expected results.

Required independent fixtures:

- explicit unsorted root-name arrays with hand-authored expected family/sequence values;
- an independent lexicographic priority oracle;
- mount graphs with explicit node/edge IDs and cycles;
- lossless INI documents with hand-authored semantic identities;
- expected per-key winner/suppressed chains;
- public-audit allowlist rejection fixtures.

Passing the matrix proves deterministic configured behavior and safety. It does not by itself prove original executable behavior.
