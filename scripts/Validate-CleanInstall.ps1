[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $BundlePath,

    [string] $EvidenceRoot = (Join-Path $env:TEMP 'LoperFamilyTreeBuilder-CleanInstall')
)

$ErrorActionPreference = 'Stop'
$installFolder = Join-Path $env:ProgramFiles 'Loper Family Tree Builder'
$webExecutable = Join-Path $installFolder 'Web\LoperFamilyTreeBuilder.Web.exe'
$launcherExecutable = Join-Path $installFolder 'LoperFamilyTreeBuilder.Launcher.exe'
$validationData = Join-Path $EvidenceRoot 'data'
$installLog = Join-Path $EvidenceRoot 'bundle-install.log'
$uninstallLog = Join-Path $EvidenceRoot 'bundle-uninstall.log'
$port = 51873
$baseUri = "http://127.0.0.1:$port"

New-Item -ItemType Directory -Force $EvidenceRoot | Out-Null

if (-not (Test-Path -LiteralPath $BundlePath -PathType Leaf)) {
    throw "Setup bundle does not exist: $BundlePath"
}

if (Test-Path -LiteralPath $installFolder) {
    throw "Clean-install validation requires the application folder to be absent: $installFolder"
}

function Invoke-Bundle {
    param(
        [Parameter(Mandatory)]
        [string[]] $Arguments
    )

    $process = Start-Process -FilePath $BundlePath -ArgumentList $Arguments -Wait -PassThru
    if ($process.ExitCode -notin @(0, 3010)) {
        throw "Bundle returned exit code $($process.ExitCode)."
    }
}

function Wait-ForEndpoint {
    param(
        [Parameter(Mandatory)]
        [string] $Uri,

        [int] $Attempts = 60
    )

    for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
        try {
            return Invoke-WebRequest -Uri $Uri -UseBasicParsing -TimeoutSec 5
        }
        catch {
            if ($attempt -eq $Attempts) { throw }
            Start-Sleep -Seconds 2
        }
    }
}

$webProcess = $null
try {
    Invoke-Bundle @('/install', '/quiet', '/norestart', '/log', $installLog)

    if (-not (Test-Path -LiteralPath $launcherExecutable -PathType Leaf)) {
        throw "Installed launcher was not found: $launcherExecutable"
    }
    if (-not (Test-Path -LiteralPath $webExecutable -PathType Leaf)) {
        throw "Installed web host was not found: $webExecutable"
    }

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $webExecutable
    $startInfo.WorkingDirectory = Split-Path -Parent $webExecutable
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.Environment['LOPER_FAMILY_TREE_ROOT'] = $validationData
    $startInfo.Environment['ASPNETCORE_URLS'] = $baseUri
    $webProcess = [System.Diagnostics.Process]::Start($startInfo)

    $health = Wait-ForEndpoint "$baseUri/health"
    $healthStatus = $null
    try {
        $healthStatus = ($health.Content | ConvertFrom-Json).status
    }
    catch {
        $healthStatus = $health.Content.Trim()
    }
    if ($health.StatusCode -ne 200 -or $healthStatus -ne 'ok') {
        throw "Unexpected health response: HTTP $($health.StatusCode), '$($health.Content)'"
    }

    foreach ($route in @('/research-center', '/settings')) {
        $response = Wait-ForEndpoint "$baseUri$route"
        if ($response.StatusCode -ne 200) {
            throw "Route $route returned HTTP $($response.StatusCode)."
        }
    }

    [pscustomobject]@{
        BundleSha256 = (Get-FileHash -LiteralPath $BundlePath -Algorithm SHA256).Hash
        InstalledLauncher = $launcherExecutable
        Health = 'ok'
        ResearchCenterStatus = 200
        SettingsStatus = 200
        DataRoot = $validationData
    } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $EvidenceRoot 'clean-install-evidence.json')
}
finally {
    if ($webProcess -and -not $webProcess.HasExited) {
        $webProcess.Kill($true)
        $webProcess.WaitForExit()
    }
}

Invoke-Bundle @('/uninstall', '/quiet', '/norestart', '/log', $uninstallLog)

if (Test-Path -LiteralPath $launcherExecutable -PathType Leaf) {
    throw 'Bundle uninstall left the application launcher installed.'
}

Write-Host 'Clean Windows bundle installation, first-start smoke test, and uninstall passed.'
