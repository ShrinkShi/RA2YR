# Implementation boundaries

> **Source notice:** Public-source research only. ProjectBaseline was not read. `code_imported: false`.

## Models

`MissionIdentityRaw`, `MissionDescriptor`, `CommandRequest`, `CommandTarget`, `CommandAcceptanceResult`, `CommandQueueDescriptor`, `RuntimeMissionSnapshot`, `MissionTransitionCommand/Result`, `EngagementPolicy`, `ScriptCommandCandidate`, `MissionCapabilityDescriptor`, diagnostics, limits and roundtrip descriptors.

## Formal grades

All evidence-bearing values use exactly one normalized grade. Source, Notes, Policy and AuditStatus are separate. No reviewed claim has original-runtime-source confirmation.

## Policy

```text
PolicyClassification: DefensiveDesign
AuditStatus: NotRun
```

Preserve raw Mission/action/argument/target tokens, duplicates, unknowns and sentinels; require explicit product/extension/catalog/target/queue/transition profiles; no fallback to Guard, no enum-by-list-order, no automatic target conversion, no mission execution during parsing, stable command/transition IDs, canonical actor ordering, bounded queues and checked arithmetic; no animation, cursor, UI or Unity authority.

## Layering

Parser/Core owns immutable descriptors. Session/UI emits commands. AI/Script/Trigger adapters emit declarative requests. Simulation owns capability checks, queue arbitration, target validation, path/combat/harvest/dock/repair/capture/deploy state and mission transitions. Presentation owns cursor, animation, audio and feedback. None rewrites authored Mission or Script text.

## Roundtrip

Preserve exact Mission spelling/case/whitespace, Script action/argument text, queue modifiers, target references, unknown tails, duplicates and source provenance. Runtime mission snapshots and command history are save/replay state, not source-map rewrite.
