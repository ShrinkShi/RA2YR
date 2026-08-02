[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('Prepare', 'VerifyXccCreated', 'VerifyXccExtractions')]
    [string] $Mode,

    [Parameter(Mandatory)]
    [ValidatePattern('^[a-z][a-z0-9_-]{0,63}$')]
    [string] $CaseId,

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
    [int] $TimeoutSeconds = 1800
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$expectedUnityVersion = '2022.3.60f1c1'
$expectedXccMixerSha256 =
    'DD4E54956874BE8B995BE9B046B7302BF0F40B86A7C8BEED4A94165C6B50E7ED'
$allowedModes = @('Prepare', 'VerifyXccCreated', 'VerifyXccExtractions')
$expectedStageByMode = @{
    Prepare = 'PrepareInternalContract'
    VerifyXccCreated = 'ValidateStagedCreatedCandidate'
    VerifyXccExtractions = 'ValidateStagedExtractionCandidates'
}
$allowedDiagnosticCodes = @(
    'InvalidCaseId', 'UnsafeCacheBoundary', 'CaseAlreadyExists', 'CaseMissing',
    'RequiredInputMissing', 'RequiredInputRejected', 'ArchiveBuildFailed',
    'ArchiveReadFailed', 'ArchiveMismatch', 'PayloadMismatch',
    'AtomicPublishFailed', 'ManifestWriteFailed', 'CleanupFailed',
    'PublishedArtifactMismatch', 'ExtractionBudgetExceeded', 'ExtractionChanged'
)

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

function Test-PathsOverlap {
    param(
        [Parameter(Mandatory)][string] $First,
        [Parameter(Mandatory)][string] $Second
    )

    return (Test-InsideOrEqual -Candidate $First -Root $Second) -or
        (Test-InsideOrEqual -Candidate $Second -Root $First)
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
            throw 'A controlled XCC interop path traverses a reparse point.'
        }
    }
}

function Assert-RegularFile {
    param([Parameter(Mandatory)][string] $Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw 'A required controlled XCC interop file was not found.'
    }
    Assert-NoExistingReparsePoint -Path $Path
    $attributes = [IO.File]::GetAttributes([IO.Path]::GetFullPath($Path))
    if (($attributes -band [IO.FileAttributes]::Directory) -ne 0 -or
        ($attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw 'A controlled XCC interop input is not a regular file.'
    }
}

function Ensure-SafeDirectory {
    param([Parameter(Mandatory)][string] $Path)

    $fullPath = [IO.Path]::GetFullPath($Path)
    Assert-NoExistingReparsePoint -Path $fullPath
    if (Test-Path -LiteralPath $fullPath -PathType Leaf) {
        throw 'A required external-cache directory is occupied by a file.'
    }
    [IO.Directory]::CreateDirectory($fullPath) | Out-Null
    Assert-NoExistingReparsePoint -Path $fullPath
    $attributes = [IO.File]::GetAttributes($fullPath)
    if (($attributes -band [IO.FileAttributes]::Directory) -eq 0 -or
        ($attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw 'A required external-cache path is not a regular directory.'
    }
}

function Assert-GitIgnored {
    param(
        [Parameter(Mandatory)][string] $RepositoryRoot,
        [Parameter(Mandatory)][string] $Path
    )

    if (-not (Test-InsideOrEqual -Candidate $Path -Root $RepositoryRoot)) {
        throw 'Only repository paths can be checked against repository ignores.'
    }
    $root = [IO.Path]::GetFullPath($RepositoryRoot).TrimEnd('\')
    $relative = [IO.Path]::GetFullPath($Path).Substring($root.Length).TrimStart('\')
    $relative = $relative.Replace('\', '/')
    & git -C $root check-ignore --quiet -- $relative 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw 'The local external-content configuration is not excluded by .gitignore.'
    }
}

function Open-LockedFileIdentity {
    param(
        [Parameter(Mandatory)][string] $Path,
        [Parameter(Mandatory)][int64] $MaximumBytes
    )

    if ($MaximumBytes -lt 0) {
        throw 'A non-negative locked-file size budget is required.'
    }

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
        if ([int64]$stream.Length -ne [int64]$before.Length -or
            [int64]$stream.Length -gt $MaximumBytes) {
            throw 'A controlled XCC interop input changed or exceeded its size budget while opening.'
        }
        $sha256 = [Security.Cryptography.SHA256]::Create()
        $hash = [BitConverter]::ToString($sha256.ComputeHash($stream)).Replace('-', '')
        if ([int64]$stream.Position -ne [int64]$before.Length -or
            [int64]$stream.Length -ne [int64]$before.Length) {
            throw 'A controlled XCC interop input changed during bounded hashing.'
        }
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
        throw 'A locked controlled XCC interop input changed during execution.'
    }
}

function Resolve-ConfigurationPathValue {
    param(
        [Parameter(Mandatory)][string] $Value,
        [Parameter(Mandatory)][string] $ConfigurationFile
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        throw 'The external-content configuration contains an empty path.'
    }
    if ([IO.Path]::IsPathRooted($Value)) {
        return [IO.Path]::GetFullPath($Value)
    }
    return [IO.Path]::GetFullPath((Join-Path `
        ([IO.Path]::GetDirectoryName($ConfigurationFile)) `
        $Value))
}

function Get-ConfigurationBoundaries {
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
        $document.DocumentElement.Name -ne 'ExternalContent' -or
        [string]$document.DocumentElement.GetAttribute('schemaVersion') -ne '1') {
        throw 'The local external-content configuration has an unsupported schema.'
    }
    $cachePath = Resolve-ConfigurationPathValue `
        -Value ([string]$document.DocumentElement.GetAttribute('cachePath')) `
        -ConfigurationFile $Path
    $sourcePaths = New-Object Collections.Generic.List[string]
    $sourcesNode = $document.DocumentElement.SelectSingleNode('Sources')
    if ($null -eq $sourcesNode) {
        throw 'The local external-content configuration has no Sources element.'
    }
    foreach ($node in $sourcesNode.ChildNodes) {
        if ($node.NodeType -ne [Xml.XmlNodeType]::Element) {
            continue
        }
        if ($node.Name -ne 'Source') {
            throw 'The local external-content configuration contains an unexpected source node.'
        }
        $sourcePaths.Add((Resolve-ConfigurationPathValue `
            -Value ([string]$node.GetAttribute('path')) `
            -ConfigurationFile $Path))
    }
    if ($sourcePaths.Count -eq 0) {
        throw 'The local external-content configuration declares no content sources.'
    }
    return [pscustomobject]@{
        CachePath = $cachePath
        SourcePaths = $sourcePaths.ToArray()
    }
}

function Assert-ConfigurationBoundaries {
    param(
        [Parameter(Mandatory)][object] $Boundaries,
        [Parameter(Mandatory)][string] $RepositoryRoot
    )

    $cachePath = [IO.Path]::GetFullPath([string]$Boundaries.CachePath)
    if ($cachePath.TrimEnd('\').Equals(
            [IO.Path]::GetPathRoot($cachePath).TrimEnd('\'),
            [StringComparison]::OrdinalIgnoreCase) -or
        (Test-PathsOverlap -First $cachePath -Second $RepositoryRoot)) {
        throw 'The configured external cache has an unsafe repository boundary.'
    }
    Assert-NoExistingReparsePoint -Path $cachePath
    if (Test-Path -LiteralPath $cachePath -PathType Leaf) {
        throw 'The configured external cache is occupied by a file.'
    }
    foreach ($sourcePath in $Boundaries.SourcePaths) {
        Assert-NoExistingReparsePoint -Path $sourcePath
        if (Test-PathsOverlap -First $cachePath -Second $sourcePath) {
            throw 'The configured external cache overlaps an external content source.'
        }
        if (Test-PathsOverlap -First $sourcePath -Second $RepositoryRoot) {
            throw 'An external content source overlaps the repository.'
        }
    }
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

function Assert-Sha256 {
    param(
        [Parameter(Mandatory)][string] $Value,
        [Parameter(Mandatory)][string] $Context
    )

    if ($Value -notmatch '^[0-9A-Fa-f]{64}$') {
        throw "The $Context value is not a SHA-256."
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
    foreach ($segment in $Value.Split('/')) {
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
        Assert-NoAbsolutePathInJsonValue `
            -Value $property.Value `
            -Context "$Context.$($property.Name)"
    }
}

function Assert-InteropResult {
    param(
        [Parameter(Mandatory)][object] $Result,
        [Parameter(Mandatory)][string] $ExpectedMode,
        [Parameter(Mandatory)][string] $ExpectedCaseId
    )

    Assert-ExactJsonProperties -Object $Result -Context 'interop result' -Names @(
        'schemaVersion', 'synthetic', 'mode', 'stage', 'caseId',
        'realXccExecutionEvidence', 'success', 'artifactCount',
        'diagnosticCount', 'artifacts', 'diagnostics'
    )
    if ((Get-NonNegativeInt64 -Value $Result.schemaVersion -Context 'schemaVersion') -ne 1 -or
        -not ($Result.synthetic -is [bool]) -or -not [bool]$Result.synthetic -or
        [string]$Result.mode -cne $ExpectedMode -or
        [string]$Result.stage -cne [string]$expectedStageByMode[$ExpectedMode] -or
        [string]$Result.caseId -cne $ExpectedCaseId -or
        -not ($Result.realXccExecutionEvidence -is [bool]) -or
        [bool]$Result.realXccExecutionEvidence -or
        -not ($Result.success -is [bool])) {
        throw 'The sanitized interop result identity is invalid.'
    }

    $artifacts = @($Result.artifacts)
    $diagnostics = @($Result.diagnostics)
    $artifactCount = Get-NonNegativeInt64 -Value $Result.artifactCount `
        -Context 'artifactCount'
    $diagnosticCount = Get-NonNegativeInt64 -Value $Result.diagnosticCount `
        -Context 'diagnosticCount'
    if ($artifactCount -ne $artifacts.Count -or
        $diagnosticCount -ne $diagnostics.Count -or
        ([bool]$Result.success -and $diagnosticCount -ne 0) -or
        (-not [bool]$Result.success -and
            ($diagnosticCount -eq 0 -or $artifactCount -ne 0))) {
        throw 'The sanitized interop result counts are inconsistent.'
    }

    $artifactPaths = New-Object 'Collections.Generic.HashSet[string]' `
        ([StringComparer]::OrdinalIgnoreCase)
    foreach ($artifact in $artifacts) {
        Assert-ExactJsonProperties -Object $artifact -Context 'artifact' `
            -Names @('role', 'cacheRelativePath', 'length', 'sha256')
        if ([string]::IsNullOrWhiteSpace([string]$artifact.role)) {
            throw 'A sanitized interop artifact role is invalid.'
        }
        Assert-LogicalPath -Value ([string]$artifact.cacheRelativePath) `
            -Context 'artifact.cacheRelativePath'
        if (-not $artifactPaths.Add([string]$artifact.cacheRelativePath)) {
            throw 'The sanitized interop result contains a duplicate artifact path.'
        }
        [void](Get-NonNegativeInt64 -Value $artifact.length -Context 'artifact.length')
        Assert-Sha256 -Value ([string]$artifact.sha256) -Context 'artifact.sha256'
    }

    foreach ($diagnostic in $diagnostics) {
        Assert-ExactJsonProperties -Object $diagnostic -Context 'diagnostic' `
            -Names @('code', 'message', 'cacheRelativePath')
        if ($allowedDiagnosticCodes -cnotcontains [string]$diagnostic.code -or
            [string]::IsNullOrWhiteSpace([string]$diagnostic.message)) {
            throw 'A sanitized interop diagnostic is invalid.'
        }
        if ($null -ne $diagnostic.cacheRelativePath) {
            Assert-LogicalPath -Value ([string]$diagnostic.cacheRelativePath) `
                -Context 'diagnostic.cacheRelativePath'
        }
    }
    Assert-NoAbsolutePathInJsonValue -Value $Result -Context 'interop result'
}

if ([IO.Path]::DirectorySeparatorChar -ne '\') {
    throw 'The controlled XCC synthetic interop wrapper supports Windows only.'
}
if ($allowedModes -cnotcontains $Mode) {
    throw 'Mode must use its canonical case-sensitive spelling.'
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

if (-not (Test-Path -LiteralPath $resolvedProjectRoot -PathType Container)) {
    throw 'The Unity project root was not found.'
}
Assert-NoExistingReparsePoint -Path $resolvedProjectRoot
Assert-RegularFile -Path $resolvedEditorPath
Assert-RegularFile -Path $resolvedXccMixerPath
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
    throw 'The local external-content configuration must remain inside the repository.'
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
    throw 'The Unity project is open. Close the Editor before XCC interop work.'
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

$lockedFiles = New-Object Collections.Generic.List[object]
$maximumLockedInputBytes = [int64](32 * 1024 * 1024)
$process = $null
$unityExitCode = $null
$cachePath = $null
$resultPath = $null
$resultRelativePath = $null
try {
    $configurationIdentity = Open-LockedFileIdentity `
        -Path $resolvedConfigurationPath `
        -MaximumBytes $maximumLockedInputBytes
    $lockedFiles.Add($configurationIdentity)
    $xccMixerIdentity = Open-LockedFileIdentity `
        -Path $resolvedXccMixerPath `
        -MaximumBytes $maximumLockedInputBytes
    $lockedFiles.Add($xccMixerIdentity)
    if (-not ([string]$xccMixerIdentity.Sha256).Equals(
            $expectedXccMixerSha256,
            [StringComparison]::OrdinalIgnoreCase)) {
        throw 'The supplied XCC Mixer does not match the approved SHA-256.'
    }

    $boundaries = Get-ConfigurationBoundaries -Path $resolvedConfigurationPath
    Assert-ConfigurationBoundaries `
        -Boundaries $boundaries `
        -RepositoryRoot $resolvedProjectRoot
    $cachePath = [IO.Path]::GetFullPath([string]$boundaries.CachePath)
    $wp02cRoot = Join-Path $cachePath 'wp02c'
    $commandResultRoot = Join-Path $wp02cRoot 'xcc-interop-command-results'
    Ensure-SafeDirectory -Path $cachePath
    Ensure-SafeDirectory -Path $wp02cRoot
    Ensure-SafeDirectory -Path $commandResultRoot
    $runId = 'run-' + (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssfffZ').ToLowerInvariant() +
        '-' + [Guid]::NewGuid().ToString('N')
    $runRoot = Join-Path $commandResultRoot $runId
    Ensure-SafeDirectory -Path $runRoot
    $resultPath = Join-Path $runRoot 'result.json'
    $logPath = Join-Path $runRoot 'unity.log'
    $resultRelativePath = 'wp02c/xcc-interop-command-results/' + $runId +
        '/result.json'

    $arguments = @(
        '-batchmode',
        '-nographics',
        '-quit',
        '-projectPath', (Quote-ProcessArgument -Value $resolvedProjectRoot),
        '-executeMethod', 'RA2YR.Editor.XccSyntheticInteropCommand.Run',
        '-ra2yrExternalContentConfig',
            (Quote-ProcessArgument -Value $resolvedConfigurationPath),
        '-ra2yrXccInteropMode', $Mode,
        '-ra2yrXccInteropCaseId', $CaseId,
        '-ra2yrXccInteropResultOutput', (Quote-ProcessArgument -Value $resultPath),
        '-logFile', (Quote-ProcessArgument -Value $logPath)
    )

    $process = Start-Process -FilePath $resolvedEditorPath -ArgumentList $arguments `
        -PassThru -WindowStyle Hidden
    if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
        $process.Kill()
        $process.WaitForExit()
        $process.Refresh()
        $unityExitCode = $process.ExitCode
        throw "Unity XCC interop timed out; terminated process exit code: $unityExitCode."
    }
    $process.Refresh()
    $unityExitCode = $process.ExitCode
    Assert-LockedFileUnchanged -Identity $configurationIdentity
    Assert-LockedFileUnchanged -Identity $xccMixerIdentity
} finally {
    if ($null -ne $process) {
        $process.Dispose()
    }
    for ($index = $lockedFiles.Count - 1; $index -ge 0; $index--) {
        $lockedFiles[$index].Stream.Dispose()
    }
}

if ($null -eq $resultPath -or -not (Test-Path -LiteralPath $resultPath -PathType Leaf)) {
    throw "Unity XCC interop produced no sanitized result; process exit code: $unityExitCode."
}
Assert-RegularFile -Path $resultPath
$resultInfo = New-Object IO.FileInfo($resultPath)
if ($resultInfo.Length -le 0 -or $resultInfo.Length -gt 1MB) {
    throw 'The sanitized XCC interop result has an invalid length.'
}
try {
    $resultText = [IO.File]::ReadAllText(
        $resultPath,
        (New-Object Text.UTF8Encoding($false, $true)))
    $result = $resultText | ConvertFrom-Json
} catch {
    throw 'The sanitized XCC interop result is not valid strict UTF-8 JSON.'
}
Assert-InteropResult -Result $result -ExpectedMode $Mode -ExpectedCaseId $CaseId

foreach ($artifact in @($result.artifacts)) {
    $artifactPath = [IO.Path]::GetFullPath((Join-Path `
        $cachePath `
        ([string]$artifact.cacheRelativePath).Replace('/', '\')))
    if (-not (Test-InsideOrEqual -Candidate $artifactPath -Root $cachePath)) {
        throw 'A reported XCC interop artifact escaped the external cache.'
    }
    $artifactIdentity = Open-LockedFileIdentity `
        -Path $artifactPath `
        -MaximumBytes $maximumLockedInputBytes
    try {
        if ([int64]$artifactIdentity.Length -ne [int64]$artifact.length -or
            -not ([string]$artifactIdentity.Sha256).Equals(
                [string]$artifact.sha256,
                [StringComparison]::OrdinalIgnoreCase)) {
            throw 'A reported XCC interop artifact does not match its sanitized identity.'
        }
    } finally {
        $artifactIdentity.Stream.Dispose()
    }
}

$resultIdentity = Open-LockedFileIdentity `
    -Path $resultPath `
    -MaximumBytes $maximumLockedInputBytes
try {
    $resultSha256 = [string]$resultIdentity.Sha256
} finally {
    $resultIdentity.Stream.Dispose()
}

"Unity process exit code: $unityExitCode"
"XCC Mixer SHA-256: $expectedXccMixerSha256"
"Mode: $($result.mode)"
"Internal validation stage: $($result.stage)"
"Real XCC execution evidence: $($result.realXccExecutionEvidence)"
"Case ID: $($result.caseId)"
"Success: $($result.success)"
"Artifact count: $($result.artifactCount)"
foreach ($artifact in @($result.artifacts)) {
    "Artifact: $($artifact.role) | $($artifact.cacheRelativePath) | bytes=$($artifact.length) | sha256=$($artifact.sha256)"
}
"Diagnostic count: $($result.diagnosticCount)"
foreach ($diagnostic in @($result.diagnostics)) {
    $diagnosticPath = if ($null -eq $diagnostic.cacheRelativePath) {
        '(none)'
    } else {
        [string]$diagnostic.cacheRelativePath
    }
    "Diagnostic: $($diagnostic.code) | $diagnosticPath | $($diagnostic.message)"
}
"Sanitized result SHA-256: $resultSha256"
"Sanitized result: $resultRelativePath"

if ([bool]$result.success -and $unityExitCode -ne 0) {
    throw "Unity reported a successful interop result but exited with code $unityExitCode."
}
if (-not [bool]$result.success) {
    if ($unityExitCode -ne 1) {
        throw "Unity reported an interop failure with unexpected exit code $unityExitCode."
    }
    throw 'The XCC synthetic interop stage failed with the diagnostics reported above.'
}
