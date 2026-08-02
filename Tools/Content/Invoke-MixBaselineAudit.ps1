[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $UnityEditorPath,

    [Parameter(Mandatory)]
    [string] $XccMixerPath,

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
$expectedXccMixerSha256 =
    'DD4E54956874BE8B995BE9B046B7302BF0F40B86A7C8BEED4A94165C6B50E7ED'
$expectedXccDatabaseSha256 =
    'C76F529AF17CBE516E85AA4DDDCE614CF0AD98A8590208C71FBE3A047FB77AB8'
$xccDatabaseFileName = 'global mix database.dat'

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
            throw 'A controlled MIX audit path traverses a reparse point.'
        }
    }
}

function Assert-RegularFile {
    param([Parameter(Mandatory)][string] $Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw 'A required controlled MIX audit file was not found.'
    }
    Assert-NoExistingReparsePoint -Path $Path
    $attributes = [IO.File]::GetAttributes([IO.Path]::GetFullPath($Path))
    if (($attributes -band [IO.FileAttributes]::Directory) -ne 0 -or
        ($attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw 'A controlled MIX audit input is not a regular file.'
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
        throw 'A local configuration or result path is not excluded by .gitignore.'
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
        throw 'A locked controlled MIX audit input changed during execution.'
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

    if ($Value -notmatch '^[0-9a-f]{64}$') {
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
    if ($Value -is [System.Collections.IEnumerable] -and
        -not ($Value -is [pscustomobject])) {
        $index = 0
        foreach ($item in $Value) {
            Assert-NoAbsolutePathInJsonValue -Value $item -Context "$Context[$index]"
            $index++
        }
        return
    }
    foreach ($property in $Value.PSObject.Properties) {
        Assert-NoAbsolutePathInJsonValue `
            -Value $property.Value `
            -Context "$Context.$($property.Name)"
    }
}

function Assert-SanitizedSummary {
    param(
        [Parameter(Mandatory)][object] $Summary,
        [Parameter(Mandatory)][object] $XccDatabaseIdentity
    )

    Assert-ExactJsonProperties -Object $Summary -Context 'summary' -Names @(
        'schemaVersion', 'baselineLogicalName', 'auditStatus', 'sourceVersion',
        'directoryFingerprint', 'startedUtc', 'completedUtc', 'externalManifest',
        'xccGlobalNameDatabase', 'rootArchives', 'mountedArchives', 'entries',
        'diagnostics', 'targets', 'limitations'
    )
    if ((Get-NonNegativeInt64 -Value $Summary.schemaVersion -Context 'schemaVersion') -ne 1 -or
        [string]$Summary.baselineLogicalName -ne $baselineName -or
        [string]::IsNullOrWhiteSpace([string]$Summary.sourceVersion)) {
        throw 'The sanitized MIX audit summary identity is invalid.'
    }
    Assert-LowerSha256 -Value ([string]$Summary.directoryFingerprint) `
        -Context 'directoryFingerprint'
    if ([string]$Summary.auditStatus -notin @('Complete', 'CompleteWithArchiveFailures')) {
        throw 'The sanitized MIX audit status is invalid.'
    }

    $started = [DateTimeOffset]::MinValue
    $completed = [DateTimeOffset]::MinValue
    if (-not [DateTimeOffset]::TryParse(
            [string]$Summary.startedUtc,
            [Globalization.CultureInfo]::InvariantCulture,
            [Globalization.DateTimeStyles]::RoundtripKind,
            [ref]$started) -or
        -not [DateTimeOffset]::TryParse(
            [string]$Summary.completedUtc,
            [Globalization.CultureInfo]::InvariantCulture,
            [Globalization.DateTimeStyles]::RoundtripKind,
            [ref]$completed) -or
        $completed -lt $started) {
        throw 'The sanitized MIX audit timestamps are invalid.'
    }

    Assert-ExactJsonProperties -Object $Summary.externalManifest `
        -Context 'externalManifest' `
        -Names @('schemaVersion', 'cacheRelativePath', 'length', 'sha256')
    if ((Get-NonNegativeInt64 -Value $Summary.externalManifest.schemaVersion `
            -Context 'externalManifest.schemaVersion') -ne 1) {
        throw 'The external manifest schema version is invalid.'
    }
    Assert-LogicalPath -Value ([string]$Summary.externalManifest.cacheRelativePath) `
        -Context 'externalManifest.cacheRelativePath'
    [void](Get-NonNegativeInt64 -Value $Summary.externalManifest.length `
        -Context 'externalManifest.length')
    Assert-LowerSha256 -Value ([string]$Summary.externalManifest.sha256) `
        -Context 'externalManifest.sha256'

    Assert-ExactJsonProperties -Object $Summary.xccGlobalNameDatabase `
        -Context 'xccGlobalNameDatabase' -Names @('length', 'sha256')
    $databaseLength = Get-NonNegativeInt64 `
        -Value $Summary.xccGlobalNameDatabase.length `
        -Context 'xccGlobalNameDatabase.length'
    Assert-LowerSha256 -Value ([string]$Summary.xccGlobalNameDatabase.sha256) `
        -Context 'xccGlobalNameDatabase.sha256'
    if ($databaseLength -ne [int64]$XccDatabaseIdentity.Length -or
        -not ([string]$Summary.xccGlobalNameDatabase.sha256).Equals(
            ([string]$XccDatabaseIdentity.Sha256).ToLowerInvariant(),
            [StringComparison]::Ordinal)) {
        throw 'The sanitized summary XCC name database identity does not match the locked input.'
    }

    Assert-ExactJsonProperties -Object $Summary.rootArchives -Context 'rootArchives' `
        -Names @('count', 'totalBytes', 'parsed', 'failed')
    $rootCount = Get-NonNegativeInt64 -Value $Summary.rootArchives.count `
        -Context 'rootArchives.count'
    $rootBytes = Get-NonNegativeInt64 -Value $Summary.rootArchives.totalBytes `
        -Context 'rootArchives.totalBytes'
    $rootParsed = Get-NonNegativeInt64 -Value $Summary.rootArchives.parsed `
        -Context 'rootArchives.parsed'
    $rootFailed = Get-NonNegativeInt64 -Value $Summary.rootArchives.failed `
        -Context 'rootArchives.failed'
    if ($rootCount -le 0 -or $rootBytes -le 0 -or
        $rootParsed + $rootFailed -ne $rootCount -or
        ([string]$Summary.auditStatus -eq 'Complete' -and $rootFailed -ne 0) -or
        ([string]$Summary.auditStatus -eq 'CompleteWithArchiveFailures' -and
            $rootFailed -eq 0)) {
        throw 'The sanitized MIX root archive counts are inconsistent.'
    }

    Assert-ExactJsonProperties -Object $Summary.mountedArchives `
        -Context 'mountedArchives' -Names @(
            'count', 'classicHeader', 'extendedHeader', 'encryptedDirectory',
            'checksum', 'nestedCount', 'maximumNestedDepth'
        )
    $mountedCount = Get-NonNegativeInt64 -Value $Summary.mountedArchives.count `
        -Context 'mountedArchives.count'
    $classicCount = Get-NonNegativeInt64 -Value $Summary.mountedArchives.classicHeader `
        -Context 'mountedArchives.classicHeader'
    $extendedCount = Get-NonNegativeInt64 -Value $Summary.mountedArchives.extendedHeader `
        -Context 'mountedArchives.extendedHeader'
    $encryptedCount = Get-NonNegativeInt64 `
        -Value $Summary.mountedArchives.encryptedDirectory `
        -Context 'mountedArchives.encryptedDirectory'
    $checksumCount = Get-NonNegativeInt64 -Value $Summary.mountedArchives.checksum `
        -Context 'mountedArchives.checksum'
    $nestedCount = Get-NonNegativeInt64 -Value $Summary.mountedArchives.nestedCount `
        -Context 'mountedArchives.nestedCount'
    [void](Get-NonNegativeInt64 -Value $Summary.mountedArchives.maximumNestedDepth `
        -Context 'mountedArchives.maximumNestedDepth')
    if ($classicCount + $extendedCount -ne $mountedCount -or
        $encryptedCount -gt $mountedCount -or
        $checksumCount -gt $mountedCount -or
        $nestedCount -gt $mountedCount) {
        throw 'The sanitized mounted archive counts are inconsistent.'
    }

    Assert-ExactJsonProperties -Object $Summary.entries -Context 'entries' `
        -Names @('count', 'unknownIdCount')
    $entryCount = Get-NonNegativeInt64 -Value $Summary.entries.count `
        -Context 'entries.count'
    $unknownCount = Get-NonNegativeInt64 -Value $Summary.entries.unknownIdCount `
        -Context 'entries.unknownIdCount'
    if ($unknownCount -gt $entryCount) {
        throw 'The sanitized MIX entry counts are inconsistent.'
    }

    $diagnosticCodes = New-Object 'Collections.Generic.HashSet[string]' `
        ([StringComparer]::Ordinal)
    foreach ($diagnostic in @($Summary.diagnostics)) {
        Assert-ExactJsonProperties -Object $diagnostic -Context 'diagnostic' `
            -Names @('code', 'count')
        if ([string]::IsNullOrWhiteSpace([string]$diagnostic.code) -or
            -not $diagnosticCodes.Add([string]$diagnostic.code) -or
            (Get-NonNegativeInt64 -Value $diagnostic.count `
                -Context 'diagnostic.count') -le 0) {
            throw 'The sanitized MIX diagnostics are invalid.'
        }
    }

    $expectedTargets = @(
        'isotem.pal', 'temperat.pal', 'unittem.pal', 'rulesmd.ini',
        'artmd.ini', 'ai.ini', 'ra2md.csf'
    )
    $observedTargets = New-Object 'Collections.Generic.HashSet[string]' `
        ([StringComparer]::OrdinalIgnoreCase)
    $targets = @($Summary.targets)
    if ($targets.Count -ne $expectedTargets.Count) {
        throw 'The sanitized MIX audit does not contain exactly seven target records.'
    }
    foreach ($target in $targets) {
        Assert-ExactJsonProperties -Object $target -Context 'target' -Names @(
            'logicalName', 'mixId', 'status', 'matchCount', 'diagnosticCount', 'matches'
        )
        $logicalName = [string]$target.logicalName
        Assert-LogicalPath -Value $logicalName -Context 'target.logicalName'
        if ($expectedTargets -notcontains $logicalName -or
            -not $observedTargets.Add($logicalName) -or
            [string]$target.mixId -notmatch '^0x[0-9A-F]{8}$' -or
            [string]$target.status -notin @('found', 'not-found', 'ambiguous')) {
            throw 'A sanitized MIX target identity is invalid.'
        }
        $matches = @($target.matches)
        $matchCount = Get-NonNegativeInt64 -Value $target.matchCount `
            -Context 'target.matchCount'
        $diagnosticCount = Get-NonNegativeInt64 -Value $target.diagnosticCount `
            -Context 'target.diagnosticCount'
        if ($matchCount -ne $matches.Count -or
            ([string]$target.status -eq 'found' -and
                ($matchCount -ne 1 -or $diagnosticCount -ne 0)) -or
            ([string]$target.status -eq 'not-found' -and
                ($matchCount -ne 0 -or $diagnosticCount -lt 1)) -or
            ([string]$target.status -eq 'ambiguous' -and $diagnosticCount -lt 1)) {
            throw 'A sanitized MIX target count or status is inconsistent.'
        }
        foreach ($match in $matches) {
            Assert-ExactJsonProperties -Object $match -Context 'target.match' -Names @(
                'storageKind', 'length', 'sha256', 'encryptedChain', 'provenance'
            )
            if ([string]$match.storageKind -notin @('Directory', 'MixArchive') -or
                -not ($match.encryptedChain -is [bool])) {
                throw 'A sanitized MIX target match is invalid.'
            }
            [void](Get-NonNegativeInt64 -Value $match.length -Context 'target.match.length')
            Assert-LowerSha256 -Value ([string]$match.sha256) `
                -Context 'target.match.sha256'
            if ([string]$match.storageKind -eq 'Directory') {
                if ($null -ne $match.provenance) {
                    throw 'A directory target match unexpectedly carries MIX provenance.'
                }
                continue
            }
            Assert-ExactJsonProperties -Object $match.provenance `
                -Context 'target.match.provenance' `
                -Names @('sourceId', 'rootArchive', 'chain')
            if ([string]$match.provenance.sourceId -ne $baselineName) {
                throw 'A MIX target provenance source is not the approved baseline.'
            }
            Assert-LogicalPath -Value ([string]$match.provenance.rootArchive) `
                -Context 'target.match.provenance.rootArchive'
            $chain = @($match.provenance.chain)
            if ($chain.Count -eq 0) {
                throw 'A MIX target provenance chain is empty.'
            }
            foreach ($step in $chain) {
                Assert-ExactJsonProperties -Object $step -Context 'provenance.step' `
                    -Names @('archive', 'entryId', 'resolvedName')
                Assert-LogicalPath -Value ([string]$step.archive) `
                    -Context 'provenance.step.archive'
                if ([string]$step.entryId -notmatch '^0x[0-9A-F]{8}$') {
                    throw 'A MIX provenance entry ID is invalid.'
                }
                if ($null -ne $step.resolvedName) {
                    Assert-LogicalPath -Value ([string]$step.resolvedName) `
                        -Context 'provenance.step.resolvedName'
                }
            }
        }
    }

    $limitations = @($Summary.limitations)
    if ($limitations.Count -eq 0) {
        throw 'The sanitized MIX audit summary omits its limitations.'
    }
    foreach ($limitation in $limitations) {
        if (-not ($limitation -is [string]) -or
            [string]::IsNullOrWhiteSpace([string]$limitation)) {
            throw 'A sanitized MIX audit limitation is invalid.'
        }
    }
    Assert-NoAbsolutePathInJsonValue -Value $Summary -Context 'summary'
}

if ([IO.Path]::DirectorySeparatorChar -ne '\') {
    throw 'The controlled YR1001_ProjectBaseline MIX audit supports Windows only.'
}
if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = Join-Path $PSScriptRoot '..\..'
}

$resolvedProjectRoot = [IO.Path]::GetFullPath($ProjectRoot).TrimEnd('\')
$resolvedEditorPath = [IO.Path]::GetFullPath($UnityEditorPath)
$resolvedXccMixerPath = [IO.Path]::GetFullPath($XccMixerPath)
if ([string]::IsNullOrWhiteSpace($ConfigurationPath)) {
    $ConfigurationPath = Join-Path $resolvedProjectRoot 'Config\ExternalContent.local.xml'
}
$resolvedConfigurationPath = [IO.Path]::GetFullPath($ConfigurationPath)
$resolvedXccDatabasePath = [IO.Path]::GetFullPath((Join-Path `
    ([IO.Path]::GetDirectoryName($resolvedXccMixerPath)) `
    $xccDatabaseFileName))

if (-not (Test-Path -LiteralPath $resolvedProjectRoot -PathType Container)) {
    throw 'The Unity project root was not found.'
}
Assert-NoExistingReparsePoint -Path $resolvedProjectRoot
Assert-RegularFile -Path $resolvedEditorPath
Assert-RegularFile -Path $resolvedXccMixerPath
Assert-RegularFile -Path $resolvedXccDatabasePath
Assert-RegularFile -Path $resolvedConfigurationPath
if (-not ([IO.Path]::GetFileName($resolvedXccMixerPath)).Equals(
        'XCC Mixer.exe',
        [StringComparison]::OrdinalIgnoreCase)) {
    throw 'The supplied XCC reference tool is not named XCC Mixer.exe.'
}
if (Test-InsideOrEqual -Candidate $resolvedXccMixerPath -Root $resolvedProjectRoot) {
    throw 'The XCC reference tool must remain outside the RA2YR repository.'
}
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
    throw 'The Unity project is open. Close the Editor before the MIX baseline audit.'
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
    throw 'The complete MIX audit manifest cache must remain outside the repository.'
}
Assert-NoExistingReparsePoint -Path $cachePath

$resultsRoot = Join-Path $resolvedProjectRoot 'TestResults'
Assert-NoExistingReparsePoint -Path $resultsRoot
Assert-GitIgnored -RepositoryRoot $resolvedProjectRoot -Path $resultsRoot
[IO.Directory]::CreateDirectory($resultsRoot) | Out-Null
$runId = (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssfffZ') + '-' +
    [Guid]::NewGuid().ToString('N')
$runRoot = Join-Path $resultsRoot $runId
[IO.Directory]::CreateDirectory($runRoot) | Out-Null
Assert-NoExistingReparsePoint -Path $runRoot
$summaryPath = Join-Path $runRoot 'wp02c-mix-baseline-summary.json'
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
    $xccMixerIdentity = Open-LockedFileIdentity -Path $resolvedXccMixerPath
    $lockedFiles.Add($xccMixerIdentity)
    $xccDatabaseIdentity = Open-LockedFileIdentity -Path $resolvedXccDatabasePath
    $lockedFiles.Add($xccDatabaseIdentity)

    if (-not ([string]$xccMixerIdentity.Sha256).Equals(
            $expectedXccMixerSha256,
            [StringComparison]::OrdinalIgnoreCase)) {
        throw 'The supplied XCC Mixer does not match the approved SHA-256.'
    }
    if (-not ([string]$xccDatabaseIdentity.Sha256).Equals(
            $expectedXccDatabaseSha256,
            [StringComparison]::OrdinalIgnoreCase)) {
        throw 'The adjacent XCC global name database does not match the approved SHA-256.'
    }

    $arguments = @(
        '-batchmode',
        '-nographics',
        '-quit',
        '-projectPath', (Quote-ProcessArgument -Value $resolvedProjectRoot),
        '-executeMethod', 'RA2YR.Editor.MixBaselineAuditCommand.Run',
        '-ra2yrExternalContentConfig',
            (Quote-ProcessArgument -Value $resolvedConfigurationPath),
        '-ra2yrXccGlobalNameDatabase',
            (Quote-ProcessArgument -Value $resolvedXccDatabasePath),
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
        throw "Unity MIX baseline audit timed out; terminated process exit code: $unityExitCode."
    }
    $process.Refresh()
    $unityExitCode = $process.ExitCode

    Assert-LockedFileUnchanged -Identity $configurationIdentity
    Assert-LockedFileUnchanged -Identity $xccMixerIdentity
    Assert-LockedFileUnchanged -Identity $xccDatabaseIdentity
    if ($unityExitCode -ne 0) {
        throw "Unity MIX baseline audit process exited with code $unityExitCode."
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
if ($summaryInfo.Length -le 0 -or $summaryInfo.Length -gt 8MB) {
    throw 'The sanitized MIX audit summary has an invalid length.'
}
try {
    $summaryText = [IO.File]::ReadAllText(
        $summaryPath,
        (New-Object Text.UTF8Encoding($false, $true)))
    $summary = $summaryText | ConvertFrom-Json
} catch {
    throw 'The sanitized MIX audit summary is not valid strict UTF-8 JSON.'
}
Assert-SanitizedSummary -Summary $summary -XccDatabaseIdentity $xccDatabaseIdentity

$manifestRelativePath = [string]$summary.externalManifest.cacheRelativePath
$manifestPath = [IO.Path]::GetFullPath((Join-Path `
    $cachePath `
    $manifestRelativePath.Replace('/', '\')))
if (-not (Test-InsideOrEqual -Candidate $manifestPath -Root $cachePath) -or
    ([IO.Path]::GetFullPath($manifestPath).TrimEnd('\')).Equals(
        [IO.Path]::GetFullPath($cachePath).TrimEnd('\'),
        [StringComparison]::OrdinalIgnoreCase)) {
    throw 'The external MIX audit manifest escaped its configured cache boundary.'
}
Assert-NoExistingReparsePoint -Path $manifestPath
$externalManifestIdentity = Open-LockedFileIdentity -Path $manifestPath
try {
    if ([int64]$externalManifestIdentity.Length -ne
            [int64]$summary.externalManifest.length -or
        -not ([string]$externalManifestIdentity.Sha256).Equals(
            [string]$summary.externalManifest.sha256,
            [StringComparison]::OrdinalIgnoreCase)) {
        throw 'The external MIX audit manifest does not match the sanitized summary.'
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
"XCC Mixer SHA-256: $expectedXccMixerSha256"
"XCC global name database SHA-256: $expectedXccDatabaseSha256"
"Audit status: $($summary.auditStatus)"
"Root MIX archives: $($summary.rootArchives.count)"
"Parsed root MIX archives: $($summary.rootArchives.parsed)"
"Failed root MIX archives: $($summary.rootArchives.failed)"
"Mounted MIX archives: $($summary.mountedArchives.count)"
"MIX entries: $($summary.entries.count)"
"External manifest SHA-256: $($summary.externalManifest.sha256)"
"Sanitized summary SHA-256: $summarySha256"
"Sanitized summary: TestResults/$runId/wp02c-mix-baseline-summary.json"
