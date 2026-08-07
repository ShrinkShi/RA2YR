# M3-R15 — Production, prerequisites and technology dossier

> **Source notice:** ChatGPT Web public-source research only. ProjectBaseline was not read. Public implementations are reference-only; no production, prerequisite, queue, placement, AI or UI code was copied or ported. `code_imported: false`.

## Boundary

```text
raw Rules/scenario descriptors
→ type registries and logical definitions
→ ownership/product profiles
→ prerequisite and factory candidates
→ availability query inputs
→ queue/payment/completion/placement contracts
→ future deterministic simulation and UI adapters
```

Parsing never creates actors, queues, factories, sidebar buttons, credits transactions, placement reservations or Unity objects.

## Non-collapse rules

Registry entry, type definition, runtime type, actor instance and sidebar entry are distinct. Factory definition, capability, runtime instance, queue, dock and exit are distinct. Visibility, prerequisite satisfaction, TechLevel, ownership, BuildLimit, credits, power, queue acceptance, completion and placement are independent dimensions.

## Formal grades

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

No complete original RA2/YR production runtime source was found and no claim has proven independent implementation lineages sufficient for the multiple-independent grade. FinalAlert behavior is official-tool evidence; OpenRA/Ares/Phobos/clients/tools are named implementation behavior; stable community conventions remain community evidence; cross-tool candidates are underconfirmed; direct prerequisite/queue/cost/power conflicts remain conflicting; exact runtime algorithms are unresolved.

Raw preservation, explicit profiles, no registry compression, no prerequisite repair, no visual/sidebar authority, checked arithmetic, stable queue identities and no simulation during parsing are `DefensiveDesign`.

```text
AuditStatus: NotRun
FutureEvidenceSource: ProjectBaselineAggregateAudit
```

These fields do not imply ProjectBaseline access and cannot promote compatibility or runtime evidence.

## Normalized claims

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert exposes production/type fields and editor validation | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Official editor only. | Named editor profile. | `NotRun` |
| OpenRA/Ares/Phobos/client production models | `ImplementationSpecificBehavior` | Named implementations | Target/extension-specific. | Keep separate. | `NotRun` |
| Common registry, Owner, Prerequisite, TechLevel, BuildLimit, Cost and BuildTime conventions | `ConfirmedCommunityConvention` | ModEnc/PPM/RA2 DIY | Convention only. | Product/profile provenance. | `NotRun` |
| Standard factory/category and availability candidates | `Underconfirmed` | Tools/community | Runtime strictness and lineage independence unproven. | Explicit profiles. | `NotRun` |
| Prerequisite grammar, queue ownership, payment, modifiers, capture and placement | `ConflictingSources` | Engines/extensions/community | Public models differ directly. | Preserve alternatives. | `NotRun` |
| Exact runtime availability, queue, transaction, completion and deployment | `Unresolved` | No runtime source | No complete contract. | Future simulation adapter. | `NotRun` |
| Raw/no-repair/layered availability and deterministic queues | `DefensiveDesign` | Project policy | Preservation/architecture. | Fail closed. | `NotRun` |

## Non-goals

No production parser/evaluator, queue, credits transaction, spawn/placement, deployment/upgrade, power/capture simulation, sidebar, AI, Unity, ProjectBaseline audit or compatibility promotion is included.
