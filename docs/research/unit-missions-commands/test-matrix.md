> **Source notice:** Prepared by **ChatGPT Web** from public sources only. ProjectBaseline was not read. This is not a Codex artifact. GPL and unclear-license implementations are reference-only; no code was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

# Test Matrix

The matrix defines exactly **184** research design cases. Synthetic fixtures must not reuse production mission, command, queue, target-selection, path, transport, or roundtrip logic.

## Mission records and identity — 28

1. missing placement Mission.
2. empty Mission.
3. unknown text token.
4. negative numeric candidate.
5. overflow numeric candidate.
6. case collision.
7. duplicate placement record.
8. placement versus current runtime.
9. Rules default versus map value.
10. Script assign mission.
11. Trigger mission reference.
12. AI mission reference.
13. TS ordinal profile.
14. RA2 ordinal profile.
15. YR additions.
16. extension mission.
17. Guard token.
18. Area Guard token.
19. Sticky candidate.
20. Stop token.
21. Hunt token.
22. Ambush token.
23. Construction/Selling/Repair building missions.
24. Unload mission.
25. Patrol mission.
26. unknown preserved.
27. evidence grade serialization.
28. lossless roundtrip.

## Stop, hold, guard, and autonomy — 30

29. S interrupts explicit Move.
30. S clears explicit attack target.
31. S clears path request.
32. S queue-clear policy.
33. S followed by in-range auto-attack.
34. S allows retaliation candidate.
35. S does not enable Hold.
36. H forbids autonomous movement.
37. H forbids chase.
38. H allows in-place turn.
39. H allows in-place aim.
40. H allows legal in-place fire.
41. H cleared by next explicit Move.
42. H with unreachable target.
43. G opens policy UI only.
44. G does not issue stock Guard.
45. G profile serialization.
46. Guard acquire target.
47. Guard chase then return.
48. ordinary non-Guard auto-fire.
49. Area Guard wider policy.
50. passive acquire disabled.
51. retaliation enabled without acquire.
52. manual fire disabled but autonomous allowed.
53. target persistence.
54. target lost.
55. cloakable Guard extension.
56. civilian auto-repel extension.
57. multiple autonomous blockers.
58. stable target candidate order.

## Move, attack, chase, and leash — 30

59. move command actor authorized.
60. move invalid cell.
61. attack actor.
62. attack cell force fire.
63. attack-move.
64. move then attack.
65. fire while moving.
66. turret tracking independent.
67. target enters range.
68. target leaves range.
69. target inside MinimumRange.
70. target cloaks.
71. target dies.
72. target enters limbo.
73. target owner changes.
74. path blocked.
75. unreachable target.
76. ground layer.
77. naval layer.
78. aircraft layer.
79. subterranean layer.
80. bridge deck versus under bridge.
81. chase begins.
82. chase leash exceeded.
83. return to leash origin.
84. hold cancels chase.
85. new command cancels chase.
86. target persistence timeout.
87. stable path-request identity.
88. movement intent creates no Unity path.

## Queue, waypoint, and patrol — 24

89. Shift append.
90. replace existing queue.
91. Stop clears queue.
92. Stop clears active only profile.
93. invalid queued target.
94. target dies before activation.
95. actor dies with queue.
96. partial completion.
97. ordinary waypoint route.
98. waypoint synchronized release.
99. multiple groups release.
100. delete middle node.
101. route node reconnect.
102. patrol loop.
103. patrol target acquisition.
104. patrol chase and return.
105. attack waypoint.
106. enter waypoint.
107. queue save/load.
108. queue replay.
109. multiplayer batch.
110. stable queue ordinal.
111. UI list differs from queue.
112. route budget exceeded.

## Deploy, repair, sell, capture, and enter — 26

113. DeploysInto valid.
114. UndeploysInto valid.
115. IsSimpleDeployer.
116. deploy blocked terrain.
117. deploy blocked occupancy.
118. deploy transfer policy.
119. deploy cancelled by Stop.
120. repair facility enter.
121. repair target relationship.
122. repair credits separate.
123. sell building command.
124. sell owner invalid.
125. sell/deploy conflict.
126. engineer capture.
127. spy infiltration.
128. sabotage/C4.
129. enter transport.
130. enter garrison.
131. enter grinder.
132. occupied target.
133. target destroyed during approach.
134. target captured during approach.
135. capacity fills during approach.
136. new command cancels enter.
137. unknown enter target domain.
138. no runtime transformation created.

## Transport, garrison, and occupants — 24

139. transport capacity zero.
140. transport full.
141. passenger Size.
142. SizeLimit.
143. allowed passenger extension.
144. disallowed passenger extension.
145. manual enter hidden but script allowed.
146. embark reservation.
147. embark path failure.
148. unload all.
149. unload one.
150. blocked unload.
151. partial unload.
152. alternate unload cell.
153. naval unload.
154. airborne/paradrop candidate.
155. garrison capacity.
156. garrison occupant weapon profile.
157. garrison capture.
158. evacuate occupants.
159. transport destruction.
160. passenger survival profile.
161. occupancy save/load.
162. image dimensions do not set capacity.

## Selection, UI, determinism, safety, and audit — 22

163. click selection.
164. box selection.
165. Shift add/remove.
166. type select on screen.
167. type select across map.
168. control group assign.
169. control group select.
170. observer selection no authority.
171. invalid cursor.
172. duplicate hotkey.
173. stock H versus project H profile.
174. selected-only health/load indicators.
175. deterministic same-tick command order.
176. stable autonomous-decision order.
177. save/load deterministic state.
178. replay equivalence.
179. Memory input.
180. seekable Stream input.
181. short-read Stream input.
182. exact MIX window input.
183. budgets/checked arithmetic/no-progress.
184. noEngineReferences and no Unity objects.

## Cross-cutting expectations

Every applicable case checks:

- structured diagnostics rather than silent fallback;
- explicit product/extension profile;
- raw and derived separation;
- deterministic ordering;
- checked arithmetic and bounded collections;
- Memory, Stream, short-read Stream, and exact MIX-window equivalence;
- no `UnityEngine` references;
- no actor, path, target, cursor, button, or UI object creation.
