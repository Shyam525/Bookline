Write-Host "Resetting Bookline Environment..." -ForegroundColor Yellow
docker compose down -v
docker compose up -d postgres redis mailpit
Start-Sleep -Seconds 3
dotnet build backend/Bookline.slnx
Write-Host "Environment Reset Complete." -ForegroundColor Green
