# ADR 0016: Minimal Rules and Art resource views are explicit and fail closed

## Status

Accepted for WP-02G2; ProjectBaseline input composition amended by ADR 0022.

## Context

WP-02F preserves INI bytes and WP-02G1 can resolve candidates under an
explicit, evidence-labelled policy. Neither layer proves stock YR archive
precedence, defaults, Rules object semantics, Art fallbacks, or resource-file
selection. A first typed layer is nevertheless needed to choose later SHP and
VXL research samples without discarding the physical and resolution evidence.

## Decision

- A typed view accepts only `IniResolutionStatus.Complete`. `Ambiguous` and
  `Failed` inputs return no typed document and preserve the input trace and
  diagnostics.
- Scalar parsers are explicit: raw bytes, bounded ASCII identifier, named
  yes/no policy, bounded non-negative decimal integer, and comma-separated
  ASCII identifiers. They do not use the current culture, `Encoding.Default`,
  defaults, spelling repair, or extra trimming.
- The minimal Rules view reads only the five explicit type registries needed
  for resource discovery. It preserves ordinal spelling, entry order, value
  candidate chains, physical line IDs, and MIX provenance.
- The minimal Art view reads only ten explicitly named resource-routing
  fields. Missing and invalid values remain distinct. It does not use the
  section name as `Image`, append an extension, select a palette, infer a
  shadow frame, or create a runtime object.
- Opaque lines, unresolved inline semicolons, or duplicate policies that may
  affect a typed target make the result `Incomplete`. They are never silently
  ignored to produce a `Complete` claim.
- ProjectBaseline `rulesmd.ini` documents are composed low-to-high by
  `SectionName + KeyName` under `ConfiguredForProjectBaseline`. The typed audit
  still uses explicit `ConfiguredForTesting` policies for unresolved
  intradocument name, duplicate, semicolon, whitespace, and empty-value
  semantics. It performs neither text concatenation nor whole-file selection.

## Consequences

The engine can generate deterministic, source-tracked resource-reference
aggregates suitable for planning later format work. It still cannot claim
complete Rules or Art support, stock precedence, default/fallback behavior,
SHP/VXL support, visual compatibility, or gameplay behavior.

## Rejected alternatives

- Consume ambiguous input and choose a candidate inside the typed layer.
- Build dictionaries that discard duplicate candidates and physical lines.
- Treat the Art section name as an implicit image or synthesize extensions.
- Treat community, editor, or independent-engine behavior as stock runtime
  proof.
