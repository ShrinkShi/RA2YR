# ADR 0001: Repository and external-content boundary

- Status: Accepted
- Date: 2026-08-01

## Context

The compatibility engine requires locally supplied original game data and
external tools. Those materials are not project source and must not be copied,
cached, tested, or reported in a way that distributes their contents.

## Decision

The only formal Git repository root is the Unity project directory:

`E:\时锐\RA2\RA2YR-unity\RA2YR`

The parent directory is a workspace, not a repository. Original content,
unpacked data, patched data, FinalAlert 2, reference material, generated
caches, and local golden samples remain outside the repository. Logical
external roles are:

- `ExternalContent/YR1001_Clean`
- `ExternalContent/YR1001_Unpacked`
- `ExternalContent/YR1001_Patched`
- `ExternalTools/FinalAlert2`
- `Reference`

The current user-designated YR baseline is the external directory
`../尤里的复仇-1.001-原版（已加官方地图增补包、音乐包、win10兼容补丁）`.
It is identified through a read-only source configuration and future SHA-256
manifest. Its files must not be copied into `Assets` or committed.

The content layer must expose source identity and precedence. Caches use a
dedicated repository-external location and contain no source-of-truth data.
CI uses only independently generated synthetic samples. Local golden reports
contain hashes and derived metrics, never original asset bodies.

## Consequences

- Repository scanning fails closed on prohibited extensions/names, an
  intentionally limited set of binary signatures, known rejected SHA-256
  values, missing Git objects/files, unsafe index modes, and reparse points.
- Designated external-content and tool roots are rejected when physically
  present below the formal repository even if `.gitignore` hides them.
- Synthetic binary exemptions require an exact policy registration containing
  repository-relative path, SHA-256, generator, and provenance. The approved
  registration list is empty until a fixture is independently generated and
  reviewed; a directory name alone grants no exemption.
- A path being ignored is not sufficient authorization to place content under
  the repository root.
- Moving or renaming user content is outside normal engine operation.
- MIX support is an abstraction boundary; unpacked content can be used first.

## Verification

- `git rev-parse --show-toplevel` from the project resolves to `RA2YR`.
- The workspace parent is not a Git worktree.
- The scanner separately verifies index blobs and non-ignored untracked files
  with NUL-delimited path transport; tracked worktree state cannot hide a
  staged blob.
- Tracked-file and copyright scans report no external content or tools.
- Content tests prove read-only access and record source path plus SHA-256.
