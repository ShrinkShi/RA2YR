> **Source notice:** This document was prepared by **ChatGPT Web** from public sources only. ProjectBaseline was not read. This is not a Codex artifact. GPL and unclear-license implementations are reference-only; no code was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

# Source comparison

## Evidence matrix

| Topic | Official editor | OpenRA | Ares | ModEnc / PPM | Result |
|---|---|---|---|---|---|
| type registry ordinal | editor performs Rules/type lookup | independent actor definitions | extends stock registries | documents registry constraints and bugs | preserve raw registry identity and gaps |
| prerequisite grammar | editor exposes data but not full runtime evaluator | explicit prerequisite traits | negative, alternative, theater, stolen-tech and factory-plan extensions | stock/community grammar claims | explicit grammar profiles |
| factory category | type/editor evidence | Production and Exit are separate traits | `BuiltAt`, explicit factories and cloning | factory tutorials and shared-queue reports | capability and exit remain separate |
| TechLevel | field/editor/community evidence | independent prerequisite system | extension interaction | numeric behavior claims | authored/session/UI separation |
| BuildLimit | no complete public runtime source | independent limit implementations | fixes and extensions | detailed community observations | explicit counting policy |
| cost/time | editor displays values | target-engine ticks and queue policy | per-type build-time controls | community formulas | no Westwood unit inferred |
| queue ownership | no complete runtime source | several queue families | AI parallel/load-sharing extensions | stock shared-category queue reports | product-profile policy |
| placement | authoring/editor behavior | independent footprint/buildability | irregular foundation extensions | community observations | simulation query separate from preview |
| sidebar | editor is not runtime sidebar | independent widget layer | UI queue/hotkey extensions | sorting claims | UI is downstream |
| capture/power | metadata only | explicit runtime traits | many extensions | community behavior | future simulation policy |

## EA FinalSun / FinalAlert 2

Pinned revision: `6abf0f557469baea73079c6bf6550709e2e3584e`.

Relevant paths include `MissionEditor/MapData.cpp` and `MissionEditor/Defines.h`. License headers state GPL-3.0-or-later.

The official editor is authoritative for editor data handling only. Editor defaults and recovery behavior are not original runtime production facts.

## OpenRA

Pinned revision: `a520984d91eda9de48a62b1d15c1e3bad0d4fb1a`, GPL-3.0-or-later.

Relevant architecture paths include:

- `OpenRA.Mods.Common/Traits/Buildable.cs`;
- `OpenRA.Mods.Common/Traits/Player/ProductionQueue.cs`;
- `OpenRA.Mods.Common/Traits/Player/ClassicProductionQueue.cs`;
- `OpenRA.Mods.Common/Traits/Player/ParallelProductionQueue.cs`;
- `OpenRA.Mods.Common/Traits/Production.cs`;
- `OpenRA.Mods.Common/Traits/Buildings/Building.cs`;
- `OpenRA.Mods.Common/Traits/Buildings/Exit.cs`;
- production tooltip widget logic.

The separation is useful architecture evidence. YAML, ticks, placement, queue and transaction algorithms are independent implementation choices.

## Ares

Versioned Ares 3.0 documentation is strong evidence for named extension fields and stated bug fixes. Relevant pages cover prerequisites, generic groups, negative prerequisites, factory-owner plans, `BuiltAt`, cloning, BuildTime, Factory Plant, powered units and UI queue extensions.

Every such field carries `ExtensionProvider=Ares` and version provenance.

## ModEnc

Permanent/current revisions for `BuildLimit`, `Cost`, `BuildTime` and `BuildTimeMultiplier` provide detailed community claims. They remain `CommunityDocumented` unless supported by stronger evidence.

## PPM and RA2 DIY

Forums and tutorials are conflict discovery and practical behavior evidence. Individual examples do not define a universal parser or runtime contract.

## Shared lineage

XCC-derived, OpenRA-derived and community-derived implementations are not counted as independent confirmations when they share code or documentation ancestry.

## Unresolved source gaps

No complete public RA2/YR source was found for:

- production availability precedence;
- exact prerequisite grammar;
- BuildLimit counting sets;
- shared versus per-factory queues;
- credits deduction/refund order;
- construction-time rounding;
- capture/queue transfer;
- completion and blocked-exit behavior;
- sidebar visibility and sorting.

All reviewed code is reference-only; `code_imported: false`.
