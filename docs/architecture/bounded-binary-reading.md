# Bounded binary reading foundation

WP-02B provides a format-neutral binary input boundary in `RA2YR.Core.Binary`.
It does not identify or decode any YR file format. It provides the mandatory
bounded operations and accounting hooks that later Core parsers must use for
untrusted lengths, offsets, counts, and stream behavior.

## Input model

A `BinaryReadSession` owns one explicitly bounded input and one shared budget
ledger. `ReadOnlyMemory<byte>` supplies its length directly. A non-seekable
`Stream` requires the caller to provide both the bounded length and logical
absolute origin; the implementation never queries its `Position` or `Length`.
The seekable factory may infer the remaining bound from those properties and
reports an explicit diagnostic when they are unsupported or fail.

Session, reader, tail policy, and completion types are internal to
`RA2YR.Core`; later public format parsers expose their own validated results.
The session is synchronously confined to its creating thread, and foreign
thread mutation fails before changing position or budget state. A caller that
provides `ReadOnlyMemory<byte>` must keep its backing storage unchanged for the
session lifetime.

Stream input is copied by a short-read loop into a stable snapshot. The
declared bound is validated against both the input and cumulative allocation
budgets before conversion or allocation. Positive short reads continue, a
zero read is EOF, and I/O failure messages are not copied into public
diagnostics. `leaveOpen` determines whether disposing the session closes the
caller's stream. Once the seekable factory delegates ownership to the bounded
snapshot factory, only that factory handles a failure, so an owned Stream is
disposed exactly once.

Readers expose relative position, logical absolute offset, bound length,
remaining length, exact reads, skips, non-advancing peeks, and child ranges.
A child is a view into the stable snapshot and cannot exceed its parent. All
children share the session ledger, so nesting cannot reset allocations,
records, string length, depth, or child-count budgets.

## Resource budgets

Every session has finite limits for:

- input bytes;
- one read operation;
- cumulative allocated bytes, including a Stream snapshot;
- cumulative record reservations;
- one declared string length;
- child-range nesting depth;
- cumulative child-range count.

File-driven values remain `long` until they have passed sign, range, parent,
and budget checks. Range endpoints and cumulative counters use checked
arithmetic. Conversion to the array/index representation occurs only after
validation. A budget failure has its own diagnostic and is never reported as
ordinary EOF.

Record and string limits are charged through explicit Core parser operations.
Every concrete parser must reserve a record before iterating it and validate a
declared string length before decoding or allocating it; bypassing these calls
is not an accepted parser implementation.

## Diagnostics and state

A diagnostic records the parser logical name, logical source ID,
`LogicalContentPath`, exact logical byte offset, requested length, remaining
length, field or section label, severity, and code. Parser names, source IDs,
and field labels reject path separators and control characters; physical
paths, input bytes, and original `IOException.Message` text are never copied
to the diagnostic.

The session exposes one read-only diagnostic view instead of copying the full
history on every completion. A child completion contains only a diagnostic
created by that completion; the finalized root exposes the sealed session
history. This keeps repeated child completions linear in their diagnostic
count rather than quadratic.

A failed read poisons the session, preventing callers from catching an error
and repeatedly reading damaged input. `BinaryParseCompletion` has no public
surface. It proves only that one bounded range used an explicit consumption or
tail policy; a concrete format result must additionally be created by an
internal parser factory after the root range completes. A parent cannot
complete while any delegated child is incomplete, and a completed reader/root
session rejects further mutation. Tail decisions are:

- require full consumption, otherwise return a trailing-data error;
- allow and consume a tail while recording a warning;
- copy and preserve the opaque tail within the allocation budget;
- defer the decision to a format parser without returning complete status.

## Determinism and scope

Unsigned values are assembled explicitly in little-endian order; signed
values use the corresponding two's-complement bit pattern. No operation uses
host byte order, Unity frame state, a scene, or `UnityEngine`.

The current Stream implementation intentionally snapshots the entire declared
bound, so inputs larger than the configured allocation budget are rejected.
A future incremental backing store may extend this implementation, but it
must preserve the same offsets, budgets, diagnostics, and tail semantics.
MIX, Blowfish, PAL, SHP, VXL/HVA, TMP, CSF, INI, map Pack, rendering, maps,
units, and gameplay remain unimplemented.
