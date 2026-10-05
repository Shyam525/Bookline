# Launch Bookline Docker Infrastructure and Web API
Write-Host "Starting Bookline Infrastructure (PostgreSQL, Redis, Mailpit)..." -ForegroundColor Cyan
docker-compose up -d

Write-Host "Stopping any lingering Bookline API processes..." -ForegroundColor Yellow
Stop-Process -Name "Bookline.Api" -Force -ErrorAction SilentlyContinue

Write-Host "Starting Bookline Web API in Release Mode..." -ForegroundColor Green
dotnet run -c Release --project backend/src/Bookline.Api
