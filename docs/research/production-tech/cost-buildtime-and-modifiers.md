# Cost, build time and modifiers

> **Source notice:** Public-source research only. ProjectBaseline was not read. `code_imported: false`.

## Separate values

```text
AuthoredCost
CurrentCostCandidate
DisplayedCost
CreditsReservation/Deduction/Refund
BuildTimeInputs
Factory/Country/Difficulty/Power/Plant/MultipleFactoryModifiers
FinalDuration
DisplayedTime
```

No equality or precedence is assumed.

## Evidence

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert exposes Cost/BuildTime-related fields and editor validation | `ConfirmedByOfficialToolSource` | EA editor | Official tool only. | Named editor profile. | `NotRun` |
| OpenRA/Ares/Phobos implement cost/time modifiers and transactions | `ImplementationSpecificBehavior` | Named implementations | Target/extension-specific. | Keep separate profiles. | `NotRun` |
| Stable Cost/BuildTime/BuildTimeMultiplier conventions | `ConfirmedCommunityConvention` | ModEnc/PPM/community docs | Convention only. | Preserve raw spelling/applicability. | `NotRun` |
| Cost-derived time and common modifier candidates | `Underconfirmed` | Tools/community | Runtime units/order/rounding and independence unproven. | Explicit arithmetic profile. | `NotRun` |
| Upfront/progressive payment, modifier stacking, low-power, multiple-factory, Factory Plant and refunds | `ConflictingSources` | Engines/extensions/community | Models differ directly. | Preserve alternatives. | `NotRun` |
| Exact runtime transaction timing, rounding, cancellation and capture/destruction behavior | `Unresolved` | No runtime source | No complete contract. | Future simulation adapter. | `NotRun` |
| Checked arithmetic, no clamp/default and separate presentation | `DefensiveDesign` | Project policy | Safety/architecture. | Fail on overflow; record profile. | `NotRun` |

`BuildTimeExpressionCandidate` records base inputs, units, ordered modifier candidates, rounding/clamp profile and evidence. Parser never reserves/deducts/refunds credits or advances progress. Zero, negative, missing, invalid and overflowed values remain distinct.
