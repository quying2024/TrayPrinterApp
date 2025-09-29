#!/usr/bin/env pwsh
# TrayPrinterApp PDF???????????
# ???? "Unable to load DLL 'pdfium.dll'" ??

param(
    [switch]$AutoFix = $false,
    [switch]$Verbose = $false
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "TrayPrinterApp PDF????????" -ForegroundColor Cyan
Write-Host "??pdfium.dll???????????" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# ??????????????
$ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$AppDirectories = @(
    (Join-Path $ProjectRoot "src\bin\Debug\net8.0-windows"),
    (Join-Path $ProjectRoot "src\bin\Release\net8.0-windows")
)

# ????
$DiagnosticResults = @{
    NativeLibrariesFound = $false
    AppDirectoriesExist = @()
    MissingLibraries = @()
    NuGetCacheAvailable = $false
    Solutions = @()
}

Write-Host "`n?? ????..." -ForegroundColor Yellow

# 1. ????????
Write-Host "`n?? ????????..." -ForegroundColor White
foreach ($appDir in $AppDirectories) {
    if (Test-Path $appDir) {
        $DiagnosticResults.AppDirectoriesExist += $appDir
        Write-Host "? ????: $appDir" -ForegroundColor Green
    } else {
        Write-Host "? ?????: $appDir" -ForegroundColor Red
    }
}

if ($DiagnosticResults.AppDirectoriesExist.Count -eq 0) {
    Write-Host "??  ??????????????" -ForegroundColor Yellow
    $DiagnosticResults.Solutions += "???????: dotnet build src/TrayApp.csproj"
}

# 2. ???????
Write-Host "`n?? ??PdfiumViewer???..." -ForegroundColor White
$RequiredLibraries = @("pdfium.dll", "pdfiumviewer.dll")

foreach ($appDir in $DiagnosticResults.AppDirectoriesExist) {
    Write-Host "????: $appDir" -ForegroundColor Gray
    foreach ($lib in $RequiredLibraries) {
        $libPath = Join-Path $appDir $lib
        if (Test-Path $libPath) {
            $fileInfo = Get-Item $libPath
            Write-Host "  ? $lib - ??: $([math]::Round($fileInfo.Length / 1MB, 2))MB" -ForegroundColor Green
            $DiagnosticResults.NativeLibrariesFound = $true
        } else {
            Write-Host "  ? $lib - ??" -ForegroundColor Red
            $DiagnosticResults.MissingLibraries += @{
                Library = $lib
                Directory = $appDir
            }
        }
    }
}

# 3. ??NuGet??
Write-Host "`n?? ??NuGet???..." -ForegroundColor White
$NuGetCache = "${env:USERPROFILE}\.nuget\packages"
$PdfiumViewerPaths = @(
    "$NuGetCache\pdfiumviewer\2.13.0\runtimes\win-x64\native",
    "$NuGetCache\pdfiumviewer.core\1.0.1\runtimes\win-x64\native",
    "$NuGetCache\pdfiumviewer.core\1.0.0\runtimes\win-x64\native"
)

foreach ($nugetPath in $PdfiumViewerPaths) {
    if (Test-Path $nugetPath) {
        Write-Host "? ??NuGet??: $nugetPath" -ForegroundColor Green
        $DiagnosticResults.NuGetCacheAvailable = $true
        
        if ($Verbose) {
            Get-ChildItem $nugetPath -Filter "*.dll" | ForEach-Object {
                Write-Host "  - $($_.Name) ($([math]::Round($_.Length / 1MB, 2))MB)" -ForegroundColor Gray
            }
        }
        break
    }
}

if (-not $DiagnosticResults.NuGetCacheAvailable) {
    Write-Host "? ???PdfiumViewer?NuGet??" -ForegroundColor Red
    $DiagnosticResults.Solutions += "????NuGet?: dotnet restore src/TrayApp.csproj"
}

# 4. ???????????
Write-Host "`n?? ??????..." -ForegroundColor Yellow

if ($DiagnosticResults.MissingLibraries.Count -gt 0) {
    Write-Host "? ????: ???????" -ForegroundColor Red
    
    # ?????????
    if ($DiagnosticResults.NuGetCacheAvailable) {
        $DiagnosticResults.Solutions += "?NuGet?????????"
    }
    $DiagnosticResults.Solutions += "?????????: .\deploy-pdfium-native.ps1"
    $DiagnosticResults.Solutions += "????pdfium.dll???????"
} else {
    Write-Host "? ?????????" -ForegroundColor Green
}

# 5. ??????
Write-Host "`n???  ??????:" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Cyan

$solutionIndex = 1
foreach ($solution in $DiagnosticResults.Solutions) {
    Write-Host "$solutionIndex. $solution" -ForegroundColor White
    $solutionIndex++
}

# ??????
if ($DiagnosticResults.MissingLibraries.Count -gt 0) {
    Write-Host "`n?? ??????:" -ForegroundColor Green
    Write-Host "----------------------------------------" -ForegroundColor Cyan
    
    Write-Host "??A: ???? (??)" -ForegroundColor White
    Write-Host "  ??: .\fix-pdf-printing.ps1 -AutoFix" -ForegroundColor Gray
    
    Write-Host "`n??B: ????" -ForegroundColor White
    Write-Host "  1. ??????:" -ForegroundColor Gray
    Write-Host "     dotnet clean src/TrayApp.csproj" -ForegroundColor Gray
    Write-Host "     dotnet restore src/TrayApp.csproj" -ForegroundColor Gray
    Write-Host "     dotnet build src/TrayApp.csproj --configuration Release" -ForegroundColor Gray
    Write-Host "`n  2. ?????:" -ForegroundColor Gray
    Write-Host "     .\deploy-pdfium-native.ps1" -ForegroundColor Gray
    Write-Host "`n  3. ????????" -ForegroundColor Gray
}

# ????
if ($AutoFix -and $DiagnosticResults.MissingLibraries.Count -gt 0) {
    Write-Host "`n?? ??????..." -ForegroundColor Yellow
    
    try {
        # ??1: ??????
        Write-Host "??1: ??????..." -ForegroundColor White
        dotnet clean src/TrayApp.csproj
        dotnet restore src/TrayApp.csproj
        dotnet build src/TrayApp.csproj --configuration Release
        
        # ??2: ?????
        Write-Host "??2: ?????..." -ForegroundColor White
        & ".\deploy-pdfium-native.ps1" -Force
        
        # ??3: ??????
        Write-Host "??3: ??????..." -ForegroundColor White
        $allFixed = $true
        foreach ($missing in $DiagnosticResults.MissingLibraries) {
            $libPath = Join-Path $missing.Directory $missing.Library
            if (Test-Path $libPath) {
                Write-Host "? ???: $($missing.Library)" -ForegroundColor Green
            } else {
                Write-Host "? ????: $($missing.Library)" -ForegroundColor Red
                $allFixed = $false
            }
        }
        
        if ($allFixed) {
            Write-Host "`n?? ???????" -ForegroundColor Green
            Write-Host "????????TrayPrinterApp?PDF???????????" -ForegroundColor Green
        } else {
            Write-Host "`n??  ????????" -ForegroundColor Yellow
            Write-Host "???????????????" -ForegroundColor Yellow
        }
        
    } catch {
        Write-Host "? ??????: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "??????????" -ForegroundColor Yellow
    }
}

# 6. ??????
Write-Host "`n?? ??????..." -ForegroundColor White
$reportPath = Join-Path $ProjectRoot "PDF????????.md"

$reportContent = @"
# TrayPrinterApp PDF????????

**????**: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
**????**: pdfium.dll ?????PDF????

## ????

### ????????
$(if ($DiagnosticResults.AppDirectoriesExist.Count -gt 0) {
    "? ?? $($DiagnosticResults.AppDirectoriesExist.Count) ???????"
    $DiagnosticResults.AppDirectoriesExist | ForEach-Object { "- $_" }
} else {
    "? ?????????"
})

### ???????
$(if ($DiagnosticResults.NativeLibrariesFound) {
    "? ?????????"
} else {
    "? ?????????"
})

??????:
$(if ($DiagnosticResults.MissingLibraries.Count -gt 0) {
    $DiagnosticResults.MissingLibraries | ForEach-Object { "- $($_.Library) (???: $($_.Directory))" }
} else {
    "?????"
})

### NuGet????
$(if ($DiagnosticResults.NuGetCacheAvailable) {
    "? NuGet????"
} else {
    "? NuGet?????"
})

## ????

$(
$index = 1
$DiagnosticResults.Solutions | ForEach-Object { 
    "$index. $_"
    $index++
}
)

## ??????

```powershell
# ??????
.\fix-pdf-printing.ps1 -AutoFix

# ??????
dotnet clean src/TrayApp.csproj
dotnet restore src/TrayApp.csproj  
dotnet build src/TrayApp.csproj --configuration Release
.\deploy-pdfium-native.ps1
```

## ????

**Q: ??????pdfium.dll???**
A: PdfiumViewer?????pdfium.dll?????????????????????

**Q: ???????????**
A: ????TrayPrinterApp???????PDF??????????"Unable to load DLL"??????????

**Q: ????????????**
A: ??????????????pdfium.dll??????????5MB???

---
*????TrayPrinterApp????????*
"@

$reportContent | Out-File -FilePath $reportPath -Encoding UTF8
Write-Host "? ???????: $reportPath" -ForegroundColor Green

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "????" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

if ($DiagnosticResults.MissingLibraries.Count -eq 0) {
    Write-Host "?? PDF?????????" -ForegroundColor Green
} else {
    Write-Host "??  ?????????????????" -ForegroundColor Yellow
    Write-Host "????: .\fix-pdf-printing.ps1 -AutoFix" -ForegroundColor White
}