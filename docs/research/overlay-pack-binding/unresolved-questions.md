# Unresolved Overlay pack questions

> **Research provenance:** This document was produced by ChatGPT Web from public sources. It did not read ProjectBaseline, is not a local Codex Agent artifact, and imports no code. GPL or unclear-license sources are reference-only; no source was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

## 1. Priority model

- **P0** — blocks a faithful default parser or binder policy;
- **P1** — blocks exact semantic interpretation or safe writing;
- **P2** — useful for compatibility, diagnostics, or optimization;
- **P3** — historical or edge-case research.

All questions remain `Unresolved` unless a different normalized evidence grade is explicitly recorded in another document.

```text
AuditStatus: NotRun
FutureEvidenceSource: ProjectBaselineAggregateAudit
```

The future audit fields describe planned aggregate work and do not imply that ProjectBaseline has been read.

## 2. Highest-priority questions

1. **P0:** Does the original RA2/YR runtime require exactly 262144 decoded bytes for each ordinary stream, or does it tolerate short/long output because destinations are preallocated?
2. **P0:** Is `X + 512 × Y` the original runtime's external storage formula, or is EA's editor transpose evidence of a different axis convention?
3. **P0:** Does the original runtime construct `[OverlayTypes]` identity from numeric keys, physical list order, or another parser-specific mechanism?
4. **P0:** How does the original runtime handle registry gaps, duplicate ordinals, duplicate names, and nonnumeric keys?
5. **P0:** Does map-local `[OverlayTypes]` participate in vanilla RA2/YR, and can it add, override, or reorder registry entries?
6. **P0:** What exact Overlay Format80 position mode and marker policy do stock RA2/YR maps require?
7. **P0:** Does every Overlay Format80 payload require a `0x80` terminator, exact compressed consumption, and exact output length?
8. **P0:** Is `0xFF` always the sole ordinary no-overlay sentinel, including malformed registries and extension contexts?
9. **P0:** What does the runtime do with an unbound raw type ID below `0xFF`?
10. **P0:** What does the runtime do when only one of the two packed sections exists or decodes successfully?
11. **P0:** What is the scope of WAE's `NewINIFormat >= 5` 16-bit Overlay type stream: patched engines, specific mods, or any stock-compatible path?
12. **P0:** Does the runtime process, ignore, or preserve nonempty Overlay storage outside the active scenario diamond?
13. **P0:** Does the runtime process an Overlay at a storage coordinate with no IsoMap cell?
14. **P0:** Which Overlay families cause the runtime to trust stored data, recompute it, or combine it with context?
15. **P0:** What information must a lossless writer retain to avoid corrupting unknown or domain-external bytes?

## 3. Section and fragment questions

16. **P1:** Are numbered fragments always one-based in stock writers?
17. **P1:** Does a missing fragment number terminate collection or permit later fragments?
18. **P1:** How does FinalAlert resolve source order versus numeric order when the INI parser preserves a different order?
19. **P1:** Are leading-zero fragment keys accepted by the runtime?
20. **P1:** How are duplicate normalized fragment numbers handled?
21. **P2:** Are empty fragment values ignored, concatenated as empty, or treated as malformed?
22. **P2:** Does inline whitespace survive into Base64 parsing or get removed by the INI layer?
23. **P2:** Are duplicate packed sections possible in official maps, and which occurrence is used?
24. **P2:** Can either section be intentionally empty in a valid map?
25. **P3:** Do official writer versions use stable fragment line lengths and ordering?

## 4. Chunk and Format80 questions

26. **P0:** Are medium/long references absolute output positions for every RA2/YR Overlay stream?
27. **P0:** Is an initial relative-stream marker ever valid for Overlay sections?
28. **P1:** Does a 0/0 chunk header terminate the outer stream, represent an empty block, or constitute malformed input?
29. **P1:** Can only one chunk size field be zero?
30. **P1:** Are trailing compressed bytes after a Format80 terminator accepted by the runtime?
31. **P1:** Can a chunk produce fewer bytes than its declared output size?
32. **P1:** Can a valid final chunk be shorter than common writer chunk sizes without a sentinel?
33. **P2:** What maximum uncompressed chunk size did each official editor version emit?
34. **P2:** Are overlapping copies required by official Overlay streams, and what maximum observed distance is relevant?
35. **P2:** Do any stock maps use command forms absent from WAE's inspected decoder?
36. **P2:** Does the official editor's prefilled destination mask decoder underflow in practice?
37. **P3:** Does recompression command selection influence original-runtime acceptance beyond semantic output equality?

## 5. Array layout and length questions

38. **P0:** Is the fixed 512 dimension a hard runtime storage boundary or a shared writer/editor convention?
39. **P1:** Are decoded bytes beyond 262144 ignored, rejected, or interpreted by any stock path?
40. **P1:** Are short type streams implicitly `0xFF`-filled in the runtime?
41. **P1:** Are short data streams implicitly zero-filled in the runtime?
42. **P1:** Does the runtime require the two aggregate decoded lengths to match?
43. **P1:** Can `OverlayPack` and `OverlayDataPack` use different chunk counts and boundaries while remaining valid?
44. **P2:** Do official maps ever contain nonzero decoded trailing bytes beyond the fixed storage plane?
45. **P2:** Does FinalAlert preserve domain-external storage when saving an unmodified map?
46. **P2:** Does map resize clear, transpose, retain, or relocate domain-external Overlay bytes?
47. **P3:** Did TS, RA2, and YR editor builds initialize absent data arrays identically?

## 6. Coordinate and domain questions

48. **P0:** What external coordinate names correspond to EA editor internal `u/v` and `x/y` axes?
49. **P0:** Is EA's `internalY + 512 × internalX` mapping purely an internal transpose?
50. **P1:** Is Overlay storage always addressed by IsoMap raw coordinates rather than normalized canvas coordinates?
51. **P1:** Which exact map `Size` formula defines valid scenario coordinates for Overlay analysis?
52. **P1:** Does `LocalSize` affect runtime loading or only visibility/playability?
53. **P1:** What happens to a bound Overlay inside storage but outside `Size`?
54. **P1:** What happens to a bound Overlay inside `Size` but outside `LocalSize`?
55. **P1:** What happens when row-major and transposed views each point to plausible, different nonempty cells?
56. **P2:** Are coordinates 0 or 511 used by stock maps for meaningful data?
57. **P2:** Do official maps contain stale data outside the diamond that can reveal writer behavior?
58. **P2:** Is object-section `1000 × Y + X` always expressed in the same raw map coordinate domain?
59. **P3:** Are there editor versions with asymmetric read/write coordinate transforms?

## 7. Registry questions

60. **P0:** Are numeric `[OverlayTypes]` keys authoritative IDs in vanilla runtime?
61. **P0:** Does the engine stop at the first missing ordinal or continue scanning later numeric keys?
62. **P0:** Can a map-local registry legally introduce an ordinal beyond global Rules?
63. **P1:** Are registry names case-insensitive in the engine's logical section lookup?
64. **P1:** Can two ordinals point to the same Overlay logical section?
65. **P1:** Can one ordinal change logical name through map-local Rules composition without invalidating saved raw IDs?
66. **P1:** What is the runtime behavior for a registry entry whose logical section is missing?
67. **P1:** What is the runtime behavior when Art/image resources are missing?
68. **P1:** Does Ares or Phobos formally define extended ordinals or deletion/reordering syntax?
69. **P2:** Does FinalAlert build the same ordinal map as the runtime when numeric keys are sparse or out of source order?
70. **P2:** Are leading-zero registry keys accepted by any official parser?
71. **P2:** Are duplicate normalized keys first-wins, last-wins, merged, or fatal?
72. **P3:** Are there hardcoded ordinal ranges that bypass `[OverlayTypes]` lookup for some systems?

## 8. OverlayData semantic questions

73. **P0:** Which vanilla Overlay families interpret the byte as a direct visual frame?
74. **P0:** Which families recompute a frame/state from neighboring cells?
75. **P0:** Which families use hardcoded ordinal plus raw data behavior independent of ordinary Art frames?
76. **P1:** What does the runtime do with `0xFF` type plus nonzero data?
77. **P1:** Is data value `0xFF` valid for any bound vanilla type?
78. **P1:** Does runtime validation clamp an Art-frame index or allow out-of-range access?
79. **P1:** Are animation phase and static frame stored identically for animated overlays?
80. **P1:** Can one Overlay type use different data semantics by theater or game mode?
81. **P2:** Does FinalAlert display and resave unknown data unchanged?
82. **P2:** Which extension flags alter raw-data interpretation?

## 9. Resource questions

83. **P0:** What exact mapping connects resource Overlay type/data to initial harvest quantity in RA2/YR?
84. **P0:** Is the raw data byte a stage, visual frame, density, or index into a hardcoded table for ore/gems?
85. **P1:** How do Rules resource values combine with the stored stage?
86. **P1:** Does the runtime normalize resource data on load?
87. **P1:** Are growth/spread states stored, derived, or initialized separately?
88. **P1:** Do ore and gem families share one data mapping?
89. **P2:** Does FinalAlert's money estimate match the runtime in all stages?
90. **P2:** How do extension-defined resource families bind to raw ordinals and data profiles?

## 10. Wall and fence questions

91. **P0:** Does the vanilla runtime recompute wall connection frames at load?
92. **P0:** Is the stored data byte a direct frame, a connection mask, or an input to a hardcoded mapping?
93. **P1:** How is wall damage represented relative to OverlayData and runtime health?
94. **P1:** Where are wall ownership and targetability stored or derived?
95. **P1:** How do gates relate to Overlay cells and building/object state?
96. **P1:** Does placing or destroying a neighboring wall update stored data, runtime-only state, or both?
97. **P2:** Does FinalAlert recompute all connected frames when opening/saving a map?
98. **P2:** Do different wall families use different connection tables?

## 11. Bridge questions

99. **P0:** Which bridge behaviors are keyed by Overlay ordinal rather than logical type flags?
100. **P0:** What exact state does OverlayData carry for each low-bridge and high-bridge family?
101. **P0:** How are intact, damaged, destroyed, and repaired states represented across Overlay, TMP, objects, and runtime state?
102. **P1:** Does a high bridge store only a middle Overlay cell in all RA2/YR bridge families?
103. **P1:** How does the runtime derive the visually/physically occupied three-cell width?
104. **P1:** How are bridge approach and end pieces associated with the deck?
105. **P1:** How are upper/lower occupancy and pathing layers selected?
106. **P1:** What role do bridge control/hut objects play at initial map load?
107. **P1:** How do water, shore, debris, and repair overlays interact with bridge state?
108. **P2:** Does FinalAlert's bridge writer produce canonical runtime-accepted layouts for every family?
109. **P2:** Which observed bridge rules are TS-inherited versus RA2/YR-specific?

## 12. Roundtrip and compatibility questions

110. **P0:** Must a lossless editor retain exact compressed bytes, or are exact decoded arrays sufficient for runtime acceptance?
111. **P1:** Must fragment spelling/order and chunk boundaries be retained for FinalAlert reopen?
112. **P1:** Can unknown type/data pairs be semantically roundtripped without understanding them?
113. **P1:** Can map-domain-external storage be safely omitted by a canonical writer?
114. **P1:** Does FinalAlert preserve unknown high type IDs?
115. **P1:** Does ordinary `NewINIFormat=4` always imply one-byte type storage?
116. **P1:** Which engine patches consume `NewINIFormat>=5` 16-bit type arrays?
117. **P2:** Do Ares/Phobos change fragment, Format80, registry, or data semantics independently?
118. **P2:** Are CnCNet client/editor compatibility repairs stricter or looser than original runtime behavior?
119. **P2:** Can a rewritten but semantically identical array produce different multiplayer behavior due to hidden hardcoded state?
120. **P3:** Are there known official maps whose packed sections were produced by different editor generations?

## 13. Audit and evidence questions

121. **P0:** Which sanitized aggregate can distinguish row-major from transposed views without exposing positions?
122. **P0:** Which sanitized evidence can distinguish a strict 262144 runtime contract from prefilled-buffer tolerance?
123. **P1:** Can command-kind aggregates distinguish Format80 profiles without disclosing compressed data?
124. **P1:** How many demonstrably separate source lineages, rather than repositories, are represented by observed local behavior?
125. **P1:** What sample categories are needed to avoid overfitting to resource-heavy or empty maps?
126. **P1:** How should maps with extension-defined registries be isolated from vanilla conclusions?
127. **P2:** Which aggregate hashes remain stable across Memory, Stream, short-read Stream, and MIX windows?
128. **P2:** Which diagnostics are safe to publish without revealing type names or positions?
129. **P2:** When can repeated ProjectBaseline observations justify reviewing or changing a `DefensiveDesign` project policy?
130. **P3:** What additional official documentation or binary-level runtime experiment would be required for `ConfirmedByOriginalRuntimeSource`?

## 14. Current policy posture

Until the P0 questions are resolved, the recommended project posture remains:

- ordinary exact 262144-byte arrays under an explicit profile;
- external row-major coordinate candidate plus retained transposed comparison profile;
- `0xFF` ordinary sentinel;
- explicit numeric registry with gaps and provenance;
- opaque raw data plus type-specific semantic profiles;
- explicit absolute-position Format80 candidate from M3-R2;
- no repairs, trial decoders, trial axes, registry compression, or unknown-byte cleanup;
- no default canonical writer.

These are project policies classified as `DefensiveDesign`; they are not original-runtime facts.
