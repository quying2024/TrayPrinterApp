<#
Helper script: copy publish output and related files into TrayPrinterApp.Setup\ApplicationFolder
Usage: run from repository root after running scripts/publish-win-x64.ps1
#>
param()

$ErrorActionPreference = 'Stop'
$root = Get-Location
$publishDir = Join-Path $root 'publish\win-x64'
$setupAppDir = Join-Path $root 'TrayPrinterApp.Setup\ApplicationFolder'

if (-Not (Test-Path $publishDir)) { throw "Publish directory not found: $publishDir. Run .\scripts\publish-win-x64.ps1 first." }

Write-Host "Preparing setup files from: $publishDir -> $setupAppDir"

if (Test-Path $setupAppDir) { Remove-Item $setupAppDir -Recurse -Force }
New-Item -ItemType Directory -Path $setupAppDir | Out-Null

# Copy publish files
Copy-Item -Path (Join-Path $publishDir '*') -Destination $setupAppDir -Recurse -Force

# Create config and docs folders
$setupConfigDir = Join-Path $root 'TrayPrinterApp.Setup\ApplicationFolder\config'
$setupDocsDir = Join-Path $root 'TrayPrinterApp.Setup\ApplicationFolder\docs'
New-Item -ItemType Directory -Path $setupConfigDir -Force | Out-Null
New-Item -ItemType Directory -Path $setupDocsDir -Force | Out-Null

# Copy config and docs
if (Test-Path (Join-Path $root 'config\appsettings.json')) {
    Copy-Item -Path (Join-Path $root 'config\appsettings.json') -Destination $setupConfigDir -Force
}

# copy docs
$docs = @('README.md','LICENSE','????????.md','??????.md')
foreach ($d in $docs) {
    $src = Join-Path $root $d
    if (Test-Path $src) { Copy-Item -Path $src -Destination $setupDocsDir -Force }
}

$docsDir = Join-Path $root 'docs'
if (Test-Path $docsDir) { Copy-Item -Path (Join-Path $docsDir '*') -Destination $setupDocsDir -Recurse -Force }

Write-Host 'Preparation complete. Open Visual Studio, create Setup Project, add files from TrayPrinterApp.Setup\ApplicationFolder.'
