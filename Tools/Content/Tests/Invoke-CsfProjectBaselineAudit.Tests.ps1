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
$wrapperPath = Join-Path $root 'Tools\Content\Invoke-CsfProjectBaselineAudit.ps1'
$editorPath = Join-Path $root 'Assets\RA2YR\Editor\CsfProjectBaselineAuditCommand.cs'
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
    try { & $Body } catch { $thrown = $true }
    if (-not $thrown) { throw 'Expected the validation operation to throw.' }
}

function New-SyntheticSummary {
    [pscustomobject]@{
        schemaVersion = 1
        manifestType = 'RA2YR.CsfProjectBaselineAuditSanitized'
        baselineLogicalName = 'YR1001_ProjectBaseline'
        auditStatus = 'Complete'
        sourceVersion = 'Synthetic patched development source'
        directoryFingerprint = ('2' * 64)
        startedUtc = '2026-08-03T00:00:00.0000000Z'
        completedUtc = '2026-08-03T00:00:01.0000000Z'
        externalManifest = [pscustomobject]@{
            schemaVersion = 1
            cacheRelativePath = 'wp02e/csf-audits/synthetic/manifest.json'
            length = 4096
            sha256 = ('3' * 64)
        }
        csf = [pscustomobject]@{
            logicalName = 'ra2md.csf'
            mixId = '0xBD835079'
            provenance = [pscustomobject]@{
                sourceId = 'YR1001_ProjectBaseline'
                rootArchive = 'langmd.mix'
                layers = @([pscustomobject]@{
                    archive = 'langmd.mix'
                    entryId = '0xBD835079'
                    resolvedName = 'ra2md.csf'
                })
            }
            length = 332973
            sha256 = '1b90bb0756137f46ff529af043fe798d7f1f9fa1713a4110f17e1d674de81f1c'
            formatVersion = 3
            rawLanguageCode = 9
            labelRecordCount = 5211
            totalValueCount = 5211
            normalValueCount = 4007
            extendedValueCount = 1204
            emptyValueCount = 4
            duplicateLabelCount = 0
            maximumValuesPerLabel = 1
            labelNameLength = [pscustomobject]@{
                minimum = 6; maximum = 31; unit = 'ascii-bytes'
            }
            mainTextLength = [pscustomobject]@{
                minimum = 0; maximum = 187; unit = 'utf16-code-units'
            }
            extendedTextLength = [pscustomobject]@{
                minimum = 7; maximum = 8; unit = 'ascii-bytes'
            }
            normalizedModelSha256 = 'f9018758f35a351f2316a78db99f40141641050c9253d2f6ab7961c24c19201e'
            diagnosticCount = 0
        }
        limitations = @('Synthetic evidence does not prove runtime localization behavior.')
    }
}

if (-not (Test-Path -LiteralPath $wrapperPath -PathType Leaf) -or
    -not (Test-Path -LiteralPath $editorPath -PathType Leaf)) {
    throw 'The WP-02E controlled audit entry points are missing.'
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
        'RA2YR.CsfProjectBaselineAuditSanitized',
        '0xBD835079',
        '332973',
        '1b90bb0756137f46ff529af043fe798d7f1f9fa1713a4110f17e1d674de81f1c',
        'f9018758f35a351f2316a78db99f40141641050c9253d2f6ab7961c24c19201e',
        'langmd.mix',
        'ra2md.csf'
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
        'external CSF audit manifest escaped',
        'Unity process exit code'
    )) {
        if (-not $wrapperText.Contains($required)) {
            throw "The wrapper omits a required safety gate: $required"
        }
    }
    if ($wrapperText -match '(?i)XccMixer|XCC Mixer|global mix database') {
        throw 'The CSF audit wrapper must not depend on or start an external GUI tool.'
    }
}

Invoke-Case -Name 'Editor command uses Core service and atomic publication' -Body {
    foreach ($required in @(
        'CsfProjectBaselineAuditService.Run(configuration)',
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
$manifestType = 'RA2YR.CsfProjectBaselineAuditSanitized'
$expectedPayloadSha256 = '1b90bb0756137f46ff529af043fe798d7f1f9fa1713a4110f17e1d674de81f1c'
$expectedModelSha256 = 'f9018758f35a351f2316a78db99f40141641050c9253d2f6ab7961c24c19201e'

Invoke-Case -Name 'Synthetic sanitized summary passes' -Body {
    Assert-SanitizedSummary -Summary (New-SyntheticSummary)
}

Invoke-Case -Name 'Changed golden payload hash fails closed' -Body {
    $summary = New-SyntheticSummary
    $summary.csf.sha256 = ('0' * 64)
    Assert-Throws { Assert-SanitizedSummary $summary }
}

Invoke-Case -Name 'Changed model hash fails closed' -Body {
    $summary = New-SyntheticSummary
    $summary.csf.normalizedModelSha256 = ('0' * 64)
    Assert-Throws { Assert-SanitizedSummary $summary }
}

Invoke-Case -Name 'Changed MIX provenance fails closed' -Body {
    $summary = New-SyntheticSummary
    $summary.csf.provenance.layers[0].archive = 'other.mix'
    Assert-Throws { Assert-SanitizedSummary $summary }
}

Invoke-Case -Name 'Unexpected string body field fails closed' -Body {
    $summary = New-SyntheticSummary
    $summary.csf | Add-Member -NotePropertyName mainText -NotePropertyValue 'body'
    Assert-Throws { Assert-SanitizedSummary $summary }
}

Invoke-Case -Name 'Absolute host path fails closed' -Body {
    $summary = New-SyntheticSummary
    $summary.limitations = @('C:\private\original.csf')
    Assert-Throws { Assert-SanitizedSummary $summary }
}

"CSF audit wrapper regression tests passed: $passed"
exit 0
