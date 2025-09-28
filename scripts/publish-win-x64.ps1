<#
Publish self-contained x64 single-file release for TrayApp
Usage: run from repository root in PowerShell (run as admin if needed)
#>
param()

$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $Root\..

Write-Host "Publishing TrayApp (self-contained, win-x64) ..."

dotnet publish src/TrayApp.csproj -c Release -r win-x64 --self-contained true -o .\publish\win-x64 `
  /p:PublishSingleFile=true `
  /p:IncludeNativeLibrariesForSelfExtract=true `
  /p:PublishTrimmed=false

if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }

Write-Host "Publish complete: $(Resolve-Path .\publish\win-x64)"
