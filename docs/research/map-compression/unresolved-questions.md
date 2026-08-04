# Unresolved questions

## Format80 / LCW

1. Do stock RA2/YR Overlay chunks ever begin with the community-described relative marker?
2. Are medium/long fields always absolute in stock map packs?
3. Does any stock producer use relative medium/long commands without a marker?
4. Are zero-count fill or long-copy commands legal no-ops?
5. Is an absolute source field equal to current output ever assigned special meaning?
6. Are bytes after `0x80` legal block padding?
7. Is terminator required when declared output has already been produced?
8. Are chunk payloads canonicalized to one terminator at the final byte?
9. Does original code distinguish malformed reference from output overflow?
10. Does “reverse” in XCC APIs change field semantics, output placement, or both?
11. Are all five commands observed in stock map Overlay data?
12. Is the maximum short distance field value 4095 usable when sufficient output exists?
13. Are medium/long overlapping copies relied on by stock data?
14. Does original runtime accept partial block output?
15. Are OverlayPack and OverlayDataPack guaranteed to use the same Format80 variant?

## Chunk envelope

16. Is `0/0` a legal chunk terminator in original RA2/YR maps?
17. What is original behavior for `0/nonzero` or `nonzero/0`?
18. Is 8192 an original hard output limit or only writer convention?
19. May non-final blocks be shorter than 8192?
20. Are zero-length packed sections represented by no blocks or a sentinel?
21. Are bytes allowed between blocks?
22. Are trailing bytes after the final block ignored, rejected, or interpreted elsewhere?
23. Does original runtime stop at expected aggregate output or at source exhaustion?
24. Are header sizes signed or always unsigned 16-bit?
25. Can compressed size exceed declared output size?
26. Is there a maximum original block count?
27. Do all four map pack roles share exactly the same envelope reader?
28. Is the IsoMap decoded four-byte suffix mandatory, optional, or data-dependent?
29. Is that suffix a content terminator, zero record prefix, or padding?
30. Does Preview permit a zero-size final block?

## LZO

31. Which original LZO1X compressor level/version generated stock maps?
32. Did Westwood use a stock LZO encoder or an internal compatible encoder?
33. Are any stock streams outside miniLZO safe-decoder acceptance?
34. Does original runtime require the LZO end marker to consume the payload exactly?
35. Are trailing LZO bytes tolerated?
36. Are malformed LZO errors surfaced or ignored by the game?
37. Is a native permissive backend acceptable for all Unity target platforms?
38. Is a managed permissive implementation available with suitable security maturity?
39. What legal/patent review is required for distribution jurisdictions?
40. Should encoding be deferred until original writer compatibility is audited?

## Fragments and Base64

41. Does original runtime query keys sequentially from `1`?
42. Does it sort numeric keys?
43. Does it use INI source occurrence order?
44. Is key `0` accepted?
45. Do missing numbers stop collection?
46. Are gaps skipped?
47. Which duplicate occurrence wins, if any?
48. Are `1` and `01` equivalent?
49. Are leading plus/minus signs accepted?
50. Are nonnumeric keys ignored or fatal?
51. Does the original Base64 decoder ignore whitespace?
52. Does it accept missing padding?
53. Does it ignore nonalphabet characters?
54. Is fragment line length semantically relevant?
55. Can fragments occur across duplicate packed sections?
56. Does map-local INI composition ever combine packed sections from multiple documents?
57. Are comments allowed after fragment values?
58. Is Base64 decoded once after concatenation in the original loader?

## Architecture and evidence

59. Which independent, permissively licensed Format80 corpus can be redistributed?
60. Can an external LZO reference corpus be included without GPL contamination?
61. What canonical hash schema is stable across backend implementations?
62. Should strict failure preserve partial bytes internally for debugging?
63. Which diagnostics are public versus internal-only?
64. How are backend version and native binary hashes recorded?
65. Can original game behavior be probed without starting the executable?
66. What sample count and role diversity are sufficient for promotion?
67. Do future Ares/Phobos map extensions alter codec or only content semantics?
68. Should a tolerant forensic profile exist outside production Core?
69. How is a permissive backend sandboxed against memory-unsafe native code?
70. What is the removal/fallback plan if the selected dependency is abandoned?

All remain explicit. None authorizes a compatibility promotion.
