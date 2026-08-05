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
$manifestType = 'RA2YR.CsfProjectBaselineAuditSanitized'
$expectedPayloadSha256 = '1b90bb0756137f46ff529af043fe798d7f1f9fa1713a4110f17e1d674de81f1c'
$expectedModelSha256 = 'f9018758f35a351f2316a78db99f40141641050c9253d2f6ab7961c24c19201e'

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
            throw 'A controlled CSF audit path traverses a reparse point.'
        }
    }
}

function Assert-RegularFile {
    param([Parameter(Mandatory)][string] $Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw 'A required controlled CSF audit file was not found.'
    }
    Assert-NoExistingReparsePoint -Path $Path
    $attributes = [IO.File]::GetAttributes([IO.Path]::GetFullPath($Path))
    if (($attributes -band [IO.FileAttributes]::Directory) -ne 0 -or
        ($attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw 'A controlled CSF audit input is not a regular file.'
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
        throw 'A local CSF audit configuration or result path is not excluded by .gitignore.'
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
        if ($null -ne $stream) { $stream.Dispose() }
        throw
    } finally {
        if ($null -ne $sha256) { $sha256.Dispose() }
    }
}

function Assert-LockedFileUnchanged {
    param([Parameter(Mandatory)][object] $Identity)

    Assert-RegularFile -Path ([string]$Identity.Path)
    $current = New-Object IO.FileInfo([string]$Identity.Path)
    if ([int64]$current.Length -ne [int64]$Identity.Length -or
        [int64]$current.LastWriteTimeUtc.Ticks -ne
            [int64]$Identity.LastWriteTimeUtcTicks) {
        throw 'A locked controlled CSF audit input changed during execution.'
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

    if ($null -eq $Object) { throw "The $Context JSON object is null." }
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
    if ($number -lt 0) { throw "The $Context value is negative." }
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
        [IO.Path]::IsPathRooted($Value) -or $Value.Contains('\') -or
        $Value.Contains(':')) {
        throw "The $Context value is not a sanitized logical path."
    }
    foreach ($segment in $Value.Split('/')) {
        if ([string]::IsNullOrEmpty($segment) -or $segment -eq '.' -or $segment -eq '..') {
            throw "The $Context value contains an invalid logical path segment."
        }
    }
}

function Assert-IsoUtcTimestamp {
    param(
        [Parameter(Mandatory)][object] $Value,
        [Parameter(Mandatory)][string] $Context
    )

    if ($Value -is [DateTimeOffset]) {
        if ($Value.Offset -ne [TimeSpan]::Zero) {
            throw "The $Context timestamp is not explicitly UTC."
        }
        return
    }
    if ($Value -is [DateTime]) {
        if ($Value.Kind -ne [DateTimeKind]::Utc) {
            throw "The $Context timestamp is not explicitly UTC."
        }
        return
    }

    $text = [string]$Value
    if ($text -notmatch 'Z$') { throw "The $Context timestamp is not explicitly UTC." }
    $parsed = [DateTimeOffset]::MinValue
    if (-not [DateTimeOffset]::TryParse(
        $text,
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
    if ($null -eq $Value) { return }
    if ($Value -is [string]) {
        if ([IO.Path]::IsPathRooted([string]$Value) -or
            ([string]$Value) -match '^[A-Za-z]:[\\/]' -or
            ([string]$Value).StartsWith('\\', [StringComparison]::Ordinal)) {
            throw "The $Context JSON value contains an absolute host path."
        }
        return
    }
    if ($Value -is [System.ValueType]) { return }
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

function Assert-Range {
    param(
        [Parameter(Mandatory)][object] $Range,
        [Parameter(Mandatory)][string] $Unit,
        [Parameter(Mandatory)][int64] $Minimum,
        [Parameter(Mandatory)][int64] $Maximum,
        [Parameter(Mandatory)][string] $Context
    )
    Assert-ExactJsonProperties -Object $Range -Context $Context `
        -Names @('minimum', 'maximum', 'unit')
    if ((Get-NonNegativeInt64 $Range.minimum "$Context.minimum") -ne $Minimum -or
        (Get-NonNegativeInt64 $Range.maximum "$Context.maximum") -ne $Maximum -or
        [string]$Range.unit -cne $Unit) {
        throw "The $Context range changed."
    }
}

function Assert-SanitizedSummary {
    param([Parameter(Mandatory)][object] $Summary)

    Assert-ExactJsonProperties -Object $Summary -Context 'summary' -Names @(
        'schemaVersion', 'manifestType', 'baselineLogicalName', 'auditStatus',
        'sourceVersion', 'directoryFingerprint', 'startedUtc', 'completedUtc',
        'externalManifest', 'csf', 'limitations'
    )
    if ((Get-NonNegativeInt64 $Summary.schemaVersion 'schemaVersion') -ne 1 -or
        [string]$Summary.manifestType -cne $manifestType -or
        [string]$Summary.baselineLogicalName -cne $baselineName -or
        [string]$Summary.auditStatus -cne 'Complete' -or
        [string]::IsNullOrWhiteSpace([string]$Summary.sourceVersion)) {
        throw 'The sanitized CSF audit summary identity is invalid.'
    }
    Assert-LowerSha256 ([string]$Summary.directoryFingerprint) 'directoryFingerprint'
    Assert-IsoUtcTimestamp $Summary.startedUtc 'startedUtc'
    Assert-IsoUtcTimestamp $Summary.completedUtc 'completedUtc'

    Assert-ExactJsonProperties -Object $Summary.externalManifest `
        -Context 'externalManifest' `
        -Names @('schemaVersion', 'cacheRelativePath', 'length', 'sha256')
    if ((Get-NonNegativeInt64 $Summary.externalManifest.schemaVersion 'externalManifest.schemaVersion') -ne 1 -or
        (Get-NonNegativeInt64 $Summary.externalManifest.length 'externalManifest.length') -le 0) {
        throw 'The external CSF manifest reference is invalid.'
    }
    Assert-LogicalPath ([string]$Summary.externalManifest.cacheRelativePath) `
        'externalManifest.cacheRelativePath'
    Assert-LowerSha256 ([string]$Summary.externalManifest.sha256) `
        'externalManifest.sha256'

    $csf = $Summary.csf
    Assert-ExactJsonProperties -Object $csf -Context 'csf' -Names @(
        'logicalName', 'mixId', 'provenance', 'length', 'sha256', 'formatVersion',
        'rawLanguageCode', 'labelRecordCount', 'totalValueCount',
        'normalValueCount', 'extendedValueCount', 'emptyValueCount',
        'duplicateLabelCount', 'maximumValuesPerLabel', 'labelNameLength',
        'mainTextLength', 'extendedTextLength', 'normalizedModelSha256',
        'diagnosticCount'
    )
    if ([string]$csf.logicalName -cne 'ra2md.csf' -or
        [string]$csf.mixId -cne '0xBD835079' -or
        (Get-NonNegativeInt64 $csf.length 'csf.length') -ne 332973 -or
        [string]$csf.sha256 -cne $expectedPayloadSha256 -or
        (Get-NonNegativeInt64 $csf.formatVersion 'csf.formatVersion') -ne 3 -or
        (Get-NonNegativeInt64 $csf.rawLanguageCode 'csf.rawLanguageCode') -ne 9 -or
        (Get-NonNegativeInt64 $csf.labelRecordCount 'csf.labelRecordCount') -ne 5211 -or
        (Get-NonNegativeInt64 $csf.totalValueCount 'csf.totalValueCount') -ne 5211 -or
        (Get-NonNegativeInt64 $csf.normalValueCount 'csf.normalValueCount') -ne 4007 -or
        (Get-NonNegativeInt64 $csf.extendedValueCount 'csf.extendedValueCount') -ne 1204 -or
        (Get-NonNegativeInt64 $csf.emptyValueCount 'csf.emptyValueCount') -ne 4 -or
        (Get-NonNegativeInt64 $csf.duplicateLabelCount 'csf.duplicateLabelCount') -ne 0 -or
        (Get-NonNegativeInt64 $csf.maximumValuesPerLabel 'csf.maximumValuesPerLabel') -ne 1 -or
        [string]$csf.normalizedModelSha256 -cne $expectedModelSha256 -or
        (Get-NonNegativeInt64 $csf.diagnosticCount 'csf.diagnosticCount') -ne 0) {
        throw 'The golden CSF identity or parsed statistics changed.'
    }
    Assert-LowerSha256 ([string]$csf.sha256) 'csf.sha256'
    Assert-LowerSha256 ([string]$csf.normalizedModelSha256) `
        'csf.normalizedModelSha256'
    Assert-Range $csf.labelNameLength 'ascii-bytes' 6 31 'csf.labelNameLength'
    Assert-Range $csf.mainTextLength 'utf16-code-units' 0 187 'csf.mainTextLength'
    Assert-Range $csf.extendedTextLength 'ascii-bytes' 7 8 'csf.extendedTextLength'

    Assert-ExactJsonProperties -Object $csf.provenance -Context 'csf.provenance' `
        -Names @('sourceId', 'rootArchive', 'layers')
    if ([string]$csf.provenance.sourceId -cne $baselineName -or
        [string]$csf.provenance.rootArchive -cne 'langmd.mix') {
        throw 'The golden CSF source identity changed.'
    }
    $layers = @($csf.provenance.layers)
    if ($layers.Count -ne 1) { throw 'The golden CSF MIX chain changed.' }
    Assert-ExactJsonProperties -Object $layers[0] -Context 'csf.provenance.layer' `
        -Names @('archive', 'entryId', 'resolvedName')
    if ([string]$layers[0].archive -cne 'langmd.mix' -or
        [string]$layers[0].entryId -cne '0xBD835079' -or
        [string]$layers[0].resolvedName -cne 'ra2md.csf') {
        throw 'The golden CSF provenance chain changed.'
    }

    $limitations = @($Summary.limitations)
    if ($limitations.Count -eq 0) { throw 'The sanitized CSF summary omits limitations.' }
    foreach ($limitation in $limitations) {
        if (-not ($limitation -is [string]) -or
            [string]::IsNullOrWhiteSpace([string]$limitation)) {
            throw 'A sanitized CSF audit limitation is invalid.'
        }
    }
    Assert-NoAbsolutePathInJsonValue $Summary 'summary'
}

if ([IO.Path]::DirectorySeparatorChar -ne '\') {
    throw 'The controlled YR1001_ProjectBaseline CSF audit supports Windows only.'
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
    throw 'The supplied project directory is not the RA2YR Git repository root.'
}
Assert-GitIgnored $resolvedProjectRoot $resolvedConfigurationPath
if (Test-Path -LiteralPath (Join-Path $resolvedProjectRoot 'Temp\UnityLockfile')) {
    throw 'The Unity project is open. Close the Editor before the CSF baseline audit.'
}
$versionFile = Join-Path $resolvedProjectRoot 'ProjectSettings\ProjectVersion.txt'
Assert-RegularFile $versionFile
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
    -not $editorVersion.StartsWith($expectedUnityVersion, [StringComparison]::Ordinal)) {
    throw 'The supplied Unity Editor does not match the project version.'
}

$cachePath = Get-ConfigurationCachePath $resolvedConfigurationPath
if (Test-InsideOrEqual $cachePath $resolvedProjectRoot) {
    throw 'The complete CSF audit manifest cache must remain outside the repository.'
}
Assert-NoExistingReparsePoint $cachePath
$resultsRoot = Join-Path $resolvedProjectRoot 'TestResults'
Assert-NoExistingReparsePoint $resultsRoot
Assert-GitIgnored $resolvedProjectRoot $resultsRoot
[IO.Directory]::CreateDirectory($resultsRoot) | Out-Null
$runId = (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssfffZ') + '-' +
    [Guid]::NewGuid().ToString('N')
$runRoot = Join-Path $resultsRoot $runId
if (Test-Path -LiteralPath $runRoot) {
    throw 'The unique CSF audit result directory already exists.'
}
[IO.Directory]::CreateDirectory($runRoot) | Out-Null
Assert-NoExistingReparsePoint $runRoot
$summaryPath = Join-Path $runRoot 'wp02e-csf-project-baseline-summary.json'
$logPath = Join-Path $runRoot 'unity.log'

$lockedFiles = New-Object Collections.Generic.List[object]
$process = $null
$unityExitCode = $null
try {
    $configurationIdentity = Open-LockedFileIdentity $resolvedConfigurationPath
    $lockedFiles.Add($configurationIdentity)
    $arguments = @(
        '-batchmode', '-nographics', '-quit',
        '-projectPath', (Quote-ProcessArgument $resolvedProjectRoot),
        '-executeMethod', 'RA2YR.Editor.CsfProjectBaselineAuditCommand.Run',
        '-ra2yrExternalContentConfig', (Quote-ProcessArgument $resolvedConfigurationPath),
        '-ra2yrSummaryOutput', (Quote-ProcessArgument $summaryPath),
        '-logFile', (Quote-ProcessArgument $logPath)
    )
    $process = Start-Process -FilePath $resolvedEditorPath -ArgumentList $arguments `
        -PassThru -WindowStyle Hidden
    if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
        $process.Kill(); $process.WaitForExit(); $process.Refresh()
        $unityExitCode = $process.ExitCode
        throw "Unity CSF baseline audit timed out; terminated process exit code: $unityExitCode."
    }
    $process.Refresh()
    $unityExitCode = $process.ExitCode
    Assert-LockedFileUnchanged $configurationIdentity
    if ($unityExitCode -ne 0) {
        throw "Unity CSF baseline audit process exited with code $unityExitCode."
    }
} finally {
    if ($null -ne $process) { $process.Dispose() }
    for ($index = $lockedFiles.Count - 1; $index -ge 0; $index--) {
        $lockedFiles[$index].Stream.Dispose()
    }
}

Assert-RegularFile $summaryPath
$summaryInfo = New-Object IO.FileInfo($summaryPath)
if ($summaryInfo.Length -le 0 -or $summaryInfo.Length -gt 1MB) {
    throw 'The sanitized CSF audit summary has an invalid length.'
}
try {
    $summaryText = [IO.File]::ReadAllText(
        $summaryPath,
        (New-Object Text.UTF8Encoding($false, $true)))
    $summary = $summaryText | ConvertFrom-Json
} catch {
    throw 'The sanitized CSF audit summary is not valid strict UTF-8 JSON.'
}
Assert-SanitizedSummary $summary

$manifestRelativePath = [string]$summary.externalManifest.cacheRelativePath
$manifestPath = [IO.Path]::GetFullPath((Join-Path `
    $cachePath `
    $manifestRelativePath.Replace('/', '\')))
if (-not (Test-InsideOrEqual $manifestPath $cachePath) -or
    ([IO.Path]::GetFullPath($manifestPath).TrimEnd('\')).Equals(
        [IO.Path]::GetFullPath($cachePath).TrimEnd('\'),
        [StringComparison]::OrdinalIgnoreCase)) {
    throw 'The external CSF audit manifest escaped its configured cache boundary.'
}
Assert-NoExistingReparsePoint $manifestPath
$externalManifestIdentity = Open-LockedFileIdentity $manifestPath
try {
    if ([int64]$externalManifestIdentity.Length -ne
            [int64]$summary.externalManifest.length -or
        -not ([string]$externalManifestIdentity.Sha256).Equals(
            [string]$summary.externalManifest.sha256,
            [StringComparison]::OrdinalIgnoreCase)) {
        throw 'The external CSF audit manifest does not match the sanitized summary.'
    }
} finally {
    $externalManifestIdentity.Stream.Dispose()
}
$summaryIdentity = Open-LockedFileIdentity $summaryPath
try { $summarySha256 = [string]$summaryIdentity.Sha256 }
finally { $summaryIdentity.Stream.Dispose() }

"Unity process exit code: $unityExitCode"
"Audit status: $($summary.auditStatus)"
"Validated CSF documents: 1"
"Labels: $($summary.csf.labelRecordCount)"
"Values: $($summary.csf.totalValueCount)"
"External manifest SHA-256: $($summary.externalManifest.sha256)"
"Sanitized summary SHA-256: $summarySha256"
"Sanitized summary: TestResults/$runId/wp02e-csf-project-baseline-summary.json"
