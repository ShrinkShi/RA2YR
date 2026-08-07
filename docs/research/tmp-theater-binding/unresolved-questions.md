# Unresolved questions

## A. TMP header and raw fields

1. Are all stock RA2/YR TMP cell headers exactly 52 bytes across every theater and role?
2. Do any legitimate family variants use a different cell-header length?
3. Are the three plane offsets always relative to the cell-header start?
4. Are offset fields logically signed or unsigned in the original runtime?
5. Can a legitimate offset be zero while the associated flag is set?
6. Are duplicate cell offsets legal aliases or corruption?
7. Can different cells share plane bodies?
8. Are X/Y offsets signed in all valid assets?
9. Is HeightRaw signed or unsigned?
10. Are the final three metadata bytes padding, reserved fields, or uninitialized memory?
11. Does the runtime mask unknown flag bits?
12. Can high flag bits have defined meanings in YR or expansion assets?
13. What body representation, if any, is selected by `HasDamagedData`?
14. Are radar components stored in RGB order for all consumers?
15. Are radar bytes interpreted directly or transformed by a runtime palette/light system?

## B. Plane layout

16. Does flag bit `0x02` control the presence of diamond depth in stock RA2/YR?
17. Why does OpenRA read diamond depth unconditionally?
18. Are canonical stock offsets always sequential?
19. Can plane gaps or alignment padding exist?
20. Can extra color exist without diamond depth?
21. Can extra depth exist without extra color?
22. Is extra depth presence controlled by bit 1, nonzero offset, or both?
23. Are extra dimensions ever zero in valid files with bit 0 set?
24. Are extra dimensions signed in any meaningful way?
25. Are extra depth values normatively limited to 0..31?
26. Is palette index zero always transparent in extra graphics at runtime?
27. Can plane ranges legally overlap?
28. Are bytes after the final declared plane meaningful?
29. Does the original runtime honor offsets or consume planes sequentially?
30. Are 48×24 and 60×30 the only stock tile dimensions?

## C. Theater control INIs

31. What are the exact authoritative control INI logical names for all six YR profiles?
32. Which lower RA2 control documents participate in YR composition?
33. Are TileSet sections discovered by enumeration or active `0000..N` probing?
34. Does the original runtime stop at the first missing TileSet index?
35. Are TileSet section names case-sensitive?
36. How are duplicate TileSet sections handled after INI composition?
37. Are gaps legal in stock or MOD profiles?
38. What is the maximum supported TileSet index?
39. Are special `[General]` role values TileSet indices in every profile?
40. Which bridge keys are intentionally absent in YR?
41. Does the original runtime use NEWURBAN `.urb` fallback?
42. Which theater profiles can inherit another theater's TMP extension or palette?
43. Can map-local INI sections alter theater control in vanilla or extension engines?
44. How are empty values or deletion syntax applied to TileSet entries?
45. Which Ares/Phobos extensions affect theater registry semantics?

## D. Tile IDs and TMP asset names

46. Is global tile ID allocation always cumulative by numeric TileSet index?
47. Does `TilesInSet` reserve IDs when files are missing?
48. Can a TileSet declare zero tiles?
49. Is the per-TileSet filename number always 1-based and two digits?
50. Are three-digit or unpadded tile filenames ever discovered automatically?
51. What variation suffix range does the original runtime scan?
52. Are variations enumerated actively or found by directory/archive lookup?
53. Can variations have different cell grids or metadata?
54. How are case-colliding filenames resolved on original Windows filesystems?
55. Can one TMP be referenced by multiple global tile IDs?
56. How does Marble Madness affect identity versus display substitution?
57. What is the exact `NonMarbleMadness` behavior?
58. Are missing primary TMP assets fatal, blank, or replaced at runtime?
59. Do fallback extensions affect global tile identity?
60. Are theater-specific TMPs mounted from fixed internal MIX names or general content lookup?

## E. Palette and LAT

61. What are the exact ISO and unit palette names for NewUrban, Desert, and Lunar in the target baseline?
62. Are TMPs ever colored with a non-ISO palette in stock runtime?
63. How does lighting modify ISO-palette output?
64. Are radar metadata bytes independent of theater palettes?
65. What is the original LAT transition-selection algorithm?
66. Which TileSet pairs are vanilla versus editor/MOD extensions?
67. Is the LAT base TileSet always TileSet 0 when no explicit base exists?
68. How are `ConnectTo` lists interpreted and ordered?
69. Can LAT relationships form cycles?
70. Does Marble Madness participate before or after LAT selection?

## F. Height, ramp, and terrain

71. Does the TS++ 0..20 ramp table exactly match RA2/YR runtime?
72. What signedness and unit does TMP HeightRaw use?
73. How is local height combined with map Level?
74. What corner ordering and handedness does the runtime use?
75. Do ramp values differ by theater or game generation?
76. Does TerrainTypeRaw map directly to the modern LandType order?
77. Can TileSet role override TerrainTypeRaw?
78. Which field controls buildability versus locomotion cost?
79. Are tunnel and railroad categories present in RA2/YR TMP metadata?
80. How do extension engines alter terrain categories?

## G. Cliffs, water, shores, ice, and bridges

81. Which exact TileSet roles define cliffs in each theater?
82. Is extra graphics required for every cliff face?
83. How are shore transitions selected around water?
84. How do ice sets and ice-shore sets interact with map state?
85. Which bridge elements are TMP versus overlay versus SHP assets?
86. What is the exact intact/destroyed bridge state model?
87. How are train bridges represented in YR where a key may be absent?
88. Are wood bridges supported in every target profile?
89. Does visual depth affect bridge draw order only or also targeting/collision?
90. How does terrain Level under bridge overlays affect units above and below?

## H. Runtime, editor, and implementation

91. Which FinalAlert repairs are required only for editor display?
92. Which WAE behaviors intentionally differ from FinalAlert or the original game?
93. Can a safe reader reject malformed assets that the original runtime tolerates without breaking baseline compatibility?
94. What raw-preservation level is required for future writer support?
95. Can unknown flag/padding bytes be round-tripped without exposing original data publicly?
96. What canonical hash model best compares registry and binding results?
97. Which public fixtures can be licensed permissively for TMP metadata tests?
98. Can synthetic fixtures independently cover all plane-order candidates?
99. Which ProjectBaseline samples truly distinguish offset-driven and sequential parsing?
100. What evidence threshold is required before any of these questions changes compatibility status?
