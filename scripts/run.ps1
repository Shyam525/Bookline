# Local Development Launcher Script for Bookline

Write-Host "Starting Bookline Local Environment..." -ForegroundColor Coral

if (Get-Command docker-compose -ErrorAction SilentlyContinue) {
    Write-Host "Starting Docker Compose services (PostgreSQL, Redis, Mailpit)..." -ForegroundColor Cyan
    docker-compose up -d
}

Write-Host "Starting ASP.NET Core API server..." -ForegroundColor Cyan
# dotnet run --project backend/src/Bookline.Api/Bookline.Api.csproj

Write-Host "Starting Vite React frontend app..." -ForegroundColor Cyan
# npm --prefix frontend run dev
