# P0 unresolved questions

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明的实现仅作行为、接口与冲突参考，未复制、翻译或机械移植其代码、公式实现、switch 表或测试夹具。`code_imported: false`。


本文件列出在实现任何战斗系统前必须明确选择、审计或继续研究的问题。未解决不等于不支持；表示不能声称兼容。

## P0 list

1. 完整 stock RA2/YR Weapon 字段集合和字段默认值是什么？
2. Weapon section 的发现/注册规则是否依赖引用、硬编码或某个列表？
3. Primary/Secondary 的准确选择与fallback顺序是什么？
4. ElitePrimary/EliteSecondary 缺失时原版如何处理？
5. 同一武器多mount、多turret和多barrel的准确slot语义是什么？
6. Damage 的内部单位和允许范围是什么？
7. 负Damage在所有武器/弹头路径是否都代表healing？
8. ROF的准确时间单位以及game speed换算是什么？
9. Range/MinimumRange的准确内部单位和边界比较规则是什么？
10. Burst shot之间的准确间隔、target snapshot和ammo消耗顺序是什么？
11. Weapon Speed与Projectile字段的准确分工是什么？
12. Inviso是否在所有stock组合中都是即时/无实体弹体？
13. Arcing、ROT、Acceleration、Elasticity的准确组合优先级是什么？
14. ROT=0和ROT非零的stock tracking细节是什么？
15. Projectile与terrain/wall/cliff/elevation的准确碰撞查询是什么？
16. 高桥、桥下、aircraft和submerged目标的projectile layer规则是什么？
17. target消失、移动、limbo或死亡后的准确impact规则是什么？
18. stock RA2/YR Armor顺序是否在所有产品/模式中固定为11项？
19. Verses缺项、空项和多项的原版parser行为是什么？
20. Verses百分比的内部数值表示和舍入是什么？
21. 0%、1%、2%对force-fire、retaliation、passive acquire的准确原版行为是什么？
22. 负Verses与负Damage组合的符号和targeting行为是什么？
23. CellSpread的准确距离单位和最大安全范围是什么？
24. PercentAtMax的准确插值、距离度量和舍入是什么？
25. CellSpread中心是impact point、cell center还是target bounds？
26. 多cell building在stock YR中会被命中一次还是多次？
27. 空中和地面target是否使用2D或3D距离？
28. AffectsAllies对self/owner、healing和非damage效果的准确作用是什么？
29. CanSelect/CanFire/CanApply/CanDamage在原版中的检查顺序是什么？
30. AA/AG、Verses和target type冲突时哪个检查先发生？
31. ProneDamage、building和special modifier的准确运算顺序是什么？
32. firepower、veterancy、difficulty等multiplier的准确顺序是什么？
33. 每个百分比阶段是否立即整数截断，还是最终统一舍入？
34. minimum damage和zero damage优化的原版行为是什么？
35. 同时impact的稳定处理顺序是什么？
36. projectile和area target的对象枚举顺序是什么？
37. stock RNG的seed、draw顺序和savegame状态是什么？
38. Ammo是per actor、per weapon还是per magazine？
39. reload/cooldown/ROF之间的准确状态机是什么？
40. death weapon、debris、crew、rubble、Trigger的准确顺序是什么？
41. Fire/Radiation/MindControl/Temporal/Psychic/Parasite/Ivan/EMP的准确damage gate是什么？
42. status effect是否要求Verses>0或实际damage>0？
43. Wall、Wood、Ore、Conventional等Warhead字段的准确stock作用是什么？
44. Warhead对resources、bridges、terrain deformation的准确mutation顺序是什么？
45. presentation Anim/Report/laser/beam/particle是否存在任何模拟时序依赖？
46. screen shake和combat light的准确触发条件是什么？
47. map-local override对Weapon/Projectile/Warhead/Armor的准确composition precedence是什么？
48. Trigger weapon action传递owner/source/score attribution的准确规则是什么？
49. mind-controlled或captured source发射的projectile owner如何快照？
50. save/load mid-burst、mid-flight、mid-status的准确序列化字段是什么？
51. replay/network命令排序对combat结果的准确约束是什么？
52. original runtime对invalid/dangling Weapon/Projectile/Warhead引用的接受/崩溃行为是什么？
53. stock产品与Ares/Phobos/Vinifera named armor profile如何隔离？
54. Projectile interception等extension target family如何进入未来模型？
55. 是否存在stock projectile target、projectile armor或projectile strength？
56. Weapon/Warhead effects对allied/neutral/owner的分类是否在不同effect中不一致？
57. AreaFire、OmniFire、Lobber、FireOnce等字段的stock语义是什么？
58. TurboBoost、Supress、UseSparkParticles等字段的产品适用性是什么？
59. RadLevel、Beam、LimboLaunch等字段位于Weapon还是其他类型及其准确语义是什么？
60. editor/writer是否canonicalize Verses和numeric spelling？
61. original runtime是否接受duplicate sections/keys，winner规则是什么？
62. named armor case sensitivity在各extension provider中如何不同？
63. custom armor继承循环和default verses应由哪个policy处理？
64. Damage order候选中哪些阶段可能根本不存在于stock runtime？
65. 未来ProjectBaseline audit可否在不泄露exact值的前提下区分产品profile？

## Resolution requirements

每个问题的关闭记录必须包含：

```text
Decision
ProductProfile
ExtensionProvider?
EvidenceGrade
SourceReferences
AlternativeRejected
CompatibilityImpact
TestIds
AuditImpact
```

不得以“看起来像原版”“OpenRA这样做”或“Unity实现方便”为关闭理由。

## Implementation gate

在P0未决时允许：

- 保留raw字段；
- 建立opaque reference；
- 输出diagnostic；
- 使用显式 `ConfiguredForProjectPolicy` 的实验profile。

不允许：

- 宣称stock runtime兼容；
- 隐式fallback；
- 静默clamp/default；
- 将extension行为当vanilla；
- 用ProjectBaseline观察自动提升兼容状态。


## Evidence grades

- `ConfirmedByOfficialRuntimeSource`
- `ConfirmedByOfficialEditorSource`
- `ConfirmedByIndependentImplementation`
- `CommunityDocumented`
- `ObservedByFutureProjectBaselineAudit`
- `ConfiguredForProjectPolicy`
- `Unresolved`

没有完整公开的 RA2/YR 原版战斗运行时源码。官方 FinalSun/FinalAlert 2 只能提供编辑器、字段目录和 authoring 行为证据，不能替代 `gamemd.exe` 运行时证据。
