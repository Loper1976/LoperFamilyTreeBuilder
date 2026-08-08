param([Parameter(Mandatory=$true)][string]$Root)

$ErrorActionPreference = 'Stop'

function Replace-Exact([string]$RelativePath, [string]$Old, [string]$New) {
    $path = Join-Path $Root $RelativePath
    if (-not (Test-Path $path)) { throw "Required file not found: $path" }
    $text = Get-Content $path -Raw
    if (-not $text.Contains($Old)) { throw "Expected text not found in $RelativePath`n$Old" }
    $text = $text.Replace($Old, $New)
    Set-Content -Path $path -Value $text -Encoding utf8 -NoNewline
}

# Version and installer identity. UpgradeCodes deliberately remain unchanged.
Replace-Exact 'Directory.Build.props' '<Version>1.0.12</Version>' '<Version>1.0.13</Version>'
Replace-Exact 'Directory.Build.props' '<AssemblyVersion>1.0.12.0</AssemblyVersion>' '<AssemblyVersion>1.0.13.0</AssemblyVersion>'
Replace-Exact 'Directory.Build.props' '<FileVersion>1.0.12.0</FileVersion>' '<FileVersion>1.0.13.0</FileVersion>'
Replace-Exact 'Directory.Build.props' '<InformationalVersion>1.0.12-release-candidate</InformationalVersion>' '<InformationalVersion>1.0.13-production-baseline</InformationalVersion>'
Replace-Exact 'installer/LoperFamilyTreeBuilder.Msi/Package.wxs' 'Version="1.0.12"' 'Version="1.0.13"'
Replace-Exact 'installer/LoperFamilyTreeBuilder.Setup/Bundle.wxs' 'Version="1.0.12.0"' 'Version="1.0.13.0"'
Replace-Exact 'src/LoperFamilyTreeBuilder.Launcher/UpgradeBackupService.cs' 'private const string TargetVersion = "1.0.12";' 'private const string TargetVersion = "1.0.13";'

# Update all health/version payloads from the release candidate to the production baseline.
Replace-Exact 'src/LoperFamilyTreeBuilder.Web/Program.cs' 'version = "1.0.12"' 'version = "1.0.13"'

# Add a production release health endpoint. It reports readiness only; it never changes archive data.
$programPath = Join-Path $Root 'src/LoperFamilyTreeBuilder.Web/Program.cs'
$program = Get-Content $programPath -Raw
$anchor = 'app.MapPost("/account/bootstrap"'
if (-not $program.Contains($anchor)) { throw 'Program.cs production-health insertion anchor was not found.' }
$releaseEndpoint = @'
app.MapGet("/health/release", async (SystemDiagnosticsService diagnostics, HostingReadinessService hosting, CancellationToken cancellationToken) =>
{
    var system = await diagnostics.GetReportAsync(cancellationToken);
    var hosted = await hosting.GetReportAsync(cancellationToken);
    var localReady = system.Checks.Where(x => x.Required).All(x => x.Passed);
    var payload = new
    {
        application = "Loper Family Tree Builder",
        version = "1.0.13",
        release = "production-baseline",
        localReady,
        hostedMode = hosted.HostedMode,
        hostedReady = hosted.IsReady,
        installerSigning = "verify-external-authenticode-signature",
        utc = DateTimeOffset.UtcNow
    };

    if (localReady)
        return Results.Ok(payload);

    return Results.Json(payload, statusCode: StatusCodes.Status503ServiceUnavailable);
}).AllowAnonymous();

'@
$program = $program.Replace($anchor, $releaseEndpoint + $anchor)
Set-Content -Path $programPath -Value $program -Encoding utf8 -NoNewline

# Add production release visibility to the permanent navigation and dashboard.
Replace-Exact 'src/LoperFamilyTreeBuilder.Web/Components/Layout/NavMenu.razor' '        <NavLink href="/about">About / Version</NavLink>' "        <NavLink href=\"/production-release\">Production Release</NavLink>`r`n        <NavLink href=\"/about\">About / Version</NavLink>"
Replace-Exact 'src/LoperFamilyTreeBuilder.Web/Components/Pages/Home.razor' '            <a href="/diagnostics">Run System Diagnostics</a>' "            <a href=\"/diagnostics\">Run System Diagnostics</a>`r`n            <a href=\"/production-release\">Production Release Readiness</a>"

# Copy complete 1.0.13 overrides into the reconstructed tree.
$overrideRoot = Join-Path $env:GITHUB_WORKSPACE 'bootstrap/1.0.13/overrides'
if (-not (Test-Path $overrideRoot)) { throw "1.0.13 override root not found: $overrideRoot" }
Get-ChildItem $overrideRoot -File -Recurse | ForEach-Object {
    $relative = [IO.Path]::GetRelativePath($overrideRoot, $_.FullName)
    $destination = Join-Path $Root $relative
    New-Item -ItemType Directory -Force (Split-Path $destination -Parent) | Out-Null
    Copy-Item $_.FullName $destination -Force
}

Write-Host '1.0.13 Production Release Baseline changes applied.'
