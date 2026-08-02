# ADR 0010: XCC zero-flag extended headers and semantic round trips

- Status: Accepted
- Date: 2026-08-02

## Context

The controlled XCC Mixer 1.47 writes a four-byte extended flags word even when
the value is zero. The resulting empty extended archive is ten zero bytes. A
classic empty archive is six zero bytes, so the normal nonzero flag marker is
insufficient for this XCC output. XCC also inserts `local mix database.dat`
when creating an archive and ignores a zero-byte file dropped into its create
workflow.

## Decision

The reader treats exactly six zero bytes as the canonical classic empty form.
For a zero first word, a total length of at least ten bytes selects the XCC
extended-header interpretation; lengths seven through nine remain malformed
classic input with trailing data. This length policy is explicit and tested.
The writer is allowed to emit an extended header with zero flags.

The XCC-create interop contract uses three nonempty autonomous payloads and
accepts exactly one optional XCC-generated `local mix database.dat`. The
observed entry order, including that database, is preserved during
`PreserveEntryOrder` rebuilding. Unknown extra entries and duplicate database
entries fail closed.

Round-trip evidence is semantic unless byte equality is measured separately.
For the controlled XCC-created archive, the project preserved entry order,
IDs, lengths, and payload hashes, but the rebuilt archive was not byte-identical.
That outcome is a passed semantic round trip and a failed byte-identity check,
not a claim of byte-for-byte reproduction.

## Consequences

- XCC zero-flag output is readable and independently writable.
- The unavoidable zero-count classic/extended ambiguity has a deterministic,
  documented policy.
- Project writer support for zero-byte entries remains valid even though the
  XCC GUI create workflow ignores zero-byte input files; XCC extraction of
  project-generated zero-byte entries is tested separately.
- No XCC source or executable enters the repository.
