param([Parameter(Mandatory=$true)][string]$Root)
$ErrorActionPreference='Stop'
$workspace=$env:GITHUB_WORKSPACE
if([string]::IsNullOrWhiteSpace($workspace)){throw 'GITHUB_WORKSPACE is missing.'}
if(-not(Test-Path $Root)){throw "Source root missing: $Root"}

function Copy-BootstrapFile([string]$Name,[string]$Destination){
    $source=Join-Path $workspace "bootstrap/1.1.9/$Name"
    if(-not(Test-Path $source)){throw "1.1.9 bootstrap file missing: $source"}
    $target=Join-Path $Root $Destination
    New-Item -ItemType Directory -Path (Split-Path $target) -Force | Out-Null
    Copy-Item $source $target -Force
}

Copy-BootstrapFile 'NavMenu.razor' 'src/LoperFamilyTreeBuilder.Web/Components/Layout/NavMenu.razor'
Copy-BootstrapFile 'MainLayout.razor' 'src/LoperFamilyTreeBuilder.Web/Components/Layout/MainLayout.razor'
Copy-BootstrapFile 'ArchivePhotos.razor' 'src/LoperFamilyTreeBuilder.Web/Components/Pages/ArchivePhotos.razor'
Copy-BootstrapFile 'ArchiveDocuments.razor' 'src/LoperFamilyTreeBuilder.Web/Components/Pages/ArchiveDocuments.razor'
Copy-BootstrapFile 'ArchiveMediaDetails.razor' 'src/LoperFamilyTreeBuilder.Web/Components/Pages/ArchiveMediaDetails.razor'
Copy-BootstrapFile 'ArchiveDetailExperienceTests.cs' 'tests/LoperFamilyTreeBuilder.Tests/ArchiveDetailExperienceTests.cs'

$cssPath=Join-Path $Root 'src/LoperFamilyTreeBuilder.Web/wwwroot/app.css'
$css=Get-Content $cssPath -Raw
if($css-notmatch '1\.1\.9 - archive detail enhancements, distinct titles and clean collapsible navigation'){
    $addition=Get-Content (Join-Path $workspace 'bootstrap/1.1.9/ui-1.1.9.css') -Raw
    Set-Content $cssPath ($css.TrimEnd()+"`r`n`r`n"+$addition.Trim()+"`r`n") -Encoding utf8
}

$propsPath=Join-Path $Root 'Directory.Build.props'
$props=Get-Content $propsPath -Raw
$props=$props.Replace('<Version>1.1.8</Version>','<Version>1.1.9</Version>')
$props=$props.Replace('<FileVersion>1.1.8.0</FileVersion>','<FileVersion>1.1.9.0</FileVersion>')
$props=$props.Replace('<InformationalVersion>1.1.8-ui-cleanup-archive-separation</InformationalVersion>','<InformationalVersion>1.1.9-archive-detail-enhancements</InformationalVersion>')
Set-Content $propsPath $props -Encoding utf8

$launcherPath=Join-Path $Root 'src/LoperFamilyTreeBuilder.Launcher/UpgradeBackupService.cs'
$launcher=Get-Content $launcherPath -Raw
$launcher=$launcher.Replace('TargetVersion = "1.1.8"','TargetVersion = "1.1.9"')
Set-Content $launcherPath $launcher -Encoding utf8

$programPath=Join-Path $Root 'src/LoperFamilyTreeBuilder.Web/Program.cs'
$program=Get-Content $programPath -Raw
$program=$program.Replace('version = "1.1.8"','version = "1.1.9"')
$program=$program.Replace('release = "ui-cleanup-archive-separation"','release = "archive-detail-enhancements"')
Set-Content $programPath $program -Encoding utf8

$bundlePath=Join-Path $Root 'installer/LoperFamilyTreeBuilder.Setup/Bundle.wxs'
$bundle=Get-Content $bundlePath -Raw
$bundle=$bundle.Replace('Version="1.1.8.0"','Version="1.1.9.0"')
Set-Content $bundlePath $bundle -Encoding utf8

$msiPath=Join-Path $Root 'installer/LoperFamilyTreeBuilder.Msi/Package.wxs'
$msi=Get-Content $msiPath -Raw
$msi=$msi.Replace('Version="1.1.8"','Version="1.1.9"')
Set-Content $msiPath $msi -Encoding utf8

Write-Host '1.1.9 applied: archive detail viewer enhanced, page titles differentiated, and sidebar navigation reorganized into collapsible sections.'
