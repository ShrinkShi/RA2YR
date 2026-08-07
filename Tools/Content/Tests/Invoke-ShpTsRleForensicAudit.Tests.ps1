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
$root = [IO.Path]::GetFullPath($RepositoryRoot)
$wrapperPath = Join-Path $root 'Tools\Content\Invoke-ShpTsRleForensicAudit.ps1'
$editorPath = Join-Path $root 'Assets\RA2YR\Editor\ShpTsRleForensicAuditCommand.cs'
$servicePath = Join-Path $root 'Assets\RA2YR\Core\Content\ShpTs\Forensics\ShpTsRleForensicAuditService.cs'
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

if (-not (Test-Path -LiteralPath $wrapperPath -PathType Leaf) -or
    -not (Test-Path -LiteralPath $editorPath -PathType Leaf) -or
    -not (Test-Path -LiteralPath $servicePath -PathType Leaf)) {
    throw 'The M2-SHP1F controlled entry points are missing.'
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
$serviceText = [IO.File]::ReadAllText($servicePath, $utf8)

Invoke-Case -Name 'PowerShell parser accepts wrapper' -Body {
    if ($null -eq $ast.ParamBlock) {
        throw 'The wrapper does not declare a parameter block.'
    }
}

Invoke-Case -Name 'Wrapper pins Stage A and Stage B aggregates' -Body {
    foreach ($required in @(
        '2022.3.60f1c1',
        'YR1001_ProjectBaseline',
        'RA2YR.ShpTsRleForensicSanitized',
        'RleOutputOverflow',
        '257',
        '9495',
        '1331',
        '8164',
        "decision -ne 'B'"
    )) {
        if (-not $wrapperText.Contains($required)) {
            throw "The wrapper does not pin required evidence: $required"
        }
    }
}

Invoke-Case -Name 'Wrapper retains path and process safety gates' -Body {
    foreach ($required in @(
        'Assert-NoExistingReparsePoint',
        'Assert-GitIgnored',
        'Temp\UnityLockfile',
        'Open-LockedFileIdentity',
        'Assert-LockedFileUnchanged',
        'Unity process exit code',
        'ForcedPostResultShutdown'
    )) {
        if (-not $wrapperText.Contains($required)) {
            throw "The wrapper omits a required safety gate: $required"
        }
    }
    if ($wrapperText -match '(?i)XccMixer|XCC Mixer|FinalAlert|gamemd\.exe') {
        throw 'The forensic wrapper must not start external game or GUI tools.'
    }
}

Invoke-Case -Name 'Editor command publishes only sanitized TestResults output' -Body {
    foreach ($required in @(
        'ShpTsRleForensicAuditService.Run(configuration)',
        'ExpectedStageAFrameCount = 257',
        'WriteNewUtf8FileAtomically',
        'FileOptions.WriteThrough',
        'File.Move(temporaryPath, path)',
        'TestResults',
        'RejectSensitivePath'
    )) {
        if (-not $editorText.Contains($required)) {
            throw "The Editor command omits a required boundary: $required"
        }
    }
}

Invoke-Case -Name 'Service leaves production decoder semantics unchanged' -Body {
    foreach ($required in @(
        'ValidateBaselineLockSnapshot',
        'BaselineProbeInputDrift',
        'ShpTsRleForensicDecision.A1',
        'ShpTsRleForensicDecision.B',
        'ShpTsRleForensicDecision.C',
        'ShpTsRleForensicDecision.D'
    )) {
        if (-not $serviceText.Contains($required)) {
            throw "The forensic service omits a required fail-closed gate: $required"
        }
    }
    if ($serviceText.Contains('WidthRaw + 1') -or
        $serviceText.Contains('drop-last') -or
        $serviceText.Contains('clamp')) {
        throw 'The forensic service contains a prohibited production repair shortcut.'
    }
}

Invoke-Case -Name 'Analyzer remains independent of production row decode' -Body {
    $analyzerPath = Join-Path $root 'Assets\RA2YR\Core\Content\ShpTs\Forensics\ShpTsRleForensicAnalyzer.cs'
    $analyzerText = [IO.File]::ReadAllText($analyzerPath, $utf8)
    foreach ($prohibited in @(
        'WestwoodShpTsDecoder',
        'ShpTsSyntheticFixtureFactory',
        'IndexedLocalFrame',
        'byte[] Indices'
    )) {
        if ($analyzerText.Contains($prohibited)) {
            throw "The independent analyzer references prohibited code or output: $prohibited"
        }
    }
}

"SHP RLE forensic wrapper regression tests passed: $passed"
exit 0
