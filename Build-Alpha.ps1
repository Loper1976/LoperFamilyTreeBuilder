[CmdletBinding()]
param(
    [string] $ArtifactRoot = (Join-Path $env:TEMP 'LoperFamilyTreeBuilder-Alpha')
)

$ErrorActionPreference = 'Stop'
$sourceRoot = Join-Path $PSScriptRoot 'host-source\LoperFamilyTreeBuilder_InstallerFirst_Phase3'
$payload = Join-Path $ArtifactRoot 'payload'

New-Item -ItemType Directory -Force (Join-Path $payload 'app\Web') | Out-Null

dotnet test (Join-Path $PSScriptRoot 'research-agent\tests\ResearchAgent.Core.Tests\ResearchAgent.Core.Tests.csproj') `
    --artifacts-path (Join-Path $ArtifactRoot 'research-tests') -m:1
if ($LASTEXITCODE) { throw 'Research-agent tests failed.' }

dotnet test (Join-Path $sourceRoot 'tests\LoperFamilyTreeBuilder.Tests\LoperFamilyTreeBuilder.Tests.csproj') `
    --artifacts-path (Join-Path $ArtifactRoot 'host-tests') -m:1
if ($LASTEXITCODE) { throw 'Host tests failed.' }

dotnet publish (Join-Path $sourceRoot 'src\LoperFamilyTreeBuilder.Web\LoperFamilyTreeBuilder.Web.csproj') `
    -c Release -r win-x64 --self-contained true `
    --artifacts-path (Join-Path $ArtifactRoot 'web-artifacts') `
    -o (Join-Path $ArtifactRoot 'publish\Web') -m:1
if ($LASTEXITCODE) { throw 'Web publish failed.' }

dotnet publish (Join-Path $sourceRoot 'src\LoperFamilyTreeBuilder.Launcher\LoperFamilyTreeBuilder.Launcher.csproj') `
    -c Release -r win-x64 --self-contained true -p:EnableWindowsTargeting=true `
    --artifacts-path (Join-Path $ArtifactRoot 'launcher-artifacts') `
    -o (Join-Path $ArtifactRoot 'publish\Launcher') -m:1
if ($LASTEXITCODE) { throw 'Launcher publish failed.' }

Copy-Item (Join-Path $ArtifactRoot 'publish\Web\*') (Join-Path $payload 'app\Web') -Recurse -Force
Copy-Item (Join-Path $ArtifactRoot 'publish\Launcher\*') (Join-Path $payload 'app') -Recurse -Force

$localDb = Join-Path $payload 'SqlLocalDB.msi'
if (-not (Test-Path -LiteralPath $localDb)) {
    $bootstrapper = Join-Path $ArtifactRoot 'SQL2022-SSEI-Expr.exe'
    Invoke-WebRequest `
        'https://download.microsoft.com/download/5/1/4/5145fe04-4d30-4b85-b0d1-39533663a2f1/SQL2022-SSEI-Expr.exe' `
        -OutFile $bootstrapper
    $media = Join-Path $ArtifactRoot 'sqlmedia'
    New-Item -ItemType Directory -Force $media | Out-Null
    $download = Start-Process $bootstrapper `
        -ArgumentList @('/Action=Download', '/MediaType=LocalDB', "/MediaPath=$media", '/Quiet') `
        -Wait -PassThru -WindowStyle Hidden
    if ($download.ExitCode) { throw "LocalDB media download failed with exit code $($download.ExitCode)." }
    $downloadedMsi = Get-ChildItem $media -Filter SqlLocalDB.msi -Recurse | Select-Object -First 1
    if (-not $downloadedMsi) { throw 'SqlLocalDB.msi was not found in Microsoft installation media.' }
    Copy-Item -LiteralPath $downloadedMsi.FullName -Destination $localDb
}

$msiProject = Join-Path $sourceRoot 'installer\LoperFamilyTreeBuilder.Msi\LoperFamilyTreeBuilder.Msi.wixproj'
dotnet build $msiProject -c Release "-p:PayloadDir=$payload" -o (Join-Path $ArtifactRoot 'msi')
if ($LASTEXITCODE) { throw 'Application MSI build failed.' }
Copy-Item (Join-Path $ArtifactRoot 'msi\LoperFamilyTreeBuilder.msi') (Join-Path $payload 'LoperFamilyTreeBuilder.msi') -Force

$setupProject = Join-Path $sourceRoot 'installer\LoperFamilyTreeBuilder.Setup\LoperFamilyTreeBuilder.Setup.wixproj'
$setupObj = Join-Path $ArtifactRoot 'setup-obj'
dotnet restore $setupProject "-p:BaseIntermediateOutputPath=$setupObj\"
dotnet build $setupProject -c Release "-p:PayloadDir=$payload" `
    "-p:BaseIntermediateOutputPath=$setupObj\" `
    "-p:IntermediateOutputPath=$setupObj\Release\" `
    -o (Join-Path $ArtifactRoot 'setup')
if ($LASTEXITCODE) { throw 'Setup bundle build failed.' }

Get-FileHash (Join-Path $ArtifactRoot 'setup\LoperFamilyTreeBuilderSetup.exe') -Algorithm SHA256
