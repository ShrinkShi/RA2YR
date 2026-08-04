> **Source notice:** This document was prepared by **ChatGPT Web** from public sources only. ProjectBaseline was not read. This is not a Codex artifact. GPL and unclear-license implementations are reference-only; no code was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

# M3-R15 production, prerequisites, technology and sidebar dossier

## Purpose

Research-only boundary for RA2/YR-style type registries, factories, prerequisites, TechLevel, BuildLimit, cost/time, production queues, completion, placement, power/capture and sidebar consumers.

No production parser, evaluator, queue, spawn, placement, credits mutation, AI, sidebar or Unity object is implemented.

## Candidate pipeline

```text
raw Rules and scenario descriptors
→ type registries and logical definitions
→ ownership and product-profile binding
→ explicit prerequisite expression candidates
→ factory capability and production-category candidates
→ availability query inputs
→ queue/placement declarative contracts
→ future deterministic simulation and UI adapters
```

## Frozen identity boundaries

```text
TypeRegistryEntryRaw
!= TypeDefinitionRaw
!= ProducibleTypeDescriptor
!= RuntimeTypeDescriptor
!= RuntimeActorInstance
!= SidebarEntryDescriptor

FactoryTypeDefinition
!= FactoryCapabilityDescriptor
!= FactoryRuntimeInstance
!= ProductionQueue
!= ExitDescriptor

LogicalProductionAvailability
!= SidebarVisibility
!= QueueAcceptance
!= CreditsTransaction
!= Completion
!= PlacementResult
```

## Required rules

- A registry parser does not create units, buildings or sidebar buttons.
- Unregistered sections remain preserved but are not automatically producible.
- Registry gaps, duplicates, case collisions and map-local contributions remain explicit.
- Prerequisite parsing does not query current player assets.
- Availability supports several simultaneous blockers.
- Queue, payment, completion, exit and placement are separate transactions.
- Sidebar is downstream and never authoritative for simulation.
- Core has no `UnityEngine` dependency and creates no `GameObject`, `Button`, `ProgressBar`, `Tilemap`, actor or queue.

## Evidence grades

- `ConfirmedByOfficialRuntimeSource`
- `ConfirmedByOfficialEditorSource`
- `ConfirmedByIndependentImplementation`
- `CommunityDocumented`
- `ObservedByFutureProjectBaselineAudit`
- `ConfiguredForProjectPolicy`
- `Unresolved`

No complete public RA2/YR production executor was located. Missing runtime details remain unresolved.

## Files

1. `layer-and-domain-boundaries.md`
2. `producible-type-and-factory-binding.md`
3. `prerequisites-techlevel-and-buildlimits.md`
4. `cost-buildtime-and-modifiers.md`
5. `production-queues-and-completion.md`
6. `placement-deployment-and-exits.md`
7. `power-ownership-capture-and-availability.md`
8. `sidebar-cameo-hotkey-and-ui-boundaries.md`
9. `source-comparison.md`
10. `implementation-boundaries.md`
11. `test-matrix.md`
12. `baseline-audit-request.md`
13. `unresolved-questions.md`

## Source anchors

| Source | Revision / path | Class | Use |
|---|---|---|---|
| EA FinalSun / FinalAlert 2 | `6abf0f557469baea73079c6bf6550709e2e3584e`, `MissionEditor/MapData.cpp`, `MissionEditor/Defines.h` | GPL-3.0-or-later; official editor | registry/editor evidence only |
| OpenRA | `a520984d91eda9de48a62b1d15c1e3bad0d4fb1a`, `Traits/Buildable.cs`, `Traits/Player/ProductionQueue.cs`, `Traits/Production.cs`, `Traits/Buildings/Building.cs`, `Traits/Buildings/Exit.cs`, production tooltip logic | GPL-3.0-or-later; independent implementation | architecture comparison only |
| Ares 3.0 documentation | prerequisites, factories/cloning, build time, Factory Plant and UI queue pages | extension documentation | Ares-specific behavior only |
| ModEnc | `BuildLimit`, `Cost`, `BuildTime`, `BuildTimeMultiplier` revisions | community documentation | behavior candidates |
| Project Perfect Mod / RA2 DIY | fixed discussions and tutorials | community evidence | conflicts and edge cases |
| prior RA2YR research | PR #31, #32, #33, #35, #37, #38 | project research | dependency boundaries only |
