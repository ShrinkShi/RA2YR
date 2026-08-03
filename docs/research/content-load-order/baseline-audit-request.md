# Local ProjectBaseline read-only audit request

> **Execution boundary:** ChatGPT Web cannot read the configured runtime root or ProjectBaseline. This is a design for a later local Codex audit.

## 1. Purpose

Validate the configured archive profile, explicit nested mount graph, deterministic candidate ordering, and ordered INI composition without publishing original entry bodies or full archive indexes.

The audit must report configured results and unresolved original-runtime evidence separately.

## 2. Root boundary

The audit:

- receives the single configured authoritative game root;
- enumerates only its immediate root entries;
- never searches parent folders or registry alternatives;
- explicitly excludes FinalAlert/FinalSun, reference/tutorial, XCC export, unpacked mirror, Cache, and other installation paths;
- does not print the absolute root;
- fingerprints selected root candidates before and after;
- performs no writes.

## 3. Prohibited execution

Do not:

- start RA2/YR;
- start Unity;
- start XCC GUI;
- modify archives or loose files;
- extract entries;
- write a patch MIX;
- publish an entry directory;
- infer winners from file contents.

## 4. Root archive inventory

For root-level MIX candidates record only:

- approved logical archive name;
- family classification;
- sequence number/category;
- length;
- SHA-256;
- configured priority key;
- duplicate/invalid/gap diagnostics;
- whether mounted, ignored, ambiguous, or unclassified.

Do not publish a full list of unknown/private filenames. Aggregate unclassified counts by diagnostic category.

## 5. Numbered families

For each configured family report:

- recognized count;
- min/max sequence;
- gap count/ranges;
- duplicate normalized sequence count;
- invalid-width/nondecimal/out-of-range counts;
- canonical ordered-family hash.

For `expandmd`, evaluate the configured `01..99` grammar. Report `00` separately if present without mounting it by default.

Do not apply expansion ordering to `ecache` or `elocal`; report their names/categories only under their own descriptors.

## 6. Explicit nested mount graph

Build only configured mount edges.

Allowed public fields:

- logical parent/child archive names;
- family/role;
- aggregate node and edge counts;
- maximum depth;
- duplicate physical-mount count;
- cycle/depth/bounds diagnostics;
- canonical mount-graph hash.

Do not publish parent archive entry indexes or unconfigured child names.

## 7. Fixed logical candidate probes

Use a bounded allowlist of logical names chosen for role coverage, for example:

- `rulesmd.ini`;
- `soundmd.ini`;
- one Art/AI/UI/theater INI candidate where policy permits;
- one SHP;
- one VXL/HVA pair;
- one map or audio candidate.

For each, report:

- candidate count;
- ordered provider/archive families;
- priority keys;
- logical provenance chains;
- whole-file winner category for non-INI;
- ordered document-layer categories for INI;
- canonical trace hash;
- diagnostics.

Do not expose entry bodies, original pixels, voxel data, audio, or map content.

## 8. INI composition audit

For configured composable INIs:

1. resolve all same-named documents low-to-high;
2. parse through the repository's lossless INI document layer;
3. compose by section/key identity;
4. compute an effective semantic hash;
5. record counts only.

Allowed aggregates:

- source document count;
- section/key occurrence counts;
- effective section/key counts;
- cross-layer override count;
- inherited-key count;
- new-section/new-key count;
- same-document duplicate count;
- case-only conflict count;
- empty-value count;
- numeric-key/list count;
- per-key provenance completeness boolean;
- suppressed-chain maximum/aggregate range;
- canonical composition hash.

Forbidden:

- section names not already approved for the probe;
- key names or values;
- INI text;
- per-key hashes;
- comments;
- full lists;
- reconstruction-friendly occurrence sequences.

## 9. Required `rulesmd.ini` and `soundmd.ini` outputs

For each logical document report:

```text
ConfiguredProjectPolicy:
  LayerCategoriesLowToHigh
  LayerCount
  EffectiveEntryCount
  OverrideCount
  InheritedCount
  ProvenanceComplete
  CompositionSha256

OriginalRuntimeEvidence:
  State = Confirmed | Underconfirmed | ConflictingSources | Unresolved
  SourceReferences[]
```

Never report a whole-file winner for these configured composable documents.

## 10. Enumeration-randomization probe

Run discovery repeatedly with deterministic permutations of:

- root entry order;
- archive candidate insertion order;
- explicit child edge insertion order;
- candidate-index dictionary order.

Require identical:

- family classifications;
- priority keys;
- ordered layers;
- candidate traces;
- composition aggregates/hashes;
- diagnostics after canonical sorting.

## 11. Input-mode boundary

Memory, Stream, and MIX-window APIs may change how bytes are read, but must not decide priority.

For each selected archive/entry require equality of:

- archive/entry identity;
- candidate priority;
- provenance;
- logical document layer;
- parse/composition model hash;
- diagnostics.

## 12. Public result schema

```text
ContentLoadOrderAuditSummary
- AuditVersion
- PolicyProfileId
- SourceFingerprintBefore
- SourceFingerprintAfter
- RootArchiveAggregate
- FamilyAggregates
- MountGraphAggregate
- CandidateProbeAggregates
- IniCompositionAggregates
- EnumerationDeterminism
- InputModeEquivalence
- ExclusionCounters
- DiagnosticCounts
- ConfiguredPolicyResult
- OriginalRuntimeEvidenceState
- SanitizedSummarySha256
```

Use an explicit field allowlist and fail on unknown output fields.

## 13. Publicly forbidden data

- archive entry bodies;
- INI bodies, values, comments, or complete section/key lists;
- full archive directory listings;
- per-entry SHA lists;
- extractable or reconstructable resource data;
- Base64 or hex;
- screenshots;
- absolute paths;
- usernames or machine identifiers;
- ProjectBaseline cache paths.

## 14. Decision gates

### Configured policy verification

May be marked verified when:

- root exclusion is proven;
- all ordering is deterministic;
- candidate traces are complete;
- INI layers compose without a whole-file winner;
- randomized enumeration and input modes agree;
- before/after fingerprints match.

### Original-runtime claim

Requires official/original evidence or a separately approved black-box experiment. Project policy success alone does not promote a statement to original behavior.

### Compatibility

No audit result automatically modifies `docs/compatibility/matrix.yml`.
