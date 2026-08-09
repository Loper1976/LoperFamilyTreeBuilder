param([Parameter(Mandatory=$true)][string]$Root)

$ErrorActionPreference = 'Stop'

$parts = 0..5 | ForEach-Object { Join-Path $env:GITHUB_WORKSPACE ("bootstrap/1.1.0/part{0:D2}.b64" -f $_) }
foreach ($part in $parts) {
    if (-not (Test-Path $part)) { throw "Missing 1.1.0 package part: $part" }
}

$encoded = ($parts | ForEach-Object { (Get-Content $_ -Raw).Trim() }) -join ''
if ($encoded.Length -ne 67944) { throw "1.1.0 base64 length mismatch: $($encoded.Length)" }

$zip = Join-Path $env:RUNNER_TEMP 'LoperFamilyTreeBuilder-1.1.0-delta.zip'
[IO.File]::WriteAllBytes($zip, [Convert]::FromBase64String($encoded))
$hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
if ($hash -ne 'c1dbacb5743d0e972b21a4809bea12c90c3970b7e55ed6fe520f971a2bb52bf9') {
    throw "1.1.0 package hash mismatch: $hash"
}

Expand-Archive $zip -DestinationPath $Root -Force

$required = @(
    'src/LoperFamilyTreeBuilder.Infrastructure/Configuration/WebExperienceOptions.cs',
    'src/LoperFamilyTreeBuilder.Infrastructure/Storage/IArchiveMediaStorage.cs',
    'src/LoperFamilyTreeBuilder.Infrastructure/Storage/SecureUploadPolicy.cs',
    'src/LoperFamilyTreeBuilder.Web/Components/Pages/WebExperience.razor',
    'tests/LoperFamilyTreeBuilder.Tests/WebExperienceTests.cs'
)
foreach ($relative in $required) {
    if (-not (Test-Path (Join-Path $Root $relative))) { throw "Required 1.1.0 file missing: $relative" }
}

$props = Get-Content (Join-Path $Root 'Directory.Build.props') -Raw
$msi = Get-Content (Join-Path $Root 'installer/LoperFamilyTreeBuilder.Msi/Package.wxs') -Raw
$bundle = Get-Content (Join-Path $Root 'installer/LoperFamilyTreeBuilder.Setup/Bundle.wxs') -Raw
$launcher = Get-Content (Join-Path $Root 'src/LoperFamilyTreeBuilder.Launcher/UpgradeBackupService.cs') -Raw
if ($props -notmatch '<Version>1\.1\.0</Version>') { throw 'Application version is not 1.1.0.' }
if ($msi -notmatch 'Version="1\.1\.0"' -or $msi -notmatch '4BDBF4F0-7C82-4FA5-B0B0-6E6CA04E80E9') { throw 'MSI 1.1.0 upgrade identity failed.' }
if ($bundle -notmatch 'Version="1\.1\.0\.0"' -or $bundle -notmatch 'B90A31E4-FA5B-4DDC-8802-8D07E4206C1F') { throw 'Bundle 1.1.0 upgrade identity failed.' }
if ($launcher -notmatch 'TargetVersion = "1\.1\.0"') { throw 'Pre-upgrade backup target is not 1.1.0.' }

Write-Host '1.1.0 Web Experience Architecture applied.'
