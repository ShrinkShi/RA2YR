[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $UnityEditorPath,

    [Parameter()]
    [ValidateSet('EditMode', 'PlayMode', 'All')]
    [string] $TestPlatform = 'All',

    [Parameter()]
    [string] $ProjectRoot,

    [Parameter()]
    [ValidateRange(60, 86400)]
    [int] $TimeoutSeconds = 1800
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$expectedUnityVersion = '2022.3.60f1c1'

function Quote-ProcessArgument {
    param([Parameter(Mandatory)][string] $Value)

    if ($Value.Contains('"')) {
        throw 'A process argument contains an unsupported quote character.'
    }
    if ($Value.EndsWith('\', [StringComparison]::Ordinal)) {
        throw 'A quoted process path must not end with a directory separator.'
    }
    '"' + $Value + '"'
}

function Assert-UnityResult {
    param(
        [Parameter(Mandatory)][string] $ResultPath,
        [Parameter(Mandatory)][string] $Platform
    )

    if (-not (Test-Path -LiteralPath $ResultPath -PathType Leaf)) {
        throw "$Platform did not create its isolated test result XML."
    }
    $resultFile = Get-Item -LiteralPath $ResultPath -Force
    if ($resultFile.Length -eq 0) {
        throw "$Platform created an empty test result XML."
    }

    try {
        $document = New-Object System.Xml.XmlDocument
        $document.XmlResolver = $null
        $settings = New-Object System.Xml.XmlReaderSettings
        $settings.DtdProcessing = [System.Xml.DtdProcessing]::Prohibit
        $settings.XmlResolver = $null
        $reader = [System.Xml.XmlReader]::Create($ResultPath, $settings)
        try {
            $document.Load($reader)
        } finally {
            $reader.Dispose()
        }
    } catch {
        throw "$Platform test result XML could not be parsed."
    }
    $root = $document.DocumentElement
    if ($null -eq $root) {
        throw "$Platform test result XML has no document element."
    }

    switch ($root.LocalName) {
        'test-run' {
            $failed = 0
            $passed = 0
            $testCaseCount = 0
            if (-not [int]::TryParse([string]$root.GetAttribute('failed'), [ref]$failed)) {
                throw "$Platform NUnit test-run result has no valid failed count."
            }
            if (-not [int]::TryParse([string]$root.GetAttribute('testcasecount'), [ref]$testCaseCount) -or
                $testCaseCount -le 0) {
                throw "$Platform NUnit test-run result contains no executed test cases."
            }
            if (-not [int]::TryParse([string]$root.GetAttribute('passed'), [ref]$passed) -or
                $passed -le 0) {
                throw "$Platform NUnit test-run result contains no passing executed test cases."
            }
            if ($failed -ne 0 -or [string]$root.GetAttribute('result') -ne 'Passed') {
                throw "$Platform NUnit test-run result did not pass (failed=$failed, result=$($root.GetAttribute('result')))."
            }
        }
        'test-results' {
            $failures = 0
            $errors = 0
            $notRun = 0
            $inconclusive = 0
            $invalid = 0
            $total = 0
            if (-not [int]::TryParse([string]$root.GetAttribute('failures'), [ref]$failures)) {
                throw "$Platform NUnit test-results document has no valid failures count."
            }
            if (-not [int]::TryParse([string]$root.GetAttribute('total'), [ref]$total) -or $total -le 0) {
                throw "$Platform NUnit test-results document contains no executed test cases."
            }
            foreach ($countName in @('errors', 'not-run', 'inconclusive', 'invalid')) {
                $parsed = 0
                if (-not [int]::TryParse([string]$root.GetAttribute($countName), [ref]$parsed)) {
                    throw "$Platform NUnit test-results document has no valid $countName count."
                }
                Set-Variable -Name ($countName.Replace('-', '')) -Value $parsed
            }
            if (($total - $notRun) -le 0) {
                throw "$Platform NUnit test-results document contains no actually executed test cases."
            }
            if ($failures -ne 0 -or $errors -ne 0 -or $inconclusive -ne 0 -or $invalid -ne 0) {
                throw "$Platform NUnit test-results document reports failures=$failures, errors=$errors, inconclusive=$inconclusive, invalid=$invalid."
            }
        }
        default {
            throw "$Platform returned an unsupported test result root '$($root.LocalName)'."
        }
    }
}

function Test-UnityResultDocumentReady {
    param([Parameter(Mandatory)][string] $ResultPath)

    if (-not (Test-Path -LiteralPath $ResultPath -PathType Leaf)) {
        return $false
    }
    if ((Get-Item -LiteralPath $ResultPath -Force).Length -eq 0) {
        return $false
    }

    $reader = $null
    try {
        $document = New-Object System.Xml.XmlDocument
        $document.XmlResolver = $null
        $settings = New-Object System.Xml.XmlReaderSettings
        $settings.DtdProcessing = [System.Xml.DtdProcessing]::Prohibit
        $settings.XmlResolver = $null
        $reader = [System.Xml.XmlReader]::Create($ResultPath, $settings)
        $document.Load($reader)
        return $null -ne $document.DocumentElement
    } catch {
        return $false
    } finally {
        if ($null -ne $reader) {
            $reader.Dispose()
        }
    }
}

function Remove-StaleLaunchedUnityLock {
    param([Parameter(Mandatory)][string] $LockPath)

    if (-not (Test-Path -LiteralPath $LockPath)) {
        return
    }

    $lock = Get-Item -LiteralPath $LockPath -Force
    if ($lock.PSIsContainer -or
        $lock.Length -ne 0 -or
        ($lock.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw 'The launched Unity process left an unexpected lock-file object; refusing cleanup.'
    }

    if (@(Get-Process Unity -ErrorAction SilentlyContinue).Count -ne 0) {
        throw 'A Unity process appeared before stale lock cleanup; refusing to delete the lock file.'
    }

    [IO.File]::Delete($lock.FullName)
}

if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = Join-Path $PSScriptRoot '..\..'
}

$resolvedProjectRoot = [IO.Path]::GetFullPath($ProjectRoot).TrimEnd([IO.Path]::DirectorySeparatorChar)
$resolvedEditorPath = [IO.Path]::GetFullPath($UnityEditorPath)

if (-not (Test-Path -LiteralPath $resolvedEditorPath -PathType Leaf)) {
    throw 'Unity Editor executable was not found.'
}

$versionFile = Join-Path $resolvedProjectRoot 'ProjectSettings\ProjectVersion.txt'
if (-not (Test-Path -LiteralPath $versionFile -PathType Leaf)) {
    throw 'The requested project root is not a Unity project.'
}
$versionText = Get-Content -LiteralPath $versionFile -Raw -Encoding UTF8
$projectVersionMatch = [regex]::Match($versionText, '(?m)^m_EditorVersion:\s*(?<version>\S+)\s*$')
if (-not $projectVersionMatch.Success -or $projectVersionMatch.Groups['version'].Value -ne $expectedUnityVersion) {
    throw "ProjectVersion.txt must specify Unity $expectedUnityVersion."
}

$editorVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($resolvedEditorPath).ProductVersion
if ([string]::IsNullOrWhiteSpace($editorVersion) -or
    -not $editorVersion.StartsWith($expectedUnityVersion, [StringComparison]::Ordinal) -or
    ($editorVersion.Length -gt $expectedUnityVersion.Length -and $editorVersion[$expectedUnityVersion.Length] -notin @('_', '+'))) {
    throw "Unity Editor executable version '$editorVersion' does not match required version '$expectedUnityVersion'."
}

$lockFile = Join-Path $resolvedProjectRoot 'Temp\UnityLockfile'
if (Test-Path -LiteralPath $lockFile) {
    throw 'The Unity project is open in another Editor process. Close it before running command-line tests.'
}

$resultsRoot = Join-Path $resolvedProjectRoot 'TestResults'
[IO.Directory]::CreateDirectory($resultsRoot) | Out-Null
$runId = (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssfffZ') + '-' + [Guid]::NewGuid().ToString('N')
$runRoot = Join-Path $resultsRoot $runId
[IO.Directory]::CreateDirectory($runRoot) | Out-Null

$platforms = if ($TestPlatform -eq 'All') {
    @('EditMode', 'PlayMode')
} else {
    @($TestPlatform)
}
$postResultExitGraceSeconds = 30

foreach ($platform in $platforms) {
    $platformRoot = Join-Path $runRoot $platform
    [IO.Directory]::CreateDirectory($platformRoot) | Out-Null
    $resultPath = Join-Path $platformRoot 'results.xml'
    $logPath = Join-Path $platformRoot 'unity.log'
    if (Test-Path -LiteralPath $resultPath) {
        throw "$platform isolated result path unexpectedly already exists."
    }

    $arguments = @(
        '-batchmode',
        '-nographics',
        '-projectPath', (Quote-ProcessArgument -Value $resolvedProjectRoot),
        '-runTests',
        '-testPlatform', $platform.ToLowerInvariant(),
        '-testResults', (Quote-ProcessArgument -Value $resultPath),
        '-logFile', (Quote-ProcessArgument -Value $logPath)
    )

    $process = Start-Process -FilePath $resolvedEditorPath -ArgumentList $arguments -PassThru -NoNewWindow
    try {
        $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
        $resultReadyAt = $null
        $forcedPostResultShutdown = $false
        $timedOut = $false

        while (-not $process.WaitForExit(250)) {
            if ($null -eq $resultReadyAt -and
                (Test-UnityResultDocumentReady -ResultPath $resultPath)) {
                $resultReadyAt = [DateTime]::UtcNow
            }

            if ($null -ne $resultReadyAt -and
                [DateTime]::UtcNow.Subtract($resultReadyAt).TotalSeconds -ge $postResultExitGraceSeconds) {
                $process.Kill()
                $process.WaitForExit()
                $forcedPostResultShutdown = $true
                break
            }

            if ([DateTime]::UtcNow -ge $deadline) {
                $process.Kill()
                $process.WaitForExit()
                $timedOut = $true
                break
            }
        }

        if ($timedOut) {
            Remove-StaleLaunchedUnityLock -LockPath $lockFile
            throw "$platform tests exceeded the $TimeoutSeconds second timeout."
        }

        $process.Refresh()
        if (-not $forcedPostResultShutdown -and $process.ExitCode -ne 0) {
            throw "$platform tests failed with Unity exit code $($process.ExitCode). See the isolated log under TestResults/$runId/$platform/."
        }

        if ($forcedPostResultShutdown) {
            Remove-StaleLaunchedUnityLock -LockPath $lockFile
        }

        Assert-UnityResult -ResultPath $resultPath -Platform $platform
        if ($forcedPostResultShutdown) {
            Write-Warning "$platform results passed, but the launched headless Unity process required post-result shutdown after $postResultExitGraceSeconds seconds."
        }
    } finally {
        $process.Dispose()
    }
}

"Unity test run completed: $($platforms -join ', ')"
"Isolated results: TestResults/$runId"
