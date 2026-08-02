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
$expectedUnityVersion = '2022.3.60f1c1'
$baselineName = 'YR1001_ProjectBaseline'
$manifestType = 'RA2YR.PaletteProjectBaselineAuditSanitized'
$displayConversionStrategy = 'XccScaleToFullRangeFloor'

function Quote-ProcessArgument {
    param([Parameter(Mandatory)][string] $Value)

    if ($Value.Contains('"') -or $Value.EndsWith('\', [StringComparison]::Ordinal)) {
        throw 'A process argument cannot be quoted safely.'
    }
    '"' + $Value + '"'
}

function Test-InsideOrEqual {
    param(
        [Parameter(Mandatory)][string] $Candidate,
        [Parameter(Mandatory)][string] $Root
    )

    $candidatePath = [IO.Path]::GetFullPath($Candidate).TrimEnd('\')
    $rootPath = [IO.Path]::GetFullPath($Root).TrimEnd('\')
    if ($candidatePath.Equals($rootPath, [StringComparison]::OrdinalIgnoreCase)) {
        return $true
    }
    return $candidatePath.StartsWith(
        $rootPath + '\',
        [StringComparison]::OrdinalIgnoreCase)
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
            throw 'A controlled PAL audit path traverses a reparse point.'
        }
    }
}

function Assert-RegularFile {
    param([Parameter(Mandatory)][string] $Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw 'A required controlled PAL audit file was not found.'
    }
    Assert-NoExistingReparsePoint -Path $Path
    $attributes = [IO.File]::GetAttributes([IO.Path]::GetFullPath($Path))
    if (($attributes -band [IO.FileAttributes]::Directory) -ne 0 -or
        ($attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw 'A controlled PAL audit input is not a regular file.'
    }
}

function Assert-GitIgnored {
    param(
        [Parameter(Mandatory)][string] $RepositoryRoot,
        [Parameter(Mandatory)][string] $Path
    )

    if (-not (Test-InsideOrEqual -Candidate $Path -Root $RepositoryRoot)) {
        throw 'Only paths inside the repository can be checked against repository ignores.'
    }
    $root = [IO.Path]::GetFullPath($RepositoryRoot).TrimEnd('\')
    $relative = [IO.Path]::GetFullPath($Path).Substring($root.Length).TrimStart('\')
    $relative = $relative.Replace('\', '/')
    & git -C $root check-ignore --quiet -- $relative 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw 'A local PAL audit configuration or result path is not excluded by .gitignore.'
    }
}

function Open-LockedFileIdentity {
    param([Parameter(Mandatory)][string] $Path)

    Assert-RegularFile -Path $Path
    $fullPath = [IO.Path]::GetFullPath($Path)
    $stream = $null
    $sha256 = $null
    try {
        $before = New-Object IO.FileInfo($fullPath)
        $stream = New-Object IO.FileStream(
            $fullPath,
            [IO.FileMode]::Open,
            [IO.FileAccess]::Read,
            [IO.FileShare]::Read,
            (64 * 1024),
            [IO.FileOptions]::SequentialScan)
        $sha256 = [Security.Cryptography.SHA256]::Create()
        $hash = [BitConverter]::ToString($sha256.ComputeHash($stream)).Replace('-', '')
        $stream.Position = 0
        return [pscustomobject]@{
            Stream = $stream
            Path = $fullPath
            Length = [int64]$before.Length
            LastWriteTimeUtcTicks = [int64]$before.LastWriteTimeUtc.Ticks
            Sha256 = $hash
        }
    } catch {
        if ($null -ne $stream) {
            $stream.Dispose()
        }
        throw
    } finally {
        if ($null -ne $sha256) {
            $sha256.Dispose()
        }
    }
}

function Assert-LockedFileUnchanged {
    param([Parameter(Mandatory)][object] $Identity)

    Assert-RegularFile -Path ([string]$Identity.Path)
    $current = New-Object IO.FileInfo([string]$Identity.Path)
    if ([int64]$current.Length -ne [int64]$Identity.Length -or
        [int64]$current.LastWriteTimeUtc.Ticks -ne
            [int64]$Identity.LastWriteTimeUtcTicks) {
        throw 'A locked controlled PAL audit input changed during execution.'
    }
}

function Get-ConfigurationCachePath {
    param([Parameter(Mandatory)][string] $Path)

    $settings = New-Object Xml.XmlReaderSettings
    $settings.DtdProcessing = [Xml.DtdProcessing]::Prohibit
    $settings.XmlResolver = $null
    $reader = $null
    try {
        $reader = [Xml.XmlReader]::Create($Path, $settings)
        $document = New-Object Xml.XmlDocument
        $document.XmlResolver = $null
        $document.Load($reader)
    } finally {
        if ($null -ne $reader) {
            $reader.Dispose()
        }
    }
    if ($null -eq $document.DocumentElement -or
        $document.DocumentElement.Name -ne 'ExternalContent') {
        throw 'The local external-content configuration has an unexpected root element.'
    }
    $cachePath = [string]$document.DocumentElement.GetAttribute('cachePath')
    if ([string]::IsNullOrWhiteSpace($cachePath)) {
        throw 'The local external-content configuration does not declare cachePath.'
    }
    if ([IO.Path]::IsPathRooted($cachePath)) {
        return [IO.Path]::GetFullPath($cachePath)
    }
    return [IO.Path]::GetFullPath((Join-Path ([IO.Path]::GetDirectoryName($Path)) $cachePath))
}

function Assert-ExactJsonProperties {
    param(
        [Parameter(Mandatory)][object] $Object,
        [Parameter(Mandatory)][string[]] $Names,
        [Parameter(Mandatory)][string] $Context
    )

    if ($null -eq $Object) {
        throw "The $Context JSON object is null."
    }
    $actual = @($Object.PSObject.Properties | ForEach-Object { $_.Name })
    if ($actual.Count -ne $Names.Count) {
        throw "The $Context JSON object has an unexpected property count."
    }
    foreach ($name in $Names) {
        if ($actual -notcontains $name) {
            throw "The $Context JSON object is missing a required property."
        }
    }
}

function Get-NonNegativeInt64 {
    param(
        [Parameter(Mandatory)][object] $Value,
        [Parameter(Mandatory)][string] $Context
    )

    if (-not ($Value -is [int]) -and -not ($Value -is [long])) {
        throw "The $Context value is not a JSON integer."
    }
    $number = [int64]$Value
    if ($number -lt 0) {
        throw "The $Context value is negative."
    }
    return $number
}

function Assert-LowerSha256 {
    param(
        [Parameter(Mandatory)][string] $Value,
        [Parameter(Mandatory)][string] $Context
    )

    if ($Value -cnotmatch '^[0-9a-f]{64}$') {
        throw "The $Context value is not a lowercase SHA-256."
    }
}

function Assert-LogicalPath {
    param(
        [Parameter(Mandatory)][string] $Value,
        [Parameter(Mandatory)][string] $Context
    )

    if ([string]::IsNullOrWhiteSpace($Value) -or
        [IO.Path]::IsPathRooted($Value) -or
        $Value.Contains('\') -or
        $Value.Contains(':')) {
        throw "The $Context value is not a sanitized logical path."
    }
    $segments = $Value.Split('/')
    foreach ($segment in $segments) {
        if ([string]::IsNullOrEmpty($segment) -or $segment -eq '.' -or $segment -eq '..') {
            throw "The $Context value contains an invalid logical path segment."
        }
    }
}

function Assert-IsoUtcTimestamp {
    param(
        [Parameter(Mandatory)][string] $Value,
        [Parameter(Mandatory)][string] $Context
    )

    if ($Value -notmatch 'Z$') {
        throw "The $Context timestamp is not explicitly UTC."
    }
    $parsed = [DateTimeOffset]::MinValue
    if (-not [DateTimeOffset]::TryParse(
        $Value,
        [Globalization.CultureInfo]::InvariantCulture,
        [Globalization.DateTimeStyles]::RoundtripKind,
        [ref]$parsed)) {
        throw "The $Context timestamp is invalid."
    }
}

function Assert-NoAbsolutePathInJsonValue {
    param(
        [AllowNull()][object] $Value,
        [Parameter(Mandatory)][string] $Context
    )

    if ($null -eq $Value) {
        return
    }
    if ($Value -is [string]) {
        if ([IO.Path]::IsPathRooted([string]$Value) -or
            ([string]$Value) -match '^[A-Za-z]:[\\/]' -or
            ([string]$Value).StartsWith('\\', [StringComparison]::Ordinal)) {
            throw "The $Context JSON value contains an absolute host path."
        }
        return
    }
    if ($Value -is [System.ValueType]) {
        return
    }
    if ($Value -is [System.Collections.IEnumerable] -and
        -not ($Value -is [pscustomobject])) {
        $index = 0
        foreach ($item in $Value) {
            Assert-NoAbsolutePathInJsonValue -Value $item -Context "$Context[$index]"
            $index++
        }
        return
    }
    if (-not ($Value -is [pscustomobject])) {
        throw "The $Context JSON value has an unsupported object type."
    }
    foreach ($property in $Value.PSObject.Properties) {
        Assert-NoAbsolutePathInJsonValue -Value $property.Value `
            -Context "$Context.$($property.Name)"
    }
}

function Assert-SanitizedSummary {
    param([Parameter(Mandatory)][object] $Summary)

    Assert-ExactJsonProperties -Object $Summary -Context 'summary' -Names @(
        'schemaVersion',
        'manifestType',
        'baselineLogicalName',
        'auditStatus',
        'sourceVersion',
        'directoryFingerprint',
        'startedUtc',
        'completedUtc',
        'externalManifest',
        'palettes',
        'limitations'
    )
    if ((Get-NonNegativeInt64 -Value $Summary.schemaVersion -Context 'schemaVersion') -ne 1 -or
        [string]$Summary.manifestType -cne $manifestType -or
        [string]$Summary.baselineLogicalName -cne $baselineName -or
        [string]$Summary.auditStatus -cne 'Complete' -or
        [string]::IsNullOrWhiteSpace([string]$Summary.sourceVersion)) {
        throw 'The sanitized PAL audit summary identity is invalid.'
    }
    Assert-LowerSha256 -Value ([string]$Summary.directoryFingerprint) `
        -Context 'directoryFingerprint'
    Assert-IsoUtcTimestamp -Value ([string]$Summary.startedUtc) -Context 'startedUtc'
    Assert-IsoUtcTimestamp -Value ([string]$Summary.completedUtc) -Context 'completedUtc'

    Assert-ExactJsonProperties -Object $Summary.externalManifest `
        -Context 'externalManifest' `
        -Names @('schemaVersion', 'cacheRelativePath', 'length', 'sha256')
    if ((Get-NonNegativeInt64 `
            -Value $Summary.externalManifest.schemaVersion `
            -Context 'externalManifest.schemaVersion') -ne 1) {
        throw 'The external PAL manifest schema version is invalid.'
    }
    Assert-LogicalPath -Value ([string]$Summary.externalManifest.cacheRelativePath) `
        -Context 'externalManifest.cacheRelativePath'
    if ((Get-NonNegativeInt64 `
            -Value $Summary.externalManifest.length `
            -Context 'externalManifest.length') -le 0) {
        throw 'The external PAL manifest length is invalid.'
    }
    Assert-LowerSha256 -Value ([string]$Summary.externalManifest.sha256) `
        -Context 'externalManifest.sha256'

    $expected = @{
        'isotem.pal' = @{
            Id = '0x5F9D97B9'
            Sha256 = '5d6e40fcd11a592a31494c635d93c21796cfe86a2743f0258b1f7d0aff850795'
            DistinctColors = 256
        }
        'temperat.pal' = @{
            Id = '0x9C58DE40'
            Sha256 = '5903b69868b84f494cfbb4e7100398015ef9775b37726019a0d7b5fb6cb33b55'
            DistinctColors = 256
        }
        'unittem.pal' = @{
            Id = '0x63DA7359'
            Sha256 = 'ed785e62eed291480f3198dd44f6b656ebe3a9b75e9f641944d710abc6bde3e3'
            DistinctColors = 210
        }
    }
    $palettes = @($Summary.palettes)
    if ($palettes.Count -ne $expected.Count) {
        throw 'The sanitized PAL summary does not contain exactly three golden palettes.'
    }
    $observedNames = New-Object `
        'System.Collections.Generic.HashSet[string]' `
        ([StringComparer]::OrdinalIgnoreCase)
    foreach ($palette in $palettes) {
        Assert-ExactJsonProperties -Object $palette -Context 'palette' -Names @(
            'logicalName',
            'mixId',
            'provenance',
            'length',
            'sha256',
            'colorCount',
            'rawChannelMin',
            'rawChannelMax',
            'invalidChannelCount',
            'distinctColorCount',
            'normalizedModelSha256',
            'displayConversionStrategy',
            'diagnosticCount'
        )
        $logicalName = [string]$palette.logicalName
        Assert-LogicalPath -Value $logicalName -Context 'palette.logicalName'
        if ($logicalName -cnotin @('isotem.pal', 'temperat.pal', 'unittem.pal') -or
            -not $expected.ContainsKey($logicalName) -or
            -not $observedNames.Add($logicalName)) {
            throw 'The sanitized PAL summary contains an unknown or duplicate palette.'
        }
        $identity = $expected[$logicalName]
        if ([string]$palette.mixId -cne [string]$identity.Id -or
            [string]$palette.sha256 -cne [string]$identity.Sha256 -or
            (Get-NonNegativeInt64 -Value $palette.length -Context 'palette.length') -ne 768 -or
            (Get-NonNegativeInt64 -Value $palette.colorCount -Context 'palette.colorCount') -ne 256 -or
            (Get-NonNegativeInt64 -Value $palette.rawChannelMin -Context 'palette.rawChannelMin') -ne 0 -or
            (Get-NonNegativeInt64 -Value $palette.rawChannelMax -Context 'palette.rawChannelMax') -ne 63 -or
            (Get-NonNegativeInt64 -Value $palette.invalidChannelCount -Context 'palette.invalidChannelCount') -ne 0 -or
            (Get-NonNegativeInt64 -Value $palette.distinctColorCount -Context 'palette.distinctColorCount') -ne [int64]$identity.DistinctColors -or
            (Get-NonNegativeInt64 -Value $palette.diagnosticCount -Context 'palette.diagnosticCount') -ne 0 -or
            [string]$palette.displayConversionStrategy -cne $displayConversionStrategy) {
            throw "The golden PAL identity or parsed statistics changed for $logicalName."
        }
        Assert-LowerSha256 -Value ([string]$palette.sha256) -Context 'palette.sha256'
        Assert-LowerSha256 -Value ([string]$palette.normalizedModelSha256) `
            -Context 'palette.normalizedModelSha256'
        $provenance = @($palette.provenance)
        if ($provenance.Count -ne 1) {
            throw "The golden PAL provenance is invalid for $logicalName."
        }
        $provenanceRecord = $provenance[0]
        Assert-ExactJsonProperties -Object $provenanceRecord `
            -Context 'palette.provenance' `
            -Names @('sourceId', 'rootArchive', 'layers')
        if ([string]$provenanceRecord.sourceId -cne $baselineName -or
            [string]$provenanceRecord.rootArchive -cne 'ra2.mix') {
            throw "The golden PAL source identity changed for $logicalName."
        }
        $layers = @($provenanceRecord.layers)
        if ($layers.Count -ne 2) {
            throw "The golden PAL MIX chain changed for $logicalName."
        }
        foreach ($layer in $layers) {
            Assert-ExactJsonProperties -Object $layer -Context 'palette.provenance.layer' `
                -Names @('archive', 'entryId', 'resolvedName')
            Assert-LogicalPath -Value ([string]$layer.archive) `
                -Context 'palette.provenance.layer.archive'
            Assert-LogicalPath -Value ([string]$layer.resolvedName) `
                -Context 'palette.provenance.layer.resolvedName'
            if ([string]$layer.entryId -cnotmatch '^0x[0-9A-F]{8}$') {
                throw 'A golden PAL provenance MIX ID is invalid.'
            }
        }
        if ([string]$layers[0].archive -cne 'ra2.mix' -or
            [string]$layers[0].entryId -cne '0x3B5A96DE' -or
            [string]$layers[0].resolvedName -cne 'cache.mix' -or
            [string]$layers[1].archive -cne 'ra2.mix/cache.mix' -or
            [string]$layers[1].entryId -cne [string]$identity.Id -or
            [string]$layers[1].resolvedName -cne $logicalName) {
            throw "The golden PAL provenance chain changed for $logicalName."
        }
    }

    $limitations = @($Summary.limitations)
    if ($limitations.Count -eq 0) {
        throw 'The sanitized PAL audit summary omits its limitations.'
    }
    foreach ($limitation in $limitations) {
        if (-not ($limitation -is [string]) -or
            [string]::IsNullOrWhiteSpace([string]$limitation)) {
            throw 'A sanitized PAL audit limitation is invalid.'
        }
    }
    Assert-NoAbsolutePathInJsonValue -Value $Summary -Context 'summary'
}

if ([IO.Path]::DirectorySeparatorChar -ne '\') {
    throw 'The controlled YR1001_ProjectBaseline PAL audit supports Windows only.'
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

if (-not (Test-Path -LiteralPath $resolvedProjectRoot -PathType Container)) {
    throw 'The Unity project root was not found.'
}
Assert-NoExistingReparsePoint -Path $resolvedProjectRoot
Assert-RegularFile -Path $resolvedEditorPath
Assert-RegularFile -Path $resolvedConfigurationPath
if (-not (Test-InsideOrEqual `
        -Candidate $resolvedConfigurationPath `
        -Root $resolvedProjectRoot)) {
    throw 'The local external-content configuration must remain inside the repository ignore boundary.'
}

$gitRootOutput = @(& git -C $resolvedProjectRoot rev-parse --show-toplevel 2>$null)
if ($LASTEXITCODE -ne 0 -or $gitRootOutput.Count -ne 1 -or
    -not ([IO.Path]::GetFullPath([string]$gitRootOutput[0]).TrimEnd('\')).Equals(
        $resolvedProjectRoot,
        [StringComparison]::OrdinalIgnoreCase)) {
    throw 'The supplied project directory is not the RA2YR Git repository root.'
}
Assert-GitIgnored -RepositoryRoot $resolvedProjectRoot `
    -Path $resolvedConfigurationPath

if (Test-Path -LiteralPath (Join-Path $resolvedProjectRoot 'Temp\UnityLockfile')) {
    throw 'The Unity project is open. Close the Editor before the PAL baseline audit.'
}
$versionFile = Join-Path $resolvedProjectRoot 'ProjectSettings\ProjectVersion.txt'
Assert-RegularFile -Path $versionFile
$versionText = [IO.File]::ReadAllText(
    $versionFile,
    (New-Object Text.UTF8Encoding($false, $true)))
$versionMatch = [regex]::Match(
    $versionText,
    '(?m)^m_EditorVersion:\s*(?<version>\S+)\s*$')
if (-not $versionMatch.Success -or
    $versionMatch.Groups['version'].Value -ne $expectedUnityVersion) {
    throw "ProjectVersion.txt must specify Unity $expectedUnityVersion."
}
$editorVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo(
    $resolvedEditorPath).ProductVersion
if ([string]::IsNullOrWhiteSpace($editorVersion) -or
    -not $editorVersion.StartsWith($expectedUnityVersion, [StringComparison]::Ordinal) -or
    ($editorVersion.Length -gt $expectedUnityVersion.Length -and
        $editorVersion[$expectedUnityVersion.Length] -notin @('_', '+'))) {
    throw 'The supplied Unity Editor does not match the project version.'
}

$cachePath = Get-ConfigurationCachePath -Path $resolvedConfigurationPath
if (Test-InsideOrEqual -Candidate $cachePath -Root $resolvedProjectRoot) {
    throw 'The complete PAL audit manifest cache must remain outside the repository.'
}
Assert-NoExistingReparsePoint -Path $cachePath

$resultsRoot = Join-Path $resolvedProjectRoot 'TestResults'
Assert-NoExistingReparsePoint -Path $resultsRoot
Assert-GitIgnored -RepositoryRoot $resolvedProjectRoot -Path $resultsRoot
[IO.Directory]::CreateDirectory($resultsRoot) | Out-Null
$runId = (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssfffZ') + '-' +
    [Guid]::NewGuid().ToString('N')
$runRoot = Join-Path $resultsRoot $runId
if (Test-Path -LiteralPath $runRoot) {
    throw 'The unique PAL audit result directory already exists.'
}
[IO.Directory]::CreateDirectory($runRoot) | Out-Null
Assert-NoExistingReparsePoint -Path $runRoot
$summaryPath = Join-Path $runRoot 'wp02d-pal-project-baseline-summary.json'
$logPath = Join-Path $runRoot 'unity.log'

$lockedFiles = New-Object Collections.Generic.List[object]
$process = $null
$unityExitCode = $null
try {
    $configurationIdentity = Open-LockedFileIdentity -Path $resolvedConfigurationPath
    $lockedFiles.Add($configurationIdentity)
    $lockedCachePath = Get-ConfigurationCachePath -Path $resolvedConfigurationPath
    if (-not ([IO.Path]::GetFullPath($lockedCachePath).TrimEnd('\')).Equals(
            [IO.Path]::GetFullPath($cachePath).TrimEnd('\'),
            [StringComparison]::OrdinalIgnoreCase)) {
        throw 'The local external-content configuration changed before its read lock was acquired.'
    }

    $arguments = @(
        '-batchmode',
        '-nographics',
        '-quit',
        '-projectPath', (Quote-ProcessArgument -Value $resolvedProjectRoot),
        '-executeMethod', 'RA2YR.Editor.PaletteProjectBaselineAuditCommand.Run',
        '-ra2yrExternalContentConfig',
            (Quote-ProcessArgument -Value $resolvedConfigurationPath),
        '-ra2yrSummaryOutput', (Quote-ProcessArgument -Value $summaryPath),
        '-logFile', (Quote-ProcessArgument -Value $logPath)
    )

    $process = Start-Process -FilePath $resolvedEditorPath -ArgumentList $arguments `
        -PassThru -WindowStyle Hidden
    if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
        $process.Kill()
        $process.WaitForExit()
        $process.Refresh()
        $unityExitCode = $process.ExitCode
        throw "Unity PAL baseline audit timed out; terminated process exit code: $unityExitCode."
    }
    $process.Refresh()
    $unityExitCode = $process.ExitCode

    Assert-LockedFileUnchanged -Identity $configurationIdentity
    if ($unityExitCode -ne 0) {
        throw "Unity PAL baseline audit process exited with code $unityExitCode."
    }
} finally {
    if ($null -ne $process) {
        $process.Dispose()
    }
    for ($index = $lockedFiles.Count - 1; $index -ge 0; $index--) {
        $lockedFiles[$index].Stream.Dispose()
    }
}

Assert-RegularFile -Path $summaryPath
$summaryInfo = New-Object IO.FileInfo($summaryPath)
if ($summaryInfo.Length -le 0 -or $summaryInfo.Length -gt 1MB) {
    throw 'The sanitized PAL audit summary has an invalid length.'
}
try {
    $summaryText = [IO.File]::ReadAllText(
        $summaryPath,
        (New-Object Text.UTF8Encoding($false, $true)))
    $summary = $summaryText | ConvertFrom-Json
} catch {
    throw 'The sanitized PAL audit summary is not valid strict UTF-8 JSON.'
}
Assert-SanitizedSummary -Summary $summary

$manifestRelativePath = [string]$summary.externalManifest.cacheRelativePath
$manifestPath = [IO.Path]::GetFullPath((Join-Path `
    $cachePath `
    $manifestRelativePath.Replace('/', '\')))
if (-not (Test-InsideOrEqual -Candidate $manifestPath -Root $cachePath) -or
    ([IO.Path]::GetFullPath($manifestPath).TrimEnd('\')).Equals(
        [IO.Path]::GetFullPath($cachePath).TrimEnd('\'),
        [StringComparison]::OrdinalIgnoreCase)) {
    throw 'The external PAL audit manifest escaped its configured cache boundary.'
}
Assert-NoExistingReparsePoint -Path $manifestPath
$externalManifestIdentity = Open-LockedFileIdentity -Path $manifestPath
try {
    if ([int64]$externalManifestIdentity.Length -ne
            [int64]$summary.externalManifest.length -or
        -not ([string]$externalManifestIdentity.Sha256).Equals(
            [string]$summary.externalManifest.sha256,
            [StringComparison]::OrdinalIgnoreCase)) {
        throw 'The external PAL audit manifest does not match the sanitized summary.'
    }
} finally {
    $externalManifestIdentity.Stream.Dispose()
}

$summaryIdentity = Open-LockedFileIdentity -Path $summaryPath
try {
    $summarySha256 = [string]$summaryIdentity.Sha256
} finally {
    $summaryIdentity.Stream.Dispose()
}

"Unity process exit code: $unityExitCode"
"Audit status: $($summary.auditStatus)"
"Validated palettes: $(@($summary.palettes).Count)"
"Display conversion strategy: $displayConversionStrategy"
"External manifest SHA-256: $($summary.externalManifest.sha256)"
"Sanitized summary SHA-256: $summarySha256"
"Sanitized summary: TestResults/$runId/wp02d-pal-project-baseline-summary.json"
