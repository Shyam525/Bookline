# Bookline — Database Migration Procedure

This guide documents the procedures for authoring, reviewing, testing, and executing PostgreSQL database migrations in the Bookline platform using Entity Framework Core.

---

## 1. PostgreSQL Schema & Extensions

Bookline requires the following PostgreSQL extensions enabled on the target database before executing schema migrations:

```sql
-- Required for spatial indexing and proximity queries
CREATE EXTENSION IF NOT EXISTS postgis;

-- Required for GiST multi-column exclusion constraints (overlapping appointment protection)
CREATE EXTENSION IF NOT EXISTS btree_gist;

-- Required for UUID v4 generation functions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
```

---

## 2. Migration Architecture

- **Project Location**: `backend/src/Bookline.Infrastructure`
- **DbContext**: `BooklineDbContext`
- **Snapshot File**: `backend/src/Bookline.Infrastructure/Persistence/Migrations/BooklineDbContextModelSnapshot.cs`

### Strict Zero-Downtime Migration Principles
1. **Never drop columns directly in production**: Perform destructive schema changes across two releases (Phase 1: Mark column obsolete / write-only; Phase 2: Drop column after all application pods are updated).
2. **Always provide default values for new non-null columns**: Use EF Core `.HasDefaultValue(...)` or `.IsRequired(false)`.
3. **Add indexes concurrently in large tables**: For large production tables (>100k rows), modify generated SQL to include `CREATE INDEX CONCURRENTLY`.

---

## 3. Step-by-Step Migration Execution

### A. Development (Creating New Migrations)
From the repository root or `backend/` folder:

```powershell
# Navigate to backend directory
cd backend

# Add a new migration (Name must use PascalCase and describe business intent)
dotnet ef migrations add AddCompositeIndexToOutbox `
  --project src/Bookline.Infrastructure `
  --startup-project src/Bookline.Api `
  --output-dir Persistence/Migrations
```

### B. Generating Idempotent SQL Migration Script (For Staging & Production)
Never allow application containers to run automatic `db.Database.Migrate()` on startup in multi-pod production environments. Generate an idempotent SQL script instead:

```powershell
# Generate idempotent SQL script spanning all unapplied migrations
dotnet ef migrations script `
  --idempotent `
  --project src/Bookline.Infrastructure `
  --startup-project src/Bookline.Api `
  --output ./migrations/release_bundle.sql
```

Review the generated `release_bundle.sql` before execution to verify:
- No table locks on critical operational tables (`Bookings`, `TimeSlots`, `OutboxMessages`).
- Index additions contain correct column ordering.

### C. Applying Migrations to Target Database
Execute the generated script using `psql` within the maintenance window:

```bash
# Execute against production database with transaction safety
psql "$DATABASE_URL" -v ON_ERROR_STOP=1 -f ./migrations/release_bundle.sql
```

Verify migration history:
```sql
SELECT "MigrationId", "ProductVersion" FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC LIMIT 5;
```

---

## 4. Exclusion Constraint for Overlap Protection

Bookline implements database-level exclusion constraints to guarantee zero overlapping appointments for the same staff member:

```sql
-- Verified GiST exclusion constraint on Bookings table
ALTER TABLE "Bookings"
ADD CONSTRAINT "no_overlapping_staff_bookings"
EXCLUDE USING gist (
  "StaffId" WITH =,
  tstzrange("StartTimeUtc", "EndTimeUtc") WITH &&
)
WHERE ("Status" NOT IN ('Cancelled', 'NoShow'));
```

When validating new migrations, verify that the exclusion constraint remains intact:
```sql
SELECT conname, contype, pg_get_constraintdef(c.oid) 
FROM pg_constraint c 
JOIN pg_class t ON c.conrelid = t.oid 
WHERE t.relname = 'Bookings' AND conname = 'no_overlapping_staff_bookings';
```

---

## 5. Rollback Procedure

If a migration fails or causes application regression:

1. **Rollback via EF Core Script**:
   Generate a rollback script targeting the last known good migration:
   ```powershell
   dotnet ef migrations script <FailedMigrationName> <LastGoodMigrationName> `
     --project src/Bookline.Infrastructure `
     --startup-project src/Bookline.Api `
     --output ./migrations/rollback.sql
   ```

2. **Execute Rollback**:
   ```bash
   psql "$DATABASE_URL" -v ON_ERROR_STOP=1 -f ./migrations/rollback.sql
   ```

3. **Verify Table Consistency**:
   ```sql
   SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC LIMIT 1;
   ```

---

## 6. Post-Migration Verification Query Checklist

Run the following sanity checks immediately after applying migrations:

```sql
-- 1. Check all tables exist and have valid row counts
SELECT schemaname, relname, n_live_tup 
FROM pg_stat_user_tables 
ORDER BY relname;

-- 2. Check foreign key integrity
SELECT conname, conrelid::regclass, confrelid::regclass 
FROM pg_constraint 
WHERE contype = 'f' AND NOT convalidated;

-- 3. Check outbox index presence
SELECT indexname, indexdef 
FROM pg_indexes 
WHERE tablename = 'OutboxMessages';
```

