import React, { useState, useEffect, useRef, useCallback } from 'react';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import { ProviderCard } from '../../services/api/discovery';
import {
  ZoomIn,
  ZoomOut,
  RotateCcw,
  Sparkles,
  MapPin,
  RefreshCw,
  AlertCircle,
  ExternalLink,
  CheckCircle,
  Star,
} from 'lucide-react';
import { useNavigate } from 'react-router-dom';

export interface ViewportSearchBounds {
  swLat: number;
  swLng: number;
  neLat: number;
  neLng: number;
  center: { lat: number; lng: number };
  zoom: number;
}

export interface InteractiveMapProps {
  providers: ProviderCard[];
  selectedProviderId?: string | null;
  hoveredProviderId?: string | null;
  onSelectProvider: (provider: ProviderCard) => void;
  onHoverProvider?: (providerId: string | null) => void;
  onSearchThisArea?: (bounds: ViewportSearchBounds) => void;
  center?: { lat: number; lng: number };
  userLocation?: { lat: number; lng: number } | null;
  zoom?: number;
  className?: string;
}

export const InteractiveMap: React.FC<InteractiveMapProps> = ({
  providers,
  selectedProviderId,
  hoveredProviderId,
  onSelectProvider,
  onHoverProvider,
  onSearchThisArea,
  center = { lat: 23.0225, lng: 72.5714 },
  userLocation = null,
  zoom = 13,
  className = '',
}) => {
  const navigate = useNavigate();
  const mapContainerRef = useRef<HTMLDivElement>(null);
  const mapInstanceRef = useRef<L.Map | null>(null);
  const markersLayerRef = useRef<L.LayerGroup | null>(null);
  const userLocationLayerRef = useRef<L.LayerGroup | null>(null);

  const [isMapReady, setIsMapReady] = useState(false);
  const [mapError, setMapError] = useState<string | null>(null);
  const [isViewportDirty, setIsViewportDirty] = useState(false);
  const [isSearchingArea, setIsSearchingArea] = useState(false);

  const lastQueriedBoundsRef = useRef<{ swLat: number; swLng: number; neLat: number; neLng: number } | null>(null);
  const pendingBoundsRef = useRef<ViewportSearchBounds | null>(null);
  const hasUserMovedRef = useRef(false);

  // Filter valid geographic coordinates (Explicitly excludes 0,0 Null Island defaults)
  const validProviders = providers.filter(
    (p) =>
      p.latitude != null &&
      p.longitude != null &&
      !isNaN(p.latitude) &&
      !isNaN(p.longitude) &&
      p.latitude >= -90 &&
      p.latitude <= 90 &&
      p.longitude >= -180 &&
      p.longitude <= 180 &&
      (Math.abs(p.latitude) > 0.0001 || Math.abs(p.longitude) > 0.0001)
  );

  // Responsive container observer: ensures Leaflet recalculates tile bounds on viewport resize / mobile toggle
  useEffect(() => {
    if (!mapContainerRef.current) return;
    const observer = new ResizeObserver(() => {
      if (mapInstanceRef.current) {
        mapInstanceRef.current.invalidateSize();
      }
    });
    observer.observe(mapContainerRef.current);
    return () => observer.disconnect();
  }, [isMapReady]);

  // 1. Initialize Leaflet Map
  useEffect(() => {
    if (!mapContainerRef.current || mapInstanceRef.current) return;

    try {
      const tileUrl =
        (import.meta as any).env?.VITE_MAP_TILE_URL ||
        'https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}{r}.png';

      const map = L.map(mapContainerRef.current, {
        center: [center.lat, center.lng],
        zoom,
        zoomControl: false,
        attributionControl: true,
      });

      L.tileLayer(tileUrl, {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> &copy; <a href="https://carto.com/">CARTO</a>',
        maxZoom: 19,
        subdomains: 'abcd',
      }).addTo(map);

      const markersGroup = L.layerGroup().addTo(map);
      const userLocGroup = L.layerGroup().addTo(map);

      markersLayerRef.current = markersGroup;
      userLocationLayerRef.current = userLocGroup;
      mapInstanceRef.current = map;

      // Listen to map user movements for "Search this area" (Phase 13)
      map.on('moveend', () => {
        const bounds = map.getBounds();
        const sw = bounds.getSouthWest();
        const ne = bounds.getNorthEast();
        const currentCenter = map.getCenter();
        const currentZoom = map.getZoom();

        const newBounds: ViewportSearchBounds = {
          swLat: sw.lat,
          swLng: sw.lng,
          neLat: ne.lat,
          neLng: ne.lng,
          center: { lat: currentCenter.lat, lng: currentCenter.lng },
          zoom: currentZoom,
        };

        pendingBoundsRef.current = newBounds;

        if (lastQueriedBoundsRef.current) {
          const deltaLat = Math.abs(sw.lat - lastQueriedBoundsRef.current.swLat);
          const deltaLng = Math.abs(sw.lng - lastQueriedBoundsRef.current.swLng);
          // Mark viewport as dirty if panned significantly (> 0.005 deg)
          if (deltaLat > 0.005 || deltaLng > 0.005) {
            hasUserMovedRef.current = true;
            setIsViewportDirty(true);
          }
        } else {
          lastQueriedBoundsRef.current = { swLat: sw.lat, swLng: sw.lng, neLat: ne.lat, neLng: ne.lng };
        }
      });

      setIsMapReady(true);
    } catch (err: any) {
      setMapError('Interactive map initialization failed.');
    }

    return () => {
      if (mapInstanceRef.current) {
        mapInstanceRef.current.remove();
        mapInstanceRef.current = null;
      }
    };
  }, []);

  // 2. Center/Bounds Updates from Parent (City selection or center changes)
  useEffect(() => {
    if (!mapInstanceRef.current || !isMapReady) return;
    if (!hasUserMovedRef.current) {
      mapInstanceRef.current.setView([center.lat, center.lng], zoom, { animate: true });
    }
  }, [center.lat, center.lng, zoom, isMapReady]);

  // 3. User Location Indicator (Phase 12)
  useEffect(() => {
    if (!mapInstanceRef.current || !userLocationLayerRef.current) return;
    userLocationLayerRef.current.clearLayers();

    if (userLocation && !isNaN(userLocation.lat) && !isNaN(userLocation.lng)) {
      const userIcon = L.divIcon({
        className: 'user-gps-pulse',
        html: `
          <div class="relative flex items-center justify-center w-6 h-6">
            <span class="animate-ping absolute inline-flex h-full w-full rounded-full bg-[#158A63] opacity-60"></span>
            <span class="relative inline-flex rounded-full h-3.5 w-3.5 bg-[#158A63] border-2 border-white shadow-md"></span>
          </div>
        `,
        iconSize: [24, 24],
        iconAnchor: [12, 12],
      });

      L.marker([userLocation.lat, userLocation.lng], { icon: userIcon, zIndexOffset: 1000 })
        .addTo(userLocationLayerRef.current);
    }
  }, [userLocation]);

  // 4. Render Provider Markers with Clustering & Stable IDs (Phase 12)
  useEffect(() => {
    if (!mapInstanceRef.current || !markersLayerRef.current || !isMapReady) return;
    const markersGroup = markersLayerRef.current;
    markersGroup.clearLayers();

    // Group close providers into clusters if overlapping at current zoom level
    const clusters: Array<{ items: ProviderCard[]; centerLat: number; centerLng: number }> = [];
    const thresholdDeg = 0.008; // Proximity threshold

    validProviders.forEach((p) => {
      let matchedCluster = clusters.find((c) => {
        return (
          Math.abs(c.centerLat - p.latitude) < thresholdDeg &&
          Math.abs(c.centerLng - p.longitude) < thresholdDeg
        );
      });

      if (matchedCluster) {
        matchedCluster.items.push(p);
      } else {
        clusters.push({
          items: [p],
          centerLat: p.latitude,
          centerLng: p.longitude,
        });
      }
    });

    clusters.forEach((cluster) => {
      if (cluster.items.length === 1) {
        // Individual Provider Pin
        const p = cluster.items[0];
        const isSelected = p.id === selectedProviderId;
        const isHovered = p.id === hoveredProviderId;

        const currencySymbol = p.currency === 'INR' || !p.currency ? '₹' : p.currency === 'USD' ? '$' : p.currency;
        const priceLabel = p.startingPrice != null && p.startingPrice > 0 ? `${currencySymbol}${p.startingPrice}` : '★';

        const markerHtml = `
          <div class="group cursor-pointer transition-transform duration-200 select-none ${
            isSelected ? 'scale-110 z-50' : isHovered ? 'scale-105 z-40' : 'z-20'
          }">
            <div class="px-2.5 py-1 rounded-full text-xs font-bold flex items-center gap-1 shadow-lg transition-all ${
              isSelected
                ? 'bg-[#B93650] text-white ring-4 ring-[#B93650]/40 shadow-xl'
                : isHovered
                ? 'bg-[#E8546A] text-white ring-2 ring-[#E8546A]/30'
                : 'bg-white dark:bg-[#111620] text-[#181A1F] dark:text-[#F4F6FA] border border-[#DED8CF] dark:border-[#30394A]'
            }">
              <span>${priceLabel}</span>
              ${p.isVerified ? '<span class="text-[#158A63] dark:text-[#34D399]">✓</span>' : ''}
            </div>
            <div class="w-2 h-2 rotate-45 mx-auto -mt-1 ${
              isSelected ? 'bg-[#B93650]' : isHovered ? 'bg-[#E8546A]' : 'bg-white dark:bg-[#111620]'
            }"></div>
          </div>
        `;

        const icon = L.divIcon({
          className: 'provider-map-pin',
          html: markerHtml,
          iconSize: [60, 30],
          iconAnchor: [30, 30],
        });

        const marker = L.marker([p.latitude, p.longitude], {
          icon,
          zIndexOffset: isSelected ? 500 : isHovered ? 400 : 100,
        }).addTo(markersGroup);

        // Marker click: Synchronize with list card and show popup preview (Phase 12)
        marker.on('click', () => {
          onSelectProvider(p);
        });

        marker.on('mouseover', () => {
          onHoverProvider?.(p.id);
        });

        marker.on('mouseout', () => {
          onHoverProvider?.(null);
        });

        // Attach rich popup
        const locationArea = p.address ? p.address.split(',')[0].trim() : p.city;
        const popupContent = document.createElement('div');
        popupContent.className = 'p-3 space-y-2 min-w-[180px] text-xs font-sans';
        popupContent.innerHTML = `
          <div class="font-bold text-sm text-[#181A1F]">${p.name}</div>
          <div class="text-[#626B7A] text-[11px]">${p.category} &bull; ${locationArea}</div>
          ${p.rating > 0 ? `<div class="font-semibold text-[#A66B00]">★ ${p.rating.toFixed(1)} (${p.reviewCount} reviews)</div>` : ''}
          <div class="pt-2 border-t border-gray-200 flex justify-between items-center">
            <span class="font-bold text-[#181A1F]">${p.startingPrice > 0 ? `From ${currencySymbol}${p.startingPrice}` : 'Custom'}</span>
            <button id="view-storefront-${p.id}" class="px-2 py-1 bg-[#B93650] text-white rounded text-[11px] font-bold hover:bg-[#A52E46]">View</button>
          </div>
        `;

        const viewBtn = popupContent.querySelector(`#view-storefront-${p.id}`);
        if (viewBtn) {
          viewBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            navigate(`/business/${p.slug}`);
          });
        }

        marker.bindPopup(popupContent, { offset: [0, -25] });

        // Open popup if card is actively selected
        if (isSelected) {
          marker.openPopup();
        }
      } else {
        // Multi-provider Cluster Pin (Phase 12 Clustering)
        const clusterHtml = `
          <div class="cursor-pointer group select-none transition-transform hover:scale-110">
            <div class="px-3 py-1.5 rounded-full bg-[#B93650] hover:bg-[#A52E46] text-white font-bold text-xs flex items-center gap-1 shadow-xl ring-4 ring-[#B93650]/30">
              <span>${cluster.items.length} venues</span>
            </div>
          </div>
        `;

        const clusterIcon = L.divIcon({
          className: 'provider-map-cluster',
          html: clusterHtml,
          iconSize: [80, 30],
          iconAnchor: [40, 15],
        });

        const clusterMarker = L.marker([cluster.centerLat, cluster.centerLng], {
          icon: clusterIcon,
          zIndexOffset: 300,
        }).addTo(markersGroup);

        clusterMarker.on('click', () => {
          if (mapInstanceRef.current) {
            const bounds = L.latLngBounds(cluster.items.map((i) => [i.latitude, i.longitude]));
            mapInstanceRef.current.fitBounds(bounds, { padding: [50, 50], maxZoom: 16 });
          }
        });
      }
    });

    // Auto fit viewport on initial load if user hasn't panned
    if (!hasUserMovedRef.current && validProviders.length > 0 && mapInstanceRef.current) {
      const bounds = L.latLngBounds(validProviders.map((p) => [p.latitude, p.longitude]));
      mapInstanceRef.current.fitBounds(bounds, { padding: [40, 40], maxZoom: 15 });
    }
  }, [validProviders, selectedProviderId, hoveredProviderId, isMapReady]);

  // Handle "Search this area" action click (Phase 13)
  const handleSearchThisAreaClick = useCallback(() => {
    if (!pendingBoundsRef.current || !onSearchThisArea) return;
    setIsSearchingArea(true);
    setIsViewportDirty(false);
    lastQueriedBoundsRef.current = {
      swLat: pendingBoundsRef.current.swLat,
      swLng: pendingBoundsRef.current.swLng,
      neLat: pendingBoundsRef.current.neLat,
      neLng: pendingBoundsRef.current.neLng,
    };
    onSearchThisArea(pendingBoundsRef.current);
    setTimeout(() => setIsSearchingArea(false), 500);
  }, [onSearchThisArea]);

  const handleZoomIn = () => {
    if (mapInstanceRef.current) mapInstanceRef.current.zoomIn();
  };

  const handleZoomOut = () => {
    if (mapInstanceRef.current) mapInstanceRef.current.zoomOut();
  };

  const handleRecenter = () => {
    hasUserMovedRef.current = false;
    setIsViewportDirty(false);
    if (mapInstanceRef.current) {
      if (validProviders.length > 0) {
        const bounds = L.latLngBounds(validProviders.map((p) => [p.latitude, p.longitude]));
        mapInstanceRef.current.fitBounds(bounds, { padding: [40, 40], maxZoom: 15 });
      } else {
        mapInstanceRef.current.setView([center.lat, center.lng], 13);
      }
    }
  };

  return (
    <div className={`relative w-full h-full rounded-2xl overflow-hidden border border-[#DED8CF] dark:border-[#30394A] bg-[#FAF8F5] dark:bg-[#111620] ${className}`}>
      {/* 1. Leaflet Container */}
      <div ref={mapContainerRef} className="w-full h-full z-0" />

      {/* 2. Error Fallback (Phase 12: Map errors do not disable discovery) */}
      {mapError && (
        <div className="absolute inset-0 z-30 bg-[#FAF8F5]/90 dark:bg-[#111620]/90 backdrop-blur-sm flex flex-col items-center justify-center p-6 text-center space-y-2">
          <AlertCircle className="w-8 h-8 text-[#C74444]" />
          <p className="text-sm font-bold text-[#181A1F] dark:text-[#F4F6FA]">Map configuration is required</p>
          <p className="text-xs text-[#626B7A] dark:text-[#A2AEC1]">
            Discovery list results remain fully active and searchable.
          </p>
        </div>
      )}

      {/* 3. Phase 13: "Search this area" Floating Action Pill */}
      {isViewportDirty && (
        <div className="absolute top-4 left-1/2 -translate-x-1/2 z-20 animate-fadeIn">
          <button
            type="button"
            onClick={handleSearchThisAreaClick}
            disabled={isSearchingArea}
            className="px-4 py-2 rounded-full bg-[#181A1F] dark:bg-white text-white dark:text-[#181A1F] text-xs font-bold shadow-xl hover:bg-[#B93650] dark:hover:bg-[#B93650] dark:hover:text-white transition-all flex items-center gap-2 border border-white/20 active:scale-95"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${isSearchingArea ? 'animate-spin' : ''}`} />
            <span>Search this area</span>
          </button>
        </div>
      )}

      {/* 4. Controls Overlay: Zoom & Recenter */}
      <div className="absolute bottom-6 right-4 z-20 flex flex-col gap-1.5 shadow-lg">
        <button
          type="button"
          onClick={handleZoomIn}
          aria-label="Zoom in"
          className="w-9 h-9 rounded-lg bg-white dark:bg-[#1A2130] hover:bg-[#FAF8F5] dark:hover:bg-[#273142] border border-[#DED8CF] dark:border-[#30394A] text-[#181A1F] dark:text-[#F4F6FA] flex items-center justify-center transition-colors shadow-sm"
        >
          <ZoomIn className="w-4 h-4" />
        </button>
        <button
          type="button"
          onClick={handleZoomOut}
          aria-label="Zoom out"
          className="w-9 h-9 rounded-lg bg-white dark:bg-[#1A2130] hover:bg-[#FAF8F5] dark:hover:bg-[#273142] border border-[#DED8CF] dark:border-[#30394A] text-[#181A1F] dark:text-[#F4F6FA] flex items-center justify-center transition-colors shadow-sm"
        >
          <ZoomOut className="w-4 h-4" />
        </button>
        <button
          type="button"
          onClick={handleRecenter}
          aria-label="Recenter map"
          title="Recenter map"
          className="w-9 h-9 rounded-lg bg-white dark:bg-[#1A2130] hover:bg-[#FAF8F5] dark:hover:bg-[#273142] border border-[#DED8CF] dark:border-[#30394A] text-[#181A1F] dark:text-[#F4F6FA] flex items-center justify-center transition-colors shadow-sm"
        >
          <RotateCcw className="w-4 h-4" />
        </button>
      </div>

      {/* 5. Provider Count & Spatial Coverage Badge */}
      <div className="absolute top-4 left-4 z-20 flex flex-col gap-1.5 pointer-events-none">
        <div className="bg-white/95 dark:bg-[#111620]/95 backdrop-blur-md px-3 py-1.5 rounded-lg border border-[#DED8CF] dark:border-[#30394A] text-[11px] font-semibold text-[#181A1F] dark:text-[#F4F6FA] shadow-sm flex items-center gap-1.5">
          <MapPin className="w-3.5 h-3.5 text-[#B93650]" />
          <span>{validProviders.length} venues mapped</span>
        </div>
        {providers.length > validProviders.length && (
          <div className="bg-amber-50/95 dark:bg-amber-950/90 backdrop-blur-md px-2.5 py-1 rounded-lg border border-amber-300 dark:border-amber-800 text-[10px] font-medium text-amber-900 dark:text-amber-200 shadow-sm flex items-center gap-1.5">
            <AlertCircle className="w-3 h-3 text-amber-600 dark:text-amber-400 shrink-0" />
            <span>{providers.length - validProviders.length} without map coordinates</span>
          </div>
        )}
      </div>
    </div>
  );
};
