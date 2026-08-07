# Unresolved questions

> **Source notice:** ChatGPT Web public-source research only. ProjectBaseline was not read. This is not a Codex Agent artifact. No GPL or unclear-license code was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

No question in this file is silently converted into a vanilla default. Future answers must carry a source, revision, evidence grade, and affected profile.

## P0 — format and runtime-critical

1. Does the original RA2/YR runtime preserve empty placement tokens, or does it use a parameter getter that substitutes defaults?
2. Are techno placement record keys truly arbitrary unique strings, numeric ordinals, or runtime object identities?
3. Does runtime resolution of Unit `FollowsIndex` use source occurrence order, numeric record key, canonical list order, or another index?
4. Can a record key be nonnumeric in every RA2/YR techno section?
5. How does the runtime handle duplicate raw keys and normalized collisions such as `1` versus `01`?
6. Does runtime use first-wins, last-wins, source-order iteration, or undefined behavior for duplicate entries?
7. Are 17/14/14/12 exact runtime field counts or only canonical editor output?
8. Which missing trailing fields, if any, receive runtime defaults?
9. Are extra trailing fields ignored, preserved by editors, consumed by extensions, or fatal?
10. Is quoting ever recognized inside these comma records?
11. Is the external combined cell encoding universally `Y*1000+X` for RA2 and YR?
12. Does any original path use `X*1000+Y` or axis-swapped editor coordinates?
13. What exact integer type and signedness does the runtime use for scenario-cell IDs?
14. What happens when X or Y is negative or a radix component exceeds 999?
15. What are the exact runtime-valid Infantry subcell values?
16. Does runtime relocate or reject infantry with invalid or duplicate subcells?
17. Is placement health always interpreted as 1/256 of `Strength=` in RA2/YR?
18. How does runtime round derived hit points from health scale and Strength?
19. Are health 0, negative, above 256, or overflow values accepted, clamped, wrapped, or fatal?
20. What exact facing range and normalization does runtime apply to each family?
21. Are building, vehicle, infantry, and aircraft facing interpreted identically?
22. What mission token grammar and complete mission set does the runtime recognize for preplaced objects?
23. What happens to unknown or empty missions?
24. Does the runtime require a declared house for every Owner token, or can it construct/resolve implicit houses?
25. What are the exact Neutral, Special, and civilian house identity rules across RA2 and YR?
26. How are duplicate map house identities resolved?
27. Does runtime bind placement type by logical name alone, registry membership, registry ordinal, or both?
28. Can a map-local type section work without a map-local registry entry?
29. How exactly are map-local registry additions composed with global registries?
30. Are the official editor's special internal map-local type ranges editor-only?
31. Does an unknown placement type cause the runtime to skip the object, reject the map, or fail later?
32. What exact Tag none/null spellings are accepted by runtime?
33. Does a missing Tag leave the object untagged, invalidate the object, or cause another behavior?
34. Does a missing Trigger behind a valid Tag affect placement loading?
35. Are Waypoint keys parsed strictly as numeric slots, and what is the runtime maximum?
36. How does runtime handle duplicate waypoint IDs?
37. How does runtime handle CellTags at invalid cells or with missing Tag IDs?
38. Are Terrain and CellTag combined cell keys parsed arithmetically or by fixed decimal string slicing?
39. What is the exact Smudge fourth field semantics in RA2/YR?
40. Does runtime accept multiple Terrain or Smudge records at the same cell?

## P1 — record-layout and editor conflicts

41. Is Structure field 8 (`AI_REBUILDABLE`) read by the runtime or retained only for compatibility?
42. What are the exact boolean token spellings accepted in placement records?
43. What is the runtime behavior when UpgradeCount disagrees with upgrade tokens?
44. Can upgrade tokens reference types absent from BuildingTypes but present as sections?
45. What values and behavior are valid for Spotlight?
46. Does Nominal affect only UI, and is it honored in all RA2/YR modes?
47. Are AIRepairable, AISellable, Powered, and AIRebuildable normalized by FinalAlert on save?
48. Does Unit `High` represent bridge layer only, and what values beyond 0/1 mean?
49. Does Infantry `High` use the same semantics as Unit `High`?
50. Why is `High` absent from the common Aircraft profile?
51. What is the exact range and scale of Veterancy?
52. Is Group signed, and what sentinels besides `-1` are meaningful?
53. Are the two recruitment flags present in stock RA2 as well as YR, or added by a later editor/version?
54. What happens when recruitment flags are absent?
55. Can Unit records contain a turret-facing extension field in any stock map format?
56. Can Aircraft records contain height, altitude, or bridge-state extensions?
57. Are JumpJet vehicles serialized under Units exclusively in stock RA2/YR?
58. Which TS fields remain accepted but unused in RA2/YR?
59. Do Ares or Phobos append fields to these existing sections or define separate sections?
60. Which WAE/MapTool trailing fields are editor-only metadata?
61. Does FinalAlert renumber techno and smudge keys on every save?
62. Does FinalAlert preserve nonnumeric keys?
63. Does FinalAlert preserve duplicate keys or collapse them through its INI map structure?
64. Does FinalAlert preserve empty and trailing CSV fields?
65. Does FinalAlert preserve unknown extension tails?
66. Does WAE's `RemoveEmptyEntries` materially misparse any valid stock records?
67. Does MapTool's mission enum omit valid runtime mission spellings?
68. Do public tools agree on case-insensitive owner/type/mission matching because of shared community assumptions or independent behavior?

## P1 — coordinate and domain

69. Are direct techno X/Y coordinates exactly the same cell space as Terrain/Waypoint decoded X/Y?
70. Are those coordinates exactly the same as IsoMapPack5 raw X/Y?
71. Are there valid scenario coordinates with no IsoMap record in sparse maps?
72. Can placements exist outside `[Map] Size` but still be loaded by runtime?
73. Can placements outside `LocalSize` be intentionally used for reinforcements or staging?
74. Does map Level or bridge state affect validity of placement X/Y?
75. How are upper/lower bridge occupants represented beyond `High`?
76. Are aircraft positions validated against terrain cells at load time?
77. Are Terrain/Smudge coordinates allowed on diamond-empty storage cells?
78. How does runtime handle map resize leftovers?
79. Is decimal radix 1000 fixed in every Westwood isometric game or only TS/RA2/YR?
80. Does any parser accept leading plus signs, hexadecimal, or whitespace in cell IDs?
81. Are leading zeroes significant for cell IDs or waypoint IDs?

## P1 — owner and registry composition

82. Does `[Houses]` list order have runtime identity significance beyond names?
83. How do `[Countries]`, `[Houses]`, country sections, and house sections interact in YR map-local content?
84. Can multiple houses share one country/type safely?
85. Are Owner comparisons case-insensitive in original runtime?
86. Can an owner token reference a country directly when no map house instance exists?
87. How are multiplayer-generated houses named and bound before object loading?
88. Are Neutral and Special always present, or profile/version dependent?
89. How are map-local registry numeric keys composed with global keys—by key override or append semantics?
90. Can map-local registries intentionally reuse numeric keys?
91. Does a map-local type name override a global type of the same name or extend it key-by-key?
92. Can loose/mod Rules layers affect an already serialized map placement differently from FinalAlert?
93. Which Ares/Phobos include mechanisms participate before map-local placement binding?
94. Are extension type identities wider or differently normalized?

## P1 — Art and visual binding

95. Is `Image=` resolved before or after map-local Art overrides in stock runtime?
96. Does stock RA2/YR support any map-local Art section convention?
97. Are theater-specific visual suffixes selected by Rules type, Art type, or asset resolver?
98. Can a missing SHP/VXL/HVA cause runtime object creation failure rather than rendering failure?
99. Are foundations and occupancy sourced from Rules, Art, hardcoded type behavior, or combinations?
100. Does any editor infer foundation from image dimensions and then rewrite placement validity?
101. How should a future Unity adapter report a bound simulation type with missing visual asset?

## P1 — references

102. Is Tag identity the section key, a value field, or both across all versions?
103. Are Tag IDs case-sensitive?
104. What exact none/null sentinels are accepted for Tag fields?
105. Can duplicate Tag IDs be referenced deterministically?
106. Can a CellTag refer to a Tag declared later in source order without issue?
107. Does source order affect Tag/Trigger resolution?
108. Can Tags/Triggers form cycles that the runtime tolerates?
109. Does a placement record key ever serve as a trigger/action target?
110. Can `FollowsIndex` target a Unit declared after the source record?
111. Is Follows based on `[Units]` list only, excluding infantry and aircraft?
112. How do Group and TeamType recruitment fields interact with preplaced records?
113. Do recruitment flags change object load state or only later team selection?
114. Can waypoint IDs be referenced by raw string or only numeric index?
115. Which placement fields reference TeamTypes, TaskForces, Scripts, or AITriggerTypes directly, if any?

## P2 — Terrain and Smudge

116. Can `[Terrain]` values contain additional comma fields in stock or extensions?
117. Does TerrainType placement carry implicit facing or variation state?
118. Are tree burn/destroyed states serialized elsewhere or only runtime state?
119. Can two Terrain objects coexist at one cell through different keys/spellings?
120. Is the Smudge key semantically irrelevant or used as identity?
121. Is Smudge field 3 always zero in stock writers?
122. Do building bibs appear as source Smudge records, generated runtime smudges, or both?
123. Are crater/scorch smudges loaded before or after structures, and does order affect visuals?
124. Do smudges affect pathfinding or only rendering in RA2/YR?
125. How do extensions distinguish map-authored and runtime-created smudges?

## P2 — round-trip and implementation

126. What minimum source data is required for byte-identical placement round-trip?
127. Must duplicate section boundaries and physical order be preserved for runtime/editor acceptance?
128. Can Base INI serializers preserve duplicate keys without a custom lossless document?
129. Should a future canonical writer retain arbitrary techno keys or renumber them?
130. Can canonical renumbering safely update Follows references and every external reference?
131. Which editor reopen claims require reproducing FinalAlert defaults?
132. Which runtime acceptance claims can be tested without publishing baseline maps?
133. Can synthetic fixtures independently cover every optional and extension tail?
134. Which diagnostics should be fatal for typed view while still retaining raw records?
135. What aggregate minimum group size prevents sanitized audit results from identifying a map?
136. Can aggregate owner/type binding counts leak a rare mod identity?
137. Which source hashes are safe to publish without enabling map matching?
138. How should canonical aggregate hashes exclude source order and identities?
139. What parser limits are sufficient for pathological but valid maps?
140. Should a future parser expose lazy token views to limit allocations?
141. How should short-read Stream source spans be reported consistently?
142. Which MIX provenance fields are safe to retain internally and expose publicly?

## Research gates

Before implementation approval, resolve or explicitly configure at least:

- raw token preservation and profile selection;
- techno key identity policy;
- `Y*1000+X` coordinate policy;
- Infantry subcell policy;
- strict no-clamp health/facing policy;
- unknown mission policy;
- owner/type unresolved-record policy;
- Follows reference-basis policy;
- map-local registry composition policy;
- extension-tail preservation;
- privacy thresholds for any baseline audit.
