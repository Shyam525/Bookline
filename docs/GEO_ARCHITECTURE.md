# Geo & Spatial Architecture

## 1. Overview
The Geo subsystem provides spatial discovery, Haversine distance calculations, PostGIS bounding box filtering, and intent-aware neighborhood search.

## 2. Nine Geo Specification Capabilities (Section 131)
Bookline's geo engine is thoroughly verified across all 9 scenarios (`GeoSearchSpecificationTests.cs`):

1. **Current Location Granted**:
   - Browser GPS coordinates (`Latitude`, `Longitude`) calculate accurate `DistanceKm` for all venues.
2. **Current Location Denied**:
   - If user denies permission, discovery operates gracefully without error. `DistanceKm` is null, defaulting to city or recommended ranking.
3. **Manual City**:
   - User selection of manual cities (*Ahmedabad, Rajkot, Surat, Mumbai, Bangalore*) filters results strictly to that metropolitan area.
4. **Manual Area / Neighborhood**:
   - Queries targeting specific neighborhoods (*Bodakdev, Satellite, Bandra, Indiranagar*) resolve via `ISearchIntentService` and address matching.
5. **Radius Filtering**:
   - Distance thresholding (`RadiusKm = 5.0`) excludes venues outside the user's travel tolerance.
6. **Bounding Box Viewport Query**:
   - Spatial bounds (`SwLat`, `SwLng`, `NeLat`, `NeLng`) scope venues strictly to the interactive map viewport.
7. **Nearest Sorting**:
   - Sorts providers by `DistanceKm` ascending (closest venue first).
8. **Map Movement**:
   - Panning or zooming the map updates bounding box coordinates and recalculates candidate venues dynamically.
9. **Search This Area**:
   - Syncs map center coordinates and viewport bounds to refresh results centered on the user's focus area.
