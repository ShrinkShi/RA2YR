Describe 'M3-C8 real-map integration wrapper' {
    It 'pins the aggregate-only integration command and summary identity' {
        $script = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot '..\Invoke-M3C8RealMapIntegration.ps1')
        [regex]::IsMatch($script, 'M3C8RealMapIntegrationCommand\.Run') | Should Be $true
        [regex]::IsMatch($script, 'M3C8RealMapIntegrationSanitized') | Should Be $true
        [regex]::IsMatch($script, 'records|pixels|filename') | Should Be $true
    }
}
