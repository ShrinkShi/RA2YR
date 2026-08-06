# M3-R16 — Unit missions and command semantics dossier

> **Source notice:** ChatGPT Web public-source research only. ProjectBaseline was not read. Public implementations are reference-only; no mission, command, AI, pathfinding, targeting, combat or Unity code was copied or ported. `code_imported: false`.

## Boundary

```text
raw map/Rules/Script/command inputs
→ mission/command identity candidates
→ typed target and capability candidates
→ declarative command requests
→ deterministic transition/result contracts
→ future simulation, AI and UI adapters
```

Parsing never moves, attacks, guards, deploys, harvests, enters, repairs, captures, patrols, scatters, stops or mutates actors.

## Non-collapse rules

Authored placement Mission, ScriptType action, player command, AI order, current runtime mission, queued command and locomotor/combat substate are distinct. A mission name or editor dropdown item does not define a complete runtime state machine. `Stop`, `Guard`, `Area Guard`, hold-position and target engagement policies remain separate.

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

No complete original RA2/YR mission runtime source was found and no claim has proven independent implementation lineages sufficient for the multiple-independent grade. FinalAlert behavior is official-tool evidence; named engines/tools/extensions are implementation-specific; stable community mission names are community conventions; cross-tool candidates remain underconfirmed; command/transition conflicts remain conflicting; complete runtime lifecycle is unresolved.

Raw preservation, explicit target/capability/queue/lifecycle policies, no mission fallback, stable command identities, checked arithmetic, no execution during parsing and no UI/animation authority are `DefensiveDesign`.

```text
AuditStatus: NotRun
FutureEvidenceSource: ProjectBaselineAggregateAudit
```

These fields do not imply ProjectBaseline access and cannot promote compatibility or runtime evidence.

## Normalized claims

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert exposes Mission/Script/command catalogs and editor validation | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Official editor only. | Named editor profile. | `NotRun` |
| OpenRA/WAE/Chrono Divide/extensions implement mission/command models | `ImplementationSpecificBehavior` | Named implementations | Target/profile-specific. | Keep separate. | `NotRun` |
| Stable Guard/Move/Attack/Harvest/Enter/Repair/Capture/etc. names | `ConfirmedCommunityConvention` | ModEnc/PPM/RA2 DIY | Naming convention, not lifecycle proof. | Preserve raw tokens/product applicability. | `NotRun` |
| Common command/request and mission candidates | `Underconfirmed` | Tools/community | Runtime strictness and lineage independence unproven. | Explicit profiles. | `NotRun` |
| Guard/hold/stop, queueing, target typing, mission replacement and Script semantics | `ConflictingSources` | Engines/extensions/community | Public models differ directly. | Preserve alternatives. | `NotRun` |
| Exact runtime state transitions, interruption, persistence and AI/player precedence | `Unresolved` | No runtime source | No complete contract. | Future deterministic simulation adapter. | `NotRun` |
| Declarative requests, no fallback/execution and stable lifecycle IDs | `DefensiveDesign` | Project policy | Preservation/architecture. | Fail closed. | `NotRun` |

## Non-goals

No mission parser/executor, command queue, locomotion, pathfinding, combat, harvesting, docking, repair, capture, deployment, AI, UI, Unity, ProjectBaseline audit or compatibility promotion is included.
