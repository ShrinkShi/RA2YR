# ADR 0008: Logical content path hash contract

- Status: Accepted
- Date: 2026-08-02

## Context

`LogicalContentPath.Equals` uses the runtime's `OrdinalIgnoreCase` semantics,
but its original stable FNV implementation uppercased each UTF-16 code unit
independently. Supplementary-plane case pairs can compare equal while their
surrogate code units produce different hashes. That violates the equality and
hash contract and can split one logical path across `HashSet` or `GroupBy`.

## Decision

`LogicalContentPath.GetHashCode` first applies invariant uppercase mapping to
the complete string, not each UTF-16 code unit, and then applies deterministic
FNV-1a to the folded UTF-16 sequence. Tests cover a fixed ASCII hash, BMP
boundaries, and multiple supplementary-plane case pairs, always checking the
active Unity runtime's `OrdinalIgnoreCase` equality result.

The returned integer is a deterministic collection hash, not a persisted
protocol field. It is never a manifest identity, ordering key, replay value,
or network value. Canonical reports continue to use explicit
OrdinalIgnoreCase-then-Ordinal ordering, and persistent identity continues to
use SHA-256 where required.

## Consequences

- Every pair considered equal by the active runtime has matching collection
  hashes.
- The numeric hash is deterministic across processes using the same algorithm
  and Unicode casing table, but is not a persistent cross-runtime protocol.
- Priority and ambiguity semantics do not change; Unicode-equivalent logical
  paths now reliably enter the same group.

## Alternatives rejected

- Continue per-`char` folding: fails for case mappings represented by a UTF-16
  surrogate pair.
- Use `StringComparer.OrdinalIgnoreCase.GetHashCode`: it satisfies the equality
  contract but is process-randomized on supported runtimes.
- Normalize path spelling before storage: would discard the required original
  physical filename case.
