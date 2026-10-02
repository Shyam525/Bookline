# BOOKLINE
## Elite Production-Grade Appointment & Scheduling Platform

Bookline is a serious, high-concurrency SaaS platform for salons, clinics, spas, barbershops, and appointment-based businesses.

---

## 🏛️ Architecture Overview

- **Backend**: ASP.NET Core 8, Entity Framework Core 8, PostgreSQL 16, Redis 7, NodaTime 3.x
- **Frontend**: React 18, TypeScript, Vite, React Router, TanStack Query, React Hook Form, Zod, Tailwind CSS
- **Testing**: xUnit, FluentAssertions, Integration Tests, Architecture Tests
- **Infrastructure**: Docker, Docker Compose, PostgreSQL, Redis, Mailpit

---

## 🚀 Quick Start

### 1. Start Infrastructure & Full Application
```bash
docker compose up --build -d
```

### 2. Local Development (API + Frontend)
Backend:
```powershell
dotnet run --project backend/src/Bookline.Api
```

Frontend:
```powershell
cd frontend
npm run dev
```

---

## 🔗 Endpoints

- **Web Application**: `http://localhost:3000` (or `http://localhost:5168` when self-hosted)
- **API Health**: `http://localhost:5168/health`
- **API Readiness**: `http://localhost:5168/ready`
- **Swagger UI**: `http://localhost:5168/swagger`
- **Mailpit Email Inbox**: `http://localhost:8026`

---

## 🧪 Testing

Run backend test suite:
```powershell
dotnet test backend/Bookline.slnx -c Release
```

Run frontend typecheck and build:
```powershell
cd frontend
npm run build
```
