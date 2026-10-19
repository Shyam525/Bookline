# Bookline Demo Environment & Deterministic Seeding Specification (Sections 115–117)

## 1. Overview and Environment Gating Policy

Bookline includes a comprehensive, deterministic, idempotent, and non-destructive development seed mechanism implemented in `MarketplaceDbSeeder.cs`.

### Safety Guarantees:
- **Environment-Gated**: Demo seeding is strictly prohibited in the `Production` environment. If `IHostEnvironment.IsProduction()` evaluates to `true` or `environmentName == "Production"`, `MarketplaceDbSeeder.SeedAsync` immediately throws an `InvalidOperationException`:
  ```csharp
  if (string.Equals(environmentName, "Production", StringComparison.OrdinalIgnoreCase))
  {
      throw new InvalidOperationException("Demo seeding is prohibited in the Production environment.");
  }
  ```
- **Deterministic**: All demo fixtures use fixed, predictable GUID namespaces (e.g. `10000000-...` for Platform Admins, `20000000-...` for Customers, `30000000-...` for Providers/Tenants, `40000000-...` for Locations, `60000000-...` for Categories, `70000000-...` for Products, and `80000000-...` for Bookings).
- **Idempotent**: Seed operations employ safe existence checks and upsert patterns. Re-running the seed arbitrarily many times produces no duplicate businesses, locations, services, staff, appointments, products, or demo users.
- **Non-Destructive**: Normal demo seeding never uses `TRUNCATE`, `DROP`, or `DELETE`. Real application and customer records are completely preserved.

---

## 2. Standard Development Personas & Demo Accounts (Section 116)

The following synthetic development personas are seeded strictly for local development and QA validation:

| Persona | Email | Default Local Password | Role / Access Area | Authorized Tenant Scope |
|---|---|---|---|---|
| **Platform Administrator** | `admin@bookline.local` | `Admin123!` | `PlatformAdmin` (`/admin/*`) | Platform-wide administration and audit logs |
| **Service Provider Owner** | `provider@bookline.local` | `Provider123!` | `Owner` (`/provider/*`) | Aura Wellness Sanctuary (`aura-wellness`) |
| **Marketplace Customer** | `customer@bookline.local` | `Customer123!` | `Customer` (`/account/*`) | Private customer bookings and orders only |

> [!IMPORTANT]
> - Passwords can be overridden locally via the environment variable `BOOKLINE_DEMO_PASSWORD`.
> - Demo credentials are never committed as production secrets, and the identity system uses Argon2id/ASP.NET Core Identity password hashing (no plaintext storage).

---

## 3. Geographic Locations & Deliberate Coordinates (Section 115)

Bookline seeds realistic providers across the **5 required target cities** with deliberate, verified geographic coordinates:

| City | State | Verified Coordinates (Lat, Lng) | Seeded Provider Tenant / Branch |
|---|---|---|---|
| **Ahmedabad** | Gujarat | `23.0225, 72.5714` | Aura Wellness Sanctuary (Main Branch) |
| **Rajkot** | Gujarat | `22.3039, 70.8022` | Krystal Care Clinic |
| **Surat** | Gujarat | `21.1702, 72.8311` | Velvet Cuts Barbershop |
| **Mumbai** | Maharashtra | `19.0596, 72.8295` | Verdant Sanctuary & Longevity Suite (Bandra West), Aura BKC, Zenith Bandra |
| **Bangalore** | Karnataka | `12.9716, 77.5946` | Zenith Fitness & Movement Lab (Indiranagar) |

---

## 4. Supported Marketplace Categories (Section 115)

The seeder initializes all 8 canonical marketplace categories:

1. **Beauty** (`beauty`) - Hair styling, coloring, manicure, and aesthetic treatments.
2. **Healthcare** (`healthcare`) - Physiotherapy, dental checkups, and wellness consults.
3. **Fitness** (`fitness`) - Personal training, Pilates, and functional movement classes.
4. **Wellness** (`wellness`) - Deep tissue massages, hydrotherapy, and holistic therapies.
5. **Consulting** (`consulting`) - Business consulting, life coaching, and advisory sessions.
6. **Photography** (`photography`) - Studio portraits, editorial shoots, and commercial headshots.
7. **Education** (`education`) - Music lessons, academic tutoring, and skill workshops.
8. **Home Services** (`home-services`) - Interior decor consulting and home styling.

---

## 5. Seed Execution & CLI Commands

### Local Development Seeding
When running the Web API locally in the `Development` environment:
```powershell
# From backend directory
dotnet run --project src/Bookline.Api
```
The application will automatically detect the `Development` environment and execute `MarketplaceDbSeeder.SeedAsync` on startup.

### Verifying Seeder Idempotency in Unit Tests
```powershell
dotnet test --filter "FullyQualifiedName~MarketplaceDbSeeder"
```
Tests assert:
1. Production environment throws `InvalidOperationException`.
2. Running the seeder twice does not duplicate records.
3. All 5 target cities are present with deliberate coordinates.
4. All 3 demo identities exist with correct roles and tenant memberships.
