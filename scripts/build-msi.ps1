<#
Build MSI installer using WiX toolset (requires WiX installed: candle.exe & light.exe)
This script:
  - publishes self-contained x64 build
  - prepares setup files into TrayPrinterApp.Setup\ApplicationFolder
  - generates a WiX source file (TrayPrinterApp.Setup\installer.wxs)
  - invokes candle & light to build the MSI (if WiX is available)

Usage: run from repository root in PowerShell:
  powershell -ExecutionPolicy Bypass -File .\scripts\build-msi.ps1
#>
param()

$ErrorActionPreference = 'Stop'
$root = Get-Location
Write-Host "Repository root: $root"

# 1) Publish self-contained x64
Write-Host "Publishing self-contained x64..."
& powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\publish-win-x64.ps1

# 2) Prepare setup files
Write-Host "Preparing setup files..."
& powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\prepare-setup-files.ps1

$appDir = Join-Path $root 'TrayPrinterApp.Setup\ApplicationFolder'
if (-Not (Test-Path $appDir)) { throw "Setup application folder not found: $appDir" }

# 3) Generate WiX source file
$wxsPath = Join-Path $root 'TrayPrinterApp.Setup\installer.wxs'
$productGuid = [Guid]::NewGuid().ToString("B").ToUpper()
$upgradeGuid = [Guid]::NewGuid().ToString("B").ToUpper()
$productVersion = "1.0.0.0"
$productName = "TrayPrinterApp"
$manufacturer = "YourCompany"

Write-Host "Generating WiX source: $wxsPath"

# Enumerate files
$files = Get-ChildItem -Path $appDir -Recurse -File | Sort-Object FullName

$componentEntries = New-Object System.Text.StringBuilder
$componentRefs = New-Object System.Text.StringBuilder
$index = 0
foreach ($f in $files) {
    $index++
    $compId = "cmp$index"
    $fileId = "fil$index"
    # Wifi requires Source to be an absolute path; escape ampersands and replace backslashes
    $src = $f.FullName
    $relPath = $f.FullName.Substring($appDir.Length).TrimStart('\')
    $escapedSrc = $src -replace '&','&amp;'

    $componentEntries.AppendLine("    <Component Id=\"$compId\" Guid=\"*\">") | Out-Null
    $componentEntries.AppendLine("      <File Id=\"$fileId\" Source=\"$escapedSrc\" KeyPath=\"yes\" />") | Out-Null
    $componentEntries.AppendLine("    </Component>") | Out-Null

    $componentRefs.AppendLine("      <ComponentRef Id=\"$compId\" />") | Out-Null
}

$wxsContent = @"
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://schemas.microsoft.com/wix/2006/wi">
  <Product Id="*" Name="$productName" Language="1033" Version="$productVersion" Manufacturer="$manufacturer" UpgradeCode="$upgradeGuid">
    <Package InstallerVersion="500" Compressed="yes" InstallScope="perMachine"/>
    <MediaTemplate/>

    <Directory Id="TARGETDIR" Name="SourceDir">
      <Directory Id="ProgramFiles64Folder">
        <Directory Id="INSTALLFOLDER" Name="$productName" />
      </Directory>
    </Directory>

    <DirectoryRef Id="INSTALLFOLDER">
$($componentEntries.ToString())
    </DirectoryRef>

    <Feature Id="ProductFeature" Title="$productName" Level="1">
$($componentRefs.ToString())
    </Feature>
  </Product>
</Wix>
"@

Set-Content -Path $wxsPath -Value $wxsContent -Encoding UTF8
Write-Host "WiX source generated. Components: $index"

# 4) Build MSI if WiX tools present
$candle = Get-Command candle -ErrorAction SilentlyContinue
$light = Get-Command light -ErrorAction SilentlyContinue
if ($null -eq $candle -or $null -eq $light) {
    Write-Warning "WiX toolset (candle/light) not found in PATH. Install WiX (https://wixtoolset.org/) and re-run this script to build the MSI."
    Write-Host "WiX source is ready at: $wxsPath"
    Write-Host "Prepared application folder: $appDir"
    exit 0
}

Write-Host "WiX detected. Compiling..."
Push-Location (Join-Path $root 'TrayPrinterApp.Setup')
try {
    $wixObj = "installer.wixobj"
    & candle -out $wixObj "installer.wxs"
    if ($LASTEXITCODE -ne 0) { throw "candle failed with exit code $LASTEXITCODE" }

    $outMsi = Join-Path $root 'TrayPrinterApp.Setup\bin\Release\TrayPrinterApp.msi'
    New-Item -ItemType Directory -Path (Split-Path $outMsi) -Force | Out-Null

    & light -out $outMsi $wixObj
    if ($LASTEXITCODE -ne 0) { throw "light failed with exit code $LASTEXITCODE" }

    Write-Host "MSI built successfully: $outMsi"
}
finally {
    Pop-Location
}

Write-Host "Done."
