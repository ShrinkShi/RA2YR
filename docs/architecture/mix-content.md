# MIX content architecture

WP-02C adds MIX as a bounded virtual content source without extracting game
files into `Assets`. The implementation remains in `RA2YR.Core` and has no
`UnityEngine`, scene, frame-loop, or rendering dependency.

## Dependency and ownership boundary

The dependency direction is:

```text
external directory source
  -> seekable file window session
    -> MIX structural reader
      -> numeric entry catalog
        -> optional candidate-name catalog
          -> bounded virtual mount
            -> explicit layer precedence and provenance
```

The structural reader knows only IDs, offsets, lengths, flags, checksum state,
and observed entry order. Name resolution is a separate catalog operation;
unknown IDs remain accessible as numeric IDs and never receive invented names.
Directory-source priority and same-source MIX layer priority are both explicit.
Source IDs are identities, not hidden tiebreakers.

Opening a large archive reads only its header and directory. Payload access
uses a parent-bounded window over the same seekable handle. A child MIX is
mounted directly from its parent entry window, so it cannot seek outside that
entry and is not copied into a repository or Unity asset directory.

## Shared limits and lifecycle

One mount operation shares limits for bytes read, child windows, allocations,
records, mounted archives, total entries, and nesting depth. The mount rejects
duplicate IDs, overlapping ranges, repeated physical ranges in an ancestry,
depth overflow, archive-count overflow, and entry-count overflow. Disposing the
root mount closes owned handles and invalidates every child window.

The file-window implementation serializes seek/read operations on the owned
stream. A window and all descendants are valid only for that session lifetime;
callers must not retain payload handles after the mount is disposed. This is a
bounded read facility, not a general asynchronous I/O framework.

## Writer boundary

The writer always creates a complete new archive. It supports stable ID sorting
for `DeterministicRebuild` and observed order for `PreserveEntryOrder`. It uses
checked layout arithmetic, explicit size/entry limits, an approved external
cache or test root, a temporary file, flush, bounded reread verification, and
atomic publication. ProjectBaseline paths are rejected as output roots.

Encrypted writing requires an explicit 80-byte key source. This proves reuse
of supplied Westwood key material, not generation of a new key source.
Checksum emission calculates a new SHA-1 over the payload region. A nested
child archive can be independently built as bytes for a parent entry; automatic
rewriting of an entire mounted tree is outside WP-02C.

## Public evidence boundary

Complete baseline audit manifests, generated archives, extracted payloads,
XCC tool copies, and GUI state records stay in the configured repository-
external cache. Repository evidence contains only logical paths, IDs, sizes,
hashes, aggregate counts, sanitized diagnostics, and controlled operation
results. It contains neither absolute paths nor source-game payload bodies.

The patched `YR1001_ProjectBaseline` proves that the implementation can inspect
the configured development content. It is not a clean YR 1.001 original
comparison. XCC-openable, semantic entry equality, payload equality, and byte-
identical archives are recorded as separate outcomes.
