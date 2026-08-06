# Unresolved questions — 170 items

> **Source notice:** ChatGPT Web public-source research. ProjectBaseline was not read. This is not a Codex artifact. No GPL or unclear-license code was copied, translated, or mechanically ported. `code_imported: false`.

## Status convention

- **P0** — blocks a trustworthy first implementation or compatibility claim.
- **P1** — required for broad stock RA2/YR fidelity.
- **P2** — required for robust editor/client/extension interoperability.
- **P3** — useful for canonical writing, diagnostics, or later simulation work.

No question is answered by naming the most common behavior. Editor defaults, client behavior, community observations, and future ProjectBaseline audit results retain their own evidence grades.

# P0 — format and initialization blockers (40)

1. What are the exact original-runtime meanings of `[Map] Size` fields 0 and 1?
2. Does original RA2/YR runtime ignore nonzero Size origins, reject them, or use them?
3. Is `Size=x,y,width,height` an official runtime rectangle or only an editor/community model?
4. What exact runtime systems consume `LocalSize`?
5. Must LocalSize be contained within Size in stock runtime?
6. What happens when LocalSize is partly or wholly outside Size?
7. Are zero or negative Size/LocalSize dimensions accepted, rejected, wrapped, or clamped?
8. Which map dimension limits are format facts, runtime limits, editor limits, or client limits?
9. Is width-plus-height below 512 a stock runtime requirement or only a FinalAlert authoring constraint?
10. How does original runtime compare Theater tokens: exact case, case-insensitive, alias table, or another rule?
11. What does stock runtime do with an unknown Theater token?
12. Are all six RA2/YR theater profiles bound by the same mechanism?
13. Is any `.ubn → .urb` fallback present in stock runtime, or only editor/tool compatibility paths?
14. What is the exact RA2/YR role of `[Houses]`: House instances, definitions, or profile-dependent registry?
15. What is the exact RA2/YR role of `[Countries]` in scenario files?
16. Does numeric list ordinal in `[Houses]` have runtime identity significance?
17. Does numeric list ordinal in `[Countries]` have runtime identity significance?
18. How does stock runtime handle list gaps, duplicate ordinals, and keys such as `1` versus `01`?
19. Are House and Country IDs case-sensitive in original runtime?
20. How does runtime handle duplicate logical House identities?
21. How does runtime handle duplicate logical Country identities?
22. Does a listed House require a same-named section?
23. Can an unlisted House section still create a runtime House?
24. Can a map-local Country modify a global Rules Country without appearing in `[Countries]`?
25. What is the exact global Rules versus map-local Country composition order?
26. How are House instance, Country definition, Side, and special House selector represented internally by stock runtime?
27. What are the authoritative stock identities and semantics of Neutral, Special, and civilian Houses?
28. Does missing or invalid `Country=` cause rejection, fallback, or undefined behavior?
29. Is WAE's fallback to the first standard Country purely recovery behavior?
30. What exact field selects the campaign-controlled House: `[Basic] Player`, House control fields, executable context, or a combination?
31. What precedence exists between `[Basic] Player`, `PlayerControl`, `Human`, and session assignment?
32. Does stock runtime interpret House `Allies` as directed or symmetric?
33. Does runtime automatically add reciprocal alliance edges?
34. Is self-alliance required, optional, ignored, or special?
35. What happens when an Allies token references a missing House?
36. What is the exact load-time interaction between `Allies` and `SpecialFlags.FixedAlliance`?
37. Which Waypoint IDs are stock multiplayer starting positions in RA2 and YR?
38. How does stock runtime handle missing, duplicate, or out-of-domain start Waypoints?
39. What is the authoritative precedence among map starts, lobby selections, and generated player Houses?
40. What minimum evidence is required before any metadata/House compatibility status can be promoted?

# P1 — stock RA2/YR semantic fidelity (50)

41. Which `[Basic]` keys are read by RA2, YR, both, or neither?
42. What are the exact runtime types and ranges of `Percent`, `InitTime`, `MinPlayer`, and `MaxPlayer`?
43. Is `Author` stock map metadata, editor/client metadata, or ignored by the original game?
44. How is `Name` resolved against string tables, direct text, or client translation metadata?
45. What are the exact semantics of `Brief`, `Intro`, `Win`, `Lose`, and `Action` references?
46. How is `Theme` resolved and when is it consumed?
47. What exact units and rounding apply to `CarryOverMoney`?
48. How does `CarryOverCap` interact with authored House Credits and savegame state?
49. What is the exact runtime meaning of `Percent` in each scenario profile?
50. Is `EndOfGame` consumed by map load, campaign control, or Trigger/campaign completion logic?
51. What does `SkipScore` change in stock runtime?
52. What does `OneTimeOnly` change and where is its state persisted?
53. What are the exact meanings of `HomeCell` and `AltHomeCell`?
54. Are HomeCell values Waypoint IDs, ScenarioCell IDs, camera slots, or another identity?
55. What does `RequiredAddOn` mean in RA2 versus YR?
56. Which `NewINIFormat` values are accepted by stock RA2/YR?
57. Does NewINIFormat affect metadata parsing beyond packed-map formats?
58. What exact runtime behavior is associated with `MultiplayerOnly`?
59. Is `Official` read by the game, file transfer logic, launcher, or all of them?
60. How are `GameMode` and `GameModes` interpreted by stock game versus clients?
61. Does `[Map]` contain stock fields beyond Size, LocalSize, and Theater that affect initialization?
62. What is the runtime meaning of `MapScale` where present?
63. What is the runtime meaning of `VeteranRatio` where present?
64. What is the runtime meaning of `BaseNormal` where present?
65. How does runtime derive camera or scroll bounds from Size and LocalSize?
66. How does runtime validate ScenarioCell references against geometry?
67. Does runtime require an IsoMap cell at a player start?
68. Can a valid start lie outside LocalSize but inside Size?
69. How are House IDs allocated internally from list ordinals and names?
70. Can multiple Houses share one Country without losing trigger or ownership functionality?
71. Which Trigger/Event parameters refer to House instances versus Country/HouseType definitions?
72. How is `ParentCountry` applied at runtime?
73. Does Country property inheritance happen before or after map-local overrides?
74. How are `Side` and `ActsLike` related in RA2/YR profiles?
75. Which Country fields determine multiplayer side selection?
76. How is House `Color` bound to `[Colors]` in stock runtime?
77. Can map-local `[Colors]` override or extend the Rules color registry?
78. What happens when a House Color is missing or unknown?
79. What is the exact unit and range of House `Credits`?
80. Are negative or overflowing Credits accepted or wrapped?
81. What runtime systems consume House `IQ` during initialization?
82. Is House `TechLevel` an authored cap, initial state, AI input, or obsolete field?
83. What is the exact runtime meaning and range of `PercentBuilt`?
84. What is the exact runtime meaning of House `Edge`?
85. Does House `PlayerControl` affect campaign, skirmish, multiplayer, or only some profiles?
86. Is a separate `Human` property recognized by stock RA2/YR?
87. What is the stock runtime purpose of `MultiplayPassive` or similarly named fields?
88. Are NodeCount and indexed base nodes authoritative, advisory, or AI-only?
89. How does runtime handle base-node gaps, duplicates, and NodeCount mismatch?
90. Which House statistics fields are load inputs versus stale/editor-maintained outputs?

# P2 — multiplayer, client, extension, and composition interoperability (45)

91. Is `[MultiplayerDialogSettings]` ever read directly from a scenario file by stock RA2/YR?
92. What is the exact precedence of Rules, map-local Rules, game-mode INI, spawn INI, client, and lobby settings?
93. Which MultiplayerDialogSettings key spellings are accepted by stock RA2/YR?
94. What are the exact default and range of multiplayer money/credits?
95. How are short game, superweapons, crates, bases, and allied building options serialized into a session?
96. What exact stock key controls MCV redeploy/repack, and does the spelling differ by version?
97. What is the stock behavior of BridgeDestruction or DestroyableBridges across settings sources?
98. How do MultiEngineer and HarvesterTruce interact with map metadata?
99. How are Shroud and FogOfWar settings distinguished in RA2/YR?
100. Can a client remove start Waypoints without invalidating original scenario behavior?
101. How do CnCNet clients map player slot, side, color, start, and team into generated spawn/session data?
102. Which of those generated values correspond to stock executable inputs?
103. How are observer slots represented in stock or client launch data?
104. How are AI slots represented, and how do they relate to House IQ or AITrigger definitions?
105. What is the stock maximum number of player slots for each executable/profile?
106. Can extensions raise player, start-Waypoint, House, or Country limits?
107. How do fixed-start and random-start client modes modify the scenario before launch?
108. What is the exact relationship between lobby team numbers and initial House alliances?
109. Does FixedAlliance prevent only player diplomacy changes or also Trigger actions?
110. How do cooperative clients map multiple humans onto authored Houses?
111. Can a co-op session assign multiple peers to one House?
112. How are generated multiplayer Houses named and indexed?
113. How are generated Houses bound to selected Countries and colors?
114. Which starting-unit policies are stock Rules behavior versus client extensions?
115. How are starting MCVs/buildings positioned relative to selected Waypoint cells?
116. Does starting facing come from map data, Country policy, or runtime generation?
117. Which scenario fields are added by CnCNet clients and ignored by stock runtime?
118. Which fields are Ares-only extensions in Basic, Map, House, Country, or SpecialFlags?
119. Which fields are Phobos-only extensions, and from which version?
120. How should extension profiles identify themselves without trial parsing?
121. What map-local section classification rules are required for Ares/Phobos custom House/Country data?
122. Can a section simultaneously carry House-instance and Rules-type properties?
123. How should per-key composition handle such shared-name section collisions?
124. Does map-local Rules composition occur before House/Country registries are instantiated?
125. How do map-local Country additions interact with global numeric registries?
126. Are list ordinals stable across global and map-local composition?
127. What happens when a map-local Country ID collides case-insensitively with a global ID?
128. Which editor-private metadata sections should a lossless writer preserve but a runtime view ignore?
129. How do launcher databases classify one file for multiple game modes?
130. Can `.MPR` or `.YRM` files be registered as campaign/client missions?
131. Which `.MAP` files are multiplayer-only despite generic extension?
132. How do `.MMX` and `.YRO` containers affect provenance but not scenario semantics?
133. What exact client data decides official versus custom map transfer behavior?
134. What is the extension behavior for unknown Theater profiles?
135. Which extension profile owns custom theater resource and movement semantics?

# P3 — integrity, environment, writing, and diagnostic details (35)

136. What algorithm produces `[Digest]` in each game/editor profile?
137. Which bytes or canonical text are covered by Digest?
138. Is Digest computed before or after line-ending, key-order, or whitespace normalization?
139. Is Digest Base64, another encoding, or profile-dependent?
140. Does stock runtime enforce Digest, ignore it, or conditionally verify it?
141. Does FinalAlert recalculate, preserve, or remove Digest on save?
142. How do CnCNet clients use Digest versus their own map hashes?
143. How should duplicate Digest sections or keys be treated by consumers?
144. Can Digest be preserved byte-identically after semantic edits elsewhere?
145. Which `[Lighting]` fields are stock RA2/YR map-global inputs?
146. What are the exact numeric formats and ranges for ambient and RGB lighting fields?
147. How do normal, storm, and alternate lighting profiles coexist?
148. Which Lighting fields affect aircraft, ground, and level independently?
149. What is the relationship between Theater defaults and explicit Lighting overrides?
150. Which weather fields belong to SpecialFlags, Lighting, Rules, or extension profiles?
151. Which Basic growth flags are TS-only, WAE-only, or meaningful in RA2/YR?
152. How do TiberiumGrows/TiberiumSpreads names map to ore behavior in RA2 mode?
153. Which SpecialFlags are ignored in campaign, skirmish, or multiplayer contexts?
154. Is FogOfWar read from SpecialFlags, multiplayer settings, both, or context-dependent?
155. How do Inert and InitialVeteran affect original runtime initialization?
156. Are IonStorms/WeatherStorms meaningful in stock RA2/YR or only inherited/editor fields?
157. What exact values and spellings are accepted for every SpecialFlag boolean?
158. Does original runtime preserve or ignore unknown SpecialFlags?
159. What physical section order, if any, is required for Basic, Map, Houses, Countries, and Digest?
160. Does original runtime use first-wins, last-wins, or another rule for duplicate metadata sections?
161. Does original runtime use first-wins, last-wins, or another rule for duplicate keys?
162. Can a byte-identical roundtrip preserve all editor-private and extension metadata after semantic analysis?
163. What is the canonical FinalAlert ordering for House, Country, and property sections?
164. Does FinalAlert renumber House/Country list keys during save?
165. Does FinalAlert symmetrize, sort, deduplicate, or add self entries to Allies?
166. Does FinalAlert recalculate MaxPlayer from Waypoints in stock releases, or is this WAE-specific?
167. Which malformed values can FinalAlert reopen without rewriting?
168. Which malformed values can stock runtime accept despite editor rejection?
169. What sanitized aggregate evidence can distinguish writer conventions from runtime constraints without identifying maps?
170. What additional official, disassembly-independent, or legally publishable evidence would resolve the remaining P0 questions?

## Promotion rule

No answer is promoted because:

- most samples share it;
- multiple tools copied the same behavior;
- an editor repairs input that way;
- a client requires it;
- a field name appears intuitive;
- future ProjectBaseline aggregates observe it.

Compatibility promotion requires explicit evidence review and a separate change outside this research PR.
