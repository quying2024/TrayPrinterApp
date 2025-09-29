# PdfiumViewer.Core ??????
# ?????? PdfiumViewer.Core + System.Drawing.Printing ??

Write-Host "?? ?? TrayPrinterApp PdfiumViewer.Core ????" -ForegroundColor Green

# ???????
Write-Host "?? ????????..." -ForegroundColor Yellow
$publishPath = ".\publish\win-x64-core"
$deployTestPath = "C:\TrayPrintApp_Core_Test"

try {
    # ????
    dotnet publish src/TrayApp.csproj -c Release -r win-x64 --self-contained -o $publishPath
    
    if (-not (Test-Path "$publishPath\TrayApp.exe")) {
        Write-Host "? ?????????????" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "? ????" -ForegroundColor Green
    
    # ????????
    if (Test-Path $deployTestPath) {
        Remove-Item $deployTestPath -Recurse -Force
    }
    New-Item -ItemType Directory -Path $deployTestPath -Force | Out-Null
    
    # ????
    Copy-Item -Path "$publishPath\*" -Destination $deployTestPath -Recurse -Force
    
    # ??????
    $testWatchPath = "C:\TrayPrintTest_Core"
    if (!(Test-Path $testWatchPath)) {
        New-Item -ItemType Directory -Path $testWatchPath -Force | Out-Null
    }
    
    # ??????
    $configPath = "$deployTestPath\config\appsettings.json"
    $config = @{
        "Monitoring" = @{
            "WatchPath" = $testWatchPath
            "BatchTimeoutSeconds" = 3
            "FileTypes" = @(".pdf", ".docx", ".doc", ".jpg", ".jpeg", ".png", ".bmp")
        }
        "PrinterManagement" = @{
            "HiddenPrinters" = @("Fax", "OneNote for Windows 10")
            "DisplayOrder" = "UsageFrequency"
        }
        "PrintSettings" = @{
            "DefaultCopies" = 1
            "FitToPage" = $true
            "KeepAspectRatio" = $true
            "DefaultDpi" = 300
        }
        "TaskHistory" = @{
            "MaxRecords" = 5
            "StoragePath" = "task_history.json"
        }
        "Logging" = @{
            "LogLevel" = "Debug"
            "LogFilePath" = "app.log"
        }
    }
    
    $config | ConvertTo-Json -Depth 10 | Out-File -FilePath $configPath -Encoding UTF8
    
    Write-Host "?? PdfiumViewer.Core ?????????" -ForegroundColor Green
    Write-Host ""
    Write-Host "?? ????: $deployTestPath" -ForegroundColor Cyan
    Write-Host "?? ????: $testWatchPath" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "?? ????:" -ForegroundColor Yellow
    Write-Host "1. ????: $deployTestPath\TrayApp.exe" -ForegroundColor Gray
    Write-Host "2. ??????" -ForegroundColor Gray
    Write-Host "3. ??PDF????: $testWatchPath" -ForegroundColor Gray
    Write-Host "4. ??????????" -ForegroundColor Gray
    Write-Host "5. ????: $deployTestPath\logs\" -ForegroundColor Gray
    
} catch {
    Write-Host "? ????: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}