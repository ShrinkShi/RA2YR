> **Source notice:** This research was prepared by **ChatGPT Web** from public sources. It did not read the local ProjectBaseline and is not the product of the local Codex Agent. No GPL or unclear-license code was copied, translated, mechanically rewritten, or imported (`code_imported: false`).

# Unresolved questions

No question below is silently resolved by implementation convenience, current theater size, successful sample binding, or source-count voting.

## Official runtime evidence

1. Is any legally public RA2/YR runtime source available for IsoMapPack5 record consumption?
2. Does stock RA2/YR share the TS record interpretation without changes?
3. Did the official editor and runtime use the same packed structure definition?
4. Are FinalAlert's raw metadata bytes editor-only, runtime-used, or preserved unknowns?
5. Does the runtime accept streams written by modern sparse writers in every RA2/YR version?
6. Does the runtime require a particular record order?
7. Does the runtime stop by decoded length, record count, trailer, or another condition?

## Record layout and signedness

8. Are raw X and Y formally unsigned 16-bit values?
9. Can stock content contain raw coordinate values whose signed view is negative?
10. Is byte order always little-endian for every supported map family?
11. Are bytes 6 and 7 one 16-bit value or two independent bytes?
12. Do bytes 6 and 7 contain flags, metadata, or a tile high half in any game/version?
13. Are unknown bytes expected to roundtrip unchanged through the original editor?
14. Is `wGround` the original engine field name or an editor abstraction?
15. Are there map format versions with a different record width under the same section name?
16. Does `NewINIFormat` affect IsoMap record layout or only other packed sections?

## Tile field

17. Does stock RA2/YR consume all 32 bits at offsets 4..7?
18. Does it consume only the low 16 bits?
19. If only low16 is the tile ID, what are bytes 6..7?
20. Is the low16 tile field signed, unsigned, or sentinel-bearing?
21. Is `0xFFFF` an empty/clear sentinel in stock runtime, editor-only behavior, or tool convention?
22. Is `0xFFFFFFFF` meaningful under a full32 interpretation?
23. Can any accepted stock or official map contain a nonzero high16?
24. Can a valid mod map use GlobalTileId above 65535 in the original runtime?
25. Does FinalAlert truncate a 32-bit value when reopening or saving?
26. Does the engine mask, sign-extend, reject, or reinterpret high bits?
27. Are tile values tied to a hard engine maximum independent of theater registry size?
28. Can map-local theater extensions enlarge the usable global tile range?
29. Does any Ares/Phobos extension redefine the field width or high bits?
30. Should a future project profile support both full32 and split16 simultaneously per map, or only per configured runtime?
31. What evidence is sufficient to promote one tile interpretation from `Unresolved`?

## SubTile

32. Is byte 8 always a direct index into the selected TMP offset table?
33. Is SubTile zero-based in every TS/RA2/YR theater?
34. Can a runtime interpretation use SubTile for variation rather than TMP cell selection?
35. What does stock runtime do when SubTile is outside `CellsX × CellsY`?
36. What does it do when the indexed TMP slot is empty?
37. Can SubTile refer to a damaged or alternate cell plane?
38. Are values above 127 ever valid, despite signed-byte bugs in some languages?
39. Is the maximum legal SubTile a binary-format fact or solely resource-dependent?
40. Does FinalAlert preserve an invalid SubTile on roundtrip or repair it?

## Level

41. Is byte 9 formally unsigned?
42. What is the stock runtime's maximum accepted map level?
43. Which limits are editor UI limits versus engine limits?
44. Can a map record Level exceed the practical terrain rendering range without load failure?
45. Does Level directly represent absolute cell elevation or an encoded offset?
46. Does theater type change Level interpretation?
47. Do bridges or tunnels modify effective movement height without changing Level?
48. Does slope processing combine Level and TMP HeightRaw in a fixed formula?
49. Is an out-of-range Level rejected, wrapped, clamped, or rendered incorrectly by stock runtime?

## Final byte

50. Is byte 10 IceGrowth in TS only, TS/FS, or also RA2/YR?
51. Does RA2/YR ignore it, preserve it, or use it for another semantic?
52. Is it a scalar stage, timer, bitfield, flags byte, or reserved data?
53. What values are legal for ice growth?
54. Does theater Snow activate it while other theaters ignore it?
55. Does FinalAlert expose or alter it through any UI operation?
56. Why does the official editor name it only as generic map data?
57. Why do XCC/OpenRA call or treat it as zero?
58. Do nonzero values exist in official RA2/YR maps?
59. Can Ares/Phobos assign new meaning to the byte?
60. Should the typed semantic remain absent by default even after TS community evidence is accepted?

## Coordinates and bounds

61. What are the canonical names of the two raw coordinate fields in Westwood terminology?
62. Are X and Y swapped in FinalAlert only internally, or in the serialized record naming convention?
63. Is the common `+1` raw-coordinate bias exact for every map origin and version?
64. Do nonzero first two values in `[Map] Size` affect raw-coordinate conversion?
65. Can `LocalSize` extend outside the full Size domain in accepted maps?
66. Does LocalSize ever constrain tile loading rather than only playability/visibility?
67. Is `(2W-1) × H` always the complete valid coordinate count?
68. Are there maps whose valid-domain shape differs due to format version or special mode?
69. Is the parity rule an explicit runtime validation or merely a property of writer formulas?
70. Does stock runtime reject records in raw diamond blank areas or ignore them?
71. Can raw coordinates equal zero in legal maps?
72. Are values 512 and above universally invalid or just common array limits in tools?
73. What is the exact relationship between raw IsoMap coordinates and object coordinates encoded as `Y*1000+X`?
74. Which coordinate conversion belongs to runtime simulation rather than format parsing?
75. Is there a canonical record traversal order in the original writer?

## Dense, sparse, order, and duplicates

76. Does stock RA2/YR synthesize every missing valid coordinate as clear level0?
77. Is a missing record exactly equivalent to an explicit zero/default record for all runtime systems?
78. Are there exceptions involving byte 10, overlays, lighting, bridges, or map-local rules?
79. Does sparse acceptance depend on record order?
80. Does the runtime know expected record count from Size or simply consume available records?
81. What happens when the stream contains more complete records than the dense domain count?
82. What happens when two records have the same coordinate?
83. Is duplicate handling first-wins, last-wins, undefined, or fatal?
84. Does duplicate behavior differ between identical and conflicting records?
85. Are out-of-domain records ignored before or after duplicate indexing?
86. Can a sparse stream include only nondefault metadata while omitting tile0/level0 cells?
87. Does compression-oriented sorting alter any runtime behavior?
88. Do official maps use more than one writer order?
89. Should byte-identical duplicates be accepted with warning or rejected by the project default?
90. What exact compatibility evidence would justify a first-wins or last-wins profile?

## Decoded trailer and termination

91. Is the common four-byte decoded suffix part of the original IsoMap logical format?
92. Is it generated by a specific LZO/Format5 encoder rather than the map writer?
93. Why does the reviewed official editor writer not explicitly append it?
94. Are the four bytes always zero in stock and official maps?
95. Can the official editor's encoder append bytes internally that are absent from its input length?
96. Is the suffix a terminator, padding, writer artifact, record count, checksum, or unknown data?
97. Do any accepted maps have exact `N×11` decoded length with no suffix?
98. Do any have a remainder other than four?
99. Does the official editor preserve trailing bytes when reopening and saving?
100. Does stock runtime require exact decoded input consumption?
101. Can a trailer span chunk output boundaries?
102. Is a compressed `0/0` sentinel ever used together with a decoded four-byte trailer?
103. How should a canonical writer choose between no trailer and four-zero trailer?
104. Can a byte-identical roundtrip be claimed without retaining original chunk boundaries?

## Theater registry and TMP binding

105. Is cumulative `TilesInSet` the exact stock runtime algorithm for GlobalTileId ranges?
106. How are gaps or duplicate TileSet sections handled by stock runtime?
107. Does missing TMP content reserve its registry ID range in the original runtime?
108. Can `LastTilesInSet` or related extension keys alter cumulative ranges?
109. Are TileSet sections required to be contiguous from zero?
110. Does the runtime stop at the first missing TileSet section like WAE's reader, or enumerate another way?
111. Is TMP numbering always one-based with two decimal digits?
112. Which variation suffixes can stock runtime discover?
113. Is variation choice deterministic, random, map-seeded, or editor-only?
114. Is `.ubn → .urb` fallback used by stock YR or only FinalAlert/WAE compatibility behavior?
115. How are missing TMP, malformed TMP, and empty TMP slots distinguished by runtime?
116. Does palette or LAT binding ever change tile ID resolution?
117. Can map-local theater control INI composition change existing range boundaries safely?
118. What happens to existing map records if an earlier TileSet's `TilesInSet` changes?
119. Can a tile field refer to a reserved but intentionally empty range?
120. What evidence is required before the project emits a repaired effective tile?

## Roundtrip and preservation

121. Which unknown record bytes must be retained for FinalAlert reopen fidelity?
122. Must source record order be preserved for byte-identical output?
123. Must duplicate and out-of-domain records be preserved?
124. Must the exact decoded trailer be preserved?
125. Must Base64 fragment grouping and key spelling be preserved?
126. Must chunk sizes and boundaries be preserved?
127. Can recompression ever be byte-identical with a different LZO encoder version?
128. Does FinalAlert canonicalize record order, density, high bytes, or final byte on save?
129. Does stock runtime acceptance require any canonical ordering not required for parsing?
130. Should a future writer refuse output while tile-field interpretation remains ambiguous?
131. Can semantic roundtrip be claimed when unknown bytes are preserved but chunking changes?
132. How should the project separately certify parse success, lossless preservation, FinalAlert reopen, and stock runtime acceptance?

## Audit and evidence

133. Do the six ProjectBaseline theater groups all contain suitable non-sensitive samples?
134. Are nonzero high16 values observed in any selected group?
135. Are nonzero final bytes confined to Snow/TS-like samples?
136. Are exact `N×11` and `N×11+4` streams both observed?
137. Are sparse streams observed in official groups or only user/mod groups?
138. Are duplicate or out-of-domain records observed?
139. Do tile interpretations differ in registry-binding success counts?
140. Can aggregate observations distinguish writer lineage without exposing map identity?
141. What additional public source would be needed to elevate community runtime behavior?
142. Can any original executable behavior be tested legally and safely later without publishing original content?

## Current stop conditions

Implementation planning must retain multiple profiles while questions 17–31, 50–60, 76–90, and 91–104 remain unresolved. ProjectBaseline remains `AuditStatus: NotRun`; a future authorized aggregate audit may reduce uncertainty, but its observations must remain separately attributed and cannot be promoted to `ConfirmedByOriginalRuntimeSource` without actual original-runtime evidence.