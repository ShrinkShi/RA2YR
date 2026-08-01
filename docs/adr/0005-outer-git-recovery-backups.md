# ADR 0005: Outer Git recovery backups

- Status: Accepted
- Date: 2026-08-01

## Context

An unintended Git metadata directory appeared at the workspace root
`E:\时锐\RA2\RA2YR-unity\.git`. It was not the approved repository root and
contained material data. Destructive deletion was prohibited.

## Decision and recorded operation

The original metadata was moved, without deletion, to:

`E:\时锐\RA2\RA2YR-unity\.git.backup-20260801-162014`

Recorded backup inventory after the move:

- 21,773 files
- 646,130,014 bytes
- readable `config`, `HEAD`, `objects`, and `refs`

Attribute/permission handling initially left an empty hidden `.git` directory,
so work stopped for user approval. After a final safety check confirmed zero
files, zero bytes, no reparse point, no hidden/system file payload, and no
meaningful alternate data stream, its Hidden attribute was cleared and the
empty shell was renamed, without deletion, to:

`E:\时锐\RA2\RA2YR-unity\.git.empty-backup-20260801-171609`

Post-operation verification on 2026-08-01 found the full backup unchanged at
21,773 files and 646,130,014 bytes, the empty-shell backup at zero files and
zero bytes, and no `.git` at the workspace root.

Neither backup directory may be deleted. At minimum they must remain until the
formal remote repository, first commit, and first pull request have all been
verified. Meeting that minimum does not authorize automatic deletion; any
later deletion requires an explicit user decision and a fresh safety check.

## Consequences

- The formal repository is initialized only inside the `RA2YR` Unity project.
- Recovery data remains available if the original outer state is needed.
- Repository ignore rules are not a substitute for retaining these backups
  outside the formal repository root.

## Verification

- Confirm both backup paths exist before the first commit and pull request.
- Recheck full-backup file count, byte count, and readability of the four Git
  members without altering them.
- Confirm the workspace root is not detected as a Git repository.
