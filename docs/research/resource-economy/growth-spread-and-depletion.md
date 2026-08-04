# Growth, spread and depletion

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明的实现仅作行为、冲突和架构参考，未复制、逐句翻译、机械改写或移植其采集、经济、AI、随机、寻路或测试代码。`code_imported: false`。

## 1. Authored capability inputs

Candidate sources:
- `[SpecialFlags] TiberiumGrows`;
- `[SpecialFlags] TiberiumSpreads`;
- RA2 editor labels “Ore grows” and “Ore spreads”;
- resource type `Growth`;
- resource type `Spread`;
- percentage fields;
- scenario/game-mode/Trigger override candidates;
- extension resource generators.

A raw `yes` never modifies the map during parsing.

## 2. Official-editor boundary

The pinned EA editor reads and writes `TiberiumGrows` and `TiberiumSpreads`. In RA2 mode it relabels them as ore growth/spread. The code is an editor UI and metadata path, not a runtime growth implementation.

Evidence:
- field existence/editor applicability: `ConfirmedByOfficialEditorSource`;
- exact runtime timer/probability/cell selection: `Unresolved`.

## 3. Capability descriptors

```text
ResourceGrowthDescriptor
- ResourceTypeRef
- EnabledCandidates[]
- IntervalCandidates[]
- PercentageCandidates[]
- MaxStageCandidates[]
- CellEligibilityPolicyRef
- ProductProfile
- Evidence

ResourceSpreadDescriptor
- ResourceTypeRef
- EnabledCandidates[]
- IntervalCandidates[]
- PercentageCandidates[]
- NeighborSelectionCandidates[]
- TargetEligibilityPolicyRef
- ProductProfile
- Evidence
```

## 4. Runtime event candidates

```text
GrowthEventCandidate
- SourceCell
- ResourceType
- OldQuantity
- DeltaCandidate
- EligibilitySnapshot
- ScheduledTick
- RandomDecisionRef

SpreadEventCandidate
- SourceCell
- TargetCellCandidate
- ResourceType
- TransferOrSpawnAmountCandidate
- EligibilitySnapshot
- ScheduledTick
- RandomDecisionRef
```

Parsers create neither event.

## 5. Deterministic random policy

Future RNG must define:
- serialized seed/state;
- stream identity;
- actor/cell ordering;
- candidate list ordering;
- draw count;
- save/load restoration;
- multiplayer/replay consistency.

Forbidden:
- Unity `Random`;
- wall clock;
- hash iteration;
- renderer frame order;
- unsaved global RNG.

## 6. Cell eligibility

Potential exclusions/capabilities:
- map boundary;
- missing logical cell;
- incompatible surface;
- ramp;
- water/shore;
- bridge deck/under bridge;
- building occupancy;
- static blocker;
- temporary blocker;
- existing different resource type;
- max density;
- scenario-disabled;
- extension rules.

No visual adjacency or palette color is used.

## 7. Resource generators

Potential providers:
- authored map object;
- building type;
- Overlay;
- animation;
- Trigger;
- extension ore mine/drill;
- periodic spawn trait.

Stock RA2/YR ore-mine/drill contract is insufficiently evidenced in public sources and remains `Unresolved`. Ares/Phobos/Vinifera behavior requires explicit extension profile.

## 8. Depletion

```text
ResourceDepletionDescriptor
- ZeroThreshold
- OverlayRemovalCandidate
- EmptyVisualCandidate
- ResidualOverlayCandidate
- RegrowthEligibility
- TargetInvalidationPolicy
- MovementUpdateCandidate
- SavegameStateCandidate
```

When quantity reaches zero:
- raw authored cell is retained;
- runtime state may become depleted;
- current harvest target may invalidate;
- presentation may hide/remove/change frame;
- movement may receive a new surface-state notification;
- regrowth eligibility is separate.

## 9. Stage transition

Potential transitions:
- quantity increment without visual stage change;
- visual stage derived from quantity;
- direct stage increment;
- capped maximum;
- wrap/clamp/editor normalization;
- resource removal at zero.

Each profile states arithmetic and evidence. Parser never clamps invalid raw stage.

## 10. Save/load

Savegame must capture:
- current quantity;
- depleted/regrowth state;
- timer state;
- deterministic RNG state;
- pending events;
- reservations referencing cells.

Original map roundtrip remains unchanged.

## 11. Trigger boundary

Triggers may enable/disable, create/remove or test resources depending on opcode profile. Research records opcode/parameter candidates only. No Trigger execution or cell mutation.

## 12. Presentation boundary

Sparkle, animated resources, debris, light, radar color and growth animation observe logical state. Renderer never schedules growth or selects spread targets.
