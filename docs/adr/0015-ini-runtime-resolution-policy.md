# ADR 0015: Evidence-gated INI runtime resolution

- Status: Accepted; ProjectBaseline ordering and composition amended by ADR 0022
- Date: 2026-08-03

## Context

WP-02F preserves INI bytes and physical structure, but deliberately does not
answer which archive wins, whether same-name files replace or overlay one
another, or how duplicate sections, duplicate keys, case, whitespace, empty
values, and inline semicolons behave at runtime. These are separate decisions.

Static evidence did not establish one stock YR answer. FinalAlert 2 describes
editor behavior, OpenRA and Chrono Divide are independent implementations,
Ares and Phobos describe opt-in extensions, and community tutorials are
secondary documentation. The two ProjectBaseline `rulesmd.ini` documents and
two `soundmd.ini` documents are different files. ADR 0022 subsequently freezes
their project policy as ordered multi-document semantic composition without
claiming an original-runtime observation.

## Decision

- Container precedence, file composition, name comparison, duplicate-section
  resolution, duplicate-key resolution, inline-comment handling, whitespace,
  and empty-value handling are independent policy dimensions.
- `IniLoadPlan` contains explicit ordered layers. Each layer retains source ID,
  layer kind, complete logical container chain, optional priority, and the
  evidence supporting that priority.
- `IniLoadPlan` accepts an already-materialized trusted `IReadOnlyList`. Its
  `MaxLayers` check bounds resolver consumption, not arbitrary producer-side
  enumeration or allocation. Candidate documents remain an `IEnumerable`, but
  the resolver reads at most `MaxDocuments + 1` items and stops immediately
  after the extra item proves the budget was exceeded.
- Missing priority, equal highest priority, incomplete provenance, or an
  `Unresolved` policy returns `Ambiguous` or `Failed`; source IDs and input
  enumeration order never break a tie.
- Source IDs are exact identities and therefore compare with `Ordinal`, while
  logical INI name comparison remains a separately selected policy.
- Explicit `ConfiguredForTesting` policies are executable for synthetic work.
  They are not stock-runtime defaults.
- Resolution reads the immutable WP-02F document without changing its bytes or
  physical nodes. Opaque nodes are diagnosed and are never silently executed.
- Every completed value retains its selected candidate and the ordered chain of
  suppressed, overridden, or otherwise considered candidates back to physical
  section and key line IDs and complete document/container provenance.
- Resolution results and traces have controlled construction. A caller cannot
  publicly manufacture a complete result without the resolver.
- Runtime names are restricted to the current explicit raw-ASCII boundary.
  Raw ordinal and culture-invariant ASCII case folding are separate policies;
  host culture and `Encoding.Default` are forbidden.
- ASCII syntax recognition is physical-encoding-aware. UTF-16LE accepts only
  `XX 00` and UTF-16BE accepts only `00 XX` for ASCII code units; reversed
  lookalikes are neither delimiters nor ASCII whitespace.
- Each conclusion records one evidence level: original runtime, ProjectBaseline
  runtime, official editor source, independent implementation, community
  documentation, configured testing, or unresolved.
- Only original-runtime and ProjectBaseline-runtime evidence can establish an
  original/runtime comparison.

## Consequences

The generic resolver and per-value trace are usable when a caller supplies an
explicit, evidence-labelled plan. They cannot silently turn editor, community,
or independent-engine behavior into stock YR behavior.

`rulesmd.ini` and `soundmd.ini` are no longer ambiguous documents under the
configured `YR1001_ProjectBaseline` policy: their layers compose low-to-high by
section/key identity. Original-runtime comparison and intradocument name,
duplicate, semicolon, whitespace, and empty-value semantics remain unresolved.
Typed AI, theater, UI, sound, mission, and map-override views remain
unimplemented; minimal Rules/Art views remain incomplete where preserved
Opaque or unresolved syntax can affect them.

A controlled original-runtime black-box experiment is required to determine
the unresolved stock policies. It must use a disposable copy, fixed hashes,
A/B permutations, observable outcomes, and no writes to the authoritative
ProjectBaseline. Starting the game or creating a test MOD requires separate
user authorization and was not performed in WP-02G1.

## Alternatives

- One global `LastWins`: rejected because it conflates independent layers of
  behavior and is not supported by the available evidence.
- Use FinalAlert 2 as the runtime oracle: rejected because it is an editor and
  its map-based parser is lossy.
- Adopt OpenRA, Chrono Divide, Ares, Phobos, or community behavior as stock YR:
  rejected because those are implementations, extensions, or documentation.
- Pick one whole document using archive number or source ID: rejected because
  ProjectBaseline uses explicit ordered semantic composition, not whole-file
  replacement. Archive ordering must remain visible in `IniLoadPlan`.
- Ignore Opaque lines: rejected because they can affect later semantic views.
