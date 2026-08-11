param([Parameter(Mandatory=$true)][string]$Root)
$ErrorActionPreference='Stop'
$workspace=$env:GITHUB_WORKSPACE
if([string]::IsNullOrWhiteSpace($workspace)){throw 'GITHUB_WORKSPACE is missing.'}
if(-not(Test-Path $Root)){throw "Source root missing: $Root"}

function Copy-BootstrapFile([string]$Name,[string]$Destination){
    $source=Join-Path $workspace "bootstrap/1.2.0/$Name"
    if(-not(Test-Path $source)){throw "1.2.0 bootstrap file missing: $source"}
    $target=Join-Path $Root $Destination
    New-Item -ItemType Directory -Path (Split-Path $target) -Force | Out-Null
    Copy-Item $source $target -Force
}

Copy-BootstrapFile 'PublicLayout.razor' 'src/LoperFamilyTreeBuilder.Web/Components/Layout/PublicLayout.razor'
Copy-BootstrapFile 'PublicDirectory.razor' 'src/LoperFamilyTreeBuilder.Web/Components/Pages/PublicDirectory.razor'
Copy-BootstrapFile 'PublicPersonProfile.razor' 'src/LoperFamilyTreeBuilder.Web/Components/Pages/PublicPersonProfile.razor'
Copy-BootstrapFile 'PublicPreview.razor' 'src/LoperFamilyTreeBuilder.Web/Components/Pages/PublicPreview.razor'
Copy-BootstrapFile 'FamilyWebPreviewTests.cs' 'tests/LoperFamilyTreeBuilder.Tests/FamilyWebPreviewTests.cs'

$cssPath=Join-Path $Root 'src/LoperFamilyTreeBuilder.Web/wwwroot/app.css'
$css=Get-Content $cssPath -Raw
if($css-notmatch '1\.2\.0 - family web experience and safe public preview'){
    $addition=Get-Content (Join-Path $workspace 'bootstrap/1.2.0/public-1.2.0.css') -Raw
    Set-Content $cssPath ($css.TrimEnd()+"`r`n`r`n"+$addition.Trim()+"`r`n") -Encoding utf8
}

$navPath=Join-Path $Root 'src/LoperFamilyTreeBuilder.Web/Components/Layout/NavMenu.razor'
$nav=Get-Content $navPath -Raw
if($nav-notmatch 'href="/public-preview"'){
    $nav=$nav.Replace('<NavLink href="/public-profiles">Public Profiles</NavLink>','<NavLink href="/public-profiles">Public Profiles</NavLink>'+"`r`n            "+'<NavLink href="/public-preview">Public Preview</NavLink>')
}
Set-Content $navPath $nav -Encoding utf8

$profilesPath=Join-Path $Root 'src/LoperFamilyTreeBuilder.Web/Components/Pages/PublicProfiles.razor'
$profiles=Get-Content $profilesPath -Raw
$old='<div class="page-header modern-page-header"><div><div class="page-eyebrow">Web Publishing</div><h1>Public / Private Profiles</h1><p>Explicitly control which reviewed person profiles can appear on loper.family. Private records remain private by default.</p></div><a class="secondary-action inline-link-button" href="/family" target="_blank">Preview Public Directory</a></div>'
$new='<div class="page-header modern-page-header"><div><div class="page-eyebrow">Web Publishing</div><h1>Public / Private Profiles</h1><p>Explicitly control which reviewed person profiles can appear in the family-facing preview. Private records remain private by default.</p></div><div class="page-header-actions"><a class="secondary-action inline-link-button" href="/public-preview">Preview Center</a><a class="primary-action inline-link-button" href="/family" target="_blank">Open Public View</a></div></div>'
$profiles=$profiles.Replace($old,$new)
Set-Content $profilesPath $profiles -Encoding utf8

$productionPath=Join-Path $Root 'src/LoperFamilyTreeBuilder.Web/Components/Pages/ProductionRelease.razor'
$production=Get-Content $productionPath -Raw
$production=$production.Replace('Final local archive, recovery, hosted-deployment, and release checks for version 1.1.0.','Final local archive, recovery, hosted-deployment, and release checks for version 1.2.0.')
$production=$production.Replace('<div class="summary-value">1.1.0</div>','<div class="summary-value">1.2.0</div>')
if($production-notmatch 'Owner approval is still required before any deployment'){
    $production=$production.Replace('<section class="page-card">'+"`r`n        "+'<h2>Windows Installer Signing</h2>','<div class="alert alert-warning"><strong>Deployment gate:</strong> Owner approval is still required before any deployment or publication to loper.family. This release does not deploy the application.</div>'+"`r`n`r`n    "+'<section class="page-card">'+"`r`n        "+'<h2>Windows Installer Signing</h2>')
}
Set-Content $productionPath $production -Encoding utf8

$propsPath=Join-Path $Root 'Directory.Build.props'
$props=Get-Content $propsPath -Raw
$props=$props.Replace('<Version>1.1.9</Version>','<Version>1.2.0</Version>')
$props=$props.Replace('<FileVersion>1.1.9.0</FileVersion>','<FileVersion>1.2.0.0</FileVersion>')
$props=$props.Replace('<InformationalVersion>1.1.9-archive-detail-enhancements</InformationalVersion>','<InformationalVersion>1.2.0-family-web-preview</InformationalVersion>')
Set-Content $propsPath $props -Encoding utf8

$launcherPath=Join-Path $Root 'src/LoperFamilyTreeBuilder.Launcher/UpgradeBackupService.cs'
$launcher=Get-Content $launcherPath -Raw
$launcher=$launcher.Replace('TargetVersion = "1.1.9"','TargetVersion = "1.2.0"')
Set-Content $launcherPath $launcher -Encoding utf8

$programPath=Join-Path $Root 'src/LoperFamilyTreeBuilder.Web/Program.cs'
$program=Get-Content $programPath -Raw
$program=$program.Replace('version = "1.1.9"','version = "1.2.0"')
$program=$program.Replace('release = "archive-detail-enhancements"','release = "family-web-preview"')
Set-Content $programPath $program -Encoding utf8

$bundlePath=Join-Path $Root 'installer/LoperFamilyTreeBuilder.Setup/Bundle.wxs'
$bundle=Get-Content $bundlePath -Raw
$bundle=$bundle.Replace('Version="1.1.9.0"','Version="1.2.0.0"')
Set-Content $bundlePath $bundle -Encoding utf8

$msiPath=Join-Path $Root 'installer/LoperFamilyTreeBuilder.Msi/Package.wxs'
$msi=Get-Content $msiPath -Raw
$msi=$msi.Replace('Version="1.1.9"','Version="1.2.0"')
Set-Content $msiPath $msi -Encoding utf8

Write-Host '1.2.0 applied: family-facing directory/profile polish, local public preview center, explicit deployment gate, and version updates.'
