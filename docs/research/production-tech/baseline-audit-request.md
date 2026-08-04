> **Source notice:** This document was prepared by **ChatGPT Web** from public sources only. ProjectBaseline was not read. This is not a Codex artifact. GPL and unclear-license implementations are reference-only; no code was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

# Future ProjectBaseline sanitized audit request

## Status

This document designs a future read-only audit. No ProjectBaseline file was accessed or executed during M3-R15.

Any future result is evidence grade:

```text
ObservedByFutureProjectBaselineAudit
```

It cannot automatically promote compatibility.

## Selection basis

A future audit may select broad, non-identifying categories such as:

- type registry family;
- broad product category;
- factory-capability category;
- stock versus extension-provider profile;
- map-local contribution present/absent;
- prerequisite-shape category;
- TechLevel/BuildLimit bucket;
- cost/time-field presence;
- deployment/upgrade/power field presence.

Selection criteria must not reveal individual type identities or reconstruct the tech tree.

## Allowed public aggregates

- `SelectionBasis` description;
- broad type/category counts;
- registry section presence;
- registry entry counts and gap counts;
- duplicate-key/value and case-collision counts;
- listed-missing and unregistered-section counts;
- factory capability categories;
- production-category counts;
- prerequisite token-count and group-shape histograms;
- unknown/empty/extension-token counts;
- TechLevel and BuildLimit coarse buckets;
- Owner binding status categories;
- Required/Forbidden field-presence counts;
- Cost/BuildTime field-presence and coarse buckets;
- deploy/undeploy/upgrade field presence;
- power-production/drain field presence;
- cameo/sidebar binding status;
- structured diagnostic counts;
- non-linkable aggregate hash;
- Memory/Stream/short-read/MIX equivalence.

## Forbidden public data

Do not publish:

- type, building, unit, aircraft, factory or upgrade names;
- map names or filesystem paths;
- INI section or value text;
- exact registry lists or ordinal-to-type mappings;
- exact prerequisite expressions or token sequences;
- exact Owner, RequiredHouses or ForbiddenHouses lists;
- exact Cost, BuildTime, TechLevel or BuildLimit;
- complete technology or availability graphs;
- queue configuration or product order;
- placement coordinates, foundations or exit cells;
- cameo, Art, PCX, SHP, palette or CSF IDs;
- Trigger IDs, opcodes or parameters;
- AI build-list contents;
- graph topology;
- screenshots or rendered UI;
- per-type, per-map or per-resource hashes;
- hex, Base64 or reconstructable raw content.

## Aggregate hashing

A permitted hash must combine many records, be salted for the audit run and be unsuitable for linking one result to a specific type or map. Per-type and per-record hashes are forbidden.

## Proposed result schema

```text
ProductionAuditSummary
- SelectionBasis
- InputModeEquivalence
- RegistryFamilyCounts
- RegistryGapAndCollisionCounts
- TypeBindingStateCounts
- FactoryCategoryCounts
- PrerequisiteShapeBuckets
- TechLevelBuckets
- BuildLimitBuckets
- OwnershipBindingBuckets
- CostFieldBuckets
- BuildTimeFieldBuckets
- TransformationFieldCounts
- PowerFieldCounts
- SidebarBindingBuckets
- DiagnosticCounts
- NonLinkableAggregateHash
- EvidenceGrade
```

## Audit safety rules

- read-only;
- no map/game/editor execution;
- no content extraction;
- no asset decoding;
- no queue or availability evaluation against actual player state;
- no screenshot generation;
- no compatibility-matrix modification;
- no ADR, third-party ledger or `.dev-records` update from this research branch;
- fail closed if sanitization cannot be proven.

## Independence

The audit must compare Memory, seekable Stream, short-read Stream and exact MIX-window inputs using the same parser and limits. It must not repair raw inputs or canonicalize them before measuring.

## License statement

The audit design was written from public research only. No GPL or unclear-license implementation was copied, translated or mechanically ported. `code_imported: false`.
