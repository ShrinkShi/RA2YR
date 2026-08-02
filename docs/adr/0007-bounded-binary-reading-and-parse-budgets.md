# ADR 0007: Bounded binary reading and parse budgets

- Status: Accepted
- Date: 2026-08-02

## Context

Future YR format parsers will consume untrusted offsets, sizes, counts, and
streams. A parser that allocates directly from a declared length, assumes one
`Stream.Read` fills a buffer, or silently ignores a tail can crash, disclose
host details, or claim compatibility after incomplete parsing.

## Decision

All binary format work starts from one `BinaryReadSession` in the Unity-free
Core assembly. The session has an explicit input bound, logical source
context, stable input snapshot, and one shared finite budget ledger. Memory,
seekable Stream, and explicitly bounded non-seekable Stream inputs receive the
same reader semantics. Little-endian integers are assembled explicitly.

Session, reader, tail-policy, and range-completion types remain internal to the
Core assembly. They are synchronously confined to their creating thread. Later
public format APIs may expose immutable limits and diagnostics, but only their
internal parser factories may construct a successful format result.

Input, read, allocation, record, string, nesting, and subrange limits are
checked before conversion or allocation. Child ranges share the parent
ledger. Any read error is structured and poisons the session. A parse can be
reported complete only through an internally constructed completion after an
explicit trailing-data policy. Child completions retain only their local
completion diagnostic, while the finalized root reuses the sealed read-only
session history; repeated child warnings therefore cannot cause quadratic
diagnostic-history copying.

## Consequences

- Malformed lengths and offsets cannot become arbitrary allocations or raw
  `EndOfStreamException`/`OverflowException` failures.
- Non-seekable inputs remain deterministic because the caller supplies their
  finite logical extent and origin.
- Public diagnostics identify logical input and field context without copying
  physical paths, input bodies, or raw I/O messages.
- Stream snapshots consume allocation budget and therefore impose a finite
  current input-size limit.
- Owned Stream factories transfer failure-cleanup responsibility exactly once;
  `leaveOpen` remains authoritative on both success and failure.
- Passing these synthetic foundation tests does not promote any concrete YR
  format or original-comparison status.
- `ReadOnlyMemory<byte>` callers must keep the backing storage unchanged for a
  session's lifetime; Stream callers receive a stable bounded snapshot.

## Alternatives rejected

- Use `BinaryReader` directly: it does not provide the required shared budgets,
  child bounds, diagnostic vocabulary, or completion proof.
- Trust format-specific parsers to apply their own limits: duplicated ledgers
  can be reset by nesting and are difficult to audit consistently.
- Treat EOF and every resource limit as one failure: it hides whether input is
  truncated or deliberately exceeds policy.
- Ignore unknown trailing bytes: incompatible with lossless investigation and
  future round-trip requirements.
