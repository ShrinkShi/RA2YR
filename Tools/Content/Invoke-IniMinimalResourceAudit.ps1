[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $UnityEditorPath,

    [Parameter()]
    [string] $ProjectRoot,

    [Parameter()]
    [string] $ConfigurationPath,

    [Parameter()]
    [ValidateRange(60, 86400)]
    [int] $TimeoutSeconds = 3600
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$sharedWrapper = Join-Path $PSScriptRoot 'Invoke-IniProjectBaselineAudit.ps1'

& $sharedWrapper `
    -UnityEditorPath $UnityEditorPath `
    -ProjectRoot $ProjectRoot `
    -ConfigurationPath $ConfigurationPath `
    -AuditMode MinimalResource `
    -TimeoutSeconds $TimeoutSeconds
