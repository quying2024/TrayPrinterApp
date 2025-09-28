<#
Build MSI using WiX toolset. Requirements:
- WiX (heat.exe, candle.exe, light.exe) must be installed and on PATH.
- Run from repository root after running scripts/publish-win-x64.ps1 and tools/prepare-setup-files.ps1.

Usage: powershell -ExecutionPolicy Bypass -File .\scripts\build-msi.ps1
#>
param()

$ErrorActionPreference = 'Stop'
$root = Get-Location
$sourceDir = Join-Path $root 'TrayPrinterApp.Setup\ApplicationFolder'
$objDir = Join-Path $root 'obj\wix'
$outDir = Join-Path $root 'installer_output'

if (-Not (Test-Path $sourceDir)) { throw "Source folder not found: $sourceDir. Run publish and prepare scripts first." }

# Check WiX tools
function Find-Tool([string]$name) {
    $path = (Get-Command $name -ErrorAction SilentlyContinue)?.Source
    if (-not $path) { return $null }
    return $path
}

$heat = Find-Tool 'heat.exe'
$candle = Find-Tool 'candle.exe'
$light = Find-Tool 'light.exe'

if (-not $heat -or -not $candle -or -not $light) {
    throw "WiX tools not found in PATH. Please install WiX Toolset (https://wixtoolset.org/) and ensure heat.exe, candle.exe, light.exe are on PATH."
}

Write-Host "Using WiX tools: heat=$heat, candle=$candle, light=$light"

# prepare directories
if (Test-Path $objDir) { Remove-Item $objDir -Recurse -Force }
New-Item -ItemType Directory -Path $objDir | Out-Null
if (Test-Path $outDir) { Remove-Item $outDir -Recurse -Force }
New-Item -ItemType Directory -Path $outDir | Out-Null

# variables
$productName = 'TrayPrinterApp'
$manufacturer = 'quying2024'
$productVersion = '1.0.0.0'
$upgradeCode = '{D6E4A2B7-9F8C-4B2A-8F6C-3E5A9B1C2D3E}' # fixed upgrade code; change if needed

# 1) Harvest files using heat
$appFilesWxs = Join-Path $objDir 'AppFiles.wxs'
$heatCmd = "\"$heat\" dir `"$sourceDir`" -cg AppFiles -gg -sfrag -srd -dr INSTALLFOLDER -var var.SourceDir -out `"$appFilesWxs`""
Write-Host "Running heat to harvest files..."
Invoke-Expression $heatCmd

# 2) Generate main wxs
$mainWxs = Join-Path $objDir 'Main.wxs'
$mainContent = @"
<?xml version='1.0' encoding='utf-8'?>
<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>
  <Product Id='*' Name='$productName' Language='1033' Version='$productVersion' Manufacturer='$manufacturer' UpgradeCode='$upgradeCode'>
    <Package InstallerVersion='500' Compressed='yes' InstallScope='perMachine' />

    <MediaTemplate />

    <Feature Id='ProductFeature' Title='$productName' Level='1'>
      <ComponentGroupRef Id='AppFiles' />
    </Feature>
  
    <Icon Id='AppIcon' SourceFile='$(var.SourceDir)\TrayApp.exe' />
    <Property Id='ARPPRODUCTICON' Value='AppIcon' />

    <Directory Id='TARGETDIR' Name='SourceDir'>
      <Directory Id='ProgramFiles64Folder'>
        <Directory Id='INSTALLFOLDER' Name='$productName' />
      </Directory>
    </Directory>

    <UIRef Id='WixUI_InstallDir' />
    <Property Id='WIXUI_INSTALLDIR' Value='INSTALLFOLDER' />

  </Product>
</Wix>
"@

Set-Content -Path $mainWxs -Value $mainContent -Encoding UTF8

# 3) Compile with candle
Write-Host "Compiling .wxs with candle..."
Push-Location $objDir
$candleCmd = "\"$candle\" -dSourceDir=\"$sourceDir\" -out Main.wixobj Main.wxs AppFiles.wxs"
Invoke-Expression $candleCmd

# 4) Link with light
Write-Host "Linking MSI with light..."
$msiOut = Join-Path $outDir "$productName.msi"
$lightCmd = "\"$light\" -out `"$msiOut`" Main.wixobj AppFiles.wixobj -ext WixUIExtension"
Invoke-Expression $lightCmd
Pop-Location

Write-Host "MSI built: $msiOut"
