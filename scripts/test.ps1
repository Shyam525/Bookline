Write-Host "Running Backend Unit & Integration Tests..." -ForegroundColor Cyan
dotnet test backend/Bookline.slnx -c Release
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Running Frontend Type Checks & Build..." -ForegroundColor Cyan
Push-Location frontend
npm run build
Pop-Location

Write-Host "All Tests & Build Verification Passed Cleanly!" -ForegroundColor Green
