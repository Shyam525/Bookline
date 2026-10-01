# Reset PostgreSQL Database and Re-seed Demo Data
Write-Host "Resetting Bookline PostgreSQL Database..." -ForegroundColor Yellow
Stop-Process -Name "Bookline.Api" -Force -ErrorAction SilentlyContinue

docker exec -i bookline-postgres psql -U postgres -d bookline_db -c "DROP SCHEMA public CASCADE; CREATE SCHEMA public;"

Write-Host "Database reset complete. Next API launch will recreate tables and seed demo tenant automatically." -ForegroundColor Green
