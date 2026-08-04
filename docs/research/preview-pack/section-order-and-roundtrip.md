# Preview section placement, physical order, and round-trip boundaries

> Source notice: researched by **ChatGPT Web** from public sources; no local `ProjectBaseline`; not a Codex Agent artifact; no code copied, translated, mechanically rewritten, or imported (`code_imported: false`).

## 1. Why physical order cannot be discarded

Most INI semantics are section-name and key based, but public tools make Preview-specific claims about physical placement. A lossless parser must therefore retain:

- physical section occurrence order;
- physical key order;
- duplicate section occurrences;
- comments and blank lines;
- raw key spelling;
- Base64 wrapping;
- relative placement of `[Preview]`, `[PreviewPack]`, `[Basic]`, `[Digest]`, and other sections.

Semantic lookup and physical serialization order are separate views.

## 2. WAE placement behavior

WAE's preview writer states that original TS and YR expect the preview sections to be first. It calls section-move operations so that:

```text
[Preview]
[PreviewPack]
...
```

appear at the beginning, with Preview before PreviewPack after the two move operations.

Its dummy-preview path applies the same placement.

This is a strong editor compatibility claim, but not official runtime source.

## 3. CNCMaps placement behavior

CNCMaps' thumbnail injector creates or updates `[Preview]` and creates `[PreviewPack]` relative to Preview. Its release discussion notes a change so generated PreviewPack is inserted after `[Basic]` rather than behind `[Digest]`.

This creates a genuine public-tool conflict with WAE's “first sections” policy. Both tools can produce usable maps in their target environments, so the difference cannot be resolved by counting tools.

## 4. EA official editor behavior

The released editor regenerates the two sections during save, but the inspected writer evidence does not by itself establish a universal game-runtime placement requirement. Its internal INI container and save ordering require separate research before claiming exact output order.

The official editor is evidence for generated data and metadata, not automatically for runtime section-order acceptance.

## 5. CnCNet fast reader

CnCNet's fast extractor scans the file linearly and recognizes Preview and PreviewPack wherever they occur. It appends PreviewPack values in physical encounter order until another section changes the active state.

This demonstrates that the client consumer does not require the sections to be first. It does not prove original executable behavior.

## 6. Ordering dimensions

Do not collapse these independent questions:

1. Where `[Preview]` occurs relative to the file start.
2. Where `[PreviewPack]` occurs relative to `[Preview]`.
3. Whether duplicate sections are adjacent.
4. Whether fragment keys are physically sorted.
5. Whether the consumer numerically sorts keys.
6. Whether `[Digest]` covers or depends on section placement.
7. Whether an editor rewrites section order on save.
8. Whether an original executable accepts arbitrary order.

## 7. Round-trip identities

### Lossless INI identity

Preserves exact text structure, section order, comments, key spelling, and fragment wrapping.

### Fragment identity

Preserves fragment count, keys, values, source order, and grouping even if the aggregate Base64 bytes are unchanged.

### Compressed identity

Preserves exact Base64-decoded chunk bytes, including chunk boundaries and compressor choices.

### Decoded identity

Preserves exact aggregate pixel bytes.

### Semantic pixel identity

Preserves the same interpreted components at the same pixel coordinates under the same channel and row profiles.

### Display identity

Preserves what a particular consumer renders after swaps, flips, scaling, crop, and interpolation.

These identities are not interchangeable.

## 8. Default save policy

A future default writer must not silently canonicalize Preview content. The recommended modes are explicit:

### `PreserveSource`

If unchanged, emit original section text and order byte-for-byte where the outer document model permits.

### `PreserveDecodedRecompress`

Retain decoded bytes and semantic profiles but intentionally rewrite chunks/Base64. This is not compressed or text identity.

### `CanonicalProfileRewrite`

Use explicit section placement, fragment wrapping, chunk size, channel order, and row order for a named target profile. This is never selected implicitly.

### `RegenerateFromRenderedMap`

An editor adapter creates a new image. It is generated content and has no source pixel identity.

## 9. Data that may need preservation

- original `Size` key/value spelling;
- every Preview section occurrence;
- physical section order;
- fragment key spelling;
- fragment source order;
- gaps and duplicates;
- line wrapping and whitespace;
- aggregate Base64 characters;
- compressed bytes;
- chunk headers and boundaries;
- decoded bytes;
- trailing compressed or decoded bytes;
- selected interpretation profiles;
- known placeholder identity;
- diagnostics and evidence grades.

## 10. Reordering hazards

A generic INI serializer that alphabetizes sections or keys can:

- move Preview after unrelated sections;
- reorder `1,10,2` lexically;
- collapse duplicate keys;
- merge duplicate sections;
- rewrite `01` to `1`;
- change Base64 wrapping;
- invalidate a digest or editor expectation.

Therefore Preview round-trip must use the lossless document, not a typed dictionary serializer.

## 11. Editor reopen versus runtime acceptance

Tests and audit reports must distinguish:

- this project parses the file;
- FinalAlert reopens it;
- WAE reopens it;
- CnCNet extracts a preview;
- original runtime lists the map;
- original runtime renders the preview;
- gameplay launches.

No compatibility status is raised by research alone.

## 12. Evidence status

- WAE first-section placement: `ConfirmedByIndependentImplementation`, with a community/runtime claim.
- CNCMaps after-Basic placement: `ConfirmedByIndependentImplementation` and fixed PPM release documentation.
- CnCNet location-independent scan: `ConfirmedByIndependentImplementation`.
- Original runtime section-order rule: `Unresolved`.
- Lossless preservation requirement: `ConfiguredForProjectPolicy`.