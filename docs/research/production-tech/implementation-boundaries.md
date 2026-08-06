# Implementation boundaries

> **Source notice:** Public-source research only. ProjectBaseline was not read. `code_imported: false`.

## Models

`ProductionRulesDocument`, `TypeRegistryEntryRaw`, `ProducibleTypeRaw`, `FactoryCapabilityDescriptor`, `PrerequisiteExpressionRaw`, `TechnologySnapshot`, `ProductionAvailabilityQuery/Result`, `BuildLimitDescriptor`, `CostDescriptor`, `BuildTimeDescriptor`, `ProductionQueueDescriptor/Entry`, `CompletionCandidate`, `PlacementRequest/Result`, `ExitDescriptor`, `TypeTransformationCandidate`, `SidebarEntryDescriptor`, diagnostics, limits and roundtrip descriptors.

## Formal grades

All evidence values use exactly one normalized grade. Source, Notes, Policy and AuditStatus are separate. No reviewed claim has original-runtime-source confirmation.

## Policy

```text
PolicyClassification: DefensiveDesign
AuditStatus: NotRun
```

Preserve raw registries/definitions/prerequisites/numerics/duplicates/unknown tails; explicit product/extension/defaulting/availability/queue/transaction/placement policies; checked arithmetic and bounded counts; stable identities/order; no registry renumbering, type fabrication, prerequisite repair, Owner fallback, UI inference, queue execution, actor creation or Unity dependency.

## Layering

Core parses immutable descriptors. Session/simulation owns current Houses, credits, power, factories, queues, BuildLimit counts, placement/occupancy, deploy/upgrade/capture and RNG. UI owns sidebar, cameo, hotkey, progress and feedback. Resource adapters own Art/CSF/cameo binding. None rewrites source Rules.

## Roundtrip

Preserve registry gaps/key spelling, duplicate sections/keys/values, raw Owner/prerequisite punctuation and empties, invalid TechLevel/BuildLimit/Cost/time text, map-local provenance, extension fields and unregistered definitions. Canonical rewrite is explicit, never default.
