# Test matrix

## Legend

- `[F]` confirmed format/registry fact candidate
- `[C]` source conflict
- `[D]` defensive project policy
- `[H]` underconfirmed hypothesis or evidence gate

Total: **112 cases**.

## A. TMP header, fields, offsets, and flags — 28

1. `[F]` minimal 1×1 TMP with one 52-byte cell header.
2. `[F]` 16-byte file header plus one offset-table entry.
3. `[F]` zero offset represents an empty cell slot.
4. `[D]` negative nonzero cell offset fails.
5. `[D]` cell offset inside file header fails.
6. `[D]` cell offset inside offset table fails.
7. `[D]` cell header truncated at 51 bytes fails.
8. `[C]` fixture proves 48-byte header interpretation misaligns the color plane.
9. `[F]` all nine 32-bit header fields preserve exact bit patterns.
10. `[C]` signed and unsigned views of extra dimensions remain distinguishable.
11. `[F]` flags bit 0 exposes `HasExtraData` candidate.
12. `[F]` flags bit 1 exposes `HasZData` candidate.
13. `[F]` flags bit 2 exposes `HasDamagedData` candidate.
14. `[D]` unknown flag bits are preserved and diagnosed, not cleared.
15. `[D]` trailing three metadata bytes are preserved.
16. `[D]` duplicate cell offsets are classified without infinite recursion.
17. `[D]` reversed cell offsets do not determine parse order.
18. `[D]` overlapping cell-header windows fail or remain explicitly ambiguous.
19. `[F]` declared plane offsets are relative-to-cell-start candidate views.
20. `[C]` declared offset and sequential position disagree.
21. `[D]` declared offset before byte 52 fails.
22. `[D]` declared offset beyond bounded TMP window fails.
23. `[D]` offset addition overflow fails.
24. `[H]` zero depth offset with clear Z flag is accepted as absent candidate.
25. `[C]` zero depth offset with set Z flag yields structural ambiguity/failure.
26. `[H]` damaged-data flag without known body remains raw success plus unresolved diagnostic.
27. `[D]` maximum cell count budget is enforced before allocation.
28. `[D]` diagnostic-count budget caps repeated malformed cells.

## B. Diamond and extra planes — 20

29. `[F]` 60×30 diamond encodes exactly 900 bytes.
30. `[F]` 48×24 diamond encodes exactly 576 bytes.
31. `[F]` row widths widen/narrow in four-byte steps.
32. `[D]` width not equal to twice height fails canonical profile.
33. `[D]` zero or negative dimensions fail.
34. `[D]` multiplication overflow fails before allocation.
35. `[D]` diamond color truncation fails without padding.
36. `[F]` present diamond depth uses the same encoded length.
37. `[C]` Z flag clear but canonical sequential depth bytes exist.
38. `[C]` Z flag set but depth offset disagrees with `52 + D`.
39. `[D]` diamond color/depth overlap fails.
40. `[F]` extra color length equals `extraWidth × extraHeight`.
41. `[F]` extra depth candidate uses the same extra-area length.
42. `[D]` extra width/height zero with extra flag set fails.
43. `[D]` negative signed extra dimensions remain invalid typed candidates.
44. `[D]` extra area budget is enforced.
45. `[D]` extra color/depth overlap is diagnosed.
46. `[D]` extra rectangle outside base diamond is retained, not cropped.
47. `[C]` extra depth sample ≥32 is preserved despite permissive renderer behavior.
48. `[D]` trailing bytes after all declared planes are reported, not swallowed.

## C. Theater profiles, INI, TileSet registry, and assets — 24

49. `[F]` Temperate profile has independent extension/palette/control descriptors.
50. `[F]` Snow profile remains independent.
51. `[F]` Urban profile remains independent.
52. `[F]` NewUrban profile remains independent.
53. `[F]` Desert profile remains independent.
54. `[F]` Lunar profile remains independent.
55. `[D]` missing `[General]` yields typed-view failure while lossless INI remains available.
56. `[F]` valid `[TileSet0000]` classification.
57. `[D]` non-four-digit TileSet suffix is preserved and diagnosed.
58. `[C]` TileSet gap preserves later sections instead of WAE stop-at-gap behavior.
59. `[D]` duplicate normalized TileSet index is ambiguous.
60. `[D]` randomized INI occurrence order produces identical numeric registry ordering.
61. `[F]` cumulative global tile range starts at zero.
62. `[F]` second TileSet starts after first `TilesInSet` range.
63. `[D]` missing primary TMP does not shift later global IDs.
64. `[D]` negative `TilesInSet` fails typed allocation.
65. `[D]` cumulative global ID overflow fails.
66. `[F]` filename stem uses `FileName + 1-based D2` candidate.
67. `[H]` base variation without suffix resolves independently.
68. `[H]` `a..f` variation scan is explicit WAE profile, not universal.
69. `[C]` variation metadata differs from base asset and remains independently parsed.
70. `[C]` `.ubn` primary plus `.urb` fallback is explicit editor profile only.
71. `[D]` case-colliding TMP candidates remain ambiguous with full trace.
72. `[D]` map SubTile out of TMP slot range fails binding.

## D. Palette, LAT, ramp, height, and terrain semantics — 16

73. `[F]` TMP color indices require an external ISO-palette binding.
74. `[D]` unit palette cannot satisfy an ISO-palette request by fallback.
75. `[D]` missing ISO palette yields render-binding failure but raw TMP success.
76. `[D]` palette candidate chain preserves suppressed provenance.
77. `[F]` radar component triples remain raw and independent of ISO palette.
78. `[F]` Rough ground and clear-to-rough LAT relation candidate.
79. `[F]` Sand ground and clear-to-sand LAT relation candidate.
80. `[F]` Pave ground and clear-to-pave LAT relation candidate.
81. `[F]` Green ground and clear-to-green LAT relation candidate.
82. `[D]` LAT target TileSet missing yields explicit incomplete binding.
83. `[D]` LAT cycles and duplicate connections are diagnosed.
84. `[F]` map Level and TMP HeightRaw remain separate fields.
85. `[F]` depth plane never becomes movement height in Core.
86. `[H]` ramp values 0..20 map through named TS++ candidate table.
87. `[H]` terrain byte maps through an explicit profile, not direct enum cast.
88. `[D]` unknown ramp/terrain bytes survive raw parse and typed-view failure.

## E. Cliffs, water, shores, ice, and bridges — 14

89. `[F]` `CliffSet` binds a TileSet role, not a TMP flag.
90. `[C]` extra graphics present on a non-cliff role does not imply cliff.
91. `[F]` `WaterSet` role and TerrainType candidate remain separate evidence.
92. `[F]` `ShorePieces` relationship is distinct from LAT ground pairs.
93. `[F]` Snow ice set references bind registry roles.
94. `[D]` ice roles outside configured Snow-like profile are diagnosed.
95. `[F]` bridge terrain and overlay placement are retained independently.
96. `[D]` bridge overlay without bridge TileSet binding is incomplete, not auto-repaired.
97. `[D]` bridge TileSet without compatible overlay/state remains renderable but semantically unresolved.
98. `[C]` missing `TrainBridgeSet` in YR-like profile is not copied from another theater.
99. `[C]` NEWURBAN-specific role/fallback cannot leak into Urban profile.
100. `[D]` SetName substring does not assign cliff/water/shore/bridge role in Core.
101. `[D]` visual depth cannot determine bridge passability.
102. `[D]` destroyed/intact bridge state remains runtime/overlay semantics.

## F. Input modes, architecture, security, and audit — 10

103. `[D]` Memory, seekable Stream, short-read Stream, and MIX-window parsing are equivalent.
104. `[D]` no parser reads beyond the exact MIX-entry window.
105. `[D]` content enumeration order does not change registry or binding hash.
106. `[D]` synthetic header fixture builder does not call production offset formulas.
107. `[D]` synthetic registry oracle does not reuse production sorter/allocation code.
108. `[D]` malformed dimensions and offsets cannot cause unbounded allocation.
109. `[D]` duplicate/overlap graphs terminate under bounded work.
110. `[D]` Core assemblies contain no `UnityEngine` dependency.
111. `[D]` public audit serializer rejects TMP bytes, planes, palette bytes, INI values, paths, and per-tile hashes.
112. `[D]` ProjectBaseline evidence status and configured project policy remain separate fields.

## Required assertions for every relevant case

- structured status and diagnostic code;
- exact bytes consumed within the bounded input;
- no partial-success promotion;
- no silent clamp, padding, repair, or fallback;
- stable candidate/provenance ordering;
- canonical model hash independent of input transport and enumeration order.
