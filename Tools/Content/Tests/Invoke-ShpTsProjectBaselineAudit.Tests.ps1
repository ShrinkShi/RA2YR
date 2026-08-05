[CmdletBinding()]
param(
    [Parameter()]
    [string] $RepositoryRoot
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = Join-Path $PSScriptRoot '..\..\..'
}
$root = [IO.Path]::GetFullPath($RepositoryRoot).TrimEnd(
    [IO.Path]::DirectorySeparatorChar,
    [IO.Path]::AltDirectorySeparatorChar)
$wrapperPath = Join-Path $root 'Tools\Content\Invoke-ShpTsProjectBaselineAudit.ps1'
$editorPath = Join-Path $root 'Assets\RA2YR\Editor\ShpTsProjectBaselineAuditCommand.cs'
$utf8 = New-Object Text.UTF8Encoding($false, $true)
$passed = 0

function Invoke-Case {
    param(
        [Parameter(Mandatory)][string] $Name,
        [Parameter(Mandatory)][scriptblock] $Body
    )

    & $Body
    $script:passed++
    "PASS $Name"
}

function Assert-Throws {
    param([Parameter(Mandatory)][scriptblock] $Body)

    $thrown = $false
    try {
        & $Body
    } catch {
        $thrown = $true
    }
    if (-not $thrown) {
        throw 'Expected the validation operation to throw.'
    }
}

function New-Layer {
    param(
        [Parameter(Mandatory)][string] $Archive,
        [Parameter(Mandatory)][string] $EntryId
    )

    [pscustomobject]@{
        archive = $Archive
        entryId = $EntryId
        resolvedName = 'resolved'
    }
}

function New-Sample {
    param(
        [Parameter(Mandatory)][string] $SampleId,
        [Parameter(Mandatory)][string] $SelectionBasis,
        [Parameter(Mandatory)][string] $MixId,
        [Parameter(Mandatory)][string] $RootArchive,
        [Parameter(Mandatory)][object[]] $Layers,
        [Parameter(Mandatory)][int64] $Length,
        [Parameter(Mandatory)][string] $Sha256,
        [Parameter(Mandatory)][int] $FrameCount,
        [Parameter(Mandatory)][int] $Raw0,
        [Parameter(Mandatory)][int] $Raw1,
        [Parameter(Mandatory)][int] $Rle3,
        [Parameter(Mandatory)][int] $Empty,
        [Parameter(Mandatory)][int] $Failed,
        [Parameter(Mandatory)][string] $DirectorySha,
        [Parameter(Mandatory)][string] $DecodedSha
    )

    $diagnostics = if ($Failed -eq 0) {
        [pscustomobject]@{}
    } else {
        [pscustomobject]@{ RleOutputOverflow = $Failed }
    }
    [pscustomobject]@{
        sampleId = $SampleId
        logicalRole = $SampleId
        selectionBasis = $SelectionBasis
        mixId = $MixId
        provenance = [pscustomobject]@{
            rootArchive = $RootArchive
            layers = $Layers
        }
        length = $Length
        sha256 = $Sha256
        frameCount = $FrameCount
        canvas = [pscustomobject]@{ width = 1; height = 1 }
        frameRectangleRange = [pscustomobject]@{}
        flags = [pscustomobject]@{
            raw0 = $Raw0
            raw1 = $Raw1
            raw2 = 0
            rle3 = $Rle3
            unknown = 0
        }
        emptyFrameCount = $Empty
        reservedNonZeroCount = 0
        coordinateHighBitCount = 0
        offsetAggregation = [pscustomobject]@{}
        paddingAggregation = [pscustomobject]@{}
        decodedIndexRange = [pscustomobject]@{}
        decodedPixelCount = 0
        zeroZeroUnresolvedCount = 0
        unresolvedFrameCount = 0
        failedFrameCount = $Failed
        directoryModelSha256 = $DirectorySha
        decodedModelSha256 = $DecodedSha
        memoryStreamMixWindowEquivalent = $true
        diagnosticCounts = $diagnostics
    }
}

function New-SyntheticSummary {
    [pscustomobject]@{
        schemaVersion = 1
        manifestType = 'RA2YR.ShpTsProjectBaselineAuditSanitized'
        baselineLogicalName = 'YR1001_ProjectBaseline'
        auditStatus = 'CompleteWithDecodeFailures'
        sourceVersion = 'Synthetic patched development source'
        directoryFingerprint = ('2' * 64)
        startedUtc = '2026-08-03T00:00:00.0000000Z'
        completedUtc = '2026-08-03T00:00:01.0000000Z'
        externalManifest = [pscustomobject]@{
            cacheRelativePath = 'm2-shp1/shp-ts-audits/synthetic/manifest.json'
            length = 4096
            sha256 = ('3' * 64)
        }
        samples = @(
            (New-Sample `
                -SampleId 'building-explicit-image' `
                -SelectionBasis 'ArtExplicitImageCatalogResolved' `
                -MixId '0x03F5DAA8' `
                -RootArchive 'ra2md.mix' `
                -Layers @(
                    (New-Layer 'ra2md.mix' '0xEB108109'),
                    (New-Layer 'ra2md.mix/snowmd.mix' '0x03F5DAA8')) `
                -Length 50184 `
                -Sha256 '1addf99f3958875c4561915acb1865f91a311afc526015569d95058c0b2a4460' `
                -FrameCount 6 -Raw0 0 -Raw1 0 -Rle3 6 -Empty 0 -Failed 6 `
                -DirectorySha '7e7886f36057505d5274e0b44bef3c2dab7f80a76a4d25089dbc2a2facd0e4a9' `
                -DecodedSha '8313e86a462c0ad94fec669c9002f508831d5bb77297c34f3c25b63d0ade2893'),
            (New-Sample `
                -SampleId 'infantry-explicit-image' `
                -SelectionBasis 'ArtExplicitImageCatalogResolved' `
                -MixId '0x13C2CC8B' `
                -RootArchive 'ra2.mix' `
                -Layers @(
                    (New-Layer 'ra2.mix' '0x55DE03CC'),
                    (New-Layer 'ra2.mix/conquer.mix' '0x13C2CC8B')) `
                -Length 114032 `
                -Sha256 'f8eb0dc0156a877028ac2ed57bf2e71a9445ffcb6ef6a4caf3b0dbb7b111834c' `
                -FrameCount 506 -Raw0 0 -Raw1 27 -Rle3 479 -Empty 253 -Failed 226 `
                -DirectorySha '9f69eab472f99f5cb6b624760a148de1edf5f97eea24129dc950a640277472cc' `
                -DecodedSha '4a858e914051130cd51e1e0c1d6e646b7779045499a40fb1c62fa080d45099ac'),
            (New-Sample `
                -SampleId 'map-addon-catalog-survey' `
                -SelectionBasis 'VerifiedCatalogSurvey' `
                -MixId '0x51D8DA20' `
                -RootArchive 'expandmd01.mix' `
                -Layers @((New-Layer 'expandmd01.mix' '0x51D8DA20')) `
                -Length 16016 `
                -Sha256 'd7e92839fef021b832d96b4571f870a939f124efe77b33b503711188dea93077' `
                -FrameCount 8 -Raw0 0 -Raw1 0 -Rle3 8 -Empty 0 -Failed 8 `
                -DirectorySha '762caa222a8033fcc56cb01b3cbdd3956b707fca10a65d79aabe3c304b6a5fd7' `
                -DecodedSha '8313e86a462c0ad94fec669c9002f508831d5bb77297c34f3c25b63d0ade2893'),
            (New-Sample `
                -SampleId 'mouse-cursor-catalog-survey' `
                -SelectionBasis 'VerifiedCatalogSurvey' `
                -MixId '0x8332234B' `
                -RootArchive 'ra2.mix' `
                -Layers @(
                    (New-Layer 'ra2.mix' '0x55DE03CC'),
                    (New-Layer 'ra2.mix/conquer.mix' '0x8332234B')) `
                -Length 359800 `
                -Sha256 'e5a356737787d681dd1a2b1255c7c7d1e9bd8334e5674705f2ccc39cd12634df' `
                -FrameCount 450 -Raw0 1 -Raw1 449 -Rle3 0 -Empty 0 -Failed 0 `
                -DirectorySha '76ab99c686c8745ae6faee099eddb03c76f72a2fbc7ef449da02bb32abb87e1c' `
                -DecodedSha 'e38e2630b44da98d444dc833751f522af4c88d310ebb99216e4169340e1d0595'),
            (New-Sample `
                -SampleId 'techno-animation-catalog-survey' `
                -SelectionBasis 'VerifiedCatalogSurvey' `
                -MixId '0x6B93DFCC' `
                -RootArchive 'ra2.mix' `
                -Layers @(
                    (New-Layer 'ra2.mix' '0x55DE03CC'),
                    (New-Layer 'ra2.mix/conquer.mix' '0x6B93DFCC')) `
                -Length 298016 `
                -Sha256 'e4ce5033b296035ed5ad3f66bc8d5cba2c29b80c7061970fcacb6e5d749a6cff' `
                -FrameCount 17 -Raw0 0 -Raw1 0 -Rle3 17 -Empty 0 -Failed 17 `
                -DirectorySha '8e626052744fcce362aab01a6fb5d10442304662a7d71b4f64663ae7e9357f65' `
                -DecodedSha '8313e86a462c0ad94fec669c9002f508831d5bb77297c34f3c25b63d0ade2893'),
            (New-Sample `
                -SampleId 'ui-cameo-configuration' `
                -SelectionBasis 'UiResourceConfiguration' `
                -MixId '0x61F07B81' `
                -RootArchive 'language.mix' `
                -Layers @(
                    (New-Layer 'language.mix' '0x451C5DF2'),
                    (New-Layer 'language.mix/cameo.mix' '0x61F07B81')) `
                -Length 2912 `
                -Sha256 '438d514ffbd5e0bf925d16b71dd6bb0e03c1c259b95e47c6879d94e12fe93768' `
                -FrameCount 1 -Raw0 0 -Raw1 1 -Rle3 0 -Empty 0 -Failed 0 `
                -DirectorySha '9e7342edbebc2173b6d2fc934e10e2c02f315716cbc86e74529792eb8d31a781' `
                -DecodedSha '23ef215926a6ab7c52204bed389355613fe4465822af770984fd555294b2befc')
        )
        limitations = @(
            'Synthetic selection basis does not claim stock runtime precedence.',
            'Strict RLE-Zero conflicts remain decode failures.'
        )
    }
}

if (-not (Test-Path -LiteralPath $wrapperPath -PathType Leaf) -or
    -not (Test-Path -LiteralPath $editorPath -PathType Leaf)) {
    throw 'The M2-SHP1 controlled audit entry points are missing.'
}
$tokens = $null
$parseErrors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile(
    $wrapperPath,
    [ref]$tokens,
    [ref]$parseErrors)
if ($parseErrors.Count -ne 0) {
    $messages = @($parseErrors | ForEach-Object { $_.Message }) -join '; '
    throw "The wrapper has PowerShell parser errors: $messages"
}
$wrapperText = [IO.File]::ReadAllText($wrapperPath, $utf8)
$editorText = [IO.File]::ReadAllText($editorPath, $utf8)

Invoke-Case -Name 'PowerShell parser accepts wrapper' -Body {
    if ($null -eq $ast.ParamBlock) {
        throw 'The wrapper does not declare a parameter block.'
    }
}

Invoke-Case -Name 'Wrapper pins golden identities and strict conflict counts' -Body {
    foreach ($required in @(
        '2022.3.60f1c1',
        'YR1001_ProjectBaseline',
        'RA2YR.ShpTsProjectBaselineAuditSanitized',
        'CompleteWithDecodeFailures',
        '0x03F5DAA8',
        '0x13C2CC8B',
        '0x51D8DA20',
        '0x8332234B',
        '0x6B93DFCC',
        '0x61F07B81',
        'RleOutputOverflow',
        '257'
    )) {
        if (-not $wrapperText.Contains($required)) {
            throw "The wrapper does not pin required identity: $required"
        }
    }
}

Invoke-Case -Name 'Wrapper retains safety gates' -Body {
    foreach ($required in @(
        'Assert-NoExistingReparsePoint',
        'Assert-GitIgnored',
        'Temp\UnityLockfile',
        'Open-LockedFileIdentity',
        'Assert-LockedFileUnchanged',
        'external SHP audit manifest escaped',
        'Unity process exit code'
    )) {
        if (-not $wrapperText.Contains($required)) {
            throw "The wrapper omits a required safety gate: $required"
        }
    }
    if ($wrapperText -match '(?i)XccMixer|XCC Mixer|global mix database') {
        throw 'The SHP audit wrapper must not depend on or start XCC.'
    }
}

Invoke-Case -Name 'Editor command uses Core service and atomic publication' -Body {
    foreach ($required in @(
        'ShpTsProjectBaselineAuditService.Run(configuration)',
        'CompleteWithDecodeFailures',
        'ExpectedFailedFrameCount = 257',
        'WriteNewUtf8FileAtomically',
        'FileOptions.WriteThrough',
        'File.Move(temporaryPath, path)',
        'TestResults',
        'RejectSensitivePath'
    )) {
        if (-not $editorText.Contains($required)) {
            throw "The Editor command omits a required delivery boundary: $required"
        }
    }
}

$functionAsts = @($ast.FindAll({
    param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst]
}, $true))
foreach ($functionAst in $functionAsts) {
    Invoke-Expression $functionAst.Extent.Text
}
$baselineName = 'YR1001_ProjectBaseline'
$manifestType = 'RA2YR.ShpTsProjectBaselineAuditSanitized'

Invoke-Case -Name 'Synthetic sanitized summary passes' -Body {
    Assert-SanitizedSummary -Summary (New-SyntheticSummary)
}

Invoke-Case -Name 'Changed golden hash fails closed' -Body {
    $summary = New-SyntheticSummary
    $summary.samples[0].sha256 = ('0' * 64)
    Assert-Throws { Assert-SanitizedSummary -Summary $summary }
}

Invoke-Case -Name 'Changed provenance fails closed' -Body {
    $summary = New-SyntheticSummary
    $summary.samples[1].provenance.layers[0].entryId = '0x00000000'
    Assert-Throws { Assert-SanitizedSummary -Summary $summary }
}

Invoke-Case -Name 'Changed strict failure count fails closed' -Body {
    $summary = New-SyntheticSummary
    $summary.samples[0].failedFrameCount = 5
    Assert-Throws { Assert-SanitizedSummary -Summary $summary }
}

Invoke-Case -Name 'Absolute host path fails closed' -Body {
    $summary = New-SyntheticSummary
    $summary.limitations = @('C:\private\original.shp')
    Assert-Throws { Assert-SanitizedSummary -Summary $summary }
}

"SHP audit wrapper regression tests passed: $passed"
exit 0
