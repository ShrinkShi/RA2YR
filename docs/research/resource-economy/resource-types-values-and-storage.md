# Resource types, values and storage

> **来源与许可证声明**
>
> 本文件由 **ChatGPT 网页版**基于公开资料独立研究完成；未读取 ProjectBaseline；不是 Codex 产物；GPL 或许可证不明的实现仅作行为、冲突和架构参考，未复制、逐句翻译、机械改写或移植其采集、经济、AI、随机、寻路或测试代码。`code_imported: false`。

## 1. Registry candidates

Public tools and community documentation expose a `[Tiberiums]` list-to-section model. The Core candidate preserves:

```text
ResourceTypeRaw
- RegistryKeyRaw
- ResourceTypeIdRaw
- SectionOccurrences
- RawProperties
- DuplicateKeys
- SourceLayer
- ProductProfile
```

It does not assume every listed field exists or has the same meaning in TS, RA2 and YR.

## 2. Candidate fields

| Field candidate | Separate semantic destination |
|---|---|
| `Name` | display/localization reference |
| `Image` | visual/Overlay family reference |
| `Overlay` | explicit extension binding candidate |
| `NumImages` | visual-stage range candidate |
| `Value` | economic-value candidate |
| `Power` | gameplay-effect candidate |
| `Growth` | growth timing/capability candidate |
| `Spread` | spread timing/capability candidate |
| `GrowthPercentage` | probability/rate candidate |
| `SpreadPercentage` | probability/rate candidate |
| `Color` | radar/minimap candidate |
| `Debris` | presentation/gameplay-effect candidate |
| `Explosion` | destruction/effect candidate |
| `Heal` | gameplay-effect candidate |
| `Damage` | gameplay-effect candidate |
| `PipIndex` | UI candidate |
| unknown | raw extension property |

Fields remain raw until a product/profile-specific policy binds them.

## 3. Product profiles

```text
TS_Tiberium_Profile
RA2_OreGem_Profile
YR_OreGem_Profile
Ares_Resource_Extension_Profile
Phobos_Resource_Extension_Profile
Vinifera_TS_Extension_Profile
Unknown_Profile
```

Rules:
- TS green/blue Tiberium cannot be transferred wholesale to YR.
- Ares-restored TS behavior is extension evidence, not proof that YR supported it.
- Vinifera is TS-focused and cannot define YR defaults.
- shared editor names such as Riparius do not prove identical gameplay.

## 4. Value dimensions

Strict separation:

```text
ValuePerUnit
ValuePerStage
ValuePerCell
HarvesterLoadValue
CreditsDelivered
CurrentPlayerCredits
ScoreValue
AIResourceEstimate
```

`ValueRaw` needs:
- signed and unsigned candidate views where applicable;
- exact numeric spelling;
- missing vs explicit zero;
- negative and overflow diagnostics;
- map-local override provenance;
- extension provider;
- unit/scale unresolved marker.

## 5. Stage-to-value profiles

Potential profiles:

```text
EditorOverlayDataPlusOne
RuntimeQuantityTimesValue
VisualStageOnly
FixedCellValue
ExtensionDefined
Unknown
```

The project does not select one globally. A profile must state:

```text
QuantitySource
QuantityOffset
ValueSource
MultiplierOrder
ArithmeticWidth
Rounding
OverflowBehavior
ProductApplicability
EvidenceGrade
```

## 6. Harvester capacity units

Possible units:
- resource bales/units;
- Overlay stages;
- abstract storage points;
- delivered-credit equivalent;
- extension-defined weighted volume.

No UI pip or editor money display settles this question. `CargoCapacityPolicy` keeps units explicit.

## 7. Storage domains

```text
CargoCapacity
BuildingStorageCapacity
PlayerEconomicAccount
PhysicalStoredResource
DisplayedCredits
```

These must not be merged.

### Vehicle storage

Authored type capacity is immutable input. Current content belongs to runtime cargo.

### Building/refinery storage

A building may contribute capacity, accept delivery, convert to credits, or both depending on product/profile. `Storage` cannot be interpreted without type family and product evidence.

### Silo/player storage

Ares documentation describes restored TS-style storage and says RA2 ordinarily does not use the old silo mechanic. This is strong extension documentation but not official runtime source. Therefore:

- stock RA2/YR silo semantics: `Unresolved`;
- Ares storage: `CommunityDocumented`/versioned extension;
- OpenRA storage/cash split: independent implementation only.

## 8. Storage-full outcomes

Candidates:

```text
RejectDelivery
PartialAccept
DiscardExcess
ConvertDirectlyToCash
CapPhysicalStorage
LoseStoredResource
ExtensionDefined
Unknown
```

The parser chooses none. Unloading policy must decide atomically and deterministically.

## 9. Silo capture/sell/destruction

Future simulation needs explicit commands:

```text
StorageCapacityAdded
StorageCapacityRemoved
StoredResourceLost
StoredResourceTransferred
OwnerChanged
```

Whether and how stock RA2/YR applies these remains P0. No inference from presentation pips.

## 10. Roundtrip

Preserve:
- `[Tiberiums]` list gaps and duplicates;
- duplicate resource sections/keys;
- exact `Value`, `Growth`, `Spread` spelling;
- unknown fields;
- map-local overrides;
- unresolved Overlay/Image references;
- extension fields and provider;
- invalid values without repair.
