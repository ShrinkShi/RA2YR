# Source comparison

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明的实现仅作行为、冲突和架构参考，未复制、逐句翻译、机械改写或移植其采集、经济、AI、随机、寻路或测试代码。`code_imported: false`。

## 1. Rules

- Official editor source is not original runtime source.
- Independent implementations demonstrate viable models and conflicts, not Westwood facts.
- Community documentation records field names and observed behavior at its stated evidence grade.
- TS and extension behavior never becomes vanilla YR behavior without separate evidence.
- Shared XCC/OpenRA/tool lineage is not counted as multiple independent proofs.
- All implementation sources are reference-only. `code_imported: false`.

## 2. Pinned and versioned sources

| Source | Revision / permanent reference | Paths or pages reviewed | License | Category | Product scope | Lineage / use |
|---|---|---|---|---|---|---|
| EA FinalSun / FinalAlert 2 | `6abf0f557469baea73079c6bf6550709e2e3584e` | `MissionEditor/Defines.h`, `MapData.cpp`, `SpecialFlags.cpp`, data INIs | GPL-3.0-or-later | official editor | TS and RA2 editor modes | official editor evidence; reference-only |
| XCC Utilities mirror | `6f91bf8b00d3acabb1be765118a37c0cb74e85ec` | map/overlay/tiberium readers where locatable | GPL headers / historical SourceForge lineage | reader/tool | Westwood formats | shared ancestry; reference-only |
| OpenRA | `a520984d91eda9de48a62b1d15c1e3bad0d4fb1a` | `ResourceLayer.cs`, `TSResourceLayer.cs`, `Harvester.cs`, `StoresResources.cs`, `Buildings/Refinery.cs`, `PlayerResources.cs`, resource claims/activities/renderers/bot modules | GPL-3.0-or-later | independent simulation/editor/AI | reimplementation | architecture/conflict evidence only |
| World-Altering Editor | `b4c9481e9b00fb0a38739049a046f528b6054ce2` | Overlay and Rules resource views where locatable | GPL-3.0-or-later | editor | TS/RA2/YR/extensions | tool evidence; reference-only |
| CNCMaps | `afb9c1ec118f5128cbc1f3fb5e35c7dfa0e422fb` | map/Overlay/resource rendering paths | MIT default with imported-code exceptions | reader/renderer | RA2/YR maps | lineage-sensitive; reference-only |
| MapTool | `f85f2226905496139f1258b5854fad915f9bbac6` | Overlay/resource map paths | GPL-2.0-or-later | editor/tool | RA2/YR | reference-only |
| CnCNet XNA client | `e6e367bbe04c1a0dc1e34a8fed2856ea3ab7e8c4` | scenario/client/economy consumers where locatable | GPL-3.0 | client | client-specific | not stock runtime |
| Chrono Divide SDK | `5943c4ae6c19897929d348a417d6d2f1481b75fd` | public SDK resource/economy interfaces where locatable | repository terms | independent web runtime | RA2-inspired | independent behavior only |
| openra2 / Vanguard | fixed public repository revisions where cited | resource/economy readers and runtime interfaces | repository-specific | independent implementation | RA2-inspired | not official |
| ModEnc | permanent/current pages | `OverlayPack`, `Tiberiums`, `Value`, `Growth`, `Spread`, `PipScale`, scan/AI fields | community wiki terms | documentation | TS/RA2/YR/extension mixed | `CommunityDocumented` |
| Project Perfect Mod | fixed topic URLs | OverlayData/resource calculation, harvesting, AI, unloading observations | forum terms | community reverse engineering | topic-specific | `CommunityDocumented` |
| RA2 DIY | fixed tutorial/topic URLs | ore/gem placement, Rules/resource and unload-art tutorials | forum terms | community/editor tutorials | RA2/YR modding | `CommunityDocumented` |
| Ares | versioned documentation | storage/silos, tiberium, harvester scan, spill, chain reaction | documentation; implementation not imported | extension documentation | YR+Ares | extension only |
| Phobos | versioned documentation | storage and resource extensions | GPL/documentation terms | extension documentation | YR+Ares+Phobos | extension only |
| Vinifera / TS++ | versioned docs/repos | TS storage, resource pips, spill, resource types | GPL/extension terms | extension documentation/runtime | TS extension | never migrated to YR |
| public Rules dictionaries | fixed gist/repository revision | field/comment examples | source-specific/unclear | informal reference | mixed | low-grade; no code import |

## 3. Evidence matrix

| Question | Official editor | Independent implementation | Community / extension | Project conclusion |
|---|---|---|---|---|
| Overlay and data are separate | confirmed | confirmed | documented | raw arrays remain independent |
| resource Overlay ranges | four editor ranges | configured registries | documented hardcoded families | explicit profile, not universal registry |
| OverlayData meaning | editor uses frame/stage-like data and money estimate | density/index models vary | conflicting descriptions | preserve raw and candidates |
| `(data+1)×Value` | editor map estimate | implementations use their own quantity models | runtime discrepancy discussed | editor-only evidence |
| `[Tiberiums]` fields | editor consumes some Rules values | configurable resource types | documented | raw registry + product profile |
| cargo capacity/current load | not complete runtime evidence | cleanly separate | Rules fields documented | separate descriptors |
| mixed cargo | not proven | independently supported | reports vary | model-capable, vanilla unresolved |
| unload cadence | not proven | explicit tick/bale implementation | animations/tutorials | policy candidate only |
| storage/silos | editor metadata insufficient | explicit cash/resource/capacity split | Ares/Vinifera restore/extend | stock YR unresolved |
| growth/spread | editor exposes SpecialFlags | independent deterministic systems | fields documented | capability only, no execution |
| UI pips/load | editor/runtime proof incomplete | renderer-specific | community/Ares docs | presentation descriptor only |
| starting credits precedence | metadata sources visible | lobby/runtime-specific | client/community | explicit override policy |

## 4. License boundary

No GPL or unclear-license harvesting, refinery, economy, pathfinding, AI, RNG or UI implementation was:
- copied;
- translated line-by-line;
- mechanically rewritten;
- transformed into a C# switch;
- used to generate production tests.

Only names, observed contracts, conflicts, provenance, licensing and high-level architecture were documented.

## 5. Source URLs

- EA editor: `https://github.com/electronicarts/CNC_TS_and_RA2_Mission_Editor`
- OpenRA: `https://github.com/OpenRA/OpenRA`
- WAE: `https://github.com/CnCNet/WorldAlteringEditor`
- XCC mirror: `https://github.com/OmniBlade/xcc`
- CNCMaps: `https://github.com/zzattack/ccmaps-net`
- MapTool: public fixed GitHub repository cited by prior research
- ModEnc: `https://modenc.renegadeprojects.com/`
- PPM: `https://ppmforums.com/`
- RA2 DIY: `https://bbs.ra2diy.com/`
- Ares docs: `https://ares-developers.github.io/Ares-docs/`
- Phobos docs: `https://phobos.readthedocs.io/`

URLs are research references, not dependencies or imported source.
