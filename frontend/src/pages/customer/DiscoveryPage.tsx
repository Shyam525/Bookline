import React, { useState, useEffect, useRef } from 'react';
import { useSearchParams, useParams, useNavigate } from 'react-router-dom';
import { discoveryApi, ProviderSearchResponse } from '../../services/api/discovery';
import { InteractiveMap } from '../../components/maps/InteractiveMap';
import {
  LocationControlModal,
  LocationSelection,
} from '../../components/discovery/LocationControlModal';
import {
  ProviderCard,
  ProviderCardSkeleton,
} from '../../components/marketplace/ProviderCard';
import {
  Search,
  MapPin,
  SlidersHorizontal,
  ArrowUpDown,
  Compass,
  Map as MapIcon,
  List as ListIcon,
  ChevronLeft,
  ChevronRight,
  Sparkles,
  X,
  RotateCcw,
} from 'lucide-react';

export const DiscoveryPage: React.FC = () => {
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const { slug, category: routeCategory } = useParams<{ slug?: string; category?: string }>();

  const categories = ['All', 'Beauty', 'Hair', 'Healthcare', 'Fitness', 'Photography', 'Professional'];

  const resolveCategoryFromSlug = (raw?: string): string => {
    if (!raw) return 'All';
    const lower = raw.toLowerCase();
    if (lower === 'wellness' || lower === 'spa') return 'Beauty';
    const matched = categories.find((c) => c.toLowerCase() === lower);
    return matched || 'All';
  };

  const initialCategory =
    resolveCategoryFromSlug(slug || routeCategory) !== 'All'
      ? resolveCategoryFromSlug(slug || routeCategory)
      : searchParams.get('category') || 'All';

  const [providers, setProviders] = useState<any[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Category-specific services dictionary (Only show filters relevant to current category)
  const categoryServicesMap: Record<string, string[]> = {
    'All': ['All Services', 'Haircut', 'Facial', 'Balayage', 'Massage', 'Dental Cleaning', 'Reformer Pilates', 'Headshots', 'Legal Advisory'],
    'Beauty': ['All Services', 'Facial & Skin Ritual', 'Chemical Peel', 'Aromatherapy Massage', 'Body Wrap', 'Manicure & Pedicure'],
    'Hair': ['All Services', 'Haircut & Styling', 'Balayage & Color', 'Keratin Smoothing', 'Scalp Therapy', 'Blowout'],
    'Healthcare': ['All Services', 'Dental Cleaning', 'Laser Teeth Whitening', 'Porcelain Veneers', 'Orthodontic Exam', 'Skin Consultation'],
    'Fitness': ['All Services', 'Reformer Pilates', 'HIIT Circuit', 'Private Athletic Coaching', 'Yoga Vinyasa', 'Sports Recovery'],
    'Photography': ['All Services', 'Corporate Headshots', 'Editorial Campaign', 'Family Heirloom Portrait', 'Fashion Lookbook', 'Product Shoot'],
    'Professional': ['All Services', 'Commercial Contracts', 'Intellectual Property Filing', 'Tax Advisory', 'Corporate Restructuring'],
  };

  // Filters from query params and state
  const [query, setQuery] = useState(searchParams.get('q') || '');
  const [debouncedQuery, setDebouncedQuery] = useState(searchParams.get('q') || '');
  const [category, setCategory] = useState(initialCategory);
  const [selectedService, setSelectedService] = useState<string>(searchParams.get('service') || 'All Services');
  const [radius, setRadius] = useState<number>(Number(searchParams.get('radius')) || 25);
  const [minRating, setMinRating] = useState<number>(Number(searchParams.get('minRating')) || 0);
  const [priceRange, setPriceRange] = useState<string>(searchParams.get('priceRange') || 'all');
  const [availability, setAvailability] = useState<string>(searchParams.get('availability') || 'all');
  const [openNow, setOpenNow] = useState(false);
  const [verifiedOnly, setVerifiedOnly] = useState(false);
  const [isFiltersOpen, setIsFiltersOpen] = useState(false);
  const [sort, setSort] = useState(searchParams.get('sort') || 'Recommended');
  const [page, setPage] = useState<number>(Number(searchParams.get('page')) || 1);

  // Location State (Privacy: transient GPS coords kept in memory, not in public shareable URL)
  const [isLocationModalOpen, setIsLocationModalOpen] = useState(false);
  const [currentLocation, setCurrentLocation] = useState<LocationSelection>(() => {
    const cityParam = searchParams.get('city') || 'Ahmedabad';
    return {
      label: cityParam,
      city: cityParam,
      lat: 23.0225,
      lng: 72.5714,
    };
  });
  const [userGpsCoords, setUserGpsCoords] = useState<{ lat: number; lng: number } | null>(null);
  const [searchBounds, setSearchBounds] = useState<{ swLat: number; swLng: number; neLat: number; neLng: number } | null>(null);

  // Request sequence id to prevent race conditions (Phase 13: Stale responses cannot overwrite newer viewport state)
  const currentRequestIdRef = useRef(0);

  // Map / List Mobile Toggle
  const [mobileView, setMobileView] = useState<'list' | 'map'>('list');
  const [selectedProviderId, setSelectedProviderId] = useState<string | null>(null);
  const [hoveredProviderId, setHoveredProviderId] = useState<string | null>(null);

  // List card refs for synchronized scrolling
  const cardRefs = useRef<Record<string, HTMLDivElement | null>>({});

  // 1. Debounce text search input (350ms)
  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedQuery(query);
      setPage(1);
    }, 350);
    return () => clearTimeout(handler);
  }, [query]);

  // 2. Sync category if URL route slug changes
  useEffect(() => {
    const fromParam = resolveCategoryFromSlug(slug || routeCategory);
    if (fromParam !== 'All') {
      setCategory(fromParam);
      setSelectedService('All Services');
    }
  }, [slug, routeCategory]);

  // 3. Update shareable non-sensitive URL parameters
  useEffect(() => {
    const nextParams = new URLSearchParams();
    if (debouncedQuery.trim()) nextParams.set('q', debouncedQuery.trim());
    if (category !== 'All') nextParams.set('category', category);
    if (selectedService !== 'All Services') nextParams.set('service', selectedService);
    if (currentLocation.city && currentLocation.city !== 'All') nextParams.set('city', currentLocation.city);
    if (radius !== 25) nextParams.set('radius', radius.toString());
    if (minRating > 0) nextParams.set('minRating', minRating.toString());
    if (priceRange !== 'all') nextParams.set('priceRange', priceRange);
    if (availability !== 'all') nextParams.set('availability', availability);
    if (sort !== 'Recommended') nextParams.set('sort', sort);
    if (page > 1) nextParams.set('page', page.toString());

    setSearchParams(nextParams, { replace: true });
  }, [debouncedQuery, category, selectedService, currentLocation.city, radius, minRating, priceRange, availability, sort, page]);

  // When category changes, reset selectedService to 'All Services'
  const handleCategoryChange = (newCat: string) => {
    setCategory(newCat);
    setSelectedService('All Services');
    setPage(1);
  };

  // Active filter count calculation
  const activeFiltersCount =
    (category !== 'All' ? 1 : 0) +
    (selectedService !== 'All Services' ? 1 : 0) +
    (radius !== 25 ? 1 : 0) +
    (minRating > 0 ? 1 : 0) +
    (priceRange !== 'all' ? 1 : 0) +
    (availability !== 'all' ? 1 : 0) +
    (openNow ? 1 : 0) +
    (verifiedOnly ? 1 : 0);

  const fetchProviders = async () => {
    const requestId = ++currentRequestIdRef.current;
    setIsLoading(true);
    setError(null);
    try {
      const lat = userGpsCoords?.lat ?? (currentLocation.isCurrentLocation ? currentLocation.lat : undefined);
      const lng = userGpsCoords?.lng ?? (currentLocation.isCurrentLocation ? currentLocation.lng : undefined);

      let maxPriceVal: number | undefined;
      if (priceRange === 'under-500') maxPriceVal = 500;
      else if (priceRange === '500-1500') maxPriceVal = 1500;
      else if (priceRange === '1500-3000') maxPriceVal = 3000;

      const res = await discoveryApi.searchProviders({
        q: debouncedQuery.trim() || undefined,
        category: category !== 'All' ? category : undefined,
        service: selectedService !== 'All Services' ? selectedService : undefined,
        city: currentLocation.city !== 'All' ? currentLocation.city : undefined,
        lat,
        lng,
        radius: searchBounds ? undefined : radius || undefined,
        swLat: searchBounds?.swLat,
        swLng: searchBounds?.swLng,
        neLat: searchBounds?.neLat,
        neLng: searchBounds?.neLng,
        minRating: minRating > 0 ? minRating : undefined,
        maxPrice: maxPriceVal,
        availability: availability !== 'all' ? availability : undefined,
        sort,
        page,
        pageSize: 10,
      });

      // Discard stale response if a newer request was dispatched
      if (requestId !== currentRequestIdRef.current) return;

      let items = res.items;
      if (verifiedOnly) {
        items = items.filter((p) => p.isVerified);
      }

      setProviders(items);
      setTotalCount(res.totalCount);
      setTotalPages(res.totalPages);
      if (items.length > 0 && !selectedProviderId) {
        setSelectedProviderId(items[0].id);
      }
    } catch (err: any) {
      if (requestId === currentRequestIdRef.current) {
        setError(err.message || 'We could not load discovery results.');
      }
    } finally {
      if (requestId === currentRequestIdRef.current) {
        setIsLoading(false);
      }
    }
  };

  useEffect(() => {
    fetchProviders();
  }, [
    debouncedQuery,
    category,
    selectedService,
    currentLocation.city,
    currentLocation.lat,
    currentLocation.lng,
    searchBounds,
    radius,
    minRating,
    priceRange,
    availability,
    openNow,
    verifiedOnly,
    sort,
    page,
  ]);

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setDebouncedQuery(query);
    setPage(1);
  };

  const handleLocationSelect = (loc: LocationSelection) => {
    setCurrentLocation(loc);
    if (loc.viewportBounds) {
      setSearchBounds(loc.viewportBounds);
    } else {
      setSearchBounds(null);
    }
    if (loc.isCurrentLocation) {
      setUserGpsCoords({ lat: loc.lat, lng: loc.lng });
    } else {
      setUserGpsCoords(null);
    }
    setPage(1);
  };


  // Synchronized Selection: clicking map marker scrolls card into view
  const handleSelectProvider = (provider: any) => {
    setSelectedProviderId(provider.id);
    const element = cardRefs.current[provider.id];
    if (element) {
      element.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    }
  };

  // Center coordinates for map
  const mapCenter = { lat: currentLocation.lat, lng: currentLocation.lng };

  // Detect query intent keywords for visual feedback
  const isIntentQuery =
    debouncedQuery.toLowerCase().includes('near me') ||
    debouncedQuery.toLowerCase().includes('best') ||
    debouncedQuery.toLowerCase().includes('satellite') ||
    debouncedQuery.toLowerCase().includes('bodakdev') ||
    debouncedQuery.toLowerCase().includes('bandra');

  return (
    <div className="flex-1 flex flex-col bg-[#FAF8F5] dark:bg-[#090B10] text-[#181A1F] dark:text-[#F4F6FA] min-h-screen">
      {/* ================================================================= */}
      {/* 1. SEARCH HEADER & LOCATION CONTROL                               */}
      {/* ================================================================= */}
      <div className="border-b border-[#DED8CF] dark:border-[#30394A] bg-[#FFFFFF] dark:bg-[#111620] px-4 lg:px-8 py-3.5 sticky top-16 z-30 shadow-sm">
        <div className="max-w-7xl mx-auto flex flex-col gap-3">
          {/* Search Bar & Controls */}
          <div className="flex flex-col lg:flex-row items-stretch lg:items-center justify-between gap-3">
            {/* Search Input Bar */}
            <form onSubmit={handleSearchSubmit} className="flex-1 w-full flex flex-col sm:flex-row items-stretch sm:items-center gap-2">
              <div className="relative flex-1">
                <Search className="w-4 h-4 text-[#626B7A] dark:text-[#A2AEC1] absolute left-3.5 top-1/2 -translate-y-1/2" />
                <input
                  type="text"
                  value={query}
                  onChange={(e) => setQuery(e.target.value)}
                  placeholder="Search salons, dentists, massage, reformer pilates, or keywords..."
                  className="w-full min-h-[46px] bg-[#FAF8F5] dark:bg-[#151B27] border border-[#DED8CF] dark:border-[#30394A] rounded-xl pl-10 pr-9 py-2.5 text-xs text-[#181A1F] dark:text-[#F4F6FA] placeholder-[#626B7A] dark:placeholder-[#A2AEC1] focus:border-[#B93650] dark:focus:border-[#E8546A] outline-none transition-colors"
                />
                {query && (
                  <button
                    type="button"
                    onClick={() => {
                      setQuery('');
                      setDebouncedQuery('');
                      setPage(1);
                    }}
                    className="absolute right-3 top-1/2 -translate-y-1/2 p-1 text-[#626B7A] dark:text-[#A2AEC1] hover:text-[#181A1F]"
                  >
                    <X className="w-3.5 h-3.5" />
                  </button>
                )}
              </div>

              {/* Location Selector Button */}
              <button
                type="button"
                onClick={() => setIsLocationModalOpen(true)}
                className="min-h-[46px] sm:min-h-[40px] px-4 py-2.5 rounded-xl bg-[#FAF8F5] dark:bg-[#151B27] hover:bg-[#F3F0EB] dark:hover:bg-[#1A2130] border border-[#DED8CF] dark:border-[#30394A] hover:border-[#B93650] dark:hover:border-[#E8546A] text-xs font-semibold text-[#181A1F] dark:text-[#F4F6FA] transition-colors flex items-center justify-between sm:justify-start gap-2 shrink-0 shadow-sm"
                title="Change search city, neighborhood, or postal code"
              >
                <div className="flex items-center gap-2 truncate">
                  <MapPin className="w-4 h-4 text-[#B93650] dark:text-[#E8546A] shrink-0" />
                  <span className="truncate font-medium">{currentLocation.label}</span>
                </div>
                <span className="text-[11px] text-[#B93650] dark:text-[#E8546A] font-bold bg-[#B93650]/10 px-2 py-0.5 rounded-md">
                  Change
                </span>
              </button>

              <button
                type="submit"
                className="min-h-[46px] sm:min-h-[40px] px-6 py-2.5 bg-[#B93650] hover:bg-[#A52E46] active:bg-[#92273D] text-white text-xs font-bold rounded-xl transition-all shadow-sm shrink-0 flex items-center justify-center gap-1.5 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#B93650]"
              >
                <Search className="w-3.5 h-3.5" />
                <span>Search</span>
              </button>
            </form>

            {/* Controls: Filter Drawer & Sort */}
            <div className="flex items-center gap-2 justify-between lg:justify-end overflow-x-auto pb-1 lg:pb-0">
              <button
                type="button"
                onClick={() => setIsFiltersOpen((prev) => !prev)}
                className={`min-h-[44px] sm:min-h-[38px] px-4 py-2 rounded-xl border text-xs font-semibold flex items-center gap-2 transition-colors shrink-0 ${
                  isFiltersOpen || activeFiltersCount > 0
                    ? 'bg-[#B93650]/10 border-[#B93650] text-[#B93650] dark:bg-[#E8546A]/15 dark:border-[#E8546A] dark:text-[#E8546A]'
                    : 'bg-[#FAF8F5] dark:bg-[#151B27] border-[#DED8CF] dark:border-[#30394A] text-[#181A1F] dark:text-[#F4F6FA] hover:border-[#B5ADA1]'
                }`}
              >
                <SlidersHorizontal className="w-4 h-4" />
                <span>Filters</span>
                {activeFiltersCount > 0 && (
                  <span className="w-4 h-4 rounded-full bg-[#B93650] dark:bg-[#E8546A] text-white text-[10px] font-bold flex items-center justify-center">
                    {activeFiltersCount}
                  </span>
                )}
              </button>

              <div className="flex items-center gap-1.5 bg-[#FAF8F5] dark:bg-[#151B27] border border-[#DED8CF] dark:border-[#30394A] rounded-xl px-3 py-2 min-h-[44px] sm:min-h-[38px] shrink-0">
                <ArrowUpDown className="w-3.5 h-3.5 text-[#626B7A] dark:text-[#A2AEC1]" />
                <select
                  value={sort}
                  onChange={(e) => setSort(e.target.value)}
                  className="bg-transparent text-xs font-medium text-[#181A1F] dark:text-[#F4F6FA] outline-none cursor-pointer"
                >
                  <option value="Recommended" className="bg-white dark:bg-[#111620]">Recommended</option>
                  <option value="Nearest" className="bg-white dark:bg-[#111620]">Nearest</option>
                  <option value="Top rated" className="bg-white dark:bg-[#111620]">Top rated</option>
                  <option value="Most reviewed" className="bg-white dark:bg-[#111620]">Most reviewed</option>
                  <option value="Earliest available" className="bg-white dark:bg-[#111620]">Earliest available</option>
                  <option value="Lowest price" className="bg-white dark:bg-[#111620]">Lowest price</option>
                  <option value="Highest price" className="bg-white dark:bg-[#111620]">Highest price</option>
                </select>
              </div>
            </div>
          </div>

          {/* Categories Horizontal Carousel */}
          <div className="flex items-center gap-2 overflow-x-auto no-scrollbar py-1 -mx-4 px-4 sm:mx-0 sm:px-0">
            {categories.map((cat) => {
              const isActive = category === cat;
              return (
                <button
                  key={cat}
                  onClick={() => handleCategoryChange(cat)}
                  className={`min-h-[42px] px-4 py-2 rounded-xl text-xs font-semibold whitespace-nowrap transition-all flex items-center gap-1.5 shrink-0 ${
                    isActive
                      ? 'bg-[#B93650] dark:bg-[#E8546A] text-white shadow-sm border border-[#B93650] dark:border-[#E8546A]'
                      : 'bg-[#FAF8F5] dark:bg-[#151B27] text-[#4B5563] dark:text-[#CBD2DE] border border-[#DED8CF] dark:border-[#30394A] hover:bg-[#F3F0EB]'
                  }`}
                >
                  <span>{cat === 'All' ? '✨ All Venues' : cat}</span>
                </button>
              );
            })}
          </div>
        </div>

        {/* Expandable Filters Panel */}
        {isFiltersOpen && (
          <div className="max-w-7xl mx-auto mt-3 pt-3 border-t border-[#DED8CF] dark:border-[#30394A] p-4 bg-[#FAF8F5] dark:bg-[#151B27] rounded-xl space-y-4 animate-fadeIn">
            {/* Category Services */}
            <div>
              <div className="flex items-center justify-between mb-2">
                <span className="text-xs font-bold text-[#181A1F] dark:text-[#F4F6FA] uppercase tracking-wider">
                  Services for {category === 'All' ? 'All Categories' : category}
                </span>
                <span className="text-[11px] text-[#626B7A] dark:text-[#A2AEC1]">
                  (Filtered by category)
                </span>
              </div>
              <div className="flex flex-wrap gap-1.5">
                {(categoryServicesMap[category] || categoryServicesMap['All']).map((srv) => (
                  <button
                    key={srv}
                    type="button"
                    onClick={() => {
                      setSelectedService(srv);
                      setPage(1);
                    }}
                    className={`px-3 py-1 rounded-lg text-xs font-medium transition-all ${
                      selectedService === srv
                        ? 'bg-[#B93650] dark:bg-[#E8546A] text-white font-semibold shadow-sm'
                        : 'bg-white dark:bg-[#111620] border border-[#DED8CF] dark:border-[#30394A] text-[#4B5563] dark:text-[#CBD2DE] hover:border-[#B5ADA1]'
                    }`}
                  >
                    {srv}
                  </button>
                ))}
              </div>
            </div>

            {/* Grid of Secondary Filters */}
            <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-4 pt-2 border-t border-[#DED8CF] dark:border-[#30394A]">
              {/* Distance Radius */}
              <div>
                <label className="block text-[11px] font-semibold text-[#626B7A] dark:text-[#A2AEC1] mb-1.5">
                  Distance Radius
                </label>
                <div className="flex items-center gap-1">
                  {[5, 10, 25, 50].map((d) => (
                    <button
                      key={d}
                      type="button"
                      onClick={() => setRadius(d)}
                      className={`flex-1 py-1 text-xs rounded-lg border ${
                        radius === d
                          ? 'bg-[#B93650] dark:bg-[#E8546A] border-[#B93650] dark:border-[#E8546A] text-white font-bold'
                          : 'bg-white dark:bg-[#111620] border-[#DED8CF] dark:border-[#30394A] text-[#4B5563] dark:text-[#CBD2DE] hover:bg-[#FAF8F5]'
                      }`}
                    >
                      {d}km
                    </button>
                  ))}
                </div>
              </div>

              {/* Rating Filter */}
              <div>
                <label className="block text-[11px] font-semibold text-[#626B7A] dark:text-[#A2AEC1] mb-1.5">
                  Minimum Rating
                </label>
                <div className="flex items-center gap-1">
                  {[
                    { label: 'Any', val: 0 },
                    { label: '4.0+ ★', val: 4.0 },
                    { label: '4.5+ ★', val: 4.5 },
                    { label: '4.8+ ★', val: 4.8 },
                  ].map((r) => (
                    <button
                      key={r.label}
                      type="button"
                      onClick={() => setMinRating(r.val)}
                      className={`flex-1 py-1 text-xs rounded-lg border ${
                        minRating === r.val
                          ? 'bg-[#B93650] dark:bg-[#E8546A] border-[#B93650] dark:border-[#E8546A] text-white font-bold'
                          : 'bg-white dark:bg-[#111620] border-[#DED8CF] dark:border-[#30394A] text-[#4B5563] dark:text-[#CBD2DE] hover:bg-[#FAF8F5]'
                      }`}
                    >
                      {r.label}
                    </button>
                  ))}
                </div>
              </div>

              {/* Price Filter */}
              <div>
                <label className="block text-[11px] font-semibold text-[#626B7A] dark:text-[#A2AEC1] mb-1.5">
                  Starting Price
                </label>
                <select
                  value={priceRange}
                  onChange={(e) => setPriceRange(e.target.value)}
                  className="w-full bg-white dark:bg-[#111620] border border-[#DED8CF] dark:border-[#30394A] rounded-lg px-2.5 py-1.5 text-xs text-[#181A1F] dark:text-[#F4F6FA] outline-none"
                >
                  <option value="all">Any Price</option>
                  <option value="under-500">Under ₹500</option>
                  <option value="500-1500">₹500 - ₹1,500</option>
                  <option value="1500-3000">₹1,500 - ₹3,000</option>
                  <option value="3000+">₹3,000 &amp; Above</option>
                </select>
              </div>

              {/* Availability Filter */}
              <div>
                <label className="block text-[11px] font-semibold text-[#626B7A] dark:text-[#A2AEC1] mb-1.5">
                  Availability / Date
                </label>
                <select
                  value={availability}
                  onChange={(e) => setAvailability(e.target.value)}
                  className="w-full bg-white dark:bg-[#111620] border border-[#DED8CF] dark:border-[#30394A] rounded-lg px-2.5 py-1.5 text-xs text-[#181A1F] dark:text-[#F4F6FA] outline-none"
                >
                  <option value="all">Any Day</option>
                  <option value="today">Today</option>
                  <option value="tomorrow">Tomorrow</option>
                  <option value="weekend">This Weekend</option>
                </select>
              </div>
            </div>

            {/* Toggles & Reset */}
            <div className="flex flex-wrap items-center justify-between gap-3 pt-2 border-t border-[#DED8CF] dark:border-[#30394A]">
              <div className="flex items-center gap-4">
                <label className="flex items-center gap-2 cursor-pointer text-xs text-[#181A1F] dark:text-[#F4F6FA]">
                  <input
                    type="checkbox"
                    checked={openNow}
                    onChange={(e) => setOpenNow(e.target.checked)}
                    className="accent-[#B93650] w-3.5 h-3.5 rounded"
                  />
                  <span>Open now</span>
                </label>
                <label className="flex items-center gap-2 cursor-pointer text-xs text-[#181A1F] dark:text-[#F4F6FA]">
                  <input
                    type="checkbox"
                    checked={verifiedOnly}
                    onChange={(e) => setVerifiedOnly(e.target.checked)}
                    className="accent-[#B93650] w-3.5 h-3.5 rounded"
                  />
                  <span>Verified providers only</span>
                </label>
              </div>

              <button
                type="button"
                onClick={() => {
                  setCategory('All');
                  setSelectedService('All Services');
                  setRadius(25);
                  setMinRating(0);
                  setPriceRange('all');
                  setAvailability('all');
                  setOpenNow(false);
                  setVerifiedOnly(false);
                  setSort('Recommended');
                  setPage(1);
                }}
                className="text-xs text-[#626B7A] dark:text-[#A2AEC1] hover:text-[#B93650] dark:hover:text-[#E8546A] font-semibold transition-colors flex items-center gap-1"
              >
                <RotateCcw className="w-3 h-3" />
                <span>Reset All Filters</span>
              </button>
            </div>
          </div>
        )}

        {/* Search Intent Notification */}
        {isIntentQuery && (
          <div className="max-w-7xl mx-auto mt-2 pt-2 border-t border-[#DED8CF]/60 dark:border-[#30394A]/60 flex items-center gap-2 text-[11px] text-[#4B5563] dark:text-[#CBD2DE]">
            <Sparkles className="w-3.5 h-3.5 text-[#B93650] dark:text-[#E8546A]" />
            <span>Search intent recognized: spatial proximity, service matching &amp; ranking active for query</span>
          </div>
        )}
      </div>

      {/* ================================================================= */}
      {/* 2. MAIN SPLIT LAYOUT: LIST | MAP                                 */}
      {/* ================================================================= */}
      <div className="flex-1 flex max-w-7xl w-full mx-auto p-4 sm:p-6 lg:p-8 gap-6">
        {/* Left Column: Results List */}
        <div
          className={`flex-1 flex flex-col space-y-4 ${
            mobileView === 'map' ? 'hidden lg:flex' : 'flex'
          }`}
        >
          {/* Result Count Headline (Phase 14 Distance Reference Truthfulness) */}
          <div className="flex items-center justify-between text-xs text-[#626B7A] dark:text-[#A2AEC1]">
            <span>
              Showing <strong className="text-[#181A1F] dark:text-[#F4F6FA]">{providers.length}</strong> of{' '}
              <strong className="text-[#181A1F] dark:text-[#F4F6FA]">{totalCount}</strong> verified venues{' '}
              {userGpsCoords
                ? 'near your location'
                : searchBounds
                ? 'in selected map area'
                : `in ${currentLocation.label}`}
            </span>
            {sort === 'Recommended' && (
              <span className="flex items-center gap-1 text-[#B93650] dark:text-[#E8546A] font-semibold text-[11px]">
                <Sparkles className="w-3 h-3" /> Deterministic Multi-Signal Ranking
              </span>
            )}
          </div>

          {/* Error State */}
          {error && (
            <div className="p-6 rounded-2xl bg-white dark:bg-[#1A2130] border border-[#C74444]/40 text-center space-y-3">
              <p className="text-sm font-bold text-[#C74444] dark:text-[#F87171]">We couldn&apos;t load discovery results.</p>
              <button
                type="button"
                onClick={fetchProviders}
                className="px-4 py-2 bg-[#B93650] hover:bg-[#A52E46] text-white text-xs font-bold rounded-lg transition-colors"
              >
                Retry
              </button>
            </div>
          )}

          {/* Loading Skeletons */}
          {isLoading && !error && (
            <div className="space-y-4">
              {[1, 2, 3].map((i) => (
                <ProviderCardSkeleton key={i} />
              ))}
            </div>
          )}

          {/* Empty State */}
          {!isLoading && !error && providers.length === 0 && (
            <div className="py-16 p-8 rounded-2xl bg-white dark:bg-[#111620] border border-[#DED8CF] dark:border-[#30394A] text-center space-y-4">
              <div className="w-16 h-16 rounded-2xl bg-[#FAF8F5] dark:bg-[#1A2130] border border-[#DED8CF] dark:border-[#30394A] flex items-center justify-center mx-auto text-[#626B7A] dark:text-[#A2AEC1]">
                <Compass className="w-8 h-8" />
              </div>
              <h3 className="font-heading text-lg font-bold text-[#181A1F] dark:text-[#F4F6FA]">
                No providers found in this area
              </h3>
              <p className="text-xs text-[#4B5563] dark:text-[#CBD2DE] max-w-sm mx-auto">
                Try expanding your search radius, selecting a neighboring city, or resetting category filters.
              </p>
              <button
                type="button"
                onClick={() => {
                  setCurrentLocation({ label: 'Ahmedabad', city: 'Ahmedabad', lat: 23.0225, lng: 72.5714 });
                  setCategory('All');
                  setSelectedService('All Services');
                  setQuery('');
                  setDebouncedQuery('');
                  setPage(1);
                }}
                className="px-5 py-2.5 bg-[#B93650] hover:bg-[#A52E46] text-white text-xs font-bold rounded-lg transition-colors"
              >
                Reset Filters
              </button>
            </div>
          )}

          {/* Reusable Data-Driven Provider Cards (Phase 11) */}
          {!isLoading &&
            !error &&
            providers.map((p) => {
              const isSelected = p.id === selectedProviderId;
              const isHovered = p.id === hoveredProviderId;

              return (
                <div
                  key={p.id}
                  ref={(el) => {
                    cardRefs.current[p.id] = el;
                  }}
                >
                  <ProviderCard
                    provider={p}
                    isSelected={isSelected}
                    isHovered={isHovered}
                    onSelect={() => handleSelectProvider(p)}
                    onHover={(hovered) => setHoveredProviderId(hovered ? p.id : null)}
                  />
                </div>
              );
            })}

          {/* Pagination Controls */}
          {totalPages > 1 && (
            <div className="pt-4 flex items-center justify-between border-t border-[#DED8CF] dark:border-[#30394A]">
              <button
                type="button"
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                disabled={page <= 1}
                className="px-3.5 py-2 rounded-lg bg-white dark:bg-[#151B27] border border-[#DED8CF] dark:border-[#30394A] text-xs font-semibold text-[#181A1F] dark:text-[#F4F6FA] disabled:opacity-40 flex items-center gap-1 hover:bg-[#FAF8F5]"
              >
                <ChevronLeft className="w-3.5 h-3.5" /> Previous
              </button>
              <span className="text-xs text-[#626B7A] dark:text-[#A2AEC1]">
                Page {page} of {totalPages}
              </span>
              <button
                type="button"
                onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                disabled={page >= totalPages}
                className="px-3.5 py-2 rounded-lg bg-white dark:bg-[#151B27] border border-[#DED8CF] dark:border-[#30394A] text-xs font-semibold text-[#181A1F] dark:text-[#F4F6FA] disabled:opacity-40 flex items-center gap-1 hover:bg-[#FAF8F5]"
              >
                Next <ChevronRight className="w-3.5 h-3.5" />
              </button>
            </div>
          )}
        </div>

        {/* Right Column: Real Interactive Map */}
        <div
          className={`w-full lg:w-[480px] xl:w-[540px] shrink-0 h-[650px] sticky top-36 ${
            mobileView === 'list' ? 'hidden lg:block' : 'block h-[calc(100vh-140px)]'
          }`}
        >
          <InteractiveMap
            providers={providers}
            selectedProviderId={selectedProviderId}
            hoveredProviderId={hoveredProviderId}
            onSelectProvider={handleSelectProvider}
            onHoverProvider={setHoveredProviderId}
            onSearchThisArea={(bounds) => {
              setSearchBounds({
                swLat: bounds.swLat,
                swLng: bounds.swLng,
                neLat: bounds.neLat,
                neLng: bounds.neLng,
              });
              setCurrentLocation((prev) => ({
                ...prev,
                lat: bounds.center.lat,
                lng: bounds.center.lng,
                label: `Map Viewport Area`,
              }));
              setPage(1);
            }}

            center={mapCenter}
            userLocation={userGpsCoords}
          />
        </div>
      </div>

      {/* Location Experience Modal */}
      <LocationControlModal
        isOpen={isLocationModalOpen}
        onClose={() => setIsLocationModalOpen(false)}
        currentSelection={currentLocation}
        onSelectLocation={handleLocationSelect}
      />

      {/* Floating Sticky Map / List Toggle for Mobile */}
      <div className="lg:hidden fixed bottom-6 left-1/2 -translate-x-1/2 z-40">
        <button
          type="button"
          onClick={() => setMobileView(mobileView === 'list' ? 'map' : 'list')}
          className="min-h-[48px] px-6 py-3 rounded-full bg-[#B93650] hover:bg-[#A52E46] text-white text-xs font-bold shadow-2xl flex items-center gap-2 border border-white/20 active:scale-95 transition-all"
        >
          {mobileView === 'list' ? (
            <>
              <MapIcon className="w-4 h-4" />
              <span>View Map ({providers.length})</span>
            </>
          ) : (
            <>
              <ListIcon className="w-4 h-4" />
              <span>View List</span>
            </>
          )}
        </button>
      </div>
    </div>
  );
};
