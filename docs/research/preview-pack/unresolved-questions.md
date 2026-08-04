# Unresolved PreviewPack questions

> Source notice: compiled by **ChatGPT Web** from public research; no local `ProjectBaseline`; not a Codex Agent artifact; no code copied, translated, mechanically rewritten, or imported (`code_imported: false`).

These questions remain explicit. An editor implementation, community page, or future aggregate audit cannot be silently promoted to original runtime proof.

## P0 — format and runtime blockers

1. Does the original RA2/YR runtime interpret Size fields 0 and 1 at all?
2. If used, are fields 0 and 1 X/Y, left/top, crop origin, map origin, or another tuple?
3. Are nonzero fields 0/1 accepted by every RA2/YR version?
4. Are negative fields 0/1 accepted, ignored, or rejected?
5. Does the runtime require exactly four Size components?
6. Does it parse Size as signed or unsigned integers?
7. Are width and height strictly fields 2 and 3 in runtime code?
8. Is `width × height × 3` universally required by runtime?
9. Can the runtime accept an empty preview or zero dimensions?
10. Is any decoded trailer permitted?
11. Is any per-row padding permitted?
12. Is the packed component order RGB or BGR in the original runtime?
13. Why does ModEnc say BGR while official-editor and CnCNet behavior indicates RGB?
14. Is the official editor's RGB conversion exactly matched by the game reader?
15. Is the runtime row order top-down?
16. Can bottom-up third-party PreviewPacks be accepted?
17. Does runtime use fields 0/1 to alter row origin or crop?
18. Must `[Preview]` and `[PreviewPack]` be first?
19. Is placement after `[Basic]` equally accepted?
20. Must the two sections be adjacent?
21. How does runtime handle duplicate `[Preview]` sections?
22. How does runtime handle duplicate `[PreviewPack]` sections?
23. How does runtime handle duplicate `Size` keys?
24. Does runtime use physical or numeric fragment order?
25. Does it stop at the first fragment-key gap?
26. Is key `0` valid?
27. Are leading-zero keys normalized?
28. What happens with `1` and `01` together?
29. Are nonnumeric keys ignored, included, or fatal?
30. Does runtime require a zero-size terminal block?
31. Is input exhaustion the normal termination condition?
32. Are one-zero chunk headers accepted?
33. Is 8192 a runtime block-output maximum or only a writer convention?
34. Must each LZO block consume its payload exactly?
35. Must aggregate decoded output fill the expected buffer exactly?
36. Does runtime zero-fill short output?
37. Does runtime ignore trailing compressed bytes?
38. Does runtime ignore extra decoded bytes?
39. Which precise LZO1X-compatible stream variant is accepted in PreviewPack?
40. Does PreviewPack share every envelope rule with IsoMapPack5?

## P1 — producer and consumer compatibility

41. What exact section order does the EA editor emit after a full save?
42. Does FinalAlert preserve nonzero Size origins when only the preview is regenerated?
43. Does FinalAlert preserve existing PreviewPack when generation is skipped?
44. Does FinalAlert rewrite fragments, chunk boundaries, or section placement on reopen/save?
45. Is the WAE first-section requirement based on tested executable versions or inherited community knowledge?
46. Which RA2/YR executable versions crash without a preview?
47. Do community patches or launchers remove the requirement?
48. Does gameplay start when the map-selection preview fails?
49. What fallback image does the original menu use, if any?
50. Does the original menu reject unusual aspect ratios?
51. Are black-edge artifacts caused by runtime crop, fixed UI dimensions, metadata origin, or generation ratios?
52. Are ModEnc's official-map proportion formulas hard requirements or empirical writer choices?
53. Does CnCNet intentionally accept short decoded output by zero-fill, or is it an oversight?
54. Does CnCNet consume fragment keys in stable numeric order through its general INI parser?
55. Can the fast and normal CnCNet extractors differ on physically shuffled fragments?
56. What exact bitmap channel/row contract does MapTool's `GraphicsUtils` implement?
57. Does MapTool preserve section physical order when saving?
58. Does MapTool preserve nonzero Size origins?
59. Does CNCMaps always store RGB despite comments and local variable names?
60. Does CNCMaps handle negative bitmap stride differently?
61. Does CNCMaps insertion after Basic reflect original runtime testing?
62. Does XCC generate RGB or BGR raw bytes?
63. What chunk output size does XCC use?
64. Does XCC preserve or regenerate Size origin fields?
65. Is the WAE fixed dummy payload identical across all target games?
66. Is CnCNet's hidden-preview signature recognized by other clients?
67. Is a dummy preview structurally valid under strict exact-output rules?
68. Does the official editor generate any fixed placeholder when rendering fails?
69. Does the official editor preserve a manually authored preview image?
70. Are Preview sections included in digest behavior in a placement-sensitive way?

## P2 — metadata, image, and round-trip detail

71. Are Size field tokens allowed to contain plus signs?
72. Does runtime trim whitespace around fields?
73. Are extra comma-separated fields ignored?
74. Does integer overflow wrap in original parsers?
75. Is width/height capped by a 16-bit or UI-specific type?
76. Is there a practical maximum compressed section length in original INI readers?
77. Is fragment line length 70 required or merely conventional?
78. Are empty fragment values ignored?
79. Does Base64 decoder tolerate whitespace inside values?
80. Does it tolerate noncanonical padding?
81. Can LZO blocks split inside a pixel?
82. Can blocks split inside a scanline?
83. Do official writers prefer row-aligned blocks?
84. Is there any color-space assumption beyond 8-bit components?
85. Does runtime treat components as sRGB?
86. Is there any transparent-color convention?
87. Is alpha ever stored by an extension?
88. Do any Ares/Phobos profiles alter PreviewPack layout?
89. Do TS, RA2, and YR differ in pixel or metadata semantics?
90. Does `NewINIFormat` affect PreviewPack?
91. Is a palette-index PreviewPack accepted by any game version or tool?
92. Do any tools append a four-byte padding/trailer copied from IsoMap conventions?
93. Are compressed chunk boundaries needed for byte-identical editor reopen?
94. Are original Base64 line breaks needed for digest identity?
95. Can duplicate fragments be preserved through FinalAlert?
96. Does section case matter?
97. Does key case matter for `Size`?
98. Can comments occur inside PreviewPack without affecting a runtime parser?
99. Are duplicate sections merged by the EA editor's INI container?
100. Does canonical numeric sorting alter maps produced with physical-order payloads?

## P3 — future audit questions

101. Do authorized stock samples contain nonzero first Size fields?
102. Do all authorized samples have exact `w×h×3` decoded length?
103. Are any decoded tails present?
104. Are zero-size chunk headers present?
105. What is the observed maximum block output?
106. Are all final blocks short or sometimes exact-sized?
107. Are Preview sections first, after Basic, or mixed across producer categories?
108. Are Preview and PreviewPack always adjacent?
109. Are fragment keys always canonical `1..N`?
110. Are gaps, duplicates, or leading zeroes present?
111. Are hidden/dummy payload candidates present?
112. Are both sections ever missing in accepted map categories?
113. Does metadata ever exist without payload?
114. Does payload ever exist without valid metadata?
115. Are unusual widths/heights or aspect ratios present?
116. Can producer categories be inferred without exposing map identity?
117. Can channel order be distinguished without visual inspection? Usually it cannot; what nonvisual producer evidence exists?
118. Can row order be distinguished without rendering? Usually it cannot; what provenance markers exist?
119. Do Memory/Stream/short-read/MIX modes remain identical on all authorized samples?
120. Do malformed samples trigger bounded diagnostics without content leakage?

## P4 — implementation-policy decisions still requiring review

121. Which metadata profile is the first production target?
122. Is RGB24 selected as standard project policy before baseline audit?
123. Is top-down selected as standard project policy before runtime evidence?
124. Should strict fragment policy require key 1 and contiguous numbers?
125. Should physical-order mode exist only as an audit profile?
126. Is `0/0` accepted as sentinel in production?
127. Is 8192 enforced as a target-writer convention?
128. What maximum preview dimensions are safe for Core and Unity adapters?
129. What aggregate decoded-byte budget is appropriate?
130. What compressed-to-decoded ratio budget is appropriate?
131. How much trailing input may an audit retain privately?
132. What diagnostic severity applies to a known hidden preview?
133. Should source parsing succeed when preview is absent while target export fails?
134. Which consumers receive generated fallback art?
135. What explicit user action is required to regenerate preview?
136. Which source identities must a no-op save preserve?
137. Should intentional recompression retain old fragments in an archival side record?
138. How are interpretation profiles represented in public APIs without implying certainty?
139. Which evidence threshold can change a configured project policy?
140. What separate validation is required before compatibility-matrix promotion?

## Current highest-priority position

Until these questions are resolved:

- raw metadata and bytes remain primary;
- RGB/top-down are leading candidates, not undisputed runtime facts;
- exact `width×height×3` is the strict project length contract;
- section/fragment physical order is preserved;
- parser fabrication is forbidden;
- no canonical writer is selected;
- no compatibility status is raised.