> **Source notice:** Prepared by **ChatGPT Web** from public sources only. ProjectBaseline was not read. This is not a Codex artifact. GPL and unclear-license implementations are reference-only; no code was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

# P0 Unresolved Questions

All questions remain P0 until stronger evidence is found. No default is selected merely because it matches OpenRA, an extension, a community enum table, or familiar gameplay.

1. What is the exact stock YR Mission Control enumeration and is there public official runtime source?
2. How are text mission names mapped to numeric mission identities in stock RA2 versus YR?
3. Does map placement Mission accept every Mission Control token?
4. What happens to a missing placement Mission?
5. What happens to an unknown placement Mission?
6. Do duplicate Mission fields use first, last, or another rule?
7. Which Rules sections provide default missions by actor category?
8. Are Mission Control section order and numeric enum order identical?
9. Which mission values are perpetual in stock YR?
10. What is the exact runtime difference between Guard and Area Guard?
11. Is Sticky reachable by normal player commands in stock YR?
12. What exact behavior does Stop apply to firing, movement, queue, and target persistence?
13. When does Stop complete versus remain current mission?
14. Do ordinary non-Guard units always passively acquire enemies in range?
15. What conditions suppress passive acquisition?
16. What is the exact distinction between passive acquire and retaliation?
17. Can a unit retaliate while Hold-like movement suppression is active?
18. What target scan interval and ordering does stock YR use?
19. How is threat priority calculated for player-controlled autonomous units?
20. Does Guard return to the exact origin cell, a radius, or a path anchor?
21. What is the stock Guard chase radius or leash policy?
22. How does Area Guard choose and preserve its origin?
23. What ends an autonomous chase?
24. How are unreachable autonomous targets abandoned?
25. How does MinimumRange affect chase and target persistence?
26. How are cloaked or disguised targets retained or dropped?
27. How does target ownership change affect current attack commands?
28. How do bridge deck and under-bridge layers affect pursuit?
29. How do aircraft missions differ from ground-unit missions?
30. How do naval units interpret Guard and Area Guard origins?
31. What is the stock attack-move command representation?
32. Does attack-move use Guard, Area Guard, Patrol, or a separate state?
33. Can stock units fire while moving and under what flags?
34. How are turret tracking and body movement coordinated?
35. Are attack-cell and force-fire represented by the same command type?
36. What happens when an explicit target dies before a queued attack activates?
37. How is Shift queue append encoded and synchronized?
38. Does a normal order replace the entire queue or only pending entries?
39. Does Stop clear the full waypoint/command queue?
40. How are multiple actor queues batched from one UI command?
41. When are queued commands validated: issue time, activation time, or both?
42. How are invalid queued targets handled?
43. How are ordinary waypoint nodes represented internally?
44. How are patrol loop links represented?
45. How does patrol return after chasing an enemy?
46. How are synchronized waypoint groups released in multiplayer?
47. What route state is saved for partially completed waypoint chains?
48. What occurs when the owner of a queued actor changes?
49. What exact prerequisites validate DeploysInto and UndeploysInto?
50. How are health, veterancy, ammo, cargo, and facing transferred on deploy?
51. How are deploy and sell conflicts ordered?
52. Can Stop cancel a transformation after it begins?
53. How is a repair-facility entry command represented?
54. How do repair weapons differ from repair-facility missions?
55. How is engineer capture distinguished from ordinary Enter?
56. How is spy infiltration distinguished from capture and sabotage?
57. What relationship checks allow entering allied transports or buildings?
58. How are moving transport targets reserved during embark?
59. What happens if a transport fills while passengers approach?
60. What is the exact stock transport capacity arithmetic using Passengers, Size, and SizeLimit?
61. Which actor categories may be passengers in stock YR?
62. How are passenger order and occupant slots serialized?
63. What is the deterministic unload order?
64. How are blocked unload cells searched?
65. Can stock unload partially succeed and retain remaining passengers?
66. How do naval landing craft choose unload cells?
67. How do aircraft transports handle airborne unload and passenger survival?
68. How does IFV/gunner special handling affect unload and occupancy?
69. How are garrison occupants ordered?
70. How does RA2 occupant weapon behavior differ from YR?
71. What happens to occupants when a garrison changes owner?
72. What is the exact passenger survival policy on transport destruction?
73. How are passengers restored after save/load?
74. Are control groups synchronized game state or local UI state?
75. How does type selection order selected actors?
76. How are duplicate hotkeys resolved in stock clients?
77. Which command cursors are authoritative versus cosmetic?
78. How does cursor hiding interact with AI and scripted commands?
79. What parts of waypoint UI state are saved?
80. What is the stock same-tick command ordering across players?
81. How are simultaneous mission transitions ordered?
82. Does stock simulation use deterministic target scan ordering?
83. What RNG, if any, affects autonomous target choice?
84. What queue and autonomous state must be serialized for replay equivalence?
85. Can future ProjectBaseline audit distinguish product profiles without exposing exact tokens?
