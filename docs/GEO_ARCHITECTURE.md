# GEOLOCATION & POSTGIS SPATIAL ARCHITECTURE

## 1. Geolocation Philosophy & Privacy
- **Transient Location**: Browser geolocation coordinates (`lat`/`lng`) are used transiently during discovery and map navigation. Bookline **never** permanently stores raw transient client coordinates.
- **Permission Lifecycle**:
  - `LOCATION_UNKNOWN`: Initial state prior to user gesture.
  - `LOCATION_REQUESTING`: Browser permission dialog prompted.
  - `LOCATION_GRANTED`: Exact lat/lng used for reverse geocoding and radius sorting.
  - `LOCATION_DENIED` / `LOCATION_UNAVAILABLE`: Graceful fallback to city search, area autocomplete, or postal code.

## 2. PostGIS Integration
- **Column**: Spatial geography/geometry on `Locations` and `Tenants`.
- **Indices**: Spatial GIST indices ensure sub-millisecond bounding box and radius queries.
- **Query Types**:
  - Radius searches (`ST_DWithin`)
  - Bounding box viewport queries for "Search this area" on the map.
  - Nearest neighbor calculations (`ST_Distance`).
- **Offline / Local Fallback**: Deterministic Haversine distance calculations and city centroid geocoding for environments where PostGIS extensions are disabled or external map APIs are unconfigured.

## 3. Interactive Vector Map
- **Abstraction**: `IMapProvider` decoupling vendor SDKs (Mapbox / Google Maps / OpenStreetMap).
- **Graceful Map Degradation**: In the event of tile network failure or missing API keys, discovery remains 100% operational with distance calculations, city filters, and booking flows unimpaired.
