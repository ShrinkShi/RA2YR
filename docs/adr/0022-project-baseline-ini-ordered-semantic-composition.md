# ADR 0022: ProjectBaseline INI uses ordered multi-document semantic composition

## Status

Accepted as a configured `YR1001_ProjectBaseline` policy.

This ADR supersedes only the ProjectBaseline winner-ambiguity conclusions in
ADR 0015 and the separate-single-document ProjectBaseline audit choice in ADR
0016. Their generic fail-closed policy dimensions and typed-view boundaries
remain accepted.

## Context

The authoritative ProjectBaseline contains same-name INI documents in
`ra2md.mix` and numbered `expandmdNN.mix` archives. Treating the higher archive
as a whole-file replacement would discard lower-layer keys that are not
repeated. Treating the documents as ambiguous would also ignore the project
policy now frozen by the user.

Cross-document order does not answer the independent questions of section/key
case, duplicate physical sections, duplicate keys, inline semicolons,
whitespace, or empty-value deletion/override behavior.

## Decision

- The configured low-to-high order is `ra2 -> ra2md -> expandmd01 -> ... ->
  expandmd99 -> loose`.
- Same-name INI documents are composed semantically by `SectionName + KeyName`.
  A higher layer replaces the same value identity, inherits lower values it
  does not provide, and contributes new values.
- The load plan never selects a whole-file winner and never concatenates text.
- `expandmd01` through `expandmd99` are parsed generically as two decimal
  digits. Gaps are valid; a larger number has higher priority. Invalid or
  duplicate numbers and duplicate base/loose layers fail with structured
  diagnostics.
- Directory enumeration, candidate IDs, source IDs, and hashes do not decide
  order. The explicit `IniLoadPlan` priorities do.
- FinalAlert 2, reference material, manual extraction, caches, and tool
  temporary directories are excluded from candidate discovery. Only the
  configured `YR1001_ProjectBaseline` source may build this plan.
- Every resolved value retains the winner and all overridden candidates with
  layer ID, physical section/key line IDs, source ID, logical path, and full
  MIX provenance.
- The evidence level is `ConfiguredForProjectBaseline`. It is intentionally
  not `ConfirmedByOriginalRuntime` or `ConfirmedByProjectBaselineRuntime`.
- Name comparison, duplicate resolution, semicolon handling, whitespace, and
  empty-value semantics remain separately configured or unresolved.

### Adapter boundary and technical debt

`IniProjectBaselineLoadPlanBuilder` is a fixed adapter for controlled
`YR1001_ProjectBaseline` audits. It is not the future generic archive-discovery
service, runtime content index, or authority for mount precedence.

A future generic runtime must receive already established content topology and
ordering from the content system. Its INI path will consume:

- an explicit mount graph;
- pre-classified `ContentLayerDescriptor` values; and
- already sorted `LogicalDocumentLayer` values.

The generic INI resolver must not inspect a provenance root MIX filename and
infer that archive's layer permission or priority. The current filename
classification remains isolated to the ProjectBaseline audit adapter and must
not become a reusable runtime-discovery shortcut.

## Evidence

The fixed ProjectBaseline runtime audit orders both `rulesmd.ini` and
`soundmd.ini` as `ra2md` priority 200 followed by `expandmd01` priority 301.
The configured G2 Rules audit resolves 22,720 value identities and preserves
22,709 cross-layer override chains. All 22,720 current winners happen to be in
the expand layer because that payload repeats every lower-layer identity and
adds eleven; this is a corpus observation, not whole-file selection.

The composed minimal Rules view still contains five registries and 1,171
entries. It remains `Incomplete` because Opaque, inline-semicolon, invalid
identifier, and duplicate-identifier diagnostics are intentionally preserved.

## Consequences

ProjectBaseline `rulesmd.ini` and `soundmd.ini` documents are composition
layers, not ambiguous documents and not fallback-only files. G2 may consume a
complete composed resolution when every independent intradocument policy is
explicit; its result must identify those policies as configured testing until
stock runtime evidence exists.

Original-runtime comparison remains unimplemented. A future black-box study
may confirm or reject the configured order and must independently test the
remaining intradocument semantics.

Replacing the audit adapter with mount-graph-driven runtime inputs is tracked
technical debt. That replacement must preserve each winning occurrence, the
complete overridden-candidate chain, document layer, physical line, `SourceId`,
logical path, and MIX provenance.

## Rejected alternatives

- Select the highest archive as a whole-file winner.
- Drop `ra2md` after discovering an expand archive.
- Keep lower documents only as file-lookup fallback.
- Concatenate INI text before parsing.
- Hard-code current file hashes, logical names, or candidate enumeration order
  as precedence.
