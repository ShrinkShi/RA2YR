# Controlled content audit tools

## WP-02A directory manifest

`Invoke-ContentBaselineManifest.ps1` invokes Unity 2022.3.60f1c1 to index the
enabled `YR1001_ProjectBaseline` directory source. It is intended for Windows
PowerShell 5.1 and PowerShell 7 on Windows.

The ignored configuration must have exactly one enabled source, and its ID
must be `YR1001_ProjectBaseline`; the command refuses mixed overlay manifests.

The command:

- requires Unity Editor to be closed;
- reads the ignored local external-content configuration;
- opens mounted source files read-only for SHA-256;
- writes the complete resolved manifest only to the configured external cache;
- writes a sanitized, ignored JSON summary below `TestResults`;
- does not parse MIX payloads and does not copy source files into the repository.

The external cache and ignored `TestResults` ancestry must remain stationary
and under the local operator's control during execution. The portable
path-based checks reject observed reparse points but are not a handle-relative
transaction against concurrent privileged junction replacement.

Run from the repository root:

```powershell
./Tools/Content/Invoke-ContentBaselineManifest.ps1 `
    -UnityEditorPath 'C:\Path\To\Unity.exe'
```

The summary is an intermediate local artifact. Review it before manually
transcribing allowed aggregates and representative hashes into compatibility
evidence. Do not commit the Unity log or the complete external manifest.

## WP-02C MIX baseline audit

`Invoke-MixBaselineAudit.ps1` invokes the Unity Editor command
`RA2YR.Editor.MixBaselineAuditCommand.Run`. It audits root-level MIX archives
from the controlled `YR1001_ProjectBaseline`, writes the complete file-level
manifest only to the configured repository-external cache, and atomically
writes one sanitized summary below ignored `TestResults`.

The wrapper supports Windows PowerShell 5.1 and PowerShell 7 on Windows. It:

- requires Unity 2022.3.60f1c1 and a closed Unity project;
- requires the approved `XCC Mixer.exe` SHA-256
  `DD4E54956874BE8B995BE9B046B7302BF0F40B86A7C8BEED4A94165C6B50E7ED`;
- requires the adjacent `global mix database.dat` SHA-256
  `C76F529AF17CBE516E85AA4DDDCE614CF0AD98A8590208C71FBE3A047FB77AB8`;
- never starts XCC Mixer;
- keeps configuration, XCC, and name-database files read-locked while Unity
  performs the audit;
- rejects observed reparse points and verifies Git ignore/cache boundaries;
- validates the complete sanitized-summary schema and count relationships;
- independently verifies the external manifest length and SHA-256; and
- reports Unity's actual process exit code.

Run from the repository root:

```powershell
./Tools/Content/Invoke-MixBaselineAudit.ps1 `
    -UnityEditorPath 'C:\Path\To\Unity.exe' `
    -XccMixerPath 'C:\Path\To\XCC Mixer.exe'
```

The script does not display or commit target payload bytes, absolute content
paths, the complete external manifest, or original asset previews. Both the
Unity log and sanitized summary remain ignored local artifacts. Review the
summary before manually transcribing only approved aggregates and hashes into
delivery evidence.

## WP-02D PAL ProjectBaseline audit

`Invoke-PaletteProjectBaselineAudit.ps1` invokes
`RA2YR.Editor.PaletteProjectBaselineAuditCommand.Run`. It resolves
`isotem.pal`, `temperat.pal`, and `unittem.pal` through the merged MIX virtual
content source, parses each bounded entry, and validates its fixed ProjectBaseline
length, SHA-256, and provenance chain.

The wrapper supports Windows PowerShell 5.1 and PowerShell 7 on Windows. It:

- requires Unity 2022.3.60f1c1 and a closed Unity project;
- requires the ignored configuration to remain inside the formal Git root;
- read-locks the configuration while Unity performs the audit;
- rejects observed reparse points and verifies Git ignore/cache boundaries;
- accepts only the three fixed PAL identities and their sanitized statistics;
- independently verifies the repository-external manifest length and SHA-256;
- reports Unity's actual process exit code; and
- does not require or start XCC Mixer.

Run from the repository root:

```powershell
./Tools/Content/Invoke-PaletteProjectBaselineAudit.ps1 `
    -UnityEditorPath 'C:\Path\To\Unity.exe'
```

The complete per-index raw-color manifest remains below the configured external
Cache. The ignored JSON summary below `TestResults` contains only logical names,
MIX IDs, provenance, lengths, hashes, aggregate channel statistics, model hashes,
the explicitly named XCC reference conversion strategy, diagnostics, and
limitations. That strategy is not evidence of original YR rendering behavior and
is not a global compatibility default.

Run the wrapper contract tests once in each supported host:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File `
    ./Tools/Content/Tests/Invoke-PaletteProjectBaselineAudit.Tests.ps1

pwsh.exe -NoProfile -File `
    ./Tools/Content/Tests/Invoke-PaletteProjectBaselineAudit.Tests.ps1
```

## WP-02E CSF ProjectBaseline audit

`Invoke-CsfProjectBaselineAudit.ps1` invokes
`RA2YR.Editor.CsfProjectBaselineAuditCommand.Run`. It mounts only
`langmd.mix`, resolves the fixed `ra2md.csf` entry, validates its ID, length,
SHA-256 and provenance, then applies the strict bounded CSF v3 reader.

The wrapper supports Windows PowerShell 5.1 and PowerShell 7 on Windows. It:

- requires Unity 2022.3.60f1c1 and a closed Unity project;
- read-locks the ignored local configuration during the audit;
- rejects observed reparse points and verifies Git ignore/cache boundaries;
- rejects a loose `ra2md.csf` and any changed payload, provenance, or model hash;
- validates the exact sanitized-summary schema without reading text into the
  PowerShell process;
- independently verifies the repository-external manifest length and SHA-256;
- reports Unity's actual process exit code; and
- does not require, start, or automate XCC Mixer.

Run from the repository root:

```powershell
./Tools/Content/Invoke-CsfProjectBaselineAudit.ps1 `
    -UnityEditorPath 'C:\Path\To\Unity.exe'
```

The complete ordered record audit remains below the configured external Cache.
The ignored JSON summary below `TestResults` contains only the logical identity,
MIX provenance, file and model hashes, record counts, length ranges, language
code, diagnostics, and limitations. It contains no label list or string body.

Run the wrapper contract tests once in each supported host:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File `
    ./Tools/Content/Tests/Invoke-CsfProjectBaselineAudit.Tests.ps1

pwsh.exe -NoProfile -File `
    ./Tools/Content/Tests/Invoke-CsfProjectBaselineAudit.Tests.ps1
```

## WP-02F INI ProjectBaseline audit and identity roundtrip

`Invoke-IniProjectBaselineAudit.ps1` invokes
`RA2YR.Editor.IniProjectBaselineAuditCommand.Run`. It mounts the fixed root and
nested MIX chains for `artmd.ini`, `ai.ini`, and both distinct `rulesmd.ini`
candidates, then applies the bounded raw-byte INI parser and unmodified identity
writer. It does not choose a winning `rulesmd.ini` candidate.

The wrapper supports Windows PowerShell 5.1 and PowerShell 7 on Windows. It:

- requires Unity 2022.3.60f1c1 and a closed Unity project;
- read-locks the ignored local configuration while Unity runs;
- pins each fixed payload and canonical model identity;
- rejects observed reparse points and verifies Git ignore/cache boundaries;
- writes complete line-level and identity artifacts only below the configured
  repository-external Cache;
- independently verifies the external manifest length and SHA-256; and
- reports Unity's actual process exit code separately from parsed audit status.

Run from the repository root:

```powershell
./Tools/Content/Invoke-IniProjectBaselineAudit.ps1 `
    -UnityEditorPath 'C:\Path\To\Unity.exe'
```

The ignored JSON summary below `TestResults` contains only logical identities,
MIX provenance, aggregate structure statistics, one-way hashes, diagnostics,
and limitations. It contains no section list, key list, values, comments, raw
lines, Base64, or host paths. `roundtrip` in this workflow means only unmodified
byte-identical output; semantic editing and FinalAlert 2 edited roundtrip remain
unimplemented.

Run the wrapper contract tests in both supported hosts:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File `
    ./Tools/Content/Tests/Invoke-IniProjectBaselineAudit.Tests.ps1

pwsh.exe -NoProfile -File `
    ./Tools/Content/Tests/Invoke-IniProjectBaselineAudit.Tests.ps1
```

## WP-02G1 INI runtime-resolution audit

`Invoke-IniRuntimeResolutionAudit.ps1` forwards to the same locked,
reparse-rejecting wrapper used by WP-02F, selecting
`IniProjectBaselineAuditCommand.RunRuntimeResolution`. It mounts the fixed
ProjectBaseline chains, keeps both `rulesmd.ini` and both `soundmd.ini`
candidates, and publishes only candidate identities plus Opaque/semicolon
aggregates.

Run from the repository root:

```powershell
./Tools/Content/Invoke-IniRuntimeResolutionAudit.ps1 `
    -UnityEditorPath 'C:\Path\To\Unity.exe'
```

The command never starts the game, XCC, or FinalAlert 2. It never selects a
runtime winner. The complete line-level base audit remains in the configured
repository-external Cache, while the ignored JSON below `TestResults` contains
no source text or host path. The wrapper validates the fixed candidate hashes,
explicit null winners, external-manifest identity, and Unity's real exit code.

The shared wrapper regression suite covers both physical-document and runtime
modes and must pass in Windows PowerShell 5.1 and PowerShell 7:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File `
    ./Tools/Content/Tests/Invoke-IniProjectBaselineAudit.Tests.ps1

pwsh.exe -NoProfile -File `
    ./Tools/Content/Tests/Invoke-IniProjectBaselineAudit.Tests.ps1
```

## WP-02G2 minimal Rules and Art resource audit

`Invoke-IniMinimalResourceAudit.ps1` reuses the locked, reparse-rejecting INI
wrapper and selects `IniProjectBaselineAuditCommand.RunMinimalResourceTypedViews`.
It evaluates the two fixed Rules candidates independently and the fixed Art
candidate under a single-document `ConfiguredForTesting` plan. It does not
select or merge a stock runtime winner.

```powershell
./Tools/Content/Invoke-IniMinimalResourceAudit.ps1 `
    -UnityEditorPath 'C:\Path\To\Unity.exe'
```

The ignored JSON under `TestResults` contains aggregate registry, field,
route, diagnostic, and provenance-coverage counts plus one-way model hashes.
It rejects object-name lists, resource-name lists, values, raw bytes, and host
paths. The complete physical INI manifest remains in the configured external
Cache. Run the command with both Windows PowerShell 5.1 and PowerShell 7 for
the delivery regression.

## M2-SHP1 SHP(TS) ProjectBaseline audit

`Invoke-ShpTsProjectBaselineAudit.ps1` invokes
`RA2YR.Editor.ShpTsProjectBaselineAuditCommand.Run`. It mounts six fixed SHP
entries through the MIX virtual content source, validates every payload and
logical provenance chain, parses the immutable SHP(TS) directory, and compares
Memory, seekable Stream, and bounded MIX-window decode results.

```powershell
./Tools/Content/Invoke-ShpTsProjectBaselineAudit.ps1 `
    -UnityEditorPath 'C:\Path\To\Unity.exe'
```

The wrapper supports Windows PowerShell 5.1 and PowerShell 7. It read-locks
the ignored external-content configuration, rejects reparse points and
repository/cache boundary violations, pins all six payload and model hashes,
and independently verifies the external manifest length and SHA-256. It never
starts XCC or the game and never writes to `YR1001_ProjectBaseline`.

The accepted audit status is deliberately
`CompleteWithDecodeFailures`: 257 non-empty flags 3 frames fail strict row
width validation. Raw flags 0/1 frames decode successfully. The ignored JSON
summary contains only aggregate statistics and one-way hashes; the complete
per-frame manifest remains in the configured repository-external Cache and
contains no index buffers.

Run the wrapper contract tests in both supported hosts:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File `
    ./Tools/Content/Tests/Invoke-ShpTsProjectBaselineAudit.Tests.ps1

pwsh.exe -NoProfile -File `
    ./Tools/Content/Tests/Invoke-ShpTsProjectBaselineAudit.Tests.ps1
```

## M2-SHP1F flags-3 row-width forensic audit

`Invoke-ShpTsRleForensicAudit.ps1` invokes the independent scalar analyzer for
the 257 locked non-empty flags-3 failures. It validates the original row-zero
aggregate before inference and executes the all-row stage only when the five
final-zero-run guard preconditions are satisfied.

```powershell
./Tools/Content/Invoke-ShpTsRleForensicAudit.ps1 `
    -UnityEditorPath 'C:\Path\To\Unity.exe'
```

The accepted result is decision B: 9,495 rows contain 1,331 exact-width rows
and 8,164 width-plus-one rows, with every frame containing both classes. The
wrapper pins these aggregates, records real Unity exit state, and publishes
only a sanitized summary below ignored `TestResults`; per-frame/per-row scalar
records remain in repository-external Cache. It does not change or relax the
production decoder and does not start XCC, FinalAlert, or the game.

Run the wrapper contract tests in both supported hosts:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File `
    ./Tools/Content/Tests/Invoke-ShpTsRleForensicAudit.Tests.ps1

pwsh.exe -NoProfile -File `
    ./Tools/Content/Tests/Invoke-ShpTsRleForensicAudit.Tests.ps1
```

## WP-02C XCC synthetic interoperability

`Invoke-XccSyntheticInterop.ps1` provides three operator-facing modes around
the repository's autonomous synthetic MIX fixtures:

- `Prepare` publishes the internal deterministic contract: classic, checksum,
  encrypted, and nested MIX cases plus fixed synthetic candidate inputs;
- `VerifyXccCreated` validates a staged archive candidate and publishes a
  preserve-entry-order rebuild; and
- `VerifyXccExtractions` validates staged extraction candidates against the
  fixed autonomous payload contract.

The wrapper validates Unity 2022.3.60f1c1 and the approved `XCC Mixer.exe`
SHA-256, but never starts XCC. All cases, Unity logs, command results, and
verification artifacts stay below the configured repository-external Cache.
Console output contains only cache-relative paths, sizes, hashes, roles, and
sanitized diagnostics. The wrapper independently verifies every reported
artifact against its length and SHA-256 before accepting a stage.

Prepare a new case:

```powershell
./Tools/Content/Invoke-XccSyntheticInterop.ps1 `
    -Mode Prepare `
    -CaseId 'manual-roundtrip-01' `
    -UnityEditorPath 'C:\Path\To\Unity.exe' `
    -XccMixerPath 'C:\Path\To\XCC Mixer.exe'
```

After the operator uses XCC Mixer on copies of the generated synthetic files,
place the created archive at
`wp02c/xcc-interop/<case-id>/incoming-from-xcc/xcc-created.mix`. Place the
manual extraction candidates below the fixed
`wp02c/xcc-interop/<case-id>/extracted-candidates` subtree, using the six
directories `ra2yr-classic`, `ra2yr-checksum`, `ra2yr-encrypted`,
`ra2yr-inner`, `ra2yr-nested`, and `xcc-created-rebuild`. Then run the
remaining modes with the same case ID:

```powershell
./Tools/Content/Invoke-XccSyntheticInterop.ps1 `
    -Mode VerifyXccCreated `
    -CaseId 'manual-roundtrip-01' `
    -UnityEditorPath 'C:\Path\To\Unity.exe' `
    -XccMixerPath 'C:\Path\To\XCC Mixer.exe'

./Tools/Content/Invoke-XccSyntheticInterop.ps1 `
    -Mode VerifyXccExtractions `
    -CaseId 'manual-roundtrip-01' `
    -UnityEditorPath 'C:\Path\To\Unity.exe' `
    -XccMixerPath 'C:\Path\To\XCC Mixer.exe'
```

The stages are deliberately non-destructive and non-idempotent: an existing
case or verification publication is rejected. Use a new case ID instead of
overwriting prior evidence. Only autonomous synthetic content may be used in
this workflow; do not point it at the authoritative game baseline. These
commands validate staged files and always report
`realXccExecutionEvidence: false`; separate controlled human records are
required to prove that the staged files were actually produced by XCC Mixer.
