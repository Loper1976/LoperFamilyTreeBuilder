[CmdletBinding()]
param(
    [string] $Destination = (Join-Path $PSScriptRoot '..\host-source'),
    [switch] $SkipOverrides
)

$ErrorActionPreference = 'Stop'

$parts = @(
    'source.part01.b64',
    'source.part02.b64',
    'source.part03.b64',
    'source.part04.b64',
    'source.part05.b64',
    'source.part06.b64',
    'source.part07a.b64',
    'source.part07b.b64',
    'source.part07c.b64',
    'source.part07d1.b64'
)

$encoded = foreach ($part in $parts) {
    (Get-Content -LiteralPath (Join-Path $PSScriptRoot $part) -Raw).Trim()
}

$archiveBytes = [Convert]::FromBase64String(($encoded -join ''))
$archivePath = Join-Path ([IO.Path]::GetTempPath()) ('LoperFamilyTreeBuilder-' + [Guid]::NewGuid().ToString('N') + '.zip')

try {
    [IO.File]::WriteAllBytes($archivePath, $archiveBytes)
    New-Item -ItemType Directory -Path $Destination -Force | Out-Null
    Expand-Archive -LiteralPath $archivePath -DestinationPath $Destination -Force
}
finally {
    Remove-Item -LiteralPath $archivePath -Force -ErrorAction SilentlyContinue
}

$sourceRoot = Get-ChildItem -LiteralPath $Destination -Directory |
    Where-Object Name -eq 'LoperFamilyTreeBuilder_InstallerFirst_Phase3' |
    Select-Object -First 1 -ExpandProperty FullName

if (-not $sourceRoot) {
    throw 'The reconstructed source root was not found in the archive.'
}

if (-not $SkipOverrides) {
    $overrideParts = @(
        'person-profile-update.part01.b64',
        'person-profile-update.part02.b64',
        'person-profile-update.tail01.b64',
        'person-profile-update.tail02.b64',
        'person-profile-update.tail03.b64',
        'person-profile-update.tail04.b64',
        'person-profile-update.tail05.b64'
    )

    $overrideText = foreach ($part in $overrideParts) {
        (Get-Content -LiteralPath (Join-Path $PSScriptRoot "overrides\$part") -Raw).Trim()
    }

    $overrideScript = [Text.Encoding]::UTF8.GetString(
        [Convert]::FromBase64String(($overrideText -join '')))
    & ([ScriptBlock]::Create($overrideScript)) -SourceRoot $sourceRoot

    $firstRunSource = Join-Path $PSScriptRoot 'overrides\FirstRunSetupForm.cs'
    $firstRunTarget = Join-Path $sourceRoot 'src\LoperFamilyTreeBuilder.Launcher\FirstRunSetupForm.cs'
    Copy-Item -LiteralPath $firstRunSource -Destination $firstRunTarget -Force
}

# The archive contains historical full-source listings and a truncated workflow.
# They are reconstruction artifacts, not part of the normal buildable source tree.
$reconstructionArtifacts = @(
    (Join-Path $sourceRoot 'PHASE-2-FULL-SOURCE.txt'),
    (Join-Path $sourceRoot 'PHASE-3-FULL-SOURCE.txt'),
    (Join-Path $sourceRoot '.github\workflows\build-installer.yml')
)
foreach ($artifact in $reconstructionArtifacts) {
    Remove-Item -LiteralPath $artifact -Force -ErrorAction SilentlyContinue
}

$sourceRoot
