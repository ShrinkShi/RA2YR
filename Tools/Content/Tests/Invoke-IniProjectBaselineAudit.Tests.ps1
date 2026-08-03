[CmdletBinding()]
param([string] $RepositoryRoot)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = Join-Path $PSScriptRoot '..\..\..'
}
$root = [IO.Path]::GetFullPath($RepositoryRoot)
$wrapperPath = Join-Path $root 'Tools\Content\Invoke-IniProjectBaselineAudit.ps1'
$runtimeWrapperPath = Join-Path $root 'Tools\Content\Invoke-IniRuntimeResolutionAudit.ps1'
$minimalWrapperPath = Join-Path $root 'Tools\Content\Invoke-IniMinimalResourceAudit.ps1'
$editorPath = Join-Path $root 'Assets\RA2YR\Editor\IniProjectBaselineAuditCommand.cs'
$utf8 = New-Object Text.UTF8Encoding($false, $true)
$passed = 0

function Invoke-Case {
    param([string] $Name, [scriptblock] $Body)
    & $Body
    $script:passed++
    "PASS $Name"
}

function Assert-Throws {
    param([scriptblock] $Body)
    $thrown = $false
    try { & $Body } catch { $thrown = $true }
    if (-not $thrown) { throw 'Expected validation to fail.' }
}

function New-SyntheticSample {
    param($Id, $Name, $MixId, $Length, $Hash, $ModelHash, $RootArchive, $Layers)
    [pscustomobject]@{
        sampleId = $Id; logicalName = $Name; mixId = $MixId
        provenance = [pscustomobject]@{
            sourceId = 'YR1001_ProjectBaseline'; rootArchive = $RootArchive
            layers = @(1..$Layers | ForEach-Object {
                [pscustomobject]@{ archive = $RootArchive; entryId = $MixId; resolvedName = $Name }
            })
        }
        length = $Length; sha256 = $Hash; bom = 'none'
        encodingObservation = 'raw-single-byte-bom-absent-code-page-unresolved'
        completeness = 'StructuredWithOpaqueLines'; lineCount = 1
        lineEndings = [pscustomobject]@{ crlf = 0; lf = 0; cr = 0; none = 1 }
        nodes = [pscustomobject]@{ section = 0; keyValue = 0; comment = 0; blank = 0; opaque = 1 }
        duplicateSectionsRawExact = 0; duplicateKeysRawExactWithinPhysicalSection = 0
        maximumLineBytes = 1; canonicalModelSha256 = $ModelHash
        identityOutputSha256 = $Hash; byteIdentical = $true
        diagnosticCounts = [pscustomobject]@{ OpaqueLinePreserved = 1 }
    }
}

function New-SyntheticSummary {
    $art = 'e1f0378394313c04ebbd5073f47785ee3e46f1b3c62d65724e8f3c310ee7ba31'
    $ai = '1feac6ddea6886b177ddf7e5f8580b7a99a63f12684f2cbb42831671bb7a8a79'
    $rulesA = '3d341ef8a13a4b5ab24af2eef48ac94931ac2bb87d950fe3330a07e2d25672ef'
    $rulesB = '06761dd7f714e7d9400216ec3c06109ec5c1461f6a0727be7401eb9d8b0f6d05'
    [pscustomobject]@{
        schemaVersion = 1; manifestType = 'RA2YR.IniProjectBaselineAuditSanitized'
        baselineLogicalName = 'YR1001_ProjectBaseline'; auditStatus = 'Complete'
        sourceVersion = 'synthetic'; directoryFingerprint = ('b' * 64)
        startedUtc = '2026-08-03T00:00:00Z'; completedUtc = '2026-08-03T00:00:01Z'
        externalManifest = [pscustomobject]@{
            schemaVersion = 1; cacheRelativePath = 'wp02f/ini-audits/test/manifest.json'
            length = 1; sha256 = ('c' * 64)
        }
        samples = @(
            (New-SyntheticSample 'artmd-localmd' 'artmd.ini' '0x5B47D8D5' 336535 $art 'd138e1443bb1797b95c23857de0fffc9900ffae6838b9cd79c42707af519a64d' 'ra2md.mix' 2),
            (New-SyntheticSample 'ai-local' 'ai.ini' '0x9E11E49A' 84972 $ai 'b41fec9d9331349126b32929abbf2d1d8e77ce3959a4cf2461c034324c72a361' 'ra2.mix' 2),
            (New-SyntheticSample 'rulesmd-expandmd01' 'rulesmd.ini' '0x8218F9F4' 743215 $rulesA '86fa33b1c844101ce6facb8df50e254ceb784bafb45880e0ce2f55fc3738d287' 'expandmd01.mix' 1),
            (New-SyntheticSample 'rulesmd-localmd' 'rulesmd.ini' '0x8218F9F4' 742958 $rulesB 'b5f97e861fa620bf2af96060c8216965f682c5ae24ca50cdd6bde3219ab224e1' 'ra2md.mix' 2)
        )
        survey = [pscustomobject]@{ located = @(); notLocatedInMountedDirectoryAndMixSources = @() }
        limitations = @('one', 'two', 'three', 'four')
    }
}

function New-SyntheticRuntimeCandidate {
    param($Name, $Hash, $RootArchive)
    [pscustomobject]@{
        logicalName = $Name; mixId = '0x00000001'
        provenance = [pscustomobject]@{
            sourceId = 'YR1001_ProjectBaseline'; rootArchive = $RootArchive
            layers = @([pscustomobject]@{
                archive = $RootArchive; entryId = '0x00000001'; resolvedName = $Name
            })
        }
        length = 1; sha256 = $Hash
    }
}

function New-SyntheticRuntimeSummary {
    $rulesA = New-SyntheticRuntimeCandidate 'rulesmd.ini' `
        '3d341ef8a13a4b5ab24af2eef48ac94931ac2bb87d950fe3330a07e2d25672ef' `
        'expandmd01.mix'
    $rulesB = New-SyntheticRuntimeCandidate 'rulesmd.ini' `
        '06761dd7f714e7d9400216ec3c06109ec5c1461f6a0727be7401eb9d8b0f6d05' `
        'ra2md.mix'
    $soundA = New-SyntheticRuntimeCandidate 'soundmd.ini' `
        '0a8e85381aef1a0f97074c953bfe99504da00c6220fae1a023a1afd857023232' `
        'expandmd01.mix'
    $soundB = New-SyntheticRuntimeCandidate 'soundmd.ini' `
        'd1be76491a0888396b4d0e53f4857f33879a5afd40a8bcea65ea1d1a3096d419' `
        'ra2md.mix'
    [pscustomobject]@{
        schemaVersion = 1; manifestType = 'RA2YR.IniRuntimeResolutionAuditSanitized'
        baselineLogicalName = 'YR1001_ProjectBaseline'; auditStatus = 'Complete'
        directoryFingerprint = ('b' * 64)
        candidateSets = @(
            [pscustomobject]@{ logicalName = 'rulesmd.ini'; candidateCount = 2
                candidates = @($rulesA, $rulesB); selectedWinner = $null
                winnerEvidence = 'Unresolved' },
            [pscustomobject]@{ logicalName = 'soundmd.ini'; candidateCount = 2
                candidates = @($soundA, $soundB); selectedWinner = $null
                winnerEvidence = 'Unresolved' }
        )
        syntaxAudits = @(1, 2, 3, 4)
        runtimeBoundary = [pscustomobject]@{
            genericExplicitLoadPlanExecutable = $true
            perValueCandidateChainImplemented = $true
            projectBaselineRuntimeWinnerSelected = $false
            originalRuntimeComparisonPassed = $false
            blackBoxAuthorization = 'not-granted-not-executed'
        }
        baseIniAuditExternalManifest = [pscustomobject]@{
            cacheRelativePath = 'wp02f/ini-audits/test/manifest.json'
            length = 1; sha256 = ('c' * 64)
        }
    }
}

function New-SyntheticMinimalSummary {
    $trace = [pscustomobject]@{ complete = 2; total = 2 }
    $ruleA = [pscustomobject]@{
        candidateRole = 'rulesmd-expandmd01'; typedStatus = 'Incomplete'
        registryTypeCount = 5; registryEntryCount = 2
        invalidIdentifierCount = 0; failedIdentifierCount = 0
        sourceTraceCoverage = $trace; normalizedModelSha256 = ('d' * 64)
        diagnosticCounts = [pscustomobject]@{}
    }
    $ruleB = [pscustomobject]@{
        candidateRole = 'rulesmd-localmd'; typedStatus = 'Incomplete'
        registryTypeCount = 5; registryEntryCount = 2
        invalidIdentifierCount = 0; failedIdentifierCount = 0
        sourceTraceCoverage = $trace; normalizedModelSha256 = ('e' * 64)
        diagnosticCounts = [pscustomobject]@{}
    }
    [pscustomobject]@{
        schemaVersion = 1
        manifestType = 'RA2YR.IniMinimalResourceProjectBaselineAuditSanitized'
        baselineLogicalName = 'YR1001_ProjectBaseline'; auditStatus = 'Complete'
        policyEvidence = 'ConfiguredForTesting'; stockRuntimeWinnerSelected = $false
        sourceVersion = 'synthetic'; directoryFingerprint = ('b' * 64)
        baseIniAuditExternalManifest = [pscustomobject]@{
            schemaVersion = 1; cacheRelativePath = 'wp02f/ini-audits/test/manifest.json'
            length = 1; sha256 = ('c' * 64)
        }
        startedUtc = '2026-08-03T00:00:00Z'; completedUtc = '2026-08-03T00:00:01Z'
        rulesCandidates = @($ruleA, $ruleB)
        artCandidate = [pscustomobject]@{
            candidateRole = 'artmd-localmd'; typedStatus = 'Incomplete'
            recordCount = 1; explicitImageCount = 1
            sourceTraceCoverage = [pscustomobject]@{ complete = 1; total = 1 }
            normalizedModelSha256 = ('f' * 64)
        }
        limitations = @('explicit-only')
    }
}

if (-not (Test-Path -LiteralPath $wrapperPath -PathType Leaf) -or
    -not (Test-Path -LiteralPath $runtimeWrapperPath -PathType Leaf) -or
    -not (Test-Path -LiteralPath $minimalWrapperPath -PathType Leaf) -or
    -not (Test-Path -LiteralPath $editorPath -PathType Leaf)) {
    throw 'The WP-02F controlled audit entry points are missing.'
}
$tokens = $null
$parseErrors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile(
    $wrapperPath,
    [ref]$tokens,
    [ref]$parseErrors)
if ($parseErrors.Count -ne 0) {
    throw (($parseErrors | ForEach-Object { $_.Message }) -join '; ')
}
$wrapperText = [IO.File]::ReadAllText($wrapperPath, $utf8)
$runtimeWrapperText = [IO.File]::ReadAllText($runtimeWrapperPath, $utf8)
$minimalWrapperText = [IO.File]::ReadAllText($minimalWrapperPath, $utf8)
$editorText = [IO.File]::ReadAllText($editorPath, $utf8)

Invoke-Case 'PowerShell parser accepts wrapper' {
    if ($null -eq $ast.ParamBlock) { throw 'Missing parameter block.' }
}
Invoke-Case 'Wrapper pins four golden identities' {
    foreach ($required in @(
        '2022.3.60f1c1', 'YR1001_ProjectBaseline', '0x5B47D8D5', '0x9E11E49A',
        '0x8218F9F4', '336535', '84972', '743215', '742958',
        'e1f0378394313c04ebbd5073f47785ee3e46f1b3c62d65724e8f3c310ee7ba31',
        '1feac6ddea6886b177ddf7e5f8580b7a99a63f12684f2cbb42831671bb7a8a79',
        '3d341ef8a13a4b5ab24af2eef48ac94931ac2bb87d950fe3330a07e2d25672ef',
        '06761dd7f714e7d9400216ec3c06109ec5c1461f6a0727be7401eb9d8b0f6d05')) {
        if (-not $wrapperText.Contains($required)) { throw "Missing identity: $required" }
    }
}
Invoke-Case 'Wrapper retains safety and real-exit gates' {
    foreach ($required in @(
        'Assert-NoExistingReparsePoint', 'Assert-GitIgnored', 'Open-LockedFileIdentity',
        'Assert-LockedFileUnchanged', 'Temp\UnityLockfile', 'Unity process exit code',
        'external INI audit manifest escaped')) {
        if (-not $wrapperText.Contains($required)) { throw "Missing gate: $required" }
    }
}
Invoke-Case 'Editor command uses controlled Core service' {
    foreach ($required in @(
        'IniProjectBaselineAuditService.Run(configuration)', 'WriteNewUtf8FileAtomically',
        'FileOptions.WriteThrough', 'TestResults', 'RejectSensitivePath')) {
        if (-not $editorText.Contains($required)) { throw "Missing editor boundary: $required" }
    }
}
Invoke-Case 'Runtime audit mode is exposed through the same safety wrapper' {
    foreach ($required in @(
        "ValidateSet('PhysicalDocument', 'RuntimeResolution', 'MinimalResource')",
        'RunRuntimeResolution', 'wp02g1-ini-runtime-resolution-summary.json',
        'baseIniAuditExternalManifest')) {
        if (-not $wrapperText.Contains($required)) { throw "Missing runtime gate: $required" }
    }
    if (-not $editorText.Contains('RunRuntimeResolutionAudit(configuration)')) {
        throw 'The editor runtime audit command does not call the controlled Core service.'
    }
    foreach ($required in @(
        'Invoke-IniProjectBaselineAudit.ps1', '-AuditMode RuntimeResolution')) {
        if (-not $runtimeWrapperText.Contains($required)) {
            throw "Missing runtime forwarding boundary: $required"
        }
    }
}
Invoke-Case 'Minimal resource mode is exposed through the same safety wrapper' {
    foreach ($required in @(
        'RunMinimalResourceTypedViews', 'wp02g2-ini-minimal-resource-summary.json',
        'stockRuntimeWinnerSelected', 'ConfiguredForTesting')) {
        if (-not $wrapperText.Contains($required)) { throw "Missing minimal gate: $required" }
    }
    if (-not $editorText.Contains('RunMinimalResourceTypedViewAudit(')) {
        throw 'The editor minimal audit command does not call the controlled Core service.'
    }
    foreach ($required in @(
        'Invoke-IniProjectBaselineAudit.ps1', '-AuditMode MinimalResource')) {
        if (-not $minimalWrapperText.Contains($required)) {
            throw "Missing minimal forwarding boundary: $required"
        }
    }
}

$functionAsts = @($ast.FindAll({
    param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst]
}, $true))
foreach ($functionAst in $functionAsts) { Invoke-Expression $functionAst.Extent.Text }
$baselineName = 'YR1001_ProjectBaseline'
$manifestType = 'RA2YR.IniProjectBaselineAuditSanitized'
$runtimeResolutionAudit = $false
$minimalResourceAudit = $false

Invoke-Case 'Synthetic sanitized summary passes' {
    $summary = New-SyntheticSummary
    Assert-SanitizedSummary $summary ($summary | ConvertTo-Json -Depth 8 -Compress)
}
Invoke-Case 'Changed rules candidate hash fails closed' {
    $summary = New-SyntheticSummary
    $summary.samples[2].sha256 = ('0' * 64)
    Assert-Throws { Assert-SanitizedSummary $summary ($summary | ConvertTo-Json -Depth 8 -Compress) }
}
Invoke-Case 'Changed provenance fails closed' {
    $summary = New-SyntheticSummary
    $summary.samples[0].provenance.rootArchive = 'other.mix'
    Assert-Throws { Assert-SanitizedSummary $summary ($summary | ConvertTo-Json -Depth 8 -Compress) }
}
Invoke-Case 'Original line records are rejected' {
    $summary = New-SyntheticSummary
    $raw = ($summary | ConvertTo-Json -Depth 8 -Compress).TrimEnd('}') + ',"lineRecords":[]}'
    Assert-Throws { Assert-SanitizedSummary $summary $raw }
}
Invoke-Case 'Absolute host paths are rejected' {
    $summary = New-SyntheticSummary
    $summary.limitations[0] = 'C:\private\original.ini'
    Assert-Throws { Assert-SanitizedSummary $summary ($summary | ConvertTo-Json -Depth 8 -Compress) }
}

Invoke-Case 'Synthetic runtime resolution summary passes' {
    $script:manifestType = 'RA2YR.IniRuntimeResolutionAuditSanitized'
    $script:runtimeResolutionAudit = $true
    $summary = New-SyntheticRuntimeSummary
    Assert-SanitizedSummary $summary ($summary | ConvertTo-Json -Depth 8 -Compress)
    $script:runtimeResolutionAudit = $false
    $script:manifestType = 'RA2YR.IniProjectBaselineAuditSanitized'
}
Invoke-Case 'Runtime winner selection fails closed' {
    $script:manifestType = 'RA2YR.IniRuntimeResolutionAuditSanitized'
    $script:runtimeResolutionAudit = $true
    $summary = New-SyntheticRuntimeSummary
    $summary.candidateSets[0].selectedWinner = $summary.candidateSets[0].candidates[0]
    Assert-Throws { Assert-SanitizedSummary $summary ($summary | ConvertTo-Json -Depth 8 -Compress) }
    $script:runtimeResolutionAudit = $false
    $script:manifestType = 'RA2YR.IniProjectBaselineAuditSanitized'
}

Invoke-Case 'Synthetic minimal resource summary passes' {
    $script:manifestType = 'RA2YR.IniMinimalResourceProjectBaselineAuditSanitized'
    $script:minimalResourceAudit = $true
    $summary = New-SyntheticMinimalSummary
    Assert-SanitizedSummary $summary ($summary | ConvertTo-Json -Depth 8 -Compress)
    $script:minimalResourceAudit = $false
    $script:manifestType = 'RA2YR.IniProjectBaselineAuditSanitized'
}
Invoke-Case 'Minimal resource stock winner selection fails closed' {
    $script:manifestType = 'RA2YR.IniMinimalResourceProjectBaselineAuditSanitized'
    $script:minimalResourceAudit = $true
    $summary = New-SyntheticMinimalSummary
    $summary.stockRuntimeWinnerSelected = $true
    Assert-Throws { Assert-SanitizedSummary $summary ($summary | ConvertTo-Json -Depth 8 -Compress) }
    $script:minimalResourceAudit = $false
    $script:manifestType = 'RA2YR.IniProjectBaselineAuditSanitized'
}

"INI audit wrapper regression tests passed: $passed"
exit 0
