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

## Standard Development Demo Accounts (Section 116)

The local demo environment is pre-seeded with synthetic personas for each product area (see [DEMO_ENVIRONMENT.md](file:///c:/z-projects/Bookline/docs/DEMO_ENVIRONMENT.md) for complete details):

- **Platform Admin**:
  - Email: `admin@bookline.local`
  - Password: `Admin123!`
  - Area: `/admin/*`
- **Provider Owner**:
  - Email: `provider@bookline.local`
  - Password: `Provider123!`
  - Area: `/provider/*` (Tenant: `aura-wellness`)
- **Customer**:
  - Email: `customer@bookline.local`
  - Password: `Customer123!`
  - Area: `/account/*`

