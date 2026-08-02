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
    [int] $TimeoutSeconds = 1800
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$expectedUnityVersion = '2022.3.60f1c1'
$baselineName = 'YR1001_ProjectBaseline'

function Quote-ProcessArgument {
    param([Parameter(Mandatory)][string] $Value)

    if ($Value.Contains('"') -or $Value.EndsWith('\', [StringComparison]::Ordinal)) {
        throw 'A process argument cannot be quoted safely.'
    }
    '"' + $Value + '"'
}

function Assert-NoExistingReparsePoint {
    param([Parameter(Mandatory)][string] $Path)

    $fullPath = [IO.Path]::GetFullPath($Path)
    $current = [IO.Path]::GetPathRoot($fullPath)
    $remainder = $fullPath.Substring($current.Length)
    foreach ($segment in $remainder.Split(@(
        [IO.Path]::DirectorySeparatorChar,
        [IO.Path]::AltDirectorySeparatorChar
    ), [StringSplitOptions]::RemoveEmptyEntries)) {
        $current = Join-Path $current $segment
        try {
            $attributes = [IO.File]::GetAttributes($current)
        } catch [IO.FileNotFoundException] {
            return
        } catch [IO.DirectoryNotFoundException] {
            return
        }
        if (($attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw 'A controlled baseline command path traverses a reparse point.'
        }
    }
}

if ([IO.Path]::DirectorySeparatorChar -ne '\') {
    throw 'The controlled YR1001_ProjectBaseline command currently supports Windows only.'
}
if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = Join-Path $PSScriptRoot '..\..'
}

$resolvedProjectRoot = [IO.Path]::GetFullPath($ProjectRoot).TrimEnd('\')
$resolvedEditorPath = [IO.Path]::GetFullPath($UnityEditorPath)
if ([string]::IsNullOrWhiteSpace($ConfigurationPath)) {
    $ConfigurationPath = Join-Path $resolvedProjectRoot 'Config\ExternalContent.local.xml'
}
$resolvedConfigurationPath = [IO.Path]::GetFullPath($ConfigurationPath)

if (-not (Test-Path -LiteralPath $resolvedEditorPath -PathType Leaf)) {
    throw 'Unity Editor executable was not found.'
}
if (-not (Test-Path -LiteralPath $resolvedConfigurationPath -PathType Leaf)) {
    throw 'The ignored local external-content configuration was not found.'
}
Assert-NoExistingReparsePoint -Path $resolvedProjectRoot
Assert-NoExistingReparsePoint -Path $resolvedEditorPath
Assert-NoExistingReparsePoint -Path $resolvedConfigurationPath
if (Test-Path -LiteralPath (Join-Path $resolvedProjectRoot 'Temp\UnityLockfile')) {
    throw 'The Unity project is open. Close the Editor before baseline indexing.'
}

$versionFile = Join-Path $resolvedProjectRoot 'ProjectSettings\ProjectVersion.txt'
$versionText = [IO.File]::ReadAllText($versionFile, (New-Object System.Text.UTF8Encoding($false, $true)))
$versionMatch = [regex]::Match($versionText, '(?m)^m_EditorVersion:\s*(?<version>\S+)\s*$')
if (-not $versionMatch.Success -or
    $versionMatch.Groups['version'].Value -ne $expectedUnityVersion) {
    throw "ProjectVersion.txt must specify Unity $expectedUnityVersion."
}
$editorVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($resolvedEditorPath).ProductVersion
if ([string]::IsNullOrWhiteSpace($editorVersion) -or
    -not $editorVersion.StartsWith($expectedUnityVersion, [StringComparison]::Ordinal) -or
    ($editorVersion.Length -gt $expectedUnityVersion.Length -and
        $editorVersion[$expectedUnityVersion.Length] -notin @('_', '+'))) {
    throw 'The supplied Unity Editor does not match the project version.'
}

$resultsRoot = Join-Path $resolvedProjectRoot 'TestResults'
Assert-NoExistingReparsePoint -Path $resultsRoot
[IO.Directory]::CreateDirectory($resultsRoot) | Out-Null
$runId = (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssfffZ') + '-' +
    [Guid]::NewGuid().ToString('N')
$runRoot = Join-Path $resultsRoot $runId
[IO.Directory]::CreateDirectory($runRoot) | Out-Null
Assert-NoExistingReparsePoint -Path $runRoot
$summaryPath = Join-Path $runRoot 'wp02a-baseline-summary.json'
$logPath = Join-Path $runRoot 'unity.log'

$arguments = @(
    '-batchmode',
    '-nographics',
    '-quit',
    '-projectPath', (Quote-ProcessArgument -Value $resolvedProjectRoot),
    '-executeMethod', 'RA2YR.Editor.ContentBaselineManifestCommand.Run',
    '-ra2yrExternalContentConfig', (Quote-ProcessArgument -Value $resolvedConfigurationPath),
    '-ra2yrBaselineSourceId', $baselineName,
    '-ra2yrSummaryOutput', (Quote-ProcessArgument -Value $summaryPath),
    '-logFile', (Quote-ProcessArgument -Value $logPath)
)

$process = Start-Process -FilePath $resolvedEditorPath -ArgumentList $arguments `
    -PassThru -WindowStyle Hidden
try {
    if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
        $process.Kill()
        $process.WaitForExit()
        throw "Baseline indexing exceeded the $TimeoutSeconds second timeout."
    }
    $process.Refresh()
    if ($process.ExitCode -ne 0) {
        throw 'Unity baseline indexing failed; inspect the ignored run log.'
    }
} finally {
    $process.Dispose()
}

if (-not (Test-Path -LiteralPath $summaryPath -PathType Leaf)) {
    throw 'Unity did not produce the sanitized baseline summary.'
}
try {
    $summary = [IO.File]::ReadAllText(
        $summaryPath,
        (New-Object System.Text.UTF8Encoding($false, $true))) | ConvertFrom-Json
} catch {
    throw 'The sanitized baseline summary is not valid strict UTF-8 JSON.'
}
if ([int]$summary.schemaVersion -ne 1 -or
    [string]$summary.baselineLogicalName -ne $baselineName -or
    [string]$summary.manifestSha256 -notmatch '^[0-9a-f]{64}$' -or
    [int64]$summary.totalFileCount -lt 0 -or
    [int64]$summary.totalBytes -lt 0) {
    throw 'The sanitized baseline summary failed schema validation.'
}

"Baseline logical name: $($summary.baselineLogicalName)"
"Manifest SHA-256: $($summary.manifestSha256)"
"Directory files: $($summary.totalFileCount)"
"Directory bytes: $($summary.totalBytes)"
"Diagnostics: $($summary.diagnosticCount)"
"Changes detected: $($summary.changesDetected)"
"Sanitized summary: TestResults/$runId/wp02a-baseline-summary.json"
