param([Parameter(Mandatory=$true)][string]$Root)

$ErrorActionPreference='Stop'

function Replace-Regex([string]$RelativePath,[string]$Pattern,[string]$Replacement,[switch]$Required) {
    $path=Join-Path $Root $RelativePath
    if(-not(Test-Path $path)){if($Required){throw "Required file not found: $RelativePath"};return}
    $text=Get-Content $path -Raw
    $updated=[regex]::Replace($text,$Pattern,$Replacement)
    if($Required -and $updated -eq $text){throw "Expected pattern not found in $RelativePath: $Pattern"}
    Set-Content $path $updated -Encoding utf8 -NoNewline
}

$overrideRoot=Join-Path $env:GITHUB_WORKSPACE 'bootstrap/1.0.16/overrides'
if(-not(Test-Path $overrideRoot)){throw "1.0.16 overrides not found: $overrideRoot"}
Get-ChildItem $overrideRoot -File -Recurse|ForEach-Object{
    $relative=[IO.Path]::GetRelativePath($overrideRoot,$_.FullName)
    $destination=Join-Path $Root $relative
    New-Item -ItemType Directory -Force (Split-Path $destination -Parent)|Out-Null
    Copy-Item $_.FullName $destination -Force
}

# Application and installer version. Stable UpgradeCodes are intentionally preserved.
Replace-Regex 'Directory.Build.props' '<Version>1\.0\.15</Version>' '<Version>1.0.16</Version>' -Required
Replace-Regex 'Directory.Build.props' '<AssemblyVersion>1\.0\.15\.0</AssemblyVersion>' '<AssemblyVersion>1.0.16.0</AssemblyVersion>'
Replace-Regex 'Directory.Build.props' '<FileVersion>1\.0\.15\.0</FileVersion>' '<FileVersion>1.0.16.0</FileVersion>'
Replace-Regex 'Directory.Build.props' '<InformationalVersion>[^<]+</InformationalVersion>' '<InformationalVersion>1.0.16-research-intelligence</InformationalVersion>' -Required
Replace-Regex 'installer/LoperFamilyTreeBuilder.Msi/Package.wxs' 'Version="1\.0\.15"' 'Version="1.0.16"' -Required
Replace-Regex 'installer/LoperFamilyTreeBuilder.Setup/Bundle.wxs' 'Version="1\.0\.15\.0"' 'Version="1.0.16.0"' -Required
Replace-Regex 'src/LoperFamilyTreeBuilder.Launcher/UpgradeBackupService.cs' 'TargetVersion = "1\.0\.15"' 'TargetVersion = "1.0.16"' -Required
Replace-Regex 'src/LoperFamilyTreeBuilder.Web/Program.cs' 'version = "1\.0\.15"' 'version = "1.0.16"'
Replace-Regex 'src/LoperFamilyTreeBuilder.Web/Components/Pages/About.razor' '1\.0\.15' '1.0.16'
Replace-Regex 'src/LoperFamilyTreeBuilder.Web/Components/Pages/About.razor' 'Professional Detailed Person Report' 'Research Intelligence & Global Archive Search'

# Register research intelligence services.
$servicesPath=Join-Path $Root 'src/LoperFamilyTreeBuilder.Data/ServiceCollectionExtensions.cs'
$services=Get-Content $servicesPath -Raw
if($services -notmatch 'ResearchIntelligenceStore'){
    if($services.Contains('services.AddScoped<DetailedPersonReportService>();')){
        $services=$services.Replace('services.AddScoped<DetailedPersonReportService>();',"services.AddScoped<DetailedPersonReportService>();`r`n        services.AddScoped<ResearchIntelligenceStore>();`r`n        services.AddScoped<GlobalArchiveSearchService>();")
    } elseif($services.Contains('services.AddScoped<SystemDiagnosticsService>();')){
        $services=$services.Replace('services.AddScoped<SystemDiagnosticsService>();',"services.AddScoped<SystemDiagnosticsService>();`r`n        services.AddScoped<ResearchIntelligenceStore>();`r`n        services.AddScoped<GlobalArchiveSearchService>();")
    } else {
        throw 'Could not find service-registration anchor.'
    }
    Set-Content $servicesPath $services -Encoding utf8 -NoNewline
}

# Add Research Intelligence to permanent navigation.
$navPath=Join-Path $Root 'src/LoperFamilyTreeBuilder.Web/Components/Layout/NavMenu.razor'
$nav=Get-Content $navPath -Raw
if($nav -notmatch 'href="/archive-search"'){
    $insert="        <NavLink href=\"/archive-search\">Global Archive Search</NavLink>`r`n        <NavLink href=\"/research-intelligence\">Research Intelligence</NavLink>`r`n"
    $anchor=[regex]::Match($nav,'(?m)^\s*<NavLink href="/ai-research"[^\r\n]*</NavLink>\s*$')
    if($anchor.Success){$nav=$nav.Insert($anchor.Index+$anchor.Length,"`r`n$insert")}
    else {
        $anchor=[regex]::Match($nav,'(?m)^\s*<NavLink href="/research-tasks"[^\r\n]*</NavLink>\s*$')
        if($anchor.Success){$nav=$nav.Insert($anchor.Index+$anchor.Length,"`r`n$insert")}else{throw 'Research navigation insertion anchor not found.'}
    }
    Set-Content $navPath $nav -Encoding utf8 -NoNewline
}

# Dashboard shortcuts.
$homePath=Join-Path $Root 'src/LoperFamilyTreeBuilder.Web/Components/Pages/Home.razor'
$home=Get-Content $homePath -Raw
if($home -notmatch '/archive-search'){
    $anchor=[regex]::Match($home,'(?m)^\s*<a href="/research-tasks"[^\r\n]*</a>\s*$')
    $links='            <a href="/archive-search">Search Entire Archive</a>'+"`r`n"+'            <a href="/research-intelligence">Research Intelligence</a>'
    if($anchor.Success){$home=$home.Insert($anchor.Index+$anchor.Length,"`r`n$links")}
    else {
        $anchor=[regex]::Match($home,'(?m)^\s*<a href="/diagnostics"[^\r\n]*</a>\s*$')
        if($anchor.Success){$home=$home.Insert($anchor.Index+$anchor.Length,"`r`n$links")}
    }
    Set-Content $homePath $home -Encoding utf8 -NoNewline
}

# Append modern research UI styles.
$cssFile=Get-ChildItem (Join-Path $Root 'src/LoperFamilyTreeBuilder.Web/wwwroot') -Filter '*.css' -Recurse|Where-Object{(Get-Content $_.FullName -Raw)-match 'detailed-person-report|person-profile|page-card'}|Select-Object -First 1
if(-not $cssFile){$cssFile=Get-ChildItem (Join-Path $Root 'src/LoperFamilyTreeBuilder.Web/wwwroot') -Filter '*.css' -Recurse|Select-Object -First 1}
if(-not $cssFile){throw 'No Web CSS file found.'}
$css=Get-Content $cssFile.FullName -Raw
if($css -notmatch '1\.0\.16 research intelligence'){
$css+=@'

/* 1.0.16 research intelligence */
.research-page-header{align-items:flex-end}.research-page-header .eyebrow,.eyebrow{text-transform:uppercase;letter-spacing:.09em;font-size:.72rem;font-weight:700;color:#667b8f}.header-actions{display:flex;gap:.6rem;flex-wrap:wrap}.archive-search-panel{display:grid;gap:1.1rem}.archive-search-primary label,.saved-search-bar label,.research-form-grid label{display:grid;gap:.35rem;font-size:.82rem;font-weight:600;color:#425466}.archive-search-row{display:grid;grid-template-columns:1fr auto;gap:.6rem}.archive-search-row input{font-size:1.04rem;padding:.72rem .85rem}.advanced-search{border-top:1px solid #e2e8f0;border-bottom:1px solid #e2e8f0;padding:.8rem 0}.advanced-search summary{cursor:pointer;font-weight:700;color:#334e68}.advanced-filter-grid{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:.8rem;margin-top:1rem}.advanced-check-grid{display:flex;flex-wrap:wrap;gap:1rem;margin-top:.8rem;font-size:.86rem}.advanced-check-grid label,.checkbox-label{display:flex!important;align-items:center;gap:.45rem!important}.saved-search-bar{display:grid;grid-template-columns:auto minmax(220px,1fr) auto;align-items:end;gap:.65rem}.archive-search-message{padding:.7rem .85rem;background:#eef6fb;border-radius:9px;color:#334e68}.compact-section{padding-block:1rem}.section-heading-row{display:flex;justify-content:space-between;gap:1rem;align-items:center;margin-bottom:1rem}.section-heading-row h2{margin:0}.section-heading-row>span{font-size:.82rem;color:#64748b}.saved-search-chips{display:flex;gap:.55rem;flex-wrap:wrap}.saved-search-chip{border:1px solid #cbd5e1;background:#fff;border-radius:999px;padding:.45rem .75rem;color:#334e68;cursor:pointer}.saved-search-chip:hover{background:#f1f5f9}.archive-results-summary,.research-metric-grid{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:.8rem;margin:1rem 0}.research-metric-grid{grid-template-columns:repeat(6,minmax(0,1fr))}.metric-card{border:1px solid #dbe3ec;border-radius:12px;background:#fff;padding:.9rem 1rem;display:flex;flex-direction:column;gap:.2rem}.metric-card span{font-size:.76rem;color:#64748b}.metric-card strong{font-size:1.35rem;color:#17324d}.metric-card.metric-alert{border-color:#c98c8c;background:#fffafa}.people-search-results{display:grid;gap:.55rem}.person-search-result{display:grid;grid-template-columns:minmax(180px,1fr) auto;gap:.7rem 1rem;padding:.85rem 1rem;border:1px solid #e2e8f0;border-radius:10px;text-decoration:none;color:inherit}.person-search-result:hover{border-color:#9fb4c5;background:#f8fafc}.person-search-result>div:first-child{display:flex;gap:.55rem;align-items:baseline}.person-search-result>div:first-child span{font-size:.82rem;color:#64748b}.person-search-meta{display:flex;gap:.5rem;flex-wrap:wrap;justify-content:flex-end}.person-search-meta span,.person-search-flags span{font-size:.72rem;border:1px solid #dbe3ec;border-radius:999px;padding:.22rem .5rem;background:#f8fafc}.person-search-flags{grid-column:1/-1;display:flex;gap:.4rem;flex-wrap:wrap}.archive-result-group{margin-top:1rem}.archive-hit{display:grid;grid-template-columns:1fr auto;gap:1rem;padding:.85rem 0;border-top:1px solid #edf2f7}.archive-hit:first-of-type{border-top:0}.archive-hit-main p{margin:.25rem 0 0;color:#475569}.archive-hit-meta{display:flex;flex-direction:column;align-items:flex-end;gap:.18rem;font-size:.78rem;color:#64748b}.research-callout{display:flex;justify-content:space-between;gap:1rem;align-items:center;border-left:4px solid #446b86}.research-callout h2{margin:0 0 .3rem}.research-callout p{margin:0;color:#475569}.quality-summary-grid{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:.7rem;margin-bottom:1rem}.quality-summary-grid>div{padding:.75rem;border-radius:10px;background:#f8fafc;display:flex;justify-content:space-between}.quality-issue-list{display:grid;gap:.35rem;max-height:520px;overflow:auto}.quality-issue{display:grid;grid-template-columns:90px 1fr auto;gap:.8rem;align-items:start;padding:.7rem;border-top:1px solid #edf2f7}.quality-issue p{margin:.2rem 0 0;color:#475569}.quality-issue small{color:#64748b}.quality-severity{font-size:.7rem;text-transform:uppercase;letter-spacing:.06em;border-radius:999px;padding:.25rem .45rem;text-align:center;background:#e2e8f0}.quality-severity.critical{background:#fdecec;color:#8b2c2c}.quality-severity.warning{background:#fff5dd;color:#75520d}.quality-severity.research{background:#edf4fa;color:#315f80}.research-form-grid{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:.8rem;margin-bottom:.8rem}.research-form-grid.two-column{grid-template-columns:repeat(2,minmax(0,1fr))}.research-form-grid .span-two{grid-column:1/-1}.section-intro{color:#475569;max-width:850px}.research-record-list{display:grid;gap:.45rem;margin-top:1rem}.research-record{display:flex;justify-content:space-between;gap:1rem;padding:.8rem;border-top:1px solid #edf2f7}.research-record>div:first-child{display:flex;flex-direction:column;gap:.2rem}.research-record span,.research-record small{font-size:.78rem;color:#64748b}.research-record p{margin:.25rem 0;color:#475569}.link-button{border:0;background:transparent;color:#315f80;cursor:pointer;padding:.25rem}.link-button.danger{color:#8b2c2c}.proof-workspace-list{display:grid;gap:1rem;margin-top:1.2rem}.proof-workspace{border:1px solid #dbe3ec;border-radius:12px;padding:1rem}.proof-workspace-header{display:flex;justify-content:space-between;gap:1rem}.proof-workspace-header h3{margin:.35rem 0}.status-pill,.priority-pill,.negative-search-badge{display:inline-block;font-size:.68rem;text-transform:uppercase;letter-spacing:.05em;border-radius:999px;padding:.25rem .5rem;background:#edf4fa;color:#315f80}.negative-search-badge{background:#fff5dd;color:#75520d}.priority-pill.critical,.priority-pill.high{background:#fdecec;color:#8b2c2c}.priority-pill.low{background:#eef2f6;color:#566575}.proof-candidate{margin-top:.9rem;padding-top:.9rem;border-top:1px solid #edf2f7}.proof-evidence-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:.65rem;margin-top:.65rem}.proof-evidence-grid>div,.proof-reasoning,.proof-conclusion{background:#f8fafc;border-radius:9px;padding:.7rem}.proof-evidence-grid span,.proof-reasoning span,.proof-conclusion span{font-size:.68rem;text-transform:uppercase;letter-spacing:.06em;font-weight:700;color:#64748b}.proof-evidence-grid p,.proof-reasoning p,.proof-conclusion p{margin:.25rem 0 0;white-space:pre-wrap}.proof-conclusion{margin-top:.6rem;border-left:3px solid #446b86}.proof-reasoning{margin-top:.6rem}.record-title-row{display:flex;align-items:center;gap:.5rem;flex-wrap:wrap}.research-toast{position:fixed;right:1rem;bottom:1rem;max-width:420px;padding:.85rem 1rem;background:#17324d;color:#fff;border-radius:10px;box-shadow:0 8px 24px rgba(15,23,42,.18);z-index:100}
@media(max-width:1050px){.research-metric-grid{grid-template-columns:repeat(3,minmax(0,1fr))}.advanced-filter-grid,.research-form-grid{grid-template-columns:repeat(2,minmax(0,1fr))}}
@media(max-width:700px){.archive-search-row,.saved-search-bar,.person-search-result,.archive-hit,.quality-issue{grid-template-columns:1fr}.archive-hit-meta,.person-search-meta{align-items:flex-start;justify-content:flex-start}.archive-results-summary,.research-metric-grid,.quality-summary-grid,.advanced-filter-grid,.research-form-grid,.research-form-grid.two-column,.proof-evidence-grid{grid-template-columns:1fr}.research-callout,.proof-workspace-header{align-items:flex-start;flex-direction:column}.quality-issue-list{max-height:none}.research-toast{position:static;margin-top:1rem}}
'@
Set-Content $cssFile.FullName $css -Encoding utf8 -NoNewline
}

Write-Host '1.0.16 Research Intelligence & Global Archive Search applied.'
