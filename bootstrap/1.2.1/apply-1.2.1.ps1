param([Parameter(Mandatory=$true)][string]$Root)
$ErrorActionPreference='Stop'
$workspace=$env:GITHUB_WORKSPACE
if([string]::IsNullOrWhiteSpace($workspace)){throw 'GITHUB_WORKSPACE is missing.'}
if(-not(Test-Path $Root)){throw "Source root missing: $Root"}

function Copy-BootstrapFile([string]$Name,[string]$Destination){
    $source=Join-Path $workspace "bootstrap/1.2.1/$Name"
    if(-not(Test-Path $source)){throw "1.2.1 bootstrap file missing: $source"}
    $target=Join-Path $Root $Destination
    New-Item -ItemType Directory -Path (Split-Path $target) -Force | Out-Null
    Copy-Item $source $target -Force
}

Copy-BootstrapFile 'GedcomDocument.cs' 'src/LoperFamilyTreeBuilder.ImportExport/Gedcom/GedcomDocument.cs'
Copy-BootstrapFile 'GedcomParser.cs' 'src/LoperFamilyTreeBuilder.ImportExport/Gedcom/GedcomParser.cs'
Copy-BootstrapFile 'GedcomImportEntities.cs' 'src/LoperFamilyTreeBuilder.Core/Entities/GedcomImportEntities.cs'
Copy-BootstrapFile 'GedcomModels.cs' 'src/LoperFamilyTreeBuilder.Core/Models/GedcomModels.cs'
Copy-BootstrapFile 'GedcomImportService.cs' 'src/LoperFamilyTreeBuilder.Data/Services/GedcomImportService.cs'
Copy-BootstrapFile 'GedcomImportedNoteConfiguration.cs' 'src/LoperFamilyTreeBuilder.Data/Configuration/GedcomImportedNoteConfiguration.cs'
Copy-BootstrapFile 'GedcomImportReadinessMigration.cs' 'src/LoperFamilyTreeBuilder.Data/Migrations/20260811203000_GedcomImportReadiness.cs'
Copy-BootstrapFile 'GedcomImport.razor' 'src/LoperFamilyTreeBuilder.Web/Components/Pages/GedcomImport.razor'
Copy-BootstrapFile 'ImportReview.razor' 'src/LoperFamilyTreeBuilder.Web/Components/Pages/ImportReview.razor'
Copy-BootstrapFile 'GedcomValidation.razor' 'src/LoperFamilyTreeBuilder.Web/Components/Pages/GedcomValidation.razor'
Copy-BootstrapFile 'GedcomImportReadinessTests.cs' 'tests/LoperFamilyTreeBuilder.Tests/GedcomImportReadinessTests.cs'

$ctxPath=Join-Path $Root 'src/LoperFamilyTreeBuilder.Data/FamilyTreeDbContext.cs'
$ctx=Get-Content $ctxPath -Raw
if($ctx-notmatch 'GedcomImportedNotes'){
    $ctx=$ctx.Replace('public DbSet<GedcomImportIssue> GedcomImportIssues => Set<GedcomImportIssue>();', 'public DbSet<GedcomImportIssue> GedcomImportIssues => Set<GedcomImportIssue>();'+"`r`n`r`n    "+'public DbSet<GedcomImportedNote> GedcomImportedNotes => Set<GedcomImportedNote>();')
}
Set-Content $ctxPath $ctx -Encoding utf8

$cssPath=Join-Path $Root 'src/LoperFamilyTreeBuilder.Web/wwwroot/app.css'
$css=Get-Content $cssPath -Raw
if($css-notmatch '1\.2\.1 - GEDCOM import readiness and data integrity'){
    $addition=Get-Content (Join-Path $workspace 'bootstrap/1.2.1/gedcom-1.2.1.css') -Raw
    Set-Content $cssPath ($css.TrimEnd()+"`r`n`r`n"+$addition.Trim()+"`r`n") -Encoding utf8
}

$navPath=Join-Path $Root 'src/LoperFamilyTreeBuilder.Web/Components/Layout/NavMenu.razor'
$nav=Get-Content $navPath -Raw
$nav=$nav.Replace('<NavLink href="/gedcom-import">GEDCOM Import</NavLink>','<NavLink href="/gedcom-import">GEDCOM Import &amp; Dry Run</NavLink>')
Set-Content $navPath $nav -Encoding utf8

$propsPath=Join-Path $Root 'Directory.Build.props'
$props=Get-Content $propsPath -Raw
$props=$props.Replace('<Version>1.2.0</Version>','<Version>1.2.1</Version>')
$props=$props.Replace('<FileVersion>1.2.0.0</FileVersion>','<FileVersion>1.2.1.0</FileVersion>')
$props=$props.Replace('<InformationalVersion>1.2.0-family-web-preview</InformationalVersion>','<InformationalVersion>1.2.1-gedcom-import-data-integrity</InformationalVersion>')
Set-Content $propsPath $props -Encoding utf8

$launcherPath=Join-Path $Root 'src/LoperFamilyTreeBuilder.Launcher/UpgradeBackupService.cs'
$launcher=Get-Content $launcherPath -Raw
$launcher=$launcher.Replace('TargetVersion = "1.2.0"','TargetVersion = "1.2.1"')
Set-Content $launcherPath $launcher -Encoding utf8

$programPath=Join-Path $Root 'src/LoperFamilyTreeBuilder.Web/Program.cs'
$program=Get-Content $programPath -Raw
$program=$program.Replace('version = "1.2.0"','version = "1.2.1"')
$program=$program.Replace('release = "family-web-preview"','release = "gedcom-import-data-integrity"')
Set-Content $programPath $program -Encoding utf8

$productionPath=Join-Path $Root 'src/LoperFamilyTreeBuilder.Web/Components/Pages/ProductionRelease.razor'
$production=Get-Content $productionPath -Raw
$production=$production.Replace('version 1.2.0','version 1.2.1').Replace('Version 1.2.0','Version 1.2.1').Replace('>1.2.0<','>1.2.1<')
Set-Content $productionPath $production -Encoding utf8

$bundlePath=Join-Path $Root 'installer/LoperFamilyTreeBuilder.Setup/Bundle.wxs'
$bundle=Get-Content $bundlePath -Raw
$bundle=$bundle.Replace('Version="1.2.0.0"','Version="1.2.1.0"')
Set-Content $bundlePath $bundle -Encoding utf8

$msiPath=Join-Path $Root 'installer/LoperFamilyTreeBuilder.Msi/Package.wxs'
$msi=Get-Content $msiPath -Raw
$msi=$msi.Replace('Version="1.2.0"','Version="1.2.1"')
Set-Content $msiPath $msi -Encoding utf8

Write-Host '1.2.1 applied: 1.2.0 web preview preserved; GEDCOM dry run, staging, Legacy Number conflict protection, source/citation preservation, note preservation, data-quality preview, backup/apply and controlled rollback added.'
