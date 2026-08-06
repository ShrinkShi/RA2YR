# Damage resolution and rounding

> **Source notice:** Public-source research only. ProjectBaseline was not read. `code_imported: false`.

## Stage candidates

```text
BaseDamageRaw
SourceModifiers
TargetModifiers
WarheadVersesCandidate
Distance/FalloffCandidate
Difficulty/ModeCandidates
SpecialEffectCandidates
RoundingProfile
HealthMutationCommand
```

The order is not assumed. A parsed numeric expression never mutates health.

## Evidence

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert exposes Damage/Warhead/Armor-related editor fields | `ConfirmedByOfficialToolSource` | EA editor | Field/catalog behavior only. | Named editor profile. | `NotRun` |
| OpenRA and extensions implement explicit damage/rounding pipelines | `ImplementationSpecificBehavior` | Named implementations | Target/profile-specific algorithms. | Comparison profiles only. | `NotRun` |
| Percentage Verses and integer-damage conventions | `ConfirmedCommunityConvention` | ModEnc/PPM/community docs | Convention does not prove stage order or rounding. | Preserve raw spelling. | `NotRun` |
| Base damage × Verses as a leading candidate | `Underconfirmed` | Tools/community | Exact fixed-point representation and runtime order remain incomplete. | Explicit arithmetic profile. | `NotRun` |
| Truncation, rounding, minimum damage, negative damage/healing and modifier order | `ConflictingSources` | Engines/extensions/community | Models differ directly. | Keep multiple named profiles. | `NotRun` |
| Exact runtime stage order, armor lookup, multi-cell impact, death threshold and replay state | `Unresolved` | No runtime source | No complete contract. | Future simulation adapter. | `NotRun` |
| Checked wide intermediates, deterministic order and no clamp/repair | `DefensiveDesign` | Project policy | Safety/determinism. | Fail on overflow; record profile. | `NotRun` |

## Determinism

Every damage command carries stable source, weapon, projectile/impact, target and effect ordinals plus tick and arithmetic profile. Enumeration of area targets is canonical. RNG state is explicit and serializable. Camera, renderer frame, floating-point platform behavior, Unity physics order and dictionary order never enter authoritative damage.

## Outcomes

Separate damage expression, applied integer candidate, shield/armor/status candidates, health mutation, destruction/death command, score/economy consequences and presentation events. Zero, negative, overflowed and invalid values remain explicit diagnostics.
