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

# Keep installer identity stable while incrementing the product version.
Replace-Exact 'Directory.Build.props' '<Version>1.0.13</Version>' '<Version>1.0.14</Version>'
Replace-Exact 'Directory.Build.props' '<AssemblyVersion>1.0.13.0</AssemblyVersion>' '<AssemblyVersion>1.0.14.0</AssemblyVersion>'
Replace-Exact 'Directory.Build.props' '<FileVersion>1.0.13.0</FileVersion>' '<FileVersion>1.0.14.0</FileVersion>'
Replace-Exact 'Directory.Build.props' '<InformationalVersion>1.0.13-production-baseline</InformationalVersion>' '<InformationalVersion>1.0.14-modern-person-profile</InformationalVersion>'
Replace-Exact 'installer/LoperFamilyTreeBuilder.Msi/Package.wxs' 'Version="1.0.13"' 'Version="1.0.14"'
Replace-Exact 'installer/LoperFamilyTreeBuilder.Setup/Bundle.wxs' 'Version="1.0.13.0"' 'Version="1.0.14.0"'
Replace-Exact 'src/LoperFamilyTreeBuilder.Launcher/UpgradeBackupService.cs' 'private const string TargetVersion = "1.0.13";' 'private const string TargetVersion = "1.0.14";'

# Update visible and health-check version reporting where 1.0.13 is part of the reconstructed production baseline.
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
        $text = $text.Replace('1.0.13', '1.0.14')
        $text = $text.Replace('Production Release Baseline', 'Modern Person Profile')
        Set-Content -Path $path -Value $text -Encoding utf8 -NoNewline
    }
}

# Copy complete 1.0.14 overrides into the reconstructed source tree.
$overrideRoot = Join-Path $env:GITHUB_WORKSPACE 'bootstrap/1.0.14/overrides'
if (-not (Test-Path $overrideRoot)) { throw "1.0.14 override root not found: $overrideRoot" }
Get-ChildItem $overrideRoot -File -Recurse | ForEach-Object {
    $relative = [IO.Path]::GetRelativePath($overrideRoot, $_.FullName)
    $destination = Join-Path $Root $relative
    New-Item -ItemType Directory -Force (Split-Path $destination -Parent) | Out-Null
    Copy-Item $_.FullName $destination -Force
}

Write-Host '1.0.14 Modern Person Profile changes applied.'
