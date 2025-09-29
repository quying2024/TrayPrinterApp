#!/usr/bin/env pwsh
# TrayPrinterApp ??????
# ??PDF????????

param(
    [string]$DeployPath = "",
    [switch]$Force = $false
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "TrayPrinterApp ??????" -ForegroundColor Cyan
Write-Host "??PDF????????" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# ??????
if (-not $DeployPath) {
    $DeployPath = Read-Host "??????????? (??: C:\Program Files\TrayPrinterApp)"
}

if (-not (Test-Path $DeployPath)) {
    Write-Host "? ???????: $DeployPath" -ForegroundColor Red
    exit 1
}

Write-Host "?? ????: $DeployPath" -ForegroundColor Gray

# ???????
$RequiredFiles = @(
    "TrayApp.exe",
    "TrayApp.dll",
    "PdfiumViewer.dll",
    "Newtonsoft.Json.dll"
)

$MissingFiles = @()
foreach ($file in $RequiredFiles) {
    $filePath = Join-Path $DeployPath $file
    if (Test-Path $filePath) {
        Write-Host "? $file" -ForegroundColor Green
    } else {
        Write-Host "? $file - ??" -ForegroundColor Red
        $MissingFiles += $file
    }
}

# ?????
$NativeLibraries = @("pdfium.dll", "pdfiumviewer.dll")
$MissingNative = @()

foreach ($lib in $NativeLibraries) {
    $libPath = Join-Path $DeployPath $lib
    if (Test-Path $libPath) {
        $fileInfo = Get-Item $libPath
        Write-Host "? $lib - $([math]::Round($fileInfo.Length / 1MB, 2))MB" -ForegroundColor Green
    } else {
        Write-Host "? $lib - ??" -ForegroundColor Red
        $MissingNative += $lib
    }
}

# ????????
if ($MissingNative.Count -gt 0 -or $Force) {
    Write-Host "`n?? ???????..." -ForegroundColor Yellow
    
    # ??????????????
    $SourcePaths = @(
        "src\bin\Release\net8.0-windows",
        "src\bin\Debug\net8.0-windows"
    )
    
    $SourceFound = $false
    foreach ($sourcePath in $SourcePaths) {
        if (Test-Path $sourcePath) {
            Write-Host "?????: $sourcePath" -ForegroundColor Green
            
            foreach ($lib in $MissingNative) {
                $sourceFile = Join-Path $sourcePath $lib
                $targetFile = Join-Path $DeployPath $lib
                
                if (Test-Path $sourceFile) {
                    try {
                        Copy-Item $sourceFile $targetFile -Force
                        Write-Host "? ????: $lib" -ForegroundColor Green
                        $SourceFound = $true
                    } catch {
                        Write-Host "? ????: $lib - $($_.Exception.Message)" -ForegroundColor Red
                    }
                }
            }
            break
        }
    }
    
    if (-not $SourceFound) {
        Write-Host "??  ???????????..." -ForegroundColor Yellow
        
        # ??pdfium.dll
        try {
            $pdfiumPath = Join-Path $DeployPath "pdfium.dll"
            if (-not (Test-Path $pdfiumPath) -or $Force) {
                Write-Host "??pdfium.dll..." -ForegroundColor Gray
                
                # ??GitHub???????
                $downloadUrl = "https://github.com/bblanchon/pdfium-binaries/releases/download/chromium%2F6666/pdfium-win-x64.tgz"
                $tempFile = Join-Path $env:TEMP "pdfium-win-x64.tgz"
                
                Invoke-WebRequest -Uri $downloadUrl -OutFile $tempFile -UseBasicParsing
                
                # ???tar????????
                if (Get-Command tar -ErrorAction SilentlyContinue) {
                    $extractPath = Join-Path $env:TEMP "pdfium-extract"
                    tar -xf $tempFile -C $env:TEMP
                    
                    $extractedDll = Join-Path $env:TEMP "bin\pdfium.dll"
                    if (Test-Path $extractedDll) {
                        Copy-Item $extractedDll $pdfiumPath -Force
                        Write-Host "? pdfium.dll ??????" -ForegroundColor Green
                    }
                }
                
                # ??
                if (Test-Path $tempFile) { Remove-Item $tempFile -Force }
                if (Test-Path $extractPath) { Remove-Item $extractPath -Recurse -Force }
            }
        } catch {
            Write-Host "? ????: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

# ??????
$configPath = Join-Path $DeployPath "config"
if (-not (Test-Path $configPath)) {
    New-Item -ItemType Directory -Path $configPath -Force | Out-Null
    Write-Host "? ??????: $configPath" -ForegroundColor Green
}

# ??????
Write-Host "`n?? ??????..." -ForegroundColor Yellow

$AllGood = $true
foreach ($lib in $NativeLibraries) {
    $libPath = Join-Path $DeployPath $lib
    if (Test-Path $libPath) {
        $fileInfo = Get-Item $libPath
        Write-Host "? $lib - $([math]::Round($fileInfo.Length / 1MB, 2))MB" -ForegroundColor Green
    } else {
        Write-Host "? $lib - ????" -ForegroundColor Red
        $AllGood = $false
    }
}

# ??????
$testScriptPath = Join-Path $DeployPath "test-pdf-printing.ps1"
$testScript = @"
# TrayPrinterApp PDF????????
Write-Host "??PDF????..." -ForegroundColor Yellow

`$appPath = Join-Path `$PSScriptRoot "TrayApp.exe"
if (Test-Path `$appPath) {
    Write-Host "? ??????" -ForegroundColor Green
} else {
    Write-Host "? ???????" -ForegroundColor Red
    exit 1
}

# ?????
`$libs = @("pdfium.dll", "pdfiumviewer.dll")
foreach (`$lib in `$libs) {
    `$libPath = Join-Path `$PSScriptRoot `$lib
    if (Test-Path `$libPath) {
        Write-Host "? `$lib ??" -ForegroundColor Green
    } else {
        Write-Host "? `$lib ??" -ForegroundColor Red
    }
}

Write-Host "??????????PDF???????????" -ForegroundColor Green
"@

$testScript | Out-File -FilePath $testScriptPath -Encoding UTF8
Write-Host "? ??????: $testScriptPath" -ForegroundColor Green

Write-Host "`n========================================" -ForegroundColor Cyan
if ($AllGood) {
    Write-Host "?? ???????" -ForegroundColor Green
    Write-Host "PDF?????????????????" -ForegroundColor Green
    
    Write-Host "`n??  ??????:" -ForegroundColor White
    Write-Host "   cd `"$DeployPath`"" -ForegroundColor Gray
    Write-Host "   .\TrayApp.exe" -ForegroundColor Gray
    
} else {
    Write-Host "??  ???????" -ForegroundColor Yellow
    Write-Host "???????????????????" -ForegroundColor Yellow
    
    Write-Host "`n?? ?????:" -ForegroundColor White
    $NativeLibraries | ForEach-Object {
        $libPath = Join-Path $DeployPath $_
        if (-not (Test-Path $libPath)) {
            Write-Host "  - $_" -ForegroundColor Gray
        }
    }
}
Write-Host "========================================" -ForegroundColor Cyan