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
