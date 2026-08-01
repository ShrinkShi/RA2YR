[CmdletBinding()]
param(
    [Parameter()]
    [string[]] $PowerShellExecutable
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$repositoryToolsRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$validatorPath = Join-Path $repositoryToolsRoot 'Invoke-RepositoryValidation.ps1'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false, $true)
$testRoot = Join-Path ([IO.Path]::GetTempPath()) ("RA2YR-RepositoryValidationTests-" + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($testRoot) | Out-Null
$passed = 0

function New-TextFromCodePoints {
    param([Parameter(Mandatory)][int[]] $CodePoints)

    -join @($CodePoints | ForEach-Object { [char]$_ })
}

$statusUnimplemented = New-TextFromCodePoints @(0x672A, 0x5B9E, 0x73B0)
$statusParsed = New-TextFromCodePoints @(0x53EF, 0x89E3, 0x6790)
$statusDisplay = New-TextFromCodePoints @(0x53EF, 0x663E, 0x793A)
$statusExecutable = New-TextFromCodePoints @(0x53EF, 0x6267, 0x884C)
$statusApproximate = New-TextFromCodePoints @(0x884C, 0x4E3A, 0x8FD1, 0x4F3C)
$statusOriginalPassed = New-TextFromCodePoints @(0x539F, 0x7248, 0x5BF9, 0x7167, 0x901A, 0x8FC7)
$statusRoundTripPassed = New-TextFromCodePoints @(0x5F80, 0x8FD4, 0x901A, 0x8FC7)
$statusLimitation = New-TextFromCodePoints @(0x5DF2, 0x77E5, 0x9650, 0x5236)

function Write-TestText {
    param(
        [Parameter(Mandatory)][string] $Root,
        [Parameter(Mandatory)][string] $RelativePath,
        [Parameter(Mandatory)][AllowEmptyString()][string] $Text
    )

    $path = Join-Path $Root ($RelativePath.Replace('/', [IO.Path]::DirectorySeparatorChar))
    $parent = Split-Path -Parent $path
    [IO.Directory]::CreateDirectory($parent) | Out-Null
    [IO.File]::WriteAllText($path, $Text, $script:utf8NoBom)
    $path
}

function Read-TestText {
    param(
        [Parameter(Mandatory)][string] $Root,
        [Parameter(Mandatory)][string] $RelativePath
    )

    $path = Join-Path $Root ($RelativePath.Replace('/', [IO.Path]::DirectorySeparatorChar))
    [IO.File]::ReadAllText($path, $script:utf8NoBom)
}

function Write-Meta {
    param(
        [Parameter(Mandatory)][string] $Root,
        [Parameter(Mandatory)][string] $TargetRelativePath,
        [Parameter(Mandatory)][string] $Guid
    )

    Write-TestText -Root $Root -RelativePath ($TargetRelativePath + '.meta') -Text ("fileFormatVersion: 2`nguid: $Guid`n") | Out-Null
}

function New-ValidMatrixText {
    @"
schema_version: 1
last_updated: "2026-08-01"
baseline:
  game: "Synthetic game"
  content_role: "Synthetic content role"
  content_path_role: "ExternalContent/Synthetic"
  observed_workspace_path: "../Synthetic"
  content_manifest: null
  finalalert2:
    executable: "FinalAlert2.exe"
    version: "Synthetic"
    sha256: "BE939988780428271377C7592E0552E405C5982BA6BB7F468DE76CE5117F619D"
status_vocabulary:
  implementation:
    - "$script:statusUnimplemented"
    - "$script:statusParsed"
    - "$script:statusDisplay"
    - "$script:statusExecutable"
    - "$script:statusApproximate"
  original_comparison:
    - "$script:statusUnimplemented"
    - "$script:statusOriginalPassed"
  roundtrip:
    - "$script:statusUnimplemented"
    - "$script:statusRoundTripPassed"
  limitation_flag: "$script:statusLimitation"
entry_schema:
  required:
    - id
    - domain
    - feature
    - status
    - tests
    - evidence
    - limitations
  status_fields:
    - implementation
    - original_comparison
    - roundtrip
entries:
  - id: synthetic.valid
    domain: synthetic
    feature: "Synthetic validation entry"
    status: { implementation: "$script:statusParsed", original_comparison: "$script:statusUnimplemented", roundtrip: "$script:statusUnimplemented" }
    tests:
      - "Synthetic.Tests.Pass"
    evidence:
      - "docs/compatibility/evidence/synthetic.yml#proof"
    limitations: []
"@
}

function New-ValidFixture {
    param([Parameter(Mandatory)][string] $Name)

    $root = Join-Path $script:testRoot ($Name + '-' + [Guid]::NewGuid().ToString('N'))
    [IO.Directory]::CreateDirectory((Join-Path $root 'Assets\RA2YR\Core')) | Out-Null
    Write-Meta -Root $root -TargetRelativePath 'Assets/RA2YR' -Guid '00000000000000000000000000000001'
    Write-Meta -Root $root -TargetRelativePath 'Assets/RA2YR/Core' -Guid '00000000000000000000000000000002'

    $asmdef = @"
{
    "name": "RA2YR.Core",
    "references": [],
    "noEngineReferences": true
}
"@
    Write-TestText -Root $root -RelativePath 'Assets/RA2YR/Core/RA2YR.Core.asmdef' -Text $asmdef | Out-Null
    Write-Meta -Root $root -TargetRelativePath 'Assets/RA2YR/Core/RA2YR.Core.asmdef' -Guid '00000000000000000000000000000003'
    Write-TestText -Root $root -RelativePath 'Assets/RA2YR/Core/Synthetic.cs' -Text "namespace Synthetic { internal sealed class Value { } }`n" | Out-Null
    Write-Meta -Root $root -TargetRelativePath 'Assets/RA2YR/Core/Synthetic.cs' -Guid '00000000000000000000000000000004'

    Write-TestText -Root $root -RelativePath 'ProjectSettings/ProjectVersion.txt' -Text "m_EditorVersion: 2022.3.60f1c1`nm_EditorVersionWithRevision: 2022.3.60f1c1 (synthetic)`n" | Out-Null
    Write-TestText -Root $root -RelativePath 'docs/compatibility/matrix.yml' -Text (New-ValidMatrixText) | Out-Null
    Write-TestText -Root $root -RelativePath 'docs/compatibility/evidence/synthetic.yml' -Text "schema_version: 1`nproof:`n  result: synthetic`n" | Out-Null
    $root
}

function Invoke-TestValidator {
    param(
        [Parameter(Mandatory)][string] $Repository,
        [Parameter(Mandatory)][string] $Executable
    )

    $output = @(& $Executable -NoProfile -ExecutionPolicy Bypass -File $script:validatorPath -RepositoryRoot $Repository -Json 2>&1)
    $exitCode = $LASTEXITCODE
    $text = ($output | ForEach-Object { [string]$_ }) -join "`n"
    try {
        $result = $text | ConvertFrom-Json
    } catch {
        throw "Validator did not return JSON. Exit=$exitCode Output=$text"
    }
    [pscustomobject]@{ ExitCode = $exitCode; Result = $result }
}

function Assert-Validation {
    param(
        [Parameter(Mandatory)] $Validation,
        [Parameter(Mandatory)][bool] $ShouldPass,
        [Parameter()][string[]] $RequiredRules = @()
    )

    $resultPassed = [bool]$Validation.Result.Passed
    if ($ShouldPass) {
        if ($Validation.ExitCode -ne 0 -or -not $resultPassed) {
            throw "Expected exit=0 and result=true, got exit=$($Validation.ExitCode), result=$resultPassed."
        }
    } elseif ($Validation.ExitCode -eq 0 -or $resultPassed) {
        throw "Expected nonzero exit and result=false, got exit=$($Validation.ExitCode), result=$resultPassed."
    }
    foreach ($requiredRule in $RequiredRules) {
        if (@($Validation.Result.Violations | ForEach-Object { $_.Rule }) -notcontains $requiredRule) {
            $actualRules = @($Validation.Result.Violations | ForEach-Object { $_.Rule }) -join ', '
            throw "Expected rule '$requiredRule'. Actual rules: $actualRules"
        }
    }
}

function Invoke-Case {
    param(
        [Parameter(Mandatory)][string] $Name,
        [Parameter(Mandatory)][scriptblock] $Body
    )

    & $Body
    $script:passed++
    "PASS $Name"
}

function Set-MatrixText {
    param(
        [Parameter(Mandatory)][string] $Repository,
        [Parameter(Mandatory)][scriptblock] $Mutation
    )

    $text = Read-TestText -Root $Repository -RelativePath 'docs/compatibility/matrix.yml'
    $updated = & $Mutation $text
    Write-TestText -Root $Repository -RelativePath 'docs/compatibility/matrix.yml' -Text $updated | Out-Null
}

if ($null -eq $PowerShellExecutable -or $PowerShellExecutable.Count -eq 0) {
    $detected = New-Object System.Collections.Generic.List[string]
    foreach ($name in @('powershell.exe', 'pwsh.exe')) {
        $command = Get-Command $name -ErrorAction SilentlyContinue
        if ($null -ne $command -and -not $detected.Contains($command.Source)) {
            $detected.Add($command.Source)
        }
    }
    $PowerShellExecutable = @($detected)
}
if ($PowerShellExecutable.Count -eq 0) {
    throw 'No PowerShell executable was found for repository validation tests.'
}

try {
    foreach ($executableInput in $PowerShellExecutable) {
        $executable = (Get-Command $executableInput -ErrorAction Stop).Source
        "HOST $executable"

        Invoke-Case -Name 'Valid repository fixture' -Body {
            $repo = New-ValidFixture -Name 'valid'
            Assert-Validation -Validation (Invoke-TestValidator -Repository $repo -Executable $executable) -ShouldPass $true
        }

        Invoke-Case -Name 'Missing meta' -Body {
            $repo = New-ValidFixture -Name 'missing-meta'
            Remove-Item -LiteralPath (Join-Path $repo 'Assets\RA2YR\Core\Synthetic.cs.meta') -Force
            Assert-Validation -Validation (Invoke-TestValidator -Repository $repo -Executable $executable) -ShouldPass $false -RequiredRules @('meta-missing')
        }

        Invoke-Case -Name 'Orphan meta' -Body {
            $repo = New-ValidFixture -Name 'orphan-meta'
            Remove-Item -LiteralPath (Join-Path $repo 'Assets\RA2YR\Core\Synthetic.cs') -Force
            Assert-Validation -Validation (Invoke-TestValidator -Repository $repo -Executable $executable) -ShouldPass $false -RequiredRules @('meta-orphan')
        }

        Invoke-Case -Name 'Duplicate meta GUID' -Body {
            $repo = New-ValidFixture -Name 'duplicate-guid'
            Write-Meta -Root $repo -TargetRelativePath 'Assets/RA2YR/Core/Synthetic.cs' -Guid '00000000000000000000000000000003'
            Assert-Validation -Validation (Invoke-TestValidator -Repository $repo -Executable $executable) -ShouldPass $false -RequiredRules @('meta-guid-duplicate')
        }

        Invoke-Case -Name 'Malformed meta GUID' -Body {
            $repo = New-ValidFixture -Name 'malformed-guid'
            Write-Meta -Root $repo -TargetRelativePath 'Assets/RA2YR/Core/Synthetic.cs' -Guid 'not-a-guid'
            Assert-Validation -Validation (Invoke-TestValidator -Repository $repo -Executable $executable) -ShouldPass $false -RequiredRules @('meta-guid')
        }

        Invoke-Case -Name 'Wrong Unity version' -Body {
            $repo = New-ValidFixture -Name 'wrong-version'
            Write-TestText -Root $repo -RelativePath 'ProjectSettings/ProjectVersion.txt' -Text "m_EditorVersion: 2022.3.59f1`n" | Out-Null
            Assert-Validation -Validation (Invoke-TestValidator -Repository $repo -Executable $executable) -ShouldPass $false -RequiredRules @('unity-version')
        }

        Invoke-Case -Name 'Core noEngineReferences false' -Body {
            $repo = New-ValidFixture -Name 'engine-reference-flag'
            $path = 'Assets/RA2YR/Core/RA2YR.Core.asmdef'
            $text = (Read-TestText -Root $repo -RelativePath $path).Replace('"noEngineReferences": true', '"noEngineReferences": false')
            Write-TestText -Root $repo -RelativePath $path -Text $text | Out-Null
            Assert-Validation -Validation (Invoke-TestValidator -Repository $repo -Executable $executable) -ShouldPass $false -RequiredRules @('core-engine-boundary')
        }

        Invoke-Case -Name 'Core Unity API reference' -Body {
            $repo = New-ValidFixture -Name 'core-unity-reference'
            Write-TestText -Root $repo -RelativePath 'Assets/RA2YR/Core/Synthetic.cs' -Text "using UnityEngine;`nusing UnityEditor;`n" | Out-Null
            Assert-Validation -Validation (Invoke-TestValidator -Repository $repo -Executable $executable) -ShouldPass $false -RequiredRules @('core-unity-reference')
        }

        Invoke-Case -Name 'Unsupported matrix schema' -Body {
            $repo = New-ValidFixture -Name 'matrix-schema'
            Set-MatrixText -Repository $repo -Mutation { param($text) $text.Replace('schema_version: 1', 'schema_version: 2') }
            Assert-Validation -Validation (Invoke-TestValidator -Repository $repo -Executable $executable) -ShouldPass $false -RequiredRules @('matrix-schema')
        }

        Invoke-Case -Name 'Duplicate matrix schema version' -Body {
            $repo = New-ValidFixture -Name 'duplicate-matrix-schema'
            Set-MatrixText -Repository $repo -Mutation { param($text) "schema_version: 1`n" + $text }
            Assert-Validation -Validation (Invoke-TestValidator -Repository $repo -Executable $executable) -ShouldPass $false -RequiredRules @('matrix-schema')
        }

        Invoke-Case -Name 'Unknown matrix top-level key' -Body {
            $repo = New-ValidFixture -Name 'unknown-matrix-top-level'
            Set-MatrixText -Repository $repo -Mutation { param($text) $text.TrimEnd("`r", "`n") + "`nunknown_top_level: true`n" }
            Assert-Validation -Validation (Invoke-TestValidator -Repository $repo -Executable $executable) -ShouldPass $false -RequiredRules @('matrix-schema')
        }

        Invoke-Case -Name 'Duplicate matrix section' -Body {
            $repo = New-ValidFixture -Name 'duplicate-matrix-section'
            Set-MatrixText -Repository $repo -Mutation { param($text) $text.TrimEnd("`r", "`n") + "`nentries:`n" }
            Assert-Validation -Validation (Invoke-TestValidator -Repository $repo -Executable $executable) -ShouldPass $false -RequiredRules @('matrix-schema')
        }

        Invoke-Case -Name 'Unowned indented matrix content' -Body {
            $repo = New-ValidFixture -Name 'unowned-matrix-content'
            Set-MatrixText -Repository $repo -Mutation { param($text) "  orphan: true`n" + $text }
            Assert-Validation -Validation (Invoke-TestValidator -Repository $repo -Executable $executable) -ShouldPass $false -RequiredRules @('matrix-schema')
        }

        Invoke-Case -Name 'Unsupported matrix top-level syntax' -Body {
            $repo = New-ValidFixture -Name 'unsupported-matrix-top-level'
            Set-MatrixText -Repository $repo -Mutation { param($text) "orphan`n" + $text }
            Assert-Validation -Validation (Invoke-TestValidator -Repository $repo -Executable $executable) -ShouldPass $false -RequiredRules @('matrix-schema')
        }

        Invoke-Case -Name 'Duplicate matrix entry ID' -Body {
            $repo = New-ValidFixture -Name 'duplicate-entry'
            Set-MatrixText -Repository $repo -Mutation {
                param($text)
                $entryStart = $text.IndexOf('  - id: synthetic.valid')
                $text.TrimEnd("`r", "`n") + "`n" + $text.Substring($entryStart)
            }
            Assert-Validation -Validation (Invoke-TestValidator -Repository $repo -Executable $executable) -ShouldPass $false -RequiredRules @('matrix-id-duplicate')
        }

        Invoke-Case -Name 'Invalid matrix status' -Body {
            $repo = New-ValidFixture -Name 'invalid-status'
            Set-MatrixText -Repository $repo -Mutation {
                param($text)
                $text.Replace("status: { implementation: `"$script:statusParsed`"", 'status: { implementation: "invalid"')
            }
            Assert-Validation -Validation (Invoke-TestValidator -Repository $repo -Executable $executable) -ShouldPass $false -RequiredRules @('matrix-status-value')
        }

        Invoke-Case -Name 'Missing matrix evidence field' -Body {
            $repo = New-ValidFixture -Name 'missing-field'
            Set-MatrixText -Repository $repo -Mutation {
                param($text)
                $text.Replace("    evidence:`n      - `"docs/compatibility/evidence/synthetic.yml#proof`"`n", '')
            }
            Assert-Validation -Validation (Invoke-TestValidator -Repository $repo -Executable $executable) -ShouldPass $false -RequiredRules @('matrix-entry-field')
        }

        Invoke-Case -Name 'Missing matrix tests field' -Body {
            $repo = New-ValidFixture -Name 'missing-tests-field'
            Set-MatrixText -Repository $repo -Mutation {
                param($text)
                $text.Replace("    tests:`n      - `"Synthetic.Tests.Pass`"`n", '')
            }
            Assert-Validation -Validation (Invoke-TestValidator -Repository $repo -Executable $executable) -ShouldPass $false -RequiredRules @('matrix-entry-field')
        }

        Invoke-Case -Name 'Empty matrix test item' -Body {
            $repo = New-ValidFixture -Name 'empty-test-item'
            Set-MatrixText -Repository $repo -Mutation {
                param($text)
                $text.Replace('      - "Synthetic.Tests.Pass"', '      - ""')
            }
            Assert-Validation -Validation (Invoke-TestValidator -Repository $repo -Executable $executable) -ShouldPass $false -RequiredRules @('matrix-entry-list')
        }

        Invoke-Case -Name 'Missing matrix limitations field' -Body {
            $repo = New-ValidFixture -Name 'missing-limitations-field'
            Set-MatrixText -Repository $repo -Mutation {
                param($text)
                $text.Replace('    limitations: []', '')
            }
            Assert-Validation -Validation (Invoke-TestValidator -Repository $repo -Executable $executable) -ShouldPass $false -RequiredRules @('matrix-entry-field')
        }

        Invoke-Case -Name 'Missing evidence file' -Body {
            $repo = New-ValidFixture -Name 'missing-evidence'
            Remove-Item -LiteralPath (Join-Path $repo 'docs\compatibility\evidence\synthetic.yml') -Force
            Assert-Validation -Validation (Invoke-TestValidator -Repository $repo -Executable $executable) -ShouldPass $false -RequiredRules @('matrix-evidence-missing')
        }

        Invoke-Case -Name 'Evidence path escape' -Body {
            $repo = New-ValidFixture -Name 'evidence-escape'
            Set-MatrixText -Repository $repo -Mutation {
                param($text)
                $text.Replace('docs/compatibility/evidence/synthetic.yml#proof', '../outside.yml')
            }
            Assert-Validation -Validation (Invoke-TestValidator -Repository $repo -Executable $executable) -ShouldPass $false -RequiredRules @('matrix-evidence-path')
        }

        Invoke-Case -Name 'Missing evidence fragment' -Body {
            $repo = New-ValidFixture -Name 'evidence-fragment'
            Set-MatrixText -Repository $repo -Mutation {
                param($text)
                $text.Replace('synthetic.yml#proof', 'synthetic.yml#missing')
            }
            Assert-Validation -Validation (Invoke-TestValidator -Repository $repo -Executable $executable) -ShouldPass $false -RequiredRules @('matrix-evidence-fragment')
        }
    }

    "Repository validation regression tests passed: $passed"
} finally {
    $resolvedTestRoot = [IO.Path]::GetFullPath($testRoot)
    $resolvedSystemTemp = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if (-not $resolvedTestRoot.StartsWith($resolvedSystemTemp, [StringComparison]::OrdinalIgnoreCase) -or
        [IO.Path]::GetFileName($resolvedTestRoot) -notlike 'RA2YR-RepositoryValidationTests-*') {
        throw "Refusing to clean an unexpected test path: $resolvedTestRoot"
    }
    if (Test-Path -LiteralPath $resolvedTestRoot) {
        Remove-Item -LiteralPath $resolvedTestRoot -Recurse -Force
    }
}

exit 0
