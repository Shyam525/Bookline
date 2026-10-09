import React, { useState, useEffect, useRef } from 'react';
import { useSearchParams, Link, useParams, useNavigate } from 'react-router-dom';
import { discoveryApi, ProviderCard, ProviderSearchResponse } from '../../services/api/discovery';
import { InteractiveMap } from '../../components/maps/InteractiveMap';
import {
  LocationControlModal,
  LocationSelection,
} from '../../components/discovery/LocationControlModal';
import { ProviderImage } from '../../components/common/ProviderImage';
import {
  Search,
  MapPin,
  SlidersHorizontal,
  Star,
  CheckCircle,
  Clock,
  ArrowUpDown,
  Compass,
  Map as MapIcon,
  List as ListIcon,
  ChevronLeft,
  ChevronRight,
  Sparkles,
  Navigation,
  ExternalLink,
  Sliders,
  X,
  AlertTriangle,
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

  const [providers, setProviders] = useState<ProviderCard[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Category-specific services dictionary (Section 36: Only show filters relevant to current category)
  const categoryServicesMap: Record<string, string[]> = {
    'All': ['All Services', 'Haircut', 'Facial', 'Balayage', 'Massage', 'Dental Cleaning', 'Reformer Pilates', 'Headshots', 'Legal Advisory'],
    'Beauty': ['All Services', 'Facial & Skin Ritual', 'Chemical Peel', 'Aromatherapy Massage', 'Body Wrap', 'Manicure & Pedicure'],
    'Hair': ['All Services', 'Haircut & Styling', 'Balayage & Color', 'Keratin Smoothing', 'Scalp Therapy', 'Blowout'],
    'Healthcare': ['All Services', 'Dental Cleaning', 'Laser Teeth Whitening', 'Porcelain Veneers', 'Orthodontic Exam', 'Skin Consultation'],
    'Fitness': ['All Services', 'Reformer Pilates', 'HIIT Circuit', 'Private Athletic Coaching', 'Yoga Vinyasa', 'Sports Recovery'],
    'Photography': ['All Services', 'Corporate Headshots', 'Editorial Campaign', 'Family Heirloom Portrait', 'Fashion Lookbook', 'Product Shoot'],
    'Professional': ['All Services', 'Commercial Contracts', 'Intellectual Property Filing', 'Tax Advisory', 'Corporate Restructuring'],
  };

  // Filters from query params and state (Section 35 & 36)
  const [query, setQuery] = useState(searchParams.get('q') || '');
  const [category, setCategory] = useState(initialCategory);
  const [selectedService, setSelectedService] = useState<string>('All Services');
  const [radius, setRadius] = useState<number>(Number(searchParams.get('radius')) || 25);
  const [minRating, setMinRating] = useState<number>(Number(searchParams.get('minRating')) || 0);
  const [priceRange, setPriceRange] = useState<string>('all'); // 'all', 'under-500', '500-1500', '1500-3000', '3000+'
  const [availability, setAvailability] = useState<string>('all'); // 'all', 'today', 'tomorrow', 'weekend'
  const [openNow, setOpenNow] = useState(false);
  const [verifiedOnly, setVerifiedOnly] = useState(false);
  const [isFiltersOpen, setIsFiltersOpen] = useState(false);
  const [sort, setSort] = useState(searchParams.get('sort') || 'Recommended');
  const [page, setPage] = useState<number>(Number(searchParams.get('page')) || 1);

  // When category changes, reset selectedService to 'All Services' (Section 36 rule)
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

  // Location State (Section 25 & 26)
  const [isLocationModalOpen, setIsLocationModalOpen] = useState(false);
  const [currentLocation, setCurrentLocation] = useState<LocationSelection>(() => {
    const cityParam = searchParams.get('city') || 'Ahmedabad';
    const latParam = Number(searchParams.get('lat')) || 23.0225;
    const lngParam = Number(searchParams.get('lng')) || 72.5714;
    return {
      label: cityParam,
      city: cityParam,
      lat: latParam,
      lng: lngParam,
    };
  });
  const [userGpsCoords, setUserGpsCoords] = useState<{ lat: number; lng: number } | null>(() => {
    const lat = searchParams.get('lat');
    const lng = searchParams.get('lng');
    return lat && lng ? { lat: Number(lat), lng: Number(lng) } : null;
  });

  // Map / List Mobile Toggle
  const [mobileView, setMobileView] = useState<'list' | 'map'>('list');
  const [selectedProviderId, setSelectedProviderId] = useState<string | null>(null);
  const [hoveredProviderId, setHoveredProviderId] = useState<string | null>(null);

  // List card refs for synchronized scrolling (Section 28 & 101)
  const cardRefs = useRef<Record<string, HTMLDivElement | null>>({});

  // Sync category if URL param changes
  useEffect(() => {
    const fromParam = resolveCategoryFromSlug(slug || routeCategory);
    if (fromParam !== 'All') {
      setCategory(fromParam);
      setSelectedService('All Services');
    }
  }, [slug, routeCategory]);

  const fetchProviders = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const lat = userGpsCoords?.lat ?? (currentLocation.lat || undefined);
      const lng = userGpsCoords?.lng ?? (currentLocation.lng || undefined);

      let maxPriceVal: number | undefined;
      if (priceRange === 'under-500') maxPriceVal = 500;
      else if (priceRange === '500-1500') maxPriceVal = 1500;
      else if (priceRange === '1500-3000') maxPriceVal = 3000;

      const res = await discoveryApi.searchProviders({
        q: query || undefined,
        category: category !== 'All' ? category : undefined,
        service: selectedService !== 'All Services' ? selectedService : undefined,
        city: currentLocation.city !== 'All' ? currentLocation.city : undefined,
        lat,
        lng,
        radius: radius || undefined,
        minRating: minRating > 0 ? minRating : undefined,
        maxPrice: maxPriceVal,
        availability: availability !== 'all' ? availability : undefined,
        sort,
        page,
        pageSize: 10,
      });

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
      setError(err.message || 'We could not load discovery results.');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchProviders();
  }, [
    category,
    selectedService,
    currentLocation.city,
    currentLocation.lat,
    currentLocation.lng,
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
    setPage(1);
    fetchProviders();
  };

  const handleLocationSelect = (loc: LocationSelection, saveToProfile?: boolean) => {
    setCurrentLocation(loc);
    if (loc.isCurrentLocation) {
      setUserGpsCoords({ lat: loc.lat, lng: loc.lng });
    } else {
      setUserGpsCoords(null);
    }
    setPage(1);
  };

  // Synchronized Selection: clicking map marker scrolls card into view (Section 28)
  const handleSelectProvider = (provider: ProviderCard) => {
    setSelectedProviderId(provider.id);
    const element = cardRefs.current[provider.id];
    if (element) {
      element.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    }
  };

  // Center coordinates for map
  const mapCenter = { lat: currentLocation.lat, lng: currentLocation.lng };

  // Detect query intent keywords for visual feedback (Section 33)
  const isIntentQuery =
    query.toLowerCase().includes('near me') ||
    query.toLowerCase().includes('best') ||
    query.toLowerCase().includes('satellite') ||
    query.toLowerCase().includes('bodakdev') ||
    query.toLowerCase().includes('bandra');

  return (
    <div className="flex-1 flex flex-col bg-[#090B10] text-[#F4F6FA]">
      {/* ================================================================= */}
      {/* 1. SEARCH HEADER & LOCATION CONTROL (Section 106 Mobile Priority) */}
      {/* ================================================================= */}
      <div className="border-b border-[#273142] bg-[#111620] px-4 lg:px-8 py-3.5 sticky top-16 z-30 shadow-md">
        <div className="max-w-7xl mx-auto flex flex-col gap-3">
          {/* Priority 1: Search & Priority 2: Location on Mobile */}
          <div className="flex flex-col lg:flex-row items-stretch lg:items-center justify-between gap-3">
            {/* Search Input Bar (Priority 1) */}
            <form onSubmit={handleSearchSubmit} className="flex-1 w-full flex flex-col sm:flex-row items-stretch sm:items-center gap-2">
              <div className="relative flex-1">
                <Search className="w-4 h-4 text-[#8F9AAF] absolute left-3.5 top-1/2 -translate-y-1/2" />
                <input
                  type="text"
                  value={query}
                  onChange={(e) => setQuery(e.target.value)}
                  placeholder="Search salons, dentists, massage, reformer pilates, or keywords..."
                  className="w-full min-h-[48px] bg-[#151B27] border border-[#273142] rounded-xl pl-10 pr-9 py-2.5 text-xs text-[#F4F6FA] placeholder-[#8F9AAF] focus:border-[#E8546A] outline-none transition-colors"
                />
                {query && (
                  <button
                    type="button"
                    onClick={() => { setQuery(''); setPage(1); }}
                    className="absolute right-3 top-1/2 -translate-y-1/2 p-1 text-[#8F9AAF] hover:text-white"
                  >
                    <X className="w-3.5 h-3.5" />
                  </button>
                )}
              </div>

              {/* Location Selector Button (Priority 2: Touch-friendly min 44px) */}
              <button
                type="button"
                onClick={() => setIsLocationModalOpen(true)}
                className="min-h-[48px] sm:min-h-[40px] px-4 py-2.5 rounded-xl bg-[#151B27] hover:bg-[#1A2130] border border-[#273142] hover:border-[#E8546A] text-xs font-semibold text-[#F4F6FA] transition-colors flex items-center justify-between sm:justify-start gap-2 flex-shrink-0 shadow-sm"
                title="Change search city, neighborhood, or postal code"
              >
                <div className="flex items-center gap-2 truncate">
                  <MapPin className="w-4 h-4 text-[#E8546A] flex-shrink-0" />
                  <span className="truncate font-medium">{currentLocation.label}</span>
                </div>
                <span className="text-[11px] text-[#E8546A] font-bold bg-[#E8546A]/10 px-2 py-0.5 rounded-md">Change</span>
              </button>

              <button
                type="submit"
                className="min-h-[48px] sm:min-h-[40px] px-6 py-2.5 bg-[#E8546A] hover:bg-[#F06A7D] active:bg-[#C94358] text-white text-xs font-bold rounded-xl transition-all shadow-md flex-shrink-0 flex items-center justify-center gap-1.5"
              >
                <Search className="w-3.5 h-3.5" />
                <span>Search</span>
              </button>
            </form>

            {/* Controls: Filter Drawer & Sort (Touch-friendly targets) */}
            <div className="flex items-center gap-2 justify-between lg:justify-end overflow-x-auto pb-1 lg:pb-0">
              <button
                type="button"
                onClick={() => setIsFiltersOpen((prev) => !prev)}
                className={`min-h-[44px] sm:min-h-[38px] px-4 py-2 rounded-xl border text-xs font-semibold flex items-center gap-2 transition-colors flex-shrink-0 ${
                  isFiltersOpen || activeFiltersCount > 0
                    ? 'bg-[#E8546A]/15 border-[#E8546A] text-[#E8546A]'
                    : 'bg-[#151B27] border-[#273142] text-[#F4F6FA] hover:border-[#8F9AAF]'
                }`}
              >
                <SlidersHorizontal className="w-4 h-4" />
                <span>Filters</span>
                {activeFiltersCount > 0 && (
                  <span className="w-4 h-4 rounded-full bg-[#E8546A] text-white text-[10px] font-bold flex items-center justify-center">
                    {activeFiltersCount}
                  </span>
                )}
              </button>

              <div className="flex items-center gap-1.5 bg-[#151B27] border border-[#273142] rounded-xl px-3 py-2 min-h-[44px] sm:min-h-[38px] flex-shrink-0">
                <ArrowUpDown className="w-3.5 h-3.5 text-[#8F9AAF]" />
                <select
                  value={sort}
                  onChange={(e) => setSort(e.target.value)}
                  className="bg-transparent text-xs font-medium text-[#F4F6FA] outline-none cursor-pointer"
                >
                  <option value="Recommended" className="bg-[#111620]">Recommended</option>
                  <option value="Nearest" className="bg-[#111620]">Nearest</option>
                  <option value="Top rated" className="bg-[#111620]">Top rated</option>
                  <option value="Most reviewed" className="bg-[#111620]">Most reviewed</option>
                  <option value="Earliest available" className="bg-[#111620]">Earliest available</option>
                  <option value="Lowest price" className="bg-[#111620]">Lowest price</option>
                  <option value="Highest price" className="bg-[#111620]">Highest price</option>
                </select>
              </div>
            </div>
          </div>

          {/* Priority 3: Categories Horizontal Touch Carousel (Section 106) */}
          <div className="flex items-center gap-2 overflow-x-auto no-scrollbar py-1 -mx-4 px-4 sm:mx-0 sm:px-0">
            {categories.map((cat) => {
              const isActive = category === cat;
              return (
                <button
                  key={cat}
                  onClick={() => handleCategoryChange(cat)}
                  className={`min-h-[44px] px-4 py-2 rounded-xl text-xs font-semibold whitespace-nowrap transition-all flex items-center gap-1.5 flex-shrink-0 ${
                    isActive
                      ? 'bg-[#E8546A] text-white shadow-lg shadow-[#E8546A]/25 border border-[#E8546A]'
                      : 'bg-[#151B27] text-[#C3CAD6] border border-[#273142] hover:bg-[#1A2130]'
                  }`}
                >
                  <span>{cat === 'All' ? '✨ All Venues' : cat}</span>
                </button>
              );
            })}
          </div>
        </div>

        {/* Section 36: Expandable Filters Panel (Relevant to current category) */}
        {isFiltersOpen && (
          <div className="max-w-7xl mx-auto mt-3 pt-3 border-t border-[#273142] p-4 bg-[#151B27] rounded-[12px] space-y-4 animate-fadeIn">
            {/* 1. Services relevant to current category */}
            <div>
              <div className="flex items-center justify-between mb-2">
                <span className="text-xs font-bold text-[#F4F6FA] uppercase tracking-wider">
                  Services for {category === 'All' ? 'All Categories' : category}
                </span>
                <span className="text-[11px] text-[#8F9AAF]">
                  (Only showing services relevant to {category})
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
                    className={`px-3 py-1 rounded-[6px] text-xs font-medium transition-all ${
                      selectedService === srv
                        ? 'bg-[#E8546A] text-white font-semibold shadow-sm'
                        : 'bg-[#111620] border border-[#273142] text-[#C3CAD6] hover:border-[#8F9AAF]'
                    }`}
                  >
                    {srv}
                  </button>
                ))}
              </div>
            </div>

            {/* 2. Grid of Secondary Filters: Distance, Rating, Price, Availability, Toggles */}
            <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-4 pt-2 border-t border-[#273142]">
              {/* Distance Filter */}
              <div>
                <label className="block text-[11px] font-semibold text-[#8F9AAF] mb-1.5">
                  Distance Radius
                </label>
                <div className="flex items-center gap-1">
                  {[5, 10, 25, 50].map((d) => (
                    <button
                      key={d}
                      type="button"
                      onClick={() => setRadius(d)}
                      className={`flex-1 py-1 text-xs rounded-[6px] border ${
                        radius === d
                          ? 'bg-[#E8546A] border-[#E8546A] text-white font-bold'
                          : 'bg-[#111620] border-[#273142] text-[#C3CAD6] hover:bg-[#1A2130]'
                      }`}
                    >
                      {d}km
                    </button>
                  ))}
                </div>
              </div>

              {/* Rating Filter */}
              <div>
                <label className="block text-[11px] font-semibold text-[#8F9AAF] mb-1.5">
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
                      className={`flex-1 py-1 text-xs rounded-[6px] border ${
                        minRating === r.val
                          ? 'bg-[#E8546A] border-[#E8546A] text-white font-bold'
                          : 'bg-[#111620] border-[#273142] text-[#C3CAD6] hover:bg-[#1A2130]'
                      }`}
                    >
                      {r.label}
                    </button>
                  ))}
                </div>
              </div>

              {/* Price Filter */}
              <div>
                <label className="block text-[11px] font-semibold text-[#8F9AAF] mb-1.5">
                  Starting Price
                </label>
                <select
                  value={priceRange}
                  onChange={(e) => setPriceRange(e.target.value)}
                  className="w-full bg-[#111620] border border-[#273142] rounded-[6px] px-2.5 py-1.5 text-xs text-[#F4F6FA] outline-none"
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
                <label className="block text-[11px] font-semibold text-[#8F9AAF] mb-1.5">
                  Availability / Date
                </label>
                <select
                  value={availability}
                  onChange={(e) => setAvailability(e.target.value)}
                  className="w-full bg-[#111620] border border-[#273142] rounded-[6px] px-2.5 py-1.5 text-xs text-[#F4F6FA] outline-none"
                >
                  <option value="all">Any Day</option>
                  <option value="today">Today</option>
                  <option value="tomorrow">Tomorrow</option>
                  <option value="weekend">This Weekend</option>
                </select>
              </div>
            </div>

            {/* 3. Toggles & Reset Button */}
            <div className="flex flex-wrap items-center justify-between gap-3 pt-2 border-t border-[#273142]">
              <div className="flex items-center gap-4">
                <label className="flex items-center gap-2 cursor-pointer text-xs text-[#F4F6FA]">
                  <input
                    type="checkbox"
                    checked={openNow}
                    onChange={(e) => setOpenNow(e.target.checked)}
                    className="accent-[#E8546A] w-3.5 h-3.5 rounded"
                  />
                  <span>Open now</span>
                </label>
                <label className="flex items-center gap-2 cursor-pointer text-xs text-[#F4F6FA]">
                  <input
                    type="checkbox"
                    checked={verifiedOnly}
                    onChange={(e) => setVerifiedOnly(e.target.checked)}
                    className="accent-[#E8546A] w-3.5 h-3.5 rounded"
                  />
                  <span>Provider verification only</span>
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
                className="text-xs text-[#8F9AAF] hover:text-[#E8546A] font-semibold transition-colors"
              >
                Reset All Filters
              </button>
            </div>
          </div>
        )}

        {/* Search Intent Notification Pill (Section 33) */}
        {isIntentQuery && (
          <div className="max-w-7xl mx-auto mt-2 pt-2 border-t border-[#273142]/60 flex items-center gap-2 text-[11px] text-[#C3CAD6]">
            <Sparkles className="w-3.5 h-3.5 text-[#E8546A]" />
            <span>Search intent recognized: spatial proximity, service matching &amp; ranking active for query</span>
          </div>
        )}
      </div>

      {/* ================================================================= */}
      {/* 2. MAIN SPLIT LAYOUT: LIST | MAP (Section 24 Desktop & Mobile)     */}
      {/* ================================================================= */}
      <div className="flex-1 flex max-w-7xl w-full mx-auto p-4 sm:p-6 lg:p-8 gap-6">
        {/* Left Column: Results List */}
        <div
          className={`flex-1 flex flex-col space-y-4 ${
            mobileView === 'map' ? 'hidden lg:flex' : 'flex'
          }`}
        >
          {/* Result Count Headline */}
          <div className="flex items-center justify-between text-xs text-[#8F9AAF]">
            <span>
              Showing <strong className="text-[#F4F6FA]">{providers.length}</strong> of{' '}
              <strong className="text-[#F4F6FA]">{totalCount}</strong> verified venues in {currentLocation.city}
            </span>
            {sort === 'Recommended' && (
              <span className="flex items-center gap-1 text-[#E8546A] font-semibold text-[11px]">
                <Sparkles className="w-3 h-3" /> Section 34 Multi-Signal Ranking
              </span>
            )}
          </div>

          {/* Error State */}
          {error && (
            <div className="p-6 rounded-[16px] bg-[#1A2130] border border-[#F87171]/40 text-center space-y-2">
              <p className="text-sm font-bold text-[#F87171]">We couldn't load discovery results.</p>
              <button
                onClick={fetchProviders}
                className="px-4 py-2 bg-[#E8546A] hover:bg-[#F06A7D] text-white text-xs font-bold rounded-[8px]"
              >
                Retry
              </button>
            </div>
          )}

          {/* Loading Skeletons */}
          {isLoading && !error && (
            <div className="space-y-4">
              {[1, 2, 3].map((i) => (
                <div
                  key={i}
                  className="p-5 rounded-[16px] bg-[#111620] border border-[#273142] animate-pulse space-y-3"
                >
                  <div className="h-4 bg-[#1A2130] rounded w-1/3" />
                  <div className="h-3 bg-[#1A2130] rounded w-1/2" />
                  <div className="h-10 bg-[#1A2130] rounded w-full" />
                </div>
              ))}
            </div>
          )}

          {/* Empty State */}
          {!isLoading && !error && providers.length === 0 && (
            <div className="py-16 p-8 rounded-[16px] bg-[#111620] border border-[#273142] text-center space-y-4">
              <div className="w-16 h-16 rounded-[12px] bg-[#1A2130] border border-[#273142] flex items-center justify-center mx-auto text-[#8F9AAF]">
                <Compass className="w-8 h-8" />
              </div>
              <h3 className="font-heading text-lg font-bold text-[#F4F6FA]">No providers found in this area</h3>
              <p className="text-xs text-[#C3CAD6] max-w-sm mx-auto">
                Try expanding your search radius, selecting a neighboring city, or resetting category filters.
              </p>
              <button
                onClick={() => {
                  setCurrentLocation({ label: 'Ahmedabad', city: 'Ahmedabad', lat: 23.0225, lng: 72.5714 });
                  setCategory('All');
                  setSelectedService('All Services');
                  setQuery('');
                  setPage(1);
                }}
                className="px-5 py-2.5 bg-[#E8546A] hover:bg-[#F06A7D] text-white text-xs font-bold rounded-[8px] transition-colors"
              >
                Reset Filters
              </button>
            </div>
          )}

          {/* Section 37: Provider Card - quick communication format */}
          {!isLoading &&
            !error &&
            providers.map((p) => {
              const isSelected = p.id === selectedProviderId;

              // Neighborhood / location display
              const locationArea = p.address?.split(',')[0] || p.city;
              const formattedDistance = p.distanceKm != null ? `${p.distanceKm} km` : '1.2 km';
              const servicesString =
                p.servicesSummary && p.servicesSummary.length > 0
                  ? p.servicesSummary.slice(0, 3).join(' · ')
                  : 'Haircut · Facial · Colour';

              const isHovered = p.id === hoveredProviderId;

              return (
                <div
                  key={p.id}
                  ref={(el) => { cardRefs.current[p.id] = el; }}
                  onClick={() => navigate(`/business/${p.slug}`)}
                  onMouseEnter={() => setHoveredProviderId(p.id)}
                  onMouseLeave={() => setHoveredProviderId(null)}
                  className={`p-5 rounded-[16px] border transition-all cursor-pointer flex flex-col sm:flex-row gap-5 ${
                    isSelected || isHovered
                      ? 'border-[#E8546A] shadow-xl shadow-[#E8546A]/20 bg-[#151B27] ring-1 ring-[#E8546A]/40 -translate-y-0.5'
                      : 'border-[#273142] hover:border-[#344054] bg-[#111620] hover:bg-[#151B27]'
                  }`}
                >
                  {/* Provider Logo / Cover Image (Section 105: Resilient ProviderImage) */}
                  <div className="w-full sm:w-44 h-36 rounded-[14px] bg-[#1A2130] border border-[#273142] overflow-hidden flex-shrink-0 relative">
                    <ProviderImage
                      src={p.coverImageUrl}
                      alt={p.name}
                      type="cover"
                      category={p.category}
                      className="w-full h-full object-cover"
                      loading="lazy"
                    />
                    <div className="absolute top-2 left-2 bg-[#090B10]/85 backdrop-blur-md px-2.5 py-1 rounded-[6px] text-[10px] font-semibold text-[#ECEFFE] border border-[#212638]">
                      {p.category}
                    </div>
                  </div>

                  {/* Provider Card Details (Section 37) */}
                  <div className="flex-1 flex flex-col justify-between">
                    <div>
                      {/* Name & Verification Status */}
                      <div className="flex items-center justify-between gap-2 mb-1">
                        <div className="flex items-center gap-1.5 truncate">
                          <h3 className="font-heading text-lg font-bold text-[#F4F6FA] truncate">
                            {p.name}
                          </h3>
                          {p.isVerified && (
                            <span title="Verified Business" className="inline-flex items-center">
                              <CheckCircle className="w-4 h-4 text-[#34D399] flex-shrink-0" />
                            </span>
                          )}
                        </div>
                      </div>

                      {/* Rating · Review Count */}
                      <div className="flex items-center gap-1 text-xs text-[#ECEFFE] font-medium mb-1">
                        <span className="text-[#FBBF24] font-bold">{p.rating.toFixed(1)} ★</span>
                        <span className="text-[#8F9AAF]">&middot;</span>
                        <span className="text-[#8F9AAF]">{p.reviewCount} reviews</span>
                      </div>

                      {/* Distance · Location */}
                      <div className="flex items-center gap-1.5 text-xs text-[#8F9AAF] mb-2">
                        <span className="text-[#34D399] font-medium">{formattedDistance}</span>
                        <span>&middot;</span>
                        <span className="truncate">{locationArea}</span>
                      </div>

                      {/* Services: Haircut · Facial · Colour */}
                      <div className="text-xs text-[#C3CAD6] font-medium mb-2.5 truncate">
                        {servicesString}
                      </div>
                    </div>

                    {/* Next Available, Starting Price, [View] [Book] CTA */}
                    <div className="pt-3 border-t border-[#273142] flex flex-wrap items-center justify-between gap-3">
                      <div className="flex items-center gap-4">
                        <div>
                          <span className="text-[11px] text-[#8F9AAF] block">Next available</span>
                          <span className="text-xs font-semibold text-[#F4F6FA] flex items-center gap-1">
                            <Clock className="w-3.5 h-3.5 text-[#FBBF24]" />
                            {p.nextAvailableSlot || '10:30 AM'}
                          </span>
                        </div>
                        <div className="border-l border-[#273142] pl-3">
                          <span className="text-[11px] text-[#8F9AAF] block">From</span>
                          <span className="text-sm font-bold text-[#34D399]">
                            From ₹{p.startingPrice || 600}
                          </span>
                        </div>
                      </div>

                      <div className="flex items-center gap-2 w-full sm:w-auto pt-2 sm:pt-0">
                        <Link
                          to={`/business/${p.slug}`}
                          className="flex-1 sm:flex-initial min-h-[44px] px-4 py-2 rounded-xl border border-[#273142] hover:bg-[#1A2130] text-xs font-semibold text-[#F4F6FA] transition-colors flex items-center justify-center"
                        >
                          View Profile
                        </Link>
                        <Link
                          to={`/business/${p.slug}?book=true`}
                          className="flex-1 sm:flex-initial min-h-[44px] px-5 py-2 rounded-xl bg-[#E8546A] hover:bg-[#F06A7D] text-xs font-bold text-white transition-all shadow-md shadow-[#E8546A]/20 flex items-center justify-center gap-1.5"
                        >
                          Book Now
                        </Link>
                      </div>
                    </div>
                  </div>
                </div>
              );
            })}

          {/* Pagination Controls */}
          {totalPages > 1 && (
            <div className="pt-4 flex items-center justify-between border-t border-[#273142]">
              <button
                type="button"
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                disabled={page <= 1}
                className="px-3 py-1.5 rounded-[8px] bg-[#151B27] border border-[#273142] text-xs font-semibold text-[#F4F6FA] disabled:opacity-40 flex items-center gap-1"
              >
                <ChevronLeft className="w-3.5 h-3.5" /> Previous
              </button>
              <span className="text-xs text-[#8F9AAF]">
                Page {page} of {totalPages}
              </span>
              <button
                type="button"
                onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                disabled={page >= totalPages}
                className="px-3 py-1.5 rounded-[8px] bg-[#151B27] border border-[#273142] text-xs font-semibold text-[#F4F6FA] disabled:opacity-40 flex items-center gap-1"
              >
                Next <ChevronRight className="w-3.5 h-3.5" />
              </button>
            </div>
          )}
        </div>

        {/* Right Column: Real Interactive Map (Section 28) */}
        <div
          className={`w-full lg:w-[480px] xl:w-[540px] flex-shrink-0 h-[650px] sticky top-36 ${
            mobileView === 'list' ? 'hidden lg:block' : 'block h-[calc(100vh-140px)]'
          }`}
        >
          <InteractiveMap
            providers={providers}
            selectedProviderId={selectedProviderId}
            hoveredProviderId={hoveredProviderId}
            onSelectProvider={handleSelectProvider}
            onHoverProvider={setHoveredProviderId}
            onSearchThisArea={(viewport) => {
              if (viewport) {
                setCurrentLocation((prev) => ({
                  ...prev,
                  lat: viewport.lat,
                  lng: viewport.lng,
                  label: `Area (${viewport.lat.toFixed(2)}, ${viewport.lng.toFixed(2)})`,
                }));
              }
              fetchProviders();
            }}
            center={mapCenter}
            userLocation={userGpsCoords}
          />
        </div>
      </div>

      {/* Location Experience Modal (Section 25, 26, 27) */}
      <LocationControlModal
        isOpen={isLocationModalOpen}
        onClose={() => setIsLocationModalOpen(false)}
        currentSelection={currentLocation}
        onSelectLocation={handleLocationSelect}
      />

      {/* Section 106: Floating Sticky Map / List Toggle for Mobile */}
      <div className="lg:hidden fixed bottom-6 left-1/2 -translate-x-1/2 z-40">
        <button
          type="button"
          onClick={() => setMobileView(mobileView === 'list' ? 'map' : 'list')}
          className="min-h-[48px] px-6 py-3 rounded-full bg-[#E8546A] hover:bg-[#D44359] text-white text-xs font-bold shadow-2xl shadow-[#E8546A]/50 flex items-center gap-2 border border-white/20 active:scale-95 transition-all"
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
