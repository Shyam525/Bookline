/**
 * Geographic validation and distance display formatting utilities.
 */

export function validateCoordinates(lat?: number | null, lng?: number | null): boolean {
  if (lat == null || lng == null || isNaN(lat) || isNaN(lng) || !isFinite(lat) || !isFinite(lng)) {
    return false;
  }
  return lat >= -90 && lat <= 90 && lng >= -180 && lng <= 180;
}

export function validateRadius(radiusKm?: number | null, minKm = 0.1, maxKm = 500): boolean {
  if (radiusKm == null || isNaN(radiusKm) || !isFinite(radiusKm)) {
    return false;
  }
  return radiusKm >= minKm && radiusKm <= maxKm;
}

export function validateBoundingBox(swLat: number, swLng: number, neLat: number, neLng: number): boolean {
  if (!validateCoordinates(swLat, swLng) || !validateCoordinates(neLat, neLng)) {
    return false;
  }
  if (swLat > neLat) {
    return false;
  }
  return true;
}

/**
 * Format distance for display:
 * - < 1 km: formatted in meters (e.g. "450m")
 * - >= 1 km: formatted in kilometers (e.g. "2.4 km")
 * - null/NaN: returns null
 */
export function formatDistanceKm(distanceKm?: number | null): string | null {
  if (distanceKm == null || isNaN(distanceKm) || !isFinite(distanceKm) || distanceKm < 0) {
    return null;
  }
  if (distanceKm < 1) {
    return `${Math.round(distanceKm * 1000)}m`;
  }
  return `${distanceKm.toFixed(1)} km`;
}
