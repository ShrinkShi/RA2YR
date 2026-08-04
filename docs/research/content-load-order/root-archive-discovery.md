# Root archive discovery

## 1. Root boundary

Discovery receives one symbolic `ConfiguredRuntimeRoot`. It must not search parent folders, registry-discovered alternate installs, editor folders, exports, caches, or unpacked mirrors.

A root-boundary object should contain:

- stable root identity;
- allowed provider kinds;
- excluded path classes;
- case-comparison policy;
- maximum entries and archives;
- a public-safe logical label.

The public audit never emits the absolute root.

## 2. Discovery phases

1. Enumerate immediate root entries under a bounded API.
2. Normalize filename metadata without opening content.
3. Classify exact names and family-pattern candidates.
4. Diagnose duplicates, ambiguous case variants, invalid sequence syntax, and unsupported names.
5. Sort by a centralized profile, not enumeration order.
6. Open only selected archive candidates under byte/count budgets.
7. Build explicit child-mount candidates from family descriptors.
8. Publish a deterministic discovery model and diagnostics.

Opening every `.mix` and recursively scanning it is forbidden.

## 3. Exact base roots

For the configured YR profile:

```text
ra2.mix    base rank 0
ra2md.mix  base rank 1
```

Missing base files are diagnostics. The reader does not substitute a similarly named file or search another installation.

`ra2md.mix` overlays `ra2.mix` under `ConfiguredProjectPolicy`; this is distinct from proving every original executable code path used the same generalized provider abstraction.

## 4. Numbered roots

`expandmdNN.mix` candidates are recognized by a strict grammar owned by the profile. The baseline grammar accepts exactly two ASCII digits and configured sequence `01..99`.

The scanner:

- continues through gaps;
- does not stop at the first missing number;
- sorts numeric sequence ascending for low-to-high composition;
- exposes high-to-low order as a derived query view;
- diagnoses `00`, one-digit, three-digit, signed, suffixed, or otherwise malformed variants according to explicit policy;
- does not use directory enumeration order as a tie-breaker.

Public sources that include `00` are recorded as conflicting evidence. The project does not silently expand its profile.

## 5. Loose provider

The loose provider is a separately configured layer, normally highest in the project profile.

It does not mean every file in the root is runtime content. The provider uses:

- allowed logical extensions and scopes;
- exact root-only location;
- stable filename normalization;
- collision diagnostics;
- explicit opt-in for development-only modern assets.

Whether vanilla accepts loose INI, SHP, VXL, map, audio, or UI files uniformly is underconfirmed. The project policy may expose them through a deterministic provider while retaining `EvidenceLevel=ConfiguredProjectPolicy`.

## 6. Excluded directories

The scanner must prove that it did not traverse:

- FinalAlert/FinalSun;
- tutorial/reference directories;
- XCC exports;
- unpacked mirrors;
- cache directories;
- alternate installations.

A test fixture should place attractive matching names under each excluded directory and assert zero candidates.

## 7. Case and duplicate names

Modern Windows behavior is not proof of original Westwood lookup semantics.

The project must configure a canonical archive-name comparer. If two physical names normalize to the same logical name or sequence:

- retain both candidates;
- emit `DuplicateNormalizedArchiveName` or `DuplicateArchiveSequence`;
- do not choose by enumeration order, timestamp, size, SHA, or path spelling;
- fail the family layer or require an explicit administrator decision.

## 8. Invalid names

Files that end in `.mix` but fail all configured families are:

- retained as unclassified discovery records when within audit budget;
- not mounted automatically;
- diagnosed once per bounded group;
- eligible for a future explicit user-mod provider descriptor.

They are not silently treated as ordinary numbered expansions.

## 9. Discovery outputs

Suggested output:

```text
ContentDiscoveryResult
- RootIdentity
- PolicyProfileId
- OrderedRootLayers[]
- UnclassifiedArchives[]
- DuplicateGroups[]
- ExcludedSourceCounters
- Diagnostics[]
- CanonicalDiscoverySha256
```

No entry bodies are needed to establish root ordering.
