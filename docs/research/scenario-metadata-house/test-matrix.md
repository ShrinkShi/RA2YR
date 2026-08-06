# Test matrix — 160 design cases

> **Source notice:** ChatGPT Web public-source research. ProjectBaseline was not read. This is not a Codex artifact. No GPL or unclear-license code was copied, translated, or mechanically ported. `code_imported: false`.

## Test policy

These are design cases only. No test code is implemented in this research.

All fixtures must be synthetic and independently constructed. Fixture builders must not reuse production rectangle, identity, alliance, Waypoint, boolean, mode-resolution, or composition algorithms.

Expected results distinguish:

- lossless raw parse;
- typed candidate creation;
- unique semantic binding;
- consistency analysis;
- future initialization availability;
- byte-identical or semantic roundtrip.

No test creates a Unity object, runtime player, network peer, alliance state, starting unit, campaign session, or AI.

## Distribution

| Category | Count |
|---|---:|
| Basic and Map metadata | 26 |
| Size, LocalSize, and Theater | 24 |
| House and Country identity | 26 |
| House properties and player control | 22 |
| Alliances | 18 |
| Multiplayer, starts, and game modes | 28 |
| Safety, roundtrip, architecture, and audit | 16 |
| **Total** | **160** |

# A. Basic and Map metadata — 26

1. **M001** — A single `[Basic]` section with unknown keys preserves key casing, order, and raw values.
2. **M002** — Duplicate `[Basic]` sections remain separate occurrences and produce semantic ambiguity.
3. **M003** — Duplicate `Name` keys remain separate; no first/last-wins selection occurs by default.
4. **M004** — Empty `Player=` is retained and not normalized to `none`.
5. **M005** — `Player=none` remains raw and requires a profile before sentinel interpretation.
6. **M006** — Invalid boolean spelling in `MultiplayerOnly` produces a candidate failure without replacement default.
7. **M007** — `Official=YES`, `Official=1`, and `Official=true` remain distinct raw spellings under equivalent candidate values.
8. **M008** — Negative `MinPlayer` is retained and diagnosed.
9. **M009** — `MaxPlayer` integer overflow produces no derived player-count value.
10. **M010** — `MinPlayer > MaxPlayer` yields a consistency conflict without swapping values.
11. **M011** — `NewINIFormat=4` creates a format-profile candidate but does not select all map semantics automatically.
12. **M012** — `NewINIFormat=5` does not silently enable every WAE extension.
13. **M013** — Unknown `NewINIFormat` remains raw and requests an explicit profile.
14. **M014** — Decimal `CarryOverMoney` preserves exact spelling and invariant numeric candidate.
15. **M015** — Locale-style `CarryOverMoney=1,5` is not silently parsed as 1.5.
16. **M016** — Negative `CarryOverCap` remains raw and is not clamped.
17. **M017** — Campaign media fields remain logical references and do not load files.
18. **M018** — `GameMode` and `GameModes` coexist without one replacing the other.
19. **M019** — `Official=yes` plus `.MPR` evidence is retained as potentially conflicting mode evidence.
20. **M020** — Missing `[Basic]` permits raw Map parsing but reports unavailable Basic semantic view.
21. **M021** — A single valid `[Map]` section preserves all properties.
22. **M022** — Duplicate `[Map]` sections produce ambiguity without composition.
23. **M023** — Missing `Size` prevents geometry descriptor creation while retaining Map raw data.
24. **M024** — Missing `LocalSize` prevents playable-area candidate creation without fabricating one from Size.
25. **M025** — Missing `Theater` prevents theater binding without defaulting to Temperate.
26. **M026** — Unknown Map fields survive semantic analysis and lossless roundtrip.

# B. Size, LocalSize, and Theater — 24

27. **M027** — `Size=0,0,80,80` produces four raw fields and the leading origin-plus-dimensions candidate.
28. **M028** — Nonzero `Size=10,20,80,80` preserves origin values even when the WAE profile ignores them.
29. **M029** — `Size` with three tokens is malformed and is not padded.
30. **M030** — `Size` with five tokens preserves the extra token and does not truncate.
31. **M031** — Empty middle `Size` token is preserved and prevents unique numeric interpretation.
32. **M032** — Negative Size origin remains a signed candidate and is not clamped.
33. **M033** — Negative Size width produces no valid positive-dimension descriptor.
34. **M034** — Zero Size width is classified separately from malformed syntax.
35. **M035** — Size width-plus-origin arithmetic overflow produces a structured diagnostic.
36. **M036** — Size area multiplication overflow cannot drive allocation.
37. **M037** — Valid nonzero LocalSize origin is preserved.
38. **M038** — LocalSize equal to Size is classified as contained/equal.
39. **M039** — LocalSize fully inside Size is classified as contained.
40. **M040** — LocalSize partially outside Size is reported without clamping.
41. **M041** — LocalSize completely outside Size is retained as an invalid relationship candidate.
42. **M042** — LocalSize with negative origin is analyzed under explicit signedness policy.
43. **M043** — LocalSize zero width/height is not repaired from Size.
44. **M044** — Geometry interpretation remains ambiguous when candidate rectangle layouts disagree.
45. **M045** — IsoMap record count is not derived from Size area.
46. **M046** — Preview Size is not substituted for Map Size.
47. **M047** — `Theater=TEMPERATE` binds the logical Temperate profile without loading resources.
48. **M048** — Case-variant theater token preserves raw casing and records comparison candidates.
49. **M049** — Unknown theater returns unresolved and does not fall back to Temperate.
50. **M050** — `.ubn → .urb` is available only under an explicit editor compatibility profile.

# C. House and Country identity — 26

51. **M051** — `[Houses] 0=Alpha` creates separate raw list-key and logical-name identities.
52. **M052** — House list gap `0,2` is preserved and not compressed.
53. **M053** — Duplicate raw House key remains duplicated.
54. **M054** — Keys `1` and `01` produce a normalized ordinal collision while preserving raw spelling.
55. **M055** — Duplicate House logical name at two ordinals produces an ambiguous identity group.
56. **M056** — `Alpha` and `ALPHA` produce a case-collision candidate under case-insensitive policy.
57. **M057** — Listed House with missing property section remains an identity with unresolved section.
58. **M058** — Unlisted section containing House-like fields is reported as a candidate, not auto-registered.
59. **M059** — Duplicate House sections retain all property occurrences.
60. **M060** — House section name collision with an unrelated Rules type preserves per-key provenance.
61. **M061** — `[Countries]` list gaps remain intact.
62. **M062** — Duplicate Country ordinal is reported without renumbering.
63. **M063** — Duplicate Country logical name remains ambiguous.
64. **M064** — Listed Country with missing section is retained as unresolved.
65. **M065** — Unlisted map-local Country section remains a candidate only.
66. **M066** — House `Country=` binds uniquely to a map-local Country candidate.
67. **M067** — House `Country=` binds uniquely to a global Rules Country candidate.
68. **M068** — Global and map-local Country candidates preserve winner and suppressed provenance under explicit composition.
69. **M069** — Missing Country remains dangling and does not fall back to the first standard Country.
70. **M070** — Unknown `ParentCountry` remains a dangling definition reference.
71. **M071** — Country-to-Side binding remains separate from House-to-Country binding.
72. **M072** — Two Houses referencing one Country remain two House identities.
73. **M073** — Neutral candidate is recognized only by an explicit profile, not by unknown-name fallback.
74. **M074** — Special selector candidate remains separate from a real House section.
75. **M075** — Civilian identity candidate is not inferred solely from Country or color.
76. **M076** — House ordinal is not automatically interpreted as multiplayer player slot.

# D. House properties and player control — 22

77. **M077** — Unknown House property remains raw and roundtrips.
78. **M078** — Duplicate House property key remains ambiguous.
79. **M079** — Invalid `IQ` preserves raw text and no numeric value.
80. **M080** — Negative `TechLevel` is not clamped.
81. **M081** — `PercentBuilt=150` remains raw and is not constrained to 100 without a profile.
82. **M082** — Unknown `Edge` remains a string candidate.
83. **M083** — Negative `Credits` remains raw and does not become zero.
84. **M084** — Credits overflow produces a diagnostic without allocation or wrap.
85. **M085** — House Credits, carry-over money, and lobby money remain three independent sources.
86. **M086** — Invalid Color reference remains dangling and does not select red/white.
87. **M087** — Missing `[Colors]` registry does not invalidate House identity.
88. **M088** — Color does not infer House or Country identity.
89. **M089** — `NodeCount` smaller than actual node entries produces a mismatch without deletion.
90. **M090** — Base-node gap preserves later entries.
91. **M091** — Duplicate base-node index remains duplicated.
92. **M092** — Unknown base-node BuildingType remains a raw template reference.
93. **M093** — Out-of-domain base-node coordinate remains raw and diagnosed.
94. **M094** — `[Basic] Player=Alpha` creates an authored player-House candidate only.
95. **M095** — Basic Player references missing House and remains dangling.
96. **M096** — Basic Player conflicts with House `PlayerControl=yes` on another House and no controller is selected.
97. **M097** — Multiple Houses with `PlayerControl=yes` produce a controller ambiguity.
98. **M098** — Session local-player assignment remains external and can differ from authored candidates without mutating them.

# E. Alliances — 18

99. **M099** — `Allies=A,B` creates two ordered directed raw edges.
100. **M100** — Empty Allies token is preserved and diagnosed.
101. **M101** — Trailing comma in Allies is preserved.
102. **M102** — Duplicate ally token creates duplicate-edge analysis without deduplication.
103. **M103** — Self ally is preserved and classified.
104. **M104** — Missing ally target remains a dangling directed edge.
105. **M105** — Ally case mismatch creates exact and case-insensitive resolution candidates.
106. **M106** — Ally case collision remains ambiguous.
107. **M107** — Duplicate `Allies` keys remain separate property occurrences.
108. **M108** — A→B with B→A is classified as symmetric.
109. **M109** — A→B without B→A is classified as asymmetric and not repaired.
110. **M110** — Same Country does not create an alliance edge.
111. **M111** — Same Side does not create an alliance edge.
112. **M112** — Same Color does not create an alliance edge.
113. **M113** — Lobby team number does not modify authored Allies during parsing.
114. **M114** — `FixedAlliance=yes` is retained separately from alliance graph symmetry.
115. **M115** — Invalid FixedAlliance boolean does not default to false.
116. **M116** — Trigger-driven future alliance changes are not executed during metadata parsing.

# F. Multiplayer, starts, and game modes — 28

117. **M117** — Rules-level MultiplayerDialogSettings is distinguished from map-authored section of the same name.
118. **M118** — Client/lobby setting with same key as Rules default retains separate provenance.
119. **M119** — Unknown client-only multiplayer field remains raw and is not promoted to stock format.
120. **M120** — Lobby money conflicts with House Credits without overwriting it.
121. **M121** — Lobby TechLevel conflicts with House TechLevel without computing buildability.
122. **M122** — `MinPlayer`/`MaxPlayer` conflict with client hard maximum and all evidence remains.
123. **M123** — Basic MaxPlayer conflicts with count of start candidates.
124. **M124** — Waypoint 0 binds to start slot 0 only under an explicit multiplayer-start profile.
125. **M125** — Low-numbered Waypoint in campaign profile is not automatically a start.
126. **M126** — Missing start Waypoint for a declared slot produces a diagnostic without synthesis.
127. **M127** — Duplicate Waypoint identity produces ambiguous start binding.
128. **M128** — Two start slots resolving to the same cell are retained and diagnosed.
129. **M129** — Start outside Size remains raw and invalid for selected geometry.
130. **M130** — Start inside Size but outside LocalSize is classified separately.
131. **M131** — Start with no IsoMap cell is preserved and diagnosed.
132. **M132** — Geometry ambiguity prevents automatic start-domain validation.
133. **M133** — House StartLocation candidate conflicts with low-numbered Waypoint candidate without precedence.
134. **M134** — Client RemoveStartingLocations option is treated as a future launch mutation, not source parse behavior.
135. **M135** — Fixed-start and random-start evidence conflict without selecting an algorithm.
136. **M136** — File extension `.MPR` contributes multiplayer evidence but does not guarantee final classification.
137. **M137** — `.MAP` plus campaign registration contributes campaign evidence.
138. **M138** — `.MAP` plus multiplayer client registration produces multiple mode candidates.
139. **M139** — `MultiplayerOnly=yes` plus campaign-control registration produces a mode conflict.
140. **M140** — Basic Player plus multiplayer metadata produces competing campaign/multiplayer evidence.
141. **M141** — Skirmish and online multiplayer remain separable by session context even with the same map.
142. **M142** — Cooperative classification requires combined evidence and is not inferred from two allies alone.
143. **M143** — Tutorial/client challenge category remains external evidence and does not alter map metadata.
144. **M144** — Unknown SpecialFlag and duplicate Digest are retained while mode resolution proceeds independently.

# G. Safety, roundtrip, architecture, and audit — 16

145. **M145** — Section-count budget fails with structured diagnostic before unbounded allocation.
146. **M146** — House-count budget fails without partial semantic success.
147. **M147** — Alliance-edge budget fails without an unbounded graph.
148. **M148** — Raw value-length budget rejects oversized input while preserving failure provenance.
149. **M149** — Rectangle checked addition detects overflow.
150. **M150** — Total token-count checked arithmetic detects overflow.
151. **M151** — Streaming parser makes progress or terminates; a zero-consumption loop is rejected.
152. **M152** — In-memory and seekable Stream inputs produce equivalent raw and semantic results.
153. **M153** — Short-read Stream produces the same result as normal Stream.
154. **M154** — Bounded MIX window produces the same result and cannot read beyond its window.
155. **M155** — MIX filename/container context does not silently select scenario mode or theater.
156. **M156** — Lossless roundtrip preserves duplicate sections, keys, casing, invalid values, gaps, and asymmetric allies.
157. **M157** — Canonical editor rewrite is labeled non-byte-identical and requires explicit policy.
158. **M158** — No parser or analysis result references `UnityEngine` or creates Player/GameObject/Camera/UI objects.
159. **M159** — No test executes SpecialFlags, economy, alliances, network, starting units, campaign progression, or AI.
160. **M160** — Sanitized audit output contains only approved aggregates and cannot reconstruct a specific scenario.

## Required result assertions

Across the matrix:

- raw source survives failed semantic interpretation;
- no automatic correction is performed;
- every selected interpretation records its policy and evidence grade;
- unknown fields and references remain available for roundtrip;
- missing or ambiguous identity prevents unique initialization without deleting input;
- reader budgets are enforced before allocation;
- input modes share one state machine;
- no future simulation/session behavior is executed.
