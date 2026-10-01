# Developer Guide

## Prerequisites

- .NET 8 SDK
- Docker Desktop (for PostgreSQL, Redis, Mailpit)

## Running Locally

1. **Start Infrastructure**:
   ```bash
   docker compose up -d
   ```

2. **Run Web API**:
   ```powershell
   Stop-Process -Name "Bookline.Api" -Force -ErrorAction SilentlyContinue
   dotnet run -c Release --project src/Bookline.Api
   ```

3. **Run Test Suite**:
   ```powershell
   dotnet test -c Release
   ```

## Demo Tenant Credentials

- **Public Slug**: `acme-salon`
- **Demo Owner Email**: `demo@bookline.local`
- **Demo Owner Password**: `BooklineDemo123!`
