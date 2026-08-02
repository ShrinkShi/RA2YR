# ADR 0009: Independent MIX implementation and container boundaries

- Status: Accepted
- Date: 2026-08-02

## Context

The controlled project baseline primarily stores required files inside MIX
containers. The previous directory content source cannot see those entries.
XCC is the principal historical and interoperability reference, but its source
is GPLv2 while this repository is Apache-2.0. Existing WP-02A manifests also
bind directory-relative paths to logical names and have stable schema-1 hashes
that cannot be silently redefined for archive entries.

## Decision

MIX support is independently implemented in `RA2YR.Core` from documented
format facts, public algorithm definitions, synthetic vectors, controlled
baseline observations, and black-box XCC results. XCC and other copyleft code
are reference only; no source, translation, mechanical rewrite, binary, or
generated derivative enters this repository.

The implementation separates four concerns:

1. a bounded seekable window reads only header, directory, and selected entry
   ranges from large files;
2. the MIX structural model preserves numeric IDs, observed entry order,
   offsets, lengths, flags, and validation state;
3. candidate-name resolution maps only evidence-backed names to IDs;
4. a mounted-content layer adds explicit archive priority and full container
   provenance without changing WP-02A directory-manifest schema 1.

All windows in one mount audit share hard budgets for bytes, allocations,
records, child ranges, mounted archives, total entries, and nesting depth.
Closing the root invalidates child windows. A nested child can never read
outside its parent entry.

The writer uses complete reconstruction. `DeterministicRebuild` defines a
stable ID ordering; `PreserveEntryOrder` retains observed input order. Output
is written through an external temporary file, flushed, reread, verified, and
atomically published only to approved cache or test roots. Authority baseline
paths are never valid write targets.

Encryption and checksum capabilities are reported separately as read and
write states. Encrypted writes require an explicitly supplied/reused 80-byte
key source unless future evidence proves safe key-source generation. Checksum
support means actual SHA-1 verification or emission, not merely recognizing a
20-byte trailer.

## Consequences

- Large archives are not copied into memory per entry access.
- Unknown IDs remain accessible structurally without fabricated paths.
- Directory schema-1 hashes and WP-02A evidence remain stable.
- Parser completeness, checksum verification, decryption, name resolution,
  mounting, and interoperability are independently observable states.
- XCC's permissive duplicate, overlap, and nesting behavior is not adopted;
  malformed inputs fail closed under project limits.
- A format parser succeeding does not promote PAL or any payload format.

## Alternatives rejected

- Make the current directory `IContentSource` return archive pseudo-paths:
  this loses unknown IDs and container ancestry and creates hidden priority.
- Snapshot every MIX through the WP-02B Stream factory: this repeats hundreds
  of megabytes and resets budgets across nested sessions.
- Port XCC code: incompatible with the approved GPL reference-only boundary.
- Treat an XCC-openable file as byte-identical round-trip evidence: XCC open,
  semantic entry equality, payload equality, and archive byte equality are
  separate outcomes.

