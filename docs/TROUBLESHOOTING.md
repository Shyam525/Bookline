# Troubleshooting & Operational Guide

## 1. Windows Application Control / Smart App Control (0x800711C7)
### Symptom
When running `dotnet test`, tests in some test projects may output:
```text
An Application Control policy has blocked this file. (0x800711C7)
```
### Cause
Windows 11 Smart App Control (SAC) blocks newly compiled, unsigned DLLs in local directories until they are unblocked or granted reputation.

### Solution
1. Ensure the `<Target Name="UnblockOutputDlls" AfterTargets="Build">` hook is present in all `.csproj` files:
   ```xml
   <Target Name="UnblockOutputDlls" AfterTargets="Build">
     <Exec Command="powershell -Command &quot;Get-ChildItem -Path '$(TargetDir)*.dll' | Unblock-File&quot;" />
   </Target>
   ```
2. Unblock all compiled assemblies manually before test runs:
   ```powershell
   Get-ChildItem -Path 'backend' -Recurse -Filter '*.dll' | Unblock-File
   ```

---

## 2. Port Conflicts in Docker Compose
### Symptom
`docker compose up --build` fails with `port is already allocated` on 5432 or 6379.

### Solution
1. Stop any locally running PostgreSQL or Redis instances:
   ```powershell
   Stop-Service postgresql* -ErrorAction SilentlyContinue
   ```
2. Verify assigned ports in `docker-compose.yml`:
   - `postgres`: `5432:5432`
   - `redis`: `6379:6379`
   - `mailpit`: `8026:8025` (Web UI), `1026:1025` (SMTP)
   - `bookline-api`: `5168:8080`
   - `bookline-web`: `3000:80`

---

## 3. Redis Coordination Fallback
If Redis is temporarily down or not started, Bookline API does not crash. It automatically logs a warning and falls back to its in-memory hold and coordination engine, allowing local development and testing to continue uninterrupted.

---

## 4. Database Reset & Reseeding
To wipe and reseed the deterministic demo dataset:
```bash
docker compose down -v
docker compose up --build
```
On boot, `MarketplaceDbSeeder` runs automatically to seed all 8 categories across the 5 canonical cities, demo accounts, mixed appointment and order states, and reviews labeled `[Demo Seeded]`.
