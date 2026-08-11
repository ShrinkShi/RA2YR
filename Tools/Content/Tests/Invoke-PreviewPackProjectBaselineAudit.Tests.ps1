Describe 'Invoke-PreviewPackProjectBaselineAudit' {
    BeforeAll {
        $scriptPath = Join-Path $PSScriptRoot '..\Invoke-PreviewPackProjectBaselineAudit.ps1'
        $scriptText = Get-Content -LiteralPath $scriptPath -Raw
    }

    It 'pins the project Unity version' {
        ($scriptText -match '2022\.3\.60f1c1') | Should Be $true
    }

    It 'uses the dedicated PreviewPack audit command and sanitized output' {
        ($scriptText -match 'PreviewPackProjectBaselineAuditCommand\.Run') | Should Be $true
        ($scriptText -match 'm3-c5-preview-pack-project-baseline-summary\.json') | Should Be $true
    }

    It 'rejects repository-local cache and forbidden detail' {
        ($scriptText -match 'cache must remain outside the repository') | Should Be $true
        ($scriptText -match 'forbidden detail') | Should Be $true
    }
}
