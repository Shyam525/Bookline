Write-Host "Pushing 31 Bookline Phase 0 commits to GitHub..." -ForegroundColor Cyan
git push origin main
if ($LASTEXITCODE -eq 0) {
    Write-Host "Successfully pushed commits! Check your GitHub profile contribution graph." -ForegroundColor Green
} else {
    Write-Host "Push failed or requires credentials. Please log in to GitHub in your terminal." -ForegroundColor Red
}
