# RA2/YR INI runtime-resolution plans

## Scope

WP-02G1 adds an evidence-gated semantic layer above the immutable WP-02F INI
document. It models candidates and can execute a caller-supplied policy, but it
does not define the stock YR policy. No Rules, Art, AI, theater, UI, sound,
mission, or map-override fields are interpreted.

## Independent policy dimensions

| Dimension | Explicit choices implemented | Stock YR status |
|---|---|---|
| Container layer | integer priority per declared layer | ProjectBaseline order configured; original runtime unconfirmed |
| Same-name files | select highest document; overlay low to high | ProjectBaseline uses semantic overlay; original runtime unconfirmed |
| Section/key names | raw ASCII ordinal; ASCII ordinal-ignore-case | Unresolved |
| Duplicate sections | first; last; merge in physical order | Unresolved |
| Duplicate keys | first; last | Unresolved |
| Inline semicolon | preserve; first semicolon starts comment | Unresolved |
| Value whitespace | preserve; trim ASCII space/tab | Unresolved |
| Empty value | overrides; does not override | Unresolved |

Every selected choice carries an evidence level and reference ID. An
`Unresolved` choice remains an error-producing ambiguity, rather than falling
back to a default. Culture-sensitive comparison, current Windows code pages,
and `Encoding.Default` are not used.

## Load-plan and provenance model

An `IniLoadPlan` owns declared `IniLoadLayer` objects. Each layer includes:

- stable layer and source IDs;
- directory, expand, ecache, elocal, base, nested, test, or other kind;
- the complete logical chain from the mounted source through every MIX layer;
- an optional explicit priority; and
- evidence for that priority.

The constructor accepts only a pre-materialized trusted `IReadOnlyList` and
copies it. `MaxLayers` is a resolver bound over that list; it does not claim to
bound construction of an arbitrary lazy sequence. Candidate documents use a
separate bounded materializer: it reads at most `MaxDocuments + 1` items, uses
the extra item only to prove overflow, returns `DocumentBudgetExceeded`, and
does not touch later elements or invent them in the trace.

Candidate documents separately bind a logical INI name and immutable
`IniRawDocument` to a layer. The resolver validates every binding before it
examines values. It sorts by explicit priority and stable physical facts.
Input enumeration, dictionary order, thread order, current culture, and source
ID cannot decide an equal-priority conflict.

Layer and provenance source IDs are identities, so their comparison is exact
`Ordinal`; a case-only difference is `IncompleteProvenance`. This is separate
from the explicit section/key name comparison policy.

For syntax bytes, raw single-byte and UTF-8 BOM documents retain their existing
ASCII-byte behavior. UTF-16LE recognizes ASCII only as `XX 00`; UTF-16BE only
as `00 XX`. Name decoding, semicolon policy, ASCII space/tab trimming, and the
sanitized syntax auditor all use this same rule. Thus U+3B00 cannot masquerade
as U+003B, and U+2000 cannot masquerade as U+0020.

A completed value trace retains the winning candidate and all considered
candidates with dispositions including duplicate suppression, empty-value
suppression, cross-file override, and ambiguity. Each candidate retains the
layer ID, physical section/key line IDs, and its document's source/MIX chain.

## Configured ProjectBaseline composition

The frozen `YR1001_ProjectBaseline` plan is low-to-high:

```text
ra2 -> ra2md -> expandmd01 -> ... -> expandmd99 -> loose
```

`expandmdNN.mix` uses exactly two digits from `01` through `99`. Gaps are
valid and the numeric value defines priority, independent of directory
enumeration. Invalid numbers, duplicate numbers, duplicate base/loose layers,
and non-ProjectBaseline sources produce structured errors.

Same-name documents use ordered multi-document semantic composition. Values
are grouped by the explicit section/key comparison policy; a higher-layer
value wins the same identity, lower-only values are inherited, and higher-only
values are added. The implementation does not choose one whole file, discard
`ra2md` after finding an expand archive, or concatenate text.

This order is labelled `ConfiguredForProjectBaseline`. It is not an original
runtime claim. Name comparison, duplicate sections/keys, inline semicolons,
whitespace, and empty values remain independent evidence-gated dimensions.

## Evidence review

- FinalAlert 2 commit `6abf0f557469baea73079c6bf6550709e2e3584e`
  provides official editor-source evidence: its search code scans numbered
  ecache/expand archives in a declared order, and its INI map strips at a
  semicolon, trims views, merges repeated sections, and overwrites repeated
  keys. This is not proof of game-runtime behavior.
- OpenRA commit `a520984d91eda9de48a62b1d15c1e3bad0d4fb1a`
  independently strips inline semicolons and trims, but lowercases section
  names and preserves the first duplicate key. The disagreement prevents a
  stock-runtime conclusion.
- Chrono Divide SDK commit
  `5943c4ae6c19897929d348a417d6d2f1481b75fd` documents an independent archive
  order. Ares `7e2a509b731efb3a523d64a6933f2fde01903623`
  and Phobos `c5355981cfe15bab929bb440379ae0b12cee62f4`
  document extension-specific include behavior. None is stock-runtime proof.
- The external 2025 Chinese RA2 tutorial bundle documents community practice
  around case, comments, duplicates, and standard syntax. Its conclusions are
  recorded only as `CommunityDocumented`.

All source code is reference-only. No GPL source, translation, or mechanical
rewrite was imported. Exact source and license records are in
`docs/third-party/sources.yml`.

## ProjectBaseline observation

The existing bounded MIX source finds two distinct candidates for each of:

| Logical name | Candidate chain | Bytes | SHA-256 |
|---|---|---:|---|
| `rulesmd.ini` | `expandmd01.mix -> rulesmd.ini` | 743215 | `3D341EF8A13A4B5AB24AF2EEF48AC94931AC2BB87D950FE3330A07E2D25672EF` |
| `rulesmd.ini` | `ra2md.mix -> localmd.mix -> rulesmd.ini` | 742958 | `06761DD7F714E7D9400216EC3C06109EC5C1461F6A0727BE7401EB9D8B0F6D05` |
| `soundmd.ini` | `expandmd01.mix -> soundmd.ini` | 99392 | `0A8E85381AEF1A0F97074C953BFE99504DA00C6220FAE1A023A1AFD857023232` |
| `soundmd.ini` | `ra2md.mix -> localmd.mix -> soundmd.ini` | 99292 | `D1BE76491A0888396B4D0E53F4857F33879A5AFD40A8BCEA65EA1D1A3096D419` |

Both pairs are configured composition layers: `ra2md` priority 200 followed by
`expandmd01` priority 301. No whole-file winner exists. Discovery also
located `rules.ini`, `art.ini`, `aimd.ini`, `temperat.ini`, `snow.ini`,
`urban.ini`, `uimd.ini`, `evamd.ini`, and `missionmd.ini` through mounted MIX
sources. `desert.ini`, `lunar.ini`, and `urbann.ini` were not located in the
currently mounted directory/MIX sources; this does not prove absence elsewhere.

## Opaque and semicolon audit

The public audit records only aggregates, never original text or per-line
hashes.

| Sample | Opaque | Before / inside / after | Contains `=` | Known punctuation | Inline `;` start / middle / end |
|---|---:|---:|---:|---:|---:|
| `artmd.ini` | 1210 | 282 / 27 / 901 | 1054 | 154 | 0 / 252 / 0 |
| `ai.ini` | 0 | 0 / 0 / 0 | 0 | 0 | 0 / 0 / 0 |
| `rulesmd.ini` expand | 735 | 0 / 38 / 697 | 669 | 61 | 9 / 2423 / 16 |
| `rulesmd.ini` local | 735 | 0 / 38 / 697 | 669 | 61 | 9 / 2419 / 16 |

The conservative reasons are `KeyOutsideSection`, `MissingEquals`, and
`SectionTrailingContent`. They are preservation categories, not claims that
the stock runtime ignores the lines. Every nonzero sample can affect a future
minimum typed view.

## Black-box validation plan

The configured ProjectBaseline policy does not require a black-box experiment
to execute. Original-runtime confirmation and the independent intradocument
policies still require separate authorization. That experiment must:

1. clone the baseline into an isolated, repository-external disposable tree;
2. hash and lock the authoritative baseline before any operation;
3. create paired synthetic differences in copies of competing containers,
   changing one policy dimension at a time;
4. run the original executable only against that disposable tree;
5. capture an objective behavior, screen, or log observation and all hashes;
6. swap A/B placement and repeat to distinguish content from order;
7. repeat for container order, file composition, duplicates, case, whitespace,
   empty values, and inline semicolons; and
8. prove authoritative baseline hashes and attributes remain unchanged.

Until separately authorized experiments produce reproducible evidence, the
configured composition must not be labelled stock-original runtime behavior,
and no unresolved intradocument parsing rule may be promoted.
