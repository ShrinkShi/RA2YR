[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $UnityEditorPath,
    [Parameter()][string] $ProjectRoot,
    [Parameter()][string] $ConfigurationPath,
    [Parameter()][ValidateRange(60,86400)][int] $TimeoutSeconds = 3600
)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($ProjectRoot)) { $ProjectRoot = Join-Path $PSScriptRoot '..\..' }
$project = [IO.Path]::GetFullPath($ProjectRoot)
if ([string]::IsNullOrWhiteSpace($ConfigurationPath)) { $ConfigurationPath = Join-Path $project 'Config\ExternalContent.local.xml' }
$configuration = [IO.Path]::GetFullPath($ConfigurationPath)
$unity = [IO.Path]::GetFullPath($UnityEditorPath)
if (-not (Test-Path -LiteralPath $unity -PathType Leaf)) { throw 'Unity Editor executable was not found.' }
if (-not (Test-Path -LiteralPath $configuration -PathType Leaf)) { throw 'External content configuration was not found.' }
$resultsRoot = Join-Path $project 'TestResults'; [IO.Directory]::CreateDirectory($resultsRoot) | Out-Null
$runId = (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssfffZ') + '-' + [Guid]::NewGuid().ToString('N')
$runRoot = Join-Path $resultsRoot $runId; [IO.Directory]::CreateDirectory($runRoot) | Out-Null
$summary = Join-Path $runRoot 'm3-c6-tmp-theater-project-baseline-summary.json'; $log = Join-Path $runRoot 'unity.log'
$args = @('-batchmode','-nographics','-quit','-projectPath',$project,'-executeMethod','RA2YR.Editor.TmpTheaterProjectBaselineAuditCommand.Run','-ra2yrExternalContentConfig',$configuration,'-ra2yrSummaryOutput',$summary,'-logFile',$log)
$process = Start-Process -FilePath $unity -ArgumentList $args -PassThru -WindowStyle Hidden
try { if (-not $process.WaitForExit($TimeoutSeconds * 1000)) { $process.Kill(); $process.WaitForExit(); throw 'TMP/theater audit timed out.' }; $process.Refresh(); $exitCode = [int]$process.ExitCode } finally { $process.Dispose() }
if ($exitCode -ne 0) { throw "TMP/theater audit exited with code $exitCode." }
if (-not (Test-Path -LiteralPath $summary -PathType Leaf)) { throw 'TMP/theater audit did not produce a sanitized summary.' }
$summaryText = Get-Content -LiteralPath $summary -Raw -Encoding UTF8; $summaryObject = $summaryText | ConvertFrom-Json
if ($summaryObject.manifestType -ne 'RA2YR.TmpTheaterProjectBaselineAuditSanitized') { throw 'TMP/theater summary identity is invalid.' }
if ($summaryText -match '([A-Za-z]:\\|"bytes"\s*:|"pixels"\s*:|"raw"\s*:|"filename"\s*:)') { throw 'TMP/theater summary contains forbidden detail.' }
"Unity process exit code: $exitCode"; "Audit status: $($summaryObject.status)"; "TMP candidates: $($summaryObject.tmpCandidateCount)"; "Valid: $($summaryObject.validTmpCount)"; "Invalid: $($summaryObject.invalidTmpCount)"; "Sanitized summary: TestResults/$runId/m3-c6-tmp-theater-project-baseline-summary.json"
