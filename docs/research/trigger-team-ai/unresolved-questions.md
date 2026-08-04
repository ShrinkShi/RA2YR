# Unresolved questions

> **Source notice:** ChatGPT Web public-source research. No local `ProjectBaseline` access. Not a Codex artifact. No GPL or unclear-license code was copied, translated, mechanically rewritten, or ported. `code_imported: false`.

The following questions remain unresolved. Editor behavior, community documentation, and future sanitized observations must not be promoted to original-runtime facts without stronger evidence.

## P0 — blocks a strict vanilla parser or stable graph contract

1. Is the common eight-token Trigger layout exact for all stock RA2 and YR map versions?
2. Does the Trigger tail field have runtime meaning, or is it reserved/unused after Tag persistence was introduced?
3. What exact no-link sentinel spellings does the stock runtime accept for linked Triggers?
4. Are Trigger IDs case-sensitive in the original runtime?
5. What happens when two Trigger records share the same key?
6. What happens when `[Events]` or `[Actions]` contains a key without a corresponding Trigger?
7. Does the runtime require every Trigger to have both Events and Actions records?
8. Does the runtime use first, last, or merged behavior for duplicate Events/Actions keys?
9. Is the Event base tuple always opcode plus two parameters in stock RA2/YR?
10. Which stock Event opcodes append a third string parameter or another additional slot?
11. Is Event tuple width determined solely by opcode, game version, or another marker?
12. How does the runtime handle Event declared-count mismatch?
13. How does the runtime handle a truncated Event tuple?
14. Does an unknown Event opcode invalidate the Trigger, become inert, or cause undefined behavior?
15. Is the Action tuple always exactly opcode plus seven parameter slots in every stock RA2/YR version?
16. How does the runtime handle Action declared-count mismatch?
17. How does the runtime handle a truncated Action tuple?
18. Does an unknown Action opcode invalidate the Trigger, become inert, or cause undefined behavior?
19. What is the exact stock meaning and encoding of Action parameter slot 7, including `A`?
20. Are unused Action parameter slots ignored, validated, or consumed by hidden behavior?
21. Are Event and Action orders always semantically significant in the runtime?
22. Is `[Tags]` exactly `persistence,name,TriggerID` for all stock RA2/YR versions?
23. What exact Tag persistence values and runtime semantics exist in RA2/YR?
24. Are Tag IDs case-sensitive, and how are duplicate Tag IDs handled?
25. Does a missing Tag target preserve an attached object/cell without activation, or invalidate the record?
26. How are Trigger-link and Tag/Trigger cycles handled by the original runtime?
27. What is the exact global-versus-map-local composition rule for TeamTypes, TaskForces, and ScriptTypes?
28. Does `-G` have any runtime significance or only authoring convention significance?
29. Does the original runtime accept TeamType list gaps and nonnumeric list keys?
30. What is the exact stock AITrigger 18-field contract, including fields 11 and 13?
31. What is the exact comparator blob layout, byte order, and reserved-tail meaning?
32. Which public evidence is sufficient to define the first compatibility-promotable vanilla layout profiles?

## P1 — required before semantic catalog or canonical writer design

33. Does FinalAlert regenerate Trigger IDs during ordinary save, only on clone/create, or under repair workflows?
34. Does FinalAlert preserve ID case exactly?
35. Does FinalAlert preserve duplicate Trigger, Tag, or TeamType IDs?
36. Does the runtime permit empty Trigger display names?
37. Does the Trigger owner field bind to House identity, HouseType/country, or another selector?
38. What happens when Trigger owner is missing or unknown?
39. Is the linked Trigger evaluated before, after, or independently of the current Trigger?
40. Is linked-Trigger execution recursive, queued, or state-linked?
41. Are Event count and tuple tokens signed or unsigned integers?
42. What maximum Event count does the runtime support safely?
43. What maximum Action count does the runtime support safely?
44. Are Event opcode values signed, unsigned, or simply parsed as decimal text?
45. Are Action opcode values signed, unsigned, or parsed by `atoi`-like behavior?
46. Are hexadecimal opcode or parameter spellings accepted?
47. Does whitespace around CSV tokens affect runtime parsing?
48. Are empty CSV tokens preserved as empty, interpreted as zero, or collapsed?
49. Are quoted CSV tokens supported anywhere in stock scenario records?
50. Does a semicolon inside an Event/Action parameter begin a comment or remain text?
51. Are extra Event tokens ignored, rejected, or consumed by a following tuple?
52. Are extra Action tokens ignored or treated as another Action despite the declared count?
53. Does the runtime validate unknown nonzero values in unused Action slots?
54. Which Action parameters are strings rather than numeric values in stock RA2/YR?
55. Which Event parameters reference Houses, Waypoints, TeamTypes, Triggers, or Rules types?
56. Which Action parameters reference Tags rather than Triggers?
57. Which Event/Action parameters use Rules registry ordinal versus logical name?
58. Are logical-ID comparisons case-sensitive for TeamType, TaskForce, and ScriptType references?
59. What exact sentinel forms are accepted per parameter slot?
60. Does waypoint `A` represent zero only in editor serialization, or is it a runtime encoding?
61. What is the exact relationship between local variable IDs and `[VariableNames]` keys?
62. Which section defines global variable identities in RA2/YR?
63. Are local variables boolean-only, integer, or profile dependent?
64. What state persists across missions, savegames, and campaign transitions?
65. How are duplicate variable IDs handled?
66. Are Trigger easy/normal/hard fields independent booleans or a mask interpreted together?
67. Which boolean spellings does stock runtime accept in Trigger and Team AI sections?
68. Does invalid boolean text map to zero, one, a default, or a parse failure?
69. Is TeamType list source order or numeric-key order authoritative?
70. May a TeamType per-ID section exist without a `[TeamTypes]` list entry and still be used?
71. What is the exact stock set of TeamType keys for RA2 versus YR?
72. Which TeamType keys are editor-only metadata?
73. Which TeamType flags are inherited from TS but ignored in RA2/YR?
74. How do duplicate TeamType properties within one section compose?
75. How do global and map-local duplicate TeamType IDs compose?
76. What exact waypoint sentinel and alphabetic/numeric forms does TeamType accept?
77. Is `TransportWaypoint` a YR-only field, an RA2 field, or editor extension?
78. How is `Group` shared, if at all, between placement units and TeamType recruitment?
79. What exact `VeteranLevel` values are accepted and how are invalid values handled?
80. Which TeamType flags are extension-defined by Ares/Phobos rather than vanilla?

## P1 — TaskForce, ScriptType, and AITrigger details

81. Is the six-entry TaskForce limit a hard runtime limit or editor convention?
82. Does the runtime stop TaskForce entry parsing at the first missing numeric key?
83. Are TaskForce entries after a gap ignored or accepted?
84. What happens with duplicate TaskForce entry keys?
85. Are zero or negative TaskForce counts accepted?
86. What happens when the same TechnoType appears in multiple TaskForce entries?
87. Which Rules type families are legal in TaskForces in stock RA2/YR?
88. Does a missing TaskForce type invalidate the entry, TaskForce, TeamType, or only future team creation?
89. Is TaskForce `Group` consumed by runtime or editor-only?
90. Is TaskForce `Name` ignored by runtime?
91. What is the stock maximum ScriptType step count?
92. Does stock runtime stop Script parsing at the first missing numeric key?
93. Are Script steps after a gap accepted?
94. What happens with duplicate Script step keys?
95. Are negative Script action opcodes accepted or wrapped?
96. Are Script arguments always signed 32-bit decimal values?
97. Which Script actions treat the argument as Waypoint, type, time, mission, or jump?
98. How are Script jumps/loops represented and bounded in stock runtime?
99. What happens when a Script references a missing Waypoint or type?
100. Are Script Action opcode ranges different between RA2 and YR?
101. Which high Script actions are strictly Ares/Phobos extensions?
102. Does a TeamType missing Script or TaskForce remain selectable by AI?
103. Are AITrigger IDs case-sensitive?
104. How do global and map-local duplicate AITrigger IDs compose?
105. Is `[AITriggerTypesEnable]` applied before or after map/global AITrigger composition?
106. Does a missing primary TeamType disable an AITrigger or cause another behavior?
107. Does a missing secondary TeamType act as none or error?
108. How is `OwnerRaw` interpreted relative to `SideRaw`?
109. What are the stock AITrigger condition-type numeric values and parameter contracts?
110. Which Rules type registries may appear in `ConditionObjectRaw`?
111. Is the comparator quantity signed, unsigned, or condition-dependent?
112. What do all comparator operator values mean in stock runtime?
113. Is the comparator tail ignored, reserved, or used by another condition type?
114. Are AITrigger weights floating-point, fixed-point, or parsed decimal doubles?
115. What happens when minimum weight exceeds maximum weight?
116. How are invalid, negative, NaN-like, or extremely large weight strings handled?
117. Which runtime events modify AITrigger weight?
118. Is AITrigger field 10 multiplayer/skirmish enablement, and does stock runtime enforce it?
119. Is field 13 truly base defense, version-specific, or unused?
120. What exact difficulty-flag and side selection logic applies to AITriggers?

## P2 — roundtrip, extensions, and executor research

121. Which section physical order, if any, is required by stock runtime?
122. Does FinalAlert preserve duplicate sections and physical record order?
123. Does FinalAlert preserve unknown Event/Action opcodes or refuse/rewrite the map?
124. Does FinalAlert preserve nonzero unused Action parameters?
125. Does FinalAlert canonicalize Event/Action counts?
126. Does FinalAlert reorder TeamType, TaskForce, or ScriptType entries?
127. Does FinalAlert drop Script steps after a gap?
128. Does FinalAlert preserve unknown TeamType flags and AITrigger tail fields?
129. What must be retained for byte-identical roundtrip: comments, whitespace, section order, duplicate keys, or file encoding?
130. Can semantic roundtrip succeed while byte-identical roundtrip fails, and what compatibility grade should that receive?
131. What exact Ares versions introduced each extended Event and Action layout?
132. What exact Phobos versions introduced each high Script action and AI behavior?
133. How should mixed Ares/Phobos profiles be versioned and selected?
134. Can extension metadata identify its own version without probing opcodes?
135. How should unknown future extension opcodes be preserved in a canonical writer?
136. Which opcode names/descriptions can be distributed without importing a GPL editor catalog?
137. What independent fixture corpus can validate opcode storage without using original map records?
138. Can a permissively licensed factual opcode catalog be constructed from primary extension documentation?
139. What original-runtime evidence exists for Event persistence, incidental callbacks, and AND/OR behavior?
140. Where is Trigger repeatability state stored at runtime and in savegames?
141. How are elapsed-time Events synchronized in multiplayer?
142. How are Event checks ordered relative to simulation ticks and object destruction?
143. Are Actions executed immediately, queued, or split across frames?
144. How are recursive Trigger activations bounded?
145. How are Trigger state and variables serialized in savegames?
146. How are object identity references restored after load?
147. How does runtime handle a Trigger whose referenced object is destroyed before activation?
148. How does runtime select and create a Team from an AITrigger deterministically?
149. How are AITrigger weights updated after team success, failure, or timeout?
150. How do TeamType recruitment and placed-unit `Group`/recruitable fields interact?
151. How do TaskForce counts interact with production availability and missing TechnoTypes?
152. How are Script instruction pointers saved and restored?
153. How does pathfinding failure affect Script progression?
154. Which execution commands must be deterministic command-sink operations rather than direct engine mutation?
155. What world-query interfaces are minimally required by each vanilla Event opcode?
156. What command interfaces are minimally required by each vanilla Action opcode?
157. Which runtime actions require UI/audio/video adapters but no simulation mutation?
158. Which Trigger/Team behaviors differ between campaign and skirmish?
159. Which behaviors differ between RA2 and YR executables?
160. What multiplayer desynchronization hazards arise from extension Events/Actions?
161. What validation is required before enabling any graph for execution?
162. Should unknown opcodes block the entire graph, only one Trigger, or one tuple in a future executor?
163. Can a graph with dangling references remain inspectable but explicitly non-executable?
164. What savegame-version policy is needed when opcode catalogs change?
165. How should future audit evidence be aggregated without revealing rare opcode fingerprints?
166. What minimum sample diversity is necessary to distinguish editor conventions from stock-map conventions?
167. Can public test maps with permissive licenses provide non-stock execution fixtures?
168. What legal review is needed before distributing large community-derived opcode tables?
169. Which findings, if any, can later promote compatibility without running original executables?
170. What exact acceptance criteria distinguish parse support, semantic binding, editor reopen, runtime acceptance, and gameplay equivalence?

## Resolution discipline

For every future answer, record:

- source and permanent locator;
- version/commit;
- reader/writer/editor/runtime category;
- license;
- independent/shared lineage;
- evidence grade;
- whether the conclusion changes only a profile or a public compatibility status.

ProjectBaseline observations remain `ObservedByFutureProjectBaselineAudit`. They cannot alone close an original-runtime question.
