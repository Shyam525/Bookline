import React, { useState, useEffect } from 'react';
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
  Sparkles,
} from 'lucide-react';

export interface LocationSelection {
  label: string;
  city: string;
  area?: string;
  postcode?: string;
  lat: number;
  lng: number;
  isCurrentLocation?: boolean;
}

interface LocationControlModalProps {
  isOpen: boolean;
  onClose: () => void;
  currentSelection: LocationSelection;
  onSelectLocation: (loc: LocationSelection, saveToProfile?: boolean) => void;
}

export const LocationControlModal: React.FC<LocationControlModalProps> = ({
  isOpen,
  onClose,
  currentSelection,
  onSelectLocation,
}) => {
  const [searchQuery, setSearchQuery] = useState('');
  const [gpsLoading, setGpsLoading] = useState(false);
  const [gpsError, setGpsError] = useState<string | null>(null);
  const [saveOptIn, setSaveOptIn] = useState(false);

  // Recent locations stored transiently in sessionStorage (Section 27 Privacy)
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

  // Saved locations (opt-in explicit profile storage)
  const [savedLocations, setSavedLocations] = useState<LocationSelection[]>(() => {
    try {
      const stored = localStorage.getItem('bookline_saved_locations');
      if (stored) return JSON.parse(stored);
    } catch {}
    return [
      { label: 'Home (Satellite, Ahmedabad)', city: 'Ahmedabad', area: 'Satellite', postcode: '380015', lat: 23.0305, lng: 72.5178 },
      { label: 'Office (Indiranagar, Bangalore)', city: 'Bangalore', area: 'Indiranagar', postcode: '560038', lat: 12.9784, lng: 77.6408 },
    ];
  });

  // Comprehensive city & neighborhood directory (Section 25 & 30)
  const directoryLocations: LocationSelection[] = [
    // Ahmedabad
    { label: 'Bodakdev, Ahmedabad (380054)', city: 'Ahmedabad', area: 'Bodakdev', postcode: '380054', lat: 23.0396, lng: 72.5074 },
    { label: 'Satellite, Ahmedabad (380015)', city: 'Ahmedabad', area: 'Satellite', postcode: '380015', lat: 23.0305, lng: 72.5178 },
    { label: 'Navrangpura, Ahmedabad (380009)', city: 'Ahmedabad', area: 'Navrangpura', postcode: '380009', lat: 23.0373, lng: 72.5539 },
    { label: 'Vastrapur, Ahmedabad (380015)', city: 'Ahmedabad', area: 'Vastrapur', postcode: '380015', lat: 23.0350, lng: 72.5293 },
    { label: 'Sindhu Bhavan, Ahmedabad', city: 'Ahmedabad', area: 'Sindhu Bhavan', lat: 23.0450, lng: 72.5020 },
    // Mumbai
    { label: 'Bandra West, Mumbai (400050)', city: 'Mumbai', area: 'Bandra', postcode: '400050', lat: 19.0596, lng: 72.8295 },
    { label: 'Andheri West, Mumbai (400058)', city: 'Mumbai', area: 'Andheri', postcode: '400058', lat: 19.1136, lng: 72.8697 },
    { label: 'Juhu, Mumbai (400049)', city: 'Mumbai', area: 'Juhu', postcode: '400049', lat: 19.1075, lng: 72.8263 },
    // Bangalore
    { label: 'Koramangala, Bangalore (560034)', city: 'Bangalore', area: 'Koramangala', postcode: '560034', lat: 12.9352, lng: 77.6245 },
    { label: 'Indiranagar, Bangalore (560038)', city: 'Bangalore', area: 'Indiranagar', postcode: '560038', lat: 12.9784, lng: 77.6408 },
    { label: 'Whitefield, Bangalore (560066)', city: 'Bangalore', area: 'Whitefield', postcode: '560066', lat: 12.9698, lng: 77.7500 },
    // Surat & Rajkot
    { label: 'Surat City Center (395003)', city: 'Surat', postcode: '395003', lat: 21.1702, lng: 72.8311 },
    { label: 'Rajkot Central (360001)', city: 'Rajkot', postcode: '360001', lat: 22.3039, lng: 70.8022 },
  ];

  const handleUseCurrentLocation = () => {
    if (!navigator.geolocation) {
      setGpsError('Geolocation is not supported by your browser.');
      return;
    }

    setGpsLoading(true);
    setGpsError(null);

    navigator.geolocation.getCurrentPosition(
      (pos) => {
        setGpsLoading(false);
        const lat = pos.coords.latitude;
        const lng = pos.coords.longitude;

        // Approximate city based on nearest coordinates
        let approximateCity = 'Ahmedabad';
        if (Math.abs(lat - 19.0760) < 1.0) approximateCity = 'Mumbai';
        else if (Math.abs(lat - 12.9716) < 1.0) approximateCity = 'Bangalore';
        else if (Math.abs(lat - 21.1702) < 1.0) approximateCity = 'Surat';
        else if (Math.abs(lat - 22.3039) < 1.0) approximateCity = 'Rajkot';

        const gpsLoc: LocationSelection = {
          label: `Current Location (${approximateCity})`,
          city: approximateCity,
          lat,
          lng,
          isCurrentLocation: true,
        };

        saveRecentLocation(gpsLoc);
        onSelectLocation(gpsLoc, saveOptIn);
        onClose();
      },
      (err) => {
        setGpsLoading(false);
        // Section 26: Never block discovery if permission is denied
        setGpsError(
          'Location access was denied or unavailable. You can search or select any city, neighborhood, or postal code below without restriction.'
        );
      },
      { timeout: 8000, enableHighAccuracy: true }
    );
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

  const filteredDirectory = directoryLocations.filter((item) => {
    if (!searchQuery) return true;
    const q = searchQuery.toLowerCase();
    return (
      item.label.toLowerCase().includes(q) ||
      item.city.toLowerCase().includes(q) ||
      (item.area && item.area.toLowerCase().includes(q)) ||
      (item.postcode && item.postcode.includes(q))
    );
  });

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-sm animate-fadeIn">
      <div className="bg-[#111620] border border-[#273142] rounded-[20px] w-full max-w-lg p-6 space-y-5 shadow-2xl overflow-hidden max-h-[90vh] flex flex-col">
        {/* Header */}
        <div className="flex items-center justify-between border-b border-[#273142] pb-4">
          <div className="flex items-center gap-2.5">
            <div className="w-10 h-10 rounded-[8px] bg-[#1A2130] border border-[#273142] text-[#E8546A] flex items-center justify-center">
              <MapPin className="w-5 h-5" />
            </div>
            <div>
              <h3 className="font-heading font-bold text-lg text-[#F4F6FA]">
                Choose Location
              </h3>
              <p className="text-xs text-[#C3CAD6]">
                Discover local providers by GPS, city, neighborhood, or postal code
              </p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="p-1.5 rounded-[8px] bg-[#1A2130] hover:bg-[#273142] text-[#8F9AAF] hover:text-[#F4F6FA] transition-colors"
          >
            <X className="w-4 h-4" />
          </button>
        </div>

        {/* GPS Button (Section 26) */}
        <div className="space-y-2">
          <button
            type="button"
            onClick={handleUseCurrentLocation}
            disabled={gpsLoading}
            className="w-full py-3 px-4 rounded-[12px] bg-[#1A2130] hover:bg-[#273142] border border-[#273142] hover:border-[#E8546A] text-xs font-bold text-[#F4F6FA] transition-all flex items-center justify-between group shadow-sm"
          >
            <div className="flex items-center gap-3">
              <div className="w-8 h-8 rounded-[8px] bg-[#34D399]/15 text-[#34D399] flex items-center justify-center group-hover:scale-105 transition-transform">
                <Navigation className="w-4 h-4" />
              </div>
              <div className="text-left">
                <p className="font-bold">Use my current location</p>
                <p className="text-[11px] text-[#8F9AAF]">Transient GPS &bull; Proximity discovery</p>
              </div>
            </div>
            <span className="text-[11px] text-[#34D399] font-mono">
              {gpsLoading ? 'Locating...' : 'Locate'}
            </span>
          </button>

          {gpsError && (
            <div className="p-3 rounded-[8px] bg-[#1A2130] border border-[#F87171]/40 text-[#F87171] text-xs flex items-start gap-2">
              <AlertCircle className="w-4 h-4 flex-shrink-0 mt-0.5" />
              <span>{gpsError}</span>
            </div>
          )}
        </div>

        {/* Search Input (Section 25: city, area, postcode) */}
        <div className="relative">
          <Search className="w-4 h-4 text-[#8F9AAF] absolute left-3.5 top-1/2 -translate-y-1/2" />
          <input
            type="text"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            placeholder="Search city, area, or postcode (e.g. Bodakdev, 380015, Bandra)..."
            className="w-full bg-[#151B27] border border-[#273142] rounded-[10px] pl-10 pr-4 py-2.5 text-xs text-[#F4F6FA] placeholder-[#8F9AAF] focus:border-[#E8546A] outline-none transition-colors"
          />
        </div>

        {/* Scrollable Location Lists */}
        <div className="flex-1 overflow-y-auto space-y-4 pr-1">
          {/* Saved Locations */}
          {savedLocations.length > 0 && !searchQuery && (
            <div className="space-y-1.5">
              <span className="text-[10px] font-bold uppercase tracking-wider text-[#8F9AAF] flex items-center gap-1.5">
                <Bookmark className="w-3 h-3 text-[#FBBF24]" />
                <span>Saved Locations</span>
              </span>
              <div className="space-y-1">
                {savedLocations.map((loc) => (
                  <button
                    key={loc.label}
                    onClick={() => handleSelect(loc)}
                    className="w-full px-3 py-2 rounded-[8px] bg-[#151B27] hover:bg-[#1A2130] border border-[#273142] text-xs text-left flex items-center justify-between text-[#C3CAD6] hover:text-[#F4F6FA] transition-colors"
                  >
                    <span>{loc.label}</span>
                    {currentSelection.label === loc.label && (
                      <Check className="w-3.5 h-3.5 text-[#34D399]" />
                    )}
                  </button>
                ))}
              </div>
            </div>
          )}

          {/* Recent Locations */}
          {recentLocations.length > 0 && !searchQuery && (
            <div className="space-y-1.5">
              <span className="text-[10px] font-bold uppercase tracking-wider text-[#8F9AAF] flex items-center gap-1.5">
                <Clock className="w-3 h-3 text-[#34D399]" />
                <span>Recent Locations</span>
              </span>
              <div className="space-y-1">
                {recentLocations.map((loc) => (
                  <button
                    key={loc.label}
                    onClick={() => handleSelect(loc)}
                    className="w-full px-3 py-2 rounded-[8px] bg-[#151B27] hover:bg-[#1A2130] border border-[#273142] text-xs text-left flex items-center justify-between text-[#C3CAD6] hover:text-[#F4F6FA] transition-colors"
                  >
                    <span>{loc.label}</span>
                    {currentSelection.label === loc.label && (
                      <Check className="w-3.5 h-3.5 text-[#34D399]" />
                    )}
                  </button>
                ))}
              </div>
            </div>
          )}

          {/* Directory Listings */}
          <div className="space-y-1.5">
            <span className="text-[10px] font-bold uppercase tracking-wider text-[#8F9AAF]">
              {searchQuery ? 'Matching Locations' : 'Cities & Neighborhoods'}
            </span>
            <div className="space-y-1 max-h-48 overflow-y-auto">
              {filteredDirectory.map((loc) => (
                <button
                  key={loc.label}
                  onClick={() => handleSelect(loc)}
                  className="w-full px-3 py-2 rounded-[8px] bg-[#151B27] hover:bg-[#1A2130] border border-[#273142] text-xs text-left flex items-center justify-between text-[#C3CAD6] hover:text-[#F4F6FA] transition-colors"
                >
                  <div>
                    <span className="font-semibold">{loc.label}</span>
                    <span className="text-[11px] text-[#8F9AAF] block">{loc.city}</span>
                  </div>
                  {currentSelection.label === loc.label && (
                    <Check className="w-3.5 h-3.5 text-[#34D399]" />
                  )}
                </button>
              ))}
            </div>
          </div>
        </div>

        {/* Section 27: Privacy Opt-In Checkbox */}
        <div className="pt-3 border-t border-[#273142] flex items-center justify-between text-xs">
          <label className="flex items-center gap-2 cursor-pointer text-[#C3CAD6] hover:text-[#F4F6FA] transition-colors">
            <input
              type="checkbox"
              checked={saveOptIn}
              onChange={(e) => setSaveOptIn(e.target.checked)}
              className="rounded-[4px] bg-[#151B27] border-[#273142] text-[#E8546A] focus:ring-0 cursor-pointer"
            />
            <span className="text-[11px]">Save this location to profile (Opt-In)</span>
          </label>

          <span className="text-[10px] text-[#8F9AAF] flex items-center gap-1">
            <Shield className="w-3 h-3 text-[#34D399]" />
            <span>Transient Privacy Guarantee</span>
          </span>
        </div>
      </div>
    </div>
  );
};
