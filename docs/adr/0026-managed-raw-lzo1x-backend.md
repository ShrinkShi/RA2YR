# ADR 0026: Managed RawLzo1X decoding remains bounded and evidence-gated

## Status

Accepted for M3-C4 managed implementation and sanitized external audit.

## Decision

Add one independently authored, Unity-free managed `RawLzo1X` decoder behind
the existing `ILzoDecodeBackend` contract. The backend accepts only
`PackedCodecKind.RawLzo1X`, consumes a bounded compressed window, requires an
exact declared output length, checks output and input budgets, supports
cancellation, and returns consumed/produced lengths, identity, provenance, and
structured diagnostics. Literal, short/medium/long match, overlap, and terminal
marker behavior are implemented without fallback or partial success.

The backend is composed with the existing fragment/Base64/chunk pipeline and
the raw IsoMapPack5 reader. The ProjectBaseline command may run against the
configured external patched development source, but it emits only sanitized
aggregate counts, diagnostic categories, source fingerprints, and canonical
aggregate hashes. A `CompleteWithFailures` audit remains a failure-bearing
observation and is not promoted to original-runtime compatibility.

## Consequences

No miniLZO/GPL source, native plugin, P/Invoke binding, NuGet dependency,
writer, map-specific semantic reader, Preview/TMP/Overlay integration,
palette, renderer, or gameplay code is added. The backend identity and audit
profile are explicit, so future implementations can be compared without
silently changing codec policy.

## Evidence boundary

Synthetic tests establish decoder and pipeline contracts. The external audit is
performed only on the operator-provided patched development source outside the
repository. Original runtime behavior, clean YR 1.001 compatibility, tile
meaning, coordinate semantics, and visual map loading remain unconfirmed.
