# Unresolved questions and implementation gates

## 1. MAP/MPR/YRM family

1. Which extension/discovery rules are confirmed for every RA2 and YR runtime path rather than community convention?
2. Which official `.map` files require PKT/campaign registration, and which can be discovered independently?
3. Does original runtime treat extension case differently on different Windows versions/filesystems?
4. Which `NewINIFormat` values are safely rejected versus merely unsupported?
5. Are Preview sections position-sensitive in both RA2 and YR, and under which executable versions?

## 2. Packed fragments

6. Does original runtime sort numbered pack keys numerically, use INI occurrence order or retrieve sequential names actively?
7. Are gaps, key zero, leading zeros or duplicate normalized keys accepted?
8. Are nonnumeric keys ignored, fatal or included by a generic INI enumerator?
9. Do LZO streams permit a zero-size terminal block, input exhaustion only, or both?
10. What exact trailing-byte policy applies to LZO and Format80 map sections?

## 3. IsoMapPack5

11. Are record X/Y fields signed or unsigned in the stock model?
12. Are bytes 4..7 a true 32-bit tile index or a 16-bit tile plus reserved/zero word?
13. If the high word is normally zero, is it validated, ignored or used by an extension?
14. Is the eleventh byte ice growth, reserved or game-family-specific in RA2/YR?
15. Are four final zero bytes padding or an interpreted `(0,0)` terminal coordinate?
16. Does stock runtime require dense `((2W)-1)H` records or accept omitted clear cells?
17. How are duplicate coordinates resolved by original runtime?
18. Are noncanonical record orders fully legal?
19. What is the exact valid map-level range for RA2/YR?

## 4. Overlay and Preview

20. Is vanilla overlay output always exactly 262144 one-byte entries for every supported map profile?
21. Is OverlayData `0xFF` meaningful for any stock overlay roles?
22. Which bridge/wall/resource frame values have hardcoded runtime semantics outside Rules?
23. What is the exact Preview file-byte channel order?
24. Are Preview dimensions required to follow map/local-size ratios or only rendering conventions?
25. What does stock runtime do with absent, malformed or partly decompressed previews?
26. Is a dummy preview requirement universal across RA2/YR releases or editor/tool-specific?
27. Does the runtime consume Preview line keys sequentially or by enumeration order?

## 5. Mission and local configuration

28. What are the exact RA2 versus YR positional schemas for every placement/mission section?
29. Which unknown/extra fields are tolerated by vanilla?
30. Are section and identity comparisons ASCII case-insensitive, CRC-based or context-specific?
31. How are duplicate IDs in triggers/tags/teams/scripts handled?
32. Which event/action opcode schemas differ between RA2 and YR?
33. Which map-local Rules sections are composed over global Rules, and at what stage?
34. Is map-local Art supported by vanilla YR, a specific naming convention, or only extensions?
35. What do empty local values mean for scalar, list and reference fields?
36. Are numbered registries merged by key, appended, replaced or consumer-specific?

## 6. TMP physical layout

37. What are the exact names and offsets of all fields in the approximately 52-byte cell header?
38. Which fields are signed, and which are raw byte/word values?
39. Which flag bits beyond extra-data bit 0 are valid and what do they select?
40. What are the exact height, terrain/land and ramp fields for TS versus RA2/YR?
41. What are the two reported color/radar-related metadata values?
42. Are duplicate cell offsets legal aliases or corruption?
43. Are nonmonotonic but disjoint cell records legal?
44. Are trailing bytes/padding allowed after the last cell?
45. What exact depth-value ranges and semantics are used by stock rendering?
46. Are extra depth values limited to 0..31 as an OpenRA/XCC observation suggests?
47. Which pixel/color index is transparent for normal and extra planes under each theater?
48. Are zero-dimensional or all-empty TMP templates legal?

## 7. Terrain binding and runtime

49. How does the theater control data map a numeric tile index to a TMP filename and subtile across RA2/YR theaters?
50. How are randomized tile filename suffixes selected?
51. What is the exact composition of map level, TMP ramp metadata and runtime slope transform?
52. Which cliff/shore/water behaviors are hardcoded versus data-driven?
53. How are bridge overlays combined with TMP bridge-head cells and map elevation?
54. What coordinate basis and scale should a future renderer use without importing OpenRA/editor conventions as file facts?

## 8. Roundtrip

55. Which section reorderings are required for original compatibility?
56. Can untouched packed sections always be preserved byte-for-byte after unrelated map edits?
57. Which editor-generated repairs alter game semantics?
58. Which unknown sections/fields are currently dropped by FinalAlert or WAE?
59. Is record renumbering semantically safe after all reference updates?
60. What evidence is sufficient for `SemanticRoundTrip` versus `EditorCompatibleRoundTrip`?

## 9. Decision levels

### Level A — confirmed default

Requires:

- at least two meaningfully independent public sources or official evidence;
- distinguishing synthetic fixtures;
- multiple sanitized local samples across map roles/theaters;
- exact bounded consumption;
- no repair, clipping, padding or hidden fallback;
- stable Memory/Stream/MIX results.

### Level B — explicit experiment

One strong source plus consistent local evidence may justify a named, default-off strategy and separate hashes/diagnostics.

### Level C — unresolved

If alternatives remain equally plausible or only permissive editor behavior succeeds, retain raw data and do not promote compatibility.

### Level D — family/profile issue

If incompatible samples cluster by extension, NewINIFormat, theater, engine extension or corruption category, classify the family/profile before changing the base parser.

## 10. Prohibited shortcuts

Do not:

- discard the high IsoMap tile word because current samples happen to contain zero;
- choose RGB/BGR by visual intuition;
- infer terrain from PreviewPack;
- clamp invalid map or TMP records to clear terrain;
- use WAE/FinalAlert save output as byte-layout proof;
- assume every map unknown section is a Rules override;
- execute unknown triggers to test plausibility;
- call editor repair a compatibility fix;
- publish original map text, mission logic, coordinates, preview pixels or TMP planes;
- implement Unity terrain/rendering before raw contracts are evidence-gated.
