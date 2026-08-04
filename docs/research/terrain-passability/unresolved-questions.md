# Unresolved questions

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明实现仅作行为、格式事实与冲突参考，未复制、翻译、逐句改写或机械移植代码、寻路算法、Locomotor 实现或测试夹具。`code_imported: false`。


## 1. P0 — blocks implementation policy freeze

1. 原版 RA2/YR runtime对TMP `TerrainTypeRaw`的准确enum、signedness和precedence是什么？
2. theater TileSet role、TMP terrain byte和Rules land type冲突时谁优先？
3. `HeightRaw`是否对movement surface有任何stock语义，还是只服务TMP/rendering/tool？
4. stock ramp edge如何结合两侧RampType、Level和direction？
5. 最大允许ground Level delta及其产品差异是什么？
6. diagonal/corner movement是否存在额外surface检查？
7. `MovementZone`完整vanilla token集合、大小写和ordinal是什么？
8. 社区拼写 `Subterrannean` 是否是实际runtime token？
9. `WaterBeach`、`CrusherAll`、`InfantryDestroyer`等各自适用TS/RA2/YR哪个版本？
10. unknown/missing MovementZone实际fallback是什么？
11. `SpeedType`完整stock token集合和product applicability是什么？
12. explicit 0、missing terrain percentage和unknown land type如何区分？
13. road bonus与terrain percentage、base speed的组合顺序和rounding是什么？
14. Locomotor CLSID到stock能力的精确绑定表是什么？
15. invalid/missing Locomotor实际fallback或失败行为是什么？
16. MovementZone、SpeedType和Locomotor冲突时runtime path/step逻辑如何分工？
17. crusher/destroyer是否允许path planner穿过可压碎/可摧毁对象，还是只在movement execution阶段处理？
18. `OmniCrusher`与MovementZone Crusher/Destroyer的准确关系是什么？
19. resource Overlay是否对所有unit family均不阻塞，还是存在family/extension例外？
20. walls、fences和gates的blocking、crush和dynamic state precedence是什么？
21. gate state变化如何使path cache/graph失效？
22. TerrainTypes的stock occupancy由哪些Rules字段决定？
23. Smudge是否存在任何stock movement/buildability影响？
24. building Foundation、Foundation.X/Y、Bib与path blocking的准确关系是什么？
25. custom/irregular foundation如何隔离为extension而不污染vanilla profile？
26. high bridge Overlay存储到deck cells的准确expand规则是什么？
27. high bridge deck elevation、entrance/exit和under-bridge node如何由raw数据确定？
28. low bridge是否只覆盖land type而不改变elevation？
29. partially destroyed bridge的node/edge state机是什么？
30. bridge repair对underlying Overlay/occupancy的precedence是什么？
31. Unit placement `High`字段的准确语义和合法值是什么？
32. Tube metadata在stock RA2/YR是否被runtime消费，还是主要TS/editor遗留？
33. Tube direction sequence是否单向，何时需要counterpart？
34. subterranean/AllowBurrowing在YR stock和扩展中的边界是什么？
35. aircraft/jumpjet是否使用cell graph、continuous domain或混合模型？
36. landing/docking restriction是否硬绑定MovementZone/Locomotor？
37. infantry SubCell对path node occupancy和sharing的准确规则是什么？
38. moving/stationary/reservation在stock path query中的优先级是什么？
39. buildability和movement是否共享任何底层land flag，如何避免过度拆分？
40. exact movement cost numeric representation、sentinel、overflow和tie-breaking是什么？

## 2. P1 — needed before broad compatibility

- shallow water与ice dynamic transitions；
- water-bound buildings/naval yard adjacency；
- bridge-over-water naval movement；
- teleporter/special Overlay graph；
- veins/crates/debris family-specific passability；
- wall/gate owner access；
- deployable units和temporary foundation；
- destroyed buildings and rubble；
- weather/mission modifiers；
- path cache invalidation；
- save/load dynamic occupancy；
- extension land/MovementZone registry；
- custom ramp and cliff destruction；
- map edge/hidden transition；
- network deterministic cost composition。

## 3. P2 — adapter choices

- graph storage（dense/sparse）；
- incremental updates；
- query-time filter vs graph mutation；
- debug visualization；
- Unity adapter representation；
- pathfinder selection；
- heuristic；
- flow-field/cache；
- steering/reservation；
- editor diagnostics UX。

这些不应反向定义Core格式语义。

## 4. Evidence gaps

缺少：

- complete official RA2/YR runtime source；
- official locomotor source；
- official path/cost precedence；
- official bridge destruction topology；
- official MovementZone/SpeedType enum source；
- official dynamic occupancy model。

因此相关项保持 `Unresolved`。

## 5. Resolution requirements

P0项只能由以下之一提升：

- official runtime source；
- official editor source（仅能确认editor行为）；
- 多个真正独立实现加严格冲突分析；
- 固定community资料（仍只到CommunityDocumented）；
- future sanitized baseline observation（只到ObservedByFutureProjectBaselineAudit）；
- explicit project policy。

不得用截图、颜色、文件名或单个mod经验“证明”。
