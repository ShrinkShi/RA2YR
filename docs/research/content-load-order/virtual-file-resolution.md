# Virtual file resolution

## 1. Candidate model

Every located logical file is represented as a `ContentCandidate`:

```text
- LogicalNameRaw
- LogicalNameNormalized
- ProviderIdentity
- ProviderKind
- ArchiveFamily
- SequenceNumber?
- ParentMountChain[]
- EntryIdentity or loose identity
- ContentPriorityKey
- Length
- SourceSha256?        // provenance only, never priority
- EvidenceLevel
- Diagnostics[]
```

The same logical name may have many candidates.

## 2. Two resolution modes

### Whole-file resolution

Used for ordinary binary resources unless a specific document policy says otherwise.

- sort candidates deterministically low-to-high;
- highest valid candidate is the winner;
- retain all lower candidates as suppressed;
- return one stream/window plus a complete trace;
- do not merge bytes.

### Ordered-document resolution

Used for configured composable INIs.

- sort all valid same-named documents low-to-high;
- return every layer;
- open each as a bounded lossless document;
- do not choose a whole-file winner;
- pass layers to the semantic INI composer.

These modes are explicit API choices. The generic resolver must not guess from file extension alone without a registered logical-document policy.

## 3. Resolution trace

```text
ContentResolutionTrace
- LogicalName
- ResolutionMode
- PolicyProfileId
- OrderedCandidates[]
- Winner?                  // only whole-file mode
- OrderedDocumentLayers[]  // only composition mode
- RejectedCandidates[]
- Diagnostics[]
- CanonicalTraceSha256
```

For INIs, `Winner` is absent by contract.

## 4. Candidate ordering

Ordering compares only explicit stable fields:

1. provider scope;
2. archive-family rank;
3. sequence number where the family defines one;
4. explicit nested mount ordinal/role;
5. configured provider ordinal;
6. deterministic logical-name tie-breakers used only for reporting, never to resolve ambiguous duplicates.

Input enumeration index, timestamp, file size, SHA, and physical path are excluded.

## 5. Generic binary example

Suppose `unit.vxl` exists in `ra2.mix`, `ra2md.mix`, `expandmd01`, and loose.

Whole-file resolution returns loose as winner and retains the three lower candidates in ascending precedence. It does not combine VXL sections across files.

## 6. INI example

Suppose `rulesmd.ini` exists in multiple layers. Ordered-document resolution returns all layers:

```text
ra2 candidate document
ra2md candidate document
expandmd01 candidate document
...
loose candidate document
```

The INI composer determines effective section/key values. Content discovery never removes the lower documents.

## 7. Filename identity and MIX hashing

Westwood MIX directories may identify entries by hashed IDs rather than stored names. Filename-to-ID behavior belongs to archive lookup, while layer ordering belongs to the virtual content system.

The model retains:

- requested logical name;
- hash algorithm/profile used;
- matched archive entry ID;
- archive provenance;
- collision diagnostics.

A hash collision is not resolved by provider priority inside one archive. It is an archive-level ambiguity/failure.

## 8. Provider scopes

Required provider kinds include:

- `LegacyMix`
- `LooseDevelopment`
- `UserMod`
- `ModernAssetPackage`
- `GeneratedOrCached` only when explicitly enabled

Each provider declares logical scope. A modern model package must not unexpectedly override legacy INIs unless its descriptor explicitly permits that scope.

## 9. Query budgets

Bound:

- providers considered;
- archives mounted;
- candidates per logical name;
- nested mount depth;
- trace records;
- diagnostics;
- bytes opened;
- repeated mount identities.

When a budget is exceeded, fail with a partial trace; never drop lower candidates silently.

## 10. Determinism

Canonical resolution hashes must remain identical under:

- randomized filesystem enumeration;
- randomized archive-entry enumeration;
- Memory, Stream, and MIX-window access;
- dictionary insertion-order changes;
- repeated equivalent discovery runs.

The synthetic oracle must implement expected sorting independently from the production sorter.
