param([Parameter(Mandatory=$true)][string]$Root)

$ErrorActionPreference = 'Stop'

$parts = @(
    'part00.b64',
    'part01.b64',
    'part01b.b64',
    'part01c.b64',
    'part02.b64',
    'part03.b64',
    'part04.b64'
)

$encoded = ''
foreach ($part in $parts) {
    $path = Join-Path $env:GITHUB_WORKSPACE "bootstrap/1.0.16/$part"
    if (-not (Test-Path $path)) { throw "Missing 1.0.16 package part: $part" }
    $encoded += (Get-Content $path -Raw).Trim()
}

if ($encoded.Length -ne 49120) {
    throw "1.0.16 base64 length mismatch. Expected 49120, found $($encoded.Length)."
}

$zipPath = Join-Path $env:RUNNER_TEMP 'LoperFamilyTreeBuilder_1.0.16_delta.zip'
[IO.File]::WriteAllBytes($zipPath, [Convert]::FromBase64String($encoded))
$hash = (Get-FileHash $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
if ($hash -ne 'cafec464c9acd07fdecbf39a5063ddd1fbe802f30062592de9e32a0ce7c33ecb') {
    throw "1.0.16 reconstructed package hash mismatch: $hash"
}

Expand-Archive -Path $zipPath -DestinationPath $Root -Force

$props = Get-Content (Join-Path $Root 'Directory.Build.props') -Raw
$msi = Get-Content (Join-Path $Root 'installer/LoperFamilyTreeBuilder.Msi/Package.wxs') -Raw
$bundle = Get-Content (Join-Path $Root 'installer/LoperFamilyTreeBuilder.Setup/Bundle.wxs') -Raw
$launcher = Get-Content (Join-Path $Root 'src/LoperFamilyTreeBuilder.Launcher/UpgradeBackupService.cs') -Raw
$policy = Get-Content (Join-Path $Root 'src/LoperFamilyTreeBuilder.Core/Policies/LegacyNumberPolicy.cs') -Raw
$services = Get-Content (Join-Path $Root 'src/LoperFamilyTreeBuilder.Data/ServiceCollectionExtensions.cs') -Raw
$program = Get-Content (Join-Path $Root 'src/LoperFamilyTreeBuilder.Web/Program.cs') -Raw

if ($props -notmatch '<Version>1\.0\.16</Version>') { throw 'Application version is not 1.0.16.' }
if ($msi -notmatch 'Version="1\.0\.16"' -or $msi -notmatch '4BDBF4F0-7C82-4FA5-B0B0-6E6CA04E80E9') { throw 'MSI 1.0.16 cumulative-upgrade identity failed.' }
if ($bundle -notmatch 'Version="1\.0\.16\.0"' -or $bundle -notmatch 'B90A31E4-FA5B-4DDC-8802-8D07E4206C1F') { throw 'Bundle 1.0.16 cumulative-upgrade identity failed.' }
if ($launcher -notmatch 'TargetVersion = "1\.0\.16"') { throw 'Pre-upgrade backup target is not 1.0.16.' }
if ($policy -notmatch 'Legacy Number') { throw 'Legacy Number preservation policy is missing.' }
if ($services -notmatch 'ResearchIntelligenceService') { throw 'Research Intelligence service is not registered.' }
if ($program -notmatch '1\.0\.16') { throw 'Web version reporting is not 1.0.16.' }

foreach ($required in @(
    'src/LoperFamilyTreeBuilder.Core/Entities/ResearchIntelligenceEntities.cs',
    'src/LoperFamilyTreeBuilder.Core/Models/ResearchIntelligenceModels.cs',
    'src/LoperFamilyTreeBuilder.Data/Configuration/ResearchIntelligenceConfiguration.cs',
    'src/LoperFamilyTreeBuilder.Data/Migrations/20260809110000_ResearchIntelligence.cs',
    'src/LoperFamilyTreeBuilder.Data/Services/ResearchIntelligenceService.cs',
    'src/LoperFamilyTreeBuilder.Web/Components/Pages/GlobalSearch.razor',
    'src/LoperFamilyTreeBuilder.Web/Components/Pages/ResearchIntelligence.razor',
    'tests/LoperFamilyTreeBuilder.Tests/ResearchIntelligenceTests.cs'
)) {
    if (-not (Test-Path (Join-Path $Root $required))) { throw "Required 1.0.16 file missing: $required" }
}

Write-Host "1.0.16 Research Intelligence reconstructed package verified: $hash"
Write-Host '1.0.16 Research Intelligence & Global Archive Search applied.'
