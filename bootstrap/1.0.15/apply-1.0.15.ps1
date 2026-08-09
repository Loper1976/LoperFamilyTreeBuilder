param([Parameter(Mandatory=$true)][string]$Root)

$ErrorActionPreference = 'Stop'

function Replace-Exact([string]$RelativePath, [string]$Old, [string]$New) {
    $path = Join-Path $Root $RelativePath
    if (-not (Test-Path $path)) { throw "Required file not found: $path" }
    $text = Get-Content $path -Raw
    if (-not $text.Contains($Old)) { throw "Expected text not found in $RelativePath`n$Old" }
    $text = $text.Replace($Old, $New)
    Set-Content -Path $path -Value $text -Encoding utf8 -NoNewline
}

# Reassemble the verified 1.0.15 feature package.
$s = (Get-Content "$env:GITHUB_WORKSPACE\bootstrap\1.0.15\part00.b64" -Raw).Trim()
$s += (Get-Content "$env:GITHUB_WORKSPACE\bootstrap\1.0.15\part01.b64" -Raw).Trim()
if ($s.Length -ne 16728) { throw "1.0.15 base64 length mismatch: $($s.Length)" }
[IO.File]::WriteAllBytes('115.zip', [Convert]::FromBase64String($s))
$hash = (Get-FileHash 115.zip -Algorithm SHA256).Hash.ToLowerInvariant()
if ($hash -ne 'c68c757a724db1b73cca9a1e4fed2dffae12eb0ef3b8036120c5337dba0fc8e1') { throw "1.0.15 package hash mismatch: $hash" }
Expand-Archive 115.zip -DestinationPath $Root -Force

# Version and installer identity. UpgradeCodes remain stable for cumulative upgrades.
Replace-Exact 'Directory.Build.props' '<Version>1.0.14</Version>' '<Version>1.0.15</Version>'
Replace-Exact 'Directory.Build.props' '<AssemblyVersion>1.0.14.0</AssemblyVersion>' '<AssemblyVersion>1.0.15.0</AssemblyVersion>'
Replace-Exact 'Directory.Build.props' '<FileVersion>1.0.14.0</FileVersion>' '<FileVersion>1.0.15.0</FileVersion>'
Replace-Exact 'Directory.Build.props' '<InformationalVersion>1.0.14-modern-person-profile</InformationalVersion>' '<InformationalVersion>1.0.15-professional-person-report</InformationalVersion>'
Replace-Exact 'installer/LoperFamilyTreeBuilder.Msi/Package.wxs' 'Version="1.0.14"' 'Version="1.0.15"'
Replace-Exact 'installer/LoperFamilyTreeBuilder.Setup/Bundle.wxs' 'Version="1.0.14.0"' 'Version="1.0.15.0"'
Replace-Exact 'src/LoperFamilyTreeBuilder.Launcher/UpgradeBackupService.cs' 'private const string TargetVersion = "1.0.14";' 'private const string TargetVersion = "1.0.15";'

# Align visible, health, and diagnostics version reporting.
$versionFiles = @(
    'src/LoperFamilyTreeBuilder.Web/Program.cs',
    'src/LoperFamilyTreeBuilder.Web/Components/Pages/About.razor',
    'src/LoperFamilyTreeBuilder.Web/Components/Pages/ProductionRelease.razor',
    'src/LoperFamilyTreeBuilder.Data/Services/SystemDiagnosticsService.cs'
)
foreach ($relative in $versionFiles) {
    $path = Join-Path $Root $relative
    if (Test-Path $path) {
        $text = Get-Content $path -Raw
        $text = $text.Replace('1.0.12', '1.0.15')
        $text = $text.Replace('1.0.14', '1.0.15')
        $text = $text.Replace('Modern Person Profile', 'Professional Detailed Person Report')
        Set-Content $path $text -Encoding utf8 -NoNewline
    }
}

# Register the report assembly service.
Replace-Exact 'src/LoperFamilyTreeBuilder.Data/ServiceCollectionExtensions.cs' '        services.AddScoped<SystemDiagnosticsService>();' "        services.AddScoped<SystemDiagnosticsService>();`r`n        services.AddScoped<DetailedPersonReportService>();"

# Put the professional report one click away from the desktop Person Profile.
$oldPersonAction = '                <button class="primary-action" @onclick="EditPerson">Edit Person</button>'
$newPersonAction = $oldPersonAction + "`r`n" + '                <a class="secondary-action inline-link-button" href="@($"/people/{PersonId}/detailed-report")">Detailed Report</a>'
Replace-Exact 'src/LoperFamilyTreeBuilder.Web/Components/Pages/PersonDetails.razor' $oldPersonAction $newPersonAction

Write-Host '1.0.15 Professional Detailed Person Report applied.'
