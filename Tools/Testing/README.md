# Unity test entry point

Close the Unity Editor before invoking batch-mode tests, then run:

```powershell
./Tools/Testing/Invoke-UnityTests.ps1 `
    -UnityEditorPath 'C:\Path\To\Unity.exe' `
    -TestPlatform All
```

The script requires both the project and Editor executable to identify as
Unity `2022.3.60f1c1`, and refuses to run while `Temp/UnityLockfile` exists.
Each invocation writes XML and logs to a new ignored
`TestResults/<run-id>/<platform>/` directory, applies a process timeout, and
accepts the run only when that invocation created parseable, passing NUnit
XML containing at least one genuinely executed, passing test. An all-skipped,
inconclusive, stale, empty, or zero-test result cannot satisfy the check.

Unity Test Framework 1.1.33 owns test-run shutdown. Do not add Unity's general
`-quit` switch: it can exit before scheduling tests while still returning
process exit code 0. Missing or zero-test XML is always treated as failure.

After a complete result XML appears, the wrapper allows the launched headless
Editor 30 seconds to exit normally. If that exact child process remains hung,
the wrapper stops it, safely removes only its empty non-reparse
`Temp/UnityLockfile`, validates the completed XML, and emits a warning. A
timeout before a complete result remains a failure. Lock cleanup also refuses
to proceed if any Unity process appears after the launched child is stopped.
