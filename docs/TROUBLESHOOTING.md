# Troubleshooting & Operations Guide

## Common Issues & Solutions

### 1. Port 5168 / Process Lock Error
If `dotnet run` or `dotnet build` fails because `Bookline.Api` is holding a process lock:
```powershell
Stop-Process -Name "Bookline.Api" -Force -ErrorAction SilentlyContinue
```

### 2. AppLocker DLL Execution Error (0x800711C7)
On Windows machines with strict AppLocker policies, always run in Release mode:
```powershell
dotnet run -c Release --project src/Bookline.Api
dotnet test -c Release
```

### 3. PostgreSQL Database Reset
To drop and re-seed the local PostgreSQL database:
```powershell
./scripts/reset-db.ps1
```
