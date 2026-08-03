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
$manifestType = 'RA2YR.IniProjectBaselineAuditSanitized'

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
            throw 'A controlled INI audit path traverses a reparse point.'
        }
    }
}

function Assert-RegularFile {
    param([Parameter(Mandatory)][string] $Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw 'A required controlled INI audit file was not found.'
    }
    Assert-NoExistingReparsePoint -Path $Path
    $attributes = [IO.File]::GetAttributes([IO.Path]::GetFullPath($Path))
    if (($attributes -band [IO.FileAttributes]::Directory) -ne 0 -or
        ($attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw 'A controlled INI audit input is not a regular file.'
    }
}

function Assert-GitIgnored {
    param(
        [Parameter(Mandatory)][string] $RepositoryRoot,
        [Parameter(Mandatory)][string] $Path
    )

    if (-not (Test-InsideOrEqual -Candidate $Path -Root $RepositoryRoot)) {
        throw 'Only repository-local paths can be checked against repository ignores.'
    }
    $root = [IO.Path]::GetFullPath($RepositoryRoot).TrimEnd('\')
    $relative = [IO.Path]::GetFullPath($Path).Substring($root.Length).TrimStart('\')
    $relative = $relative.Replace('\', '/')
    & git -C $root check-ignore --quiet -- $relative 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw 'A local INI audit configuration or result path is not ignored.'
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
        if ($null -ne $reader) { $reader.Dispose() }
    }
    if ($null -eq $document.DocumentElement -or
        $document.DocumentElement.Name -ne 'ExternalContent') {
        throw 'The local external-content configuration has an unexpected root.'
    }
    $cachePath = [string]$document.DocumentElement.GetAttribute('cachePath')
    if ([string]::IsNullOrWhiteSpace($cachePath)) {
        throw 'The local external-content configuration omits cachePath.'
    }
    if ([IO.Path]::IsPathRooted($cachePath)) {
        return [IO.Path]::GetFullPath($cachePath)
    }
    return [IO.Path]::GetFullPath((Join-Path ([IO.Path]::GetDirectoryName($Path)) $cachePath))
}

function Open-LockedFileIdentity {
    param([Parameter(Mandatory)][string] $Path)

    Assert-RegularFile -Path $Path
    $fullPath = [IO.Path]::GetFullPath($Path)
    $stream = $null
    $algorithm = $null
    try {
        $before = New-Object IO.FileInfo($fullPath)
        $stream = New-Object IO.FileStream(
            $fullPath,
            [IO.FileMode]::Open,
            [IO.FileAccess]::Read,
            [IO.FileShare]::Read,
            (64 * 1024),
            [IO.FileOptions]::SequentialScan)
        $algorithm = [Security.Cryptography.SHA256]::Create()
        $hash = [BitConverter]::ToString($algorithm.ComputeHash($stream)).Replace('-', '').ToLowerInvariant()
        $stream.Position = 0
        return [pscustomobject]@{
            Stream = $stream
            Path = $fullPath
            Length = [int64]$before.Length
            LastWriteTimeUtcTicks = [int64]$before.LastWriteTimeUtc.Ticks
            Sha256 = $hash
        }
    } catch {
        if ($null -ne $stream) { $stream.Dispose() }
        throw
    } finally {
        if ($null -ne $algorithm) { $algorithm.Dispose() }
    }
}

function Assert-LockedFileUnchanged {
    param([Parameter(Mandatory)][object] $Identity)

    Assert-RegularFile -Path ([string]$Identity.Path)
    $current = New-Object IO.FileInfo([string]$Identity.Path)
    if ([int64]$current.Length -ne [int64]$Identity.Length -or
        [int64]$current.LastWriteTimeUtc.Ticks -ne [int64]$Identity.LastWriteTimeUtcTicks) {
        throw 'A locked controlled INI audit input changed during execution.'
    }
}

function Assert-LowerSha256 {
    param([Parameter(Mandatory)][string] $Value, [string] $Context = 'value')
    if ($Value -cnotmatch '^[0-9a-f]{64}$') {
        throw "The $Context is not a lowercase SHA-256."
    }
}

function Assert-LogicalPath {
    param([Parameter(Mandatory)][string] $Value, [string] $Context = 'value')
    if ([string]::IsNullOrWhiteSpace($Value) -or [IO.Path]::IsPathRooted($Value) -or
        $Value.Contains('\') -or $Value.Contains(':')) {
        throw "The $Context is not a sanitized logical path."
    }
    foreach ($segment in $Value.Split('/')) {
        if ([string]::IsNullOrEmpty($segment) -or $segment -eq '.' -or $segment -eq '..') {
            throw "The $Context contains an invalid path segment."
        }
    }
}

function Assert-NoAbsolutePathInValue {
    param([AllowNull()][object] $Value)

    if ($null -eq $Value) { return }
    if ($Value -is [string]) {
        if ([IO.Path]::IsPathRooted([string]$Value) -or
            ([string]$Value) -match '^[A-Za-z]:[\\/]' -or
            ([string]$Value).StartsWith('\\', [StringComparison]::Ordinal)) {
            throw 'The sanitized INI summary contains an absolute host path.'
        }
        return
    }
    if ($Value -is [ValueType]) { return }
    if ($Value -is [Collections.IEnumerable] -and -not ($Value -is [pscustomobject])) {
        foreach ($item in $Value) { Assert-NoAbsolutePathInValue $item }
        return
    }
    foreach ($property in $Value.PSObject.Properties) {
        Assert-NoAbsolutePathInValue $property.Value
    }
}

function Assert-SampleIdentity {
    param(
        [Parameter(Mandatory)][object] $Sample,
        [Parameter(Mandatory)][string] $SampleId,
        [Parameter(Mandatory)][string] $LogicalName,
        [Parameter(Mandatory)][string] $MixId,
        [Parameter(Mandatory)][int64] $Length,
        [Parameter(Mandatory)][string] $Sha256,
        [Parameter(Mandatory)][string] $ModelSha256,
        [Parameter(Mandatory)][string] $RootArchive,
        [Parameter(Mandatory)][int] $LayerCount
    )

    if ([string]$Sample.sampleId -cne $SampleId -or
        [string]$Sample.logicalName -cne $LogicalName -or
        [string]$Sample.mixId -cne $MixId -or
        [int64]$Sample.length -ne $Length -or
        [string]$Sample.sha256 -cne $Sha256 -or
        [string]$Sample.bom -cne 'none' -or
        [string]$Sample.encodingObservation -cne 'raw-single-byte-bom-absent-code-page-unresolved' -or
        [bool]$Sample.byteIdentical -ne $true -or
        [string]$Sample.identityOutputSha256 -cne $Sha256 -or
        [string]$Sample.canonicalModelSha256 -cne $ModelSha256 -or
        [string]$Sample.provenance.sourceId -cne $baselineName -or
        [string]$Sample.provenance.rootArchive -cne $RootArchive -or
        @($Sample.provenance.layers).Count -ne $LayerCount) {
        throw "The fixed INI sample identity changed: $SampleId"
    }
    Assert-LowerSha256 ([string]$Sample.canonicalModelSha256) "$SampleId model hash"
}

function Assert-SanitizedSummary {
    param([Parameter(Mandatory)][object] $Summary, [Parameter(Mandatory)][string] $RawJson)

    if ([int]$Summary.schemaVersion -ne 1 -or
        [string]$Summary.manifestType -cne $manifestType -or
        [string]$Summary.baselineLogicalName -cne $baselineName -or
        [string]$Summary.auditStatus -cne 'Complete' -or
        @($Summary.samples).Count -ne 4 -or
        @($Summary.limitations).Count -lt 4) {
        throw 'The sanitized INI summary identity is invalid.'
    }
    Assert-LowerSha256 ([string]$Summary.directoryFingerprint) 'directory fingerprint'
    Assert-LogicalPath ([string]$Summary.externalManifest.cacheRelativePath) 'external manifest path'
    Assert-LowerSha256 ([string]$Summary.externalManifest.sha256) 'external manifest hash'
    if ([int64]$Summary.externalManifest.length -le 0) {
        throw 'The external INI manifest length is invalid.'
    }

    $samples = @{}
    foreach ($sample in @($Summary.samples)) { $samples[[string]$sample.sampleId] = $sample }
    Assert-SampleIdentity $samples['artmd-localmd'] 'artmd-localmd' 'artmd.ini' '0x5B47D8D5' 336535 `
        'e1f0378394313c04ebbd5073f47785ee3e46f1b3c62d65724e8f3c310ee7ba31' `
        'd138e1443bb1797b95c23857de0fffc9900ffae6838b9cd79c42707af519a64d' 'ra2md.mix' 2
    Assert-SampleIdentity $samples['ai-local'] 'ai-local' 'ai.ini' '0x9E11E49A' 84972 `
        '1feac6ddea6886b177ddf7e5f8580b7a99a63f12684f2cbb42831671bb7a8a79' `
        'b41fec9d9331349126b32929abbf2d1d8e77ce3959a4cf2461c034324c72a361' 'ra2.mix' 2
    Assert-SampleIdentity $samples['rulesmd-expandmd01'] 'rulesmd-expandmd01' 'rulesmd.ini' '0x8218F9F4' 743215 `
        '3d341ef8a13a4b5ab24af2eef48ac94931ac2bb87d950fe3330a07e2d25672ef' `
        '86fa33b1c844101ce6facb8df50e254ceb784bafb45880e0ce2f55fc3738d287' 'expandmd01.mix' 1
    Assert-SampleIdentity $samples['rulesmd-localmd'] 'rulesmd-localmd' 'rulesmd.ini' '0x8218F9F4' 742958 `
        '06761dd7f714e7d9400216ec3c06109ec5c1461f6a0727be7401eb9d8b0f6d05' `
        'b5f97e861fa620bf2af96060c8216965f682c5ae24ca50cdd6bde3219ab224e1' 'ra2md.mix' 2

    foreach ($forbidden in @(
        '"lineRecords"', '"identityCacheRelativePath"', '"rawBytes"',
        '"sectionName"', '"keyName"', '"valueText"', '"commentText"')) {
        if ($RawJson.Contains($forbidden)) {
            throw "The sanitized INI summary contains forbidden field: $forbidden"
        }
    }
    Assert-NoAbsolutePathInValue $Summary
}

if ([IO.Path]::DirectorySeparatorChar -ne '\') {
    throw 'The controlled YR1001_ProjectBaseline INI audit supports Windows only.'
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
Assert-NoExistingReparsePoint $resolvedProjectRoot
Assert-RegularFile $resolvedEditorPath
Assert-RegularFile $resolvedConfigurationPath
if (-not (Test-InsideOrEqual $resolvedConfigurationPath $resolvedProjectRoot)) {
    throw 'The local external-content configuration must remain inside the repository ignore boundary.'
}
$gitRootOutput = @(& git -C $resolvedProjectRoot rev-parse --show-toplevel 2>$null)
if ($LASTEXITCODE -ne 0 -or $gitRootOutput.Count -ne 1 -or
    -not ([IO.Path]::GetFullPath([string]$gitRootOutput[0]).TrimEnd('\')).Equals(
        $resolvedProjectRoot,
        [StringComparison]::OrdinalIgnoreCase)) {
    throw 'The supplied project directory is not the RA2YR Git root.'
}
Assert-GitIgnored $resolvedProjectRoot $resolvedConfigurationPath
if (Test-Path -LiteralPath (Join-Path $resolvedProjectRoot 'Temp\UnityLockfile')) {
    throw 'The Unity project is open. Close it before the INI baseline audit.'
}

$versionFile = Join-Path $resolvedProjectRoot 'ProjectSettings\ProjectVersion.txt'
Assert-RegularFile $versionFile
$versionText = [IO.File]::ReadAllText($versionFile, (New-Object Text.UTF8Encoding($false, $true)))
$versionMatch = [regex]::Match($versionText, '(?m)^m_EditorVersion:\s*(?<version>\S+)\s*$')
if (-not $versionMatch.Success -or $versionMatch.Groups['version'].Value -ne $expectedUnityVersion) {
    throw "ProjectVersion.txt must specify Unity $expectedUnityVersion."
}
$editorVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($resolvedEditorPath).ProductVersion
if ([string]::IsNullOrWhiteSpace($editorVersion) -or
    -not $editorVersion.StartsWith($expectedUnityVersion, [StringComparison]::Ordinal)) {
    throw 'The supplied Unity Editor does not match the project version.'
}

$cachePath = Get-ConfigurationCachePath $resolvedConfigurationPath
if (Test-InsideOrEqual $cachePath $resolvedProjectRoot) {
    throw 'The complete INI audit manifest and identity files must remain outside the repository.'
}
Assert-NoExistingReparsePoint $cachePath
$resultsRoot = Join-Path $resolvedProjectRoot 'TestResults'
Assert-NoExistingReparsePoint $resultsRoot
Assert-GitIgnored $resolvedProjectRoot $resultsRoot
[IO.Directory]::CreateDirectory($resultsRoot) | Out-Null
$runId = (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssfffZ') + '-' +
    [Guid]::NewGuid().ToString('N')
$runRoot = Join-Path $resultsRoot $runId
[IO.Directory]::CreateDirectory($runRoot) | Out-Null
Assert-NoExistingReparsePoint $runRoot
$summaryPath = Join-Path $runRoot 'wp02f-ini-project-baseline-summary.json'
$logPath = Join-Path $runRoot 'unity.log'

$configurationIdentity = Open-LockedFileIdentity $resolvedConfigurationPath
$process = $null
$unityExitCode = $null
try {
    $arguments = @(
        '-batchmode', '-nographics', '-quit',
        '-projectPath', (Quote-ProcessArgument $resolvedProjectRoot),
        '-executeMethod', 'RA2YR.Editor.IniProjectBaselineAuditCommand.Run',
        '-ra2yrExternalContentConfig', (Quote-ProcessArgument $resolvedConfigurationPath),
        '-ra2yrSummaryOutput', (Quote-ProcessArgument $summaryPath),
        '-logFile', (Quote-ProcessArgument $logPath)
    )
    $process = Start-Process -FilePath $resolvedEditorPath -ArgumentList $arguments `
        -PassThru -WindowStyle Hidden
    if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
        $process.Kill()
        $process.WaitForExit()
        $process.Refresh()
        $unityExitCode = $process.ExitCode
        throw "Unity INI baseline audit timed out; terminated process exit code: $unityExitCode."
    }
    $process.Refresh()
    $unityExitCode = $process.ExitCode
    Assert-LockedFileUnchanged $configurationIdentity
    if ($unityExitCode -ne 0) {
        throw "Unity INI baseline audit process exited with code $unityExitCode."
    }
} finally {
    if ($null -ne $process) { $process.Dispose() }
    $configurationIdentity.Stream.Dispose()
}

Assert-RegularFile $summaryPath
$summaryInfo = New-Object IO.FileInfo($summaryPath)
if ($summaryInfo.Length -le 0 -or $summaryInfo.Length -gt 2MB) {
    throw 'The sanitized INI audit summary has an invalid length.'
}
try {
    $summaryText = [IO.File]::ReadAllText(
        $summaryPath,
        (New-Object Text.UTF8Encoding($false, $true)))
    $summary = $summaryText | ConvertFrom-Json
} catch {
    throw 'The sanitized INI audit summary is not valid strict UTF-8 JSON.'
}
Assert-SanitizedSummary $summary $summaryText

$manifestRelativePath = [string]$summary.externalManifest.cacheRelativePath
$manifestPath = [IO.Path]::GetFullPath((Join-Path $cachePath $manifestRelativePath.Replace('/', '\')))
if (-not (Test-InsideOrEqual $manifestPath $cachePath) -or
    ([IO.Path]::GetFullPath($manifestPath).TrimEnd('\')).Equals(
        [IO.Path]::GetFullPath($cachePath).TrimEnd('\'),
        [StringComparison]::OrdinalIgnoreCase)) {
    throw 'The external INI audit manifest escaped its configured cache boundary.'
}
Assert-NoExistingReparsePoint $manifestPath
$manifestIdentity = Open-LockedFileIdentity $manifestPath
try {
    if ([int64]$manifestIdentity.Length -ne [int64]$summary.externalManifest.length -or
        [string]$manifestIdentity.Sha256 -cne [string]$summary.externalManifest.sha256) {
        throw 'The external INI audit manifest does not match the sanitized summary.'
    }
} finally {
    $manifestIdentity.Stream.Dispose()
}
$summaryIdentity = Open-LockedFileIdentity $summaryPath
try { $summarySha256 = [string]$summaryIdentity.Sha256 }
finally { $summaryIdentity.Stream.Dispose() }

"Unity process exit code: $unityExitCode"
"Audit status: $($summary.auditStatus)"
"Validated INI documents: $(@($summary.samples).Count)"
"External manifest SHA-256: $($summary.externalManifest.sha256)"
"Sanitized summary SHA-256: $summarySha256"
"Sanitized summary: TestResults/$runId/wp02f-ini-project-baseline-summary.json"
