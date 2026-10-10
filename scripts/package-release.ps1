# Bookline Production Release Packaging Script (Section 156)
param(
    [string]$SourceDir = "c:\z-projects\Bookline",
    [string]$ZipOutput = "c:\z-projects\Bookline\BOOKLINE-final.zip",
    [string]$ExtractTestDir = "c:\z-projects\Bookline\scratch\verify-extract"
)

Write-Host "===> Packaging Bookline Production Release <===" -ForegroundColor Cyan

# 1. Clean old zip if exists
if (Test-Path $ZipOutput) {
    Remove-Item $ZipOutput -Force
    Write-Host "Removed previous $ZipOutput" -ForegroundColor Gray
}

# 2. Exclude patterns
$excludePatterns = @(
    "\\node_modules",
    "\\bin\\",
    "\\bin$",
    "\\obj\\",
    "\\obj$",
    "\\.git",
    "\\.vs",
    "\\.gemini",
    "\\dist\\",
    "\\dist$",
    "\\.vite",
    "BOOKLINE-final\.zip",
    "\\scratch",
    "\\verify-extract"
)

$sourcePath = (Resolve-Path $SourceDir).Path

# Gather items to compress
$allFiles = Get-ChildItem -Path $sourcePath -Recurse -File | Where-Object {
    $filePath = $_.FullName
    $shouldExclude = $false
    foreach ($pattern in $excludePatterns) {
        if ($filePath -match $pattern) {
            $shouldExclude = $true
            break
        }
    }
    -not $shouldExclude
}

Write-Host "Found $($allFiles.Count) pristine source files to archive." -ForegroundColor Green

# 3. Create zip using System.IO.Compression.ZipFile
Add-Type -AssemblyName System.IO.Compression.FileSystem

$tempDir = Join-Path $env:TEMP ([System.Guid]::NewGuid().ToString())
New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

try {
    foreach ($file in $allFiles) {
        $relPath = $file.FullName.Substring($sourcePath.Length).TrimStart('\', '/')
        $targetFile = Join-Path $tempDir $relPath
        $targetFolder = [System.IO.Path]::GetDirectoryName($targetFile)
        if (-not (Test-Path $targetFolder)) {
            New-Item -ItemType Directory -Path $targetFolder -Force | Out-Null
        }
        Copy-Item -Path $file.FullName -Destination $targetFile -Force
    }

    [System.IO.Compression.ZipFile]::CreateFromDirectory($tempDir, $ZipOutput, [System.IO.Compression.CompressionLevel]::Optimal, $false)
    $zipSize = (Get-Item $ZipOutput).Length / 1MB
    Write-Host "Created $ZipOutput ($([Math]::Round($zipSize, 2)) MB)" -ForegroundColor Green

    # 4. Extract to verify
    if (Test-Path $ExtractTestDir) {
        Remove-Item $ExtractTestDir -Recurse -Force
    }
    New-Item -ItemType Directory -Path $ExtractTestDir -Force | Out-Null
    [System.IO.Compression.ZipFile]::ExtractToDirectory($ZipOutput, $ExtractTestDir)
    Write-Host "Extracted $ZipOutput to $ExtractTestDir successfully." -ForegroundColor Green

    $extractedCount = (Get-ChildItem -Path $ExtractTestDir -Recurse -File).Count
    Write-Host "Verification: $extractedCount files extracted cleanly." -ForegroundColor Cyan
}
finally {
    if (Test-Path $tempDir) {
        Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}
