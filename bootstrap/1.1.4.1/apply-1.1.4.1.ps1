param(
    [Parameter(Mandatory=$true)]
    [string]$Root
)

$ErrorActionPreference = 'Stop'

function Read-Text([string]$Path) {
    if (-not (Test-Path $Path)) { throw "Required file not found: $Path" }
    return Get-Content $Path -Raw
}

function Write-Text([string]$Path, [string]$Content) {
    Set-Content -Path $Path -Value $Content -Encoding utf8
}

$cssPath = Join-Path $Root 'src/LoperFamilyTreeBuilder.Web/wwwroot/app.css'
$propsPath = Join-Path $Root 'Directory.Build.props'
$programPath = Join-Path $Root 'src/LoperFamilyTreeBuilder.Web/Program.cs'
$launcherPath = Join-Path $Root 'src/LoperFamilyTreeBuilder.Launcher/UpgradeBackupService.cs'
$bundlePath = Join-Path $Root 'installer/LoperFamilyTreeBuilder.Setup/Bundle.wxs'

# Repair the 1.1.4 stylesheet serialization defect. The original release appended the
# modern style block with literal backslash-n sequences, causing the browser to ignore it.
$css = Read-Text $cssPath
$escapedMarker = '\n\n/* 1.1.4 - modern UI, navigation clarity, reliability states */'
$markerIndex = $css.IndexOf($escapedMarker, [StringComparison]::Ordinal)
if ($markerIndex -ge 0) {
    $prefix = $css.Substring(0, $markerIndex)
    $tail = $css.Substring($markerIndex).Replace('\n', [Environment]::NewLine)
    $css = $prefix + $tail
}

$hotfixMarker = '/* 1.1.4.1 - compact brand and independent workspace scrolling */'
if ($css -notmatch [regex]::Escape($hotfixMarker)) {
    $hotfixCss = @'

/* 1.1.4.1 - compact brand and independent workspace scrolling */
.app-shell {
    height: 100vh;
    min-height: 100vh;
    overflow: hidden;
    grid-template-columns: 252px minmax(0, 1fr);
}

.sidebar {
    display: flex;
    flex-direction: column;
    height: 100vh;
    min-height: 0;
    overflow: hidden;
}

.brand {
    flex: 0 0 auto;
    min-height: 64px;
    padding: 10px 12px;
    gap: 10px;
}

.brand-logo {
    display: block;
    width: 44px;
    height: 44px;
    max-width: 44px;
    max-height: 44px;
    flex: 0 0 44px;
    object-fit: contain;
    object-position: center;
    border-radius: 9px;
    background: #0d1723;
}

.brand-logo.compact {
    width: 38px;
    height: 38px;
    max-width: 38px;
    max-height: 38px;
    flex-basis: 38px;
}

.brand-copy {
    overflow: hidden;
}

.brand-title,
.brand-subtitle {
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.nav-groups {
    flex: 1 1 auto;
    min-height: 0;
    overflow-y: auto;
    overflow-x: hidden;
    overscroll-behavior: contain;
    scrollbar-gutter: stable;
}

.main-workspace {
    height: 100vh;
    min-height: 0;
    overflow-y: auto;
    overflow-x: hidden;
}

.web-topbar {
    z-index: 300;
}

.mobile-nav-panel {
    z-index: 1000;
}

@media (max-width: 1100px) {
    .app-shell {
        grid-template-columns: 224px minmax(0, 1fr);
    }

    .brand-logo {
        width: 40px;
        height: 40px;
        max-width: 40px;
        max-height: 40px;
        flex-basis: 40px;
    }
}

@media (max-width: 820px) {
    .app-shell,
    .web-app-shell {
        height: auto;
        min-height: 100vh;
        overflow: visible;
    }

    .main-workspace {
        height: auto;
        min-height: 100vh;
        overflow: visible;
    }
}
'@
    $css += $hotfixCss
}
Write-Text $cssPath $css

# Version the hotfix consistently while preserving the stable assembly major/minor identity.
$props = Read-Text $propsPath
$props = [regex]::Replace($props, '<Version>[^<]+</Version>', '<Version>1.1.4.1</Version>', 1)
$props = [regex]::Replace($props, '<FileVersion>[^<]+</FileVersion>', '<FileVersion>1.1.4.1</FileVersion>', 1)
$props = [regex]::Replace($props, '<InformationalVersion>[^<]+</InformationalVersion>', '<InformationalVersion>1.1.4.1-ui-reliability-hotfix</InformationalVersion>', 1)
Write-Text $propsPath $props

$program = Read-Text $programPath
$program = $program.Replace('version = "1.1.4"', 'version = "1.1.4.1"')
$program = $program.Replace('release = "modern-ui-reliability"', 'release = "ui-reliability-hotfix"')
Write-Text $programPath $program

$launcher = Read-Text $launcherPath
$launcher = $launcher.Replace('TargetVersion = "1.1.4"', 'TargetVersion = "1.1.4.1"')
Write-Text $launcherPath $launcher

$bundle = Read-Text $bundlePath
$bundle = $bundle.Replace('Version="1.1.4.0"', 'Version="1.1.4.1"')
Write-Text $bundlePath $bundle

Write-Host '1.1.4.1 UI Reliability Hotfix applied: stylesheet serialization repaired, logo constrained, sidebar/workspace scrolling separated, and version markers updated.'
