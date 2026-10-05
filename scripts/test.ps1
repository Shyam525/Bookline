# Automated Build & Test Script for Bookline Solution

Write-Host "==========================================" -ForegroundColor Coral
Write-Host "BOOKLINE AUTOMATED VERIFICATION SUITE" -ForegroundColor White
Write-Host "==========================================" -ForegroundColor Coral

Write-Host "[1/2] Running Backend Unit Tests (Release Mode)..." -ForegroundColor Cyan
dotnet test backend/tests/Bookline.Application.UnitTests/Bookline.Application.UnitTests.csproj -c Release

if ($LASTEXITCODE -ne 0) {
    Write-Host "X Backend Unit Tests Failed!" -ForegroundColor Red
    exit 1
}

Write-Host "[2/2] Running Frontend TypeScript & Vite Build..." -ForegroundColor Cyan
npm --prefix frontend run build

if ($LASTEXITCODE -ne 0) {
    Write-Host "X Frontend Build Failed!" -ForegroundColor Red
    exit 1
}

Write-Host "✓ ALL BACKEND TESTS & FRONTEND BUILDS PASSED CLEANLY!" -ForegroundColor Green
