Describe 'Invoke-IsoMapPack5ProjectBaselineAudit' {
    BeforeAll {
        $scriptPath = Join-Path $PSScriptRoot '..\Invoke-IsoMapPack5ProjectBaselineAudit.ps1'
        $scriptText = Get-Content -LiteralPath $scriptPath -Raw
    }

    It 'pins the project Unity version' {
        ($scriptText -match '2022\.3\.60f1c1') | Should Be $true
    }

    It 'uses the dedicated audit command and sanitized output' {
        ($scriptText -match 'IsoMapPack5ProjectBaselineAuditCommand\.Run') | Should Be $true
        ($scriptText -match 'm3-c4-isomap-pack5-project-baseline-summary\.json') | Should Be $true
    }

    It 'rejects repository-local audit cache and forbidden detail' {
        ($scriptText -match 'cache must remain outside the repository') | Should Be $true
        ($scriptText -match 'forbidden detail') | Should Be $true
    }
}
