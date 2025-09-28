<#
Build MSIX package for TrayPrinterApp (self-contained win-x64).
Creates a self-signed certificate, packs using MakeAppx, signs with SignTool, and optionally installs the package.

Requirements:
- makeappx.exe (Windows 10 SDK)
- signtool.exe (Windows SDK)
- PowerShell run as Administrator for Add-AppxPackage if installing

Usage:
  powershell -ExecutionPolicy Bypass -File .\scripts\build-msix.ps1
#>
param()

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root\..

Write-Host "Repository root: $(Get-Location)"

# 1) Publish self-contained x64
Write-Host "Publishing self-contained x64..."
& powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\publish-win-x64.ps1

$publishDir = Join-Path (Get-Location) 'publish\win-x64'
$packageRoot = Join-Path (Get-Location) 'TrayPrinterApp.Setup\PackageRoot'

# 2) Prepare package root
if (Test-Path $packageRoot) { Remove-Item $packageRoot -Recurse -Force }
New-Item -ItemType Directory -Path $packageRoot | Out-Null

Write-Host "Copying publish output to package root..."
Copy-Item -Path (Join-Path $publishDir '*') -Destination $packageRoot -Recurse -Force

# create config and docs folders
$srcConfig = Join-Path (Get-Location) 'config'
if (Test-Path $srcConfig) { Copy-Item -Path (Join-Path $srcConfig '*') -Destination (Join-Path $packageRoot 'config') -Recurse -Force }
$srcDocs = Join-Path (Get-Location) 'docs'
if (Test-Path $srcDocs) { Copy-Item -Path (Join-Path $srcDocs '*') -Destination (Join-Path $packageRoot 'docs') -Recurse -Force }

# 3) Create Assets (small placeholder PNGs)
$assetsDir = Join-Path $packageRoot 'Assets'
New-Item -ItemType Directory -Path $assetsDir | Out-Null
$pngBase64 = 'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8Xw8AAn8B9nZJmQAAAABJRU5ErkJggg=='
[System.IO.File]::WriteAllBytes((Join-Path $assetsDir 'StoreLogo.png'), [Convert]::FromBase64String($pngBase64))
[System.IO.File]::WriteAllBytes((Join-Path $assetsDir 'SmallLogo.png'), [Convert]::FromBase64String($pngBase64))

# 4) Generate AppxManifest.xml
$manifestPath = Join-Path $packageRoot 'AppxManifest.xml'
$identityName = 'com.quying.TrayPrinterApp'
$publisher = 'CN=LocalTest'
$displayName = 'TrayPrinterApp'
$publisherDisplayName = 'quying2024'
$manifest = @"
<?xml version="1.0" encoding="utf-8"?>
<Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
         xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
         xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities"
         IgnorableNamespaces="uap rescap">
  <Identity Name="$identityName" Publisher="$publisher" Version="1.0.0.0" />
  <Properties>
    <DisplayName>$displayName</DisplayName>
    <PublisherDisplayName>$publisherDisplayName</PublisherDisplayName>
    <Logo>Assets\StoreLogo.png</Logo>
  </Properties>
  <Dependencies>
    <TargetDeviceFamily Name="Windows.Desktop" MinVersion="10.0.17763.0" MaxVersionTested="10.0.19041.0" />
  </Dependencies>
  <Resources>
    <Resource Language="en-US" />
  </Resources>
  <Applications>
    <Application Id="TrayApp" Executable="TrayApp.exe" EntryPoint="Windows.FullTrustApplication">
      <uap:VisualElements DisplayName="$displayName" Square150x150Logo="Assets\StoreLogo.png" Description="$displayName">
        <uap:DefaultTile ShortName="$displayName" Square44x44Logo="Assets\SmallLogo.png" />
      </uap:VisualElements>
    </Application>
  </Applications>
  <Capabilities>
    <rescap:Capability Name="runFullTrust" />
  </Capabilities>
</Package>
"@

Set-Content -Path $manifestPath -Value $manifest -Encoding UTF8
Write-Host "AppxManifest generated at: $manifestPath"

# 5) Find MakeAppx and SignTool
$makeappx = Get-Command MakeAppx -ErrorAction SilentlyContinue
$signtool = Get-Command signtool -ErrorAction SilentlyContinue
if ($null -eq $makeappx) {
    Write-Warning "MakeAppx.exe not found in PATH. Please install Windows 10/11 SDK or add MakeAppx to PATH.";
}
if ($null -eq $signtool) {
    Write-Warning "signtool.exe not found in PATH. Please install Windows SDK or add signtool to PATH.";
}

# 6) Pack MSIX
$msixOut = Join-Path (Get-Location) 'TrayPrinterApp.msix'
if ($null -ne $makeappx) {
    Write-Host "Packing MSIX..."
    & $makeappx.Path pack /d $packageRoot /p $msixOut
    if ($LASTEXITCODE -ne 0) { throw "MakeAppx failed with exit code $LASTEXITCODE" }
    Write-Host "MSIX package created: $msixOut"
} else {
    Write-Error "Cannot create MSIX because MakeAppx is not available."; exit 1
}

# 7) Create self-signed cert and sign
$pfxPath = Join-Path (Get-Location) 'TrayPrinterAppCert.pfx'
$pwd = ConvertTo-SecureString -String 'P@ssw0rd!' -Force -AsPlainText
$cert = New-SelfSignedCertificate -Type CodeSigningCert -Subject "CN=LocalTest" -CertStoreLocation "Cert:\CurrentUser\My" -KeyExportPolicy Exportable
Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $pwd | Out-Null
if ($null -ne $signtool) {
    Write-Host "Signing MSIX..."
    & $signtool.Path sign /fd SHA256 /a /f $pfxPath /p 'P@ssw0rd!' $msixOut
    if ($LASTEXITCODE -ne 0) { throw "signtool failed with exit code $LASTEXITCODE" }
    Write-Host "MSIX signed: $msixOut"
} else {
    Write-Warning "MSIX created but not signed (signtool missing): $msixOut"
}

# 8) Install for testing (optional)
try {
    Write-Host "Installing MSIX for testing..."
    Add-AppxPackage -Path $msixOut -ForceApplicationShutdown
    Write-Host "MSIX installed.";
} catch {
    Write-Warning "Add-AppxPackage failed: $_";
}

Write-Host "Done. MSIX: $msixOut"
