#!/usr/bin/env pwsh
# TrayPrinterApp ??????????
# ?? .NET 8 ? PdfiumViewer.Core + System.Drawing.Printing ??

param(
    [string]$Configuration = "Debug",
    [string]$TestFilter = "",
    [switch]$Coverage = $false,
    [switch]$Verbose = $false,
    [switch]$SkipBuild = $false,
    [int]$Timeout = 300  # 5????
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "TrayPrinterApp ????????" -ForegroundColor Cyan
Write-Host "??: PdfiumViewer.Core + System.Drawing.Printing" -ForegroundColor Cyan
Write-Host "????: .NET 8" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# ??????
$ErrorActionPreference = "Stop"

# ???????
$ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$SolutionFile = Join-Path $ProjectRoot "TrayPrinterApp.sln"
$TestProject = Join-Path $ProjectRoot "tests\TrayApp.Tests.csproj"
$TestResultsDir = Join-Path $ProjectRoot "TestResults"
$CoverageDir = Join-Path $ProjectRoot "Coverage"

# ??????????
if (!(Test-Path $TestResultsDir)) {
    New-Item -ItemType Directory -Path $TestResultsDir -Force | Out-Null
}

if ($Coverage -and !(Test-Path $CoverageDir)) {
    New-Item -ItemType Directory -Path $CoverageDir -Force | Out-Null
}

Write-Host "????:" -ForegroundColor Yellow
Write-Host "  ????: $SolutionFile" -ForegroundColor Gray
Write-Host "  ????: $TestProject" -ForegroundColor Gray
Write-Host "  ??: $Configuration" -ForegroundColor Gray
Write-Host "  ??: $Timeout ?" -ForegroundColor Gray

# ?? .NET 8 ????
try {
    $dotnetVersion = dotnet --version
    Write-Host "  .NET ??: $dotnetVersion" -ForegroundColor Gray
    
    if (-not $dotnetVersion.StartsWith("8.")) {
        Write-Warning ".NET 8 ?????????????????"
    }
} catch {
    Write-Error ".NET CLI ??????? .NET 8 SDK"
    exit 1
}

# ??????
if (-not $SkipBuild) {
    Write-Host "`n????????..." -ForegroundColor Yellow
    try {
        $buildArgs = @(
            "build"
            $SolutionFile
            "--configuration", $Configuration
            "--verbosity", $(if ($Verbose) { "detailed" } else { "minimal" })
        )
        
        dotnet @buildArgs
        if ($LASTEXITCODE -ne 0) {
            throw "????????: $LASTEXITCODE"
        }
        Write-Host "? ????" -ForegroundColor Green
    } catch {
        Write-Error "????: $($_.Exception.Message)"
        exit 1
    }
} else {
    Write-Host "??  ??????" -ForegroundColor Yellow
}

# ??????
$testArgs = @(
    "test"
    $TestProject
    "--configuration", $Configuration
    "--no-build"
    "--logger", "trx;LogFileName=TestResults_$(Get-Date -Format 'yyyyMMdd_HHmmss').trx"
    "--results-directory", $TestResultsDir
)

# ??????
$testArgs += "--blame-hang-timeout", "${Timeout}s"

# ??????
if ($Verbose) {
    $testArgs += "--verbosity", "detailed"
} else {
    $testArgs += "--verbosity", "normal"
}

# ???????
if ($TestFilter) {
    $testArgs += "--filter", $TestFilter
    Write-Host "?????: $TestFilter" -ForegroundColor Gray
}

# ?????????
if ($Coverage) {
    $testArgs += "--collect", "XPlat Code Coverage"
    $testArgs += "--settings", (Join-Path $ProjectRoot "tests\coverlet.runsettings")
    Write-Host "?????????" -ForegroundColor Gray
}

# ????
Write-Host "`n??????..." -ForegroundColor Yellow
Write-Host "????: dotnet $($testArgs -join ' ')" -ForegroundColor Gray

try {
    $testStartTime = Get-Date
    dotnet @testArgs
    $testExitCode = $LASTEXITCODE
    $testDuration = (Get-Date) - $testStartTime
    
    Write-Host "`n?????? (??: $($testDuration.ToString('mm\:ss')))" -ForegroundColor Yellow
    
    if ($testExitCode -eq 0) {
        Write-Host "? ??????!" -ForegroundColor Green
    } else {
        Write-Host "? ?????? (???: $testExitCode)" -ForegroundColor Red
    }
    
} catch {
    Write-Error "??????: $($_.Exception.Message)"
    exit 1
}

# ?????????
if ($Coverage) {
    Write-Host "`n???????..." -ForegroundColor Yellow
    
    # ???????
    $coverageFiles = Get-ChildItem -Path $TestResultsDir -Filter "coverage.cobertura.xml" -Recurse
    
    if ($coverageFiles.Count -gt 0) {
        $latestCoverageFile = $coverageFiles | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        
        # ????????
        $targetCoverageFile = Join-Path $CoverageDir "coverage_$(Get-Date -Format 'yyyyMMdd_HHmmss').xml"
        Copy-Item $latestCoverageFile.FullName $targetCoverageFile
        
        Write-Host "?????: $targetCoverageFile" -ForegroundColor Gray
        
        # ????HTML????????reportgenerator?
        try {
            dotnet tool install --global dotnet-reportgenerator-globaltool --ignore-failed-sources | Out-Null
            
            $htmlReportDir = Join-Path $CoverageDir "html"
            reportgenerator "-reports:$targetCoverageFile" "-targetdir:$htmlReportDir" "-reporttypes:Html"
            
            Write-Host "HTML?????: $htmlReportDir\index.html" -ForegroundColor Green
        } catch {
            Write-Warning "????HTML??????????? dotnet-reportgenerator-globaltool"
        }
    } else {
        Write-Warning "????????"
    }
}

# ????????
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "??????" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# ??????????????
$testResultFiles = Get-ChildItem -Path $TestResultsDir -Filter "*.trx" | Sort-Object LastWriteTime -Descending
if ($testResultFiles.Count -gt 0) {
    $latestResultFile = $testResultFiles[0]
    Write-Host "??????: $($latestResultFile.FullName)" -ForegroundColor Gray
    
    # ????????
    try {
        [xml]$testResults = Get-Content $latestResultFile.FullName
        $resultsSummary = $testResults.TestRun.ResultSummary
        
        if ($resultsSummary) {
            $total = $resultsSummary.Counters.total
            $passed = $resultsSummary.Counters.passed
            $failed = $resultsSummary.Counters.failed
            $skipped = $resultsSummary.Counters.notExecuted
            
            Write-Host "????: $total" -ForegroundColor White
            Write-Host "??: $passed" -ForegroundColor Green
            Write-Host "??: $failed" -ForegroundColor $(if ($failed -eq "0") { "Green" } else { "Red" })
            Write-Host "??: $skipped" -ForegroundColor Yellow
        }
    } catch {
        Write-Warning "??????????"
    }
}

# ??????
Write-Host "`n????:" -ForegroundColor Yellow
Write-Host "• ?????????????? (PdfiumViewer.Core, Office)" -ForegroundColor Gray
Write-Host "• ???????????????????" -ForegroundColor Gray
Write-Host "• ??????????????" -ForegroundColor Gray
Write-Host "• ????????????" -ForegroundColor Gray

Write-Host "`n??????:" -ForegroundColor Yellow
Write-Host "• ????: ???????????" -ForegroundColor Gray
Write-Host "• ????: ????????" -ForegroundColor Gray
Write-Host "• ????: ????????????" -ForegroundColor Gray
Write-Host "• ??????: ?????????" -ForegroundColor Gray

# ???
exit $testExitCode