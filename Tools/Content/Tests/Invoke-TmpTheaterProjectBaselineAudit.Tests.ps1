Describe 'M3-C6 TMP/theater ProjectBaseline audit wrapper' {
    It 'has a strict Unity command and sanitized summary contract' {
        $script = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot '..\Invoke-TmpTheaterProjectBaselineAudit.ps1')
        [regex]::IsMatch($script, 'TmpTheaterProjectBaselineAuditCommand\.Run') | Should Be $true
        [regex]::IsMatch($script, 'TmpTheaterProjectBaselineAuditSanitized') | Should Be $true
        [regex]::IsMatch($script, 'failed|Invalid') | Should Be $true
    }
}
