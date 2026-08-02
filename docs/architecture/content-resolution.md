# Logical content resolution

WP-02A resolves logical files across directory sources without depending on
Unity, the host culture, file enumeration order, thread scheduling, or
dictionary iteration order.

WP-02B additionally makes `ContentResolutionSource.TotalBytes` an explicit
checked accumulation. A source whose synthetic metadata cannot fit in Int64 is
omitted from candidates, reported with `SourceTotalBytesOverflow`, and makes
the result incomplete. It cannot leak an unexplained `OverflowException` or be
serialized as a complete manifest. This does not change priority or ambiguity
semantics.

## Logical path identity

- `/` is the only logical separator.
- Paths are relative and contain no empty, `.`, or `..` segments.
- NUL, invalid UTF-16, control characters, Windows-unsafe characters,
  reserved device names, and unstable trailing dots or spaces are rejected.
- Identity, equality, and primary ordering use `OrdinalIgnoreCase` to match the
  YR/Windows target. Current culture is never consulted.
- The selected physical file's original relative filename case is retained.
- Logical path values never contain or expose a mounted absolute root.
- Stable reporting adds an ordinal case-sensitive secondary order only after
  logical identity has already been established. It does not choose a winner.

## Precedence algorithm

For every logical path, the resolver builds a provenance chain ordered by
priority descending. Source ID and actual relative path provide deterministic
report ordering within a priority, but cannot resolve a top-priority tie.

1. Disabled sources do not enter the source index.
2. Multiple case variants inside one source are a source conflict; no file is
   selected for that logical identity.
3. A unique highest-priority candidate is selected.
4. Two or more highest-priority sources produce
   `AmbiguousContentResolution`, even when their hashes are identical.
5. Every lower-priority candidate remains in the provenance chain as
   overridden evidence.

An incomplete source index, conflict, or ambiguity makes the aggregate result
incomplete. Complete resolved results have no public constructors, and the
serializer refuses every incomplete result.

## Directory-source stability

The scanner opens regular files read-only, rejects reparse points, checks size
and last-write time around hashing, and performs a second tree snapshot to
detect ordinary addition, deletion, rename, size change, or timestamp change.
These checks fail closed but are not an atomic file-system snapshot: an
adversarial same-size, timestamp-preserving mutation remains a known limit.

## Manifest boundary

The schema-1 resolved manifest is deterministic and includes safe source
identity/version/priority/fingerprint data, selected file metadata, and full
provenance. It excludes source roots, machine paths, bodies, and scan times.
The manifest is stored only in the configured external cache under its own
SHA-256. Public evidence records that hash and a sanitized aggregate summary.
The portable writer rejects observed reparse points but does not claim to
defeat a privileged actor concurrently swapping cache ancestry between every
path check and operation; controlled cache ancestry must remain stationary.

`YR1001_ProjectBaseline` is patched development content. Its directory-level
manifest is not a clean YR 1.001 golden manifest, does not inspect MIX payloads,
and is not original-behavior comparison evidence.
