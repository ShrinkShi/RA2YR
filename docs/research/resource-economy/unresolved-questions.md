# P0 unresolved questions

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明的实现仅作行为、冲突和架构参考，未复制、逐句翻译、机械改写或移植其采集、经济、AI、随机、寻路或测试代码。`code_imported: false`。

No question below is answered by assumption. Product, editor, independent implementation and extension evidence remain separate.

## Resource Overlay and registry

1. What exact RA2 1.006 / YR 1.001 runtime table binds Overlay ordinals to ore and gems?
2. Do the four official-editor ranges exactly match runtime family ranges in every theater/product?
3. How does YR bind map-local or extension resource Overlay types?
4. Is `[Tiberiums]` actively consumed by stock RA2/YR runtime in the same form as TS?
5. What precedence exists between hardcoded Overlay ranges, `[Tiberiums]`, `[OverlayTypes]`, theater/control INI and map-local Rules?
6. How are duplicate resource registry entries resolved by stock runtime?
7. What happens when resource Art is missing but the logical Overlay exists?
8. Are veins ever harvestable or economically linked in stock YR?

## Stage, quantity and value

9. What is the exact runtime meaning of `OverlayData` for every stock ore/gem Overlay family?
10. Is `OverlayData + 1` a runtime quantity, editor-only estimate, or visual-stage proxy?
11. Do ore and gems use identical stage ranges and quantity scales?
12. What does stage zero mean: one unit, empty, minimum density or frame zero?
13. What happens to out-of-range OverlayData: accepted, clamped, wrapped, normalized or rejected?
14. What exact unit does resource `Value` represent?
15. Is value applied per collection unit, per Overlay stage, per cell or through a separate bale scale?
16. What integer width, rounding and overflow behavior does stock runtime use?
17. Do difficulty, game mode or campaign rules modify resource value?
18. Can stock YR custom resources use values/families beyond the standard ore/gem sets?

## Harvester capability and cargo

19. Which exact Rules fields make a stock RA2/YR type a harvester?
20. What is the authoritative meaning and unit of vehicle `Storage`?
21. Is capacity shared across ore/gems or maintained per resource type?
22. Can stock YR cargo contain mixed ore and gems simultaneously?
23. What occurs with zero, negative, missing or oversized capacity?
24. Can map/script/savegame data create a partially loaded harvester, and in what representation?
25. Does stock runtime track cargo amount and cargo value separately?
26. Which state determines “full” and return-to-refinery behavior?
27. Does cargo affect speed in stock RA2/YR, and through what field?
28. What is the exact stock visual feedback: pips, body frame, selection UI, tooltip or combination?
29. How many pips and what resource-color mapping are stock behavior for each harvester family?

## Targeting, collection and reservation

30. Does a harvester collect from its own cell or an adjacent cell?
31. What facing/animation conditions gate a collection tick?
32. What amount is removed per collection event?
33. What is the exact collection interval and simulation-clock basis?
34. How does stock multiplayer resolve simultaneous harvesters on one resource cell?
35. Does stock runtime reserve resource cells, and how are claims released?
36. How are auto-target, retarget and refinery-direction preferences ordered?
37. What happens if the target depletes between path completion and collection?
38. Are stop/hold/guard commands allowed to interrupt the collection transaction atomically?

## Refinery and docking

39. Which fields identify a refinery and accepted resource types in stock YR?
40. How are dock cell, approach cell, facing and exit cell authored or derived?
41. Does stock YR support multiple refinery docks?
42. How are competing harvesters queued and tie-broken?
43. Can allied harvesters dock, and under what command/ownership rules?
44. What happens when a refinery is captured or destroyed during approach/unload?
45. Does power loss affect docking or unloading?
46. Is cargo unloaded once, per bale, per tick or by animation event?
47. In what order are cargo decrement, storage update, credits update and statistics committed?
48. Can unloading be partial because of storage limits?
49. What happens to mixed cargo during unload?
50. How is save/load performed mid-docking or mid-unload?

## Growth, spread and depletion

51. Do `TiberiumGrows` and `TiberiumSpreads` drive stock YR ore runtime exactly as the editor labels imply?
52. Which resource type fields control stock YR growth/spread?
53. What are the timer units, probabilities and deterministic RNG rules?
54. Which surfaces and occupancy states permit growth?
55. Which neighbors and ordering permit spread?
56. Does growth change quantity, visual stage or both?
57. Does depletion remove Overlay, leave residual data or select an empty frame?
58. Can depleted cells regrow without an Overlay?
59. Is there a stock RA2/YR ore mine/drill/generator contract?
60. Which public “resource generator” behaviors are extension-only?

## Economy and storage

61. What exact precedence selects starting credits among House, Basic carry-over, campaign state, lobby and game mode?
62. What are the units/scales of House `Credits` and carry-over fields?
63. How does stock YR distinguish cash from physical stored ore, if at all?
64. Are silos/storage active, vestigial or bypassed in each stock product mode?
65. What happens when storage capacity falls below stored resources?
66. What happens to stored resources on silo sell, destruction or capture?
67. Does refinery delivery convert directly to credits or first enter physical storage?
68. What are stock overflow/saturation limits for credits and stored resources?
69. How do crate and Trigger credit mutations interact with session accounts?
70. Which Trigger opcodes/parameters inspect or modify resource/economy state?
71. Which AI rules estimate ore fields, harvester/refinery counts and replacement needs?
72. Does AI use exact resource value, stage, quantity or a heuristic estimate?

## Movement, presentation and roundtrip

73. Does resource quantity affect movement cost, or only resource-family terrain?
74. Are resource cells passable for every ordinary ground locomotor?
75. How do reservations interact with occupancy and path graph updates?
76. What movement update occurs at depletion?
77. Which renderer frames correspond to canonical quantity, if any?
78. Can unload animation or sound be interrupted without changing economic outcome?
79. How should original raw Overlay identity be retained beside runtime depleted state in savegames?
80. Which writer behavior is required for invalid stages, unknown resources and extension values without normalizing them?
