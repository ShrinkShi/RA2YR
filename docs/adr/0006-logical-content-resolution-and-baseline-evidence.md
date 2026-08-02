# ADR 0006: Logical content resolution and baseline evidence

- Status: Accepted
- Date: 2026-08-02

## Context

YR content is selected by Windows-compatible logical filenames across several
possible sources. File-system enumeration, source IDs, current culture, or
dictionary order must not silently change the selected bytes. The full
ProjectBaseline file list is useful locally but must not enter the public
repository.

## Decision

Logical paths use `/` syntax and `OrdinalIgnoreCase` identity while preserving
the selected physical filename's case. Larger numeric priority wins. A
source-internal case collision is rejected, and multiple highest-priority
sources are reported as ambiguous. Source IDs identify provenance and provide
stable report order only; they never break a winning tie.

Complete resolved manifests include the full provenance chain and are written
content-addressed below the repository-external cache. Only a sanitized
summary containing aggregate counts, manifest SHA-256, scan facts, and a small
approved representative set may be committed as evidence.

## Consequences

- Resolution is independent of enumeration, scheduling, dictionary, and
  current-culture behavior.
- Equal highest priority requires an explicit configuration decision instead
  of producing a hidden winner.
- Callers cannot serialize incomplete or forged production results.
- Local indexing reads file bytes for SHA-256, while public evidence contains
  no file bodies, absolute paths, or complete file-level manifest.
- MIX payload reading, clean YR 1.001 comparison, and behavior comparison
  remain separate unimplemented work.

## Alternatives rejected

- Source ID or enumeration order as a tie-breaker: deterministic-looking but
  semantically hidden precedence.
- Lower numeric priority wins: conflicts with the approved schema semantics.
- Commit the full hashed file list: unnecessary disclosure of proprietary
  content inventory.
- Treat the patched ProjectBaseline as a clean original baseline: unsupported
  by its declared composition.
