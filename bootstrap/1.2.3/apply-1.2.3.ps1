param([Parameter(Mandatory=$true)][string]$Root)
$ErrorActionPreference='Stop'
$workspace=$env:GITHUB_WORKSPACE
if([string]::IsNullOrWhiteSpace($workspace)){throw 'GITHUB_WORKSPACE is missing.'}
if(-not(Test-Path $Root)){throw "Source root missing: $Root"}

function Copy-BootstrapFile([string]$Name,[string]$Destination){
    $source=Join-Path $workspace "bootstrap/1.2.3/$Name"
    if(-not(Test-Path $source)){throw "1.2.3 bootstrap file missing: $source"}
    $target=Join-Path $Root $Destination
    New-Item -ItemType Directory -Path (Split-Path $target) -Force|Out-Null
    Copy-Item $source $target -Force
}

Copy-BootstrapFile 'TreeIntegrityEntities.cs' 'src/LoperFamilyTreeBuilder.Core/Entities/TreeIntegrityEntities.cs'
Copy-BootstrapFile 'TreeIntegrityModels.cs' 'src/LoperFamilyTreeBuilder.Core/Models/TreeIntegrityModels.cs'
Copy-BootstrapFile 'TreeIntegrityConfiguration.cs' 'src/LoperFamilyTreeBuilder.Data/Configuration/TreeIntegrityConfiguration.cs'
Copy-BootstrapFile 'TreeIntegrityService.cs' 'src/LoperFamilyTreeBuilder.Data/Services/TreeIntegrityService.cs'
Copy-BootstrapFile 'TreeIntegrityMigration.cs' 'src/LoperFamilyTreeBuilder.Data/Migrations/20260812003000_TreeIntegrityChecker.cs'
Copy-BootstrapFile 'DataQuality.razor' 'src/LoperFamilyTreeBuilder.Web/Components/Pages/DataQuality.razor'
Copy-BootstrapFile 'TreeIntegrityTests.cs' 'tests/LoperFamilyTreeBuilder.Tests/TreeIntegrityTests.cs'
Copy-BootstrapFile 'loper-texas-logo.jpg' 'src/LoperFamilyTreeBuilder.Web/wwwroot/images/loper-logo.jpg'

$dbContextPath=Join-Path $Root 'src/LoperFamilyTreeBuilder.Data/FamilyTreeDbContext.cs'
$dbContext=Get-Content $dbContextPath -Raw
if($dbContext-notmatch 'DbSet<TreeIntegrityIssue>'){
    $anchor='    public DbSet<MediaMigrationItem> MediaMigrationItems => Set<MediaMigrationItem>();'
    if($dbContext.Contains($anchor)){
        $dbContext=$dbContext.Replace($anchor,$anchor+"`r`n`r`n"+'    public DbSet<TreeIntegrityIssue> TreeIntegrityIssues => Set<TreeIntegrityIssue>();'+"`r`n`r`n"+'    public DbSet<TreeIntegrityScanRun> TreeIntegrityScanRuns => Set<TreeIntegrityScanRun>();')
    } else {
        $dbContext=$dbContext.Replace('    public DbSet<ResearchTaskRecord> ResearchTasks => Set<ResearchTaskRecord>();','    public DbSet<TreeIntegrityIssue> TreeIntegrityIssues => Set<TreeIntegrityIssue>();'+"`r`n`r`n"+'    public DbSet<TreeIntegrityScanRun> TreeIntegrityScanRuns => Set<TreeIntegrityScanRun>();'+"`r`n`r`n"+'    public DbSet<ResearchTaskRecord> ResearchTasks => Set<ResearchTaskRecord>();')
    }
}
Set-Content $dbContextPath $dbContext -Encoding utf8

$servicesPath=Join-Path $Root 'src/LoperFamilyTreeBuilder.Data/ServiceCollectionExtensions.cs'
$services=Get-Content $servicesPath -Raw
if($services-notmatch 'TreeIntegrityService'){
    if($services.Contains('        services.AddScoped<MediaMigrationService>();')){$services=$services.Replace('        services.AddScoped<MediaMigrationService>();','        services.AddScoped<MediaMigrationService>();'+"`r`n"+'        services.AddScoped<TreeIntegrityService>();')}
    else{$services=$services.Replace('        services.AddScoped<ResearchIntelligenceService>();','        services.AddScoped<ResearchIntelligenceService>();'+"`r`n"+'        services.AddScoped<TreeIntegrityService>();')}
}
Set-Content $servicesPath $services -Encoding utf8

$navPath=Join-Path $Root 'src/LoperFamilyTreeBuilder.Web/Components/Layout/NavMenu.razor'
$nav=Get-Content $navPath -Raw
if($nav-notmatch 'href="/data-quality"'){
    $nav=$nav.Replace('            <NavLink href="/relationship-finder">Relationship Finder</NavLink>','            <NavLink href="/relationship-finder">Relationship Finder</NavLink>'+"`r`n"+'            <NavLink href="/data-quality">Data Quality Center</NavLink>')
}
Set-Content $navPath $nav -Encoding utf8

$cssPath=Join-Path $Root 'src/LoperFamilyTreeBuilder.Web/wwwroot/app.css'
$css=Get-Content $cssPath -Raw
if($css-notmatch '1\.2\.3 - automated tree error checker'){
    $addition=Get-Content (Join-Path $workspace 'bootstrap/1.2.3/tree-integrity-1.2.3.css') -Raw
    Set-Content $cssPath ($css.TrimEnd()+"`r`n`r`n"+$addition.Trim()+"`r`n") -Encoding utf8
}

$propsPath=Join-Path $Root 'Directory.Build.props'
$props=Get-Content $propsPath -Raw
$props=$props.Replace('<Version>1.2.2</Version>','<Version>1.2.3</Version>')
$props=$props.Replace('<FileVersion>1.2.2.0</FileVersion>','<FileVersion>1.2.3.0</FileVersion>')
$props=$props.Replace('<InformationalVersion>1.2.2-media-migration-messaging-hud</InformationalVersion>','<InformationalVersion>1.2.3-tree-error-checker</InformationalVersion>')
Set-Content $propsPath $props -Encoding utf8

$launcherPath=Join-Path $Root 'src/LoperFamilyTreeBuilder.Launcher/UpgradeBackupService.cs'
$launcher=Get-Content $launcherPath -Raw
$launcher=$launcher.Replace('TargetVersion = "1.2.2"','TargetVersion = "1.2.3"')
Set-Content $launcherPath $launcher -Encoding utf8

$programPath=Join-Path $Root 'src/LoperFamilyTreeBuilder.Web/Program.cs'
$program=Get-Content $programPath -Raw
$program=$program.Replace('version = "1.2.2"','version = "1.2.3"')
$program=$program.Replace('release = "media-migration-messaging-hud"','release = "tree-error-checker"')
Set-Content $programPath $program -Encoding utf8

$productionPath=Join-Path $Root 'src/LoperFamilyTreeBuilder.Web/Components/Pages/ProductionRelease.razor'
if(Test-Path $productionPath){
    $production=Get-Content $productionPath -Raw
    $production=$production.Replace('version 1.2.2','version 1.2.3').Replace('>1.2.2<','>1.2.3<')
    Set-Content $productionPath $production -Encoding utf8
}

$bundlePath=Join-Path $Root 'installer/LoperFamilyTreeBuilder.Setup/Bundle.wxs'
$bundle=Get-Content $bundlePath -Raw
$bundle=$bundle.Replace('Version="1.2.2.0"','Version="1.2.3.0"')
Set-Content $bundlePath $bundle -Encoding utf8

$msiPath=Join-Path $Root 'installer/LoperFamilyTreeBuilder.Msi/Package.wxs'
$msi=Get-Content $msiPath -Raw
$msi=$msi.Replace('Version="1.2.2"','Version="1.2.3"')
Set-Content $msiPath $msi -Encoding utf8

Write-Host '1.2.3 applied: automated tree error checker, Data Quality Center, protected review workflow, and official LOPER Texas logo.'
