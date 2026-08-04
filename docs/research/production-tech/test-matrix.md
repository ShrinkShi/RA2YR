> **Source notice:** This document was prepared by **ChatGPT Web** from public sources only. ProjectBaseline was not read. This is not a Codex artifact. GPL and unclear-license implementations are reference-only; no code was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

# Test matrix

Exactly **184** research design cases are specified. These are not implemented tests.

## A. Registries, types and factory binding — 28

1. Empty registry section.
2. Registry with a numeric gap.
3. Duplicate numeric key.
4. Leading-zero normalized-key collision.
5. Duplicate type value.
6. Case-only type-value collision.
7. Listed type with missing definition section.
8. Unregistered type-like section.
9. Unknown registry family.
10. Map-local type contribution.
11. Rules composition override with provenance.
12. Competing same-name type definitions.
13. Invalid registry ordinal text.
14. Overflowing registry ordinal.
15. Unknown `Factory` token.
16. Factory-name heuristic explicitly rejected.
17. Factory with several category candidates.
18. `Naval` conflict with factory category.
19. `NumberOfDocks=0`.
20. Overflowing `NumberOfDocks`.
21. Multiple primary-factory candidates.
22. FactoryPlant capability candidate.
23. Ares `BuiltAt` isolated from stock profile.
24. Ares explicit-only factory isolated.
25. Same logical type listed in several registries.
26. Missing Art does not remove type identity.
27. Registry/field count budgets.
28. Binder creates no runtime actor or queue.

## B. Owner, Side, Country and availability — 24

29. Unknown Owner token.
30. Empty Owner list.
31. Duplicate Owner token.
32. `RequiredHouses` only.
33. `ForbiddenHouses` only.
34. Required/forbidden conflict.
35. Side and Country remain distinct.
36. House and player slot remain distinct.
37. Multiplayer random-country candidate.
38. Campaign-authored player House.
39. Neutral/civilian owner.
40. Map-local Owner override.
41. Current owner differs from type Owner.
42. Captured owner snapshot.
43. Initial factory owner differs from current owner.
44. Stolen-tech state.
45. Secret Lab state.
46. Reverse-engineering extension isolation.
47. Disguise ignored by type binder.
48. Mind control ignored by Rules parser.
49. Several simultaneous blocker reasons.
50. Visible-but-unavailable result.
51. Hidden sidebar policy with simulation blockers retained.
52. No Player or House runtime object creation.

## C. Prerequisite, TechLevel and BuildLimit — 32

53. Missing `Prerequisite`.
54. Empty prerequisite value.
55. One prerequisite token.
56. Duplicate prerequisite token.
57. Unknown prerequisite token.
58. Empty token between separators.
59. Trailing separator.
60. Leading separator.
61. Comma grammar candidate.
62. Whitespace grammar candidate.
63. AND-group candidate.
64. OR-group candidate.
65. Alternative list candidate.
66. Negative prerequisite candidate.
67. `PrerequisiteOverride` candidate.
68. Generic prerequisite candidate.
69. Generic alternate candidate.
70. Upgrade prerequisite candidate.
71. Stolen-tech prerequisite candidate.
72. Theater prerequisite extension.
73. Factory-owner prerequisite extension.
74. Missing TechLevel.
75. `TechLevel=0`.
76. Negative TechLevel.
77. Invalid TechLevel text.
78. Overflowing TechLevel.
79. Lobby TechLevel lower than authored value.
80. Lobby TechLevel higher than authored value.
81. TechLevel passes while prerequisite fails.
82. Missing BuildLimit.
83. `BuildLimit=0`.
84. Negative BuildLimit.

## D. Cost, build time and modifiers — 24

85. Overflowing BuildLimit.
86. Queued-count BuildLimit policy conflict.
87. Deploy-equivalence BuildLimit conflict.
88. Captured-count BuildLimit conflict.
89. Missing Cost.
90. `Cost=0`.
91. Negative Cost.
92. Invalid Cost text.
93. Overflowing Cost.
94. Locale-ambiguous decimal modifier.
95. `BuildTimeMultiplier=0`.
96. Negative BuildTimeMultiplier.
97. Overflowing BuildTimeMultiplier.
98. Missing BuildSpeed candidate.
99. Cost-derived time candidate.
100. Explicit Ares BuildTime candidate.
101. Country cost modifier.
102. Country time modifier.
103. Difficulty modifier.
104. FactoryPlant modifier.
105. Multiple-factory modifier.
106. Low-power modifier.
107. Modifier-order conflict.
108. Checked multiplication and deterministic rounding.

## E. Queues, completion and transactions — 28

109. Empty queue.
110. One active entry.
111. Several queued entries.
112. Repeat request.
113. Pause request.
114. Hold request.
115. Resume request.
116. Cancel one entry.
117. Cancel all entries of a type.
118. Reorder candidate not silently supported.
119. Shared category queue profile.
120. Per-factory queue profile.
121. Parallel queue extension profile.
122. Insufficient credits at request time.
123. Insufficient credits during progressive deduction.
124. Up-front reservation profile.
125. Progressive deduction profile.
126. Partial-progress refund.
127. Factory offline.
128. Low-power pause candidate.
129. Factory destroyed during production.
130. Factory captured during production.
131. Player defeated during production.
132. Save/load mid-progress.
133. Simultaneous completions.
134. Stable completion ordering.
135. Completed unit awaiting exit.
136. Completed building awaiting placement.

## F. Placement, deploy, exit, power and capture — 28

137. Several ready buildings.
138. Transaction rollback on failed acceptance.
139. No spawn in Core.
140. Valid rectangular foundation.
141. Irregular extension foundation.
142. Foundation outside map.
143. Terrain buildability failure.
144. Resource/overlay conflict.
145. Bridge-layer placement.
146. Naval placement.
147. Shore placement.
148. Shroud policy conflict.
149. Adjacency failure.
150. Construction yard destroyed before placement.
151. Dynamic occupancy conflict.
152. Simultaneous placement reservation conflict.
153. Blocked factory exit.
154. Alternate exit candidate.
155. Naval exit layer.
156. Aircraft dock/pad candidate.
157. Foundation-center spawn explicitly rejected.
158. Deploy into building.
159. Undeploy into vehicle.
160. One-way transformation.
161. Health/cargo/ammo transfer unresolved.
162. Upgrade host missing.
163. Power deficit.
164. EMP/offline factory candidate.

## G. Sidebar, AI, Trigger, safety and audit — 20

165. Capture and completion on same tick.
166. Queue owner after capture unresolved.
167. Cameo missing.
168. AltCameo extension isolation.
169. CSF label missing.
170. Duplicate hotkey.
171. Locale-dependent hotkey candidate.
172. Stable sidebar sort.
173. Hidden versus disabled presentation.
174. Queue-count presentation.
175. Ready presentation without actor existence.
176. Observer/replay read-only view.
177. Trigger enable/disable-production raw candidate.
178. Trigger create-product raw candidate.
179. AI build-list reference boundary.
180. AI queue command boundary.
181. Memory / seekable Stream equivalence.
182. Short-read Stream / exact MIX-window equivalence.
183. Budgets, checked arithmetic and no-progress protection.
184. `noEngineReferences`: no Unity actor, `Button`, `ProgressBar`, `Tilemap`, queue, placement or sidebar object.

## Distribution check

```text
registries/type/factory binding                 28
Owner/Side/Country/availability                 24
Prerequisite/TechLevel/BuildLimit               32
Cost/BuildTime/modifiers                        24
queues/completion/transactions                  28
placement/deploy/exit/power/capture             28
sidebar/AI/Trigger/safety/audit                 20
--------------------------------------------------
Total                                           184
```

## Oracle independence

Synthetic fixtures and expected-result oracles must not call production prerequisite, availability, BuildLimit, time, cost, queue, placement, exit or transaction logic.

## Input and architecture requirements

- identical results for Memory, seekable Stream, short-read Stream and exact MIX window;
- checked arithmetic;
- bounded registries, expressions, queues, foundations and diagnostics;
- deterministic ordering;
- no partial semantic success after budget or no-progress failure;
- Core has no `UnityEngine` dependency;
- no runtime objects are created.

All public implementations are reference-only; `code_imported: false`.
