# Test matrix

> Total: **108** research-designed cases. These are synthetic/structural specifications, not executed tests in this research PR.

## Classification

- `Fact`: convergent format behavior.
- `Conflict`: distinguishes competing public-source interpretations.
- `Policy`: fail-closed project safety contract.
- `Assumption`: evidence-gated candidate awaiting local audit.

| ID | Area | Evidence class | Case | Expected oracle |
|---|---|---|---|---|
| MC-001 | F80 | Fact/Policy | empty input | failure MissingCommand |
| MC-002 | F80 | Fact/Policy | terminator only | success output 0, consumed 1 |
| MC-003 | F80 | Fact/Policy | one-byte literal | success exact byte count |
| MC-004 | F80 | Fact/Policy | 63-byte literal | success literal maximum |
| MC-005 | F80 | Fact/Policy | two literal commands | success concatenated output |
| MC-006 | F80 | Fact/Policy | truncated literal payload | failure TruncatedLiteral |
| MC-007 | F80 | Fact/Policy | short relative minimum length | success length 3 |
| MC-008 | F80 | Fact/Policy | short relative maximum length | success length 10 |
| MC-009 | F80 | Fact/Policy | short relative distance one overlap | success repeated byte |
| MC-010 | F80 | Fact/Policy | short relative distance zero | failure InvalidBackReference |
| MC-011 | F80 | Fact/Policy | short relative distance exceeds output | failure LookbehindOverrun |
| MC-012 | F80 | Fact/Policy | medium absolute offset zero after prefix | success |
| MC-013 | F80 | Fact/Policy | medium absolute current-output offset | failure FutureReference |
| MC-014 | F80 | Fact/Policy | medium relative distance one | success overlap |
| MC-015 | F80 | Fact/Policy | medium relative distance zero | failure InvalidBackReference |
| MC-016 | F80 | Fact/Policy | medium maximum command length | success length 64 |
| MC-017 | F80 | Fact/Policy | long absolute copy | success |
| MC-018 | F80 | Fact/Policy | long relative copy | success |
| MC-019 | F80 | Fact/Policy | long zero count | failure ZeroProgressCommand |
| MC-020 | F80 | Fact/Policy | long count output overflow | failure OutputOverflow |
| MC-021 | F80 | Fact/Policy | fill one byte | success |
| MC-022 | F80 | Fact/Policy | fill maximum declared count within budget | success |
| MC-023 | F80 | Fact/Policy | fill zero count | failure ZeroProgressCommand |
| MC-024 | F80 | Fact/Policy | fill truncated count | failure TruncatedCommand |
| MC-025 | F80 | Fact/Policy | fill missing value | failure TruncatedCommand |
| MC-026 | F80 | Fact/Policy | overlap copy period two | success bytewise semantics |
| MC-027 | F80 | Fact/Policy | nested overlap after earlier overlap | success deterministic |
| MC-028 | F80 | Fact/Policy | terminator before expected output | failure OutputUnderflow |
| MC-029 | F80 | Fact/Policy | output reaches expected then terminator | success |
| MC-030 | F80 | Fact/Policy | output reaches expected then producing command | failure OutputOverflow |
| MC-031 | F80 | Fact/Policy | terminator with trailing payload byte | failure TrailingCompressedInput |
| MC-032 | F80 | Fact/Policy | missing terminator at exact output | failure MissingTerminator |
| MC-033 | F80 | Fact/Policy | unknown/invalid command under selected variant | structured failure |
| MC-034 | F80 | Fact/Policy | initial relative marker accepted in marker profile | success marker consumed |
| MC-035 | F80 | Fact/Policy | initial relative marker rejected in absolute profile | failure |
| MC-036 | F80 | Fact/Policy | absolute and relative interpretations distinguish fixture | different model hashes |
| MC-037 | F80 | Fact/Policy | command-count budget exceeded | failure LimitExceeded |
| MC-038 | F80 | Fact/Policy | input byte budget exceeded | failure LimitExceeded |
| MC-039 | F80 | Fact/Policy | Memory/Stream/MIX-window equivalence | same status/hash/diagnostics |
| MC-040 | F80 | Fact/Policy | fixture builder independent oracle | mutation caught |
| MC-041 | CHUNK | Fact/Conflict/Policy | single Format80 block | exact block/output |
| MC-042 | CHUNK | Fact/Conflict/Policy | multiple Format80 blocks | concatenated exact output |
| MC-043 | CHUNK | Fact/Conflict/Policy | single LZO block | exact block/output |
| MC-044 | CHUNK | Fact/Conflict/Policy | multiple LZO blocks | concatenated exact output |
| MC-045 | CHUNK | Fact/Conflict/Policy | final short uncompressed block | accepted |
| MC-046 | CHUNK | Fact/Conflict/Policy | header truncated at byte 0 | failure TruncatedHeader |
| MC-047 | CHUNK | Fact/Conflict/Policy | header truncated at byte 2 | failure TruncatedHeader |
| MC-048 | CHUNK | Fact/Conflict/Policy | payload truncated | failure TruncatedPayload |
| MC-049 | CHUNK | Fact/Conflict/Policy | compressed size zero output nonzero | failure ZeroSizeField |
| MC-050 | CHUNK | Fact/Conflict/Policy | compressed nonzero output zero | failure ZeroSizeField |
| MC-051 | CHUNK | Fact/Conflict/Policy | zero/zero default profile | failure UnderconfirmedSentinel |
| MC-052 | CHUNK | Fact/Conflict/Policy | zero/zero final sentinel profile | success only if final |
| MC-053 | CHUNK | Fact/Conflict/Policy | zero/zero followed by bytes | failure TrailingInput |
| MC-054 | CHUNK | Fact/Conflict/Policy | padding between blocks | failure invalid next header |
| MC-055 | CHUNK | Fact/Conflict/Policy | block count budget | failure LimitExceeded |
| MC-056 | CHUNK | Fact/Conflict/Policy | compressed block budget | failure LimitExceeded |
| MC-057 | CHUNK | Fact/Conflict/Policy | output block budget | failure LimitExceeded |
| MC-058 | CHUNK | Fact/Conflict/Policy | aggregate compressed budget | failure LimitExceeded |
| MC-059 | CHUNK | Fact/Conflict/Policy | aggregate output budget | failure LimitExceeded |
| MC-060 | CHUNK | Fact/Conflict/Policy | backend input overrun | failure normalized |
| MC-061 | CHUNK | Fact/Conflict/Policy | backend output overrun | failure normalized |
| MC-062 | CHUNK | Fact/Conflict/Policy | backend lookbehind overrun | failure normalized |
| MC-063 | CHUNK | Fact/Conflict/Policy | backend trailing input | failure exact-consumption |
| MC-064 | CHUNK | Fact/Conflict/Policy | backend output length mismatch | failure |
| MC-065 | CHUNK | Fact/Conflict/Policy | extra block after expected aggregate output | failure |
| MC-066 | CHUNK | Fact/Conflict/Policy | input ends exactly after last payload | success |
| MC-067 | CHUNK | Fact/Conflict/Policy | IsoMap and Preview envelope structural equivalence | same directory model |
| MC-068 | CHUNK | Fact/Conflict/Policy | 8192 and alternative chunk boundaries | same decoded aggregate hash |
| MC-069 | FRAG | Fact/Conflict/Policy | 1..N source and numeric order same | success |
| MC-070 | FRAG | Fact/Conflict/Policy | physical INI order shuffled numeric unique | numeric policy stable |
| MC-071 | FRAG | Fact/Conflict/Policy | source-order policy preserves shuffle | different explicit policy |
| MC-072 | FRAG | Fact/Conflict/Policy | gap at 2 | diagnostic/failure strict |
| MC-073 | FRAG | Fact/Conflict/Policy | gap after complete prefix | diagnostic/failure strict |
| MC-074 | FRAG | Fact/Conflict/Policy | duplicate key 1 | failure ambiguity |
| MC-075 | FRAG | Fact/Conflict/Policy | keys 1 and 01 | failure normalized duplicate |
| MC-076 | FRAG | Fact/Conflict/Policy | key 0 | failure vanilla strict |
| MC-077 | FRAG | Fact/Conflict/Policy | negative key | failure |
| MC-078 | FRAG | Fact/Conflict/Policy | plus-signed key | failure |
| MC-079 | FRAG | Fact/Conflict/Policy | nonnumeric key empty | preserve diagnostic |
| MC-080 | FRAG | Fact/Conflict/Policy | nonnumeric key nonempty | failure packed view |
| MC-081 | FRAG | Fact/Conflict/Policy | empty fragment | failure strict |
| MC-082 | FRAG | Fact/Conflict/Policy | 70-character first fragment | concatenate before decode |
| MC-083 | FRAG | Fact/Conflict/Policy | fragment boundary inside Base64 quartet | success after concat |
| MC-084 | FRAG | Fact/Conflict/Policy | invalid Base64 alphabet | failure |
| MC-085 | FRAG | Fact/Conflict/Policy | padding before final fragment | failure |
| MC-086 | FRAG | Fact/Conflict/Policy | bad padding count | failure |
| MC-087 | FRAG | Fact/Conflict/Policy | data after padding | failure |
| MC-088 | FRAG | Fact/Conflict/Policy | fragment count budget | failure LimitExceeded |
| MC-089 | FRAG | Fact/Conflict/Policy | character/decoded budget | failure LimitExceeded |
| MC-090 | FRAG | Fact/Conflict/Policy | provenance and order disagreement retained | complete trace |
| MC-091 | X | Policy | pipeline stops after fragment failure | codec not invoked |
| MC-092 | X | Policy | pipeline stops after Base64 failure | envelope not invoked |
| MC-093 | X | Policy | pipeline stops after envelope failure | map reader not invoked |
| MC-094 | X | Policy | codec cannot read INI | architecture check |
| MC-095 | X | Policy | MAP reader cannot implement Base64 | architecture check |
| MC-096 | X | Policy | LZO backend cannot know record size | architecture check |
| MC-097 | X | Policy | Format80 cannot construct Overlay object | architecture check |
| MC-098 | X | Policy | Core has no UnityEngine | dependency check |
| MC-099 | X | Policy | checked arithmetic at every offset | overflow fixture fails |
| MC-100 | X | Policy | no file-driven allocation | preflight limit |
| MC-101 | X | Policy | diagnostic budget | bounded |
| MC-102 | X | Policy | no dead loop on zero-progress backend | failure |
| MC-103 | X | Policy | same data split across stream reads | same result |
| MC-104 | X | Policy | random input enumeration order | normalized hash stable |
| MC-105 | X | Policy | GPL code absence scan | repository safety/license review |
| MC-106 | X | Policy | public audit allowlist | forbidden fields rejected |
| MC-107 | X | Policy | failed partial output has no success hash | policy |
| MC-108 | X | Policy | variant fallback prohibited | single selected variant only |

## Count by area

| Area | Count |
|---|---:|
| Format80/LCW | 40 |
| Chunk envelope and codec backend | 28 |
| Numbered fragments and Base64 | 22 |
| Cross-cutting architecture, safety, licensing and audit | 18 |
| **Total** | **108** |

## Fixture independence

Fixture producers must be independent from production parsing formulas:

- hand-authored byte vectors for each command class;
- at least one external non-GPL interoperability corpus where license permits redistribution;
- mutation tests for masks, lengths, endian fields and reference bases;
- a fixture builder may serialize declarative commands, but must not call production decode helpers;
- expected output and consumed lengths are independently specified;
- variant-distinguishing fixtures must succeed under exactly one intended profile.

## Promotion gates

A production default is eligible only when:

1. synthetic cases for its exact variant pass;
2. Memory/Stream/MIX-window results are identical;
3. multiple sanitized stock-map roles agree;
4. no success depends on padding, clipping, ignored backend errors or trailing input;
5. selected LZO dependency/implementation passes legal and security review;
6. diagnostic and allocation budgets are enforced.
