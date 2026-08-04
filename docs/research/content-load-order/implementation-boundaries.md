# Recommended implementation boundaries

> Design only. No C# implementation is included.

## 1. Layered architecture

```text
Root boundary
  ↓
Content discovery
  ↓
Archive-family classification
  ↓
Explicit mount graph
  ↓
Ordered candidate index
  ├── whole-file resolver
  └── ordered logical-document resolver
            ↓
      lossless INI documents
            ↓
      semantic composition policy
            ↓
      effective typed views
```

## 2. Proposed types

### `ContentLayerDescriptor`

- stable descriptor ID/version;
- provider kind and scope;
- archive family;
- accepted exact names or grammar;
- game/version profile;
- root/nested placement;
- family rank;
- sequence rule;
- explicit child mount descriptors;
- evidence level;
- allowed logical content scopes.

### `ContentProviderKind`

Candidates:

- `LegacyMix`
- `LooseDevelopment`
- `UserMod`
- `ModernAssetPackage`
- `ExplicitGeneratedCache`

### `ArchiveFamily`

Named values, not filename conditionals:

- RA2 base;
- YR base;
- numbered RA2/YR expansion;
- cache/local/language;
- maps/movies/theme;
- theater/generic/conquer;
- user/modern manifest package;
- unknown.

### `SequenceNumber`

- raw digit text;
- parsed integer;
- width;
- configured range;
- validity diagnostics;
- no implicit conversion from malformed names.

### `ContentPriorityKey`

A serializable lexicographic key compiled centrally from descriptors. It must not contain path, timestamp, length, SHA, or enumeration ordinal.

### `ContentCandidate`

Raw/normalized name, provider, mount chain, entry identity, length, optional provenance SHA, priority key, evidence, diagnostics.

### `ContentResolutionTrace`

All ordered, suppressed, rejected, and ambiguous candidates plus canonical hash.

### `ContentDiscoveryDiagnostic`

Stable code, severity, provider/mount identity, safe logical name, family/sequence context, and bounded details.

### `ContentDiscoveryLimits`

Bounds for root entries, archives, nested depth, mount edges, candidates/name, bytes, diagnostics, and canonical serialization.

## 3. Additional INI types

### `LogicalDocumentLayer`

- logical document name;
- content candidate;
- layer ordinal low-to-high;
- lossless document identity;
- composition profile.

### `IniCompositionPolicy`

- section/key comparers;
- within-document duplicate policy reference;
- cross-layer override rule;
- empty/reset behavior;
- numbered-list handling strategy;
- include/inheritance extension flags;
- map/mode overlay placement.

### `IniCompositionResult`

- ordered source documents;
- effective sections/keys;
- winner/suppressed occurrence provenance per key;
- unresolved/ambiguous entries;
- diagnostics;
- canonical semantic hash.

### `IniEffectiveEntry`

The effective raw value plus source and suppressed chain.

## 4. Responsibilities

### Discovery

- bounded filesystem enumeration;
- classify names;
- no content parsing except archive header validation after selection;
- no INI parsing.

### MIX reader

- parse one bounded archive;
- look up entry IDs/windows;
- no root scan;
- no global priority;
- no semantic merge.

### Mount graph

- follow explicit child descriptors;
- no arbitrary recursive MIX scan;
- detect repeated identities/cycles.

### Candidate index/sorter

- single source of truth for all priority keys;
- deterministic and serializable;
- retains all candidates.

### INI resolver

- requests ordered document layers;
- parses losslessly;
- composes section/key semantics;
- never scans disk or chooses archives.

### Typed Rules/Art/Sound views

- consume a completed `IniCompositionResult`;
- apply schema/default/list/reference semantics;
- never open files.

## 5. Future providers

Modern packages can participate by declaring:

- provider kind;
- scope;
- explicit priority relative to named legacy layers;
- logical-name and format policy;
- manifest identity;
- diagnostics.

They cannot alter the legacy ordering profile implicitly. A modern texture provider may override textures while being denied Rules/Art scope.

## 6. Data-driven policy

Archive descriptors should be configuration/data objects validated at startup. Benefits:

- no scattered magic priority integers;
- separate RA2, YR 1.000, YR 1.001, Ares, and future profiles;
- testable sequence grammar;
- auditable policy diff;
- reusable discovery engine;
- explicit evidence labels.

## 7. Deterministic sorter

All order calculations live in one component. Tests must use an independently implemented oracle, not production comparer helpers.

The key and normalized model must be serializable so the local audit can report it without entry content.

## 8. Provenance

Provenance is never discarded after choosing a winner or effective INI key.

For each file/key retain:

- provider and archive family;
- root and nested mount chain;
- sequence and priority key;
- entry/window identity;
- document/occurrence ordinal;
- winner/suppression reason;
- diagnostics.

## 9. Error behavior

- duplicate logical layer: ambiguous/fail-closed;
- malformed sequence: unclassified/diagnostic;
- candidate budget exceeded: partial trace and failure;
- nested cycle/depth exceeded: stop edge;
- unconfigured family: do not mount;
- INI composition ambiguity: typed view unavailable or partial according to explicit policy;
- unknown deletion/list semantics: preserve raw and diagnose, never guess.

## 10. Core isolation

No UnityEngine dependency is needed for discovery, archive parsing, lossless INI, composition, or provenance. Rendering and modern asset import adapters consume resolved content later.
