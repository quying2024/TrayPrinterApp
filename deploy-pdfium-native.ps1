#!/usr/bin/env pwsh
# TrayPrinterApp PdfiumViewer ???????
# ?? "Unable to load DLL 'pdfium.dll'" ??

param(
    [string]$TargetDirectory = "",
    [switch]$Force = $false
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "PdfiumViewer ???????" -ForegroundColor Cyan
Write-Host "?? pdfium.dll ????" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# ???????
$ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$BinDirectory = if ($TargetDirectory) { $TargetDirectory } else { Join-Path $ProjectRoot "src\bin\Debug\net8.0-windows" }

Write-Host "?????: $ProjectRoot" -ForegroundColor Gray
Write-Host "????: $BinDirectory" -ForegroundColor Gray

# ????????
if (!(Test-Path $BinDirectory)) {
    Write-Host "??????: $BinDirectory" -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $BinDirectory -Force | Out-Null
}

# ??DLL????
$NativeDlls = @(
    "pdfium.dll",
    "pdfiumviewer.dll"
)

# ????DLL
$MissingDlls = @()
foreach ($dll in $NativeDlls) {
    $dllPath = Join-Path $BinDirectory $dll
    if (!(Test-Path $dllPath) -or $Force) {
        $MissingDlls += $dll
    } else {
        Write-Host "? ???: $dll" -ForegroundColor Green
    }
}

if ($MissingDlls.Count -eq 0 -and !$Force) {
    Write-Host "? ????DLL??????????" -ForegroundColor Green
    exit 0
}

Write-Host "?????DLL: $($MissingDlls -join ', ')" -ForegroundColor Yellow

# ??1: ?NuGet??????
Write-Host "`n??1: ?NuGet?????..." -ForegroundColor Yellow

$NuGetCache = "${env:USERPROFILE}\.nuget\packages"
$PdfiumViewerPaths = @(
    "$NuGetCache\pdfiumviewer\2.13.0\runtimes\win-x64\native",
    "$NuGetCache\pdfiumviewer.core\1.0.1\runtimes\win-x64\native",
    "$NuGetCache\pdfiumviewer.core\1.0.0\runtimes\win-x64\native"
)

$FoundSource = $false
foreach ($sourcePath in $PdfiumViewerPaths) {
    if (Test-Path $sourcePath) {
        Write-Host "??NuGet??: $sourcePath" -ForegroundColor Green
        try {
            foreach ($dll in $MissingDlls) {
                $sourceFile = Join-Path $sourcePath $dll
                $targetFile = Join-Path $BinDirectory $dll
                
                if (Test-Path $sourceFile) {
                    Copy-Item $sourceFile $targetFile -Force
                    Write-Host "? ????: $dll" -ForegroundColor Green
                } else {
                    Write-Host "??  ??????: $dll" -ForegroundColor Yellow
                }
            }
            $FoundSource = $true
            break
        } catch {
            Write-Host "? ????: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

# ??2: ?????????
if (-not $FoundSource) {
    Write-Host "`n??2: ????????..." -ForegroundColor Yellow
    
    try {
        # ??pdfium.dll
        $PdfiumUrl = "https://github.com/bblanchon/pdfium-binaries/releases/download/chromium%2F6666/pdfium-win-x64.tgz"
        $TempTgz = Join-Path $env:TEMP "pdfium-win-x64.tgz"
        $ExtractPath = Join-Path $env:TEMP "pdfium-extract"
        
        Write-Host "?? pdfium ???..." -ForegroundColor Gray
        Invoke-WebRequest -Uri $PdfiumUrl -OutFile $TempTgz -UseBasicParsing
        
        # ?????7zip?tar???
        if (Get-Command tar -ErrorAction SilentlyContinue) {
            tar -xzf $TempTgz -C $env:TEMP
            $ExtractedDll = Join-Path $env:TEMP "bin\pdfium.dll"
            if (Test-Path $ExtractedDll) {
                Copy-Item $ExtractedDll (Join-Path $BinDirectory "pdfium.dll") -Force
                Write-Host "? pdfium.dll ??????" -ForegroundColor Green
                $FoundSource = $true
            }
        } else {
            Write-Host "??  ??tar???????????" -ForegroundColor Yellow
        }
        
        # ??????
        if (Test-Path $TempTgz) { Remove-Item $TempTgz -Force }
        if (Test-Path $ExtractPath) { Remove-Item $ExtractPath -Recurse -Force }
        
    } catch {
        Write-Host "? ????: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# ??3: ????
if (-not $FoundSource) {
    Write-Host "`n??3: ??????" -ForegroundColor Yellow
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "?????????????????" -ForegroundColor Red
    Write-Host ""
    Write-Host "1. ?????????NuGet??" -ForegroundColor White
    Write-Host "   dotnet restore src/TrayApp.csproj" -ForegroundColor Gray
    Write-Host "   dotnet build src/TrayApp.csproj" -ForegroundColor Gray
    Write-Host ""
    Write-Host "2. ??????pdfium.dll?" -ForegroundColor White
    Write-Host "   ??????????Windows x64?pdfium.dll?" -ForegroundColor Gray
    Write-Host "   https://github.com/bblanchon/pdfium-binaries/releases" -ForegroundColor Gray
    Write-Host ""
    Write-Host "3. ?pdfium.dll??????????" -ForegroundColor White
    Write-Host "   $BinDirectory" -ForegroundColor Gray
    Write-Host ""
    Write-Host "4. ????????" -ForegroundColor White
    Write-Host "========================================" -ForegroundColor Cyan
}

# ??????
Write-Host "`n??????..." -ForegroundColor Yellow
$AllDeployed = $true
foreach ($dll in $NativeDlls) {
    $dllPath = Join-Path $BinDirectory $dll
    if (Test-Path $dllPath) {
        $fileInfo = Get-Item $dllPath
        Write-Host "? $dll - ??: $([math]::Round($fileInfo.Length / 1MB, 2))MB" -ForegroundColor Green
    } else {
        Write-Host "? $dll - ??" -ForegroundColor Red
        $AllDeployed = $false
    }
}

Write-Host "`n========================================" -ForegroundColor Cyan
if ($AllDeployed) {
    Write-Host "?? ????????" -ForegroundColor Green
    Write-Host "????????PDF??????" -ForegroundColor Green
} else {
    Write-Host "??  ?????????" -ForegroundColor Yellow
    Write-Host "??????????????" -ForegroundColor Yellow
}
Write-Host "========================================" -ForegroundColor Cyan