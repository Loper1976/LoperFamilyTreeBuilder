param([Parameter(Mandatory=$true)][string]$Root)

$ErrorActionPreference = 'Stop'

function Replace-RequiredText {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$OldText,
        [Parameter(Mandatory=$true)][string]$NewText
    )
    $text = Get-Content $Path -Raw
    if (-not $text.Contains($OldText)) {
        throw "Required text was not found in ${Path}: $OldText"
    }
    Set-Content -Path $Path -Value ($text.Replace($OldText, $NewText)) -Encoding utf8
}

$parts = @(
    Join-Path $env:GITHUB_WORKSPACE 'bootstrap/1.1.2/part00.b64'
    Join-Path $env:GITHUB_WORKSPACE 'bootstrap/1.1.2/part00b.b64'
    Join-Path $env:GITHUB_WORKSPACE 'bootstrap/1.1.2/part01.b64'
    Join-Path $env:GITHUB_WORKSPACE 'bootstrap/1.1.2/part02.b64'
    Join-Path $env:GITHUB_WORKSPACE 'bootstrap/1.1.2/part03.b64'
)
foreach ($part in $parts) {
    if (-not (Test-Path $part)) { throw "Missing 1.1.2 package part: $part" }
}

$encoded = ($parts | ForEach-Object { (Get-Content $_ -Raw).Trim() }) -join ''
if ($encoded.Length -ne 36724) {
    throw "1.1.2 base64 length mismatch: $($encoded.Length)"
}

$zip = Join-Path $env:RUNNER_TEMP 'LoperFamilyTreeBuilder-1.1.2-delta.zip'
[IO.File]::WriteAllBytes($zip, [Convert]::FromBase64String($encoded))
$hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
if ($hash -ne '61556429e55a89a2ba378fe86cd374f09cf1d08c017128c33d16ff18702e293a') {
    throw "1.1.2 package hash mismatch: $hash"
}

Expand-Archive $zip -DestinationPath $Root -Force

# Preserve the cumulative installer identity while advancing the release version.
Replace-RequiredText (Join-Path $Root 'Directory.Build.props') '<Version>1.1.1</Version>' '<Version>1.1.2</Version>'
Replace-RequiredText (Join-Path $Root 'installer/LoperFamilyTreeBuilder.Msi/Package.wxs') 'Version="1.1.1"' 'Version="1.1.2"'
Replace-RequiredText (Join-Path $Root 'installer/LoperFamilyTreeBuilder.Setup/Bundle.wxs') 'Version="1.1.1.0"' 'Version="1.1.2.0"'
Replace-RequiredText (Join-Path $Root 'src/LoperFamilyTreeBuilder.Launcher/UpgradeBackupService.cs') 'TargetVersion = "1.1.1"' 'TargetVersion = "1.1.2"'

$programPath = Join-Path $Root 'src/LoperFamilyTreeBuilder.Web/Program.cs'
$program = Get-Content $programPath -Raw

if ($program -notmatch 'builder\.Services\.AddAuthorization\(\);') {
    throw 'Expected AddAuthorization marker was not found.'
}

$authorization = @'
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.AddPolicy("ArchiveAdmin", policy => policy.RequireRole("ProjectOwner", "Administrator"));
    options.AddPolicy("GenealogyEdit", policy => policy.RequireRole("ProjectOwner", "Administrator", "GenealogyEditor"));
    options.AddPolicy("ResearchAccess", policy => policy.RequireRole("ProjectOwner", "Administrator", "GenealogyEditor", "Reviewer", "Researcher"));
    options.AddPolicy("MedicalAccess", policy => policy.RequireRole("ProjectOwner", "Administrator"));
    options.AddPolicy("SubmissionReview", policy => policy.RequireRole("ProjectOwner", "Administrator", "Reviewer"));
});
'@
$program = $program.Replace('builder.Services.AddAuthorization();', $authorization.TrimEnd())
$program = $program.Replace('limiter.PermitLimit = 20;', 'limiter.PermitLimit = 10;')
$program = $program.Replace('version = "1.1.0"', 'version = "1.1.2"')
$program = $program.Replace('version = "1.1.1"', 'version = "1.1.2"')
$program = $program.Replace('version = "1.0.16"', 'version = "1.1.2"')

# Make the base health endpoint explicitly public under the new fallback policy.
$programLines = $program -split '\r?\n'
for ($i = 0; $i -lt $programLines.Count; $i++) {
    $trimmed = $programLines[$i].Trim()
    if ($trimmed.StartsWith('app.MapGet("/health",') -and $trimmed.EndsWith('));')) {
        $programLines[$i] = $programLines[$i].Substring(0, $programLines[$i].LastIndexOf(';')) + '.AllowAnonymous();'
    }
}
$program = $programLines -join "`r`n"

# Anonymous account bootstrap/login must remain reachable under the fallback policy.
$anonymousChain = ').DisableAntiforgery().RequireRateLimiting("auth");'
if (([regex]::Matches($program, [regex]::Escape($anonymousChain))).Count -lt 2) {
    throw 'Expected anonymous account endpoint chains were not found.'
}
$program = $program.Replace(
    $anonymousChain,
    ').AllowAnonymous().DisableAntiforgery().RequireRateLimiting("auth");')

# Redirect a normal login to the forced password-change page when needed.
$loginMarker = 'app.MapPost("/account/login"'
$loginStart = $program.IndexOf($loginMarker, [StringComparison]::Ordinal)
if ($loginStart -lt 0) { throw 'Login endpoint not found.' }
$loginReturn = $program.IndexOf('return Results.Redirect("/");', $loginStart, [StringComparison]::Ordinal)
if ($loginReturn -lt 0) { throw 'Login success redirect not found.' }
$program = $program.Remove($loginReturn, 'return Results.Redirect("/");'.Length)
$program = $program.Insert($loginReturn, 'return Results.Redirect(user.MustChangePassword ? "/change-password" : "/");')

# Add invitation acceptance and password-change endpoints immediately before logout.
$logoutMarker = 'app.MapPost("/account/logout"'
$logoutIndex = $program.IndexOf($logoutMarker, [StringComparison]::Ordinal)
if ($logoutIndex -lt 0) { throw 'Logout endpoint marker not found.' }
if ($program -notmatch '/account/accept-invitation') {
$accountEndpoints = @'
app.MapPost("/account/accept-invitation", async (HttpContext http, UserAdministrationService users, CancellationToken ct) =>
{
    var form = await http.Request.ReadFormAsync(ct);
    try
    {
        var user = await users.AcceptInvitationAsync(
            form["token"].ToString(),
            form["email"].ToString(),
            form["displayName"].ToString(),
            form["password"].ToString(),
            ct);
        await SignInAsync(http, user);
        return Results.Redirect("/");
    }
    catch (Exception ex)
    {
        return Results.Content($"<h1>Invitation acceptance failed</h1><p>{System.Net.WebUtility.HtmlEncode(ex.Message)}</p><p><a href='/accept-invitation'>Return</a></p>", "text/html");
    }
}).AllowAnonymous().DisableAntiforgery().RequireRateLimiting("auth");

app.MapPost("/account/change-password", async (HttpContext http, UserAdministrationService users, CancellationToken ct) =>
{
    var idText = http.User.FindFirst("family_user_id")?.Value;
    if (!Guid.TryParse(idText, out var userId))
        return Results.Redirect("/login");

    var form = await http.Request.ReadFormAsync(ct);
    var newPassword = form["newPassword"].ToString();
    var confirmPassword = form["confirmPassword"].ToString();
    if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
        return Results.Content("<h1>Password change failed</h1><p>The new passwords do not match.</p><p><a href='/change-password'>Return</a></p>", "text/html");

    try
    {
        var user = await users.ChangePasswordAsync(userId, form["currentPassword"].ToString(), newPassword, ct);
        await SignInAsync(http, user);
        return Results.Redirect("/");
    }
    catch (Exception ex)
    {
        return Results.Content($"<h1>Password change failed</h1><p>{System.Net.WebUtility.HtmlEncode(ex.Message)}</p><p><a href='/change-password'>Return</a></p>", "text/html");
    }
}).RequireAuthorization().DisableAntiforgery().RequireRateLimiting("auth");

'@
    $program = $program.Insert($logoutIndex, $accountEndpoints)
}

# Add the must-change-password claim used by the password-reset flow.
$emailClaim = 'new(ClaimTypes.Email, user.Email)'
if ($program.Contains($emailClaim) -and $program -notmatch 'must_change_password') {
$claimReplacement = @'
new(ClaimTypes.Email, user.Email),
        new("must_change_password", user.MustChangePassword ? "true" : "false")
'@
    $program = $program.Replace($emailClaim, $claimReplacement.TrimEnd())
}

# Enforce forced password changes for normal page requests while leaving framework/assets and account endpoints usable.
if ($program -notmatch 'MustChangePasswordGate') {
    $authenticationMarker = 'app.UseAuthentication();'
    $authIndex = $program.IndexOf($authenticationMarker, [StringComparison]::Ordinal)
    if ($authIndex -lt 0) { throw 'UseAuthentication marker missing.' }
    $authInsertAt = $authIndex + $authenticationMarker.Length
$gate = @'

// MustChangePasswordGate: administrator-issued temporary passwords cannot be used for normal archive work.
app.Use(async (context, next) =>
{
    var mustChange = string.Equals(context.User.FindFirst("must_change_password")?.Value, "true", StringComparison.OrdinalIgnoreCase);
    var path = context.Request.Path;
    var isExempt = path.StartsWithSegments("/change-password")
        || path.StartsWithSegments("/account")
        || path.StartsWithSegments("/health")
        || path.StartsWithSegments("/_")
        || (path.HasValue && System.IO.Path.HasExtension(path.Value));

    if (context.User.Identity?.IsAuthenticated == true && mustChange && !isExempt)
    {
        context.Response.Redirect("/change-password");
        return;
    }

    await next();
});
'@
    $program = $program.Insert($authInsertAt, $gate)
}

Set-Content -Path $programPath -Value $program -Encoding utf8

# Add the new privacy/security services without replacing the 1.1.0 storage-abstraction registrations.
$servicesPath = Join-Path $Root 'src/LoperFamilyTreeBuilder.Data/ServiceCollectionExtensions.cs'
$services = Get-Content $servicesPath -Raw
if ($services -notmatch 'PersonPrivacyService') {
    $services = $services.Replace(
        'services.AddScoped<UserAdministrationService>();',
        "services.AddScoped<UserAdministrationService>();`r`n        services.AddScoped<PersonPrivacyService>();`r`n        services.AddScoped<SecurityDashboardService>();")
}
Set-Content -Path $servicesPath -Value $services -Encoding utf8

# Expose the privacy entity through the context while retaining every cumulative DbSet.
$dbPath = Join-Path $Root 'src/LoperFamilyTreeBuilder.Data/FamilyTreeDbContext.cs'
$db = Get-Content $dbPath -Raw
if ($db -notmatch 'PersonPrivacySettings') {
    $db = $db.Replace(
        'public DbSet<FamilySubmission> FamilySubmissions => Set<FamilySubmission>();',
        "public DbSet<FamilySubmission> FamilySubmissions => Set<FamilySubmission>();`r`n`r`n    public DbSet<PersonPrivacySetting> PersonPrivacySettings => Set<PersonPrivacySetting>();")
}
Set-Content -Path $dbPath -Value $db -Encoding utf8

# Add security/privacy links without removing 1.1.0 Web Experience navigation.
$navPath = Join-Path $Root 'src/LoperFamilyTreeBuilder.Web/Components/Layout/NavMenu.razor'
$nav = Get-Content $navPath -Raw
if ($nav -notmatch 'href="/privacy-access"') {
$navReplacement = @'
<NavLink href="/users">Users and Permissions</NavLink>
        <NavLink href="/privacy-access">Privacy &amp; Access</NavLink>
        <NavLink href="/security-center">Security Center</NavLink>
'@
    $nav = $nav.Replace(
        '<NavLink href="/users">Users and Permissions</NavLink>',
        $navReplacement.TrimEnd())
}
Set-Content -Path $navPath -Value $nav -Encoding utf8

# Medical pages are now explicitly governed by the MedicalAccess policy.
$medicalPages = @(
    'MedicalDashboard.razor',
    'MedicalSearch.razor',
    'FamilyHealthPatterns.razor',
    'MedicalPedigree.razor',
    'PersonMedical.razor'
)
$pagesRoot = Join-Path $Root 'src/LoperFamilyTreeBuilder.Web/Components/Pages'
foreach ($name in $medicalPages) {
    $path = Join-Path $pagesRoot $name
    if (-not (Test-Path $path)) { throw "Medical page missing: $name" }
    $text = Get-Content $path -Raw
    if ($text -notmatch 'Authorize\(Policy = "MedicalAccess"\)') {
        $medicalHeader = "@rendermode InteractiveServer`r`n" + '@attribute [Authorize(Policy = "MedicalAccess")]'
        $text = $text.Replace('@rendermode InteractiveServer', $medicalHeader)
        Set-Content -Path $path -Value $text -Encoding utf8
    }
}

# Verify all literal Razor routes remain unique after the security update.
$routeOwners = @{}
Get-ChildItem $pagesRoot -Filter '*.razor' -File -Recurse | ForEach-Object {
    $file = $_
    $text = Get-Content $file.FullName -Raw
    foreach ($match in [regex]::Matches($text, '(?m)^@page\s+"([^"]+)"')) {
        $route = $match.Groups[1].Value
        if (-not $routeOwners.ContainsKey($route)) { $routeOwners[$route] = @() }
        $routeOwners[$route] += $file.Name
    }
}
$duplicates = @($routeOwners.GetEnumerator() | Where-Object { @($_.Value).Count -gt 1 } | Sort-Object Name)
if ($duplicates.Count -gt 0) {
    $details = ($duplicates | ForEach-Object { "$($_.Name) => $([string]::Join(', ', @($_.Value)))" }) -join '; '
    throw "Duplicate page routes remain after 1.1.2: $details"
}

Write-Host '1.1.2 Family Accounts & Permissions applied: fallback authentication, lockout, invitation acceptance, forced password change, roles, branch permissions, privacy settings, security center, and medical policy enforcement.'
