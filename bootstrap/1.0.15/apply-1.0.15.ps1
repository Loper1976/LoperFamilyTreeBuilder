param([Parameter(Mandatory=$true)][string]$Root)

$ErrorActionPreference='Stop'

function Replace-Regex([string]$RelativePath,[string]$Pattern,[string]$Replacement,[switch]$Required) {
    $path=Join-Path $Root $RelativePath
    if(-not (Test-Path $path)) { if($Required){throw "Required file not found: $RelativePath"}; return }
    $text=Get-Content $path -Raw
    $updated=[regex]::Replace($text,$Pattern,$Replacement)
    if($Required -and $updated -eq $text) { throw "Expected pattern not found in $RelativePath: $Pattern" }
    Set-Content $path $updated -Encoding utf8 -NoNewline
}

# Copy the complete 1.0.15 source additions first.
$overrideRoot=Join-Path $env:GITHUB_WORKSPACE 'bootstrap/1.0.15/overrides'
if(-not (Test-Path $overrideRoot)) { throw "1.0.15 overrides not found: $overrideRoot" }
Get-ChildItem $overrideRoot -File -Recurse | ForEach-Object {
    $relative=[IO.Path]::GetRelativePath($overrideRoot,$_.FullName)
    $destination=Join-Path $Root $relative
    New-Item -ItemType Directory -Force (Split-Path $destination -Parent) | Out-Null
    Copy-Item $_.FullName $destination -Force
}

# Correct nullable Guid reflection pattern: boxed Nullable<Guid> values arrive as Guid.
$reportService=Join-Path $Root 'src/LoperFamilyTreeBuilder.Data/Services/DetailedPersonReportService.cs'
$serviceText=Get-Content $reportService -Raw
$serviceText=[regex]::Replace($serviceText,'\r?\n\s*if \(value is Guid\? nullableGuid && nullableGuid\.HasValue && nullableGuid\.Value == personId\)\r?\n\s*return true;','')
Set-Content $reportService $serviceText -Encoding utf8 -NoNewline

# Version the application and preserve the existing stable MSI/Burn UpgradeCodes.
Replace-Regex 'Directory.Build.props' '<Version>1\.0\.14</Version>' '<Version>1.0.15</Version>' -Required
Replace-Regex 'Directory.Build.props' '<AssemblyVersion>1\.0\.14\.0</AssemblyVersion>' '<AssemblyVersion>1.0.15.0</AssemblyVersion>'
Replace-Regex 'Directory.Build.props' '<FileVersion>1\.0\.14\.0</FileVersion>' '<FileVersion>1.0.15.0</FileVersion>'
Replace-Regex 'Directory.Build.props' '<InformationalVersion>[^<]+</InformationalVersion>' '<InformationalVersion>1.0.15-professional-person-report</InformationalVersion>' -Required
Replace-Regex 'installer/LoperFamilyTreeBuilder.Msi/Package.wxs' 'Version="1\.0\.14"' 'Version="1.0.15"' -Required
Replace-Regex 'installer/LoperFamilyTreeBuilder.Setup/Bundle.wxs' 'Version="1\.0\.14\.0"' 'Version="1.0.15.0"' -Required
Replace-Regex 'src/LoperFamilyTreeBuilder.Launcher/UpgradeBackupService.cs' 'TargetVersion = "1\.0\.14"' 'TargetVersion = "1.0.15"' -Required
Replace-Regex 'src/LoperFamilyTreeBuilder.Web/Program.cs' 'version = "1\.0\.14"' 'version = "1.0.15"'
Replace-Regex 'src/LoperFamilyTreeBuilder.Web/Components/Pages/About.razor' '1\.0\.14' '1.0.15'
Replace-Regex 'src/LoperFamilyTreeBuilder.Web/Components/Pages/About.razor' 'Modern Person Profile' 'Professional Detailed Person Report'

# Register the read-only report service.
$servicesPath=Join-Path $Root 'src/LoperFamilyTreeBuilder.Data/ServiceCollectionExtensions.cs'
$services=Get-Content $servicesPath -Raw
if($services -notmatch 'DetailedPersonReportService') {
    if($services.Contains('services.AddScoped<SystemDiagnosticsService>();')) {
        $services=$services.Replace('services.AddScoped<SystemDiagnosticsService>();',"services.AddScoped<SystemDiagnosticsService>();`r`n        services.AddScoped<DetailedPersonReportService>();")
    } else {
        $services=[regex]::Replace($services,'(?m)^(\s*)return services;','$1services.AddScoped<DetailedPersonReportService>();`r`n$1return services;',1)
    }
    Set-Content $servicesPath $services -Encoding utf8 -NoNewline
}

# Add a prominent Detailed Report action to the existing modern desktop profile without
# changing the underlying person page model or historical identifiers.
$personPath=Join-Path $Root 'src/LoperFamilyTreeBuilder.Web/Components/Pages/PersonDetails.razor'
if(-not (Test-Path $personPath)) { throw 'PersonDetails.razor was not found.' }
$personText=Get-Content $personPath -Raw
if($personText -notmatch '/person-report/') {
    $route=[regex]::Match($personText,'@page\s+"/people/\{(?<p>[A-Za-z_][A-Za-z0-9_]*):guid\}"')
    if(-not $route.Success) { throw 'Could not determine PersonDetails route parameter.' }
    $param=$route.Groups['p'].Value
    $link='<div class="person-profile-report-action"><a class="btn btn-primary" href="@($"/person-report/{PARAM}")">Print Detailed Profile</a></div>'.Replace('PARAM',$param)
    $pageTitle=[regex]::Match($personText,'(?m)^<PageTitle[^\r\n]*</PageTitle>')
    if($pageTitle.Success) {
        $personText=$personText.Insert($pageTitle.Index+$pageTitle.Length,"`r`n`r`n$link")
    } else {
        $personText=$link+"`r`n"+$personText
    }
    Set-Content $personPath $personText -Encoding utf8 -NoNewline
}

# Append professional screen and print styles to the stylesheet already owning the modern profile.
$cssFile=Get-ChildItem (Join-Path $Root 'src/LoperFamilyTreeBuilder.Web/wwwroot') -Filter '*.css' -Recurse |
    Where-Object { (Get-Content $_.FullName -Raw) -match 'person-profile|profile-hero|page-card' } |
    Select-Object -First 1
if(-not $cssFile) { $cssFile=Get-ChildItem (Join-Path $Root 'src/LoperFamilyTreeBuilder.Web/wwwroot') -Filter '*.css' -Recurse | Select-Object -First 1 }
if(-not $cssFile) { throw 'No Web CSS file was found.' }
$css=Get-Content $cssFile.FullName -Raw
if($css -notmatch '1\.0\.15 detailed person report') {
$css += @'

/* 1.0.15 detailed person report */
.person-profile-report-action{display:flex;justify-content:flex-end;margin:.25rem 0 1rem}.report-loading{padding:2rem;color:#475569}.report-toolbar{position:sticky;top:0;z-index:20;display:flex;justify-content:space-between;gap:1rem;align-items:center;padding:.8rem 1rem;margin-bottom:1rem;background:rgba(255,255,255,.96);backdrop-filter:blur(10px);border:1px solid #dbe3ec;border-radius:14px;box-shadow:0 8px 24px rgba(15,23,42,.08)}.report-toolbar-title{display:flex;flex-direction:column}.report-toolbar-title span{font-size:.82rem;color:#64748b}.report-toolbar-actions{display:flex;gap:.6rem;align-items:center;flex-wrap:wrap}.report-toolbar select{min-width:165px;border:1px solid #cbd5e1;border-radius:9px;padding:.55rem .7rem;background:#fff}.detailed-person-report{max-width:1040px;margin:0 auto;background:#fff;border:1px solid #dbe3ec;border-radius:18px;overflow:hidden;box-shadow:0 18px 50px rgba(15,23,42,.08);color:#172033}.report-hero{display:grid;grid-template-columns:180px 1fr;gap:2rem;padding:2.25rem;background:linear-gradient(135deg,#f8fafc,#eef4f8);border-bottom:1px solid #dbe3ec}.report-photo-frame{width:180px;height:220px;border-radius:14px;overflow:hidden;background:#e2e8f0;border:5px solid #fff;box-shadow:0 8px 20px rgba(15,23,42,.14)}.report-photo-frame img{width:100%;height:100%;object-fit:cover}.report-photo-placeholder{height:100%;display:grid;place-items:center;font-size:3rem;font-weight:700;color:#64748b}.report-kicker{text-transform:uppercase;letter-spacing:.11em;font-size:.72rem;font-weight:700;color:#52687d;margin-bottom:.55rem}.report-identity h1{font-size:2.45rem;line-height:1.08;margin:.2rem 0 .4rem;color:#102a43}.report-lifespan{font-size:1.15rem;color:#52687d;margin-bottom:1rem}.report-badges{display:flex;flex-wrap:wrap;gap:.55rem}.report-badge{display:inline-flex;gap:.35rem;align-items:center;padding:.42rem .7rem;border:1px solid #cbd5e1;border-radius:999px;background:#fff;font-size:.86rem}.report-preservation-note{margin-top:1rem;color:#64748b;font-size:.82rem}.report-section{padding:1.8rem 2.25rem;border-bottom:1px solid #e6edf3;break-inside:auto}.report-section-heading{display:flex;align-items:baseline;gap:.75rem;margin-bottom:1.2rem}.report-section-heading>span{font-size:.76rem;font-weight:700;color:#6b8298;letter-spacing:.08em}.report-section-heading h2{font-size:1.35rem;margin:0;color:#15324b}.report-section-heading em{margin-left:auto;font-size:.78rem;color:#6b7280;font-style:normal}.report-fact-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:.85rem 1.5rem;margin:0}.report-fact{padding:.7rem 0;border-bottom:1px solid #edf2f7}.report-fact dt,.report-record-facts dt{text-transform:uppercase;letter-spacing:.06em;font-size:.68rem;font-weight:700;color:#6b8298}.report-fact dd,.report-record-facts dd{margin:.22rem 0 0;color:#172033}.report-family-grid{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:1rem}.report-family-group{border:1px solid #dbe3ec;border-radius:12px;padding:1rem}.report-family-group h3{margin:0 0 .65rem;font-size:.88rem;text-transform:uppercase;letter-spacing:.06em;color:#52687d}.report-family-person{padding:.45rem 0;border-top:1px solid #edf2f7}.report-family-person:first-of-type{border-top:0}.report-family-person a{font-weight:600;text-decoration:none}.report-family-person small{display:block;color:#64748b;margin-top:.15rem}.report-timeline{position:relative;margin-left:.25rem}.report-timeline:before{content:"";position:absolute;left:116px;top:.4rem;bottom:.4rem;width:2px;background:#dbe3ec}.report-timeline-item{display:grid;grid-template-columns:100px 1fr;gap:2rem;position:relative;padding:.35rem 0 1.15rem}.report-timeline-item:before{content:"";position:absolute;left:110px;top:.68rem;width:14px;height:14px;border-radius:50%;background:#fff;border:3px solid #446b86}.report-timeline-date{font-size:.82rem;font-weight:700;color:#52687d;text-align:right}.report-timeline-content{padding-left:.45rem}.report-timeline-content h3{margin:0;font-size:1rem}.report-timeline-content p{margin:.4rem 0;color:#364152}.report-place{font-size:.84rem;color:#64748b;margin-top:.15rem}.report-record{padding:1rem 0;border-top:1px solid #edf2f7;break-inside:avoid}.report-record:first-of-type{border-top:0}.report-record h3{margin:0 0 .65rem;font-size:1rem;color:#20384e}.report-narrative{white-space:pre-wrap;color:#364152}.report-record-facts{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:.55rem 1.25rem;margin:0}.report-record-facts>div{padding:.35rem 0}.report-medical-section{background:#fafcfd}.report-medical-section .report-section-heading h2{color:#334e68}.report-privacy-message{background:#f8fafc}.report-footer{display:flex;justify-content:space-between;gap:2rem;padding:1.3rem 2.25rem;background:#102a43;color:#e8eef4;font-size:.78rem}.report-footer-meta{text-align:right}.report-footer strong{color:#fff}
@media(max-width:760px){.report-toolbar{position:static;align-items:flex-start;flex-direction:column}.report-hero{grid-template-columns:1fr;padding:1.4rem}.report-photo-frame{width:135px;height:165px}.report-identity h1{font-size:2rem}.report-section{padding:1.3rem 1.4rem}.report-fact-grid,.report-record-facts{grid-template-columns:1fr}.report-family-grid{grid-template-columns:1fr}.report-timeline:before{left:83px}.report-timeline-item{grid-template-columns:70px 1fr;gap:1.5rem}.report-timeline-item:before{left:77px}.report-footer{flex-direction:column}.report-footer-meta{text-align:left}}
@media print{@page{size:Letter portrait;margin:.55in}.sidebar,.nav-menu,.top-row,.report-toolbar,.person-profile-report-action{display:none!important}html,body{background:#fff!important}.content,.main,.page{margin:0!important;padding:0!important;max-width:none!important}.detailed-person-report{max-width:none;border:0;border-radius:0;box-shadow:none;overflow:visible}.report-hero{padding:0 0 .22in;background:#fff!important;grid-template-columns:1.35in 1fr;gap:.24in}.report-photo-frame{width:1.35in;height:1.65in;border:1px solid #aab7c4;border-radius:5px;box-shadow:none}.report-identity h1{font-size:24pt}.report-badge{border-color:#9aa9b7;padding:3px 7px}.report-section{padding:.18in 0;border-bottom:1px solid #b8c4cf}.report-section-heading{margin-bottom:.1in}.report-record{break-inside:avoid}.report-timeline-item{break-inside:avoid}.report-medical-section{background:#fff!important}.report-footer{background:#fff!important;color:#475569!important;border-top:1px solid #9aa9b7;padding:.15in 0 0}.report-footer strong{color:#172033!important}a{color:#172033!important;text-decoration:none!important}}
'@
Set-Content $cssFile.FullName $css -Encoding utf8 -NoNewline
}

Write-Host '1.0.15 Professional Detailed Person Report applied.'
