Describe 'M3-C7 map terrain ProjectBaseline audit wrapper' {
    It 'pins the map-driven command and sanitized output contract' {
        $script=Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot '..\Invoke-MapTerrainProjectBaselineAudit.ps1')
        [regex]::IsMatch($script,'MapTerrainProjectBaselineAuditCommand\.Run') | Should Be $true
        [regex]::IsMatch($script,'MapTerrainProjectBaselineAuditSanitized') | Should Be $true
        [regex]::IsMatch($script,'records|pixels|filename') | Should Be $true
    }
}
