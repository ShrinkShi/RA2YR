# Controlled content manifest tool

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
- never parses MIX payloads or copies source files into the repository.

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
