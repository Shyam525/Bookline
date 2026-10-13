# PostGIS & PostgreSQL Spatial Storage Architecture

## 1. Overview
Bookline utilizes PostgreSQL with the **PostGIS** extension as its authoritative spatial database. While development and isolated unit testing run on in-memory / SQLite engines using Haversine mathematics, production environments leverage native spatial types and index-accelerated spatial calculations.

---

## 2. Geography vs. Geometry Types
Bookline standardizes on the **`geography`** type:
- **SRID 4326 (WGS 84)**: Direct representation of coordinates on the curved surface of the earth (spheroid).
- **Unit of Measurement**: Native calculations are executed in **meters**, avoiding planar projection distortion across disparate longitudinal bands.
- **Why not `geometry`?**: Planar `geometry(Point, 4326)` treats degrees as Cartesian units, leading to significant distance errors at varying latitudes (e.g., $1^\circ$ longitude at the equator $\approx 111\text{ km}$, while at $60^\circ$ latitude $\approx 55\text{ km}$).

---

## 3. Schema Design & Spatial Columns

### Entity Columns
```sql
ALTER TABLE "Tenants"
  ADD COLUMN IF NOT EXISTS "Latitude" double precision,
  ADD COLUMN IF NOT EXISTS "Longitude" double precision,
  ADD COLUMN IF NOT EXISTS "LocationPoint" geography(Point, 4326);

ALTER TABLE "Locations"
  ADD COLUMN IF NOT EXISTS "Latitude" double precision,
  ADD COLUMN IF NOT EXISTS "Longitude" double precision,
  ADD COLUMN IF NOT EXISTS "LocationPoint" geography(Point, 4326);
```

### Automatic Spatial Synchronization
A PostgreSQL trigger or EF Core value generator maintains `LocationPoint` in sync with numeric coordinates:
```sql
CREATE OR REPLACE FUNCTION sync_location_point()
RETURNS TRIGGER AS $$
BEGIN
  IF NEW."Latitude" IS NOT NULL AND NEW."Longitude" IS NOT NULL THEN
    NEW."LocationPoint" := ST_SetSRID(ST_MakePoint(NEW."Longitude", NEW."Latitude"), 4326)::geography;
  ELSE
    NEW."LocationPoint" := NULL;
  END IF;
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_sync_location_point_tenants
BEFORE INSERT OR UPDATE ON "Tenants"
FOR EACH ROW EXECUTE FUNCTION sync_location_point();
```

---

## 4. Spatial Indexing
To support sub-10ms spatial discovery across hundreds of thousands of venues, GiST (Generalized Search Tree) indexes are established:
```sql
CREATE INDEX idx_tenants_locationpoint_gist
ON "Tenants" USING GIST ("LocationPoint");

CREATE INDEX idx_locations_locationpoint_gist
ON "Locations" USING GIST ("LocationPoint");
```

---

## 5. Query Patterns

### 1. Viewport Bounding Box Query
```sql
SELECT t."Id", t."Name", t."Latitude", t."Longitude"
FROM "Tenants" t
WHERE t."LocationPoint" && ST_MakeEnvelope(
    :swLng, :swLat,
    :neLng, :neLat,
    4326
)::geography
  AND t."IsActive" = TRUE
  AND t."IsPublished" = TRUE;
```

### 2. Radius Query with ST_DWithin
```sql
SELECT t."Id", t."Name",
       ST_Distance(t."LocationPoint", ST_SetSRID(ST_MakePoint(:userLng, :userLat), 4326)::geography) / 1000.0 AS "DistanceKm"
FROM "Tenants" t
WHERE ST_DWithin(
    t."LocationPoint",
    ST_SetSRID(ST_MakePoint(:userLng, :userLat), 4326)::geography,
    :radiusMeters
)
ORDER BY "DistanceKm" ASC;
```

---

## 6. EF Core Mapping
In `BooklineDbContext.cs`, Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite provides seamless C# object integration:
```csharp
// Configured in Infrastructure
builder.Entity<Location>(b =>
{
    b.Property(l => l.Latitude).HasPrecision(9, 6);
    b.Property(l => l.Longitude).HasPrecision(9, 6);
});
```

---

## 7. Migration & Local Setup
To run migrations locally with PostGIS:
```bash
docker run -d --name bookline-postgis \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=bookline \
  -p 5432:5432 \
  postgis/postgis:16-3.4
```
Ensure the extension is initialized:
```sql
CREATE EXTENSION IF NOT EXISTS postgis;
```
