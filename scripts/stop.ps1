# Gracefully stop Bookline process and Docker containers
Write-Host "Stopping Bookline Web API process..." -ForegroundColor Yellow
Stop-Process -Name "Bookline.Api" -Force -ErrorAction SilentlyContinue

Write-Host "Stopping Docker containers..." -ForegroundColor Yellow
docker-compose down

Write-Host "Bookline services stopped." -ForegroundColor Green
