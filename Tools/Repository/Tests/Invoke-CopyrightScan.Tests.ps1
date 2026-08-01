[CmdletBinding()]
param(
    [Parameter()]
    [string[]] $PowerShellExecutable
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$repositoryToolsRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$projectRoot = [IO.Path]::GetFullPath((Join-Path $repositoryToolsRoot '..\..'))
$utf8NoBom = New-Object System.Text.UTF8Encoding($false, $true)
$testRoot = Join-Path ([IO.Path]::GetTempPath()) ("RA2YR-CopyrightScanTests-" + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($testRoot) | Out-Null

$passed = 0
$newlinePathCovered = $false

function Invoke-GitChecked {
    param(
        [Parameter(Mandatory)][string] $Repository,
        [Parameter(Mandatory)][string[]] $Arguments
    )

    $previousErrorActionPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $output = @(& git -C $Repository @Arguments 2>&1)
        $exitCode = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }
    if ($exitCode -ne 0) {
        throw "git $($Arguments -join ' ') failed: $($output -join [Environment]::NewLine)"
    }
    ($output -join "`n").Trim()
}

function Write-TestBytes {
    param(
        [Parameter(Mandatory)][string] $Repository,
        [Parameter(Mandatory)][string] $RelativePath,
        [Parameter(Mandatory)][AllowEmptyCollection()][byte[]] $Bytes
    )

    $path = Join-Path $Repository ($RelativePath.Replace('/', [IO.Path]::DirectorySeparatorChar))
    $parent = Split-Path -Parent $path
    [IO.Directory]::CreateDirectory($parent) | Out-Null
    [IO.File]::WriteAllBytes($path, $Bytes)
    $path
}

function Write-TestText {
    param(
        [Parameter(Mandatory)][string] $Repository,
        [Parameter(Mandatory)][string] $RelativePath,
        [Parameter(Mandatory)][string] $Text
    )

    $bytes = $script:utf8NoBom.GetBytes($Text)
    Write-TestBytes -Repository $Repository -RelativePath $RelativePath -Bytes $bytes
}

function Get-TestSha256 {
    param([Parameter(Mandatory)][byte[]] $Bytes)

    $algorithm = [Security.Cryptography.SHA256]::Create()
    try {
        (($algorithm.ComputeHash($Bytes) | ForEach-Object { $_.ToString('X2') }) -join '')
    } finally {
        $algorithm.Dispose()
    }
}

function New-TestRepository {
    param([Parameter(Mandatory)][string] $Name)

    $repository = Join-Path $script:testRoot ($Name + '-' + [Guid]::NewGuid().ToString('N'))
    [IO.Directory]::CreateDirectory((Join-Path $repository 'Tools\Repository')) | Out-Null
    Copy-Item -LiteralPath (Join-Path $script:repositoryToolsRoot 'Invoke-CopyrightScan.ps1') -Destination (Join-Path $repository 'Tools\Repository\Invoke-CopyrightScan.ps1')
    Copy-Item -LiteralPath (Join-Path $script:repositoryToolsRoot 'CopyrightPolicy.json') -Destination (Join-Path $repository 'Tools\Repository\CopyrightPolicy.json')
    Copy-Item -LiteralPath (Join-Path $script:projectRoot '.gitignore') -Destination (Join-Path $repository '.gitignore')
    Invoke-GitChecked -Repository $repository -Arguments @('init', '-q') | Out-Null
    Invoke-GitChecked -Repository $repository -Arguments @('config', 'user.name', 'RA2YR Scanner Tests') | Out-Null
    Invoke-GitChecked -Repository $repository -Arguments @('config', 'user.email', 'scanner-tests@invalid.example') | Out-Null
    $repository
}

function Set-TestPolicy {
    param(
        [Parameter(Mandatory)][string] $Repository,
        [Parameter(Mandatory)][scriptblock] $Mutation
    )

    $path = Join-Path $Repository 'Tools\Repository\CopyrightPolicy.json'
    $policy = Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json
    & $Mutation $policy
    $json = $policy | ConvertTo-Json -Depth 12
    [IO.File]::WriteAllText($path, $json, $script:utf8NoBom)
}

function Invoke-TestScanner {
    param(
        [Parameter(Mandatory)][string] $Repository,
        [Parameter(Mandatory)][string] $Executable
    )

    $scanner = Join-Path $Repository 'Tools\Repository\Invoke-CopyrightScan.ps1'
    $output = @(& $Executable -NoProfile -ExecutionPolicy Bypass -File $scanner -RepositoryRoot $Repository -Json 2>&1)
    $exitCode = $LASTEXITCODE
    $text = ($output | ForEach-Object { [string]$_ }) -join "`n"
    try {
        $result = $text | ConvertFrom-Json
    } catch {
        throw "Scanner did not return JSON. Exit=$exitCode Output=$text"
    }
    $debugDetail = $null
    if ($exitCode -ne 0 -and @($result.Violations | ForEach-Object { $_.Rule }) -contains 'fatal-scanner-error') {
        $debugOutput = @(& $Executable -NoProfile -ExecutionPolicy Bypass -File $scanner -RepositoryRoot $Repository -Json -IncludeAbsolutePaths 2>&1)
        try {
            $debugResult = (($debugOutput | ForEach-Object { [string]$_ }) -join "`n") | ConvertFrom-Json
            $debugDetail = @($debugResult.Violations | ForEach-Object { $_.Detail }) -join '; '
        } catch {
            $debugDetail = ($debugOutput | ForEach-Object { [string]$_ }) -join "`n"
        }
    }
    [pscustomobject]@{ ExitCode = $exitCode; Result = $result; DebugDetail = $debugDetail }
}

function Assert-Scan {
    param(
        [Parameter(Mandatory)] $Scan,
        [Parameter(Mandatory)][bool] $ShouldPass,
        [Parameter()][string[]] $RequiredRules = @()
    )

    $resultPassed = [bool]$Scan.Result.Passed
    $violationSummary = @($Scan.Result.Violations | ForEach-Object {
        "{0}:{1}:{2}" -f $_.Rule, $_.Path, $_.Detail
    }) -join '; '
    if ($ShouldPass) {
        if ($Scan.ExitCode -ne 0 -or -not $resultPassed) {
            throw "Expected exit=0 and result=true, got exit=$($Scan.ExitCode), result=$resultPassed. Violations: $violationSummary"
        }
    } elseif ($Scan.ExitCode -eq 0 -or $resultPassed) {
        throw "Expected nonzero exit and result=false, got exit=$($Scan.ExitCode), result=$resultPassed. Violations: $violationSummary"
    }
    foreach ($requiredRule in $RequiredRules) {
        if (@($Scan.Result.Violations | ForEach-Object { $_.Rule }) -notcontains $requiredRule) {
            $actualRules = @($Scan.Result.Violations | ForEach-Object { $_.Rule }) -join ', '
            throw "Expected scanner rule '$requiredRule' was not reported. Actual rules: $actualRules. Debug: $($Scan.DebugDetail)"
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
    throw 'No PowerShell executable was found for scanner regression tests.'
}

try {
    foreach ($executableInput in $PowerShellExecutable) {
        $executableCommand = Get-Command $executableInput -ErrorAction Stop
        $executable = $executableCommand.Source
        "HOST $executable"

        Invoke-Case -Name 'NUL paths: Unicode, spaces, and newline when supported' -Body {
            $repo = New-TestRepository -Name 'paths'
            Write-TestText -Repository $repo -RelativePath '目录/中文 空格.txt' -Text 'independently generated text' | Out-Null
            try {
                Write-TestText -Repository $repo -RelativePath "目录/含`n换行.txt" -Text 'newline path' | Out-Null
                $script:newlinePathCovered = $true
            } catch [System.ArgumentException], [System.NotSupportedException], [System.IO.IOException] {
                # Some filesystems reject newline characters; Unicode and spaces remain covered.
            }
            Assert-Scan -Scan (Invoke-TestScanner -Repository $repo -Executable $executable) -ShouldPass $true
        }

        Invoke-Case -Name 'Staged blob survives worktree deletion' -Body {
            $repo = New-TestRepository -Name 'staged-deleted'
            $path = Write-TestBytes -Repository $repo -RelativePath 'staged/generated.shp' -Bytes ([byte[]](1, 2, 3))
            Invoke-GitChecked -Repository $repo -Arguments @('add', '--', 'staged/generated.shp') | Out-Null
            Remove-Item -LiteralPath $path -Force
            Assert-Scan -Scan (Invoke-TestScanner -Repository $repo -Executable $executable) -ShouldPass $false -RequiredRules @('forbidden-extension')
        }

        Invoke-Case -Name 'Force-added ignored external path' -Body {
            $repo = New-TestRepository -Name 'force-add'
            Write-TestText -Repository $repo -RelativePath 'ExternalContent/generated.txt' -Text 'synthetic external-path probe' | Out-Null
            Invoke-GitChecked -Repository $repo -Arguments @('add', '-f', '--', 'ExternalContent/generated.txt') | Out-Null
            Assert-Scan -Scan (Invoke-TestScanner -Repository $repo -Executable $executable) -ShouldPass $false -RequiredRules @('forbidden-directory')
        }

        Invoke-Case -Name 'Ignored physical external root still fails' -Body {
            $repo = New-TestRepository -Name 'ignored-physical-root'
            Write-TestBytes -Repository $repo -RelativePath 'ExternalContent/probe.mix' -Bytes ([byte[]](10, 11, 12)) | Out-Null
            $ignored = Invoke-GitChecked -Repository $repo -Arguments @('check-ignore', '--', 'ExternalContent/probe.mix')
            if ($ignored -ne 'ExternalContent/probe.mix') {
                throw 'The external-root regression fixture was not ignored as expected.'
            }
            Assert-Scan -Scan (Invoke-TestScanner -Repository $repo -Executable $executable) -ShouldPass $false -RequiredRules @('forbidden-physical-root')
        }

        Invoke-Case -Name 'Every required ignore probe is checked' -Body {
            $repo = New-TestRepository -Name 'missing-ignore-probe'
            $ignorePath = Join-Path $repo '.gitignore'
            $ignoreText = [IO.File]::ReadAllText($ignorePath, $script:utf8NoBom)
            [IO.File]::WriteAllText($ignorePath, $ignoreText.Replace("/ExternalContent/`n", ''), $script:utf8NoBom)
            Assert-Scan -Scan (Invoke-TestScanner -Repository $repo -Executable $executable) -ShouldPass $false -RequiredRules @('required-ignore-missing')
        }

        Invoke-Case -Name 'Index symlink and gitlink modes' -Body {
            $repo = New-TestRepository -Name 'index-modes'
            $target = Write-TestText -Repository $repo -RelativePath 'generated-target.txt' -Text 'generated target'
            $blob = Invoke-GitChecked -Repository $repo -Arguments @('hash-object', '-w', '--', $target)
            Invoke-GitChecked -Repository $repo -Arguments @('update-index', '--add', '--cacheinfo', '120000', $blob, 'generated-link') | Out-Null
            Invoke-GitChecked -Repository $repo -Arguments @('add', '--', '.gitignore') | Out-Null
            Invoke-GitChecked -Repository $repo -Arguments @('commit', '-q', '-m', 'Synthetic test baseline') | Out-Null
            $commit = Invoke-GitChecked -Repository $repo -Arguments @('rev-parse', 'HEAD')
            Invoke-GitChecked -Repository $repo -Arguments @('update-index', '--add', '--cacheinfo', '160000', $commit, 'generated-gitlink') | Out-Null
            Assert-Scan -Scan (Invoke-TestScanner -Repository $repo -Executable $executable) -ShouldPass $false -RequiredRules @('index-symlink', 'index-gitlink')
        }

        Invoke-Case -Name 'Unregistered restricted synthetic fixture' -Body {
            $repo = New-TestRepository -Name 'unregistered'
            Write-TestBytes -Repository $repo -RelativePath 'Assets/RA2YR/Tests/Fixtures/Synthetic/generated.shp' -Bytes ([byte[]](4, 5, 6)) | Out-Null
            Assert-Scan -Scan (Invoke-TestScanner -Repository $repo -Executable $executable) -ShouldPass $false -RequiredRules @('unregistered-synthetic-fixture', 'forbidden-extension')
        }

        Invoke-Case -Name 'Exact synthetic registration and changed bytes' -Body {
            $repo = New-TestRepository -Name 'registered'
            $relative = 'Assets/RA2YR/Tests/Fixtures/Synthetic/generated.shp'
            $original = [byte[]](7, 8, 9)
            Write-TestBytes -Repository $repo -RelativePath $relative -Bytes $original | Out-Null
            $sha = Get-TestSha256 -Bytes $original
            Set-TestPolicy -Repository $repo -Mutation {
                param($policy)
                $policy.syntheticFixtures = @([pscustomobject]@{
                    path = $relative
                    sha256 = $sha
                    generator = 'Invoke-CopyrightScan.Tests.ps1'
                    provenance = 'Independent bytes 07 08 09 generated for scanner regression testing'
                })
            }
            Assert-Scan -Scan (Invoke-TestScanner -Repository $repo -Executable $executable) -ShouldPass $true
            Write-TestBytes -Repository $repo -RelativePath $relative -Bytes ([byte[]](7, 8, 10)) | Out-Null
            Assert-Scan -Scan (Invoke-TestScanner -Repository $repo -Executable $executable) -ShouldPass $false -RequiredRules @('synthetic-hash-mismatch')
        }

        Invoke-Case -Name 'Renamed forbidden binary signature' -Body {
            $repo = New-TestRepository -Name 'signature'
            Write-TestBytes -Repository $repo -RelativePath 'renamed.data' -Bytes ([byte[]](0x50, 0x4B, 0x03, 0x04, 1, 2, 3)) | Out-Null
            Assert-Scan -Scan (Invoke-TestScanner -Repository $repo -Executable $executable) -ShouldPass $false -RequiredRules @('forbidden-magic')
        }

        Invoke-Case -Name 'Known rejected SHA-256 after rename' -Body {
            $repo = New-TestRepository -Name 'known-hash'
            $bytes = $script:utf8NoBom.GetBytes('independent known-hash test payload')
            $sha = Get-TestSha256 -Bytes $bytes
            Write-TestBytes -Repository $repo -RelativePath 'innocent-name.data' -Bytes $bytes | Out-Null
            Set-TestPolicy -Repository $repo -Mutation {
                param($policy)
                $policy.knownRejectedSha256 = @([pscustomobject]@{
                    sha256 = $sha
                    label = 'Synthetic regression-test rejected hash'
                })
            }
            Assert-Scan -Scan (Invoke-TestScanner -Repository $repo -Executable $executable) -ShouldPass $false -RequiredRules @('known-rejected-sha256')
        }

        Invoke-Case -Name 'Malformed policy fails closed without absolute JSON path' -Body {
            $repo = New-TestRepository -Name 'bad-policy'
            $policyPath = Join-Path $repo 'Tools\Repository\CopyrightPolicy.json'
            [IO.File]::WriteAllText($policyPath, '{invalid', $script:utf8NoBom)
            $scan = Invoke-TestScanner -Repository $repo -Executable $executable
            Assert-Scan -Scan $scan -ShouldPass $false -RequiredRules @('fatal-scanner-error')
            if ($scan.Result.RepositoryRoot -ne '.') {
                throw 'Default JSON output disclosed an absolute repository root.'
            }
        }
    }

    "Scanner regression tests passed: $passed"
    "Embedded-newline path covered: $newlinePathCovered"
} finally {
    $resolvedTestRoot = [IO.Path]::GetFullPath($testRoot)
    $resolvedSystemTemp = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if (-not $resolvedTestRoot.StartsWith($resolvedSystemTemp, [StringComparison]::OrdinalIgnoreCase) -or
        [IO.Path]::GetFileName($resolvedTestRoot) -notlike 'RA2YR-CopyrightScanTests-*') {
        throw "Refusing to clean an unexpected test path: $resolvedTestRoot"
    }
    if (Test-Path -LiteralPath $resolvedTestRoot) {
        Remove-Item -LiteralPath $resolvedTestRoot -Recurse -Force
    }
}
