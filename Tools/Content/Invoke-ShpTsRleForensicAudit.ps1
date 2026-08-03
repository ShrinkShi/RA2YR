[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $UnityEditorPath,

    [Parameter()]
    [string] $ProjectRoot,

    [Parameter()]
    [string] $ExternalContentConfig,

    [Parameter()]
    [ValidateRange(60, 3600)]
    [int] $TimeoutSeconds = 600
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$expectedUnityVersion = '2022.3.60f1c1'
$baselineName = 'YR1001_ProjectBaseline'
$manifestType = 'RA2YR.ShpTsRleForensicSanitized'

function Get-FullPath {
    param([Parameter(Mandatory)][string] $Path)

    [IO.Path]::GetFullPath($Path).TrimEnd(
        [IO.Path]::DirectorySeparatorChar,
        [IO.Path]::AltDirectorySeparatorChar)
}

function Quote-ProcessArgument {
    param([Parameter(Mandatory)][string] $Value)

    if ($Value.Contains('"')) {
        throw 'A process argument contains an unsupported quote character.'
    }
    if ($Value.EndsWith('\', [StringComparison]::Ordinal)) {
        throw 'A quoted process path must not end with a directory separator.'
    }
    '"' + $Value + '"'
}

function Assert-NoExistingReparsePoint {
    param([Parameter(Mandatory)][string] $Path)

    $current = Get-FullPath $Path
    while (-not [string]::IsNullOrWhiteSpace($current)) {
        if (Test-Path -LiteralPath $current) {
            $item = Get-Item -LiteralPath $current -Force
            if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw "A controlled forensic path traverses a reparse point: $current"
            }
        }
        $parent = [IO.Path]::GetDirectoryName($current)
        if ([string]::IsNullOrWhiteSpace($parent) -or
            [string]::Equals($parent, $current, [StringComparison]::OrdinalIgnoreCase)) {
            break
        }
        $current = $parent
    }
}

function Assert-GitIgnored {
    param(
        [Parameter(Mandatory)][string] $RepositoryRoot,
        [Parameter(Mandatory)][string] $RelativePath
    )

    & git -C $RepositoryRoot check-ignore --quiet -- $RelativePath
    if ($LASTEXITCODE -ne 0) {
        throw "The controlled output path is not Git-ignored: $RelativePath"
    }
}

function Get-StreamSha256 {
    param([Parameter(Mandatory)][IO.FileStream] $Stream)

    $position = $Stream.Position
    try {
        $Stream.Position = 0
        $algorithm = [Security.Cryptography.SHA256]::Create()
        try {
            ([BitConverter]::ToString($algorithm.ComputeHash($Stream))).Replace('-', '').ToLowerInvariant()
        } finally {
            $algorithm.Dispose()
        }
    } finally {
        $Stream.Position = $position
    }
}

function Open-LockedFileIdentity {
    param([Parameter(Mandatory)][string] $Path)

    Assert-NoExistingReparsePoint $Path
    $item = Get-Item -LiteralPath $Path -Force
    if ($item.PSIsContainer -or
        (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0)) {
        throw 'The external content configuration must be a regular file.'
    }
    $stream = New-Object IO.FileStream(
        $item.FullName,
        [IO.FileMode]::Open,
        [IO.FileAccess]::Read,
        [IO.FileShare]::Read)
    try {
        [pscustomobject]@{
            Path = $item.FullName
            Stream = $stream
            Length = $stream.Length
            Sha256 = Get-StreamSha256 $stream
        }
    } catch {
        $stream.Dispose()
        throw
    }
}

function Assert-LockedFileUnchanged {
    param([Parameter(Mandatory)][object] $Identity)

    if ($Identity.Stream.Length -ne $Identity.Length -or
        (Get-StreamSha256 $Identity.Stream) -ne $Identity.Sha256) {
        throw 'A locked forensic input changed during the audit.'
    }
}

function Test-LowerSha256 {
    param([Parameter(Mandatory)][string] $Value)

    $Value -match '^[0-9a-f]{64}$'
}

function Assert-NoProtectedPath {
    param(
        [Parameter(Mandatory)][string] $Json,
        [Parameter(Mandatory)][string] $Path
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return
    }
    $full = Get-FullPath $Path
    $slash = $full.Replace('\', '/')
    $escaped = $full.Replace('\', '\\')
    if ($Json.IndexOf($full, [StringComparison]::OrdinalIgnoreCase) -ge 0 -or
        $Json.IndexOf($slash, [StringComparison]::OrdinalIgnoreCase) -ge 0 -or
        $Json.IndexOf($escaped, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw 'The sanitized forensic summary contains a protected host path.'
    }
}

function Assert-SanitizedSummary {
    param([Parameter(Mandatory)][object] $Summary)

    if ($Summary.manifestType -ne $manifestType -or
        $Summary.baselineLogicalName -ne $baselineName -or
        $Summary.stageA.frameCount -ne 257 -or
        $Summary.stageA.productionFailureCode -ne 'RleOutputOverflow' -or
        $Summary.stageA.productionFailureRow -ne 0 -or
        $Summary.stageA.widthRange.min -ne 14 -or
        $Summary.stageA.widthRange.max -ne 202 -or
        $Summary.stageA.parity.odd -ne 137 -or
        $Summary.stageA.parity.even -ne 120 -or
        $Summary.stageA.strictMechanical.widthPlusOne -ne 257 -or
        $Summary.stageA.extraSource.ZeroRun -ne 257 -or
        $Summary.stageA.extraSource.Literal -ne 0 -or
        $Summary.stageA.extraFromLastCommand.true -ne 257 -or
        $Summary.stageA.extraIsZero.true -ne 257 -or
        $Summary.stageA.ignoreOneExtraInputExact.true -ne 257 -or
        $Summary.stageA.overshootExactlyOne -ne 257) {
        throw 'The Stage A forensic aggregate drifted from the locked baseline.'
    }

    if (-not $Summary.stageB.executed -or
        $Summary.stageB.analyzedRows -ne 9495 -or
        $Summary.stageB.mechanicalWidth -ne 1331 -or
        $Summary.stageB.mechanicalWidthPlusOne -ne 8164 -or
        $Summary.stageB.mechanicalOther -ne 0 -or
        $Summary.stageB.extraFromFinalZeroRun -ne 8164 -or
        $Summary.stageB.extraIsZero -ne 8164 -or
        $Summary.stageB.literalOverflowRows -ne 0 -or
        $Summary.stageB.zeroZeroRows -ne 0 -or
        $Summary.stageB.malformedRows -ne 0 -or
        $Summary.stageB.ignoreOneExtraInputExact -ne 8164 -or
        $Summary.stageB.framesAllRowsGuardPattern -ne 0 -or
        $Summary.stageB.framesMixedPattern -ne 257 -or
        $Summary.decision -ne 'B' -or
        $Summary.productionRepairRecommended -ne $false -or
        $Summary.inputModesEquivalent -ne $true) {
        throw 'The Stage B forensic aggregate does not meet decision gate B.'
    }

    if (-not (Test-LowerSha256 ([string]$Summary.inputCatalogSha256)) -or
        -not (Test-LowerSha256 ([string]$Summary.canonicalModelSha256)) -or
        -not (Test-LowerSha256 ([string]$Summary.externalManifest.sha256)) -or
        [int64]$Summary.externalManifest.length -le 0 -or
        [string]::IsNullOrWhiteSpace([string]$Summary.externalManifest.cacheRelativePath)) {
        throw 'The forensic summary hashes or external manifest reference are invalid.'
    }

    $names = @($Summary.PSObject.Properties.Name)
    if ($names -contains 'records' -or $names -contains 'frames' -or $names -contains 'pixels') {
        throw 'The sanitized forensic summary contains disallowed per-record data.'
    }
}

if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = Join-Path $PSScriptRoot '..\..'
}
$ProjectRoot = Get-FullPath $ProjectRoot
if ([string]::IsNullOrWhiteSpace($ExternalContentConfig)) {
    $ExternalContentConfig = Join-Path $ProjectRoot 'Config\ExternalContent.local.xml'
}
$ExternalContentConfig = Get-FullPath $ExternalContentConfig
$UnityEditorPath = Get-FullPath $UnityEditorPath
$resultsRoot = Join-Path $ProjectRoot 'TestResults'
$lockFile = Join-Path $ProjectRoot 'Temp\UnityLockfile'

if (-not (Test-Path -LiteralPath $UnityEditorPath -PathType Leaf)) {
    throw 'The Unity Editor executable does not exist.'
}
$version = [Diagnostics.FileVersionInfo]::GetVersionInfo($UnityEditorPath).ProductVersion
if ([string]::IsNullOrWhiteSpace($version) -or
    $version.IndexOf($expectedUnityVersion, [StringComparison]::OrdinalIgnoreCase) -lt 0) {
    throw "Unity $expectedUnityVersion is required."
}
if (Test-Path -LiteralPath $lockFile) {
    throw 'Temp\UnityLockfile exists; close Unity before running the forensic audit.'
}
if (@(Get-Process Unity -ErrorAction SilentlyContinue).Count -ne 0) {
    throw 'A Unity process is already running.'
}

Assert-NoExistingReparsePoint $ProjectRoot
Assert-NoExistingReparsePoint $resultsRoot
Assert-GitIgnored $ProjectRoot 'TestResults'
$identity = Open-LockedFileIdentity $ExternalContentConfig
$runId = (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssfffZ') + '-' +
    [Guid]::NewGuid().ToString('N')
$runRoot = Join-Path $resultsRoot $runId
$summaryPath = Join-Path $runRoot 'm2-shp1f-rle-forensic-summary.json'
$logPath = Join-Path $runRoot 'Editor.log'
New-Item -ItemType Directory -Path $runRoot | Out-Null
Assert-NoExistingReparsePoint $runRoot

$arguments = @(
    '-batchmode',
    '-nographics',
    '-projectPath', (Quote-ProcessArgument $ProjectRoot),
    '-executeMethod', 'RA2YR.Editor.ShpTsRleForensicAuditCommand.Run',
    '-ra2yrExternalContentConfig', (Quote-ProcessArgument $ExternalContentConfig),
    '-ra2yrSummaryOutput', (Quote-ProcessArgument $summaryPath),
    '-logFile', (Quote-ProcessArgument $logPath)
)

$process = $null
$forcedPostResultShutdown = $false
$summaryFirstSeen = $null
try {
    $process = Start-Process -FilePath $UnityEditorPath -ArgumentList $arguments -PassThru
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    $nextProgress = [DateTime]::UtcNow.AddSeconds(60)
    while (-not $process.HasExited) {
        if ([DateTime]::UtcNow -ge $deadline) {
            Stop-Process -Id $process.Id -Force
            throw "The forensic audit exceeded its $TimeoutSeconds second timeout."
        }
        if (Test-Path -LiteralPath $summaryPath -PathType Leaf) {
            if ($null -eq $summaryFirstSeen) {
                $summaryFirstSeen = [DateTime]::UtcNow
            } elseif (([DateTime]::UtcNow - $summaryFirstSeen).TotalSeconds -ge 30) {
                Stop-Process -Id $process.Id -Force
                $forcedPostResultShutdown = $true
                break
            }
        }
        if ([DateTime]::UtcNow -ge $nextProgress) {
            $process.Refresh()
            $logLength = if (Test-Path -LiteralPath $logPath -PathType Leaf) {
                (Get-Item -LiteralPath $logPath -Force).Length
            } else { 0 }
            Write-Host ("forensic progress: pid={0}, cpu={1}, logBytes={2}, summary={3}" -f
                $process.Id, $process.CPU, $logLength,
                (Test-Path -LiteralPath $summaryPath -PathType Leaf))
            $nextProgress = [DateTime]::UtcNow.AddSeconds(60)
        }
        Start-Sleep -Seconds 1
        $process.Refresh()
    }

    if (-not $forcedPostResultShutdown) {
        $process.WaitForExit()
        if ($process.ExitCode -ne 0) {
            throw "Unity process exit code was $($process.ExitCode)."
        }
    }
    if (-not (Test-Path -LiteralPath $summaryPath -PathType Leaf)) {
        throw 'Unity did not publish the sanitized forensic summary.'
    }

    $utf8 = New-Object Text.UTF8Encoding($false, $true)
    $json = [IO.File]::ReadAllText($summaryPath, $utf8)
    $summary = $json | ConvertFrom-Json
    Assert-SanitizedSummary $summary
    Assert-NoProtectedPath $json $ProjectRoot
    Assert-NoProtectedPath $json $ExternalContentConfig
    Assert-LockedFileUnchanged $identity

    [pscustomobject]@{
        RunRoot = $runRoot
        SummaryPath = $summaryPath
        EditorLog = $logPath
        UnityExitCode = if ($forcedPostResultShutdown) { $null } else { $process.ExitCode }
        ForcedPostResultShutdown = $forcedPostResultShutdown
        Decision = [string]$summary.decision
        StageAFrames = [int]$summary.stageA.frameCount
        StageBRows = [int64]$summary.stageB.analyzedRows
        CanonicalModelSha256 = [string]$summary.canonicalModelSha256
    }
} finally {
    if ($null -ne $process) {
        $process.Dispose()
    }
    if ($null -ne $identity) {
        $identity.Stream.Dispose()
    }
}
