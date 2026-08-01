# Repository checks

`Invoke-CopyrightScan.ps1` independently scans Git index blobs and non-ignored
untracked worktree files. Git paths are consumed as NUL-delimited UTF-8 data,
so spaces, non-ASCII characters, and embedded newlines cannot split a path.
The default JSON report contains repository-relative paths only. It fails when
it finds:

- ignored or visible external-content/tool roots physically present under the
  formal repository root;
- external-content or generated directories inside the repository;
- original-game or archive file types without an exact synthetic-fixture
  registration (`path`, SHA-256, generator, and provenance);
- known original configuration file names;
- a small, explicit set of executable/archive/audio magic signatures or a
  known proprietary SHA-256, even if the file was renamed;
- index symlinks, gitlinks, unresolved index stages, candidate/ancestor
  reparse points, missing blobs/files, or unexpectedly large candidates;
- missing defensive `.gitignore` rules for local content and caches.

The synthetic registration list is intentionally empty until a generated
fixture is reviewed. Merely placing a file below the synthetic fixture
directory never bypasses a restriction. Signature checks are a limited safety
gate, not a general malware or copyright classifier.

Run from PowerShell:

```powershell
./Tools/Repository/Invoke-CopyrightScan.ps1
```

Machine-readable output is available with `-Json`.

Run the scanner regression suite (Windows PowerShell 5.1 and PowerShell 7 are
both exercised when installed):

```powershell
./Tools/Repository/Tests/Invoke-CopyrightScan.Tests.ps1
```

`Invoke-RepositoryValidation.ps1` enforces repository structure that was
previously checked by hand. It verifies:

- one matching `.meta` for every Unity resource and directory below `Assets`;
- no orphan `.meta`, malformed GUID, duplicate GUID, or reparse point below
  `Assets`;
- the exact Unity Editor version in `ProjectVersion.txt`;
- `RA2YR.Core.asmdef` has Boolean `noEngineReferences: true` and Core text does
  not reference `UnityEngine` or `UnityEditor`;
- the canonical compatibility-matrix schema-v1 subset, unique entry IDs,
  declared status vocabulary, required list fields, and repository evidence
  paths/fragments.

The matrix reader intentionally accepts only the repository's canonical YAML
schema-v1 subset. Unsupported YAML syntax fails closed and requires a reviewed
schema or validator change. No YAML module or network dependency is used.

Run validation from either Windows PowerShell 5.1 or PowerShell 7:

```powershell
./Tools/Repository/Invoke-RepositoryValidation.ps1
```

Machine-readable output is available with `-Json`. The regression suite
launches both PowerShell hosts when installed and exercises passing and
failing synthetic repositories:

```powershell
./Tools/Repository/Tests/Invoke-RepositoryValidation.Tests.ps1
```
