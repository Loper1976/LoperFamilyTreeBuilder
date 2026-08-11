param([Parameter(Mandatory=$true)][string]$Root)
$ErrorActionPreference='Stop'
$workspace=$env:GITHUB_WORKSPACE
if([string]::IsNullOrWhiteSpace($workspace)){throw 'GITHUB_WORKSPACE is missing.'}
if(-not(Test-Path $Root)){throw "Source root missing: $Root"}
function Copy-BootstrapFile([string]$Name,[string]$Destination){$source=Join-Path $workspace "bootstrap/1.2.2/$Name";if(-not(Test-Path $source)){throw "1.2.2 bootstrap file missing: $source"};$target=Join-Path $Root $Destination;New-Item -ItemType Directory -Path (Split-Path $target) -Force|Out-Null;Copy-Item $source $target -Force}
Copy-BootstrapFile 'FamilyMessagingEntities.cs' 'src/LoperFamilyTreeBuilder.Core/Entities/FamilyMessagingEntities.cs'
Copy-BootstrapFile 'MessagingModels.cs' 'src/LoperFamilyTreeBuilder.Core/Models/MessagingModels.cs'
Copy-BootstrapFile 'MessagingConfiguration.cs' 'src/LoperFamilyTreeBuilder.Data/Configuration/MessagingConfiguration.cs'
Copy-BootstrapFile 'FamilyMessagingService.cs' 'src/LoperFamilyTreeBuilder.Data/Services/FamilyMessagingService.cs'
Copy-BootstrapFile 'FamilyMessagingMigration.cs' 'src/LoperFamilyTreeBuilder.Data/Migrations/20260811234500_FamilyMessaging.cs'
Copy-BootstrapFile 'Messages.razor' 'src/LoperFamilyTreeBuilder.Web/Components/Pages/Messages.razor'
Copy-BootstrapFile 'MediaMigrationEntities.cs' 'src/LoperFamilyTreeBuilder.Core/Entities/MediaMigrationEntities.cs'
Copy-BootstrapFile 'MediaMigrationModels.cs' 'src/LoperFamilyTreeBuilder.Core/Models/MediaMigrationModels.cs'
Copy-BootstrapFile 'MediaMigrationConfiguration.cs' 'src/LoperFamilyTreeBuilder.Data/Configuration/MediaMigrationConfiguration.cs'
Copy-BootstrapFile 'MediaMigrationService.cs' 'src/LoperFamilyTreeBuilder.Data/Services/MediaMigrationService.cs'
Copy-BootstrapFile 'MediaMigrationMigration.cs' 'src/LoperFamilyTreeBuilder.Data/Migrations/20260811235000_MediaMigration.cs'
Copy-BootstrapFile 'MediaMigration.razor' 'src/LoperFamilyTreeBuilder.Web/Components/Pages/MediaMigration.razor'
Copy-BootstrapFile 'MessagingMediaMigrationTests.cs' 'tests/LoperFamilyTreeBuilder.Tests/MessagingMediaMigrationTests.cs'
$dbContextPath=Join-Path $Root 'src/LoperFamilyTreeBuilder.Data/FamilyTreeDbContext.cs';$dbContext=Get-Content $dbContextPath -Raw
if($dbContext-notmatch 'DbSet<FamilyMessage>'){$dbContext=$dbContext.Replace('    public DbSet<FamilyUser> FamilyUsers => Set<FamilyUser>();','    public DbSet<FamilyUser> FamilyUsers => Set<FamilyUser>();'+"`r`n`r`n"+'    public DbSet<FamilyMessage> FamilyMessages => Set<FamilyMessage>();'+"`r`n`r`n"+'    public DbSet<MediaMigrationSession> MediaMigrationSessions => Set<MediaMigrationSession>();'+"`r`n`r`n"+'    public DbSet<MediaMigrationItem> MediaMigrationItems => Set<MediaMigrationItem>();')};Set-Content $dbContextPath $dbContext -Encoding utf8
$servicesPath=Join-Path $Root 'src/LoperFamilyTreeBuilder.Data/ServiceCollectionExtensions.cs';$services=Get-Content $servicesPath -Raw
if($services-notmatch 'FamilyMessagingService'){$services=$services.Replace('        services.AddScoped<FamilyCollaborationService>();','        services.AddScoped<FamilyCollaborationService>();'+"`r`n"+'        services.AddScoped<FamilyMessagingService>();'+"`r`n"+'        services.AddScoped<MediaMigrationService>();')};Set-Content $servicesPath $services -Encoding utf8
$navPath=Join-Path $Root 'src/LoperFamilyTreeBuilder.Web/Components/Layout/NavMenu.razor';$nav=Get-Content $navPath -Raw
if($nav-notmatch 'href="/messages"'){$nav=$nav.Replace('            <NavLink href="/family-portal">My Family Portal</NavLink>','            <NavLink href="/family-portal">My Family Portal</NavLink>'+"`r`n"+'            <NavLink href="/messages">Family Messages</NavLink>')}
if($nav-notmatch 'href="/media-migration"'){$nav=$nav.Replace('            <NavLink href="/gedcom-import">GEDCOM Import &amp; Dry Run</NavLink>','            <NavLink href="/gedcom-import">GEDCOM Import &amp; Dry Run</NavLink>'+"`r`n"+'            <NavLink href="/media-migration">Ancestry / FTM Media</NavLink>')};Set-Content $navPath $nav -Encoding utf8
$portalPath=Join-Path $Root 'src/LoperFamilyTreeBuilder.Web/Components/Pages/FamilyPortal.razor';$portal=Get-Content $portalPath -Raw
if($portal-notmatch 'href="/messages"'){$portal=$portal.Replace('        <a class="secondary-action inline-link-button" href="/my-submissions">My Submissions</a>','        <a class="secondary-action inline-link-button" href="/messages">Family Messages</a>'+"`r`n"+'        <a class="secondary-action inline-link-button" href="/my-submissions">My Submissions</a>')};Set-Content $portalPath $portal -Encoding utf8
$cssPath=Join-Path $Root 'src/LoperFamilyTreeBuilder.Web/wwwroot/app.css';$css=Get-Content $cssPath -Raw
if($css-notmatch '1\.2\.2 - futuristic HUD command-center theme'){$addition=Get-Content (Join-Path $workspace 'bootstrap/1.2.2/hud-1.2.2.css') -Raw;Set-Content $cssPath ($css.TrimEnd()+"`r`n`r`n"+$addition.Trim()+"`r`n") -Encoding utf8}
$propsPath=Join-Path $Root 'Directory.Build.props';$props=Get-Content $propsPath -Raw;$props=$props.Replace('<Version>1.2.1</Version>','<Version>1.2.2</Version>').Replace('<FileVersion>1.2.1.0</FileVersion>','<FileVersion>1.2.2.0</FileVersion>').Replace('<InformationalVersion>1.2.1-gedcom-import-data-integrity</InformationalVersion>','<InformationalVersion>1.2.2-media-migration-messaging-hud</InformationalVersion>');Set-Content $propsPath $props -Encoding utf8
$launcherPath=Join-Path $Root 'src/LoperFamilyTreeBuilder.Launcher/UpgradeBackupService.cs';$launcher=Get-Content $launcherPath -Raw;$launcher=$launcher.Replace('TargetVersion = "1.2.1"','TargetVersion = "1.2.2"');Set-Content $launcherPath $launcher -Encoding utf8
$programPath=Join-Path $Root 'src/LoperFamilyTreeBuilder.Web/Program.cs';$program=Get-Content $programPath -Raw;$program=$program.Replace('version = "1.2.1"','version = "1.2.2"').Replace('release = "gedcom-import-data-integrity"','release = "media-migration-messaging-hud"');Set-Content $programPath $program -Encoding utf8
$productionPath=Join-Path $Root 'src/LoperFamilyTreeBuilder.Web/Components/Pages/ProductionRelease.razor';if(Test-Path $productionPath){$production=Get-Content $productionPath -Raw;$production=$production.Replace('version 1.2.0','version 1.2.2').Replace('>1.2.0<','>1.2.2<');Set-Content $productionPath $production -Encoding utf8}
$bundlePath=Join-Path $Root 'installer/LoperFamilyTreeBuilder.Setup/Bundle.wxs';$bundle=Get-Content $bundlePath -Raw;$bundle=$bundle.Replace('Version="1.2.1.0"','Version="1.2.2.0"');Set-Content $bundlePath $bundle -Encoding utf8
$msiPath=Join-Path $Root 'installer/LoperFamilyTreeBuilder.Msi/Package.wxs';$msi=Get-Content $msiPath -Raw;$msi=$msi.Replace('Version="1.2.1"','Version="1.2.2"');Set-Content $msiPath $msi -Encoding utf8
Write-Host '1.2.2 applied: Ancestry/FTM media migration, 120-day retrievable private messaging, and futuristic HUD theme.'
