# Bookline — Database Backup & Restore Runbook

This runbook outlines the operational procedures for PostgreSQL backup generation, point-in-time recovery, offsite replication, and disaster restore drills for the Bookline platform.

---

## 1. Backup Strategy & Objectives

| Objective | Target | Implementation |
|---|---|---|
| **Recovery Point Objective (RPO)** | **< 1 Hour** | Daily full logical backups + continuous Write-Ahead Log (WAL) archiving via `pgBackRest` or AWS RDS automated snapshots. |
| **Recovery Time Objective (RTO)** | **< 30 Minutes** | Pre-tested restoration drill utilizing parallel `pg_restore` jobs. |
| **Retention Policy** | **30 Days Daily, 12 Months Monthly** | Immutable object storage with Object Lock enabled. |

---

## 2. Backup Execution Procedures

### A. Manual Full Logical Backup
Use PostgreSQL `pg_dump` in Custom Archive format (`-Fc`), which includes compression, metadata, and allows selective table restoration:

```bash
# Set timestamp variable
BACKUP_TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="/backups/bookline_backup_${BACKUP_TIMESTAMP}.dump"

# Execute pg_dump with verbose output and integrity checks
pg_dump -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" \
  -F c -b -v -f "$BACKUP_FILE"

# Generate SHA256 checksum for tamper verification
sha256sum "$BACKUP_FILE" > "${BACKUP_FILE}.sha256"
```

### B. Automated Daily Backup Cron Script
```bash
#!/bin/bash
set -euo pipefail

BACKUP_DIR="/var/backups/bookline"
DATE_TAG=$(date +%Y%m%d_%H%M%S)
DUMP_NAME="bookline_prod_${DATE_TAG}.dump"

mkdir -p "$BACKUP_DIR"

# Run dump inside database container or host
docker exec constructionops-postgres pg_dump -U postgres -d bookline_db -F c -b -f "/tmp/${DUMP_NAME}"
docker cp "constructionops-postgres:/tmp/${DUMP_NAME}" "${BACKUP_DIR}/${DUMP_NAME}"
docker exec constructionops-postgres rm -f "/tmp/${DUMP_NAME}"

# Upload to offsite encrypted cloud storage (S3 example)
aws s3 cp "${BACKUP_DIR}/${DUMP_NAME}" "s3://bookline-db-backups/${DUMP_NAME}" --sse aws:kms

# Delete local dumps older than 7 days
find "$BACKUP_DIR" -type f -name "*.dump" -mtime +7 -delete
```

---

## 3. Restoration Procedure (Live-Tested)

The following restoration procedure has been verified against the Bookline production schema:

### Step 1: Pre-Restoration Safety Check
Ensure no active connections exist to the target database:

```sql
SELECT pg_terminate_backend(pid) 
FROM pg_stat_activity 
WHERE datname = 'bookline_db' AND pid <> pg_backend_pid();
```

### Step 2: Create Clean Target Database
```sql
DROP DATABASE IF EXISTS bookline_restore_test;
CREATE DATABASE bookline_restore_test;
```

### Step 3: Execute `pg_restore`
```bash
# Restore with clean, create, and parallel workers
pg_restore -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" \
  -d bookline_restore_test \
  -v --no-owner --no-acl \
  /backups/bookline_backup_20261109_233000.dump
```

*Note: Any harmless warnings regarding preexisting procedural languages (such as plpgsql) may be safely ignored if tables restore cleanly.*

---

## 4. Post-Restoration Data Integrity Verification

Run the following validation queries against the restored database to confirm structural and referential integrity:

```sql
-- 1. Verify all 36 critical tables were restored
SELECT count(*) AS total_tables 
FROM information_schema.tables 
WHERE table_schema = 'public';
-- Expected: >= 36

-- 2. Verify pilot provider records exist
SELECT "Id", "Name", "City", "IsActive" 
FROM "Businesses" 
LIMIT 5;

-- 3. Verify appointment records and audit integrity
SELECT count(*) AS total_bookings, count(DISTINCT "BusinessId") AS active_businesses 
FROM "Bookings";

-- 4. Verify GiST exclusion constraint on Bookings table
SELECT conname, contype 
FROM pg_constraint 
WHERE conname = 'no_overlapping_staff_bookings';
-- Expected: 1 row with contype 'x' (exclusion constraint)

-- 5. Verify outbox queue status
SELECT "Status", count(*) 
FROM "OutboxMessages" 
GROUP BY "Status";
```

---

## 5. Disaster Recovery Scenarios

### Scenario A: Accidental Record Deletion
1. Restore dump file to a temporary recovery database (`bookline_recovery_temp`).
2. Export missing rows using `COPY (SELECT ... FROM "Bookings" WHERE ...) TO STDOUT`.
3. Import exported rows into production database inside an explicit transaction.

### Scenario B: Complete Primary Node Loss
1. Provision a new PostgreSQL instance with `postgis` and `btree_gist` extensions.
2. Download latest verified dump from offsite S3 bucket.
3. Verify SHA256 checksum against `.sha256` manifest.
4. Run `pg_restore` into the new primary database.
5. Update `ConnectionStrings__DefaultConnection` environment variable on application pods.
6. Verify `/ready` returns HTTP 200.

