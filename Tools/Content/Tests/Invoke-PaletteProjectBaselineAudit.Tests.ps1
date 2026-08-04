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
$wrapperPath = Join-Path $root 'Tools\Content\Invoke-PaletteProjectBaselineAudit.ps1'
$editorPath = Join-Path $root 'Assets\RA2YR\Editor\PaletteProjectBaselineAuditCommand.cs'
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

function New-Provenance {
    param(
        [Parameter(Mandatory)][string] $Name,
        [Parameter(Mandatory)][string] $Id
    )

    [pscustomobject]@{
        sourceId = 'YR1001_ProjectBaseline'
        rootArchive = 'ra2.mix'
        layers = @(
            [pscustomobject]@{
                archive = 'ra2.mix'
                entryId = '0x3B5A96DE'
                resolvedName = 'cache.mix'
            },
            [pscustomobject]@{
                archive = 'ra2.mix/cache.mix'
                entryId = $Id
                resolvedName = $Name
            }
        )
    }
}

function New-Palette {
    param(
        [Parameter(Mandatory)][string] $Name,
        [Parameter(Mandatory)][string] $Id,
        [Parameter(Mandatory)][string] $Sha256,
        [Parameter(Mandatory)][int] $DistinctColors
    )

    [pscustomobject]@{
        logicalName = $Name
        mixId = $Id
        provenance = New-Provenance -Name $Name -Id $Id
        length = 768
        sha256 = $Sha256
        colorCount = 256
        rawChannelMin = 0
        rawChannelMax = 63
        invalidChannelCount = 0
        distinctColorCount = $DistinctColors
        normalizedModelSha256 = ('1' * 64)
        displayConversionStrategy = 'XccScaleToFullRangeFloor'
        diagnosticCount = 0
    }
}

function New-SyntheticSummary {
    [pscustomobject]@{
        schemaVersion = 1
        manifestType = 'RA2YR.PaletteProjectBaselineAuditSanitized'
        baselineLogicalName = 'YR1001_ProjectBaseline'
        auditStatus = 'Complete'
        sourceVersion = 'Synthetic patched development source'
        directoryFingerprint = ('2' * 64)
        startedUtc = '2026-08-03T00:00:00.0000000Z'
        completedUtc = '2026-08-03T00:00:01.0000000Z'
        externalManifest = [pscustomobject]@{
            schemaVersion = 1
            cacheRelativePath = 'wp02d/palette-audits/synthetic/manifest.json'
            length = 4096
            sha256 = ('3' * 64)
        }
        palettes = @(
            (New-Palette -Name 'isotem.pal' -Id '0x5F9D97B9' `
                -Sha256 '5d6e40fcd11a592a31494c635d93c21796cfe86a2743f0258b1f7d0aff850795' `
                -DistinctColors 256),
            (New-Palette -Name 'temperat.pal' -Id '0x9C58DE40' `
                -Sha256 '5903b69868b84f494cfbb4e7100398015ef9775b37726019a0d7b5fb6cb33b55' `
                -DistinctColors 256),
            (New-Palette -Name 'unittem.pal' -Id '0x63DA7359' `
                -Sha256 'ed785e62eed291480f3198dd44f6b656ebe3a9b75e9f641944d710abc6bde3e3' `
                -DistinctColors 210)
        )
        limitations = @(
            'Synthetic parser evidence does not prove original visual rendering.'
        )
    }
}

if (-not (Test-Path -LiteralPath $wrapperPath -PathType Leaf) -or
    -not (Test-Path -LiteralPath $editorPath -PathType Leaf)) {
    throw 'The WP-02D controlled audit entry points are missing.'
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

Invoke-Case -Name 'Wrapper pins golden identities' -Body {
    foreach ($required in @(
        '2022.3.60f1c1',
        'YR1001_ProjectBaseline',
        'RA2YR.PaletteProjectBaselineAuditSanitized',
        'XccScaleToFullRangeFloor',
        '0x5F9D97B9',
        '0x9C58DE40',
        '0x63DA7359',
        '5d6e40fcd11a592a31494c635d93c21796cfe86a2743f0258b1f7d0aff850795',
        '5903b69868b84f494cfbb4e7100398015ef9775b37726019a0d7b5fb6cb33b55',
        'ed785e62eed291480f3198dd44f6b656ebe3a9b75e9f641944d710abc6bde3e3'
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
        'external PAL audit manifest escaped',
        'Unity process exit code'
    )) {
        if (-not $wrapperText.Contains($required)) {
            throw "The wrapper omits a required safety gate: $required"
        }
    }
    if ($wrapperText -match '(?i)XccMixer|XCC Mixer|global mix database') {
        throw 'The PAL audit wrapper must not depend on or start XCC.'
    }
}

Invoke-Case -Name 'Editor command uses Core service and atomic publication' -Body {
    foreach ($required in @(
        'PaletteProjectBaselineAuditService.Run(configuration)',
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
$manifestType = 'RA2YR.PaletteProjectBaselineAuditSanitized'
$displayConversionStrategy = 'XccScaleToFullRangeFloor'

Invoke-Case -Name 'Synthetic sanitized summary passes' -Body {
    Assert-SanitizedSummary -Summary (New-SyntheticSummary)
}

Invoke-Case -Name 'JSON UTC timestamp coercion passes' -Body {
    $summary = (New-SyntheticSummary | ConvertTo-Json -Depth 8 -Compress) |
        ConvertFrom-Json
    Assert-SanitizedSummary -Summary $summary
}

Invoke-Case -Name 'Changed golden hash fails closed' -Body {
    $summary = New-SyntheticSummary
    $summary.palettes[0].sha256 = ('0' * 64)
    Assert-Throws { Assert-SanitizedSummary -Summary $summary }
}

Invoke-Case -Name 'Noncanonical hash casing fails closed' -Body {
    $summary = New-SyntheticSummary
    $summary.palettes[0].normalizedModelSha256 = ('A' * 64)
    Assert-Throws { Assert-SanitizedSummary -Summary $summary }
}

Invoke-Case -Name 'Changed MIX provenance fails closed' -Body {
    $summary = New-SyntheticSummary
    $summary.palettes[1].provenance.layers[0].resolvedName = 'other.mix'
    Assert-Throws { Assert-SanitizedSummary -Summary $summary }
}

Invoke-Case -Name 'Absolute host path fails closed' -Body {
    $summary = New-SyntheticSummary
    $summary.limitations = @('C:\private\original.pal')
    Assert-Throws { Assert-SanitizedSummary -Summary $summary }
}

"Palette audit wrapper regression tests passed: $passed"
exit 0
