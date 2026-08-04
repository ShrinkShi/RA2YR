> **Source notice:** This document was prepared by **ChatGPT Web** from public sources only. ProjectBaseline was not read. This is not a Codex artifact. GPL and unclear-license implementations are reference-only; no code was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

# P0 unresolved questions

No complete public RA2/YR production runtime source was located. The following questions remain P0 and must not be guessed during implementation.

## Registry and type identity

1. What exact stock registries participate in runtime production discovery?
2. Are unregistered but referenced sections ever accepted as producible types?
3. What are the exact duplicate-key and duplicate-value winner rules?
4. Is registry comparison case-sensitive in every context?
5. How do map-local type definitions compose with global registries?
6. Are registry gaps preserved as ordinals or compressed by the runtime?
7. What runtime limits apply to each registry?
8. Which known bugs depend on registry ordinal rather than type identity?

## Factory and category binding

9. What exact values and defaults are accepted by `Factory` in stock RA2/YR?
10. Which legacy building flags affect production capability versus exit behavior?
11. How is the primary factory selected when several qualify?
12. Is primary selection persistent, recalculated or user-controlled?
13. Which factory categories share queues?
14. Do naval and non-naval factories share any category state?
15. Does `NumberOfDocks` affect only docking or also production acceptance?
16. Which fields determine aircraft pad eligibility?
17. What exact stock behavior applies to cloning facilities?
18. How are repair, wall, upgrade and defense categories represented internally?

## Owner, Country and Side

19. What is the exact precedence among Owner, RequiredHouses and ForbiddenHouses?
20. Does Side participate directly or only through Country/House definitions?
21. How are campaign Houses and multiplayer countries mapped to production identity?
22. What does an unknown Owner token do in stock runtime?
23. Which map-local ownership fields can override Rules?
24. Does a captured factory grant category access, products, plans or none?
25. Does initial factory owner matter in stock YR outside extensions?
26. How do Secret Labs select and retain technology?
27. What are the exact stolen-tech states and persistence rules?

## Prerequisites

28. What is the exact stock grammar of `Prerequisite`?
29. Are separators only commas, and how are empty tokens handled?
30. Are all entries AND requirements in stock RA2/YR?
31. What exact semantics does `PrerequisiteOverride` have?
32. Which generic prerequisite tokens are stock and how are they expanded?
33. What is the exact precedence between generic and explicit prerequisites?
34. How does YR's refinery alternate prerequisite work?
35. Can upgrades satisfy stock prerequisites, and under what conditions?
36. How do Secret Lab, stolen tech and spy infiltration interact with normal prerequisites?
37. Does missing `Prerequisite` mean unconditional availability in every type family?
38. How are dangling prerequisite references handled?
39. Are duplicate prerequisite tokens significant?
40. Can map-local Rules alter generic prerequisite groups?

## TechLevel and BuildLimit

41. What is the stock TechLevel numeric domain?
42. What are the exact meanings of negative TechLevel values?
43. How does lobby TechLevel combine with authored TechLevel?
44. Does scenario or campaign context bypass TechLevel?
45. What exact object sets count toward BuildLimit?
46. Do queued objects count toward BuildLimit?
47. Do completed-but-unplaced buildings count?
48. Do captured and mind-controlled objects count?
49. Do cloned, scripted or starting objects count?
50. Do deploy/undeploy type pairs share BuildLimit?
51. Do upgrades share BuildLimit with hosts or other upgrades?
52. What is the exact stock meaning of negative BuildLimit values?
53. Is BuildLimit evaluated per player, House, alliance or another scope?
54. What happens when an acquisition path exceeds BuildLimit?

## Cost, time and credits

55. What exact numeric domain and clamp behavior applies to Cost?
56. Is construction cost always deducted progressively in stock YR?
57. What is the precise deduction interval and rounding?
58. What is refunded when an item is cancelled at partial progress?
59. What happens when funds reach zero mid-production?
60. What exact stock formula converts Cost and BuildSpeed into duration?
61. At which stages is `BuildTimeMultiplier` applied?
62. Which country/category modifiers affect cost and time?
63. What is the exact multiple-factory speed formula and rounding?
64. How does low power affect production rate or pause state?
65. What is the exact Factory Plant stacking, category and rounding behavior?
66. Does game speed modify authoritative build ticks or only wall-clock presentation?
67. How are negative Cost and zero-duration products handled?
68. What happens on arithmetic overflow?

## Queue, completion and capture

69. Is each stock queue owned by player, category, factory type or factory instance?
70. Can stock human players run parallel queues of one category?
71. What is the exact maximum queued count?
72. What are the semantics of pause, hold, repeat and cancel?
73. Can entries be reordered?
74. Which factory receives an accepted request?
75. Can assignment change while production is active?
76. What happens if the assigned factory is destroyed?
77. What happens if it is captured?
78. Which owner receives the completed product after capture?
79. Is paid progress retained, refunded or transferred?
80. What is the deterministic order of simultaneous completion, capture, destruction and credits transactions?
81. How is mid-queue save/load state serialized?
82. How are AI queues different from human queues?

## Exit, placement and transformation

83. How are stock unit exit cells and facing derived?
84. Is an alternate factory used when the active factory exit is blocked?
85. What happens while every valid exit remains blocked?
86. Can a completed product wait indefinitely?
87. What are the exact naval exit and shore requirements?
88. How are aircraft products assigned to pads/docks?
89. How many completed buildings may wait for placement?
90. Can the building queue continue while one item is ready?
91. What are the exact stock construction-yard adjacency rules?
92. How do shroud, resources, bridges and moving units affect placement?
93. What is the authoritative order for simultaneous placement reservations?
94. What happens when the construction yard is destroyed after completion?
95. What exact state transfers during deploy/undeploy?
96. How do deploy pairs interact with BuildLimit and prerequisites?
97. How are upgrades attached, limited, captured and removed?

## Power, UI, Trigger and AI

98. Which factory states stop, slow or permit production under low power?
99. How do EMP, power toggles and Trigger outages affect progress?
100. What exact stock sidebar visibility rules distinguish hidden and disabled?
101. What is the exact sidebar category and sorting comparator?
102. Is cost ordering applied only within equal TechLevel?
103. How are hotkeys selected and conflicts resolved?
104. Which UI state is serialized versus reconstructed?
105. Which Trigger actions can grant/remove production or technology in stock YR?
106. Which Trigger actions create products without normal availability checks?
107. How does AI evaluate prerequisites, BuildLimit, power and credits?
108. Does AI bypass or alter normal queue ownership?
109. Which rules are deterministic multiplayer state versus local client presentation?
110. Can a future sanitized audit distinguish these profiles without exposing a reconstructable tech tree?

## Resolution policy

A question can move from `Unresolved` only with explicit evidence and product/provider scope. An editor behavior, OpenRA choice, Ares extension or community tutorial cannot be silently promoted to original YR runtime fact.

No implementation was added. `code_imported: false`.
