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
$manifestType = 'RA2YR.ShpTsProjectBaselineAuditSanitized'
$script:summaryTimestampsVerifiedFromRawJson = $false

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
            throw 'A controlled SHP audit path traverses a reparse point.'
        }
    }
}

function Assert-RegularFile {
    param([Parameter(Mandatory)][string] $Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw 'A required controlled SHP audit file was not found.'
    }
    Assert-NoExistingReparsePoint -Path $Path
    $attributes = [IO.File]::GetAttributes([IO.Path]::GetFullPath($Path))
    if (($attributes -band [IO.FileAttributes]::Directory) -ne 0 -or
        ($attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw 'A controlled SHP audit input is not a regular file.'
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
        throw 'A local SHP audit configuration or result path is not excluded by .gitignore.'
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
        throw 'A locked controlled SHP audit input changed during execution.'
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
        [Parameter(Mandatory)][object] $Value,
        [Parameter(Mandatory)][string] $Context
    )

    if ($Value -is [DateTime]) {
        if ([DateTime]$Value.Kind -eq [DateTimeKind]::Unspecified) {
            throw "The $Context timestamp is not explicitly UTC."
        }
        return
    }
    if ($Value -is [DateTimeOffset]) {
        if ([DateTimeOffset]$Value.Offset -ne [TimeSpan]::Zero) {
            throw "The $Context timestamp is not explicitly UTC."
        }
        return
    }

    $text = [string]$Value
    if ($text -notmatch 'Z$') {
        if ($script:summaryTimestampsVerifiedFromRawJson) {
            return
        }
        throw "The $Context timestamp is not explicitly UTC."
    }
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
        'samples',
        'limitations'
    )
    if ((Get-NonNegativeInt64 -Value $Summary.schemaVersion -Context 'schemaVersion') -ne 1 -or
        [string]$Summary.manifestType -cne $manifestType -or
        [string]$Summary.baselineLogicalName -cne $baselineName -or
        [string]$Summary.auditStatus -cne 'CompleteWithDecodeFailures' -or
        [string]::IsNullOrWhiteSpace([string]$Summary.sourceVersion)) {
        throw 'The sanitized SHP audit summary identity is invalid.'
    }
    Assert-LowerSha256 -Value ([string]$Summary.directoryFingerprint) `
        -Context 'directoryFingerprint'
    Assert-IsoUtcTimestamp -Value ([string]$Summary.startedUtc) -Context 'startedUtc'
    Assert-IsoUtcTimestamp -Value ([string]$Summary.completedUtc) -Context 'completedUtc'

    Assert-ExactJsonProperties -Object $Summary.externalManifest `
        -Context 'externalManifest' `
        -Names @('cacheRelativePath', 'length', 'sha256')
    Assert-LogicalPath -Value ([string]$Summary.externalManifest.cacheRelativePath) `
        -Context 'externalManifest.cacheRelativePath'
    if ((Get-NonNegativeInt64 `
            -Value $Summary.externalManifest.length `
            -Context 'externalManifest.length') -le 0) {
        throw 'The external SHP manifest length is invalid.'
    }
    Assert-LowerSha256 -Value ([string]$Summary.externalManifest.sha256) `
        -Context 'externalManifest.sha256'

    $expected = @{
        'building-explicit-image' = @{
            Selection = 'ArtExplicitImageCatalogResolved'
            Id = '0x03F5DAA8'
            Root = 'ra2md.mix'
            Archives = @('ra2md.mix', 'ra2md.mix/snowmd.mix')
            EntryIds = @('0xEB108109', '0x03F5DAA8')
            Length = 50184
            Sha256 = '1addf99f3958875c4561915acb1865f91a311afc526015569d95058c0b2a4460'
            Frames = 6
            Raw0 = 0
            Raw1 = 0
            Rle3 = 6
            Empty = 0
            Failed = 6
            DirectorySha = '7e7886f36057505d5274e0b44bef3c2dab7f80a76a4d25089dbc2a2facd0e4a9'
            DecodedSha = '8313e86a462c0ad94fec669c9002f508831d5bb77297c34f3c25b63d0ade2893'
        }
        'infantry-explicit-image' = @{
            Selection = 'ArtExplicitImageCatalogResolved'
            Id = '0x13C2CC8B'
            Root = 'ra2.mix'
            Archives = @('ra2.mix', 'ra2.mix/conquer.mix')
            EntryIds = @('0x55DE03CC', '0x13C2CC8B')
            Length = 114032
            Sha256 = 'f8eb0dc0156a877028ac2ed57bf2e71a9445ffcb6ef6a4caf3b0dbb7b111834c'
            Frames = 506
            Raw0 = 0
            Raw1 = 27
            Rle3 = 479
            Empty = 253
            Failed = 226
            DirectorySha = '9f69eab472f99f5cb6b624760a148de1edf5f97eea24129dc950a640277472cc'
            DecodedSha = '4a858e914051130cd51e1e0c1d6e646b7779045499a40fb1c62fa080d45099ac'
        }
        'map-addon-catalog-survey' = @{
            Selection = 'VerifiedCatalogSurvey'
            Id = '0x51D8DA20'
            Root = 'expandmd01.mix'
            Archives = @('expandmd01.mix')
            EntryIds = @('0x51D8DA20')
            Length = 16016
            Sha256 = 'd7e92839fef021b832d96b4571f870a939f124efe77b33b503711188dea93077'
            Frames = 8
            Raw0 = 0
            Raw1 = 0
            Rle3 = 8
            Empty = 0
            Failed = 8
            DirectorySha = '762caa222a8033fcc56cb01b3cbdd3956b707fca10a65d79aabe3c304b6a5fd7'
            DecodedSha = '8313e86a462c0ad94fec669c9002f508831d5bb77297c34f3c25b63d0ade2893'
        }
        'mouse-cursor-catalog-survey' = @{
            Selection = 'VerifiedCatalogSurvey'
            Id = '0x8332234B'
            Root = 'ra2.mix'
            Archives = @('ra2.mix', 'ra2.mix/conquer.mix')
            EntryIds = @('0x55DE03CC', '0x8332234B')
            Length = 359800
            Sha256 = 'e5a356737787d681dd1a2b1255c7c7d1e9bd8334e5674705f2ccc39cd12634df'
            Frames = 450
            Raw0 = 1
            Raw1 = 449
            Rle3 = 0
            Empty = 0
            Failed = 0
            DirectorySha = '76ab99c686c8745ae6faee099eddb03c76f72a2fbc7ef449da02bb32abb87e1c'
            DecodedSha = 'e38e2630b44da98d444dc833751f522af4c88d310ebb99216e4169340e1d0595'
        }
        'techno-animation-catalog-survey' = @{
            Selection = 'VerifiedCatalogSurvey'
            Id = '0x6B93DFCC'
            Root = 'ra2.mix'
            Archives = @('ra2.mix', 'ra2.mix/conquer.mix')
            EntryIds = @('0x55DE03CC', '0x6B93DFCC')
            Length = 298016
            Sha256 = 'e4ce5033b296035ed5ad3f66bc8d5cba2c29b80c7061970fcacb6e5d749a6cff'
            Frames = 17
            Raw0 = 0
            Raw1 = 0
            Rle3 = 17
            Empty = 0
            Failed = 17
            DirectorySha = '8e626052744fcce362aab01a6fb5d10442304662a7d71b4f64663ae7e9357f65'
            DecodedSha = '8313e86a462c0ad94fec669c9002f508831d5bb77297c34f3c25b63d0ade2893'
        }
        'ui-cameo-configuration' = @{
            Selection = 'UiResourceConfiguration'
            Id = '0x61F07B81'
            Root = 'language.mix'
            Archives = @('language.mix', 'language.mix/cameo.mix')
            EntryIds = @('0x451C5DF2', '0x61F07B81')
            Length = 2912
            Sha256 = '438d514ffbd5e0bf925d16b71dd6bb0e03c1c259b95e47c6879d94e12fe93768'
            Frames = 1
            Raw0 = 0
            Raw1 = 1
            Rle3 = 0
            Empty = 0
            Failed = 0
            DirectorySha = '9e7342edbebc2173b6d2fc934e10e2c02f315716cbc86e74529792eb8d31a781'
            DecodedSha = '23ef215926a6ab7c52204bed389355613fe4465822af770984fd555294b2befc'
        }
    }
    $samples = @($Summary.samples)
    if ($samples.Count -ne $expected.Count) {
        throw 'The sanitized SHP summary does not contain exactly six golden samples.'
    }
    $observedIds = New-Object `
        'System.Collections.Generic.HashSet[string]' `
        ([StringComparer]::Ordinal)
    $failedTotal = 0
    foreach ($sample in $samples) {
        Assert-ExactJsonProperties -Object $sample -Context 'sample' -Names @(
            'sampleId',
            'logicalRole',
            'selectionBasis',
            'mixId',
            'provenance',
            'length',
            'sha256',
            'frameCount',
            'canvas',
            'frameRectangleRange',
            'flags',
            'emptyFrameCount',
            'reservedNonZeroCount',
            'coordinateHighBitCount',
            'offsetAggregation',
            'paddingAggregation',
            'decodedIndexRange',
            'decodedPixelCount',
            'zeroZeroUnresolvedCount',
            'unresolvedFrameCount',
            'failedFrameCount',
            'directoryModelSha256',
            'decodedModelSha256',
            'memoryStreamMixWindowEquivalent',
            'diagnosticCounts'
        )
        $sampleId = [string]$sample.sampleId
        if (-not $expected.ContainsKey($sampleId) -or -not $observedIds.Add($sampleId)) {
            throw 'The sanitized SHP summary contains an unknown or duplicate sample.'
        }
        $identity = $expected[$sampleId]
        $failed = Get-NonNegativeInt64 -Value $sample.failedFrameCount `
            -Context 'sample.failedFrameCount'
        $failedTotal += $failed
        if ([string]$sample.logicalRole -cne $sampleId -or
            [string]$sample.selectionBasis -cne [string]$identity.Selection -or
            [string]$sample.mixId -cne [string]$identity.Id -or
            [string]$sample.sha256 -cne [string]$identity.Sha256 -or
            (Get-NonNegativeInt64 -Value $sample.length -Context 'sample.length') -ne [int64]$identity.Length -or
            (Get-NonNegativeInt64 -Value $sample.frameCount -Context 'sample.frameCount') -ne [int64]$identity.Frames -or
            (Get-NonNegativeInt64 -Value $sample.flags.raw0 -Context 'sample.flags.raw0') -ne [int64]$identity.Raw0 -or
            (Get-NonNegativeInt64 -Value $sample.flags.raw1 -Context 'sample.flags.raw1') -ne [int64]$identity.Raw1 -or
            (Get-NonNegativeInt64 -Value $sample.flags.raw2 -Context 'sample.flags.raw2') -ne 0 -or
            (Get-NonNegativeInt64 -Value $sample.flags.rle3 -Context 'sample.flags.rle3') -ne [int64]$identity.Rle3 -or
            (Get-NonNegativeInt64 -Value $sample.flags.unknown -Context 'sample.flags.unknown') -ne 0 -or
            (Get-NonNegativeInt64 -Value $sample.emptyFrameCount -Context 'sample.emptyFrameCount') -ne [int64]$identity.Empty -or
            (Get-NonNegativeInt64 -Value $sample.reservedNonZeroCount -Context 'sample.reservedNonZeroCount') -ne 0 -or
            (Get-NonNegativeInt64 -Value $sample.coordinateHighBitCount -Context 'sample.coordinateHighBitCount') -ne 0 -or
            (Get-NonNegativeInt64 -Value $sample.zeroZeroUnresolvedCount -Context 'sample.zeroZeroUnresolvedCount') -ne 0 -or
            (Get-NonNegativeInt64 -Value $sample.unresolvedFrameCount -Context 'sample.unresolvedFrameCount') -ne 0 -or
            $failed -ne [int64]$identity.Failed -or
            [string]$sample.directoryModelSha256 -cne [string]$identity.DirectorySha -or
            [string]$sample.decodedModelSha256 -cne [string]$identity.DecodedSha -or
            $sample.memoryStreamMixWindowEquivalent -ne $true) {
            throw "The golden SHP identity or aggregate statistics changed for $sampleId."
        }
        Assert-LowerSha256 -Value ([string]$sample.sha256) -Context 'sample.sha256'
        Assert-LowerSha256 -Value ([string]$sample.directoryModelSha256) `
            -Context 'sample.directoryModelSha256'
        Assert-LowerSha256 -Value ([string]$sample.decodedModelSha256) `
            -Context 'sample.decodedModelSha256'

        Assert-ExactJsonProperties -Object $sample.provenance `
            -Context 'sample.provenance' -Names @('rootArchive', 'layers')
        if ([string]$sample.provenance.rootArchive -cne [string]$identity.Root) {
            throw "The golden SHP root archive changed for $sampleId."
        }
        $layers = @($sample.provenance.layers)
        if ($layers.Count -ne @($identity.Archives).Count) {
            throw "The golden SHP MIX chain changed for $sampleId."
        }
        for ($index = 0; $index -lt $layers.Count; $index++) {
            $layer = $layers[$index]
            Assert-ExactJsonProperties -Object $layer -Context 'sample.provenance.layer' `
                -Names @('archive', 'entryId', 'resolvedName')
            if ([string]$layer.archive -cne [string]$identity.Archives[$index] -or
                [string]$layer.entryId -cne [string]$identity.EntryIds[$index] -or
                [string]$layer.resolvedName -cne 'resolved') {
                throw "The golden SHP provenance layer changed for $sampleId."
            }
        }

        $diagnosticProperties = @($sample.diagnosticCounts.PSObject.Properties)
        if ([int64]$identity.Failed -eq 0) {
            if ($diagnosticProperties.Count -ne 0) {
                throw "A fully decoded SHP sample unexpectedly reports diagnostics for $sampleId."
            }
        } elseif ($diagnosticProperties.Count -ne 1 -or
            $diagnosticProperties[0].Name -cne 'RleOutputOverflow' -or
            (Get-NonNegativeInt64 -Value $diagnosticProperties[0].Value `
                -Context 'sample.diagnosticCounts.RleOutputOverflow') -ne
                [int64]$identity.Failed) {
            throw "The strict RLE failure diagnostics changed for $sampleId."
        }
    }
    if ($failedTotal -ne 257) {
        throw 'The sanitized SHP audit no longer reports the expected 257 strict decode failures.'
    }

    $limitations = @($Summary.limitations)
    if ($limitations.Count -eq 0) {
        throw 'The sanitized SHP audit summary omits its limitations.'
    }
    foreach ($limitation in $limitations) {
        if (-not ($limitation -is [string]) -or
            [string]::IsNullOrWhiteSpace([string]$limitation)) {
            throw 'A sanitized SHP audit limitation is invalid.'
        }
    }
    Assert-NoAbsolutePathInJsonValue -Value $Summary -Context 'summary'
}

if ([IO.Path]::DirectorySeparatorChar -ne '\') {
    throw 'The controlled YR1001_ProjectBaseline SHP audit supports Windows only.'
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
    throw 'The Unity project is open. Close the Editor before the SHP baseline audit.'
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
    throw 'The complete SHP audit manifest cache must remain outside the repository.'
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
    throw 'The unique SHP audit result directory already exists.'
}
[IO.Directory]::CreateDirectory($runRoot) | Out-Null
Assert-NoExistingReparsePoint -Path $runRoot
$summaryPath = Join-Path $runRoot 'm2-shp1-project-baseline-summary.json'
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
        '-executeMethod', 'RA2YR.Editor.ShpTsProjectBaselineAuditCommand.Run',
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
        throw "Unity SHP baseline audit timed out; terminated process exit code: $unityExitCode."
    }
    $process.Refresh()
    $unityExitCode = $process.ExitCode

    Assert-LockedFileUnchanged -Identity $configurationIdentity
    if ($unityExitCode -ne 0) {
        throw "Unity SHP baseline audit process exited with code $unityExitCode."
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
if ($summaryInfo.Length -le 0 -or $summaryInfo.Length -gt 2MB) {
    throw 'The sanitized SHP audit summary has an invalid length.'
}
try {
    $summaryText = [IO.File]::ReadAllText(
        $summaryPath,
        (New-Object Text.UTF8Encoding($false, $true)))
    if ($summaryText -notmatch '"startedUtc":"[^"]+Z"' -or
        $summaryText -notmatch '"completedUtc":"[^"]+Z"') {
        throw 'The sanitized SHP audit summary does not preserve explicit UTC timestamps.'
    }
    $script:summaryTimestampsVerifiedFromRawJson = $true
    $summary = $summaryText | ConvertFrom-Json
} catch {
    throw 'The sanitized SHP audit summary is not valid strict UTF-8 JSON.'
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
    throw 'The external SHP audit manifest escaped its configured cache boundary.'
}
Assert-NoExistingReparsePoint -Path $manifestPath
$externalManifestIdentity = Open-LockedFileIdentity -Path $manifestPath
try {
    if ([int64]$externalManifestIdentity.Length -ne
            [int64]$summary.externalManifest.length -or
        -not ([string]$externalManifestIdentity.Sha256).Equals(
            [string]$summary.externalManifest.sha256,
            [StringComparison]::OrdinalIgnoreCase)) {
        throw 'The external SHP audit manifest does not match the sanitized summary.'
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
"Validated SHP samples: $(@($summary.samples).Count)"
"Strict decode failures: $((@($summary.samples) | Measure-Object -Property failedFrameCount -Sum).Sum)"
"External manifest SHA-256: $($summary.externalManifest.sha256)"
"Sanitized summary SHA-256: $summarySha256"
"Sanitized summary: TestResults/$runId/m2-shp1-project-baseline-summary.json"
