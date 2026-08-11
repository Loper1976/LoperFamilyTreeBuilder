param([Parameter(Mandatory=$true)][string]$Root)
$ErrorActionPreference='Stop'
$workspace=$env:GITHUB_WORKSPACE
if([string]::IsNullOrWhiteSpace($workspace)){throw 'GITHUB_WORKSPACE is missing.'}
if(-not(Test-Path $Root)){throw "Source root missing: $Root"}

function Copy-BootstrapFile([string]$Name,[string]$Destination){
    $source=Join-Path $workspace "bootstrap/1.1.8/$Name"
    if(-not(Test-Path $source)){throw "1.1.8 bootstrap file missing: $source"}
    $target=Join-Path $Root $Destination
    New-Item -ItemType Directory -Path (Split-Path $target) -Force | Out-Null
    Copy-Item $source $target -Force
}

Copy-BootstrapFile 'FamilyPortal.razor' 'src/LoperFamilyTreeBuilder.Web/Components/Pages/FamilyPortal.razor'
Copy-BootstrapFile 'ArchivePhotos.razor' 'src/LoperFamilyTreeBuilder.Web/Components/Pages/ArchivePhotos.razor'
Copy-BootstrapFile 'ArchiveDocuments.razor' 'src/LoperFamilyTreeBuilder.Web/Components/Pages/ArchiveDocuments.razor'
Copy-BootstrapFile 'ArchiveLibrary.razor' 'src/LoperFamilyTreeBuilder.Web/Components/Pages/ArchiveLibrary.razor'
Copy-BootstrapFile 'UiArchiveSeparationTests.cs' 'tests/LoperFamilyTreeBuilder.Tests/UiArchiveSeparationTests.cs'

$cssPath=Join-Path $Root 'src/LoperFamilyTreeBuilder.Web/wwwroot/app.css'
$css=Get-Content $cssPath -Raw
if($css-notmatch '1\.1\.8 - UI cleanup, family portal spacing and archive page separation'){
    $addition=Get-Content (Join-Path $workspace 'bootstrap/1.1.8/ui-1.1.8.css') -Raw
    Set-Content $cssPath ($css.TrimEnd()+"`r`n`r`n"+$addition.Trim()+"`r`n") -Encoding utf8
}

$propsPath=Join-Path $Root 'Directory.Build.props'
$props=Get-Content $propsPath -Raw
$props=$props.Replace('<Version>1.1.7</Version>','<Version>1.1.8</Version>')
$props=$props.Replace('<FileVersion>1.1.7.0</FileVersion>','<FileVersion>1.1.8.0</FileVersion>')
$props=$props.Replace('<InformationalVersion>1.1.7-archive-tree-public-profiles</InformationalVersion>','<InformationalVersion>1.1.8-ui-cleanup-archive-separation</InformationalVersion>')
Set-Content $propsPath $props -Encoding utf8

$launcherPath=Join-Path $Root 'src/LoperFamilyTreeBuilder.Launcher/UpgradeBackupService.cs'
$launcher=Get-Content $launcherPath -Raw
$launcher=$launcher.Replace('TargetVersion = "1.1.7"','TargetVersion = "1.1.8"')
Set-Content $launcherPath $launcher -Encoding utf8

$programPath=Join-Path $Root 'src/LoperFamilyTreeBuilder.Web/Program.cs'
$program=Get-Content $programPath -Raw
$program=$program.Replace('version = "1.1.7"','version = "1.1.8"')
$program=$program.Replace('release = "archive-tree-public-profiles"','release = "ui-cleanup-archive-separation"')
Set-Content $programPath $program -Encoding utf8

$bundlePath=Join-Path $Root 'installer/LoperFamilyTreeBuilder.Setup/Bundle.wxs'
$bundle=Get-Content $bundlePath -Raw
$bundle=$bundle.Replace('Version="1.1.7.0"','Version="1.1.8.0"')
$bundle=$bundle.Replace('Theme="hyperlinkSidebarLicense"','Theme="hyperlinkLicense"')
$bundle=$bundle.Replace('LogoFile="..\..\src\LoperFamilyTreeBuilder.Web\wwwroot\images\loper-logo.jpg"','LogoFile="..\..\src\LoperFamilyTreeBuilder.Web\wwwroot\images\loper-logo.jpg"')
$bundle=[regex]::Replace($bundle,'\s*LogoSideFile="[^"]+"','')
Set-Content $bundlePath $bundle -Encoding utf8

$msiPath=Join-Path $Root 'installer/LoperFamilyTreeBuilder.Msi/Package.wxs'
$msi=Get-Content $msiPath -Raw
$msi=$msi.Replace('Version="1.1.7"','Version="1.1.8"')
Set-Content $msiPath $msi -Encoding utf8

Write-Host '1.1.8 applied: installer uses undistorted square Loper artwork, Family Portal spacing is rebuilt, Archive Library is cleaned up, and Photos/Documents are independent workspaces.'
