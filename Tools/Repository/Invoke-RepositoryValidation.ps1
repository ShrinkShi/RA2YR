[CmdletBinding()]
param(
    [Parameter()]
    [Alias('ProjectRoot')]
    [string] $RepositoryRoot,

    [Parameter()]
    [switch] $Json
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$expectedUnityVersion = '2022.3.60f1c1'
$maximumTextFileBytes = 4MB
$utf8Strict = New-Object System.Text.UTF8Encoding($false, $true)
$pathComparison = if ([IO.Path]::DirectorySeparatorChar -eq '\') {
    [StringComparison]::OrdinalIgnoreCase
} else {
    [StringComparison]::Ordinal
}

if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = Join-Path $PSScriptRoot '..\..'
}
$repositoryFullPath = [IO.Path]::GetFullPath($RepositoryRoot)
$repositoryPathRoot = [IO.Path]::GetPathRoot($repositoryFullPath)
if ($repositoryFullPath.Length -gt $repositoryPathRoot.Length) {
    $repositoryFullPath = $repositoryFullPath.TrimEnd(
        [IO.Path]::DirectorySeparatorChar,
        [IO.Path]::AltDirectorySeparatorChar)
}

$violations = New-Object System.Collections.Generic.List[object]
$assetEntryCount = 0
$metaFileCount = 0
$matrixEntryCount = 0
$evidenceReferenceCount = 0

function Add-Violation {
    param(
        [Parameter(Mandatory)][string] $Rule,
        [Parameter(Mandatory)][string] $Path,
        [Parameter(Mandatory)][string] $Detail
    )

    $script:violations.Add([pscustomobject]@{
        Rule = $Rule
        Path = $Path.Replace('\', '/')
        Detail = $Detail
    })
}

function Get-RelativeRepositoryPath {
    param([Parameter(Mandatory)][string] $FullPath)

    $resolved = [IO.Path]::GetFullPath($FullPath)
    $prefix = $script:repositoryFullPath + [IO.Path]::DirectorySeparatorChar
    if (-not $resolved.StartsWith($prefix, $script:pathComparison)) {
        throw "Path is outside the repository root: $resolved"
    }
    $resolved.Substring($prefix.Length).Replace('\', '/')
}

function Read-StrictUtf8Text {
    param([Parameter(Mandatory)][string] $Path)

    $file = Get-Item -LiteralPath $Path -Force -ErrorAction Stop
    if ($file.PSIsContainer) {
        throw "Expected a file but found a directory: $Path"
    }
    if ($file.Length -gt $script:maximumTextFileBytes) {
        throw "Text file exceeds the validation budget: $Path"
    }
    [IO.File]::ReadAllText($file.FullName, $script:utf8Strict)
}

function Test-FileSystemEntryExists {
    param([Parameter(Mandatory)][string] $Path)

    try {
        [IO.File]::GetAttributes($Path) | Out-Null
        $true
    } catch [IO.FileNotFoundException] {
        $false
    } catch [IO.DirectoryNotFoundException] {
        $false
    }
}

function Test-RepositoryPathContainsReparsePoint {
    param([Parameter(Mandatory)][string] $FullPath)

    $relativePath = Get-RelativeRepositoryPath -FullPath $FullPath
    $currentPath = $script:repositoryFullPath
    foreach ($segment in $relativePath.Split('/')) {
        $currentPath = Join-Path $currentPath $segment
        $attributes = [IO.File]::GetAttributes($currentPath)
        if (($attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            Add-Violation -Rule 'repository-reparse-point' -Path (Get-RelativeRepositoryPath -FullPath $currentPath) -Detail 'Repository validation does not traverse reparse points.'
            return $true
        }
    }
    $false
}

function ConvertFrom-CanonicalQuotedScalar {
    param([Parameter(Mandatory)][string] $Value)

    $match = [regex]::Match($Value, '^"([^"\\\r\n]*)"$')
    if (-not $match.Success) {
        throw "Unsupported YAML scalar: $Value"
    }
    $match.Groups[1].Value
}

function New-TextFromCodePoints {
    param([Parameter(Mandatory)][int[]] $CodePoints)

    -join @($CodePoints | ForEach-Object { [char]$_ })
}

function New-OrdinalSet {
    $set = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    Write-Output -NoEnumerate $set
}

function Test-SetEquals {
    param(
        [Parameter(Mandatory)] $Actual,
        [Parameter(Mandatory)][string[]] $Expected
    )

    if ($Actual.Count -ne $Expected.Count) {
        return $false
    }
    foreach ($value in $Expected) {
        if (-not $Actual.Contains($value)) {
            return $false
        }
    }
    $true
}

function Get-SectionRange {
    param(
        [Parameter(Mandatory)][AllowEmptyString()][string[]] $Lines,
        [Parameter(Mandatory)][string] $Name,
        [Parameter(Mandatory)][string] $MatrixPath
    )

    $sectionIndexes = New-Object System.Collections.Generic.List[int]
    for ($index = 0; $index -lt $Lines.Count; $index++) {
        if ($Lines[$index] -eq ($Name + ':')) {
            $sectionIndexes.Add($index)
        }
    }
    if ($sectionIndexes.Count -ne 1) {
        Add-Violation -Rule 'matrix-schema' -Path $MatrixPath -Detail "Section '$Name' must appear exactly once."
        return $null
    }

    $end = $Lines.Count
    for ($index = $sectionIndexes[0] + 1; $index -lt $Lines.Count; $index++) {
        if ($Lines[$index] -match '^[A-Za-z_][A-Za-z0-9_]*:\s*') {
            $end = $index
            break
        }
    }
    [pscustomobject]@{ Start = $sectionIndexes[0]; End = $end }
}

function Get-PathReferenceResult {
    param(
        [Parameter(Mandatory)][string] $Reference,
        [Parameter(Mandatory)][string] $MatrixPath
    )

    $fragment = $null
    $path = $Reference
    $fragmentIndex = $Reference.IndexOf('#')
    if ($fragmentIndex -ge 0) {
        $path = $Reference.Substring(0, $fragmentIndex)
        $fragment = $Reference.Substring($fragmentIndex + 1)
    }

    if ([string]::IsNullOrWhiteSpace($path) -or
        [IO.Path]::IsPathRooted($path) -or
        $path.Contains('\') -or
        $path.Contains(':')) {
        Add-Violation -Rule 'matrix-evidence-path' -Path $MatrixPath -Detail "Evidence reference is not a safe repository-relative path: $Reference"
        return
    }

    $segments = $path.Split('/')
    if ($segments.Count -eq 0 -or @($segments | Where-Object { $_ -eq '' -or $_ -eq '.' -or $_ -eq '..' }).Count -gt 0) {
        Add-Violation -Rule 'matrix-evidence-path' -Path $MatrixPath -Detail "Evidence reference contains an unsafe path segment: $Reference"
        return
    }
    if ($fragmentIndex -ge 0 -and [string]::IsNullOrWhiteSpace($fragment)) {
        Add-Violation -Rule 'matrix-evidence-fragment' -Path $MatrixPath -Detail "Evidence reference has an empty fragment: $Reference"
        return
    }

    $candidate = [IO.Path]::GetFullPath((Join-Path $script:repositoryFullPath ($path.Replace('/', [IO.Path]::DirectorySeparatorChar))))
    $prefix = $script:repositoryFullPath + [IO.Path]::DirectorySeparatorChar
    if (-not $candidate.StartsWith($prefix, $script:pathComparison)) {
        Add-Violation -Rule 'matrix-evidence-path' -Path $MatrixPath -Detail "Evidence reference escapes the repository: $Reference"
        return
    }
    if (-not (Test-FileSystemEntryExists -Path $candidate)) {
        Add-Violation -Rule 'matrix-evidence-missing' -Path $path -Detail 'Referenced evidence file does not exist.'
        return
    }

    if (Test-RepositoryPathContainsReparsePoint -FullPath $candidate) {
        return
    }

    $attributes = [IO.File]::GetAttributes($candidate)
    if (($attributes -band [IO.FileAttributes]::Directory) -ne 0 -or
        ($attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        Add-Violation -Rule 'matrix-evidence-type' -Path $path -Detail 'Evidence must reference a regular repository file.'
        return
    }

    if (-not [string]::IsNullOrWhiteSpace($fragment)) {
        $evidenceText = Read-StrictUtf8Text -Path $candidate
        $fragmentPattern = '(?m)^' + [regex]::Escape($fragment) + ':\s*$'
        if (-not [regex]::IsMatch($evidenceText, $fragmentPattern)) {
            Add-Violation -Rule 'matrix-evidence-fragment' -Path $path -Detail "Evidence fragment was not found: $fragment"
        }
    }
}

function Invoke-AssetMetadataChecks {
    $assetsPath = Join-Path $script:repositoryFullPath 'Assets'
    if (-not (Test-FileSystemEntryExists -Path $assetsPath)) {
        Add-Violation -Rule 'assets-missing' -Path 'Assets' -Detail 'Unity Assets directory is missing.'
        return
    }

    $assetsAttributes = [IO.File]::GetAttributes($assetsPath)
    if (($assetsAttributes -band [IO.FileAttributes]::Directory) -eq 0 -or
        ($assetsAttributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        Add-Violation -Rule 'assets-root-type' -Path 'Assets' -Detail 'Assets must be a regular directory, not a reparse point.'
        return
    }

    $entries = New-Object System.Collections.Generic.List[IO.FileSystemInfo]
    $pending = New-Object System.Collections.Generic.Stack[IO.DirectoryInfo]
    $pending.Push((Get-Item -LiteralPath $assetsPath -Force))
    while ($pending.Count -gt 0) {
        $directory = $pending.Pop()
        foreach ($entry in $directory.GetFileSystemInfos()) {
            $attributes = [IO.File]::GetAttributes($entry.FullName)
            $entries.Add($entry)
            if (($attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                Add-Violation -Rule 'assets-reparse-point' -Path (Get-RelativeRepositoryPath -FullPath $entry.FullName) -Detail 'Reparse points are not accepted below Assets.'
                continue
            }
            if (($attributes -band [IO.FileAttributes]::Directory) -ne 0) {
                $pending.Push((Get-Item -LiteralPath $entry.FullName -Force))
            }
        }
    }

    $metaPaths = New-Object System.Collections.Generic.List[string]
    foreach ($entry in $entries) {
        $relativePath = Get-RelativeRepositoryPath -FullPath $entry.FullName
        $entryAttributes = [IO.File]::GetAttributes($entry.FullName)
        if (($entryAttributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            continue
        }
        $isMeta = $entry.Name.EndsWith('.meta', [StringComparison]::OrdinalIgnoreCase)
        if ($isMeta) {
            if (($entryAttributes -band [IO.FileAttributes]::Directory) -ne 0) {
                Add-Violation -Rule 'meta-not-file' -Path $relativePath -Detail 'A Unity .meta entry must be a regular file.'
            } else {
                $metaPaths.Add($entry.FullName)
            }
            continue
        }

        $script:assetEntryCount++
        $expectedMeta = $entry.FullName + '.meta'
        if (-not (Test-FileSystemEntryExists -Path $expectedMeta)) {
            Add-Violation -Rule 'meta-missing' -Path $relativePath -Detail 'Unity resource or directory has no matching .meta file.'
            continue
        }
        $metaAttributes = [IO.File]::GetAttributes($expectedMeta)
        if (($metaAttributes -band [IO.FileAttributes]::Directory) -ne 0 -or
            ($metaAttributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            Add-Violation -Rule 'meta-type' -Path ($relativePath + '.meta') -Detail 'Matching .meta must be a regular file.'
        }
    }

    $guidOwners = New-Object 'System.Collections.Generic.Dictionary[string,string]' ([StringComparer]::OrdinalIgnoreCase)
    foreach ($metaPath in $metaPaths) {
        $script:metaFileCount++
        $relativeMeta = Get-RelativeRepositoryPath -FullPath $metaPath
        $targetPath = $metaPath.Substring(0, $metaPath.Length - 5)
        if (-not (Test-FileSystemEntryExists -Path $targetPath)) {
            Add-Violation -Rule 'meta-orphan' -Path $relativeMeta -Detail 'Unity .meta file has no matching resource or directory.'
        }

        $metaText = Read-StrictUtf8Text -Path $metaPath
        $guidMatches = [regex]::Matches($metaText, '(?m)^guid:\s*([0-9A-Fa-f]{32})\s*$')
        if ($guidMatches.Count -ne 1) {
            Add-Violation -Rule 'meta-guid' -Path $relativeMeta -Detail 'Unity .meta file must contain exactly one 32-hex GUID.'
            continue
        }

        $guid = $guidMatches[0].Groups[1].Value
        if ($guidOwners.ContainsKey($guid)) {
            Add-Violation -Rule 'meta-guid-duplicate' -Path $relativeMeta -Detail ("GUID is already owned by " + $guidOwners[$guid] + '.')
        } else {
            $guidOwners.Add($guid, $relativeMeta)
        }
    }
}

function Invoke-UnityBoundaryChecks {
    $versionRelativePath = 'ProjectSettings/ProjectVersion.txt'
    $versionPath = Join-Path $script:repositoryFullPath ($versionRelativePath.Replace('/', [IO.Path]::DirectorySeparatorChar))
    if (-not (Test-FileSystemEntryExists -Path $versionPath)) {
        Add-Violation -Rule 'unity-version-missing' -Path $versionRelativePath -Detail 'ProjectVersion.txt is missing.'
    } elseif (-not (Test-RepositoryPathContainsReparsePoint -FullPath $versionPath)) {
        $versionText = Read-StrictUtf8Text -Path $versionPath
        $matches = [regex]::Matches($versionText, '(?m)^m_EditorVersion:\s*([^\r\n]+?)\s*$')
        if ($matches.Count -ne 1 -or $matches[0].Groups[1].Value -ne $script:expectedUnityVersion) {
            Add-Violation -Rule 'unity-version' -Path $versionRelativePath -Detail ("Expected Unity " + $script:expectedUnityVersion + '.')
        }
    }

    $asmdefRelativePath = 'Assets/RA2YR/Core/RA2YR.Core.asmdef'
    $asmdefPath = Join-Path $script:repositoryFullPath ($asmdefRelativePath.Replace('/', [IO.Path]::DirectorySeparatorChar))
    if (-not (Test-FileSystemEntryExists -Path $asmdefPath)) {
        Add-Violation -Rule 'core-asmdef-missing' -Path $asmdefRelativePath -Detail 'Core assembly definition is missing.'
    } elseif (-not (Test-RepositoryPathContainsReparsePoint -FullPath $asmdefPath)) {
        try {
            $asmdef = (Read-StrictUtf8Text -Path $asmdefPath) | ConvertFrom-Json
            $property = $asmdef.PSObject.Properties['noEngineReferences']
            if ($null -eq $property -or
                -not ($property.Value -is [bool]) -or
                $property.Value -ne $true) {
                Add-Violation -Rule 'core-engine-boundary' -Path $asmdefRelativePath -Detail 'noEngineReferences must be the Boolean value true.'
            }
        } catch {
            Add-Violation -Rule 'core-asmdef-invalid' -Path $asmdefRelativePath -Detail 'Core assembly definition is not valid JSON.'
        }
    }

    $corePath = Join-Path $script:repositoryFullPath 'Assets\RA2YR\Core'
    if (-not (Test-FileSystemEntryExists -Path $corePath)) {
        Add-Violation -Rule 'core-directory-missing' -Path 'Assets/RA2YR/Core' -Detail 'Core source directory is missing.'
        return
    }
    if (Test-RepositoryPathContainsReparsePoint -FullPath $corePath) {
        return
    }

    $sourceExtensions = @('.cs', '.asmdef', '.asmref', '.rsp')
    foreach ($file in Get-ChildItem -LiteralPath $corePath -Recurse -Force -File -ErrorAction Stop) {
        if (([IO.File]::GetAttributes($file.FullName) -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            continue
        }
        if ($sourceExtensions -notcontains $file.Extension.ToLowerInvariant()) {
            continue
        }
        $text = Read-StrictUtf8Text -Path $file.FullName
        if ([regex]::IsMatch($text, '(?<![A-Za-z0-9_])(UnityEngine|UnityEditor)(?![A-Za-z0-9_])')) {
            Add-Violation -Rule 'core-unity-reference' -Path (Get-RelativeRepositoryPath -FullPath $file.FullName) -Detail 'Core text references UnityEngine or UnityEditor.'
        }
    }
}

function Invoke-CompatibilityMatrixChecks {
    $matrixRelativePath = 'docs/compatibility/matrix.yml'
    $matrixPath = Join-Path $script:repositoryFullPath ($matrixRelativePath.Replace('/', [IO.Path]::DirectorySeparatorChar))
    if (-not (Test-FileSystemEntryExists -Path $matrixPath)) {
        Add-Violation -Rule 'matrix-missing' -Path $matrixRelativePath -Detail 'Compatibility matrix is missing.'
        return
    }
    if (Test-RepositoryPathContainsReparsePoint -FullPath $matrixPath) {
        return
    }

    $matrixText = Read-StrictUtf8Text -Path $matrixPath
    if ($matrixText.Contains("`t")) {
        Add-Violation -Rule 'matrix-schema' -Path $matrixRelativePath -Detail 'Tabs are not accepted by the canonical matrix schema.'
        return
    }
    $lines = [regex]::Split($matrixText, '\r?\n')

    $topLevelNames = @('schema_version', 'last_updated', 'baseline', 'status_vocabulary', 'entry_schema', 'entries')
    $topLevelCounts = @{}
    foreach ($name in $topLevelNames) {
        $topLevelCounts[$name] = 0
    }
    $currentTopLevel = $null
    for ($index = 0; $index -lt $lines.Count; $index++) {
        $line = $lines[$index]
        if ([string]::IsNullOrWhiteSpace($line) -or $line -match '^\s*#') {
            continue
        }
        if ($line -match '^\s') {
            if ($null -eq $currentTopLevel -or $currentTopLevel -in @('schema_version', 'last_updated')) {
                Add-Violation -Rule 'matrix-schema' -Path $matrixRelativePath -Detail ("Unowned indented content at line " + ($index + 1) + '.')
            }
            continue
        }

        $topLevelMatch = [regex]::Match($line, '^([A-Za-z_][A-Za-z0-9_]*):(?:\s*(.*))?$')
        if (-not $topLevelMatch.Success) {
            $currentTopLevel = $null
            Add-Violation -Rule 'matrix-schema' -Path $matrixRelativePath -Detail ("Unsupported top-level syntax at line " + ($index + 1) + '.')
            continue
        }

        $name = $topLevelMatch.Groups[1].Value
        if ($topLevelNames -notcontains $name) {
            $currentTopLevel = $null
            Add-Violation -Rule 'matrix-schema' -Path $matrixRelativePath -Detail ("Unknown top-level key '$name' at line " + ($index + 1) + '.')
            continue
        }

        $topLevelCounts[$name]++
        $currentTopLevel = $name
        if ($name -eq 'schema_version') {
            if ($line -notmatch '^schema_version:\s*1\s*$') {
                Add-Violation -Rule 'matrix-schema' -Path $matrixRelativePath -Detail 'schema_version must have value 1.'
            }
        } elseif ($name -eq 'last_updated') {
            if ($line -notmatch '^last_updated:\s*"[0-9]{4}-[0-9]{2}-[0-9]{2}"\s*$') {
                Add-Violation -Rule 'matrix-schema' -Path $matrixRelativePath -Detail 'last_updated must be a quoted ISO date.'
            }
        } elseif ($line -ne ($name + ':')) {
            Add-Violation -Rule 'matrix-schema' -Path $matrixRelativePath -Detail ("Section '$name' must use the canonical top-level declaration.")
        }
    }
    foreach ($name in $topLevelNames) {
        if ($topLevelCounts[$name] -ne 1) {
            Add-Violation -Rule 'matrix-schema' -Path $matrixRelativePath -Detail ("Top-level key '$name' must appear exactly once.")
        }
    }

    $baselineRange = Get-SectionRange -Lines $lines -Name 'baseline' -MatrixPath $matrixRelativePath
    $vocabularyRange = Get-SectionRange -Lines $lines -Name 'status_vocabulary' -MatrixPath $matrixRelativePath
    $entrySchemaRange = Get-SectionRange -Lines $lines -Name 'entry_schema' -MatrixPath $matrixRelativePath
    $entriesRange = Get-SectionRange -Lines $lines -Name 'entries' -MatrixPath $matrixRelativePath
    if ($null -eq $baselineRange -or $null -eq $vocabularyRange -or $null -eq $entrySchemaRange -or $null -eq $entriesRange) {
        return
    }

    $baselineFields = New-OrdinalSet
    $finalAlertFields = New-OrdinalSet
    $insideFinalAlert = $false
    for ($index = $baselineRange.Start + 1; $index -lt $baselineRange.End; $index++) {
        $line = $lines[$index]
        if ([string]::IsNullOrWhiteSpace($line) -or $line -match '^\s*#') {
            continue
        }
        if ($line -match '^  (game|content_role|content_path_role|observed_workspace_path):\s+("[^"\\\r\n]*")\s*$') {
            $insideFinalAlert = $false
            if (-not $baselineFields.Add($matches[1])) {
                Add-Violation -Rule 'matrix-schema' -Path $matrixRelativePath -Detail ("baseline repeats field '$($matches[1])'.")
            }
            try {
                ConvertFrom-CanonicalQuotedScalar $matches[2] | Out-Null
            } catch {
                Add-Violation -Rule 'matrix-schema' -Path $matrixRelativePath -Detail ("Invalid baseline scalar at line " + ($index + 1) + '.')
            }
            continue
        }
        if ($line -match '^  content_manifest:\s+null\s*$') {
            $insideFinalAlert = $false
            if (-not $baselineFields.Add('content_manifest')) {
                Add-Violation -Rule 'matrix-schema' -Path $matrixRelativePath -Detail "baseline repeats field 'content_manifest'."
            }
            continue
        }
        if ($line -eq '  finalalert2:') {
            $insideFinalAlert = $true
            if (-not $baselineFields.Add('finalalert2')) {
                Add-Violation -Rule 'matrix-schema' -Path $matrixRelativePath -Detail "baseline repeats field 'finalalert2'."
            }
            continue
        }
        if ($insideFinalAlert -and $line -match '^    (executable|version|sha256):\s+("[^"\\\r\n]*")\s*$') {
            $field = $matches[1]
            if (-not $finalAlertFields.Add($field)) {
                Add-Violation -Rule 'matrix-schema' -Path $matrixRelativePath -Detail ("baseline.finalalert2 repeats field '$field'.")
            }
            try {
                $value = ConvertFrom-CanonicalQuotedScalar $matches[2]
                if ($field -eq 'sha256' -and $value -notmatch '^[A-Fa-f0-9]{64}$') {
                    Add-Violation -Rule 'matrix-schema' -Path $matrixRelativePath -Detail 'baseline.finalalert2.sha256 must contain 64 hexadecimal characters.'
                }
            } catch {
                Add-Violation -Rule 'matrix-schema' -Path $matrixRelativePath -Detail ("Invalid baseline.finalalert2 scalar at line " + ($index + 1) + '.')
            }
            continue
        }
        Add-Violation -Rule 'matrix-schema' -Path $matrixRelativePath -Detail ("Unsupported baseline syntax at line " + ($index + 1) + '.')
    }
    foreach ($field in @('game', 'content_role', 'content_path_role', 'observed_workspace_path', 'content_manifest', 'finalalert2')) {
        if (-not $baselineFields.Contains($field)) {
            Add-Violation -Rule 'matrix-schema' -Path $matrixRelativePath -Detail ("baseline is missing field '$field'.")
        }
    }
    foreach ($field in @('executable', 'version', 'sha256')) {
        if (-not $finalAlertFields.Contains($field)) {
            Add-Violation -Rule 'matrix-schema' -Path $matrixRelativePath -Detail ("baseline.finalalert2 is missing field '$field'.")
        }
    }

    $approvedVocabulary = @{
        implementation = @(
            (New-TextFromCodePoints @(0x672A, 0x5B9E, 0x73B0)),
            (New-TextFromCodePoints @(0x53EF, 0x89E3, 0x6790)),
            (New-TextFromCodePoints @(0x53EF, 0x663E, 0x793A)),
            (New-TextFromCodePoints @(0x53EF, 0x6267, 0x884C)),
            (New-TextFromCodePoints @(0x884C, 0x4E3A, 0x8FD1, 0x4F3C))
        )
        original_comparison = @(
            (New-TextFromCodePoints @(0x672A, 0x5B9E, 0x73B0)),
            (New-TextFromCodePoints @(0x539F, 0x7248, 0x5BF9, 0x7167, 0x901A, 0x8FC7))
        )
        roundtrip = @(
            (New-TextFromCodePoints @(0x672A, 0x5B9E, 0x73B0)),
            (New-TextFromCodePoints @(0x5F80, 0x8FD4, 0x901A, 0x8FC7))
        )
    }
    $approvedLimitation = New-TextFromCodePoints @(0x5DF2, 0x77E5, 0x9650, 0x5236)

    $declaredVocabulary = @{
        implementation = New-OrdinalSet
        original_comparison = New-OrdinalSet
        roundtrip = New-OrdinalSet
    }
    $declaredLimitation = $null
    $currentVocabulary = $null
    for ($index = $vocabularyRange.Start + 1; $index -lt $vocabularyRange.End; $index++) {
        $line = $lines[$index]
        if ([string]::IsNullOrWhiteSpace($line) -or $line -match '^\s*#') {
            continue
        }
        if ($line -match '^  (implementation|original_comparison|roundtrip):\s*$') {
            $currentVocabulary = $matches[1]
            continue
        }
        if ($line -match '^    -\s+("[^"\\\r\n]*")\s*$' -and $null -ne $currentVocabulary) {
            try {
                $vocabularyValue = ConvertFrom-CanonicalQuotedScalar $matches[1]
                $declaredVocabulary[$currentVocabulary].Add($vocabularyValue) | Out-Null
            } catch {
                Add-Violation -Rule 'matrix-schema' -Path $matrixRelativePath -Detail ("Invalid status scalar at line " + ($index + 1) + '.')
            }
            continue
        }
        if ($line -match '^  limitation_flag:\s+("[^"\\\r\n]*")\s*$') {
            try {
                $declaredLimitation = ConvertFrom-CanonicalQuotedScalar $matches[1]
            } catch {
                Add-Violation -Rule 'matrix-schema' -Path $matrixRelativePath -Detail 'Invalid limitation_flag scalar.'
            }
            $currentVocabulary = $null
            continue
        }
        Add-Violation -Rule 'matrix-schema' -Path $matrixRelativePath -Detail ("Unsupported status_vocabulary syntax at line " + ($index + 1) + '.')
    }

    foreach ($dimension in @('implementation', 'original_comparison', 'roundtrip')) {
        Write-Verbose ("Vocabulary {0}: declared={1}; approved={2}" -f
            $dimension,
            ((@($declaredVocabulary[$dimension]) | ForEach-Object { ([int[]][char[]]$_) -join '.' }) -join '|'),
            (($approvedVocabulary[$dimension] | ForEach-Object { ([int[]][char[]]$_) -join '.' }) -join '|'))
        if (-not (Test-SetEquals -Actual $declaredVocabulary[$dimension] -Expected $approvedVocabulary[$dimension])) {
            Add-Violation -Rule 'matrix-vocabulary' -Path $matrixRelativePath -Detail "Declared values for '$dimension' do not match the approved vocabulary."
        }
    }
    if ($declaredLimitation -ne $approvedLimitation) {
        Add-Violation -Rule 'matrix-vocabulary' -Path $matrixRelativePath -Detail 'limitation_flag does not match the approved vocabulary.'
    }

    $declaredRequired = New-OrdinalSet
    $declaredStatusFields = New-OrdinalSet
    $currentSchemaList = $null
    for ($index = $entrySchemaRange.Start + 1; $index -lt $entrySchemaRange.End; $index++) {
        $line = $lines[$index]
        if ([string]::IsNullOrWhiteSpace($line) -or $line -match '^\s*#') {
            continue
        }
        if ($line -match '^  (required|status_fields):\s*$') {
            $currentSchemaList = $matches[1]
            continue
        }
        if ($line -match '^    -\s+([A-Za-z_][A-Za-z0-9_]*)\s*$' -and $null -ne $currentSchemaList) {
            if ($currentSchemaList -eq 'required') {
                $declaredRequired.Add($matches[1]) | Out-Null
            } else {
                $declaredStatusFields.Add($matches[1]) | Out-Null
            }
            continue
        }
        Add-Violation -Rule 'matrix-schema' -Path $matrixRelativePath -Detail ("Unsupported entry_schema syntax at line " + ($index + 1) + '.')
    }

    $requiredFields = @('id', 'domain', 'feature', 'status', 'tests', 'evidence', 'limitations')
    $statusFields = @('implementation', 'original_comparison', 'roundtrip')
    Write-Verbose ("Entry schema: required={0}; status={1}" -f ((@($declaredRequired)) -join '|'), ((@($declaredStatusFields)) -join '|'))
    if (-not (Test-SetEquals -Actual $declaredRequired -Expected $requiredFields)) {
        Add-Violation -Rule 'matrix-entry-schema' -Path $matrixRelativePath -Detail 'entry_schema.required does not match schema version 1.'
    }
    if (-not (Test-SetEquals -Actual $declaredStatusFields -Expected $statusFields)) {
        Add-Violation -Rule 'matrix-entry-schema' -Path $matrixRelativePath -Detail 'entry_schema.status_fields does not match schema version 1.'
    }

    $entries = New-Object System.Collections.Generic.List[object]
    $currentEntry = $null
    $currentField = $null
    $currentLimitation = $null
    for ($index = $entriesRange.Start + 1; $index -lt $entriesRange.End; $index++) {
        $line = $lines[$index]
        if ([string]::IsNullOrWhiteSpace($line) -or $line -match '^\s*#') {
            continue
        }

        if ($line -match '^  - id:\s+([A-Za-z0-9._-]+)\s*$') {
            if ($null -ne $currentLimitation) {
                $currentEntry.Limitations.Add($currentLimitation)
                $currentLimitation = $null
            }
            if ($null -ne $currentEntry) {
                $entries.Add($currentEntry)
            }
            $fields = New-OrdinalSet
            $fields.Add('id') | Out-Null
            $currentEntry = [pscustomobject]@{
                Id = $matches[1]
                Fields = $fields
                Status = @{}
                Evidence = (New-Object System.Collections.Generic.List[string])
                TestCount = 0
                Limitations = (New-Object System.Collections.Generic.List[object])
                ListModes = @{}
            }
            $currentField = 'id'
            continue
        }

        if ($null -eq $currentEntry) {
            Add-Violation -Rule 'matrix-schema' -Path $matrixRelativePath -Detail ("Entry content appears before an entry id at line " + ($index + 1) + '.')
            continue
        }

        if ($line -match '^    ([A-Za-z_][A-Za-z0-9_]*):\s*(.*)$') {
            if ($null -ne $currentLimitation) {
                $currentEntry.Limitations.Add($currentLimitation)
                $currentLimitation = $null
            }
            $field = $matches[1]
            $value = $matches[2]
            if (-not $currentEntry.Fields.Add($field)) {
                Add-Violation -Rule 'matrix-entry-field' -Path $matrixRelativePath -Detail ("Entry '$($currentEntry.Id)' repeats field '$field'.")
            }
            $currentField = $field

            if ($field -in @('tests', 'evidence', 'limitations')) {
                if ($value -eq '') {
                    $currentEntry.ListModes[$field] = 'block'
                } elseif ($value -eq '[]') {
                    $currentEntry.ListModes[$field] = 'empty'
                } else {
                    Add-Violation -Rule 'matrix-entry-list' -Path $matrixRelativePath -Detail ("Entry '$($currentEntry.Id)' field '$field' must be [] or a block list.")
                }
                continue
            }

            if ($field -eq 'status') {
                $statusMatch = [regex]::Match(
                    $value,
                    '^\{\s*implementation:\s*"([^"\\\r\n]*)"\s*,\s*original_comparison:\s*"([^"\\\r\n]*)"\s*,\s*roundtrip:\s*"([^"\\\r\n]*)"\s*\}$')
                if (-not $statusMatch.Success) {
                    Add-Violation -Rule 'matrix-status-shape' -Path $matrixRelativePath -Detail ("Entry '$($currentEntry.Id)' has an unsupported status map.")
                } else {
                    $currentEntry.Status['implementation'] = $statusMatch.Groups[1].Value
                    $currentEntry.Status['original_comparison'] = $statusMatch.Groups[2].Value
                    $currentEntry.Status['roundtrip'] = $statusMatch.Groups[3].Value
                }
                continue
            }

            if ($field -eq 'domain' -and $value -match '^[A-Za-z0-9._-]+$') {
                continue
            }
            if ($field -eq 'feature') {
                try {
                    ConvertFrom-CanonicalQuotedScalar $value | Out-Null
                } catch {
                    Add-Violation -Rule 'matrix-entry-field' -Path $matrixRelativePath -Detail ("Entry '$($currentEntry.Id)' has an invalid feature scalar.")
                }
                continue
            }
            Add-Violation -Rule 'matrix-entry-field' -Path $matrixRelativePath -Detail ("Entry '$($currentEntry.Id)' has unsupported field '$field'.")
            continue
        }

        if ($line -match '^      -\s+("[^"\\\r\n]*")\s*$' -and $currentField -in @('tests', 'evidence')) {
            if ($currentEntry.ListModes[$currentField] -ne 'block') {
                Add-Violation -Rule 'matrix-entry-list' -Path $matrixRelativePath -Detail ("Entry '$($currentEntry.Id)' adds items to inline-empty '$currentField'.")
                continue
            }
            try {
                $item = ConvertFrom-CanonicalQuotedScalar $matches[1]
                if ([string]::IsNullOrWhiteSpace($item)) {
                    Add-Violation -Rule 'matrix-entry-list' -Path $matrixRelativePath -Detail ("Entry '$($currentEntry.Id)' has an empty '$currentField' item.")
                } elseif ($currentField -eq 'tests') {
                    $currentEntry.TestCount++
                } else {
                    $currentEntry.Evidence.Add($item)
                }
            } catch {
                Add-Violation -Rule 'matrix-entry-list' -Path $matrixRelativePath -Detail ("Entry '$($currentEntry.Id)' has an invalid '$currentField' item.")
            }
            continue
        }

        if ($line -match '^      - id:\s+([A-Za-z0-9._-]+)\s*$' -and $currentField -eq 'limitations') {
            if ($currentEntry.ListModes['limitations'] -ne 'block') {
                Add-Violation -Rule 'matrix-entry-list' -Path $matrixRelativePath -Detail ("Entry '$($currentEntry.Id)' adds limitations to an inline-empty list.")
                continue
            }
            if ($null -ne $currentLimitation) {
                $currentEntry.Limitations.Add($currentLimitation)
            }
            $limitationFields = New-OrdinalSet
            $limitationFields.Add('id') | Out-Null
            $currentLimitation = [pscustomobject]@{
                Id = $matches[1]
                Fields = $limitationFields
                Status = $null
            }
            continue
        }

        if ($line -match '^        (status|description):\s+("[^"\\\r\n]*")\s*$' -and
            $currentField -eq 'limitations' -and
            $null -ne $currentLimitation) {
            $limitationField = $matches[1]
            if (-not $currentLimitation.Fields.Add($limitationField)) {
                Add-Violation -Rule 'matrix-limitation-field' -Path $matrixRelativePath -Detail ("Limitation '$($currentLimitation.Id)' repeats '$limitationField'.")
            }
            try {
                $limitationValue = ConvertFrom-CanonicalQuotedScalar $matches[2]
                if ($limitationField -eq 'status') {
                    $currentLimitation.Status = $limitationValue
                }
            } catch {
                Add-Violation -Rule 'matrix-limitation-field' -Path $matrixRelativePath -Detail ("Limitation '$($currentLimitation.Id)' has an invalid scalar.")
            }
            continue
        }

        Add-Violation -Rule 'matrix-schema' -Path $matrixRelativePath -Detail ("Unsupported entries syntax at line " + ($index + 1) + '.')
    }

    if ($null -ne $currentLimitation) {
        $currentEntry.Limitations.Add($currentLimitation)
    }
    if ($null -ne $currentEntry) {
        $entries.Add($currentEntry)
    }

    $script:matrixEntryCount = $entries.Count
    if ($entries.Count -eq 0) {
        Add-Violation -Rule 'matrix-entries' -Path $matrixRelativePath -Detail 'Compatibility matrix contains no entries.'
        return
    }

    $entryIds = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    foreach ($entry in $entries) {
        if (-not $entryIds.Add($entry.Id)) {
            Add-Violation -Rule 'matrix-id-duplicate' -Path $matrixRelativePath -Detail ("Duplicate entry id: " + $entry.Id)
        }
        foreach ($requiredField in $requiredFields) {
            if (-not $entry.Fields.Contains($requiredField)) {
                Add-Violation -Rule 'matrix-entry-field' -Path $matrixRelativePath -Detail ("Entry '$($entry.Id)' is missing '$requiredField'.")
            }
        }
        foreach ($dimension in $statusFields) {
            if (-not $entry.Status.ContainsKey($dimension) -or
                -not $declaredVocabulary[$dimension].Contains([string]$entry.Status[$dimension])) {
                Add-Violation -Rule 'matrix-status-value' -Path $matrixRelativePath -Detail ("Entry '$($entry.Id)' has an invalid '$dimension' status.")
            }
        }
        foreach ($limitation in $entry.Limitations) {
            foreach ($requiredLimitationField in @('id', 'status', 'description')) {
                if (-not $limitation.Fields.Contains($requiredLimitationField)) {
                    Add-Violation -Rule 'matrix-limitation-field' -Path $matrixRelativePath -Detail ("Limitation '$($limitation.Id)' is missing '$requiredLimitationField'.")
                }
            }
            if ($limitation.Status -ne $declaredLimitation) {
                Add-Violation -Rule 'matrix-status-value' -Path $matrixRelativePath -Detail ("Limitation '$($limitation.Id)' has an invalid status.")
            }
        }

        $implementation = if ($entry.Status.ContainsKey('implementation')) { [string]$entry.Status['implementation'] } else { '' }
        $originalComparison = if ($entry.Status.ContainsKey('original_comparison')) { [string]$entry.Status['original_comparison'] } else { '' }
        $roundtrip = if ($entry.Status.ContainsKey('roundtrip')) { [string]$entry.Status['roundtrip'] } else { '' }
        $unimplemented = $approvedVocabulary['implementation'][0]
        if (($implementation -ne $unimplemented -or
             $originalComparison -ne $unimplemented -or
             $roundtrip -ne $unimplemented) -and
            ($entry.TestCount -eq 0 -or $entry.Evidence.Count -eq 0)) {
            Add-Violation -Rule 'matrix-promotion-evidence' -Path $matrixRelativePath -Detail ("Promoted entry '$($entry.Id)' requires nonempty tests and evidence.")
        }

        foreach ($reference in $entry.Evidence) {
            $script:evidenceReferenceCount++
            Get-PathReferenceResult -Reference $reference -MatrixPath $matrixRelativePath
        }
    }
}

try {
    if (-not (Test-Path -LiteralPath $repositoryFullPath -PathType Container)) {
        Add-Violation -Rule 'repository-root' -Path '.' -Detail 'Repository root does not exist as a directory.'
    } elseif (([IO.File]::GetAttributes($repositoryFullPath) -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        Add-Violation -Rule 'repository-root' -Path '.' -Detail 'Repository root must not be a reparse point.'
    } else {
        Invoke-AssetMetadataChecks
        Invoke-UnityBoundaryChecks
        Invoke-CompatibilityMatrixChecks
    }
} catch {
    Add-Violation -Rule 'fatal-validation-error' -Path '.' -Detail ("Validation stopped because repository metadata could not be inspected safely (" + $_.Exception.GetType().Name + ').')
}

$result = [pscustomobject]@{
    SchemaVersion = 1
    RepositoryRoot = '.'
    AssetEntryCount = $assetEntryCount
    MetaFileCount = $metaFileCount
    MatrixEntryCount = $matrixEntryCount
    EvidenceReferenceCount = $evidenceReferenceCount
    ViolationCount = $violations.Count
    Passed = $violations.Count -eq 0
    Violations = $violations.ToArray()
}

if ($Json) {
    $result | ConvertTo-Json -Depth 6 -Compress
} elseif ($result.Passed) {
    "Repository validation passed: assets=$assetEntryCount meta=$metaFileCount matrixEntries=$matrixEntryCount evidenceReferences=$evidenceReferenceCount"
} else {
    foreach ($violation in $violations) {
        "FAIL [$($violation.Rule)] $($violation.Path): $($violation.Detail)"
    }
    "Repository validation failed: violations=$($violations.Count)"
}

if (-not $result.Passed) {
    exit 1
}
