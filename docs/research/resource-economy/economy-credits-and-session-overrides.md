# Economy credits and session overrides

> **Source notice:** ChatGPT Web public-source research. ProjectBaseline was not read. `code_imported: false`.

## Economy-source graph

```text
Rules defaults
map House credits
carry-over candidates
campaign persistence
lobby/game-mode money
refinery/crate/Trigger mutations
runtime player account
AI estimates
score/statistics
```

No single parser field defines final money.

## Evidence

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert exposes authored credits/carry-over/economy fields | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Official editor behavior only. | Preserve authored source profile. | `NotRun` |
| OpenRA/Ares/Phobos/Vinifera/client storage/cash behavior | `ImplementationSpecificBehavior` | Named implementations | Product/extension/client-specific. | Keep source layers separate. | `NotRun` |
| Stable community descriptions of credits, storage, refinery and carry-over | `ConfirmedCommunityConvention` | ModEnc/PPM/community docs | Convention only. | Provenance/product applicability. | `NotRun` |
| Authored House credits and refinery delivery as economy-source candidates | `Underconfirmed` | Tools/community | Runtime precedence and units are incomplete. | Explicit `EconomyOverridePolicy`. | `NotRun` |
| Stock RA2/YR versus TS/Ares/OpenRA silo/storage/cash models | `ConflictingSources` | Products/extensions/engines | Direct model differences. | Never merge profiles. | `NotRun` |
| Exact runtime precedence, sell/destruction/capture overflow and settlement rounding | `Unresolved` | No original-runtime source located | No complete account algorithm. | Future deterministic simulation/session adapter. | `NotRun` |
| Provenance-preserving economy graph and no parser mutation | `DefensiveDesign` | Project policy | Architecture/fail-closed design. | Commands/results separate. | `NotRun` |

## Descriptors

`EconomySourceDescriptor` records source kind, raw value, numeric candidates, source layer, scope, House/player reference, mode/product profile, evidence and diagnostics. `EconomyOverrideLayer` records applicability, merge-mode candidate and conflicts without selecting precedence during parsing.

## Distinct states

House Credits, CarryOverMoney/Cap, lobby starting money, game-mode override, physical stored resource, cash, spendable total, earned/spent statistics and score remain distinct. Refinery delivery produces an economy-mutation candidate only after explicit cargo acceptance; editor estimates never authorize mutation.

## Runtime boundary

Trigger/crate/refinery/sell/capture/storage commands are future simulation inputs. Core does not add credits, discard resources, clamp accounts, update score, execute AI or write lobby state into maps.
