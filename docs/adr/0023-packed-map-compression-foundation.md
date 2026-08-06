# ADR 0023: Packed map compression foundation is codec-neutral and evidence-gated

## Status

Accepted for M3-C1 synthetic/configured implementation.

## Decision

The packed-map foundation is layered as lossless INI occurrence collection,
strict Base64, bounded Westwood chunk envelopes, an explicitly selected codec,
and exact decoded bytes.  No layer selects a codec from section names or data
shape, and no layer creates map, overlay, preview, coordinate, palette, or
Unity objects.

Format80 exposes explicit absolute/relative profiles and strict terminator,
input, output, reference, and budget contracts.  LZO is represented only by an
injectable backend contract; no LZO algorithm, native library, P/Invoke, GPL
source, binary dependency, or ProjectBaseline packed audit is included.

## Evidence and limits

The implementation is synthetic/configured only.  The public map-compression
research identifies command families and envelope candidates but does not prove
stock runtime precedence, position interpretation, or compressor details.
ProjectBaseline packed and decoded content is intentionally not read in M3-C1.

## Consequences

Future IsoMap, OverlayPack, PreviewPack, TMP and map-specific readers must
consume exact stage results and provide their own evidence-gated policies.
Malformed or incomplete stages fail closed; partial bytes are not successful
pipeline output.

The current worktree also records a host-level verification limitation: Unity
batch test launch could not produce a current XML because the desktop
PowerShell process launcher rejects the environment's duplicate `PATH`/`Path`
keys. Unity Core and EditMode assemblies nevertheless compile through the
Unity-provided Roslyn compiler with exit code 0. This does not promote the
historical focused XML to current-head evidence.
