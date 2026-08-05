# Warhead, Verses and Armor

> **Source notice:** Public-source research only. ProjectBaseline was not read. `code_imported: false`.

## Separation

```text
WarheadDefinitionRaw
ArmorIdentityRaw
VersesRawTokens
DamageMultiplierCandidate
ForceFireCandidate
RetaliateCandidate
PassiveAcquireCandidate
SpecialEffectCapability
AppliedStatus
```

A Verses value is not a complete targeting or status rule.

## Evidence

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert exposes Warhead/Armor/Verses fields and editor catalogs | `ConfirmedByOfficialToolSource` | EA editor | Official tool behavior only. | Named editor profile. | `NotRun` |
| Ares/Phobos/Vinifera/OpenRA named Armor and Verses extensions | `ImplementationSpecificBehavior` | Named implementations | Extension/target-specific. | Isolate provider/version. | `NotRun` |
| Ordered 11-entry stock-looking Armor profile and percentage Verses notation | `ConfirmedCommunityConvention` | ModEnc/PPM/community docs | Stable convention, not runtime source. | Preserve raw list and profile. | `NotRun` |
| Eleven-entry RA2/YR Armor order as leading candidate | `Underconfirmed` | Tools/community | Runtime strictness, malformed behavior and lineage independence unproven. | Explicit Armor registry profile. | `NotRun` |
| Missing/extra tokens, named versus positional Armor, side-effect suffixes and zero behavior | `ConflictingSources` | Tools/extensions/community | Direct model differences. | Keep raw tokens; no fill-to-100%. | `NotRun` |
| Exact runtime Armor lookup, multiplier fixed-point and targeting/status side effects | `Unresolved` | No runtime source | No complete contract. | Future simulation adapter. | `NotRun` |
| Raw/unknown Armor preservation, no clamp/default and separate targeting dimensions | `DefensiveDesign` | Project policy | Preservation/architecture. | Fail closed. | `NotRun` |

## Contracts

`VersesProfileRaw` preserves every token, exact percentage spelling, empties, extra tail, positional index and named-extension candidates. Unknown Armor remains an unresolved identity, not an index guess. Missing tokens are not synthesized; extra tokens are not discarded.

Warhead capability, ally/self scope, CellSpread, special effects and presentation references remain independent candidates. Applied damage/status is produced only by a future deterministic simulation command.
