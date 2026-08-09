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

    $text = $text.Replace($OldText, $NewText)
    Set-Content -Path $Path -Value $text -Encoding utf8
}

$pagesRoot = Join-Path $Root 'src/LoperFamilyTreeBuilder.Web/Components/Pages'
$placeholderPath = Join-Path $pagesRoot 'Placeholder.razor'
if (-not (Test-Path $placeholderPath)) {
    throw "Placeholder page was not found: $placeholderPath"
}

# These routes already have real pages. Leaving them on Placeholder.razor creates
# ambiguous endpoint matches and produces HTTP 500 when the route is requested.
$routesToRemove = @(
    '/family-tree',
    '/pedigree',
    '/descendants',
    '/reports',
    '/cemeteries',
    '/military',
    '/maps',
    '/users'
)

$routeDirectives = $routesToRemove | ForEach-Object { '@page "' + $_ + '"' }
$placeholderLines = Get-Content $placeholderPath
$filteredPlaceholder = $placeholderLines | Where-Object {
    $routeDirectives -notcontains $_.Trim()
}
Set-Content -Path $placeholderPath -Value $filteredPlaceholder -Encoding utf8

# Version the fix as 1.1.1 so an installed 1.1.0 can upgrade cleanly.
Replace-RequiredText `
    (Join-Path $Root 'Directory.Build.props') `
    '<Version>1.1.0</Version>' `
    '<Version>1.1.1</Version>'

Replace-RequiredText `
    (Join-Path $Root 'installer/LoperFamilyTreeBuilder.Msi/Package.wxs') `
    'Version="1.1.0"' `
    'Version="1.1.1"'

Replace-RequiredText `
    (Join-Path $Root 'installer/LoperFamilyTreeBuilder.Setup/Bundle.wxs') `
    'Version="1.1.0.0"' `
    'Version="1.1.1.0"'

Replace-RequiredText `
    (Join-Path $Root 'src/LoperFamilyTreeBuilder.Launcher/UpgradeBackupService.cs') `
    'TargetVersion = "1.1.0"' `
    'TargetVersion = "1.1.1"'

# Fail the build if any literal Blazor/Razor route is owned by more than one page.
$routeOwners = @{}
Get-ChildItem $pagesRoot -Filter '*.razor' -File -Recurse | ForEach-Object {
    $file = $_
    $text = Get-Content $file.FullName -Raw
    foreach ($match in [regex]::Matches($text, '(?m)^@page\s+"([^"]+)"')) {
        $route = $match.Groups[1].Value
        if (-not $routeOwners.ContainsKey($route)) {
            $routeOwners[$route] = @()
        }
        $routeOwners[$route] += $file.Name
    }
}

$duplicates = @($routeOwners.GetEnumerator() |
    Where-Object { @($_.Value).Count -gt 1 } |
    Sort-Object Name)

if ($duplicates.Count -gt 0) {
    $details = ($duplicates | ForEach-Object {
        "$($_.Name) => $([string]::Join(', ', @($_.Value)))"
    }) -join '; '
    throw "Duplicate page routes remain: $details"
}

$expectedOwners = [ordered]@{
    '/family-tree' = 'FamilyTree.razor'
    '/pedigree' = 'PedigreeCharts.razor'
    '/descendants' = 'DescendantCharts.razor'
    '/reports' = 'Reports.razor'
    '/cemeteries' = 'Cemeteries.razor'
    '/military' = 'Military.razor'
    '/maps' = 'Maps.razor'
    '/users' = 'Users.razor'
}

foreach ($entry in $expectedOwners.GetEnumerator()) {
    if (-not $routeOwners.ContainsKey($entry.Key)) {
        throw "Required route is missing after hotfix: $($entry.Key)"
    }

    $owners = @($routeOwners[$entry.Key])
    if ($owners.Count -ne 1 -or $owners[0] -ne $entry.Value) {
        throw "Route owner verification failed for $($entry.Key). Expected $($entry.Value), found $([string]::Join(', ', $owners))."
    }
}

Write-Host '1.1.1 route reliability hotfix applied. All Razor page routes are unique.'
