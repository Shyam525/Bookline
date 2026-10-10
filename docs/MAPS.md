# Interactive Maps & Viewport Synchronization

## 1. Overview
The Bookline map system (`InteractiveMap.tsx` and `DiscoveryIndex.tsx`) provides bidirectional synchronization between spatial map viewports and the discovery venue cards list.

## 2. Interactive Map Capabilities
- **Leaflet & OpenStreetMap Integration**: High-contrast, dark-mode cartography matching `#0B0E14` aesthetics.
- **Current User Location Dot**: Pulsing emerald dot showing accurate GPS coordinates when granted.
- **Provider Markers**:
  - Interactive marker pins displaying provider name, rating, and starting price.
  - Hover highlights: Hovering a list card highlights its corresponding marker on the map; hovering a marker previews the venue card.
- **Map Controls**:
  - Zoom in / Zoom out controls.
  - Recenter viewport button.
  - Viewport venue count badge.

## 3. "Search This Area" Viewport Dynamics
- **Movement Detection**: When the user pans or zooms the map beyond a movement threshold, a floating badge appears: `"Search this area"`.
- **Throttling & Viewport Query**:
  - Clicking the badge recalculates the bounding box (`SwLat`, `SwLng`, `NeLat`, `NeLng`) and map center (`lat`, `lng`).
  - Calls backend `IProviderSearchService` with viewport bounds.
  - Bounded results refresh the listing with zero whole-page reload.

## 4. Resilience & Fallbacks (Section 103)
- If geolocation permission is denied: The map falls back to the default city coordinates without error or user interruption.
- If tile server or map rendering is delayed or offline: The venue list remains 100% interactive and functional.
