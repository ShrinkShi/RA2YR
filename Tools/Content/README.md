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
