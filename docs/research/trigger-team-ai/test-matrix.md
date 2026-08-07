# Test matrix — 160 design cases

> **Source notice:** ChatGPT Web public-source research. No local `ProjectBaseline` access. Not a Codex artifact. No GPL or unclear-license code was copied, translated, mechanically rewritten, or ported. `code_imported: false`.

This is a test design only. No tests or parser code were implemented.

## Summary

| Category | Cases |
|---|---:|
| Identity, Tag, and Trigger layout | 24 |
| Event records and opcodes | 28 |
| Action records and opcodes | 30 |
| Parameter and reference graph | 20 |
| TeamType | 18 |
| TaskForce, ScriptType, and AITrigger | 28 |
| Safety, roundtrip, architecture, and audit | 12 |
| **Total** | **160** |

## A. Identity, Tag, and Trigger layout — 24

1. `ID-001` Parse a Trigger record with exactly eight nonempty tokens while preserving every raw token.
2. `ID-002` Parse a Trigger record with seven tokens and report a missing tail without inserting an editor default.
3. `ID-003` Parse a Trigger record with more than eight tokens and preserve the extension tail.
4. `ID-004` Preserve an empty Trigger owner token rather than shifting later fields.
5. `ID-005` Preserve consecutive commas in a Trigger value as empty tokens.
6. `ID-006` Preserve a trailing comma after the Trigger tail field.
7. `ID-007` Preserve whitespace around Trigger tokens while exposing trimmed candidates separately.
8. `ID-008` Preserve a Trigger key with non-GUID spelling as a valid raw identity.
9. `ID-009` Detect two byte-identical duplicate Trigger keys without first/last-wins deletion.
10. `ID-010` Detect duplicate Trigger keys with conflicting values.
11. `ID-011` Detect a case-only Trigger ID collision under a case-folded candidate policy.
12. `ID-012` Keep exact-case Trigger IDs separate under exact-match policy.
13. `ID-013` Preserve a self-linked Trigger edge and report a self-cycle.
14. `ID-014` Preserve a two-Trigger cycle and report the strongly connected component.
15. `ID-015` Preserve a linked Trigger target that is missing.
16. `ID-016` Recognize a field-specific no-link sentinel without applying it to other identity domains.
17. `ID-017` Parse a Tag record with `repeat,name,triggerId` candidate layout.
18. `ID-018` Preserve an empty Tag name and retain the Trigger reference position.
19. `ID-019` Detect duplicate Tag IDs with the same Trigger target.
20. `ID-020` Detect duplicate Tag IDs with conflicting Trigger targets.
21. `ID-021` Preserve a dangling Tag→Trigger edge.
22. `ID-022` Preserve an object-placement→Tag edge whose Tag is missing.
23. `ID-023` Preserve a CellTag→Tag edge whose Tag identity is duplicated.
24. `ID-024` Keep Trigger, Tag, TeamType, and placement-record IDs in separate identity domains even when raw text matches.

## B. Event records and opcodes — 28

25. `EV-001` Parse an Events record with declared count zero and no tuple tokens.
26. `EV-002` Parse one base Event tuple containing opcode plus two parameter slots.
27. `EV-003` Parse one configured Event tuple containing opcode plus four parameter slots.
28. `EV-004` Parse multiple Events and preserve tuple source order.
29. `EV-005` Preserve an empty parameter slot in a base Event tuple.
30. `EV-006` Preserve an empty additional parameter slot.
31. `EV-007` Report a nonnumeric declared Event count.
32. `EV-008` Report a negative declared Event count.
33. `EV-009` Reject semantic expansion when declared Event count exceeds budget.
34. `EV-010` Use checked arithmetic for declared count and maximum tuple width.
35. `EV-011` Report a truncated opcode-only final Event tuple.
36. `EV-012` Report a final tuple missing one base parameter.
37. `EV-013` Report a final tuple missing an extension parameter.
38. `EV-014` Preserve tokens after the declared Event count as extra tokens.
39. `EV-015` Report declared count greater than parsed tuple count.
40. `EV-016` Report declared count less than available complete tuple count without dropping the extra tuple.
41. `EV-017` Preserve unknown positive Event opcode and all parameters.
42. `EV-018` Preserve negative Event opcode as raw and mark semantic interpretation unresolved.
43. `EV-019` Preserve Event opcode larger than signed 32-bit and report numeric overflow.
44. `EV-020` Interpret an extension Event only when its explicit extension profile is selected.
45. `EV-021` Do not auto-select an extension profile merely because tuple width fits it.
46. `EV-022` Detect duplicate Events keys with identical values.
47. `EV-023` Detect duplicate Events keys with conflicting values.
48. `EV-024` Preserve an Events record whose Trigger is missing.
49. `EV-025` Preserve a Trigger with no Events record without fabricating count zero.
50. `EV-026` Keep editor Event display name separate from numeric opcode identity.
51. `EV-027` Keep unknown Event opcode out of the executor-eligible graph while retaining the raw graph.
52. `EV-028` Verify Event parsing performs no world query, timer creation, variable read, or Trigger activation.

## C. Action records and opcodes — 30

53. `AC-001` Parse an Actions record with declared count zero and no tuple tokens.
54. `AC-002` Parse one Action tuple as one opcode plus seven raw parameter slots.
55. `AC-003` Parse multiple Actions and preserve exact source order.
56. `AC-004` Preserve empty Action parameter slots.
57. `AC-005` Preserve a trailing empty seventh parameter.
58. `AC-006` Preserve `A` in the seventh slot as raw text.
59. `AC-007` Preserve nonzero data in a slot marked unused by the selected catalog.
60. `AC-008` Preserve text in a slot whose descriptor also permits numeric interpretation.
61. `AC-009` Report a nonnumeric declared Action count.
62. `AC-010` Report a negative declared Action count.
63. `AC-011` Reject semantic expansion when declared Action count exceeds budget.
64. `AC-012` Use checked arithmetic for `1 + count × 8` token expectation.
65. `AC-013` Report a final Action tuple missing the opcode.
66. `AC-014` Report a final Action tuple missing one parameter.
67. `AC-015` Report a final Action tuple missing all seven parameters.
68. `AC-016` Preserve tokens beyond declared complete Action tuples.
69. `AC-017` Report declared count greater than complete tuple count.
70. `AC-018` Report declared count less than available complete tuple count without deleting extras.
71. `AC-019` Preserve unknown positive Action opcode and all seven slots.
72. `AC-020` Preserve negative Action opcode and mark it unresolved.
73. `AC-021` Preserve Action opcode above signed 32-bit and report overflow.
74. `AC-022` Interpret an Ares Action only under an explicit Ares profile.
75. `AC-023` Interpret a Phobos Action only under an explicit Phobos profile.
76. `AC-024` Do not map an unknown Action to NoOp.
77. `AC-025` Detect duplicate Actions keys with identical records.
78. `AC-026` Detect duplicate Actions keys with conflicting records.
79. `AC-027` Preserve an Actions record whose Trigger is missing.
80. `AC-028` Preserve a Trigger with no Actions record without fabricating a zero-count value.
81. `AC-029` Verify Action parsing does not reorder, execute, schedule, or deduplicate Actions.
82. `AC-030` Verify Action parsing loads no sound, movie, theme, text, superweapon, or object resource.

## D. Parameter and reference graph — 20

83. `PR-001` Preserve an integer-looking parameter as raw plus signed/unsigned candidates.
84. `PR-002` Preserve a string-looking parameter as raw plus logical-ID candidates.
85. `PR-003` Report signed integer overflow without clamping or replacing raw text.
86. `PR-004` Preserve leading zeros and explicit plus/minus signs.
87. `PR-005` Keep a value matching both Waypoint and House identities ambiguous until a descriptor selects a target kind.
88. `PR-006` Keep a value matching Trigger and Tag IDs ambiguous under an unknown slot descriptor.
89. `PR-007` Resolve a House reference uniquely under an explicit parameter profile.
90. `PR-008` Preserve a missing House target as a dangling edge.
91. `PR-009` Resolve a TeamType reference across global and map-local layers with winner/suppressed provenance.
92. `PR-010` Report duplicate TeamType target identity instead of selecting source order.
93. `PR-011` Resolve a numeric Waypoint candidate without reading or moving a unit.
94. `PR-012` Preserve an alphabetic waypoint candidate and conversion trace.
95. `PR-013` Preserve a Rules type reference when its Art/resource is missing.
96. `PR-014` Keep local and global variable ID candidates distinct.
97. `PR-015` Preserve a missing variable definition while retaining the Event/Action parameter.
98. `PR-016` Preserve invalid boolean spelling and report an invalid boolean candidate.
99. `PR-017` Recognize `0/1`, `yes/no`, and `true/false` only under selected boolean profiles.
100. `PR-018` Treat sentinel spellings as field-specific rather than global.
101. `PR-019` Detect a reference cycle without recursion or graph mutation.
102. `PR-020` Verify graph resolution creates no runtime object, player, team, timer, or variable store.

## E. TeamType — 18

103. `TT-001` Parse `[TeamTypes]` list key and TeamType ID as separate raw identities.
104. `TT-002` Preserve a numeric key gap without compressing later TeamType IDs.
105. `TT-003` Preserve a nonnumeric TeamTypes list key.
106. `TT-004` Detect `1` versus `01` normalized-key collision while preserving both.
107. `TT-005` Detect duplicate TeamType IDs in the list.
108. `TT-006` Report a listed TeamType whose per-ID section is missing.
109. `TT-007` Preserve an unlisted per-ID TeamType section.
110. `TT-008` Preserve duplicate per-ID TeamType section occurrences.
111. `TT-009` Preserve unknown TeamType keys and extension flags.
112. `TT-010` Resolve House, TaskForce, Script, and Tag edges independently.
113. `TT-011` Preserve a missing House without substituting Neutral.
114. `TT-012` Preserve a dangling TaskForce edge.
115. `TT-013` Preserve a dangling Script edge.
116. `TT-014` Preserve a dangling Tag edge.
117. `TT-015` Preserve Waypoint and TransportWaypoint raw spellings and sentinels.
118. `TT-016` Preserve invalid boolean flag spelling instead of using editor defaults.
119. `TT-017` Keep global/local source classification separate from `-G` naming convention.
120. `TT-018` Verify TeamType parsing does not recruit, instantiate, move, or assign units.

## F. TaskForce, ScriptType, and AITrigger — 28

121. `TS-001` Parse a TaskForce `count,type` entry while preserving raw tokens.
122. `TS-002` Preserve TaskForce entry count zero and report semantic uncertainty.
123. `TS-003` Preserve negative TaskForce entry count without clamping.
124. `TS-004` Report TaskForce count numeric overflow without allocating units.
125. `TS-005` Preserve an unknown TaskForce Rules type as a dangling binding.
126. `TS-006` Preserve duplicate TaskForce entry keys.
127. `TS-007` Preserve TaskForce gaps and entries after a gap.
128. `TS-008` Preserve a seventh TaskForce entry as extension/out-of-profile data.
129. `TS-009` Keep repeated identical TechnoType entries separate.
130. `TS-010` Verify TaskForce parsing creates no units and loads no assets.
131. `TS-011` Parse a Script step as `action,argument` raw pair.
132. `TS-012` Preserve a negative Script action as unresolved raw data.
133. `TS-013` Preserve a high Phobos Script action without enabling its semantics absent profile.
134. `TS-014` Preserve a negative Script argument.
135. `TS-015` Preserve duplicate Script step keys.
136. `TS-016` Preserve Script steps after a numeric key gap.
137. `TS-017` Preserve an unknown Script action and later steps.
138. `TS-018` Detect a candidate Script jump cycle without executing it.
139. `TS-019` Keep Script Action, Trigger Action, Trigger Event, and Mission catalogs separate.
140. `TS-020` Verify Script parsing does not move units or advance an instruction pointer.
141. `TS-021` Parse an AITrigger record with exactly 18 tokens.
142. `TS-022` Preserve an AITrigger record with fewer than 18 tokens and report missing fields.
143. `TS-023` Preserve AITrigger extension-tail tokens beyond 18.
144. `TS-024` Preserve a missing primary TeamType edge.
145. `TS-025` Preserve a missing secondary TeamType edge.
146. `TS-026` Preserve a malformed or non-64-character comparator blob.
147. `TS-027` Preserve invalid/overflowing decimal weights and report consistency issues.
148. `TS-028` Verify AITrigger parsing neither selects teams nor changes weights or production queues.

## G. Safety, roundtrip, architecture, and audit — 12

149. `SA-001` Enforce section, record, token, identity, edge, and diagnostic budgets.
150. `SA-002` Enforce maximum token length and aggregate character budget.
151. `SA-003` Verify checked arithmetic for all declared counts and graph-size calculations.
152. `SA-004` Verify malformed input cannot cause a no-progress loop.
153. `SA-005` Verify Memory, seekable Stream, short-read Stream, and bounded MIX-window inputs produce equivalent raw graphs and diagnostics.
154. `SA-006` Verify truncated Stream input returns structured failure without partial semantic success.
155. `SA-007` Verify synthetic fixture builders do not reuse production tokenizer, opcode, count, normalization, or reference-resolution formulas.
156. `SA-008` Verify lossless roundtrip preserves section order, duplicate sections/keys, ID case, empty fields, unused slots, unknown opcodes, count mismatch, and extension tails.
157. `SA-009` Verify canonical rewrite is opt-in and never implied by parse success.
158. `SA-010` Verify Core assemblies have no `UnityEngine`, GameObject, coroutine, behavior-tree, timer, rendering, pathfinding, or asset-loader dependency.
159. `SA-011` Verify no test invokes Trigger execution, AI evaluation, Team creation, Script execution, or variable mutation.
160. `SA-012` Verify sanitized audit output contains only allowed aggregates and cannot reconstruct IDs, record text, opcode sequences, graph topology, or map logic.

## Required cross-case assertions

Every applicable case should also assert:

- raw values are immutable;
- derived values identify the selected profile and evidence grade;
- diagnostics include source location without leaking protected content in audit mode;
- ambiguity is not silently resolved;
- no file or resource lookup occurs from an untrusted parameter;
- no compatibility state is promoted by a test passing.

## Test fixture policy

Fixtures must be synthetic and independently constructed. They must not contain copied stock map records, original trigger graphs, map names, or reconstructable scenario logic.

Future `ProjectBaseline` audit results may inform new test categories, but stock bytes or per-record tuples must not be committed.
