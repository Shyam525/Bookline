import React, { useState, useEffect, useRef } from 'react';
import { ProviderCard } from '../../services/api/discovery';
import {
  MapPin,
  Navigation,
  ZoomIn,
  ZoomOut,
  RotateCcw,
  AlertTriangle,
  Star,
  CheckCircle,
  ExternalLink,
  Layers,
} from 'lucide-react';

interface InteractiveMapProps {
  providers: ProviderCard[];
  selectedProviderId?: string | null;
  hoveredProviderId?: string | null;
  onSelectProvider: (provider: ProviderCard) => void;
  onHoverProvider?: (providerId: string | null) => void;
  onSearchThisArea?: (viewport?: { lat: number; lng: number; zoom: number }) => void;
  center?: { lat: number; lng: number };
  userLocation?: { lat: number; lng: number } | null;
  zoom?: number;
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
}) => {
  const [currentZoom, setCurrentZoom] = useState(13);
  const [mapCenter, setMapCenter] = useState(center);
  const [hasMoved, setHasMoved] = useState(false);
  const [isMapAvailable, setIsMapAvailable] = useState(true);

  // Mouse pan & drag state (Section 28)
  const [isDragging, setIsDragging] = useState(false);
  const dragStartRef = useRef<{ x: number; y: number } | null>(null);
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    setMapCenter(center);
    setHasMoved(false);
  }, [center.lat, center.lng]);

  const handleZoomIn = () => {
    setCurrentZoom((z) => Math.min(z + 1, 18));
    setHasMoved(true);
  };

  const handleZoomOut = () => {
    setCurrentZoom((z) => Math.max(z - 1, 8));
    setHasMoved(true);
  };

  const handleRecenter = () => {
    setMapCenter(center);
    setCurrentZoom(13);
    setHasMoved(false);
  };

  // Convert lat/lng to normalized SVG/Canvas coordinates centered around mapCenter
  const scale = Math.pow(2, currentZoom - 10) * 12;
  const getCoordinates = (lat: number, lng: number) => {
    const x = 50 + (lng - mapCenter.lng) * scale * 2.5;
    const y = 50 - (lat - mapCenter.lat) * scale * 2.5;
    return { x: Math.max(5, Math.min(95, x)), y: Math.max(5, Math.min(95, y)) };
  };

  // Mouse drag handlers for real map panning (Section 28)
  const handleMouseDown = (e: React.MouseEvent) => {
    setIsDragging(true);
    dragStartRef.current = { x: e.clientX, y: e.clientY };
  };

  const handleMouseMove = (e: React.MouseEvent) => {
    if (!isDragging || !dragStartRef.current || !containerRef.current) return;
    const dx = e.clientX - dragStartRef.current.x;
    const dy = e.clientY - dragStartRef.current.y;

    if (Math.abs(dx) > 2 || Math.abs(dy) > 2) {
      dragStartRef.current = { x: e.clientX, y: e.clientY };
      // Map delta to lat/lng degrees
      const latDelta = (dy / scale) * 0.04;
      const lngDelta = (-dx / scale) * 0.04;

      setMapCenter((prev) => ({
        lat: prev.lat + latDelta,
        lng: prev.lng + lngDelta,
      }));
      setHasMoved(true);
    }
  };

  const handleMouseUp = () => {
    setIsDragging(false);
    dragStartRef.current = null;
  };

  if (!isMapAvailable) {
    return (
      <div className="w-full h-full min-h-[400px] bg-[#111620] border border-[#273142] rounded-[16px] flex flex-col items-center justify-center p-8 text-center">
        <AlertTriangle className="w-10 h-10 text-[#FBBF24] mb-3" />
        <h3 className="text-base font-bold text-[#F4F6FA] mb-1">Map Layer Initializing</h3>
        <p className="text-xs text-[#C3CAD6] max-w-sm mb-4">
          Spatial positioning and provider discovery remain operational.
        </p>
        <button
          onClick={() => setIsMapAvailable(true)}
          className="px-4 py-2 bg-[#1A2130] hover:bg-[#273142] border border-[#273142] text-xs font-semibold text-[#F4F6FA] rounded-[8px] transition-colors"
        >
          Reload Map
        </button>
      </div>
    );
  }

  const selectedProvider = providers.find((p) => p.id === selectedProviderId);

  // Clustering logic (Section 28): at zoom <= 10, group providers if count > 3
  const isClusterMode = currentZoom <= 10 && providers.length >= 4;

  return (
    <div
      ref={containerRef}
      onMouseDown={handleMouseDown}
      onMouseMove={handleMouseMove}
      onMouseUp={handleMouseUp}
      onMouseLeave={handleMouseUp}
      className={`relative w-full h-full min-h-[460px] bg-[#090B10] border border-[#273142] rounded-[16px] overflow-hidden select-none flex flex-col ${
        isDragging ? 'cursor-grabbing' : 'cursor-grab'
      }`}
    >
      {/* Dark Map Canvas Simulation with Grid, Arterials, and Waterways */}
      <div className="absolute inset-0 bg-[#090B10] pointer-events-none opacity-85">
        <svg className="w-full h-full" xmlns="http://www.w3.org/2000/svg">
          <defs>
            <pattern id="grid" width="48" height="48" patternUnits="userSpaceOnUse">
              <path d="M 48 0 L 0 0 0 48" fill="none" stroke="#151B27" strokeWidth="1" />
            </pattern>
          </defs>
          <rect width="100%" height="100%" fill="url(#grid)" />
          {/* Arterial vector roadways */}
          <path d="M 0 160 Q 250 200 600 150 T 1200 220" fill="none" stroke="#1A2130" strokeWidth="8" opacity="0.8" />
          <path d="M 240 0 Q 260 300 300 600" fill="none" stroke="#1A2130" strokeWidth="10" opacity="0.7" />
          <path d="M 0 350 C 320 330 520 440 1200 340" fill="none" stroke="#151B27" strokeWidth="12" opacity="0.9" />
          <path d="M 520 0 C 480 260 550 460 600 900" fill="none" stroke="#151B27" strokeWidth="6" opacity="0.8" />
          {/* River / Reservoir contour */}
          <ellipse cx="68%" cy="32%" rx="110" ry="70" fill="#0E131E" stroke="#161E2E" strokeWidth="2" />
        </svg>
      </div>

      {/* Floating Controls: Zoom & Recenter (Section 28) */}
      <div className="absolute top-4 right-4 z-30 flex flex-col gap-2">
        <div className="bg-[#111620]/95 backdrop-blur-md border border-[#273142] rounded-[10px] overflow-hidden shadow-xl flex flex-col">
          <button
            type="button"
            onClick={handleZoomIn}
            className="p-2.5 text-[#C3CAD6] hover:bg-[#1A2130] hover:text-[#E8546A] transition-colors border-b border-[#273142]"
            title="Zoom In"
          >
            <ZoomIn className="w-4 h-4" />
          </button>
          <button
            type="button"
            onClick={handleZoomOut}
            className="p-2.5 text-[#C3CAD6] hover:bg-[#1A2130] hover:text-[#E8546A] transition-colors"
            title="Zoom Out"
          >
            <ZoomOut className="w-4 h-4" />
          </button>
        </div>

        <button
          type="button"
          onClick={handleRecenter}
          className="p-2.5 bg-[#111620]/95 backdrop-blur-md border border-[#273142] rounded-[10px] text-[#C3CAD6] hover:bg-[#1A2130] hover:text-[#E8546A] transition-colors shadow-xl"
          title="Recenter"
        >
          <RotateCcw className="w-4 h-4" />
        </button>
      </div>

      {/* "Search this area" Viewport Button (Section 102: Throttled/Debounced Viewport Query) */}
      {hasMoved && onSearchThisArea && (
        <div className="absolute top-4 left-1/2 -translate-x-1/2 z-30 animate-fadeIn">
          <button
            type="button"
            onClick={() => {
              setHasMoved(false);
              onSearchThisArea({ lat: mapCenter.lat, lng: mapCenter.lng, zoom: currentZoom });
            }}
            className="px-4 py-2 bg-[#E8546A] hover:bg-[#F06A7D] text-white text-xs font-bold rounded-full shadow-lg shadow-[#E8546A]/25 flex items-center gap-2 transition-all hover:scale-105"
          >
            <Navigation className="w-3.5 h-3.5" />
            <span>Search this area</span>
          </button>
        </div>
      )}

      {/* Location / Viewport Stats Badge (Bottom Left) */}
      <div className="absolute bottom-4 left-4 z-30 bg-[#111620]/95 backdrop-blur-md border border-[#273142] rounded-[8px] px-3 py-1.5 flex items-center gap-2 text-xs text-[#F4F6FA]">
        <MapPin className="w-3.5 h-3.5 text-[#E8546A]" />
        <span>{providers[0]?.city || 'Interactive Viewport'}</span>
        <span className="text-[#8F9AAF] font-mono">({providers.length} venues)</span>
      </div>

      {/* Interactive Markers Container (Section 101 Map+List Synchronization) */}
      <div className="relative flex-1 w-full h-full z-10 pointer-events-auto">
        {/* Current User Location Pulsing Dot (Section 26 & 28) */}
        {userLocation && (
          <div
            style={{
              left: `${getCoordinates(userLocation.lat, userLocation.lng).x}%`,
              top: `${getCoordinates(userLocation.lat, userLocation.lng).y}%`,
            }}
            className="absolute -translate-x-1/2 -translate-y-1/2 z-20 pointer-events-none"
            title="Your Current Location"
          >
            <div className="relative flex items-center justify-center">
              <span className="animate-ping absolute inline-flex h-8 w-8 rounded-full bg-[#34D399]/40 opacity-75" />
              <span className="relative inline-flex rounded-full h-4 w-4 bg-[#34D399] border-2 border-white shadow-lg" />
            </div>
          </div>
        )}

        {/* Cluster Pill if zoomed out */}
        {isClusterMode ? (
          <div
            style={{ left: '50%', top: '50%' }}
            onClick={() => setCurrentZoom(13)}
            className="absolute -translate-x-1/2 -translate-y-1/2 cursor-pointer z-20 group"
          >
            <div className="px-4 py-2 rounded-full bg-[#E8546A] text-white font-bold text-xs flex items-center gap-1.5 shadow-2xl ring-4 ring-[#E8546A]/30 group-hover:scale-110 transition-transform">
              <Layers className="w-3.5 h-3.5" />
              <span>{providers.length} Venues Cluster (Click to Zoom)</span>
            </div>
          </div>
        ) : (
          providers.map((p) => {
            const isSelected = p.id === selectedProviderId;
            const isHovered = p.id === hoveredProviderId;
            const pos = getCoordinates(p.latitude, p.longitude);

            return (
              <div
                key={p.id}
                style={{ left: `${pos.x}%`, top: `${pos.y}%` }}
                onClick={(e) => {
                  e.stopPropagation();
                  onSelectProvider(p);
                }}
                onMouseEnter={() => onHoverProvider?.(p.id)}
                onMouseLeave={() => onHoverProvider?.(null)}
                className={`absolute -translate-x-1/2 -translate-y-1/2 cursor-pointer transition-transform hover:scale-115 ${
                  isSelected || isHovered ? 'z-30 scale-115' : 'z-20'
                }`}
              >
                {/* Marker Pill */}
                <div
                  className={`px-2.5 py-1 rounded-full text-xs font-bold flex items-center gap-1.5 shadow-xl transition-all ${
                    isSelected || isHovered
                      ? 'bg-[#E8546A] text-white ring-4 ring-[#E8546A]/40 shadow-[#E8546A]/30'
                      : 'bg-[#151B27] hover:bg-[#1A2130] text-[#F4F6FA] border border-[#273142]'
                  }`}
                >
                  <span>₹{p.startingPrice}</span>
                  {p.isVerified && <CheckCircle className="w-3 h-3 text-[#34D399]" />}
                </div>

                {/* Pin Pointer */}
                <div
                  className={`w-2 h-2 rotate-45 mx-auto -mt-1 transition-colors ${
                    isSelected || isHovered ? 'bg-[#E8546A]' : 'bg-[#151B27]'
                  }`}
                />
              </div>
            );
          })
        )}
      </div>

      {/* Selected Provider Preview Modal inside Map (Section 28) */}
      {selectedProvider && (
        <div className="absolute bottom-4 right-4 z-30 max-w-xs w-full bg-[#111620]/95 backdrop-blur-md border border-[#273142] rounded-[12px] p-3.5 shadow-2xl animate-fadeIn">
          <div className="flex items-start justify-between gap-2 mb-2">
            <div>
              <div className="flex items-center gap-1.5">
                <h4 className="text-sm font-bold text-[#F4F6FA] truncate max-w-[180px]">
                  {selectedProvider.name}
                </h4>
                {selectedProvider.isVerified && (
                  <CheckCircle className="w-3.5 h-3.5 text-[#34D399] flex-shrink-0" />
                )}
              </div>
              <p className="text-[11px] text-[#8F9AAF]">{selectedProvider.category}</p>
            </div>
            <div className="flex items-center gap-1 bg-[#1A2130] px-1.5 py-0.5 rounded text-[11px] font-bold text-[#FBBF24]">
              <Star className="w-3 h-3 fill-current" />
              <span>{selectedProvider.rating}</span>
            </div>
          </div>

          <p className="text-xs text-[#C3CAD6] mb-2 line-clamp-1">{selectedProvider.address}</p>

          <div className="flex items-center justify-between text-xs pt-2 border-t border-[#273142]">
            <span className="font-bold text-[#34D399]">From ₹{selectedProvider.startingPrice}</span>
            <a
              href={`/business/${selectedProvider.slug}`}
              className="text-xs font-semibold text-[#E8546A] hover:underline flex items-center gap-1"
            >
              <span>View Storefront</span>
              <ExternalLink className="w-3 h-3" />
            </a>
          </div>
        </div>
      )}
    </div>
  );
};
