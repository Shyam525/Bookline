import React, { useState, useEffect, useRef } from 'react';
import {
  MapPin,
  Navigation,
  Search,
  Clock,
  Bookmark,
  X,
  Check,
  Shield,
  AlertCircle,
  RotateCcw,
  Loader2,
  Building,
} from 'lucide-react';

export type LocationState =
  | 'Unknown'
  | 'Requesting'
  | 'Granted'
  | 'Denied'
  | 'Unavailable'
  | 'PermissionBlocked'
  | 'Error';

export interface ViewportBounds {
  swLat: number;
  swLng: number;
  neLat: number;
  neLng: number;
}

export interface LocationSelection {
  label: string;
  city: string;
  area?: string;
  postcode?: string;
  lat: number;
  lng: number;
  placeId?: string;
  viewportBounds?: ViewportBounds;
  isCurrentLocation?: boolean;
}

interface LocationControlModalProps {
  isOpen: boolean;
  onClose: () => void;
  currentSelection: LocationSelection;
  onSelectLocation: (loc: LocationSelection, saveToProfile?: boolean) => void;
}

/**
 * Curated reference directory of supported cities, neighborhoods, and postal codes.
 * Ensures instant manual selection even before backend geocoding responds.
 */
const DEFAULT_DIRECTORY: LocationSelection[] = [
  // Ahmedabad
  {
    label: 'Bodakdev, Ahmedabad (380054)',
    city: 'Ahmedabad',
    area: 'Bodakdev',
    postcode: '380054',
    lat: 23.0396,
    lng: 72.5074,
    viewportBounds: { swLat: 23.025, swLng: 72.495, neLat: 23.055, neLng: 72.52 },
  },
  {
    label: 'Satellite, Ahmedabad (380015)',
    city: 'Ahmedabad',
    area: 'Satellite',
    postcode: '380015',
    lat: 23.0305,
    lng: 72.5178,
    viewportBounds: { swLat: 23.018, swLng: 72.505, neLat: 23.045, neLng: 72.53 },
  },
  {
    label: 'Navrangpura, Ahmedabad (380009)',
    city: 'Ahmedabad',
    area: 'Navrangpura',
    postcode: '380009',
    lat: 23.0373,
    lng: 72.5539,
    viewportBounds: { swLat: 23.025, swLng: 72.54, neLat: 23.05, neLng: 72.57 },
  },
  {
    label: 'Vastrapur, Ahmedabad (380015)',
    city: 'Ahmedabad',
    area: 'Vastrapur',
    postcode: '380015',
    lat: 23.035,
    lng: 72.5293,
    viewportBounds: { swLat: 23.022, swLng: 72.518, neLat: 23.048, neLng: 72.542 },
  },
  {
    label: 'Sindhu Bhavan, Ahmedabad',
    city: 'Ahmedabad',
    area: 'Sindhu Bhavan',
    lat: 23.045,
    lng: 72.502,
  },
  // Mumbai
  {
    label: 'Bandra West, Mumbai (400050)',
    city: 'Mumbai',
    area: 'Bandra',
    postcode: '400050',
    lat: 19.0596,
    lng: 72.8295,
    viewportBounds: { swLat: 19.04, swLng: 72.81, neLat: 19.08, neLng: 72.85 },
  },
  {
    label: 'Andheri West, Mumbai (400058)',
    city: 'Mumbai',
    area: 'Andheri',
    postcode: '400058',
    lat: 19.1136,
    lng: 72.8697,
    viewportBounds: { swLat: 19.09, swLng: 72.84, neLat: 19.14, neLng: 72.89 },
  },
  {
    label: 'Juhu, Mumbai (400049)',
    city: 'Mumbai',
    area: 'Juhu',
    postcode: '400049',
    lat: 19.1075,
    lng: 72.8263,
  },
  // Bangalore
  {
    label: 'Koramangala, Bangalore (560034)',
    city: 'Bangalore',
    area: 'Koramangala',
    postcode: '560034',
    lat: 12.9352,
    lng: 77.6245,
    viewportBounds: { swLat: 12.915, swLng: 77.605, neLat: 12.955, neLng: 77.645 },
  },
  {
    label: 'Indiranagar, Bangalore (560038)',
    city: 'Bangalore',
    area: 'Indiranagar',
    postcode: '560038',
    lat: 12.9784,
    lng: 77.6408,
    viewportBounds: { swLat: 12.96, swLng: 77.62, neLat: 12.998, neLng: 77.66 },
  },
  {
    label: 'Whitefield, Bangalore (560066)',
    city: 'Bangalore',
    area: 'Whitefield',
    postcode: '560066',
    lat: 12.9698,
    lng: 77.75,
  },
  // Surat & Rajkot
  {
    label: 'Surat City Center (395003)',
    city: 'Surat',
    postcode: '395003',
    lat: 21.1702,
    lng: 72.8311,
  },
  {
    label: 'Rajkot Central (360001)',
    city: 'Rajkot',
    postcode: '360001',
    lat: 22.3039,
    lng: 70.8022,
  },
];

export const LocationControlModal: React.FC<LocationControlModalProps> = ({
  isOpen,
  onClose,
  currentSelection,
  onSelectLocation,
}) => {
  const [searchQuery, setSearchQuery] = useState('');
  const [locationState, setLocationState] = useState<LocationState>('Unknown');
  const [statusMessage, setStatusMessage] = useState<string | null>(null);
  const [saveOptIn, setSaveOptIn] = useState(false);
  const [apiSuggestions, setApiSuggestions] = useState<LocationSelection[]>([]);
  const [isSearchingApi, setIsSearchingApi] = useState(false);

  // Geo search debounce timer
  const searchTimerRef = useRef<NodeJS.Timeout | null>(null);

  // Transient Recent Locations (sessionStorage)
  const [recentLocations, setRecentLocations] = useState<LocationSelection[]>(() => {
    try {
      const stored = sessionStorage.getItem('bookline_recent_locations');
      if (stored) return JSON.parse(stored);
    } catch {}
    return [
      { label: 'Bodakdev, Ahmedabad', city: 'Ahmedabad', area: 'Bodakdev', lat: 23.0396, lng: 72.5074 },
      { label: 'Bandra West, Mumbai', city: 'Mumbai', area: 'Bandra', lat: 19.0596, lng: 72.8295 },
    ];
  });

  // Saved Profile Locations (explicit opt-in localStorage)
  const [savedLocations, setSavedLocations] = useState<LocationSelection[]>(() => {
    try {
      const stored = localStorage.getItem('bookline_saved_locations');
      if (stored) return JSON.parse(stored);
    } catch {}
    return [
      {
        label: 'Home (Satellite, Ahmedabad)',
        city: 'Ahmedabad',
        area: 'Satellite',
        postcode: '380015',
        lat: 23.0305,
        lng: 72.5178,
      },
      {
        label: 'Office (Indiranagar, Bangalore)',
        city: 'Bangalore',
        area: 'Indiranagar',
        postcode: '560038',
        lat: 12.9784,
        lng: 77.6408,
      },
    ];
  });

  // Check browser geolocation permission status once on mount
  useEffect(() => {
    if (navigator.permissions && navigator.permissions.query) {
      navigator.permissions
        .query({ name: 'geolocation' as PermissionName })
        .then((permissionStatus) => {
          if (permissionStatus.state === 'denied') {
            setLocationState('PermissionBlocked');
          } else if (permissionStatus.state === 'granted') {
            setLocationState('Granted');
          }
          permissionStatus.onchange = () => {
            if (permissionStatus.state === 'denied') {
              setLocationState('PermissionBlocked');
            } else if (permissionStatus.state === 'granted') {
              setLocationState('Granted');
            }
          };
        })
        .catch(() => {
          // Permissions API query not supported in all environments
        });
    }
  }, []);

  // Debounced API geocoding search
  useEffect(() => {
    if (!searchQuery || searchQuery.trim().length < 2) {
      setApiSuggestions([]);
      setIsSearchingApi(false);
      return;
    }

    if (searchTimerRef.current) {
      clearTimeout(searchTimerRef.current);
    }

    searchTimerRef.current = setTimeout(async () => {
      setIsSearchingApi(true);
      try {
        const res = await fetch(`/api/geo/search?q=${encodeURIComponent(searchQuery.trim())}`);
        if (res.ok) {
          const data = await res.json();
          if (Array.isArray(data)) {
            const mapped: LocationSelection[] = data.map((item: any) => ({
              label: item.title || item.name || `${item.area || item.city}, ${item.city}`,
              city: item.city || item.name || 'Ahmedabad',
              area: item.area || item.neighborhood,
              postcode: item.postalCode || item.postcode,
              lat: item.latitude || item.lat || 23.0225,
              lng: item.longitude || item.lng || 72.5714,
              placeId: item.placeId || item.id,
              viewportBounds: item.viewportBounds
                ? {
                    swLat: item.viewportBounds.southwestLatitude,
                    swLng: item.viewportBounds.southwestLongitude,
                    neLat: item.viewportBounds.northeastLatitude,
                    neLng: item.viewportBounds.northeastLongitude,
                  }
                : undefined,
            }));
            setApiSuggestions(mapped);
          }
        }
      } catch {
        // Fall back gracefully to local directory
      } finally {
        setIsSearchingApi(false);
      }
    }, 300);

    return () => {
      if (searchTimerRef.current) clearTimeout(searchTimerRef.current);
    };
  }, [searchQuery]);

  /**
   * Browser Geolocation Trigger (Only invoked on explicit user click)
   */
  const handleUseCurrentLocation = () => {
    if (!navigator.geolocation) {
      setLocationState('Unavailable');
      setStatusMessage('Geolocation is not supported by your browser.');
      return;
    }

    if (locationState === 'PermissionBlocked' || locationState === 'Denied') {
      setStatusMessage(
        'Location access was previously blocked in your browser. You can search or select any city, area, or postcode below.'
      );
      return;
    }

    setLocationState('Requesting');
    setStatusMessage('Requesting GPS location...');

    let timeoutFired = false;
    const timeoutHandle = setTimeout(() => {
      timeoutFired = true;
      if (locationState === 'Requesting') {
        setLocationState('Error');
        setStatusMessage('Location request timed out. You can retry or choose a city below.');
      }
    }, 8000);

    navigator.geolocation.getCurrentPosition(
      async (pos) => {
        if (timeoutFired) return;
        clearTimeout(timeoutHandle);

        const lat = pos.coords.latitude;
        const lng = pos.coords.longitude;
        setLocationState('Granted');

        // Resolve human-readable location via reverse geocoding API
        let resolvedCity = 'Ahmedabad';
        try {
          const res = await fetch(`/api/geo/reverse?lat=${lat}&lng=${lng}`);
          if (res.ok) {
            const data = await res.json();
            if (data.city) resolvedCity = data.city;
          }
        } catch {
          // Approximate by coordinates if offline
          if (Math.abs(lat - 19.076) < 1.0) resolvedCity = 'Mumbai';
          else if (Math.abs(lat - 12.9716) < 1.0) resolvedCity = 'Bangalore';
          else if (Math.abs(lat - 21.1702) < 1.0) resolvedCity = 'Surat';
          else if (Math.abs(lat - 22.3039) < 1.0) resolvedCity = 'Rajkot';
        }

        const gpsLoc: LocationSelection = {
          label: `Near me (${resolvedCity})`,
          city: resolvedCity,
          lat,
          lng,
          isCurrentLocation: true,
        };

        saveRecentLocation(gpsLoc);
        onSelectLocation(gpsLoc, saveOptIn);
        onClose();
      },
      (err) => {
        if (timeoutFired) return;
        clearTimeout(timeoutHandle);

        if (err.code === 1) {
          setLocationState('Denied');
          setStatusMessage(
            'Location permission was denied. You can search by city, neighborhood, or postcode below.'
          );
        } else if (err.code === 2) {
          setLocationState('Unavailable');
          setStatusMessage('Location information is currently unavailable.');
        } else {
          setLocationState('Error');
          setStatusMessage('Could not retrieve location. Please choose a city below.');
        }
      },
      { timeout: 8000, enableHighAccuracy: true, maximumAge: 60000 }
    );
  };

  const handleCancelRequest = () => {
    setLocationState('Unknown');
    setStatusMessage(null);
  };

  const saveRecentLocation = (loc: LocationSelection) => {
    const updated = [loc, ...recentLocations.filter((r) => r.label !== loc.label)].slice(0, 5);
    setRecentLocations(updated);
    try {
      sessionStorage.setItem('bookline_recent_locations', JSON.stringify(updated));
    } catch {}
  };

  const handleSelect = (loc: LocationSelection) => {
    saveRecentLocation(loc);
    if (saveOptIn) {
      const updatedSaved = [loc, ...savedLocations.filter((s) => s.label !== loc.label)].slice(0, 5);
      setSavedLocations(updatedSaved);
      try {
        localStorage.setItem('bookline_saved_locations', JSON.stringify(updatedSaved));
      } catch {}
    }
    onSelectLocation(loc, saveOptIn);
    onClose();
  };

  if (!isOpen) return null;

  // Filter local directory if no API suggestions yet
  const filteredDirectory = DEFAULT_DIRECTORY.filter((item) => {
    if (!searchQuery) return true;
    const q = searchQuery.toLowerCase();
    return (
      item.label.toLowerCase().includes(q) ||
      item.city.toLowerCase().includes(q) ||
      (item.area && item.area.toLowerCase().includes(q)) ||
      (item.postcode && item.postcode.includes(q))
    );
  });

  const displayList = apiSuggestions.length > 0 ? apiSuggestions : filteredDirectory;

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-labelledby="location-modal-title"
      className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm animate-fadeIn"
    >
      <div className="bg-[#FFFFFF] dark:bg-[#111620] border border-[#DED8CF] dark:border-[#30394A] rounded-2xl w-full max-w-lg p-5 sm:p-6 space-y-4 shadow-2xl overflow-hidden max-h-[90vh] flex flex-col">
        {/* Header */}
        <div className="flex items-center justify-between border-b border-[#DED8CF] dark:border-[#30394A] pb-3.5">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-[#FAF8F5] dark:bg-[#1A2130] border border-[#DED8CF] dark:border-[#30394A] text-[#B93650] dark:text-[#E8546A] flex items-center justify-center">
              <MapPin className="w-5 h-5" />
            </div>
            <div>
              <h2 id="location-modal-title" className="font-heading font-bold text-lg text-[#181A1F] dark:text-[#F4F6FA]">
                Choose Location
              </h2>
              <p className="text-xs text-[#4B5563] dark:text-[#CBD2DE]">
                Search by current location, city, area, or postcode
              </p>
            </div>
          </div>
          <button
            type="button"
            onClick={onClose}
            aria-label="Close location picker"
            className="p-1.5 rounded-lg bg-[#FAF8F5] dark:bg-[#1A2130] hover:bg-[#F3F0EB] dark:hover:bg-[#273142] text-[#626B7A] dark:text-[#A2AEC1] hover:text-[#181A1F] dark:hover:text-[#F4F6FA] transition-colors"
          >
            <X className="w-4 h-4" />
          </button>
        </div>

        {/* 1. "Use My Current Location" Control with 7 States */}
        <div className="space-y-2">
          {locationState === 'Requesting' ? (
            <div className="w-full p-3.5 rounded-xl bg-[#FAF8F5] dark:bg-[#1A2130] border border-[#B93650]/40 flex items-center justify-between">
              <div className="flex items-center gap-3">
                <Loader2 className="w-5 h-5 text-[#B93650] dark:text-[#E8546A] animate-spin" />
                <div>
                  <p className="text-xs font-bold text-[#181A1F] dark:text-[#F4F6FA]">Requesting GPS position...</p>
                  <p className="text-[11px] text-[#626B7A] dark:text-[#A2AEC1]">Connecting with browser geolocation</p>
                </div>
              </div>
              <button
                type="button"
                onClick={handleCancelRequest}
                className="text-xs font-semibold text-[#626B7A] dark:text-[#A2AEC1] hover:text-[#181A1F] dark:hover:text-[#F4F6FA] px-2.5 py-1 rounded bg-white dark:bg-[#111620] border border-[#DED8CF] dark:border-[#30394A]"
              >
                Cancel
              </button>
            </div>
          ) : (
            <button
              type="button"
              onClick={handleUseCurrentLocation}
              disabled={locationState === 'PermissionBlocked'}
              className={`w-full p-3.5 rounded-xl border text-xs font-bold transition-all flex items-center justify-between group shadow-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#B93650] ${
                locationState === 'PermissionBlocked'
                  ? 'bg-[#FAF8F5] dark:bg-[#1A2130] border-[#DED8CF] dark:border-[#30394A] text-[#9AA3B2] dark:text-[#687386] cursor-not-allowed'
                  : 'bg-[#FAF8F5] dark:bg-[#1A2130] hover:bg-[#F3F0EB] dark:hover:bg-[#273142] border-[#DED8CF] dark:border-[#30394A] hover:border-[#B93650] dark:hover:border-[#E8546A] text-[#181A1F] dark:text-[#F4F6FA]'
              }`}
            >
              <div className="flex items-center gap-3">
                <div className="w-8 h-8 rounded-lg bg-[#158A63]/10 dark:bg-[#34D399]/15 text-[#158A63] dark:text-[#34D399] flex items-center justify-center group-hover:scale-105 transition-transform">
                  <Navigation className="w-4 h-4" />
                </div>
                <div className="text-left">
                  <p className="font-bold">Use my current location</p>
                  <p className="text-[11px] text-[#626B7A] dark:text-[#A2AEC1]">
                    Transient GPS &bull; Privacy-safe discovery
                  </p>
                </div>
              </div>
              <span className="text-[11px] text-[#158A63] dark:text-[#34D399] font-medium">
                {currentSelection.isCurrentLocation ? 'Active' : 'Near me'}
              </span>
            </button>
          )}

          {/* Fallback Message for Denied / Unavailable / Error */}
          {(locationState === 'Denied' ||
            locationState === 'PermissionBlocked' ||
            locationState === 'Unavailable' ||
            locationState === 'Error') && (
            <div className="p-3 rounded-xl bg-[#FAF8F5] dark:bg-[#1A2130] border border-[#C74444]/40 text-xs space-y-1.5">
              <div className="flex items-start gap-2 text-[#C74444] dark:text-[#F87171]">
                <AlertCircle className="w-4 h-4 shrink-0 mt-0.5" />
                <span className="font-semibold">
                  {locationState === 'PermissionBlocked' || locationState === 'Denied'
                    ? 'Where would you like to search?'
                    : 'Location temporarily unavailable'}
                </span>
              </div>
              <p className="text-[11px] text-[#4B5563] dark:text-[#CBD2DE]">
                {statusMessage || 'Choose or search for any city, neighborhood, or postcode below.'}
              </p>
              {locationState === 'Error' && (
                <button
                  type="button"
                  onClick={handleUseCurrentLocation}
                  className="inline-flex items-center gap-1 text-[11px] font-bold text-[#B93650] hover:underline"
                >
                  <RotateCcw className="w-3 h-3" />
                  <span>Retry current location</span>
                </button>
              )}
            </div>
          )}
        </div>

        {/* 2. Manual Search Input */}
        <div className="relative">
          <Search className="w-4 h-4 text-[#626B7A] dark:text-[#A2AEC1] absolute left-3.5 top-1/2 -translate-y-1/2" />
          <input
            type="text"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            placeholder="Search city, area, or postcode (e.g. Bodakdev, 380015, Bandra)..."
            className="w-full bg-[#FAF8F5] dark:bg-[#151B27] border border-[#DED8CF] dark:border-[#30394A] rounded-xl pl-10 pr-9 py-2.5 text-xs text-[#181A1F] dark:text-[#F4F6FA] placeholder-[#626B7A] dark:placeholder-[#A2AEC1] focus:border-[#B93650] dark:focus:border-[#E8546A] outline-none transition-colors"
          />
          {searchQuery && (
            <button
              type="button"
              onClick={() => setSearchQuery('')}
              className="absolute right-3 top-1/2 -translate-y-1/2 text-[#626B7A] dark:text-[#A2AEC1] hover:text-[#181A1F]"
            >
              <X className="w-3.5 h-3.5" />
            </button>
          )}
        </div>

        {/* 3. Scrollable Listings */}
        <div className="flex-1 overflow-y-auto space-y-4 pr-1">
          {/* Saved Profile Locations */}
          {savedLocations.length > 0 && !searchQuery && (
            <div className="space-y-1.5">
              <span className="text-[10px] font-bold uppercase tracking-wider text-[#626B7A] dark:text-[#A2AEC1] flex items-center gap-1.5">
                <Bookmark className="w-3 h-3 text-[#A66B00] dark:text-[#FBBF24]" />
                <span>Saved Places</span>
              </span>
              <div className="space-y-1">
                {savedLocations.map((loc) => (
                  <button
                    key={loc.label}
                    type="button"
                    onClick={() => handleSelect(loc)}
                    className="w-full px-3 py-2 rounded-lg bg-[#FAF8F5] dark:bg-[#151B27] hover:bg-[#F3F0EB] dark:hover:bg-[#1A2130] border border-[#DED8CF] dark:border-[#30394A] text-xs text-left flex items-center justify-between text-[#181A1F] dark:text-[#F4F6FA] transition-colors"
                  >
                    <div className="flex items-center gap-2">
                      <Building className="w-3.5 h-3.5 text-[#626B7A] dark:text-[#A2AEC1]" />
                      <span>{loc.label}</span>
                    </div>
                    {currentSelection.label === loc.label && (
                      <Check className="w-3.5 h-3.5 text-[#158A63] dark:text-[#34D399]" />
                    )}
                  </button>
                ))}
              </div>
            </div>
          )}

          {/* Recent Locations */}
          {recentLocations.length > 0 && !searchQuery && (
            <div className="space-y-1.5">
              <span className="text-[10px] font-bold uppercase tracking-wider text-[#626B7A] dark:text-[#A2AEC1] flex items-center gap-1.5">
                <Clock className="w-3 h-3 text-[#158A63] dark:text-[#34D399]" />
                <span>Recent Locations</span>
              </span>
              <div className="space-y-1">
                {recentLocations.map((loc) => (
                  <button
                    key={loc.label}
                    type="button"
                    onClick={() => handleSelect(loc)}
                    className="w-full px-3 py-2 rounded-lg bg-[#FAF8F5] dark:bg-[#151B27] hover:bg-[#F3F0EB] dark:hover:bg-[#1A2130] border border-[#DED8CF] dark:border-[#30394A] text-xs text-left flex items-center justify-between text-[#181A1F] dark:text-[#F4F6FA] transition-colors"
                  >
                    <span>{loc.label}</span>
                    {currentSelection.label === loc.label && (
                      <Check className="w-3.5 h-3.5 text-[#158A63] dark:text-[#34D399]" />
                    )}
                  </button>
                ))}
              </div>
            </div>
          )}

          {/* Search Results / Directory */}
          <div className="space-y-1.5">
            <span className="text-[10px] font-bold uppercase tracking-wider text-[#626B7A] dark:text-[#A2AEC1]">
              {searchQuery ? 'Matching Locations' : 'Cities & Areas'}
            </span>
            <div className="space-y-1 max-h-48 overflow-y-auto">
              {displayList.map((loc) => (
                <button
                  key={loc.placeId || loc.label}
                  type="button"
                  onClick={() => handleSelect(loc)}
                  className="w-full px-3 py-2.5 rounded-lg bg-[#FAF8F5] dark:bg-[#151B27] hover:bg-[#F3F0EB] dark:hover:bg-[#1A2130] border border-[#DED8CF] dark:border-[#30394A] text-xs text-left flex items-center justify-between text-[#181A1F] dark:text-[#F4F6FA] transition-colors"
                >
                  <div>
                    <span className="font-semibold block">{loc.label}</span>
                    <span className="text-[11px] text-[#626B7A] dark:text-[#A2AEC1]">
                      {loc.city} {loc.postcode ? `(${loc.postcode})` : ''}
                    </span>
                  </div>
                  {currentSelection.label === loc.label && (
                    <Check className="w-3.5 h-3.5 text-[#158A63] dark:text-[#34D399] shrink-0" />
                  )}
                </button>
              ))}

              {displayList.length === 0 && !isSearchingApi && (
                <div className="p-4 text-center text-xs text-[#626B7A] dark:text-[#A2AEC1]">
                  No locations match &ldquo;{searchQuery}&rdquo;. Try another city or neighborhood.
                </div>
              )}
            </div>
          </div>
        </div>

        {/* 4. Privacy Guarantee Footer */}
        <div className="pt-3 border-t border-[#DED8CF] dark:border-[#30394A] flex flex-wrap items-center justify-between gap-2 text-xs">
          <label className="flex items-center gap-2 cursor-pointer text-[#4B5563] dark:text-[#CBD2DE] hover:text-[#181A1F] dark:hover:text-[#F4F6FA] transition-colors">
            <input
              type="checkbox"
              checked={saveOptIn}
              onChange={(e) => setSaveOptIn(e.target.checked)}
              className="rounded bg-[#FAF8F5] dark:bg-[#151B27] border-[#DED8CF] dark:border-[#30394A] text-[#B93650] focus:ring-0 cursor-pointer"
            />
            <span className="text-[11px]">Save location to profile (Opt-in)</span>
          </label>

          <span className="text-[11px] text-[#626B7A] dark:text-[#A2AEC1] flex items-center gap-1">
            <Shield className="w-3 h-3 text-[#158A63] dark:text-[#34D399]" />
            <span>Transient Privacy Guarantee</span>
          </span>
        </div>
      </div>
    </div>
  );
};
