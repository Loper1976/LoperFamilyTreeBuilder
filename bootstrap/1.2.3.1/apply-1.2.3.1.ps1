param([Parameter(Mandatory=$true)][string]$Root)
$ErrorActionPreference='Stop'
if(-not(Test-Path $Root)){throw "Source root missing: $Root"}

# Validate the official LOPER Texas JPEG and create a WiX-friendly 64px PNG.
$sourceLogo=Join-Path $Root 'src/LoperFamilyTreeBuilder.Web/wwwroot/images/loper-logo.jpg'
$installerLogo=Join-Path $Root 'src/LoperFamilyTreeBuilder.Web/wwwroot/images/loper-installer-logo.png'
if(-not(Test-Path $sourceLogo)){throw 'Official LOPER Texas application logo is missing.'}

Add-Type -AssemblyName System.Drawing
$image=[System.Drawing.Image]::FromFile($sourceLogo)
try {
    if($image.Width -lt 1 -or $image.Height -lt 1){throw 'LOPER Texas logo dimensions are invalid.'}
    $canvas=New-Object System.Drawing.Bitmap 64,64,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb
    try {
        $graphics=[System.Drawing.Graphics]::FromImage($canvas)
        try {
            $graphics.Clear([System.Drawing.Color]::Transparent)
            $graphics.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.SmoothingMode=[System.Drawing.Drawing2D.SmoothingMode]::HighQuality
            $scale=[Math]::Min(60.0/$image.Width,60.0/$image.Height)
            $w=[Math]::Max(1,[int][Math]::Round($image.Width*$scale))
            $h=[Math]::Max(1,[int][Math]::Round($image.Height*$scale))
            $x=[int][Math]::Floor((64-$w)/2)
            $y=[int][Math]::Floor((64-$h)/2)
            $graphics.DrawImage($image,$x,$y,$w,$h)
        } finally { $graphics.Dispose() }
        $canvas.Save($installerLogo,[System.Drawing.Imaging.ImageFormat]::Png)
    } finally { $canvas.Dispose() }
} finally { $image.Dispose() }

# Decode the generated PNG again. This is a real runtime image validation, not just a hash check.
$check=[System.Drawing.Image]::FromFile($installerLogo)
try {
    if($check.Width -ne 64 -or $check.Height -ne 64){throw "Installer logo dimensions are $($check.Width)x$($check.Height), expected 64x64."}
} finally { $check.Dispose() }
$pngBytes=[IO.File]::ReadAllBytes($installerLogo)
if($pngBytes.Length -lt 8 -or $pngBytes[0] -ne 0x89 -or $pngBytes[1] -ne 0x50 -or $pngBytes[2] -ne 0x4E -or $pngBytes[3] -ne 0x47){throw 'Generated installer logo is not a valid PNG payload.'}

# Keep the application version at 1.2.3 for SemVer compatibility while identifying the hotfix in file/host metadata.
$propsPath=Join-Path $Root 'Directory.Build.props'
$props=Get-Content $propsPath -Raw
$props=$props.Replace('<FileVersion>1.2.3.0</FileVersion>','<FileVersion>1.2.3.1</FileVersion>')
$props=$props.Replace('<InformationalVersion>1.2.3-tree-error-checker</InformationalVersion>','<InformationalVersion>1.2.3.1-installer-logo-hotfix</InformationalVersion>')
Set-Content $propsPath $props -Encoding utf8

$launcherPath=Join-Path $Root 'src/LoperFamilyTreeBuilder.Launcher/UpgradeBackupService.cs'
$launcher=Get-Content $launcherPath -Raw
$launcher=$launcher.Replace('TargetVersion = "1.2.3"','TargetVersion = "1.2.3.1"')
Set-Content $launcherPath $launcher -Encoding utf8

$programPath=Join-Path $Root 'src/LoperFamilyTreeBuilder.Web/Program.cs'
$program=Get-Content $programPath -Raw
$program=$program.Replace('version = "1.2.3"','version = "1.2.3.1"')
$program=$program.Replace('release = "tree-error-checker"','release = "installer-logo-hotfix"')
Set-Content $programPath $program -Encoding utf8

# Use the simplest stock WixStdBA theme and a generated PNG. Never use the application JPEG directly as the BA logo.
$bundlePath=Join-Path $Root 'installer/LoperFamilyTreeBuilder.Setup/Bundle.wxs'
$bundle=Get-Content $bundlePath -Raw
$bundle=$bundle.Replace('Version="1.2.3.0"','Version="1.2.3.1"')
$bundle=[regex]::Replace($bundle,'Theme="[^"]+"','Theme="hyperlinkLicense"')
$bundle=$bundle.Replace('loper-logo.jpg','loper-installer-logo.png')
$bundle=[regex]::Replace($bundle,'\s+LogoSideFile="[^"]*"','')
Set-Content $bundlePath $bundle -Encoding utf8

Write-Host '1.2.3.1 applied: validated LOPER Texas JPEG, generated 64x64 PNG for WixStdBA, simplified installer theme, and updated hotfix version markers.'
