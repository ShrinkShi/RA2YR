# INI resolution implications

## 1. Required semantic model

Configured composable INIs use ordered multi-document semantic composition.

Given layers `L0..Ln` from low to high:

1. parse each layer as a lossless INI document;
2. preserve document-local ordering, duplicate sections, duplicate keys, comments, raw spelling, and diagnostics;
3. map semantic identities using an explicit comparer;
4. apply layers in order;
5. for each `(SectionName, KeyName)`, the later candidate becomes effective;
6. retain all prior candidates as the suppressed chain;
7. inherit identities absent from later layers;
8. add identities introduced by later layers.

No raw text concatenation and no whole-file winner.

## 2. Example

Low layer:

```ini
[E1]
Weapon=none
Strength=125
```

High layer:

```ini
[E1]
Weapon=AK47
Cost=200
```

Effective view:

```ini
[E1]
Weapon=AK47
Strength=125
Cost=200
```

The effective `Weapon` points to the high-layer occurrence and retains the low-layer occurrence as suppressed provenance.

## 3. Pipeline

```text
Content discovery
→ ResolveOrderedDocuments("rulesmd.ini")
→ LosslessIniDocument[]
→ IniSemanticComposer(policy)
→ IniCompositionResult
→ typed Rules/Art/Sound/AI/UI/theater view
```

- discovery does not parse INI;
- MIX reader does not merge INI;
- INI resolver does not scan disk;
- typed views do not reopen archives.

## 4. Provenance model

Each effective entry records:

```text
IniEffectiveEntry
- SemanticSectionIdentity
- SemanticKeyIdentity
- RawWinningSectionName
- RawWinningKeyName
- RawValue
- WinningOccurrence
- SuppressedOccurrences[]   // ordered low to high
- LayerOrdinal
- DocumentIdentity
- ContentCandidateProvenance
- CompositionDiagnostics[]
```

A section also retains the ordered documents that contributed any effective or suppressed member.

## 5. Within-document duplicates versus cross-layer override

These are separate dimensions.

### Same document

Repeated section/key behavior belongs to the lossless document and its document-local resolution policy. It may preserve first, last, all, or report ambiguity depending on the repository's established INI semantics.

### Across documents

The semantic composer receives the selected occurrence set from each document and applies layer precedence.

Diagnostics must state whether a suppressed value came from:

- an earlier occurrence in the same document;
- a lower content layer;
- section inheritance/extensions from Ares/Phobos;
- a later map/mode overlay.

Do not flatten all causes into “duplicate key”.

## 6. Section/key case

Original comparison behavior is not inferred from modern Windows.

The composition policy supplies separate section and key comparers. It always preserves raw spelling.

Possible policies:

- ASCII case-insensitive candidate;
- byte/ordinal case-sensitive;
- document-family-specific comparer.

Case-only collisions are explicitly diagnosed. No default is promoted as original without evidence.

## 7. Empty values

A syntactically present empty value is not globally treated as deletion.

Candidate meanings include:

- override with empty string;
- typed default/reset;
- invalid/missing value;
- engine-extension deletion marker.

The lossless layer preserves it. Each typed/document composition policy decides later. Global deletion remains `Unresolved`.

## 8. Numbered-list sections

Keys such as `0=`, `1=`, and `2=` remain ordinary key identities at the semantic composition layer.

Default composition:

- a higher `1=` replaces lower `1=`;
- omitted lower numeric keys remain inherited;
- new numeric keys are added;
- gaps remain visible to the typed consumer.

Whether a particular Rules/AI/Sound list:

- stops at first gap;
- uses declared count;
- appends;
- clears on empty/default marker;
- requires reindexing;

belongs to that typed view. The generic composer never renumbers or concatenates list values.

## 9. Document families

The project can register composition for:

- `rules.ini` / `rulesmd.ini`;
- `art.ini` / `artmd.ini`;
- `sound.ini` / `soundmd.ini`;
- `ai.ini` / `aimd.ini`;
- UI and game-mode INIs;
- theater INIs;
- map-embedded overlays.

They may share section/key overlay mechanics while differing in:

- source layer set;
- post-processing;
- list interpretation;
- deletion/reset markers;
- map/mode ordering;
- required base document.

Do not assume one universal typed semantic policy merely because the parser is shared.

## 10. `rulesmd.ini` case

Configured low-to-high candidate layers include:

```text
ra2.mix candidate
ra2md.mix / explicit localmd child candidate
expandmd01 candidate
higher expandmd candidates
loose candidate
```

All located same-named documents participate. `expandmd01` is not a whole-file replacement, and `ra2md` is not merely a fallback.

Public evidence for exact vanilla cross-MIX automatic composition remains underconfirmed/conflicting; the behavior is nevertheless the frozen `ConfiguredProjectPolicy`.

## 11. `soundmd.ini` case

The same discovery/composition boundary applies. However sound definitions may contain numbered lists and specialized consumer behavior. The composer overlays key identities; the typed sound view owns list continuity and reference validation.

Again, no single document winner is produced.

## 12. Game-mode and map overlays

Community documentation shows game-mode INIs adding/overriding rules fields, which is evidence that semantic overlay mechanisms exist. Their relative position to base/expansion documents must be explicit:

```text
archive-layer composition
→ optional mode overlay
→ optional map embedded overlay
→ typed effective view
```

Exact order is document/profile policy and remains separately testable.

## 13. Ares and Phobos extensions

### Ares

Ares documents `[#include]`-style composition where later included values can update earlier values. This is extension behavior, not vanilla proof.

### Phobos

Phobos adds `$Include`, `$Inherits`, and typed reset/list facilities. Includes are ordered and can recursively merge documents. These operations require:

- cycle/depth/count budgets;
- explicit extension-enabled policy;
- provenance edges;
- no silent activation in vanilla profile.

Markers such as `<default>` must be interpreted only by the typed Phobos-aware policy that defines them.

## 14. Prohibited designs

- whole `rulesmd.ini` winner;
- treating lower same-named INIs only as fallback;
- dropping lower documents before the INI parser;
- byte concatenation;
- global empty-value deletion;
- global numeric-list append;
- SHA/length/path-based winner;
- INI resolver scanning the filesystem;
- typed view opening MIX streams directly.
