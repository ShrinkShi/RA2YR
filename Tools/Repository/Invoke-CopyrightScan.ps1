[CmdletBinding()]
param(
    [Parameter()]
    [string] $RepositoryRoot,

    [Parameter()]
    [switch] $Json,

    [Parameter()]
    [switch] $IncludeAbsolutePaths
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Invoke-GitRaw {
    param(
        [Parameter(Mandatory)]
        [string] $WorkingDirectory,

        [Parameter(Mandatory)]
        [string] $Arguments,

        [Parameter()]
        [byte[]] $InputBytes
    )

    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = 'git'
    $startInfo.Arguments = $Arguments
    $startInfo.WorkingDirectory = $WorkingDirectory
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.RedirectStandardInput = $true

    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $startInfo
    $stdout = New-Object System.IO.MemoryStream

    try {
        $process.Start() | Out-Null
        $stdoutTask = $process.StandardOutput.BaseStream.CopyToAsync($stdout)
        $stderrTask = $process.StandardError.ReadToEndAsync()

        if ($null -ne $InputBytes -and $InputBytes.Length -gt 0) {
            $process.StandardInput.BaseStream.Write($InputBytes, 0, $InputBytes.Length)
        }
        $process.StandardInput.Close() | Out-Null

        $process.WaitForExit() | Out-Null
        $stdoutTask.GetAwaiter().GetResult() | Out-Null
        $stderr = $stderrTask.GetAwaiter().GetResult()

        return ,([pscustomobject]@{
            ExitCode = $process.ExitCode
            Bytes = $stdout.ToArray()
            Stderr = $stderr
        })
    } finally {
        $stdout.Dispose()
        $process.Dispose()
    }
}

function ConvertFrom-Utf8Strict {
    param(
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [byte[]] $Bytes,

        [Parameter(Mandatory)]
        [string] $Purpose
    )

    $encoding = New-Object System.Text.UTF8Encoding($false, $true)
    try {
        $encoding.GetString($Bytes)
    } catch {
        throw "Git returned invalid UTF-8 while reading $Purpose."
    }
}

function ConvertFrom-NulList {
    param(
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [byte[]] $Bytes,

        [Parameter(Mandatory)]
        [string] $Purpose
    )

    if ($Bytes.Length -eq 0) {
        return @()
    }

    $text = ConvertFrom-Utf8Strict -Bytes $Bytes -Purpose $Purpose
    if ($text[$text.Length - 1] -ne [char]0) {
        throw "Git returned an unterminated NUL list while reading $Purpose."
    }

    @($text.Substring(0, $text.Length - 1).Split([char]0))
}

function Get-Sha256Hex {
    param([Parameter(Mandatory)][AllowEmptyCollection()][byte[]] $Bytes)

    $algorithm = [System.Security.Cryptography.SHA256]::Create()
    try {
        (($algorithm.ComputeHash($Bytes) | ForEach-Object { $_.ToString('X2') }) -join '')
    } finally {
        $algorithm.Dispose()
    }
}

function ConvertFrom-Hex {
    param([Parameter(Mandatory)][string] $Hex)

    if ($Hex -notmatch '^[0-9A-Fa-f]+$' -or ($Hex.Length % 2) -ne 0) {
        throw "Invalid hexadecimal signature '$Hex'."
    }

    $bytes = New-Object byte[] ($Hex.Length / 2)
    for ($index = 0; $index -lt $bytes.Length; $index++) {
        $bytes[$index] = [Convert]::ToByte($Hex.Substring($index * 2, 2), 16)
    }
    $bytes
}

function Test-BytePrefix {
    param(
        [Parameter(Mandatory)][AllowEmptyCollection()][byte[]] $Bytes,
        [Parameter(Mandatory)][byte[]] $Prefix
    )

    if ($Bytes.Length -lt $Prefix.Length) {
        return $false
    }

    for ($index = 0; $index -lt $Prefix.Length; $index++) {
        if ($Bytes[$index] -ne $Prefix[$index]) {
            return $false
        }
    }
    $true
}

function Test-PeSignature {
    param([Parameter(Mandatory)][AllowEmptyCollection()][byte[]] $Bytes)

    if ($Bytes.Length -lt 64 -or $Bytes[0] -ne 0x4D -or $Bytes[1] -ne 0x5A) {
        return $false
    }

    $peOffset = [BitConverter]::ToInt32($Bytes, 0x3C)
    if ($peOffset -lt 0 -or $peOffset -gt ($Bytes.Length - 4)) {
        return $false
    }

    $Bytes[$peOffset] -eq 0x50 -and
        $Bytes[$peOffset + 1] -eq 0x45 -and
        $Bytes[$peOffset + 2] -eq 0x00 -and
        $Bytes[$peOffset + 3] -eq 0x00
}

function Get-MagicMatches {
    param(
        [Parameter(Mandatory)][AllowEmptyCollection()][byte[]] $Bytes,
        [Parameter(Mandatory)][object[]] $Rules
    )

    $matches = New-Object System.Collections.Generic.List[string]
    foreach ($rule in $Rules) {
        $matched = $false
        switch ([string]$rule.matcher) {
            'prefix' {
                $matched = Test-BytePrefix -Bytes $Bytes -Prefix (ConvertFrom-Hex -Hex ([string]$rule.hex))
            }
            'pe' {
                $matched = Test-PeSignature -Bytes $Bytes
            }
            'riff-wave' {
                $matched = $Bytes.Length -ge 12 -and
                    [Text.Encoding]::ASCII.GetString($Bytes, 0, 4) -eq 'RIFF' -and
                    [Text.Encoding]::ASCII.GetString($Bytes, 8, 4) -eq 'WAVE'
            }
            default {
                throw "Unsupported magic matcher '$($rule.matcher)'."
            }
        }

        if ($matched) {
            $matches.Add([string]$rule.id)
        }
    }
    @($matches)
}

function Normalize-RepositoryPath {
    param(
        [Parameter(Mandatory)][string] $Path,
        [Parameter(Mandatory)][string] $Purpose
    )

    $normalized = $Path.Replace('\', '/')
    if ([string]::IsNullOrWhiteSpace($normalized) -or
        [IO.Path]::IsPathRooted($normalized) -or
        $normalized.StartsWith('/', [StringComparison]::Ordinal)) {
        throw "$Purpose is not a repository-relative path."
    }

    $segments = @($normalized.Split('/'))
    foreach ($segment in $segments) {
        if ([string]::IsNullOrEmpty($segment) -or $segment -eq '.' -or $segment -eq '..') {
            throw "$Purpose contains an unsafe path segment."
        }
    }
    $normalized
}

function Test-PathInsideRoot {
    param(
        [Parameter(Mandatory)][string] $Root,
        [Parameter(Mandatory)][string] $Candidate
    )

    $comparison = if ($env:OS -eq 'Windows_NT') {
        [StringComparison]::OrdinalIgnoreCase
    } else {
        [StringComparison]::Ordinal
    }
    $rootWithSeparator = $Root.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    $Candidate.StartsWith($rootWithSeparator, $comparison)
}

function Get-ReparseSegment {
    param(
        [Parameter(Mandatory)][string] $Root,
        [Parameter(Mandatory)][string] $RelativePath
    )

    $current = $Root
    $rootItem = Get-Item -LiteralPath $Root -Force
    if ($rootItem.Attributes -band [IO.FileAttributes]::ReparsePoint) {
        return '.'
    }

    foreach ($segment in $RelativePath.Split('/')) {
        $current = Join-Path $current $segment
        if (-not (Test-Path -LiteralPath $current)) {
            break
        }
        $item = Get-Item -LiteralPath $current -Force
        if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) {
            return $segment
        }
    }
    $null
}

function Assert-Policy {
    param([Parameter(Mandatory)] $Policy)

    $requiredProperties = @(
        'schemaVersion', 'maximumCandidateFileBytes', 'syntheticFixtureRoot',
        'syntheticFixtures', 'forbiddenPhysicalRootDirectories',
        'knownRejectedSha256', 'forbiddenMagicSignatures',
        'forbiddenDirectories', 'forbiddenExtensions', 'forbiddenFileNames',
        'requiredIgnoredProbes'
    )
    foreach ($property in $requiredProperties) {
        if (-not ($Policy.PSObject.Properties.Name -contains $property)) {
            throw "Copyright policy is missing '$property'."
        }
    }

    if ([int]$Policy.schemaVersion -ne 2) {
        throw "Unsupported copyright policy schema '$($Policy.schemaVersion)'."
    }

    $maximumBytes = 0L
    if (-not [int64]::TryParse([string]$Policy.maximumCandidateFileBytes, [ref]$maximumBytes) -or $maximumBytes -le 0) {
        throw 'maximumCandidateFileBytes must be a positive integer.'
    }

    $fixtureRoot = Normalize-RepositoryPath -Path ([string]$Policy.syntheticFixtureRoot).TrimEnd('/') -Purpose 'syntheticFixtureRoot'
    $fixtureRoot = $fixtureRoot + '/'
    $fixturePaths = New-Object 'System.Collections.Generic.Dictionary[string,object]' ([StringComparer]::Ordinal)
    foreach ($fixture in @($Policy.syntheticFixtures)) {
        foreach ($property in @('path', 'sha256', 'generator', 'provenance')) {
            if (-not ($fixture.PSObject.Properties.Name -contains $property) -or
                [string]::IsNullOrWhiteSpace([string]$fixture.$property)) {
                throw "Synthetic fixture registration is missing '$property'."
            }
        }
        $path = Normalize-RepositoryPath -Path ([string]$fixture.path) -Purpose 'synthetic fixture path'
        if (-not $path.StartsWith($fixtureRoot, [StringComparison]::Ordinal)) {
            throw "Synthetic fixture '$path' is outside syntheticFixtureRoot."
        }
        if ([string]$fixture.sha256 -notmatch '^[0-9A-Fa-f]{64}$') {
            throw "Synthetic fixture '$path' has an invalid SHA-256."
        }
        if ($fixturePaths.ContainsKey($path)) {
            throw "Synthetic fixture '$path' is registered more than once."
        }
        $fixturePaths.Add($path, $fixture)
    }

    $physicalRootComparer = if ($env:OS -eq 'Windows_NT') {
        [StringComparer]::OrdinalIgnoreCase
    } else {
        [StringComparer]::Ordinal
    }
    $physicalRoots = New-Object 'System.Collections.Generic.HashSet[string]' ($physicalRootComparer)
    foreach ($directoryValue in @($Policy.forbiddenPhysicalRootDirectories)) {
        $directoryName = Normalize-RepositoryPath -Path ([string]$directoryValue) -Purpose 'forbidden physical root directory'
        if ($directoryName.Contains('/') -or -not $physicalRoots.Add($directoryName)) {
            throw "forbiddenPhysicalRootDirectories contains an invalid or duplicate root '$directoryName'."
        }
    }

    $knownHashes = New-Object 'System.Collections.Generic.Dictionary[string,string]' ([StringComparer]::OrdinalIgnoreCase)
    foreach ($entry in @($Policy.knownRejectedSha256)) {
        if (-not ($entry.PSObject.Properties.Name -contains 'sha256') -or
            -not ($entry.PSObject.Properties.Name -contains 'label') -or
            [string]$entry.sha256 -notmatch '^[0-9A-Fa-f]{64}$' -or
            [string]::IsNullOrWhiteSpace([string]$entry.label)) {
            throw 'knownRejectedSha256 contains an invalid entry.'
        }
        if ($knownHashes.ContainsKey([string]$entry.sha256)) {
            throw "Known rejected SHA-256 '$($entry.sha256)' is duplicated."
        }
        $knownHashes.Add(([string]$entry.sha256).ToUpperInvariant(), [string]$entry.label)
    }

    $magicIds = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    foreach ($rule in @($Policy.forbiddenMagicSignatures)) {
        if (-not ($rule.PSObject.Properties.Name -contains 'id') -or
            -not ($rule.PSObject.Properties.Name -contains 'matcher') -or
            [string]::IsNullOrWhiteSpace([string]$rule.id) -or
            -not $magicIds.Add([string]$rule.id)) {
            throw 'forbiddenMagicSignatures contains a missing or duplicate id.'
        }
        if (@('prefix', 'pe', 'riff-wave') -notcontains [string]$rule.matcher) {
            throw "Unsupported magic matcher '$($rule.matcher)'."
        }
        if ([string]$rule.matcher -eq 'prefix') {
            if (-not ($rule.PSObject.Properties.Name -contains 'hex')) {
                throw "Prefix rule '$($rule.id)' is missing hex."
            }
            ConvertFrom-Hex -Hex ([string]$rule.hex) | Out-Null
        }
    }

    [pscustomobject]@{
        MaximumBytes = $maximumBytes
        FixtureRoot = $fixtureRoot
        FixturePaths = $fixturePaths
        PhysicalRoots = $physicalRoots
        KnownHashes = $knownHashes
        MagicRules = @($Policy.forbiddenMagicSignatures)
    }
}

function Add-Violation {
    param(
        [Parameter(Mandatory)][AllowEmptyCollection()][System.Collections.Generic.List[object]] $List,
        [Parameter(Mandatory)][string] $Path,
        [Parameter(Mandatory)][string] $Source,
        [Parameter(Mandatory)][string] $Rule,
        [Parameter(Mandatory)][string] $Detail
    )

    $List.Add([pscustomobject]@{
        Path = $Path
        Source = $Source
        Rule = $Rule
        Detail = $Detail
    })
}

$resolvedRootForError = $null
try {
    if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
        $RepositoryRoot = Join-Path $PSScriptRoot '..\..'
    }
    $resolvedRootForError = [IO.Path]::GetFullPath($RepositoryRoot)
    if (-not (Test-Path -LiteralPath $resolvedRootForError -PathType Container)) {
        throw 'Repository root does not exist or is not a directory.'
    }

    $rootResult = Invoke-GitRaw -WorkingDirectory $resolvedRootForError -Arguments 'rev-parse --show-toplevel'
    if ($rootResult.ExitCode -ne 0) {
        throw 'Unable to resolve the Git repository root.'
    }
    $rootText = ConvertFrom-Utf8Strict -Bytes $rootResult.Bytes -Purpose 'repository root'
    $gitRoot = [IO.Path]::GetFullPath($rootText.TrimEnd("`r", "`n"))

    $comparison = if ($env:OS -eq 'Windows_NT') {
        [StringComparison]::OrdinalIgnoreCase
    } else {
        [StringComparison]::Ordinal
    }
    if (-not $gitRoot.Equals($resolvedRootForError, $comparison)) {
        throw 'Repository root mismatch.'
    }

    $policyPath = Join-Path $PSScriptRoot 'CopyrightPolicy.json'
    if (-not (Test-Path -LiteralPath $policyPath -PathType Leaf)) {
        throw 'Copyright policy file is missing.'
    }
    try {
        $policy = Get-Content -LiteralPath $policyPath -Raw -Encoding UTF8 | ConvertFrom-Json
    } catch {
        throw 'Copyright policy could not be parsed.'
    }
    $validatedPolicy = Assert-Policy -Policy $policy

    $violations = New-Object System.Collections.Generic.List[object]
    $candidates = New-Object System.Collections.Generic.List[object]

    try {
        foreach ($rootDirectory in @(Get-ChildItem -LiteralPath $gitRoot -Force -Directory)) {
            if ($validatedPolicy.PhysicalRoots.Contains($rootDirectory.Name)) {
                Add-Violation -List $violations -Path $rootDirectory.Name -Source 'worktree' -Rule 'forbidden-physical-root' -Detail 'External content and tool roots must be physically outside the formal repository.'
            }
        }
    } catch {
        throw 'Repository root directories could not be inspected.'
    }

    $indexResult = Invoke-GitRaw -WorkingDirectory $gitRoot -Arguments 'ls-files --stage -z'
    if ($indexResult.ExitCode -ne 0) {
        throw 'Unable to enumerate Git index entries.'
    }
    foreach ($record in @(ConvertFrom-NulList -Bytes $indexResult.Bytes -Purpose 'Git index entries')) {
        if ($record -notmatch '^(?<mode>[0-9]{6}) (?<oid>[0-9A-Fa-f]+) (?<stage>[0-3])\t(?<path>.*)$') {
            throw 'Git returned an unparseable index entry.'
        }
        $path = Normalize-RepositoryPath -Path $Matches.path -Purpose 'Git index path'
        $candidates.Add([pscustomobject]@{
            Path = $path
            Source = 'index'
            Mode = $Matches.mode
            Oid = $Matches.oid
            Stage = [int]$Matches.stage
        })
    }

    $untrackedResult = Invoke-GitRaw -WorkingDirectory $gitRoot -Arguments 'ls-files --others --exclude-standard -z'
    if ($untrackedResult.ExitCode -ne 0) {
        throw 'Unable to enumerate untracked worktree files.'
    }
    foreach ($rawPath in @(ConvertFrom-NulList -Bytes $untrackedResult.Bytes -Purpose 'untracked worktree files')) {
        $path = Normalize-RepositoryPath -Path $rawPath -Purpose 'untracked worktree path'
        $candidates.Add([pscustomobject]@{
            Path = $path
            Source = 'untracked'
            Mode = $null
            Oid = $null
            Stage = 0
        })
    }

    $forbiddenDirectories = @($policy.forbiddenDirectories | ForEach-Object { ([string]$_).ToLowerInvariant() })
    $forbiddenExtensions = @($policy.forbiddenExtensions | ForEach-Object { ([string]$_).ToLowerInvariant() })
    $forbiddenFileNames = @($policy.forbiddenFileNames | ForEach-Object { ([string]$_).ToLowerInvariant() })

    foreach ($candidate in $candidates) {
        $path = [string]$candidate.Path
        $source = [string]$candidate.Source
        $segments = @($path.Split('/'))
        foreach ($segment in $segments) {
            if ($forbiddenDirectories -contains $segment.ToLowerInvariant()) {
                Add-Violation -List $violations -Path $path -Source $source -Rule 'forbidden-directory' -Detail $segment
                break
            }
        }

        $absolutePath = [IO.Path]::GetFullPath((Join-Path $gitRoot ($path.Replace('/', [IO.Path]::DirectorySeparatorChar))))
        if (-not (Test-PathInsideRoot -Root $gitRoot -Candidate $absolutePath)) {
            Add-Violation -List $violations -Path $path -Source $source -Rule 'outside-repository' -Detail 'Path escaped the repository root.'
            continue
        }

        try {
            $reparseSegment = Get-ReparseSegment -Root $gitRoot -RelativePath $path
            if ($null -ne $reparseSegment) {
                Add-Violation -List $violations -Path $path -Source $source -Rule 'reparse-point' -Detail "Candidate or ancestor '$reparseSegment' is a reparse point."
            }
        } catch {
            Add-Violation -List $violations -Path $path -Source $source -Rule 'path-inspection-failed' -Detail 'Candidate ancestry could not be inspected.'
            continue
        }

        $bytes = $null
        if ($source -eq 'index') {
            if ($candidate.Stage -ne 0) {
                Add-Violation -List $violations -Path $path -Source $source -Rule 'unresolved-index-stage' -Detail "Index stage $($candidate.Stage) is not committable."
                continue
            }
            if ($candidate.Mode -eq '120000') {
                Add-Violation -List $violations -Path $path -Source $source -Rule 'index-symlink' -Detail 'Git index mode 120000 is prohibited.'
                continue
            }
            if ($candidate.Mode -eq '160000') {
                Add-Violation -List $violations -Path $path -Source $source -Rule 'index-gitlink' -Detail 'Git index mode 160000 is prohibited.'
                continue
            }
            if (@('100644', '100755') -notcontains [string]$candidate.Mode) {
                Add-Violation -List $violations -Path $path -Source $source -Rule 'unsupported-index-mode' -Detail "Git index mode $($candidate.Mode) is prohibited."
                continue
            }
            if ([string]$candidate.Oid -notmatch '^[0-9A-Fa-f]{40,64}$') {
                Add-Violation -List $violations -Path $path -Source $source -Rule 'invalid-index-object-id' -Detail 'Index object id could not be parsed.'
                continue
            }

            $typeResult = Invoke-GitRaw -WorkingDirectory $gitRoot -Arguments "cat-file -t $($candidate.Oid)"
            if ($typeResult.ExitCode -ne 0 -or (ConvertFrom-Utf8Strict -Bytes $typeResult.Bytes -Purpose 'Git object type').Trim() -ne 'blob') {
                Add-Violation -List $violations -Path $path -Source $source -Rule 'missing-index-blob' -Detail 'Index object is missing or is not a blob.'
                continue
            }
            $sizeResult = Invoke-GitRaw -WorkingDirectory $gitRoot -Arguments "cat-file -s $($candidate.Oid)"
            $blobSize = 0L
            if ($sizeResult.ExitCode -ne 0 -or
                -not [int64]::TryParse((ConvertFrom-Utf8Strict -Bytes $sizeResult.Bytes -Purpose 'Git blob size').Trim(), [ref]$blobSize) -or
                $blobSize -lt 0) {
                Add-Violation -List $violations -Path $path -Source $source -Rule 'unreadable-index-blob-size' -Detail 'Index blob size could not be read.'
                continue
            }
            if ($blobSize -gt $validatedPolicy.MaximumBytes) {
                Add-Violation -List $violations -Path $path -Source $source -Rule 'oversized-file' -Detail "$blobSize bytes"
                continue
            }
            $blobResult = Invoke-GitRaw -WorkingDirectory $gitRoot -Arguments "cat-file blob $($candidate.Oid)"
            if ($blobResult.ExitCode -ne 0 -or $blobResult.Bytes.Length -ne $blobSize) {
                Add-Violation -List $violations -Path $path -Source $source -Rule 'unreadable-index-blob' -Detail 'Index blob bytes were missing or incomplete.'
                continue
            }
            $bytes = $blobResult.Bytes
        } else {
            try {
                if (-not (Test-Path -LiteralPath $absolutePath -PathType Leaf)) {
                    Add-Violation -List $violations -Path $path -Source $source -Rule 'missing-worktree-file' -Detail 'Untracked candidate disappeared before it could be scanned.'
                    continue
                }
                $before = Get-Item -LiteralPath $absolutePath -Force
                if ($before.Length -gt $validatedPolicy.MaximumBytes) {
                    Add-Violation -List $violations -Path $path -Source $source -Rule 'oversized-file' -Detail "$($before.Length) bytes"
                    continue
                }
                $bytes = [IO.File]::ReadAllBytes($absolutePath)
                $after = Get-Item -LiteralPath $absolutePath -Force
                if ($before.Length -ne $after.Length -or
                    $before.LastWriteTimeUtc.Ticks -ne $after.LastWriteTimeUtc.Ticks -or
                    $bytes.Length -ne $after.Length) {
                    Add-Violation -List $violations -Path $path -Source $source -Rule 'worktree-file-changed' -Detail 'Candidate changed while it was being scanned.'
                    continue
                }
            } catch {
                Add-Violation -List $violations -Path $path -Source $source -Rule 'unreadable-worktree-file' -Detail 'Untracked candidate could not be read completely.'
                continue
            }
        }

        $sha256 = Get-Sha256Hex -Bytes $bytes
        if ($validatedPolicy.KnownHashes.ContainsKey($sha256)) {
            Add-Violation -List $violations -Path $path -Source $source -Rule 'known-rejected-sha256' -Detail $validatedPolicy.KnownHashes[$sha256]
        }

        $magicMatches = @(Get-MagicMatches -Bytes $bytes -Rules $validatedPolicy.MagicRules)
        $registration = $null
        $registered = $validatedPolicy.FixturePaths.TryGetValue($path, [ref]$registration)
        $registrationMatches = $registered -and ([string]$registration.sha256).Equals($sha256, [StringComparison]::OrdinalIgnoreCase)
        if ($registered -and -not $registrationMatches) {
            Add-Violation -List $violations -Path $path -Source $source -Rule 'synthetic-hash-mismatch' -Detail 'Bytes do not match the registered synthetic fixture SHA-256.'
        }

        $extension = [IO.Path]::GetExtension($path).ToLowerInvariant()
        $fileName = [IO.Path]::GetFileName($path).ToLowerInvariant()
        $hasRestrictedClassification = ($forbiddenExtensions -contains $extension) -or
            ($forbiddenFileNames -contains $fileName) -or
            $magicMatches.Count -gt 0

        if ($hasRestrictedClassification -and -not $registrationMatches) {
            if ($path.StartsWith($validatedPolicy.FixtureRoot, [StringComparison]::Ordinal) -and -not $registered) {
                Add-Violation -List $violations -Path $path -Source $source -Rule 'unregistered-synthetic-fixture' -Detail 'Restricted fixture has no exact policy registration.'
            }
            if ($forbiddenExtensions -contains $extension) {
                Add-Violation -List $violations -Path $path -Source $source -Rule 'forbidden-extension' -Detail $extension
            }
            if ($forbiddenFileNames -contains $fileName) {
                Add-Violation -List $violations -Path $path -Source $source -Rule 'forbidden-file-name' -Detail $fileName
            }
            foreach ($magic in $magicMatches) {
                Add-Violation -List $violations -Path $path -Source $source -Rule 'forbidden-magic' -Detail $magic
            }
        }
    }

    $ignoredProbeResults = New-Object System.Collections.Generic.List[object]
    foreach ($probeValue in @($policy.requiredIgnoredProbes)) {
        $probePath = Normalize-RepositoryPath -Path ([string]$probeValue) -Purpose 'required ignore probe'
        if ($probePath -notmatch '^[A-Za-z0-9._/-]+$') {
            throw 'Required ignore probes must use command-safe ASCII repository paths.'
        }
        $ignoreResult = Invoke-GitRaw -WorkingDirectory $gitRoot -Arguments ("check-ignore --no-index -- " + $probePath)
        if (@(0, 1) -notcontains $ignoreResult.ExitCode) {
            throw 'Git failed while evaluating a required ignore probe.'
        }
        $isIgnored = $ignoreResult.ExitCode -eq 0
        $ignoredProbeResults.Add([pscustomobject]@{ Path = $probePath; Ignored = $isIgnored })
        if (-not $isIgnored) {
            Add-Violation -List $violations -Path $probePath -Source 'policy' -Rule 'required-ignore-missing' -Detail 'Defensive ignore rule did not match.'
        }
    }

    $reportedRoot = if ($IncludeAbsolutePaths) { $gitRoot } else { '.' }
    $result = [pscustomobject]@{
        SchemaVersion = 2
        RepositoryRoot = $reportedRoot
        IndexCandidateFileCount = @($candidates.ToArray() | Where-Object Source -eq 'index').Count
        UntrackedCandidateFileCount = @($candidates.ToArray() | Where-Object Source -eq 'untracked').Count
        CandidateFileCount = $candidates.Count
        IgnoredExternalProbeCount = @($ignoredProbeResults.ToArray() | Where-Object Ignored).Count
        RequiredExternalProbeCount = $ignoredProbeResults.Count
        ViolationCount = $violations.Count
        ForbiddenPhysicalRootCount = @($violations.ToArray() | Where-Object Rule -eq 'forbidden-physical-root').Count
        Passed = $violations.Count -eq 0
        IgnoredExternalProbes = @($ignoredProbeResults.ToArray())
        Violations = @($violations.ToArray())
    }

    if ($Json) {
        $result | ConvertTo-Json -Depth 8 -Compress
    } else {
        "Repository root: $($result.RepositoryRoot)"
        "Index candidate files: $($result.IndexCandidateFileCount)"
        "Untracked candidate files: $($result.UntrackedCandidateFileCount)"
        "Ignored external probes: $($result.IgnoredExternalProbeCount)/$($result.RequiredExternalProbeCount)"
        "Copyright violations: $($result.ViolationCount)"
        if ($result.ViolationCount -gt 0) {
            $result.Violations | Format-Table Path, Source, Rule, Detail -AutoSize
        }
    }

    if (-not $result.Passed) {
        exit 1
    }
} catch {
    if ($Json) {
        $fatalDetail = if ($IncludeAbsolutePaths) { $_.Exception.Message } else { 'Scanner failed closed; rerun interactively for diagnostics.' }
        [pscustomobject]@{
            SchemaVersion = 2
            RepositoryRoot = if ($IncludeAbsolutePaths -and $null -ne $resolvedRootForError) { $resolvedRootForError } else { '.' }
            CandidateFileCount = 0
            ViolationCount = 1
            Passed = $false
            Violations = @([pscustomobject]@{
                Path = '<scanner>'
                Source = 'scanner'
                Rule = 'fatal-scanner-error'
                Detail = $fatalDetail
            })
        } | ConvertTo-Json -Depth 6 -Compress
        exit 1
    }
    throw
}
