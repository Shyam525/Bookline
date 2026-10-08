$ErrorActionPreference = 'Stop'
$repoRoot = (Get-Item $PSScriptRoot).Parent.FullName
$zipPath = Join-Path $repoRoot "BOOKLINE-final.zip"
$staging = Join-Path $repoRoot "staging-zip"

Write-Host "Creating BOOKLINE-final.zip package..." -ForegroundColor Cyan

if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
if (Test-Path $staging) { Remove-Item -Recurse -Force $staging }

New-Item -ItemType Directory -Path $staging | Out-Null

$items = @(
    "backend",
    "frontend",
    "docs",
    "scripts",
    "docker-compose.yml",
    "docker-compose.dev.yml",
    "README.md",
    ".env.example",
    ".editorconfig",
    ".gitignore"
)

foreach ($item in $items) {
    $src = Join-Path $repoRoot $item
    if (Test-Path $src) {
        Write-Host "Staging $item..." -ForegroundColor Gray
        Copy-Item -Path $src -Destination $staging -Recurse -Force
    }
}

Write-Host "Cleaning build caches and node_modules from staging..." -ForegroundColor Gray
Get-ChildItem -Path $staging -Include bin,obj,node_modules,.turbo,dist -Recurse -Directory -Force | Remove-Item -Recurse -Force

Write-Host "Compressing archive to $zipPath..." -ForegroundColor Cyan
Compress-Archive -Path (Join-Path $staging "*") -DestinationPath $zipPath -CompressionLevel Optimal

Remove-Item -Recurse -Force $staging

$file = Get-Item $zipPath
Write-Host "Successfully generated: $($file.Name) ($([math]::Round($file.Length / 1MB, 2)) MB)" -ForegroundColor Green
