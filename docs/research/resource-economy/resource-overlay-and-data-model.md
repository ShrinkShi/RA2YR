# Resource Overlay and data model

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明的实现仅作行为、冲突和架构参考，未复制、逐句翻译、机械改写或移植其采集、经济、AI、随机、寻路或测试代码。`code_imported: false`。

## 1. Inputs

The resource-overlay binder consumes:

```text
OverlayPack exact decoded array
OverlayDataPack exact decoded array
explicit storage/index profile
composed [OverlayTypes] registry
resource registry candidates
product/theater/control profile
map-local provenance
```

Missing OverlayData remains missing. It is not synthesized as zero.

## 2. Family classification

Classification is evidence-bearing and may return multiple candidates:

| Family | Resource by default? | OverlayData candidate meanings |
|---|---:|---|
| Empty | no | none / stray raw data |
| Ore | yes candidate | frame, stage, density, quantity |
| Gems | yes candidate | frame, stage, density, quantity |
| TS green Tiberium | product-scoped | frame, stage, density |
| TS blue Tiberium | product-scoped | frame, stage, density |
| Veins | no ordinary ore | connection/growth/animation candidates |
| Wall/Fence | no | connection mask, damage/state |
| Bridge | no | piece/state/damage profile |
| Crate | no | type/state/animation |
| Debris/Rock | no | frame/variation |
| Rail/Track | no | connection/frame |
| Tunnel/Teleporter | extension/profile | family-specific |
| Extension resource | explicit only | extension-defined |
| Unknown | unresolved | raw only |

Rules:
- no pixel-color classification;
- no file-name-only classification except a clearly labeled low-grade profile;
- no “all nonempty Overlay are resources” rule;
- no “all nonzero OverlayData is quantity” rule;
- no deletion when Art is missing.

## 3. Official-editor evidence

At pinned EA editor revision `6abf0f557469baea73079c6bf6550709e2e3584e`, `Defines.h` carries four hardcoded ranges:

```text
Riparius  102..121
Cruentus   27..38
Vinifera  127..146
Aboreus   147..166
```

The same constants appear in both editor product branches. This confirms an official editor classification table, not the complete RA2/YR runtime registry.

`MapData.cpp`:
- initializes `overlay = 0xFF`;
- initializes `overlaydata = 0`;
- writes Overlay and OverlayData as independent 262144-byte arrays;
- sets data to zero when placing a new Overlay;
- estimates map resource money by resource-range checks.

The project does not copy these switches into runtime code. They remain reference evidence for candidate profiles.

## 4. Raw descriptor

```text
ResourceOverlayRaw
- OverlayTypeRaw
- OverlayDataPresence
- OverlayDataRaw?
- OverlayStorageCoordinate
- SourceOrdinal
- OverlayRegistryCandidates
- ProductProfile
- Diagnostics
```

`0xFF` is an ordinary-profile no-overlay sentinel inherited from M3-R7 research. Unknown high ordinals remain raw.

## 5. Binding result

```text
ResourceOverlayBindingResult
- RawReference
- FamilyCandidates[]
- ResourceTypeCandidates[]
- VisualAssetCandidates[]
- InterpretationProfiles[]
- SelectedPolicyResult?
- Ambiguities[]
- EvidenceGrade
- Diagnostics
```

Duplicate resource bindings do not last-win. The binder records all candidates and selected-policy provenance.

## 6. Stage, quantity and yield candidates

```text
ResourceStageCandidate
- RawData
- VisualFrameCandidate
- GrowthStageCandidate
- DensityCandidate
- DepletedCandidate
- RangeProfile
- Evidence

ResourceQuantityCandidate
- UnitsCandidate
- Min/MaxCandidate
- UnitScaleCandidate
- RawFormulaReference
- Evidence

ResourceValueCandidate
- ResourceType
- ValueRaw
- QuantityCandidateRef
- MultiplierCandidates
- CreditYieldCandidate
- Evidence
```

No parser-stage unique interpretation is selected.

## 7. Editor map-money estimate

The official editor contains an estimate shaped like:

```text
(OverlayData + 1) × RulesResourceValue
```

for its recognized resource ranges. Project interpretation:

- evidence: `ConfirmedByOfficialToolSource`;
- scope: editor map-money display/estimate;
- not proof of harvester capacity units;
- not proof of per-tick unloading;
- not proof of runtime credit rounding;
- not proof that every custom resource uses the same formula.

Stable community reports that runtime and editor estimates may diverge are `ConfirmedCommunityConvention`; the exact runtime formula remains `Unresolved`.

## 8. Missing and invalid states

Required diagnostics:

```text
MissingOverlayDataStream
OverlayPresentDataMissing
EmptyOverlayNonzeroData
UnknownOverlayOrdinal
ResourceFamilyAmbiguous
ResourceTypeMissing
DuplicateResourceBinding
InvalidStageCandidate
StageOutsideProfileRange
ArtReferenceMissing
ProductProfileConflict
```

Missing Art never removes the logical resource cell.

## 9. Extension ranges

Ares, Phobos, Vinifera/TS++ and other extension resources require explicit provider/version metadata:

```text
ExtensionProvider
ExtensionVersion
ApplicableProduct
ResourceOrdinalPolicy
StagePolicy
ValuePolicy
FallbackPolicy
```

Extension behavior is never labeled vanilla.

## 10. Depleted state

Raw map state and runtime state are distinct:

```text
RawAuthoredResourceCell
RuntimeResourceCellState
DepletedState
RegrowthEligibility
VisualOverlayState
```

When runtime quantity reaches zero, the simulation may select a visual/removal policy, but the raw map document remains unchanged.
