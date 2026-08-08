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

# Version and installer identity. UpgradeCodes deliberately remain unchanged.
Replace-Exact 'Directory.Build.props' '<Version>1.0.11</Version>' '<Version>1.0.12</Version>'
Replace-Exact 'Directory.Build.props' '<AssemblyVersion>1.0.11.0</AssemblyVersion>' '<AssemblyVersion>1.0.12.0</AssemblyVersion>'
Replace-Exact 'Directory.Build.props' '<FileVersion>1.0.11.0</FileVersion>' '<FileVersion>1.0.12.0</FileVersion>'
Replace-Exact 'Directory.Build.props' '<InformationalVersion>1.0.11-cumulative</InformationalVersion>' '<InformationalVersion>1.0.12-release-candidate</InformationalVersion>'
Replace-Exact 'installer/LoperFamilyTreeBuilder.Msi/Package.wxs' 'Version="1.0.11"' 'Version="1.0.12"'
Replace-Exact 'installer/LoperFamilyTreeBuilder.Setup/Bundle.wxs' 'Version="1.0.11.0"' 'Version="1.0.12.0"'
Replace-Exact 'src/LoperFamilyTreeBuilder.Launcher/UpgradeBackupService.cs' 'private const string TargetVersion = "1.0.11";' 'private const string TargetVersion = "1.0.12";'

# Health/version reporting.
Replace-Exact 'src/LoperFamilyTreeBuilder.Web/Program.cs' 'version = "1.0.11"' 'version = "1.0.12"'

# Register diagnostics service.
Replace-Exact 'src/LoperFamilyTreeBuilder.Data/ServiceCollectionExtensions.cs' '        services.AddScoped<HostingReadinessService>();' "        services.AddScoped<HostingReadinessService>();`r`n        services.AddScoped<SystemDiagnosticsService>();"

# Add diagnostics to Dashboard quick actions.
Replace-Exact 'src/LoperFamilyTreeBuilder.Web/Components/Pages/Home.razor' '            <a href="/backup">Create Manual Backup</a>' "            <a href=\"/backup\">Create Manual Backup</a>`r`n            <a href=\"/diagnostics\">Run System Diagnostics</a>"

# Add diagnostics and version pages to System navigation.
Replace-Exact 'src/LoperFamilyTreeBuilder.Web/Components/Layout/NavMenu.razor' '        <NavLink href="/hosting">Hosted Deployment</NavLink>' "        <NavLink href=\"/hosting\">Hosted Deployment</NavLink>`r`n        <NavLink href=\"/diagnostics\">System Diagnostics</NavLink>`r`n        <NavLink href=\"/about\">About / Version</NavLink>"

# Copy complete 1.0.12 new-source overrides into the reconstructed tree.
$overrideRoot = Join-Path $env:GITHUB_WORKSPACE 'bootstrap/1.0.12/overrides'
if (-not (Test-Path $overrideRoot)) { throw "1.0.12 override root not found: $overrideRoot" }
Get-ChildItem $overrideRoot -File -Recurse | ForEach-Object {
    $relative = [IO.Path]::GetRelativePath($overrideRoot, $_.FullName)
    $destination = Join-Path $Root $relative
    New-Item -ItemType Directory -Force (Split-Path $destination -Parent) | Out-Null
    Copy-Item $_.FullName $destination -Force
}

Write-Host '1.0.12 Validation & Release Candidate changes applied.'
