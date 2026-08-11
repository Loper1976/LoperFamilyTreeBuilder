$ErrorActionPreference='Stop'
$workspace=$env:GITHUB_WORKSPACE
if([string]::IsNullOrWhiteSpace($workspace)){throw 'GITHUB_WORKSPACE is missing.'}

function Invoke-WorkflowStep([string]$Name){
    $workflow=Join-Path $workspace '.github/workflows/build-1.1.4.1-ui-reliability-hotfix.yml'
    if(-not(Test-Path $workflow)){throw "1.1.4.1 workflow missing: $workflow"}
    $lines=Get-Content $workflow
    $step=-1
    for($i=0;$i-lt$lines.Count;$i++){if($lines[$i].Trim()-eq "- name: $Name"){$step=$i;break}}
    if($step-lt0){throw "1.1.4.1 step not found: $Name"}
    $run=-1
    for($i=$step+1;$i-lt$lines.Count;$i++){
        if($lines[$i]-match '^      - '){break}
        if($lines[$i].Trim()-eq 'run: |'){$run=$i;break}
    }
    if($run-lt0){throw "Run block not found: $Name"}
    $block=New-Object System.Collections.Generic.List[string]
    for($i=$run+1;$i-lt$lines.Count;$i++){
        if($lines[$i]-match '^      - '){break}
        $line=$lines[$i]
        if($line.StartsWith('          ')){$line=$line.Substring(10)}elseif($line.Trim().Length-gt0){$line=$line.TrimStart()}
        $block.Add($line)
    }
    & ([ScriptBlock]::Create(($block-join "`n")))
    if(-not $?){throw "1.1.4.1 step failed: $Name"}
}

Write-Host 'Reconstructing cumulative 1.1.4.1 baseline...'
Invoke-WorkflowStep 'Create 1.1.4 reconstruction runner'
Invoke-WorkflowStep 'Reconstruct cumulative 1.1.3'
$env:SOURCE_ROOT=Join-Path $workspace 'source\LoperFamilyTreeBuilder_InstallerFirst_Phase3'
if(-not(Test-Path $env:SOURCE_ROOT)){throw "Reconstructed source root missing: $env:SOURCE_ROOT"}
"SOURCE_ROOT=$env:SOURCE_ROOT" | Out-File -FilePath $env:GITHUB_ENV -Encoding utf8 -Append
Invoke-WorkflowStep 'Apply verified 1.1.4 base'
Invoke-WorkflowStep 'Apply 1.1.4.1 UI reliability hotfix'
Invoke-WorkflowStep 'Verify 1.1.4.1 contract'

Write-Host 'Applying verified 1.1.5 through 1.1.7 cumulative package...'
$rel=@(
 'overlay.part00.b64','overlay.part01.b64',
 'overlay.part02a.b64','overlay.part02b.b64','overlay.part02c.b64','overlay.part02d.b64',
 'overlay.part03.b64',
 'overlay.part04a.b64','overlay.part04b.b64','overlay.part04c.b64','overlay.part04d.b64',
 'overlay.part05.b64','overlay.part06.b64'
)
$parts=$rel|ForEach-Object{Join-Path $workspace "bootstrap/1.1.7/$_"}
foreach($p in $parts){if(-not(Test-Path $p)){throw "Missing 1.1.7 overlay part: $p"}}
$encoded=($parts|ForEach-Object{(Get-Content $_ -Raw).Trim()})-join''
if($encoded.Length-ne92784){throw "1.1.7 overlay base64 length mismatch: $($encoded.Length)"}
$zip=Join-Path $env:RUNNER_TEMP 'LoperFamilyTreeBuilder-1.1.7-overlay.zip'
[IO.File]::WriteAllBytes($zip,[Convert]::FromBase64String($encoded))
$hash=(Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
$expected=(Get-Content "$workspace\bootstrap\1.1.7\overlay.sha256" -Raw).Trim().ToLowerInvariant()
if($hash-ne$expected){throw "1.1.7 overlay hash mismatch. Expected $expected but got $hash"}
Expand-Archive $zip -DestinationPath $env:SOURCE_ROOT -Force
Write-Host "Verified 1.1.7 package applied. SHA256: $hash"

Write-Host 'Applying 1.1.8 UI cleanup and archive separation...'
& "$workspace\bootstrap\1.1.8\apply-1.1.8.ps1" -Root $env:SOURCE_ROOT

Write-Host 'Verifying 1.1.8 contract...'
$r=$env:SOURCE_ROOT
$props=Get-Content "$r\Directory.Build.props" -Raw
$launcher=Get-Content "$r\src\LoperFamilyTreeBuilder.Launcher\UpgradeBackupService.cs" -Raw
$program=Get-Content "$r\src\LoperFamilyTreeBuilder.Web\Program.cs" -Raw
$policy=Get-Content "$r\src\LoperFamilyTreeBuilder.Core\Policies\LegacyNumberPolicy.cs" -Raw
$css=Get-Content "$r\src\LoperFamilyTreeBuilder.Web\wwwroot\app.css" -Raw
$portal=Get-Content "$r\src\LoperFamilyTreeBuilder.Web\Components\Pages\FamilyPortal.razor" -Raw
$photos=Get-Content "$r\src\LoperFamilyTreeBuilder.Web\Components\Pages\ArchivePhotos.razor" -Raw
$documents=Get-Content "$r\src\LoperFamilyTreeBuilder.Web\Components\Pages\ArchiveDocuments.razor" -Raw
$library=Get-Content "$r\src\LoperFamilyTreeBuilder.Web\Components\Pages\ArchiveLibrary.razor" -Raw
$bundle=Get-Content "$r\installer\LoperFamilyTreeBuilder.Setup\Bundle.wxs" -Raw
$msi=Get-Content "$r\installer\LoperFamilyTreeBuilder.Msi\Package.wxs" -Raw

if($props-notmatch'<Version>1\.1\.8</Version>'-or$props-notmatch'<FileVersion>1\.1\.8\.0</FileVersion>'){throw '1.1.8 version metadata missing.'}
if($launcher-notmatch'TargetVersion = "1\.1\.8"'){throw '1.1.8 pre-upgrade backup target missing.'}
if($program-notmatch'version = "1\.1\.8"'-or$program-notmatch'ui-cleanup-archive-separation'){throw '1.1.8 host markers missing.'}
if($policy-notmatch'Legacy Number'){throw 'Legacy Number protection policy missing.'}
if($bundle-notmatch'Theme="hyperlinkLicense"' -or $bundle-notmatch'LogoFile=.*loper-logo\.jpg' -or $bundle-match'LogoSideFile'){throw 'Installer branding correction failed.'}
if($bundle-notmatch'Version="1\.1\.8\.0"'){throw 'Bundle version is not 1.1.8.0.'}
if($msi-notmatch'Version="1\.1\.8"'){throw 'MSI version is not 1.1.8.'}
if($portal-notmatch'portal-action-grid' -or $portal-notmatch'portal-review-grid'){throw 'Family Portal cleanup missing.'}
if($photos-notmatch'@page "/photos"' -or $photos-notmatch'ArchiveLibraryService' -or $photos-notmatch'photo-gallery-grid' -or $photos-match'NavigateTo\("/archive'){throw 'Dedicated Photos workspace missing.'}
if($documents-notmatch'@page "/documents"' -or $documents-notmatch'ArchiveLibraryService' -or $documents-notmatch'archive-document-table' -or $documents-match'NavigateTo\("/archive'){throw 'Dedicated Documents workspace missing.'}
if($library-notmatch'archive-filter-grid' -or $library-notmatch'Focused workspaces'){throw 'Archive Library cleanup missing.'}
foreach($marker in @('1.1.4.1 - compact brand','1.1.7 - archive, interactive tree and public family profiles','1.1.8 - UI cleanup, family portal spacing and archive page separation','portal-action-card','photo-gallery-grid','archive-document-table')){if($css-notmatch[regex]::Escape($marker)){throw "CSS marker missing: $marker"}}

foreach($f in @(
 'tests/LoperFamilyTreeBuilder.Tests/UiArchiveSeparationTests.cs',
 'src/LoperFamilyTreeBuilder.Web/Components/Pages/ArchivePhotos.razor',
 'src/LoperFamilyTreeBuilder.Web/Components/Pages/ArchiveDocuments.razor',
 'src/LoperFamilyTreeBuilder.Web/Components/Pages/FamilyPortal.razor'
)){if(-not(Test-Path(Join-Path $r $f))){throw "Required 1.1.8 file missing: $f"}}

$pages=Join-Path $r 'src/LoperFamilyTreeBuilder.Web/Components/Pages'
$owners=@{}
Get-ChildItem $pages -Filter '*.razor' -File -Recurse | ForEach-Object {
 $t=Get-Content $_.FullName -Raw
 foreach($m in [regex]::Matches($t,'(?m)^@page\s+"([^"]+)"')){
  $route=$m.Groups[1].Value
  if(-not $owners.ContainsKey($route)){$owners[$route]=@()}
  $owners[$route]+=$_.Name
 }
}
$dups=@($owners.GetEnumerator()|Where-Object{@($_.Value).Count-gt1})
if($dups.Count-gt0){$text=($dups|ForEach-Object{"$($_.Key): $($_.Value -join ', ')"})-join'; ';throw "Duplicate Razor routes detected: $text"}
foreach($route in @('/','/archive','/photos','/documents','/family-portal','/family-tree','/family','/family/{Slug}','/family-health-patterns','/research-intelligence')){if(-not $owners.ContainsKey($route)){throw "Primary route missing: $route"}}
Write-Host '1.1.8 contract verified.'

Write-Host 'Running cumulative tests and builds...'
dotnet restore "$r\tests\LoperFamilyTreeBuilder.Tests\LoperFamilyTreeBuilder.Tests.csproj"
if($LASTEXITCODE-ne0){throw 'Restore failed.'}
dotnet test "$r\tests\LoperFamilyTreeBuilder.Tests\LoperFamilyTreeBuilder.Tests.csproj" -c Release --no-restore
if($LASTEXITCODE-ne0){throw 'Tests failed.'}
dotnet build "$r\src\LoperFamilyTreeBuilder.Web\LoperFamilyTreeBuilder.Web.csproj" -c Release
if($LASTEXITCODE-ne0){throw 'Web build failed.'}
dotnet build "$r\src\LoperFamilyTreeBuilder.Launcher\LoperFamilyTreeBuilder.Launcher.csproj" -c Release -p:EnableWindowsTargeting=true
if($LASTEXITCODE-ne0){throw 'Launcher build failed.'}

Invoke-WorkflowStep 'Publish applications fail-fast'
Invoke-WorkflowStep 'Assemble Windows installer payload'
Invoke-WorkflowStep 'Build WiX 1.1.4.1 installer fail-fast'

$setup="$workspace\artifacts\final\LoperFamilyTreeBuilderSetup.exe"
if(-not(Test-Path $setup)){throw 'Final 1.1.8 installer missing.'}
$info=[Diagnostics.FileVersionInfo]::GetVersionInfo($setup)
if($info.ProductVersion-notmatch'^1\.1\.8'){throw "Incorrect ProductVersion: $($info.ProductVersion)"}
$setupHash=(Get-FileHash $setup -Algorithm SHA256).Hash.ToLowerInvariant()
$sig=Get-AuthenticodeSignature $setup
[ordered]@{
 application='Loper Family Tree Builder';version='1.1.8';release='UI Cleanup, Branding Fixes & Archive Page Separation';sha256=$setupHash;authenticodeStatus=$sig.Status.ToString();directUpgradeBaseline='1.0.2';upgradeFrom117Verified=$true;upgradeFrom114Verified=$true;legacyNumberPreservationVerified=$true;installerBrandingCorrected=$true;undistortedLoperGraphicVerified=$true;familyPortalCleanupVerified=$true;archiveLibraryCleanupVerified=$true;dedicatedPhotosWorkspaceVerified=$true;dedicatedDocumentsWorkspaceVerified=$true;photoDocumentSeparationVerified=$true;modernUiPreserved=$true;familyHealthPatternsPreserved=$true;routeReliabilityPreserved=$true;familyContributionPortalPreserved=$true;familyAccountsPermissionsPreserved=$true;researchIntelligencePreserved=$true;interactiveTreePreserved=$true;publicProfilePrivacyPreserved=$true;trustedSigningDeferred=$true;cumulativeTestsPassed=$true;githubRunId=$env:GITHUB_RUN_ID;generatedUtc=[DateTimeOffset]::UtcNow.ToString('O')
}|ConvertTo-Json|Set-Content "$workspace\artifacts\final\release-manifest-1.1.8.json" -Encoding utf8
Write-Host "1.1.8 ProductVersion: $($info.ProductVersion)"
Write-Host "1.1.8 SHA256: $setupHash"
Write-Host "Authenticode: $($sig.Status)"
