# Automated Build & Test Script for Bookline Solution

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "BOOKLINE AUTOMATED VERIFICATION SUITE" -ForegroundColor White
Write-Host "==========================================" -ForegroundColor Cyan

Write-Host "[1/3] Building Backend Solution..." -ForegroundColor Cyan
dotnet build backend/Bookline.slnx

if ($LASTEXITCODE -ne 0) {
    Write-Host "X Backend Build Failed!" -ForegroundColor Red
    exit 1
}

Write-Host "[2/3] Running All Backend Unit & Integration Tests (103 Tests)..." -ForegroundColor Cyan
Get-ChildItem -Path backend -Recurse -Include *.dll,*.exe | Unblock-File -ErrorAction SilentlyContinue
dotnet test backend

if ($LASTEXITCODE -ne 0) {
    Write-Host "X Backend Unit Tests Failed!" -ForegroundColor Red
    exit 1
}

Write-Host "[3/3] Running Frontend TypeScript & Vite Build..." -ForegroundColor Cyan
npm --prefix frontend run build

if ($LASTEXITCODE -ne 0) {
    Write-Host "X Frontend Build Failed!" -ForegroundColor Red
    exit 1
}

Write-Host "SUCCESS: ALL BACKEND TESTS AND FRONTEND BUILDS PASSED CLEANLY!" -ForegroundColor Green
