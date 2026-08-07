# Source comparison and license boundaries

> **Source notice:** ChatGPT Web public-source research. No local `ProjectBaseline` access. Not a Codex artifact. No GPL or unclear-license code was copied, translated, mechanically rewritten, or ported. `code_imported: false`.

## 1. Method and formal grades

Sources are evidence, not code-import candidates. Record project, pin, path, license, category, lineage, supported claims and limitations.

Formal grades are limited to:

```text
ConfirmedByOriginalRuntimeSource
ConfirmedByOfficialToolSource
ConfirmedByMultipleIndependentImplementations
ConfirmedCommunityConvention
ImplementationSpecificBehavior
DefensiveDesign
ConflictingSources
Underconfirmed
Unresolved
```

No complete original RA2/YR runtime source was located and no reviewed claim has demonstrably independent implementation lineages sufficient for `ConfirmedByMultipleIndependentImplementations`.

Future ProjectBaseline work remains:

```text
AuditStatus: NotRun
FutureEvidenceSource: ProjectBaselineAggregateAudit
```

Agreement between related tools and shared XCC/OpenRA/community catalogs is not source-count proof.

## 2. Source matrix

| Source | Pin/category | Direct support | Limitations | Grade use |
|---|---|---|---|---|
| EA FinalSun / FinalAlert 2 | `6abf0f557469baea73079c6bf6550709e2e3584e`; official editor; GPL-3.0-or-later | editor repair/default logic, catalogs, identity/UI and read/write behavior | not game runtime; includes XCC-related components elsewhere | `ConfirmedByOfficialToolSource` |
| World-Altering Editor | `b4c9481e9b00fb0a38739049a046f528b6054ce2`; editor; GPL-3.0-or-later | common Trigger/Tag/Event/Action/Team/TaskForce/Script/AITrigger layouts and editor behavior | configurable catalogs, defaults, skips/refusals, community dependencies | `ImplementationSpecificBehavior` |
| OpenRA | `a520984d91eda9de48a62b1d15c1e3bad0d4fb1a`; reimplementation; GPL-3.0-or-later | supplementary declarative-trigger architecture | intentionally different actor/trait runtime | `ImplementationSpecificBehavior` |
| CnCNet XNA client | `e6e367bbe04c1a0dc1e34a8fed2856ea3ab7e8c4`; client; GPL-3.0 | consumer compatibility context | no complete Trigger/AI execution source | `ImplementationSpecificBehavior` |
| MapTool | `f85f2226905496139f1258b5854fad915f9bbac6`; tool; GPL-2.0-or-later | transformation and parser/writer cross-checks | shared community conventions | `ImplementationSpecificBehavior` |
| CNCMaps | `afb9c1ec118f5128cbc1f3fb5e35c7dfa0e422fb`; mixed renderer/parser | supplementary behavior | imported OpenRA/XCC areas not independent | `ImplementationSpecificBehavior` |
| Chrono Divide SDK | `5943c4ae6c19897929d348a417d6d2f1481b75fd`; public SDK | limited reimplementation context | no complete resolving implementation found | `Underconfirmed`/absence record |
| XCC/OmniBlade | `6f91bf8b00d3acabb1be765118a37c0cb74e85ec` lineage | historical format/tool knowledge | shared with several descendants/editor components | lineage only |
| ModEnc | fixed/current Trigger/Team/AI pages | field/layout/name conventions | community aggregation, incomplete/speculative areas | `ConfirmedCommunityConvention` |
| PPM / RA2 DIY | fixed pages/topics where available | authoring practices, AI/Trigger details | community knowledge, not runtime source | `ConfirmedCommunityConvention` or `Underconfirmed` |
| Ares / Phobos docs | named extension docs | extension Events, Actions, Scripts and AI behavior | extension-only; never vanilla | `ImplementationSpecificBehavior` |

All implementation sources are reference-only; `code_imported: false`.

## 3. Official-editor boundary

FinalAlert proves its own validation, repair, default, display-catalog and ID workflow behavior. It does not prove malformed runtime acceptance, protocol-standard display names, Event/Action execution, or byte-identical stock serialization.

## 4. WAE boundary

WAE strongly supports common writer/reader shapes:

- Trigger eight fields and Tag three fields;
- Event count plus opcode/profile-dependent parameters;
- Action count plus opcode/seven slots;
- TeamType list plus per-ID section;
- TaskForce `count,type` entries and common six-slot editor model;
- Script `action,argument` entries;
- AITrigger 18-field profile.

Each is WAE behavior. Defaults, skipped invalid records, unsupported-opcode refusals and configured extensions are not runtime rules.

## 5. Community and extension boundary

ModEnc/PPM names and layout descriptions are `ConfirmedCommunityConvention` where stable. Ares and Phobos prove extension growth as `ImplementationSpecificBehavior`. Community consensus and multiple tools sharing catalogs do not establish original-runtime execution.

## 6. Retained conflicts

| Conflict | Grade | Reason |
|---|---|---|
| Trigger final field meaning | `ConflictingSources` | repeating/reserved/unused terminology differs |
| Event tuple width | `ConflictingSources` | base slots versus configured/extension additions |
| unknown Event/Action handling | `ConflictingSources` | editor refusal/default versus lossless preservation |
| Action final-slot `A` behavior | `ImplementationSpecificBehavior` plus `Underconfirmed` | WAE compatibility rewrite, runtime meaning unsourced |
| TaskForce/Script gaps | `ConflictingSources` | slot models, stop-at-gap and raw preservation differ |
| AITrigger fields 11/13 | `Unresolved` | unused/unknown/named candidates lack runtime source |
| global/local ID conventions | `Underconfirmed` | `-G` and source-layer interpretations lack runtime proof |

## 7. Normalized evidence summary

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert editor validation/catalog behavior | `ConfirmedByOfficialToolSource` | EA editor | Official tool only. | Named editor profile. | `NotRun` |
| WAE and extension implementations | `ImplementationSpecificBehavior` | Named tools/extensions | Recorded separately. | Explicit product/profile isolation. | `NotRun` |
| Stable field/opcode/layout conventions | `ConfirmedCommunityConvention` | ModEnc/PPM | Not execution proof. | Neutral labels plus provenance. | `NotRun` |
| Cross-tool common record shapes | `Underconfirmed` | Public tools/community | Independence/runtime strictness unproven. | Raw-preserving layout profiles. | `NotRun` |
| Tuple/default/gap/tail semantics | `ConflictingSources` | Tools/community/extensions | Sources directly differ. | Preserve all alternatives. | `NotRun` |
| Complete runtime Trigger/Team/AI semantics | `Unresolved` | No runtime source | No reliable complete state machine. | Future executor separate. | `NotRun` |
| No execution, no repair, explicit catalog/reference policies | `DefensiveDesign` | Project policy | Preservation and safety. | Fail closed. | `NotRun` |

## 8. License policy

Do not port GPL parsers/switches/catalog prose, translate control flow, reproduce source-shaped pseudocode, or import proprietary fixtures. Define original schemas, record factual numeric/layout information with provenance, create independent synthetic fixtures, and perform separate dependency/license review.
