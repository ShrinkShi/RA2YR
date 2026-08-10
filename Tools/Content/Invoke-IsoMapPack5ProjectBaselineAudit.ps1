[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $UnityEditorPath,
    [Parameter()][string] $ProjectRoot,
    [Parameter()][string] $ConfigurationPath,
    [Parameter()][ValidateRange(60, 86400)][int] $TimeoutSeconds = 3600
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$expectedUnityVersion = '2022.3.60f1c1'

function Test-Inside {
    param([string] $Candidate, [string] $Root)
    $candidateFull = [IO.Path]::GetFullPath($Candidate).TrimEnd('\')
    $rootFull = [IO.Path]::GetFullPath($Root).TrimEnd('\')
    return $candidateFull.Equals($rootFull, [StringComparison]::OrdinalIgnoreCase) -or
        $candidateFull.StartsWith($rootFull + '\', [StringComparison]::OrdinalIgnoreCase)
}

if ([string]::IsNullOrWhiteSpace($ProjectRoot)) { $ProjectRoot = Join-Path $PSScriptRoot '..\..' }
$project = [IO.Path]::GetFullPath($ProjectRoot).TrimEnd('\')
if ([string]::IsNullOrWhiteSpace($ConfigurationPath)) { $ConfigurationPath = Join-Path $project 'Config\ExternalContent.local.xml' }
$configuration = [IO.Path]::GetFullPath($ConfigurationPath)
$unity = [IO.Path]::GetFullPath($UnityEditorPath)
if (-not (Test-Path -LiteralPath $unity -PathType Leaf)) { throw 'Unity Editor executable was not found.' }
if (-not (Test-Path -LiteralPath $configuration -PathType Leaf)) { throw 'External content configuration was not found.' }
$versionText = Get-Content -LiteralPath (Join-Path $project 'ProjectSettings\ProjectVersion.txt') -Raw -Encoding UTF8
if ($versionText -notmatch '(?m)^m_EditorVersion:\s*' + [regex]::Escape($expectedUnityVersion) + '\s*$') { throw 'ProjectVersion.txt does not match the required Unity version.' }
$editorVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($unity).ProductVersion
if ([string]::IsNullOrWhiteSpace($editorVersion) -or -not $editorVersion.StartsWith($expectedUnityVersion, [StringComparison]::Ordinal)) { throw 'Unity Editor version does not match the project version.' }
if (Test-Path -LiteralPath (Join-Path $project 'Temp\UnityLockfile')) { throw 'The Unity project is already locked.' }

$settings = New-Object Xml.XmlReaderSettings
$settings.DtdProcessing = [Xml.DtdProcessing]::Prohibit
$settings.XmlResolver = $null
$reader = [Xml.XmlReader]::Create($configuration, $settings)
try { $xml = New-Object Xml.XmlDocument; $xml.XmlResolver = $null; $xml.Load($reader) } finally { $reader.Dispose() }
$cacheValue = [string]$xml.DocumentElement.GetAttribute('cachePath')
if ([string]::IsNullOrWhiteSpace($cacheValue)) { throw 'The external configuration does not declare cachePath.' }
$cache = if ([IO.Path]::IsPathRooted($cacheValue)) { [IO.Path]::GetFullPath($cacheValue) } else { [IO.Path]::GetFullPath((Join-Path ([IO.Path]::GetDirectoryName($configuration)) $cacheValue)) }
if (Test-Inside $cache $project) { throw 'The audit cache must remain outside the repository.' }

$resultsRoot = Join-Path $project 'TestResults'
[IO.Directory]::CreateDirectory($resultsRoot) | Out-Null
$runId = (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssfffZ') + '-' + [Guid]::NewGuid().ToString('N')
$runRoot = Join-Path $resultsRoot $runId
[IO.Directory]::CreateDirectory($runRoot) | Out-Null
$summary = Join-Path $runRoot 'm3-c4-isomap-pack5-project-baseline-summary.json'
$log = Join-Path $runRoot 'unity.log'
$arguments = @('-batchmode','-nographics','-quit','-projectPath',$project,'-executeMethod','RA2YR.Editor.IsoMapPack5ProjectBaselineAuditCommand.Run','-ra2yrExternalContentConfig',$configuration,'-ra2yrSummaryOutput',$summary,'-logFile',$log)
$process = Start-Process -FilePath $unity -ArgumentList $arguments -PassThru -WindowStyle Hidden
try {
    if (-not $process.WaitForExit($TimeoutSeconds * 1000)) { $process.Kill(); $process.WaitForExit(); throw 'IsoMapPack5 audit timed out.' }
    $process.Refresh()
    $exitCode = [int]$process.ExitCode
} finally { $process.Dispose() }
if ($exitCode -ne 0) { throw "Unity IsoMapPack5 audit exited with code $exitCode." }
if (-not (Test-Path -LiteralPath $summary -PathType Leaf)) { throw 'The audit did not produce a sanitized summary.' }
$summaryText = Get-Content -LiteralPath $summary -Raw -Encoding UTF8
$summaryObject = $summaryText | ConvertFrom-Json
if ($null -eq $summaryObject -or $summaryObject.manifestType -ne 'RA2YR.IsoMapPack5ProjectBaselineAuditSanitized') { throw 'The sanitized summary identity is invalid.' }
if ($summaryText -match '([A-Za-z]:\\|"records"\s*:|"coordinates"\s*:|"payload"\s*:|"fragmentValue"\s*:)' ) { throw 'The sanitized summary contains forbidden detail.' }
if ($summaryObject.status -ne 'Complete') { throw "IsoMapPack5 audit status is '$($summaryObject.status)', not Complete." }
"Unity process exit code: $exitCode"
"Audit status: $($summaryObject.status)"
"Candidate sections: $($summaryObject.candidateSectionCount)"
"Successful sections: $($summaryObject.successfulSectionCount)"
"Failed sections: $($summaryObject.failedSectionCount)"
"Sanitized summary: TestResults/$runId/m3-c4-isomap-pack5-project-baseline-summary.json"
